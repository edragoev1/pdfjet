/*
 * ChartReviewFourTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The findings of the fourth read-only review of the charts, of 26 October
/// 2026.
/// </summary>
public class ChartReviewFourTest {
    private static int Count(string content, string text) {
        int count = 0;
        for (int i = content.IndexOf(text, StringComparison.Ordinal); i >= 0;
                i = content.IndexOf(text, i + 1, StringComparison.Ordinal)) {
            count++;
        }
        return count;
    }

    // The right end of the text, drawn in the font at its size.
    private static float EndOf(string content, Font font, string text) {
        return TestSupport.PositionOf(content, text)[0] + font.StringWidth(text);
    }

    [Fact]
    public void ABarChartAxisKeepsTheDigitsOfItsStepWithNoFractionDigits() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 300f)
                .SetCategories("a", "b").SetFractionDigits(0);
        chart.AddSeries("", new float[] {1f, 2f});
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains("<" + TestSupport.Hex("0.25") + ">", content);    // Not 0, 0, 1, 1

        page = new Page(pdf, Letter.PORTRAIT);
        chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 300f).SetCategories("a", "b");
        chart.AddSeries("", new float[] {0.002f, 0.004f});
        chart.DrawOn(page);
        content = TestSupport.Content(page);
        Assert.Contains("<" + TestSupport.Hex("0.0020") + ">", content);     // The digits of 0.0005
        Assert.DoesNotContain("<" + TestSupport.Hex("0.00") + ">", content);
    }

    [Fact]
    public void ABarChartOfASpanPastTheLargestFloatDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f).SetCategories("a", "b");
        chart.AddSeries("", new float[] {-3e38f, 3e38f});
        TestSupport.AssertXY(350f, 250f, chart.DrawOn(page));
        Assert.Empty(page.GetContent());
    }

    [Fact]
    public void DataAtTheLargestFloatHasAnAxisOfItsOwn() {
        ChartScale.Axis axis = ChartScale.AxisOf(new float[] {float.MaxValue}, 0f, 0f, false);
        Assert.True(ChartScale.Drawable(axis));
        Assert.True(axis.min > 1e38f && axis.max == float.MaxValue, axis.min + " to " + axis.max);
        float[] flat = ChartScale.FlatRange(5f);
        Assert.Equal(5f, flat[0]);
        Assert.Equal(6f, flat[1]);
    }

    [Fact]
    public void GroupDigitsLeavesWordsAsTheyAre() {
        Assert.Equal("Infinity", ChartScale.GroupDigits("Infinity"));
        Assert.Equal("-Infinity", ChartScale.GroupDigits("-Infinity"));
        Assert.Equal("NaN", ChartScale.GroupDigits("NaN"));
        Assert.Equal("3e38", ChartScale.GroupDigits("3e38"));
        Assert.Equal("-1,234,567.25", ChartScale.GroupDigits("-1234567.25"));
    }

    [Fact]
    public void ASimpleBarChartWritesNoValueThatIsNotANumber() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        SimpleBarChart chart = new SimpleBarChart(font, font).SetTitle("Sizes")
                .Add("one", 1200f).Add("two", float.PositiveInfinity).Add("three", float.NaN);
        Assert.Equal("", chart.Label(float.PositiveInfinity));
        Assert.Equal("", chart.Label(float.NaN));
        Assert.Equal("Sizes: one 1,200.", chart.AltDescription());
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain(TestSupport.Hex("fin"), content);
        Assert.DoesNotContain(TestSupport.Hex("NaN"), content);
    }

    [Fact]
    public void StepDigitsGoPastSix() {
        Assert.Equal(9, ChartScale.StepDigits(2.5e-8f));
        Assert.Equal(9, ChartScale.DigitsOf(2.5e-8f));
        Assert.Equal(2, ChartScale.DigitsOf(0.25f));
        Assert.Equal(2, ChartScale.DigitsOf(10f));
        Assert.Equal(6, ChartScale.StepDigits(1f / 3f));
        Assert.Equal(2, ChartScale.DigitsOf(float.NaN));
    }

    [Fact]
    public void LineChartCategoriesStayInsideTheChart() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                .SetCategories("January 2026", "February 2026", "March 2026 and on")
                .AddLine("a", new float[] {1f, 2f, 3f}).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(TestSupport.PositionOf(content, "January 2026")[0] >= 50f, content);
        Assert.True(EndOf(content, font, "March 2026 and on") <= 350f, content);
    }

    [Fact]
    public void ScatterChartNumbersStayInsideTheChart() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).SetLocation(50f, 50f).SetSize(400f, 200f)
                .Add(0f, 1f).Add(1000000f, 5f).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(EndOf(content, font, "1,000,000") <= 450f, content);
    }

    [Fact]
    public void BarChartCategoriesStayInsideTheChart() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                .SetCategories("a", "b", "c", "d", "e", "f", "the last one");
        chart.AddSeries("", new float[] {1f, 2f, 3f, 4f, 5f, 6f, 7f});
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(EndOf(content, font, "the last one") <= 350f, content);
    }

    [Fact]
    public void ScatterDescriptionTellsWhatIsDrawn() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        // A trend line that misses the plot is not drawn, nor described
        SimpleScatterChart chart = new SimpleScatterChart(font, font).SetTitle("Flat")
                .Add(1f, 1f).Add(2f, 1f).Add(3f, 1f).SetTrendLine(true);
        Assert.Equal("Flat: 3 points; the trend is flat.", chart.AltDescription());
        chart.SetYAxisRange(100f, 200f);
        Assert.Equal("Flat: 0 points; 3 points outside the range.", chart.AltDescription());
        // The points outside a range that is set are counted apart
        chart = new SimpleScatterChart(font, font).SetTitle("Some")
                .Add(1f, 5f).Add(2f, 6f).Add("far", 3f, 50f).SetYAxisRange(0f, 10f);
        Assert.Equal("Some: 2 points; 1 point outside the range.", chart.AltDescription());
    }

    [Fact]
    public void LineChartDescriptionOfOneCategory() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        SimpleLineChart chart = new SimpleLineChart(font, font).SetTitle("Sales")
                .SetCategories("2019").AddLine("a", new float[] {4f});
        Assert.True(chart.AltDescription().StartsWith("Sales, 2019: a 4.", StringComparison.Ordinal),
                chart.AltDescription());
        chart.SetCategories("2019", "2020", "");
        Assert.True(chart.AltDescription().StartsWith("Sales, 2019 to 2020: a 4.", StringComparison.Ordinal),
                chart.AltDescription());
    }

    [Fact]
    public void LineChartDrawsAValueAloneBetweenGaps() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page joined = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                .AddLine("a", new float[] {1f, 2f, 3f}).DrawOn(joined);
        Page alone = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                .AddLine("a", new float[] {1f, float.NaN, 3f}).DrawOn(alone);
        Assert.Equal(0, Count(TestSupport.Content(joined), " c\n"));
        Assert.True(Count(TestSupport.Content(alone), " c\n") > 0);   // The dots of the two values
    }

    [Fact]
    public void BarChartMakesNoRoomForLabelsItDoesNotWrite() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        float[] at = new float[2];
        float[][] values = {new float[] {5f, 7f}, new float[] {5f, -1000000f}};     // The second wholly outside
        for (int k = 0; k < 2; k++) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            BarChart chart = new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f)
                    .SetCategories("a", "b").SetHorizontal(true).SetValueLabels(true).SetValueAxisRange(0f, 10f);
            chart.AddSeries("", values[k]);
            chart.DrawOn(page);
            at[k] = TestSupport.PositionOf(TestSupport.Content(page), "10")[0];
        }
        TestSupport.AssertNear(at[0], at[1], 0.01f);
    }
}
}
