/*
 * BarChartReviewTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The findings of the review of BarChart, DonutChart and ChartScale, of 28
/// October 2026.
/// </summary>
public class BarChartReviewTest {
    private static BarChart NewChart(PDF pdf) {
        Font font = TestSupport.Helvetica(pdf);
        return new BarChart(font, font).SetLocation(50f, 50f).SetSize(300f, 200f);
    }

    private static string Draw(BarChart chart, Page page) {
        TestSupport.AssertXY(350f, 250f, chart.DrawOn(page));
        return TestSupport.Content(page);
    }

    // Returns how many times the text is drawn in a core font.
    private static int Count(string content, string text) {
        string hex = "<" + TestSupport.Hex(text) + ">";
        int count = 0;
        for (int i = content.IndexOf(hex, System.StringComparison.Ordinal); i != -1;
                i = content.IndexOf(hex, i + 1, System.StringComparison.Ordinal)) {
            count++;
        }
        return count;
    }

    private static float[] Twelve(float value) {
        float[] values = new float[12];
        for (int i = 0; i < values.Length; i++) {
            values[i] = value;
        }
        return values;
    }

    // The total of a stack below a range that is set is not written: it was
    // written under the plot, or left of it by horizontal bars.
    [Fact]
    public void ATotalBelowARangeThatIsSetIsNotWritten() {
        PDF pdf = TestSupport.NewPDF();
        foreach (bool horizontal in new bool[] {false, true}) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            BarChart chart = NewChart(pdf).SetCategories("A", "B").SetStacked(true).SetValueLabels(true)
                    .SetHorizontal(horizontal).SetValueAxisRange(50f, 100f)
                    .AddSeries("x", new float[] {10f, 30f}).AddSeries("y", new float[] {10f, 30f});
            string content = Draw(chart, page);
            Assert.Equal(0, Count(content, "20"));
            Assert.Equal(1, Count(content, "30"));     // B's second segment
        }
    }

    // Values under 0.01 are written with the digits of the step of the axis,
    // and described with theirs; the values that had their digits keep them.
    [Fact]
    public void SmallValuesAreNotWrittenAsZero() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetTitle("Rates").SetCategories("A", "B").SetValueLabels(true)
                .AddSeries("s", new float[] {0.00012f, 0.00034f});
        string content = Draw(chart, page);
        Assert.Equal(1, Count(content, "0.00012"));
        Assert.Equal(1, Count(content, "0.00034"));
        Assert.Equal("Rates: s: A 0.00012, B 0.00034.", page.structures[0].altDescription);

        page = new Page(pdf, Letter.PORTRAIT);
        chart = NewChart(pdf).SetCategories("a", "b", "c").SetValueLabels(true)
                .AddSeries("", new float[] {12.5f, 1234f, 0.012f});
        content = Draw(chart, page);
        foreach (string label in new string[] {"12.5", "1,234", "0.01"}) {
            Assert.Equal(1, Count(content, label));
        }
        Assert.Equal("Bar chart: a 12.5, b 1,234, c 0.012.", page.structures[0].altDescription);
    }

    // A label wider than its bar is not written inside it: a stacked one is
    // left out, and one inside a grouped bar is written past its end. The
    // labels past the ends of narrow bars, and the totals over narrow stacks,
    // are written where they leave the one before them room.
    [Fact]
    public void LabelsKeepToTheWidthOfTheirBars() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string content = Draw(NewChart(pdf).SetStacked(true).SetValueLabels(true)
                .AddSeries("", Twelve(12345f)).AddSeries("", Twelve(12345f)), page);
        Assert.Equal(0, Count(content, "12,345"));
        int totals = Count(content, "24,690");
        Assert.True(totals > 0 && totals < 12, totals + " totals of 12");

        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetValueLabels(true).SetValueLabelsInside(true)
                .AddSeries("", Twelve(12345f)), page);
        Assert.DoesNotContain("1 1 1 rg", content);
        int labels = Count(content, "12,345");
        Assert.True(labels > 0 && labels < 12, labels + " labels of 12");
    }

    // By horizontal bars a category is written where it leaves the one before
    // it room: forty of them ran into each other.
    [Fact]
    public void HorizontalCategoriesThatDoNotFitAreSkipped() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string[] names = new string[40];
        float[] values = new float[40];
        for (int i = 0; i < 40; i++) {
            names[i] = "Category " + i;
            values[i] = i + 1;
        }
        string content = Draw(NewChart(pdf).SetHorizontal(true).SetCategories(names).AddSeries("", values), page);
        Assert.Equal(1, Count(content, "Category 0"));
        Assert.Equal(0, Count(content, "Category 1"));
    }

    // The numbers of an axis written with a power of ten have the significant
    // digits that write them apart.
    [Fact]
    public void LargeNumbersOfAnAxisAreApart() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string content = Draw(NewChart(pdf).SetCategories("a").SetValueAxisRange(4.84e15f, 4.85e15f)
                .AddSeries("", new float[] {4.845e15f}), page);
        Assert.Equal(1, Count(content, "4.84e15"));
        Assert.Equal(1, Count(content, "4.841e15"));
        Assert.Equal("4.84e15", ChartScale.Format(4.841e15f, 0, 0));
    }

    // A percentage is written inside its slice only when the slice is as long
    // across the middle of the ring as the percentage is wide.
    [Fact]
    public void APercentageWiderThanItsSliceIsNotWritten() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.Helvetica(pdf);
        DonutChart chart = new DonutChart(font, font).SetLocation(100f, 100f).SetRadii(20f, 0f)
                .Add("a", 1f).Add("b", 17f);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Equal(0, Count(content, "6%"));
        Assert.Equal(1, Count(content, "94%"));
    }

    // A null category is an empty one: it failed the drawing with a
    // NullReferenceException.
    [Fact]
    public void ANullCategoryIsAnEmptyOne() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        string content = Draw(NewChart(pdf).SetCategories("a", null).AddSeries("", new float[] {1f, 2f}), page);
        Assert.Equal(1, Count(content, "a"));
        Assert.Equal("Bar chart: a 1, 2.", page.structures[0].altDescription);
    }
}
}
