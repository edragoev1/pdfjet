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
public class BaseAnnotation implements Drawable {
    String annotationType = null;
    float[] point1 = new float[] {0f, 0f};
    float[] point2 = new float[] {0f, 0f};
    float[] vertices = null;
    float[] fillColor = new float[] {0.5f, 0.5f, 0.5f};
    float transparency = 1f;
    String title = null;
    String contents = null;
    String uri = null;
    String key = null;
    String language = null;
    String actualText = null;
    String altDescription = null;
    Container container = null;

    /**
     * Creates an annotation.
     */
    public BaseAnnotation() {
    }

    public BaseAnnotation setLocation(float x, float y) {
        this.point1 = new float[] {x, y};
        return this;
    }

    /**
     * Sets the size of this annotation, measured from its location.
     *
     * @param w the width.
     * @param h the height.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setSize(float w, float h) {
        this.point2 = new float[] {point1[0] + w, point1[1] + h};
        return this;
    }

    /**
     * Sets the fill color of this annotation.
     *
     * @param fillColor the red, green and blue components, from 0.0 to 1.0.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setFillColor(float[] fillColor) {
        this.fillColor = fillColor;
        return this;
    }

    /**
     * Sets the fill color of this annotation.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setFillColor(new float[] {r, g, b});
        return this;
    }

    /**
     * Sets the transparency of this annotation.
     *
     * @param transparency the transparency, from 0.0 to 1.0. The default is 1.0.
     * @return this BaseAnnotation object.
     */
    public BaseAnnotation setTransparency(float transparency) {
        this.transparency = transparency;
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

    /**
     * Rotates this annotation together with the container it is in.
     *
     * @param degrees the rotation angle in degrees.
     */
    public void rotate(double degrees) {
        if (container == null) { return; }
        float[] center = container.getRotationCenter();
        if (container.parent != null) {
            center[0] += container.parent.x;
            center[1] += container.parent.y;
        }
        point1 = Container.rotateAroundCenter(point1, center, degrees);
        point2 = Container.rotateAroundCenter(point2, center, degrees);
        if (annotationType.equals(Annotation.Polygon)) {
            for (int i = 0; i < vertices.length; i += 2) {
                float[] point = Container.rotateAroundCenter(
                    new float[] {vertices[i], vertices[i + 1]}, new float[] {0f, 0f}, degrees);
                vertices[i] = point[0];
                vertices[i + 1] = point[1];
            }
        }
    }

    public float[] drawOn(Page page) {
        page.addAnnotation(new Annotation(
                annotationType,
                point1[0],
                point1[1],
                point2[0],
                point2[1],
                vertices,       // Vertices
                fillColor,      // Fill Color
                transparency,   // Transparency
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
