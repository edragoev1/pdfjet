/*
 * Arc.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to create arc objects.
 */
public class Arc implements Drawable {
    private float cx;
    private float cy;
    private float rx;
    private float ry;
    private float startAngle;
    private float sweepDegrees;
    private float rotateDegrees;

    private float[] fillColor;
    private float[] strokeColor = new float[] {0f, 0f, 0f};   // Black color
    private float strokeWidth = 0f;
    private String strokeDashPattern = "[] 0";

    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    private Line line;

    /**
     * The default constructor.
     */
    public Arc() {
    }

    public Arc setLocation(float cx, float cy) {
        setCenterXY(cx, cy);
        return this;
    }

    /**
     * Sets the line whose end point this arc starts from.
     *
     * @param line the line.
     * @return this Arc object.
     */
    public Arc setStartPointToEndOf(Line line) {
        this.line = line;
        return this;
    }

    /**
     * Sets the center of this arc.
     *
     * @param cx the x coordinate of the center.
     * @param cy the y coordinate of the center.
     * @return this Arc object.
     */
    public Arc setCenterXY(float cx, float cy) {
        this.cx = cx;
        this.cy = cy;
        return this;
    }

    /**
     * Sets the horizontal radius of this arc.
     *
     * @param rx the horizontal radius.
     * @return this Arc object.
     */
    public Arc setRadiusX(float rx) {
        this.rx = rx;
        return this;
    }

    /**
     * Sets the vertical radius of this arc.
     *
     * @param ry the vertical radius.
     * @return this Arc object.
     */
    public Arc setRadiusY(float ry) {
        this.ry = ry;
        return this;
    }

    /**
     * Sets both radii of this arc to the same value, making it circular.
     *
     * @param r the radius.
     * @return this Arc object.
     */
    public Arc setRadius(float r) {
        this.rx = r;
        this.ry = r;
        return this;
    }

    /**
     * Sets the angle where this arc starts.
     *
     * @param angle the start angle in degrees.
     * @return this Arc object.
     */
    public final Arc setStartAngle(float angle) {
        this.startAngle = angle;
        return this;
    }

    /**
     * Sets how far this arc sweeps clockwise from its start angle.
     *
     * @param sweepDegrees the sweep angle in degrees.
     * @return this Arc object.
     */
    public final Arc setSweepDegreesCW(float sweepDegrees) {
        this.sweepDegrees = sweepDegrees;
        return this;
    }

    /**
     * Sets how far this arc sweeps counterclockwise from its start angle.
     *
     * @param sweepDegrees the sweep angle in degrees.
     * @return this Arc object.
     */
    public final Arc setSweepDegreesCCW(float sweepDegrees) {
        this.sweepDegrees = -sweepDegrees;
        return this;
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
     * @param strokeDashPattern the line dash pattern.
     * @return this Arc object.
     */
    public Arc setStrokeDashPattern(String strokeDashPattern) {
        this.strokeDashPattern = strokeDashPattern;
        return this;
    }

    /**
     * Sets the width of this line.
     *
     * @param strokeWidth the width.
     * @return this Arc object.
     */
    public Arc setStrokeWidth(double strokeWidth) {
        this.strokeWidth = (float) strokeWidth;
        return this;
    }

    /**
     * Sets the width of this line.
     *
     * @param strokeWidth the width.
     * @return this Arc object.
     */
    public Arc setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /**
     * Sets the color for this line.
     *
     * @param color the color specified as an integer.
     * @return this Arc object.
     */
    public Arc setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.setStrokeColor(r, g, b);
        return this;
    }

    /**
     * Sets the stroke color of this arc.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this Arc object.
     */
    public Arc setStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke color of this arc.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Arc object.
     */
    public Arc setStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    /**
     * Sets the fill color of this arc.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Arc object.
     */
    public Arc setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.setFillColor(r, g, b);
        return this;
    }

    /**
     * Sets the fill color of this arc.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this Arc object.
     */
    public Arc setFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the fill color of this arc.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Arc object.
     */
    public Arc setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /**
     * Rotates this arc clockwise.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Arc object.
     */
    public Arc setRotateDegreesCW(float degrees) {
        this.rotateDegrees = -degrees;
        return this;
    }

    /**
     * Rotates this arc clockwise.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Arc object.
     */
    public Arc setRotateDegreesCW(double degrees) {
        this.rotateDegrees = (float) -degrees;
        return this;
    }

    /**
     * Rotates this arc counterclockwise.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Arc object.
     */
    public Arc setRotateDegreesCCW(float degrees) {
        this.rotateDegrees = degrees;
        return this;
    }

    /**
     * Rotates this arc counterclockwise.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Arc object.
     */
    public Arc setRotateDegreesCCW(double degrees) {
        this.rotateDegrees = (float) degrees;
        return this;
    }

    /**
     * Sets the alternate description of this line.
     *
     * @param altDescription the alternate description of the line.
     * @return this Arc.
     */
    public Arc setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text for this line.
     *
     * @param actualText the actual text for the line.
     * @return this Arc.
     */
    public Arc setActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Scales this line by the specified factor.
     *
     * @param factor the factor used to scale the line.
     * @return this Arc object.
     */
    public Arc setScaleFactor(double factor) {
        return setScaleFactor((float) factor);
    }

    /**
     * Scales this line by the specified factor.
     *
     * @param factor the factor used to scale the line.
     * @return this Arc object.
     */
    public Arc setScaleFactor(float factor) {
        this.rx *= factor;
        this.ry *= factor;
        return this;
    }

    /**
     * Draws this line on the specified page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception if there is an error.
     */
    public float[] drawOn(Page page) throws Exception {
        // If a start point was set, calculate center so arc begins there
        if (line != null) {
            float dx = line.x2 - line.x1;
            float dy = line.y2 - line.y1;
            // Normalize and rotate 90° (clockwise perpendicular)
            float invLength = (float) (1.0 / Math.sqrt(dx*dx + dy*dy));
            float nx = -dy * invLength;
            float ny = dx * invLength;
            // Adjust direction based on sweep
            float sign = sweepDegrees > 0f ? 1f : -1f;
            cx = line.x2 + nx * rx * sign;
            cy = line.y2 + ny * ry * sign;
            startAngle = (float) (Math.atan2((double)(line.y2 - cy), (double)(line.x2 - cx)) * (180.0 / Math.PI));
        }

        page.addBMC(StructElem.P, language, actualText, altDescription);
        page.saveGraphicsState();
        float centerX = cx;
        float centerY = page.height - cy;
        page.rotateAroundCenter(centerX, centerY, rotateDegrees);
        page.drawArc(
                cx,
                cy,
                rx,
                ry,
                startAngle,
                sweepDegrees);
        if (strokeColor != null && strokeDashPattern != null) {
            page.setStrokeDashPattern(strokeDashPattern);
        }
        if (fillColor != null && strokeColor != null) {
            page.setBrushColor(fillColor);
            page.setPenWidth(strokeWidth);
            page.setPenColor(strokeColor);
            page.append("B\n");
        } else if (fillColor != null && strokeColor == null) {
            page.setBrushColor(fillColor);
            page.append("f\n");
        } else if (fillColor == null && strokeColor != null) {
            page.setPenWidth(strokeWidth);
            page.setPenColor(strokeColor);
            page.append("S\n");
        } else {    // Both brushColor == null and penColor == null
            page.setPenWidth(0f);
            page.setPenColor(Color.black);
            page.append("S\n");
        }
        page.restoreGraphicsState();
        page.addEMC();
        return new float[] {cx + rx, cy + ry};
    }
}   // End of Arc.java
