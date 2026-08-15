/**
 * Form.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

/**
 * Please see Example_42
 */
namespace PDFjet.NET {
public class Form : IDrawable {
    private List<Field> fields;
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
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     * Sets the position of this form on the page
     *
     * @param x the horizontal position
     * @param y the vertical position
     */
    public void SetPosition(double x, double y) {
        SetLocation(x, y);
    }

    /**
     * Sets the location of this form on the page
     *
     * @param x the horizontal location
     * @param y the vertical locations
     * @return the form
     */
    public Form SetLocation(float x, float y) {
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
    public Form SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     * Sets the form width
     *
     * @param formWidth the form width
     * @return this form
     */
    public Form SetFormWidth(float formWidth) {
        this.formWidth = formWidth;
        return this;
    }

    /**
     * Sets the line width
     *
     * @param lineWidth the line width
     * @return this form
     */
    public Form SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
        return this;
    }

    /**
     * Sets the font for the label
     *
     * @param f1 the font
     * @return this form
     */
    public Form SetLabelFont(Font f1) {
        this.f1 = f1;
        return this;
    }

    /**
     * Sets the size for the label font
     *
     * @param labelFontSize the label font size
     * @return the form
     */
    public Form SetLabelFontSize(float labelFontSize) {
        this.labelFontSize = labelFontSize;
        return this;
    }

    /**
     * Sets the font for the value
     *
     * @param f2 the value font
     * @return the form
     */
    public Form SetValueFont(Font f2) {
        this.f2 = f2;
        return this;
    }

    /**
     * Sets the size for the value font
     *
     * @param valueFontSize the font size
     * @return the form
     */
    public Form SetValueFontSize(float valueFontSize) {
        this.valueFontSize = valueFontSize;
        return this;
    }

    /**
     * Sets the label color
     *
     * @param labelColor the label color
     * @return the form
     */
    public Form SetLabelColor(float[] labelColor) {
        this.labelColor = labelColor;
        return this;
    }

    /**
     * Sets the color for the value
     *
     * @param valueColor the value color
     * @return the form
     */
    public Form SetValueColor(float[] valueColor) {
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
    public float[] DrawOn(Page page) {
        if (page == null) {
            throw new ArgumentNullException(nameof(page), "Page cannot be null");
        }

        float yField = 0f;
        float xOffset = 3f;
        for (int i = 0; i < fields.Count; i++) {
            Field field = fields[i];
            if (field.x == 0f) {
                if (!field.label.Equals("")) {
                    if (i > 0) {
                        Line hLine = new Line(
                                x,
                                y + yField,
                                x + formWidth,
                                y + yField);
                        hLine.SetWidth(lineWidth).DrawOn(page);
                    }
                    yField += f1.GetAscent(labelFontSize) + 3f*f1.GetDescent(labelFontSize);
                }
                yField += f2.GetAscent(valueFontSize) + f2.GetDescent(valueFontSize);
            }

            if (!field.label.Equals("")) {
                float yOffset = 2*f1.GetDescent(labelFontSize) +
                        f2.GetAscent(valueFontSize) + f2.GetDescent(valueFontSize);
                new TextLine(f1, field.label)
                        .SetFontSize(labelFontSize)
                        .SetTextColor(labelColor)
                        .SetLocation(
                                x + field.x + xOffset,
                                y + yField - yOffset).DrawOn(page);
            }

            new TextLine(f2, field.value)
                    .SetFontSize(valueFontSize)
                    .SetTextColor(valueColor)
                    .SetLocation(xOffset + x + field.x, y + yField - f2.GetDescent(valueFontSize))
                    .DrawOn(page);

            if (field.x != 0f) {
                float rowHeight = f1.GetAscent(labelFontSize) + 3f*f1.GetDescent(labelFontSize);
                rowHeight += f2.GetAscent(valueFontSize) + f2.GetDescent(valueFontSize);
                Line vLine = new Line(
                        x + field.x,
                        (y + yField) - rowHeight,
                        x + field.x,
                        y + yField);
                vLine.SetWidth(lineWidth).DrawOn(page);
            }
        }

        Rect rect = new Rect();
        rect.SetLocation(x, y);
        rect.SetBorderWidth(lineWidth);
        rect.SetBorderColor(Color.black);
        rect.SetSize(formWidth, yField);
        rect.DrawOn(page);

        return [ x + formWidth, y + yField ];
    }
}   // End of Form.cs
}   // End of namespace PDFjet.NET
