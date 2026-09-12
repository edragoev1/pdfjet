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
public class TextBlock implements Drawable {
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
    private String uri;
    private String key;
    private String uriLanguage;
    private String uriActualText;
    private String uriAltDescription;
    private Alignment textAlignment;
    private boolean underline;
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
     * Sets the size of the fallback font.
     *
     * @param fontSize the fallback font size.
     * @return this TextBlock object.
     */
    public TextBlock setFallbackFontSize(float fontSize) {
        this.fallbackFont.setSize(fontSize);
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
     * Sets the height of this text block.
     *
     * @param h the height.
     * @return this TextBlock object.
     */
    public TextBlock setHeight(float h) {
        this.height = h;
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
     * Sets the background color. Color.transparent removes the background.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBlock object.
     */
    public TextBlock setBackgroundColor(int color) {
        return setFillColor(color);
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
     * Returns the background color.
     *
     * @return the red, green and blue components, from 0.0 to 1.0, or null.
     */
    public float[] getBackgroundColor() {
        return this.fillColor;
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
     * Sets the language of the text, for example "he", "ar" or "fa". The text is
     * marked with it, for screen readers and text extraction.
     *
     * @param language the language, as a BCP 47 language tag.
     * @return this TextBlock object.
     */
    public TextBlock setLanguage(String language) {
        this.language = language;
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
            // A zero width space marks a place where the line may break in text
            // without spaces between its words, like Thai text. It is not drawn.
            String text = line.replace("\u200B", "");
            if (font.stringWidth(fallbackFont, fontSize, text) <= textAreaWidth) {
                textLines.add(new TextLine(font, text));
            } else {
                if (textIsCJK(text)) {
                    StringBuilder sb = new StringBuilder();
                    for (char ch : text.toCharArray()) {
                        if (font.stringWidth(fallbackFont, fontSize, sb.toString() + ch) <= textAreaWidth) {
                            sb.append(ch);
                        } else {
                            if (sb.length() > 0) {  // Don't emit an empty line
                                textLines.add(new TextLine(font, sb.toString()));
                            }
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
                        if (token.isEmpty()) {  // Before leading whitespace
                            continue;
                        }
                        // The words between the zero width spaces of a token
                        // are joined with no space.
                        String[] words = token.split("\u200B", -1);
                        for (int i = 0; i < words.length; i++) {
                            String word = words[i];
                            String separator = (i == words.length - 1) ? " " : "";
                            if (font.stringWidth(fallbackFont, fontSize, sb.toString() + word) <= textAreaWidth) {
                                sb.append(word);
                                sb.append(separator);
                            } else {
                                if (sb.length() > 0) {
                                    textLines.add(new TextLine(font, sb.toString().trim()));
                                    sb.setLength(0);
                                }
                                // A word too wide for a line by itself is broken.
                                int rest = addBrokenWordLines(textLines, word, textAreaWidth);
                                if (rest < word.length()) {
                                    sb.append(word.substring(rest) + separator);
                                }
                            }
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
     * Adds the lines of a word too wide for a line by itself, broken between its
     * characters, and returns the index where the rest of the word, which fits
     * on a line, starts. No line starts with a combining mark, or with a Thai
     * or Lao vowel or sign written after its consonant, and none ends with a
     * Thai or Lao vowel written before its consonant. Right to left text is
     * shaped as a whole word, so its letters keep their joined forms at the
     * breaks.
     */
    private int addBrokenWordLines(List<TextLine> textLines, String word, float textAreaWidth) {
        int start = 0;
        while (lineWidth(word, start) > textAreaWidth) {
            // Each line gets at least one character, however narrow the block.
            int end = nextCharacterBreak(word, start);
            int next = nextCharacterBreak(word, end);
            while (next < word.length() && lineWidth(word.substring(0, next), start) <= textAreaWidth) {
                end = next;
                next = nextCharacterBreak(word, end);
            }
            textLines.add(newTextLine(word.substring(0, end), start));
            start = end;
        }
        return start;
    }

    // Returns the part of the text from the index on, reordered and shaped in
    // the context of the whole text if the text is right to left.
    private String part(String text, int from, int to) {
        return rightToLeft ? Bidi.reorderVisually(text, from, to) : text.substring(from, to);
    }

    // Returns the width of a line of text from the index on.
    private float lineWidth(String text, int from) {
        return font.stringWidth(fallbackFont, fontSize, part(text, from, text.length()));
    }

    // Returns a line of the text from the index on, without its trailing spaces.
    private TextLine newTextLine(String text, int from) {
        int to = text.length();
        while (to > from && Character.isWhitespace(text.charAt(to - 1))) {
            to--;
        }
        return new TextLine(font, part(text, from, to));
    }

    private static int nextCharacterBreak(String word, int i) {
        int ch = word.codePointAt(i);
        i += Character.charCount(ch);
        while (isLeadingVowel(ch) && i < word.length()) {
            ch = word.codePointAt(i);
            i += Character.charCount(ch);
        }
        while (i < word.length() && staysWithPrevious(word.codePointAt(i))) {
            i += Character.charCount(word.codePointAt(i));
        }
        return i;
    }

    // The Thai and Lao vowels written before the consonant they follow in speech.
    private static boolean isLeadingVowel(int ch) {
        return (ch >= 0x0E40 && ch <= 0x0E44) || (ch >= 0x0EC0 && ch <= 0x0EC4);
    }

    // The combining marks, and the Thai and Lao vowels and signs written after
    // a consonant, like SARA AA and MAI YAMOK, which do not start a line.
    private static boolean staysWithPrevious(int ch) {
        if (ch == 0x200C || ch == 0x200D) {         // ZWNJ, ZWJ
            return true;
        }
        int type = Character.getType(ch);
        return type == Character.NON_SPACING_MARK ||
                type == Character.COMBINING_SPACING_MARK ||
                type == Character.ENCLOSING_MARK ||
                (ch >= 0x0E2F && ch <= 0x0E3A) || (ch >= 0x0E45 && ch <= 0x0E4E) ||
                (ch >= 0x0EAF && ch <= 0x0EBC) || (ch >= 0x0EC6 && ch <= 0x0ECE);
    }

    /**
     * Wraps a paragraph of right to left text at the spaces between words and
     * at its zero width spaces, and adds its lines in visual order. The
     * paragraph is wrapped in logical order, so its first words go on the
     * first line, and each line is measured after it is reordered, since the
     * shaped Arabic letters differ in width from the letters they replace. A
     * word too wide for a line by itself is broken between its characters.
     */
    private void addRightToLeftLines(List<TextLine> textLines, String paragraph, float textAreaWidth) {
        // sb holds the words of the line in logical order. When the line starts
        // with the rest of a word broken over the lines, sb holds the whole word
        // and from is where the rest starts: the part before it is not drawn,
        // but it is the context that gives the first letter of the rest its
        // joined form.
        StringBuilder sb = new StringBuilder();
        int from = 0;
        for (String token : paragraph.trim().split("\\s+")) {
            // The words between the zero width spaces of a token are joined
            // with no space.
            String[] words = token.split("\u200B", -1);
            for (int i = 0; i < words.length; i++) {
                String word = words[i];
                String separator = (i == words.length - 1) ? " " : "";
                if (lineWidth(sb.toString() + word, from) <= textAreaWidth) {
                    sb.append(word);
                    sb.append(separator);
                } else {
                    if (sb.length() > from) {
                        textLines.add(newTextLine(sb.toString(), from));
                        sb.setLength(0);
                        from = 0;
                    }
                    // A word too wide for a line by itself is broken.
                    int rest = addBrokenWordLines(textLines, word, textAreaWidth);
                    if (rest < word.length()) {
                        sb.append(word);
                        sb.append(separator);
                        from = rest;
                    }
                }
            }
        }
        textLines.add(newTextLine(sb.toString(), from));
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
            textLine.xOffset = textAreaWidth - font.stringWidth(fallbackFont, fontSize, textLine.text);
        }
    }

    private void centerText(TextLine[] textLines) {
        float textAreaWidth = this.width - 2 * this.textPadding;
        for (TextLine textLine : textLines) {
            textLine.xOffset = (textAreaWidth - font.stringWidth(fallbackFont, fontSize, textLine.text)) / 2f;
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
        float blockHeight = Math.max(this.height, textLines.length * leading + 2 * this.textPadding);
        if (page == null) {
            return new float[] {this.x + this.width, this.y + blockHeight};
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
            Rect rect = new Rect(this.x, this.y, this.width, blockHeight);
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
            keywordHighlightColors,
            this.language);
        page.addEMC();
        page.restoreGraphicsState();

        if (uri != null || key != null) {
            page.addAnnotation(new Annotation(
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
