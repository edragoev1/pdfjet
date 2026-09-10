/*
 * Paragraph.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
/// </summary>
public class Paragraph {
    public float xText;
    public float yText;
    public float x1;
    public float y1;
    public float x2;
    public float y2;
    internal List<TextLine> lines = null;
    internal uint alignment = Align.LEFT;

    /// <summary>
    /// Constructor for creating paragraph objects.
    /// </summary>
    public Paragraph() {
        this.lines = new List<TextLine>();
    }

    public Paragraph(TextLine text) {
        this.lines = new List<TextLine>();
        this.lines.Add(text);
    }

    /// <summary>
    /// Adds a text line to this paragraph.
    /// </summary>
    /// <param name="text">the text line to add to this paragraph.</param>
    /// <returns>this paragraph.</returns>
    public Paragraph Add(TextLine text) {
        lines.Add(text);
        return this;
    }

    /// <summary>
    /// Sets the alignment of the text in this paragraph.
    /// </summary>
    /// <param name="alignment">the alignment code.</param>
    /// <returns>this paragraph.
    /// <code>Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.</code></returns>
    public Paragraph SetAlignment(uint alignment) {
        this.alignment = alignment;
        return this;
    }

    public List<TextLine> GetTextLines() {
        return lines;
    }

    public bool StartsWith(string token) {
        return lines[0].GetText().StartsWith(token);
    }

    public Paragraph SetColor(int color) {
        foreach (TextLine line in lines) {
            line.SetTextColor(color);
        }
        return this;
    }

    public Paragraph SetColorMap(Dictionary<string, int> colorMap) {
        foreach (TextLine line in lines) {
            line.SetColorMap(colorMap);
        }
        return this;
    }
}   // End of Paragraph.cs
}   // End of namespace PDFjet.NET
