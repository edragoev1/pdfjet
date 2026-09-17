/*
 * Table.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Used to create table objects and draw them on a page.
 *
 * Please see Example_08.
 */
public class Table implements Drawable {
    private List<List<Cell>> tableData;
    private int numOfHeaderRows = 1;
    // The index of the next row to draw, or -1 when all rows are drawn.
    private int rendered = 1;
    private float x1;
    private float y1;
    private float firstPageTopMargin;
    private float bottomMargin;

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
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(fileName), StandardCharsets.UTF_8));
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
        for (int i = 0; i < numOfHeaderRows && i < tableData.size(); i++) {
            List<Cell> row = tableData.get(i);
            float h = getMaxCellHeight(row);
            int j = 0;
            while (j < row.size()) {
                Cell cell = row.get(j);
                int colspan = cell.getColSpan();
                float w = 0f;
                for (int k = 0; k < colspan; k++) {
                    w += row.get(j++).getWidth();
                }
                if (page != null) {
                    page.setBrushColor(cell.textColor);
                    if (i == (numOfHeaderRows - 1)) {
                        cell.setBorder(Border.BOTTOM, true);
                    }
                    cell.drawOn(page, x, y, w, h);
                }
                x += w;
            }
            x = x1;
            y += h;
        }
        return new float[] {x, y};
    }

    // Draws the rows from the next row to draw, as many as fit on the page.
    // With no page it measures them all and leaves the next row to draw as it is.
    private float[] drawTableRows(Page page, float[] xy) throws Exception {
        float x = xy[0];
        float y = xy[1];
        int index = (rendered == -1) ? tableData.size() : rendered;
        int first = index;
        while (index < tableData.size()) {
            List<Cell> row = tableData.get(index);
            float h = getMaxCellHeight(row);
            // A row that does not fit goes on the next page, unless it is the
            // first row of this one: a row taller than the page fits no page,
            // and leaving it for the next page would ask for pages forever.
            if (page != null && (y + h) > (page.height - bottomMargin) && index > first) {
                rendered = index;
                return new float[] {x, y};
            }
            int i = 0;
            while (i < row.size()) {
                Cell cell = row.get(i);
                int colspan = cell.getColSpan();
                float w = 0f;
                for (int j = 0; j < colspan; j++) {
                    w += row.get(i++).getWidth();
                }
                if (page != null) {
                    page.setBrushColor(cell.textColor);
                    cell.drawOn(page, x, y, w, h);
                }
                x += w;
            }
            x = x1;
            y += h;
            index++;
        }
        if (page != null) {
            rendered = -1; // We are done!
        }
        return new float[] {x, y};
    }

    private float getMaxCellHeight(List<Cell> row) throws Exception {
        float maxCellHeight = 0f;
        for (int i = 0; i < row.size(); i++) {
            Cell cell = row.get(i);
            float totalWidth = getTotalWidth(row, i);
            float cellHeight = cell.getHeight(totalWidth);
            if (cellHeight > maxCellHeight) {
                maxCellHeight = cellHeight;
            }
        }
        return maxCellHeight;
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
                            tokenWidth += cell.leftPadding + cell.rightPadding;
                            if (tokenWidth > maxColWidths[i]) {
                                maxColWidths[i] = tokenWidth;
                            }
                        }
                    } else if (cell.drawable != null) {
                        float drawableWidth = Cell.measure(cell.drawable)[0] + cell.leftPadding + cell.rightPadding;
                        if (drawableWidth > maxColWidths[i]) {
                            maxColWidths[i] = drawableWidth;
                        }
                    } else if (cell.text != null) {
                        float textWidth = cell.font.stringWidth(cell.fallbackFont, cell.fontSize, cell.text);
                        textWidth += cell.leftPadding + cell.rightPadding;
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
        cellWidth -= (cell.leftPadding + row.get(index + (colspan - 1)).rightPadding);
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
        for (int r = 0; r < tableData.size(); r++) {
            List<Cell> row = tableData.get(r);
            int first = tableData2.size();
            tableData2.add(row);    // Add the original row
            // Every cell of the row is wrapped once, here. The lines it needs
            // are what the cells stacked below it get, and the most lines any
            // cell of the row needs is how many rows to stack. The rows added
            // below a header row are header rows too.
            lines.clear();
            int maxNumVerCells = 1;
            for (int i = 0; i < row.size(); i++) {
                // A cell that draws a line of text of its own draws no cell
                // text, so there is nothing to wrap.
                List<String> cellLines = (row.get(i).text == null
                        || row.get(i).drawable instanceof BaselineDrawable)
                        ? null : wrapCellText(row, i);
                lines.add(cellLines);
                if (cellLines != null && cellLines.size() > maxNumVerCells) {
                    maxNumVerCells = cellLines.size();
                }
            }
            for (int i = 1; i < maxNumVerCells; i++) {
                List<Cell> row2 = new ArrayList<Cell>();
                for (Cell cell : row) {
                    Cell cell2 = new Cell(cell.getFont());
                    cell2.setFallbackFont(cell.getFallbackFont());
                    cell2.setFontSize(cell.fontSize);
                    cell2.setWidth(cell.getWidth());
                    cell2.setLeftPadding(cell.leftPadding);
                    cell2.setRightPadding(cell.rightPadding);
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
