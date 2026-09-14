/*
 * BarChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
public class BarChartTest {
    private static BarChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f);
    }

    private static string Draw(BarChart chart, Page page) {
        TestSupport.AssertXY(350f, 250f, chart.DrawOn(page));
        return TestSupport.Content(page);
    }

    [Fact]
    public void AChartWithoutCategoriesDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Draw(NewChart(pdf), page);
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void TheValueAxisStartsAtZeroWithWholeNumberLabels() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("abc", "def", "ghi");
        chart.AddSeries("", new float[] {20f, 75f, 31f});
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("80"), content);
        Assert.DoesNotContain(TestSupport.Hex("0.00"), content);
        Assert.DoesNotContain(TestSupport.Hex("80.00"), content);
        Assert.Contains(TestSupport.Hex("ghi"), content);
    }

    [Fact]
    public void TheLegendListsTheNamedSeriesInTheirColors() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a");
        chart.AddSeries("first", new float[] {1f}, Color.red);
        chart.AddSeries("", new float[] {2f}, Color.blue);
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("first"), content);
        Assert.Contains("1 0 0 rg", content);
        Assert.Contains("0 0 1 rg", content);
    }

    [Fact]
    public void ValueLabelsAreWrittenWithTheFractionDigitsSet() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a", "b").SetHorizontal(true);
        chart.AddSeries("", new float[] {2.5f, -1f}).SetDrawValueLabels(true);
        chart.SetMinimumFractionDigits(1);
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("2.5"), content);
        Assert.Contains(TestSupport.Hex("-1.0"), content);
    }

    [Fact]
    public void AManualAxisRangeIsUsedAsGiven() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a");
        chart.AddSeries("", new float[] {5f}).SetValueAxisMinMax(0f, 12f, 4);
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("12"), content);
        Assert.Contains(TestSupport.Hex("3"), content);
        Assert.DoesNotContain("NaN", content);
    }
}
}
