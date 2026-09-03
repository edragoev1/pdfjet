/**
 * Point.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 *  Used to create point objects with different shapes and draw them on a page.
 *  Please note: When we are mentioning (x, y) coordinates of a point - we are talking about the coordinates of the center of the point.
 *
 *  Please see Example_05.
 */
public class Point implements Drawable {
    /** INVISIBLE point */
    public static final int INVISIBLE = -1;

    /** CIRCLE shaped point */
    public static final int CIRCLE = 0;

    /** DIAMOND shaped point */
    public static final int DIAMOND = 1;

    /** BOX shaped point */
    public static final int BOX = 2;

    /** PLUS shaped point */
    public static final int PLUS = 3;

    /** H_DASH shaped point */
    public static final int H_DASH = 4;

    /** V_DASH shaped point */
    public static final int V_DASH = 5;

    /** MULTIPLY shaped point */
    public static final int MULTIPLY = 6;

    /** STAR shaped point */
    public static final int STAR = 7;

    /** X_MARK shaped point */
    public static final int X_MARK = 8;

    /** UP_ARROW shaped point */
    public static final int UP_ARROW = 9;

    /** DOWN_ARROW shaped point */
    public static final int DOWN_ARROW = 10;

    /** LEFT_ARROW shaped point */
    public static final int LEFT_ARROW = 11;

    /** RIGHT_ARROW shaped point */
    public static final int RIGHT_ARROW = 12;

    /** Bezier Control Point */
    public static final char CONTROL_POINT = 'c';

    // For the c operator we have both control points
    public static final char CONTROL_POINT_C = 'c';

    // For the v operator, the first control point shall coincide with initial point of the curve.
    public static final char CONTROL_POINT_V = 'v';

    // For the y operator, the second control point shall coincide with final point of the curve.
    public static final char CONTROL_POINT_Y = 'y';

    protected float x;
    protected float y;
    protected float r = 2f;
    protected int shape = Point.CIRCLE;
    // protected int color = Color.black;
    protected int align = Align.RIGHT;

    protected float[] fillColor = null;
    protected float strokeWidth = 1f;
    protected float[] strokeColor = null;
    protected String strokePattern = "[] 0";
    protected String pathOperator = PathOperator.CLOSE_AND_STROKE;

    protected char controlPoint = '\0';
    protected boolean drawPath = false;

    private String text;
    private int textColor;
    private int textDirection;
    private String uri;

    /**
     *  The default constructor.
     */
    public Point() {
    }

