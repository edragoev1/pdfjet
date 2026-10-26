/**
 * SimpleScatterChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct SimpleScatterChartTests {
    private func chart(_ pdf: PDF) -> SimpleScatterChart {
        let font = TestSupport.helvetica(pdf)
        return SimpleScatterChart(font, font).setSize(400, 300).setLocation(50, 50)
                .setTitle("Phones and internet").setSubtitle("by country")
                .setXAxisTitle("phones").setYAxisTitle("internet")
                .add(0.1, 5).add(0.5, 30).add("Canada", 0.5, 65).add(1.2, 70)
    }

    @Test func theTrendIsTheLeastSquaresLine() {
        let line = SimpleScatterChart.trend([SimpleScatterChart.Point(x: 0, y: 1, name: ""),
                SimpleScatterChart.Point(x: 1, y: 3, name: ""), SimpleScatterChart.Point(x: 2, y: 5, name: ""),
                SimpleScatterChart.Point(x: 3, y: 7, name: "")])
        #expect(line != nil)
        #expect(abs(line!.slope - 2) < 1e-9 && abs(line!.intercept - 1) < 1e-9)
        #expect(SimpleScatterChart.trend([SimpleScatterChart.Point(x: 1, y: 1, name: ""),
                SimpleScatterChart.Point(x: 1, y: 2, name: "")]) == nil)    // points at one x
        #expect(SimpleScatterChart.trend([SimpleScatterChart.Point(x: 1, y: 1, name: "")]) == nil)
    }

    @Test(arguments: [true, false])
    func aNamedPointIsHighlighted(_ named: Bool) {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        let chart = SimpleScatterChart(font, font).setHighlightColor(Color.red).add(1, 1).add(2, 3)
        if named {
            chart.add("Canada", 1.5, 2)
        }
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("1 0 0 rg") == named)
        #expect(content.contains(TestSupport.hex("Canada")) == named)
    }

    @Test func describesItselfFromItsData() {
        let chart = chart(TestSupport.newPDF()).add(Float.nan, 1).setTrendLine(true)
        #expect(chart.getAltDescription() == "Phones and internet, by country: 4 points, internet against phones; "
                + "Canada 0.5 and 65; the trend rises.")
        chart.setAltDescription("Four countries")
        #expect(chart.getAltDescription() == "Four countries")
    }

    @Test func anAxisRangeSetIsUsed() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setYAxisRange(0, 200).drawOn(page)
        #expect(TestSupport.content(page).contains(TestSupport.hex("200")))
    }

    @Test(arguments: [false, true])
    func theDotsAreTransparentButInPDFA1(_ pdfA1: Bool) {
        let pdf = TestSupport.newPDF()
        pdf.setCompliance(pdfA1 ? Compliance.PDF_A_1B : Compliance.PDF_UA_1)
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).drawOn(page)
        #expect(TestSupport.content(page).contains(" gs") == !pdfA1)
    }

    @Test func withoutPointsDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        let xy = SimpleScatterChart(font, font).setTitle("Nothing").add(Float.nan, 1).drawOn(page)
        #expect(TestSupport.content(page) == "")
        TestSupport.expectNear(400, xy[0])
        TestSupport.expectNear(300, xy[1])
    }
}
