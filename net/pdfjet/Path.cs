/**
 * Path.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

/**
 * Used to create path objects.
 * The path objects may consist of lines, splines or both.
 *
 * Please see Example_02.
 */
namespace PDFjet.NET {
public class Path : IDrawable {
    private List<Point> points = null;
    private float x;
    private float y;

    private float[] fillColor;
    private float[] strokeColor;
    private float strokeWidth = 0.6f;   // !! DO NOT REMOVE OR LOWER THIS VALUE !!
    private String strokePattern = "[] 0";
    private CapStyle lineCapStyle = CapStyle.BUTT;
    private JoinStyle lineJoinStyle = JoinStyle.MITER;
    private float rotateDegrees = 0f;

    private String uri = null;
    private String key = null;
    private String language = "en-US";
    private String actualText = null;
    private String altDescription = null;

    private bool closePath = false;

    /**
     * The default constructor.
     */
    public Path() {
        points = new List<Point>();
    }

    /**
     * Adds a point to this path.
     *
     * @param point the point to add.
     */
    public void Add(Point point) {
        points.Add(point);
    }

    public Path SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public Path SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     * Sets the pen color that will be used to draw this path.
     *
     * @param color the color is specified as an integer.
     */
    public void SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetFillColor(r, g, b);
    }

    public void SetFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
    }

    public void SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetStrokeColor(r, g, b);
    }

    public void SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
    }

    public void SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
    }

    /**
     * Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     * @param width the pen width.
     */
    public void SetStrokeWidth(double width) {
        this.strokeWidth = (float) width;
    }

    public void SetStrokeWidth(float width) {
        this.strokeWidth = width;
    }

    /**
     * Sets the line dash pattern for this path.
     *
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
     *
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
     * Sets the line cap style.
     *
     * @param style the cap style of this path.
     * Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
     */
    public void SetLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
    }

    /**
     * Returns the line cap style for this path.
     *
     * @return the line cap style for this path.
     */
    public CapStyle GetLineCapStyle() {
        return this.lineCapStyle;
    }

    /**
     * Sets the line join style.
     *
     * @param style the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
     */
    public void SetLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
    }

    /**
     * Returns the line join style.
     *
     * @return the line join style.
     */
    public JoinStyle GetLineJoinStyle() {
        return this.lineJoinStyle;
    }

    public void SetRotation(double degrees) {
        this.rotateDegrees = (float) degrees;
    }

    public void SetRotationClockwise(double degrees) {
        this.rotateDegrees = (float) -degrees;
    }

    public void SetRotationCounterClockwise(double degrees) {
        this.rotateDegrees = (float) degrees;
    }

    /**
     * Sets the URI for the "click box" action.
     *
     * @param uri the URI
     */
    public void SetURIAction(String uri) {
        this.uri = uri;
    }

    /**
     * Sets the destination key for the action.
     *
     * @param key the destination name.
     */
    public void SetGoToAction(String key) {
        this.key = key;
    }

    public void SetLanguage(String language) {
        this.language = language;
    }

    /**
     * Sets the actual text for this path.
     *
     * @param actualText the actual text for the path.
     * @return this Path.
     */
    public void SetActualText(String actualText) {
        this.actualText = actualText;
    }

    /**
     * Sets the alternate description of this path.
     *
     * @param altDescription the alternate description of the path.
     * @return this Path.
     */
    public void SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
    }

    /**
     * Scales the path using the specified factor.
     *
     * @param factor the specified factor.
     */
    public Path SetScaleFactor(double factor) {
        SetScaleFactor((float) factor);
        return this;
    }

    /**
     * Scales the path using the specified factor.
     *
     * @param factor the specified factor.
     */
    public Path SetScaleFactor(float factor) {
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            point.x *= factor;
            point.y *= factor;
        }
        return this;
    }

    public void SetClosePath() {
        closePath = true;
    }

    public int GetPointCount() {
        return points.Count;
    }

    public Point GetLastPoint() {
        if (points.Count > 0) {
            return points[points.Count - 1];
        } else {
            return null;
        }
    }

    /**
     * Draws this path on the page using the current selected color, pen width, line pattern and line join style.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (closePath == true) {
            this.Add(new Point(this.points[0].x, this.points[0].y));
        }

        foreach (Point point in points) {
            point.x += this.x;
            point.y += this.y;
        }

        float x = float.MaxValue;
        float y = float.MaxValue;
        float xMax = 0f;
        float yMax = 0f;
        foreach (Point point in points) {
            if (point.x < x) { x = point.x; }
            if (point.y < y) { y = point.y; }

            if (point.x > xMax) { xMax = point.x; }
            if (point.y > yMax) { yMax = point.y; }
        }
        float w = xMax - x;
        float h = yMax - y;

        if (actualText != null && altDescription != null) {
            page.AddBMC(StructElem.FIGURE, this.language, this.actualText, this.altDescription);
        }

        page.SaveGraphicsState();

        float centerX = x + w/2;
        float centerY = (page.height - y) - h/2;
        page.RotateAroundCenter(centerX, centerY, rotateDegrees);
        if (strokeColor != null && strokePattern != null) {
            page.SetStrokeDashPattern(strokePattern);
        }
        if (fillColor != null && strokeColor == null) {
            page.SetBrushColor(fillColor);
            page.DrawPath(points, PathOperator.Fill);
        } else if (fillColor == null && strokeColor != null) {
            page.SetPenColor(strokeColor);
            page.SetPenWidth(strokeWidth);
            page.DrawPath(points, PathOperator.Stroke);
        } else if (fillColor != null && strokeColor != null) {
            page.SetBrushColor(fillColor);
            page.SetPenColor(strokeColor);
            page.SetPenWidth(strokeWidth);
            page.DrawPath(points, PathOperator.FillAndStroke);
        }

        page.RestoreGraphicsState();

        if (actualText != null && altDescription != null) {
            page.AddEMC();
        }

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

        return new float[] {xMax, yMax};
    }
}   // End of Path.cs
}   // End of namespace PDFjet.NET
