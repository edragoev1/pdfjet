/*
 * Shaping.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;

// The shaping of text such as Hindi or Khmer, for TextBlock and TextLine:
// which text needs a shaper, which shaper, and the shaping, checked.
final class Shaping {
    private Shaping() {
    }

    // A shaper in place of the platform's, for the tests alone: not public,
    // as Java always has a text engine of its own
    static volatile Shaper defaultShaperSet;

    // The shaper of TextBlock and TextLine when none is set: Android's text
    // engine on Android 12 or later, else Java's java.awt.font
    static Shaper defaultShaper() {
        Shaper set = defaultShaperSet;
        if (set != null) {
            return set;
        }
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

    // The fonts text is shaped in: the font, and the fallback font for the
    // characters the font has not, when it has a file the text engine can
    // read; a fallback without one, as a core font, is left out, so that a
    // Latin fallback does not fail Hindi text.
    static Font[] shapingFonts(Font font, Font fallback) {
        if (fallback != null && fallback != font) {
            try {
                if (fallback.fontFile() != null) {
                    return new Font[] {font, fallback};
                }
            } catch (Exception e) {
                // Left out, as a font without a file
            }
        }
        return new Font[] {font};
    }

    // Shapes the text as one line, not wrapped, with the default shaper: the
    // lines of a shaper that broke it anyway, joined.
    static ShapedText shapeOneLine(Font[] fonts, float size, String language, String text) throws Exception {
        List<ShapedLine> lines = shapeText(null, fonts, size, language, text, Float.MAX_VALUE / 4);
        ShapedLine joined = null;
        for (ShapedLine line : lines) {
            if (joined == null) {
                joined = line;
                continue;
            }
            float x0 = joined.getWidth();
            int a = joined.getGlyphs().length;
            int n = a + line.getGlyphs().length;
            int[] fontIndexes = new int[n];
            int[] ids = new int[n];
            float[] positions = new float[2 * n];
            System.arraycopy(joined.getFontIndexes(), 0, fontIndexes, 0, a);
            System.arraycopy(line.getFontIndexes(), 0, fontIndexes, a, n - a);
            System.arraycopy(joined.getGlyphs(), 0, ids, 0, a);
            System.arraycopy(line.getGlyphs(), 0, ids, a, n - a);
            System.arraycopy(joined.getPositions(), 0, positions, 0, 2 * a);
            float[] more = line.getPositions();
            for (int j = 0; j < more.length; j += 2) {
                positions[2 * a + j] = x0 + more[j];
                positions[2 * a + j + 1] = more[j + 1];
            }
            joined = new ShapedLine(joined.getStart(), line.getEnd(), fontIndexes, ids, positions, x0 + line.getWidth());
        }
        if (joined == null) {
            joined = new ShapedLine(0, text.length(), new int[0], new int[0], new float[0], 0f);
        }
        return new ShapedText(joined, text, fonts, size);
    }

    // What shaped text depends on besides its text: a cache of shaped text is
    // good only for the style it was made in.
    static final class Style {
        private final Font font;
        private final Font fallback;
        private final float size;
        private final String language;

        Style(Font font, Font fallback, float size, String language) {
            this.font = font;
            this.fallback = fallback;
            this.size = size;
            this.language = language;
        }

        @Override
        public boolean equals(Object other) {
            if (!(other instanceof Style)) {
                return false;
            }
            Style style = (Style) other;
            return font == style.font && fallback == style.fallback && size == style.size
                    && Objects.equals(language, style.language);
        }

        @Override
        public int hashCode() {
            return Objects.hash(System.identityHashCode(font), System.identityHashCode(fallback), size, language);
        }
    }

    // A TextLine's cache of the texts it shaped, its own and its words', which
    // the copies a TextFrame or a TextColumn draws share, so that each is
    // shaped once, however often it is measured and drawn.
    static final class Pieces {
        final Style style;
        final Map<String, ShapedText> shaped = new HashMap<>();

        Pieces(Style style) {
            this.style = style;
        }
    }

    // The exception a document fails with, for one a shaper threw
    static RuntimeException failure(Exception e) {
        return e instanceof RuntimeException ? (RuntimeException) e : new IllegalStateException(e.getMessage(), e);
    }
}
