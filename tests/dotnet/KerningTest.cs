/*
 * KerningTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The kerning of TrueType and OpenType fonts, as in the Go kerning_test.go:
/// the pairs of the GPOS table, drawn as they are measured.
/// </summary>
public class KerningTest {
    // Opens a font of the repository, with its kerning on or off.
    internal static Font KernedFont(PDF pdf, string path, bool kerned) {
        Font font = new Font(pdf, TestSupport.RepoPath(path));
        font.SetKernPairs(kerned);
        return font;
    }

    private static readonly Regex DrawnText = new Regex(@"<([0-9A-F]*)> Tj|\[(-?[0-9.]+)\] TJ");

    // Returns how wide the text is as the page draws it, at the size: the
    // advances of its glyphs, with the moves of the TJ arrays between them,
    // from the content of the page.
    private static float DrawnWidth(Font font, string content, float size) {
        float units = 0f;
        float moves = 0f;
        foreach (Match m in DrawnText.Matches(content)) {
            if (m.Groups[2].Success) {
                moves += float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                continue;
            }
            string hex = m.Groups[1].Value;
            for (int i = 0; i + 4 <= hex.Length; i += 4) {
                int gid = int.Parse(hex.Substring(i, 4), NumberStyles.HexNumber);
                units += font.GlyphAdvance(gid);
            }
        }
        return units*size/font.unitsPerEm - moves*size/1000f;
    }

    // Draws the text with the font on a page of its own, and returns what the
    // page wrote for it.
    private static string Draw(Font font, string text) {
        Page page = new Page(font.pdf, Letter.PORTRAIT);
        int start = page.GetContent().Length;
        page.DrawString(font, null, 10f, text, 50f, 50f);
        return TestSupport.Content(page).Substring(start);
    }

    [Fact]
    public void KerningNarrowsTheKernedPairsOfAnOpenTypeFont() {
        PDF pdf = TestSupport.NewPDF();
        Font kerned = KernedFont(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", true);
        Font plain = KernedFont(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", false);
        foreach (string pair in new string[] {"AV", "To", "Ty", "LT"}) {
            Assert.True(kerned.StringWidth(10f, pair) < plain.StringWidth(10f, pair),
                    pair + ": kerned " + kerned.StringWidth(10f, pair) + ", not kerned " + plain.StringWidth(10f, pair));
        }
        Assert.Equal(plain.StringWidth(10f, "HH"), kerned.StringWidth(10f, "HH"));
    }

    [Fact]
    public void KerningIsDrawnAsItIsMeasured() {
        PDF pdf = TestSupport.NewPDF();
        foreach (string path in new string[] {
                "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/NotoSans/NotoSans-Regular.ttf"}) {
            Font font = KernedFont(pdf, path, true);
            foreach (string text in new string[] {
                    "AVATAR. To Wally, Tyler and LT: \"Yes,\" P.V. said.",
                    "Обява: Тъй, Уважаеми Г-н Петров, ТАТЯНА.",
                    "Ταύτα, ΑΥΤΟ ΤΟ ΒΑΓΟΝΙ.",
                    "A V, then A V again",
                    "soft­hy​phen, T﻿o"}) {
                float drawn = DrawnWidth(font, Draw(font, text), 10f);
                TestSupport.AssertNear(font.StringWidth(10f, text), drawn, 0.01f, path + ": " + text);
            }
        }
    }

    [Fact]
    public void KerningOfColoredTextIsDrawnAsItIsMeasured() {
        PDF pdf = TestSupport.NewPDF();
        Font font = KernedFont(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", true);
        string text = "AVATAR To \"Wally\" LT.";
        Page page = new Page(pdf, Letter.PORTRAIT);
        int start = page.GetContent().Length;
        page.DrawColoredString(font, text, new float[] {0f, 0f, 0f},
                new Dictionary<string, int> {{"To", 0xFF0000}});
        TestSupport.AssertNear(font.StringWidth(12f, text),
                DrawnWidth(font, TestSupport.Content(page).Substring(start), 12f), 0.01f, "colored");
        // A word with a mark, drawn on the shaping path, kerns with the last
        // glyph of the word before it, as StringWidth measures it
        string shaped = "A V. To Te\u0301 LT";
        start = page.GetContent().Length;
        page.DrawColoredString(font, shaped, new float[] {0f, 0f, 0f},
                new Dictionary<string, int> {{"To", 0xFF0000}});
        TestSupport.AssertNear(font.StringWidth(12f, shaped),
                DrawnWidth(font, TestSupport.Content(page).Substring(start), 12f), 0.01f, "colored, shaped");
    }

    [Fact]
    public void KerningIsOffUnlessAskedFor() {
        PDF pdf = TestSupport.NewPDF();
        Font font = KernedFont(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", false);
        string content = Draw(font, "AVATAR To");
        Assert.False(content.Contains("] TJ"), "a font not kerned moved its glyphs:\n" + content);
    }

    [Fact]
    public void KerningLeavesMarksAndRightToLeftTextAlone() {
        PDF pdf = TestSupport.NewPDF();
        foreach (string path in new string[] {
                "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf",
                "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"}) {
            Font kerned = KernedFont(pdf, path, true);
            Font plain = KernedFont(pdf, path, false);
            foreach (string text in new string[] {"שָׁלוֹם עוֹלָם", "مرحبا بالعالم"}) {
                Assert.True(kerned.StringWidth(10f, text) == plain.StringWidth(10f, text), path + ": \"" + text + "\" kerned");
            }
        }
    }

    [Fact]
    public void KerningFitsWhatIsMeasured() {
        PDF pdf = TestSupport.NewPDF();
        Font font = KernedFont(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", true);
        string text = "AVATAR To Wally";
        // GetFitChars measures at the font's own size
        float width = font.StringWidth(font.GetSize(), text);
        Assert.Equal(text.Length, font.GetFitChars(text, width + 0.001f));
        Assert.NotEqual(text.Length, font.GetFitChars(text, width - 0.1f));
        // A pair kerned wider, "T after a quotation mark, takes its room only
        // with its second character: every start of the text fits in its width
        string wider = "\"Tea\" &co";
        for (int n = 1; n <= wider.Length; n++) {
            float w = font.StringWidth(font.GetSize(), wider.Substring(0, n));
            Assert.Equal(n, font.GetFitChars(wider, w + 0.001f));
        }
    }

    [Fact]
    public void KerningOfABrokenTableIsNone() {
        byte[] buf = {0, 1, 0, 0, 0, 10, 0, 10, 0, 10, 0xFF, 0xFF, 0xFF, 0xFF};
        Kerning k = Kerning.Of(GPOS.Of(buf, 0, buf.Length, 10, false));
        Assert.Equal(0, k.Read(null).Pair(1, 2));
    }

    // The same widths in the four ports, at 10 points: what each port
    // measures, and so wraps and aligns, is the same to the hundredth of a point.
    [Theory]
    [InlineData("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "AVATAR To Wally", 75.14f, -302)]
    [InlineData("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "Тъй ТАТЯНА", 55.94f, -165)]
    [InlineData("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "ΑΥΤΟ", 24.44f, -70)]
    [InlineData("fonts/IBMPlexSans/IBMPlexSans-Regular.otf", "AVATAR To Wally", 75.14f, -302)]
    [InlineData("fonts/NotoSans/NotoSans-Regular.ttf", "AVATAR To Wally", 75.83f, -310)]
    [InlineData("fonts/NotoSans/NotoSans-Regular.ttf", "Тъй ТАТЯНА", 56.79f, -150)]
    [InlineData("fonts/NotoSans/NotoSans-Regular.ttf", "ΑΥΤΟ", 24.62f, -80)]
    public void KerningWidthsAreThoseOfTheOtherPorts(string path, string text, float kerned, int unitsOf) {
        Font font = KernedFont(TestSupport.NewPDF(), path, true);
        TestSupport.AssertNear(kerned, font.StringWidth(10f, text), 0.005f, path + ": " + text);
        Assert.Equal(unitsOf, font.TextKerning(text));
    }

    [Fact]
    public void KerningIsMeasuredFromManyThreadsAtOnce() {
        PDF pdf = TestSupport.NewPDF();
        string path = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
        string text = "AVATAR To Wally";
        float expected = KernedFont(pdf, path, true).StringWidth(10f, text);
        for (int round = 0; round < 20; round++) {
            Font font = KernedFont(pdf, path, true);
            float[] widths = new float[8];
            Parallel.For(0, widths.Length, t => widths[t] = font.StringWidth(10f, text));
            foreach (float width in widths) {
                Assert.Equal(expected, width);
            }
        }
    }

    // Returns a GPOS table whose kern feature has the lookups, all one lookup
    // of the subtables, all one PairPos subtable of the format, which moves
    // the glyph after itself by -50: the pair of the glyph, or the pair of its
    // class 1.
    private static byte[] KernTable(int lookups, int subtables, int format, int glyph) {
        List<int> v = new List<int>();
        v.AddRange(new int[] {1, 0, 10, 30, 42 + 2*lookups});
        v.AddRange(new int[] {1, 0x4446, 0x4C54, 8});   // DFLT
        v.AddRange(new int[] {4, 0, 0, 0xFFFF, 1, 0});
        v.AddRange(new int[] {1, 0x6B65, 0x726E, 8});   // kern
        v.AddRange(new int[] {0, lookups});
        for (int i = 0; i < lookups; i++) {
            v.Add(i);
        }
        v.Add(lookups);
        for (int i = 0; i < lookups; i++) {
            v.Add(2 + 2*lookups);
        }
        v.AddRange(new int[] {2, 0, subtables});
        for (int i = 0; i < subtables; i++) {
            v.Add(6 + 2*subtables);
        }
        if (format == 1) {
            v.AddRange(new int[] {1, 12, 4, 0, 1, 18});     // The coverage at 12, the pair set at 18
            v.AddRange(new int[] {1, 1, glyph});
            v.AddRange(new int[] {1, glyph, -50});
        } else {
            v.AddRange(new int[] {2, 24, 4, 0, 30, 30, 2, 2});  // The coverage at 24, the classes at 30
            v.AddRange(new int[] {0, 0, 0, -50});
            v.AddRange(new int[] {1, 1, glyph});
            v.AddRange(new int[] {1, glyph, 1, 1});
        }
        byte[] buf = new byte[2*v.Count];
        for (int i = 0; i < v.Count; i++) {
            buf[2*i] = (byte) (v[i] >> 8);
            buf[2*i + 1] = (byte) v[i];
        }
        return buf;
    }

    private static Kerning KerningOf(byte[] buf, int numGlyphs) {
        return Kerning.Of(GPOS.Of(buf, 0, buf.Length, numGlyphs, false)).Read(null);
    }

    // A table of a few kilobytes whose subtables named glyph 65535 again and
    // again made gigabytes of arrays by glyph; the room they make is work,
    // and a subtable a lookup names again is read once (the review of 28
    // October 2026). The arrays grow to twice what they hold at most.
    [Theory]
    [InlineData(1, 400, 1)]
    [InlineData(400, 1, 1)]
    [InlineData(400, 1, 2)]
    public void KerningOfATableThatNamesGlyph65535AgainIsBounded(int lookups, int subtables, int format) {
        int room = KerningOf(KernTable(lookups, subtables, format, 65535), 0).Room();
        Assert.True(room <= 2*GPOS.MAX_GPOS_WORK, room + " words");
    }

    // A glyph past the glyphs of the font is left out; a glyph of the font
    // kerns as before, and a subtable named again once.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void KerningLeavesOutAGlyphPastTheGlyphsOfTheFont(int format) {
        Kerning k = KerningOf(KernTable(1, 1, format, 65535), 1000);
        Assert.Equal(0, k.Room());
        Assert.Equal(0, k.Pair(65535, 65535));
        Assert.Equal(-50, KerningOf(KernTable(1, 1, format, 7), 1000).Pair(7, 7));
        Assert.Equal(-50, KerningOf(KernTable(1, 400, format, 65535), 0).Pair(65535, 65535));
    }
}
}
