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
            var (whole, wholeGlobals, _) = Parts(cff);
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
            // Each glyph as it was, .notdef first
            Assert.Equal(whole[0], glyphs[0]);
            for (int i = 0; i < keptGIDs.Count; i++) {
                Assert.Equal(whole[keptGIDs[i]], glyphs[i + 1]);
            }
            // The global subroutines keep their numbers: those called as they
            // were, the others emptied to no bytes
            Assert.Equal(wholeGlobals.Count, globals.Count);
            int emptied = 0;
            for (int i = 0; i < globals.Count; i++) {
                if (globals[i].Length == 0 && wholeGlobals[i].Length > 0) {
                    emptied++;
                } else {
                    Assert.Equal(wholeGlobals[i], globals[i]);
                }
            }
            Assert.True(emptied > 0, path);
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
}
}
