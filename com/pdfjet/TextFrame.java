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
 * height draws all of the text. Drawing consumes the text: a second drawOn draws
 * what is left, so build a new frame to draw the same text again.
 * <p>
 * Use a TextFrame for text that continues from one frame to the next: the
 * columns of an article, or the pages of a long text. Use a TextColumn to
 * draw paragraphs in one place, with justified text or a rotation, and a
 * TextBlock for one run of text in one font. Please see Example_03 and
 * Example_47.
 */
public class TextFrame implements Drawable {
    private final List<Paragraph> paragraphs;
    private float x;
    private float y;
    private float w;
    private float h;
    private float paragraphGap = 0f;
    private boolean hasParagraphGap = false;
    private boolean border = false;
    private float[] borderColor = {0f, 0f, 0f};
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
    private boolean startsParagraph;    // The next row starts a paragraph

    // The text of the row being drawn, drawn when the row is complete, so that it
    // can be aligned: each part is a text line with some of its text.
    private final List<RowPart> row = new ArrayList<RowPart>();

    private static final class RowPart {
        final TextLine textLine;
        final String text;
        final float x;
        final Paragraph paragraph;
        final boolean startsParagraph;  // The first text of the paragraph
        final boolean endsTextLine;     // The last text of the text line

        RowPart(TextLine textLine, String text, float x, Paragraph paragraph,
                boolean startsParagraph, boolean endsTextLine) {
            this.textLine = textLine;
            this.text = text;
            this.x = x;
            this.paragraph = paragraph;
            this.startsParagraph = startsParagraph;
            this.endsTextLine = endsTextLine;
        }
    }

    /**
     * Creates a text frame from paragraphs of text lines. An empty line separates
     * the paragraphs unless setParagraphGap sets another gap.
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
     * Sets the space between paragraphs, in points: from the bottom of the text
     * of a paragraph to the top of the text of the next, so paragraphs never
     * overlap. The default is one empty line in the size of the next paragraph,
     * so a heading is not followed by an empty line of its own size.
     *
     * @param paragraphGap the space between paragraphs, 0 or more.
     * @return this TextFrame object.
     */
    public TextFrame setParagraphGap(float paragraphGap) {
        this.paragraphGap = paragraphGap;
        this.hasParagraphGap = true;
        return this;
    }

    /**
     * Sets whether a border is drawn around this text frame.
     *
     * @param border true to draw a border.
     * @return this TextFrame object.
     */
    public TextFrame setBorders(boolean border) {
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
        return setBorderColor(new float[] {r, g, b});
    }

