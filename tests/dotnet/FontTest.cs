/*
 * FontTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
        Font font = new Font(TestSupport.NewPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        Assert.Equal(20f, font.StringWidth(10f, "日本"));
        Assert.Equal(10f, font.StringWidth(10f, "𠀋"));
    }

    [Fact]
    public void ACjkFontDrawsNoIgnorableCharacter() {
        // A CJK font does not draw or measure the soft hyphen and the other
        // characters not drawn: each was drawn, and measured, 1 em wide (review of
        // 12 October 2026, R20).
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);
        font.SetSize(10f);
        string marked = "日\u00AD本\uFE0F\u200B\u200D\uFEFF";
        Assert.Equal(20f, font.StringWidth(10f, marked));
        Assert.Equal(2, font.GetFitChars(marked, 15f));
        Assert.Equal(marked.Length, font.GetFitChars(marked, 20f));
        Func<string, string> draw = text => {
            Page page = new Page(pdf, Letter.PORTRAIT);
            new TextLine(font, text).SetLocation(10f, 10f).DrawOn(page);
            return TestSupport.Content(page);
        };
        Assert.Equal(draw("日本"), draw(marked));
    }

    [Fact]
    public void ReadsATrueTypeFont() {
        string path = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
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
        // IBM Plex Sans SC has a line gap of one em, its ascent 880 and its
        // descent 120, in its .otf, which PDFjet carries, as in its .ttf.
        string path = "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.otf";
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
        // .stream file opens the .ttf file of the same name, or else the .otf
        // file: a file beside it, or a font PDFjet carries. Before 9.1.7 the
        // fonts folder had Noto Sans and both files of IBM Plex Sans; the
        // folder of .ttf and .otf files is made here.
        using (TestSupport.TempDir dir = new TestSupport.TempDir()) {
            string ttf = dir.Write("IBMPlexSans-Regular.ttf", TestSupport.FontBytes("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
            string otf = dir.Write("IBMPlexSans-Regular.otf", TestSupport.FontBytes("tests/fonts/IBMPlexSans-Regular.otf"));
            string[][] paths = {
                // The .ttf beside the .otf
                new[] {otf + ".stream", ttf},
                new[] {ttf + ".stream", ttf},
                // The .otf, with no .ttf beside it
                new[] {TestSupport.RepoPath("tests/fonts/IBMPlexSans-Regular.otf.stream"),
                    TestSupport.RepoPath("tests/fonts/IBMPlexSans-Regular.otf")},
                // The fonts PDFjet carries
                new[] {TestSupport.RepoPath("fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf.stream"),
                    TestSupport.RepoPath("fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf")},
                new[] {TestSupport.RepoPath("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
                    TestSupport.RepoPath("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")},
            };
            foreach (string[] path in paths) {
                Assert.Equal(path[1], Font.FontFileOf(path[0]));
                Assert.NotEmpty(new Font(TestSupport.NewPDF(), path[0]).GetName());
            }
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

    // IBM Plex Sans, read from its .ttf file. Its space is 236 units wide
    // and its .notdef 472, of 1000 units to the em; it has the characters
    // from U+0020 to U+FFFD, and no Thai.
    private static Font IBMPlexSans(PDF pdf) {
        return new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
    }

    [Fact]
    public void ACharacterTheFontDoesNotHaveIsDrawnWithNotdef() {
        PDF pdf = TestSupport.NewPDF();
        Font font = IBMPlexSans(pdf);
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
        string space = font.unicodeToGID.Get(0x20).ToString("X4");
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
        new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        stamp.DrawText(font, 10f, 5f, 20f, "aก\t");
        stamp.Complete();
        pdf.Complete();
        string want = "<" + font.unicodeToGID.Get('a').ToString("X4") +
                "> Tj\n/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<" +
                font.unicodeToGID.Get(0x20).ToString("X4") + "> Tj\n";
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
        int replacement = font.unicodeToGID.Get(0xFFFD);
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
        Font helvetica = TestSupport.Helvetica(pdf);
        foreach (Font font in new Font[] {latin, helvetica}) {
            foreach (int c in new int[] {'\t', 0x7F, 0x85}) {
                Assert.True(font.HasGlyph(c), font.name + " has no glyph for U+" + c.ToString("X4"));
            }
        }
        Assert.False(latin.HasGlyph(0x0E01) || latin.HasGlyph(0x1F600), "a character drawn with .notdef has a glyph");
    }

    // A soft hyphen takes no width, as it is drawn only where a line breaks at
    // it, as a hyphen (review of 11 October 2026, #21); the kerning of the
    // characters on either side of it is kept.
    [Fact]
    public void ASoftHyphenHasNoWidthAndANoBreakSpaceIsASpace() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        font.SetKernPairs(true);
        Assert.Equal(font.StringWidth(10f, "AV"), font.StringWidth(10f, "A\u00adV"));
        Font plex = TestSupport.PlexSans(TestSupport.NewPDF());
        Assert.Equal(plex.StringWidth(10f, "T"), plex.StringWidth(10f, "T\u00ad"));
        Assert.Equal(font.StringWidth(10f, ". "), font.StringWidth(10f, ".\u00a0"));
        // KPX period space -60
        TestSupport.AssertNear(4.96f, font.StringWidth(10f, ".\u00a0"), 0.001f);
    }

    [Fact]
    public void AFallbackFontDrawsOnlyTheCharactersTheFontHasNoGlyphFor() {
        // IBM Plex Sans JP, the .otf PDFjet carries: it has 日本東京, and no
        // Thai, as its .ttf, which this test read before 9.1.7.
        PDF pdf = TestSupport.NewPDF();
        Font latin = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Font jp = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf"));
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
        // the first. The other is IBM Plex Sans (Noto Sans before 9.1.7) with
        // a wider .notdef.
        byte[] ttf = TestSupport.FontBytes("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf");
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
        if (!File.Exists(TestSupport.RepoPath("tests/fonts/IBMPlexSans-Regular.otf"))) {
            return;     // The fonts directory is not here.
        }
        Assert.Equal(1, EmbeddedFonts(DocumentWithFonts(
                "tests/fonts/IBMPlexSans-Regular.otf",
                "tests/fonts/IBMPlexSans-Regular.otf")));
        Assert.Equal(1, EmbeddedFonts(DocumentWithFonts(
                "tests/fonts/IBMPlexSans-Regular.otf",
                "tests/fonts/IBMPlexSans-Regular.otf")));
    }

    [Fact]
    public void TheFontDescriptorHasTheItalicAngleAndFlagOfThePostTable() {
        // IBM's .ttf files of IBM Plex round the angle of their .otf files to a
        // whole degree.
        if (!File.Exists(TestSupport.RepoPath("tests/fonts/IBMPlexSans-Italic.otf"))) {
            return;     // The fonts directory is not here.
        }
        string[][] fonts = {
            new[] {"tests/fonts/IBMPlexSans-Italic.otf", "-11.31"},
            new[] {"fonts/IBMPlexSans/IBMPlexSans-Italic.ttf", "-11"},
            new[] {"fonts/IBMPlexSerif/IBMPlexSerif-Italic.ttf", "-14"},
            // Cascadia Mono (IBM Plex Mono and JetBrains Mono, -9, before
            // 9.1.7), its italic angle -10 in its post table
            new[] {"fonts/CascadiaMono/CascadiaMono-Italic.ttf", "-10"},
            new[] {"fonts/CascadiaMono/CascadiaMono-SemiBoldItalic.ttf", "-10"},
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
        GlyphMap unicodeToGID = new GlyphMap();
        int[][] glyphs = {
            new[] {0x00B5, 0x03BC}, new[] {0x03A9, 0x2126}, new[] {0x004B, 0x212A}, new[] {0x00C5, 0x212B},
            new[] {0x0020, 0x00A0}, new[] {0x02BC, 0x2019},
        };
        for (int gid = 1; gid <= glyphs.Length; gid++) {
            foreach (int c in glyphs[gid - 1]) {
                unicodeToGID.Set(c, gid);
            }
        }
        int[] unicodeOf = GlyphMap.UnicodeOfGlyphs(unicodeToGID);
        int[] want = {0x03BC, 0x03A9, 0x004B, 0x00C5, 0x0020, 0x2019};
        for (int gid = 1; gid <= want.Length; gid++) {
            Assert.Equal(want[gid - 1], unicodeOf[gid]);
        }
    }

    // Returns the characters the ToUnicode maps of the objects give their
    // glyphs, each in hexadecimal, as 06F1.
    private static HashSet<string> ToUnicodeValues(List<PDFobj> objects) {
        HashSet<string> values = new HashSet<string>();
        Regex entry = new Regex("<[0-9A-Fa-f]{4}> <([0-9A-Fa-f]+)>");
        foreach (PDFobj obj in objects) {
            byte[] data = obj.GetData();
            if (data == null) {
                continue;
            }
            string cmap = TestSupport.Latin1(data);
            if (cmap.StartsWith("/CIDInit", StringComparison.Ordinal)) {
                foreach (Match match in entry.Matches(cmap)) {
                    values.Add(match.Groups[1].Value.ToUpperInvariant());
                }
            }
        }
        return values;
    }

    [Fact]
    public void AGlyphOfSeveralCharactersMapsToTheCharacterDrawn() {
        // A glyph a font draws for several characters maps to the character
        // the document drew with it, not to the lowest of them: a Persian ۱۳۹۸
        // was copied as the Arabic ١٣٩٨, and the full-width slash of km／h as a
        // division slash (review of 11 October 2026, #8). Noto Sans Arabic,
        // which this test read before 9.1.7, draws ۱, ۳, ۹ and ۸ with the
        // glyphs of ١, ٣, ٩ and ٨; IBM Plex Sans Arabic draws each with its
        // own, so its character map is made to draw them as Noto Sans
        // Arabic's does. IBM Plex Sans TC draws ／ with the glyph of ∕.
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font arabic = new Font(pdf, new MemoryStream(TestSupport.WithSharedGlyphs(
                TestSupport.FontBytes("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"),
                new int[][] {new[] {0x0661, 0x06F1}, new[] {0x0663, 0x06F3}, new[] {0x0669, 0x06F9}, new[] {0x0668, 0x06F8}})));
        Font tc = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf"));
        new TextLine(arabic, "\u06F1\u06F3\u06F9\u06F8").SetLocation(50f, 50f).DrawOn(page);
        new TextLine(tc, "60km\uFF0Fh").SetLocation(50f, 100f).DrawOn(page);
        pdf.Complete();
        HashSet<string> values = ToUnicodeValues(TestSupport.Read(stream.ToArray()));
        foreach (string want in new[] {"06F1", "06F3", "06F9", "06F8", "FF0F"}) {
            Assert.True(values.Contains(want), "no glyph maps to U+" + want);
        }
        foreach (string other in new[] {"0661", "0663", "0669", "2215"}) {
            Assert.False(values.Contains(other), "a glyph maps to U+" + other + ", which the document did not draw");
        }
    }

    [Fact]
    public void TheSecondCharacterOfAGlyphHasItsActualText() {
        // A document that draws two characters of one glyph gives the second its
        // own as actual text, as the ToUnicode map gives the glyph the first: "١٢٣"
        // and then "۱۲۳" copied the second as "١۲٣", and "∕" then "／" in a stamp
        // gave "∕" twice (review of 12 October 2026, R15).
        // Noto Sans Arabic, which this test read before 9.1.7, draws ۱ and ۳
        // with the glyphs of ١ and ٣, and ۲ with its own; IBM Plex Sans Arabic's
        // character map is made to draw them so. IBM Plex Sans TC draws ／ with
        // the glyph of ∕.
        PDF pdf = TestSupport.NewPDF();
        Font arabic = new Font(pdf, new MemoryStream(TestSupport.WithSharedGlyphs(
                TestSupport.FontBytes("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"),
                new int[][] {new[] {0x0661, 0x06F1}, new[] {0x0663, 0x06F3}})));
        Assert.Equal(arabic.unicodeToGID.Get(0x0661), arabic.unicodeToGID.Get(0x06F1));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(arabic, "\u0661\u0662\u0663").SetLocation(50f, 50f).DrawOn(page);
        Assert.DoesNotContain("/ActualText", TestSupport.Content(page));
        new TextLine(arabic, "\u06F1\u06F2\u06F3").SetLocation(50f, 100f).DrawOn(page);
        string content = TestSupport.Content(page);
        foreach (string c in new[] {"\u06F1", "\u06F3"}) {
            Assert.Contains("/ActualText <" + Page.ToUTF16Hex(c) + ">", content);
        }

        Font tc = new Font(pdf, TestSupport.RepoPath("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf"));
        Assert.Equal(tc.unicodeToGID.Get(0x2215), tc.unicodeToGID.Get(0xFF0F));
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        stamp.DrawText(tc, 12f, 0f, 12f, "1\u22152");
        stamp.DrawText(tc, 12f, 0f, 24f, "1\uFF0F2");
        content = StampContent(stamp);
        Assert.Contains("/ActualText <" + Page.ToUTF16Hex("\uFF0F") + ">", content);
        Assert.DoesNotContain("/ActualText <" + Page.ToUTF16Hex("\u2215") + ">", content);
    }

    [Fact]
    public void AStampKeepsAJoinerInTheActualText() {
        // A stamp gives a zero width non-joiner or joiner its font has no glyph
        // for to the actual text of the glyph before it, as a page does: it was left
        // out (review of 12 October 2026, R34).
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.RepoPath("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"));
        Assert.Equal(0, font.unicodeToGID.Get(0x200C));
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        stamp.DrawText(font, 12f, 0f, 12f, "a\u200C\u200Eb");
        string actualText = "/ActualText <" + Page.ToUTF16Hex("a\u200C") + ">";
        Assert.Contains(actualText, StampContent(stamp));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a\u200C\u200Eb").SetLocation(50f, 50f).DrawOn(page);
        Assert.Contains(actualText, TestSupport.Content(page));
    }

    private static string StampContent(Stamp stamp) {
        MemoryStream buf = (MemoryStream) typeof(Stamp).GetField("buf",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(stamp);
        return TestSupport.Latin1(buf.ToArray());
    }
}
}
