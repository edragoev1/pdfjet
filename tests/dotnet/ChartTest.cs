/*
 * ChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Globalization;
using Xunit;

namespace PDFjet.NET {
public class ChartTest {
    private static Chart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new Chart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f);
    }

    private static string Draw(params float[] ys) {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf);
        Series series = chart.AddSeries("");
        for (int i = 0; i < ys.Length; i++) {
            series.AddPoint(i + 1f, ys[i]);
        }
        chart.DrawOn(page);
        return TestSupport.Content(page);
    }

    [Fact]
    public void AChartWithoutPointsDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf);
        chart.AddSeries("empty");
        TestSupport.AssertXY(350f, 250f, chart.DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void AllNegativeDataGetsNegativeAxisLabels() {
        string content = Draw(-5f, -2.5f, -1f);
        Assert.Contains(TestSupport.Hex("-5.0"), content);
        Assert.Contains(TestSupport.Hex("-1.0"), content);
        Assert.DoesNotContain("NaN", content);
    }

    [Fact]
    public void FlatDataIsDrawnWithoutNaN() {
        string content = Draw(3f, 3f, 3f);
        Assert.DoesNotContain("NaN", content);
        Assert.Contains(TestSupport.Hex("3.0"), content);
        Assert.Contains(TestSupport.Hex("4.0"), content);
    }

    [Fact]
    public void WholeNumberStepsGetWholeNumberLabels() {
        string content = Draw(10f, 60f, 35f);
        Assert.Contains(TestSupport.Hex("60"), content);
        Assert.DoesNotContain(TestSupport.Hex("60.00"), content);
    }

    [Fact]
    public void APathSeriesKeepsItsStrokeWidthAndIsListedInTheLegend() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf);
        chart.AddSeries("label").SetDrawPath(true).SetShape(Shape.INVISIBLE)
                .SetStrokeWidth(20f).SetStrokeColor(Color.blue)
                .AddPoint(1f, 2f).AddPoint(3f, 2f);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("20 w", content);
        Assert.Contains(TestSupport.Hex("label"), content);

        page = new Page(pdf, Letter.PORTRAIT);
        chart.SetDrawLegend(false).DrawOn(page);
        Assert.DoesNotContain(TestSupport.Hex("label"), TestSupport.Content(page));
    }

    [Fact]
    public void APointWithoutAColorIsDrawnInTheColorOfItsSeries() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf);
        chart.AddSeries("").SetStrokeColor(Color.red)
                .AddPoint(1f, 1f)
                .AddPoint(new Point(2f, 2f).SetStrokeColor(Color.blue).SetShape(Shape.BOX));
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("1 0 0 RG", content);
        Assert.Contains("0 0 1 RG", content);
    }

    [Fact]
    public void LabelsUseAPeriodWhateverTheDefaultLocale() {
        CultureInfo saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try {
            string content = Draw(1f, 2f, 3f);
            Assert.Contains(TestSupport.Hex("1.25"), content);
            Assert.DoesNotContain(TestSupport.Hex("1,25"), content);
        } finally {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
}
