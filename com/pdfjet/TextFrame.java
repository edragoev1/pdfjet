/*
 * TextFrame.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Paragraphs of text lines, wrapped at the width of the frame, with an optional
 * border. A frame with a height draws as much of the text as fits and keeps the
 * rest for the next frame, so text flows from frame to frame. A frame without a
 * height draws all of the text, as Text does. Please see Example_47.
 */
public class TextFrame implements Drawable {
    private final List<Paragraph> paragraphs;
    private float x;
    private float y;
    private float w;
    private float h;
    private float paragraphLeading = 24f;
    private boolean border = false;
    private float[] borderColor = {0f, 0f, 1f};
    private float borderWidth = 0.5f;
    private String borderPattern = "[] 0";

    // The text that is not drawn yet starts at this paragraph, at this text line
    // of the paragraph and at this token of the text line. The tokens are null
    // when the text line has not been started.
    private int paragraphIndex = 0;
    private int lineIndex = 0;
    private List<String> tokens = null;
    private int tokenIndex = 0;

    // The row of text being drawn: where the text goes next, whether the row can
    // take more text, whether the frame has a row yet, and where the next row goes.
    private float xText;
    private float yText;
    private boolean rowOpen;
    private boolean rowPlaced;
    private float nextBaseline;

    /**
     * Creates a text frame from paragraphs of text lines. The paragraphs are 24
     * points apart unless setParagraphLeading says otherwise.
     *
     * @param paragraphs the paragraphs.
     */
    public TextFrame(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
    }

    /**
     * Creates a text frame from strings, one paragraph each, in the font at its
     * size. An empty line separates the paragraphs.
     *
     * @param f1 the font.
     * @param inputList the paragraphs.
     */
    public TextFrame(Font f1, List<String> inputList) {
        this.paragraphs = new ArrayList<Paragraph>();
        for (String text : inputList) {
            this.paragraphs.add(new Paragraph(new TextLine(f1, text)));
        }
        this.paragraphLeading = 2f * f1.getBodyHeight();
    }

    /**
     * Sets the location of the top left corner of this text frame.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this TextFrame object.
     */
    public TextFrame setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location of the top left corner of this text frame.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this TextFrame object.
     */
    public TextFrame setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the width at which the lines wrap.
     *
     * @param w the width.
     * @return this TextFrame object.
     */
    public TextFrame setWidth(float w) {
        this.w = w;
        return this;
    }

    /**
     * Sets the width at which the lines wrap.
     *
     * @param w the width.
     * @return this TextFrame object.
     */
    public TextFrame setWidth(double w) {
        return setWidth((float) w);
    }

    /**
     * Sets the height of this text frame. With a height of 0, the default, the
     * frame draws all of its text.
     *
     * @param h the height.
     * @return this TextFrame object.
     */
    public TextFrame setHeight(float h) {
        this.h = h;
        return this;
    }

    /**
     * Sets the height of this text frame. With a height of 0, the default, the
     * frame draws all of its text.
     *
     * @param h the height.
     * @return this TextFrame object.
     */
    public TextFrame setHeight(double h) {
        return setHeight((float) h);
    }

    /**
     * Returns the width of this text frame.
     *
     * @return the width.
     */
    public float getWidth() {
        return this.w;
    }

    /**
     * Returns the height of this text frame.
     *
     * @return the height.
     */
    public float getHeight() {
        return this.h;
    }

    /**
     * Sets the vertical distance between paragraphs, from the baseline of the
     * last line of a paragraph to the baseline of the first line of the next.
     *
     * @param paragraphLeading the distance between paragraphs.
     * @return this TextFrame object.
     */
    public TextFrame setParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    /**
     * Sets whether a border is drawn around this text frame.
     *
     * @param border true to draw a border.
     * @return this TextFrame object.
     */
    public TextFrame setBorder(boolean border) {
        this.border = border;
        return this;
    }

