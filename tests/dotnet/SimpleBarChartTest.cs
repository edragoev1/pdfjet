/*
 * SimpleBarChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
public class SimpleBarChartTest {
    private static SimpleBarChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new SimpleBarChart(font, font).SetWidth(330f).SetLocation(50f, 50f)
                .SetTitle("Statement, 12 pages").SetSubtitle("warm ms")
                .Add("PDFjet", 7.3f).Add("iText", 72.6f).Add("OpenPDF", 18.4f).Add("PDFBox", 44.1f);
    }

    // The widths of the rectangles filled on the page, in the order they were drawn.
    private static List<float> RectWidths(string content) {
        List<float> widths = new List<float>();
        foreach (Match m in Regex.Matches(content, @"([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+) re")) {
            widths.Add(float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture));
        }
        return widths;
    }

    [Fact]
    public void EachBarIsItsShareOfTheLargestValue() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).DrawOn(page);
        List<float> widths = RectWidths(TestSupport.Content(page));
        Assert.Equal(5, widths.Count);      // the panel and 4 bars
        TestSupport.AssertNear(330f, widths[0], TestSupport.DELTA);
        float longest = widths[2];          // iText's 72.6
        TestSupport.AssertNear(longest * 7.3f / 72.6f, widths[1], 0.05f);
        TestSupport.AssertNear(longest * 18.4f / 72.6f, widths[3], 0.05f);
        TestSupport.AssertNear(longest * 44.1f / 72.6f, widths[4], 0.05f);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheHighlightedBarHasItsOwnColor(bool highlighted) {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart chart = NewChart(pdf).SetHighlightColor(Color.red);
        if (highlighted) {
            chart.SetHighlight(0);
        }
        chart.DrawOn(page);
        Assert.Equal(highlighted, TestSupport.Content(page).Contains("1 0 0 rg"));
    }

    [Fact]
    public void DescribesItselfFromItsValues() {
        SimpleBarChart chart = NewChart(TestSupport.NewPDF()).SetFractionDigits(1);
        Assert.Equal("Statement, 12 pages, warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1.",
                chart.AltDescription());
        chart.SetAltDescription("Four libraries");
        Assert.Equal("Four libraries", chart.AltDescription());
    }

    [Fact]
    public void ValuesHaveTheDigitsTheyNeedUnlessSet() {
        SimpleBarChart chart = NewChart(TestSupport.NewPDF());
        Assert.Equal("7.25", chart.Label(7.25f));
        Assert.Equal("72", chart.Label(72f));
        chart.SetFractionDigits(1);
        Assert.Equal("72.0", chart.Label(72f));
    }

    [Fact]
    public void IsAsTallAsItsRows() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart chart = NewChart(pdf);
        float h = chart.GetHeight();
        float[] xy = chart.DrawOn(page);
        TestSupport.AssertNear(380f, xy[0], TestSupport.DELTA);
        TestSupport.AssertNear(50f + h, xy[1], TestSupport.DELTA);
        SimpleBarChart taller = NewChart(pdf).Add("Another", 10f);
        TestSupport.AssertNear(h + chart.RowHeight(), taller.GetHeight(), TestSupport.DELTA);
    }

    [Fact]
    public void ANegativeValueHasNoBarButItsValue() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        new SimpleBarChart(font, font).Add("up", 5f).Add("down", -2f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(2, RectWidths(content).Count);     // the panel and one bar
        Assert.Contains(TestSupport.Hex("-2"), content);
    }

    [Fact]
    public void WithoutBarsDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        new SimpleBarChart(font, font).SetTitle("Nothing").DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
    }

    // A null title or name is empty: it threw when the chart was drawn.
    [Fact]
    public void NullIsEmpty() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        new SimpleBarChart(font, font).SetTitle(null).SetSubtitle(null).SetAltDescription(null)
                .Add(null, 1f).Add("b", 2f).DrawOn(new Page(pdf, Letter.PORTRAIT));
    }
}
}   // End of namespace PDFjet.NET
