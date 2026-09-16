/**
 * BigTable.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A table for large amounts of data, read row by row from a delimited text file or
/// from a sequence. Each page is written as soon as it is full, so the memory stays
/// flat however many rows there are.
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
    private var rows: (() -> AnyIterator<[String]>)?
    private var readError: Error?       // The error opening the data file, if any
    private var columns: [Int] = []         // The fields drawn, in the order they are drawn
    private var numberOfColumns: Int = 0    // The length of columns
    private var fieldsNeeded: Int = 0       // The fields a row needs: the largest index plus 1
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

    private func drawTextAndLine(fields: [String]) {
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
            let text = fields[columns[i]]
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
    /// The file is read as UTF-8. Its first line with a field for every column is the header, and the lines after it are the rows. A quoted field is read as
    /// RFC 4180 reads it, so a delimiter inside one is text.
    ///
    /// - Parameter fileName: the data file.
    /// - Parameter delimiter: the field delimiter.
    /// - Returns: this BigTable object.
    ///
    @discardableResult
    public func setTableData(_ fileName: String, _ delimiter: String) throws -> BigTable {
        let fieldsNeeded = self.fieldsNeeded
        var header = [String]()
        let lines = try DataFileRows(fileName, delimiter)
        while let fields = lines.next() {
            if fields.count >= fieldsNeeded {
                header = fields
                break
            }
        }

        readError = nil
        setTableData(header) { [weak self] in
            do {
                let rows = try DataFileRows(fileName, delimiter)
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
    /// without a field for every column is skipped, as a short line of a file is. A header
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
        return setTableData(header) { AnyIterator(rows.makeIterator()) }
    }

    @discardableResult
    private func setTableData(_ header: [String], _ rows: @escaping () -> AnyIterator<[String]>) -> BigTable {
        if header.count < fieldsNeeded {
            pdf.fail("The header does not have a field for every column.")
            return self
        }
        self.rows = rows
        self.vertLines = [Float](repeating: 0.0, count: numberOfColumns + 1)
        self.headerFields = header
        self.widths = [Float](repeating: 0.0, count: numberOfColumns)
        self.alignment = [Alignment](repeating: Alignment.LEFT, count: numberOfColumns)

        measure(header)
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
            measure(fields)
            rowNumber += 1
        }
        self.dataRows = rowNumber

        setVertLines()
        return self
    }

    // Widens the columns to fit the fields of a row.
    private func measure(_ fields: [String]) {
        for i in 0..<numberOfColumns {
            let width = f1.stringWidth(fields[columns[i]]) + 2 * padding
            if width > widths[i] {
                widths[i] = width
            }
        }
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

    /// Draws the rows, then the vertical lines, with a "Page i of N" footer on every
    /// page. The pages are added to the PDF as they are drawn, so the document does not
    /// hold them all. It throws if the data file cannot be opened. A table without data
    /// draws nothing.
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
}

// The fields of the lines of a data file, split at the delimiter, reading the
// quoted fields as RFC 4180 does. The file is read as UTF-8, after the byte order
// mark at its start, if there is one, and bytes that are not valid UTF-8 are
// replaced with U+FFFD, as the Java and C# readers do.
private final class DataFileRows: IteratorProtocol {
    private let file: FileHandle
    private let delimiter: String
    private var buffer = Data()
    private var atStart = true
    private var atEnd = false

    init(_ fileName: String, _ delimiter: String) throws {
        self.file = try FileHandle(forReadingFrom: URL(fileURLWithPath: fileName))
        self.delimiter = delimiter
    }

    deinit {
        file.closeFile()
    }

    func next() -> [String]? {
        guard let line = nextLine() else {
            return nil
        }
        return Util.split(line, delimiter)
    }

    private func nextLine() -> String? {
        while true {
            if let nl = buffer.firstIndex(of: UInt8(ascii: "\n")) {
                var lineData = buffer.prefix(upTo: nl)
                buffer.removeSubrange(0...nl)
                if lineData.last == UInt8(ascii: "\r") {
                    lineData = lineData.dropLast()
                }
                // Decoded as the other ports do: String(data:encoding:) would drop
                // a byte order mark at the start of every line, and skip a line
                // that is not valid UTF-8 where the others draw U+FFFD.
                return String(decoding: lineData, as: UTF8.self)
            }
            if atEnd {
                // The last line, without a newline
                if buffer.isEmpty {
                    return nil
                }
                let line = String(decoding: buffer, as: UTF8.self)
                buffer.removeAll()
                return line
            }
            let chunk = file.readData(ofLength: 8192)
            if chunk.isEmpty {
                atEnd = true
                continue
            }
            buffer.append(chunk)
            if atStart {
                // A byte order mark at the start of the file is not part of the text.
                if buffer.starts(with: [0xEF, 0xBB, 0xBF]) {
                    buffer.removeSubrange(0..<3)
                }
                atStart = false
            }
        }
    }
}
