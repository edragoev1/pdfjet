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
    private Page page;
    private float[] pageSize;
    private final Font f1;
    private final Font f2;
    private float x;
    private float y;
    private float yText;
    private List<Page> pages;
    private Integer[] alignment;
    private float[] vertLines;
    private String[] headerRow;
    private float bottomMargin = 15f;
    private float padding = 2.0f;
    private String language = "en-US";
    private boolean highlightRow = true;
    private int highlightColor = 0xF0F0F0;
    private int penColor = 0xB0B0B0;
    private float[] widths;
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
        for (int i = 0; i < this.numberOfColumns; i++) {
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

    private void drawTextAndLines(List<String> row, Font font) throws Exception {
        if (startNewPage) {
            page = new Page(pdf, pageSize, Page.DETACHED);
            pages.add(page);
            page.setPenWidth(0f);
            this.yText = this.y + font.ascent;
            startNewPage = false;
        }

        page.addArtifactBMC();
        if (this.highlightRow) {
            highlightRow(page, font, highlightColor);
            this.highlightRow = false;
        } else {
            this.highlightRow = true;
        }
        float[] original = page.getPenColor();
        page.setPenColor(penColor);
        page.moveTo(vertLines[0], this.yText - font.ascent);
        page.lineTo(vertLines[this.numberOfColumns], this.yText - font.ascent);
        page.strokePath();
        page.setPenColor(original);
        page.addEMC();

        String rowText = getRowText(row);
        page.addBMC(StructElem.P, language, rowText, rowText);
        page.setPenWidth(0f);
        page.setTextFont(font);
        page.setBrushColor(Color.black);
        float xText1 = 0f;
        float xText2 = 0f;
        for (int i = 0; i < this.numberOfColumns; i++) {
            String text = row.get(i);
            xText1 = vertLines[i];
            xText2 = vertLines[i + 1];
            page.beginText();
            if (alignment[i] == Align.LEFT) {           // Align Left
                page.setTextLocation(xText1 + padding, this.yText);
            } else if (alignment[i] == Align.RIGHT) {   // Align Right
                page.setTextLocation((xText2 - padding) - font.stringWidth(text), this.yText);
            }
            page.drawText(text);
            page.endText();
        }
        page.addEMC();

        // Advance to next line and check pagination
        this.yText +=  font.ascent + font.descent;
        if (this.yText > (this.page.height - this.bottomMargin)) {
            startNewPage = true;
        }
    }

    /**
     * Draw the completed table.
     */
    public void complete() throws Exception {
        BufferedReader reader =
                new BufferedReader(new FileReader(this.fileName));
        boolean firstRow = true;
        String line = null;
        while ((line = reader.readLine()) != null) {
    		if (firstRow) { // Skip header (already processed)
                String[] fields = line.split(this.delimiter);
                List<String> row = new ArrayList<String>();
                for (int i = 0; i < numberOfColumns; i++) {
                    row.add(fields[i]);
                }
                this.drawTextAndLines(row, f1);
                firstRow = false;
			    continue;
		    }
            String[] fields = line.split(this.delimiter);
            List<String> row = new ArrayList<String>();
            for (int i = 0; i < numberOfColumns; i++) {
                row.add(fields[i]);
            }
            this.drawTextAndLines(row, f2);
        }
        reader.close();

        page.addArtifactBMC();
        float[]original = page.getPenColor();
        page.setPenColor(penColor);
        page.drawLine(
                vertLines[0],
                this.yText - f2.ascent,
                vertLines[this.numberOfColumns],
                this.yText - f2.ascent);
        // Draw the vertical lines
        for (int i = 0; i <= this.numberOfColumns; i++) {
            page.drawLine(
                    vertLines[i],
                    y,
                    vertLines[i],
                    this.yText - f1.ascent);
        }
        page.setPenColor(original);
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

    private String getRowText(List<String> row) {
        StringBuilder buf = new StringBuilder();
        for (String field : row) {
            buf.append(field);
            buf.append(" ");
        }
        return buf.toString();
    }

    /**
     * Returns the column widths.
     *
     * @param fileName the file name.
     * @throws IOException if there is an issue.
     * @return list of the column widths.
     */
    public void setTableData(String fileName, String delimiter) throws IOException {
        this.fileName = fileName;
        this.delimiter = delimiter;
        this.vertLines = new float[this.numberOfColumns + 1];
        this.headerRow = new String[this.numberOfColumns];
        this.widths = new float[this.numberOfColumns];
        this.alignment = new Integer[this.numberOfColumns];

        int rowNumber = 0;
        BufferedReader reader = new BufferedReader(new FileReader(fileName));
        String line = null;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split(this.delimiter);
            if (rowNumber == 0) {
                for (int i = 0; i < this.numberOfColumns; i++) {
                    headerRow[i] = fields[i];
                }
            }
            if (rowNumber == 1) {   // Determine alignment from first data row
                for (int i = 0; i < this.numberOfColumns; i++) {
                    alignment[i] = getAlignment(fields[i]);
                }
            }
            for (int i = 0; i < this.numberOfColumns; i++) {
                String field = fields[i];
                float width = f1.stringWidth(null, field);
                if ((width + 2*this.padding) > widths[i]) {
                    this.widths[i] += 2*this.padding;
                }
            }
            rowNumber++;
        }
        reader.close();

      	// Precompute vertical line positions
        this.vertLines[0] = this.x;
        float vertLineX = this.x;
        for (int i = 0; i < numberOfColumns; i++) {
            vertLineX += this.widths[i];
            this.vertLines[i + 1] = vertLineX;
        }
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
            return 1;   // Align Right
        } catch (NumberFormatException nfe) {
        }
        return 0;       // Align Left
    }
}
