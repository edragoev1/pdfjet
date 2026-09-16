package com.pdfjet;

import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Use this class if you have a lot of data.
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
    private String fileName;
    private String delimiter;
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
        if (page == null) {     // The first page
            newPage();
            return;
        }
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

    // Splits the line at the delimiter, which is not a regular expression,
    // keeping the empty fields at the end of the line and reading the quoted
    // fields as RFC 4180 does, as the other ports do.
    private String[] split(String line) {
        return Util.split(line, delimiter);
    }

    /**
     * Sets the column widths, the column alignment and header fields.
     *
     * @param fileName the file name.
     * @param delimiter the delimiter.
     * @throws IOException if there is an issue.
     * @return this BigTable object.
     */
    public BigTable setTableData(String fileName, String delimiter) throws IOException {
        this.fileName = fileName;
        this.delimiter = delimiter;
        this.vertLines = new float[this.numberOfColumns + 1];
        this.headerFields = new String[this.numberOfColumns];
        this.widths = new float[this.numberOfColumns];
        this.alignment = new Alignment[this.numberOfColumns];

        int rowNumber = 0;
        try (BufferedReader reader = openDataFile()) {
            String line;
            while ((line = reader.readLine()) != null) {
                String[] fields = split(line);
                if (fields.length < this.numberOfColumns) {
                    continue;
                }

                if (rowNumber == 0) {
                    for (int i = 0; i < this.numberOfColumns; i++) {
                        headerFields[i] = fields[i];
                    }
                }
                if (rowNumber == 1) {    // Determine alignment from first data row
                    for (int i = 0; i < this.numberOfColumns; i++) {
                        alignment[i] = getAlignment(fields[i]);
                    }
                }
                for (int i = 0; i < this.numberOfColumns; i++) {
                    String field = fields[i];
                    float width = f1.stringWidth(field) + 2*this.padding;
                    if (width > widths[i]) {
                        this.widths[i] = width;
                    }
                }
                rowNumber++;
            }
        }
        this.dataRows = (rowNumber > 0) ? rowNumber - 1 : 0;     // Without the header

        setVertLines();
        return this;
    }

    // Opens the data file, which is read as UTF-8, after the byte order mark
    // at its start, if there is one.
    private BufferedReader openDataFile() throws IOException {
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(this.fileName), StandardCharsets.UTF_8));
        try {
            reader.mark(1);
            if (reader.read() != '\uFEFF') {
                reader.reset();
            }
        } catch (IOException e) {
            reader.close();
            throw e;
        }
        return reader;
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
     * Draws the rows read from the data file, then the vertical lines, with a
     * "Page i of N" footer on every page. The pages are added to the PDF as
     * they are drawn, so the document does not hold them all. Call it after
     * the location, the bottom margin and the data file have been set.
     *
     * @throws Exception if the data file cannot be read or drawing fails.
     */
    public void complete() throws Exception {
        this.pageCount = countPages();
        try (BufferedReader reader = openDataFile()) {
            String line;
            while ((line = reader.readLine()) != null) {
                String[] fields = split(line);
                if (fields.length < this.numberOfColumns) {
                    continue;
                }
                this.drawTextAndLine(fields);
            }
        }
        drawTheVerticalLines();
        drawFooter();
    }
}
