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

    // The number of times the text is drawn in a core font at the start of a
    // string.
    static func drawn(_ content: String, _ text: String) -> Int {
        return content.components(separatedBy: "<" + TestSupport.hex(text)).count - 1
    }

    // An axis title longer than the plot is shortened as the other texts of
    // the chart are: the y title of a chart 250 points high ran past its top
    // and its bottom (the review of 28 October 2026).
    @Test func aLongAxisTitleIsShortened() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let title = "Number of battery electric vehicles registered in the state, thousands, by county"
        chart(pdf).setSize(400, 250).setXAxisTitle(title).setYAxisTitle(title).drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex(title)), "\(content)")
        #expect(SimpleLineChartTests.drawn(content, "Number of battery") == 2, "\(content)")
    }

    // The chart keeps copies of its categories and its values, which a Swift
    // array is: in the other ports a caller that reused one array for every
    // line drew every line with the last values (the review of 28 October
    // 2026).
    @Test func keepsCopiesOfItsData() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        var categories = ["2021", "2022"]
        var values: [Float] = [11, 12]
        let chart = SimpleLineChart(font, font).setCategories(categories).addLine("a", values)
        values[0] = 21
        values[1] = 22
        chart.addLine("b", values)
        categories[0] = "1999"
        #expect(chart.getAltDescription() == "Line chart, 2021 to 2022: a from 11 to 12; b from 21 to 22.")
    }

    // A title that ends a sentence is not followed by a comma, and a line of
    // one value is described by it alone, where they were "Sales., 2019 to
    // 2024:" and "a from 5 to 5" (the review of 28 October 2026).
    @Test func theDescriptionReadsWell() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        var chart = SimpleLineChart(font, font).setTitle("Sales.").setCategories("2019", "2024")
                .addLine("a", [5, Float.nan]).addLine("b", [1, 2])
        #expect(chart.getAltDescription() == "Sales. 2019 to 2024: a 5; b from 1 to 2.")
        chart = SimpleLineChart(font, font).setTitle("Vehicles?").addLine("a", [1, 2])
        #expect(chart.getAltDescription() == "Vehicles? a from 1 to 2.")
    }
    // The numbers of an axis written with a power of ten have the significant
    // digits that write them apart: an axis of 4.84e15 to 4.85e15 was written
    // 4.84e15 at each line (the review of 28 October 2026).
    @Test func largeNumbersOfAnAxisAreApart() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setYAxisRange(4.84e15, 4.85e15).drawOn(page)
        let content = TestSupport.content(page)
        for label in ["4.84e15", "4.841e15", "4.849e15"] {
            #expect(SimpleLineChartTests.drawn(content, label) == 1, "\(label): \(content)")
        }
        #expect(ChartScale.stepSignificant(4.84e15, 2e12) == 4)
        #expect(ChartScale.stepSignificant(12345, 1000) == 3)
    }

    // The content of the chart, on a page of its own.
    private func noteContent(_ pdf: PDF, _ chart: SimpleLineChart) -> String {
        let page = Page(pdf, Letter.PORTRAIT)
        chart.setLocation(50, 50).drawOn(page)
        return TestSupport.content(page)
    }

    private func noteChart(_ font: Font) -> SimpleLineChart {
        return SimpleLineChart(font, font).setTitle("T").setSize(300, 200).setCategories("a", "b", "c")
    }

    // A value outside a range that is set cuts its line, and the chart says
    // so, in print and in its description: an outlier was clipped without a
    // word. A value that is no number is not outside, and a chart with none
    // outside adds nothing.
    @Test func itSaysHowManyValuesAreOutsideTheRange() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let one = "1 value outside the range"
        let two = "2 values outside the range"
        let cases: [(SimpleLineChart, String)] = [
            (noteChart(font).addLine("x", [1, 100, 3]), ""),
            (noteChart(font).addLine("x", [0, 5, 10]).setYAxisRange(0, 10), ""),
            (noteChart(font).addLine("x", [1, Float.nan, Float.infinity]).setYAxisRange(0, 10), ""),
            (noteChart(font).addLine("x", [1, 2, 3, 50]).setYAxisRange(0, 10), ""),
            (noteChart(font).addLine("x", [1, 50, 3]).setYAxisRange(0, 10), one),
            (noteChart(font).addLine("x", [1, 2, -50]).setYAxisRange(0, 10), one),
            (noteChart(font).addLine("x", [1, 50, -50]).setYAxisRange(0, 10), two),
            (noteChart(font).addLine("x", [1, 50, 3]).addLine("y", [50, 2, 3]).setYAxisRange(0, 10), two)
        ]
        for (chart, note) in cases {
            let content = noteContent(pdf, chart)
            let description = chart.getAltDescription()
            if note.isEmpty {
                #expect(!content.contains(TestSupport.hex(" outside")), "\(description)")
                #expect(!description.contains("outside"), "\(description)")
            } else {
                #expect(content.contains("<" + TestSupport.hex(note) + ">"), "\(note)")
                #expect(description.hasSuffix("; " + note + "."), "\(description)")
            }
        }
    }

    // The note is in the gray of the numbers, in the small size, on a row of
    // its own inside the chart, under the categories and the x title, with the
    // plot that much lower and nothing else moved; its right end at the plot's.
    @Test func theNoteIsUnderThePlot() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let note = "1 value outside the range"
        let size = font.getSize()
        let small = 0.8 * size
        for title in ["", "year"] {
            func drawn(_ middle: Float) -> String {
                return noteContent(pdf, SimpleLineChart(font, font).setSize(300, 200).setXAxisTitle(title)
                        .setCategories("a", "b", "c").setYAxisRange(0, 10).addLine("x", [1, middle, 3]))
            }
            let without = drawn(2)
            let with = drawn(50)
            let at = TestSupport.positionOf(with, note)
            let end = at[0] + font.stringWidth(small, note)
            let c = TestSupport.positionOf(with, "c")
            #expect(abs(c[0] + font.stringWidth(size, "c") / 2 - end) <= 0.1, "the right end")
            #expect(at[0] >= 50 && end <= 350, "the note at \(at[0])")
            #expect(abs(792 - (250 - size) - at[1]) <= 0.01, "the baseline")
            #expect(abs(TestSupport.positionOf(without, "c")[1] + 1.4 * small - c[1]) <= 0.02, "the plot")
            let last = title.isEmpty ? c[1] : TestSupport.positionOf(with, title)[1]
            #expect(at[1] + font.getAscent(small) <= last - font.getDescent(size), "the note over the row above it")
            #expect(TestSupport.fillColorBefore(with, "c") == TestSupport.fillColorBefore(with, note))
        }
        // In a chart too narrow for it, shortened and inside
        let chart = SimpleLineChart(font, font).setSize(110, 200).setYAxisRange(0, 10).addLine("", [1, 50, 3])
        #expect(TestSupport.positionOf(noteContent(pdf, chart), "1 value")[0] >= 50 + size)
        #expect(chart.getAltDescription().hasSuffix("; " + note + "."))
    }

    // A description that is set is followed by the note, for a screen reader
    // to have what the chart prints; with none outside it stays as it is.
    @Test func theNoteFollowsADescriptionThatIsSet() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let chart = SimpleLineChart(font, font).addLine("a", [1, 2]).setAltDescription("A line")
        #expect(chart.getAltDescription() == "A line")
        chart.addLine("b", [1, 50]).setYAxisRange(0, 10)
        #expect(chart.getAltDescription() == "A line. 1 value outside the range.")
    }

    // In a tagged document the note is text of the chart's figure, and its
    // description says it.
    @Test func theNoteIsInTheFigure() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        SimpleLineChart(font, font).setSize(300, 200).setYAxisRange(0, 10).addLine("a", [1, 50]).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.components(separatedBy: "BDC\n").count == 2)
        let parts = content.components(separatedBy: "EMC\n")
        #expect(parts.count == 2 && !parts[1].contains("ET\n"))
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/Alt " +
                TestSupport.textString("Line chart: a from 1 to 50; 1 value outside the range.")))
    }

    // A chart barely tall enough keeps its plot, as drawn without the note,
    // and its description says the number: the note cost it the plot.
    @Test func theNoteNeverCostsThePlot() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        func plotChart(_ height: Float, _ middle: Float) -> SimpleLineChart {
            return SimpleLineChart(font, font).setSize(300, height).setYAxisRange(0, 10)
                    .addLine("a", [1, middle, 3])
        }
        // The least height that draws, found from below
        var height: Float = 1
        while !noteContent(pdf, plotChart(height, 2)).contains(" Td\n") {
            height += 0.25
        }
        for h in [height, height + 1, height + 5] {
            let without = noteContent(pdf, plotChart(h, 2))
            let chart = plotChart(h, 50)
            let with = noteContent(pdf, chart)
            #expect(!with.contains(TestSupport.hex(" outside")), "the note left no room for the plot")
            #expect(with.contains("W\nn\n"), "the plot is not drawn")
            #expect(without.components(separatedBy: " Td\n").count == with.components(separatedBy: " Td\n").count,
                    "the plot is not drawn")
            #expect(chart.getAltDescription().hasSuffix("; 1 value outside the range."))
        }
        // With the room, the note
        #expect(noteContent(pdf, plotChart(height + 20, 50)).contains(TestSupport.hex(" outside")))
    }
}
