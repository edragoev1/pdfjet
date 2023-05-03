/**
 *  Table.cs
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
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PDFjet.NET {
/**
 *  Used to create table objects and draw them on a page.
 *
 *  Please see Example_08.
 */
public class Table {
    public static readonly int DATA_HAS_0_HEADER_ROWS = 0;
    public static readonly int DATA_HAS_1_HEADER_ROWS = 1;
    public static readonly int DATA_HAS_2_HEADER_ROWS = 2;
    public static readonly int DATA_HAS_3_HEADER_ROWS = 3;
    public static readonly int DATA_HAS_4_HEADER_ROWS = 4;
    public static readonly int DATA_HAS_5_HEADER_ROWS = 5;
    public static readonly int DATA_HAS_6_HEADER_ROWS = 6;
    public static readonly int DATA_HAS_7_HEADER_ROWS = 7;
    public static readonly int DATA_HAS_8_HEADER_ROWS = 8;
    public static readonly int DATA_HAS_9_HEADER_ROWS = 9;

    private int rendered = 0;
    private int numOfPages;

    private List<List<Cell>> tableData = null;
    private int numOfHeaderRows = 0;

    private float x1;
    private float y1;
    private float y1FirstPage;
    private float rightMargin;
    private float bottomMargin;

    /**
     *  Create a table object.
     *
     */
    public Table() {
        tableData = new List<List<Cell>>();
    }

