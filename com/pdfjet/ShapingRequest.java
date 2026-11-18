/*
 * ShapingRequest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * What a Shaper is asked to shape: the text, its language, the font files,
 * the font size and the width of the lines. One object, so that what a shaper
 * is told can grow without changing the Shaper interface.
 */
public final class ShapingRequest {
    private final String text;
    private final String language;
    private final byte[][] fontFiles;
    private final float fontSize;
    private final float width;

    /**
     * A request to shape text.
     *
     * @param text the text.
     * @param language the language of the text, such as "hi" or "km", or null.
     * @param fontFiles the bytes of the font files: the first for the text,
     *     the others, in order, for the characters it does not have.
     * @param fontSize the font size, in points.
     * @param width the width of the lines, in points.
     */
    public ShapingRequest(String text, String language, byte[][] fontFiles, float fontSize, float width) {
        this.text = text;
        this.language = language;
        this.fontFiles = fontFiles;
        this.fontSize = fontSize;
        this.width = width;
    }

    /**
     * Returns the text.
     *
     * @return the text.
     */
    public String getText() {
        return text;
    }

    /**
     * Returns the language of the text, such as "hi" or "km", or null.
     *
     * @return the language of the text, such as "hi" or "km", or null.
     */
    public String getLanguage() {
        return language;
    }

    /**
     * Returns the bytes of the font files: the first for the text, the others for the characters it does not have.
     *
     * @return the bytes of the font files: the first for the text, the others for the characters it does not have.
     */
    public byte[][] getFontFiles() {
        return fontFiles;
    }

    /**
     * Returns the font size, in points.
     *
     * @return the font size, in points.
     */
    public float getFontSize() {
        return fontSize;
    }

    /**
     * Returns the width of the lines, in points.
     *
     * @return the width of the lines, in points.
     */
    public float getWidth() {
        return width;
    }
}   // End of ShapingRequest.java
