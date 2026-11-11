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
 * Use this class for a table of plain rows of any length, a statement, a list
 * or an export: every row has one style, and a field too wide for its column
 * is cut short, or, with setWrapText, wraps onto more lines, the row as tall
 * as its tallest field. For drawables in cells, spanned cells or page totals,
 * use Table. The rows are read from a
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
    private float top;          // The top of the table on the page being drawn
    private Page firstPage;     // The page the table starts on, or null for a new one
    private float firstPageY;   // The top of the table on firstPage
    private List<Page> pages;
    private Page page;
    private float[] widths;
    private String[] headerFields;
    private Alignment[] alignment;
    private float[] vertLines;
    // The width the text of each column has when the table is too wide for
    // the page and the last of its columns are cut back to fit it, or null
    // when the table fits: a column that is as wide as its widest field
    // holds every field of it whole and needs no measuring as it is drawn.
    private float[] clipped;
    private float bottomMargin = 20.0f;
    private float padding = 2.0f;
    private float verticalPadding = 0f;     // Above and below the text of a row
    private boolean highlightRow = true;
    // In a PDF/UA document the table is a Table element that the rows of
    // every page go on adding to; see drawFieldsAndLine.
    private StructElement structElement = null;
    private float[] shadingColor = rgb(0xF0F0F0);   // null for no shading
    private float[] headerColor = null;             // null for the shading of every other row
    private float[] borderColor = rgb(0xB0B0B0);    // null for no lines
    private boolean verticalLines = true;           // false for the rows' lines alone
    private String footerText = "Page {page} of {pages}";
    private Font footerFont;                        // null for the header font
    private float footerFontSize = 0f;              // 0 for the size of the footer's font
    private float footerOffset = -1f;               // Below 0 for the ascent of the footer's font
    private Iterable<String[]> rows;
    private boolean checkLineBreaks;        // The rows are not read from a file, which has no line breaks left
    private int[] columns = new int[0];     // The fields drawn, in the order they are drawn
    private int numberOfColumns;            // The length of columns
    private float[] percents;               // The width of each column, as a percent of the table's, or null
    private int fieldsNeeded;               // The fields a row needs: the largest index plus 1
    private boolean startNewPage = true;
    private int dataRows;           // The rows under the header, counted by setTableData
    private int pageCount;          // The pages they take, counted by complete()
    private int pageNumber;         // The page being drawn
    private boolean footerDrawn;
    // With setWrapText: the fields wrap within their columns, textWidths the
    // width of the text of each column, headerLines the header's lines, and
    // firstRowOnPage whether no row is drawn yet on the page.
    private boolean wrap;
    private float[] textWidths;
    private String[][] headerLines;
    private int headerCount;        // The most lines of a header field
    private boolean firstRowOnPage;
    // With setFallbackFonts: the fonts of the characters the header's font
    // and the rows' font do not have, or null
    private Font fallback1;
    private Font fallback2;

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
     * Gives the columns, in the order setColumns draws them, their percentages
     * of the width of the table, the width of the page less the margin of
     * setLocation on each side, as Table's setColumnWidthsInPercent does;
     * percentages that do not add up to 100 are shares of it. A field wider
     * than its column is cut to fit it, its text ending in " ...". Without
     * them each column is as wide as its widest field. Call it after
     * setColumns or setNumberOfColumns: percentages that are not one for each
     * column, or one that is negative or not a number, are refused.
     *
     * @param percents the percentage of each column.
     * @return this BigTable object.
     */
    public BigTable setColumnWidthsInPercent(float... percents) {
        if (percents.length != numberOfColumns) {
            pdf.fail(new IllegalArgumentException("The table has " + numberOfColumns + " columns, and "
                    + percents.length + " percentages: give one for each column."));
        }
        for (float percent : percents) {
            if (!(percent >= 0f) || percent == Float.POSITIVE_INFINITY) {
                pdf.fail(new IllegalArgumentException(
                        "The width of a column cannot be a negative percentage, or not a number."));
            }
        }
        this.percents = percents.clone();
        if (vertLines != null) {
            setVertLines();
        }
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
     * Sets whether a field too wide for its column wraps onto more lines, at
     * its spaces, a word longer than its column broken between its
     * characters, rather than being cut short; a row is then as tall as its
     * tallest field. The columns keep the widths of setColumnWidthsInPercent,
     * or, without them, their widths, as wide as their widest fields, when the
     * table fits its page; when it does not, the narrow columns keep theirs
     * and the others share what is left equally. A row that does not fit what
     * is left of a page starts the next; one taller than a whole page is cut
     * at the lines that fit, ending in " ...", its cell keeping the whole of
     * its text. The rows are read once more to count the pages, three times
     * in all, and the memory stays flat. It is off unless set, and the table
     * is drawn as before.
     *
     * @param wrap true for the fields to wrap.
     * @return this BigTable object.
     */
    public BigTable setWrapText(boolean wrap) {
        this.wrap = wrap;
        if (this.vertLines != null) {
            setVertLines();
        }
        return this;
    }

    /**
     * Sets the fonts that draw the characters the header's font and the rows'
     * font do not have, such as Korean or Chinese in a table of a Latin font:
     * each character in the font of its row if it has it, else in the fallback
     * font, and measured so. A null font is none, as unless set. Call it
     * before setTableData or setTableRows, which measure the columns.
     *
     * @param header the fallback font of the header's font, or null.
     * @param rows the fallback font of the rows' font, or null.
     * @return this BigTable object.
     */
    public BigTable setFallbackFonts(Font header, Font rows) {
        this.fallback1 = header;
        this.fallback2 = rows;
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
     * Sets whether the vertical lines between the columns and at the sides of
     * the table are drawn. They are unless set; without them the table has
     * only the lines between its rows, a lighter look for a long table.
     *
     * @param verticalLines false to leave the vertical lines out.
     * @return this BigTable object.
     */
    public BigTable setVerticalLines(boolean verticalLines) {
        this.verticalLines = verticalLines;
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
     * Sets the space between the text of a row and the lines above and below
     * it, the header row's too. It is 0 by default, and a row is as tall as
     * its font; a table with it takes more pages.
     *
     * @param verticalPadding the padding, 0 or more.
     * @return this BigTable object.
     */
    public BigTable setVerticalPadding(float verticalPadding) {
        if (!(verticalPadding >= 0f) || Float.isInfinite(verticalPadding)) {
            pdf.fail(new IllegalArgumentException("The vertical padding must be a number, 0 or more."));
            return this;
        }
        this.verticalPadding = verticalPadding;
        return this;
    }

    /**
     * Sets the background color of the header row, on every page. Unless it
     * is set, the header is shaded as the first of every other row, in the
     * shading color. Color.transparent goes back to that.
     *
     * @param color the color as a 0xRRGGBB value, for example 0xE6E6E6.
     * @return this BigTable object.
     */
    public BigTable setHeaderColor(int color) {
        this.headerColor = (color == Color.transparent) ? null : rgb(color);
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
     * Sets the size of the footer's text, so that it can be smaller than the
     * table's with the same font. It is the size of the footer's font unless
     * it is set.
     *
     * @param size the size in points, more than 0.
     * @return this BigTable object.
     */
    public BigTable setFooterFontSize(float size) {
        if (!(size > 0f) || Float.isInfinite(size)) {
            pdf.fail(new IllegalArgumentException("The size of the footer must be a number more than 0."));
            return this;
        }
        this.footerFontSize = size;
        return this;
    }

    /**
     * Sets the distance from the bottom of the page to the baseline of the
     * footer, as Page.addFooter takes it, so that the footer can sit at the
     * bottom margin. It is the ascent of the footer's font unless it is set,
     * the footer just above the edge of the page.
     *
     * @param offset the distance in points, 0 or more.
     * @return this BigTable object.
     */
    public BigTable setFooterOffset(float offset) {
        if (!(offset >= 0f) || Float.isInfinite(offset)) {
            pdf.fail(new IllegalArgumentException("The offset of the footer must be a number, 0 or more."));
            return this;
        }
        this.footerOffset = offset;
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
     * Starts the table on a page of the PDF that has other content above it,
     * such as a heading, at y on that page. The next pages are new, and the
     * table starts on them at the y of its location. The page is one that
     * new Page(pdf, pageSize) has added to the PDF, and nothing is drawn on it
     * after the table. When the header and the first row do not fit on the
     * page above its bottom margin, the table starts on a new page instead,
     * and draws nothing on it.
     *
     * @param page the page the table starts on.
     * @param y the top of the table on that page.
     * @return this BigTable object.
     */
    public BigTable setFirstPage(Page page, float y) {
        // A page of another PDF is refused: the table was drawn on it, and
        // its mistakes recorded on the other PDF (review of 17 October 2026)
        if (page != null && page.pdf != pdf) {
            pdf.fail(new IllegalArgumentException("The page belongs to another PDF."));
            return this;
        }
        this.firstPage = page;
        this.firstPageY = y;
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
        if (pageNumber == 0 && startsOnFirstPage()) {
            page = firstPage;
            this.top = firstPageY;
        } else {
            page = new Page(pdf, pageSize);
            this.top = this.y;
        }
        pages.add(page);
        pageNumber++;
        footerDrawn = false;
        page.setPenWidth(0f);
        this.yText = this.top + verticalPadding + f1.ascent;
        this.highlightRow = true;
        // The header fields are the TH cells of the table the first time they
        // are drawn, and an artifact where they repeat on the next pages.
        StructElem header = null;
        if (structElement == null) {
            structElement = page.addStructElement(
                    page.structParent, StructElem.TABLE, null, true);
            header = StructElem.TH;
        }
        if (wrap) {
            drawWrappedFields(headerLines, headerCount, null, f1, header, true);
        } else {
            drawFieldsAndLine(headerFields, f1, header, true);
        }
        float extra = headerExtra();
        if (extra > 0f) {
            this.yText += extra;
        }
        this.yText += f1.descent + 2*verticalPadding + f2.ascent;
        startNewPage = false;
        firstRowOnPage = true;
    }

    // Returns how much taller than one line the header is: 0 unless its
    // fields wrap onto more lines.
    private float headerExtra() {
        if (!wrap || headerCount <= 1) {
            return 0f;
        }
        return (float) (headerCount - 1) * (f1.ascent + f1.descent);
    }

    // True when the table starts on the page setFirstPage set: when the header
    // and the first row fit on it above its bottom margin.
    private boolean startsOnFirstPage() {
        if (firstPage == null) {
            return false;
        }
        float need = firstPageY + f1.ascent + f1.descent + f2.ascent + f2.descent
                + 4*verticalPadding;
        float extra = headerExtra();
        if (extra > 0f) {
            need += extra;
        }
        return need <= firstPage.height - bottomMargin;
    }

    // Draws the footer of the page that was just finished, once. The page is
    // finished before the next one is created, so it is written complete.
    private void drawFooter() throws Exception {
        if (!footerDrawn && footerText != null && !footerText.isEmpty()) {
            String text = footerText
                    .replace("{page}", String.valueOf(pageNumber))
                    .replace("{pages}", String.valueOf(pageCount));
            // The page number repeats on every page, which makes it an artifact:
            // addFooter marks it as a pagination artifact, a footer.
            TextLine footer = new TextLine((footerFont != null) ? footerFont : f1, text);
            if (footerFontSize > 0f) {
                footer.setFontSize(footerFontSize);
            }
            if (footerOffset >= 0f) {
                page.addFooter(footer, footerOffset);
            } else {
                page.addFooter(footer);
            }
        }
        footerDrawn = true;
    }

    private void drawTextAndLine(String[] fields) throws Exception {
        if (wrap) {
            drawWrappedRow(fields);
            return;
        }
        if (startNewPage) {     // Create new page
            newPage();
        }

        drawFieldsAndLine(fields, f2, StructElem.TD, false);
        this.yText += f2.descent + 2*verticalPadding + f2.ascent;
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
        if (wrap) {
            return countWrappedPages();
        }
        float pageHeight = pageSize.getHeight();
        float yTop = firstRowY(this.y);
        float yPos = yTop;
        if (startsOnFirstPage()) {
            // The first page can be of another size than the next ones.
            pageHeight = firstPage.height;
            yPos = firstRowY(firstPageY);
        }
        int count = 1;
        boolean newPage = false;
        for (int i = 0; i < this.dataRows; i++) {
            if (newPage) {
                count++;
                pageHeight = pageSize.getHeight();
                yPos = yTop;
                newPage = false;
            }
            yPos += f2.descent + 2*verticalPadding + f2.ascent;
            if (yPos > (pageHeight - this.bottomMargin)) {
                newPage = true;
            }
        }
        return count;
    }

    // Returns the baseline of the first row of a page, of a table whose top is
    // there, in the order newPage adds it up: added up otherwise, it can round
    // to another float, and the count was a page off the drawing.
    private float firstRowY(float top) {
        float y = top + verticalPadding + f1.ascent;
        float extra = headerExtra();
        if (extra > 0f) {
            y += extra;
        }
        y += f1.descent + 2*verticalPadding + f2.ascent;
        return y;
    }

    // Draws a row of the table. In a PDF/UA document the row is a TR element
    // and each field a TH or TD element that holds the text, and the shading
    // and the lines are artifacts. A row with no cell structure is drawn as an
    // artifact, which is what the header rows that repeat on the next pages
    // are.
    private void drawFieldsAndLine(String[] fields, Font font, StructElem cellStructure, boolean header) {
        // The shading and the line above the text carry no meaning of their own.
        page.addArtifactBMC();
        if (header && headerColor != null) {
            highlightRow(page, font, headerColor);
            this.highlightRow = false;
        } else if (this.highlightRow) {
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
            page.moveTo(vertLines[0], this.yText - font.ascent - verticalPadding);
            page.lineTo(vertLines[this.numberOfColumns], this.yText - font.ascent - verticalPadding);
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
            // A column the page was too narrow for holds as much of the text
            // as fits it, ending in " ..."; the cell of the structure tree
            // keeps the whole of the text, which is what a reader reads. A
            // column that was not cut is as wide as its widest field, so
            // every field of it is drawn whole and none is measured here.
            String drawn = (clipped != null && clipped[i] < widths[i])
                    ? fit(text, measureOf(font), clipped[i]) : text;
            float xText = vertLines[i] + this.padding;
            if (alignment[i] == Alignment.RIGHT) {
                xText = (vertLines[i + 1] - this.padding) - measureOf(font).width(drawn);
            }
            if (tagged) {
                // A cell holds its text and nothing else, so the cell element
                // holds the marked content of the text: it needs no paragraph
                // of its own, which would be another object for every cell.
                page.structParent = rowElement;
                // A text cut short to fit its column is described by the
                // whole of it; a text drawn whole needs no description, which
                // PAC warns of on text
                String description = drawn.equals(text) ? null : text;
                page.addBDC(cellStructure, null, null, description, cellAttributes(cellStructure));
            } else {
                page.addArtifactBMC();
            }
            drawText(font, drawn, xText, this.yText);
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
        page.fillRectBetween(vertLines[0], this.yText - font.ascent - verticalPadding,
                vertLines[this.numberOfColumns], this.yText + font.descent + verticalPadding);
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
        for (int i = 0; verticalLines && i <= this.numberOfColumns; i++) {
            page.drawLine(
                    vertLines[i],
                    this.top,
                    vertLines[i],
                    this.yText - f2.ascent - verticalPadding);
        }
        // Draw the last horizontal line
        page.moveTo(vertLines[0], this.yText - f2.ascent - verticalPadding);
        page.lineTo(vertLines[this.numberOfColumns], this.yText - f2.ascent - verticalPadding);
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
            float width = measureOf(font).width(text);
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
    // Puts each vertical line where the columns before it end, and cuts the
    // last of the columns back when they run past the right edge of the page:
    // a column as wide as its widest field is what the table asks for, and a
    // table wider than its page draws the last of its columns off the edge,
    // where they are lost. The columns are cut from the last one back, so the
    // table keeps the widths it asked for as far as the page allows, and the
    // fields of a column that was cut are drawn with the end of their text
    // replaced by " ...".
    private void setVertLines() {
        if (percents != null && percents.length != this.widths.length) {
            // The columns were set again after the percentages: fewer threw, and
        // more were drawn past the page (review of 12 October 2026, C4)
            int count = percents.length;
            percents = null;
            pdf.fail(new IllegalArgumentException("The table has " + this.widths.length + " columns, and "
                    + count + " percentages: give one for each column."));
        }
        if (wrap) {
            setWrapWidths();
            return;
        }
        if (percents != null) {
            setVertLinesInPercent();
            return;
        }
        float[] widths = this.widths.clone();
        // The table is drawn between the margin it starts at and the same
        // margin on the right of the page.
        float room = pageSize.getWidth() - 2*this.x - widths.length*2*this.padding;
        float least = Math.max(f1.stringWidth(ELLIPSIS), f2.stringWidth(ELLIPSIS));
        float total = 0f;
        for (float width : widths) {
            total += width;
        }
        this.clipped = null;
        for (int i = widths.length - 1; i >= 0 && total > room; i--) {
            float cut = Math.min(total - room, Math.max(widths[i] - least, 0f));
            if (cut > 0f) {
                widths[i] -= cut;
                total -= cut;
                if (this.clipped == null) {
                    this.clipped = this.widths.clone();
                }
                this.clipped[i] = widths[i];
            }
        }
        float vertLineX = this.x;
        this.vertLines[0] = vertLineX;
        for (int i = 0; i < widths.length; i++) {
            vertLineX += widths[i] + 2*this.padding;
            this.vertLines[i + 1] = vertLineX;
        }
    }

    // Puts each vertical line where the columns before it end, each column the
    // percentage of the table's width setColumnWidthsInPercent gives it, its
    // padding in it, and cuts the fields of a column narrower than its widest
    // field.
    private void setVertLinesInPercent() {
        float tableWidth = pageSize.getWidth() - 2*this.x;
        if (!(tableWidth > 0f)) {
            // The columns were as wide as less than nothing (review of 12 October
        // 2026, T5)
            pdf.fail(new IllegalArgumentException("The margin of the table, its x, is half the width of the page or more: the columns have no room."));
        }
        // The shares are added up and taken in double, the largest float a
        // share as any other: tableWidth*percent was an infinity, and the
        // document failed (review of 12 October 2026, T5)
        double total = 0.0;
        for (float percent : percents) {
            total += percent;
        }
        this.clipped = null;
        float vertLineX = this.x;
        this.vertLines[0] = vertLineX;
        for (int i = 0; i < percents.length; i++) {
            // Percentages that are all 0 share the width equally.
            float width = (total > 0.0) ? (float) ((double) tableWidth * percents[i] / total) : tableWidth / percents.length;
            float text = Math.max(0f, width - 2*this.padding);
            if (this.widths[i] > text) {
                if (this.clipped == null) {
                    this.clipped = this.widths.clone();
                }
                this.clipped[i] = text;
            }
            vertLineX += width;
            this.vertLines[i + 1] = vertLineX;
        }
    }

    // The mark that ends a field whose text was cut to fit its column, which
    // takes the place of the last four characters of what fits.
    private static final String ELLIPSIS = " ...";

    // The text as much of it as fits the width, ending in the mark that says
    // it was cut. The mark takes the place of the last four characters of what
    // fits, rather than being added to them, so the text stays inside the
    // column. The text is cut between code points, never inside one, and
    // counted in them, as every port counts it.
    private static String fit(String text, Measure m, float width) {
        if (m.width(text) <= width) {
            return text;
        }
        return cut(text, m, width);
    }

    // As much of the text as fits the width with the mark that says it was
    // cut, the mark in place of the last four characters of what fits; or ""
    // when the width is too narrow for the mark.
    private static String cut(String text, Measure m, float width) {
        // The index of each code point, and of the end of the text.
        int count = text.codePointCount(0, text.length());
        int[] offsets = new int[count + 1];
        for (int i = 0, offset = 0; i < count; i++) {
            offsets[i] = offset;
            offset += Character.charCount(text.codePointAt(offset));
        }
        offsets[count] = text.length();
        int end = 0;                    // The most code points that fit.
        int high = count;
        while (end < high) {
            int middle = end + (high - end + 1)/2;
            if (m.width(text.substring(0, offsets[middle])) <= width) {
                end = middle;
            } else {
                high = middle - 1;
            }
        }
        end = Math.max(0, end - ELLIPSIS.length());
        while (end > 0 && m.width(text.substring(0, offsets[end]) + ELLIPSIS) > width) {
            end--;
        }
        if (m.font.stringWidth(ELLIPSIS) > width) {
            // A column too narrow for the mark draws nothing: the mark was drawn
        // over the next column (review of 12 October 2026, T5)
            return "";
        }
        return text.substring(0, offsets[end]) + ELLIPSIS;
    }

    /**
     * Draws the rows, then the vertical lines, with the footer on every page. The pages are added to the PDF as they are drawn, so the
     * document does not hold them all. Call it after the location, the bottom
     * margin and the table data have been set. A table without data draws
     * nothing.
     *
     * @throws Exception if the data file cannot be read or drawing fails.
     */
    public void complete() throws Exception {
        if (rows == null) {
            return;     // A table without data draws nothing.
        }
        try {
            this.pageCount = countPages();      // Reads the rows with setWrapText
        } catch (UncheckedIOException e) {
            throw e.getCause();
        }
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

    // The wrapping of the fields, with setWrapText

    // Gives each column the width of its text, and puts each vertical line
    // where the columns before it end: the widths of setColumnWidthsInPercent,
    // or each column as wide as its widest field when they fit the page; when
    // they do not, the columns narrower than an equal share of what is left
    // keep their widths, from the narrowest, and the others share the rest
    // equally. No field is cut: each wraps within its column.
    private void setWrapWidths() {
        int n = this.widths.length;
        this.clipped = null;
        this.textWidths = new float[n];
        float vertLineX = this.x;
        this.vertLines[0] = vertLineX;
        if (percents != null) {
            float tableWidth = pageSize.getWidth() - 2*this.x;
            if (!(tableWidth > 0f)) {
                pdf.fail(new IllegalArgumentException("The margin of the table, its x, is half the width of the page or more: the columns have no room."));
            }
            double total = 0.0;
            for (float percent : percents) {
                total += percent;
            }
            for (int i = 0; i < percents.length; i++) {
                float width = (total > 0.0) ? (float) ((double) tableWidth * percents[i] / total) : tableWidth / percents.length;
                this.textWidths[i] = Math.max(0f, width - 2*this.padding);
                vertLineX += width;
                this.vertLines[i + 1] = vertLineX;
            }
        } else {
            System.arraycopy(this.widths, 0, this.textWidths, 0, n);
            float room = pageSize.getWidth() - 2*this.x - n*2*this.padding;
            float total = 0f;
            for (float width : this.widths) {
                total += width;
            }
            if (total > room) {
                // From the narrowest column, in the order of the columns among
                // those as wide: a stable sort, an insertion sort of a few.
                int[] order = new int[n];
                for (int i = 0; i < n; i++) {
                    int j = i;
                    while (j > 0 && this.widths[i] < this.widths[order[j - 1]]) {
                        order[j] = order[j - 1];
                        j--;
                    }
                    order[j] = i;
                }
                float remaining = room;
                int left = n;
                float share = -1f;
                for (int i : order) {
                    if (share < 0f) {
                        float fair = Math.max(0f, remaining / left);
                        if (this.widths[i] <= fair) {
                            remaining -= this.widths[i];
                            left--;
                            continue;
                        }
                        share = fair;
                    }
                    this.textWidths[i] = share;
                }
            }
            for (int i = 0; i < n; i++) {
                vertLineX += this.textWidths[i] + 2*this.padding;
                this.vertLines[i + 1] = vertLineX;
            }
        }
        this.headerLines = wrapFields(headerFields, f1);
        this.headerCount = mostLines(headerLines);
    }

    // The lines of each field of the row drawn, wrapped to the width of its
    // column in the font.
    private String[][] wrapFields(String[] fields, Font font) {
        String[][] lines = new String[this.numberOfColumns][];
        for (int i = 0; i < this.numberOfColumns; i++) {
            String text = checkLineBreaks ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]];
            lines[i] = wrap(text, measureOf(font), textWidths[i]);
        }
        return lines;
    }

    // The most lines of any field of the row: at least one.
    private static int mostLines(String[][] lines) {
        int count = 1;
        for (String[] field : lines) {
            count = Math.max(count, field.length);
        }
        return count;
    }

    // The lines of the text within the width: its words, the parts between
    // its spaces, as many on a line as fit, a word wider than the width broken
    // between its characters, code points, at least one on a line. An empty
    // text, or one of spaces, is one empty line.
    static String[] wrap(String text, Measure m, float width) {
        List<String> lines = new ArrayList<String>(1);
        String line = "";
        int start = 0;
        while (start <= text.length()) {
            int space = text.indexOf(' ', start);
            if (space == -1) {
                space = text.length();
            }
            String word = text.substring(start, space);
            start = space + 1;
            if (word.isEmpty()) {
                continue;
            }
            if (!line.isEmpty()) {
                String candidate = line + " " + word;
                if (m.width(candidate) <= width) {
                    line = candidate;
                    continue;
                }
                lines.add(line);
                line = "";
            }
            if (m.width(word) <= width) {
                line = word;
                continue;
            }
            int[] runes = word.codePoints().toArray();
            int from = 0;
            while (from < runes.length) {
                int end = runesThatFit(runes, from, m, width);
                if (from + end == runes.length) {
                    line = new String(runes, from, end);
                    break;
                }
                lines.add(new String(runes, from, end));
                from += end;
            }
        }
        lines.add(line);
        return lines.toArray(new String[0]);
    }

    // How many of the code points, from the one at from, fit the width: at
    // least one, so that a column too narrow for a character still draws the
    // text, a character on each line.
    private static int runesThatFit(int[] runes, int from, Measure m, float width) {
        int end = 1;
        int high = runes.length - from;
        while (end < high) {
            int middle = end + (high - end + 1)/2;
            if (m.width(new String(runes, from, middle)) <= width) {
                end = middle;
            } else {
                high = middle - 1;
            }
        }
        return end;
    }

    // The most lines of a row whose first line is at y that the page holds
    // above the limit: at least one.
    private static int mostLines(float y, float limit, float lineHeight) {
        int most = 1;
        while (y + (float) most * lineHeight <= limit) {
            most++;
        }
        return most;
    }

    // Draws a row of fields that wrap: on the next page when it does not fit
    // what is left of this one, cut at the lines a whole page holds when it
    // does not fit that either.
    private void drawWrappedRow(String[] fields) throws Exception {
        if (startNewPage) {
            newPage();
        }
        Font font = f2;
        float lineHeight = font.ascent + font.descent;
        String[][] lines = wrapFields(fields, font);
        int count = mostLines(lines);
        float limit = page.height - bottomMargin;
        if (count > 1 && this.yText + (float) (count - 1) * lineHeight > limit && !firstRowOnPage) {
            drawTheVerticalLines();
            drawFooter();
            newPage();
            limit = page.height - bottomMargin;
        }
        String[] whole = null;
        if (count > 1 && this.yText + (float) (count - 1) * lineHeight > limit) {
            int most = mostLines(this.yText, limit, lineHeight);
            if (most < count) {
                whole = new String[this.numberOfColumns];
                for (int i = 0; i < lines.length; i++) {
                    if (lines[i].length > most) {
                        whole[i] = checkLineBreaks
                                ? Util.lineBreaksToSpaces(fields[columns[i]]) : fields[columns[i]];
                        lines[i] = Arrays.copyOf(lines[i], most);
                        lines[i][most - 1] = mark(lines[i][most - 1], measureOf(font), textWidths[i]);
                    }
                }
                count = most;
            }
        }
        drawWrappedFields(lines, count, whole, font, StructElem.TD, false);
        firstRowOnPage = false;
        if (count > 1) {
            this.yText += (float) (count - 1) * lineHeight;
        }
        this.yText += font.descent + 2*verticalPadding + font.ascent;
        if (this.yText > limit) {
            drawTheVerticalLines();
            drawFooter();
            startNewPage = true;
        }
    }

    // The line with the mark that says the text goes on, cut to make room for
    // it within the width.
    private static String mark(String line, Measure m, float width) {
        String marked = line + ELLIPSIS;
        if (m.width(marked) <= width) {
            return marked;
        }
        return cut(line, m, width);
    }

    // Draws a row of the lines of its fields, count lines tall, as
    // drawFieldsAndLine draws a row of one line: the cell of a field holds all
    // of its lines, and the whole of the text of a field that was cut,
    // whole[i] when there is one.
    private void drawWrappedFields(
            String[][] lines, int count, String[] whole, Font font, StructElem cellStructure, boolean header) {
        float lineHeight = font.ascent + font.descent;
        float extra = 0f;
        if (count > 1) {
            extra = (float) (count - 1) * lineHeight;
        }
        page.addArtifactBMC();
        if (header && headerColor != null) {
            highlightRows(page, font, headerColor, extra);
            this.highlightRow = false;
        } else if (this.highlightRow) {
            if (shadingColor != null) {
                highlightRows(page, font, shadingColor, extra);
            }
            this.highlightRow = false;
        } else {
            this.highlightRow = true;
        }
        if (borderColor != null) {
            float[] original = page.getPenColor();
            page.setPenColor(borderColor);
            page.moveTo(vertLines[0], this.yText - font.ascent - verticalPadding);
            page.lineTo(vertLines[this.numberOfColumns], this.yText - font.ascent - verticalPadding);
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
            if (tagged) {
                page.structParent = rowElement;
                String description = (whole != null) ? whole[i] : null;
                page.addBDC(cellStructure, null, null, description, cellAttributes(cellStructure));
            } else {
                page.addArtifactBMC();
            }
            for (int k = 0; k < lines[i].length; k++) {
                String line = lines[i][k];
                float xText = vertLines[i] + this.padding;
                if (alignment[i] == Alignment.RIGHT) {
                    xText = (vertLines[i + 1] - this.padding) - measureOf(font).width(line);
                }
                float yText = this.yText;
                if (k > 0) {
                    yText += (float) k * lineHeight;
                }
                drawText(font, line, xText, yText);
            }
            page.addEMC();
        }
        page.structParent = parent;
    }

    // Fills the background of a row, extra taller than one line.
    private void highlightRows(Page page, Font font, float[] color, float extra) {
        float[] original = page.getBrushColor();
        page.setBrushColor(color);
        float bottom = this.yText + font.descent + verticalPadding;
        if (extra > 0f) {
            bottom = this.yText + extra + font.descent + verticalPadding;
        }
        page.fillRectBetween(vertLines[0], this.yText - font.ascent - verticalPadding,
                vertLines[this.numberOfColumns], bottom);
        page.setBrushColor(original);
    }

    // Counts the pages the rows take as drawWrappedRow breaks them, reading
    // the rows once more, so that the footer of each page says how many there
    // are. The arithmetic is that of the drawing, in its order.
    private int countWrappedPages() {
        Font font = f2;
        float lineHeight = font.ascent + font.descent;
        float pageHeight = pageSize.getHeight();
        float yTop = firstRowY(this.y);
        float yPos = yTop;
        if (startsOnFirstPage()) {
            pageHeight = firstPage.height;
            yPos = firstRowY(firstPageY);
        }
        int count = 1;
        boolean newPage = false;
        boolean first = true;
        Iterator<String[]> iterator = rows.iterator();
        try {
            while (iterator.hasNext()) {
                String[] fields = iterator.next();
                if (fields.length < this.fieldsNeeded) {
                    continue;
                }
                if (newPage) {
                    count++;
                    pageHeight = pageSize.getHeight();
                    yPos = yTop;
                    newPage = false;
                    first = true;
                }
                int lines = mostLines(wrapFields(fields, font));
                float limit = pageHeight - this.bottomMargin;
                if (lines > 1 && yPos + (float) (lines - 1) * lineHeight > limit && !first) {
                    count++;
                    pageHeight = pageSize.getHeight();
                    yPos = yTop;
                    limit = pageHeight - this.bottomMargin;
                }
                if (lines > 1 && yPos + (float) (lines - 1) * lineHeight > limit) {
                    lines = Math.min(lines, mostLines(yPos, limit, lineHeight));
                }
                if (lines > 1) {
                    yPos += (float) (lines - 1) * lineHeight;
                }
                yPos += font.descent + 2*verticalPadding + font.ascent;
                first = false;
                if (yPos > limit) {
                    newPage = true;
                }
            }
        } finally {
            close(iterator);
        }
        return count;
    }

    // Measures text in a font, and in its fallback font, if it has one, the
    // characters the font does not have.
    static final class Measure {
        final Font font;
        final Font fallback;

        Measure(Font font, Font fallback) {
            this.font = font;
            this.fallback = fallback;
        }

        float width(String text) {
            if (fallback == null) {
                return font.stringWidth(font.size, text);
            }
            return font.stringWidth(fallback, font.size, text);
        }
    }

    // The measure of the font: the rows' with their fallback font, the
    // header's with its own.
    private Measure measureOf(Font font) {
        if (font == f2) {
            return new Measure(font, fallback2);
        }
        return new Measure(font, fallback1);
    }

    // Draws the text at its baseline, in the font and its fallback font, if
    // it has one.
    private void drawText(Font font, String text, float x, float y) {
        Font fallback = measureOf(font).fallback;
        if (fallback == null) {
            page.drawTextLine(font, text, x, y);
            return;
        }
        page.drawString(font, fallback, font.size, text, x, y);
    }
}
