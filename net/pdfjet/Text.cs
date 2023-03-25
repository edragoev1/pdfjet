/**
 *  Text.cs
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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
    private Font font;
    private Font fallbackFont;
    private float x1;
    private float y1;
    private float width;
    private float xText;
    private float yText;
    private float leading;
    private float paragraphLeading;
    private List<float[]> beginParagraphPoints;
    private float spaceBetweenTextLines;
    private bool drawBorder = true;


    public Text(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
        this.font = paragraphs[0].list[0].GetFont();
        this.fallbackFont = paragraphs[0].list[0].GetFallbackFont();
        this.leading = font.GetBodyHeight();
        this.paragraphLeading = 2*leading;
        this.beginParagraphPoints = new List<float[]>();
        this.spaceBetweenTextLines = font.StringWidth(fallbackFont, Single.space);
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


    public Text SetLeading(float leading) {
        this.leading = leading;
        return this;
    }


    public Text SetParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }


    public List<float[]> GetBeginParagraphPoints() {
        return this.beginParagraphPoints;
    }


    public Text SetSpaceBetweenTextLines(float spaceBetweenTextLines) {
        this.spaceBetweenTextLines = spaceBetweenTextLines;
        return this;
    }


    public float[] DrawOn(Page page) {
        this.xText = x1;
        this.yText = y1 + font.GetAscent();
        foreach (Paragraph paragraph in paragraphs) {
            int numberOfTextLines = paragraph.list.Count;
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < numberOfTextLines; i++) {
                TextLine textLine = paragraph.list[i];
                buf.Append(textLine.text);
            }
            for (int i = 0; i < numberOfTextLines; i++) {
                TextLine textLine = paragraph.list[i];
                if (i == 0) {
                    beginParagraphPoints.Add(new float[] { xText, yText });
                }
                float[] point = DrawTextLine(page, xText, yText, textLine);
                xText = point[0];
                if (textLine.GetTrailingSpace()) {
                    xText += spaceBetweenTextLines;
                }
                yText = point[1];
            }
            xText = x1;
            yText += paragraphLeading;
        }

        float height = ((yText - paragraphLeading) - y1) + font.descent;
        if (page != null && drawBorder) {
            Box box = new Box();
            box.SetLocation(x1, y1);
            box.SetSize(width, height);
            box.DrawOn(page);
        }

        return new float[] {x1 + width, y1 + height};
    }


    public float[] DrawTextLine(
            Page page, float x, float y, TextLine textLine) {
        this.xText = x;
        this.yText = y;

        String[] tokens = null;
        if (StringIsCJK(textLine.text)) {
            tokens = TokenizeCJK(textLine, this.width);
        }
        else {
            tokens = Regex.Split(textLine.text, @"\s+");
        }

        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < tokens.Length; i++) {
            String token = (i == 0) ? tokens[i] : (Single.space + tokens[i]);
            float lineWidth = textLine.font.StringWidth(textLine.fallbackFont, buf.ToString());
            float tokenWidth = textLine.font.StringWidth(textLine.fallbackFont, token);
            if ((lineWidth + tokenWidth) < (this.x1 + this.width) - this.xText) {
                buf.Append(token);
            }
            else {
                if (page != null) {
                    new TextLine(textLine.font, buf.ToString())
                            .SetFallbackFont(textLine.fallbackFont)
                            .SetLocation(xText, yText + textLine.GetVerticalOffset())
                            .SetColor(textLine.GetColor())
                            .SetUnderline(textLine.GetUnderline())
                            .SetStrikeout(textLine.GetStrikeout())
                            .SetLanguage(textLine.GetLanguage())
                            .DrawOn(page);
                }
                xText = x1;
                yText += leading;
                buf.Length = 0;
                buf.Append(tokens[i]);
            }
        }
        if (page != null) {
            new TextLine(textLine.font, buf.ToString())
                    .SetFallbackFont(textLine.fallbackFont)
                    .SetLocation(xText, yText + textLine.GetVerticalOffset())
                    .SetColor(textLine.GetColor())
                    .SetUnderline(textLine.GetUnderline())
                    .SetStrikeout(textLine.GetStrikeout())
                    .SetLanguage(textLine.GetLanguage())
                    .DrawOn(page);
        }

        return new float[] {
                xText + textLine.font.StringWidth(textLine.fallbackFont, buf.ToString()),
                yText};
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
            }
            else {
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
        stream.Close();
        return lines;
    }

}   // End of Text.cs
}   // End of namespace PDFjet.NET
