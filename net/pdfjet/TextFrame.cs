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
/// height draws all of the text. Drawing consumes the text: a second DrawOn
/// draws what is left, so build a new frame to draw the same text again.
///
/// Use a TextFrame for text that continues from one frame to the next: the
/// columns of an article, or the pages of a long text. Use a TextColumn to
/// draw paragraphs in one place, with justified text or a rotation, and a
/// TextBlock for one run of text in one font. Please see Example_03 and
/// Example_47.
/// </summary>
public class TextFrame : IDrawable {
    private List<Paragraph> paragraphs;
    private float x;
    private float y;
    private float w;
    private float h;
    private float paragraphGap = 0f;
    private bool hasParagraphGap = false;
    private bool border = false;
    private float[] borderColor = {0f, 0f, 0f};
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
    private bool startsParagraph;   // The next row starts a paragraph

    // The text of the row being drawn, drawn when the row is complete, so that it
    // can be aligned: each part is a text line with some of its text.
    private readonly List<RowPart> row = new List<RowPart>();

    private sealed class RowPart {
        internal readonly TextLine textLine;
        internal readonly String text;
        internal readonly float x;
        internal readonly Paragraph paragraph;
        internal readonly bool startsParagraph;     // The first text of the paragraph
        internal readonly bool endsTextLine;        // The last text of the text line

        internal RowPart(TextLine textLine, String text, float x, Paragraph paragraph,
                bool startsParagraph, bool endsTextLine) {
            this.textLine = textLine;
            this.text = text;
            this.x = x;
            this.paragraph = paragraph;
            this.startsParagraph = startsParagraph;
            this.endsTextLine = endsTextLine;
        }
    }

    /// <summary>
    /// Creates a text frame from paragraphs of text lines. An empty line separates
    /// the paragraphs unless SetParagraphGap sets another gap.
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

    /// <summary>Sets the width at which the lines wrap.</summary>
    public TextFrame SetWidth(float w) {
        this.w = w;
        return this;
    }

