/*
 * BarChart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Bar chart renderer for PDF pages: one slot per category, the bars of the
/// series grouped inside each slot. See Example_39 (horizontal bars) and
/// Example_40 (vertical bars).
/// </summary>
public class BarChart : IDrawable {
    /// <summary>One series of the chart: a name, a value per category and a color.</summary>
    private sealed class Series {
        internal readonly String name;
        internal readonly float[] values;
        internal readonly int color;
        internal Series(String name, float[] values, int color) {
            this.name = name;
            this.values = values;
            this.color = color;
        }
    }

    private const int NO_COLOR = -1;

    private float x1;
    private float y1;
    private float w = 300f;
    private float h = 200f;

    private String title = "";
    private String xAxisTitle = "";
    private String yAxisTitle = "";

    private readonly List<String> categories = new List<String>();
    private readonly List<Series> series = new List<Series>();

    private bool horizontal = false;
    private bool stacked = false;
    private float groupGap = 0.3f;
    private float barGap = 0f;

    private bool drawGridLines = true;
    private bool drawValueLabels = false;
    private bool drawLegend = true;

    private float gridLineWidth = 0f;
    private String gridLineDashPattern = "[1 1] 0";
    private float axisLineWidth = 0.5f;
    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;

    // Value axis range; auto-computed from the data unless gridLines > 0
    private float min;
    private float max;
    private int gridLines = 0;

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
        this.title = title;
        return this;
    }

    /// <summary>Sets the X axis title.</summary>
    /// <param name="title">the title.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetXAxisTitle(String title) {
        this.xAxisTitle = title;
        return this;
    }

    /// <summary>Sets the Y axis title.</summary>
    /// <param name="title">the title.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetYAxisTitle(String title) {
        this.yAxisTitle = title;
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
        this.categories.AddRange(categories);
        return this;
    }

    /// <summary>Adds a series drawn in the next color of the default palette.</summary>
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
        series.Add(new Series(name == null ? "" : name, (float[]) values.Clone(), color));
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of this chart.</summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
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
    public BarChart SetSize(double w, double h) {
        return SetSize((float) w, (float) h);
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
        this.groupGap = gap;
        return this;
    }

    /// <summary>
    /// Sets the gap between the bars of a group as a fraction of the bar width.
    /// The default is 0.0, so the bars of a group touch.
    /// </summary>
    /// <param name="gap">the gap.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetBarGap(float gap) {
        this.barGap = gap;
        return this;
    }

    /// <summary>Sets whether the grid lines of the value axis are drawn.</summary>
    /// <param name="drawGridLines">true to draw them.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetDrawGridLines(bool drawGridLines) {
        this.drawGridLines = drawGridLines;
        return this;
    }

    /// <summary>
    /// Sets whether the value of each bar is written at its end, or, in a
    /// stacked chart, inside each segment that has room for it.
    /// </summary>
    /// <param name="drawValueLabels">true to write the values.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetDrawValueLabels(bool drawValueLabels) {
        this.drawValueLabels = drawValueLabels;
        return this;
    }

    /// <summary>
    /// Sets whether the legend is drawn. The legend lists the series that have
    /// a name, under the title.
    /// </summary>
    /// <param name="drawLegend">true to draw the legend.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetDrawLegend(bool drawLegend) {
        this.drawLegend = drawLegend;
        return this;
    }

    /// <summary>
    /// Sets the width of the grid lines. A width of 0 draws the thinnest line
    /// a viewer shows; SetDrawGridLines(false) hides them.
    /// </summary>
    /// <param name="width">the line width.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetGridLineWidth(float width) {
        this.gridLineWidth = width;
        return this;
    }

    /// <summary>Sets the dash pattern of the grid lines, for example "[1 1] 0".</summary>
    /// <param name="pattern">the dash pattern.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetGridLineDashPattern(String pattern) {
        this.gridLineDashPattern = pattern;
        return this;
    }

    /// <summary>Sets the width of the axis lines. The default is 0.5.</summary>
    /// <param name="width">the line width.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetAxisLineWidth(float width) {
        this.axisLineWidth = width;
        return this;
    }

    /// <summary>Sets the width of the outer chart border. A width of 0, the default, hides it.</summary>
    /// <param name="width">the border width.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetChartBorderWidth(float width) {
        this.chartBorderWidth = width;
        return this;
    }

    /// <summary>Sets the width of the plot area border. A width of 0, the default, hides it.</summary>
    /// <param name="width">the border width.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of decimal places in the value labels. The axis
    /// labels have at least the decimal places of the axis step. The default is 0.
    /// </summary>
    /// <param name="minFractionDigits">the minimum number of decimal places.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /// <summary>Sets the maximum number of decimal places in the value labels. The default is 2.</summary>
    /// <param name="maxFractionDigits">the maximum number of decimal places.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /// <summary>
    /// Sets the range and the number of grid lines of the value axis. Without
    /// it the range is computed from the data and always includes 0.
    /// </summary>
    /// <param name="min">the value at the start of the axis.</param>
    /// <param name="max">the value at the end of the axis.</param>
    /// <param name="gridLines">the number of grid lines, at least 1.</param>
    /// <returns>this BarChart object.</returns>
    public BarChart SetValueAxisMinMax(float min, float max, int gridLines) {
        this.min = min;
        this.max = max;
        this.gridLines = gridLines;
        return this;
    }

    /// <summary>Draws this chart on the specified page.</summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the bottom right corner coordinates [x, y].</returns>
    public float[] DrawOn(Page page) {
        int n = NumberOfCategories();
        if (n == 0) {
            return new float[] {x1 + w, y1 + h};
        }

        float vMin;
        float vMax;
        int lines;
        if (gridLines > 0) {
            vMin = min;
            vMax = max;
            lines = gridLines;
        } else {
            float lo = 0f;
            float hi = 0f;
            if (stacked) {
                // The sums of the values above and below 0 in each category
                for (int i = 0; i < n; i++) {
                    float up = 0f;
                    float down = 0f;
                    foreach (Series s in series) {
                        if (i < s.values.Length) {
                            if (s.values[i] >= 0f) { up += s.values[i]; } else { down += s.values[i]; }
                        }
                    }
                    lo = Math.Min(lo, down);
                    hi = Math.Max(hi, up);
                }
            } else {
                foreach (Series s in series) {
                    foreach (float v in s.values) {
                        lo = Math.Min(lo, v);
                        hi = Math.Max(hi, v);
                    }
                }
            }
            if (hi == lo) { hi = lo + 1f; }
            Round round = Chart.RoundMaxAndMinValues(hi, lo);
            vMin = round.minValue;
            vMax = round.maxValue;
            lines = round.numOfGridLines;
        }
        if (vMax == vMin) { vMax = vMin + 1f; }
        float step = (vMax - vMin) / lines;
        int axisDigits = Math.Max(minFractionDigits, Chart.FractionDigitsOf(step, maxFractionDigits));
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
            String label = Chart.Format(vMin + step * i, axisDigits, maxFractionDigits);
            widestAxisLabel = Math.Max(widestAxisLabel, f2.StringWidth(label));
        }
        float widestValueLabel = 0f;
        if (drawValueLabels) {
            foreach (Series s in series) {
                foreach (float v in s.values) {
                    widestValueLabel = Math.Max(widestValueLabel, f2.StringWidth(ValueLabel(v)));
                }
            }
        }
        float widestCategory = 0f;
        foreach (String category in categories) {
            widestCategory = Math.Max(widestCategory, f2.StringWidth(category));
        }

        // Margins and the plot area
        float topMargin = 2.5f * f1.GetBodyHeight() + (legend ? 1.5f * bodyHeight : 0f);
        float leftMargin = 1.5f * bodyHeight + pad + (horizontal ? widestCategory : widestAxisLabel);
        float rightMargin = pad;
        float bottomMargin = 2.5f * bodyHeight;
        if (horizontal) {
            rightMargin += Math.Max(widestAxisLabel / 2f, widestValueLabel + pad);
        } else if (drawValueLabels && !stacked) {
            topMargin += bodyHeight;
        }
        float x5 = x1 + leftMargin;
        float y5 = y1 + topMargin;
        float x6 = x2 - rightMargin;
        float y8 = y2 - bottomMargin;

        // Title, then the legend under it
        page.SetBrushColor(Color.black);
        page.DrawString(f1, f1.GetSize(), title, x1 + (w - f1.StringWidth(title)) / 2f, y1 + 1.5f * f1.GetBodyHeight());
        if (legend) {
            DrawLegend(page, y1 + 1.5f * f1.GetBodyHeight() + 1.5f * bodyHeight);
        }

        if (chartBorderWidth > 0f) {
            page.SetPenColor(Color.black);
            page.SetPenWidth(chartBorderWidth);
            page.SetDefaultStrokeDashPattern();
            page.DrawRect(x1, y1, w, h);
        }

        // Grid lines and value axis labels
        for (int i = 0; i <= lines; i++) {
            float v = vMin + step * i;
            String label = Chart.Format(v, axisDigits, maxFractionDigits);
            if (horizontal) {
                float x = x5 + (v - vMin) * (x6 - x5) / (vMax - vMin);
                if (drawGridLines) {
                    GridLine(page, x, y5, x, y8);
                }
                page.DrawString(f2, f2.GetSize(), label, x - f2.StringWidth(label) / 2f, y8 + bodyHeight);
            } else {
                float y = y8 - (v - vMin) * (y8 - y5) / (vMax - vMin);
                if (drawGridLines) {
                    GridLine(page, x5, y, x6, y);
                }
                page.DrawString(f2, f2.GetSize(), label, x5 - pad - f2.StringWidth(label), y + ascent / 2f);
            }
        }

        // Bars, category labels and value labels
        int m = series.Count;
        float slot = (horizontal ? (y8 - y5) : (x6 - x5)) / n;
        float groupWidth = slot * (1f - groupGap);
        float barWidth = (m == 0 || stacked) ? groupWidth : groupWidth / (m + (m - 1) * barGap);
        for (int i = 0; i < n; i++) {
            float slotStart = (horizontal ? y5 : x5) + i * slot;
            float groupStart = slotStart + slot * groupGap / 2f;
            String category = i < categories.Count ? categories[i] : "";
            page.SetBrushColor(Color.black);
            if (horizontal) {
                page.DrawString(f2, f2.GetSize(), category, x5 - pad - f2.StringWidth(category),
                        slotStart + slot / 2f + ascent / 2f);
            } else {
                page.DrawString(f2, f2.GetSize(), category, slotStart + (slot - f2.StringWidth(category)) / 2f,
                        y8 + bodyHeight);
            }
            float up = 0f;      // the stacked values above 0 so far
            float down = 0f;    // the stacked values below 0 so far
            for (int j = 0; j < m; j++) {
                Series s = series[j];
                if (i >= s.values.Length) {
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
                from = Math.Min(Math.Max(from, vMin), vMax);
                to = Math.Min(Math.Max(to, vMin), vMax);
                float barStart = stacked ? groupStart : groupStart + j * barWidth * (1f + barGap);
                page.SetBrushColor(s.color == NO_COLOR ? Chart.DEFAULT_PALETTE[j % Chart.DEFAULT_PALETTE.Length] : s.color);
                String label = drawValueLabels ? ValueLabel(s.values[i]) : null;
                if (horizontal) {
                    float x0 = x5 + (from - vMin) * (x6 - x5) / (vMax - vMin);
                    float x = x5 + (to - vMin) * (x6 - x5) / (vMax - vMin);
                    page.FillRect(Math.Min(x0, x), barStart, Math.Abs(x - x0), barWidth);
                    if (label != null && stacked) {
                        if (Math.Abs(x - x0) >= f2.StringWidth(label) + pad) {
                            page.SetBrushColor(Color.black);
                            page.DrawString(f2, f2.GetSize(), label,
                                    Math.Min(x0, x) + (Math.Abs(x - x0) - f2.StringWidth(label)) / 2f,
                                    barStart + barWidth / 2f + ascent / 2f);
                        }
                    } else if (label != null) {
                        page.SetBrushColor(Color.black);
                        page.DrawString(f2, f2.GetSize(), label,
                                to >= baseValue ? x + pad / 2f : x - pad / 2f - f2.StringWidth(label),
                                barStart + barWidth / 2f + ascent / 2f);
                    }
                } else {
                    float y0 = y8 - (from - vMin) * (y8 - y5) / (vMax - vMin);
                    float y = y8 - (to - vMin) * (y8 - y5) / (vMax - vMin);
                    page.FillRect(barStart, Math.Min(y0, y), barWidth, Math.Abs(y - y0));
                    if (label != null && stacked) {
                        if (Math.Abs(y - y0) >= bodyHeight) {
                            page.SetBrushColor(Color.black);
                            page.DrawString(f2, f2.GetSize(), label, barStart + (barWidth - f2.StringWidth(label)) / 2f,
                                    (y + y0) / 2f + ascent / 2f);
                        }
                    } else if (label != null) {
                        page.SetBrushColor(Color.black);
                        page.DrawString(f2, f2.GetSize(), label, barStart + (barWidth - f2.StringWidth(label)) / 2f,
                                to >= baseValue ? y - pad / 2f : y + ascent + pad / 2f);
                    }
                }
            }
        }

        // Axis lines: along the labels and at the base of the bars
        page.SetPenColor(Color.black);
        page.SetPenWidth(axisLineWidth);
        page.SetDefaultStrokeDashPattern();
        if (horizontal) {
            float x0 = x5 + (baseValue - vMin) * (x6 - x5) / (vMax - vMin);
            page.DrawLine(x5, y8, x6, y8);
            page.DrawLine(x0, y5, x0, y8);
        } else {
            float y0 = y8 - (baseValue - vMin) * (y8 - y5) / (vMax - vMin);
            page.DrawLine(x5, y5, x5, y8);
            page.DrawLine(x5, y0, x6, y0);
        }
        if (innerBorderWidth > 0f) {
            page.SetPenWidth(innerBorderWidth);
            page.DrawRect(x5, y5, x6 - x5, y8 - y5);
        }

        // Axis titles
        page.SetBrushColor(Color.black);
        page.SetTextRotation(90);
        page.DrawString(f2, f2.GetSize(), yAxisTitle, x1 + bodyHeight, y8 - ((y8 - y5) - f2.StringWidth(yAxisTitle)) / 2f);
        page.SetTextRotation(0);
        page.DrawString(f2, f2.GetSize(), xAxisTitle, x5 + ((x6 - x5) - f2.StringWidth(xAxisTitle)) / 2f, y2 - bodyHeight / 2f);

        page.SetDefaultPenWidth();
        page.SetDefaultStrokeDashPattern();
        page.SetPenColor(Color.black);

        return new float[] {x1 + w, y1 + h};
    }

    /// <summary>Returns the number of category slots: the categories or the longest series.</summary>
    private int NumberOfCategories() {
        int n = categories.Count;
        foreach (Series s in series) {
            n = Math.Max(n, s.values.Length);
        }
        return n;
    }

    /// <summary>Returns true if a series has a name to list in the legend.</summary>
    private bool HasSeriesNames() {
        foreach (Series s in series) {
            if (s.name.Length > 0) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Formats the value written at the end of a bar.</summary>
    private String ValueLabel(float value) {
        return Chart.Format(value, minFractionDigits, maxFractionDigits);
    }

    /// <summary>Draws one dotted grid line.</summary>
    private void GridLine(Page page, float xa, float ya, float xb, float yb) {
        page.SetPenColor(Color.black);
        page.SetPenWidth(gridLineWidth);
        page.SetStrokeDashPattern(gridLineDashPattern);
        page.DrawLine(xa, ya, xb, yb);
    }

    /// <summary>Draws the legend centered on the chart, with a swatch before each series name.</summary>
    private void DrawLegend(Page page, float baseline) {
        float swatch = f2.GetAscent();
        float gap = swatch / 2f;
        float width = 0f;
        int entries = 0;
        foreach (Series s in series) {
            if (s.name.Length > 0) {
                width += swatch + gap + f2.StringWidth(s.name);
                entries++;
            }
        }
        width += (entries - 1) * f2.GetBodyHeight();
        float x = x1 + (w - width) / 2f;
        for (int j = 0; j < series.Count; j++) {
            Series s = series[j];
            if (s.name.Length == 0) {
                continue;
            }
            page.SetBrushColor(s.color == NO_COLOR ? Chart.DEFAULT_PALETTE[j % Chart.DEFAULT_PALETTE.Length] : s.color);
            page.FillRect(x, baseline - swatch, swatch, swatch);
            x += swatch + gap;
            page.SetBrushColor(Color.black);
            page.DrawString(f2, f2.GetSize(), s.name, x, baseline);
            x += f2.StringWidth(s.name) + f2.GetBodyHeight();
        }
    }
}   // End of BarChart.cs
}   // End of namespace PDFjet.NET
