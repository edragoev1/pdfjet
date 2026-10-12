/*
 * GPOS.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Where the marks go, from the GPOS table of a font: the marks on letters and
 * ligatures, like Hebrew and Arabic vowel marks, from its MarkToBase and
 * MarkToLigature lookups, and the marks that attach to other marks, like a
 * Thai tone mark above an upper vowel, from its MarkToMark lookups.
 *
 * The table is read the first time a mark is drawn, not when the font is
 * loaded: most documents draw no mark, and reading every anchor of a font of
 * many scripts, as Noto Sans, took most of the time of a page drawn in it
 * and much of what it allocated.
 */
class GPOS {
    // The most work the GPOS table of a font is read with, counted in the
    // glyphs of the coverage tables it names, the glyphs their indexes make
    // room for, the marks and the letters its mark lookups keep and the pairs
    // they place: each mark against each letter or mark it goes on. The
    // lookups, the subtables, the ranges of a coverage table and its indexes
    // all say their own count, and subtables can share one another, so a
    // table that claims more than a font holds is stopped here rather than
    // read to an end it does not have. Of the 252 fonts PDFjet ships the most
    // this takes is 76,168, in Noto Sans.
    private static final int MAX_GPOS_WORK = 1 << 20;

    private final byte[] buf;   // The font data the table is in
    private final int start;    // The table, from start to end of buf
    private final int end;
    private final int numGlyphs;    // Of the maxp table, or 0 when the font has none
    private int gposWork;
    private boolean read;

    /**
     * The offsets of the marks that attach to other marks, in font units, by
     * the glyph IDs of the two marks.
     */
    Map<Integer, int[]> markToMarkOffsets;
    /**
     * The anchors of the marks, by glyph ID, for each MarkToBase and
     * MarkToLigature lookup subtable: the class and anchor of each mark.
     */
    List<Map<Integer, int[]>> markAnchors;
    /**
     * The anchors of the letters and ligatures that the marks go on, by glyph
     * ID, for each subtable in markAnchors: 1 and an anchor, or 0 and no
     * anchor, for each class of marks.
     */
    List<Map<Integer, int[]>> baseAnchors;

    /**
     * The GPOS table from start to end of the font data, which is kept until
     * the table is read. A value outside the table is read as 0, so a broken
     * table cannot stop the font from loading, or a mark from being drawn.
     */
    GPOS(byte[] buf, int start, int end, int numGlyphs) {
        this.buf = buf;
        this.start = start;
        this.end = end;
        this.numGlyphs = numGlyphs;
    }

    /**
     * Returns the GPOS table of the font data, or null for a font without
     * one: the font data itself, which the font keeps, or a copy of the table
     * when the font keeps only a part of it, as a CFF font keeps its CFF
     * table.
     */
    static GPOS of(byte[] buf, int offset, int length, int numGlyphs, boolean copy) {
        int start = Math.max(Math.min(offset, buf.length), 0);
        int end = (length < 0) ? start : (int) Math.min((long) start + length, buf.length);
        if (copy) {
            return new GPOS(Arrays.copyOfRange(buf, start, end), 0, end - start, numGlyphs);
        }
        return new GPOS(buf, start, end, numGlyphs);
    }

    /**
     * Reads the table, the first time it is called, and returns this.
     */
    GPOS read() {
        if (!read) {
            read = true;
            gposWork = MAX_GPOS_WORK;
            markToMarkOffsets = new HashMap<Integer, int[]>();
            markAnchors = new ArrayList<Map<Integer, int[]>>();
            baseAnchors = new ArrayList<Map<Integer, int[]>>();
            lookups();
        }
        return this;
    }

    private void lookups() {
        int lookupList = start + getUInt16(start + 8);
        int lookupCount = getUInt16(lookupList);
        for (int i = 0; i < lookupCount && gposWork > 0; i++) {
            int lookup = lookupList + getUInt16(lookupList + 2 + 2*i);
            int lookupType = getUInt16(lookup);
            int subTableCount = getUInt16(lookup + 4);
            for (int j = 0; j < subTableCount && gposWork > 0; j++) {
                gposWork--;
                int subTable = lookup + getUInt16(lookup + 6 + 2*j);
                int type = lookupType;
                if (type == 9) {
                    // An extension lookup holds the subtable, at an offset of 32 bits
                    // that is read unsigned: one of 0x80000000 or more wrapped round
                    // into the table; past the table, there is no subtable, as in Go
                    // (review of 12 October 2026, R27)
                    type = getUInt16(subTable + 2);
                    long extension = getUInt32(subTable + 4);
                    if (extension >= end - subTable) {
                        continue;
                    }
                    subTable += (int) extension;
                }
                if (type == 4 && getUInt16(subTable) == 1) {
                    markToBase(subTable, false);
                } else if (type == 5 && getUInt16(subTable) == 1) {
                    markToBase(subTable, true);
                } else if (type == 6 && getUInt16(subTable) == 1) {
                    markToMark(subTable);
                }
            }
        }
    }

