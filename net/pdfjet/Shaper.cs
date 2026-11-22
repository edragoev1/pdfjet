/*
 * Shaper.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A text engine that shapes text for ComplexText: it wraps the text to a
/// width and finds, for each line, the glyphs of the fonts and their places.
/// PDFjet does not shape scripts such as Hindi and Khmer itself; a shaper
/// does, and ComplexText draws what it found. .NET has no text engine of its
/// own that gives glyphs: the NuGet package PDFjet.HarfBuzz gives ComplexText
/// HarfBuzz, nothing to install; SetShaper gives another, such as HBShape,
/// which runs HarfBuzz's program hb-shape, used only when set.
/// </summary>
public interface IShaper {
    /// <summary>
    /// Wraps the text to the width and shapes each line.
    /// </summary>
    /// <param name="request">the text, its language, the font files, the
    /// font size and the width of the lines.</param>
    /// <returns>the lines, in order.</returns>
    List<ShapedLine> Shape(ShapingRequest request);
}

/// <summary>
/// What a shaper is asked to shape: the text, its language, the font files,
/// the font size and the width of the lines. One object, so that what a
/// shaper is told can grow without changing the IShaper interface.
/// </summary>
public sealed class ShapingRequest {
    /// <summary>The text.</summary>
    public String Text { get; }
    /// <summary>The language of the text, such as "hi" or "km", or null.</summary>
    public String Language { get; }
    /// <summary>The bytes of the font files: the first for the text, the
    /// others, in order, for the characters it does not have.</summary>
    public byte[][] FontFiles { get; }
    /// <summary>The font size, in points.</summary>
    public float FontSize { get; }
    /// <summary>The width of the lines, in points.</summary>
    public float Width { get; }

    /// <summary>A request to shape text.</summary>
    public ShapingRequest(String text, String language, byte[][] fontFiles, float fontSize, float width) {
        Text = text;
        Language = language;
        FontFiles = fontFiles;
        FontSize = fontSize;
        Width = width;
    }
}

/// <summary>
/// A line of text a shaper shaped: the part of the text it holds, and its
/// glyphs, each with its font and its place.
/// </summary>
public sealed class ShapedLine {
    /// <summary>The index in the text of the line's first character.</summary>
    public int Start { get; }
    /// <summary>The index in the text after the line's last character.</summary>
    public int End { get; }
    /// <summary>For each glyph, the index of its font among the font files the
    /// shaper was given, or -1 for a glyph of none of them, which is not drawn.</summary>
    public int[] FontIndexes { get; }
    /// <summary>The glyph numbers, in their fonts.</summary>
    public int[] Glyphs { get; }
    /// <summary>The x and the y of each glyph, in points, from the left end of
    /// the line at its baseline, y growing down: x0, y0, x1, y1 and so on.</summary>
    public float[] Positions { get; }
    /// <summary>The width of the line, in points, without the white space at
    /// its end: what is centered, aligned to the right or justified.</summary>
    public float Width { get; }

    /// <summary>A shaped line.</summary>
    public ShapedLine(int start, int end, int[] fontIndexes, int[] glyphs, float[] positions, float width) {
        if (fontIndexes.Length != glyphs.Length || positions.Length != 2 * glyphs.Length) {
            throw new ArgumentException("A shaped line needs a font and an x and a y for each glyph.");
        }
        Start = start;
        End = end;
        FontIndexes = fontIndexes;
        Glyphs = glyphs;
        Positions = positions;
        Width = width;
    }
}
}   // End of namespace PDFjet.NET
