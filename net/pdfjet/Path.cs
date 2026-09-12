/*
 * Path.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create path objects.
/// The path objects may consist of lines, splines or both.
///
/// Please see Example_02.
/// </summary>
public class Path : IDrawable {
    private int color = Color.black;
    private float width = 0f;
    private String pattern = "[] 0";
    private bool fillShape = false;
    private bool closePath = false;
    private List<Point> points = null;
    private float xBox;
    private float yBox;
    private CapStyle lineCapStyle = CapStyle.BUTT;
    private JoinStyle lineJoinStyle = JoinStyle.MITER;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public Path() {
        points = new List<Point>();
    }

    /// <summary>
    /// Adds a point to this path.
    /// </summary>
    /// <param name="point">the point to add.</param>
    /// <returns>this Path object.</returns>
    public Path Add(Point point) {
        points.Add(point);
        return this;
    }

    /// <summary>
    /// Sets the line dash pattern for this path.
    ///
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
    /// <returns>this Path object.</returns>
    public Path SetPattern(String pattern) {
        this.pattern = pattern;
        return this;
    }

    /// <summary>
    /// Sets the stroke width that will be used to draw the lines and splines that are part of this path.
    /// </summary>
    /// <param name="width">the stroke width.</param>
    /// <returns>this Path object.</returns>
    public Path SetStrokeWidth(double width) {
        this.width = (float) width;
        return this;
    }

    /// <summary>
    /// Sets the stroke width that will be used to draw the lines and splines that are part of this path.
    /// </summary>
    /// <param name="width">the stroke width.</param>
    /// <returns>this Path object.</returns>
    public Path SetStrokeWidth(float width) {
        this.width = width;
        return this;
    }

    /// <summary>
    /// Sets the stroke color that will be used to draw this path.
    /// </summary>
    /// <param name="color">the color is specified as an integer.</param>
    /// <returns>this Path object.</returns>
    public Path SetStrokeColor(int color) {
        this.color = color;
        return this;
    }

    /// <summary>
    /// Sets the closePath variable.
    /// </summary>
    /// <param name="closePath">if closePath is true a line will be draw between the first and last point of this path.</param>
    /// <returns>this Path object.</returns>
    public Path SetClosePath(bool closePath) {
        this.closePath = closePath;
        return this;
    }

    /// <summary>
    /// Sets the fillShape private variable. If fillShape is true - the shape of the path will be filled with the current brush color.
    /// </summary>
    /// <param name="fillShape">the fillShape flag.</param>
    /// <returns>this Path object.</returns>
    public Path SetFillShape(bool fillShape) {
        this.fillShape = fillShape;
        return this;
    }

    /// <summary>
    /// Sets the line cap style.
    /// </summary>
    /// <param name="style">the cap style of this path.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE</param>
    /// <returns>this Path object.</returns>
    public Path SetLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
        return this;
    }

    /// <summary>
    /// Returns the line cap style for this path.
    /// </summary>
    /// <returns>the line cap style for this path.</returns>
    public CapStyle GetLineCapStyle() {
        return this.lineCapStyle;
    }

    /// <summary>
    /// Sets the line join style.
    /// </summary>
    /// <param name="style">the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL</param>
    /// <returns>this Path object.</returns>
    public Path SetLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
        return this;
    }

    /// <summary>
    /// Returns the line join style.
    /// </summary>
    /// <returns>the line join style.</returns>
    public JoinStyle GetLineJoinStyle() {
        return this.lineJoinStyle;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the path location.
    /// </summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>the path.</returns>
    public Path SetLocation(float x, float y) {
        xBox += x;
        yBox += y;
        return this;
    }

    /// <summary>
    /// Sets the path location.
    /// </summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>the path.</returns>
    public Path SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    ///  Scales the path using the specified factor.
    /// </summary>
    /// <param name="factor">the specified factor.</param>
    public void ScaleBy(double factor) {
        ScaleBy((float) factor);
    }

    /// <summary>
    ///  Scales the path using the specified factor.
    /// </summary>
    /// <param name="factor">the specified factor.</param>
    public void ScaleBy(float factor) {
        foreach (Point point in points) {
            point.x *= factor;
            point.y *= factor;
        }
    }

    /// <summary>
    ///  Draws this path on the page using the current selected color, pen width, line pattern and line join style.
    /// </summary>
    /// <param name="page">the page to draw this path on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        foreach (Point point in points) {
            point.x += xBox;
            point.y += yBox;
        }

        // A path carries no text, so it is decorative content.
        page.AddArtifactBMC();
        if (fillShape) {
            page.SetBrushColor(color);
            page.DrawPath(points, PathOperator.Fill);
        } else {
            page.SetPenWidth(width);
            page.SetPenColor(color);
            page.SetStrokeDashPattern(pattern);
            page.SetLineCapStyle(lineCapStyle);
            page.SetLineJoinStyle(lineJoinStyle);
            if (closePath) {
                page.DrawPath(points, PathOperator.CloseAndStroke);
            } else {
                page.DrawPath(points, PathOperator.Stroke);
            }
        }
        page.AddEMC();

        float xMax = 0f;
        float yMax = 0f;
        foreach (Point point in points) {
            if (point.x > xMax) { xMax = point.x; }
            if (point.y > yMax) { yMax = point.y; }
            point.x -= xBox;
            point.y -= yBox;
        }

        return new float[] {xMax, yMax};
    }
}
}   // End of Path.cs
