/*
 * Annotation.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create PDF annotation objects.
/// </summary>
internal class Annotation {
    public static readonly String Link = "Link";
    public static readonly String FileAttachment = "FileAttachment";
    public static readonly String Polygon = "Polygon";
    public static readonly String Circle = "Circle";
    public static readonly String Square = "Square";
    public static readonly String Text = "Text";

    // The icons that the appearances of a file attachment and of a note draw
    // in a box of 1 by 1, which is scaled to the rectangle of the annotation,
    // as a viewer draws them: a push pin, a paperclip, and a speech bubble
    // with two lines of text for a note.
    internal const String PushPinIcon =
            "0 g\n0.34 0.7 0.32 0.1 re\n0.42 0.5 0.16 0.2 re\n0.3 0.44 0.4 0.06 re\nf\n" +
            "0.04 w 1 J\n0.5 0.44 m\n0.5 0.16 l\nS\n";
    internal const String PaperclipIcon =
            "0.04 w 1 J 1 j\n0.44 0.62 m\n0.44 0.34 l\n0.44 0.26 0.56 0.26 0.56 0.34 c\n" +
            "0.56 0.76 l\n0.56 0.88 0.32 0.88 0.32 0.76 c\n0.32 0.28 l\n0.32 0.12 0.68 0.12 0.68 0.28 c\n" +
            "0.68 0.66 l\nS\n";
    internal const String NoteIcon =
            "0.04 w 1 j\n0.2 0.8 m\n0.8 0.8 l\n0.8 0.4 l\n0.48 0.4 l\n0.3 0.22 l\n0.34 0.4 l\n" +
            "0.2 0.4 l\nh\n0.3 0.66 m\n0.7 0.66 l\n0.3 0.53 m\n0.7 0.53 l\nS\n";

    internal int objNumber;
    internal String annotationType = null;
    internal float x1 = 0f;
    internal float y1 = 0f;
    internal float x2 = 0f;
    internal float y2 = 0f;
    internal float[] vertices = null;
    internal float[] fillColor = null;
    internal float opacity = 0f;
    internal String title = null;
    internal String contents = null;
    internal String uri = null;
    internal String key = null;
    internal String language = null;
    internal String actualText = null;
    internal String altDescription = null;
    internal FileAttachment fileAttachment = null;
    // The Link element of the content the link is drawn on, which the
    // annotation joins, so that the text of a link and its annotation are one
    // element, as PDF/UA asks; null for a link over content that is not tagged
    internal StructElement linkElement = null;
    // Set once the annotation has been written with a /StructParent key.
    internal bool structParentWritten = false;

    /// <summary>
    /// This class is used to create annotation objects.
    /// </summary>
    /// <param name="annotationType">the annotation type.</param>
    /// <param name="x1">the x coordinate of the top left corner.</param>
    /// <param name="y1">the y coordinate of the top left corner.</param>
    /// <param name="x2">the x coordinate of the bottom right corner.</param>
    /// <param name="y2">the y coordinate of the bottom right corner.</param>
    /// <param name="vertices">the polygon annotation vertices.</param>
    /// <param name="fillColor">the fill color as an RGB array.</param>
    /// <param name="opacity">the opacity, from 0.0 to 1.0.</param>
    /// <param name="title">the title.</param>
    /// <param name="contents">the text contents.</param>
    /// <param name="uri">the URI string.</param>
    /// <param name="key">the destination name.</param>
    /// <param name="language">the language of the annotation.</param>
    /// <param name="actualText">the actual text.</param>
    /// <param name="altDescription">the alternate description.</param>
    internal Annotation(
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
        String fallback = !String.IsNullOrEmpty(uri) ? uri : key;
        this.actualText = String.IsNullOrEmpty(actualText) ? fallback : actualText;
        this.altDescription = String.IsNullOrEmpty(altDescription) ? fallback : altDescription;
    }
}
}   // End of namespace PDFjet.NET
