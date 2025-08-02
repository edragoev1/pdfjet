package com.pdfjet;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * TextBlock.java
 *
 * ©2025 PDFJet Software
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
public class TextBlock {
    private float x;
    private float y;
    private float width;
    private float height;
    private Font font;
    private Font fallbackFont;
    private String textContent;
    private float textLineHeight;
    private int textColor;
    private float textPadding;
    private float borderWidth;
    private float borderCornerRadius;
    private int borderColor;
    private String language;
    private String altDescription;
    private String uri;
    private String key;
    private String uriLanguage;
    private String uriActualText;
    private String uriAltDescription;
    private Direction textDirection;
    private Alignment textAlignment;
    private boolean underline;
    private boolean strikeout;

    private Map<String, Integer> colors;

    /**
     * Creates a text block and sets the font.
     *
     * @param font the font.
     * @param textContent the text content.
     */
    public TextBlock(Font font, String textContent) {
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

    public void setFont(Font font) {
        this.font = font;
    }

    public void setFallbackFont(Font font) {
        this.fallbackFont = font;
    }

    public void setFontSize(float size) {
        this.font.setSize(size);
    }

    public void setFallbackFontSize(float size) {
        if (this.fallbackFont != null) {
            this.fallbackFont.setSize(size);
        }
    }

    public void setText(String text) {
        this.textContent = text;
    }

    public Font getFont() {
        return this.font;
    }

    public String getText() {
        return this.textContent;
    }

    public void setLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public void setSize(float w, float h) {
        this.width = w;
        this.height = h;
    }

    public void setWidth(float w) {
        this.width = w;
        this.height = 0.0f;
    }

    public void setBorderCornerRadius(float borderCornerRadius) {
        this.borderCornerRadius = borderCornerRadius;
    }

    public void setTextPadding(float padding) {
        this.textPadding = padding;
    }

    public void setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
    }

    public void setBorderColor(int borderColor) {
        this.borderColor = borderColor;
    }

    public void setTextLineHeight(float textLineHeight) {
        this.textLineHeight = textLineHeight;
    }

    public void setTextColor(int textColor) {
        this.textColor = textColor;
    }

    public void setHighlightColors(Map<String, Integer> colors) {
        this.colors = colors;
    }

    public void setTextAlignment(Alignment textAlignment) {
        this.textAlignment = textAlignment;
    }

    private boolean textIsCJK(String str) {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        int numOfCJK = 0;
        char[] chars = str.toCharArray();
        for (char ch : chars) {
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                (ch >= 0x3040 && ch <= 0x309F) ||
                (ch >= 0x30A0 && ch <= 0x30FF) ||
                (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK++;
            }
        }
        return numOfCJK > (chars.length / 2);
    }

    private List<String> getTextLines() {
        List<String> list = new ArrayList<String>();

        float textAreaWidth;
        if (this.textDirection == Direction.LEFT_TO_RIGHT) {
            textAreaWidth = this.width - 2 * this.textPadding;
        } else {
            // When writing text vertically!
            textAreaWidth = this.height - 2 * this.textPadding;
        }

        this.textContent = this.textContent.replace("\r\n", "\n").trim();
        String[] lines = this.textContent.split("\n");
        for (String line : lines) {
            if (this.font.stringWidth(this.fallbackFont, line) <= textAreaWidth) {
                list.add(line);
            } else {
                if (textIsCJK(line)) {
                    StringBuilder sb = new StringBuilder();
                    for (char ch : line.toCharArray()) {
                        if (this.font.stringWidth(
                                this.fallbackFont, sb.toString() + ch) <= textAreaWidth) {
                            sb.append(ch);
                        } else {
                            list.add(sb.toString());
                            sb.setLength(0);
                            sb.append(ch);
                        }
                    }
                    if (sb.length() > 0) {
                        list.add(sb.toString());
                    }
                } else {
                    StringBuilder sb = new StringBuilder();
                    String[] tokens = line.split("\\s+");
                    for (String token : tokens) {
                        if (this.font.stringWidth(
                                this.fallbackFont, sb.toString() + token) <= textAreaWidth) {
                            sb.append(token).append(" ");
                        } else {
                            list.add(sb.toString().trim());
                            sb.setLength(0);
                            sb.append(token).append(" ");
                        }
                    }
                    if (sb.toString().trim().length() > 0) {
                        list.add(sb.toString().trim());
                    }
                }
            }
        }

        return list;
    }

