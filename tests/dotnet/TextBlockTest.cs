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
    public void WrappedTextMakesTheBlockTallerThanItsSetHeight() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        TextBlock block = new TextBlock(font, TEN_WORDS).SetLocation(0f, 0f).SetSize(60f, 10f);
        // Six lines of 13.872 points.
        TestSupport.AssertXY(60f, 83.232f, block.DrawOn(null));
        TestSupport.AssertNear(83.232f, block.GetHeight(), TestSupport.DELTA);
        Assert.Equal(60f, block.GetWidth());
    }

    [Fact]
    public void DrawOnAPageWritesEveryWord() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] xy = new TextBlock(TestSupport.Helvetica(pdf), TEN_WORDS).SetLocation(0f, 0f).SetSize(60f, 10f).DrawOn(page);
        TestSupport.AssertXY(60f, 83.232f, xy);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("one"), content);
        Assert.Contains(TestSupport.Hex("ten"), content);
    }
}
}
