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

    // The icons that the appearances of a file attachment and of a note draw
    // in a box of 1 by 1, which is scaled to the rectangle of the annotation,
    // as a viewer draws them: a push pin, a paperclip, and a speech bubble
    // with two lines of text for a note.
    static final String PUSH_PIN_ICON =
            "0 g\n0.34 0.7 0.32 0.1 re\n0.42 0.5 0.16 0.2 re\n0.3 0.44 0.4 0.06 re\nf\n" +
            "0.04 w 1 J\n0.5 0.44 m\n0.5 0.16 l\nS\n";
    static final String PAPERCLIP_ICON =
            "0.04 w 1 J 1 j\n0.44 0.62 m\n0.44 0.34 l\n0.44 0.26 0.56 0.26 0.56 0.34 c\n" +
            "0.56 0.76 l\n0.56 0.88 0.32 0.88 0.32 0.76 c\n0.32 0.28 l\n0.32 0.12 0.68 0.12 0.68 0.28 c\n" +
            "0.68 0.66 l\nS\n";
    static final String NOTE_ICON =
            "0.04 w 1 j\n0.2 0.8 m\n0.8 0.8 l\n0.8 0.4 l\n0.48 0.4 l\n0.3 0.22 l\n0.34 0.4 l\n" +
            "0.2 0.4 l\nh\n0.3 0.66 m\n0.7 0.66 l\n0.3 0.53 m\n0.7 0.53 l\nS\n";

    int objNumber;
    String annotationType = null;
    float x1 = 0f;
    float y1 = 0f;
    float x2 = 0f;
    float y2 = 0f;
    float[] vertices = null;
    float[] fillColor = null;
    float opacity = 0f;
    String title = null;
    String contents = null;
    String uri = null;
    String key = null;
    String language = null;
    String actualText = null;
    String altDescription = null;
    FileAttachment fileAttachment = null;
    // The Link element of the content the link is drawn on, which the
    // annotation joins, so that the text of a link and its annotation are one
    // element, as PDF/UA asks; null for a link over content that is not tagged
    StructElement linkElement = null;
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
            float opacity,
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
        this.opacity = opacity;
        this.title = title;
        this.contents = contents;
        this.uri = uri;
        this.key = key;
        this.language = language;
        // A link created from a destination name has no uri to fall back on,
        // so use the name itself rather than leaving the link undescribed.
        String fallback = !Util.isEmpty(uri) ? uri : key;
        this.actualText = Util.isEmpty(actualText) ? fallback : actualText;
        this.altDescription = Util.isEmpty(altDescription) ? fallback : altDescription;
    }
}
