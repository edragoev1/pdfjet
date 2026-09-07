/*
 * TextBlock.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class TextBlock {
    float x;
    float y;
    private float width;
    private float height;
    private Font font;
    private Font fallbackFont;
    private float fontSize = 12f;
    private String textContent;
    private float lineSpacing = 1.0f;
    private float[] textColor;
    private Map<String, Integer> keywordHighlightColors;
    private float textPadding;
    private float[] fillColor;
    private float borderWidth = 0.5f;
    private float[] borderColor;
    private float borderCornerRadius = 0.0f;

    private String language;
    private String altDescription;
    private String uri;
    private String key;
    private String uriLanguage;
    private String uriActualText;
    private String uriAltDescription;
    private Alignment textAlignment;
    private boolean underline;
    private boolean strikeout;

    /**
     * Creates a text block and sets the font.
     *
     * @param font the font.
     * @param textContent the text content.
     */
    public TextBlock(Font font, String textContent) {
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
     * Sets the position where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public void setPosition(double x, double y) {
        setPosition((float) x, (float) y);
    }

    /**
     * Sets the position where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public void setPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public void setFont(Font font) {
        this.font = font;
        this.fallbackFont = font;
    }

    public void setFallbackFont(Font font) {
        this.fallbackFont = font;
    }

    public TextBlock setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
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

    public TextBlock setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public void setSize(float w, float h) {
        this.width = w;
        this.height = h;
    }

    public void setWidth(float w) {
        this.width = w;
        this.height = 0.0f;
    }

    public float getWidth() {
        return this.width;
    }

    public float getHeight() {
        return this.height;
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

    public void setTextColor(int color) {
        if (color == Color.transparent) {
            this.textColor = null;
            return;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
    }

    public void setTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
    }

    public void setBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            return;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.borderColor = new float[] {r, g, b};
    }

    public void setBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
    }

    public void setLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
    }

    public void setBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void setFillColor(int color) {
        if (color == Color.transparent) {
            this.fillColor = null;
            return;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void setBackgroundColor(float[] fillColor) {
        this.fillColor = fillColor;
    }

    public void setTextAlignment(Alignment textAlignment) {
        this.textAlignment = textAlignment;
    }

    public TextBlock setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    public void setKeywordHighlightColors(Map<String, Integer> map) {
        this.keywordHighlightColors = new HashMap<>();
        for (String key : map.keySet()) {
            this.keywordHighlightColors.put(key.toLowerCase(), map.get(key));
        }
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

    private TextLine[] getTextLines() {
        List<TextLine> textLines = new ArrayList<>();

        float textAreaWidth = this.width - 2 * this.textPadding;
        String[] lines = this.textContent.split("\r?\n");
        for (String line : lines) {
            if (font.stringWidth(fallbackFont, line) <= textAreaWidth) {
                textLines.add(new TextLine(font, line));
            } else {
                if (textIsCJK(line)) {
                    StringBuilder sb = new StringBuilder();
                    for (char ch : line.toCharArray()) {
                        if (font.stringWidth(fallbackFont, sb.toString() + ch) <= textAreaWidth) {
                            sb.append(ch);
                        } else {
                            textLines.add(new TextLine(font, sb.toString()));
                            sb.setLength(0);
                            sb.append(ch);
                        }
                    }
                    if (sb.toString().trim().length() > 0) {
                        textLines.add(new TextLine(font, sb.toString().trim()));
                    }
                } else {
                    StringBuilder sb = new StringBuilder();
                    String[] tokens = line.split("\\s+");
                    for (String token : tokens) {
                        if (font.stringWidth(fallbackFont, sb.toString() + token) <= textAreaWidth) {
                            sb.append(token);
                            sb.append(" ");
                        } else {
                            textLines.add(new TextLine(font, sb.toString().trim()));
                            sb.setLength(0);
                            sb.append(token + " ");
                        }
                    }
                    if (sb.toString().trim().length() > 0) {
                        textLines.add(new TextLine(font, sb.toString().trim()));
                    }
                }
            }
        }

        return textLines.toArray(new TextLine[] {});
    }

    public void setUnderline(boolean underline) {
        this.underline = underline;
    }

    private void rightAlignText(TextLine[] textLines) {
        for (TextLine textLine : textLines) {
            textLine.xOffset = this.width - font.stringWidth(textLine.text);
        }
    }

    private void centerText(TextLine[] textLines) {
        for (TextLine textLine : textLines) {
            textLine.xOffset = (this.width - font.stringWidth(textLine.text)) / 2f;
        }
    }

    private void underlineText(TextLine[] textLines) {
        for (TextLine textLine : textLines) {
            textLine.underline = true;
        }
    }

    public float[] drawOn(Page page) throws Exception {
        float ascent = this.font.getAscent(fontSize);
        float descent = this.font.getDescent(fontSize);
        float leading = (ascent + descent) * this.lineSpacing;
        TextLine[] textLines = getTextLines();
        if (page == null) {
            return new float[] {
                this.width,
                Math.max(this.height, textLines.length * leading + 2 * this.textPadding)
            };
        }

        page.saveGraphicsState();
        page.setPenWidth(this.borderWidth);
        if (textAlignment == Alignment.RIGHT) {
            rightAlignText(textLines);
        } else if (textAlignment == Alignment.CENTER) {
            centerText(textLines);
        }
        if (underline) {
            underlineText(textLines);
        }

        if (borderColor != null || fillColor != null) {
            Rect rect = new Rect(
                this.x,
                this.y,
                this.width,
                Math.max(this.height, textLines.length * leading + 2 * this.textPadding));
            if (borderColor != null) {
                rect.setBorderColor(this.borderColor);
                rect.setBorderWidth(this.borderWidth);
                rect.setCornerRadius(this.borderCornerRadius);
            }
            if (fillColor != null) {
                rect.setFillColor(this.fillColor);
            }
            rect.drawOn(page);
        }

        page.addBMC(StructElem.P, this.language, this.textContent, null);
        page.drawTextBlock(
            this.font,
            this.fontSize,
            textLines,
            this.x + this.textPadding,
            this.y + this.textPadding,
            leading,
            this.textColor,
            keywordHighlightColors);
        page.addEMC();
        page.restoreGraphicsState();

        return new float[] {
            this.x + this.width,
            Math.max(this.y + this.height, this.y + textLines.length * leading + 2 * this.textPadding)
        };
    }
}
