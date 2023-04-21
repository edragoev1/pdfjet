/**
 *  TextBlock.cs
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
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 *  Class for creating blocks of text.
 *
 */
public class TextBlock : IDrawable {
    internal Font font = null;
    internal Font fallbackFont = null;
    internal String text = null;

    private float spaceBetweenLines = 0f;
    private uint textAlign = Align.LEFT;

    private float x;
    private float y;
    private float w = 300f;
    private float h = 200f;

    private int background = Color.white;
    private int brush = Color.black;
    private bool drawBorder;

    private String uri = null;
    private String key = null;
    private String uriLanguage = null;
    private String uriActualText = null;
    private String uriAltDescription = null;

    /**
     *  Creates a text block.
     *
     *  @param font the text font.
     */
    public TextBlock(Font font) {
        this.font = font;
    }

    public TextBlock(Font font, String text) {
        this.font = font;
        this.text = text;
    }

    /**
     *  Sets the fallback font.
     *
     *  @param fallbackFont the fallback font.
     *  @return the TextBlock object.
     */
    public TextBlock SetFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /**
     *  Sets the block text.
     *
     *  @param text the block text.
     *  @return the TextBlock object.
     */
    public TextBlock SetText(String text) {
        this.text = text;
        return this;
    }

    public TextBlock SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     *  Sets the location where this text block will be drawn on the page.
     *
     *  @param x the x coordinate of the top left corner of the text block.
     *  @param y the y coordinate of the top left corner of the text block.
     *  @return the TextBlock object.
     */
    public TextBlock SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     *  Sets the width of this text block.
     *
     *  @param width the specified width.
     *  @return the TextBlock object.
     */
    public TextBlock SetWidth(float width) {
        this.w = width;
        return this;
    }

    /**
     *  Returns the text block width.
     *
     *  @return the text block width.
     */
    public float GetWidth() {
        return this.w;
    }

    /**
     *  Sets the height of this text block.
     *
     *  @param height the specified height.
     *  @return the TextBlock object.
     */
    public TextBlock SetHeight(float height) {
        this.h = height;
        return this;
    }

    /**
     *  Returns the text block height.
     *
     *  @return the text block height.
     */
    public float GetHeight() {
        return DrawOn(null)[1];
    }

    /**
     *  Sets the space between two lines of text.
     *
     *  @param spaceBetweenLines the space between two lines.
     *  @return the TextBlock object.
     */
    public TextBlock SetSpaceBetweenLines(float spaceBetweenLines) {
        this.spaceBetweenLines = spaceBetweenLines;
        return this;
    }

    /**
     *  Returns the space between two lines of text.
     *
     *  @return float the space.
     */
    public float GetSpaceBetweenLines() {
        return spaceBetweenLines;
    }

    /**
     *  Sets the text alignment.
     *
     *  @param textAlign the alignment parameter.
     *  Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public TextBlock SetTextAlignment(uint textAlign) {
        this.textAlign = textAlign;
        return this;
    }

    /**
     *  Returns the text alignment.
     *
     *  @return the alignment code.
     */
    public uint GetTextAlignment() {
        return this.textAlign;
    }

    /**
     *  Sets the background to the specified color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     *  @return the TextBlock object.
     */
    public TextBlock SetBgColor(int color) {
        this.background = color;
        return this;
    }

    /**
     *  Returns the background color.
     *
     * @return int the color as 0xRRGGBB integer.
     */
    public int GetBgColor() {
        return this.background;
    }

    /**
     *  Sets the brush color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     *  @return the TextBlock object.
     */
    public TextBlock SetBrushColor(int color) {
        this.brush = color;
        return this;
    }

    /**
     * Returns the brush color.
     *
     * @return int the brush color specified as 0xRRGGBB integer.
     */
    public int GetBrushColor() {
        return this.brush;
    }

    public void SetDrawBorder(bool drawBorder) {
        this.drawBorder = drawBorder;
    }

