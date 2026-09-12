/*
 * Paragraph.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Used to create paragraph objects.
 * See the TextColumn class for more information.
 */
public class Paragraph {
    /** The x coordinate where the text of this paragraph starts. */
    public float xText;

    /** The baseline y coordinate of the first line of this paragraph. */
    public float yText;

    /** The x coordinate of the top left corner of this paragraph. */
    public float x1;

    /** The y coordinate of the top left corner of this paragraph. */
    public float y1;

    /** The x coordinate where the last line of this paragraph ends. */
    public float x2;

    /** The y coordinate of the bottom of the last line of this paragraph. */
    public float y2;

    /** The text lines of this paragraph. */
    protected List<TextLine> lines = null;
    /** The alignment of this paragraph. */
    protected int alignment = Align.LEFT;

    /**
     * Constructor for creating paragraph objects.
     */
    public Paragraph() {
        lines = new ArrayList<TextLine>();
    }

    /**
     * Creates a paragraph with the specified text line.
     *
     * @param text the text line.
     */
    public Paragraph(TextLine text) {
        lines = new ArrayList<TextLine>();
        lines.add(text);
    }

    /**
     * Adds a text line to this paragraph.
     *
     * @param text the text line to add to this paragraph.
     * @return this paragraph.
     */
    public Paragraph add(TextLine text) {
        lines.add(text);
        return this;
    }

    /**
     * Sets the alignment of the text in this paragraph.
     *
     * @param alignment the alignment code: Align.LEFT, Align.RIGHT, Align.CENTER or Align.JUSTIFY.
     * @return this paragraph.
     */
    public Paragraph setAlignment(int alignment) {
        this.alignment = alignment;
        return this;
    }

    /**
     * Returns the text lines of this paragraph.
     *
     * @return the list of text lines.
     */
    public List<TextLine> getTextLines() {
        return lines;
    }

    /**
     * Returns true if the first line of this paragraph starts with the specified token.
     *
     * @param token the token.
     * @return true if the first line starts with the specified token.
     */
    public boolean startsWith(String token) {
        return lines.get(0).getText().startsWith(token);
    }

    /**
     * Sets the text color of all lines in this paragraph as a 0xRRGGBB value.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Paragraph object.
     */
    public Paragraph setColor(int color) {
        for (TextLine line : lines) {
            line.setTextColor(color);
        }
        return this;
    }

    /**
     * Sets the word highlight colors of all lines in this paragraph.
     *
     * @param colorMap the words and their 0xRRGGBB colors.
     * @return this Paragraph object.
     */
    public Paragraph setColorMap(Map<String, Integer> colorMap) {
        for (TextLine line : lines) {
            line.setColorMap(colorMap);
        }
        return this;
    }
}   // End of Paragraph.java
