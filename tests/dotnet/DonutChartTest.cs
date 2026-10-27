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
        DonutChart chart = NewChart(pdf).Add("none", 0f, Color.red);
        TestSupport.AssertXY(300f, 300f, chart.DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void SlicesArePercentagesOfTheSumOfTheValues() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf);
        chart.Add("a", 1f, Color.red);
        chart.Add("b", 1f, Color.green);
        chart.Add("c", 2f, Color.blue);
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
        chart.Add("a", 3f, Color.red).Add("b", 1f, Color.blue);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("75%"), content);
        Assert.Contains("200 592 l", content);   // the center, in PDF coordinates
    }

    [Fact]
    public void AChartIsAFigureThatListsItsSlices() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart donut = NewChart(pdf).Add("Apples", 25f, Color.red)
                .Add("Oranges", 75f, Color.blue);
        donut.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.StartsWith("/Figure <</MCID 0>>\nBDC\n", content);
        Assert.EndsWith("EMC\n", content);
        Assert.Equal("0.13 0.15 0.17 rg", TestSupport.FillColorBefore(content, "Apples"));
        Assert.Equal("1 1 1 rg", TestSupport.FillColorBefore(content, "25%"));
        DonutChart pie = NewChart(pdf).SetRadii(100f, 0f).Add("", 1f, Color.red);
        pie.DrawOn(page);
        DonutChart described = NewChart(pdf).SetAltDescription("Most are oranges.")
                .Add("Apples", 1f, Color.red);
        described.DrawOn(page);
        Assert.Equal("Donut chart: Apples 25%, Oranges 75%", page.structures[0].altDescription);
        Assert.Equal("Pie chart: 100%", page.structures[1].altDescription);
        Assert.Equal("Most are oranges.", page.structures[2].altDescription);
    }

    // Slices in the palette, navy first; thin white lines between them; thin
    // gray leader lines; the percentage white on navy, ink on the light steel
    // blue; and no black line or text.
    [Fact]
    public void TheDefaultLookIsQuiet() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).Add("a", 60f).Add("b", 40f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("0.12 0.23 0.37 rg", content);       // navy
        Assert.Contains("0.44 0.58 0.76 rg", content);       // steel blue
        Assert.Contains("1 1 1 RG\n1.5 w\n", content);       // the white lines between
        Assert.Contains("0.55 0.59 0.64 RG\n0.75 w\n", content);   // the leader lines
        Assert.DoesNotContain("0 0 0 RG", content);
        Assert.DoesNotContain("0 0 0 rg", content);
        Assert.Equal("1 1 1 rg", TestSupport.FillColorBefore(content, "60%"));
        Assert.Equal("0.13 0.15 0.17 rg", TestSupport.FillColorBefore(content, "40%"));
    }

    // The title at the location, the subtitle under it, the circle under
    // both; a chart without them has the circle at its location, as before.
    [Fact]
    public void ATitleIsDrawnAboveTheCircle() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        // The circle, and its label 15 under it
        TestSupport.AssertXY(300f, 315f, NewChart(pdf).Add("a", 1f).DrawOn(page));
        float[] xy = NewChart(pdf).SetTitle("Fruit").SetSubtitle("This week")
                .Add("a", 1f).DrawOn(page);
        Assert.Equal(300f, xy[0], 0.01f);
        Assert.True(xy[1] > 315f, "the circle is under the title: " + xy[1]);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("Fruit"), content);
        Assert.Contains(TestSupport.Hex("This week"), content);
        Assert.Equal("0.37 0.41 0.45 rg", TestSupport.FillColorBefore(content, "This week"));
        Assert.Equal("Fruit. This week: a 100%", page.structures[1].altDescription);
    }

    // A slice that is not a number or an infinity is not drawn, and the
    // others are: one made the arc's loop endless (review of 11 October 2026,
    // #2). A value whose 360ths are past the largest float is drawn, as it
    // compares with the others (review of 26 October 2026).
    [Fact]
    public void ASliceThatIsNotANumberIsNotDrawn() {
        foreach (float bad in new float[] {float.NaN, float.PositiveInfinity}) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            DonutChart chart = NewChart(pdf);
            chart.Add("bad", bad, Color.red);
            chart.Add("a", 1f, Color.green);
            chart.Add("b", 3f, Color.blue);
            chart.DrawOn(page);
            string content = TestSupport.Content(page);
            Assert.Contains(TestSupport.Hex("25%"), content);
            Assert.Contains(TestSupport.Hex("75%"), content);
        }
        PDF pdf2 = TestSupport.NewPDF();
        Page page2 = new Page(pdf2, Letter.PORTRAIT);
        NewChart(pdf2).Add("a", 3e38f).Add("b", 3e38f).DrawOn(page2);
        Assert.Contains(TestSupport.Hex("50%"), TestSupport.Content(page2));
    }

    // A slice is computed with the precision of Go's port, float or double
    // step by step, so the ports draw the same curves: the square root in
    // double moved a control point by a hundredth in 133 of 100,000 segments
    // (review of 11 October 2026, #25). The expected curves are Go's.
    [Fact]
    public void ASliceIsDrawnAsTheGoPortDrawsIt() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        // The radii in either order: the larger is the outer one since the
        // review of 26 October 2026, the second, and the curve is the one Go
        // and Java draw of the ring the right way out
        DonutChart chart = new DonutChart(null, null).SetRadii(46.94574f, 92.64224f);
        foreach (float value in new float[] {524.55023f, 80.410065f, 7.852018f, 515.9275f, 931.2741f}) {
            chart.Add("", value, 0x123456);
        }
        chart.SetLocation(247.509f, 97.883514f).DrawOn(page);
        Assert.Contains("432.79 600.57 432.78 599.68 432.75 598.78 c\n", TestSupport.Content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        chart = new DonutChart(null, null).SetRadii(179.36f, 65.845535f);
        foreach (float value in new float[] {472.8191f, 308.4593f, 142.94768f, 299.98398f}) {
            chart.Add("", value, 0x123456);
        }
        chart.SetLocation(88.44436f, 261.47784f).DrawOn(page);
        Assert.Contains("131.87 234.15 131.39 234.7 130.91 235.27 c\n", TestSupport.Content(page));
    }

    // A slice of a null name has no label, as one of an empty name.
    [Fact]
    public void ANullNameIsNoLabel() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).Add(null, 1f).Add("a", 3f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains(TestSupport.Hex("a")) && content.Contains(TestSupport.Hex("25%")), content);
    }

    // The labels of thin slices side by side are moved apart, a line apart at
    // least; the percentages add up to 100; the white line by a thin slice is
    // narrower than it; and DrawOn returns the bottom right corner of the
    // labels too, measured as drawn (review of 26 October 2026).
    [Fact]
    public void ThinSlicesKeepTheirLabelsApart() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).Add("Big", 90f);
        for (int i = 0; i < 6; i++) {
            chart.Add("Small " + i, 1.5f);
        }
        float[] measured = chart.DrawOn(null);
        float[] xy = chart.DrawOn(page);
        TestSupport.AssertXY(measured[0], measured[1], xy);
        string content = TestSupport.Content(page);
        Font font = TestSupport.Helvetica(pdf);
        for (int i = 1; i < 6; i++) {
            float above = TestSupport.PositionOf(content, "Small " + (i - 1))[1];
            float below = TestSupport.PositionOf(content, "Small " + i)[1];
            Assert.True(System.Math.Abs(above - below) >= font.GetBodyHeight() - 0.01f, above + " " + below);
        }
        Assert.Equal("Donut chart: Big 91%, Small 0 2%, Small 1 2%, Small 2 2%, Small 3 1%, Small 4 1%, Small 5 1%",
                page.structures[0].altDescription);
        Assert.Contains("1.5 w\n", content);                                                      // By the big slice
        Assert.True(System.Text.RegularExpressions.Regex.IsMatch(content, "\n0\\.\\d+ w\n"), content);    // Between two thin ones
    }

    // With a legend the chart has no leader lines: each slice's label and
    // percentage is in rows under the circle, which the returned corner
    // holds. The text in the hole is drawn and described; a pie chart has no
    // hole for it.
    [Fact]
    public void ALegendAndATextInTheHole() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = NewChart(pdf).SetLegend(true).SetCenterText("100 crates")
                .Add("a", 60f).Add("b", 40f);
        float[] xy = chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain("0.55 0.59 0.64 RG", content);      // No leader lines
        Assert.True(content.Contains(TestSupport.Hex("a 60%")) && content.Contains(TestSupport.Hex("b 40%")), content);
        Assert.Contains(TestSupport.Hex("100 crates"), content);
        Assert.True(xy[1] > 300f + 15f, "the legend is under the circle: " + xy[1]);
        TestSupport.AssertXY(xy[0], xy[1], chart.DrawOn(null));
        Assert.Equal("100 crates: a 60%, b 40%", page.structures[0].altDescription);
        page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetRadii(100f, 0f).SetCenterText("none").Add("a", 1f).DrawOn(page);
        Assert.DoesNotContain(TestSupport.Hex("none"), TestSupport.Content(page));
    }

    // The review of 26 October 2026, the second: labels moved apart stay
    // outside the ring; long labels, legend items and texts in the hole are
    // shortened; the caller's dash pattern does not dash the lines; an inner
    // radius past the outer one is the outer one; and the last slice, by the
    // first around the circle, is not in the first's color.
    [Fact]
    public void TheSecondReviewOfTheCharts() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart thin = NewChart(pdf).Add("Big", 90f);    // At (100, 100), radii 100 and 50
        for (int i = 0; i < 8; i++) {
            thin.Add("Small " + i, 1.2f);
        }
        thin.DrawOn(page);
        string content = TestSupport.Content(page);
        for (int i = 0; i < 8; i++) {
            float[] at = TestSupport.PositionOf(content, "Small " + i);   // In PDF space, y up
            float dy = (792f - at[1]) - 200f;
            float ring = System.Math.Abs(dy) < 100f ? (float) System.Math.Sqrt(100f * 100f - dy * dy) : 0f;
            Assert.True(at[0] + font.StringWidth("Small " + i) <= 200f - ring + 0.01f,
                    "Small " + i + " is on the ring: " + at[0] + ", " + at[1]);
        }

        page = new Page(pdf, Letter.PORTRAIT);
        page.SetStrokeDashPattern("[3 3] 0");
        string name = "A label much too long to be drawn whole beside the circle";
        NewChart(pdf).SetCenterText("A text in the hole far too long to be written in it at any size")
                .Add(name, 60f).Add("b", 40f).DrawOn(page);
        content = TestSupport.Content(page);
        // The chart's own solid lines after the caller's dashes
        int dashed = content.IndexOf("[3 3] 0 d\n", System.StringComparison.Ordinal);
        Assert.True(dashed >= 0 && content.IndexOf("[] 0 d\n", System.StringComparison.Ordinal) > dashed, content);
        Assert.DoesNotContain(TestSupport.Hex(name), content);
        Assert.Contains(" 9 Tf\n", content);    // The text in the hole at 9, three quarters of 12
        page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetLegend(true).Add(name, 60f).Add("b", 40f).DrawOn(page);
        content = TestSupport.Content(page);
        Assert.DoesNotContain(TestSupport.Hex(name), content);
        Assert.Contains(TestSupport.Hex(" 60%"), content);   // The percentage kept

        page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).Add("a", 1f).DrawOn(page);
        string outer = TestSupport.Content(page);
        page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetRadii(50f, 100f).Add("a", 1f).DrawOn(page);
        // The same but for the names of the fonts, a new one for each chart
        Assert.Equal(System.Text.RegularExpressions.Regex.Replace(outer, "/F\\d+ ", "/F "),
                System.Text.RegularExpressions.Regex.Replace(TestSupport.Content(page), "/F\\d+ ", "/F "));

        page = new Page(pdf, Letter.PORTRAIT);
        DonutChart nine = NewChart(pdf);
        for (int i = 0; i < 9; i++) {
            nine.Add("", 1f);
        }
        nine.DrawOn(page);
        content = TestSupport.Content(page);
        Assert.True(content.IndexOf("0.12 0.23 0.37 rg", System.StringComparison.Ordinal)
                == content.LastIndexOf("0.12 0.23 0.37 rg", System.StringComparison.Ordinal), content);
    }
}
}
