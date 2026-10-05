/*
 * BaseAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The base class of the annotations: circles, squares, polygons and text notes.
 */
public abstract class BaseAnnotation implements Drawable {
    String annotationType = null;
    float[] point1 = new float[] {0f, 0f};
    float[] vertices = null;
    float[] fillColor = new float[] {0.5f, 0.5f, 0.5f};
    float opacity = 1f;
    String title = null;
    String contents = null;
    String uri = null;
    String key = null;
    String language = null;
    String actualText = null;
    String altDescription = null;
    // The size setSize sets, which the second point is at from the first when
    // the annotation is drawn, wherever setLocation puts the first.
    float width = 0f;
    float height = 0f;
    boolean hasSize = false;

    /**
     * Creates an annotation. The circle, square, polygon and text annotations call it.
     */
    protected BaseAnnotation() {
    }

    public BaseAnnotation setLocation(float x, float y) {
        this.point1 = new float[] {x, y};
        return this;
    }

    /**
     * Sets the size of this annotation, measured from its location: its
     * second point is w to the right of its location and h below it, whether
     * setLocation is called before or after.
     *
     * @param w the width.
     * @param h the height.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setSize(float w, float h) {
        this.width = w;
        this.height = h;
        this.hasSize = true;
        return this;
    }

    // Returns the second point of the annotation, which is the origin of the
    // page until setSize is called.
    private float[] getCorner() {
        if (!hasSize) {
            return new float[] {0f, 0f};
        }
        return new float[] {point1[0] + width, point1[1] + height};
    }

    /**
     * Sets the fill color of this annotation.
     *
     * @param fillColor the red, green and blue components, from 0.0 to 1.0.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setFillColor(float[] fillColor) {
        this.fillColor = Util.copyOf(fillColor);
        return this;
    }

    /**
     * Sets the fill color of this annotation.
     * Color.transparent leaves it unchanged.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setFillColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setFillColor(new float[] {r, g, b});
        return this;
    }

    /**
     * Sets the opacity of this annotation.
     *
     * @param opacity the opacity, from 0.0 (invisible) to 1.0 (opaque, the default).
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setOpacity(float opacity) {
        this.opacity = opacity;
        return this;
    }

    /**
     * Sets the title of this annotation.
     *
     * @param title the title.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setTitle(String title) {
        this.title = title;
        return this;
    }

    /**
     * Sets the text contents of this annotation.
     *
     * @param contents the contents.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setContents(String contents) {
        this.contents = contents;
        return this;
    }

    public float[] drawOn(Page page) {
        float[] point2 = getCorner();
        if (page == null) {
            return point2;  // Measured, not drawn
        }
        page.addAnnotation(new Annotation(
                annotationType,
                point1[0],
                point1[1],
                point2[0],
                point2[1],
                // A copy, which the annotation keeps until the page is written
                (vertices == null) ? null : Util.copyOf(vertices),
                fillColor,      // Fill Color
                opacity,        // Opacity
                title,          // Title
                contents,       // Contents
                uri,            //
                key,            // The destination name
                language,
                actualText,
                altDescription));
        return point2;
    }
}
