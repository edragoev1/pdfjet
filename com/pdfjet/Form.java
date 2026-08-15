/**
 * Form.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Please see Example_42
 */
public class Form implements Drawable {
    private final List<Field> fields;
    private float x;
    private float y;
    private Font f1;
    private float labelFontSize = 8f;
    private Font f2;
    private float valueFontSize = 10f;
    private float formWidth = 500f;
    private float lineWidth = 0.0f;
    private float[] labelColor = new float[] {0f, 0f, 0f};
    private float[] valueColor = new float[] {0.33f, 0.33f, 0.66f};

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
     * Sets the form width
     *
     * @param formWidth the form width
     * @return this form
     */
    public Form setFormWidth(float formWidth) {
        this.formWidth = formWidth;
        return this;
    }

    /**
     * Sets the line width
     *
     * @param lineWidth the line width
     * @return this form
     */
    public Form setLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
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
        if (page == null) {
            throw new NullPointerException("Page cannot be null");
        }

        float yField = 0f;
        float xOffset = 3f;
        for (int i = 0; i < fields.size(); i++) {
            Field field = fields.get(i);
            if (field.x == 0f) {
                if (!field.label.equals("")) {
                    if (i > 0) {
                        Line hLine = new Line(
                                x,
                                y + yField,
                                x + formWidth,
                                y + yField);
                        hLine.setWidth(lineWidth).drawOn(page);
                    }
                    yField += f1.getAscent(labelFontSize) + 3f*f1.getDescent(labelFontSize);
                }
                yField += f2.getAscent(valueFontSize) + f2.getDescent(valueFontSize);
            }

            if (!field.label.equals("")) {
                float yOffset = 2*f1.getDescent(labelFontSize) +
                        f2.getAscent(valueFontSize) + f2.getDescent(valueFontSize);
                new TextLine(f1, field.label)
                        .setFontSize(labelFontSize)
                        .setTextColor(labelColor)
                        .setLocation(
                                x + field.x + xOffset,
                                y + yField - yOffset).drawOn(page);
            }

            new TextLine(f2, field.value)
                    .setFontSize(valueFontSize)
                    .setTextColor(valueColor)
                    .setLocation(xOffset + x + field.x, y + yField - f2.getDescent(valueFontSize))
                    .drawOn(page);

            if (field.x != 0f) {
                float rowHeight = f1.getAscent(labelFontSize) + 3f*f1.getDescent(labelFontSize);
                rowHeight += f2.getAscent(valueFontSize) + f2.getDescent(valueFontSize);
                Line vLine = new Line(
                        x + field.x,
                        (y + yField) - rowHeight,
                        x + field.x,
                        y + yField);
                vLine.setWidth(lineWidth).drawOn(page);
            }
        }

        Rect rect = new Rect();
        rect.setLocation(x, y);
        rect.setBorderWidth(lineWidth);
        rect.setBorderColor(Color.black);
        rect.setSize(formWidth, yField);
        rect.drawOn(page);

        return new float[] { x + formWidth, y + yField };
    }
}   // End of Form.java
