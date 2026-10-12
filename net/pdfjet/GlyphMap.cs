/*
 * GlyphMap.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
// Maps the characters of the Basic Multilingual Plane to the glyphs of a
// font, as its cmap table does, in two levels: 256 blocks of 256 characters,
// a block made only where the font has a character in it. A font of 1,000
// characters in a few blocks takes some kilobytes, where an array of an int
// for each of the 65,536 characters took 256 KB for every font of every
// document; a lookup is two reads of an array still.
internal sealed class GlyphMap {
    internal readonly int[][] blocks = new int[256][];

    // Returns the glyph of the character, or 0 for one the font does not have.
    internal int Get(int c) {
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
    internal void Set(int c, int gid) {
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

    // Calls the action for each character that has a glyph, in the order of
    // the characters.
    internal void Each(Action<int, int> action) {
        for (int b = 0; b < 256; b++) {
            int[] block = blocks[b];
            if (block == null) {
                continue;
            }
            for (int i = 0; i < 256; i++) {
                if (block[i] != 0) {
                    action(b << 8 | i, block[i]);
                }
            }
        }
    }

    // Returns the character of each glyph of the map, as the ToUnicode map
    // writes it, indexed by glyph number, -1 for a glyph of no character: the
    // first of those that share a glyph, in the order of the characters, but
    // one that is seldom text, as the soft hyphen or a combining mark, gives
    // way to one that is not. It is made once, when the font is read, where
    // the ToUnicode map walked the 65,536 characters twice for every font of
    // every document. A glyph can stand for several characters, like the space
    // and the no-break space, but viewers read a CMap with more than one entry
    // for a glyph differently.
    internal static int[] UnicodeOfGlyphs(GlyphMap map) {
        int count = 0;
        map.Each((c, gid) => { if (gid >= count) { count = gid + 1; } });
        int[] chars = new int[count];
        for (int i = 0; i < count; i++) {
            chars[i] = -1;
        }
        map.Each((c, gid) => {
            if (chars[gid] == -1 ||
                    (FontWriter.IsSeldomText(chars[gid]) && !FontWriter.IsSeldomText(c))) {
                chars[gid] = c;
            }
        });
        return chars;
    }
}
}   // End of namespace PDFjet.NET
