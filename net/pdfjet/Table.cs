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
    private List<List<Cell>> tableData;
    private int numOfHeaderRows = 1;
    private int numOfFooterRows = 0;
    // The index of the next row to draw, or -1 when all rows are drawn.
    private int rendered = 1;
    private float x1;
    private float y1;
    private float firstPageTopMargin;
    private float bottomMargin;
    // The Table element of a PDF/UA document, while the table is drawn, and
    // the TH or TD elements of the last row, by column, that the rows with
    // the next lines of its wrapped text add to.
    private StructElement structElement;
    private StructElement[] cellElements;

    /// <summary>
    /// Create a table object.
    /// </summary>
    public Table() {
        tableData = new List<List<Cell>>();
    }

    /// <summary>
    /// Creates a table from a text file with comma, pipe or tab separated values.
    /// The first line is the header row and uses f1; the other lines use f2.
    /// Every row gets as many cells as the first line has fields. A quoted field is read as
    /// RFC 4180 reads it, and a line break inside one goes on to the next line of the file and
    /// is drawn as a space.
    /// </summary>
    /// <param name="f1">the font for the header row.</param>
    /// <param name="f2">the font for the other rows.</param>
    /// <param name="fileName">the file name.</param>
    public Table(Font f1, Font f2, String fileName) {
        tableData = new List<List<Cell>>();
        // UTF-8 only, as in the other ports: the reader does not look for UTF-16
        // and UTF-32 byte order marks, and keeps a UTF-8 one.
        using (StreamReader reader = new StreamReader(fileName, new UTF8Encoding(false), false)) {
            String delimiter = null;
            int numberOfFields = 0;
            int lineNumber = 0;
            String line;
            while ((line = reader.ReadLine()) != null) {
                if (lineNumber == 0) {
                    // A byte order mark at the start of the file is not part of the text.
                    if (line.StartsWith("\uFEFF", StringComparison.Ordinal)) {
                        line = line.Substring(1);
                    }
                    delimiter = GetDelimiter(line);
                }
                List<Cell> row = new List<Cell>();
                // The empty fields at the end of the line are kept, a quoted field
                // holds its delimiters instead of being cut at them, and its line
                // breaks, which go on to the next lines, are spaces.
                String[] fields = Util.ReadRecord(line, reader, delimiter);
                if (lineNumber == 0) {
                    numberOfFields = fields.Length;
                }
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
    public Table SetBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
        return this;
    }

    /// <summary>
    /// Sets the table data.
    /// </summary>
    /// <param name="tableData">the table data.</param>
    /// <returns>this Table object.</returns>
    public Table SetTableData(List<List<Cell>> tableData) {
        return SetTableData(tableData, 0);
    }

    /// <summary>
    /// Sets the table data and specifies the number of header rows in this data.
    /// The header rows are drawn again at the top of every page.
    /// </summary>
    /// <param name="tableData">the table data.</param>
    /// <param name="numOfHeaderRows">the number of header rows in this data.</param>
    /// <returns>this Table object.</returns>
    public Table SetTableData(List<List<Cell>> tableData, int numOfHeaderRows) {
        this.tableData = tableData;
        this.numOfHeaderRows = numOfHeaderRows;
        this.rendered = numOfHeaderRows;
        AddCellsToCompleteTheGrid();
        return this;
    }

    /// <summary>
    /// Makes the last rows of the table data footer rows, which are drawn again
    /// at the end of the table on every page, under the last row the page
    /// holds, as the header rows are drawn again at the top. A page leaves room
    /// for them. In a PDF/UA document they are rows of the table once, where
    /// the table ends, and artifacts on the other pages.
    /// </summary>
    /// <param name="numOfFooterRows">the number of footer rows at the end of the data.</param>
    /// <returns>this Table object.</returns>
    public Table SetNumberOfFooterRows(int numOfFooterRows) {
        this.numOfFooterRows = Math.Max(0, numOfFooterRows);
        return this;
    }

    // The index of the first footer row, after the header rows and the rows
    // of the table.
    private int FooterStart() {
        return Math.Max(numOfHeaderRows, tableData.Count - numOfFooterRows);
    }

    // Adds empty cells to the rows that are shorter than the first row.
    private void AddCellsToCompleteTheGrid() {
        if (tableData.Count == 0 || tableData[0].Count == 0) {
            return;
        }
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
    public Table RightAlignNumbers() {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                if (cell.text != null && IsNumber(cell.text)) {
                    cell.SetTextAlignment(Alignment.RIGHT);
                }
            }
        }
        return this;
    }

    // Returns true if the text, without its periods, commas and apostrophes and
    // the parentheses around it, is an optional sign, ASCII digits and an
    // optional exponent, with optional spaces before and after.
    internal static bool IsNumber(String text) {
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
    public Table RemoveLineBetweenRows(int index1, int index2) {
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
        return this;
    }

    /// <summary>
    /// Makes the cell of a footer row show the sum of its column over the rows
    /// of each page: the page total. The numbers are read as RightAlignNumbers
    /// reads them, with commas and apostrophes between the thousands and a
    /// period before the decimals, and a cell that has no number is left out.
    /// The sum has the number of decimals, rounded half away from zero, and
    /// commas between the thousands. Until the table is drawn the cell has the
    /// sum of all the rows, so that AutoAdjustColumnWidths leaves room for it;
    /// its text is not wrapped.
    /// </summary>
    /// <param name="row">the index of a footer row, see SetNumberOfFooterRows.</param>
    /// <param name="column">the index of the column.</param>
    /// <param name="decimals">the number of decimals, from 0 to 9.</param>
    /// <returns>this Table object.</returns>
    public Table SetPageSum(int row, int column, int decimals) {
        return SetSum(row, column, decimals, Cell.PAGE_SUM);
    }

    /// <summary>
    /// Makes the cell of a footer row show the sum of its column over all the
    /// rows up to the end of each page: the total carried forward, which is
    /// the total of the table on its last page. The numbers are read and the
    /// sum is written as SetPageSum reads and writes them.
    /// </summary>
    /// <param name="row">the index of a footer row, see SetNumberOfFooterRows.</param>
    /// <param name="column">the index of the column.</param>
    /// <param name="decimals">the number of decimals, from 0 to 9.</param>
    /// <returns>this Table object.</returns>
    public Table SetRunningSum(int row, int column, int decimals) {
        return SetSum(row, column, decimals, Cell.RUNNING_SUM);
    }

    /// <summary>
    /// Makes the cell of a header row show the sum of its column over all the
    /// rows of the pages before: the total brought forward, which the cell that
    /// SetRunningSum marks shows at the end of the page before. A header row
    /// with such a cell is drawn from the second page on, under the header
    /// rows above it, and not on the first page, which has nothing to bring
    /// forward; in a PDF/UA document it is an artifact, as the header rows are
    /// on those pages. The numbers are read and the sum is written as
    /// SetPageSum reads and writes them.
    /// </summary>
    /// <param name="row">the index of a header row, see SetTableData.</param>
    /// <param name="column">the index of the column.</param>
    /// <param name="decimals">the number of decimals, from 0 to 9.</param>
    /// <returns>this Table object.</returns>
    public Table SetBroughtForwardSum(int row, int column, int decimals) {
        return SetSum(row, column, decimals, Cell.BROUGHT_FORWARD_SUM);
    }

    private Table SetSum(int row, int column, int decimals, uint kind) {
        if (row < 0 || row >= tableData.Count || column < 0 || column >= tableData[row].Count) {
            return this;
        }
        int places = Math.Max(0, Math.Min(9, decimals));
        Cell cell = tableData[row][column];
        cell.properties = (cell.properties & ~Cell.SUM_BITS) | kind | ((uint) places << Cell.SUM_DECIMALS);
        int end = (kind == Cell.BROUGHT_FORWARD_SUM) ? FooterStart() : Math.Min(row, FooterStart());
        cell.SetText(FormatSum(SumOf(column, numOfHeaderRows, end, places), places));
        return this;
    }

    // Writes the sums in the cells of the rows from start to stop that
    // SetPageSum, SetRunningSum and SetBroughtForwardSum mark: those of the
    // rows of the page, from first to end, those of all the rows to end, and
    // those of all the rows before first.
    private void SetSums(int start, int stop, int first, int end) {
        for (int r = start; r < stop && r < tableData.Count; r++) {
            List<Cell> row = tableData[r];
            for (int j = 0; j < row.Count; j++) {
                Cell cell = row[j];
                uint kind = cell.properties & Cell.SUM_KIND;
                if (kind != 0) {
                    int places = (int) ((cell.properties >> Cell.SUM_DECIMALS) & 0xF);
                    int from = (kind == Cell.PAGE_SUM) ? first : numOfHeaderRows;
                    int to = (kind == Cell.BROUGHT_FORWARD_SUM) ? first : end;
                    cell.SetText(FormatSum(SumOf(j, from, to, places), places));
                }
            }
        }
    }

    private void SetFooterSums(int first, int end) {
        SetSums(FooterStart(), tableData.Count, first, end);
    }

    // True when the row, or the row whose wrapped text it holds, has a cell
    // that brings a total forward, which the first page does not draw.
    private bool IsBroughtForwardRow(int r) {
        while (r > 0 && IsContinuation(r)) {
            r--;
        }
        foreach (Cell cell in tableData[r]) {
            if ((cell.properties & Cell.SUM_KIND) == Cell.BROUGHT_FORWARD_SUM) {
                return true;
            }
        }
        return false;
    }

    // The largest sum, and number, in units of the decimals: 18 digits.
    private const long MAX_SUM = 999999999999999999L;

    // The sum of the numbers in the column of the rows from first to end, in
    // units of the decimals. A number that would take the sum past 18 digits
    // is left out.
    private long SumOf(int column, int first, int end, int decimals) {
        long sum = 0L;
        for (int r = Math.Max(0, first); r < end && r < tableData.Count; r++) {
            List<Cell> row = tableData[r];
            if (column >= row.Count) {
                continue;
            }
            Cell cell = row[column];
            if ((cell.properties & Cell.COVERED) != 0 || cell.text == null) {
                continue;
            }
            long? value = NumberOf(cell.text, decimals);
            if (value != null && Math.Abs(sum + value.Value) <= MAX_SUM) {
                sum += value.Value;
            }
        }
        return sum;
    }

    /// <summary>
    /// Returns the number the text is, in units of the decimals, rounded half
    /// away from zero, or null when the text is not a number, as IsNumber
    /// reads it, has more than one period, or is more than 18 digits in those
    /// units. Commas and apostrophes are between the thousands, a period is
    /// before the decimals, and parentheses make a number negative.
    /// </summary>
    internal static long? NumberOf(String text, int decimals) {
        if (!IsNumber(text)) {
            return null;
        }
        String str = text;
        bool negative = false;
        if (str.Length >= 2 && str[0] == '(' && str[str.Length - 1] == ')') {
            str = str.Substring(1, str.Length - 2);
            negative = true;
        }
        str = str.Replace(",", "").Replace("'", "").Trim();
        if (str[0] == '+' || str[0] == '-') {
            negative ^= (str[0] == '-');
            str = str.Substring(1);
        }
        int exponent = 0;
        int e = str.IndexOfAny(new char[] {'e', 'E'});
        if (e != -1) {
            String exp = str.Substring(e + 1);
            int sign = 1;
            if (exp[0] == '+' || exp[0] == '-') {
                sign = (exp[0] == '-') ? -1 : 1;
                exp = exp.Substring(1);
            }
            if (exp.Length > 4 || exp.Contains('.')) {
                return null;
            }
            exponent = sign * int.Parse(exp, System.Globalization.CultureInfo.InvariantCulture);
            str = str.Substring(0, e);
        }
        int point = str.IndexOf('.');
        if (point != str.LastIndexOf('.')) {
            return null;
        }
        String fraction = (point == -1) ? "" : str.Substring(point + 1);
        String digits = ((point == -1) ? str : str.Substring(0, point) + fraction).TrimStart('0');
        if (digits.Length == 0) {
            return 0L;
        }
        // The number is the digits times 10 to the shift, in units of the
        // decimals.
        int shift = exponent - fraction.Length + decimals;
        long value;
        if (shift >= 0) {
            if (digits.Length + shift > 18) {
                return null;
            }
            value = long.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < shift; i++) {
                value *= 10;
            }
        } else {
            int keep = digits.Length + shift;
            if (keep < 0) {
                return 0L;
            }
            if (keep > 18) {
                return null;
            }
            value = (keep == 0) ? 0L : long.Parse(digits.Substring(0, keep), System.Globalization.CultureInfo.InvariantCulture);
            if (digits[keep] >= '5') {
                value++;
            }
            if (value > MAX_SUM) {
                return null;
            }
        }
        return negative ? -value : value;
    }

    /// <summary>
    /// Returns the sum in units of the decimals as text: the decimals after a
    /// period, and commas between the thousands.
    /// </summary>
    internal static String FormatSum(long sum, int decimals) {
        String digits = Math.Abs(sum).ToString(System.Globalization.CultureInfo.InvariantCulture);
        while (digits.Length <= decimals) {
            digits = "0" + digits;
        }
        int point = digits.Length - decimals;
        StringBuilder text = new StringBuilder();
        if (sum < 0) {
            text.Append('-');
        }
        for (int i = 0; i < point; i++) {
            if (i > 0 && (point - i) % 3 == 0) {
                text.Append(',');
            }
            text.Append(digits[i]);
        }
        if (decimals > 0) {
            text.Append('.').Append(digits, point, digits.Length - point);
        }
        return text.ToString();
    }

    /// <summary>
    /// Keeps the row with the specified index on the same page as the next
    /// row, as a heading row is kept with the rows under it: a page break does
    /// not fall between them, and moves both to the next page. Rows kept with
    /// the next one one after another are kept together, unless together they
    /// are taller than a page. Call it before the table is drawn.
    /// </summary>
    /// <param name="index">the index of the row.</param>
    /// <returns>this Table object.</returns>
    public Table KeepRowWithNext(int index) {
        if (index >= 0 && index < tableData.Count) {
            foreach (Cell cell in tableData[index]) {
                cell.properties |= Cell.KEPT_WITH_NEXT;
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the text alignment in the specified column.
    /// </summary>
    /// <param name="index">the index of the specified column.</param>
    /// <param name="alignment">the specified alignment. Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY.</param>
    /// <returns>this Table object.</returns>
    public Table SetTextAlignmentInColumn(int index, Alignment alignment) {
        foreach (List<Cell> row in tableData) {
            if (index < row.Count) {
                Cell cell = row[index];
                cell.SetTextAlignment(alignment);
                if (cell.GetTextBlock() != null) {
                    cell.GetTextBlock().SetTextAlignment(alignment);
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
                if (cell.GetTextBlock() != null) {
                    cell.GetTextBlock().SetTextColor(color);
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
                if (cell.GetTextBlock() != null) {
                    cell.GetTextBlock().SetFont(font).SetFontSize(font.GetSize());
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
                if (cell.GetTextBlock() != null) {
                    cell.GetTextBlock().SetTextColor(color);
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
                if (cell.GetTextBlock() != null) {
                    cell.GetTextBlock().SetFont(font).SetFontSize(font.GetSize());
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
    public Table SetColumnWidth(int index, float width) {
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
        return GetCellAt(0, index).GetWidth();
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
    /// Returns a list of cells for the specified row.
    /// </summary>
    /// <param name="index">the index of the specified row.</param>
    /// <returns>the list of cells.</returns>
    public List<Cell> GetRow(int index) {
        return tableData[index];
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

    /// <summary>
    /// Draws this table on the specified page.
    /// </summary>
    /// <param name="page">the page to draw this table on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the table.</returns>
    public float[] DrawOn(Page page) {
        if (tableData.Count == 0) {
            return new float[] {x1, y1};    // An empty table draws nothing.
        }
        WrapAroundCellText();
        ApplyRowSpans();
        SetRightBorderOnLastColumn();
        SetBottomBorderOnLastRow();
        float[] xy = DrawTableRows(page, DrawHeaderRows(page, 0));
        return new float[] {x1 + GetWidth(), xy[1]};
    }

    /// <summary>
    /// Draws this table on as many new pages as it needs.
    /// The pages are created detached and added to the list; add them to the PDF afterwards.
    /// </summary>
    /// <param name="pdf">the PDF document.</param>
    /// <param name="pages">the list that receives the new pages.</param>
    /// <param name="pageSize">the page size, for example Letter.PORTRAIT.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the table on the last page.</returns>
    public float[] DrawOn(PDF pdf, List<Page> pages, PageSize pageSize) {
        if (tableData.Count == 0) {
            return new float[] {x1, y1};    // An empty table needs no page.
        }
        WrapAroundCellText();
        ApplyRowSpans();
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
        return new float[] {x1 + GetWidth(), xy[1]};
    }

    private float[] DrawHeaderRows(Page page, int pageNumber) {
        float x = x1;
        float y = y1;
        if (pageNumber == 1 && firstPageTopMargin > 0f) {
            y = firstPageTopMargin;
        }
        // In a PDF/UA document the table is a Table element, which the rows
        // drawn on the next pages go on adding to. The header rows are TH
        // cells the first time they are drawn, and artifacts on the next pages.
        bool first = (rendered == numOfHeaderRows);
        if (page != null && (first || structElement == null)) {
            structElement = page.AddStructElement(
                    page.structParent, StructElem.TABLE, null, true);
        }
        if (page != null && !first && numOfHeaderRows > 0) {
            page.AddArtifactBMC();
        }
        float[] heights = GetRowHeights();
        // The rows that bring a total forward are drawn from the second page
        // on, with the total of the rows before the page.
        int last = -1;
        for (int i = 0; i < numOfHeaderRows && i < tableData.Count; i++) {
            if (!(first && IsBroughtForwardRow(i))) {
                last = i;
            }
        }
        if (rendered != -1) {
            SetSums(0, numOfHeaderRows, rendered, rendered);
        }
        for (int i = 0; i < numOfHeaderRows && i < tableData.Count; i++) {
            if (first && IsBroughtForwardRow(i)) {
                continue;
            }
            List<Cell> row = tableData[i];
            if (page != null) {
                if (i == last) {
                    foreach (Cell cell in row) {
                        cell.SetBorder(Border.BOTTOM, true);
                    }
                }
                DrawRow(page, row, x, y, heights, i, first ? StructElem.TH : (StructElem?) null);
            }
            y += heights[i];
        }
        if (page != null && !first && numOfHeaderRows > 0) {
            page.AddEMC();
        }
        return new float[] {x, y};
    }

    // Draws the cells of the row. In a PDF/UA document the row is a TR element
    // and each cell a TH or TD element, which holds what the cell draws; a row
    // that goes on with the wrapped text of the row above adds to its elements.
    // With no cell structure the row is not tagged, as it is an artifact.
    private void DrawRow(Page page, List<Cell> row, float x, float y, float[] heights,
            int rowIndex, StructElem? cellStructure) {
        StructElement parent = page.structParent;
        bool tagged = (structElement != null && cellStructure != null);
        bool continued = (row[0].properties & Cell.CONTINUED) != 0;
        StructElement rowElement = null;
        if (tagged && !continued && !AllCovered(row)) {
            rowElement = page.AddStructElement(structElement, StructElem.TR, null);
            if (cellElements == null || cellElements.Length != row.Count) {
                cellElements = new StructElement[row.Count];
            }
        }
        int i = 0;
        while (i < row.Count) {
            Cell cell = row[i];
            int colspan = (cell.GetColSpan() < 1) ? 1 : cell.GetColSpan();
            bool covered = (cell.properties & Cell.COVERED) != 0;
            if (tagged && !covered) {
                if (!continued) {
                    cellElements[i] = page.AddStructElement(rowElement, cellStructure.Value,
                            GetAttributes(cellStructure.Value, colspan, cell.rowsSpanned));
                }
                page.structParent = (cellElements != null && i < cellElements.Length) ?
                        cellElements[i] : null;
            }
            float w = 0f;
            for (int j = 0; j < colspan; j++) {
                w += row[i++].GetWidth();
            }
            if (!covered) {
                // A cell that spans rows is as tall as all the rows it covers.
                float cellHeight = 0f;
                int end = Math.Min(rowIndex + cell.rowsSpanned, heights.Length);
                for (int r = rowIndex; r < end; r++) {
                    cellHeight += heights[r];
                }
                page.SetBrushColor(cell.textColor);
                cell.DrawOn(page, x, y, w, cellHeight);
            }
            x += w;
        }
        page.structParent = parent;
    }

    // True when every cell of the row is one that a cell above it spans over,
    // so the row holds no cell of its own and is not a row of the table.
    private static bool AllCovered(List<Cell> row) {
        foreach (Cell cell in row) {
            if ((cell.properties & Cell.COVERED) == 0) {
                return false;
            }
        }
        return row.Count > 0;
    }

    // The attributes of a table cell element: the scope of a header cell and
    // the number of rows and columns a cell spans, or null when it has none.
    private static String GetAttributes(StructElem cellStructure, int colspan, int rowspan) {
        String spans = "";
        if (colspan > 1) {
            spans += " /ColSpan " + colspan;
        }
        if (rowspan > 1) {
            spans += " /RowSpan " + rowspan;
        }
        if (cellStructure == StructElem.TH) {
            return "<</O /Table /Scope /Column" + spans + ">>";
        }
        return (spans.Length > 0) ? "<</O /Table" + spans + ">>" : null;
    }

    // Draws the rows from the next row to draw, as many as fit on the page,
    // and the footer rows under them. With no page it measures them all and
    // leaves the next row to draw as it is.
    private float[] DrawTableRows(Page page, float[] xy) {
        float x = xy[0];
        float y = xy[1];
        int footer = FooterStart();
        bool done = (rendered == -1);
        int index = done ? footer : rendered;
        int first = index;
        float[] heights = GetRowHeights();
        // Where the rows start on the next pages, under the header rows.
        float top = y1;
        for (int r = 0; r < numOfHeaderRows && r < heights.Length; r++) {
            top += heights[r];
        }
        // Where the rows end on a page, over the footer rows.
        float bottom = (page == null) ? 0f : page.height - bottomMargin;
        for (int r = footer; r < tableData.Count; r++) {
            bottom -= heights[r];
        }
        // The rows before the first of these are not kept with the next row,
        // and the lines of the rows before the second are cut where the page
        // ends, as rows of their own.
        int cutRowsUntil = -1;
        int cutLinesUntil = -1;
        while (index < footer) {
            // The rows a cell spans, the lines a row wraps into and the rows
            // kept with the next one are drawn together, so that a page break
            // never cuts one of them in two. The footer rows are not in them.
            bool keepLines = (index >= cutLinesUntil);
            bool keepRows = keepLines && (index >= cutRowsUntil);
            int end = Math.Min(RowGroupEnd(index, keepLines, keepRows), footer);
            float groupHeight = 0f;
            for (int r = index; r < end; r++) {
                groupHeight += heights[r];
            }
            if (page != null && (y + groupHeight) > bottom) {
                if (keepLines && groupHeight > bottom - top) {
                    // Rows that would not fit the next page either are drawn
                    // from here: first each on its own, and then, for a row
                    // that is taller than a page, each line on its own, cut
                    // where the page ends.
                    if (keepRows) {
                        cutRowsUntil = end;
                    } else {
                        cutLinesUntil = end;
                    }
                    continue;
                }
                // A row that does not fit goes on the next page, unless it is
                // the first row of this one: a row taller than the page fits
                // no page, and leaving it for the next page would ask for
                // pages forever.
                if (index > first) {
                    rendered = index;
                    SetFooterSums(first, index);
                    return new float[] {x, DrawFooterRows(page, x, y, heights, false)};
                }
            }
            for (int r = index; r < end; r++) {
                if (page != null) {
                    DrawRow(page, tableData[r], x, y, heights, r, StructElem.TD);
                }
                y += heights[r];
            }
            index = end;
        }
        if (!done) {
            // The rows of the table are all drawn, and the footer rows end it.
            SetFooterSums(first, footer);
            y = DrawFooterRows(page, x, y, heights, true);
        }
        if (page != null) {
            rendered = -1; // We are done!
        }
        return new float[] {x, y};
    }

    // Draws the footer rows at y and returns the y under them. In a PDF/UA
    // document they are rows of the table where it ends, and artifacts on the
    // pages before.
    private float DrawFooterRows(Page page, float x, float y, float[] heights, bool last) {
        int footer = FooterStart();
        bool artifact = (page != null && !last && footer < tableData.Count);
        if (artifact) {
            page.AddArtifactBMC();
        }
        for (int r = footer; r < tableData.Count; r++) {
            if (page != null) {
                if (r == footer) {
                    foreach (Cell cell in tableData[r]) {
                        cell.SetBorder(Border.TOP, true);
                    }
                }
                DrawRow(page, tableData[r], x, y, heights, r, last ? StructElem.TD : (StructElem?) null);
            }
            y += heights[r];
        }
        if (artifact) {
            page.AddEMC();
        }
        return y;
    }

    // Works out what each cell that spans rows covers, after the text is
    // wrapped: a row of the table is drawn as one row for each line its
    // tallest cell needs, so a cell that spans two rows of the table spans as
    // many rows of the drawing as those two were wrapped into. The cells the
    // span covers are marked, and draw nothing.
    private void ApplyRowSpans() {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.properties &= ~Cell.COVERED;
                cell.rowsSpanned = 1;
            }
        }
        for (int r = 0; r < tableData.Count; r++) {
            if (IsContinuation(r)) {
                continue;   // A span starts in a row of the table, not in the wrap of one.
            }
            List<Cell> row = tableData[r];
            int i = 0;
            while (i < row.Count) {
                Cell cell = row[i];
                int colspan = (cell.GetColSpan() < 1) ? 1 : cell.GetColSpan();
                if (cell.GetRowSpan() > 1 && (cell.properties & Cell.COVERED) == 0) {
                    int end = RowAfter(r, cell.GetRowSpan());
                    int ownEnd = RowAfter(r, 1);
                    cell.rowsSpanned = end - r;
                    // The rows the wrapped text of this cell takes keep their
                    // text and lose the border that would cross the cell.
                    for (int r2 = r + 1; r2 < ownEnd; r2++) {
                        SetSpanned(r2, i, colspan, false);
                    }
                    for (int r2 = ownEnd; r2 < end; r2++) {
                        SetSpanned(r2, i, colspan, true);
                    }
                }
                i += colspan;
            }
        }
    }

    // Marks the columns of the row that a span covers: a covered cell draws
    // nothing, and a row of the wrapped text of the spanning cell keeps its
    // text without the border under it.
    private void SetSpanned(int r, int column, int colspan, bool covered) {
        List<Cell> row = tableData[r];
        int i = 0;
        while (i < row.Count) {
            Cell cell = row[i];
            if (i >= column && i < column + colspan) {
                if (covered) {
                    cell.properties |= Cell.COVERED;
                } else {
                    cell.SetBorder(Border.BOTTOM, false);
                }
            }
            i += (cell.GetColSpan() < 1) ? 1 : cell.GetColSpan();
        }
    }

    // The index of the row after the count rows of the table that start at r,
    // counting the rows the wrapped text of each of them takes.
    private int RowAfter(int r, int count) {
        int index = r;
        for (int i = 0; i < count && index < tableData.Count; i++) {
            index++;
            while (index < tableData.Count && IsContinuation(index)) {
                index++;
            }
        }
        return index;
    }

    // True when the row holds the wrapped text of the row above it.
    private bool IsContinuation(int r) {
        List<Cell> row = tableData[r];
        return row.Count > 0 && (row[0].properties & Cell.CONTINUED) != 0;
    }

    // True when the row, or the row whose wrapped text it holds, is kept with
    // the next row.
    private bool IsKeptWithNext(int r) {
        List<Cell> row = tableData[r];
        return row.Count > 0 && (row[0].properties & Cell.KEPT_WITH_NEXT) != 0;
    }

    // The height of each row of the table as it is drawn. A cell that spans
    // rows is not what makes its first row tall; the rows it covers hold it
    // together, and the last of them grows when they do not.
    private float[] GetRowHeights() {
        float[] heights = new float[tableData.Count];
        for (int r = 0; r < tableData.Count; r++) {
            heights[r] = GetMaxCellHeight(tableData[r]);
        }
        for (int r = 0; r < tableData.Count; r++) {
            List<Cell> row = tableData[r];
            for (int i = 0; i < row.Count; i++) {
                Cell cell = row[i];
                if (cell.rowsSpanned < 2) {
                    continue;
                }
                int end = Math.Min(r + cell.rowsSpanned, tableData.Count);
                float have = 0f;
                for (int r2 = r; r2 < end; r2++) {
                    have += heights[r2];
                }
                float needed = cell.GetHeight(GetTotalWidth(row, i));
                if (needed > have && end > r) {
                    heights[end - 1] += needed - have;
                }
            }
        }
        return heights;
    }

    // The row after the rows that a span holds together, with keepLines the
    // lines the last of them wraps into, and with keepRows the rows that the
    // last of them is kept with, which a page break keeps on one page.
    private int RowGroupEnd(int index, bool keepLines, bool keepRows) {
        int end = index + 1;
        for (int r = index; r < end && r < tableData.Count; r++) {
            foreach (Cell cell in tableData[r]) {
                if (r + cell.rowsSpanned > end) {
                    end = r + cell.rowsSpanned;
                }
            }
            if (r + 1 == end && end < tableData.Count) {
                if (keepLines && IsContinuation(end)) {
                    end++;
                } else if (keepRows && IsKeptWithNext(r)) {
                    end++;
                }
            }
        }
        return Math.Min(end, tableData.Count);
    }

    private float GetMaxCellHeight(List<Cell> row) {
        float maxCellHeight = 0f;
        float spanned = 0f;
        for (int i = 0; i < row.Count; i++) {
            Cell cell = row[i];
            if ((cell.properties & Cell.COVERED) != 0) {
                continue;   // A cell the one above it draws over.
            }
            float cellHeight = cell.GetHeight(GetTotalWidth(row, i));
            if (cell.rowsSpanned > 1) {
                // A cell that spans rows is as tall as all of them together,
                // which GetRowHeights shares out; it is the height of the row
                // only when nothing else is in it.
                spanned = Math.Max(spanned, cellHeight / cell.rowsSpanned);
                continue;
            }
            if (cellHeight > maxCellHeight) {
                maxCellHeight = cellHeight;
            }
        }
        return (maxCellHeight > 0f) ? maxCellHeight : spanned;
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
        if (tableData.Count > 0) {
            foreach (Cell cell in tableData[0]) {
                tableWidth += cell.GetWidth();
            }
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
    public Table SetCellBorderColor(int color) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetBorderColor(color);
            }
        }
        return this;
    }

    /// <summary>Sets the color of the cell border lines.</summary>
    /// <param name="rgbColor">the color as red, green and blue components from 0.0 to 1.0.</param>
    /// <returns>this Table object.</returns>
    public Table SetCellBorderColor(float[] rgbColor) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetBorderColor(rgbColor);
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the width of the cell border lines.
    /// </summary>
    /// <param name="width">the width of the cell border lines.</param>
    /// <returns>this Table object.</returns>
    public Table SetCellBorderWidth(float width) {
        foreach (List<Cell> row in tableData) {
            foreach (Cell cell in row) {
                cell.SetBorderWidth(width);
            }
        }
        return this;
    }

    // Sets the right border on all cells in the last column.
    private void SetRightBorderOnLastColumn() {
        foreach (List<Cell> row in tableData) {
            if (row.Count > 0 && row[0].GetBorder(Border.LEFT) == false) {
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
            if (cell != null) {
                cell.SetBorder(Border.RIGHT, true);
            }
        }
    }

    // Sets the bottom border on all cells in the last row.
    private void SetBottomBorderOnLastRow() {
        if (tableData.Count == 0) {
            return;
        }
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
    public Table AutoAdjustColumnWidths() {
        if (tableData.Count == 0) {
            return this;
        }
        float[] maxColWidths = new float[tableData[0].Count];
        foreach (List<Cell> row in tableData) {
            for (int i = 0; i < row.Count; i++) {
                Cell cell = row[i];
                if (cell.GetColSpan() == 1) {
                    TextBlock textBlock = cell.GetTextBlock();
                    if (textBlock != null) {
                        String[] tokens = Util.SplitOnWhitespace(textBlock.textContent);
                        foreach (String token in tokens) {
                            float tokenWidth = textBlock.font.StringWidth(textBlock.fallbackFont, token);
                            tokenWidth += cell.GetLeftPadding() + cell.GetRightPadding();
                            if (tokenWidth > maxColWidths[i]) {
                                maxColWidths[i] = tokenWidth;
                            }
                        }
                    } else if (cell.drawable != null) {
                        float drawableWidth = Cell.Measure(cell.drawable)[0] + cell.GetLeftPadding() + cell.GetRightPadding();
                        if (drawableWidth > maxColWidths[i]) {
                            maxColWidths[i] = drawableWidth;
                        }
                    } else if (cell.text != null) {
                        float textWidth = cell.font.StringWidth(cell.fallbackFont, cell.fontSize, cell.text);
                        textWidth += cell.GetLeftPadding() + cell.GetRightPadding();
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

    private float GetTotalWidth(List<Cell> row, int index) {
        Cell cell = row[index];
        int colspan = cell.GetColSpan();
        float cellWidth = 0f;
        for (int i = 0; i < colspan; i++) {
            cellWidth += row[index + i].GetWidth();
        }
        cellWidth -= (cell.GetLeftPadding() + row[index + (colspan - 1)].GetRightPadding());
        return cellWidth;
    }

    /// <summary>
    /// Wraps around the text in all cells so it fits the column width.
    /// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
    /// </summary>
    protected void WrapAroundCellText() {
        List<List<Cell>> tableData2 = new List<List<Cell>>();
        List<List<String>> lines = new List<List<String>>();
        int numOfHeaderRows2 = 0;
        // The footer rows are the rows of the wrap of the rows they were.
        int footer = (numOfFooterRows > 0) ? FooterStart() : tableData.Count;
        int footer2 = 0;
        for (int r = 0; r < tableData.Count; r++) {
            List<Cell> row = tableData[r];
            int first = tableData2.Count;
            if (r == footer) {
                footer2 = first;
            }
            tableData2.Add(row);    // Add the original row
            // Every cell of the row is wrapped once, here. The lines it needs
            // are what the cells stacked below it get, and the most lines any
            // cell of the row needs is how many rows to stack. The rows added
            // below a header row are header rows too.
            lines.Clear();
            int maxNumVerCells = 1;
            for (int i = 0; i < row.Count; i++) {
                // A cell that draws a line of text of its own draws no cell
                // text, so there is nothing to wrap. A cell that shows a sum
                // is not wrapped either, as its text is the sum of each page.
                List<String> cellLines = (row[i].text == null
                        || row[i].drawable is IBaselineDrawable
                        || (row[i].properties & Cell.SUM_BITS) != 0)
                        ? null : WrapCellText(row, i);
                lines.Add(cellLines);
                if (cellLines != null && cellLines.Count > maxNumVerCells) {
                    maxNumVerCells = cellLines.Count;
                }
            }
            // A cell whose text wraps is one cell drawn as the rows its lines
            // take, so the border under it belongs under the last of them and
            // not under every line of it. A cell that spans rows is drawn over
            // all of them at once and so draws its own bottom border under the
            // whole of it; ApplyRowSpans clears the rows of its wrap.
            bool[] bottomBorder = new bool[row.Count];
            for (int i = 0; i < row.Count; i++) {
                Cell cell = row[i];
                bottomBorder[i] = maxNumVerCells > 1 && cell.GetRowSpan() == 1
                        && cell.GetBorder(Border.BOTTOM);
                if (bottomBorder[i]) {
                    cell.SetBorder(Border.BOTTOM, false);
                }
            }
            for (int i = 1; i < maxNumVerCells; i++) {
                List<Cell> row2 = new List<Cell>();
                for (int j = 0; j < row.Count; j++) {
                    Cell cell = row[j];
                    Cell cell2 = new Cell(cell.GetFont());
                    cell2.SetFallbackFont(cell.GetFallbackFont());
                    cell2.SetFontSize(cell.fontSize);
                    cell2.SetWidth(cell.GetWidth());
                    cell2.SetLeftPadding(cell.GetLeftPadding());
                    cell2.SetRightPadding(cell.GetRightPadding());
                    cell2.backgroundColor = cell.backgroundColor;
                    cell2.SetBorderWidth(cell.GetBorderWidth());
                    cell2.borderColor = cell.borderColor;
                    cell2.textColor = cell.textColor;
                    cell2.SetColSpan(cell.GetColSpan());
                    cell2.properties = cell.properties;
                    cell2.SetTextAlignment(cell.GetTextAlignment());
                    cell2.SetVerticalAlignment(cell.GetVerticalAlignment());
                    cell2.SetTopPadding(0f);
                    cell2.SetBorder(Border.TOP, false);
                    cell2.SetBorder(Border.BOTTOM, bottomBorder[j] && i == maxNumVerCells - 1);
                    cell2.properties |= Cell.CONTINUED;
                    cell2.properties &= ~Cell.SUM_BITS;
                    row2.Add(cell2);
                }
                tableData2.Add(row2);
            }
            for (int j = 0; j < row.Count; j++) {
                List<String> cellLines = lines[j];
                if (cellLines != null) {
                    for (int n = 0; n < cellLines.Count; n++) {
                        tableData2[first + n][j].SetText(cellLines[n]);
                    }
                }
            }
            // The stacked rows are wrapped in their turn, as they were when
            // the rows were all added first and the table wrapped in one pass
            // afterwards: a line that ends in a space loses it here.
            for (int i = first + 1; i < tableData2.Count; i++) {
                List<Cell> row2 = tableData2[i];
                for (int j = 0; j < row2.Count; j++) {
                    if (row2[j].text != null) {
                        List<String> cellLines = WrapCellText(row2, j);
                        for (int n = 0; n < cellLines.Count; n++) {
                            tableData2[i + n][j].SetText(cellLines[n]);
                        }
                    }
                }
            }
            if (r < numOfHeaderRows) {
                numOfHeaderRows2 = tableData2.Count;
            }
        }
        if (rendered != -1) {
            rendered += numOfHeaderRows2 - numOfHeaderRows;
        }
        numOfHeaderRows = numOfHeaderRows2;
        if (footer < tableData.Count) {
            numOfFooterRows = tableData2.Count - footer2;
        }
        tableData = tableData2;
    }

    // The lines the text of the cell needs to fit the width of its column.
    // A token wider than the column is broken between two of its characters.
    private List<String> WrapCellText(List<Cell> row, int index) {
        Cell cell = row[index];
        float cellWidth = GetTotalWidth(row, index);
        List<String> lines = new List<String>();
        StringBuilder buf = new StringBuilder();
        foreach (String token in Util.SplitOnWhitespace(cell.text)) {
            if (cell.font.StringWidth(cell.fallbackFont, cell.fontSize, token) > cellWidth) {
                if (buf.Length > 0) {
                    buf.Append(" ");
                }
                foreach (char ch in token) {
                    if (cell.font.StringWidth(cell.fallbackFont, cell.fontSize,
                            buf.ToString() + ch) > cellWidth) {
                        lines.Add(buf.ToString());
                        buf.Length = 0;
                    }
                    buf.Append(ch);
                }
            } else if (buf.Length == 0) {
                // A token that fits the column fits a line of its own, and its
                // width is the one measured just above.
                buf.Append(token);
            } else if (cell.font.StringWidth(cell.fallbackFont, cell.fontSize,
                    Util.Trim(buf.ToString() + " " + token)) > cellWidth) {
                lines.Add(Util.Trim(buf.ToString()));
                buf.Length = 0;
                buf.Append(token);
            } else {
                buf.Append(" ");
                buf.Append(token);
            }
        }
        lines.Add(Util.Trim(buf.ToString()));
        return lines;
    }

    /// <summary>
    /// Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
    /// </summary>
    /// <returns>the number of vertical cells needed to wrap around the cell text.</returns>
    internal int GetNumVerCells(List<Cell> row, int index) {
        if (row[index].text == null) {
            return 1;
        }
        return WrapCellText(row, index).Count;
    }

    // The delimiter of the line: the commonest of a comma, a pipe and a tab.
    // The ones inside a quoted field are not counted, or a file whose values
    // hold commas could be split on the wrong character altogether.
    private String GetDelimiter(String str) {
        int comma = 0;
        int pipe = 0;
        int tab = 0;
        bool quoted = false;
        foreach (char ch in str) {
            if (ch == '"') {
                quoted = !quoted;
            } else if (quoted) {
                continue;
            } else if (ch == ',') {
                comma++;
            } else if (ch == '|') {
                pipe++;
            } else if (ch == '\t') {
                tab++;
            }
        }
        if (comma >= pipe) {
            if (comma >= tab) {
                return ",";
            }
            return "\t";
        } else {
            if (pipe >= tab) {
                return "|";
            }
            return "\t";
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
