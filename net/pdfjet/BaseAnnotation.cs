/**
 * BaseAnnotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
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

    public BaseAnnotation() {
    }

    public void SetLocation(float x, float y) {
        this.point1 = new float[] {x, y};
    }

    public void SetPosition(float x, float y) {
        this.point1 = new float[] {x, y};
    }

    public void SetSize(float w, float h) {
        this.point2 = new float[] {point1[0] + w, point1[1] + h};
    }

    public void SetFillColor(float[] fillColor) {
        this.fillColor = fillColor;
    }

    public void SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetFillColor(new float[] {r, g, b});
    }

    public void SetTransparency(float transparency) {
        this.transparency = transparency;
    }

    public void SetTitle(String title) {
        this.title = title;
    }

    public void SetContents(String contents) {
        this.contents = contents;
    }

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
        return new float[] {point2[0], point2[1]};
    }
}
}
