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
    // In a PDF/UA document a highlighted point that is a link is a figure of
    // its own, described by its name, in the Link that holds its annotation;
    // a point that is not a link stays in the chart's figure.
    [Fact]
    public void ALinkedPointIsAFigureInItsLink() {
        MemoryStream output = new MemoryStream();
        PDF pdf = new PDF(output, Compliance.PDF_UA_1).SetTitle("Test");
        Font font = new Font(pdf, new FileStream(TestSupport.RepoPath("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"),
                FileMode.Open, FileAccess.Read));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).SetSize(300f, 200f).SetLocation(50f, 50f)
                .Add(1f, 1f).Add("Andorra", 2f, 2f, "https://pdfjet.com/a")
                .Add("Belize", 3f, 1f, "https://pdfjet.com/b").Add("Chad", 4f, 3f)
                .DrawOn(page);
        pdf.Complete();
        string raw = TestSupport.Latin1(output.ToArray());
        Dictionary<string, string[]> elements = PDFTest.Elements(raw);
        int links = 0;
        foreach (KeyValuePair<string, string[]> entry in elements) {
            string[] link = entry.Value;
            if (link[0] != "Link") {
                continue;
            }
            links++;
            string figure = link[2].Substring(1).Split(' ')[0];
            Assert.Equal("Figure", elements[figure][0]);
            Assert.Equal(entry.Key, elements[figure][1]);
            Assert.Contains("/Type /OBJR", link[2]);
        }
        Assert.Equal(2, links);
        foreach (string name in new[] {"Andorra", "Belize"}) {
            Assert.Contains("/Alt " + TestSupport.TextString(name), raw);
        }
        Assert.Equal(2, raw.Split("/Subtype /Link").Length - 1);
    }

    // In a document that is not tagged, a linked point is drawn in the chart,
    // its dot and its name the link, and no elements are made.
    [Fact]
    public void ALinkedPointOfADocumentNotTaggedIsDrawnInTheChart() {
        using (TestSupport.AllowCoreFonts()) {
            MemoryStream output = new MemoryStream();
            PDF pdf = new PDF(output);
            Font font = TestSupport.Helvetica(pdf);
            Page page = new Page(pdf, Letter.PORTRAIT);
            new SimpleScatterChart(font, font).Add(1f, 1f).Add("Andorra", 2f, 2f, "https://pdfjet.com/a").DrawOn(page);
            pdf.Complete();
            string raw = TestSupport.Latin1(output.ToArray());
            Assert.DoesNotContain("/StructElem", raw);
            Assert.Equal(1, raw.Split("/Subtype /Link").Length - 1);
            Assert.Contains("/URI <" + TestSupport.Hex("https://pdfjet.com/a").ToLowerInvariant() + ">", raw);
        }
    }
}
}   // End of namespace PDFjet.NET
