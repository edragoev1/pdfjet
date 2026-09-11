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
public class OTF {
    internal String fontName;
    internal String fontInfo;
    internal readonly byte[] buf;
    internal readonly byte[] compressed;
    internal int unitsPerEm;
    internal short bBoxLLx;
    internal short bBoxLLy;
    internal short bBoxURx;
    internal short bBoxURy;
    internal short ascent;
    internal short descent;
    internal int firstChar;
    internal int lastChar;
    internal int capHeight;
    internal long postVersion;
    internal long italicAngle;
    internal short underlinePosition;
    internal short underlineThickness;
    internal int[] advanceWidth;
    internal readonly int[] unicodeToGID = new int[0x10000];
    internal System.Collections.Generic.Dictionary<int, int[]> markToMarkOffsets;
    internal System.Collections.Generic.List<System.Collections.Generic.Dictionary<int, int[]>> markAnchors;
    internal System.Collections.Generic.List<System.Collections.Generic.Dictionary<int, int[]>> baseAnchors;
    internal bool cff = false;

    private int cffOff;
    private int cffLen;
    private int index = 0;

    /// <summary>Parses the font read from the stream.</summary>
    public OTF(Stream stream) {
        buf = Content.GetFromStream(stream);

        // Extract OTF metadata
        long version = ReadUInt32();
        if (version == 0x00010000L ||   // Win OTF
            version == 0x74727565L ||   // Mac TTF
            version == 0x4F54544FL) {   // CFF OTF
            // We should be able to read this font
        } else {
            throw new Exception(
                    "OTF version == " + version + " is not supported.");
        }

        int numOfTables   = ReadUInt16();
        int searchRange   = ReadUInt16();
        int entrySelector = ReadUInt16();
        int rangeShift    = ReadUInt16();

        FontTable cmapTable = null;
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
            else if (table.name.Equals("hmtx")) { Hmtx(table); }
            else if (table.name.Equals("post")) { Post(table); }
            else if (table.name.Equals("CFF ")) { CFF_(table); }
            else if (table.name.Equals("GPOS")) { GPOS(table); }
            else if (table.name.Equals("cmap")) { cmapTable = table; }
            index = k;      // Restore the index
        }

        // This table must be processed last
        Cmap(cmapTable);

