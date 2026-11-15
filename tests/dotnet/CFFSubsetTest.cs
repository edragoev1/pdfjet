/*
 * CFFSubsetTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
public class CFFSubsetTest {
    private const string PLEX = "tests/fonts/IBMPlexSans-Regular.otf";
    private const string HAN = "tests/fonts/SourceHanSansJP-Regular.otf";

    private static OTF Otf(string path) {
        return new OTF(new MemoryStream(File.ReadAllBytes(TestSupport.RepoPath(path))));
    }

    private static byte[] Cff(OTF otf) {
        byte[] cff = new byte[otf.cffLen];
        Array.Copy(otf.buf, otf.cffOff, cff, 0, otf.cffLen);
        return cff;
    }

    // The charstrings, the global subroutines and the Top DICT of a CFF table.
    private static (List<byte[]>, List<byte[]>, List<CFFSubset.Entry>) Parts(byte[] cff) {
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2]);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        CFFSubset.Index strings = CFFSubset.readIndex(cff, tops.end);
        CFFSubset.Index globals = CFFSubset.readIndex(cff, strings.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        CFFSubset.Index charStrings = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS)[0]);
        return (CFFSubset.emptied(cff, charStrings, null), CFFSubset.emptied(cff, globals, null), top);
    }

    private static bool[] Used(OTF otf, string text) {
        bool[] used = new bool[0x10000];
        foreach (char ch in text) {
            used[otf.unicodeToGID.Get(ch)] = true;
        }
        return used;
    }

    // The CIDs the charset of the subset gives its glyphs after .notdef, of
    // formats 0, 1 and 2.
    private static List<int> Charset(byte[] cff, List<CFFSubset.Entry> top, int glyphs) {
        int at = CFFSubset.entryOf(top, CFFSubset.CHARSET)[0];
        int format = cff[at];
        at++;
        List<int> cids = new List<int>();
        while (cids.Count < glyphs - 1) {
            if (format == 0) {
                cids.Add(cff[at] << 8 | cff[at + 1]);
                at += 2;
            } else if (format == 1 || format == 2) {
                int first = cff[at] << 8 | cff[at + 1];
                int left = cff[at + 2];
                at += 3;
                if (format == 2) {
                    left = left << 8 | cff[at];
                    at++;
                }
                for (int cid = first; cid <= first + left; cid++) {
                    cids.Add(cid);
                }
            } else {
                Assert.Fail("charset format " + format);
            }
        }
        return cids;
    }

    [Fact]
    public void IsACIDFontOfTheGlyphsUsedAlone() {
        foreach (string path in new string[] {PLEX, HAN}) {
            OTF otf = Otf(path);
            byte[] cff = Cff(otf);
            bool[] used = Used(otf, "Hello 日本語");
            bool[] kept;
            byte[] subset = CFFSubset.subset(cff, used, out kept);
            var (whole, _, _) = Parts(cff);
            var (glyphs, globals, top) = Parts(subset);
            Assert.True(kept.Length == whole.Count, path + ": kept is of " + kept.Length + " glyphs");
            // The glyphs kept, .notdef and those used, alone, in their order
            List<int> keptGIDs = new List<int>();
            for (int gid = 0; gid < kept.Length; gid++) {
                Assert.True((gid == 0 || used[gid]) == kept[gid], path + " glyph " + gid);
                if (kept[gid] && gid > 0) {
                    keptGIDs.Add(gid);
                }
            }
            Assert.Equal(keptGIDs.Count + 1, glyphs.Count);
            // A CIDFont, whose charset gives each glyph its number in the font
            // as its CID, which the content streams draw it by
            Assert.NotNull(CFFSubset.entryOf(top, CFFSubset.ROS));
            Assert.NotNull(CFFSubset.entryOf(top, CFFSubset.FD_ARRAY));
            Assert.Null(CFFSubset.entryOf(top, CFFSubset.ENCODING));
            Assert.Equal(keptGIDs, Charset(subset, top, glyphs.Count));
            // No subroutines: the glyphs are written with them in place, and
            // drawn as in the whole font (WithItsSubroutinesInPlaceDrawsTheSameOutlines)
            Assert.True(globals.Count == 0, path + ": " + globals.Count + " global subroutines");
            Assert.True(subset.Length <= cff.Length / 10, path + ": " + subset.Length + " bytes");
        }
    }

    [Fact]
    public void OfEveryGlyphHasTheIdentityCharset() {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID:
        // written whole, each glyph's CID is its number.
        foreach (string path in new string[] {PLEX, HAN}) {
            byte[] cff = Cff(Otf(path));
            var (whole, _, _) = Parts(cff);
            bool[] kept;
            byte[] subset = CFFSubset.subset(cff, null, out kept);
            var (glyphs, _, top) = Parts(subset);
            Assert.Equal(whole.Count, glyphs.Count);
            Assert.Equal(whole.Count, kept.Length);
            List<int> cids = Charset(subset, top, glyphs.Count);
            for (int i = 0; i < cids.Count; i++) {
                Assert.True(cids[i] == i + 1, path + ": glyph " + (i + 1) + " has the CID " + cids[i]);
            }
            for (int gid = 0; gid < glyphs.Count; gid++) {
                Assert.Equal(whole[gid], glyphs[gid]);
            }
        }
    }

    private static string Doc(Compliance compliance, string path, bool subset, string text) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, compliance).SetTitle("Test");
        Font font = new Font(pdf, TestSupport.RepoPath(path));
        font.SetSubset(subset);
        new TextLine(font, text).SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        return TestSupport.Latin1(stream.ToArray());
    }

    private const string TAG = "/FontName /[A-Z]{6}\\+";

    [Fact]
    public void AFontWithCFFOutlinesIsEmbeddedAsASubset() {
        foreach (string path in new string[] {PLEX, HAN}) {
            string raw = Doc(Compliance.PDF_A_1B, path, true, "Hello 日本語");
            Assert.Matches(TAG, raw);
            Assert.Contains("/Subtype /CIDFontType0C\n", raw);
            Assert.Contains("/FontFile3 ", raw);
            Assert.DoesNotContain("/Length1 ", raw);
            Assert.Contains("/CIDSet ", raw);
        }
        string whole = Doc(Compliance.PDF_1_7, PLEX, false, "Hello");
        Assert.DoesNotMatch(TAG, whole);
        Assert.Contains("/FontFile3 ", whole);
    }

    [Fact]
    public void AFontWhoseLicenseForbidsSubsettingIsEmbeddedWhole() {
        byte[] data = File.ReadAllBytes(TestSupport.RepoPath(PLEX));
        int tables = data[4] << 8 | data[5];
        for (int i = 0; i < tables; i++) {
            int entry = 12 + 16 * i;
            if (TestSupport.Latin1(data[entry..(entry + 4)]) == "OS/2") {
                int at = data[entry + 8] << 24 | data[entry + 9] << 16 | data[entry + 10] << 8 | data[entry + 11];
                data[at + 8] = 0x01;
                data[at + 9] = 0x00;
            }
        }
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = new Font(pdf, new MemoryStream(data));
        new TextLine(font, "Hello").SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        Assert.DoesNotMatch(TAG, TestSupport.Latin1(stream.ToArray()));
    }

    [Fact]
    public void ASubsetOfAnAccentedLetterDrawnWithSeacIsRefused() {
        // Á's charstring made "0 0 65 194 endchar", seac: the A and the acute
        // it is drawn from were emptied, and it drew blank (the review of
        // 9 October 2026). The font is embedded whole.
        OTF otf = Otf(PLEX);
        byte[] cff = Cff(otf);
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2]);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        CFFSubset.Index charStrings = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS)[0]);
        int gid = otf.unicodeToGID.Get('Á');
        int at = charStrings.objects[gid];
        Assert.True(charStrings.objects[gid + 1] - at >= 6,
                "the charstring of Á is " + (charStrings.objects[gid + 1] - at) + " bytes");
        new byte[] {139, 139, 204, 247, 86, 14}.CopyTo(cff, at);
        bool[] kept;
        Assert.Throws<Subset.NotSubset>(() => CFFSubset.subset(cff, Used(otf, "Á"), out kept));
    }

    // The CFF table of a name-keyed font with its Top DICT written again with
    // the entries, before its own, and its offsets moved by as many bytes.
    private static byte[] WithTopDict(byte[] cff, params CFFSubset.Entry[] entries) {
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2]);
        CFFSubset.Index topDicts = CFFSubset.readIndex(cff, names.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, topDicts.objects[0], topDicts.objects[1]);
        Func<int, byte[]> written = (int delta) => {
            Dictionary<int, int> offsets = new Dictionary<int, int>();
            int privateSize = 0;
            foreach (CFFSubset.Entry entry in top) {
                if (entry.op == CFFSubset.CHARSET || entry.op == CFFSubset.CHAR_STRINGS) {
                    offsets[entry.op] = entry.integers[0] + delta;
                } else if (entry.op == CFFSubset.PRIVATE) {
                    privateSize = entry.integers[0];
                    offsets[entry.op] = entry.integers[1] + delta;
                }
            }
            List<CFFSubset.Entry> all = new List<CFFSubset.Entry>(entries);
            all.AddRange(top);
            return CFFSubset.writeDict(all, offsets, privateSize);
        };
        int delta = CFFSubset.index(new List<byte[]> {written(0)}).Length - (topDicts.end - topDicts.start);
        CFFSubset.ByteBuffer output = new CFFSubset.ByteBuffer();
        output.Add(cff[0..topDicts.start]);
        output.Add(CFFSubset.index(new List<byte[]> {written(delta)}));
        output.Add(cff[topDicts.end..]);
        return output.ToBytes();
    }

    // The FontMatrix entries of the Top DICT and of the one Font DICT.
    private static string Matrices(byte[] cff) {
        var (_, _, top) = Parts(cff);
        List<string> raws = new List<string>();
        foreach (CFFSubset.Entry entry in top) {
            if (entry.op == CFFSubset.FONT_MATRIX) {
                raws.Add(BitConverter.ToString(entry.raw));
            }
        }
        CFFSubset.Index fdArray = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.FD_ARRAY)[0]);
        Assert.True(fdArray.count() == 1, "not one Font DICT");
        foreach (CFFSubset.Entry entry in CFFSubset.readDict(cff, fdArray.objects[0], fdArray.objects[1])) {
            if (entry.op == CFFSubset.FONT_MATRIX) {
                raws.Add(BitConverter.ToString(entry.raw));
            }
        }
        return string.Join(" ", raws);
    }

    [Fact]
    public void OfANameKeyedFontWithAFontMatrixOfItsOwn() {
        // As Cascadia Mono's .otf, of 2048 units to the em, has: the matrix
        // stays in the Top DICT, and the Font DICT has the identity matrix,
        // the two concatenated the font's.
        OTF otf = Otf(PLEX);
        byte[] cff = Cff(otf);
        byte[] matrix = {30, 0xa0, 0x00, 0x48, 0x82, 0x8f};  // .00048828
        CFFSubset.ByteBuffer b = new CFFSubset.ByteBuffer();
        b.Add(matrix);
        b.Add(139);
        b.Add(139);
        b.Add(matrix);
        b.Add(new byte[] {139, 139, 12, 7});
        byte[] raw = b.ToBytes();
        byte[] font = WithTopDict(cff, new CFFSubset.Entry(CFFSubset.FONT_MATRIX, raw, null));
        var (whole, _, _) = Parts(font);
        string want = BitConverter.ToString(raw) + " " + BitConverter.ToString(CFFSubset.IDENTITY_MATRIX);
        byte[] subset = null;
        bool[] kept;
        foreach (bool[] used in new bool[][] {Used(otf, "Hello"), null}) {
            subset = CFFSubset.subset(font, used, out kept);
            var (glyphs, _, _) = Parts(subset);
            if (used == null) {
                Assert.Equal(whole.Count, glyphs.Count);
            }
            Assert.Equal(want, Matrices(subset));
        }
        // A CID-keyed font's matrices are kept as they are: the subset's
        // subset has them both again.
        byte[] again = CFFSubset.subset(subset, Used(otf, "He"), out kept);
        Assert.Equal(want, Matrices(again));
        // A name-keyed font of no FontMatrix, as IBM Plex Sans, has the
        // identity matrix in its Font DICT too, and the default written in
        // its Top DICT: no reader takes the default of 1000 units to the em
        // twice, or the Font DICT's matrix as the whole one.
        foreach (bool[] used in new bool[][] {Used(otf, "Hello"), null}) {
            subset = CFFSubset.subset(cff, used, out kept);
            Assert.Equal(BitConverter.ToString(CFFSubset.THOUSANDTH_MATRIX) + " " +
                    BitConverter.ToString(CFFSubset.IDENTITY_MATRIX), Matrices(subset));
        }
    }

    // The outline of each glyph of the CFF table, the points of its path
    // operators in order, drawn by its Type 2 charstring run with its
    // subroutines.
    private static List<string> Outlines(byte[] cff) {
        var (charStrings, globals, top) = Parts(cff);
        List<List<byte[]>> locals = new List<List<byte[]>>();
        int[] fdOf = null;
        int[] fdArrayAt = CFFSubset.entryOf(top, CFFSubset.FD_ARRAY);
        if (fdArrayAt != null) {
            CFFSubset.Index fdArray = CFFSubset.readIndex(cff, fdArrayAt[0]);
            for (int i = 0; i < fdArray.count(); i++) {
                List<CFFSubset.Entry> fd = CFFSubset.readDict(cff, fdArray.objects[i], fdArray.objects[i + 1]);
                locals.Add(Locals(cff, CFFSubset.entryOf(fd, CFFSubset.PRIVATE)));
            }
            int at = CFFSubset.entryOf(top, CFFSubset.FD_SELECT)[0];
            int length = CFFSubset.fdSelectLength(cff, at, charStrings.Count);
            fdOf = CFFSubset.fdOf(cff[at..(at + length)], charStrings.Count, locals.Count);
        } else {
            locals.Add(Locals(cff, CFFSubset.entryOf(top, CFFSubset.PRIVATE)));
        }
        List<string> outlines = new List<string>(charStrings.Count);
        for (int gid = 0; gid < charStrings.Count; gid++) {
            Type2 p = new Type2(globals, locals[(fdOf != null) ? fdOf[gid] : 0]);
            p.run(charStrings[gid], 0);
            outlines.Add(p.path.ToString());
        }
        return outlines;
    }

    // The local subroutines of the Private DICT.
    private static List<byte[]> Locals(byte[] cff, int[] p) {
        CFFSubset.PrivateDict block = CFFSubset.readPrivate(cff, p[0], p[1]);
        return (block.subrs == null) ? null : CFFSubset.emptied(cff, block.subrs, null);
    }

    // Draws a Type 2 charstring (Technical Note #5177) as the points of its
    // path, apart from the subsetter's code.
    private sealed class Type2 {
        private readonly List<byte[]> globals;
        private readonly List<byte[]> locals;
        private readonly List<double> stack = new List<double>();
        private double x;
        private double y;
        private int stems;
        private bool width;     // Looked for already
        private bool ended;
        internal readonly System.Text.StringBuilder path = new System.Text.StringBuilder();

        internal Type2(List<byte[]> globals, List<byte[]> locals) {
            this.globals = globals;
            this.locals = locals;
        }

        private static string G(double v) {
            return v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string Stack() {
            List<string> s = new List<string>();
            foreach (double v in stack) {
                s.Add(G(v));
            }
            return "[" + string.Join(" ", s) + "]";
        }

        private void point(string op, double dx, double dy) {
            x += dx;
            y += dy;
            path.Append(op + " " + G(x) + " " + G(y) + " ");
        }

        private void curve(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3) {
            point("C", dx1, dy1);
            point("", dx2, dy2);
            point("", dx3, dy3);
        }

        // Takes the width off the stack, the first time, when it has more
        // operands than even, for even, or than n.
        private void args(bool even, int n) {
            if (!width) {
                width = true;
                if ((even && stack.Count % 2 == 1) || (!even && stack.Count > n)) {
                    stack.RemoveAt(0);
                }
            }
        }

        internal void run(byte[] cs, int depth) {
            for (int i = 0; i < cs.Length && !ended; ) {
                int b0 = cs[i];
                if (b0 == 28) {
                    stack.Add((short) (cs[i + 1] << 8 | cs[i + 2]));
                    i += 3;
                    continue;
                } else if (b0 >= 32 && b0 <= 246) {
                    stack.Add(b0 - 139);
                    i++;
                    continue;
                } else if (b0 >= 247 && b0 <= 250) {
                    stack.Add((b0 - 247) * 256 + cs[i + 1] + 108);
                    i += 2;
                    continue;
                } else if (b0 >= 251 && b0 <= 254) {
                    stack.Add(-(b0 - 251) * 256 - cs[i + 1] - 108);
                    i += 2;
                    continue;
                } else if (b0 == 255) {
                    stack.Add((cs[i + 1] << 24 | cs[i + 2] << 16 | cs[i + 3] << 8 | cs[i + 4]) / 65536.0);
                    i += 5;
                    continue;
                }
                List<double> s = new List<double>(stack);
                i++;
                switch (b0) {
                case 1: case 3: case 18: case 23:   // hstem, vstem, hstemhm, vstemhm
                    args(true, 0);
                    stems += stack.Count / 2;
                    path.Append("stem" + b0 + " " + Stack() + " ");
                    break;
                case 19: case 20: {                 // hintmask, cntrmask
                    args(true, 0);
                    stems += stack.Count / 2;
                    int n = (stems + 7) / 8;
                    path.Append("mask" + b0 + " " + Stack() + " " + BitConverter.ToString(cs, i, n) + " ");
                    i += n;
                    break;
                }
                case 21:    // rmoveto
                    args(false, 2);
                    point("M", stack[0], stack[1]);
                    break;
                case 22:    // hmoveto
                    args(false, 1);
                    point("M", stack[0], 0);
                    break;
                case 4:     // vmoveto
                    args(false, 1);
                    point("M", 0, stack[0]);
                    break;
                case 5:     // rlineto
                    for (int j = 0; j + 1 < s.Count; j += 2) {
                        point("L", s[j], s[j + 1]);
                    }
                    break;
                case 6: case 7: {   // hlineto, vlineto
                    bool h = b0 == 6;
                    for (int j = 0; j < s.Count; j++, h = !h) {
                        if (h) {
                            point("L", s[j], 0);
                        } else {
                            point("L", 0, s[j]);
                        }
                    }
                    break;
                }
                case 8:     // rrcurveto
                    for (int j = 0; j + 5 < s.Count; j += 6) {
                        curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5]);
                    }
                    break;
                case 24: {  // rcurveline
                    int j = 0;
                    for (; j + 5 < s.Count - 2; j += 6) {
                        curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5]);
                    }
                    point("L", s[j], s[j + 1]);
                    break;
                }
                case 25: {  // rlinecurve
                    int j = 0;
                    for (; j + 1 < s.Count - 6; j += 2) {
                        point("L", s[j], s[j + 1]);
                    }
                    curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5]);
                    break;
                }
                case 26: case 27: {     // vvcurveto, hhcurveto
                    int j = 0;
                    double d = 0;
                    if (s.Count % 4 == 1) {
                        d = s[0];
                        j = 1;
                    }
                    for (; j + 3 < s.Count; j += 4) {
                        if (b0 == 26) {
                            curve(d, s[j], s[j + 1], s[j + 2], 0, s[j + 3]);
                        } else {
                            curve(s[j], d, s[j + 1], s[j + 2], s[j + 3], 0);
                        }
                        d = 0;
                    }
                    break;
                }
                case 30: case 31: {     // vhcurveto, hvcurveto
                    bool h = b0 == 31;
                    for (int j = 0; j + 3 < s.Count; j += 4, h = !h) {
                        double last = (s.Count - j == 5) ? s[j + 4] : 0;
                        if (h) {
                            curve(s[j], 0, s[j + 1], s[j + 2], last, s[j + 3]);
                        } else {
                            curve(0, s[j], s[j + 1], s[j + 2], s[j + 3], last);
                        }
                    }
                    break;
                }
                case 10: case 29: {     // callsubr, callgsubr
                    List<byte[]> subrs = (b0 == 29) ? globals : locals;
                    int number = (int) s[s.Count - 1] + CFFSubset.bias(subrs.Count);
                    stack.RemoveAt(stack.Count - 1);
                    run(subrs[number], depth + 1);
                    continue;
                }
                case 11:    // return
                    return;
                case 14:    // endchar
                    args(false, 0);
                    path.Append("Z");
                    ended = true;
                    return;
                case 12: {
                    int b1 = cs[i];
                    i++;
                    if (b1 == 35) {             // flex
                        curve(s[0], s[1], s[2], s[3], s[4], s[5]);
                        curve(s[6], s[7], s[8], s[9], s[10], s[11]);
                    } else if (b1 == 34) {      // hflex
                        curve(s[0], 0, s[1], s[2], s[3], 0);
                        curve(s[4], 0, s[5], -s[2], s[6], 0);
                    } else if (b1 == 36) {      // hflex1
                        curve(s[0], s[1], s[2], s[3], s[4], 0);
                        curve(s[5], 0, s[6], s[7], s[8], -(s[1] + s[3] + s[7]));
                    } else if (b1 == 37) {      // flex1
                        double dx = Math.Abs(s[0] + s[2] + s[4] + s[6] + s[8]);
                        double dy = Math.Abs(s[1] + s[3] + s[5] + s[7] + s[9]);
                        curve(s[0], s[1], s[2], s[3], s[4], s[5]);
                        if (dx > dy) {
                            curve(s[6], s[7], s[8], s[9], s[10], -(s[1] + s[3] + s[5] + s[7] + s[9]));
                        } else {
                            curve(s[6], s[7], s[8], s[9], -(s[0] + s[2] + s[4] + s[6] + s[8]), s[10]);
                        }
                    } else {
                        path.Append("op12." + b1 + " ");
                    }
                    break;
                }
                default:
                    path.Append("op" + b0 + " ");
                    break;
                }
                stack.Clear();
            }
        }
    }

    [Fact]
    public void WithItsSubroutinesInPlaceDrawsTheSameOutlines() {
        foreach (string path in new string[] {PLEX, "tests/fonts/IBMPlexSans-Italic.otf", HAN}) {
            OTF otf = Otf(path);
            byte[] cff = Cff(otf);
            bool[] text = Used(otf, "Hello, Wörld! Ýou ﬁnd 日本語の文字 ½ € @ & ŞŢ");
            bool[] every = new bool[otf.numGlyphs];
            for (int gid = 0; gid < otf.numGlyphs; gid += 3) {
                every[gid] = true;
            }
            List<string> want = Outlines(cff);
            foreach (bool[] used in new bool[][] {text, every}) {
                bool[] kept;
                byte[] subset = CFFSubset.subset(cff, used, out kept);
                // No subroutines, global or local
                var (_, globals, top) = Parts(subset);
                Assert.True(globals.Count == 0, path + ": " + globals.Count + " global subroutines");
                CFFSubset.Index fdArray = CFFSubset.readIndex(subset, CFFSubset.entryOf(top, CFFSubset.FD_ARRAY)[0]);
                for (int i = 0; i < fdArray.count(); i++) {
                    List<CFFSubset.Entry> fd = CFFSubset.readDict(subset, fdArray.objects[i], fdArray.objects[i + 1]);
                    int[] p = CFFSubset.entryOf(fd, CFFSubset.PRIVATE);
                    Assert.True(CFFSubset.readPrivate(subset, p[0], p[1]).subrs == null,
                            path + ": local subroutines in Font DICT " + i);
                }
                // Each glyph drawn as in the whole font, its hints and masks too
                List<string> got = Outlines(subset);
                int k = 0;
                for (int gid = 0; gid < kept.Length; gid++) {
                    if (!kept[gid]) {
                        continue;
                    }
                    Assert.True(got[k] == want[gid], path + ": glyph " + gid + " drawn otherwise:\n" + got[k] + "\n" + want[gid]);
                    Assert.True(got[k].EndsWith("Z") && (got[k].Contains("M ") || gid == 0 || got[k] == "Z"),
                            path + ": glyph " + gid + ": " + got[k]);
                    k++;
                }
                Assert.Equal(k, got.Count);
            }
        }
    }

    [Fact]
    public void OfAGlyphThatComputesANumberKeepsItsSubroutines() {
        // "0 0 add 0 endchar": a number computed, by add, which the
        // subroutines are not written in place for. The subset keeps the
        // glyphs as they were and the subroutines they call, the others
        // emptied to no bytes, keeping their numbers.
        OTF otf = Otf(PLEX);
        byte[] cff = Cff(otf);
        CFFSubset.Index names = CFFSubset.readIndex(cff, cff[2]);
        CFFSubset.Index tops = CFFSubset.readIndex(cff, names.end);
        List<CFFSubset.Entry> top = CFFSubset.readDict(cff, tops.objects[0], tops.objects[1]);
        CFFSubset.Index charStrings = CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.CHAR_STRINGS)[0]);
        int gid = otf.unicodeToGID.Get('Á');
        new byte[] {139, 139, 12, 10, 139, 14}.CopyTo(cff, charStrings.objects[gid]);
        bool[] kept;
        byte[] subset = CFFSubset.subset(cff, Used(otf, "Hello AÁ"), out kept);
        var (glyphs, globals, _) = Parts(subset);
        var (whole, wholeGlobals, _) = Parts(cff);
        int k = 0;
        for (int g = 0; g < kept.Length; g++) {
            if (kept[g]) {
                Assert.True(glyphs[k].AsSpan().SequenceEqual(whole[g]), "glyph " + g + " is not as it was");
                k++;
            }
        }
        Assert.Equal(wholeGlobals.Count, globals.Count);
        int emptied = 0;
        for (int i = 0; i < globals.Count; i++) {
            if (globals[i].Length == 0 && wholeGlobals[i].Length > 0) {
                emptied++;
            } else {
                Assert.Equal(wholeGlobals[i], globals[i]);
            }
        }
        Assert.True(emptied > 0 && emptied < globals.Count, emptied + " of " + globals.Count + " global subroutines emptied");
    }
}
}
