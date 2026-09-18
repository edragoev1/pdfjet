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

    [Fact]
    public void TheBordersTheAxisLinesTheGridColorAndTheSubtitleWorkAsInABarChart() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf);
        chart.AddSeries("").SetDrawPath(true).SetShape(Shape.INVISIBLE)
                .SetStrokeWidth(3f).SetStrokeColor(Color.blue)
                .AddPoint(1f, 1f).AddPoint(2f, 2f);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain("l\ns\n", content);   // a border width of 0 hides the border
        Assert.Contains("0.5 w\n", content);        // the axis lines
        Assert.DoesNotContain("1 0 0 RG", content);

        page = new Page(pdf, Letter.PORTRAIT);
        chart.SetChartBorderWidth(2f).SetInnerBorderWidth(1f).SetAxisLineWidth(0f)
                .SetGridLineColor(Color.red).SetSubtitle("Subtitle");
        chart.DrawOn(page);
        content = TestSupport.Content(page);
        Assert.Equal(2, content.Split("l\ns\n").Length - 1);
        Assert.DoesNotContain("0.5 w\n", content);
        Assert.Contains("1 0 0 RG", content);
        Assert.Contains(TestSupport.Hex("Subtitle"), content);
    }

    [Fact]
    public void TheMarkerOfASeriesIsTheOneItHasWhenTheChartIsDrawn() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf);
        chart.AddSeries("").AddPoint(1f, 1f).AddPoint(2f, 2f).SetShape(Shape.INVISIBLE);
        chart.DrawOn(page);
        Assert.DoesNotContain(" c\n", TestSupport.Content(page));  // no circles
    }

    [Fact]
    public void AChartDrawnAgainHasTheRangeOfItsDataThen() {
        PDF pdf = TestSupport.NewPDF();
        Chart chart = NewChart(pdf);
        Series series = chart.AddSeries("").AddPoint(0f, 0f).AddPoint(10f, 10f);
        chart.DrawOn(new Page(pdf, Letter.PORTRAIT));
        series.AddPoint(100f, 100f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.DrawOn(page);
        Assert.Contains("<" + TestSupport.Hex("100") + ">", TestSupport.Content(page));
    }

    [Fact]
    public void AnAxisWithoutGridLinesHasTheRangeOfItsData() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf).SetXAxisMinMax(0f, 10f, -1).SetYAxisMinMax(0f, 1000f, 0);
        chart.AddSeries("").AddPoint(1f, 1f).AddPoint(2f, 2f);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain("<" + TestSupport.Hex("1000") + ">", content);
        Assert.DoesNotContain("<" + TestSupport.Hex("10") + ">", content);
        Assert.Contains("<" + TestSupport.Hex("2.0") + ">", content);
    }

    [Fact]
    public void TheSubtitleIsGray() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = NewChart(pdf).SetTitle("Title").SetSubtitle("Subtitle");
        chart.AddSeries("").AddPoint(1f, 1f).AddPoint(2f, 2f);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal("0 0 0 rg", TestSupport.FillColorBefore(content, "Title"));
        Assert.Equal("0.41 0.41 0.41 rg", TestSupport.FillColorBefore(content, "Subtitle"));
    }

    [Fact]
    public void AChartIsAFigureDescribedByItsTitleOrItsAlternateDescription() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart titled = NewChart(pdf).SetTitle("Sales");
        titled.AddSeries("").AddPoint(1f, 1f).AddPoint(2f, 2f);
        titled.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\n", content);
        Assert.EndsWith("EMC\n", content);
        Chart described = NewChart(pdf).SetTitle("Sales").SetAltDescription("Sales rose from 1 to 2.");
        described.AddSeries("").AddPoint(1f, 1f).AddPoint(2f, 2f);
        described.DrawOn(page);
        Assert.Equal("Sales", page.structures[0].altDescription);
        Assert.Equal("Sales rose from 1 to 2.", page.structures[1].altDescription);
    }
}
}
