/**
 * Text.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.FileInputStream;
import java.io.IOException;
import java.util.*;

/**
 * Please see Example_46.java
 */
public class Text implements Drawable {
    private final List<Paragraph> paragraphs;
    private float x1;
    private float y1;
    private float width;
    private float xText;
    private float yText;
    private float paragraphLeading = 24f;
    private boolean border = false;

    public Text(List<Paragraph> paragraphs) {
        this.paragraphs = paragraphs;
    }

    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    public void setPosition(double x, double y) {
        setLocation(x, y);
    }

    public Text setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    public Text setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    public Text setWidth(float width) {
        this.width = width;
        return this;
    }

    public Text setParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    public void setBorder(boolean border) {
        this.border = border;
    }

    public float[] drawOn(Page page) throws Exception {
        this.xText = x1;
        this.yText = y1 + paragraphs.get(0).lines.get(0).font.getAscent();
        for (Paragraph paragraph : paragraphs) {
            paragraph.x1 = x1;
            paragraph.y1 = yText - paragraph.lines.get(0).font.getAscent();
            paragraph.xText = xText;
            paragraph.yText = yText;
            for (TextLine textLine : paragraph.lines) {
                float[] point = drawTextLine(page, xText, yText, textLine);
                xText = point[0];
                yText = point[1];
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.getDescent(textLine.font.size);
            }
            xText = x1;
            yText += paragraphLeading;
        }

        Paragraph lastParagraph = paragraphs.get(paragraphs.size() - 1);
        TextLine lastTextLine = lastParagraph.getTextLines().get(lastParagraph.getTextLines().size() - 1);
        float height = ((yText - paragraphLeading) - y1) + lastTextLine.font.getDescent(lastTextLine.fontSize);
        if (page != null && border) {
            Rect rect = new Rect(x1, y1, width, height);
            rect.drawOn(page);
        }

        return new float[] { x1 + width, y1 + height };
    }

    public float[] drawTextLine(Page page, float x, float y, TextLine textLine) throws Exception {
        this.xText = x;
        this.yText = y;

        String[] tokens;
        if (stringIsCJK(textLine.text)) {
            tokens = tokenizeCJK(textLine, this.width);
        } else {
            tokens = textLine.text.split("\\s+");
        }

        StringBuilder buf = new StringBuilder();
        for (String token : tokens) {
            float runLength = textLine.font.stringWidth(textLine.fallbackFont, buf.toString());
            float tokenWidth = textLine.font.stringWidth(textLine.fallbackFont, token + Single.space);
            if ((runLength + tokenWidth) < ((this.x1 + this.width) - this.xText)) {
                buf.append(token).append(Single.space);
            } else {
                new TextLine(textLine.font, buf.toString())
                        .setFallbackFont(textLine.getFallbackFont())
                        .setFontSize(textLine.getFontSize())
                        .setTextColor(textLine.getTextColor())
                        .setColorMap(textLine.getColorMap())
                        .setUnderline(textLine.getUnderline())
                        .setStrikeout(textLine.getStrikeout())
                        .setLanguage(textLine.getLanguage())
                        .setLocation(xText, yText)
                        .drawOn(page);
                xText = x1;
                yText += textLine.getHeight();
                buf.setLength(0);
                buf.append(token).append(Single.space);
            }
        }
        new TextLine(textLine.font, buf.toString())
                .setFallbackFont(textLine.fallbackFont)
                .setFontSize(textLine.getFontSize())
                .setTextColor(textLine.getTextColor())
                .setColorMap(textLine.getColorMap())
                .setUnderline(textLine.getUnderline())
                .setStrikeout(textLine.getStrikeout())
                .setLanguage(textLine.getLanguage())
                .setLocation(xText, yText)
                .drawOn(page);

        return new float[] {
                xText + textLine.font.stringWidth(textLine.fallbackFont, buf.toString()),
                yText };
    }

    private boolean stringIsCJK(String str) {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        int numOfCJK = 0;
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK += 1;
            }
        }
        return (numOfCJK > (str.length() / 2));
    }

    private String[] tokenizeCJK(TextLine textLine, float textWidth) {
        List<String> list = new ArrayList<>();
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < textLine.text.length(); i++) {
            char ch = textLine.text.charAt(i);
            if (textLine.font.stringWidth(textLine.fallbackFont, buf.toString() + ch) < textWidth) {
                buf.append(ch);
            } else {
                list.add(buf.toString());
                buf.setLength(0);
                buf.append(ch);
            }
        }
        if (!buf.toString().isEmpty()) {
            list.add(buf.toString());
        }
        return list.toArray(new String[] {});
    }

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

    public static List<String> readLines(String filePath) throws IOException {
        List<String> lines = new ArrayList<>();
        FileInputStream stream = new FileInputStream(filePath);
        StringBuilder buffer = new StringBuilder();
        int ch;
        while ((ch = stream.read()) != -1) {
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
        stream.close();
        return lines;
    }
}   // End of Text.java
