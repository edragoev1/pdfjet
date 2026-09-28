/**
 * ReviewPageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The drawing of a page: shapes, text lines, containers and annotations, as
/// the review of the page drawing found them.
@Suite struct ReviewPageTests {
    private func taggedPage() -> Page {
        return Page(MemoryPDF(Compliance.PDF_UA_1).pdf, Letter.PORTRAIT)
    }

    private func newPage() -> Page {
        return Page(TestSupport.newPDF(), Letter.PORTRAIT)
    }

    private func count(_ text: String, _ part: String) -> Int {
        return text.components(separatedBy: part).count - 1
    }

    @Test func aPolygonInATurnedContainerIsTurned() {
        let page = newPage()
        let vertices: [Float] = [0, 0, 20, 0, 0, 10]
        let polygon = PolygonAnnotation()
        polygon.setLocation(10, 10)
        polygon.setVertices(vertices)
        let container = Container(100, 100)
        container.setLocation(100, 100)
        container.setRotation(90)
        container.add(polygon)
        // Drawn twice, the container turns the polygon the same both times
        _ = container.drawOn(page)
        _ = container.drawOn(page)
        #expect(page.annots.count == 2)
        for annot in page.annots {
            // The first vertex is 40 left of and above the center, 150 and
            // 150, and a quarter turn clockwise puts it 40 right of it and above it.
            TestSupport.expectNear(190, annot.x1)
            TestSupport.expectNear(792 - 110, annot.y1)
            let want: [Float] = [0, 0, 0, 20, -10, 0]
            for i in 0..<want.count {
                TestSupport.expectNear(want[i], annot.vertices![i])
            }
        }
    }

    @Test func aPointTwiceInAPathIsOffsetOnce() {
        let page = newPage()
        let point = Point(10, 10)
        let path = Path()
        path.add(point)
        path.add(Point(50, 10))
        path.add(point)
        path.setLocation(100, 0)
        let xy = path.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("110 782 m\n150 782 l\n110 782 l\n"), "\(content)")
        #expect(point.x == 10 && point.y == 10)
        TestSupport.expectXY(150, 10, xy)
    }

    @Test func aPathThatEndsOnAControlPointIsRefused() throws {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let path = [
            Point(10, 10),
            Point(20, 20, Point.CONTROL_POINT_C),
            Point(30, 20, Point.CONTROL_POINT_C)]
        page.drawPath(path, PathOperator.STROKE)
        #expect(pdf.error == Page.PATH_ENDS_ON_CONTROL_POINT)
        #expect(TestSupport.content(page) == "")

        let pdf2 = TestSupport.newPDF()
        let stamp = Stamp(pdf2).setSize(50, 50)
        try stamp.drawPath(path, PathOperator.STROKE)
        #expect(pdf2.error == Page.PATH_ENDS_ON_CONTROL_POINT)
    }

    @Test func theLinkOfATurnedTextLineCoversTheText() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "Turned")
        line.setTextRotation(90)
        line.setURIAction("https://pdfjet.com")
        line.setLocation(100, 200)
        line.drawOn(page)
        let annot = page.annots[0]
        // A quarter turn clockwise runs the text down the page from its location.
        TestSupport.expectNear(100 - font.getDescent(12), annot.x1)
        TestSupport.expectNear(100 + font.getAscent(12), annot.x2)
        TestSupport.expectNear(792 - 200, annot.y1)
        TestSupport.expectNear(792 - (200 + font.stringWidth(12, "Turned")), annot.y2)
    }

    @Test func aSuperscriptIsRaisedOnce() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let composite = CompositeTextLine(100, 100)
        composite.setFontSize(12)
        composite.addFormula(font, "x^2")
        composite.drawOn(page)
        let two = composite.getTextLine(1)!
        // Raised by the superscript position of the base font size, 0.35 of 12
        TestSupport.expectNear(792 - (100 - 4.2), TestSupport.positionOf(TestSupport.content(page), "2")[1])
        // Measured where it is drawn, the superscript reaches no higher than the x
        let top = min(100 - font.getAscent(12), 100 - 4.2 - font.getAscent(two.getFontSize()))
        TestSupport.expectNear(top, composite.getMinMaxY()[0])
        TestSupport.expectNear(100 - top, composite.getAscent())
        TestSupport.expectNear(composite.getWidth() + 100, composite.drawOn(nil)[0])
        TestSupport.expectNear(100 - 4.2, two.drawOn(nil)[1])
    }

    @Test func aHighlightedWordKeepsItsCombiningMarks() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "the cafe\u{301} is open")
        line.setHighlightColors(["cafe\u{301}": Color.red])
        line.setLocation(100, 100)
        line.drawOn(page)
        // The mark is drawn with its letter, in the color of the word; a core
        // font has no mark, and draws a space for it.
        #expect(TestSupport.fillColorBefore(TestSupport.content(page), "cafe ") == "1 0 0 rg",
                "\(TestSupport.content(page))")
    }

    @Test func aKeywordIsMatchedByItsCodePoints() {
        // A keyword of an e with an acute accent is not the same word as one
        // of an e and a combining accent, as in the other ports.
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "the cafe\u{301} is open")
        line.setHighlightColors(["caf\u{E9}": Color.red])
        line.setLocation(100, 100)
        line.drawOn(page)
        #expect(TestSupport.fillColorBefore(TestSupport.content(page), "cafe ") == "0 0 0 rg",
                "\(TestSupport.content(page))")

        // Nor is a description of the one the text of the other
        let page2 = taggedPage()
        let described = TextLine(TestSupport.helvetica(page2.pdf), "cafe\u{301}")
        described.setAltDescription("caf\u{E9}")
        described.setLocation(100, 100)
        described.drawOn(page2)
        #expect(page2.structures[0].altDescription == "caf\u{E9}")
    }

    @Test func aShapeWithNoDescriptionIsAnArtifact() throws {
        let page = taggedPage()
        Line(10, 10, 100, 10).drawOn(page)
        let arc = Arc()
        arc.setLocation(50, 50)
        arc.setRadius(10)
        arc.setSweep(90)
        arc.drawOn(page)
        CheckBox(TestSupport.helvetica(page.pdf), "").setLocation(10, 100).drawOn(page)
        let stamp = Stamp(page.pdf).setSize(20, 20)
        stamp.drawRect(0, 0, 20, 20)
        try stamp.complete()
        stamp.setLocation(200, 200).drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains("/P <<"), "\(content)")
        #expect(count(content, "/Artifact BMC") == 4, "\(content)")

        // Described, a line is read
        let page2 = taggedPage()
        Line(10, 10, 100, 10).setAltDescription("A rule").drawOn(page2)
        #expect(TestSupport.content(page2).contains("/P <</MCID 0>>"), "\(TestSupport.content(page2))")
    }

    @Test func aRunningFooterAndAWatermarkAreArtifacts() {
        let page = taggedPage()
        let font = TestSupport.helvetica(page.pdf)
        page.addFooter(TextLine(font, "Page 1"))
        page.addHeader(TextLine(font, "Report"))
        page.addWatermark(font, "DRAFT")
        let content = TestSupport.content(page)
        for subtype in ["Footer", "Header", "Watermark"] {
            #expect(content.contains("/Artifact <</Type /Pagination /Subtype /" + subtype + ">> BDC\n"), "\(content)")
        }
        #expect(!content.contains("/P <<"), "\(content)")
    }

    @Test func aRotationOfOneDegreeIsWrittenAsOne() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "Turned")
        line.setTextRotation(1)
        line.setLocation(100, 100)
        line.drawOn(page)
        #expect(TestSupport.content(page).contains("0.99985 -0.01745 0.01745 0.99985 100 692 Tm\n"),
                "\(TestSupport.content(page))")

        let page2 = newPage()
        let container = Container(10, 10)
        container.setRotation(1)
        _ = container.drawOn(page2)
        #expect(TestSupport.content(page2).contains("0.99985 -0.01745 0.01745 0.99985 0 0 cm\n"),
                "\(TestSupport.content(page2))")
    }

    @Test func preciseNumbersHaveFiveDecimals() {
        func precise(_ value: Float) -> String {
            return TestSupport.latin1(FastFloat.toPreciseByteArray(value))
        }
        #expect(precise(Float(sin(Double.pi / 180))) == "0.01745")
        #expect(precise(Float(cos(Double.pi / 180))) == "0.99985")
        #expect(precise(Float(-(0.5).squareRoot())) == "-0.70711")
        #expect(precise(1) == "1")
        #expect(precise(-1) == "-1")
        #expect(precise(0.5) == "0.5")
        #expect(precise(0.0001) == "0.0001")
        #expect(precise(Float(cos(Double.pi / 2))) == "0")
        #expect(precise(-0.0) == "0")
        #expect(precise(-0.000004) == "0")
        #expect(precise(12.25) == "12.25")
        #expect(precise(.nan) == "0")
    }

    @Test func theSizeOfAnAnnotationFollowsItsLocation() {
        let page = newPage()
        let square = SquareAnnotation()
        square.setSize(60, 30)
        square.setLocation(100, 200)
        TestSupport.expectXY(160, 230, square.drawOn(page))
        let annot = page.annots[0]
        TestSupport.expectNear(160, annot.x2)
        TestSupport.expectNear(792 - 230, annot.y2)
    }

    @Test func aCheckBoxIsTheSizeOfItsFont() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let checkBox = CheckBox(font, "Yes")
        checkBox.setFontSize(24)
        checkBox.setLocation(100, 100)
        let xy = checkBox.drawOn(nil)
        TestSupport.expectNear(100 + 3*font.getAscent(24) + font.stringWidth(24, "Yes"), xy[0])
    }

    @Test func shapesLeaveThePenAsTheyFoundIt() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        page.setPenWidth(2)
        page.setPenColor(Color.red)
        let path = Path()
        path.add(Point(10, 10))
        path.add(Point(20, 20))
        path.setStrokeWidth(5)
        path.drawOn(page)
        let line = TextLine(font, "Underlined")
        line.setUnderline(true)
        line.setStrikeout(true)
        line.setLocation(10, 50)
        line.drawOn(page)
        CheckBox(font, "Check").setLocation(10, 80).drawOn(page)
        RadioButton(font, "Radio").setLocation(10, 110).drawOn(page)
        #expect(page.getPenWidth() == 2)
        TestSupport.expectRGB(1, 0, 0, page.getPenColor())
        let content = TestSupport.content(page)
        #expect(count(content, "q\n") == 4, "\(content)")
        #expect(count(content, "Q\n") == 4, "\(content)")
    }

    @Test func anArcOfNoSweepOrOfTooMuchIsBounded() {
        let page = newPage()
        let arc = Arc()
        arc.setLocation(50, 50)
        arc.setRadius(10)
        arc.setSweep(0)
        arc.drawOn(page)
        #expect(TestSupport.content(page) == "")
        // A sweep of a billion degrees is a full turn, four curves
        arc.setSweep(1e9)
        arc.drawOn(page)
        #expect(count(TestSupport.content(page), " c\n") == 4)

        let pdf = TestSupport.newPDF()
        let page2 = Page(pdf, Letter.PORTRAIT)
        for sweep: Float in [.nan, .infinity] {
            arc.setSweep(sweep)
            arc.drawOn(page2)
            _ = page2.addArcToPath(50, 50, 10, 10, 0, sweep)
        }
        #expect(pdf.error == "The sweep of an arc must be a finite number of degrees.")
        #expect(TestSupport.content(page2) == "")
    }

    @Test func theBoundingBoxAfterAFigureIsNotItsOwn() {
        let page = taggedPage()
        page.addBDC(StructElem.FIGURE, nil, nil, "A figure")
        page.setFigureBoundingBox(10, 10, 20, 20)
        page.addEMC()
        page.setFigureBoundingBox(50, 50, 5, 5)
        #expect(page.structures[0].attributes == "<</O /Layout /BBox [10 762 30 782]>>")
    }

    @Test func anEmptyTextLineAddsItsDestination() {
        let page = newPage()
        let line = TextLine(TestSupport.helvetica(page.pdf), "")
        line.setDestination("top")
        line.setLocation(10, 100)
        line.drawOn(page)
        #expect(page.destinations.count == 1)
        #expect(page.destinations.first?.name == "top")
    }

    @Test func aFormWithoutItsFontsIsRefused() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let form = Form([Field(0, "Name", "Value")])
        form.setLocation(10, 10)
        form.drawOn(page)
        #expect(pdf.error == "A form needs a label font and a value font: setLabelFont and setValueFont.")
    }

    @Test func theLinksInNestedContainersAreWhereTheirTextIs() throws {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "Link")
        line.setURIAction("https://pdfjet.com")
        line.setLocation(1, 20)
        let inner = Container(50, 50)
        inner.setLocation(5, 5)
        inner.add(line)
        let middle = Container(100, 100)
        middle.setLocation(10, 10)
        middle.add(inner)
        let outer = Container(200, 200)
        outer.setLocation(100, 100)
        outer.add(middle)
        _ = outer.drawOn(page)
        var annot = page.annots[0]
        TestSupport.expectNear(116, annot.x1)
        TestSupport.expectNear(792 - (135 - font.getAscent(12)), annot.y1)

        // A figure is where it is drawn
        let page2 = taggedPage()
        var barcode = try Barcode(Barcode.CODE_128, "12345")
        barcode.setAltDescription("12345")
        barcode.setLocation(110, 110)
        barcode.drawOn(page2)
        let page3 = taggedPage()
        barcode = try Barcode(Barcode.CODE_128, "12345")
        barcode.setAltDescription("12345")
        barcode.setLocation(10, 10)
        let container = Container(100, 100)
        container.add(barcode)
        container.setLocation(100, 100)
        _ = container.drawOn(page3)
        #expect(page2.structures[0].attributes != nil)
        #expect(page2.structures[0].attributes == page3.structures[0].attributes)

        // Turned a quarter, the link of a text line is turned with it
        let page4 = Page(page.pdf, Letter.PORTRAIT)
        let link = TextLine(font, "Link")
        link.setURIAction("https://pdfjet.com")
        link.setLocation(0, 50)
        let turned = Container(100, 100)
        turned.setLocation(100, 100)
        turned.setRotation(90)
        turned.add(link)
        _ = turned.drawOn(page4)
        annot = page4.annots[0]
        // The text runs down the page, from 50 left of the center to 50 right of it
        TestSupport.expectNear(150 - font.getDescent(12), annot.x1)
        TestSupport.expectNear(792 - 100, annot.y1)
        TestSupport.expectNear(150 + font.getAscent(12), annot.x2)
        TestSupport.expectNear(792 - (100 + font.stringWidth(12, "Link")), annot.y2)
    }

    @Test func aDashPatternWithAVerticalTabIsRefused() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        page.setStrokeDashPattern("[3\u{0B}3] 0")
        #expect(pdf.error == "The dash pattern \"[3\u{0B}3] 0\" is not an array of non-negative numbers, " +
                "not all zero, followed by a phase, such as \"[3 3] 0\".")
    }

    @Test func theSpacesOfUnicodeAreSpaces() {
        let page = taggedPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "\u{3000}Annual\u{3000} report\u{00A0}")
        line.setStructureType(StructElem.H1)
        line.setLocation(10, 50)
        line.drawOn(page)
        #expect(page.pdf.headings[0].title == "Annual report")
        page.addBDC(StructElem.FIGURE, nil, nil, "\u{3000}")
        #expect(page.pdf.error ==
                "A figure of a tagged document, PDF/UA or PDF/A of level A, needs an alternative description.")
    }

    @Test func anEmptyURIIsNoLink() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "Text")
        line.setURIAction("")
        line.setGoToAction("")
        line.setLocation(10, 50)
        line.drawOn(page)
        let block = TextBlock(font, "Text")
        block.setURIAction("")
        block.setLocation(10, 100)
        block.drawOn(page)
        #expect(page.annots.isEmpty)
    }

    @Test func aKeywordIsLowerCasedWhateverTheLocale() {
        let page = newPage()
        let font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "LINK")
        line.setHighlightColors(["link": Color.red])
        line.setLocation(10, 50)
        line.drawOn(page)
        #expect(TestSupport.fillColorBefore(TestSupport.content(page), "LINK") == "1 0 0 rg")
    }

    @Test func aShortColorOrTransformIsRefused() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        page.setPenColor([1, 0])
        #expect(pdf.error == Page.RGB_COUNT)
        let pdf2 = TestSupport.newPDF()
        let page2 = Page(pdf2, Letter.PORTRAIT)
        page2.setBrushColor([1])
        #expect(pdf2.error == Page.RGB_COUNT)
        let pdf3 = TestSupport.newPDF()
        let page3 = Page(pdf3, Letter.PORTRAIT)
        page3.transform([1, 0, 0])
        #expect(pdf3.error == Page.TRANSFORM_COUNT)
        #expect(TestSupport.content(page3) == "")
        // The most negative and the most positive angles are angles too
        page3.setTextRotation(Int.min)
        page3.setTextRotation(Int.max)
    }
}
