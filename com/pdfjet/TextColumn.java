/*
 * TextColumn.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A column of paragraphs, each a list of TextLine objects that can differ in
 * font, size and color, aligned left, right, center or justified, with a line
 * spacing, a paragraph spacing and an optional line between the paragraphs.
 * It draws all of its paragraphs where it is placed, top down; to rotate a
 * column, add it to a Container and rotate that.
 * <p>
 * Use a TextColumn for an article or a page of mixed text: bold or colored
 * words in a paragraph, justified paragraphs, CJK paragraphs. Use a TextBlock
 * for one run of text in one font, and a TextFrame when the text must
 * continue from one frame to the next, across columns or pages. Please see
 * Example_10, Example_29, Example_44 and Example_49.
 */
public class TextColumn implements Drawable {
    /** The text alignment. */
    protected Alignment alignment = Alignment.LEFT;
    /** The x coordinate of the top left corner. */
    protected float x;  // This variable is set in the beginning and only reset after the drawOn
    /** The y coordinate of the top left corner. */
    protected float y;  // This variable is set in the beginning and only reset after the drawOn
    private float w;
    private float h;
    private float x1;
    private float y1;
    private float lineSpacing = 1.0f;
    private float paragraphSpacing = 1.0f;
    private final List<Paragraph> paragraphs;
    private boolean lineBetweenParagraphs = false;

    /**
     *  Create a text column object.
     */
    public TextColumn() {
        this.paragraphs = new ArrayList<Paragraph>();
    }

    /**
     * Sets the lineBetweenParagraphs private variable value.
     * If the value is set to true - an empty line will be inserted between the current and next paragraphs.
     *
     * @param lineBetweenParagraphs the specified boolean value.
     * @return this TextColumn object.
     */
    public TextColumn setLineBetweenParagraphs(boolean lineBetweenParagraphs) {
        this.lineBetweenParagraphs = lineBetweenParagraphs;
        return this;
    }

    /**
     * Sets the space between paragraphs.
     *
     * @param paragraphSpacing the paragraph spacing.
     * @return this TextColumn object.
     */
    public TextColumn setParagraphSpacing(float paragraphSpacing) {
        this.paragraphSpacing = paragraphSpacing;
        return this;
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     * @return this text column.
     */
    public TextColumn setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the desired width of this text column.
     *
     * @param w the width of this text column.
     * @return this TextColumn object.
     */
    public TextColumn setWidth(float w) {
        this.w = w;
        return this;
    }

    /**
     * Returns the width of this text column.
     *
     * @return the width.
     */
    public float getWidth() {
        return this.w;
    }

    /**
     * Sets the height of this text column.
     *
     * @param h the height of this text column.
     * @return this TextColumn object.
     */
    public TextColumn setHeight(float h) {
        this.h = h;
        return this;
    }

    /**
     * Returns the height of this text column.
     *
     * @return the height.
     */
    public float getHeight() {
        return this.h;
    }

    /**
     * Sets the text alignment of the paragraphs that do not set their own.
     *
     * @param alignment the specified alignment code.
     *                  Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY
     * @return this TextColumn object.
     */
    public TextColumn setTextAlignment(Alignment alignment) {
        this.alignment = alignment;
        return this;
    }

    /**
     * Sets the spacing between the lines in this text column.
     *
     * @param lineSpacing the line spacing value.
     * @return this TextColumn object.
     */
    public TextColumn setLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
        return this;
    }

    /**
     * Adds a new paragraph to this text column.
     *
     * @param paragraph the new paragraph object.
     * @return this TextColumn object.
     */
    public TextColumn addParagraph(Paragraph paragraph) {
        this.paragraphs.add(paragraph);
        return this;
    }

    /**
     * Removes the last paragraph added to this text column.
     *
     * @return this TextColumn object.
     */
    public TextColumn removeLastParagraph() {
        if (this.paragraphs.size() >= 1) {
            this.paragraphs.remove(this.paragraphs.size() - 1);
        }
        return this;
    }

    /**
     * Returns dimension object containing the width and height of this component.
     * Please see Example_29.
     *
     * @return dimension object containing the width and height of this component.
     * @throws Exception  If an input or output exception occurred
     */
    public Dimension getSize() throws Exception {
        float[] xy = drawOn(null);
        return new Dimension(this.w, xy[1] - this.y);
    }

