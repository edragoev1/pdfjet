/*
 * GlyphMap.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
// Maps the characters to the glyphs of a font, as its cmap table does: those
// of the Basic Multilingual Plane in two levels, 256 blocks of 256
// characters, a block made only where the font has a character in it, and
// the characters past U+FFFF, as the Chinese characters of Hong Kong that IBM
// Plex Sans TC has, in a map of their own, in their order. A font of 1,000
// characters in a few blocks takes some kilobytes, where an array of an int
// for each of the 65,536 characters took 256 KB for every font of every
// document; a lookup is two reads of an array still.
internal sealed class GlyphMap {
    internal readonly int[][] blocks = new int[256][];
    // The characters past U+FFFF, in their order
    internal SortedDictionary<int, int> astral = null;

    // The most characters past U+FFFF a map takes, a font has a glyph for at
    // most 65,535: a font of groups of a million characters each is read no
    // further.
    internal const int MAX_ASTRAL = 0x10000;

    // Returns the glyph of the character, or 0 for one the font does not have.
    internal int Get(int c) {
        if (c < 0 || c > 0x10FFFF) {
            return 0;
        }
        if (c > 0xFFFF) {
            int gid;
            return astral != null && astral.TryGetValue(c, out gid) ? gid : 0;
        }
        int[] block = blocks[c >> 8];
        if (block == null) {
            return 0;
        }
        return block[c & 0xFF];
    }

    // Sets the glyph of the character.
    internal void Set(int c, int gid) {
        if (c > 0xFFFF) {
            SetAstral(c, gid, false);
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
    internal void SetIfAbsent(int c, int gid) {
        if (c > 0xFFFF) {
            SetAstral(c, gid, true);
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
    private void SetAstral(int c, int gid, bool ifAbsent) {
        if (c > 0x10FFFF) {
            return;
        }
        if (astral == null) {
            if (gid == 0) {
                return;
            }
            astral = new SortedDictionary<int, int>();
        }
        if (astral.ContainsKey(c)) {
            if (!ifAbsent) {
                astral[c] = gid;
            }
            return;
        }
        if (gid != 0 && astral.Count < MAX_ASTRAL) {
            astral[c] = gid;
        }
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
        if (astral != null) {
            foreach (KeyValuePair<int, int> e in astral) {
                if (e.Value != 0) {
                    action(e.Key, e.Value);
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
