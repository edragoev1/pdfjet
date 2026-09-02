/**
 * Chart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

/**
 * Used to create XY chart objects and draw them on a page.
 *
 * Please see Example_09.
 */
namespace PDFjet.NET {
public class Chart : IDrawable {
    private float w = 300f;
    private float h = 200f;

    private float x1;
    private float y1;
    private float x2;
    private float y2;
    private float x3;
    private float y3;
    private float x4;
    private float y4;
    private float x5;
    private float y5;
    private float x6;
    private float y6;
    private float x7;
    private float y7;
    private float x8;
    private float y8;

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

    private bool xyChart = true;

    private float hGridLineWidth = 0f;
    private float vGridLineWidth = 0f;

    private String hGridLinePattern = "[1 1] 0";
    private String vGridLinePattern = "[1 1] 0";

    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    private NumberFormat nf = null;
    private int minFractionDigits = 2;
    private int maxFractionDigits = 2;

    private Font f1 = null;
    private Font f2 = null;
    private float fontSize = 8f;

    private List<List<Point>> chartData = null;

    /**
     * Create a XY chart object.
     *
     * @param f1 the font used for the chart title.
     * @param f2 the font used for the X and Y axis titles.
     */
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
        nf = NumberFormat.GetInstance();
    }

    /**
     * Sets the title of the chart.
     *
     * @param title the title text.
     */
    public void SetTitle(String title) {
        this.title = title;
    }

    /**
     * Sets the title for the X axis.
     *
     * @param title the X axis title.
     */
    public void SetXAxisTitle(String title) {
        this.xAxisTitle = title;
    }

    /**
     * Sets the title for the Y axis.
     *
     * @param title the Y axis title.
     */
    public void SetYAxisTitle(String title) {
        this.yAxisTitle = title;
    }

    /**
     * Sets the data that will be used to draw this chart.
     *
     * @param chartData the data.
     */
    public void SetData(List<List<Point>> chartData) {
        this.chartData = chartData;
    }

    /**
     * Returns the chart data.
     *
     * @return the chart data.
     */
    public List<List<Point>> GetData() {
        return chartData;
    }

    /**
     * Sets the position of this chart on the page.
     *
     * @param x the x coordinate of the top left corner of this chart when drawn on the page.
     * @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     * Sets the position of this chart on the page.
     *
     * @param x the x coordinate of the top left corner of this chart when drawn on the page.
     * @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     * Sets the location of this chart on the page.
     *
     * @param x the x coordinate of the top left corner of this chart when drawn on the page.
     * @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public Chart SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    public Chart SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     * Sets the size of this chart.
     *
     * @param w the width of this chart.
     * @param h the height of this chart.
     */
    public void SetSize(double w, double h) {
        SetSize((float) w, (float) h);
    }

