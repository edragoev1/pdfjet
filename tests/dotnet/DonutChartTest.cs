/*
 * DonutChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class DonutChartTest {
    private static DonutChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new DonutChart(font, font).SetLocation(100f, 100f).SetRadii(100f, 50f);
    }

    [Fact]
    public void AChartWithoutValuesDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).AddSlice(new Slice(0f, Color.red, "none"));
        TestSupport.AssertXY(300f, 300f, chart.DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void SlicesArePercentagesOfTheSumOfTheValues() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf);
        chart.AddSlice(new Slice(1f, Color.red, "a"));
        chart.AddSlice(new Slice(1f, Color.green, "b"));
        chart.AddSlice(new Slice(2f, Color.blue, "c"));
        TestSupport.AssertXY(300f, 300f, chart.DrawOn(page));
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("25%"), content);
        Assert.Contains(TestSupport.Hex("50%"), content);
        Assert.DoesNotContain(TestSupport.Hex("100%"), content);
    }

    [Fact]
    public void APieChartHasAnInnerRadiusOfZero() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).SetRadii(100f, 0f);
        chart.AddSlice(new Slice(3f, Color.red, "a")).AddSlice(new Slice(1f, Color.blue, "b"));
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("75%"), content);
        Assert.Contains("200 592 l", content);   // the center, in PDF coordinates
    }

    [Fact]
    public void AChartIsAFigureThatListsItsSlices() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart donut = NewChart(pdf).AddSlice(new Slice(25f, Color.red, "Apples"))
                .AddSlice(new Slice(75f, Color.blue, "Oranges"));
        donut.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\n", content);
        Assert.EndsWith("EMC\n", content);
        Assert.Equal("0.13 0.15 0.17 rg", TestSupport.FillColorBefore(content, "Apples"));
        Assert.Equal("1 1 1 rg", TestSupport.FillColorBefore(content, "25%"));
        DonutChart pie = NewChart(pdf).SetRadii(100f, 0f).AddSlice(new Slice(1f, Color.red, ""));
        pie.DrawOn(page);
        DonutChart described = NewChart(pdf).SetAltDescription("Most are oranges.")
                .AddSlice(new Slice(1f, Color.red, "Apples"));
        described.DrawOn(page);
        Assert.Equal("Donut chart: Apples 25%, Oranges 75%", page.structures[0].altDescription);
        Assert.Equal("Pie chart: 100%", page.structures[1].altDescription);
        Assert.Equal("Most are oranges.", page.structures[2].altDescription);
    }

    // Slices in the palette, navy first; thin white lines between them; thin
    // gray leader lines; the percentage white on navy, ink on the light steel
    // blue; and no black line or text.
    [Fact]
    public void TheDefaultLookIsQuiet() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).AddSlice(new Slice(60f, "a")).AddSlice(new Slice(40f, "b")).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("0.12 0.23 0.37 rg", content);       // navy
        Assert.Contains("0.44 0.58 0.76 rg", content);       // steel blue
        Assert.Contains("1 1 1 RG\n1.5 w\n", content);       // the white lines between
        Assert.Contains("0.55 0.59 0.64 RG\n0.75 w\n", content);   // the leader lines
        Assert.DoesNotContain("0 0 0 RG", content);
        Assert.DoesNotContain("0 0 0 rg", content);
        Assert.Equal("1 1 1 rg", TestSupport.FillColorBefore(content, "60%"));
        Assert.Equal("0.13 0.15 0.17 rg", TestSupport.FillColorBefore(content, "40%"));
    }

    // The title at the location, the subtitle under it, the circle under
    // both; a chart without them has the circle at its location, as before.
    [Fact]
    public void ATitleIsDrawnAboveTheCircle() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        // The circle, and its label 15 under it
        TestSupport.AssertXY(300f, 315f, NewChart(pdf).AddSlice(new Slice(1f, "a")).DrawOn(page));
        float[] xy = NewChart(pdf).SetTitle("Fruit").SetSubtitle("This week")
                .AddSlice(new Slice(1f, "a")).DrawOn(page);
        Assert.Equal(300f, xy[0], 0.01f);
        Assert.True(xy[1] > 315f, "the circle is under the title: " + xy[1]);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("Fruit"), content);
        Assert.Contains(TestSupport.Hex("This week"), content);
        Assert.Equal("0.37 0.41 0.45 rg", TestSupport.FillColorBefore(content, "This week"));
        Assert.Equal("Fruit, This week: a 100%", page.structures[1].altDescription);
    }

    // A slice that is not a number or an infinity is not drawn, and the
    // others are: one made the arc's loop endless (review of 11 October 2026,
    // #2). A value whose 360ths are past the largest float is drawn, as it
    // compares with the others (review of 26 October 2026).
    [Fact]
    public void ASliceThatIsNotANumberIsNotDrawn() {
        foreach (float bad in new float[] {float.NaN, float.PositiveInfinity}) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            DonutChart chart = NewChart(pdf);
            chart.AddSlice(new Slice(bad, Color.red, "bad"));
            chart.AddSlice(new Slice(1f, Color.green, "a"));
            chart.AddSlice(new Slice(3f, Color.blue, "b"));
            chart.DrawOn(page);
            string content = TestSupport.Content(page);
            Assert.Contains(TestSupport.Hex("25%"), content);
            Assert.Contains(TestSupport.Hex("75%"), content);
        }
        PDF pdf2 = TestSupport.NewPDF();
        Page page2 = new Page(pdf2, Letter.PORTRAIT);
        NewChart(pdf2).AddSlice(new Slice(3e38f, "a")).AddSlice(new Slice(3e38f, "b")).DrawOn(page2);
        Assert.Contains(TestSupport.Hex("50%"), TestSupport.Content(page2));
    }

    // A slice is computed with the precision of Go's port, float or double
    // step by step, so the ports draw the same curves: the square root in
    // double moved a control point by a hundredth in 133 of 100,000 segments
    // (review of 11 October 2026, #25). The expected curves are Go's.
    [Fact]
    public void ASliceIsDrawnAsTheGoPortDrawsIt() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = new DonutChart(null, null).SetRadii(46.94574f, 92.64224f);
        foreach (float value in new float[] {524.55023f, 80.410065f, 7.852018f, 515.9275f, 931.2741f}) {
            chart.AddSlice(new Slice(value, 0x123456, ""));
        }
        chart.SetLocation(247.509f, 97.883514f).DrawOn(page);
        Assert.Contains("341.4 646.71 341.39 646.26 341.38 645.8 c\n", TestSupport.Content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        chart = new DonutChart(null, null).SetRadii(179.36f, 65.845535f);
        foreach (float value in new float[] {472.8191f, 308.4593f, 142.94768f, 299.98398f}) {
            chart.AddSlice(new Slice(value, 0x123456, ""));
        }
        chart.SetLocation(88.44436f, 261.47784f).DrawOn(page);
        Assert.Contains("131.87 234.15 131.39 234.7 130.91 235.27 c\n", TestSupport.Content(page));
    }

    // A null slice is left out: it threw when the chart was drawn.
    [Fact]
    public void ANullSliceIsLeftOut() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).AddSlice(null).AddSlice(new Slice(1f, "a")).DrawOn(page);
        Assert.Contains(TestSupport.Hex("a"), TestSupport.Content(page));
    }

    // The labels of thin slices side by side are moved apart, a line apart at
    // least; the percentages add up to 100; the white line by a thin slice is
    // narrower than it; and DrawOn returns the bottom right corner of the
    // labels too, measured as drawn (review of 26 October 2026).
    [Fact]
    public void ThinSlicesKeepTheirLabelsApart() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).AddSlice(new Slice(90f, "Big"));
        for (int i = 0; i < 6; i++) {
            chart.AddSlice(new Slice(1.5f, "Small " + i));
        }
        float[] measured = chart.DrawOn(null);
        float[] xy = chart.DrawOn(page);
        TestSupport.AssertXY(measured[0], measured[1], xy);
        string content = TestSupport.Content(page);
        Font font = TestSupport.Helvetica(pdf);
        for (int i = 1; i < 6; i++) {
            float above = TestSupport.PositionOf(content, "Small " + (i - 1))[1];
            float below = TestSupport.PositionOf(content, "Small " + i)[1];
            Assert.True(System.Math.Abs(above - below) >= font.GetBodyHeight() - 0.01f, above + " " + below);
        }
        Assert.Equal("Donut chart: Big 91%, Small 0 2%, Small 1 2%, Small 2 2%, Small 3 1%, Small 4 1%, Small 5 1%",
                page.structures[0].altDescription);
        Assert.Contains("1.5 w\n", content);                                                      // By the big slice
        Assert.True(System.Text.RegularExpressions.Regex.IsMatch(content, "\n0\\.\\d+ w\n"), content);    // Between two thin ones
    }
}
}
