/**
 *  Table.swift
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
import Foundation

///
/// Used to create table objects and draw them on a page.
///
/// Please see Example_08.
///
public class Table {
    public static let WITH_0_HEADER_ROWS = 0
    public static let WITH_1_HEADER_ROW  = 1
    public static let WITH_2_HEADER_ROWS = 2
    public static let WITH_3_HEADER_ROWS = 3
    public static let WITH_4_HEADER_ROWS = 4
    public static let WITH_5_HEADER_ROWS = 5
    public static let WITH_6_HEADER_ROWS = 6
    public static let WITH_7_HEADER_ROWS = 7
    public static let WITH_8_HEADER_ROWS = 8
    public static let WITH_9_HEADER_ROWS = 9

    private var tableData: [[Cell]]
    private var numOfHeaderRows = 1
    private var rendered = 0
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var x1FirstPage: Float = 0.0
    private var y1FirstPage: Float = 0.0
    private var bottomMargin: Float = 0.0

    ///
    /// Create a table object.
    ///
    public init() {
        tableData = [[Cell]]()
    }

    ///
    /// Create a table object.
    ///
    public init(_ f1: Font, _ f2: Font, _ fileName: String) throws {
        tableData = [[Cell]]()
        var delimiterRegex: String?
        var numberOfFields = 0
        var lineNumber = 0
        let lines = (try String(contentsOfFile:
                fileName, encoding: .utf8)).components(separatedBy: "\n")
        for line in lines {
            if lineNumber == 0 {
                delimiterRegex = getDelimiterRegex(line)
                numberOfFields = line.components(separatedBy: delimiterRegex!).count
            }
            var row = [Cell]()
            let fields = line.components(separatedBy: delimiterRegex!)
            for field in fields {
                if lineNumber == 0 {
                    let cell = Cell(f1)
                    cell.setTextBox(TextBox(f1, field))
                    row.append(cell)
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
                    row.append(Cell(f2))
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
    /// @param x the x coordinate of the top left point of the table.
    /// @param y the y coordinate of the top left point of the table.
    ///
    public func setLocation(_ x: Float, _ y: Float) {
        self.x1 = x
        self.y1 = y
    }

    ///
    /// Sets the bottom margin for this table.
    ///
    /// @param bottomMargin the margin.
    ///
    public func setBottomMargin(_ bottomMargin: Float) {
        self.bottomMargin = bottomMargin
    }

    ///
    /// Sets the table data.
    ///
    /// The table data is a perfect grid of cells.
    /// All cell should be an unique object and you can not reuse blank cell objects.
    /// Even if one or more cells have colspan bigger than zero the number of cells in the row will not change.
    ///
    /// @param tableData the table data.
    ///
    public func setData(_ tableData: [[Cell]]) {
        self.tableData = tableData
        self.numOfHeaderRows = 0
        self.rendered = self.numOfHeaderRows

        // Add the missing cells.
        let numOfColumns = tableData[0].count
        let font = tableData[0][0].font
        for i in 0..<tableData.count {
            var row = tableData[i]
            let diff = numOfColumns - row.count
            for _ in 0..<diff {
                row.append(Cell(font, ""))
            }
        }
    }

    ///
    /// Sets the table data and specifies the number of header rows in this data.
    ///
    /// @param tableData the table data.
    /// @param numOfHeaderRows the number of header rows in this data.
    ///
    public func setData(_ tableData: [[Cell]], _ numOfHeaderRows: Int) {
        self.tableData = tableData
        self.numOfHeaderRows = numOfHeaderRows
        self.rendered = numOfHeaderRows

        // Add the missing cells.
        let numOfColumns = tableData[0].count
        let font = tableData[0][0].font
        for i in 0..<tableData.count {
            var row = tableData[i]
            let diff = numOfColumns - row.count
            for _ in 0..<diff {
                row.append(Cell(font, ""))
            }
        }
    }

    ///
    /// Sets the alignment of the numbers to the right.
    ///
    public func rightAlignNumbers() {
        var buf = String()
        for row in tableData {
            for cell in row {
                if cell.text != nil {
                    buf = ""
                    let scalars = [UnicodeScalar](cell.text!.unicodeScalars)
                    var index1 = 0
                    var index2 = scalars.count
                    if scalars.count > 2 && scalars[0] == "(" && scalars[scalars.count-1] == ")" {
                        index1 = 1
                        index2 = scalars.count - 1
                    }
                    for i in index1..<index2 {
                        let scalar = scalars[i]
                        if scalar != "." && scalar != "," && scalar != "'" {
                            buf.append(String(scalar));
                        }
                    }
                    let value = Double(buf)
                    if value != nil {
                        cell.setTextAlignment(Align.RIGHT)
                    }
                }
            }
        }
    }

    ///
    /// Removes the horizontal lines between the rows from index1 to index2.
    ///
    public func removeLineBetweenRows(_ index1: Int, _ index2: Int) {
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
    }

    ///
    /// Sets the text alignment in the specified column.
    /// Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.
    ///
    /// @param index the index of the specified column.
    /// @param alignment the specified alignment.
    ///
    public func setTextAlignInColumn(_ index: Int, _ alignment: UInt32) throws {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setTextAlignment(alignment)
                if cell.textBox != nil {
                    cell.textBox!.setTextAlignment(alignment)
                }
            }
        }
    }

    ///
    /// Sets the color of the text in the specified column.
    ///
    /// @param index the index of the specified column.
    /// @param color the color specified as an integer.
    ///
    public func setTextColorInColumn(_ index: Int, _ color: Int32) {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setBrushColor(color)
                if cell.textBox != nil {
                    cell.textBox!.setBrushColor(color)
                }
            }
        }
    }

    ///
    /// Sets the font for the specified column.
    ///
    /// @param index the column index.
    /// @param font the font.
    ///
    public func setFontInColumn(_ index: Int, _ font: Font) {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.font = font
                if cell.textBox != nil {
                    cell.textBox!.font = font
                }
            }
        }
    }

    ///
    /// Sets the color of the text in the specified row.
    ///
    /// @param index the index of the specified row.
    /// @param color the color specified as an integer.
    ///
    public func setTextColorInRow(_ index: Int, _ color: Int32) {
        if index < tableData.count {
            let row = tableData[index]
            for cell in row {
                cell.setBrushColor(color)
                if cell.textBox != nil {
                    cell.textBox!.setBrushColor(color)
                }
            }
        }
    }

    ///
    /// Sets the font for the specified row.
    ///
    /// @param index the row index.
    /// @param font the font.
    ///
    public func setFontInRow(_ index: Int, _ font: Font) {
        if index < tableData.count {
            let row = tableData[index]
            for cell in row {
                cell.font = font
                if cell.textBox != nil {
                    cell.textBox!.font = font
                }
            }
        }
    }

    ///
    /// Sets the width of the column with the specified index.
    ///
    /// @param index the index of specified column.
    /// @param width the specified width.
    ///
    public func setColumnWidth(_ index: Int, _ width: Float) {
        for row in tableData {
            if index < row.count {
                let cell = row[index]
                cell.setWidth(width)
                if cell.textBox != nil {
                    cell.textBox!.setWidth(width - (cell.leftPadding + cell.rightPadding))
                }
            }
        }
    }

    ///
    /// Returns the column width of the column at the specified index.
    ///
    /// @param index the index of the column.
    /// @return the width of the column.
    ///
    public func getColumnWidth(_ index: Int) -> Float {
        return getCellAtRowColumn(0, index).getWidth()
    }

    ///
    /// Returns the cell at the specified row and column.
    ///
    /// @param row the specified row.
    /// @param col the specified column.
    ///
    /// @return the cell at the specified row and column.
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
    /// @param row the specified row.
    /// @param col the specified column.
    ///
    /// @return the cell at the specified row and column.
    ///
    public func getCellAtRowColumn(_ row: Int, _ col: Int) -> Cell {
        return getCellAt(row, col)
    }

    ///
    /// Returns a list of cell for the specified row.
    ///
    /// @param index the index of the specified row.
    ///
    /// @return the list of cells.
    ///
    public func getRow(_ index: Int) -> [Cell] {
        return tableData[index]
    }

    public func getRowAtIndex(_ index: Int) -> [Cell] {
        return getRow(index)
    }

    ///
    /// Returns a list of cell for the specified column.
    ///
    /// @param index the index of the specified column.
    ///
    /// @return the list of cells.
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

    public func getColumnAtIndex(_ index: Int) -> [Cell] {
        return getColumn(index)
    }

    ///
    /// Draws this table on the specified page.
    ///
    /// @param page the page to draw this table on.
    /// @param draw if false - do not draw the table. Use to only find out where the table ends.
    ///
    /// @return Point the point on the page where to draw the next component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        wrapAroundCellText()
        setRightBorderOnLastColumn()
        setBottomBorderOnLastRow()
        return drawTableRows(page, drawHeaderRows(page, 0))
    }

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
        if pageNumber == 1 && y1FirstPage > 0.0 {
            x = x1FirstPage
            y = y1FirstPage
        }
        for i in 0..<numOfHeaderRows {
            let row = tableData[i]
            let h = getMaxCellHeight(row)
            var j = 0
            while j < row.count {
                let cell = row[j]
                var w = row[j].getWidth()
                let colspan = row[j].getColSpan()
                for _ in 1..<colspan {
                    j += 1
                    w += row[j].width
                }
                if page != nil {
                    page!.setBrushColor(cell.getBrushColor())
                    if i == (numOfHeaderRows - 1) {
                        cell.setBorder(Border.BOTTOM, true)
                    }
                    cell.drawOn(page!, x, y, w, h)
                }
                x += w
                j += 1
            }
            x = x1
            y += h
            rendered += 1
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
                var w = cell.getWidth()
                let colspan = cell.getColSpan()
                for _ in 1..<colspan {
                    i += 1
                    w += row[i].getWidth()
                }
                if page != nil {
                    page!.setBrushColor(cell.getBrushColor())
                    cell.drawOn(page!, x, y, w, h)
                }
                x += w
                i += 1
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
        for i in  0..<row.count {
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
    /// @return the widht of this table.
    ///
    public func getWidth() -> Float {
        var table_width: Float = 0.0
        if tableData.count > 0 {
            let row = tableData[0]
            for cell in row {
                table_width += cell.getWidth()
            }
        }
        return table_width
    }

    ///
    /// Returns the number of data rows that have been rendered so far.
    ///
    /// @return the number of data rows that have been rendered so far.
    ///
    public func getRowsRendered() -> Int {
        return rendered == -1 ? rendered : rendered - numOfHeaderRows
    }

    ///
    /// Sets all table cells borders to <strong>false</strong> or <strong>true</strong>.
    ///
    public func setCellBorders(_ borders: Bool) {
        for row in tableData {
            for cell in row {
                cell.setBorders(borders)
            }
        }
    }

    ///
    /// Sets the color of the cell border lines.
    ///
    /// @param color the color of the cell border lines.
    ///
    public func setCellBordersColor(_ color: Int32) {
        for row in tableData {
            for cell in row {
                cell.setPenColor(color)
            }
        }
    }

    ///
    /// Sets the width of the cell border lines.
    ///
    /// @param width the width of the border lines.
    ///
    public func setCellBordersWidth(_ width: Float) {
        for row in tableData {
            for cell in row {
                cell.setLineWidth(width)
            }
        }
    }

    // Sets the right border on all cells in the last column.
    private func setRightBorderOnLastColumn() {
        for row in tableData {
            var cell: Cell?
            var i = 0
            while i < row.count {
                cell = row[i]
                i += Int(cell!.getColSpan())
            }
            cell!.setBorder(Border.RIGHT, true)
        }
    }

    // Sets the bottom border on all cells in the last row.
    private func setBottomBorderOnLastRow() {
        let lastRow = tableData[tableData.count - 1]
        for cell in lastRow {
            cell.setBorder(Border.BOTTOM, true)
        }
    }

    //
    // Auto adjusts the widths of all columns so that they are just wide enough to
    // hold the text without truncation.
    //
    public func setColumnWidths() {
        var maxColWidths = [Float](repeating: 0.0, count: tableData[0].count)
        for row in tableData {
            var i = 0
            while i < row.count {
                let cell = row[i]
                if cell.getColSpan() == 1 {
                    if cell.textBox != nil {
                        let tokens = cell.textBox!.text!.components(separatedBy: .whitespaces)
                        for token in tokens {
                            var tokenWidth = cell.textBox!.font!.stringWidth(cell.textBox!.fallbackFont, token)
                            tokenWidth += cell.leftPadding + cell.rightPadding
                            if (tokenWidth > maxColWidths[i]) {
                                maxColWidths[i] = tokenWidth
                            }
                        }
                    } else if cell.image != nil {
                        let imageWidth = cell.image!.getWidth() + cell.leftPadding + cell.rightPadding
                        if (imageWidth > maxColWidths[i]) {
                            maxColWidths[i] = imageWidth
                        }
                    } else if cell.barcode != nil {
                        let barcodeWidth = cell.barcode!.drawOn(nil)[0] + cell.leftPadding + cell.rightPadding
                        if (barcodeWidth > maxColWidths[i]) {
                            maxColWidths[i] = barcodeWidth
                        }
                    } else if cell.text != nil {
                        var textWidth = cell.font!.stringWidth(cell.fallbackFont, cell.text)
                        textWidth += cell.leftPadding + cell.rightPadding
                        if (textWidth > maxColWidths[i]) {
                            maxColWidths[i] = textWidth
                        }
                    }
                }
                i += 1
            }
        }
        for row in tableData {
            for i in 0..<row.count {
                row[i].setWidth(maxColWidths[i])
            }
        }
    }

    private func addExtraTableRows() -> [[Cell]] {
        var tableData2 = [[Cell]]()
        for row in tableData {
            tableData2.append(row)  // Add the original row
            var maxNumVerCells = 0
            for i in 0..<row.count {
                let numVerCells = getNumVerCells(row, i)
                if numVerCells > maxNumVerCells {
                    maxNumVerCells = numVerCells
                }
            }
            for _ in 1..<maxNumVerCells {
                var row2 = [Cell]()
                for cell in row {
                    let cell2 = Cell(cell.getFont())
                    cell2.setFallbackFont(cell.getFallbackFont())
                    cell2.setWidth(cell.getWidth())
                    cell2.setLeftPadding(cell.leftPadding)
                    cell2.setRightPadding(cell.rightPadding)
                    cell2.setLineWidth(cell.lineWidth)
                    cell2.setBgColor(cell.getBgColor())
                    cell2.setPenColor(cell.getPenColor())
                    cell2.setBrushColor(cell.getBrushColor())
                    cell2.setProperties(cell.getProperties())
                    cell2.setVerTextAlignment(cell.getVerTextAlignment())
                    cell2.setTopPadding(0.0)
                    cell2.setBorder(Border.TOP, false)
                    row2.append(cell2)
                }
                tableData2.append(row2)
            }
        }
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
    public func wrapAroundCellText() {
        let tableData2 = addExtraTableRows()
        for (i, row) in tableData2.enumerated() {
            for (j, cell) in row.enumerated() {
                if cell.text != nil {
                    let cellWidth = getTotalWidth(row, j)
                    let tokens = cell.text!.components(separatedBy: .whitespaces)
                    var n = 0
                    var buf = String()
                    for token in tokens {
                        if cell.font!.stringWidth(cell.fallbackFont, token) > cellWidth {
                            if buf.count > 0 {
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
                                if buf.count > 0 {
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
    /// @return the number of vertical cells needed to wrap around the cell text.
    ///
    public func getNumVerCells(_ row: [Cell], _ index: Int) -> Int {
        let cell = row[index]
        var numOfVerCells = 1
        if cell.text == nil {
            return numOfVerCells
        }
        let cellWidth = getTotalWidth(row, index)
        let tokens = cell.text!.components(separatedBy: .whitespaces)
        var buf = String()
        for token in tokens {
            if cell.font!.stringWidth(cell.fallbackFont, token) > cellWidth {
                if buf.count > 0 {
                    buf.append(" ")
                }
                for scalar in token.unicodeScalars {
                    if cell.font!.stringWidth(cell.fallbackFont, (buf + " " + String(scalar)).trim()) > cellWidth {
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
                    if buf.count > 0 {
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

    public func setVisibleColumns(_ visible: Int...) {
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
    }

    public func setLocationFirstPage(_ x: Float, _ y: Float) {
        self.x1FirstPage = x;
        self.y1FirstPage = y;
    }
}   // End of Table.swift
