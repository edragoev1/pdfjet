/*
 * TextColumn.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A column of paragraphs, each a list of TextLine objects that can differ in
/// font, size and color, aligned left, right, center or justified, with a line
/// spacing, a paragraph spacing and an optional line between the paragraphs.
/// It draws all of its paragraphs where it is placed, top down; to rotate a
/// column, add it to a Container and rotate that.
///
/// Use a TextColumn for an article or a page of mixed text: bold or colored
/// words in a paragraph, justified paragraphs, CJK paragraphs. Use a TextBlock
/// for one run of text in one font, and a TextFrame when the text must
/// continue from one frame to the next, across columns or pages. Please see
/// Example_10, Example_29, Example_44 and Example_49.
/// </summary>
public class TextColumn : IDrawable {
    internal Alignment alignment = Alignment.LEFT;
    internal float x;   // This variable is set in the beginning and only reset after the DrawOn
    internal float y;   // This variable is set in the beginning and only reset after the DrawOn
    internal float w;
    internal float h;
    private float x1;
    private float y1;
    private float lineSpacing = 1.0f;
    private float paragraphSpacing = 1.0f;
    private List<Paragraph> paragraphs;
    private bool lineBetweenParagraphs = false;

    /// <summary>
    /// Create a text column object.
    /// </summary>
    public TextColumn() {
        this.paragraphs = new List<Paragraph>();
    }

    /// <summary>
    /// Sets the lineBetweenParagraphs private variable value.
    /// If the value is set to true - an empty line will be inserted between the current and next paragraphs.
    /// </summary>
    /// <param name="lineBetweenParagraphs">the specified bool value.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetLineBetweenParagraphs(bool lineBetweenParagraphs) {
        this.lineBetweenParagraphs = lineBetweenParagraphs;
        return this;
    }

    /// <summary>Sets the spacing between the lines in this text column.</summary>
    public TextColumn SetLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
        return this;
    }

    /// <summary>Sets the space between paragraphs.</summary>
    public TextColumn SetParagraphSpacing(float paragraphSpacing) {
        this.paragraphSpacing = paragraphSpacing;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location of this text column on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner.</param>
    /// <param name="y">the y coordinate of the top left corner.</param>
    public TextColumn SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    /// Sets the desired width of this text column.
    /// </summary>
    /// <param name="w">the width of this text column.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetWidth(float w) {
        this.w = w;
        return this;
    }

    /// <summary>Returns the width of this text column.</summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>Sets the height of this text column.</summary>
    public TextColumn SetHeight(float h) {
        this.h = h;
        return this;
    }

    /// <summary>Returns the height of this text column.</summary>
    public float GetHeight() {
        return this.h;
    }

    /// <summary>
    /// Sets the text alignment.
    /// </summary>
    /// <param name="alignment">the specified alignment code.
    ///      Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetTextAlignment(Alignment alignment) {
        this.alignment = alignment;
        return this;
    }

    /// <summary>
    /// Adds a new paragraph to this text column.
    /// </summary>
    /// <param name="paragraph">the new paragraph object.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn AddParagraph(Paragraph paragraph) {
        this.paragraphs.Add(paragraph);
        return this;
    }

    /// <summary>
    /// Removes the last paragraph added to this text column.
    /// </summary>
    public TextColumn RemoveLastParagraph() {
        if (this.paragraphs.Count >= 1) {
            this.paragraphs.RemoveAt(this.paragraphs.Count - 1);
        }
        return this;
    }

    /// <summary>
    /// Returns dimension object containing the width and height of this component.
    /// Please see Example_29.
    /// </summary>
    /// <returns>dimension object containing the width and height of this component.</returns>
    public Dimension GetSize() {
        float[] xy = DrawOn(null);
        return new Dimension(this.w, xy[1] - this.y);
    }

    /// <summary>
    /// Draws this text column on the specified page.
    /// With no page nothing is drawn and the location of the next component is computed.
    /// </summary>
    /// <param name="page">the page to draw this text column on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of this text column.</returns>
    public float[] DrawOn(Page page) {
        float[] xy = new float[] {x, y};
        for (int i = 0; i < paragraphs.Count; i++) {
            xy = DrawParagraphOn(page, paragraphs[i], i == (paragraphs.Count - 1));
        }
        // Restore the original location
        SetLocation(this.x, this.y);
        // A column with a height reaches at least that far down from its location
        if (y + h > xy[1]) {
            xy[1] = y + h;
        }
        return new float[] {x + w, xy[1]};
    }

    private float[] DrawParagraphOn(Page page, Paragraph paragraph, bool lastParagraph) {
        Alignment alignment = paragraph.explicitAlignment ? paragraph.alignment : this.alignment;
        List<TextLine> list = new List<TextLine>();
        float lineHeight = 0f;
        float maxAscent = 0f;
        float maxDescent = 0f;
        foreach (TextLine line in paragraph.lines) {
            if ((line.GetHeight() * lineSpacing) > lineHeight) {
                lineHeight = line.GetHeight() * lineSpacing;
            }
            if (line.font.GetAscent(line.fontSize) > maxAscent) {
                maxAscent = line.font.GetAscent(line.fontSize);
            }
            if (line.font.GetDescent(line.fontSize) > maxDescent) {
                maxDescent = line.font.GetDescent(line.fontSize);
            }
        }
        y1 += maxAscent;

        float runLength = 0f;
        foreach (TextLine line in paragraph.lines) {
            String[] tokens = Util.SplitOnWhitespace(line.text ?? "");
            foreach (String token in tokens) {
                TextLine text = line.CopyWithText(token + Single.space);
                // The token is measured without the space that follows it: a
                // line is as wide as the text it shows. A token wider than the
                // column goes on a line of its own rather than after an empty
                // one, which would leave the line above it blank.
                if (list.Count == 0 || (runLength + Width(text, token)) <= this.w) {
                    list.Add(text);
                    runLength += text.GetWidth();
                } else {
                    DrawLineOfText(page, list, alignment);
                    MoveToNextLine(lineHeight);
                    list.Clear();
                    list.Add(text);
                    runLength = text.GetWidth();
                }
            }
        }
        // The last line of a paragraph is not justified.
        DrawNonJustifiedLine(page, list, alignment);

        // The paragraph reaches down to the descent of its last line. The
        // spacing and the blank line go between the paragraphs, not after the
        // last one.
        if (lastParagraph) {
            return MoveToNextParagraph(maxDescent);
        }
        if (lineBetweenParagraphs) {
            MoveToNextLine(lineHeight);
        }
        return MoveToNextParagraph(lineHeight * this.paragraphSpacing);
    }

    // The last token of a line drawn on the page ends the line: its underline
    // and its strikeout stop at its text, not after the space that follows it.
    private static void MarkLastToken(List<TextLine> list) {
        if (list.Count > 0) {
            list[list.Count - 1].isLastToken = true;
        }
    }

    // The width of the text the line shows: every token with the space after
    // it, and the last token without it.
    private static float VisibleWidth(List<TextLine> list) {
        float runLength = 0f;
        for (int i = 0; i < list.Count; i++) {
            TextLine textLine = list[i];
            runLength += (i == (list.Count - 1))
                    ? Width(textLine, TrimTrailingSpaces(textLine.text))
                    : textLine.GetWidth();
        }
        return runLength;
    }

    private static float Width(TextLine textLine, String text) {
        return textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, text);
    }

    private static String TrimTrailingSpaces(String text) {
        int end = text.Length;
        while (end > 0 && text[end - 1] == ' ') {
            end--;
        }
        return text.Substring(0, end);
    }

    private float[] MoveToNextLine(float lineHeight) {
        x1 = x;
        y1 += lineHeight;
        return new float[] {x1, y1};
    }

    private float[] MoveToNextParagraph(float paragraphSpacing) {
        x1 = x;
        y1 += paragraphSpacing;
        return new float[] {x1, y1};
    }

    private void DrawLineOfText(Page page, List<TextLine> list, Alignment alignment) {
        if (alignment == Alignment.JUSTIFY) {
            MarkLastToken(list);
            // The spaces are widened so that the text of the line reaches both
            // edges. A line of one token has no space to widen.
            float dx = (list.Count > 1) ? (w - VisibleWidth(list)) / (list.Count - 1) : 0f;
            // Each token draws its own link annotation when the line has a URI or GoTo action.
            foreach (TextLine textLine in list) {
                textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());
                textLine.DrawOn(page);
                x1 += textLine.GetWidth() + dx;
            }
        } else {
            DrawNonJustifiedLine(page, list, alignment);
        }
    }

    private void DrawNonJustifiedLine(Page page, List<TextLine> list, Alignment alignment) {
        MarkLastToken(list);
        float runLength = VisibleWidth(list);

        if (alignment == Alignment.CENTER) {
            x1 = x + ((w - runLength) / 2);
        } else if (alignment == Alignment.RIGHT) {
            x1 = x + (w - runLength);
        }

        // Each token draws its own link annotation when the line has a URI or GoTo action.
        foreach (TextLine textLine in list) {
            textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());
            textLine.DrawOn(page);
            x1 += textLine.GetWidth();
        }
    }

    /// <summary>
    /// Adds a paragraph of Chinese, Japanese or Korean text to this text column,
    /// wrapped at any character to the width of the column.
    /// </summary>
    /// <param name="font">the font used by this paragraph.</param>
    /// <param name="text">the text.</param>
    public TextColumn AddCJKParagraph(Font font, String text) {
        Paragraph paragraph;
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < text.Length; i += Util.CharCount(text, i)) {
            String ch = text.Substring(i, Util.CharCount(text, i));
            if (font.StringWidth(buf.ToString() + ch) > w) {
                paragraph = new Paragraph();
                paragraph.Add(new TextLine(font, buf.ToString()));
                AddParagraph(paragraph);
                buf.Length = 0;
            }
            buf.Append(ch);
        }
        paragraph = new Paragraph();
        paragraph.Add(new TextLine(font, buf.ToString()));
        return AddParagraph(paragraph);
    }
}   // End of TextColumn.cs
}   // End of namespace PDFjet.NET
