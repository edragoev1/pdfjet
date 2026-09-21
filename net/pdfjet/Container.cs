/*
 * Container.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A group of drawable elements that are moved, rotated and scaled together:
/// shapes, text, images, annotations and nested containers. The elements are
/// drawn into the page on every DrawOn.
///
/// Use a Container to lay out a group once and place it on a page, or to
/// rotate and scale elements that have no rotation of their own. Use a Stamp
/// for content that repeats on many pages, like a header, a footer or a
/// watermark: it is written once as a form XObject and each placement is a
/// single operator. Please see Example_06 and Example_35.
/// </summary>
public class Container : IDrawable {
    internal float x;
    internal float y;
    internal float width;
    internal float height;
    internal float rotateDegrees;
    internal float scaleX;
    internal float scaleY;
    private List<IDrawable> elements;
    private Rect border = null;
    internal Container parent = null;

    /// <summary>
    /// Creates a new container with the specified width and height.
    /// The container is initialized with:
    /// <list type="bullet">
    ///   <item><description>Rotation set to 0 degrees</description></item>
    ///   <item><description>Scaling factors set to 1.0 for both axes</description></item>
    ///   <item><description>An empty list of drawable elements</description></item>
    /// </list>
    /// </summary>
    /// <param name="width">The width of the container.</param>
    /// <param name="height">The height of the container.</param>
    public Container(float width, float height) {
        this.width = width;
        this.height = height;
        this.rotateDegrees = 0f;
        this.scaleX = 1f;
        this.scaleY = 1f;
        this.elements = new List<IDrawable>();
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location of the container on the page.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Container SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>
    /// Rotates this container around its center: clockwise for a positive
    /// angle, as every rotation in PDFjet turns, and counterclockwise for a
    /// negative angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public Container SetRotation(float degrees) {
        // The rotation of the page turns counterclockwise.
        this.rotateDegrees = -degrees;
        return this;
    }

    /// <summary>Returns the center of this container, which it rotates around.</summary>
    public float[] GetRotationCenter() {
        return new float[] {x + width/2f, y + height/2f};
    }

    /// <summary>
    /// Sets a uniform scaling factor for both X and Y axes.
    /// </summary>
    /// <param name="factor">The scaling factor to apply.</param>
    public Container ScaleBy(float factor) {
        ScaleBy(factor, factor);
        return this;
    }

    /// <summary>
    /// Sets non-uniform scaling factors for the X and Y axes.
    /// </summary>
    /// <param name="sx">The scaling factor for X.</param>
    /// <param name="sy">The scaling factor for Y.</param>
    public Container ScaleBy(float sx, float sy) {
        this.scaleX = sx;
        this.scaleY = sy;
        return this;
    }

    /// <summary>Sets the 0xRRGGBB color of the border around this container.</summary>
    public Container SetBorderColor(int borderColor) {
        if (border == null) {
            border = new Rect(0f, 0f, width, height);
            this.Add(border);
        }
        border.SetBorderColor(borderColor);
        return this;
    }

    /// <summary>Sets the color of the border around this container.</summary>
    /// <param name="rgbColor">the color as red, green and blue components from 0.0 to 1.0.</param>
    /// <returns>this Container object.</returns>
    public Container SetBorderColor(float[] rgbColor) {
        if (border == null) {
            border = new Rect(0f, 0f, width, height);
            this.Add(border);
        }
        border.SetBorderColor(rgbColor);
        return this;
    }

    /// <summary>Returns the elements in this container.</summary>
    internal List<IDrawable> GetElements() {
        return this.elements;
    }

    /// <summary>
    /// Adds a drawable element to this container.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <returns>this Container object.</returns>
    public Container Add(IDrawable element) {
        if (element is Container) {
            ((Container) element).parent = this;
        }
        this.elements.Add(element);
        return this;
    }

    internal static float[] RotateAroundCenter(float[] point, float[] center, double degrees) {
        double rad = degrees * Math.PI / 180.0; // convert to radians

        // translate to centre
        double dx = (double) (point[0] - center[0]);
        double dy = (double) (point[1] - center[1]);

        // rotate
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double dxRot =  dx * cos - dy * sin;
        double dyRot =  dx * sin + dy * cos;

        // translate back
        double nx = center[0] + dxRot;
        double ny = center[1] + dyRot;

        return new float[] {(float) nx, (float) ny};
    }

    /// <summary>
    /// Draws this container and its child elements onto the page.
    /// </summary>
    /// <param name="page">The <see cref="Page"/> to draw on.</param>
    /// <returns>An array containing the bottom-right position of the container.</returns>
    /// <exception cref="System.Exception">Thrown if drawing fails.</exception>
    public float[] DrawOn(Page page) {
        if (page == null || scaleX == 0f || scaleY == 0f) {
            return new float[] { this.x + width, this.y + height };  // Measured, or nothing to paint.
        }
        page.SaveGraphicsState();

        // 1) Translate container to its final position on the page
        //    This is logically the last transformation, but in PDF it’s applied first
        page.Append("1 0 0 1 ");
        page.Append(this.x);
        page.Append(' ');
        page.Append(-this.y);
        page.Append(" cm\n");

        float cx = width / 2f;
        float cy = height / 2f;

        // 2) Move origin to the center of the container
        //    Needed for rotation and scaling
        //    This transformation is applied on top of the previous translation
        page.Append("1 0 0 1 ");
        page.Append(cx);
        page.Append(' ');
        page.Append(page.height - cy);
        page.Append(" cm\n");

        // 3) Rotate around the container center
        double rad = rotateDegrees * (Math.PI / 180.0);
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);
        page.Append(cos);
        page.Append(' ');
        page.Append(sin);
        page.Append(' ');
        page.Append(-sin);
        page.Append(' ');
        page.Append(cos);
        page.Append(" 0 0 cm\n");

        // 4) Scale around the container center
        page.Append(scaleX);
        page.Append(' ');
        page.Append('0');
        page.Append(' ');
        page.Append('0');
        page.Append(' ');
        page.Append(scaleY);
        page.Append(' ');
        page.Append('0');
        page.Append(' ');
        page.Append('0');
        page.Append(" cm\n");

        // 5) Move origin back so children can draw using local coordinates (0,0)
        //    Executed last in the stream, but logically the first transformation for child drawing
        page.Append("1 0 0 1 ");
        page.Append(-cx);
        page.Append(' ');
        page.Append(-(page.height - cy));
        page.Append(" cm\n");

        // 6) Draw children elements
        foreach (IDrawable element in elements) {
            if (element is BaseAnnotation) {
                BaseAnnotation annot = (BaseAnnotation) element;
                // The corners of the annotation are moved and turned for
                // this drawing and put back after it, so that the container
                // can be drawn again: the annotation of the second drawing
                // was moved by the location of the container once more, and
                // ended up that far from what the container drew.
                float[] point1 = Util.CopyOf(annot.point1);
                float[] point2 = Util.CopyOf(annot.point2);
                float[] vertices = (annot.vertices == null) ? null : Util.CopyOf(annot.vertices);
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
                annot.Rotate(-rotateDegrees);
                element.DrawOn(page);
                annot.point1 = point1;
                annot.point2 = point2;
                annot.vertices = vertices;
                continue;
            }
            element.DrawOn(page);
        }

        page.RestoreGraphicsState();

        // Return bottom-right position of container
        return new float[] { this.x + width, this.y + height };
    }
}
}
