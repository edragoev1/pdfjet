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
public class OTF {
    String fontName;
    String fontInfo;
    int unitsPerEm;
    short bBoxLLx;
    short bBoxLLy;
    short bBoxURx;
    short bBoxURy;
    short ascent;
    short descent;
    int firstChar;
    int lastChar;
    short capHeight;
    long postVersion;
    long italicAngle;
    short underlinePosition;
    short underlineThickness;
    int[] advanceWidth;
    int[] unicodeToGID = new int[0x10000];
    java.util.Map<Integer, int[]> markToMarkOffsets;
    byte[] buf;
    byte[] compressed;
    boolean cff = false;
    int cffOff;
    int cffLen;
    int index = 0;

    /**
     * Creates OTF object
     *
     * @param stream the input stream
     * @throws Exception if there is a problem
     */
    public OTF(InputStream stream) throws Exception {
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
            table.name     = new String(name);
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
            index = k;      // Restore the index
        }

        // This table must be processed last
        cmap(cmapTable);

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

    private void head(FontTable table) {
        index = table.offset + 16;
        int flags = readUInt16();
        unitsPerEm = readUInt16();
        index += 16;
        bBoxLLx = readInt16();
        bBoxLLy = readInt16();
        bBoxURx = readInt16();
        bBoxURy = readInt16();
    }

    private void hhea(FontTable table) {
        index = table.offset + 4;
        ascent  = readInt16();
        descent = readInt16();
        index += 26;
        advanceWidth = new int[readUInt16()];
    }

    private void OS_2(FontTable table) {
        index = table.offset + 64;
        firstChar = readUInt16();
        lastChar  = readUInt16();
        index += 20;
        capHeight = readInt16();
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

            if (platformID == 1 && encodingID == 0 && languageID == 0) {
                // Macintosh
                String str = new String(
                        buf, table.offset + stringOffset + offset, length, "UTF-8");
                if (nameID == 6) {
                    fontName = str;
                } else {
                    macFontInfo.append(str);
                    macFontInfo.append('\n');
                }
            } else if (platformID == 3 && encodingID == 1 && languageID == 0x409) {
                // Windows
                String str = new String(
                        buf, table.offset + stringOffset + offset, length, "UTF-16");
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

        int[] glyphIdArray = new int[(tableLen - (16 + 8*segCount)) / 2];
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
                    gid = glyphIdArray[offset + (ch - startCount[seg])];
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

    private void hmtx(FontTable table) {
        index = table.offset;
        for (int j = 0; j < advanceWidth.length; j++) {
            advanceWidth[j] = readUInt16();
            index += 2;
        }
    }

    private void post(FontTable table) {
        index = table.offset;
        postVersion = readUInt32();
        italicAngle = readUInt32();
        underlinePosition  = (short) readUInt16();
        underlineThickness = (short) readUInt16();
    }

    private void CFF_(FontTable table) {
        this.cff = true;
        this.cffOff = table.offset;
        this.cffLen = table.length;
    }

    // Reads where the marks that attach to other marks go, like a Thai tone
    // mark above an upper vowel, from the MarkToMark lookups of the GPOS table.
    private void GPOS(FontTable table) {
        markToMarkOffsets = new java.util.HashMap<Integer, int[]>();
        int lookupList = table.offset + getUInt16(table.offset + 8);
        int lookupCount = getUInt16(lookupList);
        for (int i = 0; i < lookupCount; i++) {
            int lookup = lookupList + getUInt16(lookupList + 2 + 2*i);
            int lookupType = getUInt16(lookup);
            int subTableCount = getUInt16(lookup + 4);
            for (int j = 0; j < subTableCount; j++) {
                int subTable = lookup + getUInt16(lookup + 6 + 2*j);
                int type = lookupType;
                if (type == 9) {    // An extension lookup holds the subtable
                    type = getUInt16(subTable + 2);
                    subTable += getUInt32(subTable + 4);
                }
                if (type == 6 && getUInt16(subTable) == 1) {
                    markToMark(subTable);
                }
            }
        }
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
        int[] glyphs = new int[size];
        for (int i = 0; i < count; i++) {
            int range = offset + 4 + 6*i;
            int start = getUInt16(range);
            int end = getUInt16(range + 2);
            int coverageIndex = getUInt16(range + 4);
            for (int glyph = start; glyph <= end; glyph++) {
                glyphs[coverageIndex + glyph - start] = glyph;
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

    private byte readByte() {
        return buf[index++];
    }

    private short readInt16() {
        int val = 0;
        val |= (buf[index++] <<  8) & 0xFF00;
        val |= (buf[index++])       & 0x00FF;
        return (short) val;
    }

    private int readUInt16() {
        int val = 0;
        val |= (buf[index++] <<  8) & 0x0000FF00;
        val |= (buf[index++])       & 0x000000FF;
        return val;
    }

    private long readUInt32() {
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
