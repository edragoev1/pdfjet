/**
 * DonutChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct DonutChartTests {
    private func chart(_ pdf: PDF) -> DonutChart {
        let font = TestSupport.helvetica(pdf)
        return DonutChart(font, font).setLocation(100, 100).setRadii(100, 50)
    }

    @Test func aChartWithoutValuesDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).addSlice(Slice(0, Color.red, "none"))
        TestSupport.expectXY(300, 300, chart.drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func slicesArePercentagesOfTheSumOfTheValues() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSlice(Slice(1, Color.red, "a"))
        chart.addSlice(Slice(1, Color.green, "b"))
        chart.addSlice(Slice(2, Color.blue, "c"))
        TestSupport.expectXY(300, 300, chart.drawOn(page))
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("25%")), "\(content)")
        #expect(content.contains(TestSupport.hex("50%")), "\(content)")
        #expect(!content.contains(TestSupport.hex("100%")), "\(content)")
    }

    @Test func aPieChartHasAnInnerRadiusOfZero() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setRadii(100, 0)
        chart.addSlice(Slice(3, Color.red, "a")).addSlice(Slice(1, Color.blue, "b"))
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("75%")), "\(content)")
        #expect(content.contains("200 592 l"), "\(content)")   // the center, in PDF coordinates
    }

    @Test func aChartIsAFigureThatListsItsSlices() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let donut = chart(memory.pdf).addSlice(Slice(25, Color.red, "Apples"))
                .addSlice(Slice(75, Color.blue, "Oranges"))
        donut.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\n"), "\(content)")
        #expect(content.hasSuffix("EMC\n"), "\(content)")
        #expect(TestSupport.fillColorBefore(content, "Apples") == "0 0 0 rg")
        #expect(TestSupport.fillColorBefore(content, "25%") == "1 1 1 rg")
        chart(memory.pdf).setRadii(100, 0).addSlice(Slice(1, Color.red, "")).drawOn(page)
        chart(memory.pdf).setAltDescription("Most are oranges.")
                .addSlice(Slice(1, Color.red, "Apples")).drawOn(page)
        #expect(page.structures[0].altDescription == "Donut chart: Apples 25%, Oranges 75%")
        #expect(page.structures[1].altDescription == "Pie chart: 100%")
        #expect(page.structures[2].altDescription == "Most are oranges.")
    }
}
