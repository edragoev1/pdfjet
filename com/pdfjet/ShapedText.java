/*
 * ShapedText.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

// A line of TextBlock or TextLine a shaper shaped: its glyphs, drawn as
// outlines in its fonts, and its text, which tags them.
final class ShapedText {
    ShapedLine line;
    String text;
    Font[] fonts;
    float size;

    ShapedText(ShapedLine line, String text, Font[] fonts, float size) {
        this.line = line;
        this.text = text;
        this.fonts = fonts;
        this.size = size;
    }
}
