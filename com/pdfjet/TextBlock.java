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

/**
 * A block of text that wraps at its width, with an optional border, background and padding.
 */
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
    private boolean rightToLeft;

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
     * @return this TextBlock object.
     */
    public TextBlock setLocation(double x, double y) {
        setLocation((float) x, (float) y);
        return this;
    }

    /**
     * Sets the font of the text. It also becomes the fallback font.
     *
     * @param font the font.
     * @return this TextBlock object.
     */
    public TextBlock setFont(Font font) {
        this.font = font;
        this.fallbackFont = font;
        return this;
    }

    /**
     * Sets the font used for the characters the main font does not have.
     *
     * @param font the fallback font.
     * @return this TextBlock object.
     */
    public TextBlock setFallbackFont(Font font) {
        this.fallbackFont = font;
        return this;
    }

    /**
     * Sets the font size of the text.
     *
     * @param fontSize the font size.
     * @return this TextBlock object.
     */
    public TextBlock setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     * Sets the text.
     *
     * @param text the text.
     * @return this TextBlock object.
     */
    public TextBlock setText(String text) {
        this.textContent = text;
        return this;
    }

    /**
     * Returns the font of the text.
     *
     * @return the font.
     */
    public Font getFont() {
        return this.font;
    }

    /**
     * Returns the text.
     *
     * @return the text.
     */
    public String getText() {
        return this.textContent;
    }

    /**
     * Sets the location of the top left corner of this text block.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this TextBlock object.
     */
    public TextBlock setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the size of this text block.
     *
     * @param w the width.
     * @param h the height.
     * @return this TextBlock object.
     */
    public TextBlock setSize(float w, float h) {
        this.width = w;
        this.height = h;
        return this;
    }

    /**
     * Sets the width of this text block and resets its height, so the height fits the text.
     *
     * @param w the width.
     * @return this TextBlock object.
     */
    public TextBlock setWidth(float w) {
        this.width = w;
        this.height = 0.0f;
        return this;
    }

    /**
     * Returns the width of this text block.
     *
     * @return the width.
     */
    public float getWidth() {
        return this.width;
    }

    /**
     * Returns the height of this text block.
     *
     * @return the height.
     */
    public float getHeight() {
        return this.height;
    }

    /**
     * Sets the radius of the border corners.
     *
     * @param borderCornerRadius the corner radius.
     * @return this TextBlock object.
     */
    public TextBlock setBorderCornerRadius(float borderCornerRadius) {
        this.borderCornerRadius = borderCornerRadius;
        return this;
    }

    /**
     * Sets the space between the text and the border.
     *
     * @param padding the padding.
     * @return this TextBlock object.
     */
    public TextBlock setTextPadding(float padding) {
        this.textPadding = padding;
        return this;
    }

    /**
     * Sets the border width.
     *
     * @param borderWidth the border width.
     * @return this TextBlock object.
     */
    public TextBlock setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /**
     * Sets the text color.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBlock object.
     */
    public TextBlock setTextColor(int color) {
        if (color == Color.transparent) {
            this.textColor = null;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the text color.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBlock object.
     */
    public TextBlock setTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    /**
     * Sets the border color. Color.transparent removes the border.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBlock object.
     */
    public TextBlock setBorderColor(int color) {
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

    /**
     * Sets the border color.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBlock object.
     */
    public TextBlock setBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        return this;
    }

    /**
     * Sets the line spacing as a multiple of the font's body height.
     *
     * @param lineSpacing the line spacing.
     * @return this TextBlock object.
     */
    public TextBlock setLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
        return this;
    }

    /**
     * Sets the background color.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBlock object.
     */
    public TextBlock setBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the background color. Color.transparent removes the background.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBlock object.
     */
    public TextBlock setFillColor(int color) {
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

    /**
     * Sets the background color.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBlock object.
     */
    public TextBlock setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /**
     * Sets the background color.
     *
     * @param fillColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBlock object.
     */
    public TextBlock setBackgroundColor(float[] fillColor) {
        this.fillColor = fillColor;
        return this;
    }

    /**
     * Sets the horizontal alignment of the text.
     *
     * @param textAlignment the alignment.
     * @return this TextBlock object.
     */
    public TextBlock setTextAlignment(Alignment textAlignment) {
        this.textAlignment = textAlignment;
        return this;
    }

    /**
     * Sets the URI opened when this text block is clicked.
     *
     * @param uri the URI.
     * @return this TextBlock object.
     */
    public TextBlock setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Sets the colors used to highlight keywords. The keywords are matched ignoring case.
     *
     * @param map the keywords and their 0xRRGGBB colors.
     * @return this TextBlock object.
     */
    public TextBlock setKeywordHighlightColors(Map<String, Integer> map) {
        this.keywordHighlightColors = new HashMap<>();
        for (String key : map.keySet()) {
            this.keywordHighlightColors.put(key.toLowerCase(), map.get(key));
        }
        return this;
    }

    /**
     * Sets whether the text is right to left, like Arabic and Hebrew text.
     * Each paragraph is wrapped at the width in logical order, and each line
     * is then reordered with Bidi.reorderVisually, which also shapes the
     * Arabic letters, and aligned to the right, unless the text alignment is
     * Alignment.CENTER.
     *
     * @param rightToLeft true if the text is right to left.
     * @return this TextBlock object.
     */
    public TextBlock setRightToLeft(boolean rightToLeft) {
        this.rightToLeft = rightToLeft;
        return this;
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
            if (rightToLeft) {
                addRightToLeftLines(textLines, line, textAreaWidth);
                continue;
            }
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

    /**
     * Wraps a paragraph of right to left text at the spaces between words and
     * adds its lines in visual order. The paragraph is wrapped in logical
     * order, so its first words go on the first line, and each line is
     * measured after it is reordered, since the shaped Arabic letters differ
     * in width from the letters they replace.
     */
    private void addRightToLeftLines(List<TextLine> textLines, String paragraph, float textAreaWidth) {
        String line = "";
        for (String word : paragraph.trim().split("\\s+")) {
            String candidate = line.isEmpty() ? word : line + " " + word;
            if (!line.isEmpty() &&
                    font.stringWidth(fallbackFont, Bidi.reorderVisually(candidate)) > textAreaWidth) {
                textLines.add(new TextLine(font, Bidi.reorderVisually(line)));
                line = word;
            } else {
                line = candidate;
            }
        }
        textLines.add(new TextLine(font, Bidi.reorderVisually(line)));
    }

    /**
     * Sets whether the text is underlined.
     *
     * @param underline true to underline the text.
     * @return this TextBlock object.
     */
    public TextBlock setUnderline(boolean underline) {
        this.underline = underline;
        return this;
    }

    // The offsets are from the left edge of the text, inside the padding.
    private void rightAlignText(TextLine[] textLines) {
        float textAreaWidth = this.width - 2 * this.textPadding;
        for (TextLine textLine : textLines) {
            textLine.xOffset = textAreaWidth - font.stringWidth(textLine.text);
        }
    }

    private void centerText(TextLine[] textLines) {
        float textAreaWidth = this.width - 2 * this.textPadding;
        for (TextLine textLine : textLines) {
            textLine.xOffset = (textAreaWidth - font.stringWidth(textLine.text)) / 2f;
        }
    }

    private void underlineText(TextLine[] textLines) {
        for (TextLine textLine : textLines) {
            textLine.underline = true;
        }
    }

    /**
     * Draws this text block on the specified page.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of this text block.
     * @throws Exception if an input or output exception occurred.
     */
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
        if (textAlignment == Alignment.CENTER) {
            centerText(textLines);
        } else if (textAlignment == Alignment.RIGHT || rightToLeft) {
            rightAlignText(textLines);
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
