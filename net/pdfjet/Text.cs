/*
 * Text.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Paragraphs of text lines, wrapped at a width, with an optional border.
/// Please see Example_03, Example_41 and Example_49.
/// </summary>
public class Text : IDrawable {
    private List<Paragraph> paragraphs;
    private float x1;
    private float y1;
    private float width;
    private float xText;
    private float yText;
    private float paragraphLeading = 24f;
    private bool hasBorder = false;
    private float[] borderColor;
    private float borderWidth = 0.5f;
    private String borderPattern = "[] 0";

    /// <summary>Creates a text object from the paragraphs.</summary>
    public Text(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of this text.</summary>
    public Text SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>Sets the location of the top left corner of this text.</summary>
    public Text SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>Sets the width at which the lines wrap.</summary>
    public Text SetWidth(float width) {
        this.width = width;
        return this;
    }

    /// <summary>Sets the vertical distance between paragraphs.</summary>
    public Text SetParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    /// <summary>Sets the border width.</summary>
    public Text SetBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /// <summary>Sets the border color as a 0xRRGGBB value. Color.transparent removes the border.</summary>
    public Text SetBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            this.hasBorder = false;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetBorderColor(r, g, b);
        return this;
    }

    /// <summary>Sets the border color from red, green and blue values and draws a border around this text.</summary>
    public Text SetBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        this.hasBorder = true;
        return this;
    }

    /// <summary>Sets the border color from an array of red, green and blue values and draws a border around this text.</summary>
    public Text SetBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        this.hasBorder = true;
        return this;
    }

    /// <summary>Sets the dash pattern of the border.</summary>
    public Text SetBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
        return this;
    }

    /// <summary>
    /// Draws the paragraphs on the specified page. With no page nothing is drawn
    /// and the paragraphs get their coordinates.
    /// </summary>
    public float[] DrawOn(Page page) {
        TextLine firstLine = paragraphs[0].lines[0];
        this.xText = x1;
        this.yText = y1 + firstLine.font.GetAscent(firstLine.fontSize);
        foreach (Paragraph paragraph in paragraphs) {
            firstLine = paragraph.lines[0];
            paragraph.x1 = x1;
            paragraph.y1 = yText - firstLine.font.GetAscent(firstLine.fontSize);
            paragraph.xText = xText;
            paragraph.yText = yText;
            foreach (TextLine textLine in paragraph.lines) {
                float[] point = DrawTextLine(page, xText, yText, textLine);
                xText = point[0];
                yText = point[1];
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.GetDescent(textLine.fontSize);
            }
            xText = x1;
            yText += paragraphLeading;
        }

        Paragraph lastParagraph = paragraphs[paragraphs.Count - 1];
        TextLine lastTextLine = lastParagraph.GetTextLines()[lastParagraph.GetTextLines().Count - 1];
        float height = ((yText - paragraphLeading) - y1) + lastTextLine.font.GetDescent(lastTextLine.fontSize);
        if (hasBorder) {
            Rect rect = new Rect(x1, y1, width, height);
            rect.SetBorderColor(this.borderColor);
            rect.SetBorderWidth(this.borderWidth);
            rect.SetBorderPattern(this.borderPattern);
            rect.DrawOn(page);
        }

        return new float[] { x1 + width, y1 + height };
    }

    // The ASCII whitespace that Java's \s matches; a no-break space does not break a line.
    private static readonly char[] whitespace = new char[] {' ', '\t', '\n', '\x0B', '\f', '\r'};

    // Draws the text line, wrapping it at the width of this text, and returns where the next text starts.
    private float[] DrawTextLine(
            Page page, float x, float y, TextLine textLine) {
        this.xText = x;
        this.yText = y;

        String[] tokens = null;
        if (StringIsCJK(textLine.text)) {
            tokens = TokenizeCJK(textLine, this.width);
        } else {
            tokens = textLine.text.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
        }

        Font font = textLine.font;
        Font fallbackFont = textLine.fallbackFont;
        float fontSize = textLine.fontSize;
        StringBuilder buf = new StringBuilder();
        foreach (String token in tokens) {
            float runLength = font.StringWidth(fallbackFont, fontSize, buf.ToString());
            float tokenWidth = font.StringWidth(fallbackFont, fontSize, token + Single.space);
            if ((runLength + tokenWidth) < (this.x1 + this.width) - this.xText) {
                buf.Append(token + Single.space);
            } else {
                new TextLine(textLine.font, buf.ToString())
                        .SetFallbackFont(textLine.GetFallbackFont())
                        .SetFontSize(textLine.GetFontSize())
                        .SetTextColor(textLine.GetTextColor())
                        .SetColorMap(textLine.GetColorMap())
                        .SetUnderline(textLine.GetUnderline())
                        .SetStrikeout(textLine.GetStrikeout())
                        .SetLanguage(textLine.GetLanguage())
                        .SetLocation(xText, yText)
                        .DrawOn(page);
                xText = x1;
                yText += textLine.GetHeight();
                buf.Length = 0;
                buf.Append(token + Single.space);
            }
        }
        new TextLine(textLine.font, buf.ToString())
                .SetFallbackFont(textLine.fallbackFont)
                .SetFontSize(textLine.GetFontSize())
                .SetTextColor(textLine.GetTextColor())
                .SetColorMap(textLine.GetColorMap())
                .SetUnderline(textLine.GetUnderline())
                .SetStrikeout(textLine.GetStrikeout())
                .SetLanguage(textLine.GetLanguage())
                .SetLocation(xText, yText)
                .DrawOn(page);

        return new float[] {xText + font.StringWidth(fallbackFont, fontSize, buf.ToString()), yText};
    }

    private bool StringIsCJK(String str) {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        int numOfCJK = 0;
        foreach (char ch in str) {
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK += 1;
            }
        }
        return (numOfCJK > (str.Length / 2));
    }

    private String[] TokenizeCJK(TextLine textLine, float textWidth) {
        List<String> list = new List<String>();
        StringBuilder buf = new StringBuilder();
        foreach (char ch in textLine.text) {
            if (textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, buf.ToString() + ch) < textWidth) {
                buf.Append(ch);
            } else {
                list.Add(buf.ToString());
                buf.Length = 0;
                buf.Append(ch);
            }
        }
        if (buf.ToString().Length > 0) {
            list.Add(buf.ToString());
        }
        return list.ToArray();
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
        if (!sb.ToString().Equals("")) {
            textLine.SetText(sb.ToString());
            paragraph.Add(textLine);
            paragraphs.Add(paragraph);
        }
        return paragraphs;
    }

    /// <summary>Reads the lines of a UTF-8 text file, without carriage returns.</summary>
    public static List<String> ReadLines(String filePath) {
        List<String> lines = new List<String>();
        String contents = Content.OfTextFile(filePath);
        StringBuilder buffer = new StringBuilder();
        foreach (char ch in contents) {
            if (ch == '\n') {
                lines.Add(buffer.ToString());
                buffer.Length = 0;
            } else {
                buffer.Append(ch);
            }
        }
        if (buffer.Length > 0) {
            lines.Add(buffer.ToString());
        }
        return lines;
    }
}   // End of Text.cs
}   // End of namespace PDFjet.NET