        if (cff) {
            compressed = Compressor.Deflate(buf, cffOff, cffLen);
        } else {
            compressed = Compressor.Deflate(buf);
        }
    }

    private void Head(FontTable table) {
        index = table.offset + 16;
        int flags  = (int) ReadUInt16();
        unitsPerEm = (int) ReadUInt16();
        index += 16;
        bBoxLLx = (short) ReadUInt16();
        bBoxLLy = (short) ReadUInt16();
        bBoxURx = (short) ReadUInt16();
        bBoxURy = (short) ReadUInt16();
    }

    private void Hhea(FontTable table) {
        index = table.offset + 4;
        ascent  = (short) ReadUInt16();
        descent = (short) ReadUInt16();
        index += 26;
        advanceWidth = new int[(int) ReadUInt16()];
    }

    private void OS_2(FontTable table) {
        index = table.offset + 64;
        firstChar = ReadUInt16();
        lastChar  = ReadUInt16();
        index += 20;
        capHeight = ReadUInt16();
    }

    private void Name(FontTable table) {
        index = table.offset;
        int format = ReadUInt16();
        int count  = ReadUInt16();
        int stringOffset = ReadUInt16();
        StringBuilder macFontInfo = new StringBuilder();
        StringBuilder winFontInfo = new StringBuilder();

        for (int r = 0; r < count; r++) {
            int platformID = ReadUInt16();
            int encodingID = ReadUInt16();
            int languageID = ReadUInt16();
            int nameID = ReadUInt16();
            int length = ReadUInt16();
            int offset = ReadUInt16();

            if (platformID == 1 && encodingID == 0 && languageID == 0) {
                // Macintosh
                String str = Encoding.UTF8.GetString(
                        buf, table.offset + stringOffset + offset, length);
                if (nameID == 6) {
                    fontName = str;
                } else {
                    macFontInfo.Append(str);
                    macFontInfo.Append('\n');
                }
            } else if (platformID == 3 && encodingID == 1 && languageID == 0x409) {
                // Windows
                String str = Encoding.BigEndianUnicode.GetString(
                        buf, table.offset + stringOffset + offset, length);
                if (nameID == 6) {
                    fontName = str;
                } else {
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

        // Process the encoding records
        bool format4subtable = false;
        int subtableOffset = 0;
        for (int i = 0; i < numRecords; i++) {
            int platformID = ReadUInt16();
            int encodingID = ReadUInt16();
            subtableOffset = (int) ReadUInt32();
            if (platformID == 3 && encodingID == 1) {
                format4subtable = true;
                break;
            }
        }
        if (!format4subtable) {
            throw new Exception("Format 4 subtable not found in this font.");
        }

        index = tableOffset + subtableOffset;

        int format   = ReadUInt16();
        int tableLen = ReadUInt16();
        int language = ReadUInt16();
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

        int[] glyphIdArray = new int[(tableLen - (16 + 8*segCount)) / 2];
        for (int i = 0; i < glyphIdArray.Length; i++) {
            glyphIdArray[i] = ReadUInt16();
        }

        for (int ch = firstChar; ch <= lastChar; ch++) {
            int seg = GetSegmentFor(ch, startCount, endCount, segCount);
            if (seg != -1) {
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
        this.cff = true;
        this.cffOff = table.offset;
        this.cffLen = table.length;
    }

    // Reads where the marks go from the GPOS table: the marks on letters and
    // ligatures, like Hebrew and Arabic vowel marks, from its MarkToBase and
    // MarkToLigature lookups, and the marks that attach to other marks, like a
    // Thai tone mark above an upper vowel, from its MarkToMark lookups.
    private void GPOS(FontTable table) {
        markToMarkOffsets = new System.Collections.Generic.Dictionary<int, int[]>();
        markAnchors = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<int, int[]>>();
        baseAnchors = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<int, int[]>>();
        int lookupList = table.offset + GetUInt16(table.offset + 8);
        int lookupCount = GetUInt16(lookupList);
        for (int i = 0; i < lookupCount; i++) {
            int lookup = lookupList + GetUInt16(lookupList + 2 + 2*i);
            int lookupType = GetUInt16(lookup);
            int subTableCount = GetUInt16(lookup + 4);
            for (int j = 0; j < subTableCount; j++) {
                int subTable = lookup + GetUInt16(lookup + 6 + 2*j);
                int type = lookupType;
                if (type == 9) {    // An extension lookup holds the subtable
                    type = GetUInt16(subTable + 2);
                    subTable += GetUInt32(subTable + 4);
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
        System.Collections.Generic.Dictionary<int, int[]> marks = new System.Collections.Generic.Dictionary<int, int[]>();
        for (int m = 0; m < markGlyphs.Length; m++) {
            int anchor = markArray + GetUInt16(markArray + 4 + 4*m);
            marks[markGlyphs[m]] = new int[] {
                    GetUInt16(markArray + 2 + 4*m), GetInt16(anchor + 2), GetInt16(anchor + 4)};
        }
        System.Collections.Generic.Dictionary<int, int[]> bases = new System.Collections.Generic.Dictionary<int, int[]>();
        for (int b = 0; b < baseGlyphs.Length; b++) {
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
    private int[] Coverage(int offset) {
        int format = GetUInt16(offset);
        int count = GetUInt16(offset + 2);
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
        int[] glyphs = new int[size];
        for (int i = 0; i < count; i++) {
            int range = offset + 4 + 6*i;
            int start = GetUInt16(range);
            int end = GetUInt16(range + 2);
            int coverageIndex = GetUInt16(range + 4);
            for (int glyph = start; glyph <= end; glyph++) {
                glyphs[coverageIndex + glyph - start] = glyph;
            }
        }
        return glyphs;
    }

    // The GPOS table is read at offsets from its subtables. A value outside
    // the font data is read as 0, so a broken table cannot stop the font from
    // loading.
    private int GetUInt16(int offset) {
        if (offset < 0 || offset + 2 > buf.Length) {
            return 0;
        }
        return (buf[offset] << 8) | buf[offset + 1];
    }

    private int GetInt16(int offset) {
        return (short) GetUInt16(offset);
    }

    private int GetUInt32(int offset) {
        return (GetUInt16(offset) << 16) | GetUInt16(offset + 2);
    }

    private int GetSegmentFor(
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

    private byte ReadByte() {
        return buf[index++];
    }

    private UInt16 ReadUInt16() {
        UInt32 val = 0;
        val |= ((UInt32) buf[index++]) << 8;
        val |= ((UInt32) buf[index++]);
        return (UInt16) val;
    }

    private UInt32 ReadUInt32() {
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
