using System;
using System.Collections.Generic;

namespace PDFjet.NET {
    public class TextBlock {
        private float x;
        private float y;
        private float width;
        private float height;
        private Font font;
        private Font fallbackFont;
        private string textContent;
        private float textLineHeight;
        private int textColor;
        private float textPadding;
        private float borderWidth;
        private float borderCornerRadius;
        private int borderColor;
        private string language;
        private string altDescription;
        private string uri;
        private string key;
        private string uriLanguage;
        private string uriActualText;
        private string uriAltDescription;
        private Direction textDirection;
        private Alignment textAlignment;
        private bool underline;
        private bool strikeout;

        private Dictionary<string, int> colors;

        public TextBlock(Font font, string textContent) {
            this.x = 0.0f;
            this.y = 0.0f;
            this.width = 500.0f;
            this.height = 500.0f;
            this.font = font;
            this.textContent = textContent;
            this.textLineHeight = 1.0f;
            this.textColor = Color.black;
            this.textPadding = 0.0f;
            this.textDirection = Direction.LEFT_TO_RIGHT;
            this.textAlignment = Alignment.LEFT;

            this.borderWidth = 0.5f;
            this.borderCornerRadius = 0.0f;
            this.borderColor = Color.black;

            this.language = "en-US";
            this.altDescription = "";
            this.underline = false;
            this.strikeout = false;
        }

        public void SetFont(Font font) {
            this.font = font;
        }

        public void SetFallbackFont(Font font) {
            this.fallbackFont = font;
        }

        public void SetFontSize(float size) {
            this.font.SetSize(size);
        }

