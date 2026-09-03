/**
 * Point.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to create point objects with different shapes and draw them on a page.
 * Please note: When we are mentioning (x, y) coordinates of a point - we are talking about the coordinates of the center of the point.
 *
 * Please see Example_05.
 */
namespace PDFjet.NET {
public class Point : IDrawable {
    public static readonly int INVISIBLE = -1;
    public static readonly int CIRCLE = 0;
    public static readonly int DIAMOND = 1;
    public static readonly int BOX = 2;
    public static readonly int PLUS = 3;
    public static readonly int H_DASH = 4;
    public static readonly int V_DASH = 5;
    public static readonly int MULTIPLY = 6;
    public static readonly int STAR = 7;
    public static readonly int X_MARK = 8;
    public static readonly int UP_ARROW = 9;
    public static readonly int DOWN_ARROW = 10;
    public static readonly int LEFT_ARROW = 11;
    public static readonly int RIGHT_ARROW = 12;

    // For the c operator we have both control points
    public static readonly char ControlPointC = 'c';

    // For the v operator, the first control point shall coincide with initial point of the curve.
    public static readonly char ControlPointV = 'v';

    // For the y operator, the second control point shall coincide with final point of the curve.
    public static readonly char ControlPointY = 'y';

    internal float x;
    internal float y;
    internal float r = 2f;
    internal int shape = Point.CIRCLE;

    internal float[] fillColor = null;
    internal float strokeWidth = 1f;
    internal float[] strokeColor = null;
    internal string strokePattern = "[] 0";
    internal string pathOperator = PathOperator.CloseAndStroke;

    internal Alignment alignment = Alignment.RIGHT;

    internal char controlPoint = '\0';
    internal bool drawPath = false;

    private String text;
    private int textColor;
    private int textDirection;
    private String uri;

    /**
     * The default constructor.
     */
    public Point() {
    }

    /**
     * Constructor for creating point objects.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     */
    public Point(double x, double y) : this((float) x, (float) y) {
    }

    /**
     * Constructor for creating point objects.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     */
    public Point(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     * Constructor for creating point objects.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     * @param isControlPoint true if this point is one of the points specifying a curve.
     */
    public Point(double x, double y, char controlPoint) : this((float) x, (float) y, controlPoint) {
    }

    /**
     * Constructor for creating point objects.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     * @param isControlPoint true if this point is one of the points specifying a curve.
     */
    public Point(float x, float y, char controlPoint) {
        this.x = x;
        this.y = y;
        this.controlPoint = controlPoint;
    }

    /**
     * Copy constructor. Creates a deep copy of the specified point,
     * including all visual properties, URI action, and text attributes.
     *
     * @param point the point to copy.
     */
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
        this.strokePattern = point.strokePattern;
        this.pathOperator = point.pathOperator;
        this.controlPoint = point.controlPoint;
        this.drawPath = point.drawPath;
        this.text = point.text;
        this.textColor = point.textColor;
        this.textDirection = point.textDirection;
        this.uri = point.uri;
    }

    /**
     * Sets the position (x, y) of this point.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     * Sets the position (x, y) of this point.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }

    /**
     * Sets the location (x, y) of this point.
     *
     * @param x the x coordinate of this point when drawn on the page.
     * @param y the y coordinate of this point when drawn on the page.
     * @return Point the point.
     */
    public Point SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the x coordinate of this point.
     *
     * @param x the x coordinate of this point when drawn on the page.
     */
    public void SetX(double x) {
        this.x = (float) x;
    }

    /**
     * Sets the x coordinate of this point.
     *
     * @param x the x coordinate of this point when drawn on the page.
     */
    public void SetX(float x) {
        this.x = x;
    }

    /**
     * Returns the x coordinate of this point.
     *
     * @return the x coordinate of this point.
     */
    public float GetX() {
        return x;
    }

    /**
     * Sets the y coordinate of this point.
     *
     * @param y the y coordinate of this point when drawn on the page.
     */
    public void SetY(double y) {
        this.y = (float) y;
    }

    /**
     * Sets the y coordinate of this point.
     *
     * @param y the y coordinate of this point when drawn on the page.
     */
    public void SetY(float y) {
        this.y = y;
    }

    /**
     * Returns the y coordinate of this point.
     *
     * @return the y coordinate of this point.
     */
    public float GetY() {
        return y;
    }

    /**
     * Sets the radius of this point.
     *
     * @param r the radius.
     */
    public void SetRadius(double r) {
        this.r = (float) r;
    }

    /**
     * Sets the radius of this point.
     *
     * @param r the radius.
     */
    public void SetRadius(float r) {
        this.r = r;
    }

