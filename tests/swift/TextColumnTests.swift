/**
 * TextColumnTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct TextColumnTests {
    private let eightWords = "alpha beta gamma delta epsilon zeta eta theta"

    private func column(_ font: Font, _ text: String, _ alignment: Alignment) -> TextColumn {
        let column = TextColumn()
        column.setWidth(200.0)
        column.setTextAlignment(alignment)
        let paragraph = Paragraph()
        paragraph.add(TextLine(font, text))
        column.addParagraph(paragraph)
        column.setLocation(100.0, 100.0)
        return column
    }

    // The x and y of every Td of the content, in the order they are written.
    private func positions(_ content: String) -> [[Float]] {
        var list = [[Float]]()
        for line in content.components(separatedBy: "\n") {
            if line.hasSuffix(" Td") {
                let parts = line.components(separatedBy: " ")
                list.append([Float(parts[0])!, Float(parts[1])!])
            }
        }
        return list
    }

    private func lastXOfFirstLine(_ positions: [[Float]]) -> Float {
        let y = positions[0][1]
        var lastX: Float = 0.0
        for position in positions where position[1] == y {
            lastX = position[0]
        }
        return lastX
    }

    @Test func aParagraphIsAsTallAsItsTextAndNoTaller() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        // The ascent and the descent of one line, not a whole line of spacing.
        let column = column(font, "one short line", Alignment.LEFT)
        #expect(abs(column.getSize().getHeight() - font.getBodyHeight(font.getSize()))
                < TestSupport.delta)
        // A cell measures the column as it measures its own text.
        let withColumn = Cell(font)
        let inCell = TextColumn()
        inCell.setWidth(200.0)
        let paragraph = Paragraph()
        paragraph.add(TextLine(font, "one short line"))
        inCell.addParagraph(paragraph)
        withColumn.setTextColumn(inCell)
        #expect(abs(withColumn.getHeight(200.0)
                - Cell(font, "one short line").getHeight(200.0)) < TestSupport.delta)
    }

    @Test func rightAlignedTextReachesTheRightEdge() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        column(font, eightWords, Alignment.RIGHT).drawOn(page)
        // "zeta" is the last token of the first line; its space is not text.
        let lastX = lastXOfFirstLine(positions(TestSupport.content(page)))
        #expect(abs(lastX + font.stringWidth(font.getSize(), "zeta") - 300.0) < TestSupport.delta)
    }

    @Test func aJustifiedLineReachesBothEdges() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        column(font, eightWords + " iota kappa", Alignment.JUSTIFY).drawOn(page)
        let places = positions(TestSupport.content(page))
        #expect(abs(places[0][0] - 100.0) < TestSupport.delta)
        #expect(abs(lastXOfFirstLine(places) + font.stringWidth(font.getSize(), "zeta") - 300.0)
                < TestSupport.delta)
    }

    @Test func aWordWiderThanTheColumnLeavesNoBlankLineAboveIt() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let wide = TextColumn()
        wide.setWidth(120.0)
        let paragraph = Paragraph()
        paragraph.add(TextLine(font, "Supercalifragilisticexpialidocious bbb ccc"))
        wide.addParagraph(paragraph)
        wide.setLocation(0.0, 0.0)
        // The long word on a line of its own and the two short words on the next.
        #expect(abs(wide.getSize().getHeight() - 2.0 * font.getBodyHeight(font.getSize()))
                < TestSupport.delta)
        let page = Page(pdf, Letter.PORTRAIT)
        wide.setLocation(100.0, 100.0)
        wide.drawOn(page)
        let places = positions(TestSupport.content(page))
        // The first token is drawn on the first line, at the ascent of the font.
        #expect(abs((792.0 - places[0][1]) - (100.0 + font.getAscent(font.getSize())))
                < TestSupport.delta)
    }

    @Test func theUnderlineOfALineStopsAtItsTextAndRunsThroughIt() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let underlined = TextLine(font, eightWords)
        underlined.setUnderline(true)
        let column = TextColumn()
        column.setWidth(200.0)
        let paragraph = Paragraph()
        paragraph.add(underlined)
        column.addParagraph(paragraph)
        column.setLocation(100.0, 100.0)
        column.drawOn(page)
        // The segments of a line follow each other without a gap, and the last
        // one stops at the text rather than after the space that follows it.
        var segments = [[Float]]()
        let rows = TestSupport.content(page).components(separatedBy: "\n")
        for i in 0..<(rows.count - 1) {
            if rows[i].hasSuffix(" m") && rows[i + 1].hasSuffix(" l") {
                let from = rows[i].components(separatedBy: " ")
                let to = rows[i + 1].components(separatedBy: " ")
                segments.append([Float(from[0])!, Float(to[0])!, Float(from[1])!])
            }
        }
        #expect(segments.count > 2)
        for i in 1..<segments.count where segments[i][2] == segments[i - 1][2] {
            #expect(abs(segments[i][0] - segments[i - 1][1]) < TestSupport.delta)
        }
        // The first line ends with "zeta", underlined up to its last character
        // and no further: the space after it is not text.
        var endOfFirstLine: Float = 0.0
        for segment in segments where segment[2] == segments[0][2] {
            endOfFirstLine = segment[1]
        }
        let lastTokenX = lastXOfFirstLine(positions(TestSupport.content(page)))
        #expect(abs(endOfFirstLine - (lastTokenX + font.stringWidth(font.getSize(), "zeta")))
                < TestSupport.delta)
    }
    private func count(_ str: String, _ text: String) -> Int {
        return str.components(separatedBy: text).count - 1
    }

    @Test func aParagraphIsOneStructureElementOfTheTypeItIsGiven() throws {
        // The words of a paragraph are drawn one at a time, and each was an
        // element of its own, so a reader read every word as a paragraph.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let column = TextColumn()
        column.setWidth(200.0)
        column.setLocation(100.0, 100.0)
        column.addParagraph(Paragraph()
                .setStructureType(StructElem.H1).add(TextLine(font, eightWords)))
        column.addParagraph(Paragraph().add(TextLine(font, eightWords)))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        column.drawOn(page)
        let content = TestSupport.content(page)
        // The eight words of each paragraph are its marked contents.
        #expect(count(content, "/H1 <</MCID") == 8)
        #expect(count(content, "/P <</MCID") == 8)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /H1\n") == 1)
        #expect(count(raw, "/S /P\n") == 1)
        #expect(raw.contains("/K [0 1 2 3 4 5 6 7]"))
    }

    private func drawJoinedColumn(_ width: Float, _ alignment: Alignment, _ texts: String...) -> String {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let column = TextColumn()
        column.setWidth(width)
        column.setTextAlignment(alignment)
        column.addParagraph(TextFrameTests.joinedParagraph(font, texts))
        column.setLocation(10.0, 10.0)
        let page = Page(pdf, Letter.PORTRAIT)
        column.drawOn(page)
        return TestSupport.content(page)
    }

    @Test func aJoinedTextLineHasNoSpaceBeforeIt() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let content = drawJoinedColumn(300, Alignment.LEFT, "one", "+,", "two")
        let one = TestSupport.positionOf(content, "one")
        let comma = TestSupport.positionOf(content, ",")
        let two = TestSupport.positionOf(content, "two")
        TestSupport.expectNear(one[0] + font.stringWidth("one"), comma[0])
        TestSupport.expectNear(comma[0] + font.stringWidth(", "), two[0])
    }

    @Test func aLineDoesNotBreakInsideAJoinedWord() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let width = font.stringWidth("aaa bbb") + 1
        let content = drawJoinedColumn(width, Alignment.LEFT, "aaa bbb", "+ccc")
        let aaa = TestSupport.positionOf(content, "aaa")
        let bbb = TestSupport.positionOf(content, "bbb")
        let ccc = TestSupport.positionOf(content, "ccc")
        #expect(bbb[1] < aaa[1], "bbb is on the second line")
        TestSupport.expectNear(10, bbb[0])
        TestSupport.expectNear(bbb[1], ccc[1])
        TestSupport.expectNear(bbb[0] + font.stringWidth("bbb"), ccc[0])
    }

    @Test func aJoinedWordWiderThanTheColumnBreaksWhereItIsJoined() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let content = drawJoinedColumn(font.stringWidth("abc") + 1, Alignment.LEFT, "abc", "+def")
        let abc = TestSupport.positionOf(content, "abc")
        let def = TestSupport.positionOf(content, "def")
        #expect(def[1] < abc[1], "def is on the second line")
        TestSupport.expectNear(10, def[0])
    }

    @Test func aSpaceWhereTheyMeetKeepsJoinedTextLinesApart() {
        #expect(drawJoinedColumn(300, Alignment.LEFT, "one", "two")
                == drawJoinedColumn(300, Alignment.LEFT, "one", "+ two"))
    }

    @Test func aJustifiedLineDoesNotWidenAJoin() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let content = drawJoinedColumn(200, Alignment.JUSTIFY,
                "one two three", "+,", "four five six seven eight nine ten eleven twelve")
        let three = TestSupport.positionOf(content, "three")
        let comma = TestSupport.positionOf(content, ",")
        let four = TestSupport.positionOf(content, "four")
        TestSupport.expectNear(three[1], four[1])
        TestSupport.expectNear(three[0] + font.stringWidth("three"), comma[0])
        #expect(four[0] > comma[0] + font.stringWidth(", ") + 1, "the line is justified")
    }

    @Test func theSpaceBetweenTwoTextLinesIsTheNarrowerOfTheirSpaces() throws {
        try TextFrameTests.checkTheNarrowerSpaceIsUsed(true)
    }

    @Test func aMovedSpaceDoesNotStartALine() throws {
        try TextFrameTests.checkAMovedSpaceDoesNotStartARow(true)
    }

    @Test func aJustifiedLineWidensAMovedSpace() throws {
        try TextFrameTests.checkAJustifiedRowWidensAMovedSpace(true)
    }

    @Test func aLinkEndsBeforeTheSpaceAfterIt() throws {
        try TextFrameTests.checkALinkEndsBeforeTheSpaceAfterIt(true)
    }
}
