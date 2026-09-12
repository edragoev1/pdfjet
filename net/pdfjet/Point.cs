/*
 * Point.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create point objects with different shapes and draw them on a page.
/// Please note: When we are mentioning (x, y) coordinates of a point - we are talking about the coordinates of the center of the point.
///
/// Please see Example_05.
/// </summary>
public class Point : IDrawable {
    /// <summary>The point is not drawn.</summary>
    public static readonly int INVISIBLE = -1;
    /// <summary>A circle.</summary>
    public static readonly int CIRCLE = 0;
    /// <summary>A diamond.</summary>
    public static readonly int DIAMOND = 1;
    /// <summary>A box.</summary>
    public static readonly int BOX = 2;
    /// <summary>A plus sign.</summary>
    public static readonly int PLUS = 3;
    /// <summary>A horizontal dash.</summary>
    public static readonly int H_DASH = 4;
    /// <summary>A vertical dash.</summary>
    public static readonly int V_DASH = 5;
    /// <summary>A multiplication sign.</summary>
    public static readonly int MULTIPLY = 6;
    /// <summary>A star.</summary>
    public static readonly int STAR = 7;
    /// <summary>An X mark.</summary>
    public static readonly int X_MARK = 8;
    /// <summary>An up arrow.</summary>
    public static readonly int UP_ARROW = 9;
    /// <summary>A down arrow.</summary>
    public static readonly int DOWN_ARROW = 10;
    /// <summary>A left arrow.</summary>
    public static readonly int LEFT_ARROW = 11;
    /// <summary>A right arrow.</summary>
    public static readonly int RIGHT_ARROW = 12;

    // For the c operator we have both control points
    /// <summary>A control point of a curve drawn with the c operator, which uses both control points.</summary>
    public static readonly char ControlPointC = 'c';

    // For the v operator, the first control point shall coincide with initial point of the curve.
    /// <summary>A control point of a curve drawn with the v operator, where the first control point is the start point.</summary>
    public static readonly char ControlPointV = 'v';

    // For the y operator, the second control point shall coincide with final point of the curve.
    /// <summary>A control point of a curve drawn with the y operator, where the second control point is the end point.</summary>
    public static readonly char ControlPointY = 'y';

    internal float x;
    internal float y;
    internal float r = 2f;
    internal int shape = Point.CIRCLE;

    internal float[] fillColor = null;
    internal float strokeWidth = 1f;
    internal float[] strokeColor = null;
    internal string strokeDashPattern = "[] 0";
    internal string pathOperator = PathOperator.CloseAndStroke;

    internal Alignment alignment = Alignment.RIGHT;

    internal char controlPoint = '\0';
    internal bool drawPath = false;

    private String text;
    private int textColor;
    private int textDirection;
    private String uri;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public Point() {
    }

    /// <summary>
    /// Constructor for creating point objects.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    public Point(double x, double y) : this((float) x, (float) y) {
    }

    /// <summary>
    /// Constructor for creating point objects.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    public Point(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Constructor for creating point objects.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    /// <param name="controlPoint">the control point type if this point is one of the points specifying a curve.</param>
    public Point(double x, double y, char controlPoint) : this((float) x, (float) y, controlPoint) {
    }

    /// <summary>
    /// Constructor for creating point objects.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    /// <param name="controlPoint">the control point type if this point is one of the points specifying a curve.</param>
    public Point(float x, float y, char controlPoint) {
        this.x = x;
        this.y = y;
        this.controlPoint = controlPoint;
    }

    /// <summary>
    /// Copy constructor. Creates a deep copy of the specified point,
    /// including all visual properties, URI action, and text attributes.
    /// </summary>
    /// <param name="point">the point to copy.</param>
    public Point(Point point) {
        this.x = point.x;
        this.y = point.y;
        this.r = point.r;
        this.shape = point.shape;
        this.alignment = point.alignment;
        this.fillColor = point.fillColor != null
                ? new float[] {point.fillColor[0], point.fillColor[1], point.fillColor[2]}
                : null;
        this.strokeWidth = point.strokeWidth;
        this.strokeColor = point.strokeColor != null
                ? new float[] {point.strokeColor[0], point.strokeColor[1], point.strokeColor[2]}
                : null;
        this.strokeDashPattern = point.strokeDashPattern;
        this.pathOperator = point.pathOperator;
        this.controlPoint = point.controlPoint;
        this.drawPath = point.drawPath;
        this.text = point.text;
        this.textColor = point.textColor;
        this.textDirection = point.textDirection;
        this.uri = point.uri;
    }

    /// <summary>
    /// Sets the position (x, y) of this point.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    /// <returns>this Point object.</returns>
    public Point SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location (x, y) of this point.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    /// <returns>Point the point.</returns>
    public Point SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>
    /// Sets the x coordinate of this point.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <returns>this Point object.</returns>
    public Point SetX(double x) {
        this.x = (float) x;
        return this;
    }

    /// <summary>
    /// Sets the x coordinate of this point.
    /// </summary>
    /// <param name="x">the x coordinate of this point when drawn on the page.</param>
    /// <returns>this Point object.</returns>
    public Point SetX(float x) {
        this.x = x;
        return this;
    }

    /// <summary>
    /// Returns the x coordinate of this point.
    /// </summary>
    /// <returns>the x coordinate of this point.</returns>
    public float GetX() {
        return x;
    }

    /// <summary>
    /// Sets the y coordinate of this point.
    /// </summary>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    /// <returns>this Point object.</returns>
    public Point SetY(double y) {
        this.y = (float) y;
        return this;
    }

    /// <summary>
    /// Sets the y coordinate of this point.
    /// </summary>
    /// <param name="y">the y coordinate of this point when drawn on the page.</param>
    /// <returns>this Point object.</returns>
    public Point SetY(float y) {
        this.y = y;
        return this;
    }

    /// <summary>
    /// Returns the y coordinate of this point.
    /// </summary>
    /// <returns>the y coordinate of this point.</returns>
    public float GetY() {
        return y;
    }

    /// <summary>
    /// Sets the radius of this point.
    /// </summary>
    /// <param name="r">the radius.</param>
    /// <returns>this Point object.</returns>
    public Point SetRadius(double r) {
        this.r = (float) r;
        return this;
    }

    /// <summary>
    /// Sets the radius of this point.
    /// </summary>
    /// <param name="r">the radius.</param>
    /// <returns>this Point object.</returns>
    public Point SetRadius(float r) {
        this.r = r;
        return this;
    }

    /// <summary>
    /// Returns the radius of this point.
    /// </summary>
    /// <returns>the radius of this point.</returns>
    public float GetRadius() {
        return r;
    }

    /// <summary>Sets the fill color as a 0xRRGGBB value.</summary>
    public Point SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the fill color from red, green and blue values between 0.0 and 1.0.</summary>
    public Point SetFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the fill color from an array of red, green and blue values.</summary>
    public Point SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /// <summary>Sets the width of the lines used to draw this point.</summary>
    public Point SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /// <summary>Returns the fill color.</summary>
    public float[] GetFillColor() {
        return this.fillColor;
    }

    /// <summary>Sets the stroke color as a 0xRRGGBB value.</summary>
    public Point SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from red, green and blue values between 0.0 and 1.0.</summary>
    public Point SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from an array of red, green and blue values.</summary>
    public Point SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    /// <summary>Returns the stroke color.</summary>
    public float[] GetStrokeColor() {
        return this.strokeColor;
    }

    /// <summary>
    /// Sets the shape of this point.
    /// </summary>
    /// <param name="shape">the shape of this point. Supported values:
    /// <code>
    /// Point.INVISIBLE
    /// Point.CIRCLE
    /// Point.DIAMOND
    /// Point.BOX
    /// Point.PLUS
    /// Point.H_DASH
    /// Point.V_DASH
    /// Point.MULTIPLY
    /// Point.STAR
    /// Point.X_MARK
    /// Point.UP_ARROW
    /// Point.DOWN_ARROW
    /// Point.LEFT_ARROW
    /// Point.RIGHT_ARROW
    /// </code></param>
    /// <returns>this Point object.</returns>
    public Point SetShape(int shape) {
        this.shape = shape;
        return this;
    }

    /// <summary>
    /// Returns the point shape code value.
    /// </summary>
    /// <returns>the shape code value.</returns>
    public int GetShape() {
        return shape;
    }

    /// <summary>
    /// Sets the width of the lines of this point.
    /// </summary>
    /// <param name="width">the line width.</param>
    /// <returns>this Point object.</returns>
    public Point SetStrokeWidth(double width) {
        this.strokeWidth = (float) width;
        return this;
    }

    /// <summary>
    /// Returns the width of the lines used to draw this point.
    /// </summary>
    /// <returns>the width of the lines used to draw this point.</returns>
    public float GetStrokeWidth() {
        return strokeWidth;
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
    /// <param name="strokeDashPattern">the stroke dash pattern.</param>
    /// <returns>this Point object.</returns>
    public Point SetStrokeDashPattern(String strokeDashPattern) {
        this.strokeDashPattern = strokeDashPattern;
        return this;
    }

    /// <summary>
    /// Returns the dash pattern.
    /// </summary>
    /// <returns>the dash pattern.</returns>
    public String GetStrokeDashPattern() {
        return strokeDashPattern;
    }

    /// <summary>Sets the path operator used to draw this point, for example PathOperator.Stroke.</summary>
    public Point SetPathOperator(string pathOperator) {
        this.pathOperator = pathOperator;
        return this;
    }

    /// <summary>Returns the path operator used to draw this point.</summary>
    public string GetPathOperator() {
        return this.pathOperator;
    }

    /// <summary>
    /// Sets this point as the start of a path that will be drawn on the chart.
    /// </summary>
    /// <returns>the point.</returns>
    public Point SetDrawPath() {
        this.drawPath = true;
        return this;
    }

    /// <summary>
    /// Sets the URI for the "click point" action.
    /// </summary>
    /// <param name="uri">the URI</param>
    /// <returns>this Point object.</returns>
    public Point SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Returns the URI for the "click point" action.
    /// </summary>
    /// <returns>the URI for the "click point" action.</returns>
    public String GetURIAction() {
        return uri;
    }

    /// <summary>
    /// Sets the point text.
    /// </summary>
    /// <param name="text">the text.</param>
    /// <returns>this Point object.</returns>
    public Point SetText(String text) {
        this.text = text;
        return this;
    }

    /// <summary>
    /// Returns the text associated with this point.
    /// </summary>
    /// <returns>the text.</returns>
    public String GetText() {
        return text;
    }

    /// <summary>
    /// Sets the point's text color.
    /// </summary>
    /// <param name="textColor">the text color.</param>
    /// <returns>this Point object.</returns>
    public Point SetTextColor(int textColor) {
        this.textColor = textColor;
        return this;
    }

    /// <summary>
    /// Returns the point's text color.
    /// </summary>
    /// <returns>the text color.</returns>
    public int GetTextColor() {
        return this.textColor;
    }

    /// <summary>
    /// Sets the point's text direction.
    /// </summary>
    /// <param name="textDirection">the text direction.</param>
    /// <returns>this Point object.</returns>
    public Point SetTextDirection(int textDirection) {
        this.textDirection = textDirection;
        return this;
    }

    /// <summary>
    /// Returns the point's text direction.
    /// </summary>
    /// <returns>the text direction.</returns>
    public int GetTextDirection() {
        return this.textDirection;
    }

    /// <summary>
    /// Sets the point alignment.
    /// </summary>
    /// <param name="alignment">the alignment value.</param>
    /// <returns>this Point object.</returns>
    public Point SetAlignment(Alignment alignment) {
        this.alignment = alignment;
        return this;
    }

    /// <summary>
    /// Returns the point alignment.
    /// </summary>
    /// <returns>Alignment the alignment value.</returns>
    public Alignment GetAlignment() {
        return this.alignment;
    }

    /// <summary>
    /// Draws this point on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        page.SaveGraphicsState();
        if (fillColor != null && strokeColor != null) {
            page.SetBrushColor(fillColor);
            page.SetPenColor(strokeColor);
            page.SetPenWidth(strokeWidth);
            this.pathOperator = PathOperator.FillAndStroke;
        } else if (fillColor != null && strokeColor == null) {
            page.SetBrushColor(fillColor);
            this.pathOperator = PathOperator.Fill;
        } else if (fillColor == null && strokeColor != null) {
            page.SetPenColor(strokeColor);
            page.SetPenWidth(strokeWidth);
            this.pathOperator = PathOperator.CloseAndStroke;
        }
        page.DrawPoint(this);
        page.RestoreGraphicsState();

        return new float[] {x + r, y + r};
    }
}   // End of Point.cs
}   // End of namespace PDFjet.NET
