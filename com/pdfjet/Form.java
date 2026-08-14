/**
 * Form.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Please see Example_45
 */
public class Form implements Drawable {
    private final List<Field> fields;
    private float x;
    private float y;
    private Font f1;
    private float labelFontSize = 8f;
    private Font f2;
    private float valueFontSize = 10f;
    private int numberOfRows;
    private float rowWidth = 500f;
    private float rowHeight = 12f;
    private float[] labelColor = new float[] {0f, 0f, 0f};
    private float[] valueColor = new float[] {0f, 0f, 1f};

    /**
     * Creates a Form object
     *
     * @param fields the fields contained in this form
     */
    public Form(List<Field> fields) {
        this.fields = fields;
    }

    /**
     * Sets the position of this form on the page
     *
     * @param x the horizontal position
     * @param y the vertical position
     */
    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    /**
     * Sets the position of this form on the page
     *
     * @param x the horizontal position
     * @param y the vertical position
     */
    public void setPosition(double x, double y) {
        setLocation(x, y);
    }

    /**
     * Sets the location of this form on the page
     *
     * @param x the horizontal location
     * @param y the vertical locations
     * @return the form
     */
    public Form setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location of this form on the page
     *
     * @param x the horizontal location
     * @param y the vertical locations
     * @return the form
     */
    public Form setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the row length
     *
     * @param rowWidth the row length
     * @return this form
     */
    public Form setRowLength(float rowWidth) {
        this.rowWidth = rowWidth;
        return this;
    }

    /**
     * Sets the row height
     *
     * @param rowHeight the row height
     * @return this form
     */
    public Form setRowHeight(float rowHeight) {
        this.rowHeight = rowHeight;
        return this;
    }

    /**
     * Sets the font for the label
     *
     * @param f1 the font
     * @return this form
     */
    public Form setLabelFont(Font f1) {
        this.f1 = f1;
        return this;
    }

    /**
     * Sets the size for the label font
     *
     * @param labelFontSize the label font size
     * @return the form
     */
    public Form setLabelFontSize(float labelFontSize) {
        this.labelFontSize = labelFontSize;
        return this;
    }

    /**
     * Sets the font for the value
     *
     * @param f2 the value font
     * @return the form
     */
    public Form setValueFont(Font f2) {
        this.f2 = f2;
        return this;
    }

    /**
     * Sets the size for the value font
     *
     * @param valueFontSize the font size
     * @return the form
     */
    public Form setValueFontSize(float valueFontSize) {
        this.valueFontSize = valueFontSize;
        return this;
    }

    /**
     * Sets the label color
     *
     * @param labelColor the label color
     * @return the form
     */
    public Form setLabelColor(float[] labelColor) {
        this.labelColor = labelColor;
        return this;
    }

    /**
     * Sets the color for the value
     *
     * @param valueColor the value color
     * @return the form
     */
    public Form setValueColor(float[] valueColor) {
        this.valueColor = valueColor;
        return this;
    }

    /**
     *  Draws this Form on the specified page.
     *
     *  @param page the page to draw this form on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        for (Field field : fields) {
            if (field.format) {
                field.values = format(field.values[0], field.values[1], this.f2, this.rowWidth);
                field.altDescription = new String[field.values.length];
                field.actualText = new String[field.values.length];
                for (int i = 0; i < field.values.length; i++) {
                    field.altDescription[i] = field.values[i];
                    field.actualText[i] = field.values[i];
                }
            }
            if (field.x == 0f) {
                numberOfRows += field.values.length;
            }
        }

        if (numberOfRows == 0) {
            return new float[] { x, y };
        }

        float boxHeight = rowHeight*numberOfRows + f2.getDescent();
        Box box = new Box();
        box.setLocation(x, y);
        box.setLineWidth(0.2f);
        box.setSize(rowWidth, boxHeight);
        if (page != null) {
            box.drawOn(page);
        }

        float yField = 0f;
        int rowSpan = 1;
        float yRow = 0;
        for (Field field : fields) {
            if (field.x == 0f) {
                yRow += rowSpan*rowHeight;
                rowSpan = field.values.length;
            }
            yField = yRow;
            for (int i = 0; i < field.values.length; i++) {
                if (page != null) {
                    Font font = (i == 0) ? f1 : f2;
                    float fontSize = (i == 0) ? labelFontSize : valueFontSize;
                    float[] color = (i == 0) ? labelColor : valueColor;
                    new TextLine(font, field.values[i])
                            .setFontSize(fontSize)
                            .setTextColor(color)
                            .setAltDescription((i == 0) ?
                                    field.altDescription[i] : (field.altDescription[i] + ","))
                            .setLocation(2f + this.x + field.x, this.y + yField)
                            .drawOn(page);
                    if (page != null && i == (field.values.length - 1)) {
                        new Line(x, y + yField + font.getDescent(),
                                x + rowWidth, y + yField + font.getDescent()).setWidth(0.2f).drawOn(page);
                        new Line(x + field.x, y + yField + font.getDescent() - (field.values.length-1)*rowHeight,
                                x + field.x, y + yField + font.getDescent()).setWidth(0.2f).drawOn(page);
                    }
                }
                yField += rowHeight;
            }
        }

        return new float[] { x + rowWidth, y + boxHeight };
    }

    /**
     * Formats the form
     *
     * @param title the form title
     * @param text the form text
     * @param font the form font
     * @param width the width of the form
     * @return the form
     */
    public static String[] format(String title, String text, Font font, float width) {
        String[] original = text.split("\\r?\\n", -1);
        List<String> lines = new ArrayList<String>();
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < original.length; i++) {
            String line = original[i];
            if (font.stringWidth(line) < width) {
                lines.add(line);
                continue;
            }

            buf.setLength(0);
            for (int j = 0; j < line.length(); j++) {
                buf.append(line.charAt(j));
                if (font.stringWidth(buf.toString()) > (width - font.stringWidth("   "))) {
                    while (j > 0 && line.charAt(j) != ' ') {
                        j -= 1;
                    }
                    String str = line.substring(0, j).trim();
                    lines.add(str);
                    buf.setLength(0);
                    while (j < line.length() && line.charAt(j) == ' ') {
                        j += 1;
                    }
                    line = line.substring(j);
                    j = 0;
                }
            }

            if (!line.equals("")) {
                lines.add(line);
            }
        }

        int count = lines.size();
        String[] data = new String[1 + count];
        data[0] = title;
        for (int i = 0; i < count; i++) {
            data[i + 1] = lines.get(i);
        }

        return data;
    }
}   // End of Form.java
