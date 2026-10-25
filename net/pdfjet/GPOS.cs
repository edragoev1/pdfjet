/*
 * GPOS.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
// Where the marks go, from the GPOS table of a font: the marks on letters and
// ligatures, like Hebrew and Arabic vowel marks, from its MarkToBase and
// MarkToLigature lookups, and the marks that attach to other marks, like a
// Thai tone mark above an upper vowel, from its MarkToMark lookups.
//
// The table is read the first time a mark is drawn, not when the font is
// loaded: most documents draw no mark, and reading every anchor of a font of
// many scripts, as Noto Sans, took most of the time of a page drawn in it
// and much of what it allocated.
internal class GPOS {
    // The most work the GPOS table of a font is read with, counted in the
    // glyphs of the coverage tables it names, the glyphs their indexes make
    // room for, the marks and the letters its mark lookups keep and the pairs
    // they place: each mark against each letter or mark it goes on. The
    // lookups, the subtables, the ranges of a coverage table and its indexes
    // all say their own count, and subtables can share one another, so a
    // table that claims more than a font holds is stopped here rather than
    // read to an end it does not have. Of the 252 fonts PDFjet ships the most
    // this takes is 76,168, in Noto Sans.
    internal const int MAX_GPOS_WORK = 1 << 20;

    private readonly byte[] buf;    // The font data the table is in
    internal readonly int start;    // The table, from start to end of buf
    internal readonly int end;
    internal readonly int numGlyphs; // Of the maxp table, or 0 when the font has none
    internal int gposWork;          // What is left of the work, see MAX_GPOS_WORK
    private bool read;

    // The offsets of the marks that attach to other marks, in font units, by
    // the glyph IDs of the two marks.
    internal Dictionary<int, int[]> markToMarkOffsets;
    // The anchors of the marks, by glyph ID, for each MarkToBase and
    // MarkToLigature lookup subtable: the class and anchor of each mark.
    internal List<Dictionary<int, int[]>> markAnchors;
    // The anchors of the letters and ligatures that the marks go on, by glyph
    // ID, for each subtable in markAnchors: 1 and an anchor, or 0 and no
    // anchor, for each class of marks.
    internal List<Dictionary<int, int[]>> baseAnchors;

    // The GPOS table from start to end of the font data, which is kept until
    // the table is read. A value outside the table is read as 0, so a broken
    // table cannot stop the font from loading, or a mark from being drawn.
    private GPOS(byte[] buf, int start, int end, int numGlyphs) {
        this.buf = buf;
        this.start = start;
        this.end = end;
        this.numGlyphs = numGlyphs;
    }

    // Returns the GPOS table of the font data: the font data itself, which
    // the font keeps, or a copy of the table when the font keeps only a part
    // of it, as a CFF font keeps its CFF table.
    internal static GPOS Of(byte[] buf, int offset, int length, int numGlyphs, bool copy) {
        int start = Math.Max(Math.Min(offset, buf.Length), 0);
        int end = (length < 0) ? start : (int) Math.Min((long) start + length, buf.Length);
        if (copy) {
            byte[] table = new byte[end - start];
            Array.Copy(buf, start, table, 0, end - start);
            return new GPOS(table, 0, end - start, numGlyphs);
        }
        return new GPOS(buf, start, end, numGlyphs);
    }

    // Returns another reader of the same table, with work of its own, as
    // Kerning reads the pairs of the table apart from its marks.
    internal GPOS Reader() {
        return new GPOS(buf, start, end, numGlyphs);
    }

    // Reads the table, the first time it is called, and returns this.
    internal GPOS Read() {
        if (!read) {
            read = true;
            gposWork = MAX_GPOS_WORK;
            markToMarkOffsets = new Dictionary<int, int[]>();
            markAnchors = new List<Dictionary<int, int[]>>();
            baseAnchors = new List<Dictionary<int, int[]>>();
            Lookups();
        }
        return this;
    }

    private void Lookups() {
        int lookupList = start + GetUInt16(start + 8);
        int lookupCount = GetUInt16(lookupList);
        for (int i = 0; i < lookupCount && gposWork > 0; i++) {
            int lookup = lookupList + GetUInt16(lookupList + 2 + 2*i);
            int lookupType = GetUInt16(lookup);
            int subTableCount = GetUInt16(lookup + 4);
            for (int j = 0; j < subTableCount && gposWork > 0; j++) {
                gposWork--;
                int subTable = lookup + GetUInt16(lookup + 6 + 2*j);
                int type = lookupType;
                if (type == 9) {
                    // An extension lookup holds the subtable, at an offset of 32 bits
                    // that is read unsigned: one of 0x80000000 or more wrapped round
                    // into the table; past the table, there is no subtable, as in Go
                    // (review of 12 October 2026, R27)
                    type = GetUInt16(subTable + 2);
                    long extension = GetUInt32(subTable + 4);
                    if (extension >= end - subTable) {
                        continue;
                    }
                    subTable += (int) extension;
                }
                if (type == 4 && GetUInt16(subTable) == 1) {
                    MarkToBase(subTable, false);
                } else if (type == 5 && GetUInt16(subTable) == 1) {
                    MarkToBase(subTable, true);
                } else if (type == 6 && GetUInt16(subTable) == 1) {
                    MarkToMark(subTable);
                }
            }
        }
    }


    // Keeps the anchors of a MarkToBase or MarkToLigature subtable, by glyph
    // ID: the class and anchor of each mark, and an anchor of each letter for
    // each class of marks, with 1 before an anchor that is there and 0 before
    // one that is not. A ligature has anchors for each of the letters it joins,
    // and keeps those of its first letter, like the lam of a lam-alef ligature.
    private void MarkToBase(int subTable, bool ligature) {
        int[] markGlyphs = Coverage(subTable + GetUInt16(subTable + 2));
        int[] baseGlyphs = Coverage(subTable + GetUInt16(subTable + 4));
        int classCount = GetUInt16(subTable + 6);
        int markArray = subTable + GetUInt16(subTable + 8);
        int baseArray = subTable + GetUInt16(subTable + 10);
        Dictionary<int, int[]> marks = new Dictionary<int, int[]>();
        for (int m = 0; m < markGlyphs.Length; m++) {
            if (gposWork == 0) {
                break;
            }
            gposWork--;
            int anchor = markArray + GetUInt16(markArray + 4 + 4*m);
            marks[markGlyphs[m]] = new int[] {
                    GetUInt16(markArray + 2 + 4*m), GetInt16(anchor + 2), GetInt16(anchor + 4)};
        }
        Dictionary<int, int[]> bases = new Dictionary<int, int[]>();
        for (int b = 0; b < baseGlyphs.Length; b++) {
            // A letter is work of its own, whatever the classes of its marks.
            int work = Math.Max(classCount, 1);
            if (gposWork < work) {
                break;
            }
            gposWork -= work;
            // The anchor offsets are from the base array, or from the ligature
            // attach table of a ligature.
            int table = baseArray;
            int record = baseArray + 2 + 2*b*classCount;
            if (ligature) {
                table = baseArray + GetUInt16(baseArray + 2 + 2*b);
                record = table + 2;
                if (GetUInt16(table) == 0) {    // No letters
                    continue;
                }
            }
            int[] anchors = new int[3*classCount];
            for (int c = 0; c < classCount; c++) {
                int anchorOffset = GetUInt16(record + 2*c);
                if (anchorOffset != 0) {
                    anchors[3*c] = 1;
                    anchors[3*c + 1] = GetInt16(table + anchorOffset + 2);
                    anchors[3*c + 2] = GetInt16(table + anchorOffset + 4);
                }
            }
            bases[baseGlyphs[b]] = anchors;
        }
        markAnchors.Add(marks);
        baseAnchors.Add(bases);
    }

    // Keeps the offset of each mark from the mark it attaches to, in font
    // units, by the glyph IDs of the two marks. The first lookup that has a
    // pair of marks places them.
    private void MarkToMark(int subTable) {
        int[] mark1Glyphs = Coverage(subTable + GetUInt16(subTable + 2));
        int[] mark2Glyphs = Coverage(subTable + GetUInt16(subTable + 4));
        int classCount = GetUInt16(subTable + 6);
        int mark1Array = subTable + GetUInt16(subTable + 8);
        int mark2Array = subTable + GetUInt16(subTable + 10);
        for (int m1 = 0; m1 < mark1Glyphs.Length; m1++) {
            int work = Math.Max(mark2Glyphs.Length, 1);
            if (gposWork < work) {
                break;
            }
            gposWork -= work;
            int markClass = GetUInt16(mark1Array + 2 + 4*m1);
            int anchor1 = mark1Array + GetUInt16(mark1Array + 4 + 4*m1);
            for (int m2 = 0; m2 < mark2Glyphs.Length; m2++) {
                int anchorOffset = GetUInt16(mark2Array + 2 + 2*(m2*classCount + markClass));
                if (anchorOffset == 0) {
                    continue;
                }
                int anchor2 = mark2Array + anchorOffset;
                int key = (mark2Glyphs[m2] << 16) | mark1Glyphs[m1];
                if (!markToMarkOffsets.ContainsKey(key)) {
                    markToMarkOffsets[key] = new int[] {
                            GetInt16(anchor2 + 2) - GetInt16(anchor1 + 2),
                            GetInt16(anchor2 + 4) - GetInt16(anchor1 + 4)};
                }
            }
        }
    }

    // Returns the glyph IDs of a coverage table, in the order of their coverage indexes.
    internal int[] Coverage(int offset) {
        int format = GetUInt16(offset);
        int count = GetUInt16(offset + 2);
        if (count > gposWork) {
            count = gposWork;
        }
        gposWork -= count;
        if (format == 1) {
            int[] list = new int[count];
            for (int i = 0; i < count; i++) {
                list[i] = GetUInt16(offset + 4 + 2*i);
            }
            return list;
        }
        // Format 2 has ranges of glyphs, each with the coverage index of its first glyph.
        int size = 0;
        for (int i = 0; i < count; i++) {
            int range = offset + 4 + 6*i;
            int start = GetUInt16(range);
            int end = GetUInt16(range + 2);
            if (end >= start) {
                size = Math.Max(size, GetUInt16(range + 4) + end - start + 1);
            }
        }
        // A coverage table lists each glyph of the font once at most, and the
        // room its indexes make is counted as work, as each glyph of it is
        // gone through by the lookup it is of.
        int limit = 0x10000;    // A glyph ID is 16 bits.
        if (numGlyphs > 0) {
            limit = numGlyphs;
        }
        size = Math.Min(Math.Min(size, limit), gposWork);
        gposWork -= size;
        int[] glyphs = new int[size];
        for (int i = 0; i < count && gposWork > 0; i++) {
            int range = offset + 4 + 6*i;
            int start = GetUInt16(range);
            int end = GetUInt16(range + 2);
            int coverageIndex = GetUInt16(range + 4);
            // The glyphs of the range past the room of the indexes are left out.
            end = Math.Min(end, start + size - 1 - coverageIndex);
            for (int glyph = start; glyph <= end && gposWork > 0; glyph++) {
                gposWork--;
                glyphs[coverageIndex + glyph - start] = glyph;
            }
        }
        return glyphs;
    }

    // Returns the 16 bits at the offset, or 0 outside the table.
    internal int GetUInt16(int offset) {
        if (offset < start || offset > end - 2) {
            return 0;
        }
        return (buf[offset] << 8) | buf[offset + 1];
    }

    internal int GetInt16(int offset) {
        return (short) GetUInt16(offset);
    }

    internal long GetUInt32(int offset) {
        return ((long) GetUInt16(offset) << 16) | (long) GetUInt16(offset + 2);
    }
}
}
