/**
 * SimpleLineChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct SimpleLineChartTests {
    private func chart(_ pdf: PDF) -> SimpleLineChart {
        let font = TestSupport.helvetica(pdf)
        return SimpleLineChart(font, font).setSize(400, 300).setLocation(50, 50)
                .setTitle("Vehicles").setSubtitle("by model year")
                .setCategories("2021", "2022", "2023")
                .addLine("Battery", [14755, 23507, 11853])
                .addLine("Hybrid", [3541, 4015, 1500])
    }

    // The number of paths the page strokes.
    private func strokes(_ content: String) -> Int {
        return content.components(separatedBy: "\nS\n").count - 1
    }

    @Test func describesItselfFromItsData() {
        let chart = chart(TestSupport.newPDF())
        #expect(chart.getAltDescription() ==
                "Vehicles. By model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;"
                + " Hybrid from 3,541 to 1,500, highest 4,015 in 2022.")
        chart.setAltDescription("Two lines")
        #expect(chart.getAltDescription() == "Two lines")
    }

    @Test(arguments: [true, false])
    func theHighlightedLineHasItsOwnColor(_ highlighted: Bool) {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setHighlightColor(Color.red)
        if highlighted {
            chart.setHighlight(0)
        }
        chart.drawOn(page)
        #expect(TestSupport.content(page).contains("1 0 0 RG") == highlighted)
    }

    @Test func labelsTheEndsOfItsLines() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).drawOn(page)
        let content = TestSupport.content(page)
        for label in ["Battery 11,853", "Hybrid 1,500"] {
            #expect(content.contains(TestSupport.hex(label)), "\(label)")
        }
    }

    @Test func aMissingValueLeavesAGap() {
        func strokesOf(_ values: [Float]) -> Int {
            let pdf = TestSupport.newPDF()
            let page = Page(pdf, Letter.PORTRAIT)
            let font = TestSupport.helvetica(pdf)
            SimpleLineChart(font, font).setCategories("a", "b", "c", "d", "e").addLine("line", values).drawOn(page)
            return strokes(TestSupport.content(page))
        }
        let whole = strokesOf([1, 2, 3, 4, 5])
        let broken = strokesOf([1, 2, Float.nan, 4, 5])
        #expect(broken == whole + 1, "a gap in a line of five values strokes one path more")
    }

    @Test func endLabelsDoNotMeet() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let chart = SimpleLineChart(font, font).setCategories("a", "b")
                .addLine("one", [1, 5]).addLine("two", [2, 5])     // Both end at 5
        let page = Page(pdf, Letter.PORTRAIT)
        chart.drawOn(page)
        // The two labels are drawn at two baselines a line apart
        let content = TestSupport.content(page)
        let one = TestSupport.positionOf(content, "one 5")
        let two = TestSupport.positionOf(content, "two 5")
        #expect(abs(one[1] - two[1]) >= font.getBodyHeight() - TestSupport.delta,
                "the labels are less than a line apart")
    }

    @Test func withoutValuesDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        let xy = SimpleLineChart(font, font).setTitle("Nothing").addLine("line", [Float.nan]).drawOn(page)
        #expect(TestSupport.content(page) == "")
        TestSupport.expectNear(400, xy[0])
        TestSupport.expectNear(300, xy[1])
    }

    // A value outside a range that is set is drawn past the plot, and the
    // lines are clipped at its edges; a value far past it, or values whose
    // span is past the largest float, are drawn without an error that lost
    // the document (review of 26 October 2026).
    @Test func aValueOutsideTheRangeIsClipped() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).drawOn(page)
        #expect(!TestSupport.content(page).contains("W\nn\n"))     // No clip in range
        for far: Float in [60, 1e37, -1e37] {
            page = Page(pdf, Letter.PORTRAIT)
            SimpleLineChart(font, font).setYAxisRange(0, 10).addLine("a", [5, far, 5]).drawOn(page)
            let content = TestSupport.content(page)
            #expect(content.contains("W\nn\n") && content.contains(TestSupport.hex("a 5")), "\(far): \(content)")
        }
        SimpleLineChart(font, font).addLine("wide", [-3e38, 3e38]).drawOn(Page(pdf, Letter.PORTRAIT))
        SimpleLineChart(font, font).setYAxisRange(-3e38, 3e38).addLine("a", [1, 2]).drawOn(Page(pdf, Letter.PORTRAIT))
    }

    // A range that is set has grid lines on a round step: 3 to 17 was written
    // 3.00, 5.33, 7.67 in 6 lines, and is 3, 5, 7 in 7; a range of no round
    // step in 2 to 10 lines keeps its lines (review of 26 October 2026).
    @Test func aSetRangeHasARoundStep() {
        #expect(ChartScale.axisOf([5], 3, 17, true).lines == 7)
        #expect(ChartScale.axisOf([5], 0, 10, true).lines == 10)
        #expect(ChartScale.roundLines(0, 1.3 * 3.7, 6) == 6)      // No round step in 2 to 10
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setYAxisRange(3, 17).addLine("a", [5, 9]).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("15")) && !content.contains(TestSupport.hex("5.33")), "\(content)")
    }

    // The labels at the ends of the lines, moved apart, stay over the plot:
    // they were pushed down past the bottom of the panel, and with many
    // lines up past its top. As many as the plot holds are drawn, the
    // highlighted one first, then the others in their order.
    @Test func theEndLabelsStayOverThePlot() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = SimpleLineChart(font, font).setSize(400, 120)
        for i in 0..<30 {
            chart.addLine("line \(i)", [10, 0])
        }
        chart.addLine("top", [0, 10]).setHighlight(30)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("top 10")) && content.contains(TestSupport.hex("line 0 0")),
                "\(content)")
        #expect(!content.contains(TestSupport.hex("line 29 0")), "\(content)")
        // The page is 792 high, the chart from 0 to 120 down from its top
        for i in 0..<30 where content.contains(TestSupport.hex("line \(i) 0")) {
            let y = TestSupport.positionOf(content, "line \(i) 0")[1]
            #expect(y > 792 - 120 && y < 792, "line \(i) at \(y)")
        }
    }

    // The values are written by their points but the last of each line, which
    // its end label has; the area under a line is filled in a tint of its
    // color, under the line.
    @Test func theValuesAndTheAreaAreDrawnWhenAsked() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setCategories("a", "b", "c").addLine("x", [12, 37, 23])
                .setHighlight(0).setValueLabels(true).setAreaFilled(true).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("12")) && content.contains(TestSupport.hex("37")), "\(content)")
        #expect(!content.contains("<" + TestSupport.hex("23") + ">"), "\(content)")   // Only in "x 23"
        let area = content.range(of: "0.78 0.8 0.84 rg\n")      // Navy, a quarter over white
        let line = content.range(of: "0.12 0.23 0.37 RG\n")
        #expect(area != nil && line != nil && content.startIndex < area!.lowerBound
                && area!.lowerBound < line!.lowerBound, "\(content)")
        page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).addLine("x", [12, 37]).drawOn(page)
        #expect(!TestSupport.content(page).contains(TestSupport.hex("12")))
    }

    // The numbers of the line of the content that ends where the range of
    // the operator starts: "x y" of "x y m".
    private func operands(_ content: String, _ end: Range<String.Index>) -> [Float] {
        let start = content[..<end.lowerBound].lastIndex(of: "\n").map { content.index(after: $0) }
                ?? content.startIndex
        return content[start..<end.lowerBound].components(separatedBy: " ").map { Float($0)! }
    }

    // The review of 26 October 2026, the second: the value of the first point
    // of a rise is after it and under the line, and that of a peak at the top
    // of the plot under it. The fonts of a Swift chart are not optional, so
    // the Java test of a chart without them has no Swift form.
    @Test func theSecondReviewOfTheCharts() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).addLine("big", [0, 3e38]).drawOn(page)     // Had infinite coordinates
        #expect(!TestSupport.content(page).contains("inf") && !TestSupport.content(page).contains("nan"))

        page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setCategories("a", "b", "c").setYAxisRange(0, 50)
                .addLine("x", [12, 50, 23]).setValueLabels(true).drawOn(page)
        let content = TestSupport.content(page)
        // The points, from the line drawn in gray: "x y m" of the first
        // point, "x y l" of the peak
        let rg = try #require(content.range(of: "0.55 0.59 0.64 RG\n"))
        let m = try #require(content.range(of: " m\n", range: rg.upperBound..<content.endIndex))
        let start = operands(content, m)
        let first = TestSupport.positionOf(content, "12")
        #expect(first[0] > start[0] && first[1] < start[1],
                "the first value is after its point and under it: \(first[0]), \(first[1]) by \(start[0]), \(start[1])")
        let peakLine = try #require(content.range(of: " l\n", range: m.upperBound..<content.endIndex))
        let peakAt = operands(content, peakLine)
        let value = try #require(content.range(of: "<" + TestSupport.hex("50") + ">", options: .backwards))
        let td = try #require(content.range(of: " Td\n", options: .backwards,
                range: content.startIndex..<value.lowerBound))
        let valueAt = operands(content, td)
        #expect(valueAt[1] < peakAt[1], "the value of the peak at the top is under it: \(valueAt[1]) by \(peakAt[1])")
    }

    // Values under 0.01 have the digits their axis needs, where every number
    // of the axis was 0.00 and the description said from 0 to 0 (the third
    // review of 26 October 2026).
    @Test func tinyValuesHaveTheirDigits() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = SimpleLineChart(font, font).addLine("a", [0.001, 0.004])
        chart.drawOn(page)
        #expect(TestSupport.content(page).contains(TestSupport.hex("a 0.004")), "\(TestSupport.content(page))")
        #expect(chart.getAltDescription().contains("from 0.001 to 0.004"), "\(chart.getAltDescription())")
    }
}
