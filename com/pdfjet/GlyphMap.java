/*
 * GlyphMap.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.Arrays;
import java.util.Map;
import java.util.TreeMap;

/**
 * Maps the characters to the glyphs of a font, as its cmap table does: those
 * of the Basic Multilingual Plane in two levels, 256 blocks of 256
 * characters, a block made only where the font has a character in it, and
 * the characters past U+FFFF, as the Chinese characters of Hong Kong that IBM
 * Plex Sans TC has, in a map of their own, in their order. A font of 1,000
 * characters in a few blocks takes some kilobytes, where an array of an int
 * for each of the 65,536 characters took 256 KB for every font of every
 * document; a lookup is two reads of an array still.
 */
class GlyphMap {
    final int[][] blocks = new int[256][];
    // The characters past U+FFFF, in their order
    TreeMap<Integer, Integer> astral = null;

    // The most characters past U+FFFF a map takes, a font has a glyph for at
    // most 65,535: a font of groups of a million characters each is read no
    // further.
    static final int MAX_ASTRAL = 0x10000;

    /** Receives a character and its glyph, from each. */
    interface Visitor {
        void visit(int c, int gid);
    }

    // Returns the glyph of the character, or 0 for one the font does not have.
    int get(int c) {
        if (c < 0 || c > 0x10FFFF) {
            return 0;
        }
        if (c > 0xFFFF) {
            Integer gid = astral == null ? null : astral.get(c);
            return gid == null ? 0 : gid;
        }
        int[] block = blocks[c >> 8];
        if (block == null) {
            return 0;
        }
        return block[c & 0xFF];
    }

    // Sets the glyph of the character.
    void set(int c, int gid) {
        if (c > 0xFFFF) {
            setAstral(c, gid, false);
            return;
        }
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

    // Sets the glyph of the character unless it has one: as get and then
    // set, with one lookup of its block, for the format 12 subtable of a
    // font, which maps again the characters its format 4 subtable mapped
    // (12 October 2026).
    void setIfAbsent(int c, int gid) {
        if (c > 0xFFFF) {
            setAstral(c, gid, true);
            return;
        }
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

    // Maps a character past U+FFFF to the glyph, or only if it has none yet,
    // within MAX_ASTRAL characters.
    private void setAstral(int c, int gid, boolean ifAbsent) {
        if (c > 0x10FFFF) {
            return;
        }
        if (astral == null) {
            if (gid == 0) {
                return;
            }
            astral = new TreeMap<Integer, Integer>();
        }
        if (astral.containsKey(c)) {
            if (!ifAbsent) {
                astral.put(c, gid);
            }
            return;
        }
        if (gid != 0 && astral.size() < MAX_ASTRAL) {
            astral.put(c, gid);
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
        if (astral != null) {
            for (Map.Entry<Integer, Integer> e : astral.entrySet()) {
                if (e.getValue() != 0) {
                    visitor.visit(e.getKey(), e.getValue());
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
        if (map.astral != null) {
            for (int gid : map.astral.values()) {
                if (gid > max) {
                    max = gid;
                }
            }
        }
        final int[] chars = new int[max + 1];
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
        if (map.astral != null) {
            for (Map.Entry<Integer, Integer> e : map.astral.entrySet()) {
                int gid = e.getValue();
                int c = e.getKey();
                if (chars[gid] == -1 ||
                        (FontWriter.isSeldomText(chars[gid]) && !FontWriter.isSeldomText(c))) {
                    chars[gid] = c;
                }
            }
        }
        return chars;
    }
}
