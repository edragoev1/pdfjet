/*
 * RadioButton.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Creates a RadioButton, which can be set selected or unselected.
/// </summary>
public class RadioButton : IDrawable {
    private bool selected = false;
    private float x;
    private float y;
    private float r1;
    private float r2;
    private float penWidth;
    private Font font = null;
    private String label = "";
    private String uri = null;
    private String language = null;
    private String altDescription = Single.space;
    private String actualText = Single.space;

    /// <summary>
    /// Creates a RadioButton that is not selected.
    /// </summary>
    public RadioButton(Font font, String label) {
        this.font = font;
        this.label = label;
    }

    /// <summary>
    /// Sets the font size to use for this text line.
    /// </summary>
    /// <param name="fontSize">the fontSize to use.</param>
    /// <returns>this RadioButton.</returns>
    public RadioButton SetFontSize(float fontSize) {
        this.font.SetSize(fontSize);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Set the x,y location on the Page.
    /// </summary>
    /// <param name="x">the x coordinate on the Page.</param>
    /// <param name="y">the y coordinate on the Page.</param>
    /// <returns>this RadioButton.</returns>
    public RadioButton SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location of this radio button on the page.</summary>
    public RadioButton SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    /// Selects or deselects this radio button.
    /// </summary>
    /// <param name="selected">the selection flag.</param>
    /// <returns>this RadioButton.</returns>
    public RadioButton Select(bool selected) {
        this.selected = selected;
        return this;
    }

    /// <summary>
    /// Sets the URI for the "click text line" action.
    /// </summary>
    /// <param name="uri">the URI.</param>
    /// <returns>this RadioButton.</returns>
    public RadioButton SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of this radio button.
    /// </summary>
    /// <param name="altDescription">the alternate description of the radio button.</param>
    /// <returns>this RadioButton.</returns>
    public RadioButton SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the actual text for this radio button.
    /// </summary>
    /// <param name="actualText">the actual text for the radio button.</param>
    /// <returns>this RadioButton.</returns>
    public RadioButton SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>
    /// Draws this RadioButton on the specified Page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);

        this.r1 = font.GetAscent()/2;
        this.r2 = r1/2;
        this.penWidth = r1/10;

        float yBox = y;
        page.SetPenWidth(1f);
        page.SetPenColor(Color.black);
        page.SetStrokeDashPattern("[] 0");
        page.SetBrushColor(Color.black);
        page.DrawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r1);

        if (this.selected) {
            page.DrawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r2, PathOperator.Fill);
        }

        if (uri != null) {
            page.SetBrushColor(Color.blue);
        }
        page.DrawString(font, font.GetSize(), label, x + 3*r1, y + font.GetAscent());
        page.SetPenWidth(0f);
        page.SetBrushColor(Color.black);

        page.AddEMC();

        if (uri != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x + 3*r1,
                    y,
                    x + 3*r1 + font.StringWidth(label),
                    y + font.GetBodyHeight(),
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Transparency
                    null,   // Title
                    null,   // Contents
                    uri,
                    null,
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] { x + 6*r1 + font.StringWidth(label), y + font.GetBodyHeight() };
    }
}   // End of RadioButton.cs
}   // End of namespace PDFjet.NET
