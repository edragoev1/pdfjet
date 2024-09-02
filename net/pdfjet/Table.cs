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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/**
 *  Used to create table objects and draw them on a page.
 *
 *  Please see Example_08.
 */
namespace PDFjet.NET {
public class Table {
    public static readonly int WITH_0_HEADER_ROWS = 0;
    public static readonly int WITH_1_HEADER_ROW  = 1;
    public static readonly int WITH_2_HEADER_ROWS = 2;
    public static readonly int WITH_3_HEADER_ROWS = 3;
    public static readonly int WITH_4_HEADER_ROWS = 4;
    public static readonly int WITH_5_HEADER_ROWS = 5;
    public static readonly int WITH_6_HEADER_ROWS = 6;
    public static readonly int WITH_7_HEADER_ROWS = 7;
    public static readonly int WITH_8_HEADER_ROWS = 8;
    public static readonly int WITH_9_HEADER_ROWS = 9;

    private List<List<Cell>> tableData;
    private int numOfHeaderRows = 1;
    private int rendered = 0;
    private float x1;
    private float y1;
    private float x1FirstPage;
    private float y1FirstPage;
    private float bottomMargin;

    /**
     *  Create a table object.
     *
     */
    public Table() {
        tableData = new List<List<Cell>>();
    }

    /**
     *  Create a table object.
     *
     */
    public Table(Font f1, Font f2, String fileName) {
        tableData = new List<List<Cell>>();
        StreamReader reader = new StreamReader(fileName);
        Char[] delimiterRegex = null;
        int numberOfFields = 0;
        int lineNumber = 0;
        String line;
        while ((line = reader.ReadLine()) != null) {
            if (lineNumber == 0) {
                delimiterRegex = GetDelimiterRegex(line);
                numberOfFields = line.Split(delimiterRegex).Length;
            }
            List<Cell> row = new List<Cell>();
            String[] fields = line.Split(delimiterRegex);
            foreach (String field in fields) {
                if (lineNumber == 0) {
                    Cell cell = new Cell(f1);
                    cell.SetTextBox(new TextBox(f1, field));
                    row.Add(cell);
                } else {
                    row.Add(new Cell(f2, field));
                }
            }
            if (row.Count > numberOfFields) {
                List<Cell> row2 = new List<Cell>();
                for (int i = 0; i < numberOfFields; i++) {
                    row2.Add(row[i]);
                }
                tableData.Add(row2);
            } else if (row.Count < numberOfFields) {
                int diff = numberOfFields - row.Count;
                for (int i = 0; i < diff; i++) {
                    row.Add(new Cell(f2));
                }
                tableData.Add(row);
            } else {
                tableData.Add(row);
            }
            lineNumber++;
        }
        reader.Close();
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
        StringBuilder buf = new StringBuilder();
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                if (cell.text != null) {
                    buf.Length = 0;
                    String str = cell.text;
                    if (str.StartsWith("(") && str.EndsWith(")")) {
                        str = str.Substring(1, str.Length - 1);
                    }
                    foreach (char ch in str) {
                        if (ch != '.' && ch != ',' && ch != '\'') {
                            buf.Append(ch);
                        }
                    }
                    try {
                        Double.Parse(buf.ToString());
                        cell.SetTextAlignment(Align.RIGHT);
                    } catch (Exception) {
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
        if (index < tableData.Count) {
            List<Cell> row = tableData[index];
            foreach (Cell cell in row) {
                cell.SetBrushColor(color);
                if (cell.textBox != null) {
                    cell.textBox.SetBrushColor(color);
                }
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
        if (index < tableData.Count) {
            List<Cell> row = tableData[index];
            foreach (Cell cell in row) {
                cell.font = font;
                if (cell.textBox != null) {
                    cell.textBox.font = font;
                }
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
     *  Draws this table on the specified page.
     *
     *  @param page the page to draw this table on.
     *  @return Point the point on the page where to draw the next component.
     */
    public float[] DrawOn(Page page) {
        WrapAroundCellText();
        SetRightBorderOnLastColumn();
	    SetBottomBorderOnLastRow();
        return DrawTableRows(page, DrawHeaderRows(page, 0));
    }

    public float[] DrawOn(PDF pdf, List<Page> pages, float[] pageSize) {
        WrapAroundCellText();
        SetRightBorderOnLastColumn();
	    SetBottomBorderOnLastRow();
        float[] xy = null;
        int pageNumber = 1;
        while (HasMoreData()) {
            Page page = new Page(pdf, pageSize, false);
            pages.Add(page);
            xy = DrawTableRows(page, DrawHeaderRows(page, pageNumber));
            pageNumber++;
        }
        return xy;
    }

    private float[] DrawHeaderRows(Page page, int pageNumber) {
        float x = x1;
        float y = y1;
        if (pageNumber == 1 && y1FirstPage > 0f) {
            x = x1FirstPage;
            y = y1FirstPage;
        }
        for (int i = 0; i < numOfHeaderRows; i++) {
            List<Cell> row = tableData[i];
            float h = GetMaxCellHeight(row);
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                float w = cell.GetWidth();
                uint colspan = cell.GetColSpan();
                for (int k = 1; k < colspan; k++) {
                    j++;
                    w += row[j].GetWidth();
                }
                if (page != null) {
                    page.SetBrushColor(cell.GetBrushColor());
                    if (i == (numOfHeaderRows - 1)) {
                        cell.SetBorder(Border.BOTTOM, true);
                    }
                    cell.DrawOn(page, x, y, w, h);
                }
                x += w;
            }
            x = x1;
            y += h;
            rendered++;
        }
        return new float[] {x, y};
    }

    private float[] DrawTableRows(Page page, float[] xy) {
        float x = xy[0];
        float y = xy[1];
        while (rendered < tableData.Count) {
            List<Cell> row = tableData[rendered];
            float h = GetMaxCellHeight(row);
            if (page != null && (y + h) > (page.height - bottomMargin)) {
                return new float[] {x, y};
            }
            for (int i = 0; i < row.Count; i++) {
                Cell cell = row[i];
                float w = cell.GetWidth();
                int colspan = (int) cell.GetColSpan();
                for (int j = 1; j < colspan; j++) {
                    i++;
                    w += row[i].GetWidth();
                }
                if (page != null) {
                    page.SetBrushColor(cell.GetBrushColor());
                    cell.DrawOn(page, x, y, w, h);
                }
                x += w;
            }
            x = x1;
            y += h;
            rendered++;
        }
        rendered = -1; // We are done!
        return new float[] {x, y};
    }

    private float GetMaxCellHeight(List<Cell> row) {
        float maxCellHeight = 0f;
        for (int i = 0; i < row.Count; i++) {
            Cell cell = row[i];
            float totalWidth = GetTotalWidth(row, i);
            float cellHeight = cell.GetHeight(totalWidth);
            if (cellHeight > maxCellHeight) {
                maxCellHeight = cellHeight;
            }
        }
        return maxCellHeight;
    }

    /**
     *  Returns true if the table contains more data that needs to be drawn on a page.
     */
    private bool HasMoreData() {
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
     * Sets all table cells borders.
     * @param borders true or false.
     */
    public void SetCellBorders(bool borders) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetBorders(borders);
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

    // Sets the right border on all cells in the last column.
    private void SetRightBorderOnLastColumn() {
        foreach (List<Cell> row in tableData) {
            Cell cell = null;
            int i = 0;
            while (i < row.Count) {
                cell = row[i];
                i += (int) cell.GetColSpan();
            }
            cell.SetBorder(Border.RIGHT, true);
        }
    }

    // Sets the bottom border on all cells in the last row.
    private void SetBottomBorderOnLastRow() {
        List<Cell> lastRow = tableData[tableData.Count - 1];
        foreach (Cell cell in lastRow) {
            cell.SetBorder(Border.BOTTOM, true);
        }
    }

    /**
     * Auto adjusts the widths of all columns so that they are just wide enough to
     * hold the text without truncation.
     */
    public void SetColumnWidths() {
        float[] maxColWidths = new float[tableData[0].Count];
        foreach (List<Cell> row in tableData) {
            for (int i = 0; i < row.Count; i++) {
                Cell cell = row[i];
                if (cell.GetColSpan() == 1) {
                    if (cell.textBox != null) {
                        String[] tokens = Regex.Split(cell.textBox.text, "\\s+");
                        foreach (String token in tokens) {
                            float tokenWidth = cell.textBox.font.StringWidth(cell.textBox.fallbackFont, token);
                            tokenWidth += cell.leftPadding + cell.rightPadding;
                            if (tokenWidth > maxColWidths[i]) {
                                maxColWidths[i] = tokenWidth;
                            }
                        }
                    } else if (cell.image != null) {
                        float imageWidth = cell.image.GetWidth() + cell.leftPadding + cell.rightPadding;
                        if (imageWidth > maxColWidths[i]) {
                            maxColWidths[i] = imageWidth;
                        }
                    } else if (cell.barcode != null) {
                        try {
                            float barcodeWidth = cell.barcode.DrawOn(null)[0] + cell.leftPadding + cell.rightPadding;
                            if (barcodeWidth > maxColWidths[i]) {
                                maxColWidths[i] = barcodeWidth;
                            }
                        } catch (Exception) {
                        }
                    } else if (cell.text != null) {
                        float textWidth = cell.font.StringWidth(cell.fallbackFont, cell.text);
                        textWidth += cell.leftPadding + cell.rightPadding;
                        if (textWidth > maxColWidths[i]) {
                            maxColWidths[i] = textWidth;
                        }
                    }
                }
            }
        }
        foreach (List<Cell> row in tableData) {
            for (int i = 0; i < row.Count; i++) {
                row[i].SetWidth(maxColWidths[i]);
            }
        }
    }

    private List<List<Cell>> AddExtraTableRows() {
        List<List<Cell>> tableData2 = new List<List<Cell>>();
        foreach (List<Cell> row in tableData) {
            tableData2.Add(row);    // Add the original row
            int maxNumVerCells = 0;
            for (int i = 0; i < row.Count; i++) {
                int numVerCells = GetNumVerCells(row, i);
                if (numVerCells > maxNumVerCells) {
                    maxNumVerCells = numVerCells;
                }
            }
            for (int i = 1; i < maxNumVerCells; i++) {
                List<Cell> row2 = new List<Cell>();
                foreach (Cell cell in row) {
                    Cell cell2 = new Cell(cell.GetFont());
                    cell2.SetFallbackFont(cell.GetFallbackFont());
                    cell2.SetWidth(cell.GetWidth());
                    cell2.SetLeftPadding(cell.leftPadding);
                    cell2.SetRightPadding(cell.rightPadding);
                    cell2.SetLineWidth(cell.lineWidth);
                    cell2.SetBgColor(cell.GetBgColor());
                    cell2.SetPenColor(cell.GetPenColor());
                    cell2.SetBrushColor(cell.GetBrushColor());
                    cell2.SetProperties(cell.GetProperties());
                    cell2.SetVerTextAlignment(cell.GetVerTextAlignment());
                    cell2.SetTopPadding(0f);
                    cell2.SetBorder(Border.TOP, false);
                    row2.Add(cell2);
                }
                tableData2.Add(row2);
            }
        }
        return tableData2;
    }

    private float GetTotalWidth(List<Cell> row, int index) {
        Cell cell = row[index];
        int colspan = (int) cell.GetColSpan();
        float cellWidth = 0f;
        for (int i = 0; i < colspan; i++) {
            cellWidth += row[index + i].GetWidth();
        }
        cellWidth -= (cell.leftPadding + row[index + (colspan - 1)].rightPadding);
        return cellWidth;
    }

    /**
     *  Wraps around the text in all cells so it fits the column width.
     *  This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
     */
    protected void WrapAroundCellText() {
        List<List<Cell>> tableData2 = AddExtraTableRows();
        for (int i = 0; i < tableData2.Count; i++) {
            List<Cell> row = tableData2[i];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                if (cell.text != null) {
                    float cellWidth = GetTotalWidth(row, j);
                    String[] tokens = Regex.Split(cell.text, "\\s+");
                    int n = 0;
                    StringBuilder buf = new StringBuilder();
                    foreach (String token in tokens) {
                        if (cell.font.StringWidth(cell.fallbackFont, token) > cellWidth) {
                            if (buf.Length > 0) {
                                buf.Append(" ");
                            }
                            for (int k = 0; k < token.Length; k++) {
                                if (cell.font.StringWidth(cell.fallbackFont, buf.ToString() + token[k]) > cellWidth) {
                                    tableData2[i + n][j].SetText(buf.ToString());
                                    buf.Length = 0;
                                    n++;
                                }
                                buf.Append(token[k]);
                            }
                        } else {
                            if (cell.font.StringWidth(cell.fallbackFont, (buf.ToString() + " " + token).Trim()) > cellWidth) {
                                tableData2[i + n][j].SetText(buf.ToString().Trim());
                                buf.Clear();
                                buf.Append(token);
                                n++;
                            } else {
                                if (buf.Length > 0) {
                                    buf.Append(" ");
                                }
                                buf.Append(token);
                            }
                        }
                    }
                    tableData2[i + n][j].SetText(buf.ToString().Trim());
                }
            }
        }
        tableData = tableData2;
    }

    /**
     *  Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
     *
     *  @return the number of vertical cells needed to wrap around the cell text.
     */
    public int GetNumVerCells(List<Cell> row, int index) {
        Cell cell = row[index];
        int numOfVerCells = 1;
        if (cell.text == null) {
            return numOfVerCells;
        }
        float cellWidth = GetTotalWidth(row, index);
        String[] tokens = Regex.Split(cell.text, "\\s+");
        StringBuilder buf = new StringBuilder();
        foreach (String token in tokens) {
            if (cell.font.StringWidth(cell.fallbackFont, token) > cellWidth) {
                if (buf.Length > 0) {
                    buf.Append(" ");
                }
                for (int k = 0; k < token.Length; k++) {
                    if (cell.font.StringWidth(cell.fallbackFont, (buf.ToString() + " " + token[k]).Trim()) > cellWidth) {
                        numOfVerCells++;
                        buf.Length = 0;
                    }
                    buf.Append(token[k]);
                }
            } else {
                if (cell.font.StringWidth(cell.fallbackFont, (buf.ToString() + " " + token).Trim()) > cellWidth) {
                    numOfVerCells++;
                    buf.Length = 0;
                    buf.Append(token);
                } else {
                    if (buf.Length > 0) {
                        buf.Append(" ");
                    }
                    buf.Append(token);
                }
            }
        }
        return numOfVerCells;
    }

    private Char[] GetDelimiterRegex(String str) {
        int comma = 0;
        int pipe = 0;
        int tab = 0;
        foreach (char ch in str) {
            if (ch == ',') {
                comma++;
            } else if (ch == '|') {
                pipe++;
            } else if (ch == '\t') {
                tab++;
            }
        }
        if (comma >= pipe) {
            if (comma >= tab) {
                return new Char[] {','};
            }
            return new Char[] {'\t'};
        } else {
            if (pipe >= tab) {
                return new Char[] {'|'};
            }
            return new Char[] {'\t'};
        }
    }

    public void SetVisibleColumns(params int[] columns) {
        List<List<Cell>> list = new List<List<Cell>>();
        List<int> visible = new List<int>(columns);
        foreach (List<Cell> row in tableData) {
            List<Cell> row2 = new List<Cell>();
            for (int i = 0; i < row.Count; i++) {
                if (visible.Contains(i)) {
                    row2.Add(row[i]);
                }
            }
            list.Add(row2);
        }
        tableData = list;
    }

    public void SetLocationFirstPage(float x, float y) {
        this.x1FirstPage = x;
        this.y1FirstPage = y;
    }
}   // End of Table.cs
}   // End of namespace PDFjet.NET
