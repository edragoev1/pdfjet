/**
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
    private String strokePattern = "[] 0";

    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    private Line line;

    /**
     * The default constructor.
     */
    public Arc() {
    }

    public void setPosition(float cx, float cy) {
        setCenterXY(cx, cy);
    }

    public Arc setStartPointToEndOf(Line line) {
        this.line = line;
        return this;
    }

    public Arc setCenterXY(float cx, float cy) {
        this.cx = cx;
        this.cy = cy;
        return this;
    }

    public Arc setRadiusX(float rx) {
        this.rx = rx;
        return this;
    }

    public Arc setRadiusY(float ry) {
        this.ry = ry;
        return this;
    }

    public Arc setRadius(float r) {
        this.rx = r;
        this.ry = r;
        return this;
    }

    public Arc setStartAngle(float angle) {
        this.startAngle = angle;
        return this;
    }

    public Arc setSweepDegreesCW(float sweepDegrees) {
        this.sweepDegrees = sweepDegrees;
        return this;
    }

    public Arc setSweepDegreesCCW(float sweepDegrees) {
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
     * @param pattern the line dash pattern.
     * @return this Arc object.
     */
    public Arc setStrokePattern(String pattern) {
        this.strokePattern = pattern;
        return this;
    }

    /**
     * Sets the width of this line.
     *
     * @param width the width.
     * @return this Arc object.
     */
    public Arc setStrokeWidth(double width) {
        this.strokeWidth = (float) width;
        return this;
    }

    /**
     * Sets the width of this line.
     *
     * @param strokeWidth the width.
     * @return this Arc object.
     */
    public Arc setStrokeWidth(float width) {
        this.strokeWidth = width;
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

    public Arc setStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public Arc setStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    public Arc setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.setFillColor(r, g, b);
        return this;
    }

    public Arc setFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    public Arc setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    public Arc setRotateDegreesCW(float degrees) {
        this.rotateDegrees = -degrees;
        return this;
    }

    public Arc setRotateDegreesCW(double degrees) {
        this.rotateDegrees = (float) -degrees;
        return this;
    }

    public Arc setRotateDegreesCCW(float degrees) {
        this.rotateDegrees = degrees;
        return this;
    }

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
     * @throws Exception
     */
    public float[] drawOn(Page page) {
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
        page.append("q\n");
        float centerX = cx;
        float centerY = page.height - cy;
        page.rotateAroundCenter(centerX, centerY, rotateDegrees);
        float[] arcPoints = page.drawArc(
                cx,
                cy,
                rx,
                ry,
                startAngle,
                sweepDegrees);
        if (strokeColor != null && strokePattern != null) {
            page.setStrokeDashPattern(strokePattern);
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
        page.append("Q\n");
        page.addEMC();
        return arcPoints;
    }
}   // End of Arc.java
