/*
 * BarChart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Bar chart renderer for PDF pages: one slot per category, the bars of the
/// series grouped inside each slot. See Example_27, of horizontal and
/// vertical bars.
/// </summary>
public class BarChart : IDrawable {
    /// <summary>
    /// One series of the chart: a name, a value per category and a color, or
    /// a color per category.
    /// </summary>
    private sealed class Series {
        internal readonly String name;
        internal readonly float[] values;
        internal readonly int color;
        internal readonly int[] colors;     // null unless each bar has its own color
        internal Series(String name, float[] values, int color, int[] colors) {
            this.name = name;
            this.values = values;
            this.color = color;
            this.colors = colors;
        }
    }

    private const int NO_COLOR = -1;

    // The look of the simple charts: ink for the title and the values, a
    // muted gray for the rest of the text, light grid lines, and a palette
    // whose colors differ in lightness, so that they stay apart in grayscale
    // and for a reader who does not tell red from green.
    private const int INK = SimpleBarChart.INK;
    private const int MUTED = SimpleBarChart.MUTED;
    private const int GRID = 0xE1E4E8;
    private const int BASELINE = 0x8C96A3;
    internal static readonly int[] PALETTE = {
        0x1F3A5F,   // navy
        0x6F95C2,   // steel blue
        0xD08C3C,   // amber
        0x76987C,   // sage
        0xA7AFB8,   // gray
        0x9C5B5B,   // brick
        0x4F7A8C,   // slate
        0xC9B458    // ochre
    };

    private float x1;
    private float y1;
    private float w = 300f;
    private float h = 200f;

    private String title = "";
    private String subtitle = "";
    private String altDescription = null;
    private String xAxisTitle = "";
    private String yAxisTitle = "";

    private readonly List<String> categories = new List<String>();
    private readonly List<Series> series = new List<Series>();
    private readonly List<Series> lineSeries = new List<Series>();   // Drawn as lines over the bars

    private bool horizontal = false;
    private bool stacked = false;
    private float groupGap = 0.3f;
    private float barGap = 0f;

    private bool drawGridLines = true;
    private bool drawValueLabels = false;
    private bool valueLabelsInside = false;
    private bool drawLegend = true;
    private bool groupingUsed = true;

    private int gridLineColor = GRID;

    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;

    // The range of the value axis, from the data unless it is set
    private float min;
    private float max;
    private bool rangeSet = false;

    // f1 = chart title font, f2 = axis titles, labels and legend font
    private readonly Font f1;
    private readonly Font f2;

    /// <summary>
    /// Creates a bar chart.
    /// </summary>
    /// <param name="f1">the font for the chart title.</param>
    /// <param name="f2">the font for the axis titles, the labels and the legend.</param>
    public BarChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /// <summary>Sets the chart title.</summary>
    /// <param name="title">the title.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetTitle(String title) {
        this.title = title ?? "";
        return this;
    }

    /// <summary>Sets the subtitle, written in gray under the title in the second font.</summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetSubtitle(String subtitle) {
        this.subtitle = subtitle ?? "";
        return this;
    }

    /// <summary>
    /// Sets the alternate description of the chart, which a screen reader reads
    /// in a PDF/UA document, where the chart is a figure. The default is the
    /// title and the subtitle, or "Bar chart" without them, and the values of
    /// each series by category. Describe what the chart shows.
    /// </summary>
    /// <param name="altDescription">the alternate description.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetAltDescription(String altDescription) {
        this.altDescription = altDescription ?? "";
        return this;
    }

    /// <summary>Sets the X axis title.</summary>
    /// <param name="title">the title.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetXAxisTitle(String title) {
        this.xAxisTitle = title ?? "";
        return this;
    }

    /// <summary>Sets the Y axis title.</summary>
    /// <param name="title">the title.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetYAxisTitle(String title) {
        this.yAxisTitle = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the categories, one per group of bars, in the order they are drawn:
    /// left to right in a vertical chart, top to bottom in a horizontal one.
    /// </summary>
    /// <param name="categories">the category labels.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetCategories(params String[] categories) {
        this.categories.Clear();
        if (categories != null) {
            this.categories.AddRange(categories);
        }
        return this;
    }

    /// <summary>
    /// Adds a series drawn in the next color of the default palette. A value
    /// that is NaN or infinite has no bar.
    /// </summary>
    /// <param name="name">the series name, shown in the legend; empty for none.</param>
    /// <param name="values">one value per category.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart AddSeries(String name, float[] values) {
        return AddSeries(name, values, NO_COLOR);
    }

    /// <summary>Adds a series drawn in the specified color.</summary>
    /// <param name="name">the series name, shown in the legend; empty for none.</param>
    /// <param name="values">one value per category.</param>
    /// <param name="color">the bar color as a 0xRRGGBB value, for example Color.blue.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart AddSeries(String name, float[] values, int color) {
        series.Add(new Series(name == null ? "" : name, values == null ? new float[0] : (float[]) values.Clone(), color, null));
        return this;
    }

    /// <summary>
    /// Adds a series with a color per category, for a chart whose bars each
    /// have their own color. A bar past the end of the colors has the next
    /// color of the default palette.
    /// </summary>
    /// <param name="name">the series name, shown in the legend; empty for none.</param>
    /// <param name="values">one value per category.</param>
    /// <param name="colors">one 0xRRGGBB color per category.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart AddSeries(String name, float[] values, int[] colors) {
        series.Add(new Series(name == null ? "" : name, values == null ? new float[0] : (float[]) values.Clone(), NO_COLOR,
                colors == null ? null : (int[]) colors.Clone()));
        return this;
    }

    /// <summary>
    /// Adds a series drawn as a line over the bars, in ink, a dot at each
    /// category: a target, the average, or last year. It shares the value
    /// axis with the bars, is not stacked, and is in the legend and the
    /// description. A value that is NaN or infinite leaves a gap in the line.
    /// </summary>
    /// <param name="name">the series name, shown in the legend; empty for none.</param>
    /// <param name="values">one value per category.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart AddLineSeries(String name, float[] values) {
        return AddLineSeries(name, values, INK);
    }

    /// <summary>Adds a series drawn as a line over the bars, in the specified color.</summary>
    /// <param name="name">the series name, shown in the legend; empty for none.</param>
    /// <param name="values">one value per category.</param>
    /// <param name="color">the line color as a 0xRRGGBB value.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart AddLineSeries(String name, float[] values, int color) {
        lineSeries.Add(new Series(name == null ? "" : name, values == null ? new float[0] : (float[]) values.Clone(), color, null));
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of this chart.</summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>Sets the size of this chart.</summary>
    /// <param name="w">the width.</param>
    /// <param name="h">the height.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /// <summary>Sets whether the bars are horizontal. The default is vertical bars.</summary>
    /// <param name="horizontal">true for horizontal bars.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetHorizontal(bool horizontal) {
        this.horizontal = horizontal;
        return this;
    }

    /// <summary>
    /// Sets whether the series are stacked: one bar per category, with the
    /// value of each series as a segment of it. The values above 0 stack up
    /// from 0 and the values below 0 stack down. The default is grouped bars.
    /// </summary>
    /// <param name="stacked">true for stacked bars.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetStacked(bool stacked) {
        this.stacked = stacked;
        return this;
    }

    /// <summary>
    /// Sets the gap between the groups of bars as a fraction of the category
    /// slot, from 0.0 to below 1.0. The default is 0.3.
    /// </summary>
    /// <param name="gap">the gap.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetGroupGap(float gap) {
        // From 0 to 0.9 of a slot: more left the bars no width, or less than none
        if (ChartScale.IsValue(gap)) {
            this.groupGap = Math.Min(Math.Max(gap, 0f), 0.9f);
        }
        return this;
    }

    /// <summary>
    /// Sets the gap between the bars of a group as a fraction of the bar width.
    /// The default is 0.0, so the bars of a group touch.
    /// </summary>
    /// <param name="gap">the gap.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetBarGap(float gap) {
        // 0 or more: a negative gap made bars of no width, and -1 divided by 0
        if (ChartScale.IsValue(gap)) {
            this.barGap = Math.Max(gap, 0f);
        }
        return this;
    }

    /// <summary>Sets whether the grid lines of the value axis are drawn.</summary>
    /// <param name="drawGridLines">true to draw them.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetGridLines(bool drawGridLines) {
        this.drawGridLines = drawGridLines;
        return this;
    }

    /// <summary>
    /// Sets whether the value of each bar is written at its end, or, in a
    /// stacked chart, inside each segment that has room for it.
    /// </summary>
    /// <param name="drawValueLabels">true to write the values.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetValueLabels(bool drawValueLabels) {
        this.drawValueLabels = drawValueLabels;
        return this;
    }

    /// <summary>
    /// Sets whether the value labels are written inside the bars, at the end
    /// of each bar, in white or in ink, whichever reads on the bar's color,
    /// instead of next to the bar ends. A bar too short for its label gets it
    /// next to its end. The default is false.
    /// </summary>
    /// <param name="valueLabelsInside">true to write the values inside the bars.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetValueLabelsInside(bool valueLabelsInside) {
        this.valueLabelsInside = valueLabelsInside;
        return this;
    }

    /// <summary>
    /// Sets whether the labels group the digits in thousands with a comma, as
    /// in 6,650. The default is true.
    /// </summary>
    /// <param name="groupingUsed">true to group the digits.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetGroupingUsed(bool groupingUsed) {
        this.groupingUsed = groupingUsed;
        return this;
    }

    /// <summary>
    /// Sets whether the legend is drawn. The legend lists the series that have
    /// a name, under the title.
    /// </summary>
    /// <param name="drawLegend">true to draw the legend.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetLegend(bool drawLegend) {
        this.drawLegend = drawLegend;
        return this;
    }

    /// <summary>
    /// Sets the color of the grid lines. The default is light gray, 0xE1E4E8.
    /// Color.transparent leaves it unchanged.
    /// </summary>
    /// <param name="color">the color as a 0xRRGGBB value, for example Color.lightgray.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetGridLineColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        this.gridLineColor = color;
        return this;
    }

    /// <summary>
    /// Sets the number of digits after the decimal point of the values written
    /// by the bars, all of them alike, as SimpleBarChart's. Without it a value
    /// has as many as it needs, two at most. The numbers of the value axis have
    /// the digits of its step.
    /// </summary>
    /// <param name="digits">the number of digits.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetFractionDigits(int digits) {
        this.minFractionDigits = digits;
        this.maxFractionDigits = digits;
        return this;
    }

    /// <summary>
    /// Sets the range of the value axis, with grid lines on a round step, as
    /// SetYAxisRange does in SimpleLineChart and SimpleScatterChart. Without it
    /// the range is that of the data, rounded to steps, and always includes 0.
    /// A range whose end is not past its start is not set.
    /// </summary>
    /// <param name="min">the value at the start of the axis.</param>
    /// <param name="max">the value at the end of the axis.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetValueAxisRange(float min, float max) {
        this.min = min;
        this.max = max;
        this.rangeSet = max > min;
        return this;
    }

    /// <summary>
    /// Draws this chart on the specified page. A category name wider than
    /// two fifths of a horizontal chart is shortened with an ellipsis, and a
    /// chart too small for its plot draws nothing.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the bottom right corner coordinates [x, y].</returns>
    public float[] DrawOn(Page page) {
        int n = NumberOfCategories();
        if (page == null || n == 0 || f1 == null || f2 == null) {  // Measured, or nothing to draw
            return new float[] {x1 + w, y1 + h};
        }

        float vMin;
        float vMax;
        int lines;
        if (rangeSet) {
            vMin = min;
            vMax = max;
            lines = ChartScale.RoundLines(min, max,
                    Math.Max(ChartScale.RoundMaxAndMinValues(max, min).numOfGridLines, 1));
        } else {
            float lo = 0f;
            float hi = 0f;
            if (stacked) {
                // The sums of the values above and below 0 in each category
                for (int i = 0; i < n; i++) {
                    float up = 0f;
                    float down = 0f;
                    foreach (Series s in series) {
                        if (i < s.values.Length && ChartScale.IsValue(s.values[i])) {
                            if (s.values[i] >= 0f) { up += s.values[i]; } else { down += s.values[i]; }
                        }
                    }
                    lo = Math.Min(lo, down);
                    hi = Math.Max(hi, up);
                }
                // The values of a line series up to the last category, which
                // are all it draws
                foreach (Series s in lineSeries) {
                    for (int i = 0; i < Math.Min(s.values.Length, n); i++) {
                        if (ChartScale.IsValue(s.values[i])) {
                            lo = Math.Min(lo, s.values[i]);
                            hi = Math.Max(hi, s.values[i]);
                        }
                    }
                }
            } else {
                foreach (Series s in AllSeries()) {
                    for (int i = 0; i < Math.Min(s.values.Length, n); i++) {
                        if (ChartScale.IsValue(s.values[i])) {
                            lo = Math.Min(lo, s.values[i]);
                            hi = Math.Max(hi, s.values[i]);
                        }
                    }
                }
            }
            if (hi == lo) { hi = lo + ChartScale.FlatDataSpan(lo); }
            Round round = ChartScale.RoundMaxAndMinValues(hi, lo);
            vMin = round.minValue;
            vMax = round.maxValue;
            lines = round.numOfGridLines;
        }
        if (vMax == vMin) { vMax = vMin + ChartScale.FlatDataSpan(vMin); }
        float step = (vMax - vMin) / lines;
        int axisDigits = Math.Max(minFractionDigits, ChartScale.FractionDigitsOf(step, maxFractionDigits));
        float baseValue = Math.Min(Math.Max(0f, vMin), vMax);

        float x2 = x1 + w;
        float y2 = y1 + h;
        float bodyHeight = f2.GetBodyHeight();
        float ascent = f2.GetAscent();
        float pad = bodyHeight / 2f;
        bool legend = drawLegend && HasSeriesNames();

        // Widest labels on the value axis and next to the bars
        float widestAxisLabel = 0f;
        for (int i = 0; i <= lines; i++) {
            String label = AxisLabel(vMin + step * i, axisDigits);
            widestAxisLabel = Math.Max(widestAxisLabel, f2.StringWidth(label));
        }
        float widestValueLabel = 0f;
        float widestNegative = 0f;      // Of the bars below 0, written past their ends
        if (drawValueLabels) {
            foreach (Series s in series) {
                foreach (float v in s.values) {
                    if (!ChartScale.IsValue(v)) {
                        continue;
                    }
                    float width = f2.StringWidth(ValueLabel(v));
                    widestValueLabel = Math.Max(widestValueLabel, width);
                    if (v < 0f) {
                        widestNegative = Math.Max(widestNegative, width);
                    }
                }
            }
        }
        // The totals of the stacks, written past their ends
        bool totals = drawValueLabels && stacked && series.Count > 1;
        float widestTotal = 0f;
        for (int i = 0; totals && i < n; i++) {
            float up = 0f;
            foreach (Series s in series) {
                if (i < s.values.Length && ChartScale.IsValue(s.values[i]) && s.values[i] >= 0f) {
                    up += s.values[i];
                }
            }
            if (up > 0f) {
                widestTotal = Math.Max(widestTotal, f2.StringWidth(ValueLabel(up)));
            }
        }
        // The names of the categories, shortened at the left of horizontal bars
        List<String> names = new List<String>();
        float widestCategory = 0f;
        foreach (String category in categories) {
            // Two fifths of a horizontal chart at most, a quarter under vertical bars
            String name = ChartScale.Shorten(f2, f2.GetSize(), category, (horizontal ? 0.4f : 0.25f) * w);
            names.Add(name);
            widestCategory = Math.Max(widestCategory, f2.StringWidth(name));
        }

        // Margins and the plot area
        // The title, the subtitle and the legend, its rows wrapped at the
        // right edge; no room for a title the chart has not
        float titleBaseline = y1 + (title.Length == 0 ? 0f : 1.5f * f1.GetBodyHeight());
        float subtitleHeight = subtitle.Length == 0 ? 0f : bodyHeight;
        int legendRows = legend ? DrawLegend(null, 0f) : 0;
        float topMargin = (title.Length == 0 ? f1.GetBodyHeight() : 2.5f * f1.GetBodyHeight()) + subtitleHeight
                + legendRows * (1.5f * bodyHeight);
        // Room between the plot and the categories for the label of a bar
        // below 0 that reaches the end of the axis: it was written over the
        // category's name
        float negative = 0f;
        if (drawValueLabels && !stacked && widestNegative > 0f) {
            negative = horizontal ? widestNegative + pad / 2f : bodyHeight;
        }
        float leftMargin = 1.5f * bodyHeight + pad + (horizontal ? widestCategory + negative : widestAxisLabel);
        float rightMargin = pad;
        float bottomMargin = 2.5f * bodyHeight + (horizontal ? 0f : negative);
        if (horizontal) {
            float past = totals ? widestTotal + pad : (valueLabelsInside ? 0f : widestValueLabel + pad);
            rightMargin += Math.Max(widestAxisLabel / 2f, past);
        } else if (totals || (drawValueLabels && !stacked && !valueLabelsInside)) {
            topMargin += bodyHeight;
        }
        float x5 = x1 + leftMargin;
        float y5 = y1 + topMargin;
        float x6 = x2 - rightMargin;
        float y8 = y2 - bottomMargin;
        // No room for the plot, or an axis whose span is past the largest
        // float, as a set range of -3e38 to 3e38, whose steps are infinite
        if (x6 <= x5 || y8 <= y5 || !ChartScale.IsValue(step)) {
            return new float[] {x1 + w, y1 + h};
        }

        // The chart is one figure, described by its alternate description.
        page.AddBDC(StructElem.FIGURE, null, AltDescription());
        // The pen, the brush and the dash pattern of the caller are kept,
        // as a Stamp keeps them.
        page.SaveGraphicsState();
        page.SetDefaultStrokeDashPattern();     // Not the caller's, which dashed the legend's lines

        // Title, the subtitle and then the legend under it, at the left
        // The title and the subtitle shortened to the width of the chart
        DrawText(page, f1, ChartScale.Shorten(f1, f1.GetSize(), title, w), x1, titleBaseline, INK);
        if (subtitle.Length > 0) {
            DrawText(page, f2, ChartScale.Shorten(f2, f2.GetSize(), subtitle, w),
                    x1, titleBaseline + subtitleHeight, MUTED);
        }
        if (legend) {
            DrawLegend(page, titleBaseline + subtitleHeight + 1.5f * bodyHeight);
        }

        // Grid lines and value axis labels. A label is drawn at every grid line
        // that leaves it room, the widest label and a pad across, or a line high
        // up the vertical axis: at every second or third line, or more, when
        // the labels would run into each other, the lines all drawn.
        int every = horizontal
                ? ChartScale.AxisLabelStep((x6 - x5) / lines, widestAxisLabel + pad, lines)
                : ChartScale.AxisLabelStep((y8 - y5) / lines, bodyHeight, lines);
        for (int i = 0; i <= lines; i++) {
            float v = vMin + step * i;
            String label = AxisLabel(v, axisDigits);
            bool labeled = i % every == 0;
            if (horizontal) {
                float x = x5 + ChartScale.Scaled(v - vMin, x6 - x5, vMax - vMin);
                if (drawGridLines) {
                    GridLine(page, x, y5, x, y8);
                }
                if (labeled) {
                    DrawText(page, f2, label, x - f2.StringWidth(label) / 2f, y8 + bodyHeight, MUTED);
                }
            } else {
                float y = y8 - ChartScale.Scaled(v - vMin, y8 - y5, vMax - vMin);
                if (drawGridLines) {
                    GridLine(page, x5, y, x6, y);
                }
                if (labeled) {
                    DrawText(page, f2, label, x5 - pad - f2.StringWidth(label), y + ascent / 2f, MUTED);
                }
            }
        }

        // Bars, category labels and value labels
        int m = series.Count;
        float slot = (horizontal ? (y8 - y5) : (x6 - x5)) / n;
        float groupWidth = slot * (1f - groupGap);
        float barWidth = (m == 0 || stacked) ? groupWidth : groupWidth / (m + (m - 1) * barGap);
        // Under vertical bars a category is written where it leaves the one
        // before it room: at every second or third bar, or more
        int everyCategory = horizontal ? 1 : ChartScale.AxisLabelStep(slot, widestCategory + pad, n);
        for (int i = 0; i < n; i++) {
            float slotStart = (horizontal ? y5 : x5) + i * slot;
            float groupStart = slotStart + slot * groupGap / 2f;
            String category = i < names.Count ? names[i] : "";
            if (horizontal) {
                DrawText(page, f2, category, x5 - pad - negative - f2.StringWidth(category),
                        slotStart + slot / 2f + ascent / 2f, MUTED);
            } else if (i % everyCategory == 0) {
                DrawText(page, f2, category, slotStart + (slot - f2.StringWidth(category)) / 2f,
                        y8 + bodyHeight + negative, MUTED);
            }
            float up = 0f;      // the stacked values above 0 so far
            float down = 0f;    // the stacked values below 0 so far
            for (int j = 0; j < m; j++) {
                Series s = series[j];
                if (i >= s.values.Length || !ChartScale.IsValue(s.values[i])) {
                    continue;
                }
                // A bar runs from the base to its value; a segment of a stack
                // from the sum of the segments before it to that sum plus its value
                float from = baseValue;
                float to = s.values[i];
                if (stacked) {
                    if (to >= 0f) { from = up; up += to; } else { from = down; down += to; }
                    to = from + s.values[i];
                }
                // A bar wholly outside a range that is set has no length,
                // and its value is not written by it
                bool outside = Math.Min(Math.Max(from, vMin), vMax) == Math.Min(Math.Max(to, vMin), vMax)
                        && from != to;
                from = Math.Min(Math.Max(from, vMin), vMax);
                to = Math.Min(Math.Max(to, vMin), vMax);
                float barStart = stacked ? groupStart : groupStart + j * barWidth * (1f + barGap);
                int color = BarColor(s, j, i);
                page.SetBrushColor(color);
                String label = (drawValueLabels && !outside) ? ValueLabel(s.values[i]) : null;
                int on = LabelColorOn(color);
                if (horizontal) {
                    float x0 = x5 + ChartScale.Scaled(from - vMin, x6 - x5, vMax - vMin);
                    float x = x5 + ChartScale.Scaled(to - vMin, x6 - x5, vMax - vMin);
                    page.FillRect(Math.Min(x0, x), barStart, Math.Abs(x - x0), barWidth);
                    float baseline = barStart + barWidth / 2f + ascent / 2f;
                    if (label != null && stacked) {
                        if (Math.Abs(x - x0) >= f2.StringWidth(label) + pad) {
                            DrawText(page, f2, label,
                                    Math.Min(x0, x) + (Math.Abs(x - x0) - f2.StringWidth(label)) / 2f, baseline, on);
                        }
                    } else if (label != null && valueLabelsInside && Math.Abs(x - x0) >= f2.StringWidth(label) + pad) {
                        DrawText(page, f2, label,
                                to >= baseValue ? x - pad / 2f - f2.StringWidth(label) : x + pad / 2f, baseline, on);
                    } else if (label != null) {
                        DrawText(page, f2, label,
                                to >= baseValue ? x + pad / 2f : x - pad / 2f - f2.StringWidth(label), baseline, INK);
                    }
                } else {
                    float y0 = y8 - ChartScale.Scaled(from - vMin, y8 - y5, vMax - vMin);
                    float y = y8 - ChartScale.Scaled(to - vMin, y8 - y5, vMax - vMin);
                    page.FillRect(barStart, Math.Min(y0, y), barWidth, Math.Abs(y - y0));
                    float x = barStart + (barWidth - (label != null ? f2.StringWidth(label) : 0f)) / 2f;
                    if (label != null && stacked) {
                        if (Math.Abs(y - y0) >= bodyHeight) {
                            DrawText(page, f2, label, x, (y + y0) / 2f + ascent / 2f, on);
                        }
                    } else if (label != null && valueLabelsInside && Math.Abs(y - y0) >= bodyHeight + pad) {
                        DrawText(page, f2, label, x, to >= baseValue ? y + ascent + pad / 2f : y - pad / 2f, on);
                    } else if (label != null) {
                        DrawText(page, f2, label, x, to >= baseValue ? y - pad / 2f : y + ascent + pad / 2f, INK);
                    }
                }
            }
            // The total of a stack above it, or past its end
            if (totals && up > 0f && ChartScale.IsValue(up)) {     // A sum past the largest float is not written
                String total = ValueLabel(up);
                float groupMiddle = groupStart + groupWidth / 2f;
                if (horizontal) {
                    float x = x5 + ChartScale.Scaled(Math.Min(up, vMax) - vMin, x6 - x5, vMax - vMin);
                    DrawText(page, f2, total, x + pad / 2f, groupMiddle + ascent / 2f, INK);
                } else {
                    float y = y8 - ChartScale.Scaled(Math.Min(up, vMax) - vMin, y8 - y5, vMax - vMin);
                    DrawText(page, f2, total, groupMiddle - f2.StringWidth(total) / 2f, y - pad / 2f, INK);
                }
            }
        }

        // The axis line at the base of the bars, which is the zero line of
        // negative values
        page.SetPenColor(BASELINE);
        page.SetPenWidth(0.75f);
        if (horizontal) {
            float x0 = x5 + ChartScale.Scaled(baseValue - vMin, x6 - x5, vMax - vMin);
            page.DrawLine(x0, y5, x0, y8);
        } else {
            float y0 = y8 - ChartScale.Scaled(baseValue - vMin, y8 - y5, vMax - vMin);
            page.DrawLine(x5, y0, x6, y0);
        }

        // The lines over the bars, through the middles of the categories, a
        // dot at each value, broken where a value is missing; each on a white
        // line a little wider, so that it stays clear where it crosses a bar
        foreach (Series s in lineSeries) {
            List<List<float[]>> runs = new List<List<float[]>>();
            List<float[]> run = null;
            for (int i = 0; i < n; i++) {
                if (i >= s.values.Length || !ChartScale.IsValue(s.values[i])) {
                    run = null;
                    continue;
                }
                float v = Math.Min(Math.Max(s.values[i], vMin), vMax);
                float middle = (horizontal ? y5 : x5) + i * slot + slot / 2f;
                float at = horizontal
                        ? x5 + ChartScale.Scaled(v - vMin, x6 - x5, vMax - vMin)
                        : y8 - ChartScale.Scaled(v - vMin, y8 - y5, vMax - vMin);
                if (run == null) {
                    run = new List<float[]>();
                    runs.Add(run);
                }
                run.Add(horizontal ? new float[] {at, middle} : new float[] {middle, at});
            }
            page.SetLineJoinStyle(JoinStyle.ROUND);
            float dot = 0.2f * bodyHeight;
            for (int pass = 0; pass < 2; pass++) {
                int color = (pass == 0) ? Color.white : s.color;
                page.SetPenColor(color);
                page.SetPenWidth((pass == 0) ? 3.5f : 2f);
                foreach (List<float[]> points in runs) {
                    page.MoveTo(points[0][0], points[0][1]);
                    for (int k = 1; k < points.Count; k++) {
                        page.LineTo(points[k][0], points[k][1]);
                    }
                    page.StrokePath();
                }
                page.SetBrushColor(color);
                foreach (List<float[]> points in runs) {
                    foreach (float[] point in points) {
                        page.FillCircle(point[0], point[1], (pass == 0) ? dot + 0.75f : dot);
                    }
                }
            }
        }

        // Axis titles
        page.SetTextRotation(-90);
        String yTitle = ChartScale.Shorten(f2, f2.GetSize(), yAxisTitle, y8 - y5);
        DrawText(page, f2, yTitle, x1 + bodyHeight, y8 - ((y8 - y5) - f2.StringWidth(yTitle)) / 2f, MUTED);
        page.SetTextRotation(0);
        String xTitle = ChartScale.Shorten(f2, f2.GetSize(), xAxisTitle, x6 - x5);
        DrawText(page, f2, xTitle, x5 + ((x6 - x5) - f2.StringWidth(xTitle)) / 2f, y2 - bodyHeight / 2f, MUTED);

        page.RestoreGraphicsState();
        page.SetFigureBoundingBox(x1, y1, w, h);
        page.AddEMC();

        return new float[] {x1 + w, y1 + h};
    }

    // The alternate description: the one set, or the title, the subtitle and,
    // for each series, its name and its values by category.
    private String AltDescription() {
        if (!String.IsNullOrEmpty(altDescription)) {
            return altDescription;
        }
        StringBuilder sb = new StringBuilder(ChartScale.Join(title, subtitle));
        if (sb.Length == 0) {
            sb.Append("Bar chart");
        }
        String separator = ": ";
        foreach (Series s in AllSeries()) {
            StringBuilder values = new StringBuilder();
            for (int i = 0; i < Math.Min(s.values.Length, NumberOfCategories()); i++) {
                if (!ChartScale.IsValue(s.values[i])) {
                    continue;
                }
                if (values.Length > 0) {
                    values.Append(", ");
                }
                String category = i < categories.Count ? categories[i] : "";
                if (category.Length > 0) {
                    values.Append(category).Append(' ');
                }
                values.Append(ValueLabel(s.values[i]));
            }
            if (values.Length == 0) {
                continue;
            }
            sb.Append(separator);
            if (lineSeries.Contains(s)) {
                sb.Append(s.name.Length == 0 ? "line: " : s.name + ", a line: ");
            } else if (s.name.Length > 0) {
                sb.Append(s.name).Append(": ");
            }
            sb.Append(values);
            separator = "; ";
        }
        return sb.Append('.').ToString();
    }

    /// <summary>Returns the number of category slots: the categories or the longest series.</summary>
    private int NumberOfCategories() {
        int n = categories.Count;
        foreach (Series s in series) {
            n = Math.Max(n, s.values.Length);
        }
        // A line series is drawn over the categories of the bars, and adds
        // none of its own when there are bars: a longer target line added
        // empty slots
        if (n == 0) {
            foreach (Series s in lineSeries) {
                n = Math.Max(n, s.values.Length);
            }
        }
        return n;
    }

    /// <summary>Returns the series of the bars, then those of the lines.</summary>
    private List<Series> AllSeries() {
        List<Series> all = new List<Series>(series);
        all.AddRange(lineSeries);
        return all;
    }

    /// <summary>Returns true if a series has a name to list in the legend.</summary>
    private bool HasSeriesNames() {
        foreach (Series s in AllSeries()) {
            if (s.name.Length > 0) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns the color of the bar at the index: the series' own, its color for the bar, or the palette's.</summary>
    private int BarColor(Series s, int seriesIndex, int index) {
        if (s.colors != null && index >= 0 && index < s.colors.Length) {
            return s.colors[index];
        }
        return s.color == NO_COLOR ? PALETTE[seriesIndex % PALETTE.Length] : s.color;
    }

    /// <summary>Formats the value written at the end of a bar.</summary>
    private String ValueLabel(float value) {
        return Group(ChartScale.Format(value, minFractionDigits, maxFractionDigits));
    }

    /// <summary>Formats a label of the value axis.</summary>
    private String AxisLabel(float value, int digits) {
        return Group(ChartScale.Format(value, digits, maxFractionDigits));
    }

    /// <summary>Groups the digits before the decimal point in thousands with commas, if grouping is used.</summary>
    private String Group(String label) {
        return groupingUsed ? ChartScale.GroupDigits(label) : label;
    }

    /// <summary>Draws the text in the font at its size, in the color.</summary>
    private static void DrawText(Page page, Font font, String text, float x, float y, int color) {
        page.DrawString(font, font.GetSize(), text, x, y, Util.ToRGB(color), null);
    }

    /// <summary>
    /// Returns the color of a label on a bar of the color: white or ink,
    /// whichever contrasts more with it, by the WCAG contrast ratio.
    /// </summary>
    internal static int LabelColorOn(int color) {
        // White contrasts more when (1 + 0.05) / (L + 0.05) is the larger of
        // the two ratios, (L + 0.05) / (ink + 0.05) the other
        double l = Luminance(color) + 0.05;
        return l * l <= 1.05 * (Luminance(INK) + 0.05) ? Color.white : INK;
    }

    /// <summary>Returns the relative luminance of the color, as WCAG defines it.</summary>
    private static double Luminance(int color) {
        return 0.2126 * Linear((color >> 16) & 0xFF)
                + 0.7152 * Linear((color >> 8) & 0xFF)
                + 0.0722 * Linear(color & 0xFF);
    }

    /// <summary>Returns the sRGB component, 0 to 255, as a linear value from 0 to 1.</summary>
    private static double Linear(int component) {
        double c = component / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    /// <summary>Draws one grid line, thin and solid.</summary>
    private void GridLine(Page page, float xa, float ya, float xb, float yb) {
        page.SetPenColor(gridLineColor);
        page.SetPenWidth(0.5f);
        page.DrawLine(xa, ya, xb, yb);
    }

    /// <summary>
    /// Draws the legend at the left under the title, with a swatch before each
    /// series name, from the baseline of its first row, its rows wrapped at
    /// the right edge of the chart, when there is a page; and returns the
    /// number of rows. A series with a color per bar has the color of its
    /// first bar.
    /// </summary>
    private int DrawLegend(Page page, float baseline) {
        float swatch = f2.GetAscent();
        float gap = swatch / 2f;
        float x = x1;
        int rows = 1;
        List<Series> all = AllSeries();
        for (int j = 0; j < all.Count; j++) {
            Series s = all[j];
            if (s.name.Length == 0) {
                continue;
            }
            // A name too long for a row is shortened
            String name = ChartScale.Shorten(f2, f2.GetSize(), s.name, w - swatch - gap);
            if (x > x1 && x + swatch + gap + f2.StringWidth(name) > x1 + w) {
                x = x1;
                baseline += 1.5f * f2.GetBodyHeight();
                rows++;
            }
            if (page != null && j >= series.Count) {
                // A line: its color across the swatch, a dot in the middle
                page.SetPenColor(s.color);
                page.SetPenWidth(1.5f);
                page.DrawLine(x, baseline - swatch / 2f, x + swatch, baseline - swatch / 2f);
                page.SetBrushColor(s.color);
                page.FillCircle(x + swatch / 2f, baseline - swatch / 2f, swatch / 4f);
            } else if (page != null) {
                page.SetBrushColor((s.colors != null && s.colors.Length > 0) ? s.colors[0] : BarColor(s, j, -1));
                page.FillRect(x, baseline - swatch, swatch, swatch);
            }
            x += swatch + gap;
            if (page != null) {
                DrawText(page, f2, name, x, baseline, INK);
            }
            x += f2.StringWidth(name) + f2.GetBodyHeight();
        }
        return rows;
    }
}   // End of BarChart.cs
}   // End of namespace PDFjet.NET
