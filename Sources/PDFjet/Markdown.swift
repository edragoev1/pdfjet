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
/// breaks, the tables of GitHub's Markdown, and images alone in their
/// paragraph. In a PDF/UA document the headings are H1 to H6, with no level
/// skipped, the lists are lists, the quotes BlockQuotes, the code Code, the
/// tables tables and the images figures, with their text as the alternate
/// description.
///
/// It is not all of Markdown: HTML is drawn as the text it is, a line break
/// is a space, links in a table cell are its text, and an image is drawn only
/// when it is alone in its paragraph, from the directory that
/// setImageDirectory names; with none, its text is drawn instead, so that a
/// text from anyone reads no file. Please see Example_54.
///
public class Markdown {
    // The heading sizes, as factors of the size of the text.
    private static let HEADING_SIZES: [Float] = [2.0, 1.6, 1.3, 1.15, 1.0, 0.9]
    private static let CODE_BACKGROUND: Int32 = 0xF3F5F8
    private static let RULE_COLOR: Int32 = 0xC8CCD2
    private static let QUOTE_BAR_COLOR: Int32 = 0xB0B6BF
    private static let TABLE_HEADER_BACKGROUND: Int32 = 0xEEF0F3

    private let regular: Font
    private let bold: Font
    private let italic: Font
    private let boldItalic: Font
    private let code: Font
    private let markup: Markup
    private var headingFont: Font
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
    /// Sets the directory that the images are read from: an image of
    /// ![text](source) is the file of that name in the directory, a JPEG, PNG,
    /// BMP or SVG file. A source that is an absolute path, a URL or has .. in
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
        // The pages drawn go to the list, also when an image could not be read.
        defer {
            pages = self.pages
            self.pages = []
        }
        let blocks = MarkdownParser.parse(text)
        let width = pageSize.getWidth() - marginLeft - marginRight
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
            drawQuoteBar(bar.x, bar.top, y)
        }
    }

    private func drawQuoteBar(_ x: Float, _ top: Float, _ to: Float) {
        if to > top {
            Line(x, top, x, to).setStrokeColor(Markdown.QUOTE_BAR_COLOR).setStrokeWidth(2.0).drawOn(page)
        }
    }

    private func drawBlocks(_ blocks: [MarkdownParser.Block], _ x: Float, _ width: Float, _ tight: Bool) throws {
        for block in blocks {
            switch block.kind {
            case .HEADING:
                drawHeading(block, x, width)
            case .PARAGRAPH:
                gap(tight ? size() * 0.25 : size() * 0.75)
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
    private func drawHeading(_ block: MarkdownParser.Block, _ x: Float, _ width: Float) {
        let fontSize = size() * Markdown.HEADING_SIZES[block.level - 1]
        gap(size() * (block.level <= 2 ? 1.4 : 1.1))
        let paragraph = markup.paragraph(block.text!)
        for line in paragraph.lines {
            if line.font === regular {
                line.setFont(headingFont)
            } else if line.font === italic {
                line.setFont(boldItalic)
            }
            line.setFontSize(fontSize)
        }
        headingLevel = min(block.level, headingLevel + 1)
        let levels = [StructElem.H1, StructElem.H2, StructElem.H3,
                StructElem.H4, StructElem.H5, StructElem.H6]
        paragraph.setStructureType(levels[headingLevel - 1])
        ensure(headingFont.getBodyHeight(fontSize) + 2.0 * regular.getBodyHeight(size()))
        drawParagraph(paragraph, x, width)
        y += size() * 0.25
    }

    // Code in the code font on a light background, a line of the source at a
    // time, and a line too long for the width goes on under itself.
    private func drawCode(_ text: String, _ x: Float, _ width: Float) {
        gap(size() * 0.75)
        let padding = size() * 0.5
        let leading = code.getBodyHeight(code.getSize()) * 1.2
        let columns = max(1, Markdown.toInt((width - 2.0 * padding) / code.stringWidth(code.getSize(), "0")))
        // The lines of the text, split at each \n as in Java's
        // text.split("\n", -1), in UTF-16 code units.
        var lines = [[UInt16]]()
        for source in Array(text.utf16).split(separator: 0x0A, omittingEmptySubsequences: false) {
            let line = Array(source)
            var start = 0
            while line.count - start > columns {
                // A surrogate pair is not cut in two: it goes on the next
                // line, or on this one when it would be the whole line.
                var cut = start + columns
                if UTF16.isLeadSurrogate(line[cut - 1]) {
                    cut = (cut - 1 > start) ? cut - 1 : cut + 1
                }
                lines.append(Array(line[start..<cut]))
                start = cut
            }
            lines.append(Array(line[start...]))
        }
        ensure(Float(min(3, lines.count)) * leading + 2.0 * padding)
        openContainer(StructElem.CODE)
        var i = 0
        while i < lines.count {
            let fit = max(1, Markdown.toInt((bottom - y - 2.0 * padding) / leading))
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
    }

    // A quote, indented, with a bar on its left.
    private func drawQuote(_ block: MarkdownParser.Block, _ x: Float, _ width: Float) throws {
        gap(size() * 0.75)
        ensure(regular.getBodyHeight(size()))
        let indent = size() * 1.2
        let bar = QuoteBar(x + 2.0, y)
        quoteBars.append(bar)
        openContainer(StructElem.BLOCKQUOTE)
        let top = atTop
        atTop = true        // The first block of the quote starts where the bar does.
        try drawBlocks(block.children, x + indent, width - indent, false)
        atTop = top && atTop
        closeContainer()
        quoteBars.removeAll { $0 === bar }
        drawQuoteBar(bar.x, bar.top, y)
    }

    // A list: the label of each item, a bullet or a number, to the left of
    // the blocks of the item.
    private func drawList(_ list: MarkdownParser.Block, _ x: Float, _ width: Float) throws {
        gap(size() * 0.75)
        let indent = size() * (list.ordered ? 2.0 : 1.4)
        openContainer(StructElem.L)
        var number = list.start
        for item in list.children {
            if item !== list.children[0] {
                gap(list.loose ? size() * 0.5 : size() * 0.2)
            }
            ensure(regular.getBodyHeight(size()))
            openContainer(StructElem.LI)
            let label = list.ordered ? "\(number)." : "•"
            let text = TextLine(regular, label).setStructureType(StructElem.LBL)
            let labelX = list.ordered ? x + indent - size() * 0.4 - text.getWidth() : x + size() * 0.3
            text.setLocation(labelX, y + regular.getAscent(size())).drawOn(page)
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
    // wider, and on the next pages for the rows that do not fit.
    private func drawTable(_ block: MarkdownParser.Block, _ x: Float, _ width: Float) {
        gap(size() * 0.75)
        let blockRows = block.rows!
        let alignments = block.alignments!
        var rows = [[Cell]]()
        for r in 0..<blockRows.count {
            var row = [Cell]()
            for c in 0..<blockRows[r].count {
                let cell = Cell((r == 0) ? bold : regular, plainText(blockRows[r][c]))
                cell.setTextAlignment(alignments[c])
                row.append(cell)
            }
            rows.append(row)
        }
        let table = Table()
        table.setTableData(rows, 1)
        table.setHeaderRowStyle(bold, 0x000000, Markdown.TABLE_HEADER_BACKGROUND)
        table.setCellBorderColor(0xC8CCD2)
        table.autoAdjustColumnWidths()
        if table.getWidth() > width {
            table.fitToWidth(width)
        }
        let rowHeight = 2.0 * regular.getBodyHeight(size())
        ensure(2.0 * rowHeight)
        table.setLocation(x, marginTop)
        table.setFirstPageTopMargin(y)
        table.setBottomMargin(marginBottom)
        let before = pages.count
        let xy = table.drawOn(pdf, page, &pages, pageSize)
        if pages.count > before {
            // The table drew its last rows on a page of its own.
            finishQuoteBars()
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
        let text = block.text!
        let source = block.source!
        guard let path = imagePath(source) else {
            let paragraph = Paragraph(TextLine(italic, text.isEmpty ? source : text))
            drawParagraph(paragraph, x, width)
            return
        }
        let alt = text.isEmpty ? (source as NSString).lastPathComponent : text
        let scale: Float
        let imageWidth: Float
        let imageHeight: Float
        let drawable: any Drawable
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
        ensure(imageHeight * scale)
        drawable.setLocation(x, y)
        drawable.drawOn(page)
        y += imageHeight * scale
        atTop = false
    }

    // The path of an image in the image directory, or nil when there is no
    // directory, or the source is an absolute path, a URL, has .. in it, or
    // names no file.
    private func imagePath(_ source: String) -> String? {
        guard let imageDirectory else {
            return nil
        }
        if source.hasPrefix("/") || source.hasPrefix("\\")
                || source.contains(":") || source.contains("\\") {
            return nil
        }
        for part in source.split(separator: "/", omittingEmptySubsequences: false) where part == ".." {
            return nil
        }
        let path = (imageDirectory as NSString).appendingPathComponent(source)
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

    // A float as an int, as (int) in Java: toward zero, NaN as 0, and out of
    // range as the nearest int, where Int(_:) would trap.
    private static func toInt(_ value: Float) -> Int {
        if value.isNaN {
            return 0
        }
        if value >= Float(Int32.max) {
            return Int(Int32.max)
        }
        if value <= Float(Int32.min) {
            return Int(Int32.min)
        }
        return Int(value)
    }
}   // End of Markdown.swift
