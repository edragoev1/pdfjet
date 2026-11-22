/*
 * Shaping.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

// The shaping of text such as Hindi or Khmer, for TextBlock and TextLine:
// which text needs a shaper, which shaper, and the shaping, checked.
final class Shaping {
    private Shaping() {
    }

    // The shaper of TextBlock and TextLine when none is set: Android's text
    // engine on Android 12 or later, else Java's java.awt.font
    static Shaper defaultShaper() {
        if (AndroidShaper.available()) {
            return new AndroidShaper();
        }
        if (AndroidShaper.onAndroid()) {
            throw new IllegalStateException(
                    "Text such as Hindi or Khmer needs Android 12 or later, whose text engine shapes the text.");
        }
        return new AwtShaper();
    }

    // Whether the text has a character of a script whose letters change
    // shape and place, which a shaper draws: Devanagari, Bengali, Gurmukhi,
    // Gujarati, Oriya, Tamil, Telugu, Kannada, Malayalam, Sinhala, Lao,
    // Myanmar and Khmer. Thai is not one: TextBlock draws it as text, its
    // lines broken by ICU's dictionary.
    static boolean needsShaper(String text) {
        for (int i = 0; i < text.length(); ) {
            int c = text.codePointAt(i);
            i += Character.charCount(c);
            if (c < 0x0900) {
                continue;
            }
            if ((c >= 0x0900 && c <= 0x0DFF)           // Devanagari to Sinhala
                    || (c >= 0x0E80 && c <= 0x0EFF)    // Lao
                    || (c >= 0x1000 && c <= 0x109F)    // Myanmar
                    || (c >= 0x1780 && c <= 0x17FF) || (c >= 0x19E0 && c <= 0x19FF)    // Khmer
                    || (c >= 0xA8E0 && c <= 0xA8FF)    // Devanagari Extended
                    || (c >= 0xA9E0 && c <= 0xA9FF) || (c >= 0xAA60 && c <= 0xAA7F)    // Myanmar Extended
                    || (c >= 0x111E0 && c <= 0x111FF)) {   // Sinhala Archaic Numbers
                return true;
            }
        }
        return false;
    }

    // Shapes the text with the shaper, or the default one, in the fonts, at
    // the size, wrapped to the width; the lines checked, so that a shaper of
    // the program's own cannot make PDFjet draw past them.
    static List<ShapedLine> shapeText(Shaper shaper, Font[] fonts, float size, String language,
            String text, float width) throws Exception {
        Shaper engine = shaper != null ? shaper : defaultShaper();
        byte[][] files = new byte[fonts.length][];
        for (int i = 0; i < fonts.length; i++) {
            files[i] = fonts[i].fontFile();
            if (files[i] == null) {
                throw new IllegalArgumentException("Text such as Hindi or Khmer needs fonts loaded from their "
                        + "font files, by path or from their bytes, as the text engine reads the same file.");
            }
        }
        List<ShapedLine> lines = engine.shape(new ShapingRequest(text, language, files, size, width));
        if (lines == null) {
            lines = new ArrayList<>();
        }
        for (ShapedLine line : lines) {
            if (line.getFontIndexes() == null || line.getGlyphs() == null || line.getPositions() == null
                    || line.getFontIndexes().length != line.getGlyphs().length
                    || line.getPositions().length != 2 * line.getGlyphs().length
                    || line.getStart() < 0 || line.getStart() > line.getEnd() || line.getEnd() > text.length()) {
                throw new IllegalArgumentException("A shaped line needs a font and an x and a y for each glyph, "
                        + "and its part of the text.");
            }
        }
        return lines;
    }

    // The exception a document fails with, for one a shaper threw
    static RuntimeException failure(Exception e) {
        return e instanceof RuntimeException ? (RuntimeException) e : new IllegalStateException(e.getMessage(), e);
    }
}
