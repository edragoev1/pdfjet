/**
 * BarChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
        chart.addSeries("", [2.5, -1]).setDrawValueLabels(true)
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
        chart.setStacked(true).setDrawValueLabels(true)
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
        chart.setValueAxisMinMax(0, 8000, 4).setDrawValueLabels(true).setValueLabelsInside(true)
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

    @Test func aChartIsAFigureDescribedByItsTitleOrItsAlternateDescription() {
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
        #expect(page.structures[0].altDescription == "Sales")
        #expect(page.structures[1].altDescription == "Sales rose from 1 to 2.")
    }

    // A horizontal chart narrower than its categories draws nothing: its
    // axis ran backwards, the bars over the categories (review of 26 October
    // 2026).
    @Test func aChartWithNoRoomForItsPlotDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setSize(200, 200).setHorizontal(true)
                .setCategories("A very very long category name indeed, and longer")
                .addSeries("s", [5])
        TestSupport.expectXY(250, 250, chart.drawOn(page))
        #expect(TestSupport.content(page) == "")
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
}
