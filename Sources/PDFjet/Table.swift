/**
 * Table.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create table objects and draw them on a page.
///
/// Please see Example_08.
///
public class Table : Drawable {
    private var tableData: [[Cell]]
    private var numOfHeaderRows = 1
    private var numOfFooterRows = 0
    // The index of the next row to draw, or -1 when all rows are drawn.
    private var rendered = 1
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var firstPageTopMargin: Float = 0.0
    private var bottomMargin: Float = 0.0
    // The Table element of a PDF/UA document, while the table is drawn, and
    // the TH or TD elements of the last row, by column, that the rows with
    // the next lines of its wrapped text add to.
    private var structElement: StructElement?
    private var cellElements = [StructElement?]()

    ///
    /// Create a table object.
    ///
    public init() {
        tableData = [[Cell]]()
    }

    ///
    /// Creates a table from a text file with comma, pipe or tab separated values.
    /// The first line is the header row and uses f1; the other lines use f2.
    /// Every row gets as many cells as the first line has fields. A quoted field is read as
    /// RFC 4180 reads it, and a line break inside one goes on to the next line of the file and
    /// is drawn as a space.
    ///
    /// - Parameter f1: the font for the header row.
    /// - Parameter f2: the font for the other rows.
    /// - Parameter fileName: the file name.
    ///
    public init(_ f1: Font, _ f2: Font, _ fileName: String) throws {
        tableData = [[Cell]]()
        var delimiter: String?
        var numberOfFields = 0
        var lineNumber = 0
        // Swift treats "\r\n" as one character, which a "\n" separator does not
        // match, so Windows line endings are replaced first.
        // The bytes that are not UTF-8 are replaced with U+FFFD, as the other
        // ports replace them, rather than failing the whole file.
        var lines = String(decoding: try Content.ofBinaryFile(fileName), as: UTF8.self)
                .replacingOccurrences(of: "\r\n", with: "\n")
                .components(separatedBy: "\n")
        if lines.last == "" {
            lines.removeLast()          // Ignore the trailing end-of-line marker
        }
        var index = 0
        while index < lines.count {
            var line = lines[index]
            index += 1
            if lineNumber == 0 {
                // A byte order mark at the start of the file is not part of the text.
                if line.hasPrefix("\u{FEFF}") {
                    line.removeFirst()
                }
                delimiter = getDelimiter(line)
            }
            var row = [Cell]()
            // The empty fields at the end of the line are kept, a quoted field
            // holds its delimiters instead of being cut at them, and its line
            // breaks, which go on to the next lines, are spaces.
            let fields = Util.readRecord(line, delimiter!) {
                guard index < lines.count else {
                    return nil
                }
                index += 1
                return lines[index - 1]
            }
            if lineNumber == 0 {
                numberOfFields = fields.count
            }
            for field in fields {
                if lineNumber == 0 {
                    row.append(Cell(f1, field))
                } else {
                    row.append(Cell(f2, field))
                }
            }
            if row.count > numberOfFields {
                var row2 = [Cell]()
                for i in 0..<numberOfFields {
                    row2.append(row[i])
                }
                tableData.append(row2)
            } else if row.count < numberOfFields {
                let diff = numberOfFields - row.count
                for _ in 0..<diff {
                    row.append(Cell(f2, ""))
                }
                tableData.append(row)
            } else {
                tableData.append(row)
            }
            lineNumber += 1
        }
    }

    ///
    /// Sets the location (x, y) of the top left corner of this table on the page.
    ///
    /// - Parameter x: the x coordinate of the top left point of the table.
    /// - Parameter y: the y coordinate of the top left point of the table.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x1 = x
        self.y1 = y
        return self
    }

    ///
    /// Sets the bottom margin for this table.
    ///
    /// - Parameter bottomMargin: the margin.
    ///
    @discardableResult
    public func setBottomMargin(_ bottomMargin: Float) -> Table {
        self.bottomMargin = bottomMargin
        return self
    }

    ///
    /// Sets the table data.
    ///
    /// The table data is a perfect grid of cells.
    /// All cell should be an unique object and you can not reuse blank cell objects.
    /// Even if one or more cells have colspan bigger than zero the number of cells in the row will not change.
    ///
    /// - Parameter tableData: the table data.
    ///
    @discardableResult
    public func setTableData(_ tableData: [[Cell]]) -> Table {
        return setTableData(tableData, 0)
    }

    ///
    /// Sets the table data and specifies the number of header rows in this data.
    /// The header rows are drawn again at the top of every page.
    ///
    /// - Parameter tableData: the table data.
    /// - Parameter numOfHeaderRows: the number of header rows in this data.
    ///
    @discardableResult
    public func setTableData(_ tableData: [[Cell]], _ numOfHeaderRows: Int) -> Table {
        self.tableData = tableData
        self.numOfHeaderRows = numOfHeaderRows
        self.rendered = numOfHeaderRows
        addCellsToCompleteTheGrid()
        return self
    }

    ///
    /// Makes the last rows of the table data footer rows, which are drawn again
    /// at the end of the table on every page, under the last row the page
    /// holds, as the header rows are drawn again at the top. A page leaves room
    /// for them. In a PDF/UA document they are rows of the table once, where
    /// the table ends, and artifacts on the other pages.
    ///
    /// - Parameter numOfFooterRows: the number of footer rows at the end of the data.
    /// - Returns: this Table object.
    ///
    @discardableResult
    public func setNumberOfFooterRows(_ numOfFooterRows: Int) -> Table {
        self.numOfFooterRows = max(0, numOfFooterRows)
        return self
    }

    // The index of the first footer row, after the header rows and the rows
    // of the table.
    private func footerStart() -> Int {
        return max(numOfHeaderRows, tableData.count - numOfFooterRows)
    }

    // Adds empty cells to the rows that are shorter than the first row.
    private func addCellsToCompleteTheGrid() {
        if tableData.isEmpty || tableData[0].isEmpty {
            return
        }
        let numOfColumns = tableData[0].count
        let font = tableData[0][0].font
        for i in 0..<tableData.count {
            let diff = numOfColumns - tableData[i].count
            for _ in 0..<max(diff, 0) {
                tableData[i].append(Cell(font, ""))
            }
        }
    }

    ///
    /// Aligns to the right the cells whose text is a number, such as 1,234.50,
    /// (1,234.50), -5 or 1.5E+3. The periods, commas and apostrophes are ignored,
    /// and the number can be in parentheses.
    ///
    @discardableResult
    public func rightAlignNumbers() -> Table {
        for row in tableData {
            for cell in row {
                if cell.text != nil && Table.isNumber(cell.text!) {
                    cell.setTextAlignment(Alignment.RIGHT)
                }
            }
        }
        return self
    }

    // Returns true if the text, without its periods, commas and apostrophes and
    // the parentheses around it, is an optional sign, ASCII digits and an
    // optional exponent, with optional spaces before and after.
    static func isNumber(_ text: String) -> Bool {
        var scalars = [Unicode.Scalar](text.unicodeScalars)
        if scalars.count >= 2 && scalars[0] == "(" && scalars[scalars.count - 1] == ")" {
            scalars = [Unicode.Scalar](scalars[1..<(scalars.count - 1)])
        }
        var buf = String.UnicodeScalarView()
        for scalar in scalars {
            if scalar != "." && scalar != "," && scalar != "'" {
                buf.append(scalar)
            }
        }
        let number = [Unicode.Scalar](String(buf).trim().unicodeScalars)
        var i = 0
        if i < number.count && (number[i] == "+" || number[i] == "-") {
            i += 1
        }
        var start = i
        while i < number.count && number[i].value >= 0x30 && number[i].value <= 0x39 {
            i += 1
        }
        if i == start {
            return false
        }
        if i < number.count && (number[i] == "e" || number[i] == "E") {
            i += 1
            if i < number.count && (number[i] == "+" || number[i] == "-") {
                i += 1
            }
            start = i
            while i < number.count && number[i].value >= 0x30 && number[i].value <= 0x39 {
                i += 1
            }
            if i == start {
                return false
            }
        }
        return i == number.count
    }

    ///
    /// Makes the cell of a footer row show the sum of its column over the rows
    /// of each page: the page total. The numbers are read as rightAlignNumbers
    /// reads them, with commas and apostrophes between the thousands and a
    /// period before the decimals, and a cell that has no number is left out.
    /// The sum has the number of decimals, rounded half away from zero, and
    /// commas between the thousands. Until the table is drawn the cell has the
    /// sum of all the rows, so that autoAdjustColumnWidths leaves room for it;
    /// its text is not wrapped.
    ///
    /// - Parameter row: the index of a footer row, see setNumberOfFooterRows.
    /// - Parameter column: the index of the column.
    /// - Parameter decimals: the number of decimals, from 0 to 9.
    /// - Returns: this Table object.
    ///
    @discardableResult
    public func setPageSum(_ row: Int, _ column: Int, _ decimals: Int) -> Table {
        return setSum(row, column, decimals, Cell.PAGE_SUM)
    }

    ///
    /// Makes the cell of a footer row show the sum of its column over all the
    /// rows up to the end of each page: the total carried forward, which is
    /// the total of the table on its last page. The numbers are read and the
    /// sum is written as setPageSum reads and writes them.
    ///
    /// - Parameter row: the index of a footer row, see setNumberOfFooterRows.
    /// - Parameter column: the index of the column.
    /// - Parameter decimals: the number of decimals, from 0 to 9.
    /// - Returns: this Table object.
    ///
    @discardableResult
    public func setRunningSum(_ row: Int, _ column: Int, _ decimals: Int) -> Table {
        return setSum(row, column, decimals, Cell.RUNNING_SUM)
    }

    ///
    /// Makes the cell of a header row show the sum of its column over all the
    /// rows of the pages before: the total brought forward, which the cell that
    /// setRunningSum marks shows at the end of the page before. A header row
    /// with such a cell is drawn from the second page on, under the header
    /// rows above it, and not on the first page, which has nothing to bring
    /// forward; in a PDF/UA document it is an artifact, as the header rows are
    /// on those pages. The numbers are read and the sum is written as
    /// setPageSum reads and writes them.
    ///
    /// - Parameter row: the index of a header row, see setTableData.
    /// - Parameter column: the index of the column.
    /// - Parameter decimals: the number of decimals, from 0 to 9.
    /// - Returns: this Table object.
    ///
    @discardableResult
    public func setBroughtForwardSum(_ row: Int, _ column: Int, _ decimals: Int) -> Table {
        return setSum(row, column, decimals, Cell.BROUGHT_FORWARD_SUM)
    }

    private func setSum(_ row: Int, _ column: Int, _ decimals: Int, _ kind: UInt32) -> Table {
        if row < 0 || row >= tableData.count || column < 0 || column >= tableData[row].count {
            return self
        }
        let places = max(0, min(9, decimals))
        let cell = tableData[row][column]
        cell.properties = (cell.properties & ~Cell.SUM_BITS) | kind | (UInt32(places) << Cell.SUM_DECIMALS)
        let end = (kind == Cell.BROUGHT_FORWARD_SUM) ? footerStart() : min(row, footerStart())
        cell.setText(Table.formatSum(sumOf(column, numOfHeaderRows, end, places), places))
        return self
    }

    // Writes the sums in the cells of the rows from start to stop that
    // setPageSum, setRunningSum and setBroughtForwardSum mark: those of the
    // rows of the page, from first to end, those of all the rows to end, and
    // those of all the rows before first.
    private func setSums(_ start: Int, _ stop: Int, _ first: Int, _ end: Int) {
        var r = start
        while r < stop && r < tableData.count {
            for (j, cell) in tableData[r].enumerated() where (cell.properties & Cell.SUM_KIND) != 0 {
                let kind = cell.properties & Cell.SUM_KIND
                let places = Int((cell.properties >> Cell.SUM_DECIMALS) & 0xF)
                let from = (kind == Cell.PAGE_SUM) ? first : numOfHeaderRows
                let to = (kind == Cell.BROUGHT_FORWARD_SUM) ? first : end
                cell.setText(Table.formatSum(sumOf(j, from, to, places), places))
            }
            r += 1
        }
    }

    private func setFooterSums(_ first: Int, _ end: Int) {
        setSums(footerStart(), tableData.count, first, end)
    }

    // True when the row, or the row whose wrapped text it holds, has a cell
    // that brings a total forward, which the first page does not draw.
    private func isBroughtForwardRow(_ row: Int) -> Bool {
        var r = row
        while r > 0 && isContinuation(r) {
            r -= 1
        }
        return tableData[r].contains { ($0.properties & Cell.SUM_KIND) == Cell.BROUGHT_FORWARD_SUM }
    }

    // The largest sum, and number, in units of the decimals: 18 digits.
    private static let MAX_SUM: Int64 = 999_999_999_999_999_999

    // The sum of the numbers in the column of the rows from first to end, in
    // units of the decimals. A number that would take the sum past 18 digits
    // is left out.
    private func sumOf(_ column: Int, _ first: Int, _ end: Int, _ decimals: Int) -> Int64 {
        var sum: Int64 = 0
        var r = max(0, first)
        while r < end && r < tableData.count {
            let row = tableData[r]
            r += 1
            if column >= row.count {
                continue
            }
            let cell = row[column]
            if (cell.properties & Cell.COVERED) != 0 {
                continue
            }
            if let text = cell.text, let value = Table.numberOf(text, decimals), abs(sum + value) <= Table.MAX_SUM {
                sum += value
            }
        }
        return sum
    }

    ///
    /// Returns the number the text is, in units of the decimals, rounded half
    /// away from zero, or nil when the text is not a number, as isNumber
    /// reads it, has more than one period, or is more than 18 digits in those
    /// units. Commas and apostrophes are between the thousands, a period is
    /// before the decimals, and parentheses make a number negative.
    ///
    static func numberOf(_ text: String, _ decimals: Int) -> Int64? {
        if !isNumber(text) {
            return nil
        }
        var str = Substring(text)
        var negative = false
        if str.count >= 2 && str.first == "(" && str.last == ")" {
            str = str.dropFirst().dropLast()
            negative = true
        }
        var chars = Array(String(str.filter { $0 != "," && $0 != "'" })
                .trimmingCharacters(in: .whitespaces))
        if chars[0] == "+" || chars[0] == "-" {
            negative = negative != (chars[0] == "-")
            chars.removeFirst()
        }
        var exponent = 0
        if let e = chars.firstIndex(where: { $0 == "e" || $0 == "E" }) {
            var exp = Array(chars[(e + 1)...])
            var sign = 1
            if exp[0] == "+" || exp[0] == "-" {
                sign = (exp[0] == "-") ? -1 : 1
                exp.removeFirst()
            }
            if exp.count > 4 || exp.contains(".") {
                return nil
            }
            exponent = sign * Int(String(exp))!
            chars = Array(chars[..<e])
        }
        let points = chars.indices.filter { chars[$0] == "." }
        if points.count > 1 {
            return nil
        }
        let fraction = points.isEmpty ? [] : Array(chars[(points[0] + 1)...])
        var digits = points.isEmpty ? chars : Array(chars[..<points[0]]) + fraction
        while let first = digits.first, first == "0" {
            digits.removeFirst()
        }
        if digits.isEmpty {
            return 0
        }
        // The number is the digits times 10 to the shift, in units of the
        // decimals.
        let shift = exponent - fraction.count + decimals
        var value: Int64
        if shift >= 0 {
            if digits.count + shift > 18 {
                return nil
            }
            value = Int64(String(digits))!
            for _ in 0..<shift {
                value *= 10
            }
        } else {
            let keep = digits.count + shift
            if keep < 0 {
                return 0
            }
            if keep > 18 {
                return nil
            }
            value = (keep == 0) ? 0 : Int64(String(digits[..<keep]))!
            if digits[keep] >= "5" {
                value += 1
            }
            if value > MAX_SUM {
                return nil
            }
        }
        return negative ? -value : value
    }

    ///
    /// Returns the sum in units of the decimals as text: the decimals after a
    /// period, and commas between the thousands.
    ///
    static func formatSum(_ sum: Int64, _ decimals: Int) -> String {
        var digits = Array(String(abs(sum)))
        while digits.count <= decimals {
            digits.insert("0", at: 0)
        }
        let point = digits.count - decimals
        var text = (sum < 0) ? "-" : ""
        for i in 0..<point {
            if i > 0 && (point - i) % 3 == 0 {
                text += ","
            }
            text.append(digits[i])
        }
        if decimals > 0 {
            text += "." + String(digits[point...])
        }
        return text
    }

    ///
    /// Keeps the row with the specified index on the same page as the next
    /// row, as a heading row is kept with the rows under it: a page break does
    /// not fall between them, and moves both to the next page. Rows kept with
    /// the next one one after another are kept together, unless together they
    /// are taller than a page. Call it before the table is drawn.
    ///
    /// - Parameter index: the index of the row.
    /// - Returns: this Table object.
    ///
    @discardableResult
    public func keepRowWithNext(_ index: Int) -> Table {
        if index >= 0 && index < tableData.count {
            for cell in tableData[index] {
                cell.properties |= Cell.KEPT_WITH_NEXT
            }
        }
        return self
    }

    ///
    /// Removes the horizontal lines between the rows from index1 to index2.
    ///
    @discardableResult
    public func removeLineBetweenRows(_ index1: Int, _ index2: Int) -> Table {
        var j = index1
        while j < index2 {
            var row = tableData[j]
            for cell in row {
                cell.setBorder(Border.BOTTOM, false)
            }
            row = tableData[j + 1]
            for cell in row {
                cell.setBorder(Border.TOP, false)
            }
            j += 1
        }
        return self
    }

    ///
    /// Sets the text alignment in the specified column.
    /// Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY.
    ///
    /// - Parameter index: the index of the specified column.
    /// - Parameter alignment: the specified alignment.
    ///
    @discardableResult
    public func setTextAlignmentInColumn(_ index: Int, _ alignment: Alignment) -> Table {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setTextAlignment(alignment)
                if let textBlock = cell.getTextBlock() {
                    textBlock.setTextAlignment(alignment)
                }
            }
        }
        return self
    }

    ///
    /// Sets the color of the text in the specified column.
    ///
    /// - Parameter index: the index of the specified column.
    /// - Parameter color: the color specified as an integer.
    ///
    @discardableResult
    public func setTextColorInColumn(_ index: Int, _ color: Int32) -> Table {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setTextColor(color)
                if let textBlock = cell.getTextBlock() {
                    textBlock.setTextColor(color)
                }
            }
        }
        return self
    }

    ///
    /// Sets the font and the font size of the cells in the specified column.
    ///
    /// - Parameter index: the column index.
    /// - Parameter font: the font.
    ///
    @discardableResult
    public func setFontInColumn(_ index: Int, _ font: Font) -> Table {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setFont(font).setFontSize(font.size)
                if let textBlock = cell.getTextBlock() {
                    textBlock.setFont(font).setFontSize(font.getSize())
                }
            }
        }
        return self
    }

    ///
    /// Sets the color of the text in the specified row.
    ///
    /// - Parameter index: the index of the specified row.
    /// - Parameter color: the color specified as an integer.
    ///
    @discardableResult
    public func setTextColorInRow(_ index: Int, _ color: Int32) -> Table {
        if index < tableData.count {
            let row = tableData[index]
            for cell in row {
                cell.setTextColor(color)
                if let textBlock = cell.getTextBlock() {
                    textBlock.setTextColor(color)
                }
            }
        }
        return self
    }

    ///
    /// Sets the font and the font size of the cells in the specified row.
    ///
    /// - Parameter index: the row index.
    /// - Parameter font: the font.
    ///
    @discardableResult
    public func setFontInRow(_ index: Int, _ font: Font) -> Table {
        if index < tableData.count {
            let row = tableData[index]
            for cell in row {
                cell.setFont(font).setFontSize(font.size)
                if let textBlock = cell.getTextBlock() {
                    textBlock.setFont(font).setFontSize(font.getSize())
                }
            }
        }
        return self
    }

    ///
    /// Sets the width of the column with the specified index.
    ///
    /// - Parameter index: the index of specified column.
    /// - Parameter width: the specified width.
    ///
    @discardableResult
    public func setColumnWidth(_ index: Int, _ width: Float) -> Table {
        for row in tableData {
            if index < row.count {
                row[index].setWidth(width)
            }
        }
        return self
    }

    ///
    /// Returns the column width of the column at the specified index.
    ///
    /// - Parameter index: the index of the column.
    /// - Returns: the width of the column.
    ///
    public func getColumnWidth(_ index: Int) -> Float {
        return getCellAt(0, index).getWidth()
    }

    ///
    /// Returns the cell at the specified row and column.
    ///
    /// - Parameter row: the specified row.
    /// - Parameter col: the specified column.
    ///
    /// - Returns: the cell at the specified row and column.
    ///
    public func getCellAt(_ row: Int, _ col: Int) -> Cell {
        if row >= 0 {
            return tableData[row][col]
        }
        return tableData[tableData.count + row][col]
    }

    ///
    /// Returns a list of cells for the specified row.
    ///
    /// - Parameter index: the index of the specified row.
    ///
    /// - Returns: the list of cells.
    ///
    public func getRow(_ index: Int) -> [Cell] {
        return tableData[index]
    }

    ///
    /// Returns a list of cells for the specified column.
    ///
    /// - Parameter index: the index of the specified column.
    ///
    /// - Returns: the list of cells.
    ///
    public func getColumn(_ index: Int) -> [Cell] {
        var column = [Cell]()
        for row in tableData {
            if index < row.count {
                column.append(row[index])
            }
        }
        return column
    }

    ///
    /// Draws this table on the specified page.
    ///
    /// - Parameter page: the page to draw this table on.
    ///
    /// - Returns: the x and y coordinates of the bottom right corner of the table.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if tableData.isEmpty {
            return [x1, y1]     // An empty table draws nothing.
        }
        wrapAroundCellText()
        applyRowSpans()
        setRightBorderOnLastColumn()
        setBottomBorderOnLastRow()
        let xy = drawTableRows(page, drawHeaderRows(page, 0))
        return [x1 + getWidth(), xy[1]]
    }

    ///
    /// Draws this table on as many new pages as it needs.
    /// The pages are created detached and added to the list; add them to the PDF afterwards.
    ///
    /// - Parameter pdf: the PDF document.
    /// - Parameter pages: the list that receives the new pages.
    /// - Parameter pageSize: the page size, for example Letter.PORTRAIT.
    /// - Returns: the x and y coordinates of the bottom right corner of the table on the last page,
    ///   or nil when the table was already drawn and no page was added.
    ///
    @discardableResult
    public func drawOn(_ pdf: PDF, _ pages: inout [Page], _ pageSize: PageSize) -> [Float]? {
        if tableData.isEmpty {
            return [x1, y1]     // An empty table needs no page.
        }
        wrapAroundCellText()
        applyRowSpans()
        setRightBorderOnLastColumn()
        setBottomBorderOnLastRow()
        var xy: [Float]?
        var pageNumber: Int = 1
        while (hasMoreData()) {
            let page = Page(pdf, pageSize, false)
            pages.append(page)
            xy = drawTableRows(page, drawHeaderRows(page, pageNumber))
            pageNumber += 1
        }
        if let xy = xy {
            return [x1 + getWidth(), xy[1]]
        }
        return nil
    }

    private func drawHeaderRows(_ page: Page?, _ pageNumber: Int) -> [Float] {
        let x = x1
        var y = y1
        if pageNumber == 1 && firstPageTopMargin > 0.0 {
            y = firstPageTopMargin
        }
        // In a PDF/UA document the table is a Table element, which the rows
        // drawn on the next pages go on adding to. The header rows are TH
        // cells the first time they are drawn, and artifacts on the next pages.
        let first = (rendered == numOfHeaderRows)
        if let page = page, first || structElement == nil {
            structElement = page.addStructElement(
                    page.structParent, StructElem.TABLE, nil, true)
        }
        if let page = page, !first && numOfHeaderRows > 0 {
            page.addArtifactBMC()
        }
        let heights = getRowHeights()
        // The rows that bring a total forward are drawn from the second page
        // on, with the total of the rows before the page.
        var last = -1
        for i in 0..<min(numOfHeaderRows, tableData.count) where !(first && isBroughtForwardRow(i)) {
            last = i
        }
        if rendered != -1 {
            setSums(0, numOfHeaderRows, rendered, rendered)
        }
        for i in 0..<min(numOfHeaderRows, tableData.count) {
            if first && isBroughtForwardRow(i) {
                continue
            }
            let row = tableData[i]
            if let page = page {
                if i == last {
                    for cell in row {
                        cell.setBorder(Border.BOTTOM, true)
                    }
                }
                drawRow(page, row, x, y, heights, i, first ? StructElem.TH : nil)
            }
            y += heights[i]
        }
        if let page = page, !first && numOfHeaderRows > 0 {
            page.addEMC()
        }
        return [x, y]
    }

    // Draws the cells of the row. In a PDF/UA document the row is a TR element
    // and each cell a TH or TD element, which holds what the cell draws; a row
    // that goes on with the wrapped text of the row above adds to its elements.
    // With no cell structure the row is not tagged, as it is an artifact.
    private func drawRow(
            _ page: Page,
            _ row: [Cell],
            _ x: Float,
            _ y: Float,
            _ heights: [Float],
            _ rowIndex: Int,
            _ cellStructure: StructElem?) {
        let parent = page.structParent
        let tagged = (structElement != nil && cellStructure != nil)
        let continued = (row[0].properties & Cell.CONTINUED) != 0
        var rowElement: StructElement?
        if tagged && !continued && !allCovered(row) {
            rowElement = page.addStructElement(structElement, StructElem.TR, nil)
            if cellElements.count != row.count {
                cellElements = [StructElement?](repeating: nil, count: row.count)
            }
        }
        var x = x
        var i = 0
        while i < row.count {
            let cell = row[i]
            let colspan = max(1, cell.getColSpan())
            let covered = (cell.properties & Cell.COVERED) != 0
            if tagged && !covered {
                if !continued {
                    cellElements[i] = page.addStructElement(rowElement, cellStructure!,
                            getAttributes(cellStructure!, colspan, cell.rowsSpanned))
                }
                page.structParent = (i < cellElements.count) ? cellElements[i] : nil
            }
            var w: Float = 0.0
            for _ in 0..<colspan {
                w += row[i].getWidth()
                i += 1
            }
            if !covered {
                // A cell that spans rows is as tall as all the rows it covers.
                var cellHeight: Float = 0.0
                for r in rowIndex..<min(rowIndex + cell.rowsSpanned, heights.count) {
                    cellHeight += heights[r]
                }
                page.setBrushColor(cell.textColor)
                cell.drawOn(page, x, y, w, cellHeight)
            }
            x += w
        }
        page.structParent = parent
    }

    // True when every cell of the row is one that a cell above it spans over,
    // so the row holds no cell of its own and is not a row of the table.
    private func allCovered(_ row: [Cell]) -> Bool {
        for cell in row where (cell.properties & Cell.COVERED) == 0 {
            return false
        }
        return !row.isEmpty
    }

    // The attributes of a table cell element: the scope of a header cell and
    // the number of rows and columns a cell spans, or nil when it has none.
    private func getAttributes(
            _ cellStructure: StructElem, _ colspan: Int, _ rowspan: Int) -> String? {
        var spans = ""
        if colspan > 1 {
            spans += " /ColSpan \(colspan)"
        }
        if rowspan > 1 {
            spans += " /RowSpan \(rowspan)"
        }
        if cellStructure == StructElem.TH {
            return "<</O /Table /Scope /Column" + spans + ">>"
        }
        return spans.isEmpty ? nil : "<</O /Table" + spans + ">>"
    }

    // Draws the rows from the next row to draw, as many as fit on the page,
    // and the footer rows under them. With no page it measures them all and
    // leaves the next row to draw as it is.
    private func drawTableRows(_ page: Page?, _ xy: [Float]) -> [Float] {
        let x = xy[0]
        var y = xy[1]
        let footer = footerStart()
        let done = (rendered == -1)
        var index = done ? footer : rendered
        let first = index
        let heights = getRowHeights()
        // Where the rows start on the next pages, under the header rows.
        var top = y1
        for r in 0..<min(numOfHeaderRows, heights.count) {
            top += heights[r]
        }
        // Where the rows end on a page, over the footer rows.
        var bottom: Float = (page == nil) ? 0.0 : page!.height - bottomMargin
        for r in footer..<tableData.count {
            bottom -= heights[r]
        }
        // The rows before the first of these are not kept with the next row,
        // and the lines of the rows before the second are cut where the page
        // ends, as rows of their own.
        var cutRowsUntil = -1
        var cutLinesUntil = -1
        while index < footer {
            // The rows a cell spans, the lines a row wraps into and the rows
            // kept with the next one are drawn together, so that a page break
            // never cuts one of them in two. The footer rows are not in them.
            let keepLines = (index >= cutLinesUntil)
            let keepRows = keepLines && (index >= cutRowsUntil)
            let end = min(rowGroupEnd(index, keepLines, keepRows), footer)
            var groupHeight: Float = 0.0
            for r in index..<end {
                groupHeight += heights[r]
            }
            if page != nil && (y + groupHeight) > bottom {
                if keepLines && groupHeight > bottom - top {
                    // Rows that would not fit the next page either are drawn
                    // from here: first each on its own, and then, for a row
                    // that is taller than a page, each line on its own, cut
                    // where the page ends.
                    if keepRows {
                        cutRowsUntil = end
                    } else {
                        cutLinesUntil = end
                    }
                    continue
                }
                // A row that does not fit goes on the next page, unless it is
                // the first row of this one: a row taller than the page fits
                // no page, and leaving it for the next page would ask for
                // pages forever.
                if index > first {
                    rendered = index
                    setFooterSums(first, index)
                    return [x, drawFooterRows(page, x, y, heights, false)]
                }
            }
            for r in index..<end {
                if let page = page {
                    drawRow(page, tableData[r], x, y, heights, r, StructElem.TD)
                }
                y += heights[r]
            }
            index = end
        }
        if !done {
            // The rows of the table are all drawn, and the footer rows end it.
            setFooterSums(first, footer)
            y = drawFooterRows(page, x, y, heights, true)
        }
        if page != nil {
            rendered = -1   // We are done!
        }
        return [x, y]
    }

    // Draws the footer rows at y and returns the y under them. In a PDF/UA
    // document they are rows of the table where it ends, and artifacts on the
    // pages before.
    private func drawFooterRows(_ page: Page?, _ x: Float, _ y: Float, _ heights: [Float], _ last: Bool) -> Float {
        let footer = footerStart()
        let artifact = (page != nil && !last && footer < tableData.count)
        if artifact {
            page!.addArtifactBMC()
        }
        var y = y
        for r in footer..<tableData.count {
            if let page = page {
                if r == footer {
                    for cell in tableData[r] {
                        cell.setBorder(Border.TOP, true)
                    }
                }
                drawRow(page, tableData[r], x, y, heights, r, last ? StructElem.TD : nil)
            }
            y += heights[r]
        }
        if artifact {
            page!.addEMC()
        }
        return y
    }

    // Works out what each cell that spans rows covers, after the text is
    // wrapped: a row of the table is drawn as one row for each line its
    // tallest cell needs, so a cell that spans two rows of the table spans as
    // many rows of the drawing as those two were wrapped into. The cells the
    // span covers are marked, and draw nothing.
    private func applyRowSpans() {
        for row in tableData {
            for cell in row {
                cell.properties &= ~Cell.COVERED
                cell.rowsSpanned = 1
            }
        }
        for r in 0..<tableData.count {
            if isContinuation(r) {
                continue    // A span starts in a row of the table, not in the wrap of one.
            }
            let row = tableData[r]
            var i = 0
            while i < row.count {
                let cell = row[i]
                let colspan = max(1, cell.getColSpan())
                if cell.getRowSpan() > 1 && (cell.properties & Cell.COVERED) == 0 {
                    let end = rowAfter(r, cell.getRowSpan())
                    let ownEnd = rowAfter(r, 1)
                    cell.rowsSpanned = end - r
                    // The rows the wrapped text of this cell takes keep their
                    // text and lose the border that would cross the cell.
                    for r2 in (r + 1)..<max(r + 1, ownEnd) {
                        setSpanned(r2, i, colspan, false)
                    }
                    for r2 in ownEnd..<max(ownEnd, end) {
                        setSpanned(r2, i, colspan, true)
                    }
                }
                i += colspan
            }
        }
    }

    // Marks the columns of the row that a span covers: a covered cell draws
    // nothing, and a row of the wrapped text of the spanning cell keeps its
    // text without the border under it.
    private func setSpanned(_ r: Int, _ column: Int, _ colspan: Int, _ covered: Bool) {
        let row = tableData[r]
        var i = 0
        while i < row.count {
            let cell = row[i]
            if i >= column && i < column + colspan {
                if covered {
                    cell.properties |= Cell.COVERED
                } else {
                    cell.setBorder(Border.BOTTOM, false)
                }
            }
            i += max(1, cell.getColSpan())
        }
    }

    // The index of the row after the count rows of the table that start at r,
    // counting the rows the wrapped text of each of them takes.
    private func rowAfter(_ r: Int, _ count: Int) -> Int {
        var index = r
        var i = 0
        while i < count && index < tableData.count {
            index += 1
            while index < tableData.count && isContinuation(index) {
                index += 1
            }
            i += 1
        }
        return index
    }

    // True when the row holds the wrapped text of the row above it.
    private func isContinuation(_ r: Int) -> Bool {
        let row = tableData[r]
        return !row.isEmpty && (row[0].properties & Cell.CONTINUED) != 0
    }

    // True when the row, or the row whose wrapped text it holds, is kept with
    // the next row.
    private func isKeptWithNext(_ r: Int) -> Bool {
        let row = tableData[r]
        return !row.isEmpty && (row[0].properties & Cell.KEPT_WITH_NEXT) != 0
    }

    // The height of each row of the table as it is drawn. A cell that spans
    // rows is not what makes its first row tall; the rows it covers hold it
    // together, and the last of them grows when they do not.
    private func getRowHeights() -> [Float] {
        var heights = [Float](repeating: 0.0, count: tableData.count)
        for r in 0..<tableData.count {
            heights[r] = getMaxCellHeight(tableData[r])
        }
        for r in 0..<tableData.count {
            let row = tableData[r]
            for i in 0..<row.count {
                let cell = row[i]
                if cell.rowsSpanned < 2 {
                    continue
                }
                let end = min(r + cell.rowsSpanned, tableData.count)
                var have: Float = 0.0
                for r2 in r..<end {
                    have += heights[r2]
                }
                let needed = cell.getHeight(getTotalWidth(row, i))
                if needed > have && end > r {
                    heights[end - 1] += needed - have
                }
            }
        }
        return heights
    }

    // The row after the rows that a span holds together, with keepLines the
    // lines the last of them wraps into, and with keepRows the rows that the
    // last of them is kept with, which a page break keeps on one page.
    private func rowGroupEnd(_ index: Int, _ keepLines: Bool, _ keepRows: Bool) -> Int {
        var end = index + 1
        var r = index
        while r < end && r < tableData.count {
            for cell in tableData[r] where r + cell.rowsSpanned > end {
                end = r + cell.rowsSpanned
            }
            if r + 1 == end && end < tableData.count {
                if keepLines && isContinuation(end) {
                    end += 1
                } else if keepRows && isKeptWithNext(r) {
                    end += 1
                }
            }
            r += 1
        }
        return min(end, tableData.count)
    }

    private func getMaxCellHeight(_ row: [Cell]) -> Float {
        var maxCellHeight: Float = 0.0
        var spanned: Float = 0.0
        for i in 0..<row.count {
            let cell = row[i]
            if (cell.properties & Cell.COVERED) != 0 {
                continue    // A cell the one above it draws over.
            }
            let cellHeight = cell.getHeight(getTotalWidth(row, i))
            if cell.rowsSpanned > 1 {
                // A cell that spans rows is as tall as all of them together,
                // which getRowHeights shares out; it is the height of the row
                // only when nothing else is in it.
                spanned = max(spanned, cellHeight / Float(cell.rowsSpanned))
                continue
            }
            if cellHeight > maxCellHeight {
                maxCellHeight = cellHeight
            }
        }
        return (maxCellHeight > 0.0) ? maxCellHeight : spanned
    }

    ///
    /// Returns true if the table contains more data that needs to be drawn on a page.
    ///
    private func hasMoreData() -> Bool {
        return self.rendered != -1
    }

    ///
    /// Returns the width of this table when drawn on a page.
    ///
    /// - Returns: the width of this table.
    ///
    public func getWidth() -> Float {
        var tableWidth: Float = 0.0
        if tableData.count > 0 {
            let row = tableData[0]
            for cell in row {
                tableWidth += cell.getWidth()
            }
        }
        return tableWidth
    }

    ///
    /// Returns the number of rows below the header rows that are drawn so far,
    /// counting each line of wrapped cell text as a row, or -1 when all rows
    /// are drawn.
    ///
    /// - Returns: the number of rendered rows.
    ///
    public func getRowsRendered() -> Int {
        return rendered == -1 ? rendered : rendered - numOfHeaderRows
    }

    ///
    /// Sets all table cells borders to `false` or `true`.
    ///
    @discardableResult
    public func setCellBorders(_ borders: Bool) -> Table {
        for row in tableData {
            for cell in row {
                cell.setBorders(borders)
            }
        }
        return self
    }

    ///
    /// Sets the color of the cell border lines.
    ///
    /// - Parameter color: the color of the cell border lines.
    ///
    @discardableResult
    public func setCellBorderColor(_ color: Int32) -> Table {
        for row in tableData {
            for cell in row {
                cell.setBorderColor(color)
            }
        }
        return self
    }

    /// Sets the color of the cell border lines from an array of red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setCellBorderColor(_ rgbColor: [Float]) -> Table {
        for row in tableData {
            for cell in row {
                cell.setBorderColor(rgbColor)
            }
        }
        return self
    }

    ///
    /// Sets the width of the cell border lines.
    ///
    /// - Parameter width: the width of the border lines.
    ///
    @discardableResult
    public func setCellBorderWidth(_ width: Float) -> Table {
        for row in tableData {
            for cell in row {
                cell.setBorderWidth(width)
            }
        }
        return self
    }

    // Sets the right border on all cells in the last column.
    private func setRightBorderOnLastColumn() {
        for row in tableData {
            if !row.isEmpty && row[0].getBorder(Border.LEFT) == false {
                return
            }
        }
        // Only run this code if all the cells in the first column have left border.
        for row in tableData {
            var cell: Cell?
            var i = 0
            while i < row.count {
                cell = row[i]
                i += Int(cell!.getColSpan())
            }
            cell?.setBorder(Border.RIGHT, true)
        }
    }

    // Sets the bottom border on all cells in the last row.
    private func setBottomBorderOnLastRow() {
        if tableData.isEmpty {
            return
        }
        let firstRow = tableData[0]
        for cell in firstRow {
            if cell.getBorder(Border.TOP) == false {
                return
            }
        }
        // Only run this code if all the cells in the first row have top border.
        let lastRow = tableData[tableData.count - 1]
        for cell in lastRow {
            cell.setBorder(Border.BOTTOM, true)
        }
    }

    ///
    /// Auto adjusts the widths of all columns so that they are just wide enough to
    /// hold the text without truncation.
    ///
    @discardableResult
    public func autoAdjustColumnWidths() -> Table {
        if tableData.isEmpty {
            return self
        }
        var maxColWidths = [Float](repeating: 0.0, count: tableData[0].count)
        for row in tableData {
            for i in 0..<row.count {
                let cell = row[i]
                if cell.getColSpan() == 1 {
                    if let textBlock = cell.getTextBlock() {
                        let tokens = textBlock.textContent.splitOnWhitespace()
                        for token in tokens {
                            var tokenWidth = textBlock.font.stringWidth(textBlock.fallbackFont, token)
                            tokenWidth += cell.getLeftPadding() + cell.getRightPadding()
                            if tokenWidth > maxColWidths[i] {
                                maxColWidths[i] = tokenWidth
                            }
                        }
                    } else if let drawable = cell.drawable {
                        let drawableWidth = Cell.measure(drawable)[0] + cell.getLeftPadding() + cell.getRightPadding()
                        if drawableWidth > maxColWidths[i] {
                            maxColWidths[i] = drawableWidth
                        }
                    } else if cell.text != nil {
                        var textWidth = cell.font.stringWidth(cell.fallbackFont, cell.fontSize, cell.text)
                        textWidth += cell.getLeftPadding() + cell.getRightPadding()
                        if textWidth > maxColWidths[i] {
                            maxColWidths[i] = textWidth
                        }
                    }
                }
            }
        }
        for row in tableData {
            for i in 0..<row.count {
                row[i].setWidth(maxColWidths[i])
            }
        }
        return self
    }

    func getTotalWidth(_ row: [Cell], _ index: Int) -> Float {
        let cell = row[index]
        let colspan = Int(cell.getColSpan())
        var cellWidth = Float(0.0)
        for i in 0..<colspan {
            cellWidth += row[index + i].getWidth()
        }
        cellWidth -= (cell.getLeftPadding() + row[index + (colspan - 1)].getRightPadding())
        return cellWidth
    }

    ///
    /// Wraps around the text in all cells so it fits the column width.
    /// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
    ///
    func wrapAroundCellText() {
        var tableData2 = [[Cell]]()
        var lines = [[String]?]()
        var numOfHeaderRows2 = 0
        // The footer rows are the rows of the wrap of the rows they were.
        let footer = (numOfFooterRows > 0) ? footerStart() : tableData.count
        var footer2 = 0
        for (r, row) in tableData.enumerated() {
            let first = tableData2.count
            if r == footer {
                footer2 = first
            }
            tableData2.append(row)  // Add the original row
            // Every cell of the row is wrapped once, here. The lines it needs
            // are what the cells stacked below it get, and the most lines any
            // cell of the row needs is how many rows to stack. The rows added
            // below a header row are header rows too.
            lines.removeAll(keepingCapacity: true)
            var maxNumVerCells = 1
            for i in 0..<row.count {
                // A cell that draws a line of text of its own draws no cell
                // text, so there is nothing to wrap. A cell that shows a sum
                // is not wrapped either, as its text is the sum of each page.
                let cellLines = (row[i].text == nil || row[i].drawable is BaselineDrawable
                        || (row[i].properties & Cell.SUM_BITS) != 0)
                        ? nil : wrapCellText(row, i)
                lines.append(cellLines)
                if let cellLines = cellLines, cellLines.count > maxNumVerCells {
                    maxNumVerCells = cellLines.count
                }
            }
            // A cell whose text wraps is one cell drawn as the rows its lines
            // take, so the border under it belongs under the last of them and
            // not under every line of it. A cell that spans rows is drawn over
            // all of them at once and so draws its own bottom border under the
            // whole of it; applyRowSpans clears the rows of its wrap.
            var bottomBorder = [Bool](repeating: false, count: row.count)
            for (i, cell) in row.enumerated() {
                bottomBorder[i] = maxNumVerCells > 1 && cell.getRowSpan() == 1
                        && cell.getBorder(Border.BOTTOM)
                if bottomBorder[i] {
                    cell.setBorder(Border.BOTTOM, false)
                }
            }
            var k = 1
            while k < maxNumVerCells {
                var row2 = [Cell]()
                for (j, cell) in row.enumerated() {
                    let cell2 = Cell(cell.getFont())
                    cell2.setFallbackFont(cell.getFallbackFont())
                    cell2.setFontSize(cell.fontSize)
                    cell2.setWidth(cell.getWidth())
                    cell2.setLeftPadding(cell.getLeftPadding())
                    cell2.setRightPadding(cell.getRightPadding())
                    cell2.backgroundColor = cell.backgroundColor
                    cell2.setBorderWidth(cell.getBorderWidth())
                    cell2.borderColor = cell.borderColor
                    cell2.textColor = cell.textColor
                    cell2.setColSpan(cell.getColSpan())
                    cell2.setTextAlignment(cell.getTextAlignment())
                    cell2.properties = cell.properties
                    cell2.setVerticalAlignment(cell.getVerticalAlignment())
                    cell2.setTopPadding(0.0)
                    cell2.setBorder(Border.TOP, false)
                    cell2.setBorder(Border.BOTTOM, bottomBorder[j] && k == maxNumVerCells - 1)
                    cell2.properties |= Cell.CONTINUED
                    cell2.properties &= ~Cell.SUM_BITS
                    row2.append(cell2)
                }
                tableData2.append(row2)
                k += 1
            }
            for j in 0..<row.count {
                if let cellLines = lines[j] {
                    for (n, line) in cellLines.enumerated() {
                        tableData2[first + n][j].setText(line)
                    }
                }
            }
            // The stacked rows are wrapped in their turn, as they were when
            // the rows were all added first and the table wrapped in one pass
            // afterwards: a line that ends in a space loses it here.
            for i in (first + 1)..<tableData2.count {
                let row2 = tableData2[i]
                for j in 0..<row2.count where row2[j].text != nil {
                    for (n, line) in wrapCellText(row2, j).enumerated() {
                        tableData2[i + n][j].setText(line)
                    }
                }
            }
            if r < numOfHeaderRows {
                numOfHeaderRows2 = tableData2.count
            }
        }
        if rendered != -1 {
            rendered += numOfHeaderRows2 - numOfHeaderRows
        }
        numOfHeaderRows = numOfHeaderRows2
        if footer < tableData.count {
            numOfFooterRows = tableData2.count - footer2
        }
        tableData = tableData2
    }

    // The lines the text of the cell needs to fit the width of its column.
    // A token wider than the column is broken between two of its characters.
    private func wrapCellText(_ row: [Cell], _ index: Int) -> [String] {
        let cell = row[index]
        let cellWidth = getTotalWidth(row, index)
        var lines = [String]()
        var buf = String()
        for token in cell.text!.splitOnWhitespace() {
            if cell.font.stringWidth(cell.fallbackFont, cell.fontSize, token) > cellWidth {
                if !buf.isEmpty {
                    buf.append(" ")
                }
                for scalar in token.unicodeScalars {
                    if cell.font.stringWidth(cell.fallbackFont, cell.fontSize,
                            buf + String(scalar)) > cellWidth {
                        lines.append(buf)
                        buf = ""
                    }
                    buf.append(String(scalar))
                }
            } else if buf.isEmpty {
                // A token that fits the column fits a line of its own, and its
                // width is the one measured just above.
                buf.append(token)
            } else if cell.font.stringWidth(cell.fallbackFont, cell.fontSize,
                    (buf + " " + token).trim()) > cellWidth {
                lines.append(buf.trim())
                buf = ""
                buf.append(token)
            } else {
                buf.append(" ")
                buf.append(token)
            }
        }
        lines.append(buf.trim())
        return lines
    }

    ///
    /// Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
    ///
    /// - Returns: the number of vertical cells needed to wrap around the cell text.
    ///
    func getNumVerCells(_ row: [Cell], _ index: Int) -> Int {
        if row[index].text == nil {
            return 1
        }
        return wrapCellText(row, index).count
    }

    // The delimiter of the line: the commonest of a comma, a pipe and a tab.
    // The ones inside a quoted field are not counted, or a file whose values
    // hold commas could be split on the wrong character altogether.
    private func getDelimiter(_ str: String) -> String {
        var comma = 0
        var pipe = 0
        var tab = 0
        var quoted = false
        for scalar in str.unicodeScalars {
            if scalar == "\"" {
                quoted = !quoted
            } else if quoted {
                continue
            } else if scalar == "," {
                comma += 1
            } else if scalar == "|" {
                pipe += 1
            } else if scalar == "\t" {
                tab += 1
            }
        }
        if comma >= pipe {
            if comma >= tab {
                return ","
            }
            return "\t"
        } else {
            if pipe >= tab {
                return "|"
            }
            return "\t"
        }
    }

    /// Keeps only the columns with the specified indexes.
    @discardableResult
    public func setVisibleColumns(_ visible: Int...) -> Table {
        var list = [[Cell]]()
        for row in tableData {
            var row2 = [Cell]()
            var i = 0
            while i < row.count {
                if visible.contains(i) {
                    row2.append(row[i])
                }
                i += 1
            }
            list.append(row2)
        }
        tableData = list
        return self
    }

    /// Sets the top margin on the first page when the table spans several pages.
    @discardableResult
    public func setFirstPageTopMargin(_ firstPageTopMargin: Float) -> Table {
        self.firstPageTopMargin = firstPageTopMargin
        return self
    }
}   // End of Table.swift
