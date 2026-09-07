/*
 * Annotation.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to create PDF annotation objects.
 */
class Annotation {
    public static final String Link = "Link";
    public static final String FileAttachment = "FileAttachment";
    public static final String Polygon = "Polygon";
    public static final String Circle = "Circle";
    public static final String Square = "Square";
    public static final String Text = "Text";

    int objNumber;
    String annotationType = null;
    float x1 = 0f;
    float y1 = 0f;
    float x2 = 0f;
    float y2 = 0f;
    float[] vertices = null;
    float[] fillColor = null;
    float transparency = 0f;
    String title = null;
    String contents = null;
    String uri = null;
    String key = null;
    String language = null;
    String actualText = null;
    String altDescription = null;
    FileAttachment fileAttachment = null;
    // Set once the annotation has been written with a /StructParent key.
    boolean structParentWritten = false;

    /**
     *  This class is used to create annotation objects.
     *
     *  @param annotationType the annotation type.
     *  @param x1 the x coordinate of the top left corner.
     *  @param y1 the y coordinate of the top left corner.
     *  @param x2 the x coordinate of the bottom right corner.
     *  @param y2 the y coordinate of the bottom right corner.
     *  @param vertices the polygon annotation vertices.
     *  @param uri the URI string.
     *  @param key the destination name.
     */
    Annotation(
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
        // A link created from a destination name has no uri to fall back on,
        // so use the name itself rather than leaving the link undescribed.
        String fallback = (uri != null) ? uri : key;
        this.actualText = (actualText == null) ? fallback : actualText;
        this.altDescription = (altDescription == null) ? fallback : altDescription;
    }
}
