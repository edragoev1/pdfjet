/**
 *  Chart.cs
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;

/**
 *  Used to create XY chart objects and draw them on a page.
 *
 *  Please see Example_09.
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

    private float chartBorderWidth = 0.3f;
    private float innerBorderWidth = 0.3f;

    private NumberFormat nf = null;
    private int minFractionDigits = 2;
    private int maxFractionDigits = 2;

    private Font f1 = null;
    private Font f2 = null;

    private List<List<Point>> chartData = null;

    /**
     *  Create a XY chart object.
     *
     *  @param f1 the font used for the chart title.
     *  @param f2 the font used for the X and Y axis titles.
     */
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
        nf = NumberFormat.GetInstance();
    }

    /**
     *  Sets the title of the chart.
     *
     *  @param title the title text.
     */
    public void SetTitle(String title) {
        this.title = title;
    }

    /**
     *  Sets the title for the X axis.
     *
     *  @param title the X axis title.
     */
    public void SetXAxisTitle(String title) {
        this.xAxisTitle = title;
    }

    /**
     *  Sets the title for the Y axis.
     *
     *  @param title the Y axis title.
     */
    public void SetYAxisTitle(String title) {
        this.yAxisTitle = title;
    }

    /**
     *  Sets the data that will be used to draw this chart.
     *
     *  @param chartData the data.
     */
    public void SetData(List<List<Point>> chartData) {
        this.chartData = chartData;
    }

    /**
     *  Returns the chart data.
     *
     *  @return the chart data.
     */
    public List<List<Point>> GetData() {
        return chartData;
    }

    /**
     *  Sets the position of this chart on the page.
     *
     *  @param x the x coordinate of the top left corner of this chart when drawn on the page.
     *  @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     *  Sets the position of this chart on the page.
     *
     *  @param x the x coordinate of the top left corner of this chart when drawn on the page.
     *  @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     *  Sets the location of this chart on the page.
     *
     *  @param x the x coordinate of the top left corner of this chart when drawn on the page.
     *  @param y the y coordinate of the top left corner of this chart when drawn on the page.
     */
    public void SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
    }

    /**
     *  Sets the size of this chart.
     *
     *  @param w the width of this chart.
     *  @param h the height of this chart.
     */
    public void SetSize(double w, double h) {
        SetSize((float) w, (float) h);
    }

    /**
     *  Sets the size of this chart.
     *
     *  @param w the width of this chart.
     *  @param h the height of this chart.
     */
    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    /**
     *  Sets the minimum number of fractions digits do display for the X and Y axis labels.
     *
     *  @param minFractionDigits the minimum number of fraction digits.
     */
    public void SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
    }

    /**
     *  Sets the maximum number of fractions digits do display for the X and Y axis labels.
     *
     *  @param maxFractionDigits the maximum number of fraction digits.
     */
    public void SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
    }

    /**
     *  Calculates the Slope of a trend line given a list of points.
     *  See Example_09.
     *
     *  @param points the list of points.
     *  @return the Slope float value.
     */
    public float Slope(List<Point> points) {
        return (Covar(points) / Devsq(points) * (points.Count - 1));
    }

    /**
     *  Calculates the Intercept of a trend line given a list of points.
     *  See Example_09.
     *
     *  @param points the list of points.
     *  @return the Intercept float value.
     */
    public float Intercept(List<Point> points, double slope) {
        return Intercept(points, (float) slope);
    }

    /**
     *  Calculates the Intercept of a trend line given a list of points.
     *  See Example_09.
     *
     *  @param points the list of points.
     *  @return the Intercept float value.
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

    /**
     *  Draws this chart on the specified page.
     *
     *  @param page the page to draw this chart on.
     */
    public float[] DrawOn(Page page) {
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

        // Draw chart title
        page.DrawString(
                f1,
                title,
                x1 + ((w - f1.StringWidth(title)) / 2),
                y1 + 1.5f * f1.bodyHeight);

        float topMargin = 2.5f * f1.bodyHeight;
        float leftMargin = GetLongestAxisYLabelWidth() + 2f * f2.bodyHeight;
        float rightMargin = 2f * f2.bodyHeight;
        float bottomMargin = 2.5f * f2.bodyHeight;

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

        // Translate the point coordinates
        for (int i = 0; i < chartData.Count; i++) {
            List<Point> points = chartData[i];
            for (int j = 0; j < points.Count; j++) {
                Point point = points[j];
                if (xyChart) {
                    point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin);
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                    point.lineWidth *= (x6 - x5) / w;
                } else {
                    point.x = x5 + point.x * (x6 - x5) / w;
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                }
                if (point.GetURIAction() != null) {
                    page.AddAnnotation(new Annotation(
                            point.GetURIAction(),
                            null,
                            point.x - point.r,
                            point.y - point.r,
                            point.x + point.r,
                            point.y + point.r,
                            null,
                            null,
                            null));
                }
            }
        }

        DrawPathsAndPoints(page, chartData);

        // Draw the Y axis title
        page.SetBrushColor(Color.black);
        page.SetTextDirection(90);
        page.DrawString(
                f1,
                yAxisTitle,
                x1 + f1.bodyHeight,
                y8 - ((y8 - y5) - f1.StringWidth(yAxisTitle)) / 2);

        // Draw the X axis title
        page.SetTextDirection(0);
        page.DrawString(
                f1,
                xAxisTitle,
                x5 + ((x6 - x5) - f1.StringWidth(xAxisTitle)) / 2,
                y4 - f1.bodyHeight / 2);

        page.SetDefaultLineWidth();
        page.SetDefaultLinePattern();
        page.SetPenColor(Color.black);

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
        page.StrokePath();
    }

    private void DrawInnerBorder(Page page) {
        page.SetPenWidth(innerBorderWidth);
        page.SetPenColor(Color.black);
        page.MoveTo(x5, y5);
        page.LineTo(x6, y6);
        page.LineTo(x7, y7);
        page.LineTo(x8, y8);
        page.ClosePath();
        page.StrokePath();
    }

    private void DrawHorizontalGridLines(Page page) {
        page.SetPenWidth(hGridLineWidth);
        page.SetPenColor(Color.black);
        page.SetLinePattern(hGridLinePattern);
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
        page.SetLinePattern(vGridLinePattern);
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
        float y = y8 + f2.bodyHeight;
        float step = (x6 - x5) / xAxisGridLines;
        page.SetBrushColor(Color.black);
        for (int i = 0; i < (xAxisGridLines + 1); i++) {
            String label = nf.Format(xMin + ((xMax - xMin) / xAxisGridLines) * i);
            page.DrawString(f2, label, x - (f2.StringWidth(label) / 2), y);
            x += step;
        }
    }

    private void DrawYAxisLabels(Page page) {
        float x = x5 - GetLongestAxisYLabelWidth();
        float y = y8 + f2.ascent / 3;
        float step = (y8 - y5) / yAxisGridLines;
        page.SetBrushColor(Color.black);
        for (int i = 0; i < (yAxisGridLines + 1); i++) {
            String label = nf.Format(yMin + ((yMax - yMin) / yAxisGridLines) * i);
            page.DrawString(f2, label, x, y);
            y -= step;
        }
    }

    private void DrawPathsAndPoints(
            Page page, List<List<Point>> chartData) {
        for (int i = 0; i < chartData.Count; i++) {
            List<Point> points = chartData[i];
            Point point = points[0];
            if (point.drawPath) {
                page.SetPenColor(point.color);
                page.SetPenWidth(point.lineWidth);
                page.SetLinePattern(point.linePattern);
                page.DrawPath(points, Operation.STROKE);
                if (point.GetText() != null) {
                    page.SetBrushColor(point.GetTextColor());
                    page.SetTextDirection(point.GetTextDirection());
                    page.DrawString(f2, point.GetText(), point.x, point.y);
                }
            }
            for (int j = 0; j < points.Count; j++) {
                point = points[j];
                if (point.GetShape() != Point.INVISIBLE) {
                    page.SetPenWidth(point.lineWidth);
                    page.SetLinePattern(point.linePattern);
                    page.SetPenColor(point.color);
                    page.SetBrushColor(point.color);
                    page.DrawPoint(point);
                }
            }
        }
    }

    private Round RoundMaxAndMinValues(float maxValue, float minValue) {
        int maxExponent = (int) Math.Floor(Math.Log(maxValue) / Math.Log(10));
        maxValue *= (float) Math.Pow(10, -maxExponent);

        if      (maxValue > 9.00f) { maxValue = 10.0f; }
        else if (maxValue > 8.00f) { maxValue = 9.00f; }
        else if (maxValue > 7.00f) { maxValue = 8.00f; }
        else if (maxValue > 6.00f) { maxValue = 7.00f; }
        else if (maxValue > 5.00f) { maxValue = 6.00f; }
        else if (maxValue > 4.00f) { maxValue = 5.00f; }
        else if (maxValue > 3.50f) { maxValue = 4.00f; }
        else if (maxValue > 3.00f) { maxValue = 3.50f; }
        else if (maxValue > 2.50f) { maxValue = 3.00f; }
        else if (maxValue > 2.00f) { maxValue = 2.50f; }
        else if (maxValue > 1.75f) { maxValue = 2.00f; }
        else if (maxValue > 1.50f) { maxValue = 1.75f; }
        else if (maxValue > 1.25f) { maxValue = 1.50f; }
        else if (maxValue > 1.00f) { maxValue = 1.25f; }
        else                       { maxValue = 1.00f; }

        Round round = new Round();

        if      (maxValue == 10.0f) { round.numOfGridLines = 10; }
        else if (maxValue == 9.00f) { round.numOfGridLines =  9; }
        else if (maxValue == 8.00f) { round.numOfGridLines =  8; }
        else if (maxValue == 7.00f) { round.numOfGridLines =  7; }
        else if (maxValue == 6.00f) { round.numOfGridLines =  6; }
        else if (maxValue == 5.00f) { round.numOfGridLines =  5; }
        else if (maxValue == 4.00f) { round.numOfGridLines =  8; }
        else if (maxValue == 3.50f) { round.numOfGridLines =  7; }
        else if (maxValue == 3.00f) { round.numOfGridLines =  6; }
        else if (maxValue == 2.50f) { round.numOfGridLines =  5; }
        else if (maxValue == 2.00f) { round.numOfGridLines =  8; }
        else if (maxValue == 1.75f) { round.numOfGridLines =  7; }
        else if (maxValue == 1.50f) { round.numOfGridLines =  6; }
        else if (maxValue == 1.25f) { round.numOfGridLines =  5; }
        else if (maxValue == 1.00f) { round.numOfGridLines = 10; }

        round.maxValue = maxValue * ((float) Math.Pow(10, maxExponent));
        float step = round.maxValue / round.numOfGridLines;
        float temp = round.maxValue;
        round.numOfGridLines = 0;
        while (true) {
            round.numOfGridLines++;
            temp -= step;
            if (temp <= minValue) {
                round.minValue = temp;
                break;
            }
        }

        return round;
    }

    private float[] Mean(List<Point> points) {
        float[] _mean = new float[2];
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            _mean[0] += point.x;
            _mean[1] += point.y;
        }
        _mean[0] /= points.Count - 1;
        _mean[1] /= points.Count - 1;
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
     *
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
     *  Sets xMin and xMax for the X axis and the number of X grid lines.
     *
     *  @param xMin for the X axis.
     *  @param xMax for the X axis.
     *  @param xAxisGridLines the number of X axis grid lines.
     */
    public void SetXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.xMin = xMin;
        this.xMax = xMax;
        this.xAxisGridLines = xAxisGridLines;
    }

    /**
     *  Sets yMin and yMax for the Y axis and the number of Y grid lines.
     *
     *  @param yMin for the Y axis.
     *  @param yMax for the Y axis.
     *  @param yAxisGridLines the number of Y axis grid lines.
     */
    public void SetYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.yMin = yMin;
        this.yMax = yMax;
        this.yAxisGridLines = yAxisGridLines;
    }
}   // End of Chart.cs
}   // End of namespace PDFjet.NET
