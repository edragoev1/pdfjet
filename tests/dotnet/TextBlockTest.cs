/*
 * TextBlockTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Globalization;
using System;
using System.Collections.Generic;
using Xunit;

namespace PDFjet.NET {
public class TextBlockTest {
    private const string TEN_WORDS = "one two three four five six seven eight nine ten";

    [Fact]
    public void ANewlineIsOneEmptyLine() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        float[] x = new TextBlock(font, "x").SetLocation(0f, 0f).DrawOn(null);
        TestSupport.AssertXY(500f, 13.872f, x);
        TestSupport.AssertXY(x[0], x[1], new TextBlock(font, "\n").SetLocation(0f, 0f).DrawOn(null));
        TestSupport.AssertXY(x[0], x[1], new TextBlock(font, "").SetLocation(0f, 0f).DrawOn(null));
    }

    [Fact]
    public void WithoutAHeightTheBlockIsAsTallAsItsText() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TextBlock block = new TextBlock(font, TEN_WORDS).SetLocation(0f, 0f).SetWidth(60f);
        // Six lines of 13.872 points.
        TestSupport.AssertXY(60f, 83.232f, block.DrawOn(null));
        TestSupport.AssertNear(83.232f, block.GetHeight(), TestSupport.DELTA);
        Assert.Equal(60f, block.GetWidth());
    }

    [Fact]
    public void AHeightCutsTheTextThatDoesNotFitAndAlignsTheRest() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        TextBlock block = new TextBlock(font, TEN_WORDS).SetLocation(0f, 0f).SetSize(60f, 30f);
        // Two of the six lines fit
        TestSupport.AssertXY(60f, 30f, block.DrawOn(null));
        Assert.Equal(30f, block.GetHeight());
        Page page = new Page(pdf, Letter.PORTRAIT);
        block.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("..."), content);
        Assert.DoesNotContain(TestSupport.Hex("ten"), content);

        // Aligned to the bottom of a block 10 points taller than its two lines,
        // the text sits where a block without a height draws it 10 points lower
        page = new Page(pdf, Letter.PORTRAIT);
        block.SetSize(60f, 2 * 13.872f + 10f).SetVerticalAlignment(Alignment.BOTTOM).DrawOn(page);
        string bottom = TestSupport.Content(page);
        page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "one two three four").SetLocation(0f, 10f).SetWidth(60f).DrawOn(page);
        string lower = TestSupport.Content(page);
        int start = lower.IndexOf("1 0 0 1 0 ");
        string textMatrix = lower.Substring(start, lower.IndexOf(" Tm\n") + 4 - start);
        Assert.Contains(textMatrix, bottom);
    }

    [Fact]
    public void AWrappedLineIsDrawnWithTheSpaceItWasWrappedAt() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, TEN_WORDS).SetLocation(0f, 0f).SetWidth(60f).DrawOn(page);
        string content = TestSupport.Content(page);
        // Six lines: after each of the first five, the space it was wrapped at,
        // so that a screen reader and a text extractor see the word break; the
        // last line has none.
        string space = "[<" + TestSupport.Hex(" ") + ">] TJ\n";
        Assert.Equal(5, (content.Length - content.Replace(space, "").Length) / space.Length);
        Assert.Contains("[<" + TestSupport.Hex("ten") + ">] TJ\nET", content);
        // Drawn after the line, not in its text: the line is measured without it
        Assert.Contains("[<" + TestSupport.Hex("one two") + ">] TJ\n", content);
    }

    [Fact]
    public void PaddingWiderThanTheBlockDrawsOneCharacterALine() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        // The text area was negative, and the word was broken past its end.
        TextBlock block = new TextBlock(font, "Hello");
        block.SetLocation(50f, 50f).SetWidth(100f).SetPadding(60f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("H"), content);
        Assert.Contains(TestSupport.Hex("o"), content);
    }

    [Fact]
    public void TheUnderlineIsDrawnInTheTextColorAtTheFontThickness() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetPenColor(Color.red);     // the underline took the pen color
        TextBlock block = new TextBlock(font, "Hello");
        block.SetLocation(50f, 50f).SetTextColor(Color.blue);
        block.SetUnderline(true);
        block.SetBorderWidth(3f);        // and the border width
        block.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("0 0 1 RG", content);
        Assert.DoesNotContain("3 w", content);
        Assert.Contains(font.GetUnderlineThickness(font.GetSize()).ToString(CultureInfo.InvariantCulture) + " w", content);
    }

    [Fact]
    public void TheCornerRadiusRoundsABackgroundWithoutABorder() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock block = new TextBlock(font, "Hello");
        block.SetLocation(50f, 50f).SetBackgroundColor(Color.yellow);
        block.SetCornerRadius(10f);
        block.DrawOn(page);
        // A rounded rectangle draws its corners with the curve operator.
        Assert.Contains(" c\n", TestSupport.Content(page));
    }

    [Fact]
    public void ANullTextColorLeavesTheTextColorUnchanged() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.SetBrushColor(Color.red);
        TextBlock block = new TextBlock(font, "Hi");
        block.SetLocation(50f, 50f);
        block.SetTextColor((float[]) null);
        block.DrawOn(page);
        TestSupport.AssertRGB(0f, 0f, 0f, block.GetTextColor());
        Assert.Contains("0 0 0 rg", TestSupport.Content(page));
    }

    [Fact]
    public void StrikeoutDrawsALineThroughEachLine() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextBlock(font, "one\ntwo").SetLocation(0f, 0f).SetStrikeout(true).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(2, content.Split(" l\n").Length - 1);
    }

    [Fact]
    public void DrawOnAPageWritesEveryWord() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] xy = new TextBlock(TestSupport.Helvetica(pdf), TEN_WORDS).SetLocation(0f, 0f).SetWidth(60f).DrawOn(page);
        TestSupport.AssertXY(60f, 83.232f, xy);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("one"), content);
        Assert.Contains(TestSupport.Hex("ten"), content);
    }

    [Fact]
    public void TransparentLeavesTheTextColorUnchanged() {
        TextBlock block = new TextBlock(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        block.SetTextColor(Color.blue).SetTextColor(Color.transparent);
        TestSupport.AssertRGB(0f, 0f, 1f, block.GetTextColor());
    }
    [Fact]
    public void IsTaggedAsItsStructureType() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextBlock heading = new TextBlock(font, "Invoice").SetStructureType(StructElem.H1);
        heading.SetLocation(50f, 50f);
        heading.DrawOn(page);
        TextBlock paragraph = new TextBlock(font, "Thank you for your order.");
        paragraph.SetLocation(50f, 100f);
        paragraph.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("/H1 <</MCID 0>>", content);
        Assert.Contains("/P <</MCID 1>>", content);
    }

    [Fact]
    public void AWordIsBrokenBetweenItsCharactersAsTheGoPortBreaksIt() {
        // Five lines: pneumono, ultramicros, copicsilico, volcanoco, niosis
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TextBlock block = new TextBlock(font, "pneumonoultramicroscopicsilicovolcanoconiosis").SetWidth(60f);
        TestSupport.AssertXY(60f, 69.36f, block.SetLocation(0f, 0f).DrawOn(null));
    }

    [Fact]
    public void ALongWordIsBrokenInTimeThatGrowsWithTheWord() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        var word = new System.Text.StringBuilder();
        for (int i = 0; i < 10000; i++) {
            word.Append("abcdefghij");      // 100,000 characters
        }
        TextBlock block = new TextBlock(font, word.ToString()).SetWidth(100f);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        float[] xy = block.SetLocation(0f, 0f).DrawOn(null);
        Assert.True(watch.ElapsedMilliseconds < 3000, "breaking a word of 100,000 characters took " + watch.ElapsedMilliseconds + " ms");
        TestSupport.AssertXY(100f, 78030f, xy);     // 5625 lines of 13.872 points
    }

    [Fact]
    public void ALongRightToLeftWordIsBrokenInTimeThatGrowsWithTheWord() {
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"));
        var word = new System.Text.StringBuilder();
        for (int i = 0; i < 25000; i++) {
            word.Append("محمد");      // 100,000 letters, which took about 20 minutes
        }
        TextBlock block = new TextBlock(font, word.ToString()).SetRightToLeft(true).SetWidth(100f);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        float[] xy = block.SetLocation(0f, 0f).DrawOn(null);
        Assert.True(watch.ElapsedMilliseconds < 3000, "breaking a right to left word of 100,000 letters took " + watch.ElapsedMilliseconds + " ms");
        TestSupport.AssertXY(100f, 120006f, xy);    // As the Go port breaks it
    }

    [Fact]
    public void ARightToLeftTextCutToItsHeightKeepsTheStartOfItsLastLine() {
        // A right to left text cut to its height keeps the start of its last
        // line, at the right as it is drawn, and its "..." at the left, where
        // the line ends in the reading (review of 11 October 2026, #7).
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"));
        string text = "שלום עולם אחד שתיים שלוש ארבע חמש שש שבע שמונה";
        TextLine[] lines = new TextBlock(font, text).SetRightToLeft(true).SetWidth(80f).Layout().textLines;
        TextLine[] fit = new TextBlock(font, text).SetRightToLeft(true).SetSize(80f, 20f).Layout().textLines;
        Assert.True(fit.Length < lines.Length, "the text is not cut: " + fit.Length + " lines of " + lines.Length);
        string last = lines[fit.Length - 1].text;
        string got = fit[fit.Length - 1].text;
        int start = 0;
        for (int k = 0; k < 3; k++) {
            start += char.IsSurrogatePair(last, start) ? 2 : 1;
        }
        Assert.True(got.StartsWith("...", StringComparison.Ordinal) &&
                got.EndsWith(last.Substring(start), StringComparison.Ordinal),
                "the last line \"" + got + "\", cut from \"" + last + "\", has lost its start");
    }

    [Fact]
    public void AnIgnorableCharacterIsNotDrawn() {
        // A character Unicode does not draw without a glyph, as the variation
        // selector after ™ in text pasted from a phone, a zero width space or a
        // word joiner, takes no width and draws nothing, where it was a box
        // with a width (review of 11 October 2026, #9).
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        string plain = "Brand™ © 2026 word";
        string marked = "Brand™\uFE0F ©\uFE0F 2026\u200B wo\u2060rd\U000E0041";
        Assert.Equal(font.StringWidth(12f, plain), font.StringWidth(12f, marked));
        Func<string, string> draw = text => {
            Page page = new Page(pdf, Letter.PORTRAIT);
            TextLine line = new TextLine(font, text);
            line.SetLocation(10f, 10f);
            line.DrawOn(page);
            return TestSupport.Content(page);
        };
        Assert.Equal(draw(plain), draw(marked));
    }

    [Fact]
    public void ACoreFontDrawsNoIgnorableCharacter() {
        // A core font does not draw or measure them either, nor the marks of
        // direction, the joiners and the byte order mark: they were spaces, so that
        // "©️ 2026" had two (review of 12 October 2026, R14). GetFitChars kerns with
        // the next character drawn, not with a soft hyphen, as StringWidth does (R19).
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, CoreFont.HELVETICA);
        font.SetSize(12f);
        string plain = "ab \u00A9 2026 word";
        string marked = "a\uFE0Fb \u00A9\uFE0F 2026\u200B wo\u2060r\u200Dd\u200E\u200C\u200F\uFEFF\U000E0041";
        Assert.Equal(font.StringWidth(12f, plain), font.StringWidth(12f, marked));
        Func<string, string> draw = text => {
            Page page = new Page(pdf, Letter.PORTRAIT);
            TextLine line = new TextLine(font, text);
            line.SetLocation(10f, 10f);
            line.DrawOn(page);
            return TestSupport.Content(page);
        };
        Assert.Equal(draw(plain), draw(marked));
        // A hair over the width, as float rounds it
        font.SetKernPairs(true);
        float width = font.StringWidth(12f, "AVAV") + 0.001f;
        Assert.Equal(6, font.GetFitChars("A\u00ADVA\u00ADV", width));
    }

    [Fact]
    public void AStampLeavesOutTheMarksOfDirection() {
        // A Stamp leaves out the marks of direction and the joiners its font
        // has no glyph for, as Page does: Hebrew with brackets, reordered for
        // drawing, wrote two .notdef glyphs (review of 11 October 2026, #10).
        PDF pdf = TestSupport.NewPDF();
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"));
        string text = Bidi.ReorderVisually("שלום (abc) עולם");
        Assert.True(text.IndexOf('\u200E') >= 0 || text.IndexOf('\u200F') >= 0,
                "the text has no marks of direction: " + text);
        Stamp stamp = new Stamp(pdf).SetSize(100f, 50f);
        stamp.DrawText(font, 12f, 0f, 12f, text);
        System.IO.MemoryStream buf = (System.IO.MemoryStream) typeof(Stamp).GetField("buf",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(stamp);
        string content = TestSupport.Latin1(buf.ToArray());
        // A mark drawn as a glyph the font lacks is in a span of its own, after
        // the first run of glyphs; this text has no character the font lacks
        Assert.False(content.Contains("/ActualText"), "the stamp draws a mark as a glyph the font lacks: " + content);
        int start = content.IndexOf('<');
        int end = content.IndexOf('>');
        string glyphs = content.Substring(start + 1, end - start - 1);
        for (int i = 0; i + 4 <= glyphs.Length; i += 4) {
            Assert.False(glyphs.Substring(i, 4) == "0000", "the stamp draws .notdef: " + content);
        }
    }

    // SoftHyphenWord is a word of soft hyphens, and SoftHyphenWidth a width
    // that fits its start up to the first of them that a line can break at
    // past "Donau", with the hyphen, and no more.
    internal const string SoftHyphenWord = "Donau\u00addampf\u00adschiff\u00adfahrt";

    internal static float SoftHyphenWidth(Font font) {
        return font.StringWidth(font.GetSize(), "Donaudampf-") + 1f;
    }

    // A word too wide for its line breaks at a soft hyphen, the line ending
    // with a hyphen; it was drawn as a hyphen wherever it was, and broken
    // between its characters (review of 11 October 2026, #21).
    [Fact]
    public void BreaksAWordAtASoftHyphen() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TextBlock block = new TextBlock(font, "x " + SoftHyphenWord);
        block.SetWidth(SoftHyphenWidth(font) + 2f * block.GetPadding());
        List<string> lines = new List<string>();
        foreach (TextLine line in block.GetTextLines()) {
            lines.Add(line.text.Replace("\u00ad", ""));
        }
        string all = "[" + string.Join("|", lines) + "]";
        Assert.True(lines.Count >= 2 && lines[0] == "x Donau-", "the lines " + all);
        for (int i = 0; i < lines.Count - 1; i++) {
            Assert.True(lines[i].EndsWith("-", StringComparison.Ordinal),
                    "the lines " + all + ": " + lines[i] + " is not broken at a soft hyphen");
        }
        Assert.Equal("x Donaudampfschifffahrt", string.Join("", lines).Replace("-", ""));
        // A word that fits draws no hyphen.
        TextLine[] fits = new TextBlock(font, SoftHyphenWord).SetWidth(500f).GetTextLines();
        Assert.True(fits.Length == 1 && !fits[0].text.Contains('-'), "the lines of a word that fits: " + fits.Length);
    }

    [Fact]
    public void AWordBrokenBetweenCharactersBreaksAtASoftHyphen() {
        // A word broken between its characters breaks at a soft hyphen where one
        // fits, as TextFrame breaks each row: "1234\u00ad5678" was "12", "34", "56",
        // "78" with no hyphen, where TextFrame draws "34-", and a line ran across
        // the soft hyphen of "aaaaaaaaaa\u00adbbbbbbbbbb" where "aa-" fit (review of
        // 12 October 2026, R18).
        using IDisposable coreFonts = TestSupport.AllowCoreFonts();
        Font font = new Font(TestSupport.NewPDF(), CoreFont.HELVETICA);
        // Two digits and a hyphen fit, three digits do not.
        Assert.Equal("12|34-|56|78", LineTexts(font, "1234\u00AD5678",
                font.StringWidth(font.GetSize(), "00-") + 1f, false));
        Assert.Equal("aaaa|aaaa|aa-|bbbb|bbbb|bb", LineTexts(font, "aaaaaaaaaa\u00ADbbbbbbbbbb",
                font.StringWidth(font.GetSize(), "aaaa") + 1f, false));
    }

    [Fact]
    public void ARightToLeftWordBreaksAtASoftHyphen() {
        // A right to left text block breaks a word at a soft hyphen, the hyphen at
        // the end of the line, on its left: it was cut between two characters, with
        // no hyphen (review of 12 October 2026, R17).
        Font font = new Font(TestSupport.NewPDF(), TestSupport.Open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"));
        font.SetSize(12f);
        float width = font.StringWidth(12f, "\u05E9\u05DC\u05D5\u05DD-") + 1f;
        string word = "\u05E9\u05DC\u05D5\u05DD\u00AD\u05E2\u05D5\u05DC\u05DD\u00AD\u05D0\u05D7\u05D3";
        string lines = "-\u05DD\u05D5\u05DC\u05E9|-\u05DD\u05DC\u05D5\u05E2|\u05D3\u05D7\u05D0";
        Assert.Equal(lines, LineTexts(font, word, width, true));
        Assert.Equal("\u05D0|" + lines, LineTexts(font, "\u05D0 " + word, width, true));
    }

    // Returns the lines of a text block of the text, at the width of text, joined by "|".
    private static string LineTexts(Font font, string text, float width, bool rightToLeft) {
        TextBlock block = new TextBlock(font, text);
        block.SetRightToLeft(rightToLeft);
        block.SetWidth(width + 2f * block.GetPadding());
        List<string> lines = new List<string>();
        foreach (TextLine line in block.GetTextLines()) {
            lines.Add(line.text.Replace("\u00AD", ""));
        }
        return string.Join("|", lines);
    }
}
}
