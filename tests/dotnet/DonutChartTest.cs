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
        Assert.Equal("0 0 0 rg", TestSupport.FillColorBefore(content, "Apples"));
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

    // A slice that is not a number, an infinity, or a value whose 360ths
    // overflow is not drawn, and the others are: one made the arc's loop
    // endless (review of 11 October 2026, #2).
    [Fact]
    public void ASliceThatIsNotANumberIsNotDrawn() {
        foreach (float bad in new float[] {float.NaN, float.PositiveInfinity, 1e36f}) {
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
}
}
