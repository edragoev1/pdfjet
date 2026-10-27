/**
 * Markdown.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Draws a Markdown text on as many pages as it needs: headings of # and of
/// underlines, paragraphs with the inline markup of Markup, bullet and
/// numbered lists that nest, block quotes, fenced and indented code, thematic
/// breaks, the tables and the task lists of GitHub's Markdown, - [ ] and
/// - [x], drawn as boxes, and images alone in their paragraph. In a PDF/UA
/// document the headings are H1 to H6, with no level skipped, the lists are
/// lists, the boxes of the tasks their labels, read as ☐ and ☑, the quotes
/// BlockQuotes, the code Code, the tables tables and the images figures, with
/// their text as the alternate description.
///
/// It is not all of Markdown: HTML is drawn as the text it is, a line break
/// is a space, links in a table cell are its text, and an image is drawn only
/// when it is alone in its paragraph, from the directory that
/// setImageDirectory names; with none, its text is drawn instead, so that a
/// text from anyone reads no file. Please see Example_19.
///
/// To build the pages from paragraphs and images of your own, use TextFlow.
///
public class Markdown {
    // The heading sizes, as factors of the size of the text.
    private static let HEADING_SIZES: [Float] = [2.0, 1.6, 1.3, 1.15, 1.0, 0.9]
    private static let CODE_BACKGROUND: Int32 = 0xF3F5F8
    private static let RULE_COLOR: Int32 = 0xC8CCD2
    private static let QUOTE_BAR_COLOR: Int32 = 0xB0B6BF
    private static let TABLE_HEADER_BACKGROUND: Int32 = 0xEEF0F3
    private static let TASK_BOX_COLOR: Int32 = 0x8C959F
    private static let CHECK_MARK: Unicode.Scalar = "\u{2713}"
    // The least width, in sizes of the text, that quotes and lists nested in
    // each other leave their text: deeper ones are not indented further, so
    // that the text is never narrower than nothing.
    private static let MIN_WIDTH: Float = 10

    private let regular: Font
    private let bold: Font
    private let italic: Font
    private let boldItalic: Font
    private let code: Font
    private let markup: Markup
    private var headingFont: Font
    private var headingColor: Int32 = Color.transparent    // Leaves the headings in the color of the text
    private var fullWidthTables = false                     // Tables as wide as the text, or as their text
    private var checkColor: Int32 = 0x1A7F37               // Of the box of a task that is done, and of a check mark in a table
    private var marginLeft: Float = 72.0
    private var marginTop: Float = 72.0
    private var marginRight: Float = 72.0
    private var marginBottom: Float = 72.0
    private var imageDirectory: String?

    // The page being drawn, where the next block goes, and the bottom of the
    // text on a page.
    private var pdf: PDF!
    private var pages = [Page]()
    private var pageSize: PageSize!
    private var page: Page?
    private var y: Float = 0.0
    private var bottom: Float = 0.0
    private var atTop = false           // Nothing is drawn on the page yet
    private var headingLevel = 0        // Of the last heading, as it is tagged
    // The structure elements that the blocks drawn now are the kids of: a
    // list, its item and the item's body, or a quote. They are open, so that
    // what the next pages draw goes on adding to them. The last is the innermost.
    private var containers = [StructElement]()
    // The quote bars that are drawn on the page, as x and the top of the bar.
    private var quoteBars = [QuoteBar]()
    private var allowSplit = false      // The next table starts where it is, under its heading
    // The tables and the images of the blocks, made when they are first
    // measured and drawn as they were made: an image is in the PDF once. They
    // are keyed by the identity of the block, a class, as Java's
    // IdentityHashMap; they are emptied with each text, so that the identity
    // of a block gone is not met again.
    private var tables = [ObjectIdentifier: Table]()
    private var images = [ObjectIdentifier: (any Drawable)?]()
    private var imageHeights = [ObjectIdentifier: Float]()

    private final class QuoteBar {
        let x: Float
        var top: Float

        init(_ x: Float, _ top: Float) {
            self.x = x
            self.top = top
        }
    }

    ///
    /// Creates the Markdown of text in the fonts, each at its size: the size
    /// of the regular font is the size of the text, which the headings are
    /// drawn larger than.
    ///
    /// - Parameter regular: the font of the text.
    /// - Parameter bold: the font of **bold** text, and of the headings unless setHeadingFont sets another.
    /// - Parameter italic: the font of *italic* text.
    /// - Parameter boldItalic: the font of ***bold italic*** text.
    /// - Parameter code: the font of code, usually a monospaced font.
    ///
    public init(_ regular: Font, _ bold: Font, _ italic: Font, _ boldItalic: Font, _ code: Font) {
        self.regular = regular
        self.bold = bold
        self.italic = italic
        self.boldItalic = boldItalic
        self.code = code
        self.markup = Markup(regular, bold, italic, boldItalic, code)
        self.headingFont = bold
    }

    ///
    /// Sets the font of the headings, which is the bold font unless it is set.
    ///
    /// - Parameter font: the font.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setHeadingFont(_ font: Font) -> Markdown {
        self.headingFont = font
        return self
    }

    ///
    /// Sets the color of the headings, as a 0xRRGGBB value. They are in the
    /// color of the text unless it is set.
    ///
    /// - Parameter headingColor: the color.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setHeadingColor(_ headingColor: Int32) -> Markdown {
        self.headingColor = headingColor
        return self
    }

    ///
    /// Sets whether the tables are as wide as the text, their columns widened
    /// by the share of the text each holds. They are as wide as their text
    /// unless it is set, and a table wider than the text is fitted to it
    /// either way.
    ///
    /// - Parameter fullWidthTables: true for tables as wide as the text.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setFullWidthTables(_ fullWidthTables: Bool) -> Markdown {
        self.fullWidthTables = fullWidthTables
        return self
    }

    ///
    /// Sets the color of the text of links, as a 0xRRGGBB value. They are blue
    /// unless it is set.
    ///
    /// - Parameter linkColor: the color.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setLinkColor(_ linkColor: Int32) -> Markdown {
        markup.setLinkColor(linkColor)
        return self
    }

    ///
    /// Sets the color of the box of a task that is done, - [x] in the
    /// Markdown, as a 0xRRGGBB value, which has a white check mark in it, and
    /// of the text of a table cell that starts with a check mark, U+2713, as a
    /// table of features marks what each has. It is green, 0x1A7F37, unless it
    /// is set; Color.transparent leaves it as it is.
    ///
    /// - Parameter checkColor: the color.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setCheckColor(_ checkColor: Int32) -> Markdown {
        if checkColor == Color.transparent {
            return self
        }
        self.checkColor = checkColor
        return self
    }

    ///
    /// Sets the margins of the pages, in points. They are 72 points, an inch, unless they are set.
    ///
    /// - Parameter left: the left margin.
    /// - Parameter top: the top margin.
    /// - Parameter right: the right margin.
    /// - Parameter bottom: the bottom margin.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setMargins(_ left: Float, _ top: Float, _ right: Float, _ bottom: Float) -> Markdown {
        self.marginLeft = left
        self.marginTop = top
        self.marginRight = right
        self.marginBottom = bottom
        return self
    }

    ///
    /// Sets the directory that the images are read from: the source of an
    /// image in the Markdown is the file of that name in the directory, a JPEG,
    /// PNG, BMP or SVG file. A source that is an absolute path, a URL or has .. in
    /// it is not read, and neither is any image unless the directory is set:
    /// the image's text is drawn instead.
    ///
    /// - Parameter directory: the directory.
    /// - Returns: this Markdown.
    ///
    @discardableResult
    public func setImageDirectory(_ directory: String) -> Markdown {
        self.imageDirectory = directory
        return self
    }

    ///
    /// Draws the text on as many new pages as it needs. The pages are created
    /// detached and added to the list, so that a footer or a page number can be
    /// drawn on each before they are added to the PDF. A text with no blocks
    /// needs no page.
    ///
    /// - Parameter pdf: the PDF document.
    /// - Parameter text: the Markdown text.
    /// - Parameter pages: the list that receives the new pages.
    /// - Parameter pageSize: the page size, for example Letter.PORTRAIT.
    /// - Returns: the x and y coordinates of the bottom right corner of the text on the last page.
    /// - Throws: an error when an image could not be read.
    ///
    @discardableResult
    public func drawOn(_ pdf: PDF, _ text: String, _ pages: inout [Page], _ pageSize: PageSize) throws -> [Float] {
        self.pdf = pdf
        self.pages = pages
        self.pageSize = pageSize
        self.page = nil
        self.headingLevel = 0
        containers.removeAll()
        quoteBars.removeAll()
        // The tables and images of this text, whose blocks are new each time
        tables.removeAll()
        images.removeAll()
        imageHeights.removeAll()
        // The pages drawn go to the list, also when an image could not be read.
        defer {
            pages = self.pages
            self.pages = []
        }
        if !marginLeft.isFinite || !marginTop.isFinite || !marginRight.isFinite || !marginBottom.isFinite {
            // Pages were made until the memory ran out (review of 12 October
            // 2026, F4)
            throw PDFjetError(message: "The margins of the Markdown must be numbers, not NaN or infinite.")
        }
        let blocks = MarkdownParser.parse(text)
        let width = pageSize.getWidth() - marginLeft - marginRight
        // A table too wide for a page, or tables too large, make the text an
        // error, as a table of more columns was drawn as a paragraph of pipes
        // (review of 12 October 2026, R33)
        guard let cells = MarkdownParser.tableCells(blocks) else {
            throw PDFjetError(message: "A table of the Markdown has more than \(MarkdownParser.maxTableColumns)"
                    + " columns, more than a page can show.")
        }
        if cells > MarkdownParser.maxTableCells {
            throw PDFjetError(message: "The tables of the Markdown have more than \(MarkdownParser.maxTableCells)"
                    + " cells: draw a table that large with BigTable.")
        }
        if !blocks.isEmpty {
            newPage()
            try drawBlocks(blocks, marginLeft, width, false)
            finishQuoteBars()
        }
        return [marginLeft + width, (page == nil) ? marginTop : y]
    }

    // --- The flow of the blocks down the pages -----------------------------

    private func newPage() {
        if page != nil {
            finishQuoteBars()
        }
        let page = Page(pdf, pageSize, Page.DETACHED)
        self.page = page
        pages.append(page)
        y = marginTop
        bottom = pageSize.getHeight() - marginBottom
        atTop = true
        page.structParent = containers.last
        for bar in quoteBars {
            bar.top = y
        }
    }

    // Starts a new page unless the height fits under what the page has.
    private func ensure(_ height: Float) {
        if !atTop && y + height > bottom {
            newPage()
        }
    }

    // The space before a block, which the top of a page does not have.
    private func gap(_ space: Float) {
        if !atTop {
            y += space
        }
    }

    private func size() -> Float {
        return regular.getSize()
    }

    private func openContainer(_ structure: StructElem) {
        let element = page!.addStructElement(page!.structParent, structure, nil, true)
        if let element {
            containers.append(element)
            page!.structParent = element
        }
    }

    private func closeContainer() {
        if page!.structParent != nil && !containers.isEmpty {
            containers.removeLast()
            page!.structParent = containers.last
        }
    }

    // Draws the quote bars of the page, from their top down to where the text is.
    private func finishQuoteBars() {
        for bar in quoteBars {
            Markdown.drawQuoteBar(page, bar.x, bar.top, y)
        }
    }

    private static func drawQuoteBar(_ page: Page?, _ x: Float, _ top: Float, _ to: Float) {
        if to > top {
            Line(x, top, x, to).setStrokeColor(Markdown.QUOTE_BAR_COLOR).setStrokeWidth(2.0).drawOn(page)
        }
    }

    private func drawBlocks(_ blocks: [MarkdownParser.Block], _ x: Float, _ width: Float, _ tight: Bool) throws {
        for (i, block) in blocks.enumerated() {
            switch block.kind {
            case .HEADING:
                // An empty heading draws nothing, and takes no level of the
                // tags: "#" made the next "##" an H2 with no H1 over it
                if block.text!.trim().isEmpty {
                    break
                }
                drawHeading(block, x, width, try headingKeep(blocks, i, width, tight))
            case .PARAGRAPH:
                let gap = tight ? size() * 0.25 : size() * 0.75
                if i + 1 < blocks.count && Markdown.leadsIntoTable(block, blocks[i + 1]) {
                    keepWithTable(block, blocks[i + 1], gap, x, width)
                }
                self.gap(gap)
                drawParagraph(markup.paragraph(block.text!), x, width)
            case .CODE:
                drawCode(block.text!, x, width)
            case .QUOTE:
                try drawQuote(block, x, width)
            case .LIST:
                try drawList(block, x, width)
            case .RULE:
                drawRule(x, width)
            case .TABLE:
                drawTable(block, x, width)
            case .IMAGE:
                try drawImage(block, x, width)
            case .ITEM:
                break
            }
        }
    }

    // The room a heading keeps on its page: itself and what comes after it,
    // as far as a page keeps that together, so that the heading does not end
    // a page. When the two do not fit on one page, the heading and two lines,
    // or the first rows of a table under it, or of one after its lead-in,
    // which then starts there: the table went to the next page alone.
    private func headingKeep(_ blocks: [MarkdownParser.Block], _ i: Int, _ width: Float, _ tight: Bool) throws
            -> Float {
        let keep = try keepHeight(blocks, i, width, tight)
        if keep <= bottom - marginTop {
            return keep
        }
        let line = regular.getBodyHeight(size())
        let heading = headingHeight(blocks[i], width)
        if i + 1 < blocks.count && blocks[i + 1].kind == .TABLE {
            allowSplit = true
            return heading + size() * 0.75 + 4.0 * line
        }
        if i + 2 < blocks.count && Markdown.leadsIntoTable(blocks[i + 1], blocks[i + 2]) {
            allowSplit = true
            return heading + paragraphGap(tight) + paragraphHeight(blocks[i + 1].text!, width)
                    + size() * 0.75 + 4.0 * line
        }
        return heading + 2.0 * line
    }

    // The room the first block of a quote or a list item needs on its page,
    // which the block does not check, drawn where the quote's bar or the
    // item's label is: a first image, code or table was drawn into the bottom
    // margin, and a short table was split.
    private func firstKeep(_ blocks: [MarkdownParser.Block], _ width: Float, _ tight: Bool) throws -> Float {
        if blocks.isEmpty {
            return regular.getBodyHeight(size())
        }
        let keep = try (blocks[0].kind == .HEADING)
                ? headingKeep(blocks, 0, width, tight) : keepHeight(blocks, 0, width, tight)
        return min(keep, bottom - marginTop)
    }

    // The height a block needs on its page where it starts: the whole of a
    // table, an image or a lead-in and its table that fit on a page; three
    // lines of code; a heading and what it keeps; or a line of text.
    private func keepHeight(_ blocks: [MarkdownParser.Block], _ i: Int, _ width: Float, _ tight: Bool) throws
            -> Float {
        let block = blocks[i]
        let line = regular.getBodyHeight(size())
        let room = bottom - marginTop
        switch block.kind {
        case .HEADING:
            let heading = headingHeight(block, width)
            if i + 1 < blocks.count {
                let next = blocks[i + 1]
                let gap = (next.kind == .PARAGRAPH) ? paragraphGap(tight)
                        : (next.kind == .HEADING) ? headingGap(next) : size() * 0.75
                let after = try keepHeight(blocks, i + 1, width, tight)
                return heading + max(gap + after, 2.0 * line)
            }
            return heading + 2.0 * line
        case .PARAGRAPH:
            if i + 1 < blocks.count && Markdown.leadsIntoTable(block, blocks[i + 1]) {
                let keep = paragraphHeight(block.text!, width) + size() * 0.75
                        + tableOf(blocks[i + 1], width).getTotalHeight()
                if keep <= room {
                    return keep
                }
            }
            return line
        case .TABLE:
            let height = tableOf(block, width).getTotalHeight()
            return (height <= room) ? height : 4.0 * line
        case .IMAGE:
            return try (imageOf(block, width) == nil) ? line : imageHeights[ObjectIdentifier(block)]!
        case .CODE:
            return block.text!.isEmpty ? 0.0 : codeHeight(block.text!, width)
        case .QUOTE:
            return try block.children.isEmpty ? line
                    : keepHeight(block.children, 0, width - indentWithin(size() * 1.2, width), false)
        case .LIST:
            if block.children.isEmpty || block.children[0].children.isEmpty {
                return line
            }
            return try keepHeight(block.children[0].children, 0,
                    width - indentWithin(size() * (block.ordered ? 2.0 : 1.4), width), !block.loose)
        default:
            return line
        }
    }

    // The space before a paragraph.
    private func paragraphGap(_ tight: Bool) -> Float {
        return tight ? size() * 0.25 : size() * 0.75
    }

    // The height of a heading's text at the width, of all its lines: a
    // heading of two lines was kept on its page by its first.
    private func headingHeight(_ block: MarkdownParser.Block, _ width: Float) -> Float {
        let frame = TextFrame([headingParagraph(block)]).setParagraphGap(0.0)
        frame.setLocation(0.0, 0.0).setWidth(width).setHeight(bottom - marginTop)
        frame.drawOn(nil)
        return frame.getTextBottom()
    }

    // The height of a paragraph's text at the width.
    private func paragraphHeight(_ text: String, _ width: Float) -> Float {
        let frame = TextFrame([markup.paragraph(text)]).setParagraphGap(0.0)
        frame.setLocation(0.0, 0.0).setWidth(width).setHeight(bottom - marginTop)
        frame.drawOn(nil)
        return frame.getTextBottom()
    }

    // The lines of code at the width, each cut where the next character would
    // pass it, measured in the code font: by the count of characters as wide
    // as a "0", CJK and other wide characters ran past the background. The
    // widths of the characters are added up in one pass, so that a long line
    // is cut in linear time. A surrogate pair is not cut in two, a line has a
    // character at least, and a code font with no width cuts none. The lines
    // are split at each \n as in Java's text.split("\n", -1), and cut in
    // UTF-16 code units, a code point at a time.
    private func codeLines(_ text: String, _ width: Float) -> [[UInt16]] {
        let room = width - 2.0 * size() * 0.5
        var lines = [[UInt16]]()
        for source in Array(text.utf16).split(separator: 0x0A, omittingEmptySubsequences: false) {
            let line = Array(source)
            var start = 0
            var used: Float = 0.0
            var i = 0
            while i < line.count {
                let next = i + Markdown.charCount(line, i)
                let advance = code.stringWidth(code.getSize(), String(decoding: line[i..<next], as: UTF16.self))
                if i > start && used + advance > room {
                    lines.append(Array(line[start..<i]))
                    start = i
                    used = 0.0
                }
                used += advance
                i = next
            }
            lines.append(Array(line[start...]))
        }
        return lines
    }

    // The number of code units of the code point at the index, as Java's
    // Character.charCount(line.codePointAt(index)): 2 for a surrogate pair.
    private static func charCount(_ units: [UInt16], _ index: Int) -> Int {
        if UTF16.isLeadSurrogate(units[index]) && index + 1 < units.count && UTF16.isTrailSurrogate(units[index + 1]) {
            return 2
        }
        return 1
    }

    // The height code keeps together on a page: three lines at most.
    private func codeHeight(_ text: String, _ width: Float) -> Float {
        let leading = code.getBodyHeight(code.getSize()) * 1.2
        return Float(min(3, codeLines(text, width).count)) * leading + 2.0 * size() * 0.5
    }

    // A paragraph in a text frame on the page, and on the next pages for what
    // does not fit.
    private func drawParagraph(_ paragraph: Paragraph, _ x: Float, _ width: Float) {
        if paragraph.lines.isEmpty {
            return
        }
        ensure(Markdown.firstLineHeight(paragraph))
        let frame = TextFrame([paragraph]).setParagraphGap(0.0)
        frame.setLocation(x, y).setWidth(width).setHeight(bottom - y)
        frame.drawOn(page)
        while frame.hasMoreText() {
            newPage()
            frame.setLocation(x, y).setHeight(bottom - y)
            frame.drawOn(page)
        }
        y = paragraph.getY2()
        atTop = false
    }

    private static func firstLineHeight(_ paragraph: Paragraph) -> Float {
        var height: Float = 0.0
        for line in paragraph.lines {
            if let font = line.font {
                height = max(height, font.getBodyHeight(line.fontSize))
            }
        }
        return height
    }

    // A heading, larger than the text, which keeps a line of the text after
    // it on its page. Its level as it is tagged is at most one more than that
    // of the heading before it, so that no level is skipped.
    private func drawHeading(_ block: MarkdownParser.Block, _ x: Float, _ width: Float, _ keep: Float) {
        gap(headingGap(block))
        let paragraph = headingParagraph(block)
        headingLevel = min(block.level, headingLevel + 1)
        let levels = [StructElem.H1, StructElem.H2, StructElem.H3,
                StructElem.H4, StructElem.H5, StructElem.H6]
        paragraph.setStructureType(levels[headingLevel - 1])
        ensure(keep)
        drawParagraph(paragraph, x, width)
        y += size() * 0.25
    }

    // The space above a heading.
    private func headingGap(_ block: MarkdownParser.Block) -> Float {
        return size() * (block.level <= 2 ? 1.4 : 1.1)
    }

    // The text of a heading in the heading font, at its level's size and in
    // the heading color.
    private func headingParagraph(_ block: MarkdownParser.Block) -> Paragraph {
        let fontSize = size() * Markdown.HEADING_SIZES[block.level - 1]
        let paragraph = markup.paragraph(block.text!)
        for line in paragraph.lines {
            if line.font === regular {
                line.setFont(headingFont)
            } else if line.font === italic {
                line.setFont(boldItalic)
            }
            line.setFontSize(fontSize)
            if (line.getURIAction() ?? "").isEmpty && (line.getGoToAction() ?? "").isEmpty {
                line.setTextColor(headingColor)
            }
        }
        return paragraph
    }

    // Code in the code font on a light background, a line of the source at a
    // time, and a line too long for the width goes on under itself.
    private func drawCode(_ text: String, _ x: Float, _ width: Float) {
        if text.isEmpty {
            return      // An empty block of code has nothing to tag, and no background
        }
        gap(size() * 0.75)
        let padding = size() * 0.5
        let leading = code.getBodyHeight(code.getSize()) * 1.2
        let lines = codeLines(text, width)
        ensure(codeHeight(text, width))
        // Code is an inline element, so the block is a paragraph that holds
        // its Code: PAC warns of a Code right under the document.
        openContainer(StructElem.P)
        openContainer(StructElem.CODE)
        var i = 0
        while i < lines.count {
            var fit = lines.count - i    // Lines of no height all fit
            if leading > 0 {
                fit = max(1, Util.saturatingInt((bottom - y - 2.0 * padding) / leading))
            }
            let count = min(fit, lines.count - i)
            Rect(x, y, width, Float(count) * leading + 2.0 * padding)
                    .setFillColor(Markdown.CODE_BACKGROUND).drawOn(page)
            var baseline = y + padding + code.getAscent(code.getSize())
                    + (leading - code.getBodyHeight(code.getSize())) / 2.0
            for _ in 0..<count {
                if !Markdown.isBlank(lines[i]) {
                    TextLine(code, String(decoding: lines[i], as: UTF16.self)).setStructureType(StructElem.SPAN)
                            .setLocation(x + padding, baseline).drawOn(page)
                }
                baseline += leading
                i += 1
            }
            y += Float(count) * leading + 2.0 * padding
            atTop = false
            if i < lines.count {
                newPage()
            }
        }
        closeContainer()
        closeContainer()
    }

    // The indent, or less, so that the blocks inside a quote or a list are not
    // narrower than MIN_WIDTH sizes of the text.
    private func indentWithin(_ indent: Float, _ width: Float) -> Float {
        return max(0, min(indent, width - Markdown.MIN_WIDTH * size()))
    }

    // A quote, indented, with a bar on its left.
    private func drawQuote(_ block: MarkdownParser.Block, _ x: Float, _ width: Float) throws {
        gap(size() * 0.75)
        let indent = indentWithin(size() * 1.2, width)
        // Room for its first block, which does not check the room it has
        ensure(try firstKeep(block.children, width - indent, false))
        let bar = QuoteBar(x + 2.0, y)
        quoteBars.append(bar)
        openContainer(StructElem.BLOCKQUOTE)
        let top = atTop
        atTop = true        // The first block of the quote starts where the bar does.
        try drawBlocks(block.children, x + indent, width - indent, false)
        atTop = top && atTop
        closeContainer()
        quoteBars.removeAll { $0 === bar }
        Markdown.drawQuoteBar(page, bar.x, bar.top, y)
    }

    // A list: the label of each item, a bullet or a number, to the left of
    // the blocks of the item.
    private func drawList(_ list: MarkdownParser.Block, _ x: Float, _ width: Float) throws {
        gap(size() * 0.75)
        let indent = indentWithin(size() * (list.ordered ? 2.0 : 1.4), width)
        openContainer(StructElem.L)
        var number = list.start
        for item in list.children {
            if item !== list.children[0] {
                gap(list.loose ? size() * 0.5 : size() * 0.2)
            }
            ensure(try firstKeep(item.children, width - indent, !list.loose))
            openContainer(StructElem.LI)
            if item.task != .NONE {
                drawTaskBox(item.task == .DONE, x, indent)
            } else {
                let label = list.ordered ? "\(number)." : "•"
                let text = TextLine(regular, label).setStructureType(StructElem.LBL)
                let labelX = list.ordered ? x + indent - size() * 0.4 - text.getWidth() : x + size() * 0.3
                text.setLocation(labelX, y + regular.getAscent(size())).drawOn(page)
            }
            openContainer(StructElem.LBODY)
            atTop = true    // The first block of the item is on the line of its label.
            try drawBlocks(item.children, x + indent, width - indent, !list.loose)
            if atTop {
                // An item with no text still takes the line of its label.
                y += regular.getBodyHeight(size())
            }
            atTop = false
            closeContainer()
            closeContainer()
            number += 1
        }
        closeContainer()
    }

    // The box of a task in place of the label of its item, on the line of its
    // first text: a gray box, or a box of the check color with a white check
    // mark when the task is done. A screen reader reads it as ☐ or ☑, the
    // label of the item.
    private func drawTaskBox(_ done: Bool, _ x: Float, _ indent: Float) {
        let page = self.page!
        let size = self.size()
        let side = size * 0.8
        let left = x + (indent - size * 0.4 - side) / 2
        let baseline = y + regular.getAscent(size)
        let top = (baseline - size * 0.36) - side / 2
        let actualText = done ? "☑" : "☐"
        // The box is drawn with the page's paths, not a Rect and Lines, which
        // are artifacts, as artifacts may not be inside the label.
        page.addBDC(StructElem.LBL, nil, actualText, actualText)
        page.saveGraphicsState()
        let box = Markdown.roundedBox(left, top, side, side * 0.2)
        if done {
            page.setBrushColor(checkColor)
            page.drawPath(box, PathOperator.FILL)
            page.setPenColor(Color.white)
            page.setPenWidth(side * 0.14)
            page.setLineCapStyle(CapStyle.ROUND)
            page.setLineJoinStyle(JoinStyle.ROUND)
            page.moveTo(left + side * 0.24, top + side * 0.52)
            page.lineTo(left + side * 0.43, top + side * 0.70)
            page.lineTo(left + side * 0.77, top + side * 0.31)
            page.strokePath()
        } else {
            page.setBrushColor(Color.white)
            page.setPenColor(Markdown.TASK_BOX_COLOR)
            page.setPenWidth(size * 0.08)
            page.drawPath(box, PathOperator.FILL_AND_STROKE)
        }
        page.restoreGraphicsState()
        page.addEMC()
    }

    // The path of a square with rounded corners, of the side and the radius,
    // its top left corner at x and y.
    private static func roundedBox(_ x: Float, _ y: Float, _ side: Float, _ r: Float) -> [Point] {
        let k: Float = 0.55228
        let c = r * k
        let C = Point.CONTROL_POINT_C
        return [
            Point(x + r, y), Point((x + side) - r, y),
            Point(((x + side) - r) + c, y, C), Point(x + side, (y + r) - c, C), Point(x + side, y + r),
            Point(x + side, (y + side) - r),
            Point(x + side, ((y + side) - r) + c, C), Point(((x + side) - r) + c, y + side, C), Point((x + side) - r, y + side),
            Point(x + r, y + side),
            Point((x + r) - c, y + side, C), Point(x, ((y + side) - r) + c, C), Point(x, (y + side) - r),
            Point(x, y + r),
            Point(x, (y + r) - c, C), Point((x + r) - c, y, C), Point(x + r, y),
        ]
    }

    private func drawRule(_ x: Float, _ width: Float) {
        gap(size() * 0.75)
        ensure(size())
        let middle = y + size() * 0.5
        Line(x, middle, x + width, middle).setStrokeColor(Markdown.RULE_COLOR).setStrokeWidth(1.0).drawOn(page)
        y += size()
        atTop = false
    }

    // A table of the text of the cells, with the header row in bold on a
    // light background, as wide as its text or as the width when it would be
    // wider, and on the next pages for the rows that do not fit. A cell that
    // starts with a check mark is in the check color, as a table of features
    // marks what each has.
    private func drawTable(_ block: MarkdownParser.Block, _ x: Float, _ width: Float) {
        gap(size() * 0.75)
        let table = tableOf(block, width)
        // A table that fits on a page is not split: it starts the next page
        // when it does not fit in what is left of this one, but under a
        // heading that keeps it, with which it does not fit on a page
        let height = table.getTotalHeight()
        if !atTop && !allowSplit && y + height > bottom && height <= bottom - marginTop {
            newPage()
        }
        allowSplit = false
        // Room for its header and a row where it does not fit, not where it
        // does: a short table went to the next page alone
        let rowHeight = 2.0 * regular.getBodyHeight(size())
        if y + height > bottom {
            ensure(2.0 * rowHeight)
        }
        table.setLocation(x, marginTop)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(marginBottom)
        let before = pages.count
        let xy = table.drawOn(pdf, page, &pages, pageSize)
        if pages.count > before {
            // The table drew its last rows on a page of its own. The bars of
            // the quotes it is in go down to the bottom of its first page and
            // the length of the pages between: they stopped where it started
            for bar in quoteBars {
                Markdown.drawQuoteBar(page, bar.x, bar.top, bottom)
                for p in before..<(pages.count - 1) {
                    Markdown.drawQuoteBar(pages[p], bar.x, marginTop, bottom)
                }
            }
            page = pages[pages.count - 1]
            page!.structParent = containers.last
            for bar in quoteBars {
                bar.top = marginTop
            }
        }
        // A table with rows always draws them, so it has a corner.
        y = xy?[1] ?? y
        atTop = false
    }

    // Tells if the block is a paragraph that ends in a colon, which leads into
    // the table after it. Its text is trimmed as Java's String.trim() trims,
    // of the characters up to U+0020 only.
    private static func leadsIntoTable(_ block: MarkdownParser.Block, _ next: MarkdownParser.Block) -> Bool {
        if block.kind != .PARAGRAPH || next.kind != .TABLE {
            return false
        }
        let text = block.text!.trim()
        return text.hasSuffix(":") || text.hasSuffix("\u{FF1A}")     // Or the full-width colon of CJK text
    }

    // A paragraph that ends in a colon leads into the table after it, as
    // "The size of the PDFs, KB:" does, and starts a page with it when the two
    // fit on a page together but not in what is left of this one.
    private func keepWithTable(_ paragraph: MarkdownParser.Block, _ table: MarkdownParser.Block, _ gap: Float,
            _ x: Float, _ width: Float) {
        if atTop || allowSplit {
            return      // Its table starts under it, after a heading that keeps them
        }
        let room = bottom - marginTop
        let frame = TextFrame([markup.paragraph(paragraph.text!)]).setParagraphGap(0.0)
        frame.setLocation(x, y + gap).setWidth(width).setHeight(room)
        frame.drawOn(nil)
        let needed = (frame.getTextBottom() - y) + size() * 0.75 + tableOf(table, width).getTotalHeight()
        if y + needed > bottom && needed - gap <= room {
            newPage()
        }
    }

    // The table of the text of the cells, as wide as its text or as the width.
    // The table of the block, made once: it was made to be measured and made
    // again to be drawn.
    private func tableOf(_ block: MarkdownParser.Block, _ width: Float) -> Table {
        let id = ObjectIdentifier(block)
        if let table = tables[id] {
            return table
        }
        let table = makeTable(block, width)
        tables[id] = table
        return table
    }

    private func makeTable(_ block: MarkdownParser.Block, _ width: Float) -> Table {
        let blockRows = block.rows!
        let alignments = block.alignments!
        var rows = [[Cell]]()
        for r in 0..<blockRows.count {
            var row = [Cell]()
            for c in 0..<blockRows[r].count {
                let text = blockRows[r][c]
                let plain = plainText(text)
                let cell = Cell((r == 0) ? bold : cellFont(text), plain)
                cell.setTextAlignment(alignments[c])
                // The first code point, as the other ports compare the first
                // code unit: the first Character of "✓️" holds U+FE0F too
                if r > 0 && plain.unicodeScalars.first == Markdown.CHECK_MARK {
                    cell.setTextColor(checkColor)
                }
                row.append(cell)
            }
            rows.append(row)
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(bold, 0x000000, Markdown.TABLE_HEADER_BACKGROUND)
        table.setCellBorderColor(0xC8CCD2)
        table.autoAdjustColumnWidths()
        if fullWidthTables || table.getWidth() > width {
            table.fitToWidth(width)
        }
        return table
    }

    // The font of a cell: the font of its emphasis when the whole of its text
    // is in one, as **2.1** is in bold, so that a table can mark a value; the
    // regular font when it has none, or more than one, as a cell has one font.
    private func cellFont(_ text: String) -> Font {
        var font: Font? = nil
        for line in markup.paragraph(text).lines {
            if (line.text ?? "").trim().isEmpty {
                continue
            }
            if font == nil {
                font = line.font
            } else if font !== line.font {
                return regular
            }
        }
        return font ?? regular
    }

    // The text of a cell, without its inline markup.
    private func plainText(_ text: String) -> String {
        var buf = ""
        let paragraph = markup.paragraph(text)
        for i in 0..<paragraph.lines.count {
            if i > 0 && !paragraph.joinsPrevious(i) {
                buf += " "
            }
            buf += (paragraph.lines[i].text ?? "").trim()
        }
        return buf
    }

    // An image as wide as it is, or as the width when it is wider, and as tall
    // as the page when it is taller; or its text, in italic, when it is not read.
    private func drawImage(_ block: MarkdownParser.Block, _ x: Float, _ width: Float) throws {
        gap(size() * 0.75)
        guard let drawable = try imageOf(block, width) else {
            let text = block.text!
            let paragraph = Paragraph(TextLine(italic, text.isEmpty ? block.source! : text))
            drawParagraph(paragraph, x, width)
            return
        }
        let height = imageHeights[ObjectIdentifier(block)]!
        ensure(height)
        drawable.setLocation(x, y)
        drawable.drawOn(page)
        y += height
        atTop = false
    }

    // The image of the block, scaled to the width and to a page, made when it
    // is first measured or drawn, and its height kept; nil for an image
    // that is not read, whose text is drawn in its place.
    private func imageOf(_ block: MarkdownParser.Block, _ width: Float) throws -> (any Drawable)? {
        let id = ObjectIdentifier(block)
        if let cached = images[id] {
            return cached
        }
        let text = block.text!
        let source = block.source!
        var drawable: (any Drawable)? = nil
        var height: Float = 0.0
        if let path = imagePath(source) {
            let alt = text.isEmpty ? (source as NSString).lastPathComponent : text
            let scale: Float
            let imageWidth: Float
            let imageHeight: Float
            if path.lowercased().hasSuffix(".svg") {
                guard let image = try SVGImage(fileAtPath: path) else {
                    throw PDFjetError(message: "Cannot open file: " + path)
                }
                image.setAltDescription(alt)
                imageWidth = image.getWidth()
                imageHeight = image.getHeight()
                scale = min(1.0, min(width / imageWidth, (bottom - marginTop) / imageHeight))
                image.scaleBy(scale)
                drawable = image
            } else {
                let image = try Image(pdf, path)
                image.setAltDescription(alt)
                imageWidth = image.getWidth()
                imageHeight = image.getHeight()
                scale = min(1.0, min(width / imageWidth, (bottom - marginTop) / imageHeight))
                image.scaleBy(scale)
                drawable = image
            }
            height = imageHeight * scale
        }
        images[id] = .some(drawable)
        imageHeights[id] = height
        return drawable
    }

    // The path of an image in the image directory, or nil when there is no
    // directory, or the source is an absolute path, a URL, has .. in it, names
    // a directory, or names no file. The source is read the same way in every
    // port: its parts between the slashes, less the empty ones and those that
    // are ".", are the names of the directories under the image directory and
    // of the file, and a source that ends in a slash or in "." names a
    // directory. An empty image directory is the working directory. The
    // source is looked at in its code points, not its characters, of which a
    // slash with a combining mark after it is one.
    func imagePath(_ source: String) -> String? {
        guard let imageDirectory else {
            return nil
        }
        let scalars = source.unicodeScalars
        if scalars.first == "/" || scalars.contains(":") || scalars.contains("\\") {
            return nil
        }
        // The parts are compared as bytes: a String compares its characters.
        let dot = UInt8(ascii: ".")
        let parts = source.utf8.split(separator: UInt8(ascii: "/"), omittingEmptySubsequences: false)
                .map { Array($0) }
        if let last = parts.last, last.isEmpty || last == [dot] {
            return nil
        }
        var names = [String]()
        for part in parts {
            if part == [dot, dot] {
                return nil
            }
            if !part.isEmpty && part != [dot] {
                names.append(String(decoding: part, as: UTF8.self))
            }
        }
        var path = names.joined(separator: "/")
        if !imageDirectory.isEmpty {
            path = (imageDirectory as NSString).appendingPathComponent(path)
        }
        var isDirectory: ObjCBool = false
        if !FileManager.default.fileExists(atPath: path, isDirectory: &isDirectory) || isDirectory.boolValue {
            return nil
        }
        return path
    }

    // True when the code units are all U+0020 or below, so that String.trim in
    // Java would leave nothing of them.
    private static func isBlank(_ units: [UInt16]) -> Bool {
        for unit in units where unit > 0x20 {
            return false
        }
        return true
    }
}   // End of Markdown.swift
