/*
 * OTF.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>Parses and extracts the data from TTF and OTF font files.</summary>
internal class OTF {
    internal String fontName;
    internal String fontInfo;
    internal readonly byte[] buf;
    internal int unitsPerEm;
    internal short bBoxLLx;
    internal short bBoxLLy;
    internal short bBoxURx;
    internal short bBoxURy;
    internal short ascent;
    internal short descent;
    internal short lineGap;
    internal int firstChar;
    internal int lastChar;
    internal int capHeight;
    internal bool hasCapHeight = false;
    internal int indexToLocFormat;
    internal FontTable loca;
    internal FontTable glyf;
    internal long postVersion;
    internal long italicAngle;
    internal short underlinePosition;
    internal short underlineThickness;
    internal int[] advanceWidth;
    internal readonly GlyphMap unicodeToGID = new GlyphMap();
    internal FontTable gpos;   // Read when a mark is first drawn, by GPOS
    internal bool cff = false;

    internal int fsType;           // Of the OS/2 table: what the license allows
    internal int cffOff;
    internal int cffLen;
    private int index = 0;
    internal int numGlyphs; // Of the maxp table, or 0 when the font has none

    private static Exception FontError(String what) {
        return new InvalidDataException("Invalid font file: " + what + ".");
    }

    /// <summary>Parses the font read from the stream.</summary>
    public OTF(Stream stream) {
        buf = Content.GetFromStream(stream);
        // A font without an OS/2 table does not say which characters it
        // holds, so its character map is read for all of them.
        firstChar = 0;
        lastChar = 0xFFFF;

        // Extract OTF metadata
        long version = ReadUInt32();
        if (version == 0x00010000L ||   // Win OTF
            version == 0x74727565L ||   // Mac TTF
            version == 0x4F54544FL) {   // CFF OTF
            // We should be able to read this font
        } else {
            throw FontError("not an OpenType or TrueType font: PDFjet reads .otf and .ttf fonts");
        }

        int numOfTables   = ReadUInt16();
        ReadUInt16();  // searchRange
        ReadUInt16();  // entrySelector
        ReadUInt16();  // rangeShift

        FontTable cmapTable = null;
        FontTable hmtxTable = null;
        for (int i = 0; i < numOfTables; i++) {
            char[] name = new char[4];
            for (int j = 0; j < 4; j++) {
                name[j] = (char) ReadByte();
            }
            FontTable table = new FontTable();
            table.name     = new String(name);
            table.checkSum = ReadUInt32();
            table.offset = (int) ReadUInt32();
            table.length = (int) ReadUInt32();

            int k = index;  // Save the current index
            if      (table.name.Equals("head")) { Head(table); }
            else if (table.name.Equals("hhea")) { Hhea(table); }
            else if (table.name.Equals("OS/2")) { OS_2(table); }
            else if (table.name.Equals("name")) { Name(table); }
            else if (table.name.Equals("hmtx")) { hmtxTable = table; }
            else if (table.name.Equals("post")) { Post(table); }
            else if (table.name.Equals("CFF ")) { CFF_(table); }
            else if (table.name.Equals("GPOS")) { gpos = table; }
            else if (table.name.Equals("maxp")) { numGlyphs = Math.Max(TableUInt16(table, 4), 0); }
            else if (table.name.Equals("cmap")) { cmapTable = table; }
            else if (table.name.Equals("loca")) { loca = table; }
            else if (table.name.Equals("glyf")) { glyf = table; }
            index = k;      // Restore the index
        }

        // The hmtx table is read after the hhea table, which says how many
        // advance widths it has, whatever their order in the directory: listed
        // first, its widths had no array to go into.
        if (hmtxTable != null && advanceWidth != null) {
            Hmtx(hmtxTable);
        }

        // This table must be processed last
        if (cmapTable == null) {
            throw FontError("no character map");
        }
        Cmap(cmapTable);

        // A font without the cap height of a version 2 OS/2 table has the top
        // of its H, as the glyph is drawn, or else its ascent: the cap height
        // goes into the font descriptor, where a reader fits text in a box
        // with it.
        if (!hasCapHeight) {
            capHeight = ascent;
            int top = GlyphTop(unicodeToGID.Get('H'));
            if (top != NO_VALUE) {
                capHeight = top;
            }
        }

        // The sizes of the text are divided by the units per em, and the
        // width of every glyph past the advance widths is that of the last one.
        if (unitsPerEm < 16 || unitsPerEm > 16384) {
            throw FontError("the units per em");
        }
        if (advanceWidth == null || advanceWidth.Length == 0) {
            throw FontError("no advance widths");
        }
        // The name goes into the PDF as the name of the font.
        if (fontName == null || !FontWriter.IsFontName(Encoding.UTF8.GetBytes(fontName))) {
            throw FontError("the font name");
        }
    }

    private void Head(FontTable table) {
        index = table.offset + 16;
        ReadUInt16();  // flags
        unitsPerEm = (int) ReadUInt16();
        index += 16;
        bBoxLLx = (short) ReadUInt16();
        bBoxLLy = (short) ReadUInt16();
        bBoxURx = (short) ReadUInt16();
        bBoxURy = (short) ReadUInt16();
        indexToLocFormat = Math.Max(TableUInt16(table, 50), 0);
    }

    private void Hhea(FontTable table) {
        index = table.offset + 4;
        ascent  = (short) ReadUInt16();
        descent = (short) ReadUInt16();
        lineGap = (short) ReadUInt16();
        index += 24;
        advanceWidth = new int[(int) ReadUInt16()];
    }

    private void OS_2(FontTable table) {

        fsType = Math.Max(TableUInt16(table, 8), 0);
        index = table.offset + 64;
        firstChar = ReadUInt16();
        lastChar  = ReadUInt16();
        // sCapHeight is in the table from its version 2 on. Where it would be
        // in a version 0 or 1 table are the bytes after the table, often those
        // of the next table.
        int version = TableUInt16(table, 0);
        if (version != NO_VALUE && version >= 2) {
            int value = TableUInt16(table, 88);
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
    private int GlyphTop(int gid) {
        if (gid == 0 || loca == null || glyf == null) {
            return NO_VALUE;
        }
        long start;
        long end;
        if (indexToLocFormat == 0) {
            // The short offsets are half the offsets.
            start = 2L * TableUInt16(loca, 2*gid);
            end = 2L * TableUInt16(loca, 2*gid + 2);
        } else {
            start = TableUInt32(loca, 4*gid);
            end = TableUInt32(loca, 4*gid + 4);
        }
        // The header of a glyph is 10 bytes: the number of contours, then
        // xMin, yMin, xMax and yMax.
        if (start < 0 || end < 0 || end - start < 10 || start > int.MaxValue - 8) {
            return NO_VALUE;
        }
        int yMax = TableUInt16(glyf, (int) start + 8);
        return (yMax == NO_VALUE) ? NO_VALUE : (short) yMax;
    }

    // The most of the name records' bytes kept as the font's information, of
    // each platform: a font of 593 KB whose 40,000 records name one string of
    // 65,000 bytes was gigabytes of text, and a smaller one put 65 million
    // characters in the XMP of every PDF that used it (review of 17 October
    // 2026). A font's copyright and license are a few KB.
    internal const int MAX_FONT_INFO_BYTES = 65536;

    private void Name(FontTable table) {
        index = table.offset;
        ReadUInt16();  // format
        int count  = ReadUInt16();
        int stringOffset = ReadUInt16();
        StringBuilder macFontInfo = new StringBuilder();
        StringBuilder winFontInfo = new StringBuilder();
        // The bytes of the records of each, up to MAX_FONT_INFO_BYTES
        int macBytes = 0;
        int winBytes = 0;

        for (int r = 0; r < count; r++) {
            int platformID = ReadUInt16();
            int encodingID = ReadUInt16();
            int languageID = ReadUInt16();
            int nameID = ReadUInt16();
            int length = ReadUInt16();
            int offset = ReadUInt16();

            int start = table.offset + stringOffset + offset;
            if (start < 0 || length > buf.Length - start) {
                continue;   // A name record outside the font is left out.
            }
            if (platformID == 1 && encodingID == 0 && languageID == 0) {
                // Macintosh
                String str = Encoding.UTF8.GetString(buf, start, length);
                if (nameID == 6) {
                    fontName = str;
                } else if (macBytes + length <= MAX_FONT_INFO_BYTES) {
                    macBytes += length;
                    macFontInfo.Append(str);
                    macFontInfo.Append('\n');
                }
            } else if (platformID == 3 && encodingID == 1 && languageID == 0x409) {
                // Windows
                String str = Encoding.BigEndianUnicode.GetString(buf, start, length);
                if (nameID == 6) {
                    fontName = str;
                } else if (winBytes + length <= MAX_FONT_INFO_BYTES) {
                    winBytes += length;
                    winFontInfo.Append(str);
                    winFontInfo.Append('\n');
                }
            }
        }
        // winFontInfo is a StringBuilder reference, so it is never null here -
        // check whether it actually collected any Windows-platform records
        // instead, so Macintosh-only fonts still get their font info.
        fontInfo = (winFontInfo.Length > 0) ? winFontInfo.ToString() : macFontInfo.ToString();
    }

    private void Cmap(FontTable table) {
        index = table.offset;
        int tableOffset = index;
        index += 2;
        int numRecords = ReadUInt16();

        // Process the encoding records: the format 4 subtable of the Windows
        // platform, and its format 12 subtable, which maps the characters past
        // the Basic Multilingual Plane too.
        bool format4subtable = false;
        int subtableOffset = 0;
        int format12Offset = -1;
        for (int i = 0; i < numRecords; i++) {
            int platformID = ReadUInt16();
            int encodingID = ReadUInt16();
            int offset = (int) ReadUInt32();
            if (platformID == 3 && encodingID == 1 && !format4subtable) {
                format4subtable = true;
                subtableOffset = offset;
            } else if (platformID == 3 && encodingID == 10 && format12Offset == -1) {
                format12Offset = offset;
            }
        }
        if (!format4subtable && format12Offset == -1) {
            throw new NotSupportedException("Format 4 subtable not found in this font.");
        }
        if (format4subtable) {
            CmapFormat4(tableOffset + subtableOffset);
        }
        if (format12Offset != -1) {
            CmapFormat12(table, format12Offset);
        }
    }

    // Maps the characters of the Basic Multilingual Plane that the format 4
    // subtable left without a glyph, from the format 12 subtable at the offset
    // in the table. IBM Plex Sans TC, as a .ttf, has an empty format 4
    // subtable and maps every character in its format 12 subtable. The groups
    // go up and do not overlap, and a group that goes back is passed over, so
    // that the map is read once at most.
    private void CmapFormat12(FontTable table, int offset) {
        if (TableUInt16(table, offset) != 12) {
            return;
        }
        long numGroups = TableUInt32(table, offset + 12);
        if (numGroups < 0 || numGroups > (table.length - offset - 16) / 12) {
            return;
        }
        long next = firstChar;
        for (int i = 0; i < numGroups; i++) {
            int group = offset + 16 + 12 * i;
            long first = TableUInt32(table, group);
            long end = TableUInt32(table, group + 4);
            long startGlyph = TableUInt32(table, group + 8);
            if (first < 0 || end < 0 || startGlyph < 0) {
                continue;   // Past the end of the font (the review of 9 October 2026: a glyph of -1)
            }
            // The glyphs are counted from the group's first character, even
            // where the characters before next are passed over (the review of
            // 9 October 2026: they were counted from next)
            long start = Math.Max(first, next);
            if (end > lastChar) {
                end = lastChar;
            }
            for (long ch = start; ch <= end; ch++) {
                long gid = startGlyph + (ch - first);
                if (gid < 0x10000) {
                    unicodeToGID.SetIfAbsent((int) ch, (int) gid);
                }
            }
            if (end + 1 > next) {
                next = end + 1;
            }
        }
    }

    // Reads the format 4 subtable that begins at subtable.
    private void CmapFormat4(int subtable) {
        index = subtable;

        int format   = ReadUInt16();
        if (format != 4) {
            throw FontError("the character map is not format 4");
        }
        int tableLen = ReadUInt16();
        ReadUInt16();  // language
        int segCount = ReadUInt16() / 2;

        index += 6; // Skip to the endCount[]
        int[] endCount = new int[segCount];
        for (int i = 0; i < segCount; i++) {
            endCount[i] = ReadUInt16();
        }

        index += 2; // Skip the reservedPad
        int[] startCount = new int[segCount];
        for (int i = 0; i < segCount; i++) {
            startCount[i] = ReadUInt16();
        }

        short[] idDelta = new short[segCount];
        for (int i = 0; i < segCount; i++) {
            idDelta[i] = (short) ReadUInt16();
        }

        int[] idRangeOffset = new int[segCount];
        for (int i = 0; i < segCount; i++) {
            idRangeOffset[i] = ReadUInt16();
        }

        // The glyph ID array is the rest of the subtable, after its header and
        // the four arrays of the segments. A length that leaves none of it is
        // read as none, not as an array of a negative size.
        int glyphIdLen = (tableLen - (16 + 8*segCount)) / 2;
        if (glyphIdLen < 0) {
            glyphIdLen = 0;
        }
        int[] glyphIdArray = new int[glyphIdLen];
        for (int i = 0; i < glyphIdArray.Length; i++) {
            glyphIdArray[i] = ReadUInt16();
        }

        // The segments are in the order of their end codes, so the segment of a
        // character is the first that ends at it or after it, if it starts at
        // it or before it. The characters go up, and so does the segment: each
        // segment has the characters after those of the segments before it,
        // up to its end, and the characters between segments are passed over,
        // not looked at one by one.
        int next = firstChar;   // The first character no segment has had yet
        for (int seg = 0; seg < segCount && next <= lastChar; seg++) {
            if (endCount[seg] < next) {
                continue;
            }
            int end = Math.Min(endCount[seg], lastChar);
            for (int ch = Math.Max(next, startCount[seg]); ch <= end; ch++) {
                int gid;
                int offset = idRangeOffset[seg];
                if (offset == 0) {
                    // Per spec this is unsigned 16-bit modulo arithmetic.
                    // C#'s % returns a negative result for a negative
                    // dividend (idDelta can legitimately be negative), so
                    // use & 0xFFFF instead to match the spec exactly.
                    gid = (idDelta[seg] + ch) & 0xFFFF;
                } else {
                    offset /= 2;
                    offset -= segCount - seg;
                    int i = offset + (ch - startCount[seg]);
                    if (i < 0 || i >= glyphIdArray.Length) {
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
                unicodeToGID.Set(ch, gid);
            }
            next = endCount[seg] + 1;
        }
    }

    private void Hmtx(FontTable table) {
        index = table.offset;
        for (int j = 0; j < advanceWidth.Length; j++) {
            advanceWidth[j] = ReadUInt16();
            index += 2;
        }
    }

    private void Post(FontTable table) {
        index = table.offset;
        postVersion = ReadUInt32();
        italicAngle = ReadUInt32();
        underlinePosition  = (short) ReadUInt16();
        underlineThickness = (short) ReadUInt16();
    }

    private void CFF_(FontTable table) {
        if (table.offset < 0 || table.length < 0 || table.length > buf.Length - table.offset) {
            throw FontError("the CFF table is not in the font");
        }
        this.cff = true;
        this.cffOff = table.offset;
        this.cffLen = table.length;
    }

    // Returns the 16 bits at the offset, or 0 outside the font data.
    private int GetUInt16(int offset) {
        if (offset < 0 || offset + 2 > buf.Length) {
            return 0;
        }
        return (buf[offset] << 8) | buf[offset + 1];
    }

    // What TableUInt16 and TableUInt32 return for a value that is not there.
    private const int NO_VALUE = int.MinValue;

    // Returns the 16 bits at the offset in the table, or NO_VALUE when the
    // table or the font ends before them: a table can be shorter than its
    // version says, and the directory can point past the end of the font.
    private int TableUInt16(FontTable table, int at) {
        if (at < 0 || table.offset < 0 || table.offset > buf.Length || table.length < 0 ||
                at > table.length - 2 || at > buf.Length - table.offset - 2) {
            return NO_VALUE;
        }
        return GetUInt16(table.offset + at);
    }

    // Returns the 32 bits at the offset in the table, as TableUInt16 returns
    // 16, and -1 when they are not there.
    private long TableUInt32(FontTable table, int at) {
        int high = TableUInt16(table, at);
        int low = TableUInt16(table, at + 2);
        if (high == NO_VALUE || low == NO_VALUE) {
            return -1;
        }
        return ((long) high << 16) | (long) low;
    }

    // Throws unless the next count bytes of the font are there. The index runs
    // past the end when a table of the directory points outside the font.
    private void Need(int count) {
        if (index < 0 || count > buf.Length - index) {
            throw FontError("the font ends too soon");
        }
    }

    private byte ReadByte() {
        Need(1);
        return buf[index++];
    }

    private UInt16 ReadUInt16() {
        Need(2);
        UInt32 val = 0;
        val |= ((UInt32) buf[index++]) << 8;
        val |= ((UInt32) buf[index++]);
        return (UInt16) val;
    }

    private UInt32 ReadUInt32() {
        Need(4);
        UInt32 val = 0;
        val |= ((UInt32) buf[index++]) << 24;
        val |= ((UInt32) buf[index++]) << 16;
        val |= ((UInt32) buf[index++]) <<  8;
        val |= ((UInt32) buf[index++]);
        return val;
    }
}   // End of OTF.cs
}   // End of namespace PDFjet.NET

class FontTable {
    internal String name;
    internal UInt32 checkSum;
    internal int offset;
    internal int length;
}
