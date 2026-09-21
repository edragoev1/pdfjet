/*
 * OTF.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.zip.*;

/**
 * This class parses and extracts the data from TTF and OTF font files.
 */
class OTF {
    String fontName;
    String fontInfo;
    int unitsPerEm;
    short bBoxLLx;
    short bBoxLLy;
    short bBoxURx;
    short bBoxURy;
    short ascent;
    short descent;
    short lineGap;
    int firstChar;
    int lastChar;
    short capHeight;
    boolean hasCapHeight = false;
    int indexToLocFormat;
    FontTable loca;
    FontTable glyf;
    long postVersion;
    long italicAngle;
    short underlinePosition;
    short underlineThickness;
    int[] advanceWidth;
    int[] unicodeToGID = new int[0x10000];
    java.util.Map<Integer, int[]> markToMarkOffsets;
    java.util.List<java.util.Map<Integer, int[]>> markAnchors;
    java.util.List<java.util.Map<Integer, int[]>> baseAnchors;
    byte[] buf;
    byte[] compressed;
    boolean cff = false;
    int cffOff;
    int cffLen;
    int index = 0;
    int gposWork;

    // The most work the GPOS table of a font is read with, counted in the
    // glyphs of the coverage tables it names and in the pairs its mark
    // lookups place: each mark against each letter or mark it goes on. The
    // lookups, the subtables and the ranges of a coverage table all say their
    // own count, so a table that claims more than a font holds is stopped
    // here rather than read to an end it does not have. Of the 252 fonts
    // PDFjet ships the most this takes is 62,954, in Noto Sans.
    private static final int MAX_GPOS_WORK = 1 << 20;

    private static IOException fontError(String what) {
        return new IOException("Invalid font file: " + what + ".");
    }

    /**
     * Creates OTF object
     *
     * @param stream the input stream
     * @throws Exception if there is a problem
     */
    OTF(InputStream stream) throws Exception {
        buf = Content.getFromStream(stream);

        // Extract OTF metadata
        long version = readUInt32();
        if (version == 0x00010000L ||   // Win OTF
            version == 0x74727565L ||   // Mac TTF
            version == 0x4F54544FL) {   // CFF OTF
            // We should be able to read this font
        } else {
            throw new Exception(
                    "OTF version == " + version + " is not supported.");
        }
        gposWork = MAX_GPOS_WORK;

        int numOfTables   = readUInt16();
        int searchRange   = readUInt16();
        int entrySelector = readUInt16();
        int rangeShift    = readUInt16();

        FontTable cmapTable = null;
        for (int i = 0; i < numOfTables; i++) {
            byte[] name = new byte[4];
            for (int j = 0; j < 4; j++) {
                name[j] = readByte();
            }
            FontTable table = new FontTable();
            table.name     = new String(name, StandardCharsets.UTF_8);
            table.checkSum = readUInt32();
            table.offset = (int) readUInt32();
            table.length = (int) readUInt32();

            int k = index;  // Save the current index
            if      (table.name.equals("head")) { head(table); }
            else if (table.name.equals("hhea")) { hhea(table); }
            else if (table.name.equals("OS/2")) { OS_2(table); }
            else if (table.name.equals("name")) { name(table); }
            else if (table.name.equals("hmtx")) { hmtx(table); }
            else if (table.name.equals("post")) { post(table); }
            else if (table.name.equals("CFF ")) { CFF_(table); }
            else if (table.name.equals("GPOS")) { GPOS(table); }
            else if (table.name.equals("cmap")) { cmapTable = table; }
            else if (table.name.equals("loca")) { loca = table; }
            else if (table.name.equals("glyf")) { glyf = table; }
            index = k;      // Restore the index
        }

        // This table must be processed last
        if (cmapTable == null) {
            throw fontError("no character map");
        }
        cmap(cmapTable);

        // A font without the cap height of a version 2 OS/2 table has the top
        // of its H, as the glyph is drawn, or else its ascent: the cap height
        // goes into the font descriptor, where a reader fits text in a box
        // with it.
        if (!hasCapHeight) {
            capHeight = ascent;
            int top = glyphTop(unicodeToGID['H']);
            if (top != NO_VALUE) {
                capHeight = (short) top;
            }
        }

        // The sizes of the text are divided by the units per em, and the
        // width of every glyph past the advance widths is that of the last one.
        if (unitsPerEm < 16 || unitsPerEm > 16384) {
            throw fontError("the units per em");
        }
        if (advanceWidth == null || advanceWidth.length == 0) {
            throw fontError("no advance widths");
        }
        // The name goes into the PDF as the name of the font, as the name of
        // a stream font does.
        if (fontName == null || !FontStream1.isFontName(fontName.getBytes(StandardCharsets.UTF_8))) {
            throw fontError("the font name");
        }

        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        try (DeflaterOutputStream dos =
                 new DeflaterOutputStream(baos, new Deflater(Deflater.BEST_SPEED))) {
            if (cff) {
                dos.write(buf, cffOff, cffLen);
            } else {
                dos.write(buf);
            }
        }
        compressed = baos.toByteArray();
    }

