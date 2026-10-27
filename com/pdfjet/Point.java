/*
 * Point.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A point with a marker: a shape drawn around its (x, y) coordinates, which
 * are the center of the marker. A point is drawn on a page on its own, as the
 * marker of a table cell, and a list of points is a
 * path for Page.drawPath.
 *
 * Please see Example_05.
 */
public class Point implements Drawable {
    /** A control point of a curve drawn with the c operator, which uses both control points. */
    public static final char CONTROL_POINT_C = 'c';

    /** A control point of a curve drawn with the v operator, where the first control point is the start point. */
    public static final char CONTROL_POINT_V = 'v';

    /** A control point of a curve drawn with the y operator, where the second control point is the end point. */
    public static final char CONTROL_POINT_Y = 'y';

    float x;
    float y;
    float r = 2f;
    Shape shape = Shape.CIRCLE;

    float[] fillColor = null;
    float strokeWidth = 1f;
    float[] strokeColor = null;
    PathOperator pathOperator = PathOperator.CLOSE_AND_STROKE;

    char controlPoint = '\0';
    private String uri;
    private String altDescription;

    /**
     *  The default constructor.
     */
    public Point() {
    }

    /**
     *  Copy constructor. Creates a copy of the specified point, including its
     *  marker and its URI action.
     *
     *  @param point the point to copy.
     */
    public Point(Point point) {
        this.x = point.x;
        this.y = point.y;
        this.r = point.r;
        this.shape = point.shape;
        this.fillColor = Util.copyOf(point.fillColor);
        this.strokeWidth = point.strokeWidth;
        this.strokeColor = Util.copyOf(point.strokeColor);
        this.pathOperator = point.pathOperator;
        this.controlPoint = point.controlPoint;
        this.uri = point.uri;
        this.altDescription = point.altDescription;
    }

    /**
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public Point(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     *  @param controlPoint the type of control point specifying.
     */
    public Point(float x, float y, char controlPoint) {
        this.x = x;
        this.y = y;
        this.controlPoint = controlPoint;
    }

    /**
     *  Sets the location (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     *  @return the location of the point.
     */
    public Point setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     *  Sets the x coordinate of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @return this Point object.
     */
    public Point setX(float x) {
        this.x = x;
        return this;
    }

    /**
     *  Returns the x coordinate of this point.
     *
     *  @return the x coordinate of this point.
     */
    public float getX() {
        return x;
    }

    /**
     *  Sets the y coordinate of this point.
     *
     *  @param y the y coordinate of this point when drawn on the page.
     *  @return this Point object.
     */
    public Point setY(float y) {
        this.y = y;
        return this;
    }

    /**
     *  Returns the y coordinate of this point.
     *
     *  @return the y coordinate of this point.
     */
    public float getY() {
        return y;
    }

    /**
     *  Sets the radius of this point.
     *
     *  @param r the radius.
     *  @return this Point object.
     */
    public Point setRadius(float r) {
        this.r = r;
        return this;
    }

    /**
     *  Returns the radius of this point.
     *
     *  @return the radius of this point.
     */
    public float getRadius() {
        return r;
    }

    /**
     * Sets the fill color of this point.
     * Color.transparent leaves it unchanged.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Point object.
     */
    public Point setFillColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the fill color of this point.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Point object.
     */
    public Point setFillColor(float[] rgbColor) {
        this.fillColor = Util.copyOf(rgbColor);
        return this;
    }

    /**
     * Returns the fill color of this point.
     *
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] getFillColor() {
        return Util.copyOf(this.fillColor);
    }

    /**
     * Sets the stroke color of this point.
     * Color.transparent leaves it unchanged.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Point object.
     */
    public Point setStrokeColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke color of this point.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Point object.
     */
    public Point setStrokeColor(float[] rgbColor) {
        this.strokeColor = Util.copyOf(rgbColor);
        return this;
    }

    /**
     * Returns the stroke color of this point.
     *
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] getStrokeColor() {
        return Util.copyOf(this.strokeColor);
    }

    /**
     *  Sets the shape of the marker drawn at this point.
     *
     *  @param shape the shape, for example Shape.CIRCLE; Shape.INVISIBLE draws no marker.
     *  @return this Point object.
     */
    public Point setShape(Shape shape) {
        this.shape = shape;
        return this;
    }

