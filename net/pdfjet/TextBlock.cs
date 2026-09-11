/*
 * TextBlock.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
    /// <summary>A block of text that wraps at its width, with an optional border, background and padding.</summary>
    public class TextBlock : IDrawable {
        internal float x;
        internal float y;
        private float width;
        private float height;
        private Font font;
        private Font fallbackFont;
        private float fontSize = 12f;
        private string textContent;
        private float lineSpacing = 1.0f;
        private Dictionary<string, int> keywordHighlightColors;
        private float textPadding = 0.0f;

        private float[] fillColor;
        private float[] textColor = new float[] {0f, 0f, 0f};
        private float[] borderColor;
        private float borderWidth = 0.5f;
        private float borderCornerRadius = 0.0f;

        private Alignment textAlignment = Alignment.LEFT;


        private string language = "en-US";
//        private string altDescription = "";
        private string uri;
//        private string key;
//        private string uriLanguage = "en-US";
//        private string uriActualText;
//        private string uriAltDescription;
        private bool underline = false;
//        private bool strikeout = false;
        private bool rightToLeft = false;

        /// <summary>Creates a text block with the specified font and text.</summary>
        public TextBlock(Font font, string textContent) {
            this.font = font;
            this.fontSize = font.size;
            this.fallbackFont = font;
            this.x = 0.0f;
            this.y = 0.0f;
            this.width = 500.0f;
            this.height = 0.0f;
            this.textContent = textContent;
            this.textColor = new float[] {0f, 0f, 0f};      // Black color
        }

        /// <summary>
        ///  Sets the position where this text box will be drawn on the page.
        /// </summary>
        /// <param name="x">the x coordinate of the top left corner of the text box.</param>
        /// <param name="y">the y coordinate of the top left corner of the text box.</param>
        /// <returns>this TextBlock object.</returns>
        public TextBlock SetLocation(double x, double y) {
            SetLocation((float) x, (float) y);
            return this;
        }

        IDrawable IDrawable.SetLocation(float x, float y) {
            return SetLocation(x, y);
        }

        /// <summary>Sets the font of the text. It also becomes the fallback font.</summary>
        public TextBlock SetFont(Font font) {
            this.font = font;
            this.fallbackFont = font;
            return this;
        }

        /// <summary>Sets the font used for characters the main font does not have.</summary>
        public TextBlock SetFallbackFont(Font font) {
            this.fallbackFont = font;
            return this;
        }

        /// <summary>Sets the font size.</summary>
        public TextBlock SetFontSize(float fontSize) {
            this.fontSize = fontSize;
            return this;
        }

        /// <summary>Sets the text.</summary>
        public TextBlock SetText(string text) {
            this.textContent = text;
            return this;
        }

        /// <summary>Returns the font of the text.</summary>
        public Font GetFont() {
            return this.font;
        }

        /// <summary>Returns the text.</summary>
        public string GetText() {
            return this.textContent;
        }

        /// <summary>Sets the location of the top left corner of this text block.</summary>
        public TextBlock SetLocation(float x, float y) {
            this.x = x;
            this.y = y;
            return this;
        }

        /// <summary>Sets the size of this text block.</summary>
        public TextBlock SetSize(float width, float height) {
            this.width = width;
            this.height = height;
            return this;
        }

        /// <summary>Sets the width of this text block.</summary>
        public TextBlock SetWidth(double width) {
            this.width = (float) width;
            return this;
        }

        /// <summary>Sets the width of this text block.</summary>
        public TextBlock SetWidth(float width) {
            this.width = width;
            return this;
        }

        /// <summary>Returns the width of this text block.</summary>
        public float GetWidth() {
            return this.width;
        }

        /// <summary>Sets the height of this text block.</summary>
        public TextBlock SetHeight(double height) {
            this.height = (float) height;
            return this;
        }

        /// <summary>Sets the height of this text block.</summary>
        public TextBlock SetHeight(float height) {
            this.height = height;
            return this;
        }

        /// <summary>Returns the height of this text block.</summary>
        public float GetHeight() {
            return this.height;
        }

        /// <summary>Sets the radius of the border corners.</summary>
        public TextBlock SetBorderCornerRadius(float borderCornerRadius) {
            this.borderCornerRadius = borderCornerRadius;
            return this;
        }

        /// <summary>Sets the space between the text and the border.</summary>
        public TextBlock SetTextPadding(float padding) {
            this.textPadding = padding;
            return this;
        }

        /// <summary>Sets the border width.</summary>
        public TextBlock SetBorderWidth(float borderWidth) {
            this.borderWidth = borderWidth;
            return this;
        }

        /// <summary>Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.</summary>
        public TextBlock SetFillColor(int color) {
            if (color == Color.transparent) {
                this.fillColor = null;
                return this;
            }
            float r = ((color >> 16) & 0xff)/255f;
            float g = ((color >>  8) & 0xff)/255f;
            float b = ((color)       & 0xff)/255f;
            this.fillColor = new float[] {r, g, b};
            return this;
        }

        /// <summary>Sets the background color from an array of red, green and blue values.</summary>
        public TextBlock SetFillColor(float[] rgbColor) {
            this.fillColor = rgbColor;
            return this;
        }

        /// <summary>Sets the background color from an array of red, green and blue values.</summary>
        public TextBlock SetBackgroundColor(float[] rgbColor) {
            this.fillColor = rgbColor;
            return this;
        }

        /// <summary>Sets the border color as a 0xRRGGBB value. Color.transparent removes the border.</summary>
        public TextBlock SetBorderColor(int color) {
            if (color == Color.transparent) {
                this.borderColor = null;
                return this;
            }
            float r = ((color >> 16) & 0xff)/255f;
            float g = ((color >>  8) & 0xff)/255f;
            float b = ((color)       & 0xff)/255f;
            this.borderColor = new float[] {r, g, b};
            return this;
        }

        /// <summary>Sets the border color from an array of red, green and blue values.</summary>
        public TextBlock SetBorderColor(float[] rgbColor) {
            this.borderColor = rgbColor;
            return this;
        }

        /// <summary>Sets the line spacing as a multiple of the font's body height.</summary>
        public TextBlock SetLineSpacing(float lineSpacing) {
            this.lineSpacing = lineSpacing;
            return this;
        }

        /// <summary>Sets the text color from an array of red, green and blue values.</summary>
        public TextBlock SetTextColor(float[] textColor) {
            this.textColor = textColor;
            return this;
        }

        /// <summary>Sets the text color as a 0xRRGGBB value.</summary>
        public TextBlock SetTextColor(int color) {
            float r = ((color >> 16) & 0xff)/255f;
            float g = ((color >>  8) & 0xff)/255f;
            float b = ((color)       & 0xff)/255f;
            this.textColor = new float[] {r, g, b};
            return this;
        }

        /// <summary>Sets the horizontal alignment of the text.</summary>
        public TextBlock SetTextAlignment(Alignment textAlignment) {
            this.textAlignment = textAlignment;
            return this;
        }

        /// <summary>Sets the URI opened when this text block is clicked.</summary>
        public TextBlock SetURIAction(string uri) {
            this.uri = uri;
            return this;
        }

        /// <summary>Returns the background color.</summary>
        public float[] GetBackgroundColor() {
            return this.fillColor;
        }

        /// <summary>Sets the colors used to highlight keywords. The keywords are matched ignoring case.</summary>
        public TextBlock SetKeywordHighlightColors(Dictionary<string, int> map) {
            this.keywordHighlightColors = new Dictionary<string, int>();
            foreach (var key in map.Keys) {
                this.keywordHighlightColors[key.ToLower()] = map[key];
            }
            return this;
        }

        /// <summary>Marks the text as Arabic. The same as SetRightToLeft(true).</summary>
        public TextBlock SetTextIsArabic() {
            return SetRightToLeft(true);
        }

        /// <summary>
        /// Sets whether the text is right to left, like Arabic and Hebrew text.
        /// Each paragraph is wrapped at the width in logical order, and each line
        /// is then reordered with Bidi.ReorderVisually, which also shapes the
        /// Arabic letters, and aligned to the right, unless the text alignment is
        /// Alignment.CENTER.
        /// </summary>
        public TextBlock SetRightToLeft(bool rightToLeft) {
            this.rightToLeft = rightToLeft;
            return this;
        }

        private bool TextIsCJK(string str) {
            int numOfCJK = 0;
            char[] chars = str.ToCharArray();
            foreach (char ch in chars) {
                if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                    numOfCJK++;
                }
            }
            return numOfCJK > (chars.Length / 2);
        }

        private TextLine[] GetTextLinesWithOffsets() {
            List<TextLine> textLines = new List<TextLine>();

            float textAreaWidth = this.width - 2 * this.textPadding;
            this.textContent = this.textContent.Replace("\r\n", "\n").Trim();
            string[] lines = this.textContent.Split('\n');
            foreach (String str in lines) {
                String line = str;
                if (rightToLeft) {
                    AddRightToLeftLines(textLines, line, textAreaWidth);
                    continue;
                }

                if (this.font.StringWidth(fallbackFont, fontSize, line) <= textAreaWidth) {
                    textLines.Add(new TextLine(font, line));
                } else {
                    if (TextIsCJK(line)) {
                        StringBuilder sb = new StringBuilder();
                        foreach (char ch in line.ToCharArray()) {
                            if (font.StringWidth(fallbackFont, fontSize, sb.ToString() + ch) <= textAreaWidth) {
                                sb.Append(ch);
                            } else {
                                textLines.Add(new TextLine(font, sb.ToString()));
                                sb.Clear();
                                sb.Append(ch);
                            }
                        }
                        if (sb.ToString().Trim().Length > 0) {
                            textLines.Add(new TextLine(font, sb.ToString().Trim()));
                        }
                    } else {
                        StringBuilder sb = new StringBuilder();
                        string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string token in tokens) {
                            if (this.font.StringWidth(fallbackFont, fontSize, sb.ToString() + token) <= textAreaWidth) {
                                sb.Append(token).Append(" ");
                            } else {
                                textLines.Add(new TextLine(font, sb.ToString().Trim()));
                                sb.Clear();
                                sb.Append(token).Append(" ");
                            }
                        }
                        if (sb.ToString().Trim().Length > 0) {
                            textLines.Add(new TextLine(font, sb.ToString().Trim()));
                        }
                    }
                }
            }

            return textLines.ToArray();
        }

        /// <summary>
        /// Wraps a paragraph of right to left text at the spaces between words and
        /// adds its lines in visual order. The paragraph is wrapped in logical
        /// order, so its first words go on the first line, and each line is
        /// measured after it is reordered, since the shaped Arabic letters differ
        /// in width from the letters they replace.
        /// </summary>
        private void AddRightToLeftLines(List<TextLine> textLines, string paragraph, float textAreaWidth) {
            string line = "";
            foreach (string word in paragraph.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)) {
                string candidate = (line.Length == 0) ? word : line + " " + word;
                if (line.Length > 0 &&
                        this.font.StringWidth(fallbackFont, fontSize, Bidi.ReorderVisually(candidate)) > textAreaWidth) {
                    textLines.Add(new TextLine(font, Bidi.ReorderVisually(line)));
                    line = word;
                } else {
                    line = candidate;
                }
            }
            textLines.Add(new TextLine(font, Bidi.ReorderVisually(line)));
        }

        /// <summary>Sets whether the text is underlined.</summary>
        public TextBlock SetUnderline(bool underline) {
            this.underline = underline;
            return this;
        }

        // The offsets are from the left edge of the text, inside the padding.
        private void RightAlignText(TextLine[] textLines) {
            float textAreaWidth = this.width - 2 * this.textPadding;
            foreach (TextLine textLine in textLines) {
                textLine.xOffset =
                    textAreaWidth - font.StringWidth(fallbackFont, fontSize, textLine.text);
            }
        }

        private void CenterText(TextLine[] textLines) {
            float textAreaWidth = this.width - 2 * this.textPadding;
            foreach (TextLine textLine in textLines) {
                textLine.xOffset =
                    (textAreaWidth - font.StringWidth(fallbackFont, fontSize, textLine.text)) / 2f;
            }
        }

        private void UnderlineText(TextLine[] textLines) {
            foreach (TextLine textLine in textLines) {
                textLine.underline = true;
            }
        }

        /// <summary>Draws this text block on the specified page.</summary>
        public float[] DrawOn(Page page) {
            float ascent = this.font.GetAscent(fontSize);
            float descent = this.font.GetDescent(fontSize);
            float leading = (ascent + descent) * this.lineSpacing;
            TextLine[] textLines = GetTextLinesWithOffsets();
            if (page == null) {
                return new float[] {
                    this.width,
                    MathF.Max(this.height, textLines.Length * leading + 2 * this.textPadding)
                };
            }

            page.SaveGraphicsState();
            page.SetPenWidth(this.borderWidth);
            if (textAlignment == Alignment.CENTER) {
                CenterText(textLines);
            } else if (textAlignment == Alignment.RIGHT || rightToLeft) {
                RightAlignText(textLines);
            }
            if (underline) {
                UnderlineText(textLines);
            }

            if (borderColor != null || fillColor != null) {
                Rect rect = new Rect(
                    this.x,
                    this.y,
                    this.width,
                    MathF.Max(this.height, textLines.Length * leading + 2 * this.textPadding));
                if (borderColor != null) {
                    rect.SetBorderColor(this.borderColor);
                    rect.SetBorderWidth(this.borderWidth);
                    rect.SetCornerRadius(this.borderCornerRadius);
                }
                if (fillColor != null) {
                    rect.SetFillColor(this.fillColor);
                }
                rect.DrawOn(page);
            }

            page.AddBMC(StructElem.P, this.language, this.textContent, null);
            page.DrawTextBlock(
                this.font,
                this.fontSize,
                textLines,
                this.x + this.textPadding,
                this.y + this.textPadding,
                leading,
                this.textColor,
                this.keywordHighlightColors);
            page.AddEMC();
            page.RestoreGraphicsState();

            return new float[] {
                this.x + this.width,
                MathF.Max(this.y + this.height, this.y + textLines.Length * leading + 2 * this.textPadding)
            };
        }
    }
}
