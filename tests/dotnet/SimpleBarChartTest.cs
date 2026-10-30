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
        Assert.Equal("Statement, 12 pages. Warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1.",
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

    // A name wider than two fifths of the chart is shortened with an
    // ellipsis, and the bars keep their room: in a chart narrower than its
    // names they went to nothing, the values past the panel (review of 26
    // October 2026). The description keeps the name whole.
    [Fact]
    public void ALongNameIsShortened() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        string name = "A name far too long for the column of names of this chart";
        SimpleBarChart chart = new SimpleBarChart(font, font).SetWidth(200f).Add(name, 5f).Add("b", 3f);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain(TestSupport.Hex(name), content);
        string shortened = ChartScale.Shorten(font, font.GetSize(), name, 0.4f * (200f - 2f * font.GetSize()));
        // The ellipsis is 85 in WinAnsi, the encoding of a core font
        string start = shortened.Substring(0, shortened.Length - 1);
        Assert.EndsWith("\u2026", shortened);
        Assert.Contains(TestSupport.Hex(start) + "85>", content);
        Assert.Contains(name, chart.AltDescription());
        Assert.Equal("A n\u2026", ChartScale.Shorten(font, 10f, "A name", font.StringWidth(10f, "A n\u2026")));
        Assert.Equal("A name", ChartScale.Shorten(font, 10f, "A name", 100f));
        Assert.Equal("A\u2026", ChartScale.Shorten(font, 10f, "A  name", font.StringWidth(10f, "A \u2026") - 0.1f));
    }

    // A chart without its fonts draws nothing and has no height; a title too
    // long for the chart is shortened; a value of 3e38 is written short (the
    // review of 26 October 2026, the second).
    [Fact]
    public void TheSecondReviewOfTheCharts() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart none = new SimpleBarChart(null, null).Add("a", 1f);
        none.DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
        Assert.Equal(0f, none.GetHeight());
        string title = "A title far too long for a chart two hundred points wide";
        new SimpleBarChart(font, font).SetWidth(200f).SetTitle(title).Add("a", 3e38f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain(TestSupport.Hex(title), content);
        Assert.Contains(TestSupport.Hex("3e38"), content);
        Assert.Equal("1.23e20", ChartScale.Format(1.2345e20f, 0, 2));
        Assert.Equal("-4.5e16", ChartScale.GroupDigits(ChartScale.Format(-4.5e16f, 0, 2)));
    }

    // Values are grouped in thousands, as in the other charts; and in a chart
    // narrower than its names and values the names give the values room,
    // which were written past the chart (the third review of 26 October 2026).
    [Fact]
    public void TheThirdReviewOfTheCharts() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleBarChart(font, font).SetWidth(300f).Add("a", 23507f).DrawOn(page);
        Assert.Contains(TestSupport.Hex("23,507"), TestSupport.Content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart narrow = new SimpleBarChart(font, font).SetWidth(90f).Add("A long name", 1234567f);
        narrow.SetLocation(0f, 0f);
        narrow.DrawOn(page);
        float[] at = TestSupport.PositionOf(TestSupport.Content(page), "1,234,567");
        Assert.True(at[0] + font.StringWidth("1,234,567") <= 90f + 0.01f, at[0] + " in 90");
    }

    // A title that ends a sentence is not followed by a colon, where it was
    // "Sales.: a 5" (the review of 28 October 2026).
    [Fact]
    public void TheDescriptionAfterATitleThatEndsASentence() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        SimpleBarChart chart = new SimpleBarChart(font, font).SetTitle("Sales.").Add("a", 5f).Add("b", 6f);
        Assert.Equal("Sales. a 5, b 6.", chart.AltDescription());
    }

    // The polish of 29 October 2026: the bar the chart highlights itself, a
    // reference line and rounded bars, each off unless set.

    [Fact]
    public void TheChartHighlightsTheLowestOrTheHighestValue() {
        SimpleBarChart chart = NewChart(TestSupport.NewPDF());     // 7.3, 72.6, 18.4, 44.1
        Assert.Equal(-1, chart.Highlighted());
        Assert.Equal(0, chart.SetHighlightLowest().Highlighted());
        Assert.Equal(1, chart.SetHighlightHighest().Highlighted());
        // A bar added later counts, as the chart finds the bar when it is drawn
        chart.Add("Faster", 100f);
        Assert.Equal(4, chart.Highlighted());
        // The index set takes the place of the value found, and the other way
        Assert.Equal(2, chart.SetHighlight(2).Highlighted());
        Assert.Equal(0, chart.SetHighlightLowest().Highlighted());
    }

    [Fact]
    public void TheFirstOfEqualValuesIsHighlightedAndNoValueIsSkipped() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        SimpleBarChart chart = new SimpleBarChart(font, font)
                .Add("none", float.NaN).Add("a", 3f).Add("b", 1f).Add("c", 1f).Add("d", 3f);
        Assert.Equal(2, chart.SetHighlightLowest().Highlighted());
        Assert.Equal(1, chart.SetHighlightHighest().Highlighted());
        SimpleBarChart empty = new SimpleBarChart(font, font).Add("none", float.NaN).SetHighlightLowest();
        Assert.Equal(-1, empty.Highlighted());
    }

    [Fact]
    public void AReferenceLineIsDashedAndLabeledUnderTheBars() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart plain = NewChart(pdf).SetFractionDigits(1);
        SimpleBarChart chart = NewChart(pdf).SetFractionDigits(1).SetReferenceLine(50f, "Target");
        float size = TestSupport.Helvetica(pdf).GetSize();
        TestSupport.AssertNear(plain.GetHeight() + 1.4f * 0.8f * size, chart.GetHeight(), TestSupport.DELTA);
        Assert.Equal("Statement, 12 pages. Warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1; "
                + "Target 50.0.", chart.AltDescription());
        Assert.Equal("Statement, 12 pages. Warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1; "
                + "reference line at 50.0.", NewChart(pdf).SetFractionDigits(1).SetReferenceLine(50f, "").AltDescription());
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        int dashed = content.IndexOf("[2 2] 0 d", StringComparison.Ordinal);
        Assert.True(dashed > 0, content);
        Assert.True(content.IndexOf("[] 0 d", dashed, StringComparison.Ordinal) > dashed, "the dash pattern set back");
    }

    [Fact]
    public void AReferenceLinePastTheLargestValueShortensTheBars() {
        PDF pdf = TestSupport.NewPDF();
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).DrawOn(page1);
        List<float> plain = RectWidths(TestSupport.Content(page1));
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetReferenceLine(2f * 72.6f, "").DrawOn(page2);
        List<float> shorter = RectWidths(TestSupport.Content(page2));
        TestSupport.AssertNear(plain[2] / 2f, shorter[2], 0.05f);
    }

    [Fact]
    public void AReferenceLineThatIsNoValueDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart chart = NewChart(pdf).SetReferenceLine(float.NaN, "Target");
        TestSupport.AssertNear(NewChart(pdf).GetHeight(), chart.GetHeight(), TestSupport.DELTA);
        chart.DrawOn(page);
        Assert.DoesNotContain("[2 2] 0 d", TestSupport.Content(page));
    }

    [Fact]
    public void RoundedBarsAreCurvesNotRectangles() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetRoundedBars(true).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Single(RectWidths(content));     // the panel alone a rectangle
        Assert.Contains(" c\n", content);       // the bars' corners are curves
    }
}
}   // End of namespace PDFjet.NET