    /**
     * Draws this text column on the specified page. With no page nothing is
     * drawn and the location of the next component is computed.
     *
     * @param page the page to draw this text column on.
     * @return the point with x and y coordinates of the location where to draw the next component.
     * @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        float[] xy = new float[] {x, y};
        for (Paragraph paragraph : paragraphs) {
            xy = drawParagraphOn(page, paragraph);
        }
        // Restore the original location
        setLocation(this.x, this.y);
        // A column with a height reaches at least that far down from its location
        if (y + h > xy[1]) {
            xy[1] = y + h;
        }
        return xy;
    }

    private float[] drawParagraphOn(Page page, Paragraph paragraph) throws Exception {
        Alignment alignment = paragraph.explicitAlignment ? paragraph.alignment : this.alignment;
        List<TextLine> list = new ArrayList<TextLine>();
        float lineHeight = 0f;
        float maxAscent = 0f;
        for (TextLine line : paragraph.lines) {
            if ((line.getHeight() * lineSpacing) > lineHeight) {
                lineHeight = line.getHeight() * lineSpacing;
            }
            if (line.font.getAscent(line.fontSize) > maxAscent) {
                maxAscent = line.font.getAscent(line.fontSize);
            }
        }
        y1 += maxAscent;

        float runLength = 0f;
        for (TextLine line : paragraph.lines) {
            String text = line.text == null ? "" : line.text;
            String[] tokens = Util.splitOnWhitespace(text);
            TextLine textLine = null;
            for (String token : tokens) {
                textLine = line.copyWithText(token + Single.space);
                runLength += textLine.getWidth();
                if (runLength < this.w) {
                    list.add(textLine);
                } else {
                    drawLineOfText(page, list, alignment);
                    moveToNextLine(lineHeight);
                    list.clear();
                    list.add(textLine);
                    runLength = textLine.getWidth();
                }
            }
            if (textLine != null) {
                textLine.isLastToken = true;
            }
        }
        drawNonJustifiedLine(page, list, alignment);

        if (lineBetweenParagraphs) {
            moveToNextLine(lineHeight);
        }

        return moveToNextParagraph(lineHeight * this.paragraphSpacing);
    }

    private float[] moveToNextLine(float lineHeight) {
        this.x1 = x;
        this.y1 += lineHeight;
        return new float[] {x1, y1};
    }

    private float[] moveToNextParagraph(float paragraphSpacing) {
        x1 = x;
        y1 += paragraphSpacing;
        return new float[] {x1, y1};
    }

    private void drawLineOfText(Page page, List<TextLine> list, Alignment alignment) throws Exception {
        if (alignment == Alignment.JUSTIFY) {
            float sumOfWordWidths = 0f;
            for (TextLine textLine : list) {
                sumOfWordWidths += textLine.getWidth();
            }
            float dx = (w - sumOfWordWidths) / (list.size() - 1);

            // Each token draws its own link annotation when the line has a URI or GoTo action.
            for (TextLine textLine : list) {
                textLine.setLocation(x1, y1 + textLine.getVerticalOffset());
                textLine.drawOn(page);
                x1 += textLine.getWidth() + dx;
            }
        } else {
            drawNonJustifiedLine(page, list, alignment);
        }
    }

    private void drawNonJustifiedLine(Page page, List<TextLine> list, Alignment alignment) throws Exception {
        float runLength = 0f;
        for (TextLine textLine : list) {
            runLength += textLine.getWidth();
        }

        if (alignment == Alignment.CENTER) {
            x1 = x + ((w - runLength) / 2);
        } else if (alignment == Alignment.RIGHT) {
            x1 = x + (w - runLength);
        }

        // Each token draws its own link annotation when the line has a URI or GoTo action.
        for (TextLine textLine : list) {
            textLine.setLocation(x1, y1 + textLine.getVerticalOffset());
            textLine.drawOn(page);
            x1 += textLine.getWidth();
        }
    }

    /**
     * Adds a paragraph of Chinese, Japanese or Korean text to this text column,
     * wrapped at any character to the width of the column.
     *
     * @param font the font used by this paragraph.
     * @param text the text.
     * @return this TextColumn object.
     */
    public TextColumn addCJKParagraph(Font font, String text) {
        Paragraph paragraph;
        StringBuilder buf = new StringBuilder();
        int i = 0;
        while (i < text.length()) {
            int ch = text.codePointAt(i);
            i += Character.charCount(ch);
            String str = new String(Character.toChars(ch));
            if (font.stringWidth(buf.toString() + str) > w) {
                paragraph = new Paragraph();
                paragraph.add(new TextLine(font, buf.toString()));
                addParagraph(paragraph);
                buf.setLength(0);
            }
            buf.appendCodePoint(ch);
        }
        paragraph = new Paragraph();
        paragraph.add(new TextLine(font, buf.toString()));
        return addParagraph(paragraph);
    }
}   // End of TextColumn.java
