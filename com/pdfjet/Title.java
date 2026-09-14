/*
 * Title.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Please see Example_48
 */
public class Title implements Drawable {
    TextLine prefix;
    TextLine textLine;

    private float offset = 0f;

    /**
     * Creates a title.
     *
     * @param font the font.
     * @param title the title text.
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public Title(Font font, String title, float x, float y) {
        this.prefix = new TextLine(font);
        this.prefix.setLocation(x, y);
        this.textLine = new TextLine(font, title);
        this.textLine.setLocation(x, y);
    }

    /**
     * Sets the prefix text.
     *
     * @param text the prefix text.
     * @return this Title object.
     */
    public Title setPrefix(String text) {
        prefix.setText(text);
        return this;
    }

    /**
     * Sets the distance from the start of the prefix to the start of the title
     * text, to make room for the prefix.
     *
     * @param offset the offset.
     * @return this Title object.
     */
    public Title setOffset(float offset) {
        this.offset = offset;
        textLine.setLocation(prefix.x + offset, prefix.y);
        return this;
    }

    /**
     * Returns the prefix drawn before the title text, such as a section number.
     *
     * @return the prefix text line.
     */
    public TextLine getPrefix() {
        return prefix;
    }

    /**
     * Returns the title text.
     *
     * @return the title text line.
     */
    public TextLine getTextLine() {
        return textLine;
    }

    /**
     * Sets the location of the prefix; the title text keeps its offset from it.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Title object.
     */
    public Title setLocation(float x, float y) {
        prefix.setLocation(x, y);
        textLine.setLocation(x + offset, y);
        return this;
    }

    public float[] drawOn(Page page) throws Exception {
        if (!prefix.equals("")) {
            prefix.drawOn(page);
        }
        return textLine.drawOn(page);
    }
}