    /**
     * Sets the size of this chart.
     *
     * @param w the width of this chart.
     * @param h the height of this chart.
     */
    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    /**
     * Sets the minimum number of fractions digits do display for the X and Y axis labels.
     *
     * @param minFractionDigits the minimum number of fraction digits.
     */
    public void SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
    }

    /**
     * Sets the maximum number of fractions digits do display for the X and Y axis labels.
     *
     * @param maxFractionDigits the maximum number of fraction digits.
     */
    public void SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
    }

    /**
     * Calculates the Slope of a trend line given a list of points.
     * See Example_09.
     *
     * @param points the list of points.
     * @return the Slope float value.
     */
    public float Slope(List<Point> points) {
        return (Covar(points) / Devsq(points) * (points.Count - 1));
    }

    /**
     * Calculates the Intercept of a trend line given a list of points.
     * See Example_09.
     *
     * @param points the list of points.
     * @return the Intercept float value.
     */
    public float Intercept(List<Point> points, double slope) {
        return Intercept(points, (float) slope);
    }

    /**
     * Calculates the Intercept of a trend line given a list of points.
     * See Example_09.
     *
     * @param points the list of points.
     * @return the Intercept float value.
     */
    public float Intercept(List<Point> points, float slope) {
        float[] _mean = Mean(points);
        return (_mean[1] - slope * _mean[0]);
    }

    public void SetDrawXAxisLabels(bool drawXAxisLabels) {
        this.drawXAxisLabels = drawXAxisLabels;
    }

    public void SetDrawYAxisLabels(bool drawYAxisLabels) {
        this.drawYAxisLabels = drawYAxisLabels;
    }

    public void SetXYChart(bool xyChart) {
        this.xyChart = xyChart;
    }

    public void SetChartBorderWidth(float width) {
        this.chartBorderWidth = width;
    }

    public void SetInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
    }

    public void SetHGridLineWidth(float width) {
        this.hGridLineWidth = width;
    }

    public void SetVGridLineWidth(float width) {
        this.vGridLineWidth = width;
    }

    public void SetHGridLinePattern(String pattern) {
        this.hGridLinePattern = pattern;
    }

    public void SetVGridLinePattern(String pattern) {
        this.vGridLinePattern = pattern;
    }

    /**
     * Draws this chart on the specified page.
     *
     * @param page the page to draw this chart on.
     */
    public float[] DrawOn(Page page) {
        if (chartData == null || chartData.Count == 0) {
            return new float[] { this.x1 + this.w, this.y1 + this.h };
        }

        page.Append("q\n"); // Save the graphics state

        nf.SetMinimumFractionDigits(minFractionDigits);
        nf.SetMaximumFractionDigits(maxFractionDigits);

        x2 = x1 + w;
        y2 = y1;

        x3 = x2;
        y3 = y1 + h;

        x4 = x1;
        y4 = y3;

        SetXAxisMinAndMaxChartValues();
        SetYAxisMinAndMaxChartValues();
        RoundXAxisMinAndMaxValues();
        RoundYAxisMinAndMaxValues();

        if (xMax == xMin) { xMax = xMin + 1f; }
        if (yMax == yMin) { yMax = yMin + 1f; }

        // Draw chart title
        page.DrawString(
                f1,
                fontSize,
                title,
                x1 + ((w - f1.StringWidth(title)) / 2),
                y1 + 1.5f * f1.GetBodyHeight(f1.GetSize()));

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

        // Create a defensive copy so DrawOn() never mutates the user's data
        List<List<Point>> plotData = new List<List<Point>>(chartData.Count);
        for (int i = 0; i < chartData.Count; i++) {
            List<Point> original = chartData[i];
            List<Point> copy = new List<Point>(original.Count);
            for (int j = 0; j < original.Count; j++) {
                copy.Add(new Point(original[j]));
            }
            plotData.Add(copy);
        }

        // Translate the point coordinates (on the copies)
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

        // Draw the Y axis title
        page.SetBrushColor(Color.black);
        page.SetTextDirection(90);
        page.DrawString(
                f2,
                fontSize,
                yAxisTitle,
                x1 + f2.GetBodyHeight(f2.GetSize()),
                y8 - ((y8 - y5) - f2.StringWidth(yAxisTitle)) / 2);

        // Draw the X axis title
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

        page.Append("Q\n"); // Restore the graphics state

        return new float[] {this.x1 + this.w, this.y1 + this.h};
    }

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

    private void RoundXAxisMinAndMaxValues() {
        Round round = RoundMaxAndMinValues(xMax, xMin);
        xMax = round.maxValue;
        xMin = round.minValue;
        xAxisGridLines = round.numOfGridLines;
    }

    private void RoundYAxisMinAndMaxValues() {
        Round round = RoundMaxAndMinValues(yMax, yMin);
        yMax = round.maxValue;
        yMin = round.minValue;
        yAxisGridLines = round.numOfGridLines;
    }

    private void DrawChartBorder(Page page) {
        page.SetPenWidth(chartBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x1, y1);
        page.LineTo(x2, y2);
        page.LineTo(x3, y3);
        page.LineTo(x4, y4);
        page.ClosePath();
    }

    private void DrawInnerBorder(Page page) {
        page.SetPenWidth(innerBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x5, y5);
        page.LineTo(x6, y6);
        page.LineTo(x7, y7);
        page.LineTo(x8, y8);
        page.ClosePath();
    }

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

    private Round RoundMaxAndMinValues(float maxValue, float minValue) {
        // Work with the span (range) instead of just maxValue.
        // This handles negative values, zero crossings, and all-positive ranges.
        float span = maxValue - minValue;
        if (span <= 0f) { span = 1f; }  // guard against flat data

        int exponent = (int) Math.Floor(Math.Log(span) / Math.Log(10));
        float normalizedSpan = span * (float) Math.Pow(10, -exponent);

        // Snap the normalized span up to a "nice" value
        // and pick a grid line count that gives clean step sizes.
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

        // Scale back to the original magnitude
        float step = niceSpan * (float) Math.Pow(10, exponent) / numOfGridLines;

        Round round = new Round();

        // Round max UP and min DOWN to the nearest step multiple
        round.maxValue = (float) Math.Ceiling(maxValue / step) * step;
        round.minValue = (float) Math.Floor(minValue / step) * step;

        // Recount grid lines based on the actual rounded range
        round.numOfGridLines = (int) Math.Round((round.maxValue - round.minValue) / step);

        return round;
    }

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

    private float Covar(List<Point> points) {
        float covariance = 0f;
        float[] _mean = Mean(points);
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            covariance += (point.x - _mean[0]) * (point.y - _mean[1]);
        }
        return (covariance / (points.Count - 1));
    }

    /**
     * Devsq() returns the sum of squares of deviations.
     */
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
     * Sets xMin and xMax for the X axis and the number of X grid lines.
     *
     * @param xMin for the X axis.
     * @param xMax for the X axis.
     * @param xAxisGridLines the number of X axis grid lines.
     */
    public void SetXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.xMin = xMin;
        this.xMax = xMax;
        this.xAxisGridLines = xAxisGridLines;
    }

    /**
     * Sets yMin and yMax for the Y axis and the number of Y grid lines.
     *
     * @param yMin for the Y axis.
     * @param yMax for the Y axis.
     * @param yAxisGridLines the number of Y axis grid lines.
     */
    public void SetYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.yMin = yMin;
        this.yMax = yMax;
        this.yAxisGridLines = yAxisGridLines;
    }
}   // End of Chart.cs
}   // End of namespace PDFjet.NET
