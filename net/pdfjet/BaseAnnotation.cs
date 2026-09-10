/*
 * BaseAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>The base class of the circle, square, polygon and text annotations.</summary>
public class BaseAnnotation : IDrawable {
    internal String annotationType = null;
    internal float[] point1 = new float[] {0f, 0f};
    internal float[] point2 = new float[] {0f, 0f};
    internal float[] vertices = null;
    internal float[] fillColor = new float[] {0.5f, 0.5f, 0.5f};
    internal float transparency = 1f;
    internal String title = null;
    internal String contents = null;
    internal String uri = null;
    internal String key = null;
    internal String language = null;
    internal String actualText = null;
    internal String altDescription = null;
    internal Container container = null;

    /// <summary>Creates an annotation.</summary>
    public BaseAnnotation() {
    }

    /// <summary>Sets the location of this annotation.</summary>
    public BaseAnnotation SetLocation(float x, float y) {
        this.point1 = new float[] {x, y};
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the size of this annotation, measured from its location.</summary>
    public BaseAnnotation SetSize(float w, float h) {
        this.point2 = new float[] {point1[0] + w, point1[1] + h};
        return this;
    }

    /// <summary>Sets the fill color from an array of red, green and blue values.</summary>
    public BaseAnnotation SetFillColor(float[] fillColor) {
        this.fillColor = fillColor;
        return this;
    }

    /// <summary>Sets the fill color as a 0xRRGGBB value.</summary>
    public BaseAnnotation SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetFillColor(new float[] {r, g, b});
        return this;
    }

    /// <summary>Sets the transparency of this annotation, from 0.0 to 1.0.</summary>
    public BaseAnnotation SetTransparency(float transparency) {
        this.transparency = transparency;
        return this;
    }

    /// <summary>Sets the title of this annotation.</summary>
    public BaseAnnotation SetTitle(String title) {
        this.title = title;
        return this;
    }

    /// <summary>Sets the text contents of this annotation.</summary>
    public BaseAnnotation SetContents(String contents) {
        this.contents = contents;
        return this;
    }

    /// <summary>Rotates this annotation together with the container it is in.</summary>
    public void Rotate(double degrees) {
        if (container == null) { return; }
        float[] center = container.GetRotationCenter();
        if (container.parent != null) {
            center[0] += container.parent.x;
            center[1] += container.parent.y;
        }
        point1 = Container.RotateAroundCenter(point1, center, degrees);
        point2 = Container.RotateAroundCenter(point2, center, degrees);
        if (annotationType.Equals(Annotation.Polygon)) {
            for (int i = 0; i < vertices.Length; i += 2) {
                float[] point = Container.RotateAroundCenter(
                    new float[] {vertices[i], vertices[i + 1]}, new float[] {0f, 0f}, degrees);
                vertices[i] = point[0];
                vertices[i + 1] = point[1];
            }
        }
    }

    /// <summary>Adds this annotation to the specified page.</summary>
    public float[] DrawOn(Page page) {
        page.AddAnnotation(new Annotation(
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
}
