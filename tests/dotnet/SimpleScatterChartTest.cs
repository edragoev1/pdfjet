/*
 * SimpleScatterChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PDFjet.NET {
public class SimpleScatterChartTest {
    private static SimpleScatterChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new SimpleScatterChart(font, font).SetSize(400f, 300f).SetLocation(50f, 50f)
                .SetTitle("Phones and internet").SetSubtitle("by country")
                .SetXAxisTitle("phones").SetYAxisTitle("internet")
                .Add(0.1f, 5f).Add(0.5f, 30f).Add("Canada", 0.5f, 65f).Add(1.2f, 70f);
    }

    [Fact]
    public void TheTrendIsTheLeastSquaresLine() {
        double[] line = SimpleScatterChart.Trend(new List<SimpleScatterChart.Point> {
                new SimpleScatterChart.Point(0f, 1f, ""), new SimpleScatterChart.Point(1f, 3f, ""),
                new SimpleScatterChart.Point(2f, 5f, ""), new SimpleScatterChart.Point(3f, 7f, "")});
        Assert.NotNull(line);
        Assert.Equal(2.0, line[0], 9);
        Assert.Equal(1.0, line[1], 9);
        Assert.Null(SimpleScatterChart.Trend(new List<SimpleScatterChart.Point> {
                new SimpleScatterChart.Point(1f, 1f, ""), new SimpleScatterChart.Point(1f, 2f, "")}));
        Assert.Null(SimpleScatterChart.Trend(new List<SimpleScatterChart.Point> {
                new SimpleScatterChart.Point(1f, 1f, "")}));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANamedPointIsHighlighted(bool named) {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        SimpleScatterChart chart = new SimpleScatterChart(font, font).SetHighlightColor(Color.red)
                .Add(1f, 1f).Add(2f, 3f);
        if (named) {
            chart.Add("Canada", 1.5f, 2f);
        }
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(named, content.Contains("1 0 0 rg"));
        Assert.Equal(named, content.Contains(TestSupport.Hex("Canada")));
    }

    [Fact]
    public void DescribesItselfFromItsData() {
        SimpleScatterChart chart = NewChart(TestSupport.NewPDF()).Add(float.NaN, 1f).SetTrendLine(true);
        Assert.Equal("Phones and internet, by country: 4 points, internet against phones; "
                + "Canada 0.5 and 65; the trend rises.", chart.AltDescription());
        chart.SetAltDescription("Four countries");
        Assert.Equal("Four countries", chart.AltDescription());
    }

    [Fact]
    public void AnAxisRangeSetIsUsed() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetYAxisRange(0f, 200f).DrawOn(page);
        Assert.Contains(TestSupport.Hex("200"), TestSupport.Content(page));
    }

    [Theory]
    [InlineData(Compliance.PDF_UA_1)]
    [InlineData(Compliance.PDF_A_1B)]
    public void TheDotsAreTransparentButInPDFA1(Compliance level) {
        using (TestSupport.AllowCoreFonts()) {
            PDF pdf = new PDF(new MemoryStream(), level);
            Page page = new Page(pdf, Letter.PORTRAIT);
            NewChart(pdf).DrawOn(page);
            Assert.Equal(level != Compliance.PDF_A_1B, TestSupport.Content(page).Contains(" gs"));
        }
    }

    [Fact]
    public void WithoutPointsDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        float[] xy = new SimpleScatterChart(font, font).SetTitle("Nothing").Add(float.NaN, 1f).DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
        TestSupport.AssertNear(400f, xy[0], TestSupport.DELTA);
        TestSupport.AssertNear(300f, xy[1], TestSupport.DELTA);
    }
}
}   // End of namespace PDFjet.NET
