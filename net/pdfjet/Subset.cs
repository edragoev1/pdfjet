/*
 * Subset.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// The subsets of the embedded fonts, .ttf and .otf files, embedded at
/// Complete() with the outlines of the glyphs the document did not draw
/// emptied. The glyph numbers stay, so the widths, the character map and the
/// ToUnicode map are those of the whole font.
/// </summary>
class Subset {
    /// <summary>
    /// The font program of a TrueType font, or the CFF table of an OpenType
    /// font with CFF outlines, kept until Complete(). Fonts read from one file
    /// share one program, and the glyphs any of them drew.
    /// </summary>
    internal sealed class Program {
        internal byte[] font;       // The TrueType font, or the CFF table
        internal bool cff;
        internal bool forbidden;    // The fsType of a CFF font's OS/2 table forbids subsetting
        // The glyphs drawn, by glyph number, written in four hexadecimal digits.
        internal readonly bool[] used = new bool[0x10000];
        internal bool whole;        // Set by SetSubset(false): the font is embedded whole

        internal Program() {
            used[0] = true;         // .notdef, drawn for a character the font lacks
        }
    }

    /// <summary>The font cannot be subset, and is embedded whole.</summary>
    internal sealed class NotSubset : Exception {
        internal NotSubset() : base("the font cannot be subset") {
        }
    }

    // The tables a subset keeps: those a PDF reader draws a TrueType font with
    // (ISO 32000-1, 9.9), and the cmap, OS/2, name and post tables, which some
    // readers and checkers look at. The tables of shaping and of vertical text,
    // which PDFjet reads from the font and the PDF does not need, are left
    // out, and so is the DSIG table, the signature of the whole font.
    private static readonly HashSet<String> SubsetTables = new HashSet<String> {
        "head", "hhea", "hmtx", "maxp", "loca", "glyf",
        "cvt ", "fpgm", "prep", "gasp",
        "cmap", "OS/2", "name", "post"
    };

    /// <summary>
    /// Gives the font the program of a font the PDF already has from the same
    /// file, if any, or else a new one of the font given.
    /// </summary>
    internal static void Share(PDF pdf, Font font, byte[] program, bool cff, bool forbidden) {
        foreach (Font f in pdf.fonts) {
            if (f.program != null && f.name.Equals(font.name) && f.checksum == font.checksum) {
                font.program = f.program;
                return;
            }
        }
        font.program = new Program();
        font.program.font = program;
        font.program.cff = cff;
        font.program.forbidden = forbidden;
    }

    /// <summary>
    /// Writes the TrueType fonts at Complete(), when every glyph they draw is
    /// known: each font program, its descriptor, its CID font and its
    /// ToUnicode map once, and the Type0 font of each Font under the number its
    /// pages refer to.
    /// </summary>
    internal static void AddTrueTypeFonts(PDF pdf) {
        foreach (Font font in pdf.fonts) {
            if (font.program == null) {
                continue;
            }
            bool[] kept = EmbedProgram(pdf, font);
            AddCIDSetObject(pdf, font, kept);
            FontWriter.AddFontDescriptorObject(pdf, font, font.baseFont);
            FontWriter.AddCIDFontDictionaryObject(pdf, font, font.baseFont, kept);
            FontWriter.AddToUnicodeCMapObject(pdf, font, kept);

            pdf.NewObj(font.objNumber);
            pdf.Append("<<\n");
            pdf.Append("/Type /Font\n");
            pdf.Append("/Subtype /Type0\n");
            pdf.Append("/BaseFont /");
            pdf.Append(font.baseFont);
            pdf.Append('\n');
            pdf.Append("/Encoding /Identity-H\n");
            pdf.Append("/DescendantFonts [");
            pdf.Append(font.cidFontDictObjNumber);
            pdf.Append(" 0 R]\n");
            pdf.Append("/ToUnicode ");
            pdf.Append(font.toUnicodeCMapObjNumber);
            pdf.Append(" 0 R\n");
            pdf.Append(">>\n");
            pdf.EndObj();
        }
        // The programs are no longer needed.
        foreach (Font font in pdf.fonts) {
            font.program = null;
        }
    }

    /// <summary>
    /// Returns the font program to embed, a subset unless it is to be whole,
    /// and sets the name of the font, with the tag of a subset, and the glyphs
    /// the subset keeps, null for a whole font.
    /// </summary>
    internal static byte[] EmbeddedProgram(Font font) {
        Program program = font.program;
        byte[] data = program.font;
        if (!program.whole && !program.forbidden) {
            try {
                bool[] kept;
                byte[] subset = program.cff ?
                        CFFSubset.subset(data, program.used, out kept) : SubsetTrueType(data, program.used, out kept);
                font.kept = kept;
                font.baseFont = SubsetTag(font.checksum, kept) + "+" + font.name;
                return subset;
            } catch (NotSubset) {
                // Embedded whole
            }
        }
        if (program.cff) {
            // Whole, written again for the identity charset of a CID-keyed
            // font, or as it is when it cannot be.
            try {
                bool[] all;
                data = CFFSubset.subset(data, null, out all);
            } catch (NotSubset) {
                // As it is
            }
        }
        font.kept = null;
        font.baseFont = font.name;
        return data;
    }

    /// <summary>
    /// Writes the font program, a subset unless it is to be whole, sets the
    /// name of the font, with the tag of a subset, and returns the glyphs the
    /// subset keeps, or null for a whole font. A font of the same program
    /// embedded before is not written again: its name and object numbers are
    /// used.
    /// </summary>
    private static bool[] EmbedProgram(PDF pdf, Font font) {
        foreach (Font f in pdf.fonts) {
            if (f == font) {
                break;
            }
            if (f.fileObjNumber != 0 && f.name.Equals(font.name) && f.checksum == font.checksum) {
                font.fileObjNumber = f.fileObjNumber;
                font.cidSetObjNumber = f.cidSetObjNumber;
                font.baseFont = f.baseFont;
                return f.kept;
            }
        }

        Program program = font.program;
        byte[] data = EmbeddedProgram(font);
        byte[] compressed = Compressor.Deflate(data);
        int length = data.Length;

        int metadataObjNumber = pdf.AddMetadataObject(font.info, true);
        if (pdf.encryption != null) {
            compressed = AES256.Encrypt(compressed, pdf.encryption.GetKey());
        }
        pdf.NewObj();
        pdf.Append("<<\n");
        if (program.cff) {
            pdf.Append("/Subtype /CIDFontType0C\n");
        }
        pdf.Append("/Filter /FlateDecode\n");
        if (!program.cff) {
            pdf.Append("/Length1 ");
            pdf.Append(length);
            pdf.Append('\n');
        }
        if (metadataObjNumber != -1) {
            pdf.Append("/Metadata ");
            pdf.Append(metadataObjNumber);
            pdf.Append(" 0 R\n");
        }
        pdf.Append("/Length ");
        pdf.Append(compressed.Length);
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(compressed);
        pdf.Append("\nendstream\n");
        pdf.EndObj();
        font.fileObjNumber = pdf.GetObjNumber();
        return font.kept;
    }

    /// <summary>
    /// Writes, for PDF/A-1, which asks it of a subset, the CIDSet of the
    /// glyphs the subset keeps: a bit for each glyph number, the first in the
    /// high bit of the first byte.
    /// </summary>
    private static void AddCIDSetObject(PDF pdf, Font font, bool[] kept) {
        if (kept == null || font.cidSetObjNumber != 0 ||
                (pdf.compliance != Compliance.PDF_A_1A && pdf.compliance != Compliance.PDF_A_1B)) {
            return;
        }
        byte[] bits = new byte[(kept.Length + 7) / 8];
        for (int gid = 0; gid < kept.Length; gid++) {
            if (kept[gid]) {
                bits[gid / 8] |= (byte) (0x80 >> (gid % 8));
            }
        }
        FontWriter.AddCompressedStream(pdf, bits);
        font.cidSetObjNumber = pdf.GetObjNumber();
    }

    /// <summary>
    /// Returns the six capitals that begin the name of a subset, from the font
    /// and the glyphs it keeps, so that another subset of the font has another
    /// tag, and the same document the same one, in the four ports.
    /// </summary>
    internal static String SubsetTag(ulong checksum, bool[] kept) {
        ulong hash = checksum;
        for (int gid = 0; gid < kept.Length; gid++) {
            if (kept[gid]) {
                hash = Font.Fold(hash, gid);
            }
        }
        StringBuilder tag = new StringBuilder(6);
        for (int i = 0; i < 6; i++) {
            tag.Append((char) ('A' + (int) (hash % 26UL)));
            hash /= 26;
        }
        return tag.ToString();
    }

    private static int U16(byte[] ttf, int at) {
        if (at < 0 || at + 2 > ttf.Length) {
            throw new NotSubset();
        }
        return ttf[at] << 8 | ttf[at + 1];
    }

    private static int U32(byte[] ttf, int at) {
        if (at < 0 || at + 4 > ttf.Length) {
            throw new NotSubset();
        }
        long value = (long) ttf[at] << 24 | (long) ttf[at + 1] << 16 | (long) ttf[at + 2] << 8 | ttf[at + 3];
        if (value > int.MaxValue) {
            throw new NotSubset();
        }
        return (int) value;
    }

    /// <summary>A table of the font: its tag, offset and length.</summary>
    private sealed class Table {
        internal readonly String tag;
        internal readonly int offset;
        internal readonly int length;

        internal Table(String tag, int offset, int length) {
            this.tag = tag;
            this.offset = offset;
            this.length = length;
        }
    }

    /// <summary>
    /// Returns the TrueType font with the outlines of the glyphs not used
    /// emptied, and gives in kept the glyphs kept: those used, glyph 0 and the
    /// parts of the composite glyphs kept. Every glyph keeps its number, so the
    /// tables kept, SubsetTables, are copied as they are, but for the glyf and
    /// loca tables made again, the head table's checksum adjustment, and the
    /// post table without the names of the glyphs. A font that forbids
    /// subsetting in the fsType of its OS/2 table, or whose tables cannot be
    /// read, throws NotSubset, and is embedded whole.
    /// </summary>
    internal static byte[] SubsetTrueType(byte[] ttf, bool[] used, out bool[] kept) {
        int numTables = U16(ttf, 4);
        List<Table> tables = new List<Table>(numTables);
        Table head = null, loca = null, glyf = null, maxp = null, os2 = null;
        for (int i = 0; i < numTables; i++) {
            int entry = 12 + 16 * i;
            int offset = U32(ttf, entry + 8);
            int length = U32(ttf, entry + 12);
            if ((long) offset + length > ttf.Length) {
                throw new NotSubset();
            }
            Table table = new Table(new String(new char[] {
                    (char) ttf[entry], (char) ttf[entry + 1], (char) ttf[entry + 2], (char) ttf[entry + 3]}), offset, length);
            tables.Add(table);
            switch (table.tag) {
                case "head": head = table; break;
                case "loca": loca = table; break;
                case "glyf": glyf = table; break;
                case "maxp": maxp = table; break;
                case "OS/2": os2 = table; break;
                default: break;
            }
        }
        if (head == null || maxp == null || loca == null || glyf == null ||
                head.length < 54 || maxp.length < 6 || loca.length == 0 || glyf.length == 0) {
            throw new NotSubset();
        }
        if (os2 != null && os2.length >= 10 && (U16(ttf, os2.offset + 8) & 0x0100) != 0) {
            throw new NotSubset(); // No subsetting
        }
        int numGlyphs = U16(ttf, maxp.offset + 4);
        int longOffsets = U16(ttf, head.offset + 50);
        if (numGlyphs == 0 || loca.length < (numGlyphs + 1) * (2 + 2 * longOffsets)) {
            throw new NotSubset();
        }
        int[] starts = new int[numGlyphs];
        int[] ends = new int[numGlyphs];
        for (int gid = 0; gid < numGlyphs; gid++) {
            int start, end;
            if (longOffsets == 1) {
                start = U32(ttf, loca.offset + 4 * gid);
                end = U32(ttf, loca.offset + 4 * gid + 4);
            } else {
                start = 2 * U16(ttf, loca.offset + 2 * gid);
                end = 2 * U16(ttf, loca.offset + 2 * gid + 2);
            }
            starts[gid] = glyf.offset + start;
            ends[gid] = glyf.offset + end;
            if (start > end || end > glyf.length) {
                starts[gid] = -1; // Read only if kept
            }
        }

        // The glyphs kept, with the parts of the composite glyphs.
        bool[] keep = new bool[numGlyphs];
        int[] stack = new int[numGlyphs];
        int top = 0;
        for (int gid = 0; gid < numGlyphs && gid < used.Length; gid++) {
            if (used[gid] || gid == 0) {
                keep[gid] = true;
                stack[top++] = gid;
            }
        }
        while (top > 0) {
            int gid = stack[--top];
            int start = starts[gid];
            int end = ends[gid];
            if (start < 0) {
                throw new NotSubset();
            }
            if (end - start < 10) {
                continue; // No outline
            }
            if (U16(ttf, start) < 0x8000) {
                continue; // A simple glyph
            }
            int at = start + 10;
            while (true) {
                int flags = U16(ttf, at);
                int component = U16(ttf, at + 2);
                if (at + 4 > end || component >= numGlyphs) {
                    throw new NotSubset();
                }
                if (!keep[component]) {
                    keep[component] = true;
                    stack[top++] = component;
                }
                at += 4;
                at += (flags & 0x0001) != 0 ? 4 : 2;     // ARG_1_AND_2_ARE_WORDS
                if ((flags & 0x0008) != 0) {            // WE_HAVE_A_SCALE
                    at += 2;
                } else if ((flags & 0x0040) != 0) {     // WE_HAVE_AN_X_AND_Y_SCALE
                    at += 4;
                } else if ((flags & 0x0080) != 0) {     // WE_HAVE_A_TWO_BY_TWO
                    at += 8;
                }
                if ((flags & 0x0020) == 0) {            // MORE_COMPONENTS
                    break;
                }
            }
        }

        // The glyf and loca tables, the kept glyphs copied, each padded to a
        // multiple of four bytes, the others empty.
        int glyfLength = 0;
        for (int gid = 0; gid < numGlyphs; gid++) {
            if (keep[gid]) {
                glyfLength += (ends[gid] - starts[gid] + 3) & ~3;
            }
        }
        if (longOffsets == 0 && glyfLength > 0x1FFFE) {
            throw new NotSubset();
        }
        byte[] newGlyf = new byte[glyfLength];
        byte[] newLoca = new byte[(numGlyphs + 1) * (2 + 2 * longOffsets)];
        int position = 0;
        for (int gid = 0; gid <= numGlyphs; gid++) {
            if (longOffsets == 1) {
                PutInt(newLoca, 4 * gid, position);
            } else {
                newLoca[2 * gid] = (byte) (position >> 9);
                newLoca[2 * gid + 1] = (byte) (position >> 1);
            }
            if (gid < numGlyphs && keep[gid]) {
                Array.Copy(ttf, starts[gid], newGlyf, position, ends[gid] - starts[gid]);
                position += (ends[gid] - starts[gid] + 3) & ~3;
            }
        }

        // The font again, its tables in the order of their tags.
        tables.Sort((a, b) => String.CompareOrdinal(a.tag, b.tag));
        List<String> tags = new List<String>();
        List<byte[]> data = new List<byte[]>();
        foreach (Table t in tables) {
            if (t.tag == "glyf") {
                data.Add(newGlyf);
            } else if (t.tag == "loca") {
                data.Add(newLoca);
            } else if (t.tag == "head") {
                byte[] h = Slice(ttf, t.offset, t.length);
                h[8] = h[9] = h[10] = h[11] = 0; // checkSumAdjustment, set below
                data.Add(h);
            } else if (t.tag == "post") {
                if (t.length < 32) {
                    continue;
                }
                // Version 3, without the names of the glyphs.
                byte[] p = Slice(ttf, t.offset, 32);
                p[0] = 0;
                p[1] = 3;
                p[2] = 0;
                p[3] = 0;
                data.Add(p);
            } else if (SubsetTables.Contains(t.tag)) {
                data.Add(Slice(ttf, t.offset, t.length));
            } else {
                continue;
            }
            tags.Add(t.tag);
        }
        int count = data.Count;
        int searchRange = 1;
        int entrySelector = 0;
        while (searchRange * 2 <= count) {
            searchRange *= 2;
            entrySelector++;
        }
        int total = 12 + 16 * count;
        foreach (byte[] d in data) {
            total += (d.Length + 3) & ~3;
        }
        byte[] output = new byte[total];
        Array.Copy(ttf, 0, output, 0, 4);
        PutShort(output, 4, count);
        PutShort(output, 6, 16 * searchRange);
        PutShort(output, 8, entrySelector);
        PutShort(output, 10, 16 * count - 16 * searchRange);
        position = 12 + 16 * count;
        int headAt = 0;
        for (int i = 0; i < count; i++) {
            byte[] d = data[i];
            int entry = 12 + 16 * i;
            for (int j = 0; j < 4; j++) {
                output[entry + j] = (byte) tags[i][j];
            }
            PutInt(output, entry + 4, unchecked((int) TableChecksum(d)));
            PutInt(output, entry + 8, position);
            PutInt(output, entry + 12, d.Length);
            if (tags[i] == "head") {
                headAt = position;
            }
            Array.Copy(d, 0, output, position, d.Length);
            position += (d.Length + 3) & ~3;
        }
        PutInt(output, headAt + 8, unchecked((int) (0xB1B0AFBAu - TableChecksum(output))));
        kept = keep;
        return output;
    }

    private static byte[] Slice(byte[] buf, int offset, int length) {
        byte[] slice = new byte[length];
        Array.Copy(buf, offset, slice, 0, length);
        return slice;
    }

    private static void PutShort(byte[] buf, int at, int value) {
        buf[at] = (byte) (value >> 8);
        buf[at + 1] = (byte) value;
    }

    private static void PutInt(byte[] buf, int at, int value) {
        buf[at] = (byte) (value >> 24);
        buf[at + 1] = (byte) (value >> 16);
        buf[at + 2] = (byte) (value >> 8);
        buf[at + 3] = (byte) value;
    }

    /// <summary>
    /// Returns the sum of the big-endian 32-bit words of the data, the last
    /// one padded with zeros.
    /// </summary>
    internal static uint TableChecksum(byte[] data) {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 4) {
            uint word = 0;
            for (int j = 0; j < 4; j++) {
                word <<= 8;
                if (i + j < data.Length) {
                    word |= data[i + j];
                }
            }
            unchecked {
                sum += word;
            }
        }
        return sum;
    }
}   // End of Subset.cs
}   // End of package PDFjet.NET
