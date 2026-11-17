/*
 * Shaper.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.List;

/**
 * A text engine that shapes text for ComplexText: it wraps the text to a
 * width and finds, for each line, the glyphs of the fonts and their places.
 * PDFjet does not shape scripts such as Hindi and Khmer itself; a shaper does,
 * and ComplexText draws what it found.
 * <p>
 * ComplexText picks the platform's shaper by itself: Java's java.awt.font on
 * servers and desktops, and Android's own text engine on Android 12 and
 * later. A program can give its own, such as one that calls HarfBuzz.
 */
public interface Shaper {
    /**
     * Wraps the text to the width and shapes each line.
     *
     * @param text the text.
     * @param language the language of the text, such as "hi" or "km", or null.
     * @param fontFiles the bytes of the font files: the first for the text,
     *     the others, in order, for the characters it does not have.
     * @param fontSize the font size, in points.
     * @param width the width of the lines, in points.
     * @return the lines, in order.
     * @throws Exception if the text cannot be shaped.
     */
    List<ShapedLine> shape(String text, String language, byte[][] fontFiles, float fontSize, float width)
            throws Exception;
}   // End of Shaper.java
