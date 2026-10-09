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
    private const string PLEX = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    private const string HAN = "fonts/Test/SourceHanSansJP-Regular.otf";

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
        return (CFFSubset.emptied(cff, charStrings, null, 0), CFFSubset.emptied(cff, globals, null, 0), top);
    }

    private static bool[] Used(OTF otf, string text) {
        bool[] used = new bool[0x10000];
        foreach (char ch in text) {
            used[otf.unicodeToGID[ch]] = true;
        }
        return used;
    }

    [Fact]
    public void KeepsTheCharstringsUsedAndEmptiesTheRest() {
        foreach (string path in new string[] {PLEX, HAN}) {
            OTF otf = Otf(path);
            byte[] cff = Cff(otf);
            bool[] used = Used(otf, "Hello 日本語");
            bool[] kept;
            byte[] subset = CFFSubset.subset(cff, used, out kept);
            var (whole, wholeGlobals, _) = Parts(cff);
            var (glyphs, globals, _) = Parts(subset);
            Assert.Equal(whole.Count, glyphs.Count);
            for (int gid = 0; gid < glyphs.Count; gid++) {
                Assert.True((gid == 0 || used[gid]) == kept[gid], path + " glyph " + gid);
                Assert.Equal(kept[gid] ? whole[gid] : new byte[] {14}, glyphs[gid]);
            }
            int emptied = 0;
            for (int i = 0; i < globals.Count; i++) {
                if (globals[i].Length == 1 && globals[i][0] == 11 && !(wholeGlobals[i].Length == 1 && wholeGlobals[i][0] == 11)) {
                    emptied++;
                } else {
                    Assert.Equal(wholeGlobals[i], globals[i]);
                }
            }
            Assert.True(emptied > 0, path);
            Assert.True(subset.Length <= cff.Length / 3, path + ": " + subset.Length + " bytes");
        }
    }

    [Fact]
    public void ACIDKeyedFontHasTheIdentityCharset() {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID.
        byte[] cff = Cff(Otf(HAN));
        foreach (bool[] used in new bool[][] {null, new bool[0x10000]}) {
            bool[] kept;
            byte[] subset = CFFSubset.subset(cff, used, out kept);
            var (glyphs, _, top) = Parts(subset);
            int at = CFFSubset.entryOf(top, CFFSubset.CHARSET)[0];
            int n = glyphs.Count - 2;
            Assert.Equal(new byte[] {2, 0, 1, (byte) (n >> 8), (byte) n}, subset[at..(at + 5)]);
        }
        var (whole, _, _) = Parts(cff);
        bool[] all;
        var (same, _, _) = Parts(CFFSubset.subset(cff, null, out all));
        for (int gid = 0; gid < same.Count; gid++) {
            Assert.Equal(whole[gid], same[gid]);
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
}
}
