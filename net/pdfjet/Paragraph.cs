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
    /// <summary>The x coordinate where the text of this paragraph starts.</summary>
    public float xText;
    /// <summary>The baseline y coordinate of the first line of this paragraph.</summary>
    public float yText;
    /// <summary>The x coordinate of the top left corner of this paragraph.</summary>
    public float x1;
    /// <summary>The y coordinate of the top left corner of this paragraph.</summary>
    public float y1;
    /// <summary>The x coordinate where the last line of this paragraph ends.</summary>
    public float x2;
    /// <summary>The y coordinate of the bottom of the last line of this paragraph.</summary>
    public float y2;
    internal List<TextLine> lines = null;
    internal uint alignment = Align.LEFT;

    /// <summary>
    /// Constructor for creating paragraph objects.
    /// </summary>
    public Paragraph() {
        this.lines = new List<TextLine>();
    }

    /// <summary>Creates a paragraph with the specified text line.</summary>
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
    /// <param name="alignment">the alignment code: Align.LEFT, Align.RIGHT, Align.CENTER or Align.JUSTIFY.</param>
    /// <returns>this paragraph.</returns>
    public Paragraph SetTextAlignment(uint alignment) {
        this.alignment = alignment;
        return this;
    }

    /// <summary>Returns the text lines of this paragraph.</summary>
    public List<TextLine> GetTextLines() {
        return lines;
    }

    /// <summary>Returns true if the first line of this paragraph starts with the specified token.</summary>
    public bool StartsWith(string token) {
        return lines[0].GetText().StartsWith(token);
    }

    /// <summary>Sets the text color of all lines in this paragraph as a 0xRRGGBB value.</summary>
    public Paragraph SetColor(int color) {
        foreach (TextLine line in lines) {
            line.SetTextColor(color);
        }
        return this;
    }

    /// <summary>Sets the word highlight colors of all lines in this paragraph.</summary>
    public Paragraph SetColorMap(Dictionary<string, int> colorMap) {
        foreach (TextLine line in lines) {
            line.SetColorMap(colorMap);
        }
        return this;
    }
}   // End of Paragraph.cs
}   // End of namespace PDFjet.NET