    private void head(FontTable table) throws IOException {
        index = table.offset + 16;
        int flags = readUInt16();
        unitsPerEm = readUInt16();
        index += 16;
        bBoxLLx = readInt16();
        bBoxLLy = readInt16();
        bBoxURx = readInt16();
        bBoxURy = readInt16();
        indexToLocFormat = Math.max(tableUInt16(table, 50), 0);
    }

    private void hhea(FontTable table) throws IOException {
        index = table.offset + 4;
        ascent  = readInt16();
        descent = readInt16();
        lineGap = readInt16();
        index += 24;
        advanceWidth = new int[readUInt16()];
    }

    private void OS_2(FontTable table) throws IOException {
        index = table.offset + 64;
        firstChar = readUInt16();
        lastChar  = readUInt16();
        // sCapHeight is in the table from its version 2 on. Where it would be
        // in a version 0 or 1 table are the bytes after the table, often those
        // of the next table.
        int version = tableUInt16(table, 0);
        if (version != NO_VALUE && version >= 2) {
            int value = tableUInt16(table, 88);
            if (value != NO_VALUE) {
                capHeight = (short) value;
                hasCapHeight = true;
            }
        }
    }

    // Returns the top of the bounding box of the glyph, the yMax of its header
    // in the glyf table, at the offset the loca table gives it. It returns
    // NO_VALUE for a font with CFF outlines, which has no glyf table, for
    // glyph 0, and for a glyph with no outline, which has no header.
    private int glyphTop(int gid) {
        if (gid == 0 || loca == null || glyf == null) {
            return NO_VALUE;
        }
        long start;
        long end;
        if (indexToLocFormat == 0) {
            // The short offsets are half the offsets.
            start = 2L * tableUInt16(loca, 2*gid);
            end = 2L * tableUInt16(loca, 2*gid + 2);
        } else {
            start = tableUInt32(loca, 4*gid);
            end = tableUInt32(loca, 4*gid + 4);
        }
        // The header of a glyph is 10 bytes: the number of contours, then
        // xMin, yMin, xMax and yMax.
        if (start < 0 || end < 0 || end - start < 10 || start > Integer.MAX_VALUE - 8) {
            return NO_VALUE;
        }
        int yMax = tableUInt16(glyf, (int) start + 8);
        return (yMax == NO_VALUE) ? NO_VALUE : (short) yMax;
    }

    private void name(FontTable table) throws IOException {
        index = table.offset;
        int format = readUInt16();
        int count  = readUInt16();
        int stringOffset = readUInt16();
        StringBuilder macFontInfo = new StringBuilder();
        StringBuilder winFontInfo = new StringBuilder();

        for (int r = 0; r < count; r++) {
            int platformID = readUInt16();
            int encodingID = readUInt16();
            int languageID = readUInt16();
            int nameID = readUInt16();
            int length = readUInt16();
            int offset = readUInt16();

            int start = table.offset + stringOffset + offset;
            if (start < 0 || length > buf.length - start) {
                continue;   // A name record outside the font is left out.
            }
            if (platformID == 1 && encodingID == 0 && languageID == 0) {
                // Macintosh
                String str = UTF8.decode(buf, start, length);
                if (nameID == 6) {
                    fontName = str;
                } else {
                    macFontInfo.append(str);
                    macFontInfo.append('\n');
                }
            } else if (platformID == 3 && encodingID == 1 && languageID == 0x409) {
                // Windows
                String str = new String(buf, start, length, "UTF-16");
                if (nameID == 6) {
                    fontName = new String(str.getBytes(StandardCharsets.UTF_8));
                } else {
                    winFontInfo.append(str);
                    winFontInfo.append('\n');
                }
            }
        }
        // winFontInfo is a StringBuilder reference, so it is never null here -
        // check whether it actually collected any Windows-platform records
        // instead, so Macintosh-only fonts still get their font info.
        fontInfo = (winFontInfo.length() > 0) ? winFontInfo.toString() : macFontInfo.toString();
    }

