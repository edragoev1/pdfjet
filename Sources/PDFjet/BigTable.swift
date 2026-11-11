/**
 * BigTable.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A table of plain rows of any length, a statement, a list or an export: every row
/// has one style, and a field too wide for its column is cut short, or, with
/// setWrapText, wraps onto more lines, the row as tall as its tallest field. For
/// drawables in cells, spanned cells or page totals, use Table. The rows are read row by row from a delimited text file or from a sequence. Each page is written as soon as it is full, so the memory stays
/// flat however many rows there are. A PDF/UA document holds no more than that
/// either: the table is tagged as a table, and the structure elements of a page are
/// written with it. What is left is one cross-reference entry for each of them,
/// which every object of a PDF has.
public class BigTable {
    private let pdf: PDF
    private let f1: Font
    private let f2: Font
    private let pageSize: PageSize
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var yText: Float = 0.0
    private var top: Float = 0.0            // The top of the table on the page being drawn
    private var firstPage: Page?            // The page the table starts on, or nil for a new one
    private var firstPageY: Float = 0.0     // The top of the table on firstPage
    private var pages: [Page] = []
    private var page: Page?
    private(set) var widths: [Float] = []
    private var headerFields: [String] = []
    private var alignment: [Alignment] = []
    private(set) var vertLines: [Float] = []
    // The width the text of each column has when the table is too wide for
    // the page and the last of its columns are cut back to fit it, or nil
    // when the table fits: a column that is as wide as its widest field holds
    // every field of it whole and needs no measuring as it is drawn.
    private var clipped: [Float]?
    private var bottomMargin: Float = 20.0
    private var padding: Float = 2.0
    private var verticalPadding: Float = 0.0    // Above and below the text of a row
    private var highlightRow: Bool = true
    // In a PDF/UA document the table is a Table element that the rows of
    // every page go on adding to; see drawFieldsAndLine.
    private var structElement: StructElement?
    private var shadingColor: [Float]? = BigTable.rgb(0xF0F0F0)   // nil for no shading
    private var headerColor: [Float]?                           // nil for the shading of every other row
    private var borderColor: [Float]? = BigTable.rgb(0xB0B0B0)    // nil for no lines
    private var verticalLines = true                              // false for the rows' lines alone
    private var footerText: String? = "Page {page} of {pages}"
    private var footerFont: Font?                               // nil for the header font
    private var footerFontSize: Float = 0.0                     // 0 for the size of the footer's font
    private var footerOffset: Float = -1.0                      // Below 0 for the ascent of the footer's font
    private var rows: (() -> AnyIterator<[String]>)?
    private var checkLineBreaks = false     // The rows are not read from a file, which has no line breaks left
    private var readError: Error?       // The error opening the data file, if any
    private var columns: [Int] = []         // The fields drawn, in the order they are drawn
    private var numberOfColumns: Int = 0    // The length of columns
    private var percents: [Float]?          // The width of each column, as a percent of the table's, or nil
    private var fieldsNeeded: Int = 0       // The fields a row needs: the largest index plus 1
    private var startNewPage: Bool = true
    private var dataRows: Int = 0       // The rows under the header, counted by setTableData
    private var pageCount: Int = 0      // The pages they take, counted by complete
    private var pageNumber: Int = 0     // The page being drawn
    private var footerDrawn: Bool = false
    // With setWrapText: the fields wrap within their columns, textWidths the
    // width of the text of each column, headerLines the header's lines, and
    // firstRowOnPage whether no row is drawn yet on the page.
    private var wrap = false
    private(set) var textWidths: [Float] = []
    private(set) var headerLines: [[String]] = []
    private(set) var headerCount: Int = 0        // The most lines of a header field
    private var firstRowOnPage = false
    // With setFallbackFonts: the fonts of the characters the header's font
    // and the rows' font do not have, or nil
    private var fallback1: Font?
    private var fallback2: Font?

    ///
    /// Creates a table with the specified fonts and page size.
    ///
    /// - Parameter pdf: the PDF.
    /// - Parameter f1: the header font.
    /// - Parameter f2: the body font.
    /// - Parameter pageSize: the page size, for example Letter.PORTRAIT.
    ///
    public init(_ pdf: PDF, _ f1: Font, _ f2: Font, _ pageSize: PageSize) {
        self.pdf = pdf
        self.f1 = f1
        self.f2 = f2
        self.pageSize = pageSize
        self.pages = []
    }

    /// Sets the location of the top left corner of this table.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> BigTable {
        self.x = x
        self.y = y
        if !vertLines.isEmpty {
            setVertLines()
        }
        return self
    }

    /// Sets the number of columns in this table: the first fields of every row, in their
    /// order. It is the same as `setColumns([0, 1, ... numberOfColumns - 1])`.
    @discardableResult
    public func setNumberOfColumns(_ numberOfColumns: Int) -> BigTable {
        return setColumns(Array(0..<max(numberOfColumns, 0)))
    }

    ///
    /// Sets the fields of every row that the table draws, by their index from 0, in the order
    /// they are drawn, so `setColumns([3, 0, 11])` draws the fourth field, then the first, then
    /// the twelfth. The indexes pick the header fields the same way. A row without a field for
    /// every index is skipped. Call it before setTableData; setTextAlignment counts the columns
    /// as they are drawn. A negative index is recorded on the PDF as misuse, and the columns are
    /// left as they were.
    ///
    /// - Parameter columns: the indexes of the fields to draw.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setColumns(_ columns: [Int]) -> BigTable {
        var fieldsNeeded = 0
        for column in columns {
            if column < 0 {
                pdf.fail("A column index cannot be negative.")
                return self
            }
            fieldsNeeded = max(fieldsNeeded, column + 1)
        }
        self.columns = columns
        self.numberOfColumns = columns.count
        self.fieldsNeeded = fieldsNeeded
        return self
    }

    /// Gives the columns, in the order setColumns draws them, their percentages
    /// of the width of the table, the width of the page less the margin of
    /// setLocation on each side, as Table's setColumnWidthsInPercent does;
    /// percentages that do not add up to 100 are shares of it. A field wider
    /// than its column is cut to fit it, its text ending in " ...". Without
    /// them each column is as wide as its widest field. Call it after
    /// setColumns or setNumberOfColumns: percentages that are not one for each
    /// column, or one that is negative or not a number, are recorded on the
    /// PDF as misuse, and the widths are left as they were.
    @discardableResult
    public func setColumnWidthsInPercent(_ percents: Float...) -> BigTable {
        if percents.count != numberOfColumns {
            pdf.fail("The table has \(numberOfColumns) columns, and \(percents.count) percentages: "
                    + "give one for each column.")
            return self
        }
        if percents.contains(where: { !($0 >= 0) || $0 == Float.infinity }) {
            pdf.fail("The width of a column cannot be a negative percentage, or not a number.")
            return self
        }
        self.percents = percents
        if !vertLines.isEmpty {
            setVertLines()
        }
        return self
    }

    /// Sets the text alignment of the specified column, which is one of the
    /// columns of the table. Call it after setTableData, which makes the
    /// columns: a column that the table does not have is recorded on the PDF
    /// as misuse, and the alignment is left as it was.
    @discardableResult
    public func setTextAlignment(_ column: Int, _ alignment: Alignment) -> BigTable {
        if column < 0 || column >= self.alignment.count {
            pdf.fail("The table has no column \(column): "
                    + "set the alignment of a column after setTableData.")
            return self
        }
        self.alignment[column] = alignment
        return self
    }

    /// Sets whether a field too wide for its column wraps onto more lines, at its spaces, a
    /// word longer than its column broken between its characters, rather than being cut
    /// short; a row is then as tall as its tallest field. The columns keep the widths of
    /// setColumnWidthsInPercent, or, without them, their widths, as wide as their widest
    /// fields, when the table fits its page; when it does not, the narrow columns keep theirs
    /// and the others share what is left equally. A row that does not fit what is left of a
    /// page starts the next; one taller than a whole page is cut at the lines that fit,
    /// ending in " ...", its cell keeping the whole of its text. The rows are read once more
    /// to count the pages, three times in all, and the memory stays flat. It is off unless
    /// set, and the table is drawn as before.
    @discardableResult
    public func setWrapText(_ wrap: Bool) -> BigTable {
        self.wrap = wrap
        if !vertLines.isEmpty {
            setVertLines()
        }
        return self
    }

    ///
    /// Sets the fonts that draw the characters the header's font and the rows' font do
    /// not have, such as Korean or Chinese in a table of a Latin font: each character in
    /// the font of its row if it has it, else in the fallback font, and measured so. A
    /// nil font is none, as unless set. Call it before setTableData, which measures the
    /// columns.
    ///
    /// - Parameter header: the fallback font of the header's font, or nil.
    /// - Parameter rows: the fallback font of the rows' font, or nil.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setFallbackFonts(_ header: Font?, _ rows: Font?) -> BigTable {
        self.fallback1 = header
        self.fallback2 = rows
        return self
    }

    /// Sets the color of every other row, starting with the header, as a 0xRRGGBB value.
    /// Color.transparent turns the shading off.
    @discardableResult
    public func setShadingColor(_ color: Int32) -> BigTable {
        self.shadingColor = (color == Color.transparent) ? nil : BigTable.rgb(color)
        return self
    }

    /// Sets the color of every other row, starting with the header, from its red, green and
    /// blue values from 0 to 1. nil turns the shading off.
    @discardableResult
    public func setShadingColor(_ color: [Float]?) -> BigTable {
        self.shadingColor = color
        return self
    }

    /// Sets whether the vertical lines between the columns and at the sides of the table are
    /// drawn. They are unless set; without them the table has only the lines between its rows,
    /// a lighter look for a long table.
    @discardableResult
    public func setVerticalLines(_ verticalLines: Bool) -> BigTable {
        self.verticalLines = verticalLines
        return self
    }

    /// Sets the color of the lines between the rows and the columns and around the table, as a
    /// 0xRRGGBB value. Color.transparent leaves the lines out.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> BigTable {
        self.borderColor = (color == Color.transparent) ? nil : BigTable.rgb(color)
        return self
    }

    /// Sets the color of the lines between the rows and the columns and around the table from
    /// its red, green and blue values from 0 to 1. nil leaves the lines out.
    @discardableResult
    public func setBorderColor(_ color: [Float]?) -> BigTable {
        self.borderColor = color
        return self
    }

    /// Sets the space between the text of a column and the lines on its left and right. It is
    /// 2 points by default. A negative padding is recorded on the PDF as misuse, and the
    /// padding is left as it was.
    @discardableResult
    public func setPadding(_ padding: Float) -> BigTable {
        if padding < 0.0 {
            pdf.fail("The padding cannot be negative.")
            return self
        }
        self.padding = padding
        if !vertLines.isEmpty {
            setVertLines()
        }
        return self
    }

    /// Sets the space between the text of a row and the lines above and below it, the header
    /// row's too. It is 0 by default, and a row is as tall as its font; a table with it takes
    /// more pages. A negative padding, or one that is not a number, is recorded on the PDF as
    /// misuse, and the padding is left as it was.
    @discardableResult
    public func setVerticalPadding(_ verticalPadding: Float) -> BigTable {
        if !(verticalPadding >= 0.0) || verticalPadding.isInfinite {
            pdf.fail("The vertical padding must be a number, 0 or more.")
            return self
        }
        self.verticalPadding = verticalPadding
        return self
    }

    /// Sets the background color of the header row, on every page, as a 0xRRGGBB value. Unless
    /// it is set, the header is shaded as the first of every other row, in the shading color.
    /// Color.transparent goes back to that.
    @discardableResult
    public func setHeaderColor(_ color: Int32) -> BigTable {
        self.headerColor = (color == Color.transparent) ? nil : BigTable.rgb(color)
        return self
    }

    ///
    /// Sets the footer drawn at the bottom of every page, centered. In the text, {page} stands
    /// for the number of the page and {pages} for the number of pages. The footer is
    /// "Page {page} of {pages}" in the header font by default. A nil or empty text leaves the
    /// footer out.
    ///
    /// - Parameter text: the text, for example "Seite {page} von {pages}".
    /// - Parameter font: the font, or nil for the header font.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setFooter(_ text: String?, _ font: Font?) -> BigTable {
        self.footerText = text
        self.footerFont = font
        return self
    }

    /// Sets the size of the footer's text, so that it can be smaller than the table's with the
    /// same font. It is the size of the footer's font unless it is set. A size of 0 or less, or
    /// one that is not a number, is recorded on the PDF as misuse, and the size is left as it
    /// was.
    @discardableResult
    public func setFooterFontSize(_ size: Float) -> BigTable {
        if !(size > 0.0) || size.isInfinite {
            pdf.fail("The size of the footer must be a number more than 0.")
            return self
        }
        self.footerFontSize = size
        return self
    }

    /// Sets the distance from the bottom of the page to the baseline of the footer, as
    /// Page.addFooter takes it, so that the footer can sit at the bottom margin. It is the
    /// ascent of the footer's font unless it is set, the footer just above the edge of the page.
    /// A negative offset, or one that is not a number, is recorded on the PDF as misuse, and the
    /// offset is left as it was.
    @discardableResult
    public func setFooterOffset(_ offset: Float) -> BigTable {
        if !(offset >= 0.0) || offset.isInfinite {
            pdf.fail("The offset of the footer must be a number, 0 or more.")
            return self
        }
        self.footerOffset = offset
        return self
    }

    /// Sets the bottom margin.
    @discardableResult
    public func setBottomMargin(_ bottomMargin: Float) -> BigTable {
        self.bottomMargin = bottomMargin
        return self
    }

    /// Starts the table on a page of the PDF that has other content above it,
    /// such as a heading, at y on that page. The next pages are new, and the
    /// table starts on them at the y of its location. The page is one that
    /// Page(pdf, pageSize) has added to the PDF, and nothing is drawn on it
    /// after the table. When the header and the first row do not fit on the
    /// page above its bottom margin, the table starts on a new page instead,
    /// and draws nothing on it.
    @discardableResult
    public func setFirstPage(_ page: Page, _ y: Float) -> BigTable {
        // A page of another PDF is refused: the table was drawn on it, and
        // its mistakes recorded on the other PDF (review of 17 October 2026)
        if page.pdf !== pdf {
            pdf.fail("The page belongs to another PDF.")
            return self
        }
        self.firstPage = page
        self.firstPageY = y
        return self
    }

    /// Returns the pages, which complete has already added to the PDF.
    public func getPages() -> [Page] {
        return pages
    }

    // Creates the next page. It is added to the PDF right away, so the content
    // of the page before it is compressed and written, and its memory freed.
    private func newPage() {
        if pageNumber == 0, startsOnFirstPage(), let firstPage = firstPage {
            page = firstPage
            top = firstPageY
        } else {
            page = Page(pdf, pageSize)
            top = self.y
        }
        pages.append(page!)
        pageNumber += 1
        footerDrawn = false
        page!.setPenWidth(0.0)
        self.yText = top + verticalPadding + f1.ascent
        self.highlightRow = true
        // The header fields are the TH cells of the table the first time they
        // are drawn, and an artifact where they repeat on the next pages.
        var header: StructElem?
        if structElement == nil {
            structElement = page!.addStructElement(
                    page!.structParent, StructElem.TABLE, nil, true)
            header = StructElem.TH
        }
        if wrap {
            drawWrappedFields(headerLines, headerCount, nil, f1, header, true)
        } else {
            drawFieldsAndLine(fields: headerFields, font: f1, cellStructure: header, header: true)
        }
        let extra = headerExtra()
        if extra > 0 {
            self.yText += extra
        }
        self.yText += f1.descent + 2 * verticalPadding + f2.ascent
        startNewPage = false
        firstRowOnPage = true
    }

    // How much taller than one line the header is: 0 unless its fields wrap
    // onto more lines.
    private func headerExtra() -> Float {
        if !wrap || headerCount <= 1 {
            return 0
        }
        return Float(headerCount - 1) * (f1.ascent + f1.descent)
    }

    // True when the table starts on the page setFirstPage set: when the header
    // and the first row fit on it above its bottom margin.
    private func startsOnFirstPage() -> Bool {
        guard let firstPage = firstPage else {
            return false
        }
        let height: Float = firstPageY + f1.ascent + f1.descent + f2.ascent + f2.descent
        var need = height + 4 * verticalPadding
        let extra = headerExtra()
        if extra > 0 {
            need += extra
        }
        return need <= firstPage.height - bottomMargin
    }

    // Draws the footer of the page that was just finished, once. The page is
    // finished before the next one is created, so it is written complete.
    private func drawFooter() {
        if !footerDrawn, let footerText = footerText, !footerText.isEmpty {
            let text = footerText
                    .replacingOccurrences(of: "{page}", with: String(pageNumber), options: .literal)
                    .replacingOccurrences(of: "{pages}", with: String(pageCount), options: .literal)
            // The page number repeats on every page, which makes it an artifact:
            // addFooter marks it as a pagination artifact, a footer.
            let footer = TextLine(footerFont ?? f1, text)
            if footerFontSize > 0.0 {
                footer.setFontSize(footerFontSize)
            }
            if footerOffset >= 0.0 {
                page!.addFooter(footer, footerOffset)
            } else {
                page!.addFooter(footer)
            }
        }
        footerDrawn = true
    }

    // Returns the baseline of the first row of a page, of a table whose top is
    // there, in the order newPage adds it up: added up otherwise, it can round
    // to another Float, and the count was a page off the drawing.
    private func firstRowY(_ top: Float) -> Float {
        var y = top + verticalPadding + f1.ascent
        let extra = headerExtra()
        if extra > 0 {
            y += extra
        }
        y += f1.descent + 2 * verticalPadding + f2.ascent
        return y
    }

    // Counts the pages the rows take, as drawTextAndLine breaks them, so that
    // the "Page i of N" footer of a page can be drawn before the next page is
    // created. The location and the bottom margin are set after setTableData,
    // so the pages are counted when the table is drawn.
    private func countPages() -> Int {
        if wrap {
            return countWrappedPages()
        }
        var pageHeight = pageSize.getHeight()
        let yTop = firstRowY(self.y)
        var yPos = yTop
        if startsOnFirstPage() {
            // The first page can be of another size than the next ones.
            pageHeight = firstPage!.height
            yPos = firstRowY(firstPageY)
        }
        var count = 1
        var isNewPage = false
        for _ in 0..<self.dataRows {
            if isNewPage {
                count += 1
                pageHeight = pageSize.getHeight()
                yPos = yTop
                isNewPage = false
            }
            yPos += f2.descent + 2 * verticalPadding + f2.ascent
            if yPos > (pageHeight - self.bottomMargin) {
                isNewPage = true
            }
        }
        return count
    }

    private func drawTextAndLine(fields: [String]) {
        if wrap {
            drawWrappedRow(fields)
            return
        }
        if startNewPage {
            newPage()
        }

        drawFieldsAndLine(fields: fields, font: f2, cellStructure: StructElem.TD, header: false)
        self.yText += f2.descent + 2 * verticalPadding + f2.ascent
        if self.yText > (page!.height - self.bottomMargin) {
            drawTheVerticalLines()
            drawFooter()
            startNewPage = true
        }
    }

    // Draws a row of the table. In a PDF/UA document the row is a TR element
    // and each field a TH or TD element that holds the text, and the shading
    // and the lines are artifacts. A row with no cell structure is drawn as an
    // artifact, which is what the header rows that repeat on the next pages
    // are.
    private func drawFieldsAndLine(
            fields: [String], font: Font, cellStructure: StructElem?, header: Bool) {
        // The shading and the line above the text carry no meaning of their own.
        page!.addArtifactBMC()
        if header, let headerColor = headerColor {
            highlightRow(page: page!, font: font, color: headerColor)
            self.highlightRow = false
        } else if self.highlightRow {
            if let shadingColor = shadingColor {
                highlightRow(page: page!, font: font, color: shadingColor)
            }
            self.highlightRow = false
        } else {
            self.highlightRow = true
        }

        // The line above the text
        if let borderColor = borderColor {
            let original = page!.getPenColor()
            page!.setPenColor(borderColor)
            page!.moveTo(vertLines[0], self.yText - font.ascent - verticalPadding)
            page!.lineTo(vertLines[numberOfColumns], self.yText - font.ascent - verticalPadding)
            page!.strokePath()
            page!.setPenColor(original)
        }
        page!.addEMC()
        page!.setBrushColor(Color.black)

        let tagged = structElement != nil && cellStructure != nil
        let parent = page!.structParent
        var rowElement: StructElement?
        if tagged {
            rowElement = page!.addStructElement(structElement, StructElem.TR, nil)
        }
        for i in 0..<numberOfColumns {
            let text = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]]
            // A column the page was too narrow for holds as much of the text
            // as fits it, ending in " ..."; the cell of the structure tree
            // keeps the whole of the text, which is what a reader reads. A
            // column that was not cut is as wide as its widest field, so every
            // field of it is drawn whole and none is measured here.
            var drawn = text
            if let clipped = self.clipped, clipped[i] < widths[i] {
                drawn = BigTable.fit(text, measureOf(font), clipped[i])
            }
            var xText = vertLines[i] + self.padding
            if alignment[i] == Alignment.RIGHT {
                xText = (vertLines[i + 1] - self.padding) - measureOf(font).width(drawn)
            }
            if tagged {
                // A cell holds its text and nothing else, so the cell element
                // holds the marked content of the text: it needs no paragraph
                // of its own, which would be another object for every cell.
                page!.structParent = rowElement
                // A text cut short to fit its column is described by the
                // whole of it; a text drawn whole needs no description, which
                // PAC warns of on text
                let description: String? = (drawn != text) ? text : nil
                page!.addBDC(cellStructure!, nil, nil, description,
                        BigTable.cellAttributes(cellStructure!))
            } else {
                page!.addArtifactBMC()
            }
            drawText(font, drawn, xText, self.yText)
            page!.addEMC()
        }
        page!.structParent = parent
    }

    // The attributes of a cell element: a header cell heads the column it is in.
    private static func cellAttributes(_ cellStructure: StructElem) -> String? {
        return (cellStructure == StructElem.TH) ? "<</O /Table /Scope /Column>>" : nil
    }

    private func highlightRow(page: Page, font: Font, color: [Float]) {
        let original = page.getBrushColor()
        page.setBrushColor(color)
        page.fillRectBetween(vertLines[0], self.yText - font.ascent - verticalPadding,
                vertLines[numberOfColumns], self.yText + font.descent + verticalPadding)
        page.setBrushColor(original)
    }

    private func drawTheVerticalLines() {
        guard let borderColor = borderColor else {
            return
        }
        // The lines of the table carry no meaning of their own.
        page!.addArtifactBMC()
        let original = page!.getPenColor()
        page!.setPenColor(borderColor)
        for i in 0...numberOfColumns where verticalLines {
            page!.drawLine(
                vertLines[i],
                top,
                vertLines[i],
                self.yText - f2.ascent - verticalPadding)
        }
        page!.moveTo(vertLines[0], self.yText - f2.ascent - verticalPadding)
        page!.lineTo(vertLines[numberOfColumns], self.yText - f2.ascent - verticalPadding)
        page!.strokePath()
        page!.setPenColor(original)
        page!.addEMC()
    }

    private static func rgb(_ color: Int32) -> [Float] {
        return [Float((color >> 16) & 0xff)/255.0, Float((color >> 8) & 0xff)/255.0, Float(color & 0xff)/255.0]
    }

    // A number is right-aligned, as Table.rightAlignNumbers aligns it.
    private func getAlignment(_ str: String) -> Alignment {
        return Table.isNumber(str) ? Alignment.RIGHT : Alignment.LEFT
    }

    ///
    /// Reads the data file to set the column widths, the column alignment and the header fields.
    /// The file is read as UTF-8. Its first line with a field for every column is the header,
    /// and the lines after it are the rows. A quoted field is read as RFC 4180 reads it, so a
    /// delimiter inside one is text, and a line break inside one goes on to the next line of
    /// the file and is drawn as a space.
    ///
    /// - Parameter fileName: the data file.
    /// - Parameter delimiter: the field delimiter.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setTableData(_ fileName: String, _ delimiter: String) throws -> BigTable {
        let fieldsNeeded = self.fieldsNeeded
        var header = [String]()
        var headerError: Error?
        let lines = try DataFileRows(fileName, delimiter)
        lines.failed = { headerError = $0 }
        while let fields = lines.next() {
            if fields.count >= fieldsNeeded {
                header = fields
                break
            }
        }
        if let error = headerError {
            throw error
        }

        readError = nil
        setTableData(header, checkLineBreaks: false) { [weak self] in
            do {
                let rows = try DataFileRows(fileName, delimiter)
                rows.failed = { self?.readError = $0 }
                // The rows start after the header.
                while let fields = rows.next(), fields.count < fieldsNeeded {
                }
                return AnyIterator(rows)
            } catch {
                self?.readError = error
                return AnyIterator { nil }
            }
        }
        if let error = readError {
            throw error
        }
        return self
    }

    ///
    /// Sets the column widths, the column alignment and the header fields from rows that are
    /// not in a file: the results of a query, or an array of objects. The rows are iterated
    /// twice, once here to measure the columns and once by complete to draw them, and neither
    /// keeps them, so the sequence must be one that can be iterated more than once, such as an
    /// array, a lazy map of one, or a sequence whose `makeIterator` runs the query again. A row
    /// without a field for every column is skipped, as a short line of a file is, and a line
    /// break in a field is drawn as a space. A header
    /// without a field for every column is recorded on the PDF as misuse, and the table is
    /// left without data.
    ///
    /// - Parameter header: the header fields, with a field for every column.
    /// - Parameter rows: the fields of each row, in the order they are drawn.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setTableData<Rows: Sequence>(
            _ header: [String], _ rows: Rows) -> BigTable where Rows.Element == [String] {
        return setTableData(header, checkLineBreaks: true) { AnyIterator(rows.makeIterator()) }
    }

    @discardableResult
    // With checkLineBreaks, each field is looked at for line breaks to draw as
    // spaces; the rows of a data file have them as spaces already.
    private func setTableData(
            _ header: [String],
            checkLineBreaks: Bool,
            _ rows: @escaping () -> AnyIterator<[String]>) -> BigTable {
        self.checkLineBreaks = checkLineBreaks
        if header.count < fieldsNeeded {
            pdf.fail("The header does not have a field for every column.")
            return self
        }
        self.rows = rows
        self.vertLines = [Float](repeating: 0.0, count: numberOfColumns + 1)
        self.headerFields = header
        self.widths = [Float](repeating: 0.0, count: numberOfColumns)
        self.alignment = [Alignment](repeating: Alignment.LEFT, count: numberOfColumns)

        measure(header, f1)
        var rowNumber = 0
        for fields in IteratorSequence(rows()) {
            if fields.count < fieldsNeeded {
                continue
            }
            if rowNumber == 0 {     // Determine alignment from first data row
                for i in 0..<numberOfColumns {
                    alignment[i] = getAlignment(fields[columns[i]])
                }
            }
            measure(fields, f2)
            rowNumber += 1
        }
        self.dataRows = rowNumber

        setVertLines()
        return self
    }

    // Widens the columns to fit the fields of a row. The widths are those of
    // the text, and setVertLines adds the padding, so it can be set later.
    // Widens the columns to fit the fields of a row, measured in the font the
    // row is drawn with: the header font for the header and the body font for
    // a row under it.
    private func measure(_ fields: [String], _ font: Font) {
        for i in 0..<numberOfColumns {
            let text = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]]
            let width = measureOf(font).width(text)
            if width > widths[i] {
                widths[i] = width
            }
        }
    }

    // Sets the x coordinates of the vertical lines from the location, the
    // column widths and the padding.
    // The mark that ends a field whose text was cut to fit its column, which
    // takes the place of the last four characters of what fits.
    private static let ELLIPSIS = " ..."

    // Puts each vertical line where the columns before it end, and cuts the
    // last of the columns back when they run past the right edge of the page:
    // a column as wide as its widest field is what the table asks for, and a
    // table wider than its page draws the last of its columns off the edge,
    // where they are lost. The columns are cut from the last one back, so the
    // table keeps the widths it asked for as far as the page allows, and the
    // fields of a column that was cut are drawn with the end of their text
    // replaced by " ...".
    private func setVertLines() {
        if let count = percents?.count, count != self.widths.count {
            // The columns were set again after the percentages: fewer stopped the program, and
        // more were drawn past the page (review of 12 October 2026, C4)
            percents = nil
            pdf.fail("The table has \(self.widths.count) columns, and \(count) percentages: give one for each column.")
        }
        if wrap {
            setWrapWidths()
            return
        }
        if percents != nil {
            setVertLinesInPercent()
            return
        }
        var widths = self.widths
        // The table is drawn between the margin it starts at and the same
        // margin on the right of the page.
        let room = pageSize.getWidth() - 2 * self.x - Float(widths.count) * 2 * self.padding
        let least = max(f1.stringWidth(BigTable.ELLIPSIS), f2.stringWidth(BigTable.ELLIPSIS))
        var total: Float = 0.0
        for width in widths {
            total += width
        }
        self.clipped = nil
        var i = widths.count - 1
        while i >= 0 && total > room {
            let cut = min(total - room, max(widths[i] - least, 0.0))
            if cut > 0.0 {
                widths[i] -= cut
                total -= cut
                if self.clipped == nil {
                    self.clipped = self.widths
                }
                self.clipped![i] = widths[i]
            }
            i -= 1
        }
        var vertLineX = self.x
        vertLines[0] = vertLineX
        for i in 0..<widths.count {
            vertLineX += widths[i] + 2 * padding
            vertLines[i + 1] = vertLineX
        }
    }

    // Puts each vertical line where the columns before it end, each column the
    // percentage of the table's width setColumnWidthsInPercent gives it, its
    // padding in it, and cuts the fields of a column narrower than its widest
    // field.
    private func setVertLinesInPercent() {
        let percents = self.percents!
        var tableWidth = pageSize.getWidth() - 2 * self.x
        if !(tableWidth > 0) {
            // The columns were as wide as less than nothing (review of 12 October
        // 2026, T5)
            pdf.fail("The margin of the table, its x, is half the width of the page or more: the columns have no room.")
            tableWidth = 0
        }
        // The shares are added up and taken in double, the largest float a
        // share as any other: tableWidth*percent was an infinity, and the
        // document failed (review of 12 October 2026, T5)
        var total = 0.0
        for percent in percents {
            total += Double(percent)
        }
        self.clipped = nil
        var vertLineX = self.x
        vertLines[0] = vertLineX
        for i in 0..<percents.count {
            // Percentages that are all 0 share the width equally.
            let width = (total > 0) ? Float(Double(tableWidth) * Double(percents[i]) / total) : tableWidth / Float(percents.count)
            let text = max(0, width - 2 * padding)
            if self.widths[i] > text {
                if self.clipped == nil {
                    self.clipped = self.widths
                }
                self.clipped![i] = text
            }
            vertLineX += width
            vertLines[i + 1] = vertLineX
        }
    }

    // The text as much of it as fits the width, ending in the mark that says
    // it was cut. The mark takes the place of the last four characters of
    // what fits, rather than being added to them, so the text stays inside
    // the column. The text is cut between code points, as in the other ports,
    // never inside one.
    private static func fit(_ text: String, _ m: TextMeasure, _ width: Float) -> String {
        if m.width(text) <= width {
            return text
        }
        return cut(text, m, width)
    }

    // As much of the text as fits the width with the mark that says it was
    // cut, the mark in place of the last four characters of what fits; or ""
    // when the width is too narrow for the mark.
    private static func cut(_ text: String, _ m: TextMeasure, _ width: Float) -> String {
        let characters = Array(text.unicodeScalars)
        var end = 0             // The most characters that fit.
        var high = characters.count
        while end < high {
            let middle = end + (high - end + 1) / 2
            if m.width(BigTable.string(characters[0..<middle])) <= width {
                end = middle
            } else {
                high = middle - 1
            }
        }
        end = max(0, end - ELLIPSIS.unicodeScalars.count)
        while end > 0 && m.width(BigTable.string(characters[0..<end]) + ELLIPSIS) > width {
            end -= 1
        }
        if m.font.stringWidth(ELLIPSIS) > width {
            // A column too narrow for the mark draws nothing: the mark was drawn
        // over the next column (review of 12 October 2026, T5)
            return ""
        }
        return BigTable.string(characters[0..<end]) + ELLIPSIS
    }

    private static func string(_ scalars: ArraySlice<Unicode.Scalar>) -> String {
        var view = String.UnicodeScalarView()
        view.append(contentsOf: scalars)
        return String(view)
    }

    /// Draws the rows, then the vertical lines, with the footer on every page. The pages are
    /// added to the PDF as they are drawn, so the document does not hold them all. It throws
    /// if the data file cannot be opened. A table without data draws nothing.
    public func complete() throws {
        guard let rows = self.rows else {
            return
        }
        self.pageCount = countPages()
        readError = nil
        newPage()
        for fields in IteratorSequence(rows()) {
            if fields.count < fieldsNeeded {
                continue
            }
            drawTextAndLine(fields: fields)
        }
        if let error = readError {
            throw error
        }
        drawTheVerticalLines()
        drawFooter()
    }

    // The wrapping of the fields, with setWrapText

    // Gives each column the width of its text, and puts each vertical line
    // where the columns before it end: the widths of setColumnWidthsInPercent,
    // or each column as wide as its widest field when they fit the page; when
    // they do not, the columns narrower than an equal share of what is left
    // keep their widths, from the narrowest, and the others share the rest
    // equally. No field is cut: each wraps within its column.
    private func setWrapWidths() {
        let n = self.widths.count
        self.clipped = nil
        self.textWidths = [Float](repeating: 0.0, count: n)
        var vertLineX = self.x
        vertLines[0] = vertLineX
        if let percents = self.percents {
            var tableWidth = pageSize.getWidth() - 2 * self.x
            if !(tableWidth > 0) {
                pdf.fail("The margin of the table, its x, is half the width of the page or more: the columns have no room.")
                tableWidth = 0
            }
            var total = 0.0
            for percent in percents {
                total += Double(percent)
            }
            for i in 0..<percents.count {
                let width = (total > 0) ? Float(Double(tableWidth) * Double(percents[i]) / total) : tableWidth / Float(percents.count)
                textWidths[i] = max(0, width - 2 * padding)
                vertLineX += width
                vertLines[i + 1] = vertLineX
            }
        } else {
            let widths = self.widths
            textWidths = widths
            let room = pageSize.getWidth() - 2 * self.x - Float(n) * 2 * self.padding
            var total: Float = 0.0
            for width in widths {
                total += width
            }
            if total > room {
                // From the narrowest column, in the order of the columns among
                // those as wide: a stable sort, which Swift's sort is not
                // said to be, so the index decides between equal widths.
                let order = (0..<n).sorted {
                    widths[$0] < widths[$1] || (widths[$0] == widths[$1] && $0 < $1)
                }
                var remaining = room
                var left = n
                var share: Float = -1
                for i in order {
                    if share < 0 {
                        let fair = max(0, remaining / Float(left))
                        if widths[i] <= fair {
                            remaining -= widths[i]
                            left -= 1
                            continue
                        }
                        share = fair
                    }
                    textWidths[i] = share
                }
            }
            for i in 0..<n {
                vertLineX += textWidths[i] + 2 * padding
                vertLines[i + 1] = vertLineX
            }
        }
        (headerLines, headerCount) = wrapFields(headerFields, f1)
    }

    // The lines of each field of the row drawn, wrapped to the width of its
    // column in the font, and the most lines of any of them.
    private func wrapFields(_ fields: [String], _ font: Font) -> ([[String]], Int) {
        var lines = [[String]]()
        lines.reserveCapacity(numberOfColumns)
        var count = 1
        for i in 0..<numberOfColumns {
            let text = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]]
            let field = BigTable.wrapText(text, measureOf(font), textWidths[i])
            lines.append(field)
            count = max(count, field.count)
        }
        return (lines, count)
    }

    // The lines of the text within the width: its words, the parts between
    // its spaces, as many on a line as fit, a word wider than the width broken
    // between its characters, at least one on a line. An empty text, or one of
    // spaces, is one empty line. The text is split at the space character and
    // broken between code points, as in the other ports.
    static func wrapText(_ text: String, _ m: TextMeasure, _ width: Float) -> [String] {
        var lines = [String]()
        var line = ""
        for piece in text.unicodeScalars.split(separator: " ", omittingEmptySubsequences: true) {
            let characters = Array(piece)
            let word = BigTable.string(characters[...])
            if !line.isEmpty {
                let candidate = line + " " + word
                if m.width(candidate) <= width {
                    line = candidate
                    continue
                }
                lines.append(line)
                line = ""
            }
            if m.width(word) <= width {
                line = word
                continue
            }
            var rest = characters[...]
            while !rest.isEmpty {
                let end = BigTable.charactersThatFit(rest, m, width)
                if end == rest.count {
                    line = BigTable.string(rest)
                    break
                }
                lines.append(BigTable.string(rest.prefix(end)))
                rest = rest.dropFirst(end)
            }
        }
        lines.append(line)
        return lines
    }

    // How many of the characters, from the first, fit the width: at least
    // one, so that a column too narrow for a character still draws the text,
    // a character on each line.
    private static func charactersThatFit(
            _ characters: ArraySlice<Unicode.Scalar>, _ m: TextMeasure, _ width: Float) -> Int {
        var end = 1
        var high = characters.count
        while end < high {
            let middle = end + (high - end + 1) / 2
            if m.width(BigTable.string(characters.prefix(middle))) <= width {
                end = middle
            } else {
                high = middle - 1
            }
        }
        return end
    }

    // The most lines of a row whose first line is at y that the page holds
    // above the limit: at least one.
    private static func mostLines(_ y: Float, _ limit: Float, _ lineHeight: Float) -> Int {
        var most = 1
        while y + Float(most) * lineHeight <= limit {
            most += 1
        }
        return most
    }

    // Draws a row of fields that wrap: on the next page when it does not fit
    // what is left of this one, cut at the lines a whole page holds when it
    // does not fit that either.
    private func drawWrappedRow(_ fields: [String]) {
        if startNewPage {
            newPage()
        }
        let font = f2
        let lineHeight = font.ascent + font.descent
        var (lines, count) = wrapFields(fields, font)
        var limit = page!.height - bottomMargin
        if count > 1 && self.yText + Float(count - 1) * lineHeight > limit && !firstRowOnPage {
            drawTheVerticalLines()
            drawFooter()
            newPage()
            limit = page!.height - bottomMargin
        }
        var whole: [String]?
        if count > 1 && self.yText + Float(count - 1) * lineHeight > limit {
            let most = BigTable.mostLines(self.yText, limit, lineHeight)
            if most < count {
                var texts = [String](repeating: "", count: numberOfColumns)
                for i in 0..<lines.count where lines[i].count > most {
                    texts[i] = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]]
                    lines[i] = Array(lines[i][0..<most])
                    lines[i][most - 1] = BigTable.mark(lines[i][most - 1], measureOf(font), textWidths[i])
                }
                whole = texts
                count = most
            }
        }
        drawWrappedFields(lines, count, whole, font, StructElem.TD, false)
        firstRowOnPage = false
        if count > 1 {
            self.yText += Float(count - 1) * lineHeight
        }
        self.yText += font.descent + 2 * verticalPadding + font.ascent
        if self.yText > limit {
            drawTheVerticalLines()
            drawFooter()
            startNewPage = true
        }
    }

    // The line with the mark that says the text goes on, cut to make room for
    // it within the width.
    private static func mark(_ line: String, _ m: TextMeasure, _ width: Float) -> String {
        let marked = line + ELLIPSIS
        if m.width(marked) <= width {
            return marked
        }
        return cut(line, m, width)
    }

    // Draws a row of the lines of its fields, count lines tall, as
    // drawFieldsAndLine draws a row of one line: the cell of a field holds all
    // of its lines, and the whole of the text of a field that was cut,
    // whole[i] when there is one.
    private func drawWrappedFields(
            _ lines: [[String]], _ count: Int, _ whole: [String]?, _ font: Font,
            _ cellStructure: StructElem?, _ header: Bool) {
        let lineHeight = font.ascent + font.descent
        var extra: Float = 0.0
        if count > 1 {
            extra = Float(count - 1) * lineHeight
        }
        page!.addArtifactBMC()
        if header, let headerColor = headerColor {
            highlightRows(page: page!, font: font, color: headerColor, extra: extra)
            self.highlightRow = false
        } else if self.highlightRow {
            if let shadingColor = shadingColor {
                highlightRows(page: page!, font: font, color: shadingColor, extra: extra)
            }
            self.highlightRow = false
        } else {
            self.highlightRow = true
        }
        if let borderColor = borderColor {
            let original = page!.getPenColor()
            page!.setPenColor(borderColor)
            page!.moveTo(vertLines[0], self.yText - font.ascent - verticalPadding)
            page!.lineTo(vertLines[numberOfColumns], self.yText - font.ascent - verticalPadding)
            page!.strokePath()
            page!.setPenColor(original)
        }
        page!.addEMC()
        page!.setBrushColor(Color.black)

        let tagged = structElement != nil && cellStructure != nil
        let parent = page!.structParent
        var rowElement: StructElement?
        if tagged {
            rowElement = page!.addStructElement(structElement, StructElem.TR, nil)
        }
        for i in 0..<numberOfColumns {
            if tagged {
                page!.structParent = rowElement
                var description: String?
                if let whole = whole, !whole[i].isEmpty {
                    description = whole[i]
                }
                page!.addBDC(cellStructure!, nil, nil, description,
                        BigTable.cellAttributes(cellStructure!))
            } else {
                page!.addArtifactBMC()
            }
            for (k, line) in lines[i].enumerated() {
                var xText = vertLines[i] + self.padding
                if alignment[i] == Alignment.RIGHT {
                    xText = (vertLines[i + 1] - self.padding) - measureOf(font).width(line)
                }
                var yText = self.yText
                if k > 0 {
                    yText += Float(k) * lineHeight
                }
                drawText(font, line, xText, yText)
            }
            page!.addEMC()
        }
        page!.structParent = parent
    }

    // Fills the background of a row, extra taller than one line.
    private func highlightRows(page: Page, font: Font, color: [Float], extra: Float) {
        let original = page.getBrushColor()
        page.setBrushColor(color)
        var bottom = self.yText + font.descent + verticalPadding
        if extra > 0 {
            bottom = self.yText + extra + font.descent + verticalPadding
        }
        page.fillRectBetween(vertLines[0], self.yText - font.ascent - verticalPadding,
                vertLines[numberOfColumns], bottom)
        page.setBrushColor(original)
    }

    // Counts the pages the rows take as drawWrappedRow breaks them, reading
    // the rows once more, so that the footer of each page says how many there
    // are. The arithmetic is that of the drawing, in its order.
    private func countWrappedPages() -> Int {
        let font = f2
        let lineHeight = font.ascent + font.descent
        var pageHeight = pageSize.getHeight()
        let yTop = firstRowY(self.y)
        var yPos = yTop
        if startsOnFirstPage() {
            pageHeight = firstPage!.height
            yPos = firstRowY(firstPageY)
        }
        var count = 1
        var isNewPage = false
        var first = true
        guard let rows = self.rows else {
            return count
        }
        for fields in IteratorSequence(rows()) {
            if fields.count < fieldsNeeded {
                continue
            }
            if isNewPage {
                count += 1
                pageHeight = pageSize.getHeight()
                yPos = yTop
                isNewPage = false
                first = true
            }
            var (_, lines) = wrapFields(fields, font)
            var limit = pageHeight - self.bottomMargin
            if lines > 1 && yPos + Float(lines - 1) * lineHeight > limit && !first {
                count += 1
                pageHeight = pageSize.getHeight()
                yPos = yTop
                limit = pageHeight - self.bottomMargin
            }
            if lines > 1 && yPos + Float(lines - 1) * lineHeight > limit {
                lines = min(lines, BigTable.mostLines(yPos, limit, lineHeight))
            }
            if lines > 1 {
                yPos += Float(lines - 1) * lineHeight
            }
            yPos += font.descent + 2 * verticalPadding + font.ascent
            first = false
            if yPos > limit {
                isNewPage = true
            }
        }
        return count
    }

    // Measures text in a font, and in its fallback font, if it has one, the
    // characters the font does not have.
    struct TextMeasure {
        let font: Font
        let fallback: Font?

        func width(_ text: String) -> Float {
            guard let fallback = fallback else {
                return font.stringWidth(font.size, text)
            }
            return font.stringWidth(fallback, font.size, text)
        }
    }

    // The measure of the font: the rows' with their fallback font, the
    // header's with its own.
    private func measureOf(_ font: Font) -> TextMeasure {
        if font === f2 {
            return TextMeasure(font: font, fallback: fallback2)
        }
        return TextMeasure(font: font, fallback: fallback1)
    }

    // Draws the text at its baseline, in the font and its fallback font, if
    // it has one.
    private func drawText(_ font: Font, _ text: String, _ x: Float, _ y: Float) {
        guard let fallback = measureOf(font).fallback else {
            page!.drawTextLine(font, text, x, y)
            return
        }
        page!.drawString(font, fallback, font.size, text, x, y)
    }
}

// The fields of the lines of a data file, split at the delimiter, reading the
// quoted fields as RFC 4180 does, from the lines DataFileLines reads. A file
// that cannot be read that way ends the rows, and the error that says why is
// passed to failed.
private final class DataFileRows: IteratorProtocol {
    private let lines: DataFileLines
    private let delimiter: String
    var failed: ((Error) -> Void)?

    init(_ fileName: String, _ delimiter: String) throws {
        self.lines = try DataFileLines(fileName)
        self.delimiter = delimiter
    }

    func next() -> [String]? {
        guard let line = lines.next() else {
            return nil
        }
        do {
            return try Util.readRecord(line, delimiter) { self.lines.next() }
        } catch {
            failed?(error)
            return nil
        }
    }
}
