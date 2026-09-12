/*
 * TextBlock.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
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


        private string language = null;
        private string uri = null;
        private string key = null;
        private string uriLanguage = null;
        private string uriActualText = null;
        private string uriAltDescription = null;
        private bool underline = false;
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

        /// <summary>Sets the size of the fallback font.</summary>
        public TextBlock SetFallbackFontSize(float fontSize) {
            this.fallbackFont.SetSize(fontSize);
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

        /// <summary>Sets the width of this text block and resets its height, so the height fits the text.</summary>
        public TextBlock SetWidth(double width) {
            return SetWidth((float) width);
        }

        /// <summary>Sets the width of this text block and resets its height, so the height fits the text.</summary>
        public TextBlock SetWidth(float width) {
            this.width = width;
            this.height = 0.0f;
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

        /// <summary>Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.</summary>
        public TextBlock SetBackgroundColor(int color) {
            return SetFillColor(color);
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

        /// <summary>
        /// Sets the language of the text, for example "he", "ar" or "fa", as a BCP 47
        /// language tag. The text is marked with it, for screen readers and text
        /// extraction.
        /// </summary>
        public TextBlock SetLanguage(string language) {
            this.language = language;
            return this;
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

        // The ASCII whitespace that Java's \s matches; a no-break space does not break a line.
        private static readonly char[] whitespace = new char[] {' ', '\t', '\n', '\x0B', '\f', '\r'};

        private TextLine[] GetTextLines() {
            List<TextLine> textLines = new List<TextLine>();

            float textAreaWidth = this.width - 2 * this.textPadding;
            // Like String.split in Java: the trailing empty lines are dropped, but an empty text is one empty line.
            List<string> lines = new List<string>(this.textContent.Replace("\r\n", "\n").Split('\n'));
            while (lines.Count > 1 && lines[lines.Count - 1].Length == 0) {
                lines.RemoveAt(lines.Count - 1);
            }
            foreach (String line in lines) {
                if (rightToLeft) {
                    AddRightToLeftLines(textLines, line, textAreaWidth);
                    continue;
                }

                // A zero width space marks a place where the line may break in text
                // without spaces between its words, like Thai text. It is not drawn.
                String text = line.Replace("\u200B", "");
                if (this.font.StringWidth(fallbackFont, fontSize, text) <= textAreaWidth) {
                    textLines.Add(new TextLine(font, text));
                } else {
                    if (TextIsCJK(text)) {
                        StringBuilder sb = new StringBuilder();
                        foreach (char ch in text.ToCharArray()) {
                            if (font.StringWidth(fallbackFont, fontSize, sb.ToString() + ch) <= textAreaWidth) {
                                sb.Append(ch);
                            } else {
                                if (sb.Length > 0) {    // Don't emit an empty line
                                    textLines.Add(new TextLine(font, sb.ToString()));
                                }
                                sb.Clear();
                                sb.Append(ch);
                            }
                        }
                        if (sb.ToString().Trim().Length > 0) {
                            textLines.Add(new TextLine(font, sb.ToString().Trim()));
                        }
                    } else {
                        StringBuilder sb = new StringBuilder();
                        string[] tokens = line.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string token in tokens) {
                            // The words between the zero width spaces of a token
                            // are joined with no space.
                            string[] words = token.Split('\u200B');
                            for (int i = 0; i < words.Length; i++) {
                                String word = words[i];
                                String separator = (i == words.Length - 1) ? " " : "";
                                if (this.font.StringWidth(fallbackFont, fontSize, sb.ToString() + word) <= textAreaWidth) {
                                    sb.Append(word).Append(separator);
                                } else {
                                    if (sb.Length > 0) {
                                        textLines.Add(new TextLine(font, sb.ToString().Trim()));
                                        sb.Clear();
                                    }
                                    // A word too wide for a line by itself is broken.
                                    int rest = AddBrokenWordLines(textLines, word, textAreaWidth);
                                    if (rest < word.Length) {
                                        sb.Append(word.Substring(rest)).Append(separator);
                                    }
                                }
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
        /// Adds the lines of a word too wide for a line by itself, broken between its
        /// characters, and returns the rest of the word, which fits on a line. No
        /// line starts with a combining mark, or with a Thai or Lao vowel or sign
        /// written after its consonant, and none ends with a Thai or Lao vowel
        /// written before its consonant.
        /// </summary>
        private int AddBrokenWordLines(List<TextLine> textLines, String word, float textAreaWidth) {
            int start = 0;
            while (LineWidth(word, start) > textAreaWidth) {
                // Each line gets at least one character, however narrow the block.
                int end = NextCharacterBreak(word, start);
                int next = NextCharacterBreak(word, end);
                while (next < word.Length && LineWidth(word.Substring(0, next), start) <= textAreaWidth) {
                    end = next;
                    next = NextCharacterBreak(word, end);
                }
                textLines.Add(NewTextLine(word.Substring(0, end), start));
                start = end;
            }
            return start;
        }

        // Returns the part of the text from the index on, reordered and shaped in
        // the context of the whole text if the text is right to left.
        private String Part(String text, int from, int to) {
            return rightToLeft ? Bidi.ReorderVisually(text, from, to) : text.Substring(from, to - from);
        }

        // Returns the width of a line of text, measured after the line is
        // reordered if the text is right to left.
        private float LineWidth(String text, int from) {
            return font.StringWidth(fallbackFont, fontSize, Part(text, from, text.Length));
        }

        // Returns a line of text, reordered if the text is right to left.
        private TextLine NewTextLine(String text, int from) {
            int to = text.Length;
            while (to > from && Char.IsWhiteSpace(text[to - 1])) {
                to--;
            }
            return new TextLine(font, Part(text, from, to));
        }

        private static int NextCharacterBreak(String word, int i) {
            int ch = CodePointAt(word, i);
            i += (ch > 0xFFFF) ? 2 : 1;
            while (IsLeadingVowel(ch) && i < word.Length) {
                ch = CodePointAt(word, i);
                i += (ch > 0xFFFF) ? 2 : 1;
            }
            while (i < word.Length && StaysWithPrevious(CodePointAt(word, i))) {
                i += (CodePointAt(word, i) > 0xFFFF) ? 2 : 1;
            }
            return i;
        }

        private static int CodePointAt(String str, int i) {
            return Char.IsSurrogatePair(str, i) ? Char.ConvertToUtf32(str, i) : str[i];
        }

        // The Thai and Lao vowels written before the consonant they follow in speech.
        private static bool IsLeadingVowel(int ch) {
            return (ch >= 0x0E40 && ch <= 0x0E44) || (ch >= 0x0EC0 && ch <= 0x0EC4);
        }

        // The combining marks, and the Thai and Lao vowels and signs written after
        // a consonant, like SARA AA and MAI YAMOK, which do not start a line.
        private static bool StaysWithPrevious(int ch) {
            if (ch == 0x200C || ch == 0x200D) {         // ZWNJ, ZWJ
                return true;
            }
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return cat == UnicodeCategory.NonSpacingMark ||
                    cat == UnicodeCategory.SpacingCombiningMark ||
                    cat == UnicodeCategory.EnclosingMark ||
                    (ch >= 0x0E2F && ch <= 0x0E3A) || (ch >= 0x0E45 && ch <= 0x0E4E) ||
                    (ch >= 0x0EAF && ch <= 0x0EBC) || (ch >= 0x0EC6 && ch <= 0x0ECE);
        }

        /// <summary>
        /// Wraps a paragraph of right to left text at the spaces between words and
        /// at its zero width spaces, and adds its lines in visual order. The
        /// paragraph is wrapped in logical order, so its first words go on the
        /// first line, and each line is measured after it is reordered, since the
        /// shaped Arabic letters differ in width from the letters they replace. A
        /// word too wide for a line by itself is broken between its characters.
        /// </summary>
        private void AddRightToLeftLines(List<TextLine> textLines, string paragraph, float textAreaWidth) {
            // sb holds the words of the line in logical order. When the line
            // starts with the rest of a word broken over the lines, sb holds the
            // whole word and from is where the rest starts: the part before it
            // is not drawn, but it is the context that gives the first letter
            // of the rest its joined form.
            StringBuilder sb = new StringBuilder();
            int from = 0;
            foreach (string token in paragraph.Split(whitespace, StringSplitOptions.RemoveEmptyEntries)) {
                // The words between the zero width spaces of a token are joined
                // with no space.
                string[] words = token.Split('\u200B');
                for (int i = 0; i < words.Length; i++) {
                    String word = words[i];
                    String separator = (i == words.Length - 1) ? " " : "";
                    if (LineWidth(sb.ToString() + word, from) <= textAreaWidth) {
                        sb.Append(word).Append(separator);
                    } else {
                        if (sb.Length > from) {
                            textLines.Add(NewTextLine(sb.ToString(), from));
                            sb.Clear();
                            from = 0;
                        }
                        // A word too wide for a line by itself is broken.
                        int rest = AddBrokenWordLines(textLines, word, textAreaWidth);
                        if (rest < word.Length) {
                            sb.Append(word).Append(separator);
                            from = rest;
                        }
                    }
                }
            }
            textLines.Add(NewTextLine(sb.ToString(), from));
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
            TextLine[] textLines = GetTextLines();
            float blockHeight = MathF.Max(this.height, textLines.Length * leading + 2 * this.textPadding);
            if (page == null) {
                return new float[] {this.x + this.width, this.y + blockHeight};
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
                Rect rect = new Rect(this.x, this.y, this.width, blockHeight);
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
                this.keywordHighlightColors,
                this.language);
            page.AddEMC();
            page.RestoreGraphicsState();

            if (uri != null || key != null) {
                page.AddAnnotation(new Annotation(
                        Annotation.Link,
                        this.x,
                        this.y,
                        this.x + this.width,
                        this.y + blockHeight,
                        null,   // Vertices
                        null,   // Fill Color
                        0f,     // Transparency
                        null,   // Title
                        null,   // Contents
                        uri,
                        key,    // The destination name
                        uriLanguage,
                        uriActualText,
                        uriAltDescription));
            }

            return new float[] {this.x + this.width, this.y + blockHeight};
        }
    }
}
