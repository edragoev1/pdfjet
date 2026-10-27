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
    private readonly int maxFractionDigits = 2;

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
        points.Add(new Point(x, y, ""));
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
        points.Add(new Point(x, y, name));
        return this;
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
        points.Add(new Point(x, y, name, uri));
        return this;
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
    /// the points, rounded to steps.
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
    /// points, rounded to steps.
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
    /// Sets the color of the panel behind the chart, or -1 for none. It is
    /// light gray, 0xF1F3F6, unless set.
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
        return top;
    }

    // A number of an axis, with the decimals its step needs.
    private String Label(float value, float step) {
        return ChartScale.Format(value, ChartScale.FractionDigitsOf(step, maxFractionDigits), maxFractionDigits);
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
        double sx = 0;
        double sy = 0;
        double sxx = 0;
        double sxy = 0;
        foreach (Point p in points) {
            double x = p.x;
            double y = p.y;
            sx += x;
            sy += y;
            sxx += x * x;
            sxy += x * y;
        }
        double d = n * sxx - sx * sx;
        if (d == 0) {
            return null;
        }
        double slope = (n * sxy - sx * sy) / d;
        return new double[] {slope, (sy - slope * sx) / n};
    }

    // The alternate description: the one set, or the title, the number of
    // points, the highlighted ones and the trend.
    internal String AltDescription() {
        if (altDescription.Length > 0) {
            return altDescription;
        }
        List<Point> points = Valid();
        StringBuilder sb = new StringBuilder(title);
        if (subtitle.Length > 0) {
            if (sb.Length > 0) {
                sb.Append(", ");
            }
            sb.Append(subtitle);
        }
        if (sb.Length == 0) {
            sb.Append("Scatter chart");
        }
        sb.Append(": ").Append(points.Count).Append(" points");
        if (xAxisTitle.Length > 0 && yAxisTitle.Length > 0) {
            sb.Append(", ").Append(yAxisTitle).Append(" against ").Append(xAxisTitle);
        }
        int named = 0;
        foreach (Point p in points) {
            if (p.name.Length == 0) {
                continue;
            }
            sb.Append(named == 0 ? "; " : ", ");
            sb.Append(p.name).Append(' ').Append(ChartScale.Format(p.x, 0, maxFractionDigits))
                    .Append(" and ").Append(ChartScale.Format(p.y, 0, maxFractionDigits));
            named++;
        }
        if (trendLine) {
            double[] line = Trend(points);
            if (line != null) {
                sb.Append(line[0] > 0 ? "; the trend rises" : line[0] < 0 ? "; the trend falls" : "; the trend is flat");
            }
        }
        return sb.Append('.').ToString();
    }

    /// <summary>
    /// Draws the chart on the page. A chart too small for its plot and its
    /// numbers draws nothing.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the chart.</returns>
    public float[] DrawOn(Page page) {
        List<Point> points = Valid();
        if (page == null || points.Count == 0) {     // Measured, or nothing to draw
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
        ChartScale.Axis xAxis = ChartScale.AxisOf(xs, xMin, xMax, xRangeSet);
        ChartScale.Axis yAxis = ChartScale.AxisOf(ys, yMin, yMax, yRangeSet);
        float xStep = (xAxis.max - xAxis.min) / xAxis.lines;
        float yStep = (yAxis.max - yAxis.min) / yAxis.lines;

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
        float right = x + w - pad;
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
        for (int i = 0; i <= xAxis.lines; i += every) {
            float v = xAxis.min + xStep * i;
            String label = Label(v, xStep);
            page.DrawString(f2, size, label, XAt(v, xAxis, plotLeft, right) - f2.StringWidth(size, label) / 2f,
                    plotBottom + 1.4f * size, Util.ToRGB(SimpleBarChart.MUTED), null);
        }
        if (xAxisTitle.Length > 0) {
            page.DrawString(f2, size, xAxisTitle, plotLeft + (right - plotLeft - f2.StringWidth(size, xAxisTitle)) / 2f,
                    plotBottom + 2.8f * size, Util.ToRGB(SimpleBarChart.INK), null);
        }
        if (yAxisTitle.Length > 0) {
            page.SetTextRotation(-90);
            page.DrawString(f2, size, yAxisTitle, x + pad + ascent,
                    plotBottom - (plotBottom - top - f2.StringWidth(size, yAxisTitle)) / 2f,
                    Util.ToRGB(SimpleBarChart.INK), null);
            page.SetTextRotation(0);
        }

        // The trend line, from the left of the plot to its right, within its
        // height
        double[] line = Trend(points);
        if (trendLine && line != null) {
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
            page.DrawString(f2, SubtitleSize(), label, x2 - width, y2 - lift, Util.ToRGB(TREND), null);
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
            if (p.name.Length == 0 && OnPlot(p, xAxis, yAxis)) {
                page.FillCircle(XAt(p.x, xAxis, plotLeft, right), YAt(p.y, yAxis, top, plotBottom), r);
            }
        }
        page.RestoreGraphicsState();
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
        page.SetBrushColor(highlightColor);
        page.FillCircle(px, py, big);
        float width = f1.StringWidth(size, p.name);
        float lx = px + big + size / 3f;
        if (lx + width > right) {
            lx = px - big - size / 3f - width;  // At the left, not past the plot
        }
        float baseline = py + ascent / 2f;
        // On a patch of the panel's color, which the dots and the grid do not
        // show through
        page.SetBrushColor(panelColor >= 0 ? panelColor : Color.white);
        page.FillRect(lx - 1f, baseline - f1.GetAscent(size) - 1f,
                width + 2f, f1.GetAscent(size) + f1.GetDescent(size) + 2f);
        page.DrawString(f1, size, p.name, lx, baseline, Util.ToRGB(highlightColor), null);
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
        return left + (v - axis.min) * (right - left) / (axis.max - axis.min);
    }

    // The y on the page of a value of the vertical axis.
    private static float YAt(float v, ChartScale.Axis axis, float top, float bottom) {
        return bottom - (v - axis.min) * (bottom - top) / (axis.max - axis.min);
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
