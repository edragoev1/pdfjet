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
        chart.AddSeries("", new float[] {2.5f, -1f}).SetValueLabels(true);
        chart.SetFractionDigits(1);
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("2.5"), content);
        Assert.Contains(TestSupport.Hex("-1.0"), content);
    }

    [Fact]
    public void AManualAxisRangeIsUsedAsGiven() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetCategories("a");
        chart.AddSeries("", new float[] {5f}).SetValueAxisRange(0f, 12f);     // In steps of 2
        string content = Draw(chart, page);
        Assert.Contains(TestSupport.Hex("12"), content);
        Assert.Contains("<" + TestSupport.Hex("2") + ">", content);
        Assert.DoesNotContain("<" + TestSupport.Hex("3") + ">", content);
        Assert.DoesNotContain("NaN", content);
    }

    [Fact]
    public void AxisLabelsThatWouldMeetAreDrawnAtEverySecondLine() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        // 8 steps of 10 across about 70 points: 9 points a step, where a label
        // of one decimal, 70.0, needs more
        BarChart chart = NewChart(pdf).SetCategories("a").SetHorizontal(true).SetSize(120f, 200f);
        chart.AddSeries("", new float[] {72.6f}).SetValueAxisRange(0f, 80f).SetFractionDigits(1);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("80.0"), content);
        Assert.DoesNotContain(TestSupport.Hex("70.0"), content);     // would meet 60.0 and 80.0
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
        chart.SetStacked(true).SetValueLabels(true);
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
        Assert.Contains("0.88 0.89 0.91 RG\n0.5 w\n", content);    // Thin and solid
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
        chart.SetValueAxisRange(0f, 8000f).SetValueLabels(true).SetValueLabelsInside(true);
        chart.SetGroupingUsed(true).SetGridLineColor(Color.lightgray);
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
    public void AChartIsAFigureDescribedByItsDataOrItsAlternateDescription() {
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
        Assert.Equal("Sales: a 1, b 2.", page.structures[0].altDescription);
        Assert.Equal("Sales rose from 1 to 2.", page.structures[1].altDescription);
    }

    // A category name wider than two fifths of a horizontal chart is
    // shortened with an ellipsis, and a chart too small for its plot draws
    // nothing: its axis ran backwards, the bars over the names (review of 26
    // October 2026).
    [Fact]
    public void ALongNameIsShortenedAndAChartWithNoRoomDrawsNothing() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string name = "A very very long category name indeed, and longer";
        BarChart chart = NewChart(pdf).SetSize(200f, 200f).SetHorizontal(true)
                .SetCategories(name).AddSeries("s", new float[] {5f});
        TestSupport.AssertXY(250f, 250f, chart.DrawOn(page));
        string content = TestSupport.Content(page);
        Assert.DoesNotContain(TestSupport.Hex(name), content);
        string shortened = ChartScale.Shorten(TestSupport.Helvetica(pdf), TestSupport.Helvetica(pdf).GetSize(), name, 0.4f * 200f);
        // The ellipsis is 85 in WinAnsi, the encoding of a core font
        string start = shortened.Substring(0, shortened.Length - 1);
        Assert.EndsWith("\u2026", shortened);
        Assert.Contains(TestSupport.Hex(start) + "85>", content);
        page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetSize(20f, 20f).SetCategories("a").AddSeries("s", new float[] {5f}).DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
    }

    // A chart without a title has no room for one: its plot starts higher,
    // its bars taller. The legend wraps at the right edge, and a series with
    // a color per bar has the color of its first bar in it.
    [Fact]
    public void TheTopOfTheChartIsWhatItHas() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string titled = Draw(NewChart(pdf).SetTitle("t").SetCategories("a").AddSeries("", new float[] {1f}), page);
        page = new Page(pdf, Letter.PORTRAIT);
        string untitled = Draw(NewChart(pdf).SetCategories("a").AddSeries("", new float[] {1f}), page);
        Assert.True(BarHeight(untitled) > BarHeight(titled) + 10f, BarHeight(untitled) + " " + BarHeight(titled));
        page = new Page(pdf, Letter.PORTRAIT);
        BarChart wide = NewChart(pdf).SetCategories("a").SetLegend(true);
        for (int i = 0; i < 8; i++) {
            wide.AddSeries("Series number " + i, new float[] {1f});
        }
        string content = Draw(wide, page);
        Assert.NotEqual(TestSupport.PositionOf(content, "Series number 0")[1],
                TestSupport.PositionOf(content, "Series number 7")[1]);
        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a", "b").SetLegend(true)
                .AddSeries("own", new float[] {1f, 2f}, new int[] {0x123456, 0x654321}), page);
        int swatch = content.IndexOf("0.07 0.2 0.34 rg\n", System.StringComparison.Ordinal);
        Assert.True(swatch >= 0 && swatch < content.IndexOf(TestSupport.Hex("own"), System.StringComparison.Ordinal), content);
    }

    // The height of the first bar the content fills in navy.
    private static float BarHeight(string content) {
        int i = content.IndexOf(" re\n", content.IndexOf("0.12 0.23 0.37 rg\n", System.StringComparison.Ordinal),
                System.StringComparison.Ordinal);
        int start = content.LastIndexOf('\n', i) + 1;
        string[] parts = content.Substring(start, i - start).Split(' ');
        return System.Math.Abs(float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
    }

    // Under vertical bars a category is written where it leaves the one
    // before it room, as the numbers of the axis are: twelve months in full
    // ran into each other.
    [Fact]
    public void CategoriesThatDoNotFitAreSkipped() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string[] months = {"January", "February", "March", "April", "May", "June", "July",
                "August", "September", "October", "November", "December"};
        float[] values = new float[12];
        System.Array.Fill(values, 1f);
        string content = Draw(NewChart(pdf).SetCategories(months).AddSeries("s", values), page);
        Assert.Contains(TestSupport.Hex("January"), content);
        Assert.DoesNotContain(TestSupport.Hex("February"), content);
        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a", "b").AddSeries("s", new float[] {1f, 2f}), page);
        Assert.Contains(TestSupport.Hex("a"), content);
        Assert.Contains(TestSupport.Hex("b"), content);
    }

    // A null title, name or list is empty: it threw when the chart was drawn.
    [Fact]
    public void NullIsEmpty() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetTitle(null).SetSubtitle(null).SetXAxisTitle(null).SetYAxisTitle(null)
                .SetAltDescription(null).SetCategories((string[]) null).SetCategories("a")
                .AddSeries(null, new float[] {1f}).AddSeries("b", null).AddSeries("c", new float[] {2f}, (int[]) null)
                .DrawOn(page);
    }

    // A line series is drawn over the bars, in ink, with a dot at each value;
    // the axis reaches its values, the legend lists it and the description
    // has its values after the bars'.
    [Fact]
    public void ALineSeriesIsDrawnOverTheBars() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = NewChart(pdf).SetTitle("Sales").SetCategories("a", "b").SetLegend(true)
                .AddSeries("Sold", new float[] {1f, 2f}).AddLineSeries("Target", new float[] {9f, float.NaN});
        string content = Draw(chart, page);
        Assert.True(content.Contains("0.13 0.15 0.17 RG\n") && content.Contains("\n2 w\n"), content);    // In ink
        Assert.Contains(TestSupport.Hex("Target"), content);
        Assert.Contains("<" + TestSupport.Hex("8") + ">", content);     // The axis reaches 9
        Assert.Equal("Sales: Sold: a 1, b 2; Target, a line: a 9.", page.structures[0].altDescription);
        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a").AddSeries("", new float[] {1f})
                .AddLineSeries("", new float[] {2f}, 0x123456), page);
        Assert.Contains("0.07 0.2 0.34 RG\n", content);
    }

    // The review of 26 October 2026, the second: a chart without its fonts
    // draws nothing; the title, a long category under vertical bars and a
    // legend name are shortened; a line series longer than the categories
    // adds no slots; the line is on a white line a little wider; and a value
    // past the rounding of the largest float is drawn on the range of the
    // values.
    [Fact]
    public void TheSecondReviewOfTheCharts() {
        PDF pdf = new PDF(new System.IO.MemoryStream(), Compliance.PDF_UA_1).SetTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new BarChart(null, null).SetCategories("a").AddSeries("", new float[] {1f}).DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));

        string longName = "A category name far too long to be written under its bar";
        string title = "A title far too long for a chart of three hundred points, which ran past it";
        string content = Draw(NewChart(pdf).SetTitle(title).SetLegend(true).SetCategories(longName, "b")
                .AddSeries("Sold", new float[] {1f, 2f})
                .AddLineSeries("Target", new float[] {2f, 3f, 4f, 5f, 6f}), page);
        Assert.False(content.Contains(TestSupport.Hex(longName)) || content.Contains(TestSupport.Hex(title)), content);
        Assert.Contains("1 1 1 RG\n3.5 w\n", content);
        Assert.True(page.structures[0].altDescription.EndsWith("Target, a line: " + longName + " 2, b 3.",
                System.StringComparison.Ordinal), page.structures[0].altDescription);

        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a", "b").SetValueLabels(true)
                .AddSeries("", new float[] {1e30f, 3e38f}), page);
        Assert.Contains(TestSupport.Hex("3e38"), content);
    }

    // The third review of 26 October 2026: a set range past the largest float
    // draws nothing, where its NaN coordinates failed the document; a line
    // series longer than the categories does not reach the axis past them; a
    // bar below 0 has room for its value between the plot and the
    // categories; the gaps are kept in their ranges; a stacked sum past the
    // largest float is not written; and a bar below a set range has no value.
    [Fact]
    public void TheThirdReviewOfTheCharts() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Draw(NewChart(pdf).SetCategories("a").AddSeries("", new float[] {1f}).SetValueAxisRange(-3e38f, 3e38f), page);
        Assert.Equal("", TestSupport.Content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        string content = Draw(NewChart(pdf).SetCategories("a", "b").AddSeries("Sold", new float[] {1f, 2f})
                .AddLineSeries("Target", new float[] {2f, 3f, 4f, 5f, 6f}), page);
        Assert.DoesNotContain("<" + TestSupport.Hex("6") + ">", content);

        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetHorizontal(true).SetValueLabels(true).SetCategories("A")
                .AddSeries("", new float[] {-1234567f}), page);
        float label = TestSupport.PositionOf(content, "-1,234,567")[0];
        float name = TestSupport.PositionOf(content, "A")[0];
        Font font = TestSupport.Helvetica(pdf);
        Assert.True(name + font.StringWidth("A") <= label, name + " " + label);

        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a").SetGroupGap(1.5f).SetBarGap(-2f)
                .AddSeries("", new float[] {1f}).AddSeries("", new float[] {2f}), page);
        Assert.DoesNotContain(" -", content);   // No negative width

        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a").SetStacked(true).SetValueLabels(true)
                .AddSeries("", new float[] {2e38f}).AddSeries("", new float[] {2e38f}), page);
        Assert.DoesNotContain(TestSupport.Hex("Infinity"), content);

        page = new Page(pdf, Letter.PORTRAIT);
        content = Draw(NewChart(pdf).SetCategories("a", "b").SetValueLabels(true).SetValueAxisRange(10f, 20f)
                .AddSeries("", new float[] {5f, 15f}), page);
        Assert.Contains("<" + TestSupport.Hex("15") + ">", content);
        Assert.DoesNotContain("<" + TestSupport.Hex("5") + ">", content);
    }

    // The labels round the exact value half to even, as Java's DecimalFormat
    // does, on .NET Framework too, whose ToString("F") rounds 15 digits half
    // away from zero (review of 17 October 2026). .NET 8's ToString("F")
    // rounds as Java does, and the two agree on any value. Moved here from
    // the test of Chart, removed in 9.1.5.
    [Fact]
    public void TheLabelsRoundHalfToEvenAsJava() {
        System.Type type = typeof(PDF).Assembly.GetType("PDFjet.NET.NumberFormat");
        System.Func<double, int, string> format = (value, digits) => {
            object nf = type.GetMethod("GetInstance").Invoke(null, null);
            type.GetMethod("SetMaximumFractionDigits").Invoke(nf, new object[] {digits});
            type.GetMethod("SetMinimumFractionDigits").Invoke(nf, new object[] {digits});
            return (string) type.GetMethod("Format").Invoke(nf, new object[] {value});
        };
        Assert.Equal("0.12", format(0.125, 2));
        Assert.Equal("1.00", format(1.005, 2));
        Assert.Equal("2", format(2.5, 0));
        Assert.Equal("4", format(3.5, 0));
        Assert.Equal("0.00", format(-0.001, 2));
        Assert.Equal("-1.50", format(-1.5, 2));
        Assert.Equal("10000000000000000000000", format(1e22, 0));
        System.Random random = new System.Random(17);
        for (int i = 0; i < 20000; i++) {
            double value = (random.NextDouble() - 0.5) * System.Math.Pow(10, random.Next(-6, 12));
            int digits = random.Next(0, 9);
            string want = value.ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture);
            if (want.Trim('-', '0', '.').Length == 0) {
                want = want.TrimStart('-');
            }
            Assert.Equal(want, format(value, digits));
        }
    }
}
}