    // Is the text Chinese, Japanese or Korean?
    private bool IsCJK(String text) {
        int cjk = 0;
        int other = 0;
        foreach (Char ch in text) {
            if (ch >= 0x4E00 && ch <= 0x9FFF ||     // Unified CJK
                ch >= 0xAC00 && ch <= 0xD7AF ||     // Hangul (Korean)
                ch >= 0x30A0 && ch <= 0x30FF ||     // Katakana (Japanese)
                ch >= 0x3040 && ch <= 0x309F) {     // Hiragana (Japanese)
                cjk += 1;
            }
            else {
                other += 1;
            }
        }
        return cjk > other;
    }

    /**
     *  Draws this text block on the specified page.
     *
     *  @param page the page to draw this text block on.
     *  @return the TextBlock object.
     */
    public float[] DrawOn(Page page) {
        if (page != null) {
            if (GetBgColor() != Color.white) {
                page.SetBrushColor(this.background);
                page.FillRect(x, y, w, h);
            }
            page.SetBrushColor(this.brush);
        }
        return DrawText(page);
    }

    private float[] DrawText(Page page) {
        List<String> list = new List<String>();
        String[] lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (String line in lines) {
            if (IsCJK(line)) {
                StringBuilder buf = new StringBuilder();
                for (int i = 0; i < line.Length; i++) {
                    Char ch = line[i];
                    if (font.StringWidth(fallbackFont, buf.ToString() + ch) <= this.w) {
                        buf.Append(ch);
                    } else {
                        list.Add(buf.ToString());
                        buf.Length = 0;
                        buf.Append(ch);
                    }
                }
                String str = buf.ToString().Trim();
                if (!str.Equals("")) {
                    list.Add(str);
                }
            } else {
                if (font.StringWidth(fallbackFont, line) < this.w) {
                    list.Add(line);
                } else {
                    StringBuilder buf = new StringBuilder();
                    String[] tokens = TextUtils.SplitTextIntoTokens(line, font, fallbackFont, this.w);
                    foreach (String token in tokens) {
                        if (font.StringWidth(fallbackFont, (buf.ToString() + " " + token).Trim()) < this.w) {
                            buf.Append(" " + token);
                        }
                        else {
                            list.Add(buf.ToString().Trim());
                            buf.Length = 0;
                            buf.Append(token);
                        }
                    }
                    String str = buf.ToString().Trim();
                    if (!str.Equals("")) {
                        list.Add(str);
                    }
                }
            }
        }
        lines = list.ToArray();

        float xText;
        float yText = y + font.ascent;
        for (int i = 0; i < lines.Length; i++) {
            if (textAlign == Align.LEFT) {
                xText = x;
            } else if (textAlign == Align.RIGHT) {
                xText = (x + this.w) - (font.StringWidth(fallbackFont, lines[i]));
            } else if (textAlign == Align.CENTER) {
                xText = x + (this.w - font.StringWidth(fallbackFont, lines[i]))/2;
            } else {
                throw new Exception("Invalid text alignment option.");
            }
            if (page != null) {
                page.DrawString(font, fallbackFont, lines[i], xText, yText);
            }
            if (i < (lines.Length - 1)) {
                yText += font.bodyHeight + spaceBetweenLines;
            }
        }
        this.h = (yText - y) + font.descent;
        if (page != null && drawBorder) {
            Box box = new Box();
            box.SetLocation(x, y);
            box.SetSize(w, h);
            box.DrawOn(page);
        }

        if (page != null && (uri != null || key != null)) {
            page.AddAnnotation(new Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    uriLanguage,
                    uriActualText,
                    uriAltDescription));
        }

        return new float[] {this.x + this.w, this.y + this.h};
    }

    /**
     *  Sets the URI for the "click text line" action.
     *
     *  @param uri the URI
     *  @return this TextBlock.
     */
    public TextBlock SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }
}   // End of TextBlock.cs
}   // End of namespace PDFjet.NET
