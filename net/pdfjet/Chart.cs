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

    // Data axis ranges (auto-computed if grid lines == 0)
    private float xMax = System.Single.MinValue;
    private float xMin = System.Single.MaxValue;
    private float yMax = System.Single.MinValue;
    private float yMin = System.Single.MaxValue;

    private int xAxisGridLines = 0;
    private int yAxisGridLines = 0;

    private String title = "";
    private String xAxisTitle = "";
    private String yAxisTitle = "";

    private bool drawXAxisLines = true;
    private bool drawYAxisLines = true;
    private bool drawXAxisLabels = true;
    private bool drawYAxisLabels = true;

    private bool xyChart = true;  // true = XY scatter, false = category mode

    // Grid line styling (width 0 = invisible, pattern default = dotted)
    private float hGridLineWidth = 0f;
    private float vGridLineWidth = 0f;
    private String hGridLinePattern = "[1 1] 0";
    private String vGridLinePattern = "[1 1] 0";

    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    // Label number formatting
    private NumberFormat nf = null;
    private int minFractionDigits = 2;
    private int maxFractionDigits = 2;

    // f1 = chart title font, f2 = axis title/label font
    private Font f1 = null;
    private Font f2 = null;
    private float fontSize = 8f;

    private List<List<Point>> chartData = null;

    private static readonly int[] DEFAULT_PALETTE = {
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    };
    private bool autoColors = true;

    /// <summary>
    /// Creates an XY chart.
    /// </summary>
    /// <param name="f1">font for the chart title.</param>
    /// <param name="f2">font for axis titles and labels.</param>
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
        nf = NumberFormat.GetInstance();
    }

    /// <summary>
    ///  Sets the chart title.
    /// </summary>
    public Chart SetTitle(String title) {
        this.title = title;
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
    ///  Sets the chart data (list of series, each a list of points).
    /// </summary>
    public Chart SetData(List<List<Point>> chartData) {
        this.chartData = chartData;
        return this;
    }

    /// <summary>
    ///  Returns the chart data.
    /// </summary>
    public List<List<Point>> GetData() {
        return chartData;
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
    ///  Sets the top-left position. Returns this for chaining.
    /// </summary>
    public Chart SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    ///  Sets the chart dimensions.
    /// </summary>
    public Chart SetSize(double w, double h) {
        SetSize((float) w, (float) h);
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
    ///  Sets the font size for axis labels.
    /// </summary>
    public Chart SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /// <summary>
    ///  Sets minimum decimal places for axis labels.
    /// </summary>
    public Chart SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /// <summary>
    ///  Sets maximum decimal places for axis labels.
    /// </summary>
    public Chart SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /// <summary>
    /// Calculates the slope of a trend line (OLS). See Example_09.
    /// </summary>
    /// <param name="points">the data points.</param>
    /// <returns>the slope.</returns>
    public float Slope(List<Point> points) {
        return (Covar(points) / Devsq(points) * (points.Count - 1));
    }

    /// <summary>
    /// Calculates the intercept of a trend line (OLS). See Example_09.
    /// </summary>
    /// <param name="points">the data points.</param>
    /// <param name="slope">the pre-computed slope.</param>
    /// <returns>the intercept.</returns>
    public float Intercept(List<Point> points, double slope) {
        return Intercept(points, (float) slope);
    }

    /// <summary>
    /// Calculates the intercept of a trend line (OLS). See Example_09.
    /// </summary>
    /// <param name="points">the data points.</param>
    /// <param name="slope">the pre-computed slope.</param>
    /// <returns>the intercept.</returns>
    public float Intercept(List<Point> points, float slope) {
        float[] _mean = Mean(points);
        return (_mean[1] - slope * _mean[0]);
    }

    /// <summary>
    ///  Toggles drawing of horizontal grid lines.
    /// </summary>
    public Chart SetDrawXAxisLines(bool drawXAxisLines) {
        this.drawXAxisLines = drawXAxisLines;
        return this;
    }

    /// <summary>
    ///  Toggles drawing of vertical grid lines.
    /// </summary>
    public Chart SetDrawYAxisLines(bool drawYAxisLines) {
        this.drawYAxisLines = drawYAxisLines;
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
    ///  Sets XY scatter mode (true) or category mode (false).
    /// </summary>
    public Chart SetXYChart(bool xyChart) {
        this.xyChart = xyChart;
        return this;
    }

    /// <summary>
    ///  Sets the outer chart border width (0 = invisible).
    /// </summary>
    public Chart SetChartBorderWidth(float width) {
        this.chartBorderWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the inner plot area border width (0 = invisible).
    /// </summary>
    public Chart SetInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the horizontal grid line width (0 = invisible).
    /// </summary>
    public Chart SetHGridLineWidth(float width) {
        this.hGridLineWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the vertical grid line width (0 = invisible).
    /// </summary>
    public Chart SetVGridLineWidth(float width) {
        this.vGridLineWidth = width;
        return this;
    }

    /// <summary>
    ///  Sets the horizontal grid line dash pattern (e.g. "[1 1] 0").
    /// </summary>
    public Chart SetHGridLinePattern(String pattern) {
        this.hGridLinePattern = pattern;
        return this;
    }

    /// <summary>
    ///  Sets the vertical grid line dash pattern (e.g. "[1 1] 0").
    /// </summary>
    public Chart SetVGridLinePattern(String pattern) {
        this.vGridLinePattern = pattern;
        return this;
    }

    /// <summary>
    /// Draws this chart on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the bottom-right corner coordinates [x, y].</returns>
    public float[] DrawOn(Page page) {
        // Guard against null or empty data
        if (chartData == null || chartData.Count == 0) {
            return new float[] { this.x1 + this.w, this.y1 + this.h };
        }

        nf.SetMinimumFractionDigits(minFractionDigits);
        nf.SetMaximumFractionDigits(maxFractionDigits);

        // Compute outer rectangle corners
        x2 = x1 + w;
        y2 = y1;
        x3 = x2;
        y3 = y1 + h;
        x4 = x1;
        y4 = y3;

        // Compute and round axis ranges
        SetXAxisMinAndMaxChartValues();
        SetYAxisMinAndMaxChartValues();
        RoundXAxisMinAndMaxValues();
        RoundYAxisMinAndMaxValues();

        // Guard against flat data (all same X or Y)
        if (xMax == xMin) { xMax = xMin + 1f; }
        if (yMax == yMin) { yMax = yMin + 1f; }

        // Draw chart title (centered, top)
        page.DrawString(
                f1,
                fontSize,
                title,
                x1 + ((w - f1.StringWidth(title)) / 2),
                y1 + 1.5f * f1.GetBodyHeight(f1.GetSize()));

        // Compute margins and inner plot area
        float topMargin = 2.5f * f1.GetBodyHeight(f1.GetSize());
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

        if (drawXAxisLines) {
            DrawHorizontalGridLines(page);
        }
        if (drawYAxisLines) {
            DrawVerticalGridLines(page);
        }

        if (drawXAxisLabels) {
            DrawXAxisLabels(page);
        }
        if (drawYAxisLabels) {
            DrawYAxisLabels(page);
        }

        // Defensive copy so the user's data is never mutated
        List<List<Point>> plotData = new List<List<Point>>(chartData.Count);
        foreach (List<Point> original in chartData) {
            List<Point> copy = new List<Point>(original.Count);
            foreach (Point point in original) {
                copy.Add(new Point(point));
            }
            plotData.Add(copy);
        }

        // Translate data coordinates to page coordinates (on the copies)
        foreach (List<Point> points in plotData) {
            foreach (Point point in points) {
                if (xyChart) {
                    point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin);
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                    point.strokeWidth *= (x6 - x5) / w;
                } else {
                    point.x = x5 + point.x * (x6 - x5) / w;
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                }
                if (point.GetURIAction() != null) {
                    page.AddAnnotation(new Annotation(
                            Annotation.Link,
                            point.x - point.r,
                            point.y - point.r,
                            point.x + point.r,
                            point.y + point.r,
                            null,   // Vertices
                            null,   // Fill Color
                            0f,     // Transparency
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
        page.SetTextDirection(90);
        page.DrawString(
                f2,
                fontSize,
                yAxisTitle,
                x1 + f2.GetBodyHeight(f2.GetSize()),
                y8 - ((y8 - y5) - f2.StringWidth(yAxisTitle)) / 2);

        // Draw X axis title
        page.SetTextDirection(0);
        page.SetBrushColor(Color.black);
        page.DrawString(
                f2,
                fontSize,
                xAxisTitle,
                x5 + ((x6 - x5) - f2.StringWidth(xAxisTitle)) / 2,
                y4 - f2.GetBodyHeight(f2.GetSize()) / 2);

        page.SetDefaultStrokeWidth();
        page.SetDefaultStrokeDashPattern();
        page.SetPenColor(Color.black);

        return new float[] {this.x1 + this.w, this.y1 + this.h};
    }

    /// <summary>
    ///  Returns the width of the widest Y axis label (for left margin).
    /// </summary>
    private float GetLongestAxisYLabelWidth() {
        float minLabelWidth =
                f2.StringWidth(nf.Format(yMin) + "0");
        float maxLabelWidth =
                f2.StringWidth(nf.Format(yMax) + "0");
        if (maxLabelWidth > minLabelWidth) {
            return maxLabelWidth;
        }
        return minLabelWidth;
    }

    /// <summary>
    ///  Scans all data points to find X axis min/max (skipped if manual).
    /// </summary>
    private void SetXAxisMinAndMaxChartValues() {
        if (xAxisGridLines != 0) {
            return;
        }
        foreach (List<Point> points in chartData) {
            foreach (Point point in points) {
                if (point.x < xMin) {
                    xMin = point.x;
                }
                if (point.x > xMax) {
                    xMax = point.x;
                }
            }
        }
    }

    /// <summary>
    ///  Scans all data points to find Y axis min/max (skipped if manual).
    /// </summary>
    private void SetYAxisMinAndMaxChartValues() {
        if (yAxisGridLines != 0) {
            return;
        }
        foreach (List<Point> points in chartData) {
            foreach (Point point in points) {
                if (point.y < yMin) {
                    yMin = point.y;
                }
                if (point.y > yMax) {
                    yMax = point.y;
                }
            }
        }
    }

    /// <summary>
    ///  Rounds X axis range to "nice" values and sets grid line count.
    /// </summary>
    private void RoundXAxisMinAndMaxValues() {
        Round round = RoundMaxAndMinValues(xMax, xMin);
        xMax = round.maxValue;
        xMin = round.minValue;
        xAxisGridLines = round.numOfGridLines;
    }

    /// <summary>
    ///  Rounds Y axis range to "nice" values and sets grid line count.
    /// </summary>
    private void RoundYAxisMinAndMaxValues() {
        Round round = RoundMaxAndMinValues(yMax, yMin);
        yMax = round.maxValue;
        yMin = round.minValue;
        yAxisGridLines = round.numOfGridLines;
    }

    /// <summary>
    ///  Draws the outer chart border.
    /// </summary>
    private void DrawChartBorder(Page page) {
        page.SetPenWidth(chartBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x1, y1);
        page.LineTo(x2, y2);
        page.LineTo(x3, y3);
        page.LineTo(x4, y4);
        page.ClosePath();
    }

    /// <summary>
    ///  Draws the inner plot area border.
    /// </summary>
    private void DrawInnerBorder(Page page) {
        page.SetPenWidth(innerBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x5, y5);
        page.LineTo(x6, y6);
        page.LineTo(x7, y7);
        page.LineTo(x8, y8);
        page.ClosePath();
    }

    /// <summary>
    ///  Draws horizontal grid lines across the plot area.
    /// </summary>
    private void DrawHorizontalGridLines(Page page) {
        page.SetPenWidth(hGridLineWidth);
        page.SetPenColor(Color.black);
        page.SetStrokeDashPattern(hGridLinePattern);
        float x = x8;
        float y = y8;
        float step = (y8 - y5) / yAxisGridLines;
        for (int i = 0; i < yAxisGridLines; i++) {
            page.DrawLine(x, y, x6, y);
            y -= step;
        }
    }

    /// <summary>
    ///  Draws vertical grid lines across the plot area.
    /// </summary>
    private void DrawVerticalGridLines(Page page) {
        page.SetPenWidth(vGridLineWidth);
        page.SetPenColor(Color.black);
        page.SetStrokeDashPattern(vGridLinePattern);
        float x = x5;
        float y = y5;
        float step = (x6 - x5) / xAxisGridLines;
        for (int i = 0; i < xAxisGridLines; i++) {
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
        page.SetBrushColor(Color.black);
        for (int i = 0; i < (xAxisGridLines + 1); i++) {
            String label = nf.Format(xMin + ((xMax - xMin) / xAxisGridLines) * i);
            page.DrawString(f2, fontSize, label, x - (f2.StringWidth(label) / 2), y);
            x += step;
        }
    }

    /// <summary>
    ///  Draws Y axis labels (one per grid line interval).
    /// </summary>
    private void DrawYAxisLabels(Page page) {
        float x = x5 - GetLongestAxisYLabelWidth();
        float y = y8 + f2.GetAscent(fontSize) / 3;
        float step = (y8 - y5) / yAxisGridLines;
        page.SetBrushColor(Color.black);
        for (int i = 0; i < (yAxisGridLines + 1); i++) {
            String label = nf.Format(yMin + ((yMax - yMin) / yAxisGridLines) * i);
            page.DrawString(f2, fontSize, label, x, y);
            y -= step;
        }
    }

    /// <summary>Sets whether the series colors are assigned automatically.</summary>
    public Chart SetAutoColors(bool autoColors) {
        this.autoColors = autoColors;
        return this;
    }

    /// <summary>Converts a 0xRRGGBB color to red, green and blue values between 0.0 and 1.0.</summary>
    public float[] ToFloatArray(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        return new float[] {r, g, b};
    }

    /// <summary>
    ///  Draws connecting paths, point markers, and point text.
    /// </summary>
    private void DrawPathsAndPoints(
            Page page, List<List<Point>> chartData) {
        int seriesIndex = 0;
        foreach (List<Point> points in chartData) {
            Point p0 = points[0];
            if (p0.drawPath) {
                if (autoColors && p0.strokeColor == null) {
                    int index = seriesIndex % DEFAULT_PALETTE.Length;
                    p0.strokeColor = ToFloatArray(DEFAULT_PALETTE[index]);
                }
                page.SetPenColor(p0.strokeColor);
                page.SetPenWidth(p0.strokeWidth);
                page.SetStrokeDashPattern(p0.strokeDashPattern);
                page.DrawPath(points, PathOperator.Stroke);
                if (p0.GetText() != null) {
                    page.SetBrushColor(p0.GetTextColor());
                    page.SetTextDirection(p0.GetTextDirection());
                    page.DrawString(
                            f2,
                            null,
                            fontSize,
                            p0.GetText(),
                            p0.x + (p0.strokeWidth - f2.GetAscent())/2f,
                            p0.y,
                            p0.GetTextColor(),
                            null);
                }
            }
            foreach (Point point in points) {
                if (point.GetShape() != Point.INVISIBLE) {
                    page.SetPenColor(point.strokeColor);
                    page.SetPenWidth(point.strokeWidth);
                    page.SetStrokeDashPattern(point.strokeDashPattern);
                    page.SetBrushColor(point.fillColor);
                    page.DrawPoint(point);
                }
            }
            seriesIndex++;
        }
    }

    /// <summary>
    /// Rounds axis range to "nice" values for clean grid lines.
    /// Uses the span (max - min) to support negative values and
    /// zero crossings. Rounds max up and min down to step multiples.
    /// </summary>
    private Round RoundMaxAndMinValues(float maxValue, float minValue) {
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
    ///  Returns [mean_x, mean_y] for the given points.
    /// </summary>
    private float[] Mean(List<Point> points) {
        float[] _mean = new float[2];
        foreach (Point point in points) {
            _mean[0] += point.x;
            _mean[1] += point.y;
        }
        _mean[0] /= points.Count;
        _mean[1] /= points.Count;
        return _mean;
    }

    /// <summary>
    ///  Returns the covariance of x and y.
    /// </summary>
    private float Covar(List<Point> points) {
        float covariance = 0f;
        float[] _mean = Mean(points);
        foreach (Point point in points) {
            covariance += (point.x - _mean[0]) * (point.y - _mean[1]);
        }
        return (covariance / (points.Count - 1));
    }

    /// <summary>
    ///  Returns the sum of squared deviations of x from mean_x.
    /// </summary>
    private float Devsq(List<Point> points) {
        float _devsq = 0f;
        float[] _mean = Mean(points);
        foreach (Point point in points) {
            _devsq += (float) Math.Pow((point.x - _mean[0]), 2);
        }
        return _devsq;
    }

    /// <summary>
    /// Manually sets X axis range and grid line count.
    /// Skips auto-computation when grid lines > 0.
    /// </summary>
    /// <returns>this Chart object.</returns>
    public Chart SetXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.xMin = xMin;
        this.xMax = xMax;
        this.xAxisGridLines = xAxisGridLines;
        return this;
    }

    /// <summary>
    /// Manually sets Y axis range and grid line count.
    /// Skips auto-computation when grid lines > 0.
    /// </summary>
    /// <returns>this Chart object.</returns>
    public Chart SetYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.yMin = yMin;
        this.yMax = yMax;
        this.yAxisGridLines = yAxisGridLines;
        return this;
    }
}
}
