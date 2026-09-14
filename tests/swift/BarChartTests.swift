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
        #expect(stacked.contains(TestSupport.hex("90")), "\(stacked)")
        #expect(stacked.contains(TestSupport.hex("75")), "\(stacked)")
    }

    @Test func barsHaveTheirOwnColorsAndLabelsInsideWithGroupedDigits() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setCategories("a", "b").setHorizontal(true).setSubtitle("sub")
        chart.addSeries("", [6650, 12], [Color.red, Color.blue])
        chart.setValueAxisMinMax(0, 8000, 4).setDrawValueLabels(true).setValueLabelsInside(true)
        chart.setGroupingUsed(true).setAxisLineWidth(0).setGridLineColor(Color.lightgray)
        let content = draw(chart, page)
        #expect(content.contains(TestSupport.hex("6,650")), "\(content)")
        #expect(content.contains(TestSupport.hex("8,000")), "\(content)")
        #expect(content.contains(TestSupport.hex("sub")), "\(content)")
        #expect(content.contains("1 0 0 rg"), "\(content)")    // the first bar
        #expect(content.contains("0 0 1 rg"), "\(content)")    // the second bar
        #expect(content.contains("1 1 1 rg"), "\(content)")    // the label inside the first bar
        #expect(content.contains(TestSupport.hex("12")), "\(content)")     // too short: next to the bar
    }
}
