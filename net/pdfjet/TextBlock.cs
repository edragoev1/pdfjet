/**
 * TextBlock.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
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
        private bool textIsArabic = false;

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

        /**
         *  Sets the position where this text box will be drawn on the page.
         *
         *  @param x the x coordinate of the top left corner of the text box.
         *  @param y the y coordinate of the top left corner of the text box.
         */
        public void SetPosition(double x, double y) {
            SetPosition((float) x, (float) y);
        }

        /**
         *  Sets the position where this text box will be drawn on the page.
         *
         *  @param x the x coordinate of the top left corner of the text box.
         *  @param y the y coordinate of the top left corner of the text box.
         */
        public void SetPosition(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public void SetFont(Font font) {
            this.font = font;
            this.fallbackFont = font;
        }

        public void SetFallbackFont(Font font) {
            this.fallbackFont = font;
        }

        public TextBlock SetFontSize(float fontSize) {
            this.fontSize = fontSize;
            return this;
        }

        public void SetText(string text) {
            this.textContent = text;
        }

        public Font GetFont() {
            return this.font;
        }

        public string GetText() {
            return this.textContent;
        }

        public void SetLocation(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public void SetSize(float width, float height) {
            this.width = width;
            this.height = height;
        }

        public void SetWidth(double width) {
            this.width = (float) width;
        }

        public void SetWidth(float width) {
            this.width = width;
        }

        public float GetWidth() {
            return this.width;
        }

        public void SetHeight(double height) {
            this.height = (float) height;
        }

        public void SetHeight(float height) {
            this.height = height;
        }

        public float GetHeight() {
            return this.height;
        }

        public void SetBorderCornerRadius(float borderCornerRadius) {
            this.borderCornerRadius = borderCornerRadius;
        }

        public void SetTextPadding(float padding) {
            this.textPadding = padding;
        }

        public void SetBorderWidth(float borderWidth) {
            this.borderWidth = borderWidth;
        }

        public void SetFillColor(int color) {
            if (color == Color.transparent) {
                this.fillColor = null;
                return;
            }
            float r = ((color >> 16) & 0xff)/255f;
            float g = ((color >>  8) & 0xff)/255f;
            float b = ((color)       & 0xff)/255f;
            this.fillColor = new float[] {r, g, b};
        }

        public void SetFillColor(float[] rgbColor) {
            this.fillColor = rgbColor;
        }

        public void SetBackgroundColor(float[] rgbColor) {
            this.fillColor = rgbColor;
        }

        public void SetBorderColor(int color) {
            if (color == Color.transparent) {
                this.borderColor = null;
                return;
            }
            float r = ((color >> 16) & 0xff)/255f;
            float g = ((color >>  8) & 0xff)/255f;
            float b = ((color)       & 0xff)/255f;
            this.borderColor = new float[] {r, g, b};
        }

        public void SetBorderColor(float[] rgbColor) {
            this.borderColor = rgbColor;
        }

        public void SetLineSpacing(float lineSpacing) {
            this.lineSpacing = lineSpacing;
        }

        public void SetTextColor(float[] textColor) {
            this.textColor = textColor;
        }

        public void SetTextColor(int color) {
            float r = ((color >> 16) & 0xff)/255f;
            float g = ((color >>  8) & 0xff)/255f;
            float b = ((color)       & 0xff)/255f;
            this.textColor = new float[] {r, g, b};
        }

        public TextBlock SetTextAlignment(Alignment textAlignment) {
            this.textAlignment = textAlignment;
            return this;
        }

        public TextBlock SetURIAction(string uri) {
            this.uri = uri;
            return this;
        }

        public float[] GetBackgroundColor() {
            return this.fillColor;
        }

        public void SetKeywordHighlightColors(Dictionary<string, int> map) {
            this.keywordHighlightColors = new Dictionary<string, int>();
            foreach (var key in map.Keys) {
                this.keywordHighlightColors[key.ToLower()] = map[key];
            }
        }

        public void SetTextIsArabic() {
            // Important!! The library renders Arabic properly, however it doesn't use ligatures like the one below !!
            // The Arabic character that looks like a Latin "I" and "J" connected is the ligature for "لَا" (lām + alif).
            // Here is the isolated form: لا
            // When it's written, the lām (ل) resembles a curved "J" or a hook,
            // and the alif (ا) is a straight vertical stroke that looks like an "I".
            this.textIsArabic = true;
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
                if (textIsArabic) {
                    line = Bidi.ReorderVisually(line);
                    textLines.Add(new TextLine(font, line));
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

        public void SetUnderline(bool underline) {
            this.underline = underline;
        }

        private void RightAlignText(TextLine[] textLines) {
            foreach (TextLine textLine in textLines) {
                textLine.xOffset =
                    this.width - font.StringWidth(fallbackFont, fontSize, textLine.text);
            }
        }

        private void CenterText(TextLine[] textLines) {
            foreach (TextLine textLine in textLines) {
                textLine.xOffset =
                    (this.width - font.StringWidth(fallbackFont, fontSize, textLine.text)) / 2f;
            }
        }

        private void UnderlineText(TextLine[] textLines) {
            foreach (TextLine textLine in textLines) {
                textLine.underline = true;
            }
        }

        private void ReorderVisually(TextLine[] textLines) {
            foreach (TextLine textLine in textLines) {
                textLine.text = Bidi.ReorderVisually(textLine.text);
            }
        }

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

            page.Append("q\n");
            page.SetPenWidth(this.borderWidth);
            if (textAlignment == Alignment.RIGHT) {
                RightAlignText(textLines);
            } else if (textAlignment == Alignment.CENTER) {
                CenterText(textLines);
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
            page.Append("Q\n");

            return new float[] {
                this.x + this.width,
                MathF.Max(this.y + this.height, this.y + textLines.Length * leading + 2 * this.textPadding)
            };
        }
    }
}
