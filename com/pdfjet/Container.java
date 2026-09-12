package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * A group of drawable elements that are moved, rotated and scaled together.
 */
public class Container implements Drawable {
    /** The x coordinate of this container on the page. */
    public float x;
    /** The y coordinate of this container on the page. */
    public float y;
    /** The width of this container. */
    public float width;
    /** The height of this container. */
    public float height;
    /** The rotation angle in degrees. */
    public float rotateDegrees;
    /** The horizontal scaling factor. */
    public float scaleX;
    /** The vertical scaling factor. */
    public float scaleY;
    private List<Drawable> elements;
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
     * Sets the rotation angle of this container.
     *
     * @param degrees the rotation angle in degrees.
     */
    public void rotate(double degrees) {
        this.rotateDegrees = (float) degrees;
    }

    /**
     * Sets the rotation angle.
     *
     * @param degrees the rotation angle in degrees
     * @return this Container object.
     */
    public Container setRotation(double degrees) {
        this.rotateDegrees = (float) degrees;
        return this;
    }

    /**
     * Sets clockwise rotation.
     *
     * @param degrees the rotation angle in degrees (clockwise)
     * @return this Container object.
     */
    public Container setRotationClockwise(double degrees) {
        this.rotateDegrees = (float) -degrees;
        return this;
    }

    /**
     * Sets counter-clockwise rotation.
     *
     * @param degrees the rotation angle in degrees (counter-clockwise)
     * @return this Container object.
     */
    public Container setRotationCounterClockwise(double degrees) {
        this.rotateDegrees = (float) degrees;
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
    public Container setScaleFactor(float factor) {
        setScaleFactorXY(factor, factor);
        return this;
    }

    /**
     * Sets non-uniform scaling factors for the X and Y axes.
     *
     * @param sx the scaling factor for X
     * @param sy the scaling factor for Y
     * @return this Container object.
     */
    public Container setScaleFactorXY(float sx, float sy) {
        this.scaleX = sx;
        this.scaleY = sy;
        return this;
    }

    /**
     * Adds a border in the specified color around this container.
     *
     * @param borderColor the border color as a 0xRRGGBB value.
     * @return this Container object.
     */
    public Container setBorderColor(int borderColor) {
        Rect rect = new Rect(0f, 0f, width, height);
        rect.setBorderColor(borderColor);
        this.add(rect);
        return this;
    }

    /**
     * Adds a black border around this container.
     */
    public void addBorder() {
        Rect rect = new Rect(0f, 0f, width, height);
        rect.setBorderColor(Color.black);
        this.add(rect);
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
