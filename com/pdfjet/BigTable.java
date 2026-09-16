package com.pdfjet;

import java.io.BufferedReader;
import java.io.Closeable;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.UncheckedIOException;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Use this class if you have a lot of data. The rows are read from a
 * delimited text file, or from an Iterable, one at a time, and each page is
 * written as soon as it is full, so the memory stays flat however many rows
 * there are.
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
    private int highlightColor = 0xF0F0F0;
    private int penColor = 0xB0B0B0;
    private Iterable<String[]> rows;
    private int numberOfColumns;    // Total column count
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
     * Sets the number of columns in this table.
     *
     * @param numberOfColumns the number of columns.
     * @return this BigTable object.
     */
    public BigTable setNumberOfColumns(int numberOfColumns) {
        this.numberOfColumns = numberOfColumns;
        return this;
    }

    /**
     * Sets the text alignment in the specified column.
     *
     * @param column the column.
     * @param alignment the alignment.
     * @return this BigTable object.
     */
    public BigTable setTextAlignment(int column, Alignment alignment) {
        this.alignment[column] = alignment;
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
        drawFieldsAndLine(headerFields, f1);
        this.yText += f1.descent + f2.ascent;
        startNewPage = false;
    }

    // Draws the footer of the page that was just finished, once. The page is
    // finished before the next one is created, so it is written complete.
    private void drawFooter() throws Exception {
        if (!footerDrawn) {
            page.addFooter(new TextLine(f1, "Page " + pageNumber + " of " + pageCount));
            footerDrawn = true;
        }
    }

    private void drawTextAndLine(String[] fields) throws Exception {
        if (startNewPage) {     // Create new page
            newPage();
        }

        drawFieldsAndLine(fields, f2);
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

    private void drawFieldsAndLine(String[] fields, Font font) {
        if (this.highlightRow) {
            highlightRow(page, font, highlightColor);
            this.highlightRow = false;
        } else {
            this.highlightRow = true;
        }

        // Draw the line above the text.
        float[] original = page.getPenColor();
        page.setPenColor(penColor);
        page.moveTo(vertLines[0], this.yText - font.ascent);
        page.lineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
        page.strokePath();
        page.setPenColor(original);
        page.setBrushColor(Color.black);

        for (int i = 0; i < this.numberOfColumns; i++) {
            String text = fields[i];
            float xText = vertLines[i] + this.padding;
            if (alignment[i] == Alignment.RIGHT) {
                xText = (vertLines[i + 1] - this.padding) - font.stringWidth(text);
            }
            page.drawTextLine(font, text, xText, this.yText);
        }
    }

    private void highlightRow(Page page, Font font, int color) {
        float[] original = page.getBrushColor();
        page.setBrushColor(color);
        page.moveTo(vertLines[0], this.yText - font.ascent);
        page.lineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
        page.lineTo(vertLines[this.numberOfColumns], this.yText + font.descent);
        page.lineTo(vertLines[0], this.yText + font.descent);
        page.fillPath();
        page.setBrushColor(original);
    }

    private void drawTheVerticalLines() {
        float[] original = page.getPenColor();
        page.setPenColor(penColor);
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
    }

    // A number is right-aligned, as Table.rightAlignNumbers aligns it.
    private Alignment getAlignment(String str) {
        return Table.isNumber(str) ? Alignment.RIGHT : Alignment.LEFT;
    }

    /**
     * Sets the column widths, the column alignment and header fields from a
     * delimited text file, read as UTF-8. Its first line with at least as
     * many fields as the table has columns is the header, and the lines after
     * it are the rows. A quoted field is read as RFC 4180 reads it, so a
     * delimiter inside one is text.
     *
     * @param fileName the file name.
     * @param delimiter the delimiter.
     * @throws IOException if there is an issue.
     * @return this BigTable object.
     */
    public BigTable setTableData(String fileName, String delimiter) throws IOException {
        int columns = this.numberOfColumns;
        try {
            String[] header = new String[0];
            try (DataFileRows lines = DataFileRows.open(fileName, delimiter, columns, false)) {
                while (lines.hasNext()) {
                    String[] fields = lines.next();
                    if (fields.length >= columns) {
                        header = fields;
                        break;
                    }
                }
            }
            return setTableData(header, () -> {
                try {
                    return DataFileRows.open(fileName, delimiter, columns, true);
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
     * is closed when the table is done with it. A row with fewer fields than
     * the table has columns is skipped, as a short line of a file is.
     *
     * @param header the header fields, at least as many as the columns.
     * @param rows the fields of each row, in the order they are drawn.
     * @return this BigTable object.
     */
    public BigTable setTableData(String[] header, Iterable<String[]> rows) {
        if (header.length < this.numberOfColumns) {
            pdf.fail(new IllegalArgumentException(
                    "The header has fewer fields than the table has columns."));
        }
        this.rows = rows;
        this.vertLines = new float[this.numberOfColumns + 1];
        this.headerFields = new String[this.numberOfColumns];
        this.widths = new float[this.numberOfColumns];
        this.alignment = new Alignment[this.numberOfColumns];

        for (int i = 0; i < this.numberOfColumns; i++) {
            headerFields[i] = header[i];
        }
        measure(header);
        int rowNumber = 0;
        Iterator<String[]> iterator = rows.iterator();
        try {
            while (iterator.hasNext()) {
                String[] fields = iterator.next();
                if (fields.length < this.numberOfColumns) {
                    continue;
                }
                if (rowNumber == 0) {    // Determine alignment from first data row
                    for (int i = 0; i < this.numberOfColumns; i++) {
                        alignment[i] = getAlignment(fields[i]);
                    }
                }
                measure(fields);
                rowNumber++;
            }
        } finally {
            close(iterator);
        }
        this.dataRows = rowNumber;

        setVertLines();
        return this;
    }

    // Widens the columns to fit the fields of a row.
    private void measure(String[] fields) {
        for (int i = 0; i < this.numberOfColumns; i++) {
            float width = f1.stringWidth(fields[i]) + 2*this.padding;
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
    // the other ports do. An iterator cannot throw an IOException, so it
    // throws an UncheckedIOException, and setTableData and complete() throw
    // its cause.
    private static final class DataFileRows implements Iterator<String[]>, Closeable {
        private final BufferedReader reader;
        private final String delimiter;
        private String[] next;

        private DataFileRows(BufferedReader reader, String delimiter) {
            this.reader = reader;
            this.delimiter = delimiter;
        }

        // With skipHeader, the rows start after the header: the first line
        // with at least as many fields as the table has columns.
        static DataFileRows open(String fileName, String delimiter, int columns, boolean skipHeader)
                throws IOException {
            BufferedReader reader = new BufferedReader(
                    new InputStreamReader(new FileInputStream(fileName), StandardCharsets.UTF_8));
            DataFileRows rows = new DataFileRows(reader, delimiter);
            try {
                reader.mark(1);
                if (reader.read() != '\uFEFF') {
                    reader.reset();
                }
                rows.advance();
                if (skipHeader) {
                    while (rows.next != null && rows.next.length < columns) {
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
                next = (line == null) ? null : Util.split(line, delimiter);
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

    // Sets the x coordinates of the vertical lines from the location and the column widths.
    private void setVertLines() {
        float vertLineX = this.x;
        this.vertLines[0] = vertLineX;
        for (int i = 0; i < widths.length; i++) {
            vertLineX += this.widths[i];
            this.vertLines[i + 1] = vertLineX;
        }
    }

    /**
     * Draws the rows, then the vertical lines, with a "Page i of N" footer on
     * every page. The pages are added to the PDF as they are drawn, so the
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
                if (fields.length < this.numberOfColumns) {
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
