/*
 * GlyphMap.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.Arrays;

/**
 * Maps the characters of the Basic Multilingual Plane to the glyphs of a
 * font, as its cmap table does, in two levels: 256 blocks of 256 characters,
 * a block made only where the font has a character in it. A font of 1,000
 * characters in a few blocks takes some kilobytes, where an array of an int
 * for each of the 65,536 characters took 256 KB for every font of every
 * document; a lookup is two reads of an array still.
 */
class GlyphMap {
    final int[][] blocks = new int[256][];

    /** Receives a character and its glyph, from each. */
    interface Visitor {
        void visit(int c, int gid);
    }

    // Returns the glyph of the character, or 0 for one the font does not have.
    int get(int c) {
        if (c < 0 || c > 0xFFFF) {
            return 0;
        }
        int[] block = blocks[c >> 8];
        if (block == null) {
            return 0;
        }
        return block[c & 0xFF];
    }

    // Sets the glyph of the character, which is in the Basic Multilingual
    // Plane.
    void set(int c, int gid) {
        int[] block = blocks[c >> 8];
        if (block == null) {
            if (gid == 0) {
                return;
            }
            block = new int[256];
            blocks[c >> 8] = block;
        }
        block[c & 0xFF] = gid;
    }

    // Sets the glyph of the character, which is in the Basic Multilingual
    // Plane, unless it has one: as get and then set, with one lookup of its
    // block, for the format 12 subtable of a font, which maps again the
    // characters its format 4 subtable mapped (12 October 2026).
    void setIfAbsent(int c, int gid) {
        int[] block = blocks[c >> 8];
        if (block == null) {
            if (gid == 0) {
                return;
            }
            block = new int[256];
            blocks[c >> 8] = block;
        }
        if (block[c & 0xFF] == 0) {
            block[c & 0xFF] = gid;
        }
    }

    // Calls the visitor for each character that has a glyph, in the order of
    // the characters.
    void each(Visitor visitor) {
        for (int b = 0; b < 256; b++) {
            int[] block = blocks[b];
            if (block == null) {
                continue;
            }
            for (int i = 0; i < 256; i++) {
                if (block[i] != 0) {
                    visitor.visit(b << 8 | i, block[i]);
                }
            }
        }
    }

    // Returns the character of each glyph of the map, as the ToUnicode map
    // writes it, indexed by glyph number, -1 for a glyph of no character: the
    // first of those that share a glyph, in the order of the characters, but
    // one that is seldom text, as the soft hyphen or a combining mark, gives
    // way to one that is not. A glyph can stand for several characters, like
    // the space and the no-break space, but viewers read a CMap with more than
    // one entry for a glyph differently. It is made once, when the font is
    // read, where the ToUnicode map walked the 65,536 characters twice for
    // every font of every document.
    static int[] unicodeOfGlyphs(GlyphMap map) {
        int max = 0;
        for (int[] block : map.blocks) {
            if (block != null) {
                for (int gid : block) {
                    if (gid > max) {
                        max = gid;
                    }
                }
            }
        }
        int[] chars = new int[max + 1];
        Arrays.fill(chars, -1);
        for (int b = 0; b < 256; b++) {
            int[] block = map.blocks[b];
            if (block == null) {
                continue;
            }
            for (int i = 0; i < 256; i++) {
                int gid = block[i];
                if (gid == 0) {
                    continue;
                }
                int c = b << 8 | i;
                if (chars[gid] == -1 ||
                        (FontWriter.isSeldomText(chars[gid]) && !FontWriter.isSeldomText(c))) {
                    chars[gid] = c;
                }
            }
        }
        return chars;
    }
}
