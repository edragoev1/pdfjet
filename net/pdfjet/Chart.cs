/*
 * Chart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// XY chart renderer for PDF pages. See Example_09.
/// </summary>
public class Chart : IDrawable {
    private float w = 300f;
    private float h = 200f;

    // Outer chart rectangle (x1,y1 = top-left, clockwise)
    private float x1, y1, x2, y2, x3, y3, x4, y4;
    // Inner plot area (x5,y5 = top-left, clockwise)
    private float x5, y5, x6, y6, x7, y7, x8, y8;

    // The axis ranges that SetXAxisMinMax and SetYAxisMinMax set, used when
    // they have grid lines
    private float manualXMin;
    private float manualXMax;
    private int manualXGridLines = 0;
    private float manualYMin;
    private float manualYMax;
    private int manualYGridLines = 0;

    // The axis ranges of the chart being drawn: the ones set, or the ranges of
    // the data rounded
    private float xMin;
    private float xMax;
    private float yMin;
    private float yMax;
    private int xAxisGridLines;
    private int yAxisGridLines;

    private String title = "";
    private String subtitle = "";
    private String xAxisTitle = "";
    private String yAxisTitle = "";

    private bool drawHorizontalGridLines = true;
    private bool drawVerticalGridLines = true;
    private bool drawXAxisLabels = true;
    private bool drawYAxisLabels = true;

    // Grid line styling (width 0 = the thinnest line, pattern default = dotted)
    private int gridLineColor = Color.black;
    private float horizontalGridLineWidth = 0f;
    private float verticalGridLineWidth = 0f;
    private String horizontalGridLineDashPattern = "[1 1] 0";
    private String verticalGridLineDashPattern = "[1 1] 0";

    private float axisLineWidth = 0.5f;
    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    // Label number formatting
    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;

    // f1 = chart title font, f2 = axis title/label font
    private Font f1 = null;
    private Font f2 = null;

    private readonly List<Series> series = new List<Series>();
    private bool drawLegend = true;
    private String altDescription = null;

    internal static readonly int[] DEFAULT_PALETTE = {
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    };

    /// <summary>
    /// Creates an XY chart.
    /// </summary>
    /// <param name="f1">font for the chart title.</param>
    /// <param name="f2">font for axis titles and labels.</param>
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /// <summary>
    ///  Sets the chart title.
    /// </summary>
    public Chart SetTitle(String title) {
        this.title = title;
        return this;
    }

    /// <summary>Sets the subtitle, written in gray under the title in the second font.</summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this Chart object.</returns>
    public Chart SetSubtitle(String subtitle) {
        this.subtitle = subtitle;
        return this;
    }

    /// <summary>
    ///  Sets the X axis title.
    /// </summary>
    public Chart SetXAxisTitle(String title) {
        this.xAxisTitle = title;
        return this;
    }

    /// <summary>
    ///  Sets the Y axis title.
    /// </summary>
    public Chart SetYAxisTitle(String title) {
        this.yAxisTitle = title;
        return this;
    }

    /// <summary>
    /// Adds a series and returns it, to add its points and set its line and
    /// marker. A series without a stroke color has the next color of the
    /// palette. The legend lists the series that have a name.
    /// </summary>
    /// <param name="name">the series name, shown in the legend; empty for none.</param>
    /// <returns>the new Series object.</returns>
    public Series AddSeries(String name) {
        Series s = new Series(name);
        series.Add(s);
        return s;
    }

    /// <summary>
    /// Sets whether the legend is drawn. The legend lists the series that have
    /// a name, under the title, each with its line or its marker.
    /// </summary>
    /// <param name="drawLegend">true to draw the legend.</param>
    /// <returns>this Chart object.</returns>
    public Chart SetDrawLegend(bool drawLegend) {
        this.drawLegend = drawLegend;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of the chart, which a screen reader reads
    /// in a PDF/UA document, where the chart is a figure. The default is the
    /// title, or "Chart" without one. Describe what the chart shows.
    /// </summary>
    /// <param name="altDescription">the alternate description.</param>
    /// <returns>this Chart object.</returns>
    public Chart SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    ///  Sets the top-left position. Returns this for chaining.
    /// </summary>
    public Chart SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    ///  Sets the chart dimensions.
    /// </summary>
    public Chart SetSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /// <summary>
    ///  Sets the minimum number of decimal places in the axis labels. The labels
    ///  of an axis have at least the decimal places of its step, so an axis with
    ///  a whole number step has whole number labels. The default is 0.
    /// </summary>
    public Chart SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /// <summary>
    ///  Sets the maximum number of decimal places in the axis labels. The default is 2.
    /// </summary>
    public Chart SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /// <summary>
    ///  Toggles drawing of horizontal grid lines.
    /// </summary>
    public Chart SetDrawHorizontalGridLines(bool drawHorizontalGridLines) {
        this.drawHorizontalGridLines = drawHorizontalGridLines;
        return this;
    }

    /// <summary>
    ///  Toggles drawing of vertical grid lines.
    /// </summary>
    public Chart SetDrawVerticalGridLines(bool drawVerticalGridLines) {
        this.drawVerticalGridLines = drawVerticalGridLines;
        return this;
    }

    /// <summary>Sets whether the x axis labels are drawn.</summary>
    public Chart SetDrawXAxisLabels(bool drawXAxisLabels) {
        this.drawXAxisLabels = drawXAxisLabels;
        return this;
    }

    /// <summary>
    ///  Toggles drawing of Y axis labels.
    /// </summary>
    public Chart SetDrawYAxisLabels(bool drawYAxisLabels) {
        this.drawYAxisLabels = drawYAxisLabels;
        return this;
    }

    /// <summary>
    ///  Sets the width of the axis lines, along the left and the bottom sides of the plot area. The default is 0.5; 0 hides them.
    /// </summary>
    public Chart SetAxisLineWidth(float width) {
        this.axisLineWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the width of the outer chart border. A width of 0, the default, hides it.
    /// </summary>
    public Chart SetChartBorderWidth(float width) {
        this.chartBorderWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the width of the plot area border. A width of 0, the default, hides it.
    /// </summary>
    public Chart SetInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the width of the horizontal grid lines. A width of 0 draws the thinnest line a viewer shows; SetDrawHorizontalGridLines(false) hides them.
    /// </summary>
    public Chart SetHorizontalGridLineWidth(float width) {
        this.horizontalGridLineWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the width of the vertical grid lines. A width of 0 draws the thinnest line a viewer shows; SetDrawVerticalGridLines(false) hides them.
    /// </summary>
    public Chart SetVerticalGridLineWidth(float width) {
        this.verticalGridLineWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the horizontal grid line dash pattern (e.g. "[1 1] 0").
    /// </summary>
    public Chart SetHorizontalGridLineDashPattern(String pattern) {
        this.horizontalGridLineDashPattern = pattern;
        return this;
    }

    /// <summary>
    ///  Sets the vertical grid line dash pattern (e.g. "[1 1] 0").
    /// </summary>
    public Chart SetVerticalGridLineDashPattern(String pattern) {
        this.verticalGridLineDashPattern = pattern;
        return this;
    }

    /// <summary>Sets the color of the grid lines. The default is black.</summary>
    /// <param name="color">the color as a 0xRRGGBB value, for example Color.lightgray.</param>
    /// <returns>this Chart object.</returns>
    public Chart SetGridLineColor(int color) {
        this.gridLineColor = color;
        return this;
    }

    /// <summary>
    /// Draws this chart on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the bottom-right corner coordinates [x, y].</returns>
    public float[] DrawOn(Page page) {
        // Guard against null or empty data
        if (page == null || !HasPoints()) {    // Measured, or nothing to draw
            return new float[] { this.x1 + this.w, this.y1 + this.h };
        }

        // Compute outer rectangle corners
        x2 = x1 + w;
        y2 = y1;
        x3 = x2;
        y3 = y1 + h;
        x4 = x1;
        y4 = y3;

        // The axis ranges are computed again for every drawing, from the data
        // the chart has then.
        SetXAxisRange();
        SetYAxisRange();

        // The chart is one figure, described by its alternate description.
        page.AddBDC(StructElem.FIGURE, null, AltDescription());
        // The pen, the brush and the dash pattern of the caller are kept,
        // as a Stamp and a CalendarMonth keep them: the chart set them to its
        // own and then to the default of a page, which is not what the caller
        // had.
        page.SaveGraphicsState();

        // Draw chart title (centered, top), the subtitle and then the legend under it
        float titleBaseline = y1 + 1.5f * f1.GetBodyHeight(f1.GetSize());
        float subtitleHeight = subtitle.Length == 0 ? 0f : f2.GetBodyHeight(f2.GetSize());
        page.SetBrushColor(Color.black);
        page.DrawString(
                f1,
                f1.GetSize(),
                title,
                x1 + ((w - f1.StringWidth(title)) / 2),
                titleBaseline);
        if (subtitle.Length > 0) {
            page.DrawString(
                    f2,
                    f2.GetSize(),
                    subtitle,
                    x1 + ((w - f2.StringWidth(subtitle)) / 2),
                    titleBaseline + subtitleHeight, Util.ToRGB(Color.dimgray), null);
        }
        bool legend = drawLegend && HasSeriesNames();
        if (legend) {
            DrawLegend(page, titleBaseline + subtitleHeight + 1.5f * f2.GetBodyHeight(f2.GetSize()));
        }

        // Compute margins and inner plot area
        float topMargin = 2.5f * f1.GetBodyHeight(f1.GetSize()) + subtitleHeight + (legend ? 1.5f * f2.GetBodyHeight(f2.GetSize()) : 0f);
        float leftMargin = GetLongestAxisYLabelWidth() + 2f * f2.GetBodyHeight(f2.GetSize());
        float rightMargin = 2f * f2.GetBodyHeight(f2.GetSize());
        float bottomMargin = 2.5f * f2.GetBodyHeight(f2.GetSize());

        x5 = x1 + leftMargin;
        y5 = y1 + topMargin;
        x6 = x2 - rightMargin;
        y6 = y5;
        x7 = x6;
        y7 = y3 - bottomMargin;
        x8 = x5;
        y8 = y7;

        DrawChartBorder(page);
        DrawInnerBorder(page);

        if (drawHorizontalGridLines) {
            DrawHorizontalGrid(page);
        }
        if (drawVerticalGridLines) {
            DrawVerticalGrid(page);
        }
        if (axisLineWidth > 0f) {
            DrawAxisLines(page);
        }

        if (drawXAxisLabels) {
            DrawXAxisLabels(page);
        }
        if (drawYAxisLabels) {
            DrawYAxisLabels(page);
        }

        // Defensive copy so the user's data is never mutated. A point added by
        // its coordinates has the marker the series has now.
        List<List<Point>> plotData = new List<List<Point>>(series.Count);
        foreach (Series s in series) {
            List<Point> copy = new List<Point>(s.points.Count);
            for (int i = 0; i < s.points.Count; i++) {
                Point point = new Point(s.points[i]);
                if (s.seriesMarker[i]) {
                    point.shape = s.shape;
                    point.r = s.radius;
                }
                copy.Add(point);
            }
            plotData.Add(copy);
        }

        // Translate data coordinates to page coordinates (on the copies)
        foreach (List<Point> points in plotData) {
            foreach (Point point in points) {
                point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin);
                point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                if (point.GetURIAction() != null) {
                    page.AddAnnotation(new Annotation(
                            Annotation.Link,
                            point.x - point.r,
                            point.y - point.r,
                            point.x + point.r,
                            point.y + point.r,
                            null,   // Vertices
                            null,   // Fill Color
                            0f,     // Opacity
                            null,   // Title
                            null,   // Contents
                            point.GetURIAction(),
                            null,
                            null,
                            null,
                            null));
                }
            }
        }

        DrawPathsAndPoints(page, plotData);

        // Draw Y axis title (rotated 90 degrees)
        page.SetBrushColor(Color.black);
        page.SetTextRotation(-90);
        page.DrawString(
                f2,
                f2.GetSize(),
                yAxisTitle,
                x1 + f2.GetBodyHeight(f2.GetSize()),
                y8 - ((y8 - y5) - f2.StringWidth(yAxisTitle)) / 2);

        // Draw X axis title
        page.SetTextRotation(0);
        page.SetBrushColor(Color.black);
        page.DrawString(
                f2,
                f2.GetSize(),
                xAxisTitle,
                x5 + ((x6 - x5) - f2.StringWidth(xAxisTitle)) / 2,
                y4 - f2.GetBodyHeight(f2.GetSize()) / 2);

        page.RestoreGraphicsState();
        page.AddEMC();

        return new float[] {this.x1 + this.w, this.y1 + this.h};
    }

    // Returns the alternate description, or the title when none is set.
    private String AltDescription() {
        if (!String.IsNullOrEmpty(altDescription)) {
            return altDescription;
        }
        return (title.Length == 0) ? "Chart" : title;
    }

    /// <summary>
    ///  Returns true if at least one series has points.
    /// </summary>
    private bool HasPoints() {
        foreach (Series s in series) {
            if (s.points.Count > 0) {
                return true;
            }
        }
        return false;
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

    /// <summary>Returns the color of the series at the index: its own or the palette's.</summary>
    private float[] SeriesColor(Series s, int index) {
        return s.strokeColor != null ? s.strokeColor : ToFloatArray(DEFAULT_PALETTE[index % DEFAULT_PALETTE.Length]);
    }

    /// <summary>
    /// Draws the legend centered on the chart: the line of each named series
    /// that draws its path, its marker when it has one, and its name.
    /// </summary>
    private void DrawLegend(Page page, float baseline) {
        float ascent = f2.GetAscent();
        float sample = 2f * ascent;     // the width of the line or the marker
        float gap = ascent / 2f;
        float width = 0f;
        int entries = 0;
        foreach (Series s in series) {
            if (s.name.Length > 0) {
                width += sample + gap + f2.StringWidth(s.name);
                entries++;
            }
        }
        width += (entries - 1) * f2.GetBodyHeight(f2.GetSize());
        float x = x1 + (w - width) / 2f;
        for (int j = 0; j < series.Count; j++) {
            Series s = series[j];
            if (s.name.Length == 0) {
                continue;
            }
            float[] color = SeriesColor(s, j);
            float yMid = baseline - ascent / 2f;
            page.SetPenColor(color);
            if (s.drawPath) {
                page.SetPenWidth(s.strokeWidth);
                page.SetStrokeDashPattern(s.strokeDashPattern);
                page.DrawLine(x, yMid, x + sample, yMid);
            }
            if (s.shape != Shape.INVISIBLE) {
                page.SetPenWidth(1f);
                page.SetDefaultStrokeDashPattern();
                page.DrawPoint(new Point(x + sample / 2f, yMid).SetShape(s.shape).SetRadius(s.radius));
            }
            x += sample + gap;
            page.SetBrushColor(Color.black);
            page.DrawString(f2, f2.GetSize(), s.name, x, baseline);
            x += f2.StringWidth(s.name) + f2.GetBodyHeight(f2.GetSize());
        }
        page.SetDefaultPenWidth();
        page.SetDefaultStrokeDashPattern();
    }

    /// <summary>
    /// Formats a label with minDigits to maxDigits decimal places, rounding the
    /// exact value half to even. The label has a "." decimal separator and no
    /// grouping whatever the default locale, and a value that rounds to zero
    /// has no minus sign.
    /// </summary>
    internal static String Format(float value, int minDigits, int maxDigits) {
        // A minimum above the maximum is lowered to it, as in NumberFormat
        maxDigits = Math.Max(maxDigits, 0);
        minDigits = Math.Min(Math.Max(minDigits, 0), maxDigits);
        NumberFormat nf = NumberFormat.GetInstance();
        nf.SetMaximumFractionDigits(maxDigits);
        nf.SetMinimumFractionDigits(minDigits);
        return nf.Format(value);
    }

    /// <summary>
    /// Returns the number of decimal places, at most maxDigits, that write the
    /// axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
    /// </summary>
    internal static int FractionDigitsOf(float step, int maxDigits) {
        for (int digits = 0; digits < maxDigits; digits++) {
            double scaled = step * Math.Pow(10, digits);
            if (Math.Abs(scaled - Math.Round(scaled)) < 1e-4) {
                return digits;
            }
        }
        return Math.Max(maxDigits, 0);
    }

    /// <summary>Formats the label of an axis with the specified step.</summary>
    private String Format(float value, float step) {
        int digits = Math.Max(minFractionDigits, FractionDigitsOf(step, maxFractionDigits));
        return Format(value, digits, maxFractionDigits);
    }

    /// <summary>
    ///  Returns the width of the widest Y axis label (for left margin).
    /// </summary>
    private float GetLongestAxisYLabelWidth() {
        float step = (yMax - yMin) / yAxisGridLines;
        float minLabelWidth =
                f2.StringWidth(Format(yMin, step) + "0");
        float maxLabelWidth =
                f2.StringWidth(Format(yMax, step) + "0");
        if (maxLabelWidth > minLabelWidth) {
            return maxLabelWidth;
        }
        return minLabelWidth;
    }

    /// <summary>
    /// Sets the X axis range of the drawing: the one set with SetXAxisMinMax,
    /// or the range of the data rounded to "nice" values, with its grid lines.
    /// </summary>
    private void SetXAxisRange() {
        if (manualXGridLines > 0) {
            xMin = manualXMin;
            xMax = manualXMax;
            xAxisGridLines = manualXGridLines;
        } else {
            xMin = System.Single.MaxValue;
            xMax = System.Single.MinValue;
            foreach (Series s in series) {
                foreach (Point point in s.points) {
                    if (point.x < xMin) {
                        xMin = point.x;
                    }
                    if (point.x > xMax) {
                        xMax = point.x;
                    }
                }
            }
        }
        // Guard against flat data before rounding, so the range has grid lines
        if (xMax == xMin) { xMax = xMin + 1f; }
        if (manualXGridLines == 0) {
            Round round = RoundMaxAndMinValues(xMax, xMin);
            xMax = round.maxValue;
            xMin = round.minValue;
            xAxisGridLines = round.numOfGridLines;
        }
    }

    /// <summary>
    /// Sets the Y axis range of the drawing: the one set with SetYAxisMinMax,
    /// or the range of the data rounded to "nice" values, with its grid lines.
    /// </summary>
    private void SetYAxisRange() {
        if (manualYGridLines > 0) {
            yMin = manualYMin;
            yMax = manualYMax;
            yAxisGridLines = manualYGridLines;
        } else {
            yMin = System.Single.MaxValue;
            yMax = System.Single.MinValue;
            foreach (Series s in series) {
                foreach (Point point in s.points) {
                    if (point.y < yMin) {
                        yMin = point.y;
                    }
                    if (point.y > yMax) {
                        yMax = point.y;
                    }
                }
            }
        }
        // Guard against flat data before rounding, so the range has grid lines
        if (yMax == yMin) { yMax = yMin + 1f; }
        if (manualYGridLines == 0) {
            Round round = RoundMaxAndMinValues(yMax, yMin);
            yMax = round.maxValue;
            yMin = round.minValue;
            yAxisGridLines = round.numOfGridLines;
        }
    }

    /// <summary>
    ///  Draws the outer chart border, unless its width is 0.
    /// </summary>
    private void DrawChartBorder(Page page) {
        if (chartBorderWidth <= 0f) {
            return;
        }
        page.SetPenWidth(chartBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x1, y1);
        page.LineTo(x2, y2);
        page.LineTo(x3, y3);
        page.LineTo(x4, y4);
        page.ClosePath();
    }

    /// <summary>
    ///  Draws the inner plot area border, unless its width is 0.
    /// </summary>
    private void DrawInnerBorder(Page page) {
        if (innerBorderWidth <= 0f) {
            return;
        }
        page.SetPenWidth(innerBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x5, y5);
        page.LineTo(x6, y6);
        page.LineTo(x7, y7);
        page.LineTo(x8, y8);
        page.ClosePath();
    }

    /// <summary>
    ///  Draws the axis lines along the left and the bottom sides of the plot area.
    /// </summary>
    private void DrawAxisLines(Page page) {
        page.SetPenWidth(axisLineWidth);
        page.SetPenColor(Color.black);
        page.SetDefaultStrokeDashPattern();
        page.DrawLine(x5, y5, x5, y8);
        page.DrawLine(x8, y8, x6, y8);
    }

    /// <summary>
    ///  Draws horizontal grid lines across the plot area, one at each label.
    /// </summary>
    private void DrawHorizontalGrid(Page page) {
        page.SetPenWidth(horizontalGridLineWidth);
        page.SetPenColor(gridLineColor);
        page.SetStrokeDashPattern(horizontalGridLineDashPattern);
        float x = x8;
        float y = y8;
        float step = (y8 - y5) / yAxisGridLines;
        for (int i = 0; i <= yAxisGridLines; i++) {
            page.DrawLine(x, y, x6, y);
            y -= step;
        }
    }

    /// <summary>
    ///  Draws vertical grid lines across the plot area, one at each label.
    /// </summary>
    private void DrawVerticalGrid(Page page) {
        page.SetPenWidth(verticalGridLineWidth);
        page.SetPenColor(gridLineColor);
        page.SetStrokeDashPattern(verticalGridLineDashPattern);
        float x = x5;
        float y = y5;
        float step = (x6 - x5) / xAxisGridLines;
        for (int i = 0; i <= xAxisGridLines; i++) {
            page.DrawLine(x, y, x, y8);
            x += step;
        }
    }

    /// <summary>
    ///  Draws X axis labels (one per grid line interval).
    /// </summary>
    private void DrawXAxisLabels(Page page) {
        float x = x5;
        float y = y8 + f2.GetBodyHeight(f2.GetSize());
        float step = (x6 - x5) / xAxisGridLines;
        float valueStep = (xMax - xMin) / xAxisGridLines;
        page.SetBrushColor(Color.black);
        for (int i = 0; i < (xAxisGridLines + 1); i++) {
            String label = Format(xMin + valueStep * i, valueStep);
            page.DrawString(f2, f2.GetSize(), label, x - (f2.StringWidth(label) / 2), y);
            x += step;
        }
    }

    /// <summary>
    ///  Draws Y axis labels (one per grid line interval).
    /// </summary>
    private void DrawYAxisLabels(Page page) {
        float x = x5 - GetLongestAxisYLabelWidth();
        float y = y8 + f2.GetAscent() / 3;
        float step = (y8 - y5) / yAxisGridLines;
        float valueStep = (yMax - yMin) / yAxisGridLines;
        page.SetBrushColor(Color.black);
        for (int i = 0; i < (yAxisGridLines + 1); i++) {
            String label = Format(yMin + valueStep * i, valueStep);
            page.DrawString(f2, f2.GetSize(), label, x, y);
            y -= step;
        }
    }

    /// <summary>Converts a 0xRRGGBB color to red, green and blue values between 0.0 and 1.0.</summary>
    private float[] ToFloatArray(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        return new float[] {r, g, b};
    }

    /// <summary>
    /// Draws the line of each series that draws its path, then the markers of
    /// its points: a point without a stroke color in the color of the series.
    /// </summary>
    private void DrawPathsAndPoints(
            Page page, List<List<Point>> plotData) {
        for (int j = 0; j < series.Count; j++) {
            Series s = series[j];
            List<Point> points = plotData[j];
            if (points.Count == 0) {
                continue;
            }
            float[] color = SeriesColor(s, j);
            if (s.drawPath) {
                page.SetPenColor(color);
                page.SetPenWidth(s.strokeWidth);
                page.SetStrokeDashPattern(s.strokeDashPattern);
                page.DrawPath(points, PathOperator.STROKE);
            }
            foreach (Point point in points) {
                if (point.shape != Shape.INVISIBLE) {
                    page.SetPenColor(point.strokeColor != null ? point.strokeColor : color);
                    page.SetPenWidth(point.strokeWidth);
                    page.SetDefaultStrokeDashPattern();
                    page.SetBrushColor(point.fillColor);
                    page.DrawPoint(point);
                }
            }
        }
    }

    /// <summary>
    /// Rounds axis range to "nice" values for clean grid lines.
    /// Uses the span (max - min) to support negative values and
    /// zero crossings. Rounds max up and min down to step multiples.
    /// </summary>
    internal static Round RoundMaxAndMinValues(float maxValue, float minValue) {
        float span = maxValue - minValue;
        if (span <= 0f) { span = 1f; }  // guard against flat data

        int exponent = (int) Math.Floor(Math.Log(span) / Math.Log(10));
        float normalizedSpan = span * (float) Math.Pow(10, -exponent);

        // Snap span up to a "nice" value with paired grid line count
        float niceSpan;
        int numOfGridLines;

        if      (normalizedSpan > 9.00f) { niceSpan = 10.0f; numOfGridLines = 10; }
        else if (normalizedSpan > 8.00f) { niceSpan =  9.00f; numOfGridLines =  9; }
        else if (normalizedSpan > 7.00f) { niceSpan =  8.00f; numOfGridLines =  8; }
        else if (normalizedSpan > 6.00f) { niceSpan =  7.00f; numOfGridLines =  7; }
        else if (normalizedSpan > 5.00f) { niceSpan =  6.00f; numOfGridLines =  6; }
        else if (normalizedSpan > 4.00f) { niceSpan =  5.00f; numOfGridLines =  5; }
        else if (normalizedSpan > 3.50f) { niceSpan =  4.00f; numOfGridLines =  8; }
        else if (normalizedSpan > 3.00f) { niceSpan =  3.50f; numOfGridLines =  7; }
        else if (normalizedSpan > 2.50f) { niceSpan =  3.00f; numOfGridLines =  6; }
        else if (normalizedSpan > 2.00f) { niceSpan =  2.50f; numOfGridLines =  5; }
        else if (normalizedSpan > 1.75f) { niceSpan =  2.00f; numOfGridLines =  8; }
        else if (normalizedSpan > 1.50f) { niceSpan =  1.75f; numOfGridLines =  7; }
        else if (normalizedSpan > 1.25f) { niceSpan =  1.50f; numOfGridLines =  6; }
        else if (normalizedSpan > 1.00f) { niceSpan =  1.25f; numOfGridLines =  5; }
        else                             { niceSpan =  1.00f; numOfGridLines = 10; }

        float step = niceSpan * (float) Math.Pow(10, exponent) / numOfGridLines;

        Round round = new Round();

        // Round max up, min down to nearest step multiple
        round.maxValue = (float) Math.Ceiling(maxValue / step) * step;
        round.minValue = (float) Math.Floor(minValue / step) * step;

        round.numOfGridLines = (int) Math.Round((round.maxValue - round.minValue) / step);

        return round;
    }

    /// <summary>
    /// Sets the X axis range and its number of grid lines. With 0 grid lines,
    /// the default, the range is the range of the data rounded to "nice"
    /// values, and xMin and xMax are not used; a negative number is taken as 0.
    /// </summary>
    /// <returns>this Chart object.</returns>
    public Chart SetXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.manualXMin = xMin;
        this.manualXMax = xMax;
        this.manualXGridLines = Math.Max(0, xAxisGridLines);
        return this;
    }

    /// <summary>
    /// Sets the Y axis range and its number of grid lines. With 0 grid lines,
    /// the default, the range is the range of the data rounded to "nice"
    /// values, and yMin and yMax are not used; a negative number is taken as 0.
    /// </summary>
    /// <returns>this Chart object.</returns>
    public Chart SetYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.manualYMin = yMin;
        this.manualYMax = yMax;
        this.manualYGridLines = Math.Max(0, yAxisGridLines);
        return this;
    }
}
}
