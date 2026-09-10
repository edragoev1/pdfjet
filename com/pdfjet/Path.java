/*
 * Path.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Used to create path objects.
 * The path objects may consist of lines, splines or both.
 *
 * Please see Example_02.
 */
public class Path implements Drawable {
    private int color = Color.black;
    private float width = 0f;
    private String pattern = "[] 0";
    private boolean fillShape = false;
    private boolean closePath = false;
    private List<Point> points = null;
    private float xBox;
    private float yBox;
    private CapStyle lineCapStyle = CapStyle.BUTT;
    private JoinStyle lineJoinStyle = JoinStyle.MITER;

    /**
     * The default constructor.
     */
    public Path() {
        points = new ArrayList<Point>();
    }

    /**
     * Adds a point to this path.
     *
     * @param point the point to add.
     */
    public void add(Point point) {
        points.add(point);
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
     *  @param pattern the line dash pattern.
     *  @return this Path object.
     */
    public Path setPattern(String pattern) {
        this.pattern = pattern;
        return this;
    }

    /**
     * Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     * @param width the pen width.
     * @return this Path object.
     */
    public Path setWidth(double width) {
        this.width = (float) width;
        return this;
    }

    /**
     * Sets the pen width that will be used to draw the lines and splines that are part of this path.
     *
     * @param width the pen width.
     * @return this Path object.
     */
    public Path setWidth(float width) {
        this.width = width;
        return this;
    }

    /**
     * Sets the pen color that will be used to draw this path.
     *
     * @param color the color is specified as an integer.
     * @return this Path object.
     */
    public Path setColor(int color) {
        this.color = color;
        return this;
    }

    /**
     * Sets the closePath variable.
     *
     * @param closePath if closePath is true a line will be draw between the first and last point of this path.
     * @return this Path object.
     */
    public Path setClosePath(boolean closePath) {
        this.closePath = closePath;
        return this;
    }

    /**
     * Sets the fillShape private variable. If fillShape is true - the shape of the path will be filled with the current brush color.
     *
     * @param fillShape the fillShape flag.
     * @return this Path object.
     */
    public Path setFillShape(boolean fillShape) {
        this.fillShape = fillShape;
        return this;
    }

    /**
     * Sets the line cap style.
     *
     * @param style the cap style of this path.
     * Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
     * @return this Path object.
     */
    public Path setLineCapStyle(CapStyle style) {
        this.lineCapStyle = style;
        return this;
    }

    /**
     * Returns the line cap style for this path.
     *
     * @return the line cap style for this path.
     */
    public CapStyle getLineCapStyle() {
        return this.lineCapStyle;
    }

    /**
     * Sets the line join style.
     *
     * @param style the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
     * @return this Path object.
     */
    public Path setLineJoinStyle(JoinStyle style) {
        this.lineJoinStyle = style;
        return this;
    }

    /**
     * Returns the line join style.
     *
     * @return the line join style.
     */
    public JoinStyle getLineJoinStyle() {
        return this.lineJoinStyle;
    }

    /**
     * Sets the path position.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Path object.
     */
    public Path setPosition(double x, double y) {
        setLocation((float) x, (float) y);
        return this;
    }

    /**
     * Sets the path position.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Path object.
     */
    public Path setPosition(float x, float y) {
        setLocation(x, y);
        return this;
    }

    /**
     * Sets the path location.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return the path.
     */
    public Path setLocation(float x, float y) {
        xBox += x;
        yBox += y;
        return this;
    }

    /**
     * Sets the path location.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return the path.
     */
    public Path setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     *  Scales the path using the specified factor.
     *
     *  @param factor the specified factor.
     */
    public void scaleBy(double factor) {
        scaleBy((float) factor);
    }

    /**
     *  Scales the path using the specified factor.
     *
     *  @param factor the specified factor.
     */
    public void scaleBy(float factor) {
        for (Point point : points) {
            point.x *= factor;
            point.y *= factor;
        }
    }

    /**
     *  Draws this path on the page using the current selected color, pen width, line pattern and line join style.
     *
     *  @param page the page to draw this path on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        for (Point point : points) {
            point.x += xBox;
            point.y += yBox;
        }

        // A path carries no text, so it is decorative content.
        page.addArtifactBMC();
        if (fillShape) {
            page.setBrushColor(color);
            page.drawPath(points, PathOperator.FILL);
        } else {
            page.setPenWidth(width);
            page.setPenColor(color);
            page.setStrokeDashPattern(pattern);
            page.setLineCapStyle(lineCapStyle);
            page.setLineJoinStyle(lineJoinStyle);
            if (closePath) {
                page.drawPath(points, PathOperator.CLOSE_AND_STROKE);
            } else {
                page.drawPath(points, PathOperator.STROKE);
            }
        }
        page.addEMC();

        float xMax = 0f;
        float yMax = 0f;
        for (Point point : points) {
            if (point.x > xMax) { xMax = point.x; }
            if (point.y > yMax) { yMax = point.y; }
            point.x -= xBox;
            point.y -= yBox;
        }

        return new float[] {xMax, yMax};
    }
}   // End of Path.java
