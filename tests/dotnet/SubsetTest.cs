/*
 * SubsetTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
public class SubsetTest {
    private static byte[] FontBytes(string path) {
        return TestSupport.FontBytes(path);
    }

    // IBM Plex Sans, which has the short offsets of loca; the subsets of a
    // font with the long ones read Cascadia Mono's .ttf or IBM Plex Math.
    private const string PLEX = "tests/fonts/IBMPlexSans-Regular.ttf";

    private static int U16(byte[] font, int at) {
        return font[at] << 8 | font[at + 1];
    }

    private static int U32(byte[] font, int at) {
        return font[at] << 24 | font[at + 1] << 16 | font[at + 2] << 8 | font[at + 3];
    }

    // Returns where the directory entry of the table begins, or -1.
    private static int Entry(byte[] font, string name) {
        for (int i = 0; i < U16(font, 4); i++) {
            int entry = 12 + 16 * i;
            if (TestSupport.Latin1(Slice(font, entry, 4)) == name) {
                return entry;
            }
        }
        return -1;
    }

    private static int Table(byte[] font, string name) {
        int entry = Entry(font, name);
        Assert.True(entry != -1, "the font has no " + name + " table");
        return U32(font, entry + 8);
    }

    private static byte[] Slice(byte[] buf, int offset, int length) {
        byte[] slice = new byte[length];
        Array.Copy(buf, offset, slice, 0, length);
        return slice;
    }

    // Returns the bytes of the glyph in the glyf table, at the offsets its
    // loca table gives.
    private static byte[] Glyph(byte[] font, int gid) {
        int loca = Table(font, "loca");
        int glyf = Table(font, "glyf");
        int start, end;
        if (U16(font, Table(font, "head") + 50) == 1) {
            start = U32(font, loca + 4 * gid);
            end = U32(font, loca + 4 * gid + 4);
        } else {
            start = 2 * U16(font, loca + 2 * gid);
            end = 2 * U16(font, loca + 2 * gid + 2);
        }
        return Slice(font, glyf + start, end - start);
    }

    private static bool[] Used(params int[] gids) {
        bool[] used = new bool[0x10000];
        foreach (int gid in gids) {
            used[gid] = true;
        }
        return used;
    }

    [Fact]
    public void KeepsTheGlyphsUsedAndThePartsOfTheComposites() {
        // In Cascadia Mono's .ttf, whose loca has the long offsets, as that of Noto
        // Sans, which this test read before 9.1.7, had: Ä (16) is made of A
        // (1) and the dieresis of capitals (3051), and ặ (229) of a (225),
        // the dot below (3029) and the breve (3020); 4,319 glyphs. Noto Sans
        // had Ä (134) of A (36) and the dieresis (106), and ǅ (913) of three.
        byte[] ttf = FontBytes("tests/fonts/CascadiaMono-Regular.ttf");
        Assert.Equal(1, U16(ttf, Table(ttf, "head") + 50));
        bool[] kept;
        byte[] subset = Subset.SubsetTrueType(ttf, Used(16, 229), out kept);
        // kept is of the numbers in the font, up to the last glyph kept, 3051.
        Assert.Equal(3052, kept.Length);
        HashSet<int> want = new HashSet<int> {0, 16, 1, 3051, 229, 225, 3029, 3020};
        for (int gid = 0; gid < kept.Length; gid++) {
            Assert.True(want.Contains(gid) == kept[gid], "glyph " + gid);
        }
        // The glyphs kept, numbered again in their order: 0, 1, 16, 225, 229,
        // 3020, 3029 and 3051 are 0 to 7 in the subset, which has those alone.
        int[] olds = {0, 1, 16, 225, 229, 3020, 3029, 3051};
        Assert.Equal(olds.Length, U16(subset, Table(subset, "maxp") + 4));
        Assert.Equal(olds.Length, U16(subset, Table(subset, "hhea") + 34));
        Assert.Equal(4 * olds.Length, U32(subset, Entry(subset, "hmtx") + 12));
        int metrics = U16(ttf, Table(ttf, "hhea") + 34);
        for (int n = 0; n < olds.Length; n++) {
            int old = olds[n];
            byte[] whole = Glyph(ttf, old);
            byte[] glyph = Glyph(subset, n);
            Assert.True(glyph.Length >= whole.Length && glyph.Length - whole.Length < 4, "glyph " + old);
            if (old == 16 || old == 229) {
                // A composite glyph, its parts named by their new numbers: Ä
                // of A (1) and the dieresis (3051), 1 and 7; ặ of a (225),
                // the dot below (3029) and the breve (3020), 3, 6 and 5.
                List<int> parts = old == 16 ? new List<int> {1, 7} : new List<int> {3, 5, 6};
                List<int> found = Components(glyph);
                found.Sort();
                Assert.Equal(parts, found);
            } else {
                // The glyph as it was, padded to four bytes.
                Assert.Equal(whole, Slice(glyph, 0, whole.Length));
            }
            // Its advance and left side bearing as they were
            int advance = U16(ttf, Table(ttf, "hmtx") + 4 * Math.Min(old, metrics - 1));
            int lsb = old < metrics ? U16(ttf, Table(ttf, "hmtx") + 4 * old + 2)
                    : U16(ttf, Table(ttf, "hmtx") + 4 * metrics + 2 * (old - metrics));
            Assert.Equal(advance, U16(subset, Table(subset, "hmtx") + 4 * n));
            Assert.Equal(lsb, U16(subset, Table(subset, "hmtx") + 4 * n + 2));
        }
        Assert.True(subset.Length <= ttf.Length / 4, "the subset is " + subset.Length + " bytes");
        // The other tables a reader needs as they were; shaping left out, and
        // the cmap and name tables, which a CIDFontType2 does not need.
        foreach (string name in new string[] {"GPOS", "GSUB", "GDEF", "cmap", "name"}) {
            Assert.Equal(-1, Entry(subset, name));
        }
        Assert.Equal(0x00030000, U32(subset, Table(subset, "post")));
        // The OS/2 table as it was, and the hhea and maxp tables but for
        // their counts, of the long metrics and of the glyphs.
        foreach (string name in new string[] {"hhea", "OS/2", "maxp"}) {
            int length = U32(subset, Entry(subset, name) + 12);
            byte[] was = Slice(ttf, Table(ttf, name), length);
            byte[] now = Slice(subset, Table(subset, name), length);
            if (name == "maxp") {
                now[4] = was[4];
                now[5] = was[5];
            } else if (name == "hhea") {
                now[34] = was[34];
                now[35] = was[35];
            }
            Assert.Equal(was, now);
        }
        // The whole font sums to the magic number of its head table.
        Assert.Equal(0xB1B0AFBAu, Subset.TableChecksum(subset));
    }

    [Fact]
    public void SubsetsAFontWithShortOffsets() {
        // IBM's .ttf of IBM Plex Sans, whose loca has the short offsets, as
        // IBM Plex Sans Thai's .ttf and Noto Sans Thai's had.
        byte[] ttf = FontBytes(PLEX);
        Assert.Equal(0, U16(ttf, Table(ttf, "head") + 50));
        bool[] kept;
        byte[] subset = Subset.SubsetTrueType(ttf, Used(5), out kept);
        // Glyph 5, the second of the subset, after .notdef
        byte[] whole = Glyph(ttf, 5);
        Assert.Equal(2, U16(subset, Table(subset, "maxp") + 4));
        Assert.Equal(whole, Slice(Glyph(subset, 1), 0, whole.Length));
    }

    [Fact]
    public void NumbersTheGlyphsKeptAgain() {
        // IBM Plex Sans has 1,019 glyphs; a subset of glyph 33, A, has two,
        // .notdef and A, a long metric for each, and kept is of the numbers
        // in the font, up to 33. The map of the CIDs to them gives A, CID 33,
        // the glyph 1.
        byte[] ttf = FontBytes(PLEX);
        Assert.Equal(1019, U16(ttf, Table(ttf, "maxp") + 4));
        Assert.True(U16(ttf, Table(ttf, "hhea") + 34) > 33);
        bool[] kept;
        byte[] subset = Subset.SubsetTrueType(ttf, Used(33), out kept);
        Assert.Equal(34, kept.Length);
        Assert.Equal(2, U16(subset, Table(subset, "maxp") + 4));
        Assert.Equal(2, U16(subset, Table(subset, "hhea") + 34));
        Assert.Equal(8, U32(subset, Entry(subset, "hmtx") + 12));
        Assert.Equal(2 * 3, U32(subset, Entry(subset, "loca") + 12));
        // The width and side bearing of A as they were
        Assert.Equal(U32(ttf, Table(ttf, "hmtx") + 4 * 33), U32(subset, Table(subset, "hmtx") + 4));
        byte[] whole = Glyph(ttf, 33);
        Assert.Equal(whole, Slice(Glyph(subset, 1), 0, whole.Length));
        byte[] map = FontWriter.CIDToGIDMap(kept);
        Assert.Equal(2 * 34, map.Length);
        for (int cid = 0; cid < 34; cid++) {
            Assert.Equal(cid == 33 ? 1 : 0, U16(map, 2 * cid));
        }
    }

    // The glyph numbers of the parts of a composite glyph, in their order.
    private static List<int> Components(byte[] glyph) {
        List<int> parts = new List<int>();
        int at = 10;
        while (true) {
            int flags = U16(glyph, at);
            parts.Add(U16(glyph, at + 2));
            at += 4;
            at += (flags & 0x0001) != 0 ? 4 : 2;
            if ((flags & 0x0008) != 0) {
                at += 2;
            } else if ((flags & 0x0040) != 0) {
                at += 4;
            } else if ((flags & 0x0080) != 0) {
                at += 8;
            }
            if ((flags & 0x0020) == 0) {
                return parts;
            }
        }
    }

    [Fact]
    public void IsRefusedByAFontWhoseLicenseForbidsIt() {
        byte[] ttf = FontBytes(PLEX);
        int os2 = Table(ttf, "OS/2");
        ttf[os2 + 8] = 0x01;
        ttf[os2 + 9] = 0x00;
        bool[] kept;
        Assert.Throws<Subset.NotSubset>(() => Subset.SubsetTrueType(ttf, Used(36), out kept));
    }

    // Draws the texts, each with a font made from the file, and returns the
    // document.
    private static string Doc(Compliance compliance, string path, bool subset, params string[] texts) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, compliance).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        for (int i = 0; i < texts.Length; i++) {
            Font font = new Font(pdf, TestSupport.RepoPath(path));
            font.SetSubset(subset);
            new TextLine(font, texts[i]).SetLocation(50f, 50f + 20f * i).DrawOn(page);
        }
        pdf.Complete();
        return TestSupport.Latin1(stream.ToArray());
    }

    private static byte[] Inflate(string raw, int at, int length) {
        byte[] data = Encoding.Latin1.GetBytes(raw.Substring(at, length));
        using MemoryStream input = new MemoryStream(data);
        using ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    // Returns the font program embedded in the document.
    private static byte[] Program(string raw) {
        Match m = Regex.Match(raw, "/Length1 (\\d+)\n(?:/Metadata \\d+ 0 R\n)?/Length (\\d+)\n>>\nstream\n");
        Assert.True(m.Success, "no font program");
        byte[] program = Inflate(raw, m.Index + m.Length, int.Parse(m.Groups[2].Value));
        Assert.Equal(int.Parse(m.Groups[1].Value), program.Length);
        return program;
    }

    private const string Tagged = "/BaseFont /([A-Z]{6}\\+IBMPlexSans)\n";

    [Fact]
    public void ATrueTypeFontIsEmbeddedAsASubsetUnderATaggedName() {
        foreach (string path in new string[] {PLEX}) {
            string raw = Doc(Compliance.PDF_1_7, path, true, "Ä");
            Match m = Regex.Match(raw, Tagged);
            Assert.True(m.Success, path);
            Assert.Contains("/FontName /" + m.Groups[1].Value + "\n", raw);
            Assert.Equal(2, Regex.Matches(raw, Tagged).Count);
            byte[] program = Program(raw);
            // In IBM Plex Sans, Ä (395) is made of A (33) and the dieresis
            // of capitals (971), and B is 34. Noto Sans had Ä (134), A (36),
            // the dieresis (106) and B (37).
            // The subset has the four glyphs kept, numbered again: .notdef,
            // A, Ä and the dieresis, 0 to 3, Ä of 1 and 3; the CIDs are their
            // numbers in the font, which the CIDToGIDMap maps to them.
            Assert.Equal(4, U16(program, Table(program, "maxp") + 4));
            for (int gid = 1; gid < 4; gid++) {
                Assert.True(Glyph(program, gid).Length > 0, path + ": glyph " + gid);
            }
            Assert.Equal(new List<int> {1, 3}, Components(Glyph(program, 2)));
            Assert.Matches("/CIDToGIDMap \\d+ 0 R\n", raw);
            // The widths of the glyphs kept: .notdef, A, Ä and the dieresis.
            Assert.Matches("/W \\[\n0\\[\\d+ \\]\n33\\[\\d+ \\]\n395\\[\\d+ \\]\n971\\[\\d+ \\]\\]\n", raw);
            Assert.DoesNotContain("/CIDSet", raw);
        }
    }

    [Fact]
    public void AFontSetToStayWholeIsEmbeddedWhole() {
        byte[] ttf = FontBytes(PLEX);
        foreach (string path in new string[] {PLEX}) {
            string raw = Doc(Compliance.PDF_1_7, path, false, "Ä");
            Assert.Contains("/BaseFont /IBMPlexSans\n", raw);
            Assert.DoesNotContain("+IBMPlexSans", raw);
            Assert.Equal(ttf, Program(raw));
        }
    }

    [Fact]
    public void TwoFontsOfOneFileShareOneSubset() {
        string raw = Doc(Compliance.PDF_1_7, PLEX, true, "A", "B");
        Assert.Single(Regex.Matches(raw, "/Length1 "));
        byte[] program = Program(raw);
        // A and B, 33 and 34 in IBM Plex Sans, 1 and 2 in the subset, after
        // .notdef; 36 and 37 in Noto Sans.
        Assert.Equal(3, U16(program, Table(program, "maxp") + 4));
        Assert.True(Glyph(program, 1).Length > 0 && Glyph(program, 2).Length > 0);
        // Two Type0 fonts and their CID font.
        Assert.Equal(3, Regex.Matches(raw, Tagged).Count);
    }

    [Fact]
    public void APDFA1HasTheCIDSetOfTheGlyphsKept() {
        string raw = Doc(Compliance.PDF_A_1B, PLEX, true, "A");
        Match m = Regex.Match(raw, "/CIDSet (\\d+) 0 R\n");
        Assert.True(m.Success, "no CIDSet");
        int at = raw.IndexOf("\n" + m.Groups[1].Value + " 0 obj\n");
        Assert.True(at != -1);
        Match s = new Regex("/Length (\\d+)\n>>\nstream\n").Match(raw, at);
        Assert.True(s.Success);
        byte[] bits = Inflate(raw, s.Index + s.Length, int.Parse(s.Groups[1].Value));
        // Glyphs 0 and 33, A, of IBM Plex Sans, whose subset ends at 33; Noto
        // Sans had A at 36 of 4,503.
        byte[] want = new byte[(33 + 1 + 7) / 8];
        want[0] = 0x80;
        want[33 / 8] |= (byte) (0x80 >> (33 % 8));
        Assert.Equal(want, bits);
    }

    // Adds the font to the objects of a PDF, draws a word with it on the page,
    // and returns the objects of the PDF written.
    private static List<PDFobj> SubsetInExistingPDF(string path, bool subset) {
        List<PDFobj> objects = TestSupport.Read(PDFTest.PdfWithObjects(new string[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"}));
        Font font = new Font(objects, TestSupport.Open(path)).SetSubset(subset);
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, pdf.GetPageObjects(objects)[0]);
        page.AddResource(font, objects);
        page.DrawString(font, null, 12f, "Hello", 72f, 72f);
        page.Complete(objects);
        pdf.AddObjects(objects);
        pdf.Complete();
        return TestSupport.Read(stream.ToArray());
    }

    // Returns the /BaseFont of the Type0 font.
    private static string SubsetFontName(List<PDFobj> objects) {
        foreach (PDFobj obj in objects) {
            if (obj.GetValue("/Subtype") == "/Type0") {
                return obj.GetValue("/BaseFont");
            }
        }
        throw new Exception("no Type0 font");
    }

    [Fact]
    public void SubsetOfATrueTypeFontAddedToAnExistingPDF() {
        string path = PLEX;
        int whole = FontBytes(path).Length;
        List<PDFobj> objects = SubsetInExistingPDF(path, true);
        string name = SubsetFontName(objects);
        Assert.Matches(@"^/[A-Z]{6}\+IBMPlexSans$", name);
        Assert.Equal(name, TestSupport.FindObject(objects, "/FontName").GetValue("/FontName"));
        Assert.Equal(name, TestSupport.FindObject(objects, "/CIDToGIDMap").GetValue("/BaseFont"));
        PDFobj file = TestSupport.FindObject(objects, "/Length1");
        int length1 = Int32.Parse(file.GetValue("/Length1"));
        Assert.True(length1 > 0 && length1 < whole, "/Length1 " + length1);
        Assert.Equal(length1, file.GetData().Length);
        foreach (PDFobj obj in objects) {
            byte[] data = obj.GetData();
            string cmap = data == null ? "" : Encoding.Latin1.GetString(data);
            if (cmap.StartsWith("/CIDInit", StringComparison.Ordinal)) {
                // The codespace range, .notdef and H, e, l, o
                Assert.Equal(6, cmap.Split("> <").Length - 1);
                return;
            }
        }
        throw new Exception("no ToUnicode map");
    }

    [Fact]
    public void SubsetOfACFFFontAddedToAnExistingPDF() {
        List<PDFobj> objects = SubsetInExistingPDF("tests/fonts/IBMPlexSans-Regular.otf", true);
        Assert.Matches(@"^/[A-Z]{6}\+IBMPlexSans$", SubsetFontName(objects));
        Assert.NotNull(TestSupport.FindObject(objects, "/FontFile3"));
    }

    [Fact]
    public void SetSubsetFalseKeepsAFontAddedToAnExistingPDFWhole() {
        string path = PLEX;
        List<PDFobj> objects = SubsetInExistingPDF(path, false);
        Assert.Equal("/IBMPlexSans", SubsetFontName(objects));
        Assert.Equal(FontBytes(path).Length,
                Int32.Parse(TestSupport.FindObject(objects, "/Length1").GetValue("/Length1")));
    }

    // Checks that the font, made wrong, is not subset, and so embedded whole,
    // rather than an exception of another kind or a subset made of it.
    private static void AssertRefused(byte[] ttf, bool[] used) {
        bool[] kept;
        Assert.Throws<Subset.NotSubset>(() => Subset.SubsetTrueType(ttf, used, out kept));
    }

    private static void PutU16(byte[] buf, int at, int value) {
        buf[at] = (byte) (value >> 8);
        buf[at + 1] = (byte) value;
    }

    private static void PutU32(byte[] buf, int at, long value) {
        buf[at] = (byte) (value >> 24);
        buf[at + 1] = (byte) (value >> 16);
        buf[at + 2] = (byte) (value >> 8);
        buf[at + 3] = (byte) value;
    }

    [Fact]
    public void ATableTwiceIsRefused() {
        // The gasp table's entry made a second head table of 8 bytes, whose
        // checksum adjustment the subset wrote past its end (the review of
        // 9 October 2026).
        byte[] ttf = FontBytes(PLEX);
        int gasp = Entry(ttf, "gasp");
        Assert.True(gasp != -1);
        Encoding.ASCII.GetBytes("head").CopyTo(ttf, gasp);
        PutU32(ttf, gasp + 12, 8);
        AssertRefused(ttf, Used(36));
    }

    [Fact]
    public void ALocaFormatOtherThan0Or1IsRefused() {
        // Format 0xFFFF made the length of the loca table an int past its
        // range, and new byte[...] threw an OverflowException (the review of
        // 9 October 2026).
        byte[] ttf = FontBytes("tests/fonts/IBMPlexMath-Regular.ttf");
        PutU16(ttf, Table(ttf, "head") + 50, 0xFFFF);
        AssertRefused(ttf, Used(36));
    }

    [Fact]
    public void OverlappingGlyphsAreRefused() {
        // Every even glyph's outline the whole glyf table, so that a subset of
        // them copied it again for each (the review of 9 October 2026: 112 KB
        // made into 384 MB). A subset is never larger than the glyf table and the
        // padding of its glyphs.
        byte[] ttf = FontBytes(PLEX);
        int head = Table(ttf, "head");
        int loca = Table(ttf, "loca");
        int glyfLength = U32(ttf, Entry(ttf, "glyf") + 12);
        int numGlyphs = U16(ttf, Table(ttf, "maxp") + 4);
        bool longOffsets = U16(ttf, head + 50) == 1;
        bool[] used = new bool[0x10000];
        for (int gid = 0; gid <= numGlyphs; gid++) {
            int offset = gid % 2 == 1 ? glyfLength & ~3 : 0;
            if (longOffsets) {
                PutU32(ttf, loca + 4 * gid, offset);
            } else {
                PutU16(ttf, loca + 2 * gid, offset / 2);
            }
            if (gid % 2 == 0 && gid < numGlyphs) {
                used[gid] = true;
            }
        }
        AssertRefused(ttf, used);
    }

    [Fact]
    public void ABadLocaEntryOfAGlyphNotDrawnStillGivesASubset() {
        // A loca entry past 2^31 - 1 refused every subset of the font, read
        // for each glyph up front (the review of 9 October 2026); as in the
        // other ports, only a glyph kept that cannot be read refuses it.
        // Cascadia Mono's .ttf has the long offsets of loca, as Noto Sans, which
        // this test read before 9.1.7, had.
        byte[] ttf = FontBytes("tests/fonts/CascadiaMono-Regular.ttf");
        Assert.Equal(1, U16(ttf, Table(ttf, "head") + 50));
        int loca = Table(ttf, "loca");
        PutU32(ttf, loca + 4 * 1000, 0x80000000L);
        bool[] kept;
        byte[] subset = Subset.SubsetTrueType(ttf, Used(36), out kept);
        // kept ends at glyph 36, the last kept, before the bad entry, and the
        // subset has two glyphs, .notdef and glyph 36.
        Assert.True(kept.Length == 37 && kept[36]);
        byte[] whole = Glyph(FontBytes("tests/fonts/CascadiaMono-Regular.ttf"), 36);
        Assert.Equal(whole, Slice(Glyph(subset, 1), 0, whole.Length));
        // Drawn, glyph 1000 cannot be read, and the font is embedded whole.
        AssertRefused(ttf, Used(36, 1000));
    }

    // A TrueType subset is numbered again, so its CIDFont maps the CIDs to the
    // glyphs kept with a CIDToGIDMap stream; /Identity drew glyph 40 of a
    // subset of 6 for H (the review of 16 November 2026). The whole font keeps
    // /Identity.
    [Fact]
    public void SubsetOfATrueTypeFontAddedToAnExistingPDFMapsItsCIDs() {
        List<PDFobj> objects = SubsetInExistingPDF(PLEX, true);
        string value = TestSupport.FindObject(objects, "/CIDToGIDMap").GetValue("/CIDToGIDMap");
        Assert.NotEqual("/Identity", value);
        int number = Int32.Parse(value.Split(' ')[0]);
        byte[] m = null;
        foreach (PDFobj obj in objects) {
            if (obj.number == number) {
                m = obj.GetData();
            }
        }
        Assert.NotNull(m);
        // .notdef, H, e, l and o: the CIDs of H, e, l and o map to 1 to 4
        List<int> gids = new List<int>();
        for (int cid = 0; cid + 1 < m.Length; cid += 2) {
            int gid = m[cid] << 8 | m[cid + 1];
            if (gid != 0) {
                gids.Add(gid);
            }
        }
        Assert.Equal(new List<int> {1, 2, 3, 4}, gids);

        List<PDFobj> whole = SubsetInExistingPDF(PLEX, false);
        Assert.Equal("/Identity", TestSupport.FindObject(whole, "/CIDToGIDMap").GetValue("/CIDToGIDMap"));
    }
}
}
