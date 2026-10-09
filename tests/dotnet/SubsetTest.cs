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
        return File.ReadAllBytes(TestSupport.RepoPath(path));
    }

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
        // In Noto Sans, Ä (134) is made of A (36) and the dieresis (106), and
        // ǅ (913) of D (39), z (93) and the caron (331).
        byte[] ttf = FontBytes("fonts/NotoSans/NotoSans-Regular.ttf");
        bool[] kept;
        byte[] subset = Subset.SubsetTrueType(ttf, Used(134, 913), out kept);
        Assert.Equal(4503, kept.Length);
        HashSet<int> want = new HashSet<int> {0, 134, 36, 106, 913, 39, 93, 331};
        for (int gid = 0; gid < kept.Length; gid++) {
            Assert.True(want.Contains(gid) == kept[gid], "glyph " + gid);
            byte[] glyph = Glyph(subset, gid);
            if (kept[gid]) {
                // The glyph as it was, padded to four bytes.
                byte[] whole = Glyph(ttf, gid);
                Assert.Equal(whole, Slice(glyph, 0, whole.Length));
                Assert.True(glyph.Length - whole.Length < 4);
            } else {
                Assert.Empty(glyph);
            }
        }
        Assert.True(subset.Length <= ttf.Length / 4, "the subset is " + subset.Length + " bytes");
        // The other tables a reader needs as they were; shaping left out.
        foreach (string name in new string[] {"GPOS", "GSUB", "GDEF"}) {
            Assert.Equal(-1, Entry(subset, name));
        }
        Assert.Equal(0x00030000, U32(subset, Table(subset, "post")));
        foreach (string name in new string[] {"cmap", "hmtx", "hhea", "name", "OS/2", "maxp"}) {
            int length = U32(ttf, Entry(ttf, name) + 12);
            Assert.Equal(Slice(ttf, Table(ttf, name), length), Slice(subset, Table(subset, name), length));
        }
        // The whole font sums to the magic number of its head table.
        Assert.Equal(0xB1B0AFBAu, Subset.TableChecksum(subset));
    }

    [Fact]
    public void SubsetsAFontWithShortOffsets() {
        byte[] ttf = FontBytes("fonts/NotoSansThai/NotoSansThai-Regular.ttf");
        Assert.Equal(0, U16(ttf, Table(ttf, "head") + 50));
        bool[] kept;
        byte[] subset = Subset.SubsetTrueType(ttf, Used(5), out kept);
        byte[] whole = Glyph(ttf, 5);
        Assert.Equal(whole, Slice(Glyph(subset, 5), 0, whole.Length));
        Assert.Empty(Glyph(subset, 6));
    }

    [Fact]
    public void IsRefusedByAFontWhoseLicenseForbidsIt() {
        byte[] ttf = FontBytes("fonts/NotoSans/NotoSans-Regular.ttf");
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

    private const string Tagged = "/BaseFont /([A-Z]{6}\\+NotoSans-Regular)\n";

    [Fact]
    public void ATrueTypeFontIsEmbeddedAsASubsetUnderATaggedName() {
        foreach (string path in new string[] {"fonts/NotoSans/NotoSans-Regular.ttf"}) {
            string raw = Doc(Compliance.PDF_1_7, path, true, "Ä");
            Match m = Regex.Match(raw, Tagged);
            Assert.True(m.Success, path);
            Assert.Contains("/FontName /" + m.Groups[1].Value + "\n", raw);
            Assert.Equal(2, Regex.Matches(raw, Tagged).Count);
            byte[] program = Program(raw);
            Assert.True(Glyph(program, 134).Length > 0 && Glyph(program, 36).Length > 0, path);
            Assert.Empty(Glyph(program, 37));
            // The widths of the glyphs kept: .notdef, A, the dieresis and Ä.
            Assert.Matches("/W \\[\n0\\[\\d+ \\]\n36\\[\\d+ \\]\n106\\[\\d+ \\]\n134\\[\\d+ \\]\\]\n", raw);
            Assert.DoesNotContain("/CIDSet", raw);
        }
    }

    [Fact]
    public void AFontSetToStayWholeIsEmbeddedWhole() {
        byte[] ttf = FontBytes("fonts/NotoSans/NotoSans-Regular.ttf");
        foreach (string path in new string[] {"fonts/NotoSans/NotoSans-Regular.ttf"}) {
            string raw = Doc(Compliance.PDF_1_7, path, false, "Ä");
            Assert.Contains("/BaseFont /NotoSans-Regular\n", raw);
            Assert.DoesNotContain("+NotoSans", raw);
            Assert.Equal(ttf, Program(raw));
        }
    }

    [Fact]
    public void TwoFontsOfOneFileShareOneSubset() {
        string raw = Doc(Compliance.PDF_1_7, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A", "B");
        Assert.Single(Regex.Matches(raw, "/Length1 "));
        byte[] program = Program(raw);
        Assert.True(Glyph(program, 36).Length > 0 && Glyph(program, 37).Length > 0);
        // Two Type0 fonts and their CID font.
        Assert.Equal(3, Regex.Matches(raw, Tagged).Count);
    }

    [Fact]
    public void APDFA1HasTheCIDSetOfTheGlyphsKept() {
        string raw = Doc(Compliance.PDF_A_1B, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A");
        Match m = Regex.Match(raw, "/CIDSet (\\d+) 0 R\n");
        Assert.True(m.Success, "no CIDSet");
        int at = raw.IndexOf("\n" + m.Groups[1].Value + " 0 obj\n");
        Assert.True(at != -1);
        Match s = new Regex("/Length (\\d+)\n>>\nstream\n").Match(raw, at);
        Assert.True(s.Success);
        byte[] bits = Inflate(raw, s.Index + s.Length, int.Parse(s.Groups[1].Value));
        // Glyphs 0 and 36 of 4503.
        byte[] want = new byte[(4503 + 7) / 8];
        want[0] = 0x80;
        want[36 / 8] |= (byte) (0x80 >> (36 % 8));
        Assert.Equal(want, bits);
    }
}
}
