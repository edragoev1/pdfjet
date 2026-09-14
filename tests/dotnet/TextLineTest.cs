/*
 * TextLineTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class TextLineTest {
    [Fact]
    public void DrawOnWritesTheTextAsHexAtTheFlippedY() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine line = new TextLine(TestSupport.Helvetica(pdf), "Hello (x)").SetLocation(10f, 20f);
        float[] xy = line.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("1 0 0 1 10 772 Tm\n", content);
        Assert.Contains("[<" + TestSupport.Hex("Hello (x)") + ">] TJ\n", content);
        TestSupport.AssertNear(44.664f, line.GetWidth(), 0.001f);
        TestSupport.AssertNear(10f + line.GetWidth(), xy[0], TestSupport.DELTA);
    }

    [Fact]
    public void EmptyTextDrawsNothingAndReturnsTheLocation() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.AssertXY(5f, 6f, new TextLine(TestSupport.Helvetica(pdf), "").SetLocation(5f, 6f).DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void ColorSettersConvertAndCopy() {
        TextLine line = new TextLine(TestSupport.Helvetica(TestSupport.NewPDF()), "x");
        line.SetTextColor(0xFF8000);
        TestSupport.AssertRGB(1f, 128f / 255f, 0f, line.GetTextColor());

        float[] rgb = {0.1f, 0.2f, 0.3f};
        line.SetTextColor(rgb);
        rgb[0] = 0.9f;
        line.GetTextColor()[1] = 0.9f;
        TestSupport.AssertRGB(0.1f, 0.2f, 0.3f, line.GetTextColor());
    }

    [Fact]
    public void UnderlineAddsAStrokedLine() {
        PDF pdf = TestSupport.NewPDF();
        Page plain = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Hello").SetLocation(10f, 20f).DrawOn(plain);
        Assert.DoesNotContain("\nS\n", TestSupport.Content(plain));

        Page underlined = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Hello").SetLocation(10f, 20f).SetUnderline(true).DrawOn(underlined);
        Assert.Contains("\nS\n", TestSupport.Content(underlined));
    }

    [Fact]
    public void SetFontChangesTheFallbackFontUnlessAnotherWasSet() {
        PDF pdf = TestSupport.NewPDF();
        Font helvetica = TestSupport.Helvetica(pdf);
        Font courier = new Font(pdf, CoreFont.COURIER);
        TextLine line = new TextLine(helvetica, "x").SetFont(courier);
        Assert.Same(courier, line.GetFallbackFont());
        line.SetFallbackFont(helvetica).SetFont(new Font(pdf, CoreFont.TIMES_ROMAN));
        Assert.Same(helvetica, line.GetFallbackFont());
    }
}
}
