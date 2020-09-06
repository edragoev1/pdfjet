/**
 *  OTF.cs
 *
Copyright 2020 Innovatics Inc.
*/
using System;
using System.IO;
using System.Text;

namespace PDFjet.NET {
public class OTF {

    public String fontName;
    public String fontInfo;
    public readonly MemoryStream baos;
    public readonly byte[] buf;
    public int unitsPerEm;
    public short bBoxLLx;
    public short bBoxLLy;
    public short bBoxURx;
    public short bBoxURy;
    public short ascent;
    public short descent;
    public int[] advanceWidth;
    public int firstChar;
    public int lastChar;
    public int capHeight;
    public int[] glyphWidth;
    public long postVersion;
    public long italicAngle;
    public short underlinePosition;
    public short underlineThickness;
    public bool cff = false;
    public int cffOff;
    public int cffLen;
    public readonly int[] unicodeToGID = new int[0x10000];
    private int index = 0;

    public OTF(Stream stream) {
        this.baos = new MemoryStream();
        byte[] buffer = new byte[0x10000];
        int count;
        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
            baos.Write(buffer, 0, count);
        }
        stream.Dispose();
        buf = baos.ToArray();

        // Extract OTF metadata
        long version = ReadUInt32();
        if (version == 0x00010000L ||   // Win OTF
            version == 0x74727565L ||   // Mac TTF
            version == 0x4F54544FL) {   // CFF OTF
            // We should be able to read this font
        }
        else {
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
            else if (table.name.Equals("cmap")) { cmapTable = table; }
            index = k;      // Restore the index
        }

        // This table must be processed last
        Cmap(cmapTable);

        baos = new MemoryStream();
        DeflaterOutputStream dos = new DeflaterOutputStream(baos);
        if (cff) {
            dos.Write(buf, cffOff, cffLen);
        }
        else {
            dos.Write(buf, 0, buf.Length);
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
                }
                else {
                    macFontInfo.Append(str);
                    macFontInfo.Append('\n');
                }
            }
            else if (platformID == 3 && encodingID == 1 && languageID == 0x409) {
                // Windows
                String str = Encoding.BigEndianUnicode.GetString(
                        buf, table.offset + stringOffset + offset, length);
                if (nameID == 6) {
                    fontName = str;
                }
                else {
                    winFontInfo.Append(str);
                    winFontInfo.Append('\n');
                }
            }
        }
        fontInfo = (winFontInfo != null) ? winFontInfo.ToString() : macFontInfo.ToString();
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

        glyphWidth = new int[lastChar + 1];
        for (int i = 0; i < glyphWidth.Length; i++) {
            glyphWidth[i] = advanceWidth[0];
        }

        for (int ch = firstChar; ch <= lastChar; ch++) {
            int seg = GetSegmentFor(ch, startCount, endCount, segCount);
            if (seg != -1) {
                int gid;
                int offset = idRangeOffset[seg];
                if (offset == 0) {
                    gid = (idDelta[seg] + ch) % 65536;
                }
                else {
                    offset /= 2;
                    offset -= segCount - seg;
                    gid = glyphIdArray[offset + (ch - startCount[seg])];
                    if (gid != 0) {
                        gid += idDelta[seg] % 65536;
                    }
                }

                if (gid < advanceWidth.Length) {
                    glyphWidth[ch] = advanceWidth[gid];
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