    /**
     *  Copy constructor. Creates a deep copy of the specified point,
     *  including all visual properties, URI action, and text attributes.
     *
     *  @param point the point to copy.
     */
    public Point(Point point) {
        this.x = point.x;
        this.y = point.y;
        this.r = point.r;
        this.shape = point.shape;
        this.align = point.align;
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
     *  Constructor for creating point objects.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public Point(double x, double y) {
        this((float) x, (float) y);
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
     *  @param controlPoint the type of control point.
     */
    public Point(double x, double y, char controlPoint) {
        this((float) x, (float) y, controlPoint);
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
     *  Sets the position (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    /**
     *  Sets the position (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void setPosition(double x, double y) {
        setLocation(x, y);
    }

    /**
     * Sets the location of the point.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void setXY(float x, float y) {
        setLocation(x, y);
    }

    /**
     * Sets the location of the point.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void setXY(double x, double y) {
        setLocation(x, y);
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
     *  Sets the location (x, y) of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     *  @param y the y coordinate of this point when drawn on the page.
     *  @return the location of the point.
     */
    public Point setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     *  Sets the x coordinate of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     */
    public void setX(double x) {
        this.x = (float) x;
    }

    /**
     *  Sets the x coordinate of this point.
     *
     *  @param x the x coordinate of this point when drawn on the page.
     */
    public void setX(float x) {
        this.x = x;
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
     */
    public void setY(double y) {
        this.y = (float) y;
    }

    /**
     *  Sets the y coordinate of this point.
     *
     *  @param y the y coordinate of this point when drawn on the page.
     */
    public void setY(float y) {
        this.y = y;
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
     */
    public void setRadius(double r) {
        this.r = (float) r;
    }

    /**
     *  Sets the radius of this point.
     *
     *  @param r the radius.
     */
    public void setRadius(float r) {
        this.r = r;
    }

    /**
     *  Returns the radius of this point.
     *
     *  @return the radius of this point.
     */
    public float getRadius() {
        return r;
    }

    public void setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void setFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
    }

    public void setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

//     public void setStrokeWidth(float strokeWidth) {
//         this.strokeWidth = strokeWidth;
//     }

    public float[] getFillColor() {
        return this.fillColor;
    }

    public Point setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public Point setStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public Point setStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    public float[] getStrokeColor() {
        return this.strokeColor;
    }

    /**
     *  Sets the shape of this point.
     *
     *  @param shape the shape of this point. Supported values:
     *  <pre>
     *  Point.INVISIBLE
     *  Point.CIRCLE
     *  Point.DIAMOND
     *  Point.BOX
     *  Point.PLUS
     *  Point.H_DASH
     *  Point.V_DASH
     *  Point.MULTIPLY
     *  Point.STAR
     *  Point.X_MARK
     *  Point.UP_ARROW
     *  Point.DOWN_ARROW
     *  Point.LEFT_ARROW
     *  Point.RIGHT_ARROW
     *  </pre>
     */
    public void setShape(int shape) {
        this.shape = shape;
    }

    /**
     *  Returns the point shape code value.
     *
     *  @return the shape code value.
     */
    public int getShape() {
        return shape;
    }

    /**
     *  Sets the width of the lines of this point.
     *
     *  @param strokeWidth the line width.
     */
    public void setStrokeWidth(double strokeWidth) {
        this.strokeWidth = (float) strokeWidth;
    }

    /**
     *  Sets the width of the lines of this point.
     *
     *  @param strokeWidth the line width.
     */
    public void setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
    }

    /**
     *  Returns the width of the lines used to draw this point.
     *
     *  @return the width of the lines used to draw this point.
     */
    public float getStrokeWidth() {
        return strokeWidth;
    }

    /**
     *
     *  The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
     *  It is specified by a dash array and a dash phase.
     *  The elements of the dash array are positive numbers that specify the lengths of
     *  alternating dashes and gaps.
     *  The dash phase specifies the distance into the dash pattern at which to start the dash.
     *  The elements of both the dash array and the dash phase are expressed in user space units.
     *  <pre>
     *  Examples of line dash patterns:
     *
     *      "[Array] Phase"     Appearance          Description
     *      _______________     _________________   ____________________________________
     *
     *      "[] 0"              -----------------   Solid line
     *      "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *      "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *      "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *      "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *      "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     *  </pre>
     *
     *  @param strokePattern the line dash pattern.
     */
    public void setStrokePattern(String strokePattern) {
        this.strokePattern = strokePattern;
    }

    /**
     *  Returns the line dash pattern.
     *
     *  @return the line dash pattern.
     */
    public String getStrokePattern() {
        return strokePattern;
    }

    public void setPathOperator(String pathOperator) {
        this.pathOperator = pathOperator;
    }

    public String getPathOperator() {
        return this.pathOperator;
    }

    /**
     *  Sets this point as the start of a path that will be drawn on the chart.
     *
     *  @return the point.
     */
    public Point setDrawPath() {
        this.drawPath = true;
        return this;
    }

    /**
     *  Sets the URI for the "click point" action.
     *
     *  @param uri the URI
     */
    public void setURIAction(String uri) {
        this.uri = uri;
    }

    /**
     *  Returns the URI for the "click point" action.
     *
     *  @return the URI for the "click point" action.
     */
    public String getURIAction() {
        return uri;
    }

    /**
     *  Sets the point text.
     *
     *  @param text the text.
     */
    public void setText(String text) {
        this.text = text;
    }

    /**
     *  Returns the text associated with this point.
     *
     *  @return the text.
     */
    public String getText() {
        return this.text;
    }

    /**
     *  Sets the point's text color.
     *
     *  @param textColor the text color.
     */
    public void setTextColor(int textColor) {
        this.textColor = textColor;
    }

    /**
     *  Returns the point's text color.
     *
     *  @return the text color.
     */
    public int getTextColor() {
        return this.textColor;
    }

    /**
     *  Sets the point's text direction.
     *
     *  @param textDirection the text direction.
     */
    public void setTextDirection(int textDirection) {
        this.textDirection = textDirection;
    }

    /**
     *  Returns the point's text direction.
     *
     *  @return the text direction.
     */
    public int getTextDirection() {
        return this.textDirection;
    }

    /**
     *  Sets the point alignment inside table cell.
     *
     *  @param align the alignment value.
     */
    public void setAlignment(int align) {
        this.align = align;
    }

    /**
     *  Returns the point alignment.
     *
     *  @return align the alignment value.
     */
    public int getAlignment() {
        return this.align;
    }

    /**
     *  Draws this point on the specified page.
     *
     *  @param page the page to draw this point on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
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
        page.drawPoint(this);
        page.restoreGraphicsState();

        return new float[] {x + r, y + r};
    }
}   // End of Point.java
