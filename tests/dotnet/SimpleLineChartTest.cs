/*
 * SimpleLineChartTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
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
        Assert.Equal("Vehicles. By model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;"
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

    // A value outside a range that is set is drawn past the plot, and the
    // lines are clipped at its edges; a value far past it, or values whose
    // span is past the largest float, are drawn without an exception that
    // lost the document (review of 26 October 2026).
    [Fact]
    public void AValueOutsideTheRangeIsClipped() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).DrawOn(page);
        Assert.DoesNotContain("W\nn\n", TestSupport.Content(page));     // No clip in range
        foreach (float far in new float[] {60f, 1e37f, -1e37f}) {
            page = new Page(pdf, Letter.PORTRAIT);
            new SimpleLineChart(font, font).SetYAxisRange(0f, 10f)
                    .AddLine("a", new float[] {5f, far, 5f}).DrawOn(page);
            string content = TestSupport.Content(page);
            Assert.Contains("W\nn\n", content);
            Assert.Contains(TestSupport.Hex("a 5"), content);
        }
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).AddLine("wide", new float[] {-3e38f, 3e38f}).DrawOn(page);
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetYAxisRange(-3e38f, 3e38f).AddLine("a", new float[] {1f, 2f}).DrawOn(page);
    }

    // A null title, name or list is empty, as it was in DonutChart: it threw
    // when the chart was drawn.
    [Fact]
    public void NullIsEmpty() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        new SimpleLineChart(font, font).SetTitle(null).SetSubtitle(null).SetXAxisTitle(null)
                .SetYAxisTitle(null).SetAltDescription(null).SetCategories((string[]) null)
                .AddLine(null, new float[] {1f, 2f}).AddLine("b", null)
                .DrawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // A range that is set has grid lines on a round step: 3 to 17 was written
    // 3.00, 5.33, 7.67 in 6 lines, and is 3, 5, 7 in 7; a range of no round
    // step in 2 to 10 lines keeps its lines (review of 26 October 2026).
    [Fact]
    public void ASetRangeHasARoundStep() {
        Assert.Equal(7, ChartScale.AxisOf(new float[] {5f}, 3f, 17f, true).lines);
        Assert.Equal(10, ChartScale.AxisOf(new float[] {5f}, 0f, 10f, true).lines);
        Assert.Equal(6, ChartScale.RoundLines(0f, 1.3f * 3.7f, 6));     // No round step in 2 to 10
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetYAxisRange(3f, 17f).AddLine("a", new float[] {5f, 9f}).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.Contains(TestSupport.Hex("15"), content);
        Assert.DoesNotContain(TestSupport.Hex("5.33"), content);
    }

    // The labels at the ends of the lines, moved apart, stay over the plot:
    // they were pushed down past the bottom of the panel, and with many
    // lines up past its top. As many as the plot holds are drawn, the
    // highlighted one first, then the others in their order.
    [Fact]
    public void TheEndLabelsStayOverThePlot() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = new SimpleLineChart(font, font).SetSize(400f, 120f);
        for (int i = 0; i < 30; i++) {
            chart.AddLine("line " + i, new float[] {10f, 0f});
        }
        chart.AddLine("top", new float[] {0f, 10f}).SetHighlight(30);
        chart.DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains(TestSupport.Hex("top 10")) && content.Contains(TestSupport.Hex("line 0 0")), content);
        Assert.DoesNotContain(TestSupport.Hex("line 29 0"), content);
        // The page is 792 high, the chart from 0 to 120 down from its top
        for (int i = 0; i < 30; i++) {
            if (content.Contains(TestSupport.Hex("line " + i + " 0"))) {
                float y = TestSupport.PositionOf(content, "line " + i + " 0")[1];
                Assert.True(y > 792f - 120f && y < 792f, "line " + i + " at " + y);
            }
        }
    }

    // The values are written by their points but the last of each line, which
    // its end label has; the area under a line is filled in a tint of its
    // color, under the line.
    [Fact]
    public void TheValuesAndTheAreaAreDrawnWhenAsked() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetCategories("a", "b", "c").AddLine("x", new float[] {12f, 37f, 23f})
                .SetHighlight(0).SetValueLabels(true).SetAreaFilled(true).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.True(content.Contains(TestSupport.Hex("12")) && content.Contains(TestSupport.Hex("37")), content);
        Assert.DoesNotContain("<" + TestSupport.Hex("23") + ">", content);    // Only in "x 23"
        int area = content.IndexOf("0.78 0.8 0.84 rg\n", StringComparison.Ordinal);    // Navy, a quarter over white
        Assert.True(area > 0 && area < content.IndexOf("0.12 0.23 0.37 RG\n", StringComparison.Ordinal), content);
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).AddLine("x", new float[] {12f, 37f}).DrawOn(page);
        Assert.DoesNotContain(TestSupport.Hex("12"), TestSupport.Content(page));
    }

    // The review of 26 October 2026, the second: a chart without its fonts
    // draws nothing; the value of the first point of a rise is after it and
    // under the line, and that of a peak at the top of the plot under it.
    [Fact]
    public void TheSecondReviewOfTheCharts() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(null, null).AddLine("a", new float[] {1f, 2f}).DrawOn(page);
        Assert.Equal("", TestSupport.Content(page));
        new SimpleLineChart(font, font).AddLine("big", new float[] {0f, 3e38f}).DrawOn(page);    // Did throw

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetCategories("a", "b", "c").SetYAxisRange(0f, 50f)
                .AddLine("x", new float[] {12f, 50f, 23f}).SetValueLabels(true).DrawOn(page);
        string content = TestSupport.Content(page);
        // The points, from the line drawn in gray: "x y m" of the first
        // point, "x y l" of the peak
        int rg = content.IndexOf("0.55 0.59 0.64 RG\n", StringComparison.Ordinal);
        int m = content.IndexOf(" m\n", rg, StringComparison.Ordinal);
        int startFrom = content.LastIndexOf('\n', m) + 1;
        string[] start = content.Substring(startFrom, m - startFrom).Split(' ');
        float x0 = float.Parse(start[0], CultureInfo.InvariantCulture);
        float y0 = float.Parse(start[1], CultureInfo.InvariantCulture);
        float[] first = TestSupport.PositionOf(content, "12");
        Assert.True(first[0] > x0 && first[1] < y0, "the first value is after its point and under it: "
                + first[0] + ", " + first[1] + " by " + x0 + ", " + y0);
        int peakLine = content.IndexOf(" l\n", m, StringComparison.Ordinal);
        int peakFrom = content.LastIndexOf('\n', peakLine) + 1;
        string[] peakAt = content.Substring(peakFrom, peakLine - peakFrom).Split(' ');
        float[] peak = TestSupport.PositionOf(content, "50");     // The first "50" drawn is the axis'
        int value = content.LastIndexOf("<" + TestSupport.Hex("50") + ">", StringComparison.Ordinal);
        int td = content.LastIndexOf(" Td\n", value, StringComparison.Ordinal);
        int tdFrom = content.LastIndexOf('\n', td) + 1;
        string[] valueAt = content.Substring(tdFrom, td - tdFrom).Split(' ');
        Assert.True(float.Parse(valueAt[1], CultureInfo.InvariantCulture) < float.Parse(peakAt[1], CultureInfo.InvariantCulture),
                "the value of the peak at the top is under it: " + valueAt[1] + " by " + peakAt[1]);
        Assert.NotNull(peak);
    }

    // Values under 0.01 have the digits their axis needs, where every number
    // of the axis was 0.00 and the description said from 0 to 0 (the third
    // review of 26 October 2026).
    [Fact]
    public void TinyValuesHaveTheirDigits() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = new SimpleLineChart(font, font).AddLine("a", new float[] {0.001f, 0.004f});
        chart.DrawOn(page);
        Assert.Contains(TestSupport.Hex("a 0.004"), TestSupport.Content(page));
        Assert.Contains("from 0.001 to 0.004", chart.AltDescription());
    }

    // The number of times the text is drawn in a core font at the start of a
    // string.
    internal static int Drawn(string content, string text) {
        string hex = "<" + TestSupport.Hex(text);
        int count = 0;
        for (int i = content.IndexOf(hex, StringComparison.Ordinal); i >= 0;
                i = content.IndexOf(hex, i + 1, StringComparison.Ordinal)) {
            count++;
        }
        return count;
    }

    // An axis title longer than the plot is shortened as the other texts of
    // the chart are: the y title of a chart 250 points high ran past its top
    // and its bottom (the review of 28 October 2026).
    [Fact]
    public void ALongAxisTitleIsShortened() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        string title = "Number of battery electric vehicles registered in the state, thousands, by county";
        NewChart(pdf).SetSize(400f, 250f).SetXAxisTitle(title).SetYAxisTitle(title).DrawOn(page);
        string content = TestSupport.Content(page);
        Assert.DoesNotContain(TestSupport.Hex(title), content);
        Assert.Equal(2, Drawn(content, "Number of battery"));
    }

    // The chart keeps copies of its categories and its values: a caller that
    // reused one array for every line drew every line with the last values
    // (the review of 28 October 2026).
    [Fact]
    public void KeepsCopiesOfItsData() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        string[] categories = {"2021", "2022"};
        float[] values = {11f, 12f};
        SimpleLineChart chart = new SimpleLineChart(font, font).SetCategories(categories).AddLine("a", values);
        values[0] = 21f;
        values[1] = 22f;
        chart.AddLine("b", values);
        categories[0] = "1999";
        Assert.Equal("Line chart, 2021 to 2022: a from 11 to 12; b from 21 to 22.", chart.AltDescription());
    }

    // A null category is an empty one, which crashed DrawOn (the review of 28
    // October 2026).
    [Fact]
    public void ANullCategoryIsAnEmptyOne() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = new SimpleLineChart(font, font).SetCategories(null, "2022", null)
                .AddLine("a", new float[] {1f, 2f, 3f});
        chart.DrawOn(page);
        Assert.Contains(TestSupport.Hex("2022"), TestSupport.Content(page));
        Assert.Equal("Line chart: a from 1 to 3.", chart.AltDescription());
    }

    // A title that ends a sentence is not followed by a comma, and a line of
    // one value is described by it alone, where they were "Sales., 2019 to
    // 2024:" and "a from 5 to 5" (the review of 28 October 2026).
    [Fact]
    public void TheDescriptionReadsWell() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        SimpleLineChart chart = new SimpleLineChart(font, font).SetTitle("Sales.").SetCategories("2019", "2024")
                .AddLine("a", new float[] {5f, float.NaN}).AddLine("b", new float[] {1f, 2f});
        Assert.Equal("Sales. 2019 to 2024: a 5; b from 1 to 2.", chart.AltDescription());
        chart = new SimpleLineChart(font, font).SetTitle("Vehicles?").AddLine("a", new float[] {1f, 2f});
        Assert.Equal("Vehicles? a from 1 to 2.", chart.AltDescription());
    }

    // The numbers of an axis written with a power of ten have the significant
    // digits that write them apart: an axis of 4.84e15 to 4.85e15 was written
    // 4.84e15 at each line (the review of 28 October 2026).
    [Fact]
    public void LargeNumbersOfAnAxisAreApart() {
        PDF pdf = TestSupport.NewPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        NewChart(pdf).SetYAxisRange(4.84e15f, 4.85e15f).DrawOn(page);
        string content = TestSupport.Content(page);
        foreach (string label in new string[] {"4.84e15", "4.841e15", "4.849e15"}) {
            Assert.Equal(1, Drawn(content, label));
        }
        Assert.Equal(4, ChartScale.StepSignificant(4.84e15f, 2e12f));
        Assert.Equal(3, ChartScale.StepSignificant(12345f, 1000f));
    }

    // The content of the chart, on a page of its own.
    private static String NoteContent(PDF pdf, SimpleLineChart chart) {
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.SetLocation(50f, 50f).DrawOn(page);
        return TestSupport.Content(page);
    }

    private static SimpleLineChart NoteChart(Font font) {
        return new SimpleLineChart(font, font).SetTitle("T").SetSize(300f, 200f).SetCategories("a", "b", "c");
    }

    // A value outside a range that is set cuts its line, and the chart says
    // so, in print and in its description: an outlier was clipped without a
    // word. A value that is no number is not outside, and a chart with none
    // outside adds nothing.
    [Fact]
    public void ItSaysHowManyValuesAreOutsideTheRange() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        float nan = float.NaN;
        float inf = float.PositiveInfinity;
        String one = "1 value outside the range";
        String two = "2 values outside the range";
        Object[][] cases = {
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, 100f, 3f}), ""},
            new Object[] {NoteChart(font).AddLine("x", new float[] {0f, 5f, 10f}).SetYAxisRange(0f, 10f), ""},
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, nan, inf}).SetYAxisRange(0f, 10f), ""},
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, 2f, 3f, 50f}).SetYAxisRange(0f, 10f), ""},
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, 50f, 3f}).SetYAxisRange(0f, 10f), one},
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, 2f, -50f}).SetYAxisRange(0f, 10f), one},
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, 50f, -50f}).SetYAxisRange(0f, 10f), two},
            new Object[] {NoteChart(font).AddLine("x", new float[] {1f, 50f, 3f}).AddLine("y", new float[] {50f, 2f, 3f})
                    .SetYAxisRange(0f, 10f), two},
        };
        foreach (Object[] c in cases) {
            SimpleLineChart chart = (SimpleLineChart) c[0];
            String note = (String) c[1];
            String content = NoteContent(pdf, chart);
            String description = chart.AltDescription();
            if (note.Length == 0) {
                Assert.DoesNotContain(TestSupport.Hex(" outside"), content);
                Assert.DoesNotContain("outside", description);
            } else {
                Assert.Contains("<" + TestSupport.Hex(note) + ">", content);
                Assert.EndsWith("; " + note + ".", description);
            }
        }
    }

    // The note is in the gray of the numbers, in the small size, on a row of
    // its own inside the chart, under the categories and the x title, with the
    // plot that much lower and nothing else moved; its right end at the plot's.
    [Fact]
    public void TheNoteIsUnderThePlot() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        String note = "1 value outside the range";
        float size = font.GetSize();
        float small = 0.8f * size;
        foreach (String title in new String[] {"", "year"}) {
            String[] drawn = new String[2];
            for (int k = 0; k < 2; k++) {
                drawn[k] = NoteContent(pdf, new SimpleLineChart(font, font).SetSize(300f, 200f)
                        .SetXAxisTitle(title).SetCategories("a", "b", "c").SetYAxisRange(0f, 10f)
                        .AddLine("x", new float[] {1f, k == 0 ? 2f : 50f, 3f}));
            }
            String with = drawn[1];
            float[] at = TestSupport.PositionOf(with, note);
            float end = at[0] + font.StringWidth(small, note);
            float[] c = TestSupport.PositionOf(with, "c");
            Assert.True(Math.Abs(c[0] + font.StringWidth(size, "c") / 2f - end) <= 0.1f);
            Assert.True(at[0] >= 50f && end <= 350f, "the note at " + at[0]);
            Assert.True(Math.Abs(792f - (250f - size) - at[1]) <= 0.01f);
            Assert.True(Math.Abs(TestSupport.PositionOf(drawn[0], "c")[1] + 1.4f * small - c[1]) <= 0.02f);
            float last = title.Length == 0 ? c[1] : TestSupport.PositionOf(with, title)[1];
            Assert.True(at[1] + font.GetAscent(small) <= last - font.GetDescent(size),
                    "the note over the row above it");
            Assert.Equal(TestSupport.FillColorBefore(with, "c"), TestSupport.FillColorBefore(with, note));
        }
        // In a chart too narrow for it, shortened and inside
        SimpleLineChart chart = new SimpleLineChart(font, font).SetSize(110f, 200f)
                .SetYAxisRange(0f, 10f).AddLine("", new float[] {1f, 50f, 3f});
        Assert.True(TestSupport.PositionOf(NoteContent(pdf, chart), "1 value")[0] >= 50f + size);
        Assert.EndsWith("; " + note + ".", chart.AltDescription());
    }

    // A description that is set is followed by the note, for a screen reader
    // to have what the chart prints; with none outside it stays as it is.
    [Fact]
    public void TheNoteFollowsADescriptionThatIsSet() {
        Font font = TestSupport.Helvetica(TestSupport.NewPDF());
        SimpleLineChart chart = new SimpleLineChart(font, font).AddLine("a", new float[] {1f, 2f})
                .SetAltDescription("A line");
        Assert.Equal("A line", chart.AltDescription());
        chart.AddLine("b", new float[] {1f, 50f}).SetYAxisRange(0f, 10f);
        Assert.Equal("A line. 1 value outside the range.", chart.AltDescription());
    }

    // In a tagged document the note is text of the chart's figure, and its
    // description says it.
    [Fact]
    public void TheNoteIsInTheFigure() {
        System.IO.MemoryStream bos = new System.IO.MemoryStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).SetTitle("Test");
        Font font = new Font(pdf, TestSupport.Open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).SetSize(300f, 200f).SetYAxisRange(0f, 10f)
                .AddLine("a", new float[] {1f, 50f}).DrawOn(page);
        String content = TestSupport.Content(page);
        Assert.Equal(content.IndexOf("BDC\n"), content.LastIndexOf("BDC\n"));
        Assert.Equal(content.IndexOf("EMC\n"), content.LastIndexOf("EMC\n"));
        Assert.True(content.LastIndexOf("ET\n") < content.LastIndexOf("EMC\n"));
        pdf.Complete();
        Assert.Contains("/Alt " + TestSupport.TextString("Line chart: a from 1 to 50; 1 value outside the range."),
                TestSupport.Latin1(bos.ToArray()));
    }

    // A chart barely tall enough keeps its plot, as drawn without the note,
    // and its description says the number: the note cost it the plot.
    [Fact]
    public void TheNoteNeverCostsThePlot() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        // The least height that draws, found from below
        float height = 1f;
        while (!NoteContent(pdf, PlotChart(font, height, 2f)).Contains(" Td\n")) {
            height += 0.25f;
        }
        foreach (float h in new float[] {height, height + 1f, height + 5f}) {
            String without = NoteContent(pdf, PlotChart(font, h, 2f));
            SimpleLineChart chart = PlotChart(font, h, 50f);
            String with = NoteContent(pdf, chart);
            Assert.DoesNotContain(TestSupport.Hex(" outside"), with);
            Assert.Contains("W\nn\n", with);
            Assert.Equal(without.Split(" Td\n").Length, with.Split(" Td\n").Length);
            Assert.EndsWith("; 1 value outside the range.", chart.AltDescription());
        }
        // With the room, the note
        Assert.Contains(TestSupport.Hex(" outside"), NoteContent(pdf, PlotChart(font, height + 20f, 50f)));
    }

    private static SimpleLineChart PlotChart(Font font, float height, float middle) {
        return new SimpleLineChart(font, font).SetSize(300f, height).SetYAxisRange(0f, 10f)
                .AddLine("a", new float[] {1f, middle, 3f});
    }
}
}   // End of namespace PDFjet.NET
