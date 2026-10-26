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
    private float yMin;
    private float yMax;
    private bool yRangeSet = false;
    private int panelColor = SimpleBarChart.PANEL;
    private int lineColor = LINE;
    private int highlightColor = SimpleBarChart.HIGHLIGHT;
    private readonly int maxFractionDigits = 2;

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
        this.title = title;
        return this;
    }

    /// <summary>
    /// Sets the subtitle, drawn small and gray under the title.
    /// </summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetSubtitle(String subtitle) {
        this.subtitle = subtitle;
        return this;
    }

    /// <summary>
    /// Sets the title of the categories, drawn under them.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetXAxisTitle(String title) {
        this.xAxisTitle = title;
        return this;
    }

    /// <summary>
    /// Sets the title of the vertical axis, drawn up its left side.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetYAxisTitle(String title) {
        this.yAxisTitle = title;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of the chart in a PDF/UA document.
    /// Without one the chart is described from its data.
    /// </summary>
    /// <param name="altDescription">the alternate description.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the categories, the years or the months, in their order left to
    /// right.
    /// </summary>
    /// <param name="categories">the categories.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart SetCategories(params String[] categories) {
        this.categories = categories;
        return this;
    }

    /// <summary>
    /// Adds a line: its name, and a value for each category, NaN where it has
    /// none.
    /// </summary>
    /// <param name="name">the name of the line.</param>
    /// <param name="values">its values.</param>
    /// <returns>this chart.</returns>
    public SimpleLineChart AddLine(String name, float[] values) {
        lines.Add(new Line(name, values));
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
    /// Sets the range of the vertical axis. Without it the range is that of
    /// the values, rounded to steps.
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
    /// Sets the color of the panel behind the chart, or -1 for none. It is
    /// light gray, 0xF1F3F6, unless set.
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
        StringBuilder sb = new StringBuilder(title);
        if (subtitle.Length > 0) {
            if (sb.Length > 0) {
                sb.Append(", ");
            }
            sb.Append(subtitle);
        }
        if (sb.Length == 0) {
            sb.Append("Line chart");
        }
        int n = Count();
        if (n > 0 && Category(0).Length > 0) {
            sb.Append(", ").Append(Category(0)).Append(" to ").Append(Category(n - 1));
        }
        sb.Append(':');
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
            sb.Append(" from ").Append(Value(line.values[first])).Append(" to ").Append(Value(line.values[last]));
            if (high != first && high != last) {
                sb.Append(", highest ").Append(Value(line.values[high]));
                if (Category(high).Length > 0) {
                    sb.Append(" in ").Append(Category(high));
                }
            }
        }
        return sb.Append('.').ToString();
    }

    /// <summary>
    /// Draws the chart on the page.
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
        if (page == null || all.Count == 0) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.GetSize();
        float pad = Pad();
        float ascent = f2.GetAscent(size);

        ChartScale.Axis axis = ChartScale.AxisOf(all.ToArray(), yMin, yMax, yRangeSet);
        float step = (axis.max - axis.min) / axis.lines;

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
                endWidth = Math.Max(endWidth, FontOf(i).StringWidth(size, line.name + " " + Value(line.values[last])));
            }
        }
        float left = x + pad + widest + size / 2f;
        if (yAxisTitle.Length > 0) {
            left += 1.8f * size;    // The title, as far from the numbers as the x title is
        }
        float right = x + w - pad - endWidth - size / 2f;
        float top = y + Top() + ascent / 2f;     // Room for the top number
        float bottom = y + h - pad - 1.4f * size;
        if (xAxisTitle.Length > 0) {
            bottom -= 1.4f * size;
        }
        if (right <= left || bottom <= top) {
            return new float[] {x + w, y + h};
        }

        page.AddBDC(StructElem.FIGURE, null, AltDescription());
        page.SaveGraphicsState();

        if (panelColor >= 0) {
            page.SetBrushColor(panelColor);
            page.FillRect(x, y, w, h);
        }
        float baseline = y + pad;
        if (title.Length > 0) {
            baseline += f1.GetAscent(f1.GetSize());
            page.DrawString(f1, f1.GetSize(), title, x + pad, baseline, Util.ToRGB(SimpleBarChart.INK), null);
            baseline += f1.GetDescent(f1.GetSize());
        }
        if (subtitle.Length > 0) {
            baseline += 1.4f * SubtitleSize();
            page.DrawString(f2, SubtitleSize(), subtitle, x + pad, baseline, Util.ToRGB(SimpleBarChart.MUTED), null);
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
        for (int i = 0; i < n; i += every) {
            String label = Category(i);
            page.DrawString(f2, size, label, XAt(i, n, left, right) - f2.StringWidth(size, label) / 2f,
                    bottom + 1.4f * size, Util.ToRGB(SimpleBarChart.MUTED), null);
        }
        if (xAxisTitle.Length > 0) {
            page.DrawString(f2, size, xAxisTitle, left + (right - left - f2.StringWidth(size, xAxisTitle)) / 2f,
                    bottom + 2.8f * size, Util.ToRGB(SimpleBarChart.INK), null);
        }
        if (yAxisTitle.Length > 0) {
            page.SetTextRotation(-90);
            page.DrawString(f2, size, yAxisTitle, x + pad + ascent,
                    bottom - (bottom - top - f2.StringWidth(size, yAxisTitle)) / 2f,
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
            if (markers) {
                page.SetBrushColor(color);
                for (int j = 0; j < n; j++) {
                    if (line.Has(j)) {
                        page.FillCircle(XAt(j, n, left, right), YAt(line.values[j], axis, top, bottom), 0.25f * size);
                    }
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
                labels.Add(new EndLabel(line.name + " " + Value(line.values[last]), i,
                        YAt(line.values[last], axis, top, bottom) + ascent / 2f));
            }
        }
        // By the baseline, and labels on one baseline in the order of their
        // lines, as a stable sort leaves them
        labels.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.index.CompareTo(b.index));
        for (int k = 1; k < labels.Count; k++) {
            labels[k].y = Math.Max(labels[k].y, labels[k - 1].y + f2.GetBodyHeight());
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

    // The x on the page of the category of the index.
    private static float XAt(int i, int n, float left, float right) {
        if (n == 1) {
            return (left + right) / 2f;
        }
        return left + i * (right - left) / (n - 1);
    }

    // The y on the page of a value.
    private static float YAt(float v, ChartScale.Axis axis, float top, float bottom) {
        return bottom - (v - axis.min) * (bottom - top) / (axis.max - axis.min);
    }
}
}   // End of SimpleLineChart.cs
