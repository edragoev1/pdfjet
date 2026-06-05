/**
 * Paragraph.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;

/**
 * Used to create paragraph objects.
 * See the TextColumn class for more information.
 */
namespace PDFjet.NET {
public class Paragraph {
    public float xText;
    public float yText;
    public float x1;
    public float y1;
    public float x2;
    public float y2;
    internal List<TextLine> lines = null;
    internal uint alignment = Align.LEFT;

    /**
     * Constructor for creating paragraph objects.
     */
    public Paragraph() {
        this.lines = new List<TextLine>();
    }

    public Paragraph(TextLine text) {
        this.lines = new List<TextLine>();
        this.lines.Add(text);
    }

    /**
     * Adds a text line to this paragraph.
     *
     * @param text the text line to add to this paragraph.
     * @return this paragraph.
     */
    public Paragraph Add(TextLine text) {
        lines.Add(text);
        return this;
    }

    /**
     * Sets the alignment of the text in this paragraph.
     *
     * @param alignment the alignment code.
     * @return this paragraph.
     * <pre>Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.</pre>
     */
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

    public void SetColor(int color) {
        foreach (TextLine line in lines) {
            line.SetTextColor(color);
        }
    }

    public void SetColorMap(Dictionary<string, int> colorMap) {
        foreach (TextLine line in lines) {
            line.SetColorMap(colorMap);
        }
    }
}   // End of Paragraph.cs
}   // End of namespace PDFjet.NET
