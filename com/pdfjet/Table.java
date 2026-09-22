/*
 * Table.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedInputStream;
import java.io.FileInputStream;
import java.io.IOException;
import java.util.*;

/**
 * Used to create table objects and draw them on a page.
 *
 * Please see Example_08.
 */
public class Table implements Drawable {
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

    /**
     * Create a table object.
     */
    public Table() {
        tableData = new ArrayList<List<Cell>>();
    }

    /**
     * Creates a table from a text file with comma, pipe or tab separated values.
     * The first line is the header row and uses f1; the other lines use f2.
     * Every row gets as many cells as the first line has fields. A quoted field
     * is read as RFC 4180 reads it, and a line break inside one goes on to the
     * next line of the file and is drawn as a space.
     *
     * @param f1 the font for the header row.
     * @param f2 the font for the other rows.
     * @param fileName the file name.
     * @throws IOException if the file cannot be read.
     */
    public Table(Font f1, Font f2, String fileName) throws IOException {
        tableData = new ArrayList<List<Cell>>();
        UTF8.LineReader reader = new UTF8.LineReader(
                new BufferedInputStream(new FileInputStream(fileName)));
        try {
            String delimiter = null;
            int numberOfFields = 0;
            int lineNumber = 0;
            String line;
            while ((line = reader.readLine()) != null) {
                if (lineNumber == 0) {
                    // A byte order mark at the start of the file is not part of the text.
                    if (line.startsWith("\uFEFF")) {
                        line = line.substring(1);
                    }
                    delimiter = getDelimiter(line);
                }
                List<Cell> row = new ArrayList<Cell>();
                // The empty fields at the end of the line are kept, a quoted
                // field holds its delimiters instead of being cut at them, and
                // its line breaks, which go on to the next lines, are spaces.
                String[] fields = Util.readRecord(line, reader, delimiter);
                if (lineNumber == 0) {
                    numberOfFields = fields.length;
                }
                for (String field : fields) {
                    if (lineNumber == 0) {
                        row.add(new Cell(f1, field));
                    } else {
                        row.add(new Cell(f2, field));
                    }
                }
                if (row.size() > numberOfFields) {
                    List<Cell> row2 = new ArrayList<Cell>();
                    for (int i = 0; i < numberOfFields; i++) {
                        row2.add(row.get(i));
                    }
                    tableData.add(row2);
                } else if (row.size() < numberOfFields) {
                    int diff = numberOfFields - row.size();
                    for (int i = 0; i < diff; i++) {
                        row.add(new Cell(f2, ""));
                    }
                    tableData.add(row);
                } else {
                    tableData.add(row);
                }
                lineNumber++;
            }
        } finally {
            reader.close();
        }
    }

