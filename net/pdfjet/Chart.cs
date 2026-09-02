/**
 * Chart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License.
 */
using System;
using System.Collections.Generic;

/**
 * XY chart renderer for PDF pages. See Example_09.
 */
namespace PDFjet.NET {
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

    /**
     * Creates an XY chart.
     *
     * @param f1 font for the chart title.
     * @param f2 font for axis titles and labels.
     */
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
        nf = NumberFormat.GetInstance();
    }

    /** Sets the chart title. */
    public void SetTitle(String title) {
        this.title = title;
    }

    /** Sets the X axis title. */
    public void SetXAxisTitle(String title) {
        this.xAxisTitle = title;
    }

    /** Sets the Y axis title. */
    public void SetYAxisTitle(String title) {
        this.yAxisTitle = title;
    }

    /** Sets the chart data (list of series, each a list of points). */
    public void SetData(List<List<Point>> chartData) {
        this.chartData = chartData;
    }

    /** Returns the chart data. */
    public List<List<Point>> GetData() {
        return chartData;
    }

    /** Sets the top-left position of this chart on the page. */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /** Sets the top-left position of this chart on the page. */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /** Sets the top-left position. Returns this for chaining. */
    public Chart SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /** Sets the top-left position. Returns this for chaining. */
    public Chart SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /** Sets the chart dimensions. */
    public void SetSize(double w, double h) {
        SetSize((float) w, (float) h);
    }

    /** Sets the chart dimensions. */
    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    /** Sets the font size for axis labels. */
    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    /** Sets minimum decimal places for axis labels. */
    public void SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
    }

    /** Sets maximum decimal places for axis labels. */
    public void SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
    }

    /**
     * Calculates the slope of a trend line (OLS). See Example_09.
     *
     * @param points the data points.
     * @return the slope.
     */
    public float Slope(List<Point> points) {
        return (Covar(points) / Devsq(points) * (points.Count - 1));
    }

    /**
     * Calculates the intercept of a trend line (OLS). See Example_09.
     *
     * @param points the data points.
     * @param slope the pre-computed slope.
     * @return the intercept.
     */
    public float Intercept(List<Point> points, double slope) {
        return Intercept(points, (float) slope);
    }

    /**
     * Calculates the intercept of a trend line (OLS). See Example_09.
     *
     * @param points the data points.
     * @param slope the pre-computed slope.
     * @return the intercept.
     */
    public float Intercept(List<Point> points, float slope) {
        float[] _mean = Mean(points);
        return (_mean[1] - slope * _mean[0]);
    }

    /** Toggles drawing of X axis labels. */
    public void SetDrawXAxisLabels(bool drawXAxisLabels) {
        this.drawXAxisLabels = drawXAxisLabels;
    }

    /** Toggles drawing of Y axis labels. */
    public void SetDrawYAxisLabels(bool drawYAxisLabels) {
        this.drawYAxisLabels = drawYAxisLabels;
    }

    /** Sets XY scatter mode (true) or category mode (false). */
    public void SetXYChart(bool xyChart) {
        this.xyChart = xyChart;
    }

    /** Sets the outer chart border width (0 = invisible). */
    public void SetChartBorderWidth(float width) {
        this.chartBorderWidth = width;
    }

    /** Sets the inner plot area border width (0 = invisible). */
    public void SetInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
    }

    /** Sets the horizontal grid line width (0 = invisible). */
    public void SetHGridLineWidth(float width) {
        this.hGridLineWidth = width;
    }

    /** Sets the vertical grid line width (0 = invisible). */
    public void SetVGridLineWidth(float width) {
        this.vGridLineWidth = width;
    }

    /** Sets the horizontal grid line dash pattern (e.g. "[1 1] 0"). */
    public void SetHGridLinePattern(String pattern) {
        this.hGridLinePattern = pattern;
    }

    /** Sets the vertical grid line dash pattern (e.g. "[1 1] 0"). */
    public void SetVGridLinePattern(String pattern) {
        this.vGridLinePattern = pattern;
    }

    /**
     * Draws this chart on the specified page.
     *
     * @param page the page to draw on.
     * @return the bottom-right corner coordinates [x, y].
     */
    public float[] DrawOn(Page page) {
        if (chartData == null || chartData.Count == 0) {
            return new float[] { this.x1 + this.w, this.y1 + this.h };
        }

        page.Append("q\n"); // Save graphics state

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

        DrawHorizontalGridLines(page);
        DrawVerticalGridLines(page);

        if (drawXAxisLabels) {
            DrawXAxisLabels(page);
        }
        if (drawYAxisLabels) {
            DrawYAxisLabels(page);
        }

        // Defensive copy so the user's data is never mutated
        List<List<Point>> plotData = new List<List<Point>>(chartData.Count);
        for (int i = 0; i < chartData.Count; i++) {
            List<Point> original = chartData[i];
            List<Point> copy = new List<Point>(original.Count);
            for (int j = 0; j < original.Count; j++) {
                copy.Add(new Point(original[j]));
            }
            plotData.Add(copy);
        }

        // Translate data coordinates to page coordinates (on the copies)
        for (int i = 0; i < plotData.Count; i++) {
            List<Point> points = plotData[i];
            for (int j = 0; j < points.Count; j++) {
                Point point = points[j];
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
        page.SetDefaultStrokePattern();
        page.SetPenColor(Color.black);

        page.Append("Q\n"); // Restore graphics state

        return new float[] {this.x1 + this.w, this.y1 + this.h};
    }

    /** Returns the width of the widest Y axis label (for left margin). */
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

    /** Scans all data points to find X axis min/max (skipped if manual). */
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

    /** Scans all data points to find Y axis min/max (skipped if manual). */
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

    /** Rounds X axis range to "nice" values and sets grid line count. */
    private void RoundXAxisMinAndMaxValues() {
        Round round = RoundMaxAndMinValues(xMax, xMin);
        xMax = round.maxValue;
        xMin = round.minValue;
        xAxisGridLines = round.numOfGridLines;
    }

    /** Rounds Y axis range to "nice" values and sets grid line count. */
    private void RoundYAxisMinAndMaxValues() {
        Round round = RoundMaxAndMinValues(yMax, yMin);
        yMax = round.maxValue;
        yMin = round.minValue;
        yAxisGridLines = round.numOfGridLines;
    }

    /** Draws the outer chart border. */
    private void DrawChartBorder(Page page) {
        page.SetPenWidth(chartBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x1, y1);
        page.LineTo(x2, y2);
        page.LineTo(x3, y3);
        page.LineTo(x4, y4);
        page.ClosePath();
    }

    /** Draws the inner plot area border. */
    private void DrawInnerBorder(Page page) {
        page.SetPenWidth(innerBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x5, y5);
        page.LineTo(x6, y6);
        page.LineTo(x7, y7);
        page.LineTo(x8, y8);
        page.ClosePath();
    }

    /** Draws horizontal grid lines across the plot area. */
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

    /** Draws vertical grid lines across the plot area. */
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

    /** Draws X axis labels (one per grid line interval). */
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

    /** Draws Y axis labels (one per grid line interval). */
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

    /** Draws connecting paths, point markers, and point text. */
    private void DrawPathsAndPoints(
            Page page, List<List<Point>> chartData) {
        foreach (List<Point> points in chartData) {
            Point point = points[0];
            if (point.drawPath) {
                page.SetPenColor(point.strokeColor);
                page.SetPenWidth(point.strokeWidth);
                page.SetStrokeDashPattern(point.strokePattern);
                page.DrawPath(points, PathOperator.Stroke);
                if (point.GetText() != null) {
                    page.SetTextDirection(point.GetTextDirection());
                    page.DrawString(
                        f2,
                        null,
                        fontSize,
                        point.GetText(),
                        point.x + 1.5f*f2.GetDescent(),
                        point.y + fontSize/3f,
                        point.GetTextColor(),
                        null);
                }
            }
            for (int i = 0; i < points.Count; i++) {
                        point = points[i];
                if (point.GetShape() != Point.INVISIBLE) {
                    page.SetPenWidth(point.strokeWidth);
                    page.SetStrokeDashPattern(point.strokePattern);
                    page.SetPenColor(point.strokeColor);
                    page.SetBrushColor(point.fillColor);
                    page.DrawPoint(point);
                }
            }
        }
    }

    /**
     * Rounds axis range to "nice" values for clean grid lines.
     * Uses the span (max - min) to support negative values and
     * zero crossings. Rounds max up and min down to step multiples.
     */
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

    /** Returns [mean_x, mean_y] for the given points. */
    private float[] Mean(List<Point> points) {
        float[] _mean = new float[2];
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            _mean[0] += point.x;
            _mean[1] += point.y;
        }
        _mean[0] /= points.Count;
        _mean[1] /= points.Count;
        return _mean;
    }

    /** Returns the covariance of x and y. */
    private float Covar(List<Point> points) {
        float covariance = 0f;
        float[] _mean = Mean(points);
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            covariance += (point.x - _mean[0]) * (point.y - _mean[1]);
        }
        return (covariance / (points.Count - 1));
    }

    /** Returns the sum of squared deviations of x from mean_x. */
    private float Devsq(List<Point> points) {
        float _devsq = 0f;
        float[] _mean = Mean(points);
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            _devsq += (float) Math.Pow((point.x - _mean[0]), 2);
        }
        return _devsq;
    }

    /**
     * Manually sets X axis range and grid line count.
     * Skips auto-computation when grid lines > 0.
     */
    public void SetXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.xMin = xMin;
        this.xMax = xMax;
        this.xAxisGridLines = xAxisGridLines;
    }

    /**
     * Manually sets Y axis range and grid line count.
     * Skips auto-computation when grid lines > 0.
     */
    public void SetYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.yMin = yMin;
        this.yMax = yMax;
        this.yAxisGridLines = yAxisGridLines;
    }
}
}
