/*
 * Form.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Please see Example_42
/// </summary>
public class Form : IDrawable {
    private List<Field> fields;
    private float x;
    private float y;
    private Font f1;
    private float labelFontSize = 9f;
    private Font f2;
    private float valueFontSize = 9f;
    private float formWidth = 500f;
    private float lineWidth = 0.0f;
    private float[] labelColor = new float[] {0f, 0f, 0f};
    private float[] valueColor = new float[] {0.33f, 0.33f, 0.66f};

    /// <summary>
    /// Creates a Form object
    /// </summary>
    /// <param name="fields">the fields contained in this form</param>
    public Form(List<Field> fields) {
        this.fields = fields;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location of this form on the page
    /// </summary>
    /// <param name="x">the horizontal location</param>
    /// <param name="y">the vertical locations</param>
    /// <returns>the form</returns>
    public Form SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>
    /// Sets the location of this form on the page
    /// </summary>
    /// <param name="x">the horizontal location</param>
    /// <param name="y">the vertical locations</param>
    /// <returns>the form</returns>
    public Form SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    /// Sets the form width
    /// </summary>
    /// <param name="formWidth">the form width</param>
    /// <returns>this form</returns>
    public Form SetFormWidth(float formWidth) {
        this.formWidth = formWidth;
        return this;
    }

    /// <summary>
    /// Sets the line width
    /// </summary>
    /// <param name="lineWidth">the line width</param>
    /// <returns>this form</returns>
    public Form SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
        return this;
    }

    /// <summary>
    /// Sets the font for the label
    /// </summary>
    /// <param name="f1">the font</param>
    /// <returns>this form</returns>
    public Form SetLabelFont(Font f1) {
        this.f1 = f1;
        return this;
    }

    /// <summary>
    /// Sets the size for the label font
    /// </summary>
    /// <param name="labelFontSize">the label font size</param>
    /// <returns>the form</returns>
    public Form SetLabelFontSize(float labelFontSize) {
        this.labelFontSize = labelFontSize;
        return this;
    }

    /// <summary>
    /// Sets the font for the value
    /// </summary>
    /// <param name="f2">the value font</param>
    /// <returns>the form</returns>
    public Form SetValueFont(Font f2) {
        this.f2 = f2;
        return this;
    }

    /// <summary>
    /// Sets the size for the value font
    /// </summary>
    /// <param name="valueFontSize">the font size</param>
    /// <returns>the form</returns>
    public Form SetValueFontSize(float valueFontSize) {
        this.valueFontSize = valueFontSize;
        return this;
    }

    /// <summary>
    /// Sets the label color
    /// </summary>
    /// <param name="labelColor">the label color</param>
    /// <returns>the form</returns>
    public Form SetLabelColor(float[] labelColor) {
        this.labelColor = labelColor;
        return this;
    }

    /// <summary>
    /// Sets the color for the value
    /// </summary>
    /// <param name="valueColor">the value color</param>
    /// <returns>the form</returns>
    public Form SetValueColor(float[] valueColor) {
        this.valueColor = valueColor;
        return this;
    }

    /// <summary>
    ///  Draws this Form on the specified page.
    /// </summary>
    /// <param name="page">the page to draw this form on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    /// <exception cref="System.Exception">If an input or output exception occurred</exception>
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
                        hLine.SetStrokeWidth(lineWidth).DrawOn(page);
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
                vLine.SetStrokeWidth(lineWidth).DrawOn(page);
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