    /**
     * Sets the border color and draws a border around this text frame.
     * Color.transparent removes the border.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextFrame object.
     */
    public TextFrame setBorderColor(int color) {
        if (color == Color.transparent) {
            this.border = false;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        return setBorderColor(r, g, b);
    }

    /**
     * Sets the border color and draws a border around this text frame.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this TextFrame object.
     */
    public TextFrame setBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        this.border = true;
        return this;
    }

    /**
     * Sets the border color and draws a border around this text frame.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextFrame object.
     */
    public TextFrame setBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        this.border = true;
        return this;
    }

    /**
     * Sets the border width.
     *
     * @param borderWidth the border width.
     * @return this TextFrame object.
     */
    public TextFrame setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /**
     * Sets the dash pattern of the border.
     *
     * @param borderPattern the dash pattern, for example "[3] 0".
     * @return this TextFrame object.
     */
    public TextFrame setBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
        return this;
    }

    /**
     * Returns true if some of the text has not been drawn yet.
     *
     * @return true if there is more text to draw.
     */
    public boolean hasMoreText() {
        return paragraphIndex < paragraphs.size();
    }

    /**
     * Draws the text on the page: all of it when this frame has no height, or as
     * much as fits in the height, keeping the rest for the next frame. The first
     * line of a frame is drawn even when it does not fit, so the text always
     * flows, and a word wider than the frame is broken. With no page nothing is
     * drawn, the paragraphs get their coordinates and the text is kept.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of this frame,
     *     or of the text when the frame has no height.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        int startParagraph = paragraphIndex;
        int startLine = lineIndex;
        List<String> startTokens = (tokens == null) ? null : new ArrayList<String>(tokens);
        int startToken = tokenIndex;

        float bottom = drawParagraphs(page);
        if (h > 0f) {
            bottom = y + h;
        }
        if (border) {
            Rect rect = new Rect(x, y, w, bottom - y);
            rect.setBorderColor(borderColor);
            rect.setBorderWidth(borderWidth);
            rect.setBorderPattern(borderPattern);
            rect.drawOn(page);
        }

        if (page == null) {
            paragraphIndex = startParagraph;
            lineIndex = startLine;
            tokens = startTokens;
            tokenIndex = startToken;
        }
        return new float[] {x + w, bottom};
    }

    // Draws the text that is left, as much of it as fits in the height of the
    // frame, and returns the bottom of the text drawn.
    private float drawParagraphs(Page page) throws Exception {
        xText = x;
        rowOpen = false;
        rowPlaced = false;
        float bottom = y;
        while (paragraphIndex < paragraphs.size()) {
            Paragraph paragraph = paragraphs.get(paragraphIndex);
            while (lineIndex < paragraph.lines.size()) {
                TextLine textLine = paragraph.lines.get(lineIndex);
                if (!rowOpen && !openRow(textLine)) {
                    return bottom;
                }
                if (tokens == null) {
                    if (lineIndex == 0) {
                        paragraph.x1 = x;
                        paragraph.y1 = yText - textLine.font.getAscent(textLine.fontSize);
                        paragraph.xText = xText;
                        paragraph.yText = yText;
                    }
                    tokens = tokenize(textLine);
                    tokenIndex = 0;
                }
                if (!drawTokens(page, textLine)) {
                    return bottom;
                }
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.getDescent(textLine.fontSize);
                bottom = paragraph.y2;
                tokens = null;
                tokenIndex = 0;
                lineIndex++;
            }
            xText = x;
            rowOpen = false;
            nextBaseline = yText + paragraphLeading;
            paragraphIndex++;
            lineIndex = 0;
        }
        return bottom;
    }

    // Starts a row of text for the text line, below the previous row or at the
    // top of the frame. Returns false when the row does not fit in the height of
    // the frame. The first row of a frame always fits, so the text keeps flowing.
    private boolean openRow(TextLine textLine) {
        float baseline = rowPlaced ? nextBaseline : y + textLine.font.getAscent(textLine.fontSize);
        if (h > 0f && rowPlaced &&
                (baseline + textLine.font.getDescent(textLine.fontSize)) > (y + h)) {
            return false;
        }
        xText = x;
        yText = baseline;
        rowOpen = true;
        rowPlaced = true;
        return true;
    }

    // Draws the tokens of the text line that are left, wrapping them at the width
    // of the frame. Returns false when a row does not fit in the height of the
    // frame; the tokens that are left stay for the next frame.
    private boolean drawTokens(Page page, TextLine textLine) throws Exception {
        Font font = textLine.font;
        Font fallbackFont = textLine.fallbackFont;
        float fontSize = textLine.fontSize;
        StringBuilder buf = new StringBuilder();
        while (tokenIndex < tokens.size()) {
            if (!rowOpen && !openRow(textLine)) {
                return false;
            }
            String token = tokens.get(tokenIndex);
            float runLength = font.stringWidth(fallbackFont, fontSize, buf.toString());
            float tokenWidth = font.stringWidth(fallbackFont, fontSize, token + Single.space);
            if ((runLength + tokenWidth) < ((x + w) - xText)) {
                buf.append(token).append(Single.space);
                tokenIndex++;
                continue;
            }
            if (buf.length() == 0 && xText == x) {
                // The token does not fit in an empty row, so the row takes as much of it as fits.
                String head = headThatFits(textLine, token);
                buf.append(head);
                if (head.length() == token.length()) {
                    tokenIndex++;
                } else {
                    tokens.set(tokenIndex, token.substring(head.length()));
                }
            }
            drawLine(page, textLine, buf.toString());
            buf.setLength(0);
            xText = x;
            rowOpen = false;
            nextBaseline = yText + textLine.getHeight();
        }
        drawLine(page, textLine, buf.toString());
        xText += font.stringWidth(fallbackFont, fontSize, buf.toString());
        return true;
    }

    // Returns the longest start of the token that is narrower than the frame, and
    // at least the first character of the token.
    private String headThatFits(TextLine textLine, String token) {
        int end = Character.charCount(token.codePointAt(0));
        while (end < token.length()) {
            int next = end + Character.charCount(token.codePointAt(end));
            if (textLine.font.stringWidth(textLine.fallbackFont, textLine.fontSize, token.substring(0, next)) >= w) {
                break;
            }
            end = next;
        }
        return token.substring(0, end);
    }

    // Draws the string at the current text position, with the attributes of the text line.
    private void drawLine(Page page, TextLine textLine, String str) throws Exception {
        new TextLine(textLine.font, str)
                .setFallbackFont(textLine.getFallbackFont())
                .setFontSize(textLine.getFontSize())
                .setTextColor(textLine.getTextColor())
                .setColorMap(textLine.getColorMap())
                .setUnderline(textLine.getUnderline())
                .setStrikeout(textLine.getStrikeout())
                .setLanguage(textLine.getLanguage())
                .setLocation(xText, yText)
                .drawOn(page);
    }

    // Splits the text of the text line into words, or, for CJK text, which has no
    // spaces between its words, into runs of characters that fit in the width.
    private List<String> tokenize(TextLine textLine) {
        List<String> list = new ArrayList<String>();
        if (!Util.isCJK(textLine.text)) {
            list.addAll(Arrays.asList(Util.splitOnWhitespace(textLine.text)));
            return list;
        }
        StringBuilder buf = new StringBuilder();
        String text = textLine.text;
        int i = 0;
        while (i < text.length()) {
            int ch = text.codePointAt(i);
            i += Character.charCount(ch);
            String str = new String(Character.toChars(ch));
            if (textLine.font.stringWidth(textLine.fallbackFont, textLine.fontSize, buf.toString() + str) < w) {
                buf.appendCodePoint(ch);
            } else {
                if (buf.length() > 0) {
                    list.add(buf.toString());
                }
                buf.setLength(0);
                buf.appendCodePoint(ch);
            }
        }
        if (buf.length() > 0) {
            list.add(buf.toString());
        }
        return list;
    }
}   // End of TextFrame.java
