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
    /// The table has no header rows.
    public static let WITH_0_HEADER_ROWS = 0
    /// The table has 1 header row.
    public static let WITH_1_HEADER_ROW  = 1
    /// The table has 2 header rows.
    public static let WITH_2_HEADER_ROWS = 2
    /// The table has 3 header rows.
    public static let WITH_3_HEADER_ROWS = 3
    /// The table has 4 header rows.
    public static let WITH_4_HEADER_ROWS = 4
    /// The table has 5 header rows.
    public static let WITH_5_HEADER_ROWS = 5
    /// The table has 6 header rows.
    public static let WITH_6_HEADER_ROWS = 6
    /// The table has 7 header rows.
    public static let WITH_7_HEADER_ROWS = 7
    /// The table has 8 header rows.
    public static let WITH_8_HEADER_ROWS = 8
    /// The table has 9 header rows.
    public static let WITH_9_HEADER_ROWS = 9

    private var tableData: [[Cell]]
    private var numOfHeaderRows = 1
    // The index of the next row to draw, or -1 when all rows are drawn.
    private var rendered = 1
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var firstPageTopMargin: Float = 0.0
    private var bottomMargin: Float = 0.0

    ///
    /// Create a table object.
    ///
    public init() {
        tableData = [[Cell]]()
    }

    ///
    /// Creates a table that uses f1 for the header rows and f2 for the other rows.
    ///
    public init(_ f1: Font, _ f2: Font) {
        tableData = [[Cell]]()
    }

    ///
    /// Creates a table from a text file with comma, pipe or tab separated values.
    /// The first line is the header row and uses f1; the other lines use f2.
    /// Every row gets as many cells as the first line has fields.
    ///
    /// - Parameter f1: the font for the header row.
    /// - Parameter f2: the font for the other rows.
    /// - Parameter fileName: the file name.
    ///
    public init(_ f1: Font, _ f2: Font, _ fileName: String) throws {
        tableData = [[Cell]]()
        var delimiterRegex: String?
        var numberOfFields = 0
        var lineNumber = 0
        // Swift treats "\r\n" as one character, which a "\n" separator does not
        // match, so Windows line endings are replaced first.
        var lines = (try String(contentsOfFile: fileName, encoding: .utf8))
                .replacingOccurrences(of: "\r\n", with: "\n")
                .components(separatedBy: "\n")
        if lines.last == "" {
            lines.removeLast()          // Ignore the trailing end-of-line marker
        }
        for line in lines {
            if lineNumber == 0 {
                delimiterRegex = getDelimiterRegex(line)
                numberOfFields = line.components(separatedBy: delimiterRegex!).count
            }
            var row = [Cell]()
            let fields = line.components(separatedBy: delimiterRegex!)
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
    public func setData(_ tableData: [[Cell]]) -> Table {
        return setData(tableData, 0)
    }

    ///
    /// Sets the table data and specifies the number of header rows in this data.
    /// The header rows are drawn again at the top of every page.
    ///
    /// - Parameter tableData: the table data.
    /// - Parameter numOfHeaderRows: the number of header rows in this data.
    ///
    @discardableResult
    public func setData(_ tableData: [[Cell]], _ numOfHeaderRows: Int) -> Table {
        self.tableData = tableData
        self.numOfHeaderRows = numOfHeaderRows
        self.rendered = numOfHeaderRows
        addCellsToCompleteTheGrid()
        return self
    }

    // Adds empty cells to the rows that are shorter than the first row.
    private func addCellsToCompleteTheGrid() {
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
    public func rightAlignNumbers() {
        for row in tableData {
            for cell in row {
                if cell.text != nil && Table.isNumber(cell.text!) {
                    cell.setTextAlignment(Align.RIGHT)
                }
            }
        }
    }

    // Returns true if the text, without its periods, commas and apostrophes and
    // the parentheses around it, is an optional sign, ASCII digits and an
    // optional exponent, with optional spaces before and after.
    private static func isNumber(_ text: String) -> Bool {
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
    /// Removes the horizontal lines between the rows from index1 to index2.
    ///
    public func removeLineBetweenRows(_ index1: Int, _ index2: Int) {
        var j = index1
        while j < index2 {
            var row = tableData[j]
            for cell in row {
                cell.setBottomBorder(false)
            }
            row = tableData[j + 1]
            for cell in row {
                cell.setTopBorder(false)
            }
            j += 1
        }
    }

    ///
    /// Sets the text alignment in the specified column.
    /// Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.
    ///
    /// - Parameter index: the index of the specified column.
    /// - Parameter alignment: the specified alignment.
    ///
    @discardableResult
    public func setTextAlignInColumn(_ index: Int, _ alignment: UInt32) -> Table {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setTextAlignment(alignment)
                if cell.textBox != nil {
                    cell.textBox!.setTextAlignment(alignment)
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
                if cell.textBox != nil {
                    cell.textBox!.setTextColor(color)
                }
            }
        }
        return self
    }

    ///
    /// Sets the font for the specified column.
    ///
    /// - Parameter index: the column index.
    /// - Parameter font: the font.
    ///
    @discardableResult
    public func setFontInColumn(_ index: Int, _ font: Font) -> Table {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.font = font
                if cell.textBox != nil {
                    cell.textBox!.font = font
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
                if cell.textBox != nil {
                    cell.textBox!.setTextColor(color)
                }
            }
        }
        return self
    }

    ///
    /// Sets the font for the specified row.
    ///
    /// - Parameter index: the row index.
    /// - Parameter font: the font.
    ///
    @discardableResult
    public func setFontInRow(_ index: Int, _ font: Font) -> Table {
        if index < tableData.count {
            let row = tableData[index]
            for cell in row {
                cell.font = font
                if cell.textBox != nil {
                    cell.textBox!.font = font
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
        return getCellAtRowColumn(0, index).getWidth()
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
    /// Returns the cell at the specified row and column.
    ///
    /// - Parameter row: the specified row.
    /// - Parameter col: the specified column.
    ///
    /// - Returns: the cell at the specified row and column.
    ///
    public func getCellAtRowColumn(_ row: Int, _ col: Int) -> Cell {
        return getCellAt(row, col)
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

    /// Returns the cells in the specified row. Same as getRow.
    public func getRowAtIndex(_ index: Int) -> [Cell] {
        return getRow(index)
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

    /// Returns the cells in the specified column. Same as getColumn.
    public func getColumnAtIndex(_ index: Int) -> [Cell] {
        return getColumn(index)
    }

    ///
    /// Draws this table on the specified page.
    ///
    /// - Parameter page: the page to draw this table on.
    ///
    /// - Returns: Point the point on the page where to draw the next component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        wrapAroundCellText()
        setRightBorderOnLastColumn()
        setBottomBorderOnLastRow()
        return drawTableRows(page, drawHeaderRows(page, 0))
    }

    ///
    /// Draws this table on as many new pages as it needs.
    /// The pages are created detached and added to the list; add them to the PDF afterwards.
    ///
    /// - Parameter pdf: the PDF document.
    /// - Parameter pages: the list that receives the new pages.
    /// - Parameter pageSize: the page size, for example Letter.PORTRAIT.
    /// - Returns: the x and y coordinates below the table on the last page.
    ///
    @discardableResult
    public func drawOn(_ pdf: PDF, _ pages: inout [Page], _ pageSize: [Float]) -> [Float] {
        wrapAroundCellText()
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
        return xy!
    }

    private func drawHeaderRows(_ page: Page?, _ pageNumber: Int) -> [Float] {
        var x = x1
        var y = y1
        if pageNumber == 1 && firstPageTopMargin > 0.0 {
            y = firstPageTopMargin
        }
        for i in 0..<numOfHeaderRows {
            let row = tableData[i]
            let h = getMaxCellHeight(row)
            var j = 0
            while j < row.count {
                let cell = row[j]
                let colspan = cell.getColSpan()
                var w: Float = 0.0
                for _ in 0..<colspan {
                    w += row[j].getWidth()
                    j += 1
                }
                if page != nil {
                    page!.setBrushColor(cell.getTextColor())
                    if i == (numOfHeaderRows - 1) {
                        cell.setBottomBorder(true)
                    }
                    cell.drawOn(page!, x, y, w, h)
                }
                x += w
            }
            x = x1
            y += h
        }
        return [x, y]
    }

    private func drawTableRows(_ page: Page?, _ xy: [Float]) -> [Float] {
        var x = xy[0]
        var y = xy[1]
        while rendered < tableData.count {
            let row = tableData[rendered]
            let h = getMaxCellHeight(row)
            if page != nil && (y + h) > (page!.height - bottomMargin) {
                return [x, y]
            }
            var i = 0
            while i < row.count {
                let cell = row[i]
                let colspan = cell.getColSpan()
                var w: Float = 0.0
                for _ in 0..<colspan {
                    w += row[i].getWidth()
                    i += 1
                }
                if page != nil {
                    page!.setBrushColor(cell.getTextColor())
                    cell.drawOn(page!, x, y, w, h)
                }
                x += w
            }
            x = x1
            y += h
            rendered += 1
        }
        rendered = -1   // We are done!
        return [x, y]
    }

    private func getMaxCellHeight(_ row: [Cell]) -> Float {
        var maxCellHeight: Float = 0.0
        for i in 0..<row.count {
            let cell = row[i]
            let totalWidth = getTotalWidth(row, i)
            let cellHeight = cell.getHeight(totalWidth)
            if cellHeight > maxCellHeight {
                maxCellHeight = cellHeight
            }
        }
        return maxCellHeight
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
    public func setCellBordersColor(_ color: Int32) -> Table {
        for row in tableData {
            for cell in row {
                cell.setStrokeColor(color)
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
    public func setCellBordersWidth(_ width: Float) -> Table {
        for row in tableData {
            for cell in row {
                cell.setLineWidth(width)
            }
        }
        return self
    }

    // Sets the right border on all cells in the last column.
    private func setRightBorderOnLastColumn() {
        for row in tableData {
            if row[0].getLeftBorder() == false {
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
            cell!.setRightBorder(true)
        }
    }

    // Sets the bottom border on all cells in the last row.
    private func setBottomBorderOnLastRow() {
        let firstRow = tableData[0]
        for cell in firstRow {
            if cell.getTopBorder() == false {
                return
            }
        }
        // Only run this code if all the cells in the first row have top border.
        let lastRow = tableData[tableData.count - 1]
        for cell in lastRow {
            cell.setBottomBorder(true)
        }
    }

    ///
    /// Auto adjusts the widths of all columns so that they are just wide enough to
    /// hold the text without truncation.
    ///
    @discardableResult
    public func setColumnWidths() -> Table {
        var maxColWidths = [Float](repeating: 0.0, count: tableData[0].count)
        for row in tableData {
            for i in 0..<row.count {
                let cell = row[i]
                if cell.getColSpan() == 1 {
                    if cell.textBox != nil {
                        let tokens = (cell.textBox!.text ?? "").splitOnWhitespace()
                        for token in tokens {
                            var tokenWidth = cell.textBox!.font.stringWidth(cell.textBox!.fallbackFont, token)
                            tokenWidth += cell.leftPadding + cell.rightPadding
                            if tokenWidth > maxColWidths[i] {
                                maxColWidths[i] = tokenWidth
                            }
                        }
                    } else if cell.image != nil {
                        let imageWidth = cell.image!.getWidth() + cell.leftPadding + cell.rightPadding
                        if imageWidth > maxColWidths[i] {
                            maxColWidths[i] = imageWidth
                        }
                    } else if cell.barcode != nil {
                        let barcodeWidth = cell.barcode!.drawOn(nil)[0] + cell.leftPadding + cell.rightPadding
                        if barcodeWidth > maxColWidths[i] {
                            maxColWidths[i] = barcodeWidth
                        }
                    } else if cell.text != nil {
                        var textWidth = cell.font!.stringWidth(cell.fallbackFont, cell.text)
                        textWidth += cell.leftPadding + cell.rightPadding
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

    // Returns the table data with a row added below each row for every extra
    // line that its wrapped cell text needs. The rows added below a header row
    // are header rows too.
    private func addExtraTableRows() -> [[Cell]] {
        var tableData2 = [[Cell]]()
        var numOfHeaderRows2 = 0
        for r in 0..<tableData.count {
            let row = tableData[r]
            tableData2.append(row)  // Add the original row
            var maxNumVerCells = 0
            for i in 0..<row.count {
                let numVerCells = getNumVerCells(row, i)
                if numVerCells > maxNumVerCells {
                    maxNumVerCells = numVerCells
                }
            }
            var k = 1
            while k < maxNumVerCells {
                var row2 = [Cell]()
                for cell in row {
                    let cell2 = Cell(cell.getFont())
                    cell2.setFallbackFont(cell.getFallbackFont())
                    cell2.setWidth(cell.getWidth())
                    cell2.setLeftPadding(cell.getLeftPadding())
                    cell2.setRightPadding(cell.getRightPadding())
                    cell2.setLineWidth(cell.getLineWidth())
                    // Java copies a null background across as null, which
                    // leaves the new cell without one.
                    if cell.hasBackground {
                        cell2.setBackgroundColor(cell.getBackgroundColor())
                    }
                    cell2.setStrokeWidth(cell.getStrokeWidth())
                    cell2.setStrokeColor(cell.getStrokeColor())
                    cell2.setTextColor(cell.getTextColor())
                    // The column span and text alignment.
                    cell2.setProperties(cell.getProperties())
                    cell2.setTopBorder(cell.getTopBorder())
                    cell2.setBottomBorder(cell.getBottomBorder())
                    cell2.setLeftBorder(cell.getLeftBorder())
                    cell2.setRightBorder(cell.getRightBorder())
                    cell2.setUnderline(cell.getUnderline())
                    cell2.setStrikeout(cell.getStrikeout())
                    cell2.setVerTextAlignment(cell.getVerTextAlignment())
                    cell2.setTopPadding(0.0)
                    cell2.setTopBorder(false)
                    row2.append(cell2)
                }
                tableData2.append(row2)
                k += 1
            }
            if r < numOfHeaderRows {
                numOfHeaderRows2 = tableData2.count
            }
        }
        if rendered != -1 {
            rendered += numOfHeaderRows2 - numOfHeaderRows
        }
        numOfHeaderRows = numOfHeaderRows2
        return tableData2
    }

    func getTotalWidth(_ row: [Cell], _ index: Int) -> Float {
        let cell = row[index]
        let colspan = Int(cell.getColSpan())
        var cellWidth = Float(0.0)
        for i in 0..<colspan {
            cellWidth += row[index + i].getWidth()
        }
        cellWidth -= (cell.leftPadding + row[index + (colspan - 1)].rightPadding)
        return cellWidth
    }

    ///
    /// Wraps around the text in all cells so it fits the column width.
    /// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
    ///
    func wrapAroundCellText() {
        let tableData2 = addExtraTableRows()
        for (i, row) in tableData2.enumerated() {
            for (j, cell) in row.enumerated() {
                if cell.text != nil {
                    let cellWidth = getTotalWidth(row, j)
                    let tokens = cell.text!.splitOnWhitespace()
                    var n = 0
                    var buf = String()
                    for token in tokens {
                        if cell.font!.stringWidth(cell.fallbackFont, token) > cellWidth {
                            if !buf.isEmpty {
                                buf.append(" ")
                            }
                            for scalar in token.unicodeScalars {
                                if cell.font!.stringWidth(cell.fallbackFont, buf + String(scalar)) > cellWidth {
                                    tableData2[i + n][j].setText(buf)
                                    buf = ""
                                    n += 1
                                }
                                buf.append(String(scalar))
                            }
                        } else {
                            if cell.font!.stringWidth(cell.fallbackFont, (buf + " " + token).trim()) > cellWidth {
                                tableData2[i + n][j].setText(buf.trim())
                                buf = ""
                                buf.append(token)
                                n += 1
                            } else {
                                if !buf.isEmpty {
                                    buf.append(" ")
                                }
                                buf.append(token)
                            }
                        }
                    }
                    tableData2[i + n][j].setText(buf.trim())
                }
            }
        }
        tableData = tableData2
    }

    ///
    /// Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
    ///
    /// - Returns: the number of vertical cells needed to wrap around the cell text.
    ///
    func getNumVerCells(_ row: [Cell], _ index: Int) -> Int {
        let cell = row[index]
        var numOfVerCells = 1
        if cell.text == nil {
            return numOfVerCells
        }
        let cellWidth = getTotalWidth(row, index)
        let tokens = cell.text!.splitOnWhitespace()
        var buf = String()
        for token in tokens {
            if cell.font!.stringWidth(cell.fallbackFont, token) > cellWidth {
                if !buf.isEmpty {
                    buf.append(" ")
                }
                for scalar in token.unicodeScalars {
                    if cell.font!.stringWidth(cell.fallbackFont, buf + String(scalar)) > cellWidth {
                        numOfVerCells += 1
                        buf = ""
                    }
                    buf.append(String(scalar))
                }
            } else {
                if cell.font!.stringWidth(cell.fallbackFont, (buf + " " + token).trim()) > cellWidth {
                    numOfVerCells += 1
                    buf = ""
                    buf.append(token)
                } else {
                    if !buf.isEmpty {
                        buf.append(" ")
                    }
                    buf.append(token)
                }
            }
        }
        return numOfVerCells
    }

    private func getDelimiterRegex(_ str: String) -> String {
        var comma = 0
        var pipe = 0
        var tab = 0
        for scalar in str.unicodeScalars {
            if scalar == "," {
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
