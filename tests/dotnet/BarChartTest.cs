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
    public void AxisLabelsThatWouldMeetAreDrawnAtEverySecondLine() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a").SetHorizontal(true);
        // 16 steps of 5 across about 240 points: 15 points a step, where a
        // label of one decimal, 70.0, needs more
        chart.AddSeries("", new float[] {72.6f}).SetValueAxisMinMax(0f, 80f, 16).SetMinimumFractionDigits(1);
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("70.0"), content);
        Assert.Contains(TestSupport.Hex("80.0"), content);
        Assert.DoesNotContain(TestSupport.Hex("75.0"), content);     // would meet 70.0 and 80.0
    }

    [Fact]
    public void AxisLabelStepIsTheFewestLinesALabelFits() {
        Assert.Equal(1, ChartScale.AxisLabelStep(30f, 20f, 8));     // fits
        Assert.Equal(2, ChartScale.AxisLabelStep(15f, 20f, 16));
        Assert.Equal(3, ChartScale.AxisLabelStep(10f, 25f, 16));
        Assert.Equal(4, ChartScale.AxisLabelStep(5f, 100f, 4));     // never more than the lines
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
        Assert.Contains(TestSupport.Hex("75"), stacked);
        // The total of each stack above it
        Assert.Contains(TestSupport.Hex("50"), stacked);
        Assert.Contains(TestSupport.Hex("85"), stacked);
    }

    [Fact]
    public void TheDefaultLookIsQuiet() {
        // The first series in navy, the grid lines light gray and solid, the
        // text gray or ink, and no black line or text.
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a", "b").SetTitle("t").SetSubtitle("s");
        chart.AddSeries("one", new float[] {20f, 75f}).AddSeries("two", new float[] {30f, 10f});
        string content = Draw(chart, page);
        Assert.Contains("0.12 0.23 0.37 rg", content);
        Assert.Contains("0.44 0.58 0.76 rg", content);
        Assert.Contains("0.88 0.89 0.91 RG\n0.5 w\n[] 0 d", content);
        Assert.DoesNotContain("0 0 0 RG", content);
        Assert.DoesNotContain("0 0 0 rg", content);
        Assert.Equal("0.13 0.15 0.17 rg", TestSupport.FillColorBefore(content, "t"));
        Assert.Equal("0.37 0.41 0.45 rg", TestSupport.FillColorBefore(content, "s"));
        // A label on a dark bar is white, on a light one ink: whichever
        // contrasts more. On sage and on the old sage, which a rule by
        // brightness gave white, ink contrasts more; on slate white.
        Assert.Equal(Color.white, BarChart.LabelColorOn(0x1F3A5F));
        Assert.Equal(0x22262B, BarChart.LabelColorOn(0x6F95C2));
        Assert.Equal(0x22262B, BarChart.LabelColorOn(0x76987C));
        Assert.Equal(0x22262B, BarChart.LabelColorOn(0x6B8F71));
        Assert.Equal(Color.white, BarChart.LabelColorOn(0x4F7A8C));
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
        Assert.Equal("1 1 1 rg", TestSupport.FillColorBefore(content, "6,650"));    // inside the first bar
        Assert.Equal("0.37 0.41 0.45 rg", TestSupport.FillColorBefore(content, "sub"));
        Assert.Contains(TestSupport.Hex("12"), content);    // too short: next to the bar
    }

    [Fact]
    public void AChartIsAFigureDescribedByItsTitleOrItsAlternateDescription() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart titled = NewChart(pdf).SetTitle("Sales").SetCategories("a", "b");
        titled.AddSeries("", new float[] {1f, 2f});
        string content = Draw(titled, page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\n", content);
        Assert.EndsWith("EMC\n", content);
        BarChart described = NewChart(pdf).SetTitle("Sales").SetAltDescription("Sales rose from 1 to 2.")
                .SetCategories("a", "b");
        described.AddSeries("", new float[] {1f, 2f});
        Draw(described, page);
        Assert.Equal("Sales", page.structures[0].altDescription);
        Assert.Equal("Sales rose from 1 to 2.", page.structures[1].altDescription);
    }
}
}
