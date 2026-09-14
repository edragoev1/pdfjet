package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * A group of drawable elements that are moved, rotated and scaled together:
 * shapes, text, images, annotations and nested containers. The elements are
 * drawn into the page on every drawOn.
 * <p>
 * Use a Container to lay out a group once and place it on a page, or to
 * rotate and scale elements that have no rotation of their own. Use a Stamp
 * for content that repeats on many pages, like a header, a footer or a
 * watermark: it is written once as a form XObject and each placement is a
 * single operator. Please see Example_06 and Example_35.
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
    Container parent = null;

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
     * Sets the rotation angle.
     *
     * @param degrees the rotation angle in degrees
     * @return this Container object.
     */
    public Container setRotation(float degrees) {
        this.rotateDegrees = degrees;
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
        if (element instanceof Container) {
            ((Container) element).parent = this;
        }
        this.elements.add(element);
        return this;
    }

    /**
     * Rotates a point around a center point.
     *
     * @param point the x and y coordinates of the point.
     * @param center the x and y coordinates of the center.
     * @param degrees the rotation angle in degrees.
     * @return the x and y coordinates of the rotated point.
     */
    protected static float[] rotateAroundCenter(float[] point, float[] center, double degrees) {
        double rad = degrees * Math.PI / 180.0; // convert to radians

        // translate to centre
        double dx = (double) (point[0] - center[0]);
        double dy = (double) (point[1] - center[1]);

        // rotate
        double cos = Math.cos(rad);
        double sin = Math.sin(rad);
        double dxRot =  dx * cos - dy * sin;
        double dyRot =  dx * sin + dy * cos;

        // translate back
        double nx = center[0] + dxRot;
        double ny = center[1] + dyRot;

        return new float[] {(float) nx, (float) ny};
    }

    /**
     * Draws this container and its child elements onto the page.
     *
     * @param page the {@link Page} to draw on
     * @return an array containing the bottom-right position of the container
     * @throws Exception if drawing fails
     */
    public float[] drawOn(Page page) throws Exception {
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
        page.append(FastFloat.toByteArray(cos));
        page.append(' ');
        page.append(FastFloat.toByteArray(sin));
        page.append(' ');
        page.append(FastFloat.toByteArray(-sin));
        page.append(' ');
        page.append(FastFloat.toByteArray(cos));
        page.append(" 0 0 cm\n");

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

        for (Drawable element : elements) {
            if (element instanceof BaseAnnotation) {
                BaseAnnotation annot = (BaseAnnotation) element;
                annot.container = this;
                annot.point1[0] += x;
                annot.point1[1] += y;
                annot.point2[0] += x;
                annot.point2[1] += y;
                if (this.parent != null) {
                    annot.point1[0] += parent.x;
                    annot.point1[1] += parent.y;
                    annot.point2[0] += parent.x;
                    annot.point2[1] += parent.y;
                }
                annot.rotate(-rotateDegrees);
            }
            element.drawOn(page);
        }

        page.restoreGraphicsState();

        return new float[] { this.x + width, this.y + height };
    }
}
