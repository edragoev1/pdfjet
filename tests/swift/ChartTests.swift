/**
 * ChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct ChartTests {
    private func chart(_ pdf: PDF) -> Chart {
        let font = TestSupport.helvetica(pdf)
        return Chart(font, font).setLocation(50, 50).setSize(300, 200)
    }

    private func draw(_ ys: Float...) -> String {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        let series = chart.addSeries("")
        for (i, y) in ys.enumerated() {
            series.addPoint(Float(i) + 1, y)
        }
        chart.drawOn(page)
        return TestSupport.content(page)
    }

    @Test func aChartWithoutPointsDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSeries("empty")
        TestSupport.expectXY(350, 250, chart.drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func allNegativeDataGetsNegativeAxisLabels() {
        let content = draw(-5, -2.5, -1)
        #expect(content.contains(TestSupport.hex("-5.0")), "\(content)")
        #expect(content.contains(TestSupport.hex("-1.0")), "\(content)")
        #expect(!content.contains("NaN"))
    }

    @Test func flatDataIsDrawnWithoutNaN() {
        let content = draw(3, 3, 3)
        #expect(!content.contains("NaN"))
        #expect(content.contains(TestSupport.hex("3.0")), "\(content)")
        #expect(content.contains(TestSupport.hex("4.0")), "\(content)")
    }

    @Test func wholeNumberStepsGetWholeNumberLabels() {
        let content = draw(10, 60, 35)
        #expect(content.contains(TestSupport.hex("60")), "\(content)")
        #expect(!content.contains(TestSupport.hex("60.00")), "\(content)")
    }

    @Test func aPathSeriesKeepsItsStrokeWidthAndIsListedInTheLegend() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSeries("label").setDrawPath(true).setShape(Shape.INVISIBLE)
                .setStrokeWidth(20).setStrokeColor(Color.blue)
                .addPoint(1, 2).addPoint(3, 2)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("20 w"), "\(content)")
        #expect(content.contains(TestSupport.hex("label")), "\(content)")

        page = Page(pdf, Letter.PORTRAIT)
        chart.setDrawLegend(false).drawOn(page)
        #expect(!TestSupport.content(page).contains(TestSupport.hex("label")))
    }

    @Test func aPointWithoutAColorIsDrawnInTheColorOfItsSeries() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSeries("").setStrokeColor(Color.red)
                .addPoint(1, 1)
                .addPoint(Point(2, 2).setStrokeColor(Color.blue).setShape(Shape.BOX))
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("1 0 0 RG"), "\(content)")
        #expect(content.contains("0 0 1 RG"), "\(content)")
    }

    @Test func labelsUseAPeriodWhateverTheDefaultLocale() {
        // Java sets the default locale to German first; a Swift process cannot
        // change its current locale, so this checks the labels as they are.
        let content = draw(1, 2, 3)
        #expect(content.contains(TestSupport.hex("1.25")), "\(content)")
        #expect(!content.contains(TestSupport.hex("1,25")))
    }

    @Test func theBordersTheAxisLinesTheGridColorAndTheSubtitleWorkAsInABarChart() {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSeries("").setDrawPath(true).setShape(Shape.INVISIBLE)
                .setStrokeWidth(3).setStrokeColor(Color.blue)
                .addPoint(1, 1).addPoint(2, 2)
        chart.drawOn(page)
        var content = TestSupport.content(page)
        #expect(!content.contains("l\ns\n"), "\(content)")   // a border width of 0 hides the border
        #expect(content.contains("0.5 w\n"), "\(content)")   // the axis lines
        #expect(!content.contains("1 0 0 RG"), "\(content)")

        page = Page(pdf, Letter.PORTRAIT)
        chart.setChartBorderWidth(2).setInnerBorderWidth(1).setAxisLineWidth(0)
                .setGridLineColor(Color.red).setSubtitle("Subtitle")
        chart.drawOn(page)
        content = TestSupport.content(page)
        #expect(content.components(separatedBy: "l\ns\n").count - 1 == 2, "\(content)")
        #expect(!content.contains("0.5 w\n"), "\(content)")
        #expect(content.contains("1 0 0 RG"), "\(content)")
        #expect(content.contains(TestSupport.hex("Subtitle")), "\(content)")
    }

    @Test func theMarkerOfASeriesIsTheOneItHasWhenTheChartIsDrawn() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSeries("").addPoint(1, 1).addPoint(2, 2).setShape(Shape.INVISIBLE)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains(" c\n"), "\(content)")   // no circles
    }

    @Test func aChartDrawnAgainHasTheRangeOfItsDataThen() {
        let pdf = TestSupport.newPDF()
        let chart = chart(pdf)
        let series = chart.addSeries("").addPoint(0, 0).addPoint(10, 10)
        chart.drawOn(Page(pdf, Letter.PORTRAIT))
        series.addPoint(100, 100)
        let page = Page(pdf, Letter.PORTRAIT)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("<" + TestSupport.hex("100") + ">"), "\(content)")
    }

    @Test func anAxisWithoutGridLinesHasTheRangeOfItsData() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setXAxisMinMax(0, 10, -1).setYAxisMinMax(0, 1000, 0)
        chart.addSeries("").addPoint(1, 1).addPoint(2, 2)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains("<" + TestSupport.hex("1000") + ">"), "\(content)")
        #expect(!content.contains("<" + TestSupport.hex("10") + ">"), "\(content)")
        #expect(content.contains("<" + TestSupport.hex("2.0") + ">"), "\(content)")
    }

    @Test func theSubtitleIsGray() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setTitle("Title").setSubtitle("Subtitle")
        chart.addSeries("").addPoint(1, 1).addPoint(2, 2)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(TestSupport.fillColorBefore(content, "Title") == "0 0 0 rg")
        #expect(TestSupport.fillColorBefore(content, "Subtitle") == "0.41 0.41 0.41 rg")
    }

    @Test func aChartIsAFigureDescribedByItsTitleOrItsAlternateDescription() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let titled = chart(memory.pdf).setTitle("Sales")
        titled.addSeries("").addPoint(1, 1).addPoint(2, 2)
        titled.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\n"), "\(content)")
        #expect(content.hasSuffix("EMC\n"), "\(content)")
        let described = chart(memory.pdf).setTitle("Sales").setAltDescription("Sales rose from 1 to 2.")
        described.addSeries("").addPoint(1, 1).addPoint(2, 2)
        described.drawOn(page)
        #expect(page.structures[0].altDescription == "Sales")
        #expect(page.structures[1].altDescription == "Sales rose from 1 to 2.")
    }
}
