/*
 * Rect.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A rectangle that can be drawn on a page.
 */
public class Rect implements Drawable {
    /** The x coordinate of the top left corner. */
    protected float x;
    /** The y coordinate of the top left corner. */
    protected float y;
    private float w;
    private float h;
    private float r;

    private float[] fillColor;
    private float borderWidth;
    private float[] borderColor;
    private String borderPattern = "[] 0";

    private String uri;
    private String key;
    private String language = "en-US";
    private String actualText = null;
    private String altDescription = null;

    /**
     * The default constructor.
     */
    public Rect() {
    }

    /**
     * Creates a rectangle.
     *
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @param w the width.
     * @param h the height.
     */
    public Rect(float x, float y, float w, float h) {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    /**
     * Creates a rectangle.
     *
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @param w the width.
     * @param h the height.
     */
    public Rect(double x, double y, double w, double h) {
        this.x = (float) x;
        this.y = (float) y;
        this.w = (float) w;
        this.h = (float) h;
    }

    public Rect setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location of the top left corner of this rectangle.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Rect object.
     */
    public Rect setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the size of this rectangle.
     *
     * @param w the width.
     * @param h the height.
     * @return this Rect object.
     */
    public Rect setSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /**
     * Sets the fill color of this rectangle.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Rect object.
     */
    public Rect setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setFillColor(r, g, b);
        return this;
    }

    /**
     * Sets the fill color of this rectangle.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this Rect object.
     */
    public Rect setFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the fill color of this rectangle.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Rect object.
     */
    public Rect setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /**
     * Sets the border width of this rectangle.
     *
     * @param borderWidth the border width.
     * @return this Rect object.
     */
    public Rect setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /**
     * Sets the border color of this rectangle. Color.transparent removes the border.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Rect object.
     */
    public Rect setBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setBorderColor(r, g, b);
        return this;
    }

    /**
     * Sets the border color of this rectangle.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this Rect object.
     */
    public Rect setBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the border color of this rectangle.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Rect object.
     */
    public Rect setBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        return this;
    }

    /**
     * Sets the corner radius.
     * @param r the radius.
     * @return this Rect object.
     */
    public Rect setCornerRadius(float r) {
        this.r = r;
        return this;
    }

    /**
     * Sets the URI for the "click rect" action.
     * @param uri the URI
     * @return this Rect object.
     */
    public Rect setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Sets the destination key for the action.
     * @param key the destination name.
     * @return this Rect object.
     */
    public Rect setGoToAction(String key) {
        this.key = key;
        return this;
    }

    /**
     * Sets the language of this rect, used for accessibility.
     * @param language the language, for example "en-US".
     * @return this Rect object.
     */
    public Rect setLanguage(String language) {
        this.language = language;
        return this;
    }

    /**
     * Sets the alternate description of this rect.
     * @param altDescription the alternate description of the rect.
     * @return this Rect.
     */
    public Rect setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text for this rect.
     * @param actualText the actual text for the rect.
     * @return this Rect.
     */
    public Rect setActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Sets the line dash pattern that controls the pattern of dashes and gaps used to stroke paths.
     * @param borderPattern the line dash pattern.
     * @return this Rect object.
     */
    public Rect setBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
        return this;
    }

    /**
     * Scales this rect by the specified factor.
     * @param factor the factor used to scale the rect.
     */
    public void scaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    /**
     * Draws this rect on the specified page.
     * @param page the page to draw this rect on.
     * @return x and y coordinates of the bottom right corner of this component.
     */
    public float[] drawOn(Page page) throws Exception {
        if (page == null) {
            return new float[] {x + w, y + h};
        }

        final float k = 0.55228f;
        // A rectangle carries no text, so it is decorative content.
        page.addArtifactBMC();
        page.saveGraphicsState();
        if (this.r == 0.0f) {
            if (this.fillColor != null) {
                page.moveTo(this.x, this.y);
                page.lineTo(this.x + this.w, this.y);
                page.lineTo(this.x + this.w, this.y + this.h);
                page.lineTo(this.x, this.y + this.h);
                page.lineTo(this.x, this.y);
                page.setBrushColor(this.fillColor);
                page.fillPath();
            }
            if (borderColor != null) {
                page.moveTo(this.x, this.y);
                page.lineTo(this.x + this.w, this.y);
                page.lineTo(this.x + this.w, this.y + this.h);
                page.lineTo(this.x, this.y + this.h);
                page.setPenColor(this.borderColor);
                page.setPenWidth(this.borderWidth);
                page.setStrokeDashPattern(this.borderPattern);
                page.closePath();
            }
        } else {
            // The pen and brush must be set before the path is painted,
            // otherwise the rounded rectangle is drawn with whatever state
            // the page happened to be left in.
            if (borderColor != null && borderPattern != null) {
                page.setStrokeDashPattern(borderPattern);
            }
            if (fillColor != null) {
                page.setBrushColor(fillColor);
            }
            if (borderColor != null) {
                page.setPenWidth(borderWidth);
                page.setPenColor(borderColor);
            }

            List<Point> points = new ArrayList<Point>();
            points.add(new Point((this.x + this.r), this.y));
            points.add(new Point((this.x + this.w) - this.r, this.y));
            points.add(new Point((this.x + this.w - this.r) + this.r * k, this.y, Point.CONTROL_POINT_C));
            points.add(new Point((this.x + this.w), (this.y + this.r) - this.r * k, Point.CONTROL_POINT_C));
            points.add(new Point((this.x + this.w), (this.y + this.r)));
            points.add(new Point((this.x + this.w), (this.y + this.h) - this.r));
            points.add(new Point((this.x + this.w), ((this.y + this.h) - this.r) + this.r * k, Point.CONTROL_POINT_C));
            points.add(new Point(((this.x + this.w) - this.r) + this.r * k, (this.y + this.h), Point.CONTROL_POINT_C));
            points.add(new Point(((this.x + this.w) - this.r), (this.y + this.h)));
            points.add(new Point((this.x + this.r), (this.y + this.h)));
            points.add(new Point(((this.x + this.r) - this.r * k), (this.y + this.h), Point.CONTROL_POINT_C));
            points.add(new Point(this.x, ((this.y + this.h) - this.r) + this.r * k, Point.CONTROL_POINT_C));
            points.add(new Point(this.x, (this.y + this.h) - this.r));
            points.add(new Point(this.x, (this.y + this.r)));
            points.add(new Point(this.x, (this.y + this.r) - this.r * k, Point.CONTROL_POINT_C));
            points.add(new Point((this.x + this.r) - this.r * k, this.y, Point.CONTROL_POINT_C));
            points.add(new Point((this.x + this.r), this.y));

            if (fillColor != null && borderColor == null) {
                page.drawPath(points, PathOperator.FILL);
            } else if (fillColor == null && borderColor != null) {
                page.drawPath(points, PathOperator.STROKE);
            } else if (fillColor != null && borderColor != null) {
                page.drawPath(points, PathOperator.FILL_AND_STROKE);
            }
        }
        page.restoreGraphicsState();
        page.addEMC();

        if (this.uri != null || this.key != null) {
            page.addAnnotation(new Annotation(
                    Annotation.Link,
                    this.x,
                    this.y,
                    this.x + this.w,
                    this.y + this.h,
                    null,       // Vertices
                    null,       // Fill Color
                    0f,         // Transparency
                    null,       // Title
                    null,       // Contents
                    this.uri,
                    this.key,
                    this.language,
                    this.actualText,
                    this.altDescription));
        }

        return new float[] { this.x + this.w, this.y + this.h };
    }
}
