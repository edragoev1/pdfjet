/*
 * FontTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
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
    public void ReadsAStreamFont() {
        string path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        if (!File.Exists(TestSupport.RepoPath(path))) {
            return;     // The fonts directory is not here.
        }
        using (Stream stream = TestSupport.Open(path)) {
            Font font = new Font(TestSupport.NewPDF(), stream);
            Assert.Equal("IBMPlexSans", font.GetName());
            TestSupport.AssertNear(28.32f, font.StringWidth(12f, "Hello"), 0.001f);
        }
    }

    // The embedded font file of a PDF that draws a line of text in the font.
    private static byte[] EmbeddedFontFile(byte[] fontStream) {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output);
        Font font = new Font(pdf, new MemoryStream(fontStream));
        TextLine text = new TextLine(font, "Hello");
        text.SetLocation(50f, 50f);
        text.DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        foreach (PDFobj obj in TestSupport.Read(output.ToArray())) {
            if (obj.GetValue("/Subtype") == "/CIDFontType0C") {
                return obj.GetData();
            }
        }
        return null;
    }

    private static int Int32At(byte[] buffer, int i) {
        return (buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | buffer[i + 3];
    }

    [Fact]
    public void AnOpenTypeStreamFontKeepsItsOtherTablesAndEmbedsOnlyItsCFFData() {
        string path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        if (!File.Exists(TestSupport.RepoPath(path))) {
            return;     // The fonts directory is not here.
        }
        byte[] whole = File.ReadAllBytes(TestSupport.RepoPath(path));
        // The name, the info and the metrics come first, then 'R' with the
        // length of the other tables of the font, and then the CFF data.
        int i = 1 + whole[0];
        i += 3 + ((whole[i] << 16) | (whole[i + 1] << 8) | whole[i + 2]);
        i += 4 + Int32At(whole, i);
        Assert.Equal((byte) 'R', whole[i]);
        int length = Int32At(whole, i + 1);
        // The same stream without the tables, as streams were written before.
        byte[] cffOnly = new byte[whole.Length - 5 - length];
        Array.Copy(whole, 0, cffOnly, 0, i);
        Array.Copy(whole, i + 5 + length, cffOnly, i, whole.Length - i - 5 - length);
        Assert.Equal((byte) 'Y', cffOnly[i]);

        byte[] embedded = EmbeddedFontFile(whole);
        Assert.True(embedded.Length > 0);
        Assert.Equal(embedded, EmbeddedFontFile(cffOnly));
    }

    // The content of a page with a line of Thai in the font: po pla, the upper
    // vowel sara ii on it and the tone mark mai ek above the vowel.
    private static string ThaiContent(string path) {
        PDF pdf = TestSupport.NewPDF();
        Font font;
        using (Stream stream = TestSupport.Open(path)) {
            font = new Font(pdf, stream);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine text = new TextLine(font, "\u0E1B\u0E35\u0E48");
        text.SetLocation(50f, 50f);
        text.DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void AStreamFontPlacesTheMarksAsTheOpenTypeFontDoes() {
        if (!File.Exists(TestSupport.RepoPath("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"))) {
            return;     // The fonts directory is not here.
        }
        Assert.Equal(ThaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"), ThaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf.stream"));
    }

    [Fact]
    public void ACoreFontNumberOutsideTheFourteenIsRejected() {
        PDF pdf = TestSupport.NewPDF();
        Assert.Throws<ArgumentException>(() => new Font(pdf, 0));
        Assert.Throws<ArgumentException>(() => new Font(pdf, 15));
    }

    [Fact]
    public void TheLineGapOfAFontSpacesTheLinesOfATextBlock() {
        string path = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream";
        if (!File.Exists(TestSupport.RepoPath(path))) {
            return;     // The fonts directory is not here.
        }
        PDF pdf = TestSupport.NewPDF();
        Font jp = new Font(pdf, TestSupport.Open(path));
        TestSupport.AssertNear(10f, jp.GetLineGap(10f), 0.001f);
        TestSupport.AssertNear(10f, new Font(pdf, TestSupport.Open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")).GetLineGap(10f), 0.001f);
        Assert.Equal(0f, new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
                .GetLineGap(10f));
        // The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
        jp.SetSize(10f);
        TestSupport.AssertXY(500f, 40f, new TextBlock(jp, "日本\n日本").SetLocation(0f, 0f).DrawOn(null));
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
    public void ASoftHyphenIsAHyphenAndANoBreakSpaceIsASpace() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        font.SetKernPairs(true);
        Assert.Equal(font.StringWidth(10f, "T-"), font.StringWidth(10f, "T\u00ad"));
        Assert.Equal(font.StringWidth(10f, ". "), font.StringWidth(10f, ".\u00a0"));
        // KPX period space -60
        TestSupport.AssertNear(4.96f, font.StringWidth(10f, ".\u00a0"), 0.001f);
    }
}
}
