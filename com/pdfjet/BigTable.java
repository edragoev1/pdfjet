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
    private float x1;
    private float y1;
    private float yText;
    private List<Page> pages;
    private List<Integer> align;
    private List<Float> vertLines;
    private List<String> headerRow;
    private float bottomMargin = 15f;
    private float padding = 2.0f;
    private String language = "en-US";
    private boolean highlightRow = true;
    private int highlightColor = 0xF0F0F0;
    private int penColor = 0xB0B0B0;
    private List<Float> widths;
    private String fileName;
    private String delimiter;
    private int numberOfColumns;    // Total column count

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
     * @param x1 the x coordinate of the top left corner of the table box.
     * @param y1 the y coordinate of the top left corner of the table box.
     */
    public void setLocation(float x1, float y1) {
        this.x1 = x1;
        this.y1 = y1;
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
    public void setTextAlignment(int column, int align) {
        this.align.set(column, align);
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

    /**
     * Sets the column widths.
     *
     * @param widths the widths.
     */
    public void setColumnWidths(List<Float> widths) {
        vertLines = new ArrayList<Float>();
        vertLines.add(x1);
        float sumOfWidths = x1;
        for (Float width : widths) {
            sumOfWidths += width + 2 * padding;
            vertLines.add(sumOfWidths);
        }
    }

    /**
     * Draws the specified row.
     *
     * @param row the row to draw.
     * @param markerColor the color of the marker.
     * @throws Exception if there is an issue.
     */
    public void drawRow(List<String> row, int markerColor) throws Exception {
        if (headerRow == null) {
            headerRow = row;
            newPage(Color.black);
        } else {
            drawOn(row, markerColor);
        }
    }

    private void newPage(int color) throws Exception {
        float[] original;
        if (page != null) {
            page.addArtifactBMC();
            original = page.getPenColor();
            page.setPenColor(penColor);
            page.drawLine(
                    vertLines.get(0),
                    yText - f1.ascent,
                    vertLines.get(headerRow.size()),
                    yText - f1.ascent);
            // Draw the vertical lines
            for (int i = 0; i <= headerRow.size(); i++) {
                page.drawLine(
                        vertLines.get(i),
                        y1,
                        vertLines.get(i),
                        yText - f1.ascent);
            }
            page.setPenColor(original);
            page.addEMC();
        }

        page = new Page(pdf, pageSize, Page.DETACHED);
        pages.add(page);
        page.setPenWidth(0f);
        yText = y1 + f1.ascent;

        // Highlight row and draw horizontal line
        page.addArtifactBMC();
        highlightRow(page, highlightColor, f1);
        highlightRow = false;
        original = page.getPenColor();
        page.setPenColor(penColor);
        page.drawLine(
            vertLines.get(0),
            yText - f1.ascent,
            vertLines.get(headerRow.size()),
            yText - f1.ascent);
        page.setPenColor(original);
        page.addEMC();

        String rowText = getRowText(headerRow);
        page.addBMC(StructElem.P, language, rowText, rowText);
        page.setTextFont(f1);
        page.setBrushColor(color);
        float xText = 0f;
        float xText2 = 0f;
        for (int i = 0; i < headerRow.size(); i++) {
            String text = headerRow.get(i);
            xText = vertLines.get(i);
            xText2 = vertLines.get(i + 1);
            page.beginText();
            if (align == null || align.get(i) == 0) {   // Align Left
                page.setTextLocation(xText + padding, yText);
            } else if (align.get(i) == 1) {             // Align Right
                page.setTextLocation((xText2 - padding) - f1.stringWidth(text), yText);
            }
            page.drawText(text);
            page.endText();
        }
        page.addEMC();
        yText += f1.descent + f2.ascent;
    }

    private void drawOn(List<String> row, int markerColor) throws Exception {
        if (row.size() > headerRow.size()) {
            // Prevent crashes when some data rows have extra fields!
            // The application should check for this and handle it the right way.
            return;
        }

        // Highlight row and draw horizontal line
        page.addArtifactBMC();
        if (highlightRow) {
            highlightRow(page, highlightColor, f2);
            highlightRow = false;
        } else {
            highlightRow = true;
        }
        float[] original = page.getPenColor();
        page.setPenColor(penColor);
        page.moveTo(vertLines.get(0), yText - f2.ascent);
        page.lineTo(vertLines.get(headerRow.size()), yText - f2.ascent);
        page.strokePath();
        page.setPenColor(original);
        page.addEMC();

        String rowText = getRowText(row);
        page.addBMC(StructElem.P, language, rowText, rowText);
        page.setPenWidth(0f);
        page.setTextFont(f2);
        page.setBrushColor(Color.black);
        float xText = 0f;
        float xText2 = 0f;
        for (int i = 0; i < row.size(); i++) {
            String text = row.get(i);
            xText = vertLines.get(i);
            xText2 = vertLines.get(i + 1);
            page.beginText();
            if (align == null || align.get(i) == 0) {   // Align Left
                page.setTextLocation(xText + padding, yText);
            } else if (align.get(i) == 1) {             // Align Right
                page.setTextLocation((xText2 - padding) - f2.stringWidth(text), yText);
            }
            page.drawText(text);
            page.endText();
        }
        page.addEMC();
        if (markerColor != Color.black) {
            page.addArtifactBMC();
            float[] originalColor = page.getPenColor();
            page.setPenColor(markerColor);
            page.setPenWidth(3f);
            page.drawLine(
                    vertLines.get(0) - this.padding,
                    yText - f2.ascent,
                    vertLines.get(0) - this.padding,
                    yText + f2.descent);
            page.drawLine(
                xText2 + this.padding,
                yText - f2.ascent,
                xText2 + this.padding,
                yText + f2.descent);
            page.setPenColor(originalColor);
            page.setPenWidth(0f);
            page.addEMC();
        }
        yText += f2.descent + f2.ascent;
        if (yText + f2.descent > (page.height - bottomMargin)) {
            newPage(Color.black);
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
			    firstRow = false;
			    continue;
		    }
            String[] fields = line.split(this.delimiter);
            List<String> row = new ArrayList<String>();
            for (int i = 0; i < 10; i++) {
                row.add(fields[i]);
            }
            this.drawRow(row, Color.black);
        }
        reader.close();

        page.addArtifactBMC();
        float[]original = page.getPenColor();
        page.setPenColor(penColor);
        page.drawLine(
                vertLines.get(0),
                yText - f2.ascent,
                vertLines.get(this.numberOfColumns),
                yText - f2.ascent);
        // Draw the vertical lines
        for (int i = 0; i <= this.numberOfColumns; i++) {
            page.drawLine(
                    vertLines.get(i),
                    y1,
                    vertLines.get(i),
                    yText - f1.ascent);
        }
        page.setPenColor(original);
        page.addEMC();
    }

    /**
     * highlightRow fills a row's background with highlight color
     */
    private void highlightRow(Page page, int color, Font font) {
        float[] original = page.getBrushColor();
        page.setBrushColor(color);
        page.moveTo(vertLines.get(0), yText - font.ascent);
        page.lineTo(vertLines.get(this.numberOfColumns), yText - font.ascent);
        page.lineTo(vertLines.get(this.numberOfColumns), yText + font.descent);
        page.lineTo(vertLines.get(0), yText + font.descent);
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
        BufferedReader reader = new BufferedReader(new FileReader(fileName));
        this.widths = new ArrayList<Float>();
        align = new ArrayList<Integer>();
        int rowNumber = 0;
        String line = null;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split(this.delimiter);
            for (int i = 0; i < fields.length; i++) {
                String field = fields[i];
                float width = f1.stringWidth(null, field);
                if (rowNumber == 0) {   // Header Row
                    this.widths.add(width);
                } else {
                    if (i < widths.size() && width > widths.get(i)) {
                        this.widths.set(i, width);
                    }
                }
            }
            if (rowNumber == 1) {       // First Data Row
                for (String field : fields) {
                    align.add(getAlignment(field));
                }
            }
            rowNumber++;
        }
        reader.close();

      	// Precompute vertical line positions
        this.vertLines = new ArrayList<>();
        this.vertLines.add(this.x1);
        float vertLineX = this.x1;
        for (int i = 0; i < numberOfColumns; i++) {
            vertLineX += this.widths.get(i);
            this.vertLines.add(vertLineX);
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
