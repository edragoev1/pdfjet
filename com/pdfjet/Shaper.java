/*
 * Shaper.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.List;

/**
 * A text engine that shapes text for TextBlock and TextLine: it wraps the
 * text to a width and finds, for each line, the glyphs of the fonts and their
 * places. PDFjet does not shape scripts such as Hindi and Khmer itself; a
 * shaper does, and PDFjet draws what it found, as outlines.
 * <p>
 * PDFjet picks the platform's shaper by itself: Java's java.awt.font on
 * servers and desktops, and Android's own text engine on Android 12 and
 * later. A program can give its own, such as one that calls HarfBuzz.
 */
public interface Shaper {
    /**
     * Wraps the text to the width and shapes each line.
     *
     * @param request the text, its language, the font files, the font size
     *     and the width of the lines.
     * @return the lines, in order.
     * @throws Exception if the text cannot be shaped.
     */
    List<ShapedLine> shape(ShapingRequest request) throws Exception;
}   // End of Shaper.java
