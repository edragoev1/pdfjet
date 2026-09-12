/*
 * Text.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Paragraphs of text lines, wrapped at a width, with an optional border.
 * Please see Example_03, Example_41 and Example_49.
 */
public class Text implements Drawable {
    private final List<Paragraph> paragraphs;
    private float x1;
    private float y1;
    private float width;
    private float xText;
    private float yText;
    private float paragraphLeading = 24f;
    private boolean hasBorder = false;
    private float[] borderColor;
    private float borderWidth = 0.5f;
    private String borderPattern = "[] 0";

    /**
     * Creates a text object from the paragraphs.
     *
     * @param paragraphs the paragraphs.
     */
    public Text(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
    }

    /**
     * Sets the location of the top left corner of this text.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Text object.
     */
    public Text setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the location of the top left corner of this text.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Text object.
     */
    public Text setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the width at which the lines wrap.
     *
     * @param width the width.
     * @return this Text object.
     */
    public Text setWidth(float width) {
        this.width = width;
        return this;
    }

    /**
     * Sets the vertical distance between paragraphs.
     *
     * @param paragraphLeading the distance between paragraphs.
     * @return this Text object.
     */
    public Text setParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    /**
     * Sets the border width.
     *
     * @param borderWidth the border width.
     * @return this Text object.
     */
    public Text setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /**
     * Sets the dash pattern of the border.
     *
     * @param borderPattern the dash pattern, for example "[3] 0".
     * @return this Text object.
     */
    public Text setBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
        return this;
    }

    /**
     * Sets the border color and draws a border around this text. Color.transparent removes the border.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Text object.
     */
    public Text setBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            this.hasBorder = false;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setBorderColor(r, g, b);
        return this;
    }

    /**
     * Sets the border color and draws a border around this text.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this Text object.
     */
    public Text setBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        this.hasBorder = true;
        return this;
    }

    /**
     * Sets the border color and draws a border around this text.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Text object.
     */
    public Text setBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        this.hasBorder = true;
        return this;
    }

    /**
     * Draws the paragraphs on the specified page. With no page nothing is drawn
     * and the paragraphs get their coordinates.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of this text.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        TextLine firstLine = paragraphs.get(0).lines.get(0);
        this.xText = x1;
        this.yText = y1 + firstLine.font.getAscent(firstLine.fontSize);
        for (Paragraph paragraph : paragraphs) {
            firstLine = paragraph.lines.get(0);
            paragraph.x1 = x1;
            paragraph.y1 = yText - firstLine.font.getAscent(firstLine.fontSize);
            paragraph.xText = xText;
            paragraph.yText = yText;
            for (TextLine textLine : paragraph.lines) {
                float[] point = drawTextLine(page, xText, yText, textLine);
                xText = point[0];
                yText = point[1];
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.getDescent(textLine.fontSize);
            }
            xText = x1;
            yText += paragraphLeading;
        }

        Paragraph lastParagraph = paragraphs.get(paragraphs.size() - 1);
        TextLine lastTextLine = lastParagraph.getTextLines().get(lastParagraph.getTextLines().size() - 1);
        float height = ((yText - paragraphLeading) - y1) + lastTextLine.font.getDescent(lastTextLine.fontSize);
        if (hasBorder) {
            Rect rect = new Rect(x1, y1, width, height);
            rect.setBorderColor(this.borderColor);
            rect.setBorderWidth(this.borderWidth);
            rect.setBorderPattern(this.borderPattern);
            rect.drawOn(page);
        }

        return new float[] { x1 + width, y1 + height };
    }

    // Draws the text line, wrapping it at the width of this text, and returns
    // the x and y coordinates where the next text starts.
    private float[] drawTextLine(Page page, float x, float y, TextLine textLine) throws Exception {
        this.xText = x;
        this.yText = y;

        String[] tokens;
        if (Util.isCJK(textLine.text)) {
            tokens = tokenizeCJK(textLine, this.width);
        } else {
            tokens = Util.splitOnWhitespace(textLine.text);
        }

        Font font = textLine.font;
        Font fallbackFont = textLine.fallbackFont;
        float fontSize = textLine.fontSize;
        StringBuilder buf = new StringBuilder();
        for (String token : tokens) {
            float runLength = font.stringWidth(fallbackFont, fontSize, buf.toString());
            float tokenWidth = font.stringWidth(fallbackFont, fontSize, token + Single.space);
            if ((runLength + tokenWidth) < ((this.x1 + this.width) - this.xText)) {
                buf.append(token).append(Single.space);
            } else {
                drawLine(page, textLine, buf.toString());
                xText = x1;
                yText += textLine.getHeight();
                buf.setLength(0);
                buf.append(token).append(Single.space);
            }
        }
        drawLine(page, textLine, buf.toString());

        return new float[] {xText + font.stringWidth(fallbackFont, fontSize, buf.toString()), yText};
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

    private String[] tokenizeCJK(TextLine textLine, float textWidth) {
        List<String> list = new ArrayList<>();
        StringBuilder buf = new StringBuilder();
        String text = textLine.text;
        int i = 0;
        while (i < text.length()) {
            int ch = text.codePointAt(i);
            i += Character.charCount(ch);
            String str = new String(Character.toChars(ch));
            if (textLine.font.stringWidth(textLine.fallbackFont, textLine.fontSize, buf.toString() + str) < textWidth) {
                buf.appendCodePoint(ch);
            } else {
                if (buf.length() > 0) {  // Never emit an empty token
                    list.add(buf.toString());
                }
                buf.setLength(0);
                buf.appendCodePoint(ch);
            }
        }
        if (buf.length() > 0) {
            list.add(buf.toString());
        }
        return list.toArray(new String[] {});
    }

    /**
     * Reads a text file and returns its paragraphs. An empty line separates the paragraphs.
     *
     * @param f1 the font for the text.
     * @param filePath the path of the text file.
     * @return the paragraphs.
     * @throws Exception if the file cannot be read.
     */
    public static List<Paragraph> paragraphsFromFile(Font f1, String filePath) throws Exception {
        List<Paragraph> paragraphs = new ArrayList<>();
        String contents = Content.ofTextFile(filePath);
        Paragraph paragraph = new Paragraph();
        TextLine textLine = new TextLine(f1);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < contents.length(); i++) {
            char ch = contents.charAt(i);
            // We need at least one character after the \n\n to begin new paragraph!
            if (i < (contents.length() - 2) &&
                    ch == '\n' && contents.charAt(i + 1) == '\n') {
                textLine.setText(sb.toString());
                paragraph.add(textLine);
                paragraphs.add(paragraph);
                paragraph = new Paragraph();
                textLine = new TextLine(f1);
                sb.setLength(0);
                i += 1;
            } else {
                sb.append(ch);
            }
        }
        if (!sb.toString().isEmpty()) {
            textLine.setText(sb.toString());
            paragraph.add(textLine);
            paragraphs.add(paragraph);
        }
        return paragraphs;
    }

    /**
     * Reads the lines of a UTF-8 text file, without carriage returns.
     *
     * @param filePath the path of the text file.
     * @return the lines.
     * @throws IOException if the file cannot be read.
     */
    public static List<String> readLines(String filePath) throws IOException {
        List<String> lines = new ArrayList<>();
        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(filePath), StandardCharsets.UTF_8))) {
            StringBuilder buffer = new StringBuilder();
            int ch;
            while ((ch = reader.read()) != -1) {
                if (ch == '\r') {
                    continue;
                } else if (ch == '\n') {
                    lines.add(buffer.toString());
                    buffer.setLength(0);
                } else {
                    buffer.append((char) ch);
                }
            }
            if (buffer.length() > 0) {
                lines.add(buffer.toString());
            }
        }
        return lines;
    }
}   // End of Text.java