    // Keeps the anchors of a MarkToBase or MarkToLigature subtable, by glyph
    // ID: the class and anchor of each mark, and an anchor of each letter for
    // each class of marks, with 1 before an anchor that is there and 0 before
    // one that is not. A ligature has anchors for each of the letters it joins,
    // and keeps those of its first letter, like the lam of a lam-alef ligature.
    private void markToBase(int subTable, boolean ligature) {
        int[] markGlyphs = coverage(subTable + getUInt16(subTable + 2));
        int[] baseGlyphs = coverage(subTable + getUInt16(subTable + 4));
        int classCount = getUInt16(subTable + 6);
        int markArray = subTable + getUInt16(subTable + 8);
        int baseArray = subTable + getUInt16(subTable + 10);
        Map<Integer, int[]> marks = new HashMap<Integer, int[]>();
        for (int m = 0; m < markGlyphs.length; m++) {
            if (gposWork == 0) {
                break;
            }
            gposWork--;
            int anchor = markArray + getUInt16(markArray + 4 + 4*m);
            marks.put(markGlyphs[m], new int[] {
                    getUInt16(markArray + 2 + 4*m), getInt16(anchor + 2), getInt16(anchor + 4)});
        }
        Map<Integer, int[]> bases = new HashMap<Integer, int[]>();
        for (int b = 0; b < baseGlyphs.length; b++) {
            // A letter is work of its own, whatever the classes of its marks.
            int work = Math.max(classCount, 1);
            if (gposWork < work) {
                break;
            }
            gposWork -= work;
            // The anchor offsets are from the base array, or from the ligature
            // attach table of a ligature.
            int table = baseArray;
            int record = baseArray + 2 + 2*b*classCount;
            if (ligature) {
                table = baseArray + getUInt16(baseArray + 2 + 2*b);
                record = table + 2;
                if (getUInt16(table) == 0) {    // No letters
                    continue;
                }
            }
            int[] anchors = new int[3*classCount];
            for (int c = 0; c < classCount; c++) {
                int anchorOffset = getUInt16(record + 2*c);
                if (anchorOffset != 0) {
                    anchors[3*c] = 1;
                    anchors[3*c + 1] = getInt16(table + anchorOffset + 2);
                    anchors[3*c + 2] = getInt16(table + anchorOffset + 4);
                }
            }
            bases.put(baseGlyphs[b], anchors);
        }
        markAnchors.add(marks);
        baseAnchors.add(bases);
    }

    // Keeps the offset of each mark from the mark it attaches to, in font
    // units, by the glyph IDs of the two marks. The first lookup that has a
    // pair of marks places them.
    private void markToMark(int subTable) {
        int[] mark1Glyphs = coverage(subTable + getUInt16(subTable + 2));
        int[] mark2Glyphs = coverage(subTable + getUInt16(subTable + 4));
        int classCount = getUInt16(subTable + 6);
        int mark1Array = subTable + getUInt16(subTable + 8);
        int mark2Array = subTable + getUInt16(subTable + 10);
        for (int m1 = 0; m1 < mark1Glyphs.length; m1++) {
            int work = Math.max(mark2Glyphs.length, 1);
            if (gposWork < work) {
                break;
            }
            gposWork -= work;
            int markClass = getUInt16(mark1Array + 2 + 4*m1);
            int anchor1 = mark1Array + getUInt16(mark1Array + 4 + 4*m1);
            for (int m2 = 0; m2 < mark2Glyphs.length; m2++) {
                int anchorOffset = getUInt16(mark2Array + 2 + 2*(m2*classCount + markClass));
                if (anchorOffset == 0) {
                    continue;
                }
                int anchor2 = mark2Array + anchorOffset;
                int key = (mark2Glyphs[m2] << 16) | mark1Glyphs[m1];
                if (!markToMarkOffsets.containsKey(key)) {
                    markToMarkOffsets.put(key, new int[] {
                            getInt16(anchor2 + 2) - getInt16(anchor1 + 2),
                            getInt16(anchor2 + 4) - getInt16(anchor1 + 4)});
                }
            }
        }
    }

    // Returns the glyph IDs of a coverage table, in the order of their coverage indexes.
    private int[] coverage(int offset) {
        int format = getUInt16(offset);
        int count = getUInt16(offset + 2);
        if (count > gposWork) {
            count = gposWork;
        }
        gposWork -= count;
        if (format == 1) {
            int[] glyphs = new int[count];
            for (int i = 0; i < count; i++) {
                glyphs[i] = getUInt16(offset + 4 + 2*i);
            }
            return glyphs;
        }
        // Format 2 has ranges of glyphs, each with the coverage index of its first glyph.
        int size = 0;
        for (int i = 0; i < count; i++) {
            int range = offset + 4 + 6*i;
            int start = getUInt16(range);
            int end = getUInt16(range + 2);
            if (end >= start) {
                size = Math.max(size, getUInt16(range + 4) + end - start + 1);
            }
        }
        // A coverage table lists each glyph of the font once at most, and the
        // room its indexes make is counted as work, as each glyph of it is
        // gone through by the lookup it is of.
        int limit = (numGlyphs > 0) ? numGlyphs : 0x10000;    // A glyph ID is 16 bits.
        size = Math.min(Math.min(size, limit), gposWork);
        gposWork -= size;
        int[] glyphs = new int[size];
        for (int i = 0; i < count && gposWork > 0; i++) {
            int range = offset + 4 + 6*i;
            int start = getUInt16(range);
            int end = getUInt16(range + 2);
            int coverageIndex = getUInt16(range + 4);
            // The glyphs of the range past the room of the indexes are left out.
            end = Math.min(end, start + size - 1 - coverageIndex);
            for (int glyph = start; glyph <= end && gposWork > 0; glyph++) {
                gposWork--;
                glyphs[coverageIndex + glyph - start] = glyph;
            }
        }
        return glyphs;
    }

    // Returns the 16 bits at the offset, or 0 outside the table.
    private int getUInt16(int offset) {
        if (offset < start || offset > end - 2) {
            return 0;
        }
        return ((buf[offset] & 0xFF) << 8) | (buf[offset + 1] & 0xFF);
    }

    private int getInt16(int offset) {
        return (short) getUInt16(offset);
    }

    private long getUInt32(int offset) {
        return ((long) getUInt16(offset) << 16) | getUInt16(offset + 2);
    }
}
