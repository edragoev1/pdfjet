/*
 * TextBlockTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
        Assert.Contains(font.GetUnderlineThickness(font.GetSize()) + " w", content);
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
}
}
