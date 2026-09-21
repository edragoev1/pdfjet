/*
 * BigTable.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedInputStream;
import java.io.Closeable;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.UncheckedIOException;
import java.util.*;

/**
 * Use this class if you have a lot of data. The rows are read from a
 * delimited text file, or from an Iterable, one at a time, and each page is
 * written as soon as it is full, so the memory stays flat however many rows
 * there are. A PDF/UA document holds no more than that either: the table is
 * tagged as a table, and the structure elements of a page are written with
 * it. What is left is one cross-reference entry for each of them, which every
 * object of a PDF has.
 */
public class BigTable {
    private final PDF pdf;
    private final Font f1;
    private final Font f2;
    private final PageSize pageSize;
    private float x;
    private float y;
    private float yText;
    private List<Page> pages;
    private Page page;
    private float[] widths;
    private String[] headerFields;
    private Alignment[] alignment;
    private float[] vertLines;
    private float bottomMargin = 20.0f;
    private float padding = 2.0f;
    private boolean highlightRow = true;
    // In a PDF/UA document the table is a Table element that the rows of
    // every page go on adding to; see drawFieldsAndLine.
    private StructElement structElement = null;
    private float[] shadingColor = rgb(0xF0F0F0);   // null for no shading
    private float[] borderColor = rgb(0xB0B0B0);    // null for no lines
    private String footerText = "Page {page} of {pages}";
    private Font footerFont;                        // null for the header font
    private Iterable<String[]> rows;
    private boolean checkLineBreaks;        // The rows are not read from a file, which has no line breaks left
    private int[] columns = new int[0];     // The fields drawn, in the order they are drawn
    private int numberOfColumns;            // The length of columns
    private int fieldsNeeded;               // The fields a row needs: the largest index plus 1
    private boolean startNewPage = true;
    private int dataRows;           // The rows under the header, counted by setTableData
    private int pageCount;          // The pages they take, counted by complete()
    private int pageNumber;         // The page being drawn
    private boolean footerDrawn;

    /**
     * Creates a table and sets the fonts and page size.
     *
     * @param pdf the font.
     * @param f1 the header font.
     * @param f2 the body font.
     * @param pageSize specifies the page size.
     */
    public BigTable(PDF pdf, Font f1, Font f2, PageSize pageSize) {
        this.pdf = pdf;
        this.f1 = f1;
        this.f2 = f2;
        this.pageSize = pageSize;
        this.pages = new ArrayList<Page>();
    }

