/*
 * Paragraph.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
/// </summary>
public class Paragraph {
    internal float xText;
    internal float yText;
    internal float x1;
    internal float y1;
    internal float x2;
    internal float y2;
    internal List<TextLine> lines = null;
    internal Alignment alignment = Alignment.LEFT;
    // True after SetTextAlignment. Otherwise the alignment of the text column applies.
    internal bool explicitAlignment = false;
    // The structure element of the paragraph in a PDF/UA document, which is a
    // paragraph unless it is set to a heading; see SetStructureType.
    internal StructElem structureType = StructElem.P;
    // The label of a paragraph that is an item of a list, and how far to the
    // left of the text it is drawn; see SetListLabel.
    internal TextLine listLabel = null;
    internal float listLabelIndent = 0f;

    /// <summary>
    /// Makes this paragraph an item of a list, labelled by the text line, which is
    /// drawn indent points to the left of the text of the paragraph and on the
    /// baseline of its first line. A run of paragraphs that have a label is a list:
    /// in a PDF/UA document it is an L of an LI for each paragraph, each holding the
    /// Lbl of its label and the LBody of its text, so that a reader reads the label
    /// of an item before the item, however the two are drawn.
    /// </summary>
    public Paragraph SetListLabel(TextLine label, float indent) {
        this.listLabel = label;
        this.listLabelIndent = indent;
        return this;
    }

    /// <summary>
    /// Sets the structure element type of this paragraph, for a PDF/UA document:
    /// StructElem.H1 to StructElem.H6 for a heading, and StructElem.P, which it is,
    /// for a paragraph. The paragraph is one element however many lines and words it
    /// is drawn in.
    /// </summary>
    public Paragraph SetStructureType(StructElem structureType) {
        this.structureType = structureType;
        return this;
    }

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
    /// Sets the alignment of the text in this paragraph. A paragraph with no
    /// alignment set takes the alignment of the text column it is drawn in.
    /// </summary>
    /// <param name="alignment">the alignment code: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER or Alignment.JUSTIFY.</param>
    /// <returns>this paragraph.</returns>
    public Paragraph SetTextAlignment(Alignment alignment) {
        this.alignment = alignment;
        this.explicitAlignment = true;
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
    public Paragraph SetTextColor(int color) {
        foreach (TextLine line in lines) {
            line.SetTextColor(color);
        }
        return this;
    }

    /// <summary>Sets the text color of all lines in this paragraph.</summary>
    /// <param name="rgbColor">the color as red, green and blue components from 0.0 to 1.0.</param>
    /// <returns>this Paragraph object.</returns>
    public Paragraph SetTextColor(float[] rgbColor) {
        foreach (TextLine line in lines) {
            line.SetTextColor(rgbColor);
        }
        return this;
    }

    /// <summary>Sets the word highlight colors of all lines in this paragraph.</summary>
    public Paragraph SetHighlightColors(Dictionary<string, int> colorMap) {
        foreach (TextLine line in lines) {
            line.SetHighlightColors(colorMap);
        }
        return this;
    }

    /// <summary>Reads a text file and returns its paragraphs. An empty line separates the paragraphs.</summary>
    public static List<Paragraph> ParagraphsFromFile(Font f1, String filePath) {
        List<Paragraph> paragraphs = new List<Paragraph>();
        String contents = Content.OfTextFile(filePath);
        Paragraph paragraph = new Paragraph();
        TextLine textLine = new TextLine(f1);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < contents.Length; i++) {
            char ch = contents[i];
            // We need at least one character after the \n\n to begin new paragraph!
            if (i < (contents.Length - 2) &&
                    ch == '\n' && contents[i + 1] == '\n') {
                textLine.SetText(sb.ToString());
                paragraph.Add(textLine);
                paragraphs.Add(paragraph);
                paragraph = new Paragraph();
                textLine = new TextLine(f1);
                sb.Length = 0;
                i += 1;
            } else {
                sb.Append(ch);
            }
        }
        if (sb.Length > 0) {
            textLine.SetText(sb.ToString());
            paragraph.Add(textLine);
            paragraphs.Add(paragraph);
        }
        return paragraphs;
    }

    /// <summary>Returns the x coordinate where the text of this paragraph starts, once a TextFrame or TextColumn has drawn it.</summary>
    public float GetTextX() {
        return xText;
    }

    /// <summary>Returns the baseline y coordinate of the first line of this paragraph, once a TextFrame or TextColumn has drawn it.</summary>
    public float GetTextY() {
        return yText;
    }

    /// <summary>Returns the x coordinate of the top left corner of this paragraph, once a TextFrame or TextColumn has drawn it.</summary>
    public float GetX1() {
        return x1;
    }

    /// <summary>Returns the y coordinate of the top left corner of this paragraph, once a TextFrame or TextColumn has drawn it.</summary>
    public float GetY1() {
        return y1;
    }

    /// <summary>Returns the x coordinate where the last line of this paragraph ends, once a TextFrame or TextColumn has drawn it.</summary>
    public float GetX2() {
        return x2;
    }

    /// <summary>Returns the y coordinate of the bottom of the last line of this paragraph, once a TextFrame or TextColumn has drawn it.</summary>
    public float GetY2() {
        return y2;
    }
}   // End of Paragraph.cs
}   // End of namespace PDFjet.NET
