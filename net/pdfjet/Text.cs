/**
 * Text.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

///
/// Please see Example_45
///
namespace PDFjet.NET {
public class Text : IDrawable {
    private List<Paragraph> paragraphs;
    private float x1;
    private float y1;
    private float width;
    private float xText;
    private float yText;
    private float paragraphLeading = 24f;
	private float[] borderColor;
	private bool hasBorderColor = false;
	private float borderWidth = 0.5f;
	private String borderPattern;

    public Text(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
    }

    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public Text SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    public Text SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    public Text SetWidth(float width) {
        this.width = width;
        return this;
    }

    public Text SetParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    public void SetBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
    }

    public void SetBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            return;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetBorderColor(r, g, b);
    }

    public void SetBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        this.hasBorderColor = true;
    }

    public void SetBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        this.hasBorderColor = true;
    }

    public void SetBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
    }

    public float[] DrawOn(Page page) {
        this.xText = x1;
        this.yText = y1 + paragraphs[0].lines[0].font.GetAscent();
        foreach (Paragraph paragraph in paragraphs) {
            paragraph.x1 = x1;
            paragraph.y1 = yText - paragraph.lines[0].font.GetAscent();
            paragraph.xText = xText;
            paragraph.yText = yText;
            foreach (TextLine textLine in paragraph.lines) {
                float[] point = DrawTextLine(page, xText, yText, textLine);
                xText = point[0];
                yText = point[1];
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.GetDescent(textLine.font.size);
            }
            xText = x1;
            yText += paragraphLeading;
        }

        Paragraph lastParagraph = paragraphs[paragraphs.Count - 1];
        TextLine lastTextLine = lastParagraph.GetTextLines()[lastParagraph.GetTextLines().Count - 1];
        float height = ((yText - paragraphLeading) - y1) + lastTextLine.font.GetDescent(lastTextLine.fontSize);
        if (hasBorderColor) {
            Rect rect = new Rect(x1, y1, width, height);
            rect.SetBorderColor(borderColor);
            // rect.SetBorderPattern(borderPattern);
            rect.DrawOn(page);
        }

        return new float[] { x1 + width, y1 + height };
    }

    public float[] DrawTextLine(
            Page page, float x, float y, TextLine textLine) {
        this.xText = x;
        this.yText = y;

        String[] tokens = null;
        if (StringIsCJK(textLine.text)) {
            tokens = TokenizeCJK(textLine, this.width);
        } else {
            tokens = Regex.Split(textLine.text, @"\s+");
        }

        StringBuilder buf = new StringBuilder();
        foreach (String token in tokens) {
            float runLength = textLine.font.StringWidth(textLine.fallbackFont, buf.ToString());
            float tokenWidth = textLine.font.StringWidth(textLine.fallbackFont, token + Single.space);
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

        return new float[] {
                xText + textLine.font.StringWidth(textLine.fallbackFont, buf.ToString()),
                yText };
    }

    private bool StringIsCJK(String str) {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        int numOfCJK = 0;
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
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
        for (int i = 0; i < textLine.text.Length; i++) {
            char ch = textLine.text[i];
            if (textLine.font.StringWidth(textLine.fallbackFont, buf.ToString() + ch) < textWidth) {
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

    public static List<Paragraph> paragraphsFromFile(Font f1, String filePath) {
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

    public static List<String> ReadLines(String filePath) {
        List<String> lines = new List<String>();
        FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        StringBuilder buffer = new StringBuilder();
        int ch;
        while ((ch = stream.ReadByte()) != -1) {
            if (ch == '\r') {
                continue;
            } else if (ch == '\n') {
                lines.Add(buffer.ToString());
                buffer.Length = 0;
            } else {
                buffer.Append((char) ch);
            }
        }
        if (buffer.Length > 0) {
            lines.Add(buffer.ToString());
        }
        stream.Close();
        return lines;
    }
}   // End of Text.cs
}   // End of namespace PDFjet.NET
