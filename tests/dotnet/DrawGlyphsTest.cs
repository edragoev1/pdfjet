/**
 * DrawGlyphsTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
// DrawGlyphs draws glyphs another engine shaped, by their numbers, as
// outlines read from the font file: paths, no text and no font in the
// document; a glyph drawn more than once a form XObject placed wherever it
// is; in a tagged document marked content with the text as its actual text.
public class DrawGlyphsTest {
    private const string DEVANAGARI = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf";
    private const string CASCADIA = "tests/fonts/CascadiaMono-Regular.ttf";

    private static int Count(string text, string part) {
        int n = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i >= 0;
                i = text.IndexOf(part, i + 1, StringComparison.Ordinal)) {
            n++;
        }
        return n;
    }

    // The content of each form XObject of the document, inflated.
    private static List<string> Forms(byte[] doc) {
        List<string> forms = new List<string>();
        string text = TestSupport.Latin1(doc);
        for (int i = text.IndexOf("/Subtype /Form", StringComparison.Ordinal); i >= 0;
                i = text.IndexOf("/Subtype /Form", i + 1, StringComparison.Ordinal)) {
            int start = text.IndexOf("stream\n", i, StringComparison.Ordinal) + 7;
            int end = text.IndexOf("\nendstream", start, StringComparison.Ordinal);
            using (ZLibStream inflater = new ZLibStream(
                    new MemoryStream(doc, start, end - start), CompressionMode.Decompress)) {
                MemoryStream output = new MemoryStream();
                inflater.CopyTo(output);
                forms.Add(TestSupport.Latin1(output.ToArray()));
            }
        }
        return forms;
    }

    [Fact]
    public void TheGlyphsAreOutlinesAndNoFontIsWritten() {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.PlexSans(pdf);
        font.SetSize(12f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.Get('A');
        int b = font.unicodeToGID.Get('B');
        page.DrawGlyphs(font, new int[] {a, b}, new float[] {50f, 100f, 60f, 100f}, "AB", "en");
        string content = TestSupport.Content(page);
        // Each placed by the matrix of the em at its place
        Assert.Contains("q 0.012 0 0 0.012 50 692 cm /Fm", content);
        Assert.Contains("q 0.012 0 0 0.012 60 692 cm /Fm", content);
        Assert.DoesNotContain("BT", content);
        Assert.DoesNotContain("Tf", content);
        Assert.DoesNotContain("BDC", content);  // Untagged: just the shapes
        pdf.Complete();
        string doc = TestSupport.Latin1(bos.ToArray());
        Assert.DoesNotContain("/FontFile", doc);    // No font program
        Assert.DoesNotContain("/Type /Font", doc);  // No font
        Assert.DoesNotContain("/ToUnicode", doc);   // No ToUnicode map
        // The outlines: paths in the font's units, filled, no text
        List<string> forms = Forms(bos.ToArray());
        Assert.Equal(2, forms.Count);
        foreach (string form in forms) {
            Assert.Contains(" m\n", form);
            Assert.Contains(" l\n", form);
            Assert.EndsWith("f\n", form);
            Assert.DoesNotContain("BT", form);
            Assert.DoesNotContain("Tj", form);
        }
    }

    [Fact]
    public void AGlyphDrawnTwiceIsOneStampPlacedTwice() {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.PlexSans(pdf);
        font.SetSize(10f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.Get('A');
        int b = font.unicodeToGID.Get('B');
        int space = font.unicodeToGID.Get(' ');
        page.DrawGlyphs(font, new int[] {a, space, b, a},
                new float[] {50f, 100f, 60f, 100f, 65f, 100f, 70f, 100f}, "A BA", null);
        string content = TestSupport.Content(page);
        MatchCollection m = Regex.Matches(content, "q 0.01 0 0 0.01 (50|70) 692 cm (/Fm\\d+) Do Q\n");
        Assert.Equal(2, m.Count);
        string name = m[0].Groups[2].Value;
        Assert.Equal(name, m[1].Groups[2].Value);
        Assert.Equal(3, Count(content, " Do Q\n"));    // The space draws nothing
        // Drawn again, in another run and at another size, A is placed again
        font.SetSize(20f);
        page.DrawGlyphs(font, new int[] {a}, new float[] {80f, 100f}, "A", null);
        content = TestSupport.Content(page);
        Assert.Contains("q 0.02 0 0 0.02 80 692 cm " + name + " Do Q\n", content);
        pdf.Complete();
        string doc = TestSupport.Latin1(bos.ToArray());
        Assert.Equal(2, Count(doc, "/Subtype /Form"));  // A and B, each once
        Assert.Contains(name + " ", doc);              // The stamp is a resource
    }

    [Fact]
    public void InATaggedParagraphTheGlyphsCarryTheirText() {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.Get('A');
        page.AddBDC(StructElem.P, "hi", null, null);
        page.DrawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, "क", "hi");
        page.AddEMC();
        string content = TestSupport.Content(page);
        Assert.Contains("/P <</MCID 0>>\nBDC\n/Span <</Lang <"
                + Util.ToHexString(System.Text.Encoding.UTF8.GetBytes("hi"))
                + "> /ActualText <" + Page.ToUTF16Hex("क") + ">>> BDC\nq ", content);
        Assert.Contains(" Do Q\nEMC\nEMC\n", content);
        Assert.Equal(2, Count(content, "BDC\n"));
        pdf.Complete();
        Assert.DoesNotContain("/FontFile", TestSupport.Latin1(bos.ToArray()));
    }

    [Fact]
    public void OutsideAnyContentTheGlyphsAreAParagraphOfTheirOwn() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.Get('A');
        page.DrawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, "A", null);
        string content = TestSupport.Content(page);
        Assert.Contains("/P <</MCID 0>>\nBDC\n/Span <</ActualText <" + Page.ToUTF16Hex("A")
                + ">>> BDC\n", content);
        // A tagged document asks for the text the glyphs stand for
        Page next = new Page(pdf, Letter.PORTRAIT);
        Assert.Throws<ArgumentException>(
                () => next.DrawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, null, null));
    }

    [Fact]
    public void AFontUsedForTextTooIsWritten() {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.Get('A');
        page.DrawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, "A", null);
        new TextLine(font, "Text").SetLocation(50f, 200f).DrawOn(page);
        pdf.Complete();
        Assert.Contains("/FontFile3", TestSupport.Latin1(bos.ToArray()));  // The font of the text line
    }

    [Fact]
    public void TrueTypeOutlinesAreCubicCurvesAndCompositesHaveTheirParts() {
        PDF pdf = TestSupport.NewPDF();
        Font cascadia = new Font(pdf, TestSupport.RepoPath(CASCADIA));
        GlyphOutline outline = new GlyphOutline(cascadia.program.font, false);
        Assert.Equal(1.0 / 2048, outline.MatrixOf(1)[0], 12);
        // O: two contours of quadratic curves, drawn as cubic ones
        string o = outline.Outline(cascadia.unicodeToGID.Get('O')).operators;
        Assert.Equal(2, Count(o, " m\n"));
        Assert.Contains(" c\n", o);
        // Aacute, a composite glyph: the contours of A and of the acute
        int aacute = cascadia.unicodeToGID.Get(0x00C1);
        int capitalA = cascadia.unicodeToGID.Get('A');
        string ofA = outline.Outline(capitalA).operators;
        string composite = outline.Outline(aacute).operators;
        Assert.True(Count(composite, " m\n") > Count(ofA, " m\n"), composite);
        Assert.True(composite.StartsWith(ofA, StringComparison.Ordinal)
                || composite.EndsWith(ofA, StringComparison.Ordinal), composite);

        Font devanagari = new Font(pdf, TestSupport.RepoPath(DEVANAGARI));
        GlyphOutline noto = new GlyphOutline(devanagari.program.font, false);
        string ii = noto.Outline(12).operators;     // iideva, a composite glyph
        Assert.Contains(" m\n", ii);
        Assert.Contains(" c\n", ii);
        Assert.Equal("", noto.Outline(devanagari.unicodeToGID.Get(' ')).operators);
    }

    [Fact]
    public void CffOutlinesHaveTheirSubroutinesAndMatrix() {
        PDF pdf = TestSupport.NewPDF();
        Font plex = TestSupport.PlexSans(pdf);
        GlyphOutline outline = new GlyphOutline(plex.program.font, true);
        Assert.Equal(0.001, outline.MatrixOf(1)[0], 12);
        GlyphOutline.Path o = outline.Outline(plex.unicodeToGID.Get('O'));
        Assert.Equal(2, Count(o.operators, " m\n"));
        Assert.True(o.yMin < 0 && o.yMax > 600, o.yMin + " " + o.yMax);
        Assert.Equal("", outline.Outline(plex.unicodeToGID.Get(' ')).operators);
    }

    [Fact]
    public void CffOutlinesOfAFontMatrixOfTheirOwnAreThoseOfTheTrueTypeEdition() {
        // Cascadia Mono's .otf has 2,048 units to the em, in its FontMatrix,
        // 0.00048828: its glyphs, of the numbers of those of its .ttf, are of
        // their size and in their place
        Font otf = new Font(TestSupport.NewPDF(), "fonts/CascadiaMono/CascadiaMono-Regular.otf");
        // In a PDF of its own: a font of the name and the glyphs of one read
        // before is that one
        Font ttf = new Font(TestSupport.NewPDF(), TestSupport.RepoPath(CASCADIA));
        GlyphOutline cff = new GlyphOutline(otf.program.font, true);
        GlyphOutline glyf = new GlyphOutline(ttf.program.font, false);
        Assert.True(otf.program.cff && !ttf.program.cff);
        Assert.True(Math.Abs(cff.MatrixOf(1)[0] - 1.0 / 2048) < 1e-8, cff.MatrixOf(1)[0].ToString());
        foreach (char c in "OAgé") {
            int gid = otf.unicodeToGID.Get(c);
            Assert.True(gid != 0 && gid == ttf.unicodeToGID.Get(c), c + ": glyph " + gid);
            GlyphOutline.Path a = cff.Outline(gid);
            GlyphOutline.Path b = glyf.Outline(gid);
            Assert.Equal(b.xMin + " " + b.yMin + " " + b.xMax + " " + b.yMax,
                    a.xMin + " " + a.yMin + " " + a.xMax + " " + a.yMax);
        }
    }

    [Fact]
    public void NumbersAreHundredthsRoundedHalfAwayFromZero() {
        Assert.Equal("0", GlyphOutline.Decimals(-0.004, 2));
        Assert.Equal("1.5", GlyphOutline.Decimals(1.5, 2));
        Assert.Equal("-2.67", GlyphOutline.Decimals(-8.0 / 3, 2));
        Assert.Equal("0.0058594", GlyphOutline.Decimals(12.0 / 2048, 7));
        Assert.Equal("100", GlyphOutline.Decimals(99.999, 2));
    }

    [Fact]
    public void AttachTextCarriesTheTextAsAnAssociatedFile() {
        MemoryStream bos = new MemoryStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).SetTitle("Title");
        Font font = TestSupport.PlexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Text").SetLocation(50f, 200f).DrawOn(page);
        pdf.AttachText("article-1.txt", "सभी text");
        pdf.Complete();
        string doc = TestSupport.Latin1(bos.ToArray());
        Assert.Contains("/Subtype /text#2Fplain\n", doc);  // The media type, with no parameters for PDF/A-3
        Assert.Contains("/AFRelationship /Source", doc);                   // The relationship
        Assert.Contains("/AF [", doc);                                     // An associated file
        Assert.Contains("/Desc ", doc);                                    // The description
    }

    [Fact]
    public void AttachTextIsRefusedInPDFA1AndPDFA2() {
        foreach (Compliance compliance in new Compliance[] {Compliance.PDF_A_1B, Compliance.PDF_A_2B}) {
            PDF pdf = new PDF(new MemoryStream(), compliance);
            Assert.Throws<InvalidOperationException>(() => pdf.AttachText("a.txt", "text"));
        }
    }

    [Fact]
    public void MisuseIsReported() {
        using (TestSupport.AllowCoreFonts()) {
            PDF pdf = new PDF(new MemoryStream());
            Font plex = TestSupport.PlexSans(pdf);
            Font core = TestSupport.Helvetica(pdf);
            Page page = new Page(pdf, Letter.PORTRAIT);
            int a = plex.unicodeToGID.Get('A');
            Assert.Throws<ArgumentException>(
                    () => page.DrawGlyphs(core, new int[] {1}, new float[] {0f, 0f}, "A", null));
            Assert.Throws<ArgumentException>(
                    () => page.DrawGlyphs(plex, new int[] {a}, new float[] {0f}, "A", null));
            Assert.Throws<ArgumentException>(
                    () => page.DrawGlyphs(plex, new int[] {plex.advanceWidth.Length}, new float[] {0f, 0f}, "A", null));
            Assert.Throws<ArgumentException>(
                    () => page.DrawGlyphs(plex, new int[] {-1}, new float[] {0f, 0f}, "A", null));
        }
    }
}
}   // End of DrawGlyphsTest.cs
