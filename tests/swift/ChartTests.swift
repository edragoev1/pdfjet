/**
 * ChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
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
        memory.pdf.setTitle("Test")
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

    private static let trueTypeFont = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"

    // The structure elements of the raw PDF, by their object numbers: the S,
    // the P and the K of each.
    private func elements(_ raw: String) throws -> [String: [String]] {
        let regex = try NSRegularExpression(pattern:
            "(\\d+) 0 obj\\n<<\\n/Type /StructElem /S /(\\w+)\\n/P (\\d+) 0 R /Pg \\d+ 0 R\\n(?:/K (\\[[^\\n]*\\]|<<[^\\n]*>>|\\d+)\\n)?")
        let string = raw as NSString
        var found = [String: [String]]()
        for match in regex.matches(in: raw, range: NSRange(location: 0, length: string.length)) {
            let group = { (i: Int) -> String in
                match.range(at: i).location == NSNotFound ? "" : string.substring(with: match.range(at: i))
            }
            found[group(1)] = [group(2), group(3), group(4)]
        }
        return found
    }

    // The text as PDF writes a text string: UTF-16 with its byte order mark,
    // in lower case hexadecimal.
    private func textString(_ text: String) -> String {
        var hex = "<feff"
        for unit in text.utf16 {
            hex += String(format: "%04x", unit)
        }
        return hex + ">"
    }

    // A point of a chart that is a link is, in a tagged document, a figure of
    // its own, described by what it stands for, in the Link that holds its
    // annotation, after the chart; the chart is a figure of the rest.
    @Test(.enabled(if: TestSupport.exists(trueTypeFont), "the fonts directory is not here"))
    func aLinkedPointIsAFigureInItsLink() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let pdf = memory.pdf
        _ = pdf.setTitle("Title")
        let font = try Font(pdf, TestSupport.open(ChartTests.trueTypeFont))
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = Chart(font, font).setLocation(50, 50).setSize(300, 200)
        chart.setTitle("Countries")
        chart.addSeries("")
            .addPoint(Point(1, 1).setURIAction("https://pdfjet.com/a").setAltDescription("Andorra"))
            .addPoint(Point(2, 2).setURIAction("https://pdfjet.com/b"))
            .addPoint(3, 3)
        chart.drawOn(page)
        try pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        let all = try elements(raw)
        let links = all.filter { $0.value[0] == "Link" }
        #expect(links.count == 2)
        for (number, link) in links {
            let figure = link[2].trimmingCharacters(in: CharacterSet(charactersIn: "["))
                .components(separatedBy: " ")[0]
            #expect(all[figure]?[0] == "Figure" && all[figure]?[1] == number && link[2].contains("/Type /OBJR"),
                    "the Link \(number) holds \(link[2])")
        }
        // Described by what it stands for, or by its URI
        for want in ["Andorra", "https://pdfjet.com/b"] {
            #expect(raw.contains("/Alt " + textString(want)), "no figure is described as \(want)")
        }
    }

    // In a document that is not tagged, a chart draws its linked points in it,
    // as before, and makes no elements.
    @Test func aLinkedPointOfADocumentNotTaggedIsDrawnInTheChart() throws {
        let memory = MemoryPDF()
        let pdf = memory.pdf
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        chart.addSeries("").addPoint(Point(1, 1).setURIAction("https://pdfjet.com/a")).addPoint(2, 2)
        chart.drawOn(page)
        try pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(!raw.contains("/StructElem"))
        #expect(raw.components(separatedBy: "/Subtype /Link").count - 1 == 1)
    }
}
