/**
 * SimpleBarChartTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct SimpleBarChartTests {
    private func chart(_ pdf: PDF) -> SimpleBarChart {
        let font = TestSupport.helvetica(pdf)
        return SimpleBarChart(font, font).setWidth(330).setLocation(50, 50)
                .setTitle("Statement, 12 pages").setSubtitle("warm ms")
                .add("PDFjet", 7.3).add("iText", 72.6).add("OpenPDF", 18.4).add("PDFBox", 44.1)
    }

    // The widths of the rectangles filled on the page, in the order they were drawn.
    private func rectWidths(_ content: String) -> [Float] {
        let regex = try! NSRegularExpression(pattern: "([-\\d.]+) ([-\\d.]+) ([-\\d.]+) ([-\\d.]+) re")
        let range = NSRange(content.startIndex..., in: content)
        return regex.matches(in: content, range: range).map {
            Float(String(content[Range($0.range(at: 3), in: content)!]))!
        }
    }

    @Test func eachBarIsItsShareOfTheLargestValue() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).drawOn(page)
        let widths = rectWidths(TestSupport.content(page))
        #expect(widths.count == 5, "the panel and 4 bars: \(widths)")
        TestSupport.expectNear(330, widths[0])
        let longest = widths[2]     // iText's 72.6
        TestSupport.expectNear(longest * 7.3 / 72.6, widths[1], 0.05)
        TestSupport.expectNear(longest * 18.4 / 72.6, widths[3], 0.05)
        TestSupport.expectNear(longest * 44.1 / 72.6, widths[4], 0.05)
    }

    @Test(arguments: [true, false])
    func theHighlightedBarHasItsOwnColor(_ highlighted: Bool) {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf).setHighlightColor(Color.red)
        if highlighted {
            chart.setHighlight(0)
        }
        chart.drawOn(page)
        #expect(TestSupport.content(page).contains("1 0 0 rg") == highlighted)
    }

    @Test func describesItselfFromItsValues() {
        let chart = chart(TestSupport.newPDF()).setFractionDigits(1)
        #expect(chart.getAltDescription()
                == "Statement, 12 pages, warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1.")
        chart.setAltDescription("Four libraries")
        #expect(chart.getAltDescription() == "Four libraries")
    }

    @Test func valuesHaveTheDigitsTheyNeedUnlessSet() {
        let chart = chart(TestSupport.newPDF())
        #expect(chart.label(7.25) == "7.25")
        #expect(chart.label(72) == "72")
        chart.setFractionDigits(1)
        #expect(chart.label(72) == "72.0")
    }

    @Test func isAsTallAsItsRows() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let chart = chart(pdf)
        let h = chart.getHeight()
        let xy = chart.drawOn(page)
        TestSupport.expectNear(380, xy[0])
        TestSupport.expectNear(50 + h, xy[1])
        let taller = self.chart(pdf).add("Another", 10)
        TestSupport.expectNear(h + chart.rowHeight(), taller.getHeight())
    }

    @Test func aNegativeValueHasNoBarButItsValue() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        SimpleBarChart(font, font).add("up", 5).add("down", -2).drawOn(page)
        let content = TestSupport.content(page)
        #expect(rectWidths(content).count == 2, "the panel and one bar")
        #expect(content.contains(TestSupport.hex("-2")), "\(content)")
    }

    @Test func withoutBarsDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        SimpleBarChart(font, font).setTitle("Nothing").drawOn(page)
        #expect(TestSupport.content(page) == "")
    }

    // A name wider than two fifths of the chart is shortened with an
    // ellipsis, and the bars keep their room: in a chart narrower than its
    // names they went to nothing, the values past the panel (review of 26
    // October 2026). The description keeps the name whole.
    @Test func aLongNameIsShortened() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let name = "A name far too long for the column of names of this chart"
        let chart = SimpleBarChart(font, font).setWidth(200).add(name, 5).add("b", 3)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex(name)), "\(content)")
        let shortened = ChartScale.shorten(font, font.getSize(), name, 0.4 * (200 - 2 * font.getSize()))
        // The ellipsis is 85 in WinAnsi, the encoding of a core font
        let start = String(String.UnicodeScalarView(shortened.unicodeScalars.dropLast()))
        #expect(shortened.hasSuffix("\u{2026}") && content.contains(TestSupport.hex(start) + "85>"), "\(content)")
        #expect(chart.getAltDescription().contains(name))
        #expect(ChartScale.shorten(font, 10, "A name", font.stringWidth(10, "A n\u{2026}")) == "A n\u{2026}")
        #expect(ChartScale.shorten(font, 10, "A name", 100) == "A name")
        #expect(ChartScale.shorten(font, 10, "A  name", font.stringWidth(10, "A \u{2026}") - 0.1) == "A\u{2026}")
    }
}
