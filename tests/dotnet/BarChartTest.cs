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

    [Fact]
    public void StackedBarsUseTheSumsOfTheCategoriesForTheValueAxis() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a", "b");
        chart.AddSeries("", new float[] {20f, 75f}).AddSeries("", new float[] {30f, 10f});
        string grouped = Draw(chart, page);
        Assert.DoesNotContain(TestSupport.Hex("90"), grouped);

        page = new Page(pdf, Letter.PORTRAIT);
        chart.SetStacked(true).SetDrawValueLabels(true);
        string stacked = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("90"), stacked);
        Assert.Contains(TestSupport.Hex("75"), stacked);
    }

    [Fact]
    public void BarsHaveTheirOwnColorsAndLabelsInsideWithGroupedDigits() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a", "b").SetHorizontal(true).SetSubtitle("sub");
        chart.AddSeries("rivers", new float[] {6650f, 12f}, new int[] {Color.red, Color.blue});
        chart.SetValueAxisMinMax(0f, 8000f, 4).SetDrawValueLabels(true).SetValueLabelsInside(true);
        chart.SetGroupingUsed(true).SetAxisLineWidth(0f).SetGridLineColor(Color.lightgray);
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("6,650"), content);
        Assert.Contains(TestSupport.Hex("8,000"), content);
        Assert.Contains(TestSupport.Hex("sub"), content);
        Assert.Contains("1 0 0 rg", content);   // the first bar
        Assert.Contains("0 0 1 rg", content);   // the second bar
        Assert.Contains("1 1 1 rg", content);   // the label inside the first bar
        Assert.Contains(TestSupport.Hex("12"), content);    // too short: next to the bar
    }
}
}
