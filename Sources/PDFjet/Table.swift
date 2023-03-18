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

    public static let DATA_HAS_0_HEADER_ROWS = 0
    public static let DATA_HAS_1_HEADER_ROWS = 1
    public static let DATA_HAS_2_HEADER_ROWS = 2
    public static let DATA_HAS_3_HEADER_ROWS = 3
    public static let DATA_HAS_4_HEADER_ROWS = 4
    public static let DATA_HAS_5_HEADER_ROWS = 5
    public static let DATA_HAS_6_HEADER_ROWS = 6
    public static let DATA_HAS_7_HEADER_ROWS = 7
    public static let DATA_HAS_8_HEADER_ROWS = 8
    public static let DATA_HAS_9_HEADER_ROWS = 9

    private var rendered = 0
    private var numOfPages = 0
    private var tableData: [[Cell]]
    private var numOfHeaderRows = 0

    private var x1: Float?
    private var y1: Float?
    private var y1FirstPage: Float = 0.0
    private var rightMargin: Float = 0.0
    private var bottomMargin: Float = 30.0
    private var numOfHeaderRowsAdjusted: Bool = false


    ///
    /// Create a table object.
    ///
    public init() {
        tableData = [[Cell]]()
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
    /// Auto adjusts the widths of all columns so that they are just wide enough to hold the text without truncation.
    ///
/*
    public func autoAdjustColumnWidths() {
        // Find the maximum text width for each column
        var maxColWidths = [Float](repeating: 0, count: (tableData[0].count))
        for row in tableData {
            for (i, cell) in row.enumerated() {
                if cell.getColSpan() == 1 {
                    var cellWidth: Float = 0.0
                    if cell.getImage() != nil {
                        cellWidth = cell.getImage()!.getWidth()!
                    }
                    if cell.text != nil {
                        if cell.font!.stringWidth(cell.fallbackFont, cell.text) > cellWidth {
                            cellWidth = cell.font!.stringWidth(cell.fallbackFont, cell.text)
                        }
                    }
                    cell.setWidth(cellWidth + cell.leftPadding + cell.rightPadding)
                    if maxColWidths[i] == 0.0 ||
                            cell.getWidth() > maxColWidths[i] {
                        maxColWidths[i] = cell.getWidth()
                    }
                }
            }
        }

        for row in tableData {
            for (i, cell) in row.enumerated() {
                cell.setWidth(maxColWidths[i])
            }
        }
    }
*/

    ///
    /// Sets the alignment of the numbers to the right.
    ///
    public func rightAlignNumbers() {
        let digitsPlus = [UnicodeScalar]("0123456789()-.,'".unicodeScalars)
        var i = numOfHeaderRows
        while i < tableData.count {
            let row = tableData[i]
            for cell in row {
                if cell.text != nil {
                    let scalars = [UnicodeScalar](cell.text!.unicodeScalars)
                    var isNumber = true
                    for scalar in scalars {
                        if digitsPlus.firstIndex(of: scalar) == nil {
                            isNumber = false
                            break
                        }
                    }
                    if isNumber {
                        cell.setTextAlignment(Align.RIGHT)
                    }
                }
            }
            i += 1
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
                if cell.textBlock != nil {
                    cell.textBlock!.setTextAlignment(alignment)
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
                if cell.textBlock != nil {
                    cell.textBlock!.setBrushColor(color)
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
                if cell.textBlock != nil {
                    cell.textBlock!.font = font
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
                if cell.textBlock != nil {
                    cell.textBlock!.setBrushColor(color)
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
                if cell.textBlock != nil {
                    cell.textBlock!.font = font
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
                if cell.textBlock != nil {
                    cell.textBlock!.setWidth(width - (cell.leftPadding + cell.rightPadding))
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
        return getCellAtRowColumn(0, index).getWidth();
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
    /// Returns the total number of pages that are required to draw this table on.
    ///
    /// @param page the type of pages we are drawing this table on.
    ///
    /// @return the number of pages.
    ///
    @discardableResult
    public func getNumberOfPages(_ page: Page) throws -> Int {
        self.numOfPages = 1
        while hasMoreData() {
            drawOn(nil)
        }
        resetRenderedPagesCount()
        return self.numOfPages
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
        return drawTableRows(page, drawHeaderRows(page, 0))
    }


    @discardableResult
    public func drawOn(_ pdf: PDF, _ pages: inout [Page], _ pageSize: [Float]) -> [Float] {
        var xy: [Float]?
        var pageNumber: Int = 1
        while (self.hasMoreData()) {
            let page = Page(pdf, pageSize, false)
            pages.append(page)
            xy = drawTableRows(page, drawHeaderRows(page, pageNumber))
            pageNumber += 1
        }
        // Allow the table to be drawn again later:
        resetRenderedPagesCount()
        return xy!
    }


    private func drawHeaderRows(_ page: Page?, _ pageNumber: Int) -> [Float] {
        var x = x1
        var y = (pageNumber == 1) ? y1FirstPage : y1;

        var cellH: Float = 0.0
        for i in 0..<numOfHeaderRows {
            let dataRow = tableData[i]
            cellH = getMaxCellHeight(dataRow)
            var j = 0
            while j < dataRow.count {
                let cell = dataRow[j]
                var cellW = dataRow[j].getWidth()
                let colspan = dataRow[j].getColSpan()
                var k = 1
                while k < colspan {
                    j += 1
                    cellW += dataRow[j].width
                    k += 1
                }
                if page != nil {
                    page!.setBrushColor(cell.getBrushColor())
                    cell.paint(page!, x!, y!, cellW, cellH)
                }

                x! += cellW
                j += 1
            }
            x = x1
            y! += cellH
        }

        return [x!, y!]
    }


    private func drawTableRows(_ page: Page?, _ parameter: [Float]) -> [Float] {
        var x = parameter[0]
        var y = parameter[1]

        var cellH: Float = 0.0
        var i = rendered
        while i < tableData.count {
            let dataRow = tableData[i]
            cellH = getMaxCellHeight(dataRow)
            var j = 0
            while j < dataRow.count {
                let cell = dataRow[j]
                var cellW = cell.getWidth()
                let colspan = dataRow[j].getColSpan()
                var k = 1
                while k < colspan {
                    j += 1
                    cellW += dataRow[j].getWidth()
                    k += 1
                }
                if page != nil {
                    page!.setBrushColor(cell.getBrushColor())
                    cell.paint(page!, x, y, cellW, cellH)
                }
                x += cellW
                j += 1
            }
            x = x1!
            y += cellH

            // Consider the height of the next row when checking if we must go to a new page
            if i < (tableData.count - 1) {
                for cell in tableData[i + 1] {
                    if cell.getHeight() > cellH {
                        cellH = cell.getHeight()
                    }
                }
            }

            if page != nil && (y + cellH) > (page!.height - bottomMargin) {
                if i == (tableData.count - 1) {
                    rendered = -1
                }
                else {
                    rendered = i + 1
                    numOfPages += 1
                }
                return [x, y]
            }

            i += 1
        }
        rendered = -1

        return [x, y]
    }


    private func getMaxCellHeight(_ row: [Cell]) -> Float {
        var maxCellHeight: Float = 0.0
        for cell in row {
            if cell.getHeight() > maxCellHeight {
                maxCellHeight = cell.getHeight()
            }
        }
        return maxCellHeight
    }


    ///
    /// Returns true if the table contains more data that needs to be drawn on a page.
    ///
    public func hasMoreData() -> Bool {
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
    /// Wraps around the text in all cells so it fits the column width.
    /// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
    ///
    public func wrapAroundCellText() {
        var tableData2 = [[Cell]]()
        for row in tableData {
            var maxNumVerCells = 1
            for i in 0..<row.count {
                var n = 1
                while n < row[i].getColSpan() {
                    row[i].width += row[i + n].width
                    row[i + n].width = 0.0
                    n += 1
                }
                let numVerCells = row[i].getNumVerCells()
                if numVerCells > maxNumVerCells {
                    maxNumVerCells = numVerCells
                }
            }

            for i in 0..<maxNumVerCells {
                var row2 = [Cell]()
                for cell in row {
                    let cell2 = Cell(cell.getFont(), "")
                    cell2.setFallbackFont(cell.getFallbackFont())
                    cell2.setPoint(cell.getPoint())
                    cell2.setWidth(cell.getWidth())
                    if i == 0 {
                        cell2.setTopPadding(cell.topPadding)
                    }
                    if i == (maxNumVerCells - 1) {
                        cell2.setBottomPadding(cell.bottomPadding)
                    }
                    cell2.setLeftPadding(cell.leftPadding)
                    cell2.setRightPadding(cell.rightPadding)
                    cell2.setLineWidth(cell.lineWidth)
                    cell2.setBgColor(cell.getBgColor())
                    cell2.setPenColor(cell.getPenColor())
                    cell2.setBrushColor(cell.getBrushColor())
                    cell2.setProperties(cell.getProperties())
                    cell2.setVerTextAlignment(cell.getVerTextAlignment())
                    if i == 0 {
                        if (cell.getImage() != nil) {
                            cell2.setImage(cell.getImage())
                        }
                        if cell.getCompositeTextLine() != nil {
                            cell2.setCompositeTextLine(cell.getCompositeTextLine())
                        }
                        else {
                            cell2.setText(cell.getText())
                        }
                        if maxNumVerCells > 1 {
                            cell2.setBorder(Border.BOTTOM, false)
                        }
                    }
                    else  {
                        cell2.setBorder(Border.TOP, false)
                        if i < (maxNumVerCells - 1) {
                            cell2.setBorder(Border.BOTTOM, false)
                        }
                    }
                    row2.append(cell2)
                }
                tableData2.append(row2)
            }
        }

        for (i, row) in tableData2.enumerated() {
            for (j, cell) in row.enumerated() {
                if cell.text != nil {
                    var n = 0
                    let textLines = cell.text!.trim().components(separatedBy: "\n")
                    for textLine in textLines {
                        var sb = String()
                        let tokens = textLine.trim().components(separatedBy: .whitespaces)
                        if tokens.count == 1 {
                            sb.append(tokens[0])
                        }
                        else {
                            for k in 0..<tokens.count {
                                let token = tokens[k].trim()
                                if cell.font!.stringWidth(cell.fallbackFont, sb + " " + token) >
                                        (cell.getWidth() - (cell.leftPadding + cell.rightPadding)) {
                                    tableData2[i + n][j].setText(sb)
                                    sb = token
                                    n += 1
                                }
                                else {
                                    if k > 0 {
                                        sb.append(" ")
                                    }
                                    sb.append(token)
                                }
                            }
                        }
                        tableData2[i + n][j].setText(sb)
                        n += 1
                    }
                }
                else {
                    tableData2[i][j].setCompositeTextLine(cell.getCompositeTextLine())
                }
            }
        }

        tableData = tableData2
    }


    ///
    /// Sets all table cells borders to <strong>false</strong>.
    ///
    public func setNoCellBorders() {
        for row in tableData {
            for cell in row {
                cell.setNoBorders()
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


    ///
    /// Resets the rendered pages count.
    /// Call this method if you have to draw this table more than one time.
    ///
    public func resetRenderedPagesCount() {
        self.rendered = numOfHeaderRows
    }


    ///
    /// This method removes borders that have the same color and overlap 100%.
    /// The result is improved onscreen rendering of thin border lines by some PDF viewers.
    ///
    public func mergeOverlaidBorders() {
        for (i, row) in tableData.enumerated() {
            for (j, cell) in row.enumerated() {
                if j < (row.count - 1) {
                    let cellAtRight = row[j + 1]
                    if cellAtRight.getBorder(Border.LEFT) &&
                            cell.getPenColor() == cellAtRight.getPenColor() &&
                            cell.getLineWidth() == cellAtRight.getLineWidth() &&
                            (Int(cell.getColSpan()) + j) < (row.count - 1) {
                        cell.setBorder(Border.RIGHT, false)
                    }
                }
                if i < (tableData.count - 1) {
                    let nextRow = tableData[i + 1]
                    let cellBelow = nextRow[j]
                    if cellBelow.getBorder(Border.TOP) &&
                            cell.getPenColor() == cellBelow.getPenColor() &&
                            cell.getLineWidth() == cellBelow.getLineWidth() {
                        cell.setBorder(Border.BOTTOM, false)
                    }
                }
            }
        }
    }


    /**
     *  Auto adjusts the widths of all columns so that they are just wide enough to hold the text without truncation.
     */
    public func autoAdjustColumnWidths() {
        var maxColWidths = [Float](repeating: 0, count: (tableData[0].count))

        for i in 0..<numOfHeaderRows {
            for j in 0..<maxColWidths.count {
                let cell = tableData[i][j]
                var textWidth = cell.font!.stringWidth(cell.fallbackFont, cell.text)
                textWidth += cell.leftPadding + cell.rightPadding
                if textWidth > maxColWidths[j] {
                    maxColWidths[j] = textWidth
                }
            }
        }

        for i in numOfHeaderRows..<tableData.count {
            for j in 0..<maxColWidths.count {
                let cell = tableData[i][j]
                if cell.getColSpan() > 1 {
                    continue
                }
                if cell.text != nil {
                    var textWidth = cell.font!.stringWidth(cell.fallbackFont, cell.text)
                    textWidth += cell.leftPadding + cell.rightPadding
                    if textWidth > maxColWidths[j] {
                        maxColWidths[j] = textWidth
                    }
                }
                if cell.image != nil {
                    let imageWidth = cell.image!.getWidth() + cell.leftPadding + cell.rightPadding
                    if imageWidth > maxColWidths[j] {
                        maxColWidths[j] = imageWidth
                    }
                }
                if cell.barCode != nil {
                    let barcodeWidth = cell.barCode!.drawOn(nil)[0] + cell.leftPadding + cell.rightPadding
                    if barcodeWidth > maxColWidths[j] {
                        maxColWidths[j] = barcodeWidth
                    }
                }
                if cell.textBlock != nil {
                    let tokens = cell.textBlock!.text!.components(separatedBy: .whitespaces)
                    for token in tokens {
                        var tokenWidth = cell.textBlock!.font.stringWidth(cell.textBlock!.fallbackFont, token)
                        tokenWidth += cell.leftPadding + cell.rightPadding
                        if tokenWidth > maxColWidths[j] {
                            maxColWidths[j] = tokenWidth
                        }
                    }
                }
            }
        }

        for i in 0..<tableData.count {
            let row = tableData[i]
            for j in 0..<row.count {
                let cell = row[j]
                cell.setWidth(maxColWidths[j] + 0.1)
            }
        }

        autoResizeColumnsWithColspanBiggerThanOne()
    }


    private func isTextColumn(_ index: Int) -> Bool {
        for i in numOfHeaderRows..<tableData.count {
            let dataRow = tableData[i]
            if dataRow[index].image != nil || dataRow[index].barCode != nil {
                return false
            }
        }
        return true
    }


    public func fitToPage(_ pageSize: [Float]) {
        autoAdjustColumnWidths()

        let tableWidth: Float = (pageSize[0] - self.x1!) - rightMargin
        var textColumnWidths: Float = 0.0
        var otherColumnWidths: Float = 0.0
        let row = tableData[0]
        for i in 0..<row.count {
            let cell = row[i]
            if isTextColumn(i) {
                textColumnWidths += cell.getWidth()
            }
            else {
                otherColumnWidths += cell.getWidth()
            }
        }

        var adjusted: Float = 0.0
        if (tableWidth - otherColumnWidths) > textColumnWidths {
            adjusted = textColumnWidths + ((tableWidth - otherColumnWidths) - textColumnWidths)
        }
        else {
            adjusted = textColumnWidths - (textColumnWidths - (tableWidth - otherColumnWidths))
        }
        let factor: Float = adjusted / textColumnWidths
        for i in 0..<row.count {
            if isTextColumn(i) {
                setColumnWidth(i, getColumnWidth(i) * factor)
            }
        }

        autoResizeColumnsWithColspanBiggerThanOne()
        mergeOverlaidBorders()
    }


    private func autoResizeColumnsWithColspanBiggerThanOne() {
        for i in 0..<tableData.count {
            let dataRow = tableData[i]
            var j = 0
            while j < dataRow.count {
                let cell = dataRow[j]
                let colspan = cell.getColSpan()
                if colspan > 1 {
                    if cell.textBlock != nil {
                        var sumOfWidths = cell.getWidth()
                        for _ in 1..<colspan {
                            j += 1
                            sumOfWidths += dataRow[j].getWidth()
                        }
                        cell.textBlock!.setWidth(sumOfWidths - (cell.leftPadding + cell.rightPadding))
                    }
                }
                j += 1
            }
        }
    }


    public func setRightMargin(_ rightMargin: Float) {
        self.rightMargin = rightMargin
    }


    public func setFirstPageTopMargin(_ topMargin: Float) {
        self.y1FirstPage = y1! + topMargin
    }


    public func addToRow(_ row: inout [Cell], _ cell: Cell) {
        row.append(cell)
        for _ in 1..<cell.getColSpan() {
            row.append(Cell(cell.getFont(), ""))
        }
    }

}   // End of Table.swift
