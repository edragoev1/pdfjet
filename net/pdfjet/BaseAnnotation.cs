/*
 * BaseAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>The base class of the circle, square, polygon and text annotations.</summary>
public abstract class BaseAnnotation : IDrawable {
    internal String annotationType = null;
    internal float[] point1 = new float[] {0f, 0f};
    internal float[] vertices = null;
    internal float[] fillColor = new float[] {0.5f, 0.5f, 0.5f};
    internal float opacity = 1f;
    internal String title = null;
    internal String contents = null;
    internal String uri = null;
    internal String key = null;
    internal String language = null;
    internal String actualText = null;
    internal String altDescription = null;
    // The size SetSize sets, which the second point is at from the first when
    // the annotation is drawn, wherever SetLocation puts the first.
    internal float width = 0f;
    internal float height = 0f;
    internal bool hasSize = false;

    /// <summary>Creates an annotation. The circle, square, polygon and text annotations call it.</summary>
    protected BaseAnnotation() {
    }

    /// <summary>Sets the location of this annotation.</summary>
    public BaseAnnotation SetLocation(float x, float y) {
        this.point1 = new float[] {x, y};
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the size of this annotation, measured from its location: its
    /// second point is w to the right of its location and h below it, whether
    /// SetLocation is called before or after.</summary>
    public BaseAnnotation SetSize(float w, float h) {
        this.width = w;
        this.height = h;
        this.hasSize = true;
        return this;
    }

    // Returns the second point of the annotation, which is the origin of the
    // page until SetSize is called.
    private float[] GetCorner() {
        if (!hasSize) {
            return new float[] {0f, 0f};
        }
        return new float[] {point1[0] + width, point1[1] + height};
    }

    /// <summary>Sets the fill color from an array of red, green and blue values.</summary>
    public BaseAnnotation SetFillColor(float[] fillColor) {
        this.fillColor = Util.CopyOf(fillColor);
        return this;
    }

    /// <summary>Sets the fill color as a 0xRRGGBB value. Color.transparent leaves it unchanged.</summary>
    public BaseAnnotation SetFillColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetFillColor(new float[] {r, g, b});
        return this;
    }

    /// <summary>Sets the opacity of this annotation, from 0.0 (invisible) to 1.0 (opaque, the default).</summary>
    public BaseAnnotation SetOpacity(float opacity) {
        this.opacity = opacity;
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

    /// <summary>Adds this annotation to the specified page.</summary>
    public float[] DrawOn(Page page) {
        float[] point2 = GetCorner();
        if (page == null) {
            return point2;  // Measured, not drawn
        }
        page.AddAnnotation(new Annotation(
                annotationType,
                point1[0],
                point1[1],
                point2[0],
                point2[1],
                // A copy, which the annotation keeps until the page is written
                (vertices == null) ? null : Util.CopyOf(vertices),
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
}
