/*
 * Series.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// One series of a Chart: its points, the line that connects them when it is
/// drawn, and the marker of the points added by their coordinates. A series is
/// created with Chart.AddSeries; its name is listed in the legend of the chart.
/// See Example_09.
/// </summary>
public class Series {
    internal readonly String name;
    internal readonly List<Point> points = new List<Point>();

    internal float[] strokeColor = null;    // null: the next color of the palette
    internal float strokeWidth = 1f;
    internal String strokeDashPattern = "[] 0";
    internal bool drawPath = false;

    internal Shape shape = Shape.CIRCLE;
    internal float radius = 2f;

    internal Series(String name) {
        this.name = name == null ? "" : name;
    }

    /// <summary>Adds a point with the marker of this series.</summary>
    /// <param name="x">the x value.</param>
    /// <param name="y">the y value.</param>
    /// <returns>this Series object.</returns>
    public Series AddPoint(float x, float y) {
        points.Add(new Point(x, y).SetShape(shape).SetRadius(radius));
        return this;
    }

    /// <summary>
    /// Adds a point with its own marker: its shape, radius and colors.
    /// A point without a stroke color is drawn in the color of the series.
    /// </summary>
    /// <param name="point">the point.</param>
    /// <returns>this Series object.</returns>
    public Series AddPoint(Point point) {
        points.Add(point);
        return this;
    }

    /// <summary>
    /// Sets whether the points are connected with a line, in the order they
    /// were added. The default is false: only the markers are drawn.
    /// </summary>
    /// <param name="drawPath">true to draw the line.</param>
    /// <returns>this Series object.</returns>
    public Series SetDrawPath(bool drawPath) {
        this.drawPath = drawPath;
        return this;
    }

    /// <summary>
    /// Sets the color of the line and of the markers that have no color of
    /// their own. Without it the series has the next color of the palette.
    /// </summary>
    /// <param name="color">the color as a 0xRRGGBB value, for example Color.blue.</param>
    /// <returns>this Series object.</returns>
    public Series SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the width of the line. The default is 1.</summary>
    /// <param name="strokeWidth">the line width.</param>
    /// <returns>this Series object.</returns>
    public Series SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /// <summary>
    /// Sets the dash pattern of the line, for example "[3 3] 0". The default
    /// is a solid line.
    /// </summary>
    /// <param name="strokeDashPattern">the dash pattern.</param>
    /// <returns>this Series object.</returns>
    public Series SetStrokeDashPattern(String strokeDashPattern) {
        this.strokeDashPattern = strokeDashPattern;
        return this;
    }

    /// <summary>
    /// Sets the shape of the marker of the points added by their coordinates.
    /// The default is Shape.CIRCLE; Shape.INVISIBLE draws no markers.
    /// </summary>
    /// <param name="shape">the shape.</param>
    /// <returns>this Series object.</returns>
    public Series SetShape(Shape shape) {
        this.shape = shape;
        return this;
    }

    /// <summary>
    /// Sets the radius of the marker of the points added by their coordinates.
    /// The default is 2.
    /// </summary>
    /// <param name="radius">the radius.</param>
    /// <returns>this Series object.</returns>
    public Series SetRadius(float radius) {
        this.radius = radius;
        return this;
    }
}   // End of Series.cs
}   // End of namespace PDFjet.NET