    public float[] drawOn(Page page) throws Exception {
        if (page != null) {
            // TODO: Deal with this now!!
        }

        page.setBrushColor(this.textColor);
        page.setPenWidth(this.font.getUnderlineThickness());
        page.setPenColor(this.borderColor);

        float ascent = this.font.getAscent();
        float descent = this.font.getDescent();
        float leading = (ascent + descent) * this.textLineHeight;
        List<String> lines = getTextLines();
        float xText = 0.0f;
        float yText = 0.0f;
        switch (this.textDirection) {
            case Direction.LEFT_TO_RIGHT:
                yText = this.y + ascent + this.textPadding;
                for (String line : lines) {
                    switch (this.textAlignment) {
                        case Alignment.LEFT:
                            xText = this.x + this.textPadding;
                            break;
                        case Alignment.RIGHT:
                            xText = (this.x + this.width) -
                                (this.font.stringWidth(this.fallbackFont, line) + this.textPadding);
                            break;
                        case Alignment.CENTER:
                            xText = this.x + (this.width - this.font.stringWidth(this.fallbackFont, line)) / 2;
                            break;
                    }
                    drawTextLine(
                            page,
                            this.font,
                            this.fallbackFont,
                            line,
                            xText,
                            yText,
                            this.textColor,
                            this.colors);
                    yText += leading;
                }
                break;
            case Direction.BOTTOM_TO_TOP:
                xText = this.x + this.textPadding + ascent;
                yText = this.y + this.height - this.textPadding;
                for (String line : lines) {
                    drawTextLine(
                            page,
                            this.font,
                            this.fallbackFont,
                            line,
                            xText,
                            yText,
                            this.textColor,
                            this.colors);
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
        rect.setBorderColor(this.borderColor);
        rect.setCornerRadius(this.borderCornerRadius);
        rect.drawOn(page);

        if (this.textDirection == Direction.LEFT_TO_RIGHT && (this.uri != null || this.key != null)) {
            page.addAnnotation(new Annotation(
                    this.uri,
                    this.key, // The destination name
                    this.x,
                    this.y,
                    this.x + this.width,
                    this.y + this.height,
                    this.uriLanguage,
                    this.uriActualText,
                    this.uriAltDescription));
        }
        page.setTextDirection(0);

        return new float[] { this.x + this.width, this.y + this.height };
    }

    private void drawTextLine(
            Page page,
            Font font,
            Font fallbackFont,
            String text,
            float xText,
            float yText,
            int brush,
            Map<String, Integer> colors) throws Exception {
        page.addBMC("P", this.language, text, this.altDescription);
        if (this.textDirection == Direction.BOTTOM_TO_TOP) {
            page.setTextDirection(90);
        }
        page.drawString(font, fallbackFont, text, xText, yText, brush, colors);
        page.addEMC();
        if (this.textDirection == Direction.LEFT_TO_RIGHT) {
            float lineLength = this.font.stringWidth(fallbackFont, text);
            if (this.underline) {
                page.addArtifactBMC();
                page.moveTo(xText, yText + font.getUnderlinePosition());
                page.lineTo(xText + lineLength, yText + font.getUnderlinePosition());
                page.strokePath();
                page.addEMC();
            }
            if (this.strikeout) {
                page.addArtifactBMC();
                page.moveTo(xText, yText - (font.getBodyHeight() / 4));
                page.lineTo(xText + lineLength, yText - (font.getBodyHeight() / 4));
                page.strokePath();
                page.addEMC();
            }
        }
    }

    public TextBlock setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    public void setTextDirection(Direction textDirection) {
        this.textDirection = textDirection;
    }
}
