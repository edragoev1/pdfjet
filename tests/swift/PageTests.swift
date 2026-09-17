/**
 * PageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct PageTests {
    @Test func aNewPageTracksTheDefaultGraphicsState() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        #expect(page.getPenWidth() == 1)
        TestSupport.expectRGB(0, 0, 0, page.getPenColor())
        TestSupport.expectRGB(0, 0, 0, page.getBrushColor())
    }

    @Test func cmykSettersWriteCmykAndTrackTheRgbOfTheStandard() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setPenColorCMYK(0, 1, 1, 0)
        page.setBrushColorCMYK(0, 0, 0, 1)
        #expect(TestSupport.content(page) == "0 1 1 0 K\n0 0 0 1 k\n")
        TestSupport.expectRGB(1, 0, 0, page.getPenColor())
        TestSupport.expectRGB(0, 0, 0, page.getBrushColor())
    }

    @Test func restoreGraphicsStateRestoresTheTrackedState() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setPenColor(Int32(0xFF0000))
        page.saveGraphicsState()
        page.setPenWidth(3)
        page.setPenColor(Int32(0x00FF00))
        page.restoreGraphicsState()
        #expect(page.getPenWidth() == 1)
        TestSupport.expectRGB(1, 0, 0, page.getPenColor())
        #expect(TestSupport.content(page).hasSuffix("q\n3 w\n0 1 0 RG\nQ\n"), "\(TestSupport.content(page))")
    }

    @Test func aColorOrPenWidthThatIsSetAlreadyIsNotWrittenAgain() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setBrushColor(Color.black)     // Written, as a new page has written no color
        page.setBrushColor(Color.black)
        page.setPenColor(Color.red)
        page.setPenColor([Float(1), 0, 0])
        page.setPenWidth(0)
        page.setDefaultPenWidth()
        #expect(TestSupport.content(page) == "0 0 0 rg\n1 0 0 RG\n0 w\n")
    }

    @Test func qAndQKeepWhatTheContentHasWritten() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setBrushColor(Color.black)
        page.saveGraphicsState()
        page.setBrushColor(Color.blue)
        page.setBrushColor(Color.blue)
        page.restoreGraphicsState()
        page.setBrushColor(Color.black)     // Q restored black
        page.setBrushColor(Color.blue)
        #expect(TestSupport.content(page) == "0 0 0 rg\nq\n0 0 1 rg\nQ\n0 0 1 rg\n")
    }

    @Test func anRgbColorAfterACmykColorIsWritten() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setBrushColor(Color.black)
        page.setBrushColorCMYK(0, 0, 0, 1)
        page.setBrushColor(Color.black)
        #expect(TestSupport.content(page) == "0 0 0 rg\n0 0 0 1 k\n0 0 0 rg\n")
    }

    @Test func theFontOfTheTextIsWrittenWhenItChanges() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "a").setLocation(10, 20).drawOn(page)
        TextLine(font, "b").setLocation(10, 40).drawOn(page)
        TextLine(font, "c").setFontSize(14).setLocation(10, 60).drawOn(page)
        page.saveGraphicsState()
        TextLine(font, "d").setLocation(10, 80).drawOn(page)
        page.restoreGraphicsState()
        TextLine(font, "e").setFontSize(14).setLocation(10, 100).drawOn(page)
        let content = TestSupport.content(page)
        let fonts = content.split(separator: "\n").filter { $0.hasSuffix(" Tf") }.map { String($0.split(separator: " ", maxSplits: 1)[1]) }
        #expect(fonts == ["12 Tf", "14 Tf", "12 Tf"], "\(content)")
    }

    @Test func fillRectWritesOneRectangleWithTheEdgesOfThePath() {
        var page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.fillRect(10, 20, 30, 40)
        #expect(TestSupport.content(page) == "10 732 30 40 re\nf\n")

        // A path wrote the top edge at 792 and the bottom one at 791.99, where
        // rounding the height alone would make it 0.
        page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.fillRect(0, 0.004, 1, 0.004)
        #expect(TestSupport.content(page) == "0 791.99 1 0.01 re\nf\n")

        // Far outside the page the rectangle is still a path.
        page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.fillRect(200000, 0, 10, 10)
        #expect(TestSupport.content(page) == "200000 792 m\n200010 792 l\n200010 782 l\n200000 782 l\nf\n")
    }

    @Test func gettersReturnCopies() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        var pen = page.getPenColor()
        pen[0] = 1
        var brush = page.getBrushColor()
        brush[0] = 1
        TestSupport.expectRGB(0, 0, 0, page.getPenColor())
        TestSupport.expectRGB(0, 0, 0, page.getBrushColor())
        page.drawLine(0, 0, 10, 10)
        var content = page.getContent()
        content[0] = UInt8(ascii: "X")
        #expect(page.getContent()[0] != UInt8(ascii: "X"))
    }

    @Test func appendWritesStringsInUtf8AndIntegersAndNumbersInDecimal() {
        let euros = String(repeating: "\u{20AC}", count: 256)
        let digits = String(repeating: "0123456789", count: 1000) + "\u{E9}"
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.append("BT \u{E9}\u{2260}\u{1F600} ")
        page.append(-2147483648 as Int)
        page.append(UInt8(ascii: " "))
        page.append(2147483647 as Int)
        page.append(UInt8(ascii: " "))
        page.append(Float(-8388607.5))
        page.append(UInt8(ascii: " "))
        page.append(Float(0.125))
        page.append(UInt8(ascii: " "))
        page.append(euros)
        page.append(UInt8(ascii: " "))
        page.append(digits)
        let expected = "BT \u{E9}\u{2260}\u{1F600} -2147483648 2147483647 -8388607.5 0.13 " + euros + " " + digits
        #expect(page.getContent() == Array(expected.utf8))
    }

    @Test func drawLineWritesAStrokedPathWithTheYFlipped() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.drawLine(10, 20, 30, 40)
        let content = TestSupport.content(page)
        #expect(content.contains("10 772 m\n30 752 l\nS\n"), "\(content)")
    }

    @Test func aGoToLinkPointsAtItsDestinationOnAnotherPage() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let page1 = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(font, "Go").setGoToAction("there").setLocation(50, 50).drawOn(page1)
        Rect(10, 10, 20, 20).setGoToAction("there").drawOn(page1)
        TextLine(font, "Nowhere").setGoToAction("missing").setLocation(50, 100).drawOn(page1)
        let page2 = Page(memory.pdf, Letter.PORTRAIT)
        page2.addDestination("there", 30, 100)
        try memory.pdf.complete()
        let file = TestSupport.latin1(memory.bytes)
        // The text and the rect link to the destination, 100 points down page 2; the
        // link to a destination no page has is written without a /Dest
        #expect(file.components(separatedBy: "/Dest [").count - 1 == 2, "\(file)")
        #expect(file.components(separatedBy: "/XYZ 30 692 0]").count - 1 == 2, "\(file)")
        #expect(file.components(separatedBy: "/Subtype /Link").count - 1 == 3, "\(file)")
    }

    @Test func aTextLineAddsItsDestinationWhenItIsDrawn() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let page1 = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(font, "Go").setGoToAction("there").setLocation(50, 50).drawOn(page1)
        let page2 = Page(memory.pdf, Letter.PORTRAIT)
        let target = TextLine(font, "There").setDestination("there")
        target.setLocation(30, 100 + font.getSize())
        target.drawOn(page2)
        try memory.pdf.complete()
        let file = TestSupport.latin1(memory.bytes)
        // The destination is at the left edge of page 2, a font size above the baseline.
        #expect(target.getDestination() == "there")
        #expect(file.components(separatedBy: "/Dest [").count - 1 == 1, "\(file)")
        #expect(file.components(separatedBy: "/XYZ 0 692 0]").count - 1 == 1, "\(file)")
    }

    @Test func positiveAnglesTurnClockwise() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        // y grows downward, so text turned a quarter clockwise runs down the page
        // and text turned a quarter counterclockwise runs up from its location.
        let down = TextLine(font, "Down").setTextRotation(90).setLocation(100, 100).drawOn(page)
        let up = TextLine(font, "Up").setTextRotation(-90).setLocation(300, 100).drawOn(page)
        #expect(down[1] > 100 + font.stringWidth("Down") / 2, "down \(down[1])")
        #expect(abs(up[1] - 100) < 0.01, "up \(up[1])")
        page.setRotation(90)
        try memory.pdf.complete()
        let file = TestSupport.latin1(memory.bytes)
        #expect(file.contains("/Rotate 90"), "no /Rotate 90")
    }

    @Test func aPathWithFewerThanTwoPointsPaintsNothing() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        var path = [Point]()
        page.drawPath(path, PathOperator.STROKE)
        path.append(Point(10, 10))
        page.drawPath(path, PathOperator.STROKE)
        #expect(TestSupport.content(page) == "")
    }

    @Test func theShapesAreDrawnOrFilled() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.drawCircle(50, 50, 10)
        page.fillCircle(50, 50, 10)
        page.drawRoundedRect(10, 10, 100, 50, 5, 5)
        page.fillRoundedRect(10, 10, 100, 50, 5, 5)
        // A drawn shape is stroked with S, and a filled one filled with f.
        let painted = TestSupport.content(page)
                .split(whereSeparator: { $0 == " " || $0 == "\n" })
                .map(String.init)
                .filter { $0 == "S" || $0 == "f" }
        #expect(painted == ["S", "f", "S", "f"])
    }

    @Test func aRadioButtonFontSizeLeavesTheFontAlone() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        RadioButton(font, "Yes").setLocation(50, 50).setFontSize(20).drawOn(page)
        #expect(font.getSize() == 12)
        #expect(TestSupport.content(page).contains(" 20 Tf\n"), "\(TestSupport.content(page))")
    }

    @Test func importedContentIsSeparatedFromTheOperatorAfterIt() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.drawContents(TestSupport.bytes("BT ET"), 100, 0, 0, 1, 1)
        #expect(TestSupport.content(page).contains("BT ET\nQ\n"), "\(TestSupport.content(page))")
    }
}
