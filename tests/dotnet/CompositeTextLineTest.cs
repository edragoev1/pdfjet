/*
 * CompositeTextLineTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
public class CompositeTextLineTest {
    private static CompositeTextLine Water(Font font, float fontSize) {
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        if (fontSize > 0f) {
            composite.SetFontSize(fontSize);
        }
        TextLine h = new TextLine(font, "H");
        TextLine two = new TextLine(font, "2");
        two.SetScriptPosition(ScriptPosition.SUBSCRIPT);
        TextLine o = new TextLine(font, "O");
        composite.AddComponent(h);
        composite.AddComponent(two);
        composite.AddComponent(o);
        return composite;
    }

    [Fact]
    public void ASubscriptIsSmallerAndLowerWithoutSettingTheFontSize() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        // The font size of the components is the base when the composite text
        // line has none of its own.
        CompositeTextLine composite = Water(font, 0f);
        TextLine two = composite.GetTextLine(1);
        Assert.Equal(font.GetSize() * composite.GetSubscriptFactor(), two.GetFontSize(),
                TestSupport.DELTA);
        Assert.Equal(100f + font.GetSize() * composite.GetSubscriptPosition(),
                two.GetLocation()[1], TestSupport.DELTA);
        // The same composite text line with the font size set draws the same.
        CompositeTextLine withSize = Water(font, font.GetSize());
        Assert.Equal(two.GetFontSize(), withSize.GetTextLine(1).GetFontSize(), TestSupport.DELTA);
        Assert.Equal(two.GetLocation()[1], withSize.GetTextLine(1).GetLocation()[1],
                TestSupport.DELTA);
    }

    [Fact]
    public void TheFontSizeSetAfterTheComponentsWereAddedStillReachesThem() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        TextLine h = new TextLine(font, "H");
        TextLine two = new TextLine(font, "2");
        two.SetScriptPosition(ScriptPosition.SUBSCRIPT);
        composite.AddComponent(h).AddComponent(two);
        composite.SetFontSize(24f);
        Assert.Equal(24f, h.GetFontSize(), TestSupport.DELTA);
        Assert.Equal(24f * composite.GetSubscriptFactor(), two.GetFontSize(), TestSupport.DELTA);
        Assert.Equal(100f + 24f * composite.GetSubscriptPosition(), two.GetLocation()[1],
                TestSupport.DELTA);
    }

    [Fact]
    public void TheHeightIsTheExtentOfWhatIsDrawn() {
        PDF pdf = TestSupport.NewPDF();
        Font big = new Font(pdf, CoreFont.HELVETICA);
        big.SetSize(24f);
        Font small = new Font(pdf, CoreFont.HELVETICA);
        small.SetSize(8f);
        // The components come from fonts of their own sizes, and the composite
        // text line draws them at 12 points and at the subscript size.
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        composite.SetFontSize(12f);
        TextLine h = new TextLine(big, "H");
        TextLine two = new TextLine(small, "2");
        two.SetScriptPosition(ScriptPosition.SUBSCRIPT);
        composite.AddComponent(h).AddComponent(two);
        float[] minMax = composite.GetMinMaxY();
        Assert.Equal(100f - big.GetAscent(12f), minMax[0], TestSupport.DELTA);
        Assert.Equal(two.GetLocation()[1] + small.GetDescent(two.GetFontSize()), minMax[1],
                TestSupport.DELTA);
        Assert.Equal(minMax[1] - minMax[0], composite.GetHeight(), TestSupport.DELTA);
    }

    [Fact]
    public void AnEmptyCompositeTextLineMeasuresItsOwnLocation() {
        CompositeTextLine composite = new CompositeTextLine(100f, 50f);
        TestSupport.AssertXY(100f, 50f, composite.DrawOn(null));
        Assert.Equal(0f, composite.GetWidth());
        Assert.Equal(0f, composite.GetHeight());
    }

    [Fact]
    public void TheWidthAndTheLocationSurviveASecondSetLocation() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        CompositeTextLine composite = Water(font, 12f);
        float width = composite.GetWidth();
        composite.SetLocation(200f, 300f);
        composite.SetLocation(200f, 300f);
        Assert.Equal(width, composite.GetWidth(), TestSupport.DELTA);
        Assert.Equal(200f, composite.GetTextLine(0).GetLocation()[0], TestSupport.DELTA);
        Assert.Equal(300f, composite.GetTextLine(0).GetLocation()[1], TestSupport.DELTA);
    }

    [Fact]
    public void AFormulaSubscriptsTheAtomCounts() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        CompositeTextLine water = new CompositeTextLine(0f, 0f);
        water.AddFormula(font, "H2O");
        Assert.Equal(3, water.GetNumberOfTextLines());
        Assert.Equal("H", water.GetTextLine(0).GetText());
        Assert.Equal("2", water.GetTextLine(1).GetText());
        Assert.Equal("O", water.GetTextLine(2).GetText());
        Assert.Equal(ScriptPosition.SUBSCRIPT, water.GetTextLine(1).GetScriptPosition());
        Assert.Equal(ScriptPosition.NORMAL, water.GetTextLine(0).GetScriptPosition());
        Assert.True(water.GetTextLine(1).GetFontSize() < water.GetTextLine(0).GetFontSize());

        CompositeTextLine glucose = new CompositeTextLine(0f, 0f);
        glucose.AddFormula(font, "C6H12O6");
        Assert.Equal(6, glucose.GetNumberOfTextLines());
        Assert.Equal("12", glucose.GetTextLine(3).GetText());
        Assert.Equal(ScriptPosition.SUBSCRIPT, glucose.GetTextLine(3).GetScriptPosition());

        // A digit that begins the formula counts the molecules, not the atoms.
        CompositeTextLine coefficient = new CompositeTextLine(0f, 0f);
        coefficient.AddFormula(font, "2H2O");
        Assert.Equal("2H", coefficient.GetTextLine(0).GetText());
        Assert.Equal(ScriptPosition.NORMAL, coefficient.GetTextLine(0).GetScriptPosition());
        Assert.Equal(ScriptPosition.SUBSCRIPT, coefficient.GetTextLine(1).GetScriptPosition());

        // The digits of a group in brackets follow the bracket.
        CompositeTextLine lime = new CompositeTextLine(0f, 0f);
        lime.AddFormula(font, "Ca(OH)2");
        Assert.Equal("Ca(OH)", lime.GetTextLine(0).GetText());
        Assert.Equal(ScriptPosition.SUBSCRIPT, lime.GetTextLine(1).GetScriptPosition());
    }

    [Fact]
    public void AFormulaSuperscriptsTheChargeAfterACircumflex() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        CompositeTextLine calcium = new CompositeTextLine(0f, 0f);
        calcium.AddFormula(font, "Ca^2+");
        Assert.Equal(2, calcium.GetNumberOfTextLines());
        Assert.Equal("Ca", calcium.GetTextLine(0).GetText());
        Assert.Equal("2+", calcium.GetTextLine(1).GetText());
        Assert.Equal(ScriptPosition.SUPERSCRIPT, calcium.GetTextLine(1).GetScriptPosition());
        Assert.True(calcium.GetTextLine(1).GetLocation()[1]
                < calcium.GetTextLine(0).GetLocation()[1]);

        CompositeTextLine sulfate = new CompositeTextLine(0f, 0f);
        sulfate.AddFormula(font, "SO4^2-");
        Assert.Equal(3, sulfate.GetNumberOfTextLines());
        Assert.Equal(ScriptPosition.SUBSCRIPT, sulfate.GetTextLine(1).GetScriptPosition());
        Assert.Equal(ScriptPosition.SUPERSCRIPT, sulfate.GetTextLine(2).GetScriptPosition());

        // Nothing at all is one component of nothing.
        CompositeTextLine empty = new CompositeTextLine(0f, 0f);
        empty.AddFormula(font, "");
        Assert.Equal(0, empty.GetNumberOfTextLines());
    }

    [Fact]
    public void ACellDrawsACompositeTextLineWithoutAnyText() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Cell cell = new Cell(font);
        CompositeTextLine composite = new CompositeTextLine(0f, 0f);
        composite.AddFormula(font, "H2O");
        cell.SetCompositeTextLine(composite);
        // The cell is as tall as the taller of its font and the composite.
        float fontHeight = font.GetBodyHeight(font.GetSize());
        float expected = Math.Max(fontHeight, composite.GetHeight())
                + cell.GetTopPadding() + cell.GetBottomPadding();
        Assert.Equal(expected, cell.GetHeight(100f), TestSupport.DELTA);
        cell.DrawOn(page, 50f, 50f, 100f, cell.GetHeight(100f));
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("H"), content);
        Assert.Contains(TestSupport.Hex("2"), content);
        Assert.Contains(TestSupport.Hex("O"), content);
    }
}
}   // End of namespace PDFjet.NET
