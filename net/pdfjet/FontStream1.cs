/*
 * FontStream1.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
class FontStream1 {
    internal static void Register(
            PDF pdf,
            Font font,
            Stream inputStream) {
        GetFontData(font, inputStream);
        EmbedFontFile(pdf, font, inputStream);
        AddFontDescriptorObject(pdf, font);
        AddCIDFontDictionaryObject(pdf, font);
        AddToUnicodeCMapObject(pdf, font);

        // Type0 Font Dictionary
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /Font\n");
        pdf.Append("/Subtype /Type0\n");
        pdf.Append("/BaseFont /");
        pdf.Append(Encoding.UTF8.GetBytes(font.name));
        pdf.Append(Token.Newline);
        pdf.Append("/Encoding /Identity-H\n");
        pdf.Append("/DescendantFonts [");
        pdf.Append(font.cidFontDictObjNumber);
        pdf.Append(" 0 R]\n");
        pdf.Append("/ToUnicode ");
        pdf.Append(font.toUnicodeCMapObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        font.objNumber = pdf.GetObjNumber();
        pdf.fonts.Add(font);
    }

    private static void EmbedFontFile(PDF pdf, Font font, Stream stream) {
        // Check if the font file is already embedded
        foreach (Font f in pdf.fonts) {
            if (f.fileObjNumber != 0 && f.name.Equals(font.name)) {
                font.fileObjNumber = f.fileObjNumber;
                return;
            }
        }

        int metadataObjNumber = pdf.AddMetadataObject(font.info, true);
        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);

        pdf.Append("/Metadata ");
        pdf.Append(metadataObjNumber);
        pdf.Append(" 0 R\n");

        if (font.cff) {
            pdf.Append("/Subtype /CIDFontType0C\n");
        } else {
            pdf.Append("/Length1 ");
            pdf.Append(font.uncompressedSize);
            pdf.Append(Token.Newline);
        }
        pdf.Append("/Filter /FlateDecode\n");

        byte[] compressed = ReadBytes(stream, font.compressedSize);
        byte[] encrypted = null;
        if (pdf.encryption != null) {
            encrypted = AES256.Encrypt(compressed, pdf.encryption.GetKey());
        }
        stream.Dispose();

        pdf.Append("/Length ");
        if (pdf.encryption != null) {
            pdf.Append(encrypted.Length);
        } else {
            pdf.Append(font.compressedSize);
        }
        pdf.Append(Token.Newline);
        pdf.Append(Token.EndDictionary);
        pdf.Append(Token.Stream);
        if (pdf.encryption != null) {
            pdf.Append(encrypted);
        } else {
            pdf.Append(compressed);
        }
        pdf.Append(Token.EndStream);
        pdf.EndObj();

        font.fileObjNumber = pdf.GetObjNumber();
    }

    private static void AddFontDescriptorObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.fontDescriptorObjNumber != 0 && f.name.Equals(font.name)) {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber;
                return;
            }
        }

        pdf.NewObj();
        pdf.Append(Token.BeginDictionary);
        pdf.Append("/Type /FontDescriptor\n");
        pdf.Append("/FontName /");
        pdf.Append(font.name);
        pdf.Append('\n');
        if (font.cff) {
            pdf.Append("/FontFile3 ");
        } else {
            pdf.Append("/FontFile2 ");
        }
        pdf.Append(font.fileObjNumber);
        pdf.Append(" 0 R\n");
        pdf.Append("/Flags 32\n");
        pdf.Append("/FontBBox [");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxLLx, font.unitsPerEm));
        pdf.Append(' ');
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxLLy, font.unitsPerEm));
        pdf.Append(' ');
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxURx, font.unitsPerEm));
        pdf.Append(' ');
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.bBoxURy, font.unitsPerEm));
        pdf.Append("]\n");
        pdf.Append("/Ascent ");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.fontAscent, font.unitsPerEm));
        pdf.Append('\n');
        pdf.Append("/Descent ");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.fontDescent, font.unitsPerEm));
        pdf.Append('\n');
        pdf.Append("/ItalicAngle 0\n");
        pdf.Append("/CapHeight ");
        pdf.Append(OpenTypeFont.ToGlyphSpace(font.capHeight, font.unitsPerEm));
        pdf.Append('\n');
        pdf.Append("/StemV 79\n");
        pdf.Append(Token.EndDictionary);
        pdf.EndObj();

        font.fontDescriptorObjNumber = pdf.GetObjNumber();
    }

    private static void AddToUnicodeCMapObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.toUnicodeCMapObjNumber != 0 && f.name.Equals(font.name)) {
                font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber;
                return;
            }
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("/CIDInit /ProcSet findresource begin\n");
        sb.Append("12 dict begin\n");
        sb.Append("begincmap\n");
        sb.Append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n");
        sb.Append("/CMapName /Adobe-Identity def\n");
        sb.Append("/CMapType 2 def\n");

        sb.Append("1 begincodespacerange\n");
        sb.Append("<0000> <FFFF>\n");
        sb.Append("endcodespacerange\n");

        List<String> list = new List<String>();
        // A character the font does not contain is drawn with the .notdef
        // glyph. PDF/UA requires every glyph to map to Unicode, so map it to
        // the replacement character.
        list.Add("<0000> <FFFD>\n");
        StringBuilder buf = new StringBuilder();
        int[] unicodeOf = UnicodeOfGlyphs(font.unicodeToGID);
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = font.unicodeToGID[cid];
            if (gid > 0 && unicodeOf[gid] == cid) {
                buf.Append('<');
                buf.Append(ToHexString(gid));
                buf.Append("> <");
                // A presentation form that the Bidi class puts in maps to the letters it stands for.
                String letters = Bidi.LettersOf(cid);
                if (letters == null) {
                    buf.Append(ToHexString(cid));
                } else {
                    foreach (char ch in letters) {
                        buf.Append(ToHexString(ch));
                    }
                }
                buf.Append(">\n");
                list.Add(buf.ToString());
                buf.Length = 0;
                if (list.Count == 100) {
                    WriteListToBuffer(sb, list);
                }
            }
        }
        if (list.Count > 0) {
            WriteListToBuffer(sb, list);
        }

        sb.Append("endcmap\n");
        sb.Append("CMapName currentdict /CMap defineresource pop\n");
        sb.Append("end\nend");

        byte[] buf2 = Encoding.UTF8.GetBytes(sb.ToString());
        if (pdf.encryption != null) {
            buf2 = AES256.Encrypt(buf2, pdf.encryption.GetKey());
        }

        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Length ");
        pdf.Append(buf2.Length);
        pdf.Append("\n");
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(buf2);
        pdf.Append("\nendstream\n");
        pdf.EndObj();

        font.toUnicodeCMapObjNumber = pdf.GetObjNumber();
    }

    private static void AddCIDFontDictionaryObject(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f.cidFontDictObjNumber != 0 && f.name.Equals(font.name)) {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber;
                return;
            }
        }

        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /Font\n");
        if (font.cff) {
            pdf.Append("/Subtype /CIDFontType0\n");
        } else {
            pdf.Append("/Subtype /CIDFontType2\n");
        }
        pdf.Append("/BaseFont /");
        pdf.Append(font.name);
        pdf.Append('\n');

        byte[] registry = Encoding.UTF8.GetBytes("Adobe");
        byte[] ordering = Encoding.UTF8.GetBytes("Identity");
        if (pdf.encryption != null) {
            registry = AES256.Encrypt(registry, pdf.encryption.GetKey());
            ordering = AES256.Encrypt(ordering, pdf.encryption.GetKey());
        }
        pdf.Append("/CIDSystemInfo <</Registry <");
        pdf.Append(Util.ToHexString(registry));
        pdf.Append("> /Ordering <");
        pdf.Append(Util.ToHexString(ordering));
        pdf.Append("> /Supplement 0>>\n");

        pdf.Append("/FontDescriptor ");
        pdf.Append(font.fontDescriptorObjNumber);
        pdf.Append(" 0 R\n");

        float k = 1000.0f / Convert.ToSingle(font.unitsPerEm);
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        pdf.Append("/DW ");
        pdf.Append((int) Math.Round(k * Convert.ToSingle(font.advanceWidth[font.advanceWidth.Length - 1]), MidpointRounding.AwayFromZero));
        pdf.Append('\n');

        pdf.Append("/W [0[\n");
        foreach (int width in font.advanceWidth) {
            pdf.Append((int) Math.Round(k * Convert.ToSingle(width), MidpointRounding.AwayFromZero));
            pdf.Append(' ');
        }
        pdf.Append("]]\n");

        pdf.Append("/CIDToGIDMap /Identity\n");
        pdf.Append(">>\n");
        pdf.EndObj();

        font.cidFontDictObjNumber = pdf.GetObjNumber();
    }

    /// <summary>
    /// Returns the character that each glyph maps to in a ToUnicode CMap. A
    /// glyph can stand for several characters, like the space and the no-break
    /// space, but viewers read a CMap with more than one entry for a glyph
    /// differently. So a glyph maps to the first character that uses it, unless
    /// that is one text seldom has and another character uses the glyph too,
    /// like the modifier letter apostrophe and the right single quotation mark.
    /// </summary>
    internal static int[] UnicodeOfGlyphs(int[] unicodeToGID) {
        int[] unicode = new int[0x10000];
        for (int i = 0; i < unicode.Length; i++) {
            unicode[i] = -1;
        }
        for (int cid = 0; cid <= 0xffff; cid++) {
            int gid = unicodeToGID[cid];
            if (gid > 0 && (unicode[gid] == -1 ||
                    (IsSeldomText(unicode[gid]) && !IsSeldomText(cid)))) {
                unicode[gid] = cid;
            }
        }
        return unicode;
    }

    // The soft hyphen, the spacing modifier letters, the combining marks, the
    // figure dash, the deprecated angle brackets, the CJK and Kangxi radicals,
    // which CJK fonts draw with the glyphs of the ideographs, and the private
    // use characters.
    private static bool IsSeldomText(int ch) {
        return ch == 0x00AD ||
                (ch >= 0x02B0 && ch <= 0x036F) ||
                (ch >= 0x1AB0 && ch <= 0x1AFF) ||
                (ch >= 0x1DC0 && ch <= 0x1DFF) ||
                ch == 0x2012 ||
                (ch >= 0x20D0 && ch <= 0x20FF) ||
                (ch >= 0x2329 && ch <= 0x232A) ||
                (ch >= 0x2E80 && ch <= 0x2FDF) ||
                (ch >= 0xE000 && ch <= 0xF8FF) ||
                (ch >= 0xFE20 && ch <= 0xFE2F);
    }

    internal static String ToHexString(int code) {
        String str = Convert.ToString(code, 16);
        if (str.Length == 1) {
            return "000" + str;
        } else if (str.Length == 2) {
            return "00" + str;
        } else if (str.Length == 3) {
            return "0" + str;
        }
        return str;
    }

    internal static void WriteListToBuffer(StringBuilder sb, List<String> list) {
        sb.Append(list.Count);
        sb.Append(" beginbfchar\n");
        foreach (String str in list) {
            sb.Append(str);
        }
        sb.Append("endbfchar\n");
        list.Clear();
    }

    // The most bytes the metrics of a stream font decode to, with its marks
    // compressed in them. IBM Plex Sans JP has 180 KB.
    private const int MaxFontMetricsLength = 16 * 1024 * 1024;

    private static void FontStreamError(String what) {
        throw new Exception("Invalid font stream: " + what + ".");
    }

    // Returns true if the name can be written as a PDF name as it is:
    // printable ASCII, and none of the characters that end a name or start an
    // escape.
    private static bool IsFontName(byte[] name) {
        if (name.Length == 0) {
            return false;
        }
        foreach (byte b in name) {
            if (b < 0x21 || b > 0x7E || "()<>[]{}/%#".IndexOf((char) b) != -1) {
                return false;
            }
        }
        return true;
    }

    private static int GetByte(Stream stream) {
        int b = stream.ReadByte();
        if (b == -1) {
            throw new EndOfStreamException("Unexpected end of the font stream.");
        }
        return b;
    }

    private static int GetInt24(Stream stream) {
        return GetByte(stream) << 16 | GetByte(stream) << 8 | GetByte(stream);
    }

    private static long GetUInt32(Stream stream) {
        return (long) GetByte(stream) << 24 | (long) GetByte(stream) << 16 |
                (long) GetByte(stream) << 8 | (long) GetByte(stream);
    }

    // A size that must fit an int, which any font file does.
    private static int GetSize(Stream stream) {
        long size = GetUInt32(stream);
        if (size > int.MaxValue) {
            FontStreamError("the size of the font file");
        }
        return (int) size;
    }

    // Reads the next length bytes. It reads them as they come, so a length
    // that the stream does not have takes no memory.
    internal static byte[] ReadBytes(Stream stream, long length) {
        using (MemoryStream buf = new MemoryStream()) {
            byte[] buffer = new byte[4096];
            while (buf.Length < length) {
                int bytesRead = stream.Read(buffer, 0, (int) Math.Min(buffer.Length, length - buf.Length));
                if (bytesRead == 0) {
                    throw new EndOfStreamException("Unexpected end of the font stream.");
                }
                buf.Write(buffer, 0, bytesRead);
            }
            return buf.ToArray();
        }
    }

    // Reads the metrics of a stream font from a byte array, after checking
    // that the bytes are there.
    private sealed class Metrics {
        private readonly byte[] data;
        private readonly String what;
        internal int pos = 0;

        internal Metrics(byte[] data, String what) {
            this.data = data;
            this.what = what;
        }

        internal int Remaining() {
            return data.Length - pos;
        }

        // Checks that count entries of size bytes follow.
        internal void Need(int count, int size) {
            if (count < 0 || count > Remaining() / size) {
                FontStreamError(what + " end too soon");
            }
        }

        internal int GetInt32() {
            Need(1, 4);
            int v = data[pos] << 24 | data[pos + 1] << 16 | data[pos + 2] << 8 | data[pos + 3];
            pos += 4;
            return v;
        }

        // Reads the number of entries of size bytes that follow.
        internal int GetCount(int size) {
            int count = GetInt32();
            Need(count, size);
            return count;
        }

        internal int GetUInt16() {
            int v = data[pos] << 8 | data[pos + 1];
            pos += 2;
            return v;
        }

        internal byte[] GetBytes(int length) {
            byte[] bytes = new byte[length];
            Array.Copy(data, pos, bytes, 0, length);
            pos += length;
            return bytes;
        }
    }

    internal static void GetFontData(Font font, Stream inputStream) {
        byte[] fontName = ReadBytes(inputStream, GetByte(inputStream));
        if (!IsFontName(fontName)) {
            FontStreamError("the font name");
        }
        font.name = System.Text.Encoding.UTF8.GetString(fontName);
        font.info = System.Text.Encoding.UTF8.GetString(ReadBytes(inputStream, GetInt24(inputStream)));

        Metrics metrics = new Metrics(Decompressor.Inflate(
                ReadBytes(inputStream, GetUInt32(inputStream)), MaxFontMetricsLength), "the metrics");
        font.unitsPerEm = metrics.GetInt32();
        font.bBoxLLx = metrics.GetInt32();
        font.bBoxLLy = metrics.GetInt32();
        font.bBoxURx = metrics.GetInt32();
        font.bBoxURy = metrics.GetInt32();
        font.fontAscent = metrics.GetInt32();
        font.fontDescent = metrics.GetInt32();
        font.firstChar = metrics.GetInt32();
        font.lastChar = metrics.GetInt32();
        font.capHeight = metrics.GetInt32();
        font.fontUnderlinePosition = metrics.GetInt32();
        font.fontUnderlineThickness = metrics.GetInt32();
        // The range OpenType allows; the sizes of the text are divided by it.
        if (font.unitsPerEm < 16 || font.unitsPerEm > 16384) {
            FontStreamError("the units per em");
        }
        // A character in the range is looked up in unicodeToGID.
        if (font.firstChar < 0 || font.lastChar > 0xFFFF) {
            FontStreamError("the first or last character");
        }

        int len = metrics.GetCount(2);
        if (len == 0) {
            FontStreamError("no advance widths");
        }
        font.advanceWidth = new int[len];
        for (int i = 0; i < len; i++) {
            font.advanceWidth[i] = metrics.GetUInt16();
        }

        len = metrics.GetCount(2);
        if (len != 0x10000) {
            FontStreamError("the character map");
        }
        font.unicodeToGID = new int[len];
        for (int i = 0; i < len; i++) {
            font.unicodeToGID[i] = metrics.GetUInt16();
        }

        // Where the GPOS table of the font puts the marks, compressed on its
        // own after the metrics of a stream that has them. It is kept as it is
        // and read when a mark is drawn in the font; see ReadMarks. A font with
        // no marks has none, or 0 bytes of them.
        if (metrics.Remaining() > 0) {
            byte[] markData = metrics.GetBytes(metrics.GetCount(1));
            if (markData.Length > 0) {
                font.markData = markData;
            }
        }
        // The line gap of a font that has one follows the marks, where a library
        // that does not read it stops.
        if (metrics.Remaining() > 0) {
            font.fontLineGap = metrics.GetInt32();
        }

        int flag = GetByte(inputStream);
        if (flag == 'R') {
            // The tables of an OpenType font that are not in its CFF data,
            // which keep the font whole; they are not embedded.
            SkipFully(inputStream, GetUInt32(inputStream));
            flag = GetByte(inputStream);
        }
        font.cff = flag == 'Y';
        font.uncompressedSize = GetSize(inputStream);
        font.compressedSize = GetSize(inputStream);
    }

    // Reads where the marks go, which GetFontData keeps compressed: for each
    // MarkToBase and MarkToLigature subtable the class and anchor of each mark
    // and the anchors of each letter, and then the offsets of the marks that go
    // on other marks. Done once, the first time a mark is drawn in the font.
    internal static void ReadMarks(Font font) {
        Metrics stream = new Metrics(
                Decompressor.Inflate(font.markData, MaxFontMetricsLength), "the marks");
        int subTables = stream.GetCount(8);
        List<Dictionary<int, int[]>> markAnchors = new List<Dictionary<int, int[]>>(subTables);
        List<Dictionary<int, int[]>> baseAnchors = new List<Dictionary<int, int[]>>(subTables);
        for (int i = 0; i < subTables; i++) {
            int count = stream.GetCount(16);
            Dictionary<int, int[]> marks = new Dictionary<int, int[]>(count);
            for (int j = 0; j < count; j++) {
                int gid = stream.GetInt32();
                int markClass = stream.GetInt32();
                // The anchors of a letter are 3 ints for each mark class.
                if (markClass < 0 || markClass > 0xFFFF) {
                    FontStreamError("a mark class");
                }
                int x = stream.GetInt32();
                marks[gid] = new int[] {markClass, x, stream.GetInt32()};
            }
            count = stream.GetCount(8);
            Dictionary<int, int[]> bases = new Dictionary<int, int[]>(count);
            for (int j = 0; j < count; j++) {
                int gid = stream.GetInt32();
                int[] anchors = new int[stream.GetCount(4)];
                for (int k = 0; k < anchors.Length; k++) {
                    anchors[k] = stream.GetInt32();
                }
                bases[gid] = anchors;
            }
            markAnchors.Add(marks);
            baseAnchors.Add(bases);
        }
        int pairs = stream.GetCount(16);
        Dictionary<int, int[]> markToMarkOffsets = new Dictionary<int, int[]>(pairs);
        for (int j = 0; j < pairs; j++) {
            int other = stream.GetInt32();
            int mark = stream.GetInt32();
            int dx = stream.GetInt32();
            markToMarkOffsets[(other << 16) | mark] = new int[] {dx, stream.GetInt32()};
        }
        font.markAnchors = markAnchors;
        font.baseAnchors = baseAnchors;
        font.markToMarkOffsets = markToMarkOffsets;
        font.markData = null;
    }

    private static void SkipFully(Stream stream, long count) {
        byte[] buffer = new byte[4096];
        while (count > 0) {
            int bytesRead = stream.Read(buffer, 0, (int) Math.Min(count, buffer.Length));
            if (bytesRead == 0) {
                throw new EndOfStreamException("Unexpected end of the font stream.");
            }
            count -= bytesRead;
        }
    }
}   // End of FontStream1.cs
}   // End of namespace PDFjet.NET