    /**
     *  Sets the position (x, y) of the top left corner of this table on the page.
     *
     *  @param x the x coordinate of the top left point of the table.
     *  @param y the y coordinate of the top left point of the table.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     *  Sets the position (x, y) of the top left corner of this table on the page.
     *
     *  @param x the x coordinate of the top left point of the table.
     *  @param y the y coordinate of the top left point of the table.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     *  Sets the location (x, y) of the top left corner of this table on the page.
     *
     *  @param x the x coordinate of the top left point of the table.
     *  @param y the y coordinate of the top left point of the table.
     */
    public void SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
    }

    /**
     *  Sets the bottom margin for this table.
     *
     *  @param bottomMargin the margin.
     */
    public void SetBottomMargin(double bottomMargin) {
        this.bottomMargin = (float) bottomMargin;
    }

    /**
     *  Sets the bottom margin for this table.
     *
     *  @param bottomMargin the margin.
     */
    public void SetBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
    }

    /**
     *  Sets the table data.
     *
     *  @param tableData the table data.
     */
    public void SetData(List<List<Cell>> tableData) {
        this.tableData = tableData;
        this.numOfHeaderRows = 0;
        this.rendered = numOfHeaderRows;

        // Add the missing cells.
        int numOfColumns = tableData[0].Count;
        Font font = tableData[0][0].font;
        foreach (List<Cell> row in tableData) {
            int diff = numOfColumns - row.Count;
            for (int i = 0; i < diff; i++) {
                row.Add(new Cell(font, ""));
            }
        }
    }

    /**
     *  Sets the table data and specifies the number of header rows in this data.
     *
     *  @param tableData the table data.
     *  @param numOfHeaderRows the number of header rows in this data.
     */
    public void SetData(List<List<Cell>> tableData, int numOfHeaderRows) {
        this.tableData = tableData;
        this.numOfHeaderRows = numOfHeaderRows;
        this.rendered = numOfHeaderRows;
    }

    /**
     *  Sets the alignment of the numbers to the right.
     */
    public void RightAlignNumbers() {
        for (int i = numOfHeaderRows; i < tableData.Count; i++) {
            List<Cell> row = tableData[i];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                if (cell.text != null) {
                    String str = cell.text;
                    int len = str.Length;
                    bool isNumber = true;
                    int k = 0;
                    while (k < len) {
                        char ch = str[k++];
                        if (!Char.IsNumber(ch)
                                && ch != '('
                                && ch != ')'
                                && ch != '-'
                                && ch != '.'
                                && ch != ','
                                && ch != '\'') {
                            isNumber = false;
                        }
                    }
                    if (isNumber) {
                        cell.SetTextAlignment(Align.RIGHT);
                    }
                }
            }
        }
    }

    /**
     *  Removes the horizontal lines between the rows from index1 to index2.
     */
    public void RemoveLineBetweenRows(int index1, int index2) {
        for (int i = index1; i < index2; i++) {
            List<Cell> row = tableData[i];
            foreach (Cell cell in row) {
                cell.SetBorder(Border.BOTTOM, false);
            }
            row = tableData[i + 1];
            foreach (Cell cell in row) {
                cell.SetBorder(Border.TOP, false);
            }
        }
    }

    /**
     *  Sets the text alignment in the specified column.
     *
     *  @param index the index of the specified column.
     *  @param alignment the specified alignment. Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.
     */
    public void SetTextAlignInColumn(int index, uint alignment) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.SetTextAlignment(alignment);
                if (cell.textBox != null) {
                    cell.textBox.SetTextAlignment(alignment);
                }
            }
        }
    }

    /**
     *  Sets the color of the text in the specified column.
     *
     *  @param index the index of the specified column.
     *  @param color the color specified as an integer.
     */
    public void SetTextColorInColumn(int index, int color) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.SetBrushColor(color);
                if (cell.textBox != null) {
                    cell.textBox.SetBrushColor(color);
                }
            }
        }
    }

    /**
     *  Sets the font for the specified column.
     *
     *  @param index the column index.
     *  @param font the font.
     */
    public void SetFontInColumn(int index, Font font) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.font = font;
                if (cell.textBox != null) {
                    cell.textBox.font = font;
                }
            }
        }
    }

    /**
     *  Sets the color of the text in the specified row.
     *
     *  @param index the index of the specified row.
     *  @param color the color specified as an integer.
     */
    public void SetTextColorInRow(int index, int color) {
        List<Cell> row = tableData[index];
        foreach (Cell cell in row) {
            cell.SetBrushColor(color);
            if (cell.textBox != null) {
                cell.textBox.SetBrushColor(color);
            }
        }
    }

    /**
     *  Sets the font for the specified row.
     *
     *  @param index the row index.
     *  @param font the font.
     */
    public void SetFontInRow(int index, Font font) {
        List<Cell> row = tableData[index];
        foreach (Cell cell in row) {
            cell.font = font;
            if (cell.textBox != null) {
                cell.textBox.font = font;
            }
        }
    }

    /**
     *  Sets the width of the column with the specified index.
     *
     *  @param index the index of specified column.
     *  @param width the specified width.
     */
    public void SetColumnWidth(int index, double width) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                row[index].SetWidth(width);
            }
        }
    }

    /**
     *  Returns the column width of the column at the specified index.
     *
     *  @param index the index of the column.
     *  @return the width of the column.
     */
    public float GetColumnWidth(int index) {
        return GetCellAtRowColumn(0, index).GetWidth();
    }

    /**
     *  Returns the cell at the specified row and column.
     *
     *  @param row the specified row.
     *  @param col the specified column.
     *
     *  @return the cell at the specified row and column.
     */
    public Cell GetCellAt(int row, int col) {
        if (row >= 0) {
            return tableData[row][col];
        }
        return tableData[tableData.Count + row][col];
    }

    /**
     *  Returns the cell at the specified row and column.
     *
     *  @param row the specified row.
     *  @param col the specified column.
     *
     *  @return the cell at the specified row and column.
     */
    public Cell GetCellAtRowColumn(int row, int col) {
        return GetCellAt(row, col);
    }

    /**
     *  Returns a list of cell for the specified row.
     *
     *  @param index the index of the specified row.
     *
     *  @return the list of cells.
     */
    public List<Cell> GetRow(int index) {
        return tableData[index];
    }

    public List<Cell> GetRowAtIndex(int index) {
        return GetRow(index);
    }

    /**
     *  Returns a list of cell for the specified column.
     *
     *  @param index the index of the specified column.
     *
     *  @return the list of cells.
     */
    public List<Cell> GetColumn(int index) {
        List<Cell> column = new List<Cell>();
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                column.Add(row[index]);
            }
        }
        return column;
    }

    /**
     *  Returns the total number of pages that are required to draw this table on.
     *
     *  @param page the type of pages we are drawing this table on.
     *
     *  @return the number of pages.
     */
    [Obsolete("This method is deprecated. Please use Page.DETACHED. See Example_13.cs")]
    public int GetNumberOfPages(Page page) {
        numOfPages = 1;
        while (HasMoreData()) {
            DrawOn(null);
        }
        ResetRenderedPagesCount();
        return numOfPages;
    }

    /**
     *  Draws this table on the specified page.
     *
     *  @param page the page to draw this table on.
     *  @return Point the point on the page where to draw the next component.
     */
    public float[] DrawOn(Page page) {
        return DrawTableRows(page, DrawHeaderRows(page, 0));
    }

    public float[] DrawOn(PDF pdf, List<Page> pages, float[] pageSize) {
        float[] xy = null;
        int pageNumber = 1;
        while (this.HasMoreData()) {
            Page page = new Page(pdf, pageSize, false);
            pages.Add(page);
            xy = DrawTableRows(page, DrawHeaderRows(page, pageNumber));
            pageNumber++;
        }
        // Allow the table to be drawn again later:
        ResetRenderedPagesCount();
        return xy;
    }

    private float[] DrawHeaderRows(Page page, int pageNumber) {
        float x = x1;
        float y = (pageNumber == 1) ? y1FirstPage : y1;

        float cellH;
        for (int i = 0; i < numOfHeaderRows; i++) {
            List<Cell> dataRow = tableData[i];
            cellH = GetMaxCellHeight(dataRow);
            for (int j = 0; j < dataRow.Count; j++) {
                Cell cell = dataRow[j];
                float cellW = cell.GetWidth();
                uint colspan = cell.GetColSpan();
                for (int k = 1; k < colspan; k++) {
                    cellW += dataRow[++j].GetWidth();
                }
                if (page != null) {
                    page.SetBrushColor(cell.GetBrushColor());
                    cell.Paint(page, x, y, cellW, cellH);
                }
                x += cellW;
            }
            x = x1;
            y += cellH;
        }

        return new float[] {x, y};
    }

    private float[] DrawTableRows(Page page, float[] parameter) {
        float x = parameter[0];
        float y = parameter[1];

        float cellH;
        for (int i = rendered; i < tableData.Count; i++) {
            List<Cell> dataRow = tableData[i];
            cellH = GetMaxCellHeight(dataRow);
            for (int j = 0; j < dataRow.Count; j++) {
                Cell cell = dataRow[j];
                float cellW = cell.GetWidth();
                uint colspan = cell.GetColSpan();
                for (int k = 1; k < colspan; k++) {
                    cellW += dataRow[++j].GetWidth();
                }
                if (page != null) {
                    page.SetBrushColor(cell.GetBrushColor());
                    cell.Paint(page, x, y, cellW, cellH);
                }
                x += cellW;
            }
            x = x1;
            y += cellH;

            // Consider the height of the next row when checking if we must go to a new page
            if (i < (tableData.Count - 1)) {
                List<Cell> nextRow = tableData[i + 1];
                for (int j = 0; j < nextRow.Count; j++) {
                    Cell cell = nextRow[j];
                    float cellHeight = cell.GetHeight();
                    if (cellHeight > cellH) {
                        cellH = cellHeight;
                    }
                }
            }

            if (page != null && (y + cellH) > (page.height - bottomMargin)) {
                if (i == tableData.Count - 1) {
                    rendered = -1;
                } else {
                    rendered = i + 1;
                    numOfPages++;
                }
                return new float[] {x, y};
            }
        }
        rendered = -1;

        return new float[] {x, y};
    }

    private float GetMaxCellHeight(List<Cell> row) {
        float maxCellHeight = 0f;
        foreach (Cell cell in row) {
            if (cell.GetHeight() > maxCellHeight) {
                maxCellHeight = cell.GetHeight();
            }
        }
        return maxCellHeight;
    }

    /**
     *  Returns true if the table contains more data that needs to be drawn on a page.
     */
    public bool HasMoreData() {
        return rendered != -1;
    }

    /**
     *  Returns the width of this table when drawn on a page.
     *
     *  @return the width of this table.
     */
    public float GetWidth() {
        float tableWidth = 0f;
        List<Cell> row = tableData[0];
        foreach (Cell cell in row) {
            tableWidth += cell.GetWidth();
        }
        return tableWidth;
    }

    /**
     *  Returns the number of data rows that have been rendered so far.
     *
     *  @return the number of data rows that have been rendered so far.
     */
    public int GetRowsRendered() {
        return rendered == -1 ? rendered : rendered - numOfHeaderRows;
    }

    /**
     *  Wraps around the text in all cells so it fits the column width.
     *  This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
     */
    public void WrapAroundCellText() {
        List<List<Cell>> tableData2 = new List<List<Cell>>();

        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> row = tableData[i];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                uint colspan = cell.GetColSpan();
                for (int n = 1; n < colspan; n++) {
                    Cell next = row[j + n];
                    cell.SetWidth(cell.GetWidth() + next.GetWidth());
                    next.SetWidth(0f);
                }
            }
        }

        // Adjust the number of header rows automatically!
        numOfHeaderRows = GetNumHeaderRows();
        rendered = numOfHeaderRows;
        AddExtraTableRows(tableData2);
        for (int i = 0; i < tableData2.Count; i++) {
            List<Cell> row = tableData2[i];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                if (cell.text != null) {
                    int n = 0;
                    float effectiveWidth = cell.width - (cell.leftPadding + cell.rightPadding);
                    String[] tokens = TextUtils.SplitTextIntoTokens(
                        cell.text, cell.font, cell.fallbackFont, effectiveWidth);
                    StringBuilder buf = new StringBuilder();
                    foreach (String token in tokens) {
                        if (cell.font.StringWidth(cell.fallbackFont,
                                (buf.ToString() + " " + token).Trim()) > effectiveWidth) {
                            tableData2[i + n][j].SetText(buf.ToString().Trim());
                            buf = new StringBuilder(token);
                            n++;
                        } else {
                            buf.Append(" ");
                            buf.Append(token);
                        }
                    }
                    tableData2[i + n][j].SetText(buf.ToString().Trim());
                } else {
                    tableData2[i][j].SetCompositeTextLine(cell.GetCompositeTextLine());
                }
            }
        }

        tableData = tableData2;
    }

    private void AddExtraTableRows(List<List<Cell>> tableData2) {
        foreach (List<Cell> row in tableData) {
            int maxNumVerCells = 0;
            foreach (Cell cell in row) {
                int numVerCells = cell.GetNumVerCells();
                if (numVerCells > maxNumVerCells) {
                    maxNumVerCells = numVerCells;
                }
            }

            for (int i = 0; i < maxNumVerCells; i++) {
                List<Cell> row2 = new List<Cell>();
                foreach (Cell cell in row) {
                    Cell cell2 = new Cell(cell.GetFont(), "");
                    cell2.SetFallbackFont(cell.GetFallbackFont());
                    cell2.SetWidth(cell.GetWidth());
                    if (i == 0) {
                        cell2.SetPoint(cell.GetPoint());
                        cell2.SetTopPadding(cell.topPadding);
                    }
                    if (i == (maxNumVerCells - 1)) {
                        cell2.SetBottomPadding(cell.bottomPadding);
                    }
                    cell2.SetLeftPadding(cell.leftPadding);
                    cell2.SetRightPadding(cell.rightPadding);
                    cell2.SetLineWidth(cell.lineWidth);
                    cell2.SetBgColor(cell.GetBgColor());
                    cell2.SetPenColor(cell.GetPenColor());
                    cell2.SetBrushColor(cell.GetBrushColor());
                    cell2.SetProperties(cell.GetProperties());
                    cell2.SetVerTextAlignment(cell.GetVerTextAlignment());
                    if (i == 0) {
                        if (cell.image != null) {
                            cell2.SetImage(cell.GetImage());
                        }
                        if (cell.GetCompositeTextLine() != null) {
                            cell2.SetCompositeTextLine(cell.GetCompositeTextLine());
                        } else {
                            cell2.SetText(cell.GetText());
                        }
                        if (maxNumVerCells > 1) {
                            cell2.SetBorder(Border.BOTTOM, false);
                        }
                    } else  {
                        cell2.SetBorder(Border.TOP, false);
                        if (i < (maxNumVerCells - 1)) {
                            cell2.SetBorder(Border.BOTTOM, false);
                        }
                    }
                    row2.Add(cell2);
                }
                tableData2.Add(row2);
            }
        }
    }

    private int GetNumHeaderRows() {
        int numberOfHeaderRows = 0;
        for (int i = 0; i < this.numOfHeaderRows; i++) {
            List<Cell> row = tableData[i];
            int maxNumVerCells = 0;
            foreach (Cell cell in row) {
                int numVerCells = cell.GetNumVerCells();
                if (numVerCells > maxNumVerCells) {
                    maxNumVerCells = numVerCells;
                }
            }
            numberOfHeaderRows += maxNumVerCells;
        }
        return numberOfHeaderRows;
    }

    /**
     *  Sets all table cells borders to <strong>false</strong>.
     *
     */
    public void SetNoCellBorders() {
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> row = tableData[i];
            for (int j = 0; j < row.Count; j++) {
                tableData[i][j].SetNoBorders();
            }
        }
    }

    /**
     *  Sets the color of the cell border lines.
     *
     *  @param color the color of the cell border lines.
     */
    public void SetCellBordersColor(int color) {
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> row = tableData[i];
            for (int j = 0; j < row.Count; j++) {
                tableData[i][j].SetPenColor(color);
            }
        }
    }

    /**
     *  Sets the width of the cell border lines.
     *
     *  @param width the width of the cell border lines.
     */
    public void SetCellBordersWidth(float width) {
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> row = tableData[i];
            for (int j = 0; j < row.Count; j++) {
                tableData[i][j].SetLineWidth(width);
            }
        }
    }

    /**
     * Resets the rendered pages count.
     * Call this method if you have to draw this table more than one time.
     */
    public void ResetRenderedPagesCount() {
        this.rendered = numOfHeaderRows;
    }

    /**
     * This method removes borders that have the same color and overlap 100%.
     * The result is improved onscreen rendering of thin border lines by some PDF viewers.
     */
    public void MergeOverlaidBorders() {
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> currentRow = tableData[i];
            for (int j = 0; j < currentRow.Count; j++) {
                Cell currentCell = currentRow[j];
                if (j < currentRow.Count - 1) {
                    Cell cellAtRight = currentRow[j + 1];
                    if (cellAtRight.GetBorder(Border.LEFT) &&
                            currentCell.GetPenColor() == cellAtRight.GetPenColor() &&
                            currentCell.GetLineWidth() == cellAtRight.GetLineWidth() &&
                            (currentCell.GetColSpan() + j) < (currentRow.Count - 1)) {
                        currentCell.SetBorder(Border.RIGHT, false);
                    }
                }
                if (i < tableData.Count - 1) {
                    List<Cell> nextRow = tableData[i + 1];
                    Cell cellBelow = nextRow[j];
                    if (cellBelow.GetBorder(Border.TOP) &&
                            currentCell.GetPenColor() == cellBelow.GetPenColor() &&
                            currentCell.GetLineWidth() == cellBelow.GetLineWidth()) {
                        currentCell.SetBorder(Border.BOTTOM, false);
                    }
                }
            }
        }
    }

    /**
     *  Auto adjusts the widths of all columns so that they are just wide enough to hold the text without truncation.
     */
    public void AutoAdjustColumnWidths() {
        float[] maxColWidths = new float[tableData[0].Count];

        for (int i = 0; i < numOfHeaderRows; i++) {
            for (int j = 0; j < maxColWidths.Length; j++) {
                Cell cell = tableData[i][j];
                float textWidth = cell.font.StringWidth(cell.fallbackFont, cell.text);
                textWidth += cell.leftPadding + cell.rightPadding;
                if (textWidth > maxColWidths[j]) {
                    maxColWidths[j] = textWidth;
                }
            }
        }

        for (int i = numOfHeaderRows; i < tableData.Count; i++) {
            for (int j = 0; j < maxColWidths.Length; j++) {
                Cell cell = tableData[i][j];
                if (cell.GetColSpan() > 1) {
                    continue;
                }
                if (cell.text != null) {
                    float textWidth = cell.font.StringWidth(cell.fallbackFont, cell.text);
                    textWidth += cell.leftPadding + cell.rightPadding;
                    if (textWidth > maxColWidths[j]) {
                        maxColWidths[j] = textWidth;
                    }
                }
                if (cell.image != null) {
                    float imageWidth = cell.image.GetWidth() + cell.leftPadding + cell.rightPadding;
                    if (imageWidth > maxColWidths[j]) {
                        maxColWidths[j] = imageWidth;
                    }
                }
                if (cell.barCode != null) {
                    try {
                        float barcodeWidth = cell.barCode.DrawOn(null)[0] + cell.leftPadding + cell.rightPadding;
                        if (barcodeWidth > maxColWidths[j]) {
                            maxColWidths[j] = barcodeWidth;
                        }
                    } catch (Exception) {
                    }
                }
                if (cell.textBox != null) {
                    String[] tokens = Regex.Split(cell.textBox.text, @"\s+");
                    foreach (String token in tokens) {
                        float tokenWidth = cell.textBox.font.StringWidth(cell.textBox.fallbackFont, token);
                        tokenWidth += cell.leftPadding + cell.rightPadding;
                        if (tokenWidth > maxColWidths[j]) {
                            maxColWidths[j] = tokenWidth;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> row = tableData[i];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                cell.SetWidth(maxColWidths[j] + 0.1f);
            }
        }

        AutoResizeColumnsWithColspanBiggerThanOne();
    }

    private bool IsTextColumn(int index) {
        for (int i = numOfHeaderRows; i < tableData.Count; i++) {
            List<Cell> dataRow = tableData[i];
            if (dataRow[index].image != null || dataRow[index].barCode != null) {
                return false;
            }
        }
        return true;
    }

    public void FitToPage(float[] pageSize) {
        AutoAdjustColumnWidths();

        float tableWidth = (pageSize[0] - this.x1) - rightMargin;
        float textColumnWidths = 0f;
        float otherColumnWidths = 0f;
        List<Cell> row = tableData[0];
        for (int i = 0; i < row.Count; i++) {
            Cell cell = row[i];
            if (IsTextColumn(i)) {
                textColumnWidths += cell.GetWidth();
            } else {
                otherColumnWidths += cell.GetWidth();
            }
        }

        float adjusted;
        if ((tableWidth - otherColumnWidths) > textColumnWidths) {
            adjusted = textColumnWidths + ((tableWidth - otherColumnWidths) - textColumnWidths);
        } else {
            adjusted = textColumnWidths - (textColumnWidths - (tableWidth - otherColumnWidths));
        }
        float factor = adjusted / textColumnWidths;
        for (int i = 0; i < row.Count; i++) {
            if (IsTextColumn(i)) {
                SetColumnWidth(i, GetColumnWidth(i) * factor);
            }
        }

        AutoResizeColumnsWithColspanBiggerThanOne();
        MergeOverlaidBorders();
    }

    private void AutoResizeColumnsWithColspanBiggerThanOne() {
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> dataRow = tableData[i];
            for (int j = 0; j < dataRow.Count; j++) {
                Cell cell = dataRow[j];
                uint colspan = cell.GetColSpan();
                if (colspan > 1) {
                    if (cell.textBox != null) {
                        float sumOfWidths = cell.GetWidth();
                        for (int k = 1; k < colspan; k++) {
                            sumOfWidths += dataRow[++j].GetWidth();
                        }
                        cell.textBox.SetWidth(sumOfWidths - (cell.leftPadding + cell.rightPadding));
                    }
                }
            }
        }
    }

    public void SetRightMargin(float rightMargin) {
        this.rightMargin = rightMargin;
    }

    public void SetFirstPageTopMargin(float topMargin) {
        this.y1FirstPage = y1 + topMargin;
    }

    public static void AddToRow(List<Cell> row, Cell cell) {
        row.Add(cell);
        for (int i = 1; i < cell.GetColSpan(); i++) {
            row.Add(new Cell(cell.GetFont(), ""));
        }
    }
}   // End of Table.cs
}   // End of namespace PDFjet.NET