    /**
     *  Returns the shape of the marker drawn at this point.
     *
     *  @return the shape.
     */
    public Shape getShape() {
        return shape;
    }

    /**
     *  Sets the width of the lines of this point.
     *
     *  @param strokeWidth the line width.
     *  @return this Point object.
     */
    public Point setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /**
     *  Returns the width of the lines used to draw this point.
     *
     *  @return the width of the lines used to draw this point.
     */
    public float getStrokeWidth() {
        return strokeWidth;
    }

    /** Returns the path operator the marker is painted with. */
    PathOperator getPathOperator() {
        return this.pathOperator;
    }

    /**
     *  Sets the URI of the link opened by a click on this point.
     *
     *  @param uri the URI.
     *  @return this Point object.
     */
    public Point setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     *  Sets what a point that is a link stands for, like the country a point
     *  of a chart is, for a screen reader: in a tagged document, PDF/UA or a
     *  PDF/A of level A, a point of a chart that is a link is a figure of its
     *  own in the Link, described by it. Without one it is described by its
     *  URI.
     *
     *  @param altDescription what the point stands for.
     *  @return this Point object.
     */
    public Point setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     *  Returns what the point stands for.
     *
     *  @return the description, or null.
     */
    public String getAltDescription() {
        return altDescription;
    }

    /**
     *  Returns the URI of the link opened by a click on this point.
     *
     *  @return the URI, or null.
     */
    public String getURIAction() {
        return uri;
    }

    /**
     *  Draws this point on the specified page.
     *
     *  @param page the page to draw this point on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        if (page == null) {
            return new float[] {x + r, y + r};
        }

        // A point is a marker and carries no text, so it is decorative content;
        // a point that is a link is a figure of its own in the Link, described
        // by what it stands for or by its URI, as a point of a chart is. Its
        // link was not made (review of 12 October 2026, F15).
        StructElement link = null;
        String description = altDescription;
        if (uri != null && !uri.isEmpty()) {
            if (description == null || description.isEmpty()) {
                description = uri;
            }
            link = page.beginLink();
            page.addBDC(StructElem.FIGURE, null, description);
        } else {
            page.addArtifactBMC();
        }
        page.saveGraphicsState();
        if (fillColor != null && strokeColor != null) {
            page.setBrushColor(fillColor);
            page.setPenColor(strokeColor);
            page.setPenWidth(strokeWidth);
            this.pathOperator = PathOperator.FILL_AND_STROKE;
        } else if (fillColor != null && strokeColor == null) {
            page.setBrushColor(fillColor);
            this.pathOperator = PathOperator.FILL;
        } else if (fillColor == null && strokeColor != null) {
            page.setPenColor(strokeColor);
            page.setPenWidth(strokeWidth);
            this.pathOperator = PathOperator.CLOSE_AND_STROKE;
        }
        if (fillColor != null && strokeColor == null && isLineShape(shape)) {
            // A marker of lines is stroked in the point's color: it was drawn
            // in the pen's (review of 12 October 2026, F15)
            page.setPenColor(fillColor);
            page.setPenWidth(strokeWidth);
        }
        page.drawPoint(this);
        page.restoreGraphicsState();
        if (uri != null && !uri.isEmpty()) {
            page.setFigureBoundingBox(x - r, y - r, 2*r, 2*r);
        }
        page.addEMC();
        if (uri != null && !uri.isEmpty()) {
            page.endLink(link);
            Annotation annotation = new Annotation(
                    Annotation.Link, x - r, y - r, x + r, y + r,
                    null, null, 0f, null, null, uri, null, null, null, description);
            annotation.linkElement = link;
            page.addAnnotation(annotation);
        }

        return new float[] {x + r, y + r};
    }

    // Returns true for a marker drawn of lines alone, which has nothing to fill.
    private static boolean isLineShape(Shape shape) {
        return shape == Shape.PLUS || shape == Shape.HORIZONTAL_DASH || shape == Shape.VERTICAL_DASH
                || shape == Shape.X_MARK || shape == Shape.MULTIPLY;
    }
}   // End of Point.java
