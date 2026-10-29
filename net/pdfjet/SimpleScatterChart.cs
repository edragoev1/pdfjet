/*
 * SimpleScatterChart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// A chart of points on two axes that shows how two values go together: many
/// points as small gray dots, a little transparent so that a cluster shows
/// darker, a few of them highlighted, navy and labeled with their names beside
/// them, and the trend line of the points if asked, labeled at its end. A light
/// horizontal grid alone, no border or legend, on a light panel, with a title
/// and a subtitle at the top left, in the look of SimpleBarChart.
///
/// In a PDF/UA document the chart is one figure, described from its data: the
/// number of points, the highlighted ones and their values, and which way the
/// trend goes, unless an alternate description is set. It draws with the page
/// alone, and needs no other chart. In PDF/A-1, which has no transparency, the
/// dots are drawn opaque. Please see Example_26.
/// </summary>
public class SimpleScatterChart : IDrawable {
    // The colors of a simple scatter chart unless set, those of
    // SimpleBarChart: a light panel, gray dots a little transparent, the
    // highlighted points navy, the grid a light gray, the trend line and the
    // numbers dark gray.
    internal const int DOT = 0x8C96A3;
    internal const int GRID = 0xD5D9DE;
    internal const int TREND = 0x5F6873;
    internal const float ALPHA = 0.55f;

    // A point of the chart, highlighted when it has a name.
    internal sealed class Point {
        internal readonly float x;
        internal readonly float y;
        internal readonly String name;
        internal readonly String uri;      // The link of a highlighted point, or null
        internal int series = -1;          // The index of its series, or -1 for none

        internal Point(float x, float y, String name) : this(x, y, name, null) {
        }

        internal Point(float x, float y, String name, String uri) {
            this.x = x;
            this.y = y;
            this.name = name ?? "";
            this.uri = uri;
        }
    }

    private readonly Font f1;      // The title and the names
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
    private readonly List<Point> points = new List<Point>();
    private readonly List<String> series = new List<String>();     // The names of the series
    private bool trendLine = false;
    private float xMin;
    private float xMax;
    private float yMin;
    private float yMax;
    private bool xRangeSet = false;
    private bool yRangeSet = false;
    private int panelColor = SimpleBarChart.PANEL;
    private int dotColor = DOT;
    private int highlightColor = SimpleBarChart.HIGHLIGHT;
    private int maxFractionDigits = 2;      // As many as the steps of the axes need

    /// <summary>
    /// Creates a simple scatter chart, 400 by 300 points.
    /// </summary>
    /// <param name="f1">the font of the title and of the names of the highlighted points, a bold one.</param>
    /// <param name="f2">the font of the subtitle, the axis titles and the numbers.</param>
    public SimpleScatterChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /// <summary>
    /// Sets the title, drawn at the top left of the chart.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetTitle(String title) {
        this.title = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the subtitle, drawn small and gray under the title.
    /// </summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetSubtitle(String subtitle) {
        this.subtitle = subtitle ?? "";
        return this;
    }

    /// <summary>
    /// Sets the title of the horizontal axis, drawn under it.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetXAxisTitle(String title) {
        this.xAxisTitle = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the title of the vertical axis, drawn up its left side.
    /// </summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetYAxisTitle(String title) {
        this.yAxisTitle = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the alternate description of the chart in a PDF/UA document.
    /// Without one the chart is described from its data.
    /// </summary>
    /// <param name="altDescription">the alternate description.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetAltDescription(String altDescription) {
        this.altDescription = altDescription ?? "";
        return this;
    }

    /// <summary>
    /// Adds a point, drawn as a small gray dot. A point whose x or y is not a
    /// number is left out.
    /// </summary>
    /// <param name="x">the x of the point.</param>
    /// <param name="y">the y of the point.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart Add(float x, float y) {
        return AddPoint(new Point(x, y, ""));
    }

    // Adds the point to the series added last, if any.
    private SimpleScatterChart AddPoint(Point p) {
        p.series = series.Count - 1;
        points.Add(p);
        return this;
    }

    /// <summary>
    /// Starts a series: the points added after it are its points, drawn in its
    /// color and its shape, and its name is in the legend over the plot. The
    /// series take the colors of the palette of BarChart, blue, orange, red,
    /// teal and so on, and the shapes circle, square, triangle, star, diamond
    /// and cross, in turn. The points added before the first series are gray
    /// dots, as those of a chart without series.
    /// </summary>
    /// <param name="name">the name of the series, in the legend.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart AddSeries(String name) {
        series.Add(name ?? "");
        return this;
    }

    /// <summary>
    /// Adds a point that is highlighted: a navy dot with its name beside it.
    /// </summary>
    /// <param name="name">the name of the point.</param>
    /// <param name="x">the x of the point.</param>
    /// <param name="y">the y of the point.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart Add(String name, float x, float y) {
        return AddPoint(new Point(x, y, name));
    }

    /// <summary>
    /// Adds a point that is highlighted and is a link: a navy dot with its name
    /// beside it, both of them the link to the URI. In a PDF/UA document each
    /// such point is a figure of its own, described by its name, in the Link
    /// that holds its annotation, as PDF/UA asks.
    /// </summary>
    /// <param name="name">the name of the point.</param>
    /// <param name="x">the x of the point.</param>
    /// <param name="y">the y of the point.</param>
    /// <param name="uri">the URI the point links to.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart Add(String name, float x, float y, String uri) {
        // An empty URI is no link, as in the Go port
        return AddPoint(new Point(x, y, name, (uri == null || uri.Length == 0) ? null : uri));
    }

    /// <summary>
    /// Draws the trend line of the points, their least squares line, thin and
    /// dark gray, labeled "trend" at its right end.
    /// </summary>
    /// <param name="trendLine">true to draw it.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetTrendLine(bool trendLine) {
        this.trendLine = trendLine;
        return this;
    }

    /// <summary>
    /// Sets the range of the horizontal axis. Without it the range is that of
    /// the points, rounded to steps. A range whose end is not past its start
    /// is not set.
    /// </summary>
    /// <param name="min">the first value of the axis.</param>
    /// <param name="max">the last value of the axis.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetXAxisRange(float min, float max) {
        this.xMin = min;
        this.xMax = max;
        this.xRangeSet = max > min;
        return this;
    }

    /// <summary>
    /// Sets the range of the vertical axis. Without it the range is that of the
    /// points, rounded to steps. A range whose end is not past its start is not
    /// set.
    /// </summary>
    /// <param name="min">the first value of the axis.</param>
    /// <param name="max">the last value of the axis.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetYAxisRange(float min, float max) {
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
    public SimpleScatterChart SetPanelColor(int color) {
        this.panelColor = color;
        return this;
    }

    /// <summary>
    /// Sets the color of the points that are not highlighted. It is gray,
    /// 0x8C96A3, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetDotColor(int color) {
        this.dotColor = color;
        return this;
    }

    /// <summary>
    /// Sets the color of the highlighted points and of their names. It is navy,
    /// 0x1F3A5F, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetHighlightColor(int color) {
        this.highlightColor = color;
        return this;
    }

    /// <summary>
    /// Sets the top left corner of the chart.
    /// </summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this chart.</returns>
    public SimpleScatterChart SetLocation(float x, float y) {
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
    public SimpleScatterChart SetSize(float w, float h) {
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
        // The legend, and half a size of the numbers under it, so that the
        // top number of the axis is not against its last row
        int rows = DrawLegend(null, 0f);
        return top + rows*LEGEND_ROW*f2.GetSize() + ((rows > 0) ? 0.5f*f2.GetSize() : 0f);
    }

    // The shapes of the series, in turn, as D3 draws its symbols.
    internal const int CIRCLE = 0;
    internal const int SQUARE = 1;
    internal const int TRIANGLE = 2;
    internal const int STAR = 3;
    internal const int DIAMOND = 4;
    internal const int CROSS = 5;
    internal const int SHAPES = 6;
    // The height of a row of the legend, in sizes of the numbers.
    private const float LEGEND_ROW = 1.5f;

    // The color of a series.
    private static int SeriesColor(int index) {
        return BarChart.PALETTE[index % BarChart.PALETTE.Length];
    }

    // Draws the legend, the shape and the name of each series, from the
    // baseline of its first row, its rows wrapped at the right of the chart,
    // when there is a page; and returns the number of its rows, 0 for a chart
    // without series.
    private int DrawLegend(Page page, float baseline) {
        if (series.Count == 0) {
            return 0;
        }
        float size = f2.GetSize();
        float pad = Pad();
        float r = 0.35f*size;
        float gap = size/2f;
        float left = x + pad;
        float px = left;
        int rows = 1;
        for (int i = 0; i < series.Count; i++) {
            String name = ChartScale.Shorten(f2, size, series[i], w - 2f*pad - 2f*r - gap);
            float width = 2f*r + gap + f2.StringWidth(size, name);
            if (px > left && px + width > x + w - pad) {
                px = left;
                baseline += LEGEND_ROW*size;
                rows++;
            }
            if (page != null) {
                page.SetPenColor(SeriesColor(i));
                page.SetPenWidth(0.12f*size);
                DrawShape(page, i % SHAPES, px + r, baseline - f2.GetAscent(size)/2f, r, false);
                page.DrawString(f2, size, name, px + 2f*r + gap, baseline, Util.ToRGB(SimpleBarChart.INK), null);
            }
            px += width + size;
        }
        return rows;
    }

    // Draws a shape of a series centered on cx, cy, of about the area of a
    // circle of radius r, its outline in the pen color, or filled in the
    // brush color.
    internal static void DrawShape(Page page, int shape, float cx, float cy, float r, bool fill) {
        if (shape == CIRCLE) {
            if (fill) {
                page.FillCircle(cx, cy, r);
            } else {
                page.DrawCircle(cx, cy, r);
            }
            return;
        }
        float[] xy;
        if (shape == SQUARE) {
            float a = 0.886f*r;
            xy = new float[] {-a, -a, a, -a, a, a, -a, a};
        } else if (shape == TRIANGLE) {
            // Pointing up, its center halfway down its height
            float a = 1.35f*r;
            xy = new float[] {0f, -a, 0.866f*a, 0.5f*a, -0.866f*a, 0.5f*a};
        } else if (shape == STAR) {
            xy = new float[20];
            for (int i = 0; i < 10; i++) {
                double angle = Math.PI*i/5.0;
                float radius = (i % 2 == 0) ? 1.3f*r : 0.5f*r;
                xy[2*i] = (float) (radius*Math.Sin(angle));
                xy[2*i + 1] = (float) (-radius*Math.Cos(angle));
            }
        } else if (shape == DIAMOND) {
            float a = 1.15f*r;
            float b = 0.75f*r;
            xy = new float[] {0f, -a, b, 0f, 0f, a, -b, 0f};
        } else {
            // A cross of five squares, as wide as three
            float a = 1.1f*r;
            float b = a/3f;
            xy = new float[] {-b, -a, b, -a, b, -b, a, -b, a, b, b, b, b, a, -b, a, -b, b, -a, b, -a, -b, -b, -b};
        }
        page.MoveTo(cx + xy[0], cy + xy[1]);
        for (int i = 2; i < xy.Length; i += 2) {
            page.LineTo(cx + xy[i], cy + xy[i + 1]);
        }
        if (fill) {
            page.FillPath();
        } else {
            page.ClosePath();   // Closed and stroked
        }
    }

    // A number of an axis, with the decimals its step needs.
    private String Label(float value, float step) {
        int digits = ChartScale.FractionDigitsOf(step, maxFractionDigits);
        return ChartScale.GroupDigits(ChartScale.Format(
                value, digits, maxFractionDigits, ChartScale.StepSignificant(value, step)));
    }

    // The points whose x and y are numbers.
    private List<Point> Valid() {
        List<Point> valid = new List<Point>();
        foreach (Point p in points) {
            if (ChartScale.IsValue(p.x) && ChartScale.IsValue(p.y)) {
                valid.Add(p);
            }
        }
        return valid;
    }

    // The slope and the intercept of the least squares line of the points, or
    // null when there is none: fewer than two points, or all at one x.
    internal static double[] Trend(List<Point> points) {
        double n = points.Count;
        if (n < 2) {
            return null;
        }
        // From the sums of the distances from the means, which keep their
        // precision where the raw sums lost it, for x near 1e8 of a small spread
        double sx = 0;
        double sy = 0;
        foreach (Point p in points) {
            sx += p.x;
            sy += p.y;
        }
        double mx = sx / n;
        double my = sy / n;
        double sxx = 0;
        double sxy = 0;
        foreach (Point p in points) {
            double dx = p.x - mx;
            sxx += dx * dx;
            sxy += dx * (p.y - my);
        }
        if (sxx == 0) {
            return null;
        }
        double slope = sxy / sxx;
        return new double[] {slope, my - slope * mx};
    }

    // The horizontal and the vertical axis of the points.
    private ChartScale.Axis[] Axes(List<Point> points) {
        float[] xs = new float[points.Count];
        float[] ys = new float[points.Count];
        for (int i = 0; i < points.Count; i++) {
            xs[i] = points[i].x;
            ys[i] = points[i].y;
        }
        return new ChartScale.Axis[] {
            ChartScale.AxisOf(xs, xMin, xMax, xRangeSet),
            ChartScale.AxisOf(ys, yMin, yMax, yRangeSet)
        };
    }

    // The trend of the points as it is drawn: none where it misses the plot,
    // both of its ends past one edge, as a flat one over a range that is set,
    // which was drawn outside the chart.
    private static double[] ShownTrend(List<Point> points, ChartScale.Axis xAxis, ChartScale.Axis yAxis) {
        // Of the points drawn, not of those outside a range that is set
        List<Point> shown = new List<Point>();
        foreach (Point p in points) {
            if (OnPlot(p, xAxis, yAxis)) {
                shown.Add(p);
            }
        }
        double[] line = Trend(shown);
        if (line != null) {
            double a = line[0] * xAxis.min + line[1];
            double b = line[0] * xAxis.max + line[1];
            if ((a > yAxis.max && b > yAxis.max) || (a < yAxis.min && b < yAxis.min)) {
                return null;
            }
        }
        return line;
    }

    // The alternate description: the one set, or the title, the number of
    // points, the highlighted ones and the trend.
    internal String AltDescription() {
        if (altDescription.Length > 0) {
            return altDescription;
        }
        List<Point> points = Valid();
        ChartScale.Axis[] axes = points.Count == 0 ? null : Axes(points);
        if (axes != null) {
            maxFractionDigits = Math.Max(ChartScale.DigitsOf((axes[0].max - axes[0].min) / axes[0].lines),
                    ChartScale.DigitsOf((axes[1].max - axes[1].min) / axes[1].lines));
        }
        // The points that are drawn: not those outside a range that is set
        List<Point> shown = new List<Point>();
        foreach (Point p in points) {
            if (OnPlot(p, axes[0], axes[1])) {
                shown.Add(p);
            }
        }
        StringBuilder sb = new StringBuilder(ChartScale.Join(title, subtitle));
        if (sb.Length == 0) {
            sb.Append("Scatter chart");
        }
        sb.Append(SimpleBarChart.PunctuationAfter(sb, ":")).Append(' ').Append(shown.Count).Append(" points");
        if (xAxisTitle.Length > 0 && yAxisTitle.Length > 0) {
            sb.Append(", ").Append(yAxisTitle).Append(" against ").Append(xAxisTitle);
        }
        if (series.Count > 0) {
            // in the series A, B and C
            sb.Append(", in the series ");
            for (int i = 0; i < series.Count; i++) {
                if (i > 0) {
                    sb.Append((i == series.Count - 1) ? " and " : ", ");
                }
                sb.Append(series[i]);
            }
        }
        int named = 0;
        foreach (Point p in shown) {
            if (p.name.Length == 0) {
                continue;
            }
            sb.Append(named == 0 ? "; " : ", ");
            sb.Append(p.name).Append(' ').Append(ChartScale.GroupDigits(ChartScale.Format(p.x, 0, maxFractionDigits)))
                    .Append(" and ").Append(ChartScale.GroupDigits(ChartScale.Format(p.y, 0, maxFractionDigits)));
            named++;
        }
        if (trendLine && axes != null) {
            double[] line = ShownTrend(points, axes[0], axes[1]);    // Not one that is not drawn
            if (line != null) {
                sb.Append(line[0] > 0 ? "; the trend rises" : line[0] < 0 ? "; the trend falls" : "; the trend is flat");
            }
        }
        return sb.Append(SimpleBarChart.PunctuationAfter(sb, ".")).ToString();
    }

    /// <summary>
    /// Draws the chart on the page. A chart too small for its plot and its
    /// numbers draws nothing.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the chart.</returns>
    public float[] DrawOn(Page page) {
        List<Point> points = Valid();
        if (page == null || points.Count == 0 || f1 == null || f2 == null) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.GetSize();
        float pad = Pad();
        float ascent = f2.GetAscent(size);

        float[] xs = new float[points.Count];
        float[] ys = new float[points.Count];
        for (int i = 0; i < points.Count; i++) {
            xs[i] = points[i].x;
            ys[i] = points[i].y;
        }
        if ((!xRangeSet && !ChartScale.SpanFits(xs)) || (!yRangeSet && !ChartScale.SpanFits(ys))) {
            return new float[] {x + w, y + h};      // A span past the largest float, which no axis can draw
        }
        ChartScale.Axis[] axes = Axes(points);
        ChartScale.Axis xAxis = axes[0];
        ChartScale.Axis yAxis = axes[1];
        float xStep = (xAxis.max - xAxis.min) / xAxis.lines;
        float yStep = (yAxis.max - yAxis.min) / yAxis.lines;
        maxFractionDigits = Math.Max(ChartScale.DigitsOf(xStep), ChartScale.DigitsOf(yStep));

        // The plot: right of the y title and the widest number, above the
        // numbers and the x title
        float widest = 0f;
        for (int i = 0; i <= yAxis.lines; i++) {
            widest = Math.Max(widest, f2.StringWidth(size, Label(yAxis.min + yStep * i, yStep)));
        }
        float left = x + pad + widest + size / 2f;
        if (yAxisTitle.Length > 0) {
            left += 1.8f * size;     // The title, as far from the numbers as the x title is
        }
        // Room for the halves of the first and the last number under the plot,
        // centered on its edges: the last, "1,000,000", went past the chart
        float firstWidth = f2.StringWidth(size, Label(xAxis.min, xStep));
        float lastWidth = f2.StringWidth(size, Label(xAxis.min + xStep * xAxis.lines, xStep));
        left = Math.Max(left, x + pad / 2f + firstWidth / 2f);
        float right = Math.Min(x + w - pad, x + w - pad / 2f - lastWidth / 2f);
        float top = y + Top() + ascent / 2f;     // Room for the top number
        float bottom = y + h - pad - 1.4f * size;
        if (xAxisTitle.Length > 0) {
            bottom -= 1.4f * size;
        }
        if (right <= left || bottom <= top || !ChartScale.Drawable(xAxis) || !ChartScale.Drawable(yAxis)) {
            return new float[] {x + w, y + h};
        }
        float plotLeft = left;
        float plotBottom = bottom;

        // In a tagged document a point that is a link is drawn after the
        // chart, as a figure of its own in its Link, which holds its
        // annotation, as PDF/UA asks: a link inside the chart's figure has
        // nothing to hold
        bool linksApart = page.TagsContent();

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
        if (series.Count > 0) {
            if (title.Length > 0 || subtitle.Length > 0) {
                baseline += f2.GetSize();
            }
            DrawLegend(page, baseline + LEGEND_ROW*size - (LEGEND_ROW*size - ascent)/2f);
        }

        // The horizontal grid and the numbers of both axes, each where it
        // leaves the one before it room
        page.SetPenColor(GRID);
        page.SetPenWidth(0.5f);
        int every = ChartScale.AxisLabelStep((plotBottom - top) / yAxis.lines, f2.GetBodyHeight(), yAxis.lines);
        for (int i = 0; i <= yAxis.lines; i++) {
            float v = yAxis.min + yStep * i;
            float gy = YAt(v, yAxis, top, plotBottom);
            page.DrawLine(plotLeft, gy, right, gy);
            if (i % every == 0) {
                String label = Label(v, yStep);
                page.DrawString(f2, size, label, plotLeft - size / 2f - f2.StringWidth(size, label),
                        gy + ascent / 2f, Util.ToRGB(SimpleBarChart.MUTED), null);
            }
        }
        float xWidest = 0f;
        for (int i = 0; i <= xAxis.lines; i++) {
            xWidest = Math.Max(xWidest, f2.StringWidth(size, Label(xAxis.min + xStep * i, xStep)));
        }
        every = ChartScale.AxisLabelStep((right - plotLeft) / xAxis.lines, xWidest + pad, xAxis.lines);
        // Each centered on its line, and inside the chart
        float[] centers = new float[xAxis.lines + 1];
        float[] widths = new float[xAxis.lines + 1];
        for (int i = 0; i <= xAxis.lines; i++) {
            centers[i] = XAt(xAxis.min + xStep * i, xAxis, plotLeft, right);
            widths[i] = f2.StringWidth(size, Label(xAxis.min + xStep * i, xStep));
        }
        every = ChartScale.PlacedStep(every, centers, widths, x + pad / 2f, x + w - pad / 2f, pad / 2f);
        for (int i = 0; i <= xAxis.lines; i += every) {
            String label = Label(xAxis.min + xStep * i, xStep);
            page.DrawString(f2, size, label, ChartScale.Placed(centers[i], widths[i], x + pad / 2f, x + w - pad / 2f),
                    plotBottom + 1.4f * size, Util.ToRGB(SimpleBarChart.MUTED), null);
        }
        // The axis titles, shortened to the width and the height of the plot:
        // a long one ran past the chart (the review of 28 October 2026)
        if (xAxisTitle.Length > 0) {
            String xTitle = ChartScale.Shorten(f2, size, xAxisTitle, right - plotLeft);
            page.DrawString(f2, size, xTitle, plotLeft + (right - plotLeft - f2.StringWidth(size, xTitle)) / 2f,
                    plotBottom + 2.8f * size, Util.ToRGB(SimpleBarChart.INK), null);
        }
        if (yAxisTitle.Length > 0) {
            String yTitle = ChartScale.Shorten(f2, size, yAxisTitle, plotBottom - top);
            page.SetTextRotation(-90);
            page.DrawString(f2, size, yTitle, x + pad + ascent,
                    plotBottom - (plotBottom - top - f2.StringWidth(size, yTitle)) / 2f,
                    Util.ToRGB(SimpleBarChart.INK), null);
            page.SetTextRotation(0);
        }

        // The trend line, from the left of the plot to its right, within its
        // height
        double[] line = trendLine ? ShownTrend(points, xAxis, yAxis) : null;
        if (line != null) {
            double slope = line[0];
            double intercept = line[1];
            double[] p1 = Clip(xAxis.min, slope * xAxis.min + intercept, slope, intercept, yAxis);
            double[] p2 = Clip(xAxis.max, slope * xAxis.max + intercept, slope, intercept, yAxis);
            page.SetPenColor(TREND);
            page.SetPenWidth(0.75f);
            float x1 = XAt((float)p1[0], xAxis, plotLeft, right);
            float y1 = YAt((float)p1[1], yAxis, top, plotBottom);
            float x2 = XAt((float)p2[0], xAxis, plotLeft, right);
            float y2 = YAt((float)p2[1], yAxis, top, plotBottom);
            page.DrawLine(x1, y1, x2, y2);
            // The label over the end of the line, lifted by as much as a
            // falling line rises under it
            String label = "trend";
            float width = f2.StringWidth(SubtitleSize(), label);
            float lift = size / 3f;
            if (x2 > x1 && y2 > y1) {
                lift += width * ((y2 - y1) / (x2 - x1));
            }
            // Not left of the plot, where a short steep line put it
            page.DrawString(f2, SubtitleSize(), label, Math.Max(plotLeft, x2 - width), y2 - lift,
                    Util.ToRGB(TREND), null);
        }

        // The dots, then the highlighted points and their names over them
        page.SaveGraphicsState();
        if (!page.pdf.IsPDFA1()) {
            GraphicsState gs = new GraphicsState();
            gs.SetAlphaNonStroking(ALPHA);
            page.SetGraphicsState(gs);
        }
        page.SetBrushColor(dotColor);
        float r = 0.3f * size;
        foreach (Point p in points) {
            if (p.series < 0 && p.name.Length == 0 && OnPlot(p, xAxis, yAxis)) {
                page.FillCircle(XAt(p.x, xAxis, plotLeft, right), YAt(p.y, yAxis, top, plotBottom), r);
            }
        }
        page.RestoreGraphicsState();
        // The points of the series, the outlines of their shapes, opaque;
        // a chart without series draws as it did
        if (series.Count > 0) {
            page.SetPenWidth(0.12f * size);
        }
        foreach (Point p in points) {
            if (p.series >= 0 && p.name.Length == 0 && OnPlot(p, xAxis, yAxis)) {
                page.SetPenColor(SeriesColor(p.series));
                DrawShape(page, p.series % SHAPES, XAt(p.x, xAxis, plotLeft, right),
                        YAt(p.y, yAxis, top, plotBottom), 0.35f * size, false);
            }
        }
        List<Point> linked = new List<Point>();
        foreach (Point p in points) {
            if (p.name.Length == 0 || !OnPlot(p, xAxis, yAxis)) {
                continue;
            }
            if (p.uri != null && linksApart) {
                linked.Add(p);
                continue;
            }
            float[] box = DrawNamed(page, p, xAxis, yAxis, plotLeft, right, top, plotBottom);
            if (p.uri != null) {
                page.AddAnnotation(LinkOf(p, box));
            }
        }

        page.RestoreGraphicsState();
        page.SetFigureBoundingBox(x, y, w, h);
        page.AddEMC();

        foreach (Point p in linked) {
            page.SaveGraphicsState();
            StructElement link = page.BeginLink();
            page.AddBDC(StructElem.FIGURE, null, p.name);
            float[] box = DrawNamed(page, p, xAxis, yAxis, plotLeft, right, top, plotBottom);
            page.SetFigureBoundingBox(box[0], box[1], box[2], box[3]);
            page.AddEMC();
            page.EndLink(link);
            page.RestoreGraphicsState();
            Annotation annotation = LinkOf(p, box);
            annotation.linkElement = link;
            page.AddAnnotation(annotation);
        }
        return new float[] {x + w, y + h};
    }

    // Draws a highlighted point and its name, and returns the box of the two:
    // x, y, w, h.
    private float[] DrawNamed(Page page, Point p, ChartScale.Axis xAxis, ChartScale.Axis yAxis,
            float left, float right, float top, float bottom) {
        float size = f2.GetSize();
        float ascent = f2.GetAscent(size);
        float big = 0.42f * size;
        float px = XAt(p.x, xAxis, left, right);
        float py = YAt(p.y, yAxis, top, bottom);
        int color = (p.series >= 0) ? SeriesColor(p.series) : highlightColor;
        page.SetBrushColor(color);
        if (p.series >= 0) {
            DrawShape(page, p.series % SHAPES, px, py, big, true);
        } else {
            page.FillCircle(px, py, big);
        }
        // A name of two fifths of the plot at most, and in the plot
        String name = ChartScale.Shorten(f1, size, p.name, 0.4f * (right - left));
        float width = f1.StringWidth(size, name);
        float lx = px + big + size / 3f;
        if (lx + width > right) {
            lx = px - big - size / 3f - width;  // At the left, not past the plot
        }
        lx = Math.Max(lx, left);
        float baseline = py + ascent / 2f;
        // On a patch of the panel's color, which the dots and the grid do not
        // show through
        page.SetBrushColor(panelColor >= 0 ? panelColor : Color.white);
        page.FillRect(lx - 1f, baseline - f1.GetAscent(size) - 1f,
                width + 2f, f1.GetAscent(size) + f1.GetDescent(size) + 2f);
        // The name of a point of a series in ink, which reads on the panel
        // whatever the color of the series
        page.DrawString(f1, size, name, lx, baseline,
                Util.ToRGB((p.series >= 0) ? SimpleBarChart.INK : highlightColor), null);
        float x1 = Math.Min(px - big, lx);
        float y1 = Math.Min(py - big, baseline - f1.GetAscent(size));
        float x2 = Math.Max(px + big, lx + width);
        float y2 = Math.Max(py + big, baseline + f1.GetDescent(size));
        return new float[] {x1, y1, x2 - x1, y2 - y1};
    }

    // The link annotation of a point, over the box of its dot and its name,
    // described by its name.
    private static Annotation LinkOf(Point p, float[] box) {
        return new Annotation(
                Annotation.Link,
                box[0],
                box[1],
                box[0] + box[2],
                box[1] + box[3],
                null,   // Vertices
                null,   // Fill Color
                0f,     // Opacity
                null,   // Title
                null,   // Contents
                p.uri,
                null,
                null,
                null,
                p.name);
    }

    // Tells if the point is on the plot: a point outside a range that is set
    // is not drawn.
    private static bool OnPlot(Point p, ChartScale.Axis xAxis, ChartScale.Axis yAxis) {
        return ChartScale.OnAxis(p.x, xAxis) && ChartScale.OnAxis(p.y, yAxis);
    }

    // The x on the page of a value of the horizontal axis.
    private static float XAt(float v, ChartScale.Axis axis, float left, float right) {
        return left + ChartScale.Scaled(v - axis.min, right - left, axis.max - axis.min);
    }

    // The y on the page of a value of the vertical axis.
    private static float YAt(float v, ChartScale.Axis axis, float top, float bottom) {
        return bottom - ChartScale.Scaled(v - axis.min, bottom - top, axis.max - axis.min);
    }

    // The point of the trend line at x, moved along the line to the top or
    // the bottom of the plot when it is past them.
    private static double[] Clip(double x, double y, double slope, double intercept, ChartScale.Axis axis) {
        if (slope != 0 && y < axis.min) {
            return new double[] {(axis.min - intercept) / slope, axis.min};
        }
        if (slope != 0 && y > axis.max) {
            return new double[] {(axis.max - intercept) / slope, axis.max};
        }
        return new double[] {x, y};
    }
}   // End of SimpleScatterChart.cs
}   // End of namespace PDFjet.NET
