/*
 * Rect.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

public class Rect implements Drawable {
    protected float x;
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

    public Rect(float x, float y, float w, float h) {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

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

    public Rect setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    public void setPosition(double x, double y) {
        setLocation((float) x, (float) y);
    }

    public void setSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    public void setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setFillColor(r, g, b);
    }

    public void setFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
    }

    public void setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
    }

    public void setBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            return;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setBorderColor(r, g, b);
    }

    public void setBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
    }

    public void setBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
    }

    /**
     * Sets the corner radius.
     * @param r the radius.
     */
    public void setCornerRadius(float r) {
        this.r = r;
    }

    /**
     * Sets the URI for the "click rect" action.
     * @param uri the URI
     */
    public void setURIAction(String uri) {
        this.uri = uri;
    }

    /**
     * Sets the destination key for the action.
     * @param key the destination name.
     */
    public void setGoToAction(String key) {
        this.key = key;
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
     * Sets the type of the structure.
     * @param structureType the structure type.
     * @return this Rect.
     */
    public Rect setStructureType(String structureType) {
        // this.structureType = structureType;  // TODO
        return this;
    }

    /**
     * Sets the line dash pattern that controls the pattern of dashes and gaps used to stroke paths.
     * @param borderPattern the line dash pattern.
     */
    public void setBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
    }

    /**
     * Sets the private fillShape variable.
     * If the value of fillShape is true - the rect is filled with the current brush color.
     * @param fillShape the value used to set the private fillShape variable.
     */
    public void setFillShape(boolean fillShape) {
// TODO:        this.fillShape = fillShape;
    }

    /**
     * Places this rect in the another rect.
     * @param rect the other rect.
     * @param xOffset the x offset from the top left corner of the rect.
     * @param yOffset the y offset from the top left corner of the rect.
     */
    public void placeIn(Rect rect, float xOffset, float yOffset) {
        this.x = rect.x + xOffset;
        this.y = rect.y + yOffset;
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

            if (borderColor != null && borderPattern != null) {
                page.setStrokeDashPattern(borderPattern);
            }
            if (fillColor != null && borderColor != null) {
                page.setBrushColor(fillColor);
                page.setPenWidth(borderWidth);
                page.setPenColor(borderColor);
                page.append("B\n");
            } else if (fillColor != null && borderColor == null) {
                page.setBrushColor(fillColor);
                page.append("f\n");
            } else if (fillColor == null && borderColor != null) {
                page.setPenWidth(borderWidth);
                page.setPenColor(borderColor);
                page.append("S\n");
            }
        }
        page.restoreGraphicsState();
        page.addEMC();

        if (this.uri != null || this.key != null) {
// TODO:
//             page.addAnnotation(new Annotation(
//                 Annotation.Link,
//                 this.x,
//                 this.y,
//                 this.x + this.w,
//                 this.y + this.h,
//                 null,       // Vertices
//                 null,       // Fill Color
//                 0f,         // Transparency
//                 null,       // Title
//                 null,       // Contents
//                 this.uri,
//                 this.key,
//                 this.language,
//                 this.actualText,
//                 this.altDescription));
        }

        return new float[] { this.x + this.w, this.y + this.h };
    }
}
