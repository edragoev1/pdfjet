/**
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
    /** The xText coordinate. */
    public float xText;

    /** The yText coordinate. */
    public float yText;

    /** The x1 coordinate. */
    public float x1;

    /** The y1 coordinate. */
    public float y1;

    /** The x2 coordinate. */
    public float x2;

    /** The xy coordinate. */
    public float y2;

    protected List<TextLine> lines = null;
    protected int alignment = Align.LEFT;

    /**
     * Constructor for creating paragraph objects.
     */
    public Paragraph() {
        lines = new ArrayList<TextLine>();
    }

    /**
     * Constructor for creating paragraph objects.
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
     * @param alignment the alignment code.
     * @return this paragraph.
     */
    public Paragraph setAlignment(int alignment) {
        this.alignment = alignment;
        return this;
    }

    /**
     * Returns the text lines.
     *
     * @return the list of text lines.
     */
    public List<TextLine> getTextLines() {
        return lines;
    }

    /**
     * Checks if the line starts with the specified token.
     *
     * @param token the token.
     * @return true if the line starts with the specified token.
     */
    public boolean startsWith(String token) {
        return lines.get(0).getText().startsWith(token);
    }

    /**
     * Sets the text lines color.
     *
     * @param color the text lines color.
     */
    public void setColor(int color) {
        for (TextLine line : lines) {
            line.setTextColor(color);
        }
    }

    /**
     * Sets the text lines color based on the specified color map.
     *
     * @param colorMap the color map.
     */
    public void setColorMap(Map<String, Integer> colorMap) {
        for (TextLine line : lines) {
            line.setColorMap(colorMap);
        }
    }
}   // End of Paragraph.java
