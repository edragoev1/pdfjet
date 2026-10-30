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
                .setTitle("Life expectancy and children per woman").setSubtitle("by country")
                .setXAxisTitle("children per woman").setYAxisTitle("life expectancy")
                .add(5.2, 61).add(3, 70).add("Canada", 1.3, 82).add(2, 76)
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
        #expect(chart.getAltDescription() == "Life expectancy and children per woman. By country: 4 points, "
                + "life expectancy against children per woman; Canada 1.3 and 82; the trend falls.")
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
    @Test
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

    // A linked dot is drawn as a point of add(x, y) is, stroke for stroke, with
    // nothing beside it: in a document that is not tagged it adds a link alone,
    // described by its name, and the chart's description counts it and does
    // not name it.
    @Test(arguments: [false, true])
    func aLinkedDotIsADotWithADescription(_ withSeries: Bool) throws {
        let plain = try drawDot(withSeries, false)
        let dot = try drawDot(withSeries, true)
        #expect(dot.content == plain.content, "not drawn as a plain point")
        #expect(dot.alt == plain.alt, "not described as a plain point")
        #expect(dot.raw.components(separatedBy: "/Subtype /Link").count - 1 == 1)
        #expect(dot.raw.contains("/Contents " + TestSupport.textString("Andorra: 84.0 years")), "the name")
        #expect(dot.raw.contains("/URI <" + TestSupport.hex("https://pdfjet.com/a").lowercased() + ">"), "the URI")
    }

    // The content, the description and the bytes of a chart with a third point
    // that is a linked dot, or a plain one.
    private func drawDot(_ withSeries: Bool, _ linked: Bool) throws -> (content: String, alt: String, raw: String) {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let chart = SimpleScatterChart(font, font).setTitle("T").setTrendLine(true).add(1, 1).add(3, 4)
        if withSeries {
            chart.addSeries("Series")
        }
        if linked {
            chart.addLinkedDot("Andorra: 84.0 years", 2, 2, "https://pdfjet.com/a")
        } else {
            chart.add(2, 2)
        }
        let alt = chart.getAltDescription()
        chart.drawOn(page)
        let content = TestSupport.content(page)
        try memory.pdf.complete()
        return (content, alt, TestSupport.latin1(memory.bytes))
    }

    // In a PDF/UA document a linked dot is a figure of its own in the Link that
    // holds its annotation, as a linked point is, and looks as it does in the
    // chart: transparent, or the outline of its series, undashed. A highlighted
    // point stays over it.
    @Test(arguments: [false, true])
    func aLinkedDotIsAFigureInItsLink(_ withSeries: Bool) throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        page.setStrokeDashPattern("[3 3] 0")
        let chart = SimpleScatterChart(font, font).setSize(300, 200).setLocation(50, 50)
                .setHighlightColor(Color.red).add(1, 1)
        if withSeries {
            chart.addSeries("Series")
        }
        chart.addLinkedDot("Andorra", 2, 2, "https://pdfjet.com/a")
                .addLinkedDot("Belize", 3, 1, "https://pdfjet.com/b")
                .add("Chad", 4, 3, "https://pdfjet.com/c").add("Dominica", 2, 3)
        let alt = chart.getAltDescription()
        chart.drawOn(page)
        let content = TestSupport.content(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        let all = try TestSupport.structElements(raw)
        let links = all.filter { $0.value[0] == "Link" }
        #expect(links.count == 3)
        for (number, link) in links {
            let figure = link[2].trimmingCharacters(in: CharacterSet(charactersIn: "["))
                .components(separatedBy: " ")[0]
            #expect(all[figure]?[0] == "Figure" && all[figure]?[1] == number && link[2].contains("/Type /OBJR"),
                    "the Link \(number) holds \(link[2])")
        }
        for name in ["Andorra", "Belize", "Chad"] {
            #expect(raw.contains("/Alt " + TestSupport.textString(name)), "no figure is described as \(name)")
        }
        #expect(!alt.contains("Andorra") && !alt.contains("Belize"), "a dot named in \(alt)")
        // The chart, the dots' figures, the named point over them, Chad's
        let parts = content.components(separatedBy: "/Figure <</MCID ")
        #expect(parts.count == 5, "the figures of the content")
        #expect(parts.count == 5 && parts[1].hasPrefix("0>>"))
        if parts.count == 5 {
            let dot = parts[2]
            #expect(!dot.contains("1 0 0 rg"), "\(dot)")
            #expect(dot.contains("\nS\n") == withSeries, "\(dot)")
            #expect(dot.contains("\nf\n") == !withSeries, "\(dot)")
            let before = parts[1].components(separatedBy: "EMC").last ?? ""
            #expect(before.contains("[] 0 d\n") && before.contains(" gs\n"), "\(before)")
            #expect(parts[3].contains("/Artifact BMC"), "the named point is not over the dots")
        }
    }

    // A dot's description is its name, or its URI when it has no name, and a
    // dot without a URI is a plain point; one outside a range that is set is
    // no link.
    @Test func aLinkedDotWithoutANameOrAURI() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).setSize(300, 200).setLocation(50, 50).setXAxisRange(0, 5)
                .addLinkedDot("", 1, 1, "https://pdfjet.com/a").addLinkedDot("Belize", 2, 2, "")
                .addLinkedDot("Chad", 9, 3, "https://pdfjet.com/c").drawOn(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/Subtype /Link").count - 1 == 1)
        #expect(raw.contains("/Alt " + TestSupport.textString("https://pdfjet.com/a")), "the URI")
        #expect(!raw.contains(TestSupport.textString("Belize")) && !raw.contains(TestSupport.textString("Chad")))
    }

    // The link of a dot is at least 8 points square and holds the dot, on the
    // page as written, and inside the page, in a corner too.
    @Test(arguments: [Float(2), Float(9), Float(30)])
    func theLinkOfADotCanBeHit(_ size: Float) throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        font.setSize(size)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).setSize(612, 792).setLocation(0, 0)
                .setXAxisRange(0, 4).setYAxisRange(0, 4)
                .addLinkedDot("Middle", 2, 2, "https://pdfjet.com/m")
                .addLinkedDot("Left", 0, 2, "https://pdfjet.com/l").drawOn(page)
        // Past the corners of the page, as a chart that hangs over it
        page.addAnnotation(corner(-1, -1, 0, 0))
        page.addAnnotation(corner(612, 792, 613, 793))
        try memory.pdf.complete()
        let rects = TestSupport.latin1(memory.bytes).components(separatedBy: "/Rect [")
        #expect(rects.count == 5)
        for i in 1..<rects.count {
            let rect = rects[i].components(separatedBy: "]")[0]
            let box = rect.components(separatedBy: " ").map { Float($0) ?? -1000 }
            #expect(box[2] - box[0] >= 7.999 && abs(box[3] - box[1]) >= 7.999, "\(rect)")
            #expect(i > 2 || box[2] - box[0] >= 0.9 * size, "\(rect)")
            #expect(box[0] >= 0 && box[2] <= 612 && Swift.min(box[1], box[3]) >= 0 && Swift.max(box[1], box[3]) <= 792,
                    "\(rect)")
        }
    }

    // A link with the hit size of a dot, at the box.
    private func corner(_ x1: Float, _ y1: Float, _ x2: Float, _ y2: Float) -> Annotation {
        let annotation = Annotation(Annotation.Link, x1, y1, x2, y2, nil, nil, 0.0, nil, nil,
                "https://pdfjet.com/c", nil, nil, nil, "Corner")
        annotation.hitSize = SimpleScatterChart.HIT_SIZE
        return annotation
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

    // The name of a highlighted point is on a patch of the panel's color, so
    // that the gray dots under it do not show through.
    @Test func aNameIsOnAPatchOfThePanel() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        chart(pdf).drawOn(page)
        let content = TestSupport.content(page)
        guard let name = content.range(of: "<" + TestSupport.hex("Canada")),
              let patch = content.range(of: "0.95 0.95 0.96 rg\n", options: .backwards,
                      range: content.startIndex..<name.lowerBound),
              let navy = content.range(of: "0.12 0.23 0.37 rg\n") else {
            Issue.record("no name, patch or navy dot in \(content)")
            return
        }
        #expect(patch.lowerBound > navy.lowerBound, "\(content)")      // After the navy dot
        #expect(content[patch.lowerBound..<name.lowerBound].contains(" re\n"), "\(content)")
    }

    // A long name of a point is shortened and kept in the plot (the review
    // of 26 October 2026, the second). The fonts of a Swift chart are not
    // optional, so the Java test of a chart without them has no Swift form.
    @Test func theSecondReviewOfTheCharts() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).add(0, 0).add(3e38, 3e38).drawOn(page)    // Had infinite coordinates
        let name = "A name far too long to be written whole beside its point in the chart"
        chart(pdf).add(name, 3, 65).drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex(name)), "\(content)")
        #expect(!content.contains("inf") && !content.contains("nan"), "\(content)")
    }

    // The third review of 26 October 2026: no trend line where it misses the
    // plot; an empty URI is no link; values under 0.01 have the digits their
    // axis needs; numbers are grouped in thousands; and points whose span is
    // past the largest float draw nothing.
    @Test func theThirdReviewOfTheCharts() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).setYAxisRange(0, 10).add(0, 50).add(10, 50).setTrendLine(true)
                .drawOn(page)
        #expect(!TestSupport.content(page).contains(TestSupport.hex("trend")), "\(TestSupport.content(page))")

        page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).add("A", 1, 2, "").add(2, 3).drawOn(page)
        #expect(page.annots.isEmpty)

        page = Page(pdf, Letter.PORTRAIT)
        let tiny = SimpleScatterChart(font, font).add(0.001, 1).add(0.004, 2)
        tiny.drawOn(page)
        #expect(TestSupport.content(page).contains(TestSupport.hex("0.001")), "\(TestSupport.content(page))")

        page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).add("Big", 23507, 1).add(1000, 2).drawOn(page)
        #expect(TestSupport.content(page).contains(TestSupport.hex("25,000")), "\(TestSupport.content(page))")

        page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).add(-3e38, -3e38).add(3e38, 3e38).drawOn(page)
        #expect(TestSupport.content(page).isEmpty)
    }

    // An axis title longer than the plot is shortened as the other texts of
    // the chart are, and the description keeps it whole; a title that ends a
    // sentence is not followed by a colon (the review of 28 October 2026).
    @Test func aLongAxisTitleIsShortened() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let title = "Number of battery electric vehicles registered in the state, thousands, by county"
        var chart = chart(pdf).setSize(400, 250).setXAxisTitle(title).setYAxisTitle(title)
        chart.drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains(TestSupport.hex(title)), "\(content)")
        #expect(SimpleLineChartTests.drawn(content, "Number of battery") == 2, "\(content)")
        #expect(chart.getAltDescription().contains(title + " against " + title), "\(chart.getAltDescription())")
        let font = TestSupport.helvetica(pdf)
        chart = SimpleScatterChart(font, font).setTitle("Life expectancy?").add(1, 2).add(2, 3)
        #expect(chart.getAltDescription() == "Life expectancy? 2 points.")
    }

    // The end of an axis rounded to steps is never inside the values: 591
    // steps of 0.1 come to 59.100002 in Float, and a point at 59.1 was not
    // drawn (the review of 30 October 2026, E1).
    @Test func anAxisHoldsItsValues() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        SimpleScatterChart(font, font).setSize(400, 300).add("Only", 59.1, 59.1).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("Only")), "\(content)")
        for i in 1...20000 {
            let value = Float(i) * 0.1
            let axis = ChartScale.axisOf([value], 0, 0, false)
            #expect(ChartScale.onAxis(value, axis), "\(value) on \(axis.min) to \(axis.max)")
            let flat = ChartScale.flatRange(value)
            let round = ChartScale.roundMaxAndMinValues(flat.hi, flat.lo)
            #expect(round.minValue <= flat.lo && round.maxValue >= flat.hi, "\(flat.lo) to \(flat.hi)")
        }
    }
}
