/**
 * BigTable.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A table for large amounts of data, read row by row from a delimited text file.
public class BigTable {
    private let pdf: PDF
    private let f1: Font
    private let f2: Font
    private let pageSize: PageSize
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var yText: Float = 0.0
    private var pages: [Page] = []
    private var page: Page?
    private var widths: [Float] = []
    private var headerFields: [String] = []
    private var alignment: [Alignment] = []
    private var vertLines: [Float] = []
    private var bottomMargin: Float = 20.0
    private var padding: Float = 2.0
    private var highlightRow: Bool = true
    private var highlightColor: Int32 = 0xF0F0F0
    private var penColor: Int32 = 0xB0B0B0
    private var fileName: String = ""
    private var delimiter: String = ""
    private var numberOfColumns: Int = 0
    private var startNewPage: Bool = true
    private var dataRows: Int = 0       // The rows under the header, counted by setTableData
    private var pageCount: Int = 0      // The pages they take, counted by complete
    private var pageNumber: Int = 0     // The page being drawn
    private var footerDrawn: Bool = false

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

    /// Sets the number of columns in this table.
    @discardableResult
    public func setNumberOfColumns(_ numberOfColumns: Int) -> BigTable {
        self.numberOfColumns = numberOfColumns
        return self
    }

    /// Sets the text alignment of the specified column.
    @discardableResult
    public func setTextAlignment(_ column: Int, _ alignment: Alignment) -> BigTable {
        self.alignment[column] = alignment
        return self
    }

    /// Sets the bottom margin.
    @discardableResult
    public func setBottomMargin(_ bottomMargin: Float) -> BigTable {
        self.bottomMargin = bottomMargin
        return self
    }

    /// Returns the pages, which complete has already added to the PDF.
    public func getPages() -> [Page] {
        return pages
    }

    // Creates the next page. It is added to the PDF right away, so the content
    // of the page before it is compressed and written, and its memory freed.
    private func newPage() {
        page = Page(pdf, pageSize)
        pages.append(page!)
        pageNumber += 1
        footerDrawn = false
        page!.setPenWidth(0.0)
        self.yText = self.y + f1.ascent
        self.highlightRow = true
        drawFieldsAndLine(fields: headerFields, font: f1)
        self.yText += f1.descent + f2.ascent
        startNewPage = false
    }

    // Draws the footer of the page that was just finished, once. The page is
    // finished before the next one is created, so it is written complete.
    private func drawFooter() {
        if !footerDrawn {
            page!.addFooter(TextLine(f1, "Page \(pageNumber) of \(pageCount)"))
            footerDrawn = true
        }
    }

    // Counts the pages the rows take, as drawTextAndLine breaks them, so that
    // the "Page i of N" footer of a page can be drawn before the next page is
    // created. The location and the bottom margin are set after setTableData,
    // so the pages are counted when the table is drawn.
    private func countPages() -> Int {
        let pageHeight = pageSize.getHeight()
        let yTop = self.y + f1.ascent + f1.descent + f2.ascent
        var yPos = yTop
        var count = 1
        var isNewPage = false
        for _ in 0..<self.dataRows {
            if isNewPage {
                count += 1
                yPos = yTop
                isNewPage = false
            }
            yPos += f2.descent + f2.ascent
            if yPos > (pageHeight - self.bottomMargin) {
                isNewPage = true
            }
        }
        return count
    }

    private func drawTextAndLine(fields: [String], font: Font) throws {
        if page == nil {
            newPage()
            return
        }
        if startNewPage {
            newPage()
        }

        drawFieldsAndLine(fields: fields, font: f2)
        self.yText += f2.descent + f2.ascent
        if self.yText > (page!.height - self.bottomMargin) {
            drawTheVerticalLines()
            drawFooter()
            startNewPage = true
        }
    }

    private func drawFieldsAndLine(fields: [String], font: Font) {
        if self.highlightRow {
            highlightRow(page: page!, font: font, color: highlightColor)
            self.highlightRow = false
        } else {
            self.highlightRow = true
        }

        let original = page!.getPenColor()
        page!.setPenColor(penColor)
        page!.moveTo(vertLines[0], self.yText - font.ascent)
        page!.lineTo(vertLines[numberOfColumns], self.yText - font.ascent)
        page!.strokePath()
        page!.setPenColor(original)
        page!.setBrushColor(Color.black)

        for i in 0..<numberOfColumns {
            let text = fields[i]
            var xText = vertLines[i] + self.padding
            if alignment[i] == Alignment.RIGHT {
                xText = (vertLines[i + 1] - self.padding) - font.stringWidth(text)
            }
            page!.drawTextLine(font, text, xText, self.yText)
        }
    }

    private func highlightRow(page: Page, font: Font, color: Int32) {
        let original = page.getBrushColor()
        page.setBrushColor(color)
        page.moveTo(vertLines[0], self.yText - font.ascent)
        page.lineTo(vertLines[numberOfColumns], self.yText - font.ascent)
        page.lineTo(vertLines[numberOfColumns], self.yText + font.descent)
        page.lineTo(vertLines[0], self.yText + font.descent)
        page.fillPath()
        page.setBrushColor(original)
    }

    private func drawTheVerticalLines() {
        let original = page!.getPenColor()
        page!.setPenColor(penColor)
        for i in 0...numberOfColumns {
            page!.drawLine(
                vertLines[i],
                self.y,
                vertLines[i],
                self.yText - f2.ascent)
        }
        page!.moveTo(vertLines[0], self.yText - f2.ascent)
        page!.lineTo(vertLines[numberOfColumns], self.yText - f2.ascent)
        page!.strokePath()
        page!.setPenColor(original)
    }

    // A number is right-aligned, as Table.rightAlignNumbers aligns it.
    private func getAlignment(_ str: String) -> Alignment {
        return Table.isNumber(str) ? Alignment.RIGHT : Alignment.LEFT
    }

    ///
    /// Reads the data file to set the column widths, the column alignment and the header fields.
    ///
    /// - Parameter fileName: the data file.
    /// - Parameter delimiter: the field delimiter.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setTableData(_ fileName: String, _ delimiter: String) throws -> BigTable {
        self.fileName = fileName
        self.delimiter = delimiter
        self.vertLines = [Float](repeating: 0.0, count: numberOfColumns + 1)
        self.headerFields = [String](repeating: "", count: numberOfColumns)
        self.widths = [Float](repeating: 0.0, count: numberOfColumns)
        self.alignment = [Alignment](repeating: Alignment.LEFT, count: numberOfColumns)

        var rowNumber = 0
        try enumerateFileLines(fileName) { line in
            let fields = Util.split(line, self.delimiter)
            if fields.count < self.numberOfColumns {
                return
            }

            if rowNumber == 0 {
                for i in 0..<self.numberOfColumns {
                    self.headerFields[i] = fields[i]
                }
            }
            if rowNumber == 1 {
                for i in 0..<self.numberOfColumns {
                    self.alignment[i] = self.getAlignment(fields[i])
                }
            }
            for i in 0..<self.numberOfColumns {
                let field = fields[i]
                let width = self.f1.stringWidth(field) + 2 * self.padding
                if width > self.widths[i] {
                    self.widths[i] = width
                }
            }
            rowNumber += 1
        }
        self.dataRows = (rowNumber > 0) ? rowNumber - 1 : 0      // Without the header

        setVertLines()
        return self
    }

    // Sets the x coordinates of the vertical lines from the location and the column widths.
    private func setVertLines() {
        var vertLineX = self.x
        vertLines[0] = vertLineX
        for i in 0..<widths.count {
            vertLineX += widths[i]
            vertLines[i + 1] = vertLineX
        }
    }

    /// Draws the rows read from the data file, then the vertical lines, with a
    /// "Page i of N" footer on every page. The pages are added to the PDF as
    /// they are drawn, so the document does not hold them all.
    public func complete() throws {
        self.pageCount = countPages()
        try enumerateFileLines(self.fileName) { line in
            let fields = Util.split(line, self.delimiter)
            if fields.count < self.numberOfColumns {
                return
            }
            try self.drawTextAndLine(fields: fields, font: self.f2)
        }
        drawTheVerticalLines()
        drawFooter()
    }
}

// Calls the handler with each line of the file. Bytes that are not valid UTF-8
// are replaced with U+FFFD, as the Java and C# readers do.
private func enumerateFileLines(_ fileName: String, _ handler: (String) throws -> Void) throws {
    let file = try FileHandle(forReadingFrom: URL(fileURLWithPath: fileName))
    defer { file.closeFile() }

    var buffer = Data()
    var atStart = true

    while true {
        let chunk = file.readData(ofLength: 8192)
        if chunk.isEmpty { break }
        buffer.append(chunk)
        if atStart {
            // A byte order mark at the start of the file is not part of the text.
            if buffer.starts(with: [0xEF, 0xBB, 0xBF]) {
                buffer.removeSubrange(0..<3)
            }
            atStart = false
        }

        while let nl = buffer.firstIndex(of: UInt8(ascii: "\n")) {
            var lineData = buffer.prefix(upTo: nl)
            buffer.removeSubrange(0...nl)
            if lineData.last == UInt8(ascii: "\r") {
                lineData = lineData.dropLast()
            }

            // Decoded as the other ports do: String(data:encoding:) would drop
            // a byte order mark at the start of every line, and skip a line
            // that is not valid UTF-8 where the others draw U+FFFD.
            try handler(String(decoding: lineData, as: UTF8.self))
        }
    }

    // Last line without newline
    if !buffer.isEmpty {
        try handler(String(decoding: buffer, as: UTF8.self))
    }
}