    private void cmap(FontTable table) throws Exception {
        index = table.offset;
        int tableOffset = index;
        index += 2;
        int numRecords = readUInt16();

        // Process the encoding records
        boolean format4subtable = false;
        int subtableOffset = 0;
        for (int i = 0; i < numRecords; i++) {
            int platformID = readUInt16();
            int encodingID = readUInt16();
            subtableOffset = (int) readUInt32();
            if (platformID == 3 && encodingID == 1) {
                format4subtable = true;
                break;
            }
        }
        if (!format4subtable) {
            throw new Exception("Format 4 subtable not found in this font.");
        }

        index = tableOffset + subtableOffset;

        int format   = readUInt16();
        if (format != 4) {
            throw fontError("the character map is not format 4");
        }
        int tableLen = readUInt16();
        int language = readUInt16();
        int segCount = readUInt16() / 2;

        index += 6; // Skip to the endCount[]
        int[] endCount = new int[segCount];
        for (int i = 0; i < segCount; i++) {
            endCount[i] = readUInt16();
        }

        index += 2; // Skip the reservedPad
        int[] startCount = new int[segCount];
        for (int i = 0; i < segCount; i++) {
            startCount[i] = readUInt16();
        }

        short[] idDelta = new short[segCount];
        for (int i = 0; i < segCount; i++) {
            idDelta[i] = (short) readUInt16();
        }

        int[] idRangeOffset = new int[segCount];
        for (int i = 0; i < segCount; i++) {
            idRangeOffset[i] = readUInt16();
        }

        // The glyph ID array is the rest of the subtable, after its header and
        // the four arrays of the segments. A length that leaves none of it is
        // read as none, not as an array of a negative size.
        int glyphIdLen = (tableLen - (16 + 8*segCount)) / 2;
        if (glyphIdLen < 0) {
            glyphIdLen = 0;
        }
        int[] glyphIdArray = new int[glyphIdLen];
        for (int i = 0; i < glyphIdArray.length; i++) {
            glyphIdArray[i] = readUInt16();
        }

        for (int ch = firstChar; ch <= lastChar; ch++) {
            int seg = getSegmentFor(ch, startCount, endCount, segCount);
            if (seg != -1) {
                int gid;
                int offset = idRangeOffset[seg];
                if (offset == 0) {
                    // Per spec this is unsigned 16-bit modulo arithmetic.
                    // Java's % returns a negative result for a negative
                    // dividend (idDelta can legitimately be negative), so
                    // use & 0xFFFF instead to match the spec exactly.
                    gid = (idDelta[seg] + ch) & 0xFFFF;
                } else {
                    offset /= 2;
                    offset -= segCount - seg;
                    int i = offset + (ch - startCount[seg]);
                    if (i < 0 || i >= glyphIdArray.length) {
                        // The segment points outside the glyph ID array, so it
                        // gives this character no glyph.
                        continue;
                    }
                    gid = glyphIdArray[i];
                    if (gid != 0) {
                        // Same as above: wrap the *sum* to unsigned 16 bits,
                        // not idDelta on its own (which is already in range
                        // and left the actual overflow/negative case
                        // unhandled).
                        gid = (gid + idDelta[seg]) & 0xFFFF;
                    }
                }
                unicodeToGID[ch] = gid;
            }
        }
    }

    private void hmtx(FontTable table) throws IOException {
        index = table.offset;
        for (int j = 0; j < advanceWidth.length; j++) {
            advanceWidth[j] = readUInt16();
            index += 2;
        }
    }

    private void post(FontTable table) throws IOException {
        index = table.offset;
        postVersion = readUInt32();
        italicAngle = readUInt32();
        underlinePosition  = (short) readUInt16();
        underlineThickness = (short) readUInt16();
    }

    private void CFF_(FontTable table) throws IOException {
        if (table.offset < 0 || table.length < 0 || table.length > buf.length - table.offset) {
            throw fontError("the CFF table is not in the font");
        }
        this.cff = true;
        this.cffOff = table.offset;
        this.cffLen = table.length;
    }

