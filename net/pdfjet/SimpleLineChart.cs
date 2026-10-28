/*
 * SimpleLineChart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// A chart of a few lines over time, or over any categories in their order:
/// the categories left to right, each line labeled at its right end with its
/// name and its last value, so that it needs no legend; one line highlighted
/// in navy, its label bold, the others gray. A light horizontal grid alone, no
/// border, on a light panel, with a title and a subtitle at the top left, in
/// the look of SimpleBarChart and SimpleScatterChart. A value that is NaN
/// leaves a gap in its line.
///
/// In a PDF/UA document the chart is one figure, described from its data: the
/// lines, their first and last values and their highest, unless an alternate
/// description is set. It draws with the page alone, and needs no other chart.
/// Please see Example_25.
/// </summary>
public class SimpleLineChart : IDrawable {
    // The color of the lines that are not highlighted, unless set
    internal const int LINE = 0x8C96A3;

    // A line of the chart: its name and a value for each category, NaN where
    // it has none.
    private sealed class Line {
        internal readonly String name;
        internal readonly float[] values;

        internal Line(String name, float[] values) {
            this.name = name;
            this.values = values;
        }

        // Whether the line has a value at the category.
        internal bool Has(int i) {
            return i < values.Length && ChartScale.IsValue(values[i]);
        }
    }

    // A label at the end of a line: its text, the index of its line and the
    // baseline it is drawn at.
    private sealed class EndLabel {
        internal readonly String text;
        internal readonly int index;
        internal float y;

        internal EndLabel(String text, int index, float y) {
            this.text = text;
            this.index = index;
            this.y = y;
        }
    }

    private readonly Font f1;      // The title and the highlighted label
    private readonly Font f2;      // The rest
    private float x;
    private float y;
    private float w = 400f;
    private float h = 300f;
    private String title = "";
    private String subtitle = "";
    private String xAxisTitle = "";
    private String yAxisTitle = "";
    private String altDescription = "";
    private String[] categories = new String[0];
    private readonly List<Line> lines = new List<Line>();
    private int highlight = -1;     // The index of the highlighted line, or -1
    private bool markers = false;
    private bool valueLabels = false;
    private bool areaFilled = false;
    private float yMin;
    private float yMax;
    private bool yRangeSet = false;
    private int panelColor = SimpleBarChart.PANEL;
    private int lineColor = LINE;
    private int highlightColor = SimpleBarChart.HIGHLIGHT;
    private int maxFractionDigits = 2;      // As many as the step of the axis needs

    /// <summary>
    /// Creates a simple line chart, 400 by 300 points.
    /// </summary>
    /// <param name="f1">the font of the title and of the highlighted line's label, a bold one.</param>
    /// <param name="f2">the font of the subtitle, the axis titles, the numbers, the categories and the other labels.</param>
    public SimpleLineChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /// <summary>
    /// Sets the title, drawn at the top left of the chart.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetTitle(String title) {
        this.title = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the subtitle, drawn small and gray under the title.
    /// </summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetSubtitle(String subtitle) {
        this.subtitle = subtitle ?? "";
        return this;
    }

    /// <summary>
    /// Sets the title of the categories, drawn under them.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetXAxisTitle(String title) {
        this.xAxisTitle = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the title of the vertical axis, drawn up its left side.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetYAxisTitle(String title) {
        this.yAxisTitle = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the alternate description of the chart in a PDF/UA document.
    /// Without one the chart is described from its data.
    /// </summary>
    /// <param name="altDescription">the alternate description.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetAltDescription(String altDescription) {
        this.altDescription = altDescription ?? "";
        return this;
    }

    /// <summary>
    /// Sets the categories, the years or the months, in their order left to
    /// right. The chart keeps a copy of them, a null category an empty one,
    /// which crashed DrawOn (the review of 28 October 2026).
    /// </summary>
    /// <param name="categories">the categories.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetCategories(params String[] categories) {
        if (categories == null) {
            this.categories = new String[0];
            return this;
        }
        this.categories = new String[categories.Length];
        for (int i = 0; i < categories.Length; i++) {
            this.categories[i] = categories[i] ?? "";
        }
        return this;
    }

    /// <summary>
    /// Adds a line: its name, and a value for each category, NaN where it has
    /// none. The chart keeps a copy of the values: a caller that reused one
    /// array for every line drew every line with the last values (the review
    /// of 28 October 2026).
    /// </summary>
    /// <param name="name">the name of the line.</param>
    /// <param name="values">its values.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart AddLine(String name, float[] values) {
        lines.Add(new Line(name ?? "", values == null ? new float[0] : (float[]) values.Clone()));
        return this;
    }

    /// <summary>
    /// Highlights the line of the index, the first line 0: navy and a little
    /// wider, its label bold.
    /// </summary>
    /// <param name="index">the index of the line.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetHighlight(int index) {
        this.highlight = index;
        return this;
    }

    /// <summary>
    /// Draws a dot at each value of the lines.
    /// </summary>
    /// <param name="markers">true to draw them.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetMarkers(bool markers) {
        this.markers = markers;
        return this;
    }

    /// <summary>
    /// Writes each value of the lines by its point, small, where the line
    /// leaves it room: above a peak, under a valley, before or after a point
    /// on a rise or a fall; but the last of each line, which its label at the
    /// end of the line has, and a value outside a range set with
    /// SetYAxisRange, whose point is not drawn.
    /// </summary>
    /// <param name="valueLabels">true to write them.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetValueLabels(bool valueLabels) {
        this.valueLabels = valueLabels;
        return this;
    }

    /// <summary>
    /// Fills the area between each line and 0, or the edge of the plot nearest
    /// 0 when 0 is outside it, in a light tint of the line's color, which makes
    /// an area chart. The highlighted line's area is drawn last, over the
    /// others.
    /// </summary>
    /// <param name="areaFilled">true to fill the areas.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetAreaFilled(bool areaFilled) {
        this.areaFilled = areaFilled;
        return this;
    }

    /// <summary>
    /// Sets the range of the vertical axis. Without it the range is that of
    /// the values, rounded to steps. A range whose end is not past its start
    /// is not set.
    /// </summary>
    /// <param name="min">the first value of the axis.</param>
    /// <param name="max">the last value of the axis.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetYAxisRange(float min, float max) {
        this.yMin = min;
        this.yMax = max;
        this.yRangeSet = max > min;
        return this;
    }

    /// <summary>
    /// Sets the color of the panel behind the chart. It is light gray,
    /// 0xF1F3F6, unless set. Color.transparent, -1, draws no panel: the one
    /// color setter where it does not leave the color as it was.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetPanelColor(int color) {
        this.panelColor = color;
        return this;
    }

    /// <summary>
    /// Sets the color of the lines that are not highlighted. It is gray,
    /// 0x8C96A3, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetLineColor(int color) {
        this.lineColor = color;
        return this;
    }

    /// <summary>
    /// Sets the color of the highlighted line and of its label. It is navy,
    /// 0x1F3A5F, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetHighlightColor(int color) {
        this.highlightColor = color;
        return this;
    }

    /// <summary>
    /// Sets the top left corner of the chart.
    /// </summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the width and the height of the chart.
    /// </summary>
    /// <param name="w">the width.</param>
    /// <param name="h">the height.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    // The space around the chart's content, a size of its numbers.
    private float Pad() {
        return f2.GetSize();
    }

    // The size of the subtitle.
    private float SubtitleSize() {
        return 0.8f * f2.GetSize();
    }

    // The height above the plot: the padding, the title and the subtitle.
    private float Top() {
        float top = Pad();
        if (title.Length > 0) {
            top += f1.GetAscent(f1.GetSize()) + f1.GetDescent(f1.GetSize());
        }
        if (subtitle.Length > 0) {
            top += 1.4f * SubtitleSize();
        }
        if (title.Length > 0 || subtitle.Length > 0) {
            top += f2.GetSize();
        }
        return top;
    }

    // The number of categories the chart draws: the categories set, or the
    // longest line when there are none.
    private int Count() {
        int n = categories.Length;
        if (n == 0) {
            foreach (Line line in lines) {
                n = Math.Max(n, line.values.Length);
            }
        }
        return n;
    }

    // A number of the axis, with the decimals its step needs, its digits
    // grouped.
    private String Label(float value, float step) {
        return ChartScale.GroupDigits(
                ChartScale.Format(value, ChartScale.FractionDigitsOf(step, maxFractionDigits), maxFractionDigits));
    }

    // A value of a line, as its label writes it.
    private String Value(float v) {
        return ChartScale.GroupDigits(ChartScale.Format(v, 0, maxFractionDigits));
    }

    // The name of the category of the index.
    private String Category(int i) {
        return i < categories.Length ? categories[i] : "";
    }

    // The font of the label of the line of the index.
    private Font FontOf(int index) {
        return index == highlight ? f1 : f2;
    }

    // The label at the end of the line of the index: its name, shortened to a
    // quarter of the chart, and its last value.
    private String EndText(int i, int last) {
        Line line = lines[i];
        return ChartScale.Shorten(FontOf(i), f2.GetSize(), line.name, 0.25f * w) + " " + Value(line.values[last]);
    }

    // The category of the last value of the line, or -1 when it has none.
    private int LastOf(Line line) {
        for (int i = Count() - 1; i >= 0; i--) {
            if (line.Has(i)) {
                return i;
            }
        }
        return -1;
    }

    // The alternate description: the one set, or the title, the categories
    // and, for each line, its first and last values and its highest.
    internal String AltDescription() {
        if (altDescription.Length > 0) {
            return altDescription;
        }
        // The values in the digits of the numbers of the axis
        List<float> all = new List<float>();
        foreach (Line line in lines) {
            for (int j = 0; j < Count(); j++) {
                if (line.Has(j)) {
                    all.Add(line.values[j]);
                }
            }
        }
        if (all.Count > 0) {
            ChartScale.Axis axis = ChartScale.AxisOf(all.ToArray(), yMin, yMax, yRangeSet);
            maxFractionDigits = ChartScale.DigitsOf((axis.max - axis.min) / axis.lines);
        }
        StringBuilder sb = new StringBuilder(ChartScale.Join(title, subtitle));
        if (sb.Length == 0) {
            sb.Append("Line chart");
        }
        int n = Count();
        if (n > 0 && Category(0).Length > 0) {
            // To the last category that has a name: one category was "2019 to
            // 2019", and an empty last one "2019 to :"
            int end = n - 1;
            while (end > 0 && Category(end).Length == 0) {
                end--;
            }
            sb.Append(SimpleBarChart.PunctuationAfter(sb, ",")).Append(' ').Append(Category(0));
            if (end > 0) {
                sb.Append(" to ").Append(Category(end));
            }
        }
        sb.Append(SimpleBarChart.PunctuationAfter(sb, ":"));
        for (int i = 0; i < lines.Count; i++) {
            Line line = lines[i];
            int first = -1;
            int last = -1;
            int high = -1;
            for (int j = 0; j < n; j++) {
                if (!line.Has(j)) {
                    continue;
                }
                if (first < 0) {
                    first = j;
                }
                last = j;
                if (high < 0 || line.values[j] > line.values[high]) {
                    high = j;
                }
            }
            if (i > 0) {
                sb.Append(';');
            }
            sb.Append(' ').Append(line.name);
            if (first < 0) {
                sb.Append(" has no values");
                continue;
            }
            if (first == last) {    // One value, which was "from 5 to 5"
                sb.Append(' ').Append(Value(line.values[first]));
                continue;
            }
            sb.Append(" from ").Append(Value(line.values[first])).Append(" to ").Append(Value(line.values[last]));
            if (high != first && high != last) {
                sb.Append(", highest ").Append(Value(line.values[high]));
                if (Category(high).Length > 0) {
                    sb.Append(" in ").Append(Category(high));
                }
            }
        }
        return sb.Append(SimpleBarChart.PunctuationAfter(sb, ".")).ToString();
    }

    /// <summary>
    /// Draws the chart on the page. A chart too small for its plot, its
    /// numbers and its labels draws nothing.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the chart.</returns>
    public float[] DrawOn(Page page) {
        int n = Count();
        List<float> all = new List<float>();
        foreach (Line line in lines) {
            for (int j = 0; j < n; j++) {
                if (line.Has(j)) {
                    all.Add(line.values[j]);
                }
            }
        }
        if (page == null || all.Count == 0 || f1 == null || f2 == null) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.GetSize();
        float pad = Pad();
        float ascent = f2.GetAscent(size);

        if (!yRangeSet && !ChartScale.SpanFits(all.ToArray())) {
            return new float[] {x + w, y + h};      // A span past the largest float, which no axis can draw
        }
        ChartScale.Axis axis = ChartScale.AxisOf(all.ToArray(), yMin, yMax, yRangeSet);
        float step = (axis.max - axis.min) / axis.lines;
        maxFractionDigits = ChartScale.DigitsOf(step);

        // The plot: right of the y title and the widest number, left of the
        // widest label at the end of a line, above the categories and the x
        // title
        float widest = 0f;
        for (int i = 0; i <= axis.lines; i++) {
            widest = Math.Max(widest, f2.StringWidth(size, Label(axis.min + step * i, step)));
        }
        float endWidth = 0f;
        for (int i = 0; i < lines.Count; i++) {
            Line line = lines[i];
            int last = LastOf(line);
            if (last >= 0) {
                endWidth = Math.Max(endWidth, FontOf(i).StringWidth(size, EndText(i, last)));
            }
        }
        float left = x + pad + widest + size / 2f;
        if (yAxisTitle.Length > 0) {
            left += 1.8f * size;    // The title, as far from the numbers as the x title is
        }
        float right = x + w - pad - endWidth - size / 2f;
        // Room for the halves of the first and the last category, centered
        // under the ends of the plot: the first, "January 2026", went past
        // the chart's left edge
        if (n > 1) {
            left = Math.Max(left, x + pad / 2f + f2.StringWidth(size, Category(0)) / 2f);
            right = Math.Min(right, x + w - pad / 2f - f2.StringWidth(size, Category(n - 1)) / 2f);
        }
        float top = y + Top() + ascent / 2f;     // Room for the top number
        float bottom = y + h - pad - 1.4f * size;
        if (xAxisTitle.Length > 0) {
            bottom -= 1.4f * size;
        }
        if (right <= left || bottom <= top || !ChartScale.Drawable(axis)) {
            return new float[] {x + w, y + h};
        }

        page.AddBDC(StructElem.FIGURE, null, AltDescription());
        page.SaveGraphicsState();
        page.SetDefaultStrokeDashPattern();     // Not the caller's, which dashed the lines

        if (panelColor >= 0) {
            page.SetBrushColor(panelColor);
            page.FillRect(x, y, w, h);
        }
        float baseline = y + pad;
        if (title.Length > 0) {
            baseline += f1.GetAscent(f1.GetSize());
            page.DrawString(f1, f1.GetSize(), ChartScale.Shorten(f1, f1.GetSize(), title, w - 2f * pad), x + pad, baseline, Util.ToRGB(SimpleBarChart.INK), null);
            baseline += f1.GetDescent(f1.GetSize());
        }
        if (subtitle.Length > 0) {
            baseline += 1.4f * SubtitleSize();
            page.DrawString(f2, SubtitleSize(), ChartScale.Shorten(f2, SubtitleSize(), subtitle, w - 2f * pad),
                    x + pad, baseline, Util.ToRGB(SimpleBarChart.MUTED), null);
        }

        // The horizontal grid and its numbers, and the categories under the
        // plot, each where it leaves the one before it room
        page.SetPenColor(SimpleScatterChart.GRID);
        page.SetPenWidth(0.5f);
        int every = ChartScale.AxisLabelStep((bottom - top) / axis.lines, f2.GetBodyHeight(), axis.lines);
        for (int i = 0; i <= axis.lines; i++) {
            float v = axis.min + step * i;
            float gy = YAt(v, axis, top, bottom);
            page.DrawLine(left, gy, right, gy);
            if (i % every == 0) {
                String label = Label(v, step);
                page.DrawString(f2, size, label, left - size / 2f - f2.StringWidth(size, label),
                        gy + ascent / 2f, Util.ToRGB(SimpleBarChart.MUTED), null);
            }
        }
        float widestCategory = 0f;
        for (int i = 0; i < n; i++) {
            widestCategory = Math.Max(widestCategory, f2.StringWidth(size, Category(i)));
        }
        every = 1;
        if (n > 1) {
            every = ChartScale.AxisLabelStep((right - left) / (n - 1), widestCategory + pad, n - 1);
        }
        // Each centered on its category, and inside the chart
        float[] centers = new float[n];
        float[] widths = new float[n];
        for (int i = 0; i < n; i++) {
            centers[i] = XAt(i, n, left, right);
            widths[i] = f2.StringWidth(size, Category(i));
        }
        every = ChartScale.PlacedStep(every, centers, widths, x + pad / 2f, x + w - pad / 2f, pad / 2f);
        for (int i = 0; i < n; i += every) {
            page.DrawString(f2, size, Category(i), ChartScale.Placed(centers[i], widths[i], x + pad / 2f, x + w - pad / 2f),
                    bottom + 1.4f * size, Util.ToRGB(SimpleBarChart.MUTED), null);
        }
        // The axis titles, shortened to the width and the height of the plot:
        // a long one ran past the chart (the review of 28 October 2026)
        if (xAxisTitle.Length > 0) {
            String xTitle = ChartScale.Shorten(f2, size, xAxisTitle, right - left);
            page.DrawString(f2, size, xTitle, left + (right - left - f2.StringWidth(size, xTitle)) / 2f,
                    bottom + 2.8f * size, Util.ToRGB(SimpleBarChart.INK), null);
        }
        if (yAxisTitle.Length > 0) {
            String yTitle = ChartScale.Shorten(f2, size, yAxisTitle, bottom - top);
            page.SetTextRotation(-90);
            page.DrawString(f2, size, yTitle, x + pad + ascent,
                    bottom - (bottom - top - f2.StringWidth(size, yTitle)) / 2f,
                    Util.ToRGB(SimpleBarChart.INK), null);
            page.SetTextRotation(0);
        }

        // The lines, the highlighted one last, over the others, broken where a
        // value is missing
        List<int> order = new List<int>();
        for (int i = 0; i < lines.Count; i++) {
            if (i != highlight) {
                order.Add(i);
            }
        }
        if (highlight >= 0 && highlight < lines.Count) {
            order.Add(highlight);
        }
        // A value outside a range that is set is drawn past the plot, and the
        // lines are clipped at its edges, and as far past them as a dot is wide
        bool outside = false;
        foreach (float v in all) {
            outside |= !ChartScale.OnAxis(v, axis);
        }
        if (outside) {
            float edge = 0.25f * size + 1f;
            page.SaveGraphicsState();
            page.ClipRect(left - edge, top - edge, right - left + 2f * edge, bottom - top + 2f * edge);
        }
        // The areas of the lines, each run of values to 0, or to the edge of
        // the plot nearest 0 when 0 is outside it, in a tint of the line's color
        if (areaFilled) {
            float zero = YAt(Math.Min(Math.Max(0f, axis.min), axis.max), axis, top, bottom);
            foreach (int i in order) {
                Line line = lines[i];
                page.SetBrushColor(Tint((i == highlight) ? highlightColor : lineColor));
                int start = -1;
                for (int j = 0; j <= n; j++) {
                    if (j < n && line.Has(j)) {
                        if (start < 0) {
                            start = j;
                        }
                        continue;
                    }
                    if (start >= 0 && j - 1 > start) {
                        page.MoveTo(XAt(start, n, left, right), zero);
                        for (int k = start; k < j; k++) {
                            page.LineTo(XAt(k, n, left, right), YAt(line.values[k], axis, top, bottom));
                        }
                        page.LineTo(XAt(j - 1, n, left, right), zero);
                        page.FillPath();
                    }
                    start = -1;
                }
            }
        }
        foreach (int i in order) {
            Line line = lines[i];
            int color = (i == highlight) ? highlightColor : lineColor;
            page.SetPenColor(color);
            page.SetPenWidth((i == highlight) ? 2f : 1.5f);
            page.SetLineJoinStyle(JoinStyle.ROUND);     // No spike at a sharp peak
            bool drawing = false;
            for (int j = 0; j < n; j++) {
                if (!line.Has(j)) {
                    if (drawing) {
                        page.StrokePath();
                    }
                    drawing = false;
                    continue;
                }
                if (drawing) {
                    page.LineTo(XAt(j, n, left, right), YAt(line.values[j], axis, top, bottom));
                } else {
                    page.MoveTo(XAt(j, n, left, right), YAt(line.values[j], axis, top, bottom));
                    drawing = true;
                }
            }
            if (drawing) {
                page.StrokePath();
            }
            // A dot at each value, or at a value alone between gaps, which a
            // line of no length did not show
            page.SetBrushColor(color);
            for (int j = 0; j < n; j++) {
                bool alone = !(j > 0 && line.Has(j - 1)) && !(j + 1 < n && line.Has(j + 1));
                if (line.Has(j) && (markers || alone)) {
                    page.FillCircle(XAt(j, n, left, right), YAt(line.values[j], axis, top, bottom), 0.25f * size);
                }
            }
        }
        if (outside) {
            page.RestoreGraphicsState();
        }

        // The values by their points, where the line leaves them room: above
        // a peak, under a valley, above and before a point on a rise, above
        // and after one on a fall; but those outside the axis and the last of
        // each line, which its label at the end has
        if (valueLabels) {
            float small = SubtitleSize();
            float gap = 0.5f * size;
            foreach (int i in order) {
                Line line = lines[i];
                int last = LastOf(line);
                int color = (i == highlight) ? highlightColor : SimpleBarChart.MUTED;
                for (int j = 0; j < last; j++) {
                    if (!line.Has(j) || !ChartScale.OnAxis(line.values[j], axis)) {
                        continue;
                    }
                    float v = line.values[j];
                    bool opens = !(j > 0 && line.Has(j - 1));     // The first of its run
                    float before = opens ? v : line.values[j - 1];
                    float after = line.Has(j + 1) ? line.values[j + 1] : v;
                    String text = Value(v);
                    float width = f2.StringWidth(small, text);
                    float px = XAt(j, n, left, right);
                    float py = YAt(v, axis, top, bottom);
                    float lx = px - width / 2f;
                    float at = py - gap;
                    if (opens && after > v) {                   // The start of a rise
                        lx = px + gap;
                        at = Math.Min(py + gap + f2.GetAscent(small), bottom - f2.GetDescent(small));
                    } else if (opens && after < v) {            // The start of a fall
                        lx = px + gap;
                    } else if (before > v && after > v) {       // A valley
                        at = py + gap + f2.GetAscent(small);
                        if (at > bottom) {
                            at = py - gap;
                        }
                    } else if (before < v && after > v) {       // A rise
                        lx = px - gap - width;
                    } else if (before > v && after < v) {       // A fall
                        lx = px + gap;
                    }
                    if (at - f2.GetAscent(small) < top - ascent / 2f) {
                        at = py + gap + f2.GetAscent(small);    // Under a point at the top
                    }
                    page.DrawString(f2, small, text, lx, at, Util.ToRGB(color), null);
                }
            }
        }

        // The labels at the ends of the lines, moved apart where they would
        // meet
        List<EndLabel> labels = new List<EndLabel>();
        for (int i = 0; i < lines.Count; i++) {
            Line line = lines[i];
            int last = LastOf(line);
            if (last >= 0) {
                // At the end of its line, or at the edge of the plot it is past
                float end = Math.Max(top, Math.Min(bottom, YAt(line.values[last], axis, top, bottom)));
                labels.Add(new EndLabel(EndText(i, last), i, end + ascent / 2f));
            }
        }
        // As many as the height of the plot holds, a line apart, the
        // highlighted one first and then the others in their order: more were
        // pushed up past the top of the chart
        int room = (f2.GetBodyHeight() > 0f) ? (int) ((bottom - top) / f2.GetBodyHeight()) + 1 : labels.Count;
        if (labels.Count > room) {
            List<EndLabel> kept = new List<EndLabel>();
            foreach (EndLabel label in labels) {
                if (label.index == highlight) {
                    kept.Add(label);
                }
            }
            foreach (EndLabel label in labels) {
                if (label.index != highlight && kept.Count < room) {
                    kept.Add(label);
                }
            }
            labels = kept;
        }
        // By the baseline, and labels on one baseline in the order they are
        // in, as a stable sort leaves them: the highlighted one first
        List<EndLabel> inOrder = new List<EndLabel>(labels);
        labels.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : inOrder.IndexOf(a).CompareTo(inOrder.IndexOf(b)));
        for (int k = 1; k < labels.Count; k++) {
            labels[k].y = Math.Max(labels[k].y, labels[k - 1].y + f2.GetBodyHeight());
        }
        // and up from the bottom of the plot, when the last is pushed past it
        if (labels.Count > 0) {
            int last = labels.Count - 1;
            labels[last].y = Math.Min(labels[last].y, bottom + ascent / 2f);
            for (int k = last - 1; k >= 0; k--) {
                labels[k].y = Math.Min(labels[k].y, labels[k + 1].y - f2.GetBodyHeight());
            }
        }
        foreach (EndLabel label in labels) {
            int color = (label.index == highlight) ? highlightColor : SimpleBarChart.MUTED;
            page.DrawString(FontOf(label.index), size, label.text, right + size / 2f, label.y, Util.ToRGB(color), null);
        }

        page.RestoreGraphicsState();
        page.SetFigureBoundingBox(x, y, w, h);
        page.AddEMC();
        return new float[] {x + w, y + h};
    }

    // A tint of the color, a quarter of it over three quarters of white.
    private static int Tint(int color) {
        int r = (color >> 16) & 0xFF;
        int g = (color >> 8) & 0xFF;
        int b = color & 0xFF;
        return (((r + 3 * 255) / 4) << 16) | (((g + 3 * 255) / 4) << 8) | ((b + 3 * 255) / 4);
    }

    // The x on the page of the category of the index.
    private static float XAt(int i, int n, float left, float right) {
        if (n == 1) {
            return (left + right) / 2f;
        }
        return left + i * (right - left) / (n - 1);
    }

    // The y on the page of a value, held near the plot.
    private static float YAt(float v, ChartScale.Axis axis, float top, float bottom) {
        v = ChartScale.Held(v, axis);
        return bottom - ChartScale.Scaled(v - axis.min, bottom - top, axis.max - axis.min);
    }
}
}   // End of SimpleLineChart.cs