    /**
     * Sets the location where this table will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the table box.
     * @param y the y coordinate of the top left corner of the table box.
     * @return this BigTable object.
     */
    public BigTable setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        if (this.vertLines != null) {
            setVertLines();
        }
        return this;
    }

    /**
     * Sets the number of columns in this table: the first fields of every row,
     * in their order. It is the same as setColumns(0, 1, ... numberOfColumns - 1).
     *
     * @param numberOfColumns the number of columns.
     * @return this BigTable object.
     */
    public BigTable setNumberOfColumns(int numberOfColumns) {
        int[] columns = new int[Math.max(numberOfColumns, 0)];
        for (int i = 0; i < columns.length; i++) {
            columns[i] = i;
        }
        return setColumns(columns);
    }

    /**
     * Sets the fields of every row that the table draws, by their index from
     * 0, in the order they are drawn, so setColumns(3, 0, 11) draws the fourth
     * field, then the first, then the twelfth. The indexes pick the header
     * fields the same way. A row without a field for every index is skipped.
     * Call it before setTableData; setTextAlignment counts the columns as they
     * are drawn.
     *
     * @param columns the indexes of the fields to draw.
     * @return this BigTable object.
     */
    public BigTable setColumns(int... columns) {
        int fieldsNeeded = 0;
        for (int column : columns) {
            if (column < 0) {
                pdf.fail(new IllegalArgumentException("A column index cannot be negative."));
            }
            fieldsNeeded = Math.max(fieldsNeeded, column + 1);
        }
        this.columns = columns.clone();
        this.numberOfColumns = columns.length;
        this.fieldsNeeded = fieldsNeeded;
        return this;
    }

    /**
     * Sets the text alignment in the specified column, which is one of the
     * columns of the table. Call it after setTableData, which makes the
     * columns: a column that the table does not have is refused.
     *
     * @param column the column.
     * @param alignment the alignment.
     * @return this BigTable object.
     */
    public BigTable setTextAlignment(int column, Alignment alignment) {
        if (this.alignment == null || column < 0 || column >= this.alignment.length) {
            pdf.fail(new IllegalArgumentException("The table has no column " + column
                    + ": set the alignment of a column after setTableData."));
        }
        this.alignment[column] = alignment;
        return this;
    }

    /**
     * Sets the color of every other row, starting with the header.
     * Color.transparent turns the shading off.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.lightgray.
     * @return this BigTable object.
     */
    public BigTable setShadingColor(int color) {
        this.shadingColor = (color == Color.transparent) ? null : rgb(color);
        return this;
    }

    /**
     * Sets the color of every other row, starting with the header, from its
     * red, green and blue values from 0 to 1. null turns the shading off.
     *
     * @param color the red, green and blue values.
     * @return this BigTable object.
     */
    public BigTable setShadingColor(float[] color) {
        this.shadingColor = Util.copyOf(color);
        return this;
    }

    /**
     * Sets the color of the lines between the rows and the columns and around
     * the table. Color.transparent leaves the lines out.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.gray.
     * @return this BigTable object.
     */
    public BigTable setBorderColor(int color) {
        this.borderColor = (color == Color.transparent) ? null : rgb(color);
        return this;
    }

    /**
     * Sets the color of the lines between the rows and the columns and around
     * the table from its red, green and blue values from 0 to 1. null leaves
     * the lines out.
     *
     * @param color the red, green and blue values.
     * @return this BigTable object.
     */
    public BigTable setBorderColor(float[] color) {
        this.borderColor = Util.copyOf(color);
        return this;
    }

    /**
     * Sets the space between the text of a column and the lines on its left
     * and right. It is 2 points by default.
     *
     * @param padding the padding.
     * @return this BigTable object.
     */
    public BigTable setPadding(float padding) {
        if (padding < 0f) {
            pdf.fail(new IllegalArgumentException("The padding cannot be negative."));
        }
        this.padding = padding;
        if (this.vertLines != null) {
            setVertLines();
        }
        return this;
    }

    /**
     * Sets the footer drawn at the bottom of every page, centered. In the
     * text, {page} stands for the number of the page and {pages} for the
     * number of pages. The footer is "Page {page} of {pages}" in the header
     * font by default. A null or empty text leaves the footer out.
     *
     * @param text the text, for example "Seite {page} von {pages}".
     * @param font the font, or null for the header font.
     * @return this BigTable object.
     */
    public BigTable setFooter(String text, Font font) {
        this.footerText = text;
        this.footerFont = font;
        return this;
    }

    /**
     * Sets the bottom margin.
     *
     * @param bottomMargin the bottom margin.
     * @return this BigTable object.
     */
    public BigTable setBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
        return this;
    }

    /**
     * Returns the pages, which complete() has already added to the PDF.
     *
     * @return the pages.
     */
    public List<Page> getPages() {
        return pages;
    }

    // Creates the next page. It is added to the PDF right away, so the content
    // of the page before it is compressed and written, and its memory freed.
    private void newPage() throws Exception {
        page = new Page(pdf, pageSize);
        pages.add(page);
        pageNumber++;
        footerDrawn = false;
        page.setPenWidth(0f);
        this.yText = this.y + f1.ascent;
        this.highlightRow = true;
        // The header fields are the TH cells of the table the first time they
        // are drawn, and an artifact where they repeat on the next pages.
        StructElem header = null;
        if (structElement == null) {
            structElement = page.addStructElement(
                    page.structParent, StructElem.TABLE, null, true);
            header = StructElem.TH;
        }
        drawFieldsAndLine(headerFields, f1, header);
        this.yText += f1.descent + f2.ascent;
        startNewPage = false;
    }

    // Draws the footer of the page that was just finished, once. The page is
    // finished before the next one is created, so it is written complete.
    private void drawFooter() throws Exception {
        if (!footerDrawn && footerText != null && !footerText.isEmpty()) {
            String text = footerText
                    .replace("{page}", String.valueOf(pageNumber))
                    .replace("{pages}", String.valueOf(pageCount));
            // The page number repeats on every page, which makes it an artifact.
            page.addArtifactBMC();
            page.addFooter(new TextLine((footerFont != null) ? footerFont : f1, text));
            page.addEMC();
        }
        footerDrawn = true;
    }

    private void drawTextAndLine(String[] fields) throws Exception {
        if (startNewPage) {     // Create new page
            newPage();
        }

        drawFieldsAndLine(fields, f2, StructElem.TD);
        this.yText += f2.descent + f2.ascent;
        if (this.yText > (this.page.height - this.bottomMargin)) {
            drawTheVerticalLines();
            drawFooter();
            startNewPage = true;
        }
    }

    // Counts the pages the rows take, as drawTextAndLine breaks them, so that
    // the "Page i of N" footer of a page can be drawn before the next page is
    // created. The location and the bottom margin are set after setTableData,
    // so the pages are counted when the table is drawn.
    private int countPages() {
        float pageHeight = pageSize.getHeight();
        float yTop = this.y + f1.ascent + f1.descent + f2.ascent;
        float yPos = yTop;
        int count = 1;
        boolean newPage = false;
        for (int i = 0; i < this.dataRows; i++) {
            if (newPage) {
                count++;
                yPos = yTop;
                newPage = false;
            }
            yPos += f2.descent + f2.ascent;
            if (yPos > (pageHeight - this.bottomMargin)) {
                newPage = true;
            }
        }
        return count;
    }

    // Draws a row of the table. In a PDF/UA document the row is a TR element
    // and each field a TH or TD element that holds the text, and the shading
    // and the lines are artifacts. A row with no cell structure is drawn as an
    // artifact, which is what the header rows that repeat on the next pages
    // are.
    private void drawFieldsAndLine(String[] fields, Font font, StructElem cellStructure) {
        // The shading and the line above the text carry no meaning of their own.
        page.addArtifactBMC();
        if (this.highlightRow) {
            if (shadingColor != null) {
                highlightRow(page, font, shadingColor);
            }
            this.highlightRow = false;
        } else {
            this.highlightRow = true;
        }

        // Draw the line above the text.
        if (borderColor != null) {
            float[] original = page.getPenColor();
            page.setPenColor(borderColor);
            page.moveTo(vertLines[0], this.yText - font.ascent);
            page.lineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
            page.strokePath();
            page.setPenColor(original);
        }
        page.addEMC();
        page.setBrushColor(Color.black);

        boolean tagged = structElement != null && cellStructure != null;
        StructElement parent = page.structParent;
        StructElement rowElement = null;
        if (tagged) {
            rowElement = page.addStructElement(structElement, StructElem.TR, null);
        }
        for (int i = 0; i < this.numberOfColumns; i++) {
            String text = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]];
            float xText = vertLines[i] + this.padding;
            if (alignment[i] == Alignment.RIGHT) {
                xText = (vertLines[i + 1] - this.padding) - font.stringWidth(text);
            }
            if (tagged) {
                // A cell holds its text and nothing else, so the cell element
                // holds the marked content of the text: it needs no paragraph
                // of its own, which would be another object for every cell.
                page.structParent = rowElement;
                page.addBDC(cellStructure, null, null, text, cellAttributes(cellStructure));
            } else {
                page.addArtifactBMC();
            }
            page.drawTextLine(font, text, xText, this.yText);
            page.addEMC();
        }
        page.structParent = parent;
    }

    // The attributes of a cell element: a header cell heads the column it is in.
    private static String cellAttributes(StructElem cellStructure) {
        return (cellStructure == StructElem.TH) ? "<</O /Table /Scope /Column>>" : null;
    }

    private void highlightRow(Page page, Font font, float[] color) {
        float[] original = page.getBrushColor();
        page.setBrushColor(color);
        page.fillRectBetween(vertLines[0], this.yText - font.ascent,
                vertLines[this.numberOfColumns], this.yText + font.descent);
        page.setBrushColor(original);
    }

    private void drawTheVerticalLines() {
        if (borderColor == null) {
            return;
        }
        // The lines of the table carry no meaning of their own.
        page.addArtifactBMC();
        float[] original = page.getPenColor();
        page.setPenColor(borderColor);
        for (int i = 0; i <= this.numberOfColumns; i++) {
            page.drawLine(
                    vertLines[i],
                    this.y,
                    vertLines[i],
                    this.yText - f2.ascent);
        }
        // Draw the last horizontal line
        page.moveTo(vertLines[0], this.yText - f2.ascent);
        page.lineTo(vertLines[this.numberOfColumns], this.yText - f2.ascent);
        page.strokePath();
        page.setPenColor(original);
        page.addEMC();
    }

    private static float[] rgb(int color) {
        return new float[] {
                ((color >> 16) & 0xff)/255f, ((color >> 8) & 0xff)/255f, (color & 0xff)/255f};
    }

    // A number is right-aligned, as Table.rightAlignNumbers aligns it.
    private Alignment getAlignment(String str) {
        return Table.isNumber(str) ? Alignment.RIGHT : Alignment.LEFT;
    }

    /**
     * Sets the column widths, the column alignment and header fields from a
     * delimited text file, read as UTF-8. Its first line with a field for
     * every column is the header, and the lines after it are the rows. A
     * quoted field is read as RFC 4180 reads it, so a delimiter inside one is
     * text, and a line break inside one goes on to the next line of the file
     * and is drawn as a space.
     *
     * @param fileName the file name.
     * @param delimiter the delimiter.
     * @throws IOException if there is an issue.
     * @return this BigTable object.
     */
    public BigTable setTableData(String fileName, String delimiter) throws IOException {
        int fieldsNeeded = this.fieldsNeeded;
        try {
            String[] header = new String[0];
            try (DataFileRows lines = DataFileRows.open(fileName, delimiter, fieldsNeeded, false)) {
                while (lines.hasNext()) {
                    String[] fields = lines.next();
                    if (fields.length >= fieldsNeeded) {
                        header = fields;
                        break;
                    }
                }
            }
            return setTableData(header, false, () -> {
                try {
                    return DataFileRows.open(fileName, delimiter, fieldsNeeded, true);
                } catch (IOException e) {
                    throw new UncheckedIOException(e);
                }
            });
        } catch (UncheckedIOException e) {
            throw e.getCause();
        }
    }

    /**
     * Sets the column widths, the column alignment and header fields from
     * rows that are not in a file: the results of a query, or a list of
     * objects. The rows are iterated twice, once here to measure the columns
     * and once by complete() to draw them, and neither keeps them, so an
     * Iterable whose iterator() runs the query again, or maps the objects to
     * fields as it goes, keeps the memory flat. An iterator that is Closeable
     * is closed when the table is done with it. A row without a field for
     * every column is skipped, as a short line of a file is, and a line break
     * in a field is drawn as a space.
     *
     * @param header the header fields, with a field for every column.
     * @param rows the fields of each row, in the order they are drawn.
     * @return this BigTable object.
     */
    public BigTable setTableData(String[] header, Iterable<String[]> rows) {
        return setTableData(header, true, rows);
    }

    // With checkLineBreaks, each field is looked at for line breaks to draw as
    // spaces; the rows of a data file have them as spaces already.
    private BigTable setTableData(String[] header, boolean checkLineBreaks, Iterable<String[]> rows) {
        this.checkLineBreaks = checkLineBreaks;
        if (header.length < this.fieldsNeeded) {
            pdf.fail(new IllegalArgumentException(
                    "The header does not have a field for every column."));
        }
        this.rows = rows;
        this.vertLines = new float[this.numberOfColumns + 1];
        this.headerFields = header.clone();
        this.widths = new float[this.numberOfColumns];
        this.alignment = new Alignment[this.numberOfColumns];

        measure(header, f1);
        int rowNumber = 0;
        Iterator<String[]> iterator = rows.iterator();
        try {
            while (iterator.hasNext()) {
                String[] fields = iterator.next();
                if (fields.length < this.fieldsNeeded) {
                    continue;
                }
                if (rowNumber == 0) {    // Determine alignment from first data row
                    for (int i = 0; i < this.numberOfColumns; i++) {
                        alignment[i] = getAlignment(fields[columns[i]]);
                    }
                }
                measure(fields, f2);
                rowNumber++;
            }
        } finally {
            close(iterator);
        }
        this.dataRows = rowNumber;

        setVertLines();
        return this;
    }

    // Widens the columns to fit the fields of a row, measured in the font the
    // row is drawn with: the header font for the header and the body font for
    // a row under it. The widths are those of the text, and setVertLines adds
    // the padding, so it can be set later.
    private void measure(String[] fields, Font font) {
        for (int i = 0; i < this.numberOfColumns; i++) {
            String text = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]];
            float width = font.stringWidth(text);
            if (width > widths[i]) {
                this.widths[i] = width;
            }
        }
    }

    private static void close(Iterator<String[]> iterator) {
        if (iterator instanceof Closeable) {
            try {
                ((Closeable) iterator).close();
            } catch (IOException e) {
                throw new UncheckedIOException(e);
            }
        }
    }

    // The fields of the lines of a data file, read as UTF-8, after the byte
    // order mark at its start, if there is one. The lines are split at the
    // delimiter, which is not a regular expression, keeping the empty fields
    // at the end of a line and reading the quoted fields as RFC 4180 does, as
    // the other ports do: a quoted field with line breaks takes the lines up
    // to its closing quote. An iterator cannot throw an IOException, so it
    // throws an UncheckedIOException, and setTableData and complete() throw
    // its cause.
    private static final class DataFileRows implements Iterator<String[]>, Closeable {
        private final UTF8.LineReader reader;
        private final String delimiter;
        private String[] next;

        private DataFileRows(UTF8.LineReader reader, String delimiter) {
            this.reader = reader;
            this.delimiter = delimiter;
        }

        // With skipHeader, the rows start after the header: the first line
        // with at least fieldsNeeded fields.
        static DataFileRows open(String fileName, String delimiter, int fieldsNeeded, boolean skipHeader)
                throws IOException {
            UTF8.LineReader reader = new UTF8.LineReader(
                    new BufferedInputStream(new FileInputStream(fileName)));
            DataFileRows rows = new DataFileRows(reader, delimiter);
            try {
                reader.skipByteOrderMark();
                rows.advance();
                if (skipHeader) {
                    while (rows.next != null && rows.next.length < fieldsNeeded) {
                        rows.advance();
                    }
                    rows.advance();
                }
            } catch (IOException | RuntimeException e) {
                reader.close();
                throw e;
            }
            return rows;
        }

        private void advance() {
            try {
                String line = reader.readLine();
                next = (line == null) ? null : Util.readRecord(line, reader, delimiter);
            } catch (IOException e) {
                throw new UncheckedIOException(e);
            }
        }

        @Override
        public boolean hasNext() {
            return next != null;
        }

        @Override
        public String[] next() {
            if (next == null) {
                throw new NoSuchElementException();
            }
            String[] fields = next;
            advance();
            return fields;
        }

        @Override
        public void close() throws IOException {
            reader.close();
        }
    }

    // Sets the x coordinates of the vertical lines from the location, the
    // column widths and the padding.
    private void setVertLines() {
        float vertLineX = this.x;
        this.vertLines[0] = vertLineX;
        for (int i = 0; i < widths.length; i++) {
            vertLineX += this.widths[i] + 2*this.padding;
            this.vertLines[i + 1] = vertLineX;
        }
    }

    /**
     * Draws the rows, then the vertical lines, with the footer on every page. The pages are added to the PDF as they are drawn, so the
     * document does not hold them all. Call it after the location, the bottom
     * margin and the table data have been set.
     *
     * @throws Exception if the data file cannot be read or drawing fails.
     */
    public void complete() throws Exception {
        this.pageCount = countPages();
        newPage();
        Iterator<String[]> iterator = rows.iterator();
        try {
            while (iterator.hasNext()) {
                String[] fields = iterator.next();
                if (fields.length < this.fieldsNeeded) {
                    continue;
                }
                this.drawTextAndLine(fields);
            }
        } catch (UncheckedIOException e) {
            throw e.getCause();
        } finally {
            close(iterator);
        }
        drawTheVerticalLines();
        drawFooter();
    }
}
