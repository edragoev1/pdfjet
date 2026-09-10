/*
 * CheckBox.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Creates a CheckBox, which can be set checked or unchecked.
/// By default the check box is unchecked.
/// Portions provided by Shirley C. Christenson
/// Shirley Christenson Consulting
/// </summary>
public class CheckBox : IDrawable {
    private float x;
    private float y;
    private float w;
    private float h;
    private int boxColor = Color.black;
    private int checkColor = Color.blue;
    private float penWidth;
    private float checkWidth;
    private int mark = 0;
    private Font font = null;
    private float fontSize = 12f;
    private String label = "";
    private String uri = null;

    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /// <summary>
    /// Creates a CheckBox with blue check mark.
    /// </summary>
    public CheckBox(Font font, String label) {
        this.font = font;
        this.label = label;
    }

    /// <summary>
    /// Sets the font size to use for this text line.
    /// </summary>
    /// <param name="fontSize">the fontSize to use.</param>
    /// <returns>this CheckBox.</returns>
    public CheckBox SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /// <summary>
    /// Sets the color of the check box.
    /// </summary>
    /// <param name="boxColor">the check box color specified as an 0xRRGGBB integer.</param>
    /// <returns>this CheckBox.</returns>
    public CheckBox SetBoxColor(int boxColor) {
        this.boxColor = boxColor;
        return this;
    }

    /// <summary>
    /// Sets the color of the check mark.
    /// </summary>
    /// <param name="checkColor">the check mark color specified as an 0xRRGGBB integer.</param>
    /// <returns>this CheckBox.</returns>
    public CheckBox SetCheckmark(int checkColor) {
        this.checkColor = checkColor;
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
    /// <returns>this CheckBox.</returns>
    public CheckBox SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location of this check box on the page.</summary>
    public CheckBox SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    /// Gets the height of the CheckBox.
    /// </summary>
    public float GetHeight() {
        return this.h;
    }

    /// <summary>
    /// Gets the width of the CheckBox.
    /// </summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>
    /// Checks or unchecks this check box. See the Mark class for available options.
    /// </summary>
    /// <returns>this CheckBox.</returns>
    public CheckBox Check(int mark) {
        this.mark = mark;
        return this;
    }

    /// <summary>
    /// Sets the URI for the "click text line" action.
    /// </summary>
    /// <param name="uri">the URI.</param>
    /// <returns>this CheckBox.</returns>
    public CheckBox SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of this check box.
    /// </summary>
    /// <param name="altDescription">the alternate description of the check box.</param>
    /// <returns>this CheckBox.</returns>
    public CheckBox SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the actual text for this check box.
    /// </summary>
    /// <param name="actualText">the actual text for the check box.</param>
    /// <returns>this CheckBox.</returns>
    public CheckBox SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>Draws a blue X mark of the specified size at the specified location.</summary>
    public static void XMark(Page page, float x, float y, float size) {
        page.SetPenColor(Color.blue);
        page.SetPenWidth(size / 5);
        page.MoveTo(x, y);
        page.LineTo(x + size, y + size);
        page.MoveTo(x, y + size);
        page.LineTo(x + size, y);
        page.StrokePath();
    }

    /// <summary>
    /// Draws this CheckBox on the specified Page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    /// <exception cref="System.Exception"/>
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);

        this.w = font.GetAscent();
        this.h = this.w;
        this.penWidth = this.w/15;
        this.checkWidth = this.w/5;

        float yBox = y;
        page.SetPenWidth(penWidth);
        page.SetPenColor(boxColor);
        page.SetStrokeDashPattern("[] 0");
        page.DrawRect(x + this.penWidth, yBox + this.penWidth, w, h);

        if (mark == Mark.CHECK || mark == Mark.X) {
            page.SetPenWidth(checkWidth);
            page.SetPenColor(checkColor);
            if (mark == Mark.CHECK) {
                // Draw check mark
                page.MoveTo(x + checkWidth + penWidth, yBox + h/2 + penWidth);
                page.LineTo((x + w/6 + checkWidth) + penWidth, ((yBox + h) - 4f*checkWidth/3f) + penWidth);
                page.LineTo(((x + w) - checkWidth) + penWidth, (yBox + checkWidth) + penWidth);
                page.StrokePath();
            } else if (mark == Mark.X) {
                // Draw 'X' mark
                page.MoveTo(x + checkWidth + penWidth, yBox + checkWidth + penWidth);
                page.LineTo(((x + w) - checkWidth) + penWidth, ((yBox + h) - checkWidth) + penWidth);
                page.MoveTo(((x + w) - checkWidth) + penWidth, (yBox + checkWidth) + penWidth);
                page.LineTo((x + checkWidth) + penWidth, ((yBox + h) - checkWidth) + penWidth);
                page.StrokePath();
            }
        }

        if (uri != null) {
            page.SetBrushColor(Color.blue);
        }
        page.DrawString(font, fontSize, label, x + 3f*w/2f, y + font.GetAscent());
        page.SetPenWidth(0f);
        page.SetPenColor(Color.black);
        page.SetBrushColor(Color.black);

        page.AddEMC();

        if (uri != null) {  // TODO: BMC and EMC here!
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x + 3f*w/2f,
                    y,
                    x + 3f*w/2f + font.StringWidth(label),
                    y + font.GetBodyHeight(),       // TODO: Use fontSize
                    null,       // Vertices
                    null,       // Fill Color
                    0f,         // Transparency
                    null,       // Title
                    null,       // Contents
                    uri,
                    null,
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] { x + 3f*w + font.StringWidth(label), y + font.GetBodyHeight() };
    }
}   // End of CheckBox.cs
}   // End of namespace PDFjet.NET
