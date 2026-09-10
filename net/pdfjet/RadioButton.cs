/**
 * RadioButton.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Creates a RadioButton, which can be set selected or unselected.
 */
namespace PDFjet.NET {
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

    /**
     * Creates a RadioButton that is not selected.
     */
    public RadioButton(Font font, String label) {
        this.font = font;
        this.label = label;
    }

    /**
     * Sets the font size to use for this text line.
     *
     * @param fontSize the fontSize to use.
     * @return this RadioButton.
     */
    public RadioButton SetFontSize(float fontSize) {
        this.font.SetSize(fontSize);
        return this;
    }

    public RadioButton SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
    }

    /**
     * Set the x,y position on the Page.
     *
     * @param x the x coordinate on the Page.
     * @param y the y coordinate on the Page.
     * @return this RadioButton.
     */
    public RadioButton SetPosition(float x, float y) {
        SetLocation(x, y);
        return this;
    }

    IDrawable IDrawable.SetPosition(float x, float y) {
        return SetPosition(x, y);
    }

    /**
     * Set the x,y location on the Page.
     *
     * @param x the x coordinate on the Page.
     * @param y the y coordinate on the Page.
     * @return this RadioButton.
     */
    public RadioButton SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public RadioButton SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     * Selects or deselects this radio button.
     *
     * @param selected the selection flag.
     * @return this RadioButton.
     */
    public RadioButton Select(bool selected) {
        this.selected = selected;
        return this;
    }

    /**
     * Sets the URI for the "click text line" action.
     *
     * @param uri the URI.
     * @return this RadioButton.
     */
    public RadioButton SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Sets the alternate description of this radio button.
     *
     * @param altDescription the alternate description of the radio button.
     * @return this RadioButton.
     */
    public RadioButton SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text for this radio button.
     *
     * @param actualText the actual text for the radio button.
     * @return this RadioButton.
     */
    public RadioButton SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Draws this RadioButton on the specified Page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
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
