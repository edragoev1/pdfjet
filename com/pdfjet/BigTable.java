package com.pdfjet;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.*;

/**
 * Use this class if you have a lot of data.
 */
public class BigTable {
    private final PDF pdf;
    private final Font f1;
    private final Font f2;
    private float[] pageSize;
    private float y;
    private float yText;
    private List<Page> pages;
    private Page page;
    private float[] widths;
    private String[] headerFields;
    private Integer[] alignment;
    private float[] vertLines;
    private float bottomMargin = 20.0f;
    private float padding = 2.0f;
    private String language = "en-US";
    private boolean highlightRow = true;
    private int highlightColor = 0xF0F0F0;
    private int penColor = 0xB0B0B0;
    private String fileName;
    private String delimiter;
    private int numberOfColumns;    // Total column count
    private boolean startNewPage = true;

    /**
     * Creates a table and sets the fonts and page size.
     *
     * @param pdf the font.
     * @param f1 the header font.
     * @param f2 the the body font.
     * @param pageSize specifies the page size.
     */
    public BigTable(PDF pdf, Font f1, Font f2, float[] pageSize) {
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
     */
    public void setLocation(float x, float y) {
        // Adjust all vertical line positions relative to new X
        for (int i = 0; i <= this.numberOfColumns; i++) {
            this.vertLines[i] += x;
        }
        this.y = y;
    }

    public void setNumberOfColumns(int numberOfColumns) {
        this.numberOfColumns = numberOfColumns;
    }

    /**
     * Sets the text alignment in the specified column.
     *
     * @param column the column.
     * @param align the alignment.
     */
    public void setTextAlignment(int column, int alignment) {
        this.alignment[column] = alignment;
    }

    /**
     * Sets the bottom margin.
     *
     * @param bottomMargin the bottom margin.
     */
    public void setBottomMargin(float bottomMargin) {
        this.bottomMargin = bottomMargin;
    }

    /**
     * Sets the language.
     *
     * @param language the language.
     */
    public void setLanguage(String language) {
        this.language = language;
    }

    /**
     * Returns the pages.
     *
     * @return the pages.
     */
    public List<Page> getPages() {
        return pages;
    }

    private void drawTextAndLine(String[] fields, Font font) throws Exception {
        if (page == null) {     // The first page
            page = new Page(pdf, pageSize, Page.DETACHED);
            pages.add(page);
            page.setPenWidth(0f);
            this.yText = this.y + f1.ascent;
            this.highlightRow = true;
            drawFieldsAndLine(headerFields, f1);
            this.yText += f1.descent + f2.ascent;
            startNewPage = false;
            return;
        }
        if (startNewPage) {     // Create new page
            page = new Page(pdf, pageSize, Page.DETACHED);
            pages.add(page);
            page.setPenWidth(0f);
            this.yText = this.y + f1.ascent;
            this.highlightRow = true;
            drawFieldsAndLine(headerFields, f1);
            this.yText += f1.descent + f2.ascent;
            startNewPage = false;
        }

        drawFieldsAndLine(fields, f2);
        // Advance to next line and check pagination
        this.yText +=  f2.ascent + f2.descent;
        if (this.yText > (this.page.height - this.bottomMargin)) {
            drawTheVerticalLines();
            startNewPage = true;
        }
    }

    private void drawFieldsAndLine(String[] fields, Font font) {
        page.addArtifactBMC();
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
        // page.addEMC();

        // String rowText = getRowText(fields);
        // page.addBMC(StructElem.P, language, rowText, rowText);
        // page.addArtifactBMC();
        page.setPenWidth(0f);
        page.setTextFont(font);
        page.setBrushColor(Color.black);
        for (int i = 0; i < this.numberOfColumns; i++) {
            String text = fields[i];
            float xText1 = vertLines[i] + this.padding;
            float xText2 = vertLines[i + 1] - this.padding;
            page.beginText();
            if (alignment[i] == Align.LEFT) {           // Align Left
                page.setTextLocation(xText1, this.yText);
            } else if (alignment[i] == Align.RIGHT) {   // Align Right
                page.setTextLocation(xText2 - font.stringWidth(text), this.yText);
            }
            page.drawText(text);
            page.endText();
        }
        page.addEMC();
    }

    /**
     * highlightRow fills a row's background with highlight color
     */
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
        page.addArtifactBMC();
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
        page.addEMC();
    }

    private String getRowText(String[] row) {
        StringBuilder buf = new StringBuilder();
        for (String field : row) {
            buf.append(field);
            buf.append(" ");
        }
        return buf.toString();
    }

    private int getAlignment(String str) {
        StringBuilder buf = new StringBuilder();
        if (str.startsWith("(") && str.endsWith(")")) {
            str = str.substring(1, str.length() - 1);
        }
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (ch != '.' && ch != ',' && ch != '\'') {
                buf.append(ch);
            }
        }
        try {
            Double.parseDouble(buf.toString());
            return Align.RIGHT; // Align Right
        } catch (NumberFormatException nfe) {
        }
        return Align.LEFT;      // Align Left
    }

    /**
     * Sets the column widths, the column alignment and header fields.
     *
     * @param fileName the file name.
     * @throws IOException if there is an issue.
     */
    public void setTableData(String fileName, String delimiter) throws IOException {
        this.fileName = fileName;
        this.delimiter = delimiter;
        this.vertLines = new float[this.numberOfColumns + 1];
        this.headerFields = new String[this.numberOfColumns];
        this.widths = new float[this.numberOfColumns];
        this.alignment = new Integer[this.numberOfColumns];

        int rowNumber = 0;
        BufferedReader reader = new BufferedReader(new FileReader(fileName));
        String line = null;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split(this.delimiter);
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
        reader.close();

      	// Precompute vertical line positions
        this.vertLines[0] = 0.0f;
        float vertLineX = 0.0f;
        for (int i = 0; i < widths.length; i++) {
            vertLineX += this.widths[i];
            this.vertLines[i + 1] = vertLineX;
        }
    }

    public void complete() throws Exception {
        BufferedReader reader =
                new BufferedReader(new FileReader(this.fileName));
        String line = null;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split(this.delimiter);
            if (fields.length < this.numberOfColumns) {
                continue;
            }
            this.drawTextAndLine(fields, f2);
        }
        reader.close();
        drawTheVerticalLines();
    }
}
