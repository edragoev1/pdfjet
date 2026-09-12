/*
 * TextFrame.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Paragraphs of text lines, wrapped at the width of the frame, with an optional
/// border. A frame with a height draws as much of the text as fits and keeps the
/// rest for the next frame, so text flows from frame to frame. A frame without a
/// height draws all of the text, as Text does. Please see Example_47.
/// </summary>
public class TextFrame : IDrawable {
    private List<Paragraph> paragraphs;
    private float x;
    private float y;
    private float w;
    private float h;
    private float paragraphLeading = 24f;
    private bool border = false;
    private float[] borderColor = {0f, 0f, 1f};
    private float borderWidth = 0.5f;
    private String borderPattern = "[] 0";

    // The text that is not drawn yet starts at this paragraph, at this text line
    // of the paragraph and at this token of the text line. The tokens are null
    // when the text line has not been started.
    private int paragraphIndex = 0;
    private int lineIndex = 0;
    private List<String> tokens = null;
    private int tokenIndex = 0;

    // The row of text being drawn: where the text goes next, whether the row can
    // take more text, whether the frame has a row yet, and where the next row goes.
    private float xText;
    private float yText;
    private bool rowOpen;
    private bool rowPlaced;
    private float nextBaseline;

    /// <summary>
    /// Creates a text frame from paragraphs of text lines. The paragraphs are 24
    /// points apart unless SetParagraphLeading says otherwise.
    /// </summary>
    public TextFrame(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
    }

    /// <summary>
    /// Creates a text frame from strings, one paragraph each, in the font at its
    /// size. An empty line separates the paragraphs.
    /// </summary>
    public TextFrame(Font f1, List<String> inputList) {
        this.paragraphs = new List<Paragraph>();
        foreach (String text in inputList) {
            this.paragraphs.Add(new Paragraph(new TextLine(f1, text)));
        }
        this.paragraphLeading = 2f * f1.GetBodyHeight();
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of this text frame.</summary>
    public TextFrame SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location of the top left corner of this text frame.</summary>
    public TextFrame SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>Sets the width at which the lines wrap.</summary>
    public TextFrame SetWidth(float w) {
        this.w = w;
        return this;
    }

    /// <summary>Sets the width at which the lines wrap.</summary>
    public TextFrame SetWidth(double w) {
        return SetWidth((float) w);
    }

    /// <summary>Sets the height of this text frame. With a height of 0, the default, the frame draws all of its text.</summary>
    public TextFrame SetHeight(float h) {
        this.h = h;
        return this;
    }

    /// <summary>Sets the height of this text frame. With a height of 0, the default, the frame draws all of its text.</summary>
    public TextFrame SetHeight(double h) {
        return SetHeight((float) h);
    }

    /// <summary>Returns the width of this text frame.</summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>Returns the height of this text frame.</summary>
    public float GetHeight() {
        return this.h;
    }

    /// <summary>
    /// Sets the vertical distance between paragraphs, from the baseline of the
    /// last line of a paragraph to the baseline of the first line of the next.
    /// </summary>
    public TextFrame SetParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    /// <summary>Sets whether a border is drawn around this text frame.</summary>
    public TextFrame SetBorder(bool border) {
        this.border = border;
        return this;
    }

    /// <summary>Sets the border color as a 0xRRGGBB value and draws a border around this text frame. Color.transparent removes the border.</summary>
    public TextFrame SetBorderColor(int color) {
        if (color == Color.transparent) {
            this.border = false;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        return SetBorderColor(r, g, b);
    }

    /// <summary>Sets the border color from red, green and blue values and draws a border around this text frame.</summary>
    public TextFrame SetBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        this.border = true;
        return this;
    }

    /// <summary>Sets the border color from an array of red, green and blue values and draws a border around this text frame.</summary>
    public TextFrame SetBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        this.border = true;
        return this;
    }

    /// <summary>Sets the border width.</summary>
    public TextFrame SetBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /// <summary>Sets the dash pattern of the border, for example "[3] 0".</summary>
    public TextFrame SetBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
        return this;
    }

    /// <summary>Returns true if some of the text has not been drawn yet.</summary>
    public bool HasMoreText() {
        return paragraphIndex < paragraphs.Count;
    }

    /// <summary>
    /// Draws the text on the page: all of it when this frame has no height, or as
    /// much as fits in the height, keeping the rest for the next frame. The first
    /// line of a frame is drawn even when it does not fit, so the text always
    /// flows, and a word wider than the frame is broken. With no page nothing is
    /// drawn, the paragraphs get their coordinates and the text is kept.
    /// Returns the x and y coordinates of the bottom right corner of this frame,
    /// or of the text when the frame has no height.
    /// </summary>
    public float[] DrawOn(Page page) {
        int startParagraph = paragraphIndex;
        int startLine = lineIndex;
        List<String> startTokens = (tokens == null) ? null : new List<String>(tokens);
        int startToken = tokenIndex;

        float bottom = DrawParagraphs(page);
        if (h > 0f) {
            bottom = y + h;
        }
        if (border) {
            Rect rect = new Rect(x, y, w, bottom - y);
            rect.SetBorderColor(borderColor);
            rect.SetBorderWidth(borderWidth);
            rect.SetBorderPattern(borderPattern);
            rect.DrawOn(page);
        }

        if (page == null) {
            paragraphIndex = startParagraph;
            lineIndex = startLine;
            tokens = startTokens;
            tokenIndex = startToken;
        }
        return new float[] {x + w, bottom};
    }

    // Draws the text that is left, as much of it as fits in the height of the
    // frame, and returns the bottom of the text drawn.
    private float DrawParagraphs(Page page) {
        xText = x;
        rowOpen = false;
        rowPlaced = false;
        float bottom = y;
        while (paragraphIndex < paragraphs.Count) {
            Paragraph paragraph = paragraphs[paragraphIndex];
            while (lineIndex < paragraph.lines.Count) {
                TextLine textLine = paragraph.lines[lineIndex];
                if (!rowOpen && !OpenRow(textLine)) {
                    return bottom;
                }
                if (tokens == null) {
                    if (lineIndex == 0) {
                        paragraph.x1 = x;
                        paragraph.y1 = yText - textLine.font.GetAscent(textLine.fontSize);
                        paragraph.xText = xText;
                        paragraph.yText = yText;
                    }
                    tokens = Tokenize(textLine);
                    tokenIndex = 0;
                }
                if (!DrawTokens(page, textLine)) {
                    return bottom;
                }
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.GetDescent(textLine.fontSize);
                bottom = paragraph.y2;
                tokens = null;
                tokenIndex = 0;
                lineIndex++;
            }
            xText = x;
            rowOpen = false;
            nextBaseline = yText + paragraphLeading;
            paragraphIndex++;
            lineIndex = 0;
        }
        return bottom;
    }

    // Starts a row of text for the text line, below the previous row or at the
    // top of the frame. Returns false when the row does not fit in the height of
    // the frame. The first row of a frame always fits, so the text keeps flowing.
    private bool OpenRow(TextLine textLine) {
        float baseline = rowPlaced ? nextBaseline : y + textLine.font.GetAscent(textLine.fontSize);
        if (h > 0f && rowPlaced &&
                (baseline + textLine.font.GetDescent(textLine.fontSize)) > (y + h)) {
            return false;
        }
        xText = x;
        yText = baseline;
        rowOpen = true;
        rowPlaced = true;
        return true;
    }

    // Draws the tokens of the text line that are left, wrapping them at the width
    // of the frame. Returns false when a row does not fit in the height of the
    // frame; the tokens that are left stay for the next frame.
    private bool DrawTokens(Page page, TextLine textLine) {
        Font font = textLine.font;
        Font fallbackFont = textLine.fallbackFont;
        float fontSize = textLine.fontSize;
        StringBuilder buf = new StringBuilder();
        while (tokenIndex < tokens.Count) {
            if (!rowOpen && !OpenRow(textLine)) {
                return false;
            }
            String token = tokens[tokenIndex];
            float runLength = font.StringWidth(fallbackFont, fontSize, buf.ToString());
            float tokenWidth = font.StringWidth(fallbackFont, fontSize, token + Single.space);
            if ((runLength + tokenWidth) < ((x + w) - xText)) {
                buf.Append(token).Append(Single.space);
                tokenIndex++;
                continue;
            }
            if (buf.Length == 0 && xText == x) {
                // The token does not fit in an empty row, so the row takes as much of it as fits.
                String head = HeadThatFits(textLine, token);
                buf.Append(head);
                if (head.Length == token.Length) {
                    tokenIndex++;
                } else {
                    tokens[tokenIndex] = token.Substring(head.Length);
                }
            }
            DrawLine(page, textLine, buf.ToString());
            buf.Length = 0;
            xText = x;
            rowOpen = false;
            nextBaseline = yText + textLine.GetHeight();
        }
        DrawLine(page, textLine, buf.ToString());
        xText += font.StringWidth(fallbackFont, fontSize, buf.ToString());
        return true;
    }

    // Returns the longest start of the token that is narrower than the frame, and
    // at least the first character of the token.
    private String HeadThatFits(TextLine textLine, String token) {
        int end = Util.CharCount(token, 0);
        while (end < token.Length) {
            int next = end + Util.CharCount(token, end);
            if (textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, token.Substring(0, next)) >= w) {
                break;
            }
            end = next;
        }
        return token.Substring(0, end);
    }

    // Draws the string at the current text position, with the attributes of the text line.
    private void DrawLine(Page page, TextLine textLine, String str) {
        new TextLine(textLine.font, str)
                .SetFallbackFont(textLine.GetFallbackFont())
                .SetFontSize(textLine.GetFontSize())
                .SetTextColor(textLine.GetTextColor())
                .SetColorMap(textLine.GetColorMap())
                .SetUnderline(textLine.GetUnderline())
                .SetStrikeout(textLine.GetStrikeout())
                .SetLanguage(textLine.GetLanguage())
                .SetLocation(xText, yText)
                .DrawOn(page);
    }

    // Splits the text of the text line into words, or, for CJK text, which has no
    // spaces between its words, into runs of characters that fit in the width,
    // never between the two halves of a surrogate pair.
    private List<String> Tokenize(TextLine textLine) {
        List<String> list = new List<String>();
        if (!Util.IsCJK(textLine.text)) {
            list.AddRange(Util.SplitOnWhitespace(textLine.text));
            return list;
        }
        StringBuilder buf = new StringBuilder();
        String text = textLine.text;
        for (int i = 0; i < text.Length; i += Util.CharCount(text, i)) {
            String ch = text.Substring(i, Util.CharCount(text, i));
            if (textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, buf.ToString() + ch) < w) {
                buf.Append(ch);
            } else {
                if (buf.Length > 0) {
                    list.Add(buf.ToString());
                }
                buf.Length = 0;
                buf.Append(ch);
            }
        }
        if (buf.Length > 0) {
            list.Add(buf.ToString());
        }
        return list;
    }
}   // End of TextFrame.cs
}   // End of namespace PDFjet.NET