    /// <summary>Sets the height of this text frame. With a height of 0, the default, the frame draws all of its text.</summary>
    public TextFrame SetHeight(float h) {
        this.h = h;
        return this;
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
    /// Sets the space between paragraphs, in points, 0 or more: from the bottom of
    /// the text of a paragraph to the top of the text of the next, so paragraphs
    /// never overlap. The default is one empty line in the size of the next
    /// paragraph, so a heading is not followed by an empty line of its own size.
    /// A negative gap is taken as 0.
    /// </summary>
    public TextFrame SetParagraphGap(float paragraphGap) {
        this.paragraphGap = Math.Max(0f, paragraphGap);
        this.hasParagraphGap = true;
        return this;
    }

    /// <summary>Sets whether a border is drawn around this text frame.</summary>
    public TextFrame SetBorders(bool border) {
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
        return SetBorderColor(new float[] {r, g, b});
    }

    /// <summary>Sets the border color from an array of red, green and blue values and draws a border around this text frame.</summary>
    public TextFrame SetBorderColor(float[] rgbColor) {
        this.borderColor = Util.CopyOf(rgbColor);
        this.border = true;
        return this;
    }

    /// <summary>Sets the border width.</summary>
    public TextFrame SetBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /// <summary>Sets the dash pattern of the border, for example "[3] 0".</summary>
    public TextFrame SetBorderDashPattern(String borderPattern) {
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
            rect.SetBorderDashPattern(borderPattern);
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
        startsParagraph = false;
        row.Clear();
        float bottom = y;
        while (paragraphIndex < paragraphs.Count) {
            Paragraph paragraph = paragraphs[paragraphIndex];
            while (lineIndex < paragraph.lines.Count) {
                TextLine textLine = paragraph.lines[lineIndex];
                if (!rowOpen && !OpenRow(textLine)) {
                    DrawRow(page, false);
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
                if (!DrawTokens(page, paragraph, textLine)) {
                    DrawRow(page, false);
                    return bottom;
                }
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.GetDescent(textLine.fontSize);
                bottom = paragraph.y2;
                tokens = null;
                tokenIndex = 0;
                lineIndex++;
            }
            DrawRow(page, true);
            xText = x;
            rowOpen = false;
            if (paragraph.lines.Count > 0) {
                // The next paragraph starts below the descent of this one, after the gap.
                TextLine lastLine = paragraph.lines[paragraph.lines.Count - 1];
                nextBaseline = yText + lastLine.font.GetDescent(lastLine.fontSize);
                startsParagraph = true;
            }
            paragraphIndex++;
            lineIndex = 0;
        }
        return bottom;
    }

    // Starts a row of text for the text line, below the previous row or at the
    // top of the frame. Returns false when the row does not fit in the height of
    // the frame. The first row of a frame always fits, so the text keeps flowing.
    private bool OpenRow(TextLine textLine) {
        float baseline = y + textLine.font.GetAscent(textLine.fontSize);
        if (rowPlaced) {
            baseline = nextBaseline;
            if (startsParagraph) {
                // The gap, one empty line of this text by default, then its ascent.
                float gap = hasParagraphGap ? paragraphGap : textLine.GetHeight();
                baseline += gap + textLine.font.GetAscent(textLine.fontSize);
            }
        }
        if (h > 0f && rowPlaced &&
                (baseline + textLine.font.GetDescent(textLine.fontSize)) > (y + h)) {
            return false;
        }
        xText = x;
        yText = baseline;
        rowOpen = true;
        rowPlaced = true;
        startsParagraph = false;
        return true;
    }

    // Draws the tokens of the text line that are left, wrapping them at the width
    // of the frame. Returns false when a row does not fit in the height of the
    // frame; the tokens that are left stay for the next frame.
    private bool DrawTokens(Page page, Paragraph paragraph, TextLine textLine) {
        Font font = textLine.font;
        Font fallbackFont = textLine.fallbackFont;
        float fontSize = textLine.fontSize;
        StringBuilder buf = new StringBuilder();
        float runLength = 0f;
        while (tokenIndex < tokens.Count) {
            if (!rowOpen && !OpenRow(textLine)) {
                return false;
            }
            String token = tokens[tokenIndex];
            // The token is measured without the space that follows it, as in
            // TextColumn: a row is as wide as the text it shows.
            if ((runLength + Width(textLine, token)) <= ((x + w) - xText)) {
                buf.Append(token).Append(Single.space);
                runLength += Width(textLine, token + Single.space);
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
            AddToRow(paragraph, textLine, buf.ToString(), false);
            DrawRow(page, false);
            buf.Length = 0;
            runLength = 0f;
            xText = x;
            rowOpen = false;
            nextBaseline = yText + textLine.GetHeight();
        }
        AddToRow(paragraph, textLine, buf.ToString(), true);
        xText += font.StringWidth(fallbackFont, fontSize, buf.ToString());
        return true;
    }

    // Returns the longest start of the token that fits in the width of the frame,
    // and at least the first character of the token.
    private String HeadThatFits(TextLine textLine, String token) {
        int end = Util.CharCount(token, 0);
        while (end < token.Length) {
            int next = end + Util.CharCount(token, end);
            if (textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, token.Substring(0, next)) > w) {
                break;
            }
            end = next;
        }
        return token.Substring(0, end);
    }

    // Adds the string to the row, at the current text position.
    private void AddToRow(Paragraph paragraph, TextLine textLine, String str, bool endsTextLine) {
        bool first = row.Count == 0 && lineIndex == 0 && xText == paragraph.xText
                && yText == paragraph.yText;
        row.Add(new RowPart(textLine, str, xText, paragraph, first, endsTextLine));
    }

    // Draws the parts of the row, with every setting of their text lines, including
    // the vertical offset and the link, as TextColumn does. A paragraph aligned to
    // the right or to the center moves the row, and a justified one widens the
    // spaces of every row but its last.
    private void DrawRow(Page page, bool lastRowOfParagraph) {
        if (row.Count == 0) {
            return;
        }
        Paragraph paragraph = row[0].paragraph;
        Alignment alignment = paragraph.explicitAlignment ? paragraph.alignment : Alignment.LEFT;
        RowPart last = row[row.Count - 1];
        float rowWidth = last.x + Width(last.textLine, TrimTrailingSpaces(last.text)) - x;

        if (alignment == Alignment.JUSTIFY && !lastRowOfParagraph) {
            DrawJustifiedRow(page, rowWidth);
        } else {
            float shift = 0f;
            if (alignment == Alignment.RIGHT) {
                shift = w - rowWidth;
            } else if (alignment == Alignment.CENTER) {
                shift = (w - rowWidth) / 2f;
            }
            foreach (RowPart part in row) {
                part.textLine.CopyWithText(part.text).SetLocation(part.x + shift, yText).DrawOn(page);
                if (part.startsParagraph) {
                    part.paragraph.xText += shift;
                }
                if (part.endsTextLine) {
                    part.paragraph.x2 = part.x + Width(part.textLine, part.text) + shift;
                }
            }
        }
        row.Clear();
    }

    // Draws the words of the row one by one, with the width left in the row shared
    // out among the spaces between them.
    private void DrawJustifiedRow(Page page, float rowWidth) {
        int spaces = 0;
        for (int i = 0; i < row.Count; i++) {
            String text = row[i].text;
            for (int j = 0; j < text.Length; j++) {
                if (text[j] == ' ' && HasWordAfter(i, j + 1)) {
                    spaces++;
                }
            }
        }
        float dx = (spaces > 0) ? (w - rowWidth) / spaces : 0f;
        float xWord = x;
        for (int i = 0; i < row.Count; i++) {
            RowPart part = row[i];
            String text = part.text;
            int start = 0;
            while (start < text.Length) {
                int end = text.IndexOf(' ', start);
                if (end == -1) {
                    end = text.Length;
                }
                if (end > start) {
                    String word = text.Substring(start, end - start);
                    part.textLine.CopyWithText(word).SetLocation(xWord, yText).DrawOn(page);
                    xWord += Width(part.textLine, word);
                }
                if (end < text.Length) {
                    xWord += Width(part.textLine, Single.space);
                    if (HasWordAfter(i, end + 1)) {
                        xWord += dx;
                    }
                }
                start = end + 1;
            }
            if (part.endsTextLine) {
                part.paragraph.x2 = xWord;
            }
        }
    }

    // Returns true when a word follows this position of the row.
    private bool HasWordAfter(int partIndex, int charIndex) {
        for (int i = partIndex; i < row.Count; i++) {
            String text = row[i].text;
            for (int j = (i == partIndex) ? charIndex : 0; j < text.Length; j++) {
                if (text[j] != ' ') {
                    return true;
                }
            }
        }
        return false;
    }

    private static String TrimTrailingSpaces(String text) {
        int end = text.Length;
        while (end > 0 && text[end - 1] == ' ') {
            end--;
        }
        return text.Substring(0, end);
    }

    private static float Width(TextLine textLine, String text) {
        return textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, text);
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
            if (textLine.font.StringWidth(textLine.fallbackFont, textLine.fontSize, buf.ToString() + ch) <= w) {
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
