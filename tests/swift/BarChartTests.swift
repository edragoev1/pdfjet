/**
 * BarChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct BarChartTests {
    private func chart(_ pdf: PDF) -> BarChart {
        let font = TestSupport.helvetica(pdf)
        return BarChart(font, font).setLocation(50, 50).setSize(300, 200)
    }

    private func draw(_ chart: BarChart, _ page: Page) -> String {
        TestSupport.expectXY(350, 250, chart.drawOn(page))
        return TestSupport.content(page)
    }

    @Test func aChartWithoutCategoriesDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        _ = draw(chart(pdf), page)
        #expect(page.getContent().isEmpty)
    }

    @Test func theValueAxisStartsAtZeroWithWholeNumberLabels() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("abc", "def", "ghi")
        chart.addSeries("", [20, 75, 31])
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("80")), "\(content)")
        #expect(!content.contains(TestSupport.hex("0.00")), "\(content)")
        #expect(!content.contains(TestSupport.hex("80.00")), "\(content)")
        #expect(content.contains(TestSupport.hex("ghi")), "\(content)")
    }

    @Test func theLegendListsTheNamedSeriesInTheirColors() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a")
        chart.addSeries("first", [1], Color.red)
        chart.addSeries("", [2], Color.blue)
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("first")), "\(content)")
        #expect(content.contains("1 0 0 rg"), "\(content)")
        #expect(content.contains("0 0 1 rg"), "\(content)")
    }

    @Test func valueLabelsAreWrittenWithTheFractionDigitsSet() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a", "b").setHorizontal(true)
        chart.addSeries("", [2.5, -1]).setValueLabels(true)
        chart.setMinimumFractionDigits(1)
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("2.5")), "\(content)")
        #expect(content.contains(TestSupport.hex("-1.0")), "\(content)")
    }

    @Test func aManualAxisRangeIsUsedAsGiven() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a")
        chart.addSeries("", [5]).setValueAxisMinMax(0, 12, 4)
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("12")), "\(content)")
        #expect(content.contains(TestSupport.hex("3")), "\(content)")
        #expect(!content.contains("NaN"))
    }

    @Test func axisLabelsThatWouldMeetAreDrawnAtEverySecondLine() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a").setHorizontal(true)
        // 16 steps of 5 across about 240 points: 15 points a step, where a
        // label of one decimal, 70.0, needs more
        chart.addSeries("", [72.6]).setValueAxisMinMax(0, 80, 16).setMinimumFractionDigits(1)
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("70.0")) && content.contains(TestSupport.hex("80.0")), "\(content)")
        #expect(!content.contains(TestSupport.hex("75.0")), "\(content)")     // would meet 70.0 and 80.0
    }

    @Test func axisLabelStepIsTheFewestLinesALabelFits() {
        #expect(ChartScale.axisLabelStep(30, 20, 8) == 1)    // fits
        #expect(ChartScale.axisLabelStep(15, 20, 16) == 2)
        #expect(ChartScale.axisLabelStep(10, 25, 16) == 3)
        #expect(ChartScale.axisLabelStep(5, 100, 4) == 4)    // never more than the lines
    }

    @Test func stackedBarsUseTheSumsOfTheCategoriesForTheValueAxis() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a", "b")
        chart.addSeries("", [20, 75]).addSeries("", [30, 10])
        let grouped = draw(chart, page)
        #expect(!grouped.contains(TestSupport.hex("90")), "\(grouped)")

        page = Page(pdf, Letter.PORTRAIT)
        chart.setStacked(true).setValueLabels(true)
        let stacked = draw(chart, page)
        #expect(stacked.contains(TestSupport.hex("75")), "\(stacked)")
        // The total of each stack above it
        #expect(stacked.contains(TestSupport.hex("50")) && stacked.contains(TestSupport.hex("85")), "\(stacked)")
    }

    // The first series in navy, the grid lines light gray and solid, the text
    // gray or ink, and no black line or text.
    @Test func theDefaultLookIsQuiet() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = self.chart(pdf).setCategories("a", "b").setTitle("t").setSubtitle("s")
        chart.addSeries("one", [20.0, 75.0]).addSeries("two", [30.0, 10.0])
        let content = draw(chart, page)
        #expect(content.contains("0.12 0.23 0.37 rg"))
        #expect(content.contains("0.44 0.58 0.76 rg"))
        #expect(content.contains("0.88 0.89 0.91 RG\n0.5 w\n[] 0 d"))
        #expect(!content.contains("0 0 0 RG") && !content.contains("0 0 0 rg"))
        #expect(TestSupport.fillColorBefore(content, "t") == "0.13 0.15 0.17 rg")
        #expect(TestSupport.fillColorBefore(content, "s") == "0.37 0.41 0.45 rg")
        // A label on a dark bar is white, on a light one ink: whichever
        // contrasts more. On sage and on the old sage, which a rule by
        // brightness gave white, ink contrasts more; on slate white.
        #expect(BarChart.labelColorOn(0x1F3A5F) == Color.white)
        #expect(BarChart.labelColorOn(0x6F95C2) == 0x22262B)
        #expect(BarChart.labelColorOn(0x76987C) == 0x22262B)
        #expect(BarChart.labelColorOn(0x6B8F71) == 0x22262B)
        #expect(BarChart.labelColorOn(0x4F7A8C) == Color.white)
    }

    @Test func barsHaveTheirOwnColorsAndLabelsInsideWithGroupedDigits() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a", "b").setHorizontal(true).setSubtitle("sub")
        chart.addSeries("rivers", [6650, 12], [Color.red, Color.blue])
        chart.setValueAxisMinMax(0, 8000, 4).setValueLabels(true).setValueLabelsInside(true)
        chart.setGroupingUsed(true).setAxisLineWidth(0).setGridLineColor(Color.lightgray)
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("6,650")), "\(content)")
        #expect(content.contains(TestSupport.hex("8,000")), "\(content)")
        #expect(content.contains(TestSupport.hex("sub")), "\(content)")
        #expect(content.contains("1 0 0 rg"), "\(content)")    // the first bar
        #expect(content.contains("0 0 1 rg"), "\(content)")    // the second bar
        #expect(TestSupport.fillColorBefore(content, "6,650") == "1 1 1 rg")    // inside the first bar
        #expect(TestSupport.fillColorBefore(content, "sub") == "0.37 0.41 0.45 rg")
        #expect(content.contains(TestSupport.hex("12")), "\(content)")     // too short: next to the bar
    }

    @Test func aChartIsAFigureDescribedByItsDataOrItsAlternateDescription() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let titled = chart(memory.pdf).setTitle("Sales").setCategories("a", "b")
        titled.addSeries("", [1, 2])
        let content = draw(titled, page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\n"), "\(content)")
        #expect(content.hasSuffix("EMC\n"), "\(content)")
        let described = chart(memory.pdf).setTitle("Sales").setAltDescription("Sales rose from 1 to 2.")
                .setCategories("a", "b")
        described.addSeries("", [1, 2])
        _ = draw(described, page)
        #expect(page.structures[0].altDescription == "Sales: a 1, b 2.")
        #expect(page.structures[1].altDescription == "Sales rose from 1 to 2.")
    }

    // A category name wider than two fifths of a horizontal chart is
    // shortened with an ellipsis, and a chart too small for its plot draws
    // nothing: its axis ran backwards, the bars over the names (review of 26
    // October 2026).
    @Test func aLongNameIsShortenedAndAChartWithNoRoomDrawsNothing() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let name = "A very very long category name indeed, and longer"
        let chart = chart(pdf).setSize(200, 200).setHorizontal(true)
                .setCategories(name).addSeries("s", [5])
        TestSupport.expectXY(250, 250, chart.drawOn(page))
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex(name)), "\(content)")
        let font = TestSupport.helvetica(pdf)
        let shortened = ChartScale.shorten(font, font.getSize(), name, 0.4 * 200)
        // The ellipsis is 85 in WinAnsi, the encoding of a core font
        let start = String(String.UnicodeScalarView(shortened.unicodeScalars.dropLast()))
        #expect(shortened.hasSuffix("\u{2026}") && content.contains(TestSupport.hex(start) + "85>"), "\(content)")
        page = Page(pdf, Letter.PORTRAIT)
        self.chart(pdf).setSize(20, 20).setCategories("a").addSeries("s", [5]).drawOn(page)
        #expect(TestSupport.content(page) == "")
    }

    // A chart without a title has no room for one: its plot starts higher,
    // its bars taller. The legend wraps at the right edge, and a series with
    // a color per bar has the color of its first bar in it.
    @Test func theTopOfTheChartIsWhatItHas() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let titled = draw(chart(pdf).setTitle("t").setCategories("a").addSeries("", [1]), page)
        page = Page(pdf, Letter.PORTRAIT)
        let untitled = draw(chart(pdf).setCategories("a").addSeries("", [1]), page)
        #expect(barHeight(untitled) > barHeight(titled) + 10, "\(barHeight(untitled)) \(barHeight(titled))")
        page = Page(pdf, Letter.PORTRAIT)
        let wide = chart(pdf).setCategories("a").setLegend(true)
        for i in 0..<8 {
            wide.addSeries("Series number \(i)", [1])
        }
        var content = draw(wide, page)
        #expect(TestSupport.positionOf(content, "Series number 0")[1]
                != TestSupport.positionOf(content, "Series number 7")[1])
        page = Page(pdf, Letter.PORTRAIT)
        content = draw(chart(pdf).setCategories("a", "b").setLegend(true)
                .addSeries("own", [1, 2], [0x123456, 0x654321]), page)
        let swatch = content.range(of: "0.07 0.2 0.34 rg\n")
        let own = content.range(of: TestSupport.hex("own"))
        #expect(swatch != nil && own != nil && swatch!.lowerBound < own!.lowerBound, "\(content)")
    }

    // The height of the first bar the content fills in navy.
    private func barHeight(_ content: String) -> Float {
        guard let navy = content.range(of: "0.12 0.23 0.37 rg\n"),
              let re = content.range(of: " re\n", range: navy.upperBound..<content.endIndex) else {
            Issue.record("no navy bar in \(content)")
            return 0
        }
        let start = content[..<re.lowerBound].lastIndex(of: "\n").map { content.index(after: $0) }
                ?? content.startIndex
        let parts = content[start..<re.lowerBound].components(separatedBy: " ")
        return abs(Float(parts[3])!)
    }

    // Under vertical bars a category is written where it leaves the one
    // before it room, as the numbers of the axis are: twelve months in full
    // ran into each other.
    @Test func categoriesThatDoNotFitAreSkipped() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let months = ["January", "February", "March", "April", "May", "June", "July",
                "August", "September", "October", "November", "December"]
        var content = draw(chart(pdf).setCategories(months).addSeries("s", [Float](repeating: 1, count: 12)), page)
        #expect(content.contains(TestSupport.hex("January")), "\(content)")
        #expect(!content.contains(TestSupport.hex("February")), "\(content)")
        page = Page(pdf, Letter.PORTRAIT)
        content = draw(chart(pdf).setCategories(["a", "b"]).addSeries("s", [1, 2]), page)
        #expect(content.contains(TestSupport.hex("a")) && content.contains(TestSupport.hex("b")), "\(content)")
    }

    // A line series is drawn over the bars, in ink, with a dot at each value;
    // the axis reaches its values, the legend lists it and the description
    // has its values after the bars'.
    @Test func aLineSeriesIsDrawnOverTheBars() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        var page = Page(memory.pdf, Letter.PORTRAIT)
        let chart = chart(memory.pdf).setTitle("Sales").setCategories("a", "b").setLegend(true)
                .addSeries("Sold", [1, 2]).addLineSeries("Target", [9, Float.nan])
        var content = draw(chart, page)
        #expect(content.contains("0.13 0.15 0.17 RG\n") && content.contains("\n2 w\n"), "\(content)")   // In ink
        #expect(content.contains(TestSupport.hex("Target")), "\(content)")
        #expect(content.contains("<" + TestSupport.hex("8") + ">"), "\(content)")   // The axis reaches 9
        #expect(page.structures[0].altDescription == "Sales: Sold: a 1, b 2; Target, a line: a 9.")
        page = Page(memory.pdf, Letter.PORTRAIT)
        content = draw(self.chart(memory.pdf).setCategories("a").addSeries("", [1])
                .addLineSeries("", [2], 0x123456), page)
        #expect(content.contains("0.07 0.2 0.34 RG\n"), "\(content)")
    }

    // The review of 26 October 2026, the second: the title, a long category
    // under vertical bars and a legend name are shortened; a line series
    // longer than the categories adds no slots; the line is on a white line a
    // little wider; the old names of the setters still work; and a value past
    // the rounding of the largest float is drawn on the range of the values.
    // The fonts of a Swift chart are not optional, so the Java test of a chart
    // without them has no Swift form.
    @available(*, deprecated)   // The old names of the setters, tested until they are removed
    @Test func theSecondReviewOfTheCharts() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        var page = Page(memory.pdf, Letter.PORTRAIT)
        let longName = "A category name far too long to be written under its bar"
        let title = "A title far too long for a chart of three hundred points, which ran past it"
        var content = draw(chart(memory.pdf).setTitle(title).setLegend(true).setCategories(longName, "b")
                .addSeries("Sold", [1, 2])
                .addLineSeries("Target", [2, 3, 4, 5, 6]), page)
        #expect(!(content.contains(TestSupport.hex(longName)) || content.contains(TestSupport.hex(title))), "\(content)")
        #expect(content.contains("1 1 1 RG\n3.5 w\n"), "\(content)")
        let description = page.structures[0].altDescription ?? ""
        #expect(description.hasSuffix("Target, a line: " + longName + " 2, b 3."), "\(description)")

        let plain = TestSupport.newPDF()
        page = Page(plain, Letter.PORTRAIT)
        let old = draw(chart(plain).setCategories("a").addSeries("s", [1])
                .setDrawLegend(true).setDrawValueLabels(true).setDrawGridLines(false), page)
        page = Page(plain, Letter.PORTRAIT)
        let renamed = draw(chart(plain).setCategories("a").addSeries("s", [1])
                .setLegend(true).setValueLabels(true).setGridLines(false), page)
        // The same but for the names of the fonts, a new one for each chart
        #expect(old.replacingOccurrences(of: "/F\\d+ ", with: "/F ", options: .regularExpression)
                == renamed.replacingOccurrences(of: "/F\\d+ ", with: "/F ", options: .regularExpression))

        page = Page(memory.pdf, Letter.PORTRAIT)
        content = draw(chart(memory.pdf).setCategories("a", "b").setValueLabels(true)
                .addSeries("", [1e30, 3e38]), page)
        #expect(content.contains(TestSupport.hex("3e38")), "\(content)")
    }
}
