/*
 * Title.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Please see Example_51 and Example_52
 */
public class Title implements Drawable {
    /** The prefix drawn before the title text, such as a section number. */
    public TextLine prefix;
    /** The title text. */
    public TextLine textLine;

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
     * Moves the title text to the right by the specified offset, to make room for the prefix.
     *
     * @param offset the offset.
     * @return this Title object.
     */
    public Title setOffset(float offset) {
        textLine.setLocation(textLine.x + offset, textLine.y);
        return this;
    }

    public Title setLocation(float x, float y) {
        prefix.setLocation(x, y);
        textLine.setLocation(x, y);
        return this;
    }

    /**
     * Sets the location of this title.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Title object.
     */
    public Title setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    public float[] drawOn(Page page) throws Exception {
        if (!prefix.equals("")) {
            prefix.drawOn(page);
        }
        return textLine.drawOn(page);
    }
}
