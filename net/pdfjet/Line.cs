/*
 * Line.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create line objects.
///
/// Please see Example_01.
/// </summary>
public class Line : IDrawable {
    internal float x1;
    internal float y1;
    internal float x2;
    internal float y2;
    private float[] color = new float[] {0f, 0f, 0f};   // Black color
    private float width = 0f;
    private String pattern = "[] 0";
    private CapStyle capStyle = CapStyle.BUTT;

    private String language = null;
    private String altDescription = Single.space;
    private String actualText = Single.space;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public Line() {
    }

    /// <summary>
    /// Create a line object.
    /// </summary>
    /// <param name="x1">the x coordinate of the start point.</param>
    /// <param name="y1">the y coordinate of the start point.</param>
    /// <param name="x2">the x coordinate of the end point.</param>
    /// <param name="y2">the y coordinate of the end point.</param>
    public Line(double x1, double y1, double x2, double y2) : this((float) x1, (float) y1, (float) x2, (float) y2) {
    }

    /// <summary>
    /// Create a line object.
    /// </summary>
    /// <param name="x1">the x coordinate of the start point.</param>
    /// <param name="y1">the y coordinate of the start point.</param>
    /// <param name="x2">the x coordinate of the end point.</param>
    /// <param name="y2">the y coordinate of the end point.</param>
    public Line(float x1, float y1, float x2, float y2) {
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
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
    ///     "[] 0"              -----------------   Solid line
    ///     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// </code>
    /// </summary>
    /// <param name="pattern">the line dash pattern.</param>
    /// <returns>this Line object.</returns>
    public Line SetPattern(String pattern) {
        this.pattern = pattern;
        return this;
    }

    /// <summary>
    /// Sets the x and y coordinates of the start point.
    /// </summary>
    /// <param name="x">the x coordinate of the start point.</param>
    /// <param name="y">the t coordinate of the start point.</param>
    /// <returns>this Line object.</returns>
    public Line SetStartPoint(double x, double y) {
        this.x1 = (float) x;
        this.y1 = (float) y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the start point of this line.</summary>
    public Line SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    /// Sets the x and y coordinates of the start point.
    /// </summary>
    /// <param name="x">the x coordinate of the start point.</param>
    /// <param name="y">the y coordinate of the start point.</param>
    /// <returns>this Line object.</returns>
    public Line SetStartPoint(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    /// Sets the x and y coordinates of the start point.
    /// </summary>
    /// <param name="x">the x coordinate of the start point.</param>
    /// <param name="y">the y coordinate of the start point.</param>
    /// <returns>this Line object.</returns>
    public Line SetPointA(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    /// Returns the start point of this line.
    /// </summary>
    /// <returns>Point the point.</returns>
    public Point GetStartPoint() {
        return new Point(x1, y1);
    }

    /// <summary>
    /// Sets the x and y coordinates of the end point.
    /// </summary>
    /// <param name="x">the x coordinate of the end point.</param>
    /// <param name="y">the y coordinate of the end point.</param>
    /// <returns>this Line object.</returns>
    public Line SetEndPoint(double x, double y) {
        this.x2 = (float) x;
        this.y2 = (float) y;
        return this;
    }

    /// <summary>
    /// Sets the x and y coordinates of the end point.
    /// </summary>
    /// <param name="x">the x coordinate of the end point.</param>
    /// <param name="y">the y coordinate of the end point.</param>
    /// <returns>this Line object.</returns>
    public Line SetEndPoint(float x, float y) {
        this.x2 = x;
        this.y2 = y;
        return this;
    }

    /// <summary>
    /// Sets the x and y coordinates of the end point.
    /// </summary>
    /// <param name="x">the x coordinate of the end point.</param>
    /// <param name="y">the y coordinate of the end point.</param>
    /// <returns>this Line object.</returns>
    public Line SetPointB(float x, float y) {
        this.x2 = x;
        this.y2 = y;
        return this;
    }

    /// <summary>
    /// Returns the end point of this line.
    /// </summary>
    /// <returns>Point the point.</returns>
    public Point GetEndPoint() {
        return new Point(x2, y2);
    }

    /// <summary>
    /// Sets the stroke width of this line.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <returns>this Line object.</returns>
    public Line SetStrokeWidth(double width) {
        this.width = (float) width;
        return this;
    }

    /// <summary>
    /// Sets the stroke width of this line.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <returns>this Line object.</returns>
    public Line SetStrokeWidth(float width) {
        this.width = width;
        return this;
    }

    /// <summary>
    /// Sets the stroke color of this line.
    /// </summary>
    /// <param name="color">the color specified as an integer.</param>
    /// <returns>this Line object.</returns>
    public Line SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetStrokeColor(r, g, b);
        return this;
    }

    /// <summary>Sets the stroke color from red, green and blue values between 0.0 and 1.0.</summary>
    public Line SetStrokeColor(float r, float g, float b) {
        this.color = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from an array of red, green and blue values.</summary>
    public Line SetStrokeColor(float[] rgbColor) {
        this.color = rgbColor;
        return this;
    }

    /// <summary>
    /// Sets the line cap style.
    /// </summary>
    /// <param name="style">the cap style of the current line.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE</param>
    /// <returns>this Line object.</returns>
    public Line SetCapStyle(CapStyle style) {
        this.capStyle = style;
        return this;
    }

    /// <summary>
    /// Returns the line cap style.
    /// </summary>
    /// <returns>the cap style.</returns>
    public CapStyle GetCapStyle() {
        return capStyle;
    }

    /// <summary>
    /// Scales this line by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the line.</param>
    /// <returns>this Line object.</returns>
    public Line ScaleBy(double factor) {
        return ScaleBy((float) factor);
    }

    /// <summary>
    /// Sets the alternate description of this line.
    /// </summary>
    /// <param name="altDescription">the alternate description of the line.</param>
    /// <returns>this Line.</returns>
    public Line SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the actual text for this line.
    /// </summary>
    /// <param name="actualText">the actual text for the line.</param>
    /// <returns>this Line.</returns>
    public Line SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>Scales the coordinates of this line by the specified factor.</summary>
    public Line ScaleBy(float factor) {
        this.x1 *= factor;
        this.x2 *= factor;
        this.y1 *= factor;
        this.y2 *= factor;
        return this;
    }

    /// <summary>
    /// Draws this line on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);
        page.SaveGraphicsState();
        page.SetPenColor(color);
        page.SetPenWidth(width);
        page.SetLineCapStyle(capStyle);
        page.SetStrokeDashPattern(pattern);
        page.DrawLine(x1, y1, x2, y2);
        page.RestoreGraphicsState();
        page.AddEMC();

        float xMax = Math.Max(x1, x2);
        float yMax = Math.Max(y1, y2);
        return new float[] {xMax, yMax};
    }
}   // End of Line.cs
}   // End of namespace PDFjet.NET
