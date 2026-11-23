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
/// Complete() with the glyphs the document drew alone. The PDF names each
/// glyph by its number in the whole font, its CID, so the widths, the
/// character map and the ToUnicode map are those of the whole font: a .ttf
/// subset numbers its glyphs again, and its CIDFont maps the CIDs to them
/// with a CIDToGIDMap; an .otf subset gives each glyph its number in the font
/// as its CID in its charset.
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
        internal readonly GlyphSet used;
        // The character first drawn with each glyph, by glyph number, for the
        // ToUnicode map of a glyph of several characters, as U+3000 and U+2003:
        // in blocks of 256 glyphs, each made when one of its glyphs is first
        // drawn, the character plus one, 0 for none, as in Java, where a map
        // of boxed numbers made each character drawn allocate (12 October 2026).
        private readonly int[][] chars = new int[256][];
        internal bool whole;        // Set by SetSubset(false): the font is embedded whole
        // The outlines of the glyphs, read when DrawGlyphs first draws one,
        // and the stamp of each glyph it drew, a form XObject of its outline
        // drawn once and placed wherever the glyph is; null for a glyph of no
        // outline, as the space.
        internal GlyphOutline outline;
        internal System.Collections.Generic.Dictionary<int, Stamp> glyphStamps;

        // Returns the character first drawn with the glyph, or -1.
        internal int FirstChar(int gid) {
            int[] block = chars[gid >> 8];
            return block == null ? -1 : block[gid & 0xFF] - 1;
        }

        // Records the character as the one drawn with the glyph, unless one
        // was, and returns the one drawn first, or -1 when it is this one.
        internal int FirstChar(int gid, int c) {
            int[] block = chars[gid >> 8];
            if (block == null) {
                block = new int[256];
                chars[gid >> 8] = block;
            }
            int first = block[gid & 0xFF] - 1;
            if (first < 0) {
                block[gid & 0xFF] = c + 1;
            }
            return first;
        }

        // The program of a font of the number of glyphs given, the count of its
        // maxp table, or 0 when it has none.
        internal Program(int glyphs) {
            used = new GlyphSet(glyphs);
            used.Add(0);            // .notdef, drawn for a character the font lacks
        }
    }

    // The set of the glyphs drawn with a font, a bit for each glyph number:
    // 128 bytes for the 1,019 glyphs of IBM Plex Sans, where a bool for each of
    // the 65,536 glyph numbers was 64 KB for every font of every document (the
    // owner's idea, 10 October 2026). It grows for a glyph past the font's
    // count, up to the 65,536 a glyph number can be, so that it keeps what the
    // array kept.
    internal sealed class GlyphSet {
        private ulong[] words;

        internal GlyphSet(int glyphs) {
            if (glyphs <= 0 || glyphs > 0x10000) {
                glyphs = 0x10000;   // A glyph number is written in four hexadecimal digits
            }
            words = new ulong[(glyphs + 63) / 64];
        }

        // Adds the glyph number to the set.
        internal void Add(int gid) {
            if (gid < 0 || gid >= 0x10000) {
                return;
            }
            int i = gid >> 6;
            if (i >= words.Length) {
                Array.Resize(ref words, i + 1);
            }
            words[i] |= 1UL << (gid & 63);
        }

        // The set as a bool for each glyph number up to the end of its last
        // word, for the subsetters, which read a glyph number's bool.
        internal bool[] Bools() {
            bool[] used = new bool[words.Length * 64];
            for (int gid = 0; gid < used.Length; gid++) {
                used[gid] = (words[gid >> 6] & (1UL << (gid & 63))) != 0;
            }
            return used;
        }
    }

    /// <summary>The font cannot be subset, and is embedded whole.</summary>
    internal sealed class NotSubset : Exception {
        internal NotSubset() : base("the font cannot be subset") {
        }
    }

    // The tables a subset keeps: those a PDF reader draws a TrueType font with
    // (ISO 32000-1, 9.9), and the OS/2 and post tables, which some readers and
    // checkers look at. The cmap and name tables are left out: a subset is
    // embedded as a CIDFontType2 with an Identity CIDToGIDMap, whose glyphs
    // the PDF names by their numbers, and the two were nearly a third of a subset of
    // IBM Plex Sans, more to compress for every document (26 October 2026).
    // The tables of shaping and of vertical text, which PDFjet reads from the
    // font and the PDF does not need, are left out, and so is the DSIG table,
    // the signature of the whole font.
    private static readonly HashSet<String> SubsetTables = new HashSet<String> {
        "head", "hhea", "hmtx", "maxp", "loca", "glyf",
        "cvt ", "fpgm", "prep", "gasp",
        "OS/2", "post"
    };

    /// <summary>
    /// Gives the font the program of a font the PDF already has from the same
    /// file, if any, or else a new one of the font given.
    /// </summary>
    internal static void Share(PDF pdf, Font font, byte[] program, bool cff, bool forbidden,
            int glyphs) {
        foreach (Font f in pdf.fonts) {
            if (f.program != null && f.name.Equals(font.name) && f.checksum == font.checksum) {
                font.program = f.program;
                return;
            }
        }
        font.program = new Program(glyphs);
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
            if (font.program == null || font.OutlinesOnly()) {
                continue;   // A font DrawGlyphs alone drew with, as outlines
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
                        CFFSubset.subset(data, program.used.Bools(), out kept)
                        : SubsetTrueType(data, program.used.Bools(), out kept);
                font.kept = kept;
                font.baseFont = SubsetTag(font.checksum, kept) + "+" + font.name;
                return subset;
            } catch (Exception e) when (Unreadable(e)) {
                // Embedded whole
            }
        }
        if (program.cff) {
            // Whole, written again for the identity charset of a CID-keyed
            // font, or as it is when it cannot be.
            try {
                bool[] all;
                data = CFFSubset.subset(data, null, out all);
            } catch (Exception e) when (Unreadable(e)) {
                // As it is
            }
        }
        font.kept = null;
        font.baseFont = font.name;
        return data;
    }

    // Whether the subsetters found the font one they cannot read: NotSubset,
    // or, as a backstop, an index, a length or a size out of range that a
    // check of theirs missed, which embeds the font whole rather than failing
    // the document (the review of 9 October 2026).
    private static bool Unreadable(Exception e) {
        return e is NotSubset || e is IndexOutOfRangeException ||
                e is OverflowException || e is ArgumentException;
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
        compressed = pdf.Encrypted(compressed);
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
        pdf.AppendStreamBody(compressed);
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

    // The unsigned 32-bit value of a loca entry, which can be past 2^31 - 1.
    private static long LocaU32(byte[] ttf, int at) {
        if (at < 0 || at + 4 > ttf.Length) {
            throw new NotSubset();
        }
        return (long) ttf[at] << 24 | (long) ttf[at + 1] << 16 | (long) ttf[at + 2] << 8 | ttf[at + 3];
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
    /// Returns the TrueType font of the glyphs used alone, and gives in kept
    /// the glyphs kept: those used, glyph 0 and the parts of the composite
    /// glyphs kept. The tables kept, SubsetTables, are copied as they are, but
    /// for the glyf, loca and hmtx tables made again, the counts of the maxp
    /// and hhea tables, the head table's checksum adjustment, and the post
    /// table without the names of the glyphs. The glyphs kept are numbered
    /// again, from 0, in the order of their numbers in the font, and the
    /// maxp, hhea and hmtx tables made for them, so that a subset of a font of
    /// thousands of glyphs carries no entry for each of them (Noto Sans's
    /// 4,503 glyphs were 36 KB of loca and hmtx for a page that drew 38); kept
    /// is of the numbers in the font, which the CIDToGIDMap maps to the new
    /// ones. A font that forbids
    /// subsetting in the fsType of its OS/2 table, or whose tables cannot be
    /// read, throws NotSubset, and is embedded whole.
    /// </summary>
    internal static byte[] SubsetTrueType(byte[] ttf, bool[] used, out bool[] kept) {
        int numTables = U16(ttf, 4);
        List<Table> tables = new List<Table>(numTables);
        Table head = null, loca = null, glyf = null, maxp = null, os2 = null, hhea = null, hmtx = null;
        HashSet<String> seen = new HashSet<String>();
        for (int i = 0; i < numTables; i++) {
            int entry = 12 + 16 * i;
            int offset = U32(ttf, entry + 8);
            int length = U32(ttf, entry + 12);
            if ((long) offset + length > ttf.Length) {
                throw new NotSubset();
            }
            Table table = new Table(new String(new char[] {
                    (char) ttf[entry], (char) ttf[entry + 1], (char) ttf[entry + 2], (char) ttf[entry + 3]}), offset, length);
            // A tag twice, as two head tables, one of them too short to copy,
            // is not a font to subset (the review of 9 October 2026)
            if (!seen.Add(table.tag)) {
                throw new NotSubset();
            }
            tables.Add(table);
            switch (table.tag) {
                case "head": head = table; break;
                case "loca": loca = table; break;
                case "glyf": glyf = table; break;
                case "maxp": maxp = table; break;
                case "OS/2": os2 = table; break;
                case "hhea": hhea = table; break;
                case "hmtx": hmtx = table; break;
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
        // A loca format other than 0 or 1 is refused before the length of the
        // table is worked out from it, which overflowed an int (the review of
        // 9 October 2026)
        if (numGlyphs == 0 || longOffsets > 1 || loca.length < (numGlyphs + 1) * (2 + 2 * longOffsets)) {
            throw new NotSubset();
        }
        int[] starts = new int[numGlyphs];
        int[] ends = new int[numGlyphs];
        for (int gid = 0; gid < numGlyphs; gid++) {
            int start, end;
            if (longOffsets == 1) {
                // An offset past 2^31 - 1 makes the glyph unreadable, refused
                // only if it is kept, not the whole subset (the review of
                // 9 October 2026)
                long start32 = LocaU32(ttf, loca.offset + 4 * gid);
                long end32 = LocaU32(ttf, loca.offset + 4 * gid + 4);
                if (start32 > int.MaxValue || end32 > int.MaxValue) {
                    starts[gid] = -1; // Read only if kept
                    continue;
                }
                start = (int) start32;
                end = (int) end32;
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

        // The glyphs of the subset, numbered again: the glyphs kept in the
        // order of their numbers in the font, glyph 0 first, each the next
        // number, so that a subset of a font of thousands of glyphs has an
        // entry in its loca and hmtx tables for each glyph it keeps alone.
        // The PDF names a glyph by its number in the font, its CID, which the
        // CIDToGIDMap of the CIDFont maps to its number here (FontWriter).
        // A font whose metrics cannot be read is not subset.
        int numberOfHMetrics = 0;
        if (hhea != null && hhea.length >= 36 && hmtx != null) {
            numberOfHMetrics = U16(ttf, hhea.offset + 34);
        }
        if (numberOfHMetrics == 0 || numberOfHMetrics > numGlyphs ||
                hmtx.length < 4 * numberOfHMetrics + 2 * (numGlyphs - numberOfHMetrics)) {
            throw new NotSubset();
        }
        int last = numGlyphs - 1;
        while (last > 0 && !keep[last]) {
            last--;
        }
        int[] newGid = new int[last + 1];
        int glyphs = 0;
        for (int gid = 0; gid <= last; gid++) {
            newGid[gid] = keep[gid] ? glyphs++ : -1;
        }

        // The glyf, loca and hmtx tables of the glyphs kept, each glyph
        // padded to a multiple of four bytes, a composite glyph's parts named
        // by their new numbers, and a long metric for each glyph.
        long glyfLength = 0;
        for (int gid = 0; gid <= last; gid++) {
            if (keep[gid]) {
                glyfLength += (ends[gid] - starts[gid] + 3) & ~3;
                // Glyphs whose outlines overlap, each copied again, could make
                // the subset larger than the whole font many times over (the
                // review of 9 October 2026: 112 KB into 384 MB); a subset is never
                // larger than the glyf table and the padding of each glyph.
                if (glyfLength > (long) glyf.length + 3L * glyphs) {
                    throw new NotSubset();
                }
            }
        }
        if (longOffsets == 0 && glyfLength > 0x1FFFE) {
            throw new NotSubset();
        }
        byte[] newGlyf = new byte[glyfLength];
        byte[] newLoca = new byte[(glyphs + 1) * (2 + 2 * longOffsets)];
        byte[] newHmtx = new byte[4 * glyphs];
        int position = 0;
        int n = 0;
        for (int gid = 0; gid <= last; gid++) {
            if (!keep[gid]) {
                continue;
            }
            PutLoca(newLoca, longOffsets, n, position);
            int length = ends[gid] - starts[gid];
            Array.Copy(ttf, starts[gid], newGlyf, position, length);
            if (length >= 10 && U16(ttf, starts[gid]) >= 0x8000) {
                RenumberComponents(newGlyf, position, length, newGid);
            }
            // The advance of a glyph past numberOfHMetrics is the last one's,
            // and its left side bearing follows the long metrics.
            int advance = U16(ttf, hmtx.offset + 4 * Math.Min(gid, numberOfHMetrics - 1));
            int lsb = (gid < numberOfHMetrics)
                    ? U16(ttf, hmtx.offset + 4 * gid + 2)
                    : U16(ttf, hmtx.offset + 4 * numberOfHMetrics + 2 * (gid - numberOfHMetrics));
            PutShort(newHmtx, 4 * n, advance);
            PutShort(newHmtx, 4 * n + 2, lsb);
            position += (length + 3) & ~3;
            n++;
        }
        PutLoca(newLoca, longOffsets, glyphs, position);

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
            } else if (t.tag == "maxp") {
                byte[] m = Slice(ttf, t.offset, t.length);
                PutShort(m, 4, glyphs);
                data.Add(m);
            } else if (t.tag == "hhea") {
                byte[] h = Slice(ttf, t.offset, t.length);
                PutShort(h, 34, glyphs);    // numberOfHMetrics: one for each glyph
                data.Add(h);
            } else if (t.tag == "hmtx") {
                data.Add(newHmtx);
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
        Array.Resize(ref keep, last + 1);
        kept = keep;
        return output;
    }

    private static byte[] Slice(byte[] buf, int offset, int length) {
        byte[] slice = new byte[length];
        Array.Copy(buf, offset, slice, 0, length);
        return slice;
    }

    // Writes the offset of the glyph in the glyf table into its entry of the
    // loca table, of four bytes or of two, the offset halved.
    private static void PutLoca(byte[] loca, int longOffsets, int gid, int offset) {
        if (longOffsets == 1) {
            PutInt(loca, 4 * gid, offset);
        } else {
            loca[2 * gid] = (byte) (offset >> 9);
            loca[2 * gid + 1] = (byte) (offset >> 1);
        }
    }

    // Names the parts of the composite glyph at start in the glyf table by
    // their new numbers, as each was kept with it.
    private static void RenumberComponents(byte[] glyf, int start, int length, int[] newGid) {
        int end = start + length;
        int at = start + 10;
        while (true) {
            int flags = U16(glyf, at);
            int component = U16(glyf, at + 2);
            if (at + 4 > end || component >= newGid.Length || newGid[component] < 0) {
                throw new NotSubset();
            }
            PutShort(glyf, at + 2, newGid[component]);
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
