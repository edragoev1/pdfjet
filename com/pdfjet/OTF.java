/*
 * OTF.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;

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
    GlyphMap unicodeToGID = new GlyphMap();
    FontTable gpos;     // Read when a mark is first drawn, by GPOS
    byte[] buf;
    boolean cff = false;
    int fsType;         // Of the OS/2 table: what the license allows
    int cffOff;
    int cffLen;
    int index = 0;
    int numGlyphs;  // Of the maxp table, or 0 when the font has none

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
        // A font without an OS/2 table does not say which characters it
        // holds, so its character map is read for all of them.
        firstChar = 0;
        lastChar = 0xFFFF;

        // Extract OTF metadata
        long version = readUInt32();
        if (version == 0x00010000L ||   // Win OTF
            version == 0x74727565L ||   // Mac TTF
            version == 0x4F54544FL) {   // CFF OTF
            // We should be able to read this font
        } else {
            throw fontError("not an OpenType or TrueType font: PDFjet reads .otf and .ttf fonts");
        }

        int numOfTables   = readUInt16();
        readUInt16();  // searchRange
        readUInt16();  // entrySelector
        readUInt16();  // rangeShift

        FontTable cmapTable = null;
        FontTable hmtxTable = null;
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
            else if (table.name.equals("hmtx")) { hmtxTable = table; }
            else if (table.name.equals("post")) { post(table); }
            else if (table.name.equals("CFF ")) { CFF_(table); }
            else if (table.name.equals("GPOS")) { gpos = table; }
            else if (table.name.equals("maxp")) { numGlyphs = Math.max(tableUInt16(table, 4), 0); }
            else if (table.name.equals("cmap")) { cmapTable = table; }
            else if (table.name.equals("loca")) { loca = table; }
            else if (table.name.equals("glyf")) { glyf = table; }
            index = k;      // Restore the index
        }

        // The hmtx table is read after the hhea table, which says how many
        // advance widths it has, whatever their order in the directory: listed
        // first, its widths had no array to go into.
        if (hmtxTable != null && advanceWidth != null) {
            hmtx(hmtxTable);
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
            int top = glyphTop(unicodeToGID.get('H'));
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
        // The name goes into the PDF as the name of the font.
        if (fontName == null || !FontWriter.isFontName(fontName.getBytes(StandardCharsets.UTF_8))) {
            throw fontError("the font name");
        }
    }

    private void head(FontTable table) throws IOException {
        index = table.offset + 16;
        readUInt16();  // flags
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

        fsType = Math.max(tableUInt16(table, 8), 0);
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
        readUInt16();  // format
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

        // Process the encoding records: the format 4 subtable of the Windows
        // platform, and its format 12 subtable, which maps the characters past
        // the Basic Multilingual Plane too.
        boolean format4subtable = false;
        int subtableOffset = 0;
        int format12Offset = -1;
        for (int i = 0; i < numRecords; i++) {
            int platformID = readUInt16();
            int encodingID = readUInt16();
            int offset = (int) readUInt32();
            if (platformID == 3 && encodingID == 1 && !format4subtable) {
                format4subtable = true;
                subtableOffset = offset;
            } else if (platformID == 3 && encodingID == 10 && format12Offset == -1) {
                format12Offset = offset;
            }
        }
        if (!format4subtable && format12Offset == -1) {
            throw new Exception("Format 4 subtable not found in this font.");
        }
        if (format4subtable) {
            cmapFormat4(tableOffset + subtableOffset);
        }
        if (format12Offset != -1) {
            cmapFormat12(table, format12Offset);
        }
    }

    // Maps the characters of the Basic Multilingual Plane that the format 4
    // subtable left without a glyph, from the format 12 subtable at the offset
    // in the table. IBM Plex Sans TC, as a .ttf, has an empty format 4
    // subtable and maps every character in its format 12 subtable. The groups
    // go up and do not overlap, and a group that goes back is passed over, so
    // that the map is read once at most.
    private void cmapFormat12(FontTable table, int offset) {
        if (tableUInt16(table, offset) != 12) {
            return;
        }
        long numGroups = tableUInt32(table, offset + 12);
        if (numGroups < 0 || numGroups > (table.length - offset - 16) / 12) {
            return;
        }
        long next = firstChar;
        for (int i = 0; i < numGroups; i++) {
            int group = offset + 16 + 12 * i;
            long first = tableUInt32(table, group);
            long end = tableUInt32(table, group + 4);
            long startGlyph = tableUInt32(table, group + 8);
            if (first == -1 || end == -1 || startGlyph == -1) {
                continue;   // Past the end of the font
            }
            // The glyphs are counted from the group's first character, even
            // where the characters before next are passed over (the review of
            // 9 October 2026: they were counted from next)
            long start = Math.max(first, next);
            if (end > lastChar) {
                end = lastChar;
            }
            for (long ch = start; ch <= end; ch++) {
                long gid = startGlyph + (ch - first);
                if (gid < 0x10000 && unicodeToGID.get((int) ch) == 0) {
                    unicodeToGID.set((int) ch, (int) gid);
                }
            }
            if (end + 1 > next) {
                next = end + 1;
            }
        }
    }

    // Reads the format 4 subtable that begins at subtable.
    private void cmapFormat4(int subtable) throws Exception {
        index = subtable;

        int format   = readUInt16();
        if (format != 4) {
            throw fontError("the character map is not format 4");
        }
        int tableLen = readUInt16();
        readUInt16();  // language
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

        // The segments are in the order of their end codes, so the segment of a
        // character is the first that ends at it or after it, if it starts at
        // it or before it. The characters go up, and so does the segment.
        int seg = 0;
        for (int ch = firstChar; ch <= lastChar; ch++) {
            while (seg < segCount && endCount[seg] < ch) {
                seg++;
            }
            if (seg == segCount) {
                break;
            }
            if (startCount[seg] <= ch) {
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
                unicodeToGID.set(ch, gid);
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

    // Returns the 16 bits at the offset, or 0 outside the font data.
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
