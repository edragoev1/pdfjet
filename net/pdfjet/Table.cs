/*
 * Table.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Used to create table objects and draw them on a page.
///
/// Please see Example_08.
/// </summary>
public class Table : IDrawable {
    /// <summary>The table has no header rows.</summary>
    public static readonly int WITH_0_HEADER_ROWS = 0;
    /// <summary>The table has 1 header row.</summary>
    public static readonly int WITH_1_HEADER_ROW  = 1;
    /// <summary>The table has 2 header rows.</summary>
    public static readonly int WITH_2_HEADER_ROWS = 2;
    /// <summary>The table has 3 header rows.</summary>
    public static readonly int WITH_3_HEADER_ROWS = 3;
    /// <summary>The table has 4 header rows.</summary>
    public static readonly int WITH_4_HEADER_ROWS = 4;
    /// <summary>The table has 5 header rows.</summary>
    public static readonly int WITH_5_HEADER_ROWS = 5;
    /// <summary>The table has 6 header rows.</summary>
    public static readonly int WITH_6_HEADER_ROWS = 6;
    /// <summary>The table has 7 header rows.</summary>
    public static readonly int WITH_7_HEADER_ROWS = 7;
    /// <summary>The table has 8 header rows.</summary>
    public static readonly int WITH_8_HEADER_ROWS = 8;
    /// <summary>The table has 9 header rows.</summary>
    public static readonly int WITH_9_HEADER_ROWS = 9;

    private List<List<Cell>> tableData;
    private int numOfHeaderRows = 1;
    // The index of the next row to draw, or -1 when all rows are drawn.
    private int rendered = 1;
    private float x1;
    private float y1;
    private float firstPageTopMargin;
    private float bottomMargin;

    /// <summary>
    /// Create a table object.
    /// </summary>
    public Table() {
        tableData = new List<List<Cell>>();
    }

    /// <summary>
    /// Creates a table from a text file with comma, pipe or tab separated values.
    /// The first line is the header row and uses f1; the other lines use f2.
    /// Every row gets as many cells as the first line has fields.
    /// </summary>
    /// <param name="f1">the font for the header row.</param>
    /// <param name="f2">the font for the other rows.</param>
    /// <param name="fileName">the file name.</param>
    public Table(Font f1, Font f2, String fileName) {
        tableData = new List<List<Cell>>();
        using (StreamReader reader = new StreamReader(fileName)) {
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
                        row.Add(new Cell(f1, field));
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
                        row.Add(new Cell(f2, ""));
                    }
                    tableData.Add(row);
                } else {
                    tableData.Add(row);
                }
                lineNumber++;
            }
        }
    }

    /// <summary>
    /// Sets the location (x, y) of the top left corner of this table on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left point of the table.</param>
    /// <param name="y">the y coordinate of the top left point of the table.</param>
    /// <returns>this Table object.</returns>
    public Table SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
    }

    /// <summary>
    /// Sets the location (x, y) of the top left corner of this table on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left point of the table.</param>
    /// <param name="y">the y coordinate of the top left point of the table.</param>
    public Table SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the bottom margin for this table.
    /// </summary>
    /// <param name="bottomMargin">the margin.</param>
    /// <returns>this Table object.</returns>
    public Table SetBottomMargin(double bottomMargin) {
        this.bottomMargin = (float) bottomMargin;
        return this;
    }

    /// <summary>
    /// Sets the bottom margin for this table.
    /// </summary>
    /// <param name="bottomMargin">the margin.</param>
    /// <returns>this Table object.</returns>
    public Table SetBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
        return this;
    }

    /// <summary>
    /// Sets the table data.
    /// </summary>
    /// <param name="tableData">the table data.</param>
    /// <returns>this Table object.</returns>
    public Table SetData(List<List<Cell>> tableData) {
        return SetData(tableData, 0);
    }

    /// <summary>
    /// Sets the table data and specifies the number of header rows in this data.
    /// The header rows are drawn again at the top of every page.
    /// </summary>
    /// <param name="tableData">the table data.</param>
    /// <param name="numOfHeaderRows">the number of header rows in this data.</param>
    /// <returns>this Table object.</returns>
    public Table SetData(List<List<Cell>> tableData, int numOfHeaderRows) {
        this.tableData = tableData;
        this.numOfHeaderRows = numOfHeaderRows;
        this.rendered = numOfHeaderRows;
        AddCellsToCompleteTheGrid();
        return this;
    }

    // Adds empty cells to the rows that are shorter than the first row.
    private void AddCellsToCompleteTheGrid() {
        int numOfColumns = tableData[0].Count;
        Font font = tableData[0][0].font;
        foreach (List<Cell> row in tableData) {
            int diff = numOfColumns - row.Count;
            for (int i = 0; i < diff; i++) {
                row.Add(new Cell(font, ""));
            }
        }
    }

    /// <summary>
    /// Aligns to the right the cells whose text is a number, such as 1,234.50,
    /// (1,234.50), -5 or 1.5E+3. The periods, commas and apostrophes are ignored,
    /// and the number can be in parentheses.
    /// </summary>
    public void RightAlignNumbers() {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                if (cell.text != null && IsNumber(cell.text)) {
                    cell.SetTextAlignment(Align.RIGHT);
                }
            }
        }
    }

    // Returns true if the text, without its periods, commas and apostrophes and
    // the parentheses around it, is an optional sign, ASCII digits and an
    // optional exponent, with optional spaces before and after.
    private static bool IsNumber(String text) {
        String str = text;
        if (str.Length >= 2 && str[0] == '(' && str[str.Length - 1] == ')') {
            str = str.Substring(1, str.Length - 2);
        }
        StringBuilder buf = new StringBuilder();
        foreach (char ch in str) {
            if (ch != '.' && ch != ',' && ch != '\'') {
                buf.Append(ch);
            }
        }
        String number = Util.Trim(buf.ToString());
        int i = 0;
        if (i < number.Length && (number[i] == '+' || number[i] == '-')) {
            i++;
        }
        int start = i;
        while (i < number.Length && number[i] >= '0' && number[i] <= '9') {
            i++;
        }
        if (i == start) {
            return false;
        }
        if (i < number.Length && (number[i] == 'e' || number[i] == 'E')) {
            i++;
            if (i < number.Length && (number[i] == '+' || number[i] == '-')) {
                i++;
            }
            start = i;
            while (i < number.Length && number[i] >= '0' && number[i] <= '9') {
                i++;
            }
            if (i == start) {
                return false;
            }
        }
        return i == number.Length;
    }

    /// <summary>
    /// Removes the horizontal lines between the rows from index1 to index2.
    /// </summary>
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

    /// <summary>
    /// Sets the text alignment in the specified column.
    /// </summary>
    /// <param name="index">the index of the specified column.</param>
    /// <param name="alignment">the specified alignment. Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.</param>
    /// <returns>this Table object.</returns>
    public Table SetTextAlignInColumn(int index, uint alignment) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.SetTextAlignment(alignment);
                if (cell.textBox != null) {
                    cell.textBox.SetTextAlignment(alignment);
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the color of the text in the specified column.
    /// </summary>
    /// <param name="index">the index of the specified column.</param>
    /// <param name="color">the color specified as an integer.</param>
    /// <returns>this Table object.</returns>
    public Table SetTextColorInColumn(int index, int color) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.SetTextColor(color);
                if (cell.textBox != null) {
                    cell.textBox.SetTextColor(color);
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the font and the font size of the cells in the specified column.
    /// </summary>
    /// <param name="index">the column index.</param>
    /// <param name="font">the font.</param>
    /// <returns>this Table object.</returns>
    public Table SetFontInColumn(int index, Font font) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.SetFont(font).SetFontSize(font.GetSize());
                if (cell.textBox != null) {
                    cell.textBox.font = font;
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the color of the text in the specified row.
    /// </summary>
    /// <param name="index">the index of the specified row.</param>
    /// <param name="color">the color specified as an integer.</param>
    /// <returns>this Table object.</returns>
    public Table SetTextColorInRow(int index, int color) {
        if (index < tableData.Count) {
            List<Cell> row = tableData[index];
            foreach (Cell cell in row) {
                cell.SetTextColor(color);
                if (cell.textBox != null) {
                    cell.textBox.SetTextColor(color);
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the font and the font size of the cells in the specified row.
    /// </summary>
    /// <param name="index">the row index.</param>
    /// <param name="font">the font.</param>
    /// <returns>this Table object.</returns>
    public Table SetFontInRow(int index, Font font) {
        if (index < tableData.Count) {
            List<Cell> row = tableData[index];
            foreach (Cell cell in row) {
                cell.SetFont(font).SetFontSize(font.GetSize());
                if (cell.textBox != null) {
                    cell.textBox.font = font;
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the width of the column with the specified index.
    /// </summary>
    /// <param name="index">the index of specified column.</param>
    /// <param name="width">the specified width.</param>
    /// <returns>this Table object.</returns>
    public Table SetColumnWidth(int index, double width) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                row[index].SetWidth(width);
            }
        }
        return this;
    }

    /// <summary>
    /// Returns the column width of the column at the specified index.
    /// </summary>
    /// <param name="index">the index of the column.</param>
    /// <returns>the width of the column.</returns>
    public float GetColumnWidth(int index) {
        return GetCellAtRowColumn(0, index).GetWidth();
    }

    /// <summary>
    /// Returns the cell at the specified row and column.
    /// </summary>
    /// <param name="row">the specified row.</param>
    /// <param name="col">the specified column.</param>
    /// <returns>the cell at the specified row and column.</returns>
    public Cell GetCellAt(int row, int col) {
        if (row >= 0) {
            return tableData[row][col];
        }
        return tableData[tableData.Count + row][col];
    }

    /// <summary>
    /// Returns the cell at the specified row and column.
    /// </summary>
    /// <param name="row">the specified row.</param>
    /// <param name="col">the specified column.</param>
    /// <returns>the cell at the specified row and column.</returns>
    public Cell GetCellAtRowColumn(int row, int col) {
        return GetCellAt(row, col);
    }

    /// <summary>
    /// Returns a list of cells for the specified row.
    /// </summary>
    /// <param name="index">the index of the specified row.</param>
    /// <returns>the list of cells.</returns>
    public List<Cell> GetRow(int index) {
        return tableData[index];
    }

    /// <summary>Returns the cells in the specified row. Same as GetRow.</summary>
    public List<Cell> GetRowAtIndex(int index) {
        return GetRow(index);
    }

    /// <summary>
    /// Returns a list of cells for the specified column.
    /// </summary>
    /// <param name="index">the index of the specified column.</param>
    /// <returns>the list of cells.</returns>
    public List<Cell> GetColumn(int index) {
        List<Cell> column = new List<Cell>();
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                column.Add(row[index]);
            }
        }
        return column;
    }

    /// <summary>Returns the cells in the specified column. Same as GetColumn.</summary>
    public List<Cell> GetColumnAtIndex(int index) {
        return GetColumn(index);
    }

    /// <summary>
    /// Draws this table on the specified page.
    /// </summary>
    /// <param name="page">the page to draw this table on.</param>
    /// <returns>Point the point on the page where to draw the next component.</returns>
    public float[] DrawOn(Page page) {
        WrapAroundCellText();
        SetRightBorderOnLastColumn();
        SetBottomBorderOnLastRow();
        return DrawTableRows(page, DrawHeaderRows(page, 0));
    }

    /// <summary>
    /// Draws this table on as many new pages as it needs.
    /// The pages are created detached and added to the list; add them to the PDF afterwards.
    /// </summary>
    /// <param name="pdf">the PDF document.</param>
    /// <param name="pages">the list that receives the new pages.</param>
    /// <param name="pageSize">the page size, for example Letter.PORTRAIT.</param>
    /// <returns>the x and y coordinates below the table on the last page.</returns>
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
        if (pageNumber == 1 && firstPageTopMargin > 0f) {
            y = firstPageTopMargin;
        }
        for (int i = 0; i < numOfHeaderRows; i++) {
            List<Cell> row = tableData[i];
            float h = GetMaxCellHeight(row);
            int j = 0;
            while (j < row.Count) {
                Cell cell = row[j];
                int colspan = cell.GetColSpan();
                float w = 0f;
                for (int k = 0; k < colspan; k++) {
                    w += row[j++].GetWidth();
                }
                if (page != null) {
                    page.SetBrushColor(cell.GetTextColor());
                    if (i == (numOfHeaderRows - 1)) {
                        cell.SetBorder(Border.BOTTOM, true);
                    }
                    cell.DrawOn(page, x, y, w, h);
                }
                x += w;
            }
            x = x1;
            y += h;
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
            int i = 0;
            while (i < row.Count) {
                Cell cell = row[i];
                int colspan = cell.GetColSpan();
                float w = 0f;
                for (int j = 0; j < colspan; j++) {
                    w += row[i++].GetWidth();
                }
                if (page != null) {
                    page.SetBrushColor(cell.GetTextColor());
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

    /// <summary>
    /// Returns true if the table contains more data that needs to be drawn on a page.
    /// </summary>
    private bool HasMoreData() {
        return rendered != -1;
    }

    /// <summary>
    /// Returns the width of this table when drawn on a page.
    /// </summary>
    /// <returns>the width of this table.</returns>
    public float GetWidth() {
        float tableWidth = 0f;
        List<Cell> row = tableData[0];
        foreach (Cell cell in row) {
            tableWidth += cell.GetWidth();
        }
        return tableWidth;
    }

    /// <summary>
    /// Returns the number of rows below the header rows that are drawn so far,
    /// counting each line of wrapped cell text as a row, or -1 when all rows
    /// are drawn.
    /// </summary>
    /// <returns>the number of rendered rows.</returns>
    public int GetRowsRendered() {
        return rendered == -1 ? rendered : rendered - numOfHeaderRows;
    }

    /// <summary>
    /// Sets all table cells borders.
    /// </summary>
    /// <param name="borders">true or false.</param>
    /// <returns>this Table object.</returns>
    public Table SetCellBorders(bool borders) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetBorders(borders);
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the color of the cell border lines.
    /// </summary>
    /// <param name="color">the color of the cell border lines.</param>
    /// <returns>this Table object.</returns>
    public Table SetCellBordersColor(int color) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetStrokeColor(color);
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the width of the cell border lines.
    /// </summary>
    /// <param name="width">the width of the cell border lines.</param>
    /// <returns>this Table object.</returns>
    public Table SetCellBordersWidth(float width) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetStrokeWidth(width);
            }
        }
        return this;
    }

    // Sets the right border on all cells in the last column.
    private void SetRightBorderOnLastColumn() {
        foreach (List<Cell> row in tableData) {
            if (row[0].GetBorder(Border.LEFT) == false) {
                return;
            }
        }
        // Only run this code if all the cells in the first column have left border.
        foreach (List<Cell> row in tableData) {
            Cell cell = null;
            int i = 0;
            while (i < row.Count) {
                cell = row[i];
                i += cell.GetColSpan();
            }
            cell.SetBorder(Border.RIGHT, true);
        }
    }

    // Sets the bottom border on all cells in the last row.
    private void SetBottomBorderOnLastRow() {
        List<Cell> firstRow = tableData[0];
        foreach (Cell cell in firstRow) {
            if (cell.GetBorder(Border.TOP) == false) {
                return;
            }
        }
        // Only run this code if all the cells in the first row have top border.
        List<Cell> lastRow = tableData[tableData.Count - 1];
        foreach (Cell cell in lastRow) {
            cell.SetBorder(Border.BOTTOM, true);
        }
    }

    /// <summary>
    /// Auto adjusts the widths of all columns so that they are just wide enough to
    /// hold the text without truncation.
    /// </summary>
    /// <returns>this Table object.</returns>
    public Table SetColumnWidths() {
        float[] maxColWidths = new float[tableData[0].Count];
        foreach (List<Cell> row in tableData) {
            for (int i = 0; i < row.Count; i++) {
                Cell cell = row[i];
                if (cell.GetColSpan() == 1) {
                    if (cell.textBox != null) {
                        String[] tokens = Util.SplitOnWhitespace(cell.textBox.text);
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
        return this;
    }

    // Returns the table data with a row added below each row for every extra
    // line that its wrapped cell text needs. The rows added below a header row
    // are header rows too.
    private List<List<Cell>> AddExtraTableRows() {
        List<List<Cell>> tableData2 = new List<List<Cell>>();
        int numOfHeaderRows2 = 0;
        for (int r = 0; r < tableData.Count; r++) {
            List<Cell> row = tableData[r];
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
                    cell2.SetBackgroundColor(cell.GetBackgroundColor());
                    cell2.SetStrokeWidth(cell.GetStrokeWidth());
                    cell2.SetStrokeColor(cell.GetStrokeColor());
                    cell2.SetTextColor(cell.GetTextColor());
                    // The borders, text alignment, underline and strikeout.
                    cell2.SetProperties(cell.GetProperties());
                    cell2.SetColSpan(cell.GetColSpan());
                    cell2.SetVerTextAlignment(cell.GetVerTextAlignment());
                    cell2.SetTopPadding(0f);
                    cell2.SetBorder(Border.TOP, false);
                    row2.Add(cell2);
                }
                tableData2.Add(row2);
            }
            if (r < numOfHeaderRows) {
                numOfHeaderRows2 = tableData2.Count;
            }
        }
        if (rendered != -1) {
            rendered += numOfHeaderRows2 - numOfHeaderRows;
        }
        numOfHeaderRows = numOfHeaderRows2;
        return tableData2;
    }

    private float GetTotalWidth(List<Cell> row, int index) {
        Cell cell = row[index];
        int colspan = cell.GetColSpan();
        float cellWidth = 0f;
        for (int i = 0; i < colspan; i++) {
            cellWidth += row[index + i].GetWidth();
        }
        cellWidth -= (cell.leftPadding + row[index + (colspan - 1)].rightPadding);
        return cellWidth;
    }

    /// <summary>
    /// Wraps around the text in all cells so it fits the column width.
    /// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
    /// </summary>
    protected void WrapAroundCellText() {
        List<List<Cell>> tableData2 = AddExtraTableRows();
        for (int i = 0; i < tableData2.Count; i++) {
            List<Cell> row = tableData2[i];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                if (cell.text != null) {
                    float cellWidth = GetTotalWidth(row, j);
                    String[] tokens = Util.SplitOnWhitespace(cell.text);
                    int n = 0;
                    StringBuilder buf = new StringBuilder();
                    foreach (String token in tokens) {
                        if (cell.font.StringWidth(cell.fallbackFont, token) > cellWidth) {
                            if (buf.Length > 0) {
                                buf.Append(" ");
                            }
                            foreach (char ch in token) {
                                if (cell.font.StringWidth(cell.fallbackFont, buf.ToString() + ch) > cellWidth) {
                                    tableData2[i + n][j].SetText(buf.ToString());
                                    buf.Length = 0;
                                    n++;
                                }
                                buf.Append(ch);
                            }
                        } else {
                            if (cell.font.StringWidth(cell.fallbackFont, Util.Trim(buf.ToString() + " " + token)) > cellWidth) {
                                tableData2[i + n][j].SetText(Util.Trim(buf.ToString()));
                                buf.Length = 0;
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
                    tableData2[i + n][j].SetText(Util.Trim(buf.ToString()));
                }
            }
        }
        tableData = tableData2;
    }

    /// <summary>
    /// Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
    /// </summary>
    /// <returns>the number of vertical cells needed to wrap around the cell text.</returns>
    internal int GetNumVerCells(List<Cell> row, int index) {
        Cell cell = row[index];
        int numOfVerCells = 1;
        if (cell.text == null) {
            return numOfVerCells;
        }
        float cellWidth = GetTotalWidth(row, index);
        String[] tokens = Util.SplitOnWhitespace(cell.text);
        StringBuilder buf = new StringBuilder();
        foreach (String token in tokens) {
            if (cell.font.StringWidth(cell.fallbackFont, token) > cellWidth) {
                if (buf.Length > 0) {
                    buf.Append(" ");
                }
                foreach (char ch in token) {
                    if (cell.font.StringWidth(cell.fallbackFont, buf.ToString() + ch) > cellWidth) {
                        numOfVerCells++;
                        buf.Length = 0;
                    }
                    buf.Append(ch);
                }
            } else {
                if (cell.font.StringWidth(cell.fallbackFont, Util.Trim(buf.ToString() + " " + token)) > cellWidth) {
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

    /// <summary>Keeps only the columns with the specified indexes.</summary>
    public Table SetVisibleColumns(params int[] columns) {
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
        return this;
    }

    /// <summary>Sets the top margin on the first page when the table spans several pages.</summary>
    public Table SetFirstPageTopMargin(float firstPageTopMargin) {
        this.firstPageTopMargin = firstPageTopMargin;
        return this;
    }
}   // End of Table.cs
}   // End of namespace PDFjet.NET
