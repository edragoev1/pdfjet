/**
 * BaseAnnotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

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

    public BaseAnnotation() {
    }

    public void setLocation(float x, float y) {
        this.point1 = new float[] {x, y};
    }

    public void setPosition(float x, float y) {
        this.point1 = new float[] {x, y};
    }

    public void setSize(float w, float h) {
        this.point2 = new float[] {point1[0] + w, point1[1] + h};
    }

    public void setFillColor(float[] fillColor) {
        this.fillColor = fillColor;
    }

    public void setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setFillColor(new float[] {r, g, b});
    }

    public void setTransparency(float transparency) {
        this.transparency = transparency;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public void setContents(String contents) {
        this.contents = contents;
    }

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
