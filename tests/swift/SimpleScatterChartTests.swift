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
    // In a PDF/UA document a highlighted point that is a link is a figure of
    // its own, described by its name, in the Link that holds its annotation;
    // a point that is not a link stays in the chart's figure.
    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"), "the fonts directory is not here"))
    func aLinkedPointIsAFigureInItsLink() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).setSize(300, 200).setLocation(50, 50)
                .add(1, 1).add("Andorra", 2, 2, "https://pdfjet.com/a")
                .add("Belize", 3, 1, "https://pdfjet.com/b").add("Chad", 4, 3)
                .drawOn(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        let all = try TestSupport.structElements(raw)
        let links = all.filter { $0.value[0] == "Link" }
        #expect(links.count == 2)
        for (number, link) in links {
            let figure = link[2].trimmingCharacters(in: CharacterSet(charactersIn: "["))
                .components(separatedBy: " ")[0]
            #expect(all[figure]?[0] == "Figure" && all[figure]?[1] == number && link[2].contains("/Type /OBJR"),
                    "the Link \(number) holds \(link[2])")
        }
        for name in ["Andorra", "Belize"] {
            #expect(raw.contains("/Alt " + TestSupport.textString(name)), "no figure is described as \(name)")
        }
        #expect(raw.components(separatedBy: "/Subtype /Link").count - 1 == 2)
    }

    // In a document that is not tagged, a linked point is drawn in the chart,
    // its dot and its name the link, and no elements are made.
    @Test func aLinkedPointOfADocumentNotTaggedIsDrawnInTheChart() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).add(1, 1).add("Andorra", 2, 2, "https://pdfjet.com/a").drawOn(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(!raw.contains("/StructElem"))
        #expect(raw.components(separatedBy: "/Subtype /Link").count - 1 == 1)
        #expect(raw.contains("/URI <" + TestSupport.hex("https://pdfjet.com/a").lowercased() + ">"), "the URI")
    }

    // A point outside a range that is set is not drawn; a point far past it,
    // or points whose span is past the largest float, are drawn without an
    // error that lost the document (review of 26 October 2026).
    @Test func aPointOutsideTheRangeIsNotDrawn() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).setXAxisRange(0, 2).setYAxisRange(0, 100).add("Far", 20, 30).add(1e37, 1e37).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("Canada")), "\(content)")
        #expect(!content.contains(TestSupport.hex("Far")), "\(content)")
        SimpleScatterChart(font, font).add(-3e38, -3e38).add(3e38, 3e38).drawOn(Page(pdf, Letter.PORTRAIT))
    }
}