    /**
     * Returns the radius of this point.
     *
     * @return the radius of this point.
     */
    public float GetRadius() {
        return r;
    }

    public void SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void SetFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
    }

    public void SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
    }

    public float[] GetFillColor() {
        return this.fillColor;
    }

    public Point SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public Point SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public Point SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    public float[] GetStrokeColor() {
        return this.strokeColor;
    }

    /**
     * Sets the shape of this point.
     *
     * @param shape the shape of this point. Supported values:
     * <pre>
     * Point.INVISIBLE
     * Point.CIRCLE
     * Point.DIAMOND
     * Point.BOX
     * Point.PLUS
     * Point.H_DASH
     * Point.V_DASH
     * Point.MULTIPLY
     * Point.STAR
     * Point.X_MARK
     * Point.UP_ARROW
     * Point.DOWN_ARROW
     * Point.LEFT_ARROW
     * Point.RIGHT_ARROW
     * </pre>
     */
    public void SetShape(int shape) {
        this.shape = shape;
    }

    /**
     * Returns the point shape code value.
     *
     * @return the shape code value.
     */
    public int GetShape() {
        return shape;
    }

    /**
     * Sets the width of the lines of this point.
     *
     * @param width the line width.
     */
    public void SetStrokeWidth(double width) {
        this.strokeWidth = (float) width;
    }

    /**
     * Returns the width of the lines used to draw this point.
     *
     * @return the width of the lines used to draw this point.
     */
    public float GetStrokeWidth() {
        return strokeWidth;
    }

    /**
     * The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
     * It is specified by a dash array and a dash phase.
     * The elements of the dash array are positive numbers that specify the lengths of
     * alternating dashes and gaps.
     * The dash phase specifies the distance into the dash pattern at which to start the dash.
     * The elements of both the dash array and the dash phase are expressed in user space units.
     * <pre>
     * Examples of line dash patterns:
     *
     *     "[Array] Phase"     Appearance          Description
     *     _______________     _________________   ____________________________________
     *     "[] 0"              -----------------   Solid line
     *     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     * </pre>
     *
     * @param pattern the line dash pattern.
     */
    public void SetStrokePattern(String pattern) {
        this.strokePattern = pattern;
    }

    /**
     * Returns the dash pattern.
     *
     * @return the dash pattern.
     */
    public String GetStrokePattern() {
        return strokePattern;
    }

    public void SetPathOperator(string pathOperator) {
        this.pathOperator = pathOperator;
    }

    public string GetPathOperator() {
        return this.pathOperator;
    }

    /**
     * Sets this point as the start of a path that will be drawn on the chart.
     *
     * @return the point.
     */
    public Point SetStartOfPath() {
        this.drawPath = true;
        return this;
    }

    /**
     * Sets this point as the start of a path that will be drawn on the chart.
     *
     * @return the point.
     */
    public Point SetDrawPath() {
        this.drawPath = true;
        return this;
    }

    /**
     * Sets the URI for the "click point" action.
     *
     * @param uri the URI
     */
    public void SetURIAction(String uri) {
        this.uri = uri;
    }

    /**
     * Returns the URI for the "click point" action.
     *
     * @return the URI for the "click point" action.
     */
    public String GetURIAction() {
        return uri;
    }

    /**
     * Sets the point text.
     *
     * @param text the text.
     */
    public void SetText(String text) {
        this.text = text;
    }

    /**
     * Returns the text associated with this point.
     *
     * @return the text.
     */
    public String GetText() {
        return text;
    }

    /**
     * Sets the point's text color.
     *
     * @param textColor the text color.
     */
    public void SetTextColor(int textColor) {
        this.textColor = textColor;
    }

    /**
     * Returns the point's text color.
     *
     * @return the text color.
     */
    public int GetTextColor() {
        return this.textColor;
    }

    /**
     * Sets the point's text direction.
     *
     * @param textDirection the text direction.
     */
    public void SetTextDirection(int textDirection) {
        this.textDirection = textDirection;
    }

    /**
     * Returns the point's text direction.
     *
     * @return the text direction.
     */
    public int GetTextDirection() {
        return this.textDirection;
    }

    /**
     * Sets the point alignment.
     *
     * @param align the alignment value.
     */
    public void SetAlignment(Alignment alignment) {
        this.alignment = alignment;
    }

    /**
     * Returns the point alignment.
     *
     * @return Alignment the alignment value.
     */
    public Alignment GetAlignment() {
        return this.alignment;
    }

    /**
     * Draws this point on the specified page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
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
