/*
 * Box.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create rectangular boxes on a page.
/// </summary>
public class Box : IDrawable {
    internal float x;
    internal float y;

    private float w;
    private float h;

    private int color = Color.black;
    private float width = 0f;
    private String pattern = "[] 0";
    private bool fillShape = false;

    private String uri = null;
    private String key = null;
    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public Box() {
    }

    /*
     * Creates a box object.
     *
     * @param x the x coordinate of the top left corner of this box when drawn on the page.
     * @param y the y coordinate of the top left corner of this box when drawn on the page.
     * @param w the width of this box.
     * @param h the height of this box.
     */
//    public Box(double x, double y, double w, double h) : this((float) x, (float) y, (float) w, (float) h) {
//    }

    /// <summary>
    /// Creates a box object.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of this box when drawn on the page.</param>
    /// <param name="y">the y coordinate of the top left corner of this box when drawn on the page.</param>
    /// <param name="w">the width of this box.</param>
    /// <param name="h">the height of this box.</param>
    public Box(float x, float y, float w, float h) {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of this box. Same as SetLocation.</summary>
    public Box SetXY(float x, float y) {
        SetLocation(x, y);
        return this;
    }

    /// <summary>
    /// Sets the location of this box on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of this box when drawn on the page.</param>
    /// <param name="y">the y coordinate of the top left corner of this box when drawn on the page.</param>
    public Box SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location of the top left corner of this box.</summary>
    public Box SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    /// Sets the size of this box.
    /// </summary>
    /// <param name="w">the width of this box.</param>
    /// <param name="h">the height of this box.</param>
    /// <returns>this Box object.</returns>
    public Box SetSize(double w, double h) {
        SetSize((float) w, (float) h);
        return this;
    }

    /// <summary>
    /// Sets the size of this box.
    /// </summary>
    /// <param name="w">the width of this box.</param>
    /// <param name="h">the height of this box.</param>
    /// <returns>this Box object.</returns>
    public Box SetSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /// <summary>
    /// Sets the color for this box.
    /// </summary>
    /// <param name="color">the color specified as an integer.</param>
    /// <returns>this Box object.</returns>
    public Box SetColor(int color) {
        this.color = color;
        return this;
    }

    /// <summary>
    /// Sets the width of this line.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <returns>this Box object.</returns>
    public Box SetLineWidth(double width) {
        this.width = (float) width;
        return this;
    }

    /// <summary>
    /// Sets the width of this line.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <returns>this Box object.</returns>
    public Box SetLineWidth(float width) {
        this.width = width;
        return this;
    }

    /// <summary>
    /// Sets the URI for the "click box" action.
    /// </summary>
    /// <param name="uri">the URI</param>
    /// <returns>this Box object.</returns>
    public Box SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Sets the destination key for the action.
    /// </summary>
    /// <param name="key">the destination name.</param>
    /// <returns>this Box object.</returns>
    public Box SetGoToAction(String key) {
        this.key = key;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of this box.
    /// </summary>
    /// <param name="altDescription">the alternate description of the box.</param>
    /// <returns>this Box.</returns>
    public Box SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the actual text for this box.
    /// </summary>
    /// <param name="actualText">the actual text for the box.</param>
    /// <returns>this Box.</returns>
    public Box SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>
    /// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    /// <code>
    /// Examples of line dash patterns:
    ///
    ///     "[Array] Phase"     Appearance          Description
    ///     _______________     _________________   ____________________________________
    ///
    ///     "[] 0"              -----------------   Solid line
    ///     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// </code>
    /// </summary>
    /// <param name="pattern">the line dash pattern.</param>
    /// <returns>this Box object.</returns>
    public Box SetPattern(String pattern) {
        this.pattern = pattern;
        return this;
    }

    /// <summary>
    /// Sets the private fillShape variable.
    /// If the value of fillShape is true - the box is filled with the current brush color.
    /// </summary>
    /// <param name="fillShape">the value used to set the private fillShape variable.</param>
    /// <returns>this Box object.</returns>
    public Box SetFillShape(bool fillShape) {
        this.fillShape = fillShape;
        return this;
    }

    /// <summary>
    /// Scales this box by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the box.</param>
    public void ScaleBy(double factor) {
        ScaleBy((float) factor);
    }

    /// <summary>
    /// Scales this box by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the box.</param>
    public void ScaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    /// <summary>
    /// Draws this box on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);
        page.SetPenWidth(width);
        page.SetStrokeDashPattern(pattern);
        if (fillShape) {
            page.SetBrushColor(color);
        } else {
            page.SetPenColor(color);
        }
        page.MoveTo(x, y);
        page.LineTo(x + w, y);
        page.LineTo(x + w, y + h);
        page.LineTo(x, y + h);
        if (fillShape) {
            page.FillPath();
        } else {
            page.ClosePath();
        }
        page.AddEMC();

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w,
                    y + h,
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Transparency
                    null,   // Title
                    null,   // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] {x + w, y + h + width};
    }
}   // End of Box.cs
}   // End of namespace PDFjet.NET
