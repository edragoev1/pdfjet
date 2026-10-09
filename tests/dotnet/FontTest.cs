/*
 * FontTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public class FontTest {
    [Fact]
    public void CoreFontWidthsComeFromTheAfmMetrics() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        Assert.Equal("Helvetica", font.GetName());
        Assert.Equal(12f, font.GetSize());
        // H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
        TestSupport.AssertNear(27.336f, font.StringWidth(12f, "Hello"), 0.001f);
        TestSupport.AssertNear(27.336f, font.StringWidth("Hello"), 0.001f);
        font.SetSize(24f);
        TestSupport.AssertNear(54.672f, font.StringWidth("Hello"), 0.001f);
    }

    [Fact]
    public void TheWidthOfNoTextIsZero() {
        Assert.Equal(0f, TestSupport.Helvetica(TestSupport.NewPDF()).StringWidth(null));
    }

    [Fact]
    public void KerningPairsNarrowTheText() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TestSupport.AssertNear(16.008f, font.StringWidth(12f, "AV"), 0.001f);
        font.SetKernPairs(true);
        // KPX A V -70
        TestSupport.AssertNear(15.168f, font.StringWidth(12f, "AV"), 0.001f);
    }

    [Fact]
    public void CoreFontVerticalMetrics() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TestSupport.AssertNear(11.172f, font.GetAscent(12f), 0.001f);
        TestSupport.AssertNear(2.7f, font.GetDescent(12f), 0.001f);
        TestSupport.AssertNear(13.872f, font.GetBodyHeight(12f), 0.001f);
    }

    [Fact]
    public void GetFitCharsCountsTheCharactersThatFit() {
        Assert.Equal(5, TestSupport.Helvetica(TestSupport.NewPDF()).GetFitChars("Hello world", 30f));
    }

    [Fact]
    public void EveryCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() {
#pragma warning disable CS0618 // The CJK fonts are deprecated, and still tested
        Font font = new Font(TestSupport.NewPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
#pragma warning restore CS0618
        Assert.Equal(20f, font.StringWidth(10f, "日本"));
        Assert.Equal(10f, font.StringWidth(10f, "𠀋"));
    }

    [Fact]
    public void ReadsATrueTypeFont() {
        string path = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
        if (!File.Exists(TestSupport.RepoPath(path))) {
            return;     // The fonts directory is not here.
        }
        using (Stream stream = TestSupport.Open(path)) {
            Font font = new Font(TestSupport.NewPDF(), stream);
            Assert.Equal("IBMPlexSans", font.GetName());
            TestSupport.AssertNear(28.32f, font.StringWidth(12f, "Hello"), 0.001f);
        }
    }

    private static int Int32At(byte[] buffer, int i) {
        return (buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | buffer[i + 3];
    }

    [Fact]
    public void ACoreFontNumberOutsideTheFourteenIsRejected() {
        PDF pdf = TestSupport.NewPDF();
        Assert.Throws<ArgumentException>(() => new Font(pdf, 0));
        Assert.Throws<ArgumentException>(() => new Font(pdf, 15));
    }

    [Fact]
    public void TheLineGapOfAFontSpacesTheLinesOfATextBlock() {
        // IBM Plex Sans SC has a line gap of one em.
        string path = "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.ttf";
        if (!File.Exists(TestSupport.RepoPath(path))) {
            return;     // The fonts directory is not here.
        }
        PDF pdf = TestSupport.NewPDF();
        Font sc = new Font(pdf, TestSupport.Open(path));
        TestSupport.AssertNear(10f, sc.GetLineGap(10f), 0.001f);
        Assert.Equal(0f, new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
                .GetLineGap(10f));
        // The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
        sc.SetSize(10f);
        TestSupport.AssertXY(500f, 40f, new TextBlock(sc, "日本\n日本").SetLocation(0f, 0f).DrawOn(null));
    }

    [Fact]
    public void AStreamFontPathOpensTheTrueTypeFont() {
        // PDFjet reads .otf and .ttf fonts alone from 9.0.5: a path to a
        // .stream file opens the .ttf file of the same name.
        foreach (string path in new string[] {
                "fonts/NotoSans/NotoSans-Regular.ttf.stream", "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"}) {
            string want = path.Substring(0, path.IndexOf('.')) + ".ttf";
            Assert.Equal(TestSupport.RepoPath(want), Font.FontFileOf(TestSupport.RepoPath(path)));
            Assert.NotEmpty(new Font(TestSupport.NewPDF(), TestSupport.RepoPath(path)).GetName());
        }
        // Any other font is refused in PDFjet's words.
        Exception e = Assert.ThrowsAny<Exception>(() => new Font(TestSupport.NewPDF(),
                new MemoryStream(System.Text.Encoding.Latin1.GetBytes("\u000ePlexSans-Bold\u0000\u0000\u0000"))));
        Assert.Equal("Invalid font file: not an OpenType or TrueType font: PDFjet reads .otf and .ttf fonts.", e.Message);
    }

    [Fact]
    public void ACoreFontDrawsTheWinAnsiCharactersFrom128To159() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        // ’ is 146 in WinAnsi and 222 units wide, where a space is 278.
        TestSupport.AssertNear(2.22f, font.StringWidth(10f, "\u2019"), 0.001f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Don\u2019t \u20ac5 \u2014 \u201cHi\u201d").SetLocation(10f, 20f).DrawOn(page);
        Assert.Contains("<446f6e927420803520972093486994>", TestSupport.Content(page).ToLowerInvariant());
    }

    [Fact]
    public void ACoreFontDrawsDeleteAsASpace() {
        // WinAnsi draws a bullet at 127, where the widths of the core fonts
        // have a space: U+007F is a control, drawn as a space as the C1
        // controls are.
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Assert.Equal(font.StringWidth(10f, " "), font.StringWidth(10f, "\u007f"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a\u007fb\u0085c").SetLocation(10f, 20f).DrawOn(page);
        Assert.Contains("<6120622063>", TestSupport.Content(page));
    }

    // IBM Plex Sans, read from its .ttf file, or null when the fonts
    // directory is not here. Its space is 236 units wide and its .notdef 472,
    // of 1000 units to the em; it has the characters from U+0020 to U+FFFD,
    // and no Thai.
    private static Font IBMPlexSans(PDF pdf) {
        if (!File.Exists(TestSupport.RepoPath("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))) {
            return null;
        }
        return new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
    }

    [Fact]
    public void ACharacterTheFontDoesNotHaveIsDrawnWithNotdef() {
        PDF pdf = TestSupport.NewPDF();
        Font font = IBMPlexSans(pdf);
        if (font == null) {
            return;     // The fonts directory is not here.
        }
        // ก, U+0E01, is in the range of the font and not in the font, and 😀,
        // U+1F600, is past its range: both are drawn with .notdef, as wide as
        // it is. A control character is drawn as a space.
        TestSupport.AssertNear(4.72f, font.StringWidth(10f, "ก"), 0.001f, "a character in the range");
        TestSupport.AssertNear(4.72f, font.StringWidth(10f, "\U0001F600"), 0.001f, "a character past the range");
        foreach (string c in new string[] {"\t", "\u007f", "\u0085"}) {
            TestSupport.AssertNear(2.36f, font.StringWidth(10f, c), 0.001f, "U+" + ((int) c[0]).ToString("X4"));
        }
        // The width of the text that fits is that of the glyphs drawn.
        Assert.Equal(2, font.SetSize(10f).GetFitChars("กก", 9.5f));
        // The glyph of a missing character is .notdef, in a span whose actual
        // text is the character, so that a copy of the text has it and not
        // the U+FFFD the ToUnicode map gives .notdef. A control is the space
        // glyph.
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "ก\t\U0001F600").SetLocation(10f, 20f).DrawOn(page);
        string content = TestSupport.Content(page);
        string space = font.unicodeToGID[0x20].ToString("X4");
        Assert.Contains("/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<" + space + ">", content);
        Assert.Contains("/Span <</ActualText <FEFFD83DDE00>>> BDC\n<0000> Tj\nEMC\n", content);
        // The text a glyph maps to is the character, and a space for a control.
        Assert.Equal("ก", Page.TextOf(0x0E01));
        Assert.Equal(" ", Page.TextOf(0x0085));
    }

    [Fact]
    public void AStampDrawsACharacterTheFontDoesNotHaveWithNotdef() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Font font = IBMPlexSans(pdf);
        if (font == null) {
            return;     // The fonts directory is not here.
        }
        new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        stamp.DrawText(font, 10f, 5f, 20f, "aก\t");
        stamp.Complete();
        pdf.Complete();
        string want = "<" + font.unicodeToGID['a'].ToString("X4") +
                "> Tj\n/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<" +
                font.unicodeToGID[0x20].ToString("X4") + "> Tj\n";
        Assert.Contains(want, TestSupport.Latin1(stream.ToArray()));
    }

    [Fact]
    public void ACompliantDocumentDrawsACharacterTheFontDoesNotHaveWithoutNotdef() {
        // PDF/UA and PDF/A forbid .notdef, so a PDF/UA document draws the
        // replacement character of the font, as wide as it is, with the missing
        // character as its actual text, and never glyph 0.
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        pdf.SetCompliance(Compliance.PDF_UA_1).SetTitle("Test");
        Font font = IBMPlexSans(pdf);
        if (font == null) {
            return;     // The fonts directory is not here.
        }
        int replacement = font.unicodeToGID[0xFFFD];
        Assert.True(replacement != 0, "IBM Plex Sans has no U+FFFD");
        float width = font.GlyphAdvance(replacement) * 10f / font.unitsPerEm;
        TestSupport.AssertNear(width, font.StringWidth(10f, "ก"), 0.001f, "a character in the range");
        TestSupport.AssertNear(width, font.StringWidth(10f, "\U0001F600"), 0.001f, "a character past the range");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "ก\U0001F600").SetLocation(10f, 20f).DrawOn(page);
        string content = TestSupport.Content(page);
        string glyph = "<" + replacement.ToString("X4") + "> Tj\nEMC\n";
        Assert.Contains("/Span <</ActualText <FEFF0E01>>> BDC\n" + glyph, content);
        Assert.Contains("/Span <</ActualText <FEFFD83DDE00>>> BDC\n" + glyph, content);
        Assert.DoesNotContain("<0000>", content);
        // The stamp draws it so too.
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        stamp.DrawText(font, 10f, 5f, 20f, "ก");
        stamp.Complete();
        pdf.Complete();
        Assert.Contains("/Span <</ActualText <FEFF0E01>>> BDC\n" + glyph, TestSupport.Latin1(stream.ToArray()));
        // The character still has no glyph, so a fallback font draws it.
        Assert.False(font.HasGlyph(0x0E01), "a missing character has a glyph");
    }

    [Fact]
    public void AControlCharacterStaysInTheFontOfItsText() {
        // A control character is drawn as a space by the font, not by the
        // fallback font: the fallback font would draw it as a space too.
        PDF pdf = TestSupport.NewPDF();
        Font latin = IBMPlexSans(pdf);
        if (latin == null) {
            return;     // The fonts directory is not here.
        }
        Font helvetica = TestSupport.Helvetica(pdf);
        foreach (Font font in new Font[] {latin, helvetica}) {
            foreach (int c in new int[] {'\t', 0x7F, 0x85}) {
                Assert.True(font.HasGlyph(c), font.name + " has no glyph for U+" + c.ToString("X4"));
            }
        }
        Assert.False(latin.HasGlyph(0x0E01) || latin.HasGlyph(0x1F600), "a character drawn with .notdef has a glyph");
    }

    [Fact]
    public void ASoftHyphenIsAHyphenAndANoBreakSpaceIsASpace() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        font.SetKernPairs(true);
        Assert.Equal(font.StringWidth(10f, "T-"), font.StringWidth(10f, "T\u00ad"));
        Assert.Equal(font.StringWidth(10f, ". "), font.StringWidth(10f, ".\u00a0"));
        // KPX period space -60
        TestSupport.AssertNear(4.96f, font.StringWidth(10f, ".\u00a0"), 0.001f);
    }

    [Fact]
    public void AFallbackFontDrawsOnlyTheCharactersTheFontHasNoGlyphFor() {
        if (!File.Exists(TestSupport.RepoPath("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.ttf"))) {
            return;     // The fonts directory is not here.
        }
        PDF pdf = TestSupport.NewPDF();
        Font latin = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Font jp = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.ttf"));
        Font helvetica = TestSupport.Helvetica(pdf);
        // The Latin letters after the Japanese ones are in the font again.
        TestSupport.AssertNear(latin.StringWidth(10f, "abc") + jp.StringWidth(10f, "\u65e5\u672c") + latin.StringWidth(10f, "def"),
                latin.StringWidth(jp, 10f, "abc\u65e5\u672cdef"), 0.001f);
        // A core font has a fallback font too.
        TestSupport.AssertNear(helvetica.StringWidth(10f, "Tokyo ") + jp.StringWidth(10f, "\u6771\u4eac"),
                helvetica.StringWidth(jp, 10f, "Tokyo \u6771\u4eac"), 0.001f);
        // A character that neither font has stays in the font.
        Assert.Equal(latin.StringWidth(10f, "x\u0e01y"), latin.StringWidth(jp, 10f, "x\u0e01y"));
        // A combining mark stays with the character before it, in one run of one font.
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(latin, "\u65e5\u0301").SetFallbackFont(jp).SetLocation(10f, 20f).DrawOn(page);
        Assert.Equal(1, TestSupport.Content(page).Split(" Tf\n").Length - 1);
    }
    // The number of font programs the PDF embeds.
    private static int EmbeddedFonts(byte[] pdf) {
        return Encoding.Latin1.GetString(pdf).Split("/FontFile").Length - 1;
    }

    // Draws a character with each font of a PDF made from the font files.
    private static byte[] DocumentWithFonts(params string[] paths) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        float y = 50f;
        foreach (string path in paths) {
            Font font = new Font(pdf, TestSupport.Open(path)).SetSize(12f);
            new TextLine(font, "\u4e2d").SetLocation(50f, y).DrawOn(page);
            y += 20f;
        }
        pdf.Complete();
        return stream.ToArray();
    }

    [Fact]
    public void TwoFontsOfOneNameWithOtherGlyphsAreBothEmbedded() {
        // A font and another of its name with other glyphs, as a font and a
        // subset of it made by a font tool are, are two font programs. A PDF
        // embedded the font file of the first of two fonts of one name for
        // both, so the text drawn with the second came out in the glyphs of
        // the first. The other is Noto Sans with a wider .notdef.
        string path = TestSupport.RepoPath("fonts/NotoSans/NotoSans-Regular.ttf");
        if (!File.Exists(path)) {
            return;     // The fonts directory is not here.
        }
        byte[] ttf = File.ReadAllBytes(path);
        byte[] other = (byte[]) ttf.Clone();
        int tables = ttf[4] << 8 | ttf[5];
        for (int i = 0; i < tables; i++) {
            int entry = 12 + 16 * i;
            if (ttf[entry] == 'h' && ttf[entry + 1] == 'm' && ttf[entry + 2] == 't' && ttf[entry + 3] == 'x') {
                int at = ttf[entry + 8] << 24 | ttf[entry + 9] << 16 | ttf[entry + 10] << 8 | ttf[entry + 11];
                int width = (ttf[at] << 8 | ttf[at + 1]) + 1;
                other[at] = (byte) (width >> 8);
                other[at + 1] = (byte) width;
            }
        }
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        float y = 50f;
        foreach (byte[] font in new byte[][] {ttf, other}) {
            new TextLine(new Font(pdf, new MemoryStream(font)), "A").SetLocation(50f, y).DrawOn(page);
            y += 20f;
        }
        pdf.Complete();
        Assert.Equal(2, EmbeddedFonts(stream.ToArray()));
    }

    [Fact]
    public void OneFontProgramIsEmbeddedOnce() {
        // The same font added twice is one font program.
        if (!File.Exists(TestSupport.RepoPath("fonts/IBMPlexSans/IBMPlexSans-Regular.otf"))) {
            return;     // The fonts directory is not here.
        }
        Assert.Equal(1, EmbeddedFonts(DocumentWithFonts(
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")));
        Assert.Equal(1, EmbeddedFonts(DocumentWithFonts(
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")));
    }

    [Fact]
    public void TheFontDescriptorHasTheItalicAngleAndFlagOfThePostTable() {
        // IBM's .ttf files of IBM Plex round the angle of their .otf files to a
        // whole degree.
        if (!File.Exists(TestSupport.RepoPath("fonts/IBMPlexSans/IBMPlexSans-Italic.otf"))) {
            return;     // The fonts directory is not here.
        }
        string[][] fonts = {
            new[] {"fonts/IBMPlexSans/IBMPlexSans-Italic.otf", "-11.31"},
            new[] {"fonts/IBMPlexSans/IBMPlexSans-Italic.ttf", "-11"},
            new[] {"fonts/IBMPlexSerif/IBMPlexSerif-Italic.ttf", "-14"},
            new[] {"fonts/IBMPlexMono/IBMPlexMono-Italic.ttf", "-9"},
            new[] {"fonts/JetBrainsMono/JetBrainsMono-Italic.ttf", "-9"},
            new[] {"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "0"},
        };
        foreach (string[] font in fonts) {
            string pdf = Encoding.Latin1.GetString(DocumentWithFonts(font[0]));
            Assert.Contains("/ItalicAngle " + font[1] + "\n", pdf);
            // Nonsymbolic, and Italic for an italic font.
            Assert.Contains(font[1] == "0" ? "/Flags 32\n" : "/Flags 96\n", pdf);
        }
        Assert.Equal("0.05", OpenTypeFont.ItalicAngleOf(3277));
        Assert.Equal("0", OpenTypeFont.ItalicAngleOf(-1));
        Assert.Equal("-90", OpenTypeFont.ItalicAngleOf(-90 * 65536));
    }

    [Fact]
    public void AGlyphOfALetterAndASignOfItIsCopiedAsTheLetter() {
        // A font may draw the micro sign with the glyph of mu, as Source Serif 4
        // does, and the ohm, kelvin and angstrom signs with those of omega, K and
        // A with a ring: the glyph is copied as the letter, which Greek text
        // needs, and which Unicode makes the signs. The space still wins over
        // the no-break space, and the right single quotation mark over the
        // modifier letter apostrophe.
        int[] unicodeToGID = new int[0x10000];
        int[][] glyphs = {
            new[] {0x00B5, 0x03BC}, new[] {0x03A9, 0x2126}, new[] {0x004B, 0x212A}, new[] {0x00C5, 0x212B},
            new[] {0x0020, 0x00A0}, new[] {0x02BC, 0x2019},
        };
        for (int gid = 1; gid <= glyphs.Length; gid++) {
            foreach (int c in glyphs[gid - 1]) {
                unicodeToGID[c] = gid;
            }
        }
        int[] unicodeOf = FontWriter.UnicodeOfGlyphs(unicodeToGID);
        int[] want = {0x03BC, 0x03A9, 0x004B, 0x00C5, 0x0020, 0x2019};
        for (int gid = 1; gid <= want.Length; gid++) {
            Assert.Equal(want[gid - 1], unicodeOf[gid]);
        }
    }
}
}
