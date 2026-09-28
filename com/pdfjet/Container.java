/*
 * Container.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * A group of drawable elements that are moved, rotated and scaled together:
 * shapes, text, images, annotations, stamps and nested containers.
 * <p>
 * What a container is good at: it takes anything that implements Drawable,
 * so a group can hold an Image, a Table, a chart, a barcode, a link or
 * another annotation, and each element keeps everything it can do. Each
 * element also tags itself in a PDF/UA document, so a TextLine in a container
 * is a paragraph of its own and an Image is a figure with its alternate
 * description. Laying a group out once and placing it, rotated or scaled, is
 * what it is for; elements that have no rotation of their own get one this
 * way.
 * <p>
 * What it costs: the elements are drawn into the page on every drawOn, so a
 * container on 100 pages writes its drawing 100 times over.
 * <p>
 * Use a Stamp instead for content that repeats on many pages -- a header, a
 * footer, a logo, a watermark -- which is written once as a form XObject and
 * placed with a few bytes. Of a 200 by 50 point box with two lines of text, a
 * container writes 367 bytes into every page and a stamp 83, so the stamp is
 * the smaller of the two from the fourth page on: over 100 pages it saves
 * 12,855 bytes of a 104,530 byte file, and over 500 pages 66,052 of 275,553.
 * On one page the stamp is the larger, since its XObject costs about 300
 * bytes of its own, and it draws only paths and text in an embedded font.
 * <p>
 * Please see Example_06 and Example_35.
 */
public class Container implements Drawable {
    float x;
    float y;
    float width;
    float height;
    float rotateDegrees;
    float scaleX;
    float scaleY;
    private List<Drawable> elements;
    private Rect border = null;

    /**
     * Creates a new container with the specified width and height.
     * <p>
     * The container is initialized with:
     * <ul>
     *   <li>Rotation set to {@code 0} degrees</li>
     *   <li>Scaling factors set to {@code 1.0} for both axes</li>
     *   <li>An empty list of drawable elements</li>
     * </ul>
     *
     * @param width  the width of the container
     * @param height the height of the container
     */
    public Container(float width, float height) {
        this.width = width;
        this.height = height;
        this.rotateDegrees = 0f;
        this.scaleX = 1f;
        this.scaleY = 1f;
        this.elements = new ArrayList<Drawable>();
    }

    /**
     * Sets the location of the container on the page.
     *
     * @param x the X coordinate
     * @param y the Y coordinate
     * @return Container this container
     */
    public Container setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Rotates this container around its center: clockwise for a positive
     * angle, as every rotation in PDFjet turns, and counterclockwise for a
     * negative angle.
     *
     * @param degrees the rotation angle in degrees.
     * @return this Container object.
     */
    public Container setRotation(float degrees) {
        // The rotation of the page turns counterclockwise.
        this.rotateDegrees = -degrees;
        return this;
    }

    /**
     * Returns the center of this container, which it rotates around.
     *
     * @return the x and y coordinates of the center.
     */
    public float[] getRotationCenter() {
        return new float[] {x + width/2f, y + height/2f};
    }

    /**
     * Sets a uniform scaling factor for both X and Y axes.
     *
     * @param factor the scaling factor to apply
     * @return this Container object.
     */
    public Container scaleBy(float factor) {
        scaleBy(factor, factor);
        return this;
    }

    /**
     * Sets non-uniform scaling factors for the X and Y axes.
     *
     * @param sx the scaling factor for X
     * @param sy the scaling factor for Y
     * @return this Container object.
     */
    public Container scaleBy(float sx, float sy) {
        this.scaleX = sx;
        this.scaleY = sy;
        return this;
    }

    /**
     * Sets the color of the border around this container.
     *
     * @param borderColor the border color as a 0xRRGGBB value.
     * @return this Container object.
     */
    public Container setBorderColor(int borderColor) {
        if (border == null) {
            border = new Rect(0f, 0f, width, height);
            this.add(border);
        }
        border.setBorderColor(borderColor);
        return this;
    }

    /**
     * Sets the border color, drawing a border around this container.
     *
     * @param rgbColor the color as red, green and blue components from 0.0 to 1.0.
     * @return this Container object.
     */
    public Container setBorderColor(float[] rgbColor) {
        if (border == null) {
            border = new Rect(0f, 0f, width, height);
            this.add(border);
        }
        border.setBorderColor(rgbColor);
        return this;
    }

    /**
     * Adds a drawable element to this container.
     *
     * @param element the element to add
     * @return this Container object.
     */
    public Container add(Drawable element) {
        this.elements.add(element);
        return this;
    }

    /**
     * Draws this container and its child elements onto the page.
     *
     * @param page the {@link Page} to draw on
     * @return an array containing the bottom-right position of the container
     * @throws Exception if drawing fails
     */
    public float[] drawOn(Page page) throws Exception {
        if (page == null || scaleX == 0f || scaleY == 0f) {
            return new float[] { this.x + width, this.y + height };  // Measured, or nothing to paint.
        }
        page.saveGraphicsState();

        page.append("1 0 0 1 ");
        page.append(this.x);
        page.append(' ');
        page.append(-this.y);
        page.append(" cm\n");

        float cx = width / 2f;
        float cy = height / 2f;

        page.append("1 0 0 1 ");
        page.append(cx);
        page.append(' ');
        page.append(page.height - cy);
        page.append(" cm\n");

        double rad = rotateDegrees * (Math.PI / 180.0);
        float cos = (float)Math.cos(rad);
        float sin = (float)Math.sin(rad);
        page.appendRotation(cos, sin);

        page.append(scaleX);
        page.append(' ');
        page.append('0');
        page.append(' ');
        page.append('0');
        page.append(' ');
        page.append(scaleY);
        page.append(' ');
        page.append('0');
        page.append(' ');
        page.append('0');
        page.append(" cm\n");

        page.append("1 0 0 1 ");
        page.append(-cx);
        page.append(' ');
        page.append(-(page.height - cy));
        page.append(" cm\n");

        // What the page does not move with the cm operators above, the
        // rectangles of the links and of the annotations and the bounding
        // boxes of the figures, it moves with the same transform, from the top
        // left corner of the page: turned and scaled around the center of the
        // container, and moved to its location.
        float m0 = scaleX * cos;
        float m1 = -scaleX * sin;
        float m2 = scaleY * sin;
        float m3 = scaleY * cos;
        float centerX = this.x + cx;
        float centerY = this.y + cy;
        float[] saved = page.pushTransform(new float[] {
                m0, m1, m2, m3, centerX - m0*cx - m2*cy, centerY - m1*cx - m3*cy});
        for (Drawable element : elements) {
            element.drawOn(page);
        }
        page.popTransform(saved);

        page.restoreGraphicsState();

        return new float[] { this.x + width, this.y + height };
    }
}