    // Reads where the marks go from the GPOS table: the marks on letters and
    // ligatures, like Hebrew and Arabic vowel marks, from its MarkToBase and
    // MarkToLigature lookups, and the marks that attach to other marks, like a
    // Thai tone mark above an upper vowel, from its MarkToMark lookups.
    private void GPOS(FontTable table) {
        markToMarkOffsets = new java.util.HashMap<Integer, int[]>();
        markAnchors = new java.util.ArrayList<java.util.Map<Integer, int[]>>();
        baseAnchors = new java.util.ArrayList<java.util.Map<Integer, int[]>>();
        int lookupList = table.offset + getUInt16(table.offset + 8);
        int lookupCount = getUInt16(lookupList);
        for (int i = 0; i < lookupCount && gposWork > 0; i++) {
            int lookup = lookupList + getUInt16(lookupList + 2 + 2*i);
            int lookupType = getUInt16(lookup);
            int subTableCount = getUInt16(lookup + 4);
            for (int j = 0; j < subTableCount && gposWork > 0; j++) {
                gposWork--;
                int subTable = lookup + getUInt16(lookup + 6 + 2*j);
                int type = lookupType;
                if (type == 9) {    // An extension lookup holds the subtable
                    type = getUInt16(subTable + 2);
                    subTable += getUInt32(subTable + 4);
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
        java.util.Map<Integer, int[]> marks = new java.util.HashMap<Integer, int[]>();
        for (int m = 0; m < markGlyphs.length; m++) {
            int anchor = markArray + getUInt16(markArray + 4 + 4*m);
            marks.put(markGlyphs[m], new int[] {
                    getUInt16(markArray + 2 + 4*m), getInt16(anchor + 2), getInt16(anchor + 4)});
        }
        java.util.Map<Integer, int[]> bases = new java.util.HashMap<Integer, int[]>();
        for (int b = 0; b < baseGlyphs.length; b++) {
            if (gposWork < classCount) {
                break;
            }
            gposWork -= classCount;
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
            if (gposWork < mark2Glyphs.length) {
                break;
            }
            gposWork -= mark2Glyphs.length;
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
        // A coverage table lists each glyph once, and a glyph ID is 16 bits.
        if (size > 0x10000) {
            size = 0x10000;
        }
        int[] glyphs = new int[size];
        for (int i = 0; i < count && gposWork > 0; i++) {
            int range = offset + 4 + 6*i;
            int start = getUInt16(range);
            int end = getUInt16(range + 2);
            int coverageIndex = getUInt16(range + 4);
            for (int glyph = start; glyph <= end && gposWork > 0; glyph++) {
                gposWork--;
                int index = coverageIndex + glyph - start;
                if (index < size) {
                    glyphs[index] = glyph;
                }
            }
        }
        return glyphs;
    }

    // The GPOS table is read at offsets from its subtables. A value outside
    // the font data is read as 0, so a broken table cannot stop the font from
    // loading.
    private int getUInt16(int offset) {
        if (offset < 0 || offset + 2 > buf.length) {
            return 0;
        }
        return ((buf[offset] & 0xFF) << 8) | (buf[offset + 1] & 0xFF);
    }

    // What tableUInt16 and tableUInt32 return for a value that is not there.
    private static final int NO_VALUE = Integer.MIN_VALUE;

    // Returns the 16 bits at the offset in the table, or NO_VALUE when the
    // table or the font ends before them: a table can be shorter than its
    // version says, and the directory can point past the end of the font.
    private int tableUInt16(FontTable table, int at) {
        if (at < 0 || table.offset < 0 || table.offset > buf.length || table.length < 0 ||
                at > table.length - 2 || at > buf.length - table.offset - 2) {
            return NO_VALUE;
        }
        return getUInt16(table.offset + at);
    }

    // Returns the 32 bits at the offset in the table, as tableUInt16 returns
    // 16, and -1 when they are not there.
    private long tableUInt32(FontTable table, int at) {
        int high = tableUInt16(table, at);
        int low = tableUInt16(table, at + 2);
        if (high == NO_VALUE || low == NO_VALUE) {
            return -1;
        }
        return ((long) high << 16) | low;
    }

    private int getInt16(int offset) {
        return (short) getUInt16(offset);
    }

    private int getUInt32(int offset) {
        return (getUInt16(offset) << 16) | getUInt16(offset + 2);
    }

    private int getSegmentFor(
            int ch, int[] startCount, int[] endCount, int segCount) {
        int segment = -1;
        for (int i = 0; i < segCount; i++) {
            if (ch <= endCount[i] && ch >= startCount[i]) {
                segment = i;
                break;
            }
        }
        return segment;
    }

    // Panics unless the next count bytes of the font are there. The index runs
    // past the end when a table of the directory points outside the font.
    private void need(int count) throws IOException {
        if (index < 0 || count > buf.length - index) {
            throw fontError("the font ends too soon");
        }
    }

    private byte readByte() throws IOException {
        need(1);
        return buf[index++];
    }

    private short readInt16() throws IOException {
        need(2);
        int val = 0;
        val |= (buf[index++] <<  8) & 0xFF00;
        val |= (buf[index++])       & 0x00FF;
        return (short) val;
    }

    private int readUInt16() throws IOException {
        need(2);
        int val = 0;
        val |= (buf[index++] <<  8) & 0x0000FF00;
        val |= (buf[index++])       & 0x000000FF;
        return val;
    }

    private long readUInt32() throws IOException {
        need(4);
        long val = 0L;
        val |= (buf[index++] << 24) & 0xFF000000L;
        val |= (buf[index++] << 16) & 0x00FF0000L;
        val |= (buf[index++] <<  8) & 0x0000FF00L;
        val |= (buf[index++])       & 0x000000FFL;
        return val;
    }
}   // End of OTF.java

class FontTable {
    protected String name;
    protected long checkSum;
    protected int offset;
    protected int length;
}
