/*
 * Series.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * One series of a Chart: its points, the line that connects them when it is
 * drawn, and the marker of the points added by their coordinates. A series is
 * created with Chart.addSeries; its name is listed in the legend of the chart.
 * See Example_09.
 */
public class Series {
    final String name;
    final List<Point> points = new ArrayList<Point>();
    // True for a point added by its coordinates, drawn with the marker of the series
    final List<Boolean> seriesMarker = new ArrayList<Boolean>();

    float[] strokeColor = null;     // null: the next color of the palette
    float strokeWidth = 1f;
    String strokeDashPattern = "[] 0";
    boolean drawPath = false;

    Shape shape = Shape.CIRCLE;
    float radius = 2f;

    Series(String name) {
        this.name = name == null ? "" : name;
    }

    /**
     * Adds a point with the marker of this series, the one it has when the
     * chart is drawn.
     *
     * @param x the x value.
     * @param y the y value.
     * @return this Series object.
     */
    public Series addPoint(float x, float y) {
        points.add(new Point(x, y));
        seriesMarker.add(true);
        return this;
    }

    /**
     * Adds a point with its own marker: its shape, radius and colors.
     * A point without a stroke color is drawn in the color of the series.
     *
     * @param point the point.
     * @return this Series object.
     */
    public Series addPoint(Point point) {
        points.add(point);
        seriesMarker.add(false);
        return this;
    }

    /**
     * Sets whether the points are connected with a line, in the order they
     * were added. The default is false: only the markers are drawn.
     *
     * @param drawPath true to draw the line.
     * @return this Series object.
     */
    public Series setDrawPath(boolean drawPath) {
        this.drawPath = drawPath;
        return this;
    }

    /**
     * Sets the color of the line and of the markers that have no color of
     * their own. Without it the series has the next color of the palette.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Series object.
     */
    public Series setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the width of the line. The default is 1.
     *
     * @param strokeWidth the line width.
     * @return this Series object.
     */
    public Series setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /**
     * Sets the dash pattern of the line, for example "[3 3] 0". The default
     * is a solid line.
     *
     * @param strokeDashPattern the dash pattern.
     * @return this Series object.
     */
    public Series setStrokeDashPattern(String strokeDashPattern) {
        this.strokeDashPattern = strokeDashPattern;
        return this;
    }

    /**
     * Sets the shape of the marker of the points added by their coordinates.
     * The default is Shape.CIRCLE; Shape.INVISIBLE draws no markers.
     *
     * @param shape the shape.
     * @return this Series object.
     */
    public Series setShape(Shape shape) {
        this.shape = shape;
        return this;
    }

    /**
     * Sets the radius of the marker of the points added by their coordinates.
     * The default is 2.
     *
     * @param radius the radius.
     * @return this Series object.
     */
    public Series setRadius(float radius) {
        this.radius = radius;
        return this;
    }
}   // End of Series.java
