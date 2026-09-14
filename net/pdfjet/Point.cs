/*
 * Point.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// A point with a marker: a shape drawn around its (x, y) coordinates, which
/// are the center of the marker. A point is drawn on a page on its own, as the
/// marker of a table cell, or in a chart Series, and a list of points is a
/// path for Page.DrawPath.
///
/// Please see Example_05.
/// </summary>
public class Point : IDrawable {
    /// <summary>A control point of a curve drawn with the c operator, which uses both control points.</summary>
    public static readonly char CONTROL_POINT_C = 'c';

    /// <summary>A control point of a curve drawn with the v operator, where the first control point is the start point.</summary>
    public static readonly char CONTROL_POINT_V = 'v';

    /// <summary>A control point of a curve drawn with the y operator, where the second control point is the end point.</summary>
    public static readonly char CONTROL_POINT_Y = 'y';

    internal float x;
    internal float y;
    internal float r = 2f;
    internal Shape shape = Shape.CIRCLE;

    internal float[] fillColor = null;
    internal float strokeWidth = 1f;
    internal float[] strokeColor = null;
    internal PathOperator pathOperator = PathOperator.CLOSE_AND_STROKE;

    internal char controlPoint = '\0';
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
    public Point(float x, float y, char controlPoint) {
        this.x = x;
        this.y = y;
        this.controlPoint = controlPoint;
    }

    /// <summary>
    /// Copy constructor. Creates a copy of the specified point, including its
    /// marker and its URI action.
    /// </summary>
    /// <param name="point">the point to copy.</param>
    public Point(Point point) {
        this.x = point.x;
        this.y = point.y;
        this.r = point.r;
        this.shape = point.shape;
        this.fillColor = Util.CopyOf(point.fillColor);
        this.strokeWidth = point.strokeWidth;
        this.strokeColor = Util.CopyOf(point.strokeColor);
        this.pathOperator = point.pathOperator;
        this.controlPoint = point.controlPoint;
        this.uri = point.uri;
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

    /// <summary>Sets the fill color from an array of red, green and blue values.</summary>
    public Point SetFillColor(float[] rgbColor) {
        this.fillColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>Returns the fill color.</summary>
    public float[] GetFillColor() {
        return Util.CopyOf(this.fillColor);
    }

    /// <summary>Sets the stroke color as a 0xRRGGBB value.</summary>
    public Point SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from an array of red, green and blue values.</summary>
    public Point SetStrokeColor(float[] rgbColor) {
        this.strokeColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>Returns the stroke color.</summary>
    public float[] GetStrokeColor() {
        return Util.CopyOf(this.strokeColor);
    }

    /// <summary>Sets the shape of the marker drawn at this point, for example Shape.CIRCLE; Shape.INVISIBLE draws no marker.</summary>
    public Point SetShape(Shape shape) {
        this.shape = shape;
        return this;
    }

    /// <summary>Returns the shape of the marker drawn at this point.</summary>
    public Shape GetShape() {
        return shape;
    }

    /// <summary>Sets the width of the lines used to draw this point.</summary>
    public Point SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /// <summary>
    /// Returns the width of the lines used to draw this point.
    /// </summary>
    /// <returns>the width of the lines used to draw this point.</returns>
    public float GetStrokeWidth() {
        return strokeWidth;
    }

    /// <summary>Returns the path operator the marker is painted with.</summary>
    internal PathOperator GetPathOperator() {
        return this.pathOperator;
    }

    /// <summary>
    /// Sets the URI of the link opened by a click on this point.
    /// </summary>
    /// <param name="uri">the URI.</param>
    /// <returns>this Point object.</returns>
    public Point SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Returns the URI of the link opened by a click on this point.
    /// </summary>
    /// <returns>the URI, or null.</returns>
    public String GetURIAction() {
        return uri;
    }

    /// <summary>
    /// Draws this point on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + r, y + r};
        }

        page.SaveGraphicsState();
        if (fillColor != null && strokeColor != null) {
            page.SetBrushColor(fillColor);
            page.SetPenColor(strokeColor);
            page.SetPenWidth(strokeWidth);
            this.pathOperator = PathOperator.FILL_AND_STROKE;
        } else if (fillColor != null && strokeColor == null) {
            page.SetBrushColor(fillColor);
            this.pathOperator = PathOperator.FILL;
        } else if (fillColor == null && strokeColor != null) {
            page.SetPenColor(strokeColor);
            page.SetPenWidth(strokeWidth);
            this.pathOperator = PathOperator.CLOSE_AND_STROKE;
        }
        page.DrawPoint(this);
        page.RestoreGraphicsState();

        return new float[] {x + r, y + r};
    }
}   // End of Point.cs
}   // End of namespace PDFjet.NET
