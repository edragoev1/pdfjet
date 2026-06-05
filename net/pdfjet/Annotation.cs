/**
 * Annotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to create PDF annotation objects.
 */
namespace PDFjet.NET {
internal class Annotation {
    public static readonly String Link = "Link";
    public static readonly String FileAttachment = "FileAttachment";
    public static readonly String Polygon = "Polygon";
    public static readonly String Circle = "Circle";
    public static readonly String Square = "Square";
    public static readonly String Text = "Text";

    internal int objNumber;
    internal String annotationType = null;
    internal float x1 = 0f;
    internal float y1 = 0f;
    internal float x2 = 0f;
    internal float y2 = 0f;
    internal float[] vertices = null;
    internal float[] fillColor = null;
    internal float transparency = 0f;
    internal String title = null;
    internal String contents = null;
    internal String uri = null;
    internal String key = null;
    internal String language = null;
    internal String actualText = null;
    internal String altDescription = null;
    internal FileAttachment fileAttachment = null;

    /**
     * This class is used to create annotation objects.
     *
     * @param annotationType the annotation type.
     * @param x1 the x coordinate of the top left corner.
     * @param y1 the y coordinate of the top left corner.
     * @param x2 the x coordinate of the bottom right corner.
     * @param y2 the y coordinate of the bottom right corner.
     * @param vertices the polygon annotation vertices.
     * @param uri the URI string.
     * @param key the destination name.
     */
    internal Annotation(
            String annotationType,
            float x1,
            float y1,
            float x2,
            float y2,
            float[] vertices,
            float[] fillColor,
            float transparency,
            String title,
            String contents,
            String uri,
            String key,
            String language,
            String actualText,
            String altDescription) {
        this.annotationType = annotationType;
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
        this.vertices = vertices;
        this.fillColor = fillColor;
        this.transparency = transparency;
        this.title = title;
        this.contents = contents;
        this.uri = uri;
        this.key = key;
        this.language = language;
        this.actualText = (actualText == null) ? uri : actualText;
        this.altDescription = (altDescription == null) ? uri : altDescription;
    }
}
}   // End of namespace PDFjet.NET
