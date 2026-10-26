/*
 * SimpleLineChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
public sealed class SimpleLineChartTest {
    private static SimpleLineChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new SimpleLineChart(font, font).SetSize(400f, 300f).SetLocation(50f, 50f)
                .SetTitle("Vehicles").SetSubtitle("by model year")
                .SetCategories("2021", "2022", "2023")
                .AddLine("Battery", new float[] {14755f, 23507f, 11853f})
                .AddLine("Hybrid", new float[] {3541f, 4015f, 1500f});
    }

    // The number of paths the page strokes.
    private static int Strokes(string content) {
        int count = 0;
        for (int i = content.IndexOf("\nS\n", StringComparison.Ordinal); i >= 0;
                i = content.IndexOf("\nS\n", i + 1, StringComparison.Ordinal)) {
            count++;
        }
        return count;
    }

    [Fact]
    public void DescribesItselfFromItsData() {
        SimpleLineChart chart = NewChart(TestSupport.NewPDF());
        Assert.Equal("Vehicles, by model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;"
                + " Hybrid from 3,541 to 1,500, highest 4,015 in 2022.", chart.AltDescription());
        chart.SetAltDescription("Two lines");
        Assert.Equal("Two lines", chart.AltDescription());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheHighlightedLineHasItsOwnColor(bool highlighted) {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = NewChart(pdf).SetHighlightColor(Color.red);
        if (highlighted) {
            chart.SetHighlight(0);
        }
        chart.DrawOn(page);
        Assert.Equal(highlighted, TestSupport.Content(page).Contains("1 0 0 RG"));
    }

    [Fact]
    public void LabelsTheEndsOfItsLines() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).DrawOn(page);
        string content = TestSupport.Content(page);
        foreach (string label in new[] {"Battery 11,853", "Hybrid 1,500"}) {
            Assert.Contains(TestSupport.Hex(label), content);
        }
    }

    [Fact]
    public void AMissingValueLeavesAGap() {
        int[] counts = new int[2];
        float[][] lines = {new float[] {1f, 2f, 3f, 4f, 5f}, new float[] {1f, 2f, float.NaN, 4f, 5f}};
        for (int k = 0; k < 2; k++) {
            PDF pdf = TestSupport.NewPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.Helvetica(pdf);
            new SimpleLineChart(font, font).SetCategories("a", "b", "c", "d", "e")
                    .AddLine("line", lines[k]).DrawOn(page);
            counts[k] = Strokes(TestSupport.Content(page));
        }
        Assert.Equal(counts[0] + 1, counts[1]);     // A gap strokes one path more
    }

    [Fact]
    public void EndLabelsDoNotMeet() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        SimpleLineChart chart = new SimpleLineChart(font, font).SetCategories("a", "b")
                .AddLine("one", new float[] {1f, 5f}).AddLine("two", new float[] {2f, 5f});  // Both end at 5
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.DrawOn(page);
        // The two labels are drawn at two baselines a line apart
        string content = TestSupport.Content(page);
        float[] one = TestSupport.PositionOf(content, "one 5");
        float[] two = TestSupport.PositionOf(content, "two 5");
        Assert.True(Math.Abs(one[1] - two[1]) >= font.GetBodyHeight() - TestSupport.DELTA,
                "the labels are less than a line apart");
    }

    [Fact]
    public void WithoutValuesDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        float[] xy = new SimpleLineChart(font, font).SetTitle("Nothing")
                .AddLine("line", new float[] {float.NaN}).DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
        TestSupport.AssertNear(400f, xy[0], TestSupport.DELTA);
        TestSupport.AssertNear(300f, xy[1], TestSupport.DELTA);
    }
}
}   // End of namespace PDFjet.NET