        public void SetFallbackFontSize(float size) {
            this.fallbackFont?.SetSize(size);
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

        public void SetSize(float w, float h) {
            this.width = w;
            this.height = h;
        }

        public void SetWidth(float w) {
            this.width = w;
            this.height = 0.0f;
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

        public void SetBorderColor(int borderColor) {
            this.borderColor = borderColor;
        }

        public void SetTextLineHeight(float textLineHeight) {
            this.textLineHeight = textLineHeight;
        }

        public void SetTextColor(int textColor) {
            this.textColor = textColor;
        }

        public void SetHighlightColors(Dictionary<string, int> colors) {
            this.colors = colors;
        }

        public void SetTextAlignment(Alignment textAlignment) {
            this.textAlignment = textAlignment;
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

        private List<string> GetTextLines() {
            List<string> list = new List<string>();

            float textAreaWidth;
            if (this.textDirection == Direction.LEFT_TO_RIGHT) {
                textAreaWidth = this.width - 2 * this.textPadding;
            } else {
                textAreaWidth = this.height - 2 * this.textPadding;
            }

            this.textContent = this.textContent.Replace("\r\n", "\n").Trim();
            string[] lines = this.textContent.Split('\n');
            foreach (string line in lines) {
                if (this.font.StringWidth(this.fallbackFont, line) <= textAreaWidth) {
                    list.Add(line);
                } else {
                    if (TextIsCJK(line)) {
                        var sb = new System.Text.StringBuilder();
                        foreach (char ch in line.ToCharArray()) {
                            if (this.font.StringWidth(this.fallbackFont, sb.ToString() + ch) <= textAreaWidth) {
                                sb.Append(ch);
                            } else {
                                list.Add(sb.ToString());
                                sb.Clear();
                                sb.Append(ch);
                            }
                        }
                        if (sb.Length > 0) {
                            list.Add(sb.ToString());
                        }
                    } else {
                        var sb = new System.Text.StringBuilder();
                        string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string token in tokens) {
                            if (this.font.StringWidth(this.fallbackFont, sb.ToString() + token) <= textAreaWidth) {
                                sb.Append(token).Append(" ");
                            } else {
                                list.Add(sb.ToString().Trim());
                                sb.Clear();
                                sb.Append(token).Append(" ");
                            }
                        }
                        if (sb.ToString().Trim().Length > 0) {
                            list.Add(sb.ToString().Trim());
                        }
                    }
                }
            }

            return list;
        }

        public float[] DrawOn(Page page) {
            if (page != null) {
                // TODO: Deal with this now!!
            }

            page.SetBrushColor(this.textColor);
            page.SetPenWidth(this.font.GetUnderlineThickness());
            page.SetPenColor(this.borderColor);

            float ascent = this.font.GetAscent();
            float descent = this.font.GetDescent();
            float leading = (ascent + descent) * this.textLineHeight;
            List<string> lines = GetTextLines();
            float xText = 0.0f;
            float yText = 0.0f;
            switch (this.textDirection) {
                case Direction.LEFT_TO_RIGHT:
                    yText = this.y + ascent + this.textPadding;
                    foreach (string line in lines) {
                        switch (this.textAlignment) {
                            case Alignment.LEFT:
                                xText = this.x + this.textPadding;
                                break;
                            case Alignment.RIGHT:
                                xText = (this.x + this.width) -
                                    (this.font.StringWidth(this.fallbackFont, line) + this.textPadding);
                                break;
                            case Alignment.CENTER:
                                xText = this.x + (this.width - this.font.StringWidth(this.fallbackFont, line)) / 2;
                                break;
                        }
                        DrawTextLine(page, this.font, this.fallbackFont, line, xText, yText, this.textColor, this.colors);
                        yText += leading;
                    }
                    break;
                case Direction.BOTTOM_TO_TOP:
                    xText = this.x + this.textPadding + ascent;
                    yText = this.y + this.height - this.textPadding;
                    foreach (string line in lines) {
                        DrawTextLine(page, this.font, this.fallbackFont, line, xText, yText, this.textColor, this.colors);
                        xText += leading;
                    }
                    break;
                case Direction.TOP_TO_BOTTOM:
                    break;
            }

            xText -= leading;
            if ((xText + descent + this.textPadding) - this.x > this.width) {
                this.width = (xText + descent + this.textPadding) - this.x;
            }
            yText -= leading;
            if ((yText + descent + this.textPadding) - this.y > this.height) {
                this.height = (yText + descent + this.textPadding) - this.y;
            }

            Rect rect = new Rect(this.x, this.y, this.width, this.height);
            rect.SetBorderColor(this.borderColor);
            rect.SetCornerRadius(this.borderCornerRadius);
            rect.DrawOn(page);

            if (this.textDirection == Direction.LEFT_TO_RIGHT && (this.uri != null || this.key != null)) {
                page.AddAnnotation(new Annotation(
                    this.uri,
                    this.key,
                    this.x,
                    this.y,
                    this.x + this.width,
                    this.y + this.height,
                    this.uriLanguage,
                    this.uriActualText,
                    this.uriAltDescription));
            }
            page.SetTextDirection(0);

            return new float[] { this.x + this.width, this.y + this.height };
        }

        private void DrawTextLine(
                Page page,
                Font font,
                Font fallbackFont,
                string text,
                float xText,
                float yText,
                int brush,
                Dictionary<string, int> colors) {
            page.AddBMC("P", this.language, text, this.altDescription);
            if (this.textDirection == Direction.BOTTOM_TO_TOP) {
                page.SetTextDirection(90);
            }
            page.DrawString(font, fallbackFont, text, xText, yText, brush, colors);
            page.AddEMC();

            if (this.textDirection == Direction.LEFT_TO_RIGHT) {
                float lineLength = this.font.StringWidth(fallbackFont, text);
                if (this.underline) {
                    page.AddArtifactBMC();
                    page.MoveTo(xText, yText + font.GetUnderlinePosition());
                    page.LineTo(xText + lineLength, yText + font.GetUnderlinePosition());
                    page.StrokePath();
                    page.AddEMC();
                }
                if (this.strikeout) {
                    page.AddArtifactBMC();
                    page.MoveTo(xText, yText - (font.GetBodyHeight() / 4));
                    page.LineTo(xText + lineLength, yText - (font.GetBodyHeight() / 4));
                    page.StrokePath();
                    page.AddEMC();
                }
            }
        }

        public TextBlock SetURIAction(string uri) {
            this.uri = uri;
            return this;
        }

        public void SetTextDirection(Direction textDirection) {
            this.textDirection = textDirection;
        }
    }
}