    /**
     * Sets the border color and draws a border around this text frame.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextFrame object.
     */
    public TextFrame setBorderColor(float[] rgbColor) {
        this.borderColor = Util.copyOf(rgbColor);
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
    public TextFrame setBorderDashPattern(String borderPattern) {
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
            rect.setBorderDashPattern(borderPattern);
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
        startsParagraph = false;
        row.clear();
        float bottom = y;
        while (paragraphIndex < paragraphs.size()) {
            Paragraph paragraph = paragraphs.get(paragraphIndex);
            while (lineIndex < paragraph.lines.size()) {
                TextLine textLine = paragraph.lines.get(lineIndex);
                if (!rowOpen && !openRow(textLine)) {
                    drawRow(page, false);
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
                if (!drawTokens(page, paragraph, textLine)) {
                    drawRow(page, false);
                    return bottom;
                }
                paragraph.x2 = xText;
                paragraph.y2 = yText + textLine.font.getDescent(textLine.fontSize);
                bottom = paragraph.y2;
                tokens = null;
                tokenIndex = 0;
                lineIndex++;
            }
            drawRow(page, true);
            xText = x;
            rowOpen = false;
            if (!paragraph.lines.isEmpty()) {
                // The next paragraph starts below the descent of this one, after the gap.
                TextLine lastLine = paragraph.lines.get(paragraph.lines.size() - 1);
                nextBaseline = yText + lastLine.font.getDescent(lastLine.fontSize);
                startsParagraph = true;
            }
            paragraphIndex++;
            lineIndex = 0;
        }
        return bottom;
    }

    // Starts a row of text for the text line, below the previous row or at the
    // top of the frame. Returns false when the row does not fit in the height of
    // the frame. The first row of a frame always fits, so the text keeps flowing.
    private boolean openRow(TextLine textLine) {
        float baseline = y + textLine.font.getAscent(textLine.fontSize);
        if (rowPlaced) {
            baseline = nextBaseline;
            if (startsParagraph) {
                // The gap, one empty line of this text by default, then its ascent.
                float gap = hasParagraphGap ? paragraphGap : textLine.getHeight();
                baseline += gap + textLine.font.getAscent(textLine.fontSize);
            }
        }
        if (h > 0f && rowPlaced &&
                (baseline + textLine.font.getDescent(textLine.fontSize)) > (y + h)) {
            return false;
        }
        xText = x;
        yText = baseline;
        rowOpen = true;
        rowPlaced = true;
        startsParagraph = false;
        return true;
    }

    // Draws the tokens of the text line that are left, wrapping them at the width
    // of the frame. Returns false when a row does not fit in the height of the
    // frame; the tokens that are left stay for the next frame.
    private boolean drawTokens(Page page, Paragraph paragraph, TextLine textLine) throws Exception {
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
            addToRow(paragraph, textLine, buf.toString(), false);
            drawRow(page, false);
            buf.setLength(0);
            xText = x;
            rowOpen = false;
            nextBaseline = yText + textLine.getHeight();
        }
        addToRow(paragraph, textLine, buf.toString(), true);
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

    // Adds the string to the row, at the current text position.
    private void addToRow(Paragraph paragraph, TextLine textLine, String str, boolean endsTextLine) {
        boolean first = row.isEmpty() && lineIndex == 0 && xText == paragraph.xText
                && yText == paragraph.yText;
        row.add(new RowPart(textLine, str, xText, paragraph, first, endsTextLine));
    }

    // Draws the parts of the row, with every setting of their text lines, including
    // the vertical offset and the link, as TextColumn does. A paragraph aligned to
    // the right or to the center moves the row, and a justified one widens the
    // spaces of every row but its last.
    private void drawRow(Page page, boolean lastRowOfParagraph) throws Exception {
        if (row.isEmpty()) {
            return;
        }
        Paragraph paragraph = row.get(0).paragraph;
        Alignment alignment = paragraph.explicitAlignment ? paragraph.alignment : Alignment.LEFT;
        RowPart last = row.get(row.size() - 1);
        float rowWidth = last.x + width(last.textLine, trimTrailingSpaces(last.text)) - x;

        if (alignment == Alignment.JUSTIFY && !lastRowOfParagraph) {
            drawJustifiedRow(page, rowWidth);
        } else {
            float shift = 0f;
            if (alignment == Alignment.RIGHT) {
                shift = w - rowWidth;
            } else if (alignment == Alignment.CENTER) {
                shift = (w - rowWidth) / 2f;
            }
            for (RowPart part : row) {
                part.textLine.copyWithText(part.text).setLocation(part.x + shift, yText).drawOn(page);
                if (part.startsParagraph) {
                    part.paragraph.xText += shift;
                }
                if (part.endsTextLine) {
                    part.paragraph.x2 = part.x + width(part.textLine, part.text) + shift;
                }
            }
        }
        row.clear();
    }

    // Draws the words of the row one by one, with the width left in the row shared
    // out among the spaces between them.
    private void drawJustifiedRow(Page page, float rowWidth) throws Exception {
        int spaces = 0;
        for (int i = 0; i < row.size(); i++) {
            String text = row.get(i).text;
            for (int j = 0; j < text.length(); j++) {
                if (text.charAt(j) == ' ' && hasWordAfter(i, j + 1)) {
                    spaces++;
                }
            }
        }
        float dx = (spaces > 0) ? (w - rowWidth) / spaces : 0f;
        float xWord = x;
        for (int i = 0; i < row.size(); i++) {
            RowPart part = row.get(i);
            String text = part.text;
            int start = 0;
            while (start < text.length()) {
                int end = text.indexOf(' ', start);
                if (end == -1) {
                    end = text.length();
                }
                if (end > start) {
                    String word = text.substring(start, end);
                    part.textLine.copyWithText(word).setLocation(xWord, yText).drawOn(page);
                    xWord += width(part.textLine, word);
                }
                if (end < text.length()) {
                    xWord += width(part.textLine, Single.space);
                    if (hasWordAfter(i, end + 1)) {
                        xWord += dx;
                    }
                }
                start = end + 1;
            }
            if (part.endsTextLine) {
                part.paragraph.x2 = xWord;
            }
        }
    }

    // Returns true when a word follows this position of the row.
    private boolean hasWordAfter(int partIndex, int charIndex) {
        for (int i = partIndex; i < row.size(); i++) {
            String text = row.get(i).text;
            for (int j = (i == partIndex) ? charIndex : 0; j < text.length(); j++) {
                if (text.charAt(j) != ' ') {
                    return true;
                }
            }
        }
        return false;
    }

    private static String trimTrailingSpaces(String text) {
        int end = text.length();
        while (end > 0 && text.charAt(end - 1) == ' ') {
            end--;
        }
        return text.substring(0, end);
    }

    private static float width(TextLine textLine, String text) {
        return textLine.font.stringWidth(textLine.fallbackFont, textLine.fontSize, text);
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
