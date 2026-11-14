/**
 * DrawGlyphsTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {
// DrawGlyphs draws glyphs another engine shaped, by their numbers, as real
// text: a span of actual text for each cluster, each glyph at its place, and
// the glyphs no character maps to mapped to their cluster's letters in
// ToUnicode, or to the zero width space.
public class DrawGlyphsTest {
    // The first glyph of the font no character maps to, like a conjunct
    private static int UnmappedGlyph(Font font) {
        for (int gid = 1; gid < font.unicodeOf.Length; gid++) {
            if (font.unicodeOf[gid] < 0) {
                return gid;
            }
        }
        throw new InvalidOperationException("the font maps every glyph");
    }

    private static int Count(string text, string part) {
        int n = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i >= 0;
                i = text.IndexOf(part, i + 1, StringComparison.Ordinal)) {
            n++;
        }
        return n;
    }

    [Fact]
    public void TheGlyphsAreRealTextInATaggedParagraph() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.PlexSans(pdf);
        font.SetSize(12f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.Get('A');
        int x = UnmappedGlyph(font);
        page.AddBDC(StructElem.P, "en", null, null);
        page.DrawGlyphs(font, new int[] {a, x}, new float[] {50f, 100f, 60f, 100f},
                new int[] {0, 1}, "AXY", "hi");
        page.AddEMC();
        string content = TestSupport.Content(page);
        // The language around them all, then a span of actual text for each
        // cluster, its glyphs in a TJ array
        Assert.Contains("/Span <</Lang <" + Util.ToHexString(System.Text.Encoding.UTF8.GetBytes("hi"))
                + ">>> BDC\nBT\n", content);
        Assert.Contains("/Span <</ActualText <" + Page.ToUTF16Hex("A") + ">>> BDC\n"
                + "1 0 0 1 50 692 Tm\n[<" + a.ToString("X4") + ">] TJ\nEMC\n", content);
        Assert.Contains("/Span <</ActualText <" + Page.ToUTF16Hex("XY") + ">>> BDC\n"
                + "1 0 0 1 60 692 Tm\n[<" + x.ToString("X4") + ">] TJ\nEMC\nET\nEMC\n", content);
        Assert.Equal(4, Count(content, "BDC\n"));   // The paragraph's, the language's, two spans
        // The unmapped glyph maps to its cluster's letters, "XY"; the mapped
        // one keeps its own
        string map = FontWriter.ToUnicodeCMap(font, null).ToLowerInvariant();
        Assert.Contains("<" + x.ToString("x4") + "> <00580059>", map);
        Assert.DoesNotContain("<" + a.ToString("x4") + "> <0058", map);
    }

    [Fact]
    public void AnUnmappedGlyphBesideALetterTheClusterLacksIsTheZeroWidthSpace() {
        // As Khmer's ោ, drawn as the glyph of េ and a second part: the letters
        // cannot add up to the cluster's, and MuPDF would copy it twice
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int x = UnmappedGlyph(font);
        page.DrawGlyphs(font, new int[] {font.unicodeToGID.Get('B'), x}, new float[] {50f, 100f, 60f, 100f},
                new int[] {0, 0}, "XY", "en");
        string map = FontWriter.ToUnicodeCMap(font, null).ToLowerInvariant();
        Assert.Contains("<" + x.ToString("x4") + "> <200b>", map);
    }

    [Fact]
    public void OutsideAnyContentTheGlyphsAreAParagraphOfTheirOwn() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.DrawGlyphs(font, new int[] {font.unicodeToGID.Get('A')}, new float[] {50f, 100f}, null, "A", "en");
        string content = TestSupport.Content(page);
        Assert.StartsWith("/P <</MCID 0>>\nBDC\n/Span <<", content);
        Assert.EndsWith("ET\nEMC\nEMC\n", content);
        Assert.Single(page.structures);
    }

    [Fact]
    public void AnUntaggedPageHasTheSpansOfActualTextAlone() {
        PDF pdf = new PDF(new MemoryStream());
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.DrawGlyphs(font, new int[] {font.unicodeToGID.Get('A')}, new float[] {50f, 100f}, null, "A", null);
        string content = TestSupport.Content(page);
        Assert.StartsWith("BT\n", content);
        Assert.Contains("/Span <</ActualText <" + Page.ToUTF16Hex("A") + ">>> BDC\n", content);
        Assert.DoesNotContain("MCID", content);
    }

    [Fact]
    public void MisuseIsReported() {
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = new PDF(new MemoryStream());
        Font plex = TestSupport.PlexSans(pdf);
        Font core = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = plex.unicodeToGID.Get('A');
        Assert.Throws<ArgumentException>(() => page.DrawGlyphs(core, new int[] {1}, new float[] {0f, 0f}, null, "A", null));
        Assert.Throws<ArgumentException>(() => page.DrawGlyphs(plex, new int[] {a}, new float[] {0f}, null, "A", null));
        Assert.Throws<ArgumentException>(() => page.DrawGlyphs(plex, new int[] {plex.advanceWidth.Length}, new float[] {0f, 0f}, null, "A", null));
        Assert.Throws<ArgumentException>(() => page.DrawGlyphs(plex, new int[] {a}, new float[] {0f, 0f}, new int[] {1}, "A", null));
        Assert.Throws<ArgumentException>(() => page.DrawGlyphs(plex, new int[] {a}, new float[] {0f, 0f}, new int[] {0, 0}, "A", null));
    }
}
}