    /**
     * Sets the location (x, y) of the top left corner of this table on the page.
     *
     * @param x the x coordinate of the top left point of the table.
     * @param y the y coordinate of the top left point of the table.
     * @return this Table object.
     */
    public Table setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the bottom margin for this table.
     *
     * @param bottomMargin the margin.
     * @return this Table object.
     */
    public Table setBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
        return this;
    }

    /**
     * Sets the table data.
     *
     * The table data is a perfect grid of cells.
     * All cell should be an unique object and you can not reuse blank cell objects.
     * Even if one or more cells have colspan bigger than zero the number of cells
     * in the row will not change.
     *
     * @param tableData the table data.
     * @return this Table object.
     */
    public Table setTableData(List<List<Cell>> tableData) {
        return setTableData(tableData, 0);
    }

    /**
     * Sets the table data and specifies the number of header rows in this data.
     * The header rows are drawn again at the top of every page.
     *
     * @param tableData       the table data.
     * @param numOfHeaderRows the number of header rows in this data.
     * @return this Table object.
     */
    public Table setTableData(List<List<Cell>> tableData, int numOfHeaderRows) {
        this.tableData = tableData;
        this.numOfHeaderRows = numOfHeaderRows;
        this.rendered = numOfHeaderRows;
        addCellsToCompleteTheGrid();
        return this;
    }

    /**
     * Makes the last rows of the table data footer rows, which are drawn again
     * at the end of the table on every page, under the last row the page
     * holds, as the header rows are drawn again at the top. A page leaves room
     * for them. In a PDF/UA document they are rows of the table once, where
     * the table ends, and artifacts on the other pages.
     *
     * @param numOfFooterRows the number of footer rows at the end of the data.
     * @return this Table object.
     */
    public Table setNumberOfFooterRows(int numOfFooterRows) {
        this.numOfFooterRows = Math.max(0, numOfFooterRows);
        return this;
    }

    // The index of the first footer row, after the header rows and the rows
    // of the table.
    private int footerStart() {
        return Math.max(numOfHeaderRows, tableData.size() - numOfFooterRows);
    }

    // Adds empty cells to the rows that are shorter than the first row.
    private void addCellsToCompleteTheGrid() {
        if (tableData.isEmpty() || tableData.get(0).isEmpty()) {
            return;
        }
        int numOfColumns = tableData.get(0).size();
        Font font = tableData.get(0).get(0).font;
        for (List<Cell> row : tableData) {
            int diff = numOfColumns - row.size();
            for (int i = 0; i < diff; i++) {
                row.add(new Cell(font, ""));
            }
        }
    }

    /**
     * Aligns to the right the cells whose text is a number, such as 1,234.50,
     * (1,234.50), -5 or 1.5E+3. The periods, commas and apostrophes are ignored,
     * and the number can be in parentheses.
     *
     * @return this Table object.
     */
    public Table rightAlignNumbers() {
        for (List<Cell> row : tableData) {
            for (Cell cell : row) {
                if (cell.text != null && isNumber(cell.text)) {
                    cell.setTextAlignment(Alignment.RIGHT);
                }
            }
        }
        return this;
    }

    // Returns true if the text, without its periods, commas and apostrophes and
    // the parentheses around it, is an optional sign, ASCII digits and an
    // optional exponent, with optional spaces before and after.
    static boolean isNumber(String text) {
        String str = text;
        if (str.length() >= 2 && str.charAt(0) == '(' && str.charAt(str.length() - 1) == ')') {
            str = str.substring(1, str.length() - 1);
        }
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (ch != '.' && ch != ',' && ch != '\'') {
                buf.append(ch);
            }
        }
        String number = buf.toString().trim();
        int i = 0;
        if (i < number.length() && (number.charAt(i) == '+' || number.charAt(i) == '-')) {
            i++;
        }
        int start = i;
        while (i < number.length() && number.charAt(i) >= '0' && number.charAt(i) <= '9') {
            i++;
        }
        if (i == start) {
            return false;
        }
        if (i < number.length() && (number.charAt(i) == 'e' || number.charAt(i) == 'E')) {
            i++;
            if (i < number.length() && (number.charAt(i) == '+' || number.charAt(i) == '-')) {
                i++;
            }
            start = i;
            while (i < number.length() && number.charAt(i) >= '0' && number.charAt(i) <= '9') {
                i++;
            }
            if (i == start) {
                return false;
            }
        }
        return i == number.length();
    }

    /**
     * Removes the horizontal lines between the rows from index1 to index2.
     *
     * @param index1 the index of the first specified row.
     * @param index2 the index of the second specified row.
     * @return this Table object.
     */
    public Table removeLineBetweenRows(int index1, int index2) {
        for (int i = index1; i < index2; i++) {
            List<Cell> row = tableData.get(i);
            for (Cell cell : row) {
                cell.setBorder(Border.BOTTOM, false);
            }
            row = tableData.get(i + 1);
            for (Cell cell : row) {
                cell.setBorder(Border.TOP, false);
            }
        }
        return this;
    }

    /**
     * Makes the cell of a footer row show the sum of its column over the rows
     * of each page: the page total. The numbers are read as rightAlignNumbers
     * reads them, with commas and apostrophes between the thousands and a
     * period before the decimals, and a cell that has no number is left out.
     * The sum has the number of decimals, rounded half away from zero, and
     * commas between the thousands. Until the table is drawn the cell has the
     * sum of all the rows, so that autoAdjustColumnWidths leaves room for it;
     * its text is not wrapped.
     *
     * @param row the index of a footer row, see setNumberOfFooterRows.
     * @param column the index of the column.
     * @param decimals the number of decimals, from 0 to 9.
     * @return this Table object.
     */
    public Table setPageSum(int row, int column, int decimals) {
        return setSum(row, column, decimals, Cell.PAGE_SUM);
    }

    /**
     * Makes the cell of a footer row show the sum of its column over all the
     * rows up to the end of each page: the total carried forward, which is
     * the total of the table on its last page. The numbers are read and the
     * sum is written as setPageSum reads and writes them.
     *
     * @param row the index of a footer row, see setNumberOfFooterRows.
     * @param column the index of the column.
     * @param decimals the number of decimals, from 0 to 9.
     * @return this Table object.
     */
    public Table setRunningSum(int row, int column, int decimals) {
        return setSum(row, column, decimals, Cell.RUNNING_SUM);
    }

    /**
     * Makes the cell of a header row show the sum of its column over all the
     * rows of the pages before: the total brought forward, which the cell that
     * setRunningSum marks shows at the end of the page before. A header row
     * with such a cell is drawn from the second page on, under the header
     * rows above it, and not on the first page, which has nothing to bring
     * forward; in a PDF/UA document it is an artifact, as the header rows are
     * on those pages. The numbers are read and the sum is written as
     * setPageSum reads and writes them.
     *
     * @param row the index of a header row, see setTableData.
     * @param column the index of the column.
     * @param decimals the number of decimals, from 0 to 9.
     * @return this Table object.
     */
    public Table setBroughtForwardSum(int row, int column, int decimals) {
        return setSum(row, column, decimals, Cell.BROUGHT_FORWARD_SUM);
    }

    private Table setSum(int row, int column, int decimals, int kind) {
        if (row < 0 || row >= tableData.size() || column < 0 || column >= tableData.get(row).size()) {
            return this;
        }
        int places = Math.max(0, Math.min(9, decimals));
        Cell cell = tableData.get(row).get(column);
        cell.properties = (cell.properties & ~Cell.SUM_BITS) | kind | (places << Cell.SUM_DECIMALS);
        int end = (kind == Cell.BROUGHT_FORWARD_SUM) ? footerStart() : Math.min(row, footerStart());
        cell.setText(formatSum(sumOf(column, numOfHeaderRows, end, places), places));
        return this;
    }

    // Writes the sums in the cells of the rows from start to stop that
    // setPageSum, setRunningSum and setBroughtForwardSum mark: those of the
    // rows of the page, from first to end, those of all the rows to end, and
    // those of all the rows before first.
    private void setSums(int start, int stop, int first, int end) {
        for (int r = start; r < stop && r < tableData.size(); r++) {
            List<Cell> row = tableData.get(r);
            for (int j = 0; j < row.size(); j++) {
                Cell cell = row.get(j);
                int kind = cell.properties & Cell.SUM_KIND;
                if (kind != 0) {
                    int places = (cell.properties >> Cell.SUM_DECIMALS) & 0xF;
                    int from = (kind == Cell.PAGE_SUM) ? first : numOfHeaderRows;
                    int to = (kind == Cell.BROUGHT_FORWARD_SUM) ? first : end;
                    cell.setText(formatSum(sumOf(j, from, to, places), places));
                }
            }
        }
    }

    private void setFooterSums(int first, int end) {
        setSums(footerStart(), tableData.size(), first, end);
    }

    // True when the row, or the row whose wrapped text it holds, has a cell
    // that brings a total forward, which the first page does not draw.
    private boolean isBroughtForwardRow(int r) {
        while (r > 0 && isContinuation(r)) {
            r--;
        }
        for (Cell cell : tableData.get(r)) {
            if ((cell.properties & Cell.SUM_KIND) == Cell.BROUGHT_FORWARD_SUM) {
                return true;
            }
        }
        return false;
    }

    // The largest sum, and number, in units of the decimals: 18 digits.
    private static final long MAX_SUM = 999999999999999999L;

    // The sum of the numbers in the column of the rows from first to end, in
    // units of the decimals. A number that would take the sum past 18 digits
    // is left out.
    private long sumOf(int column, int first, int end, int decimals) {
        long sum = 0L;
        for (int r = Math.max(0, first); r < end && r < tableData.size(); r++) {
            List<Cell> row = tableData.get(r);
            if (column >= row.size()) {
                continue;
            }
            Cell cell = row.get(column);
            if ((cell.properties & Cell.COVERED) != 0 || cell.text == null) {
                continue;
            }
            Long value = numberOf(cell.text, decimals);
            if (value != null && Math.abs(sum + value) <= MAX_SUM) {
                sum += value;
            }
        }
        return sum;
    }

    /**
     * Returns the number the text is, in units of the decimals, rounded half
     * away from zero, or null when the text is not a number, as isNumber
     * reads it, has more than one period, or is more than 18 digits in those
     * units. Commas and apostrophes are between the thousands, a period is
     * before the decimals, and parentheses make a number negative.
     */
    static Long numberOf(String text, int decimals) {
        if (!isNumber(text)) {
            return null;
        }
        String str = text;
        boolean negative = false;
        if (str.length() >= 2 && str.charAt(0) == '(' && str.charAt(str.length() - 1) == ')') {
            str = str.substring(1, str.length() - 1);
            negative = true;
        }
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (ch != ',' && ch != '\'') {
                buf.append(ch);
            }
        }
        str = buf.toString().trim();
        if (str.charAt(0) == '+' || str.charAt(0) == '-') {
            negative ^= (str.charAt(0) == '-');
            str = str.substring(1);
        }
        int exponent = 0;
        int e = Math.max(str.indexOf('e'), str.indexOf('E'));
        if (e != -1) {
            String digits = str.substring(e + 1);
            int sign = 1;
            if (digits.charAt(0) == '+' || digits.charAt(0) == '-') {
                sign = (digits.charAt(0) == '-') ? -1 : 1;
                digits = digits.substring(1);
            }
            if (digits.length() > 4 || digits.indexOf('.') != -1) {
                return null;
            }
            exponent = sign * Integer.parseInt(digits);
            str = str.substring(0, e);
        }
        int point = str.indexOf('.');
        if (point != str.lastIndexOf('.')) {
            return null;
        }
        String fraction = (point == -1) ? "" : str.substring(point + 1);
        String digits = (point == -1) ? str : str.substring(0, point) + fraction;
        int start = 0;
        while (start < digits.length() && digits.charAt(start) == '0') {
            start++;
        }
        digits = digits.substring(start);
        if (digits.isEmpty()) {
            return 0L;
        }
        // The number is the digits times 10 to the shift, in units of the
        // decimals.
        int shift = exponent - fraction.length() + decimals;
        long value;
        if (shift >= 0) {
            if (digits.length() + shift > 18) {
                return null;
            }
            value = Long.parseLong(digits);
            for (int i = 0; i < shift; i++) {
                value *= 10;
            }
        } else {
            int keep = digits.length() + shift;
            if (keep < 0) {
                return 0L;
            }
            if (keep > 18) {
                return null;
            }
            value = (keep == 0) ? 0L : Long.parseLong(digits.substring(0, keep));
            if (digits.charAt(keep) >= '5') {
                value++;
            }
            if (value > MAX_SUM) {
                return null;
            }
        }
        return negative ? -value : value;
    }

    /**
     * Returns the sum in units of the decimals as text: the decimals after a
     * period, and commas between the thousands.
     */
    static String formatSum(long sum, int decimals) {
        StringBuilder digits = new StringBuilder(String.valueOf(Math.abs(sum)));
        while (digits.length() <= decimals) {
            digits.insert(0, '0');
        }
        int point = digits.length() - decimals;
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < point; i++) {
            if (i > 0 && (point - i) % 3 == 0) {
                text.append(',');
            }
            text.append(digits.charAt(i));
        }
        if (decimals > 0) {
            text.append('.').append(digits, point, digits.length());
        }
        return (sum < 0) ? "-" + text : text.toString();
    }

    /**
     * Keeps the row with the specified index on the same page as the next
     * row, as a heading row is kept with the rows under it: a page break does
     * not fall between them, and moves both to the next page. Rows kept with
     * the next one one after another are kept together, unless together they
     * are taller than a page. Call it before the table is drawn.
     *
     * @param index the index of the row.
     * @return this Table object.
     */
    public Table keepRowWithNext(int index) {
        if (index >= 0 && index < tableData.size()) {
            for (Cell cell : tableData.get(index)) {
                cell.properties |= Cell.KEPT_WITH_NEXT;
            }
        }
        return this;
    }

    /**
     * Sets the text alignment in the specified column.
     *
     * @param index     the index of the specified column.
     * @param alignment the specified alignment. Supported values: Alignment.LEFT,
     *                  Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY.
     * @return this Table object.
     */
    public Table setTextAlignmentInColumn(int index, Alignment alignment) {
        for (List<Cell> row : tableData) {
            if (index < row.size()) {
                Cell cell = row.get(index);
                cell.setTextAlignment(alignment);
                if (cell.getTextBlock() != null) {
                    cell.getTextBlock().setTextAlignment(alignment);
                }
            }
        }
        return this;
    }

    /**
     * Sets the color of the text in the specified column.
     *
     * @param index the index of the specified column.
     * @param color the color specified as an integer.
     * @return this Table object.
     */
    public Table setTextColorInColumn(int index, int color) {
        for (List<Cell> row : tableData) {
            if (index < row.size()) {
                Cell cell = row.get(index);
                cell.setTextColor(color);
                if (cell.getTextBlock() != null) {
                    cell.getTextBlock().setTextColor(color);
                }
            }
        }
        return this;
    }

    /**
     * Sets the font and the font size of the cells in the specified column.
     *
     * @param index the column index.
     * @param font  the font.
     * @return this Table object.
     */
    public Table setFontInColumn(int index, Font font) {
        for (List<Cell> row : tableData) {
            if (index < row.size()) {
                Cell cell = row.get(index);
                cell.setFont(font).setFontSize(font.getSize());
                if (cell.getTextBlock() != null) {
                    cell.getTextBlock().setFont(font).setFontSize(font.getSize());
                }
            }
        }
        return this;
    }

    /**
     * Sets the color of the text in the specified row.
     *
     * @param index the index of the specified row.
     * @param color the color specified as an integer.
     * @return this Table object.
     */
    public Table setTextColorInRow(int index, int color) {
        if (index < tableData.size()) {
            List<Cell> row = tableData.get(index);
            for (Cell cell : row) {
                cell.setTextColor(color);
                if (cell.getTextBlock() != null) {
                    cell.getTextBlock().setTextColor(color);
                }
            }
        }
        return this;
    }

    /**
     * Sets the font and the font size of the cells in the specified row.
     *
     * @param index the row index.
     * @param font  the font.
     * @return this Table object.
     */
    public Table setFontInRow(int index, Font font) {
        if (index < tableData.size()) {
            List<Cell> row = tableData.get(index);
            for (Cell cell : row) {
                cell.setFont(font).setFontSize(font.getSize());
                if (cell.getTextBlock() != null) {
                    cell.getTextBlock().setFont(font).setFontSize(font.getSize());
                }
            }
        }
        return this;
    }

    /**
     * Sets the width of the column with the specified index.
     *
     * @param index the index of specified column.
     * @param width the specified width.
     * @return this Table object.
     */
    public Table setColumnWidth(int index, float width) {
        for (List<Cell> row : tableData) {
            if (index < row.size()) {
                row.get(index).setWidth(width);
            }
        }
        return this;
    }

    /**
     * Returns the column width of the column at the specified index.
     *
     * @param index the index of the column.
     * @return the width of the column.
     */
    public float getColumnWidth(int index) {
        return getCellAt(0, index).getWidth();
    }

    /**
     * Returns the cell at the specified row and column.
     *
     * @param row the specified row.
     * @param col the specified column.
     *
     * @return the cell at the specified row and column.
     */
    public Cell getCellAt(int row, int col) {
        if (row >= 0) {
            return tableData.get(row).get(col);
        }
        return tableData.get(tableData.size() + row).get(col);
    }

    /**
     * Returns a list of cells for the specified row.
     *
     * @param index the index of the specified row.
     *
     * @return the list of cells.
     */
    public List<Cell> getRow(int index) {
        return tableData.get(index);
    }

    /**
     * Returns a list of cells for the specified column.
     *
     * @param index the index of the specified column.
     *
     * @return the list of cells.
     */
    public List<Cell> getColumn(int index) {
        List<Cell> column = new ArrayList<Cell>();
        for (List<Cell> row : tableData) {
            if (index < row.size()) {
                column.add(row.get(index));
            }
        }
        return column;
    }

    /**
     * Draws this table on the specified page.
     *
     * @param page the page to draw this table on.
     *
     * @return the x and y coordinates of the bottom right corner of the table.
     * @throws Exception If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        if (tableData.isEmpty()) {
            return new float[] {x1, y1};    // An empty table draws nothing.
        }
        wrapAroundCellText();
        applyRowSpans();
        setRightBorderOnLastColumn();
        setBottomBorderOnLastRow();
        float[] xy = drawTableRows(page, drawHeaderRows(page, 0));
        return new float[] {x1 + getWidth(), xy[1]};
    }

    /**
     * Draws this table on as many new pages as it needs.
     * The pages are created detached and added to the list; add them to the PDF afterwards.
     *
     * @param pdf the PDF document.
     * @param pages the list that receives the new pages.
     * @param pageSize the page size, for example Letter.PORTRAIT.
     * @return the x and y coordinates of the bottom right corner of the table on the last page.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(PDF pdf, List<Page> pages, PageSize pageSize) throws Exception {
        if (tableData.isEmpty()) {
            return new float[] {x1, y1};    // An empty table needs no page.
        }
        wrapAroundCellText();
        applyRowSpans();
        setRightBorderOnLastColumn();
        setBottomBorderOnLastRow();
        float[] xy = null;
        int pageNumber = 1;
        while (hasMoreData()) {
            Page page = new Page(pdf, pageSize, Page.DETACHED);
            pages.add(page);
            xy = drawTableRows(page, drawHeaderRows(page, pageNumber));
            pageNumber++;
        }
        return new float[] {x1 + getWidth(), xy[1]};
    }

    private float[] drawHeaderRows(Page page, int pageNumber) throws Exception {
        float x = x1;
        float y = y1;
        if (pageNumber == 1 && firstPageTopMargin > 0f) {
            y = firstPageTopMargin;
        }
        // In a PDF/UA document the table is a Table element, which the rows
        // drawn on the next pages go on adding to. The header rows are TH
        // cells the first time they are drawn, and artifacts on the next pages.
        boolean first = (rendered == numOfHeaderRows);
        if (page != null && (first || structElement == null)) {
            structElement = page.addStructElement(
                    page.structParent, StructElem.TABLE, null, true);
        }
        if (page != null && !first && numOfHeaderRows > 0) {
            page.addArtifactBMC();
        }
        float[] heights = getRowHeights();
        // The rows that bring a total forward are drawn from the second page
        // on, with the total of the rows before the page.
        int last = -1;
        for (int i = 0; i < numOfHeaderRows && i < tableData.size(); i++) {
            if (!(first && isBroughtForwardRow(i))) {
                last = i;
            }
        }
        if (rendered != -1) {
            setSums(0, numOfHeaderRows, rendered, rendered);
        }
        for (int i = 0; i < numOfHeaderRows && i < tableData.size(); i++) {
            if (first && isBroughtForwardRow(i)) {
                continue;
            }
            List<Cell> row = tableData.get(i);
            if (page != null) {
                if (i == last) {
                    for (Cell cell : row) {
                        cell.setBorder(Border.BOTTOM, true);
                    }
                }
                drawRow(page, row, x, y, heights, i, first ? StructElem.TH : null);
            }
            y += heights[i];
        }
        if (page != null && !first && numOfHeaderRows > 0) {
            page.addEMC();
        }
        return new float[] {x, y};
    }

    // Draws the cells of the row. In a PDF/UA document the row is a TR element
    // and each cell a TH or TD element, which holds what the cell draws; a row
    // that goes on with the wrapped text of the row above adds to its elements.
    // With no cell structure the row is not tagged, as it is an artifact.
    private void drawRow(Page page, List<Cell> row, float x, float y, float[] heights,
            int rowIndex, StructElem cellStructure) throws Exception {
        StructElement parent = page.structParent;
        boolean tagged = (structElement != null && cellStructure != null);
        boolean continued = (row.get(0).properties & Cell.CONTINUED) != 0;
        StructElement rowElement = null;
        if (tagged && !continued && !allCovered(row)) {
            rowElement = page.addStructElement(structElement, StructElem.TR, null);
            if (cellElements == null || cellElements.length != row.size()) {
                cellElements = new StructElement[row.size()];
            }
        }
        int i = 0;
        while (i < row.size()) {
            Cell cell = row.get(i);
            int colspan = Math.max(1, cell.getColSpan());
            boolean covered = (cell.properties & Cell.COVERED) != 0;
            if (tagged && !covered) {
                if (!continued) {
                    cellElements[i] = page.addStructElement(rowElement, cellStructure,
                            getAttributes(cellStructure, colspan, cell.rowsSpanned));
                }
                page.structParent = (cellElements != null && i < cellElements.length) ?
                        cellElements[i] : null;
            }
            float w = 0f;
            for (int j = 0; j < colspan; j++) {
                w += row.get(i++).getWidth();
            }
            if (!covered) {
                // A cell that spans rows is as tall as all the rows it covers.
                float cellHeight = 0f;
                int end = Math.min(rowIndex + cell.rowsSpanned, heights.length);
                for (int r = rowIndex; r < end; r++) {
                    cellHeight += heights[r];
                }
                page.setBrushColor(cell.textColor);
                cell.drawOn(page, x, y, w, cellHeight);
            }
            x += w;
        }
        page.structParent = parent;
    }

    // True when every cell of the row is one that a cell above it spans over,
    // so the row holds no cell of its own and is not a row of the table.
    private static boolean allCovered(List<Cell> row) {
        for (Cell cell : row) {
            if ((cell.properties & Cell.COVERED) == 0) {
                return false;
            }
        }
        return !row.isEmpty();
    }

    // The attributes of a table cell element: the scope of a header cell and
    // the number of rows and columns a cell spans, or null when it has none.
    private static String getAttributes(StructElem cellStructure, int colspan, int rowspan) {
        StringBuilder spans = new StringBuilder();
        if (colspan > 1) {
            spans.append(" /ColSpan ").append(colspan);
        }
        if (rowspan > 1) {
            spans.append(" /RowSpan ").append(rowspan);
        }
        if (cellStructure == StructElem.TH) {
            return "<</O /Table /Scope /Column" + spans + ">>";
        }
        return (spans.length() > 0) ? "<</O /Table" + spans + ">>" : null;
    }

    // Draws the rows from the next row to draw, as many as fit on the page,
    // and the footer rows under them. With no page it measures them all and
    // leaves the next row to draw as it is.
    private float[] drawTableRows(Page page, float[] xy) throws Exception {
        float x = xy[0];
        float y = xy[1];
        float[] heights = getRowHeights();
        int footer = footerStart();
        boolean done = (rendered == -1);
        int index = done ? footer : rendered;
        int first = index;
        // Where the rows start on the next pages, under the header rows.
        float top = y1;
        for (int r = 0; r < numOfHeaderRows && r < heights.length; r++) {
            top += heights[r];
        }
        // Where the rows end on a page, over the footer rows.
        float bottom = (page == null) ? 0f : page.height - bottomMargin;
        for (int r = footer; r < tableData.size(); r++) {
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
            boolean keepLines = (index >= cutLinesUntil);
            boolean keepRows = keepLines && (index >= cutRowsUntil);
            int end = Math.min(rowGroupEnd(index, keepLines, keepRows), footer);
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
                    setFooterSums(first, index);
                    return new float[] {x, drawFooterRows(page, x, y, heights, false)};
                }
            }
            for (int r = index; r < end; r++) {
                if (page != null) {
                    drawRow(page, tableData.get(r), x, y, heights, r, StructElem.TD);
                }
                y += heights[r];
            }
            index = end;
        }
        if (!done) {
            // The rows of the table are all drawn, and the footer rows end it.
            setFooterSums(first, footer);
            y = drawFooterRows(page, x, y, heights, true);
        }
        if (page != null) {
            rendered = -1; // We are done!
        }
        return new float[] {x, y};
    }

    // Draws the footer rows at y and returns the y under them. In a PDF/UA
    // document they are rows of the table where it ends, and artifacts on the
    // pages before.
    private float drawFooterRows(Page page, float x, float y, float[] heights, boolean last)
            throws Exception {
        int footer = footerStart();
        if (page != null && !last && footer < tableData.size()) {
            page.addArtifactBMC();
        }
        for (int r = footer; r < tableData.size(); r++) {
            if (page != null) {
                if (r == footer) {
                    for (Cell cell : tableData.get(r)) {
                        cell.setBorder(Border.TOP, true);
                    }
                }
                drawRow(page, tableData.get(r), x, y, heights, r, last ? StructElem.TD : null);
            }
            y += heights[r];
        }
        if (page != null && !last && footer < tableData.size()) {
            page.addEMC();
        }
        return y;
    }

    // Works out what each cell that spans rows covers, after the text is
    // wrapped: a row of the table is drawn as one row for each line its
    // tallest cell needs, so a cell that spans two rows of the table spans as
    // many rows of the drawing as those two were wrapped into. The cells the
    // span covers are marked, and draw nothing.
    private void applyRowSpans() {
        for (List<Cell> row : tableData) {
            for (Cell cell : row) {
                cell.properties &= ~Cell.COVERED;
                cell.rowsSpanned = 1;
            }
        }
        for (int r = 0; r < tableData.size(); r++) {
            if (isContinuation(r)) {
                continue;       // A span starts in a row of the table, not in the wrap of one.
            }
            List<Cell> row = tableData.get(r);
            int i = 0;
            while (i < row.size()) {
                Cell cell = row.get(i);
                int colspan = Math.max(1, cell.getColSpan());
                if (cell.getRowSpan() > 1 && (cell.properties & Cell.COVERED) == 0) {
                    int end = rowAfter(r, cell.getRowSpan());
                    int ownEnd = rowAfter(r, 1);
                    cell.rowsSpanned = end - r;
                    // The rows the wrapped text of this cell takes keep their
                    // text and lose the border that would cross the cell.
                    for (int r2 = r + 1; r2 < ownEnd; r2++) {
                        setSpanned(r2, i, colspan, false);
                    }
                    for (int r2 = ownEnd; r2 < end; r2++) {
                        setSpanned(r2, i, colspan, true);
                    }
                }
                i += colspan;
            }
        }
    }

    // Marks the columns of the row that a span covers: a covered cell draws
    // nothing, and a row of the wrapped text of the spanning cell keeps its
    // text without the border under it.
    private void setSpanned(int r, int column, int colspan, boolean covered) {
        List<Cell> row = tableData.get(r);
        int i = 0;
        while (i < row.size()) {
            Cell cell = row.get(i);
            if (i >= column && i < column + colspan) {
                if (covered) {
                    cell.properties |= Cell.COVERED;
                } else {
                    cell.setBorder(Border.BOTTOM, false);
                }
            }
            i += Math.max(1, cell.getColSpan());
        }
    }

    // The index of the row after the count rows of the table that start at r,
    // counting the rows the wrapped text of each of them takes.
    private int rowAfter(int r, int count) {
        int index = r;
        for (int i = 0; i < count && index < tableData.size(); i++) {
            index++;
            while (index < tableData.size() && isContinuation(index)) {
                index++;
            }
        }
        return index;
    }

    // True when the row holds the wrapped text of the row above it.
    private boolean isContinuation(int r) {
        List<Cell> row = tableData.get(r);
        return !row.isEmpty() && (row.get(0).properties & Cell.CONTINUED) != 0;
    }

    // The height of each row of the table as it is drawn. A cell that spans
    // rows is not what makes its first row tall; the rows it covers hold it
    // together, and the last of them grows when they do not.
    private float[] getRowHeights() throws Exception {
        float[] heights = new float[tableData.size()];
        for (int r = 0; r < tableData.size(); r++) {
            heights[r] = getMaxCellHeight(tableData.get(r));
        }
        for (int r = 0; r < tableData.size(); r++) {
            List<Cell> row = tableData.get(r);
            for (int i = 0; i < row.size(); i++) {
                Cell cell = row.get(i);
                if (cell.rowsSpanned < 2) {
                    continue;
                }
                int end = Math.min(r + cell.rowsSpanned, tableData.size());
                float have = 0f;
                for (int r2 = r; r2 < end; r2++) {
                    have += heights[r2];
                }
                float needed = cell.getHeight(getTotalWidth(row, i));
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
    private int rowGroupEnd(int index, boolean keepLines, boolean keepRows) {
        int end = index + 1;
        for (int r = index; r < end && r < tableData.size(); r++) {
            for (Cell cell : tableData.get(r)) {
                if (r + cell.rowsSpanned > end) {
                    end = r + cell.rowsSpanned;
                }
            }
            if (r + 1 == end && end < tableData.size()) {
                if (keepLines && isContinuation(end)) {
                    end++;
                } else if (keepRows && isKeptWithNext(r)) {
                    end++;
                }
            }
        }
        return Math.min(end, tableData.size());
    }

    // True when the row, or the row whose wrapped text it holds, is kept with
    // the next row.
    private boolean isKeptWithNext(int r) {
        List<Cell> row = tableData.get(r);
        return !row.isEmpty() && (row.get(0).properties & Cell.KEPT_WITH_NEXT) != 0;
    }

    private float getMaxCellHeight(List<Cell> row) throws Exception {
        float maxCellHeight = 0f;
        float spanned = 0f;
        for (int i = 0; i < row.size(); i++) {
            Cell cell = row.get(i);
            if ((cell.properties & Cell.COVERED) != 0) {
                continue;       // A cell the one above it draws over.
            }
            float cellHeight = cell.getHeight(getTotalWidth(row, i));
            if (cell.rowsSpanned > 1) {
                // A cell that spans rows is as tall as all of them together,
                // which getRowHeights shares out; it is the height of the row
                // only when nothing else is in it.
                spanned = Math.max(spanned, cellHeight / cell.rowsSpanned);
                continue;
            }
            if (cellHeight > maxCellHeight) {
                maxCellHeight = cellHeight;
            }
        }
        return (maxCellHeight > 0f) ? maxCellHeight : spanned;
    }

    /**
     * Returns true if the table contains more data that needs to be drawn on a
     * page.
     *
     * @return whether the table has more data to be drawn on a page.
     */
    private boolean hasMoreData() {
        return rendered != -1;
    }

    /**
     * Returns the width of this table when drawn on a page.
     *
     * @return the width of this table.
     */
    public float getWidth() {
        float tableWidth = 0f;
        if (!tableData.isEmpty()) {
            for (Cell cell : tableData.get(0)) {
                tableWidth += cell.getWidth();
            }
        }
        return tableWidth;
    }

    /**
     * Returns the number of rows below the header rows that are drawn so far,
     * counting each line of wrapped cell text as a row, or -1 when all rows
     * are drawn.
     *
     * @return the number of rendered rows.
     */
    public int getRowsRendered() {
        return rendered == -1 ? rendered : rendered - numOfHeaderRows;
    }

    /**
     * Sets all table cells borders.
     * @param borders true or false.
     * @return this Table object.
     */
    public Table setCellBorders(boolean borders) {
        for (List<Cell> row : tableData) {
            for (Cell cell : row) {
                cell.setBorders(borders);
            }
        }
        return this;
    }

    /**
     * Sets the color of the cell border lines.
     *
     * @param color the color of the cell border lines.
     * @return this Table object.
     */
    public Table setCellBorderColor(int color) {
        for (List<Cell> row : tableData) {
            for (Cell cell : row) {
                cell.setBorderColor(color);
            }
        }
        return this;
    }

    /**
     * Sets the border color of every cell.
     *
     * @param rgbColor the color as red, green and blue components from 0.0 to 1.0.
     * @return this Table object.
     */
    public Table setCellBorderColor(float[] rgbColor) {
        for (List<Cell> row : tableData) {
            for (Cell cell : row) {
                cell.setBorderColor(rgbColor);
            }
        }
        return this;
    }

    /**
     * Sets the width of the cell border lines.
     *
     * @param width the width of the border lines.
     * @return this Table object.
     */
    public Table setCellBorderWidth(float width) {
        for (List<Cell> row : tableData) {
            for (Cell cell : row) {
                cell.setBorderWidth(width);
            }
        }
        return this;
    }

    // Sets the right border on all cells in the last column.
    private void setRightBorderOnLastColumn() {
        for (List<Cell> row : tableData) {
            if (!row.isEmpty() && row.get(0).getBorder(Border.LEFT) == false) {
                return;
            }
        }
        // Only run this code if all the cells in the first column have left border.
        for (List<Cell> row : tableData) {
            Cell cell = null;
            int i = 0;
            while (i < row.size()) {
                cell = row.get(i);
                i += cell.getColSpan();
            }
            if (cell != null) {
                cell.setBorder(Border.RIGHT, true);
            }
        }
    }

    // Sets the bottom border on all cells in the last row.
    private void setBottomBorderOnLastRow() {
        if (tableData.isEmpty()) {
            return;
        }
        List<Cell> firstRow = tableData.get(0);
        for (Cell cell : firstRow) {
            if (cell.getBorder(Border.TOP) == false) {
                return;
            }
        }
        // Only run this code if all the cells in the first row have top border.
        List<Cell> lastRow = tableData.get(tableData.size() - 1);
        for (Cell cell : lastRow) {
            cell.setBorder(Border.BOTTOM, true);
        }
    }

    /**
     * Auto adjusts the widths of all columns so that they are just wide enough to
     * hold the text without truncation.
     *
     * @return this Table object.
     */
    public Table autoAdjustColumnWidths() {
        if (tableData.isEmpty()) {
            return this;
        }
        float[] maxColWidths = new float[tableData.get(0).size()];
        for (List<Cell> row : tableData) {
            for (int i = 0; i < row.size(); i++) {
                Cell cell = row.get(i);
                if (cell.getColSpan() == 1) {
                    TextBlock textBlock = cell.getTextBlock();
                    if (textBlock != null) {
                        String[] tokens = Util.splitOnWhitespace(textBlock.textContent);
                        for (String token : tokens) {
                            float tokenWidth = textBlock.font.stringWidth(textBlock.fallbackFont, token);
                            tokenWidth += cell.getLeftPadding() + cell.getRightPadding();
                            if (tokenWidth > maxColWidths[i]) {
                                maxColWidths[i] = tokenWidth;
                            }
                        }
                    } else if (cell.drawable != null) {
                        float drawableWidth = Cell.measure(cell.drawable)[0] + cell.getLeftPadding() + cell.getRightPadding();
                        if (drawableWidth > maxColWidths[i]) {
                            maxColWidths[i] = drawableWidth;
                        }
                    } else if (cell.text != null) {
                        float textWidth = cell.font.stringWidth(cell.fallbackFont, cell.fontSize, cell.text);
                        textWidth += cell.getLeftPadding() + cell.getRightPadding();
                        if (textWidth > maxColWidths[i]) {
                            maxColWidths[i] = textWidth;
                        }
                    }
                }
            }
        }
        for (List<Cell> row : tableData) {
            for (int i = 0; i < row.size(); i++) {
                row.get(i).setWidth(maxColWidths[i]);
            }
        }
        return this;
    }

    private float getTotalWidth(List<Cell> row, int index) {
        Cell cell = row.get(index);
        int colspan = cell.getColSpan();
        float cellWidth = 0f;
        for (int i = 0; i < colspan; i++) {
            cellWidth += row.get(index + i).getWidth();
        }
        cellWidth -= (cell.getLeftPadding() + row.get(index + (colspan - 1)).getRightPadding());
        return cellWidth;
    }

    /**
     * Wraps around the text in all cells so it fits the column width.
     * This method should be called after all calls to setColumnWidth and
     * autoAdjustColumnWidths.
     */
    protected void wrapAroundCellText() {
        List<List<Cell>> tableData2 = new ArrayList<List<Cell>>();
        List<List<String>> lines = new ArrayList<List<String>>();
        int numOfHeaderRows2 = 0;
        // The footer rows are the rows of the wrap of the rows they were.
        int footer = (numOfFooterRows > 0) ? footerStart() : tableData.size();
        int footer2 = 0;
        for (int r = 0; r < tableData.size(); r++) {
            List<Cell> row = tableData.get(r);
            int first = tableData2.size();
            if (r == footer) {
                footer2 = first;
            }
            tableData2.add(row);    // Add the original row
            // Every cell of the row is wrapped once, here. The lines it needs
            // are what the cells stacked below it get, and the most lines any
            // cell of the row needs is how many rows to stack. The rows added
            // below a header row are header rows too.
            lines.clear();
            int maxNumVerCells = 1;
            for (int i = 0; i < row.size(); i++) {
                // A cell that draws a line of text of its own draws no cell
                // text, so there is nothing to wrap. A cell that shows a sum
                // is not wrapped either, as its text is the sum of each page.
                List<String> cellLines = (row.get(i).text == null
                        || row.get(i).drawable instanceof BaselineDrawable
                        || (row.get(i).properties & Cell.SUM_BITS) != 0)
                        ? null : wrapCellText(row, i);
                lines.add(cellLines);
                if (cellLines != null && cellLines.size() > maxNumVerCells) {
                    maxNumVerCells = cellLines.size();
                }
            }
            // A cell whose text wraps is one cell drawn as the rows its lines
            // take, so the border under it belongs under the last of them and
            // not under every line of it. A cell that spans rows is drawn over
            // all of them at once and so draws its own bottom border under the
            // whole of it; applyRowSpans clears the rows of its wrap.
            boolean[] bottomBorder = new boolean[row.size()];
            for (int i = 0; i < row.size(); i++) {
                Cell cell = row.get(i);
                bottomBorder[i] = maxNumVerCells > 1 && cell.getRowSpan() == 1
                        && cell.getBorder(Border.BOTTOM);
                if (bottomBorder[i]) {
                    cell.setBorder(Border.BOTTOM, false);
                }
            }
            for (int i = 1; i < maxNumVerCells; i++) {
                List<Cell> row2 = new ArrayList<Cell>();
                for (int j = 0; j < row.size(); j++) {
                    Cell cell = row.get(j);
                    Cell cell2 = new Cell(cell.getFont());
                    cell2.setFallbackFont(cell.getFallbackFont());
                    cell2.setFontSize(cell.fontSize);
                    cell2.setWidth(cell.getWidth());
                    cell2.setLeftPadding(cell.getLeftPadding());
                    cell2.setRightPadding(cell.getRightPadding());
                    cell2.backgroundColor = cell.backgroundColor;
                    cell2.setBorderWidth(cell.getBorderWidth());
                    cell2.borderColor = cell.borderColor;
                    cell2.textColor = cell.textColor;
                    cell2.setColSpan(cell.getColSpan());
                    cell2.properties = cell.properties;
                    cell2.setTextAlignment(cell.getTextAlignment());
                    cell2.setVerticalAlignment(cell.getVerticalAlignment());
                    cell2.setTopPadding(0f);
                    cell2.setBorder(Border.TOP, false);
                    cell2.setBorder(Border.BOTTOM, bottomBorder[j] && i == maxNumVerCells - 1);
                    cell2.properties |= Cell.CONTINUED;
                    cell2.properties &= ~Cell.SUM_BITS;
                    row2.add(cell2);
                }
                tableData2.add(row2);
            }
            for (int j = 0; j < row.size(); j++) {
                List<String> cellLines = lines.get(j);
                if (cellLines != null) {
                    for (int n = 0; n < cellLines.size(); n++) {
                        tableData2.get(first + n).get(j).setText(cellLines.get(n));
                    }
                }
            }
            // The stacked rows are wrapped in their turn, as they were when
            // the rows were all added first and the table wrapped in one pass
            // afterwards: a line that ends in a space loses it here.
            for (int i = first + 1; i < tableData2.size(); i++) {
                List<Cell> row2 = tableData2.get(i);
                for (int j = 0; j < row2.size(); j++) {
                    if (row2.get(j).text != null) {
                        List<String> cellLines = wrapCellText(row2, j);
                        for (int n = 0; n < cellLines.size(); n++) {
                            tableData2.get(i + n).get(j).setText(cellLines.get(n));
                        }
                    }
                }
            }
            if (r < numOfHeaderRows) {
                numOfHeaderRows2 = tableData2.size();
            }
        }
        if (rendered != -1) {
            rendered += numOfHeaderRows2 - numOfHeaderRows;
        }
        numOfHeaderRows = numOfHeaderRows2;
        if (footer < tableData.size()) {
            numOfFooterRows = tableData2.size() - footer2;
        }
        tableData = tableData2;
    }

    // The lines the text of the cell needs to fit the width of its column.
    // A token wider than the column is broken between two of its characters.
    private List<String> wrapCellText(List<Cell> row, int index) {
        Cell cell = row.get(index);
        float cellWidth = getTotalWidth(row, index);
        List<String> lines = new ArrayList<String>();
        StringBuilder buf = new StringBuilder();
        for (String token : Util.splitOnWhitespace(cell.text)) {
            if (cell.font.stringWidth(cell.fallbackFont, cell.fontSize, token) > cellWidth) {
                if (buf.length() > 0) {
                    buf.append(" ");
                }
                for (int k = 0; k < token.length(); k++) {
                    if (cell.font.stringWidth(cell.fallbackFont, cell.fontSize,
                            buf.toString() + token.charAt(k)) > cellWidth) {
                        lines.add(buf.toString());
                        buf.setLength(0);
                    }
                    buf.append(token.charAt(k));
                }
            } else if (buf.length() == 0) {
                // A token that fits the column fits a line of its own, and its
                // width is the one measured just above.
                buf.append(token);
            } else if (cell.font.stringWidth(cell.fallbackFont, cell.fontSize,
                    (buf.toString() + " " + token).trim()) > cellWidth) {
                lines.add(buf.toString().trim());
                buf.setLength(0);
                buf.append(token);
            } else {
                buf.append(" ");
                buf.append(token);
            }
        }
        lines.add(buf.toString().trim());
        return lines;
    }

    /**
     *  Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
     *
     *  @param row the list of cells.
     *  @param index the index of the column.
     *  @return the number of vertical cells needed to wrap around the cell text.
     */
    protected int getNumVerCells(List<Cell> row, int index) {
        if (row.get(index).text == null) {
            return 1;
        }
        return wrapCellText(row, index).size();
    }


    // The delimiter of the line: the commonest of a comma, a pipe and a tab.
    // The ones inside a quoted field are not counted, or a file whose values
    // hold commas could be split on the wrong character altogether.
    private String getDelimiter(String str) {
        int comma = 0;
        int pipe = 0;
        int tab = 0;
        boolean quoted = false;
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
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

    /**
     * Keeps only the specified columns in this table.
     *
     * @param columns the indexes of the columns to keep.
     * @return this Table object.
     */
    public Table setVisibleColumns(Integer... columns) {
        List<List<Cell>> list = new ArrayList<List<Cell>>();
        List<Integer> visible = Arrays.asList(columns);
        for (List<Cell> row : tableData) {
            List<Cell> row2 = new ArrayList<Cell>();
            for (int i = 0; i < row.size(); i++) {
                if (visible.contains(i)) {
                    row2.add(row.get(i));
                }
            }
            list.add(row2);
        }
        tableData = list;
        return this;
    }

    /**
     * Sets the top margin on the first page, when the table is drawn on several pages.
     *
     * @param firstPageTopMargin the top margin.
     * @return this Table object.
     */
    public Table setFirstPageTopMargin(float firstPageTopMargin) {
        this.firstPageTopMargin = firstPageTopMargin;
        return this;
    }
} // End of Table.java
