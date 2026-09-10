/*
 * Chart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;
import java.text.*;

/**
 * XY chart renderer for PDF pages. See Example_09.
 */
public class Chart implements Drawable {
    private float w = 300f;
    private float h = 200f;

    // Outer chart rectangle (x1,y1 = top-left, clockwise)
    private float x1;
    private float y1;
    private float x2;
    private float y2;
    private float x3;
    private float y3;
    private float x4;
    private float y4;

    // Inner plot area (x5,y5 = top-left, clockwise)
    private float x5;
    private float y5;
    private float x6;
    private float y6;
    private float x7;
    private float y7;
    private float x8;
    private float y8;

    // Data axis ranges (auto-computed if grid lines == 0)
    private float xMax = Float.MIN_VALUE;
    private float xMin = Float.MAX_VALUE;
    private float yMax = Float.MIN_VALUE;
    private float yMin = Float.MAX_VALUE;

    private int xAxisGridLines = 0;
    private int yAxisGridLines = 0;

    private String title = "";
    private String xAxisTitle = "";
    private String yAxisTitle = "";

    private boolean drawXAxisLines = true;
    private boolean drawYAxisLines = true;
    private boolean drawXAxisLabels = true;
    private boolean drawYAxisLabels = true;

    private boolean xyChart = true;  // true = XY scatter, false = category mode

    // Grid line styling (width 0 = invisible, pattern default = dotted)
    private float hGridLineWidth;
    private float vGridLineWidth;
    private String hGridLinePattern = "[1 1] 0";
    private String vGridLinePattern = "[1 1] 0";

    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    // Label number formatting
    private NumberFormat nf = null;
    private int minFractionDigits = 2;
    private int maxFractionDigits = 2;

    // f1 = chart title font, f2 = axis title/label font
    private Font f1;
    private Font f2;
    private float fontSize = 8f;

    private List<List<Point>> chartData = null;

    private static final int[] DEFAULT_PALETTE = {
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    };
    private boolean autoColors = true;

    /**
     *  Creates an XY chart.
     *
     *  @param f1 the font for the chart title.
     *  @param f2 the font for axis titles and labels.
     */
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
        nf = NumberFormat.getInstance();
    }

    /**
     * Sets the chart title.
     *
     * @param title the title.
     * @return this Chart object.
     */
    public Chart setTitle(String title) {
        this.title = title;
        return this;
    }

    /**
     * Sets the X axis title.
     *
     * @param title the title.
     * @return this Chart object.
     */
    public Chart setXAxisTitle(String title) {
        this.xAxisTitle = title;
        return this;
    }

    /**
     * Sets the Y axis title.
     *
     * @param title the title.
     * @return this Chart object.
     */
    public Chart setYAxisTitle(String title) {
        this.yAxisTitle = title;
        return this;
    }

    /**
     * Sets the chart data: a list of series, each a list of points.
     *
     * @param chartData the chart data.
     * @return this Chart object.
     */
    public Chart setData(List<List<Point>> chartData) {
        this.chartData = chartData;
        return this;
    }

    /**
     * Returns the chart data.
     *
     * @return the list of series, each a list of points.
     */
    public List<List<Point>> getData() {
        return chartData;
    }

    /**
     * Sets the location of the top left corner of this chart.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this Chart object.
     */
    public Chart setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /** Sets the top-left position. Returns this for chaining. */
    public Chart setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the size of this chart.
     *
     * @param w the width.
     * @param h the height.
     * @return this Chart object.
     */
    public Chart setSize(double w, double h) {
        setSize((float) w, (float) h);
        return this;
    }

    /**
     * Sets the size of this chart.
     *
     * @param w the width.
     * @param h the height.
     * @return this Chart object.
     */
    public Chart setSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /**
     * Sets the font size of the axis labels.
     *
     * @param fontSize the font size.
     * @return this Chart object.
     */
    public Chart setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     * Sets the minimum number of decimal places in the axis labels.
     *
     * @param minFractionDigits the minimum number of decimal places.
     * @return this Chart object.
     */
    public Chart setMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /**
     * Sets the maximum number of decimal places in the axis labels.
     *
     * @param maxFractionDigits the maximum number of decimal places.
     * @return this Chart object.
     */
    public Chart setMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /**
     *  Calculates the slope of a trend line (OLS). See Example_09.
     *
     *  @param points the list of points.
     *  @return the slope.
     */
    public float slope(List<Point> points) {
        return (covar(points) / devsq(points) * (points.size() - 1));
    }

    /**
     *  Calculates the intercept of a trend line (OLS). See Example_09.
     *
     *  @param points the list of points.
     *  @param slope the slope.
     *  @return the intercept.
     */
    public float intercept(List<Point> points, double slope) {
        return intercept(points, (float) slope);
    }

    /**
     *  Calculates the intercept of a trend line (OLS). See Example_09.
     *
     *  @param points the list of points.
     *  @param slope the slope.
     *  @return the intercept.
     */
    public float intercept(List<Point> points, float slope) {
        float[] _mean = mean(points);
        return (_mean[1] - slope * _mean[0]);
    }

    /**
     * Sets whether the horizontal grid lines are drawn.
     *
     * @param drawXAxisLines true to draw them.
     * @return this Chart object.
     */
    public Chart setDrawXAxisLines(boolean drawXAxisLines) {
        this.drawXAxisLines = drawXAxisLines;
        return this;
    }

    /**
     * Sets whether the vertical grid lines are drawn.
     *
     * @param drawYAxisLines true to draw them.
     * @return this Chart object.
     */
    public Chart setDrawYAxisLines(boolean drawYAxisLines) {
        this.drawYAxisLines = drawYAxisLines;
        return this;
    }

    /**
     * Sets whether the X axis labels are drawn.
     *
     * @param drawXAxisLabels true to draw them.
     * @return this Chart object.
     */
    public Chart setDrawXAxisLabels(boolean drawXAxisLabels) {
        this.drawXAxisLabels = drawXAxisLabels;
        return this;
    }

    /**
     * Sets whether the Y axis labels are drawn.
     *
     * @param drawYAxisLabels true to draw them.
     * @return this Chart object.
     */
    public Chart setDrawYAxisLabels(boolean drawYAxisLabels) {
        this.drawYAxisLabels = drawYAxisLabels;
        return this;
    }

    /**
     * Sets whether this is an XY scatter chart or a category chart.
     *
     * @param xyChart true for an XY scatter chart, false for a category chart.
     * @return this Chart object.
     */
    public Chart setXYChart(boolean xyChart) {
        this.xyChart = xyChart;
        return this;
    }

    /**
     * Sets the width of the outer chart border. A width of 0 hides it.
     *
     * @param width the border width.
     * @return this Chart object.
     */
    public Chart setChartBorderWidth(float width) {
        this.chartBorderWidth = width;
        return this;
    }

    /**
     * Sets the width of the plot area border. A width of 0 hides it.
     *
     * @param width the border width.
     * @return this Chart object.
     */
    public Chart setInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
        return this;
    }

    /**
     * Sets the width of the horizontal grid lines. A width of 0 hides them.
     *
     * @param width the line width.
     * @return this Chart object.
     */
    public Chart setHGridLineWidth(float width) {
        this.hGridLineWidth = width;
        return this;
    }

    /**
     * Sets the width of the vertical grid lines. A width of 0 hides them.
     *
     * @param width the line width.
     * @return this Chart object.
     */
    public Chart setVGridLineWidth(float width) {
        this.vGridLineWidth = width;
        return this;
    }

    /**
     * Sets the dash pattern of the horizontal grid lines, for example "[1 1] 0".
     *
     * @param pattern the dash pattern.
     * @return this Chart object.
     */
    public Chart setHGridLinePattern(String pattern) {
        this.hGridLinePattern = pattern;
        return this;
    }

    /**
     * Sets the dash pattern of the vertical grid lines, for example "[1 1] 0".
     *
     * @param pattern the dash pattern.
     * @return this Chart object.
     */
    public Chart setVGridLinePattern(String pattern) {
        this.vGridLinePattern = pattern;
        return this;
    }

    /**
     * Sets whether the series colors are assigned automatically.
     *
     * @param autoColors true to assign the colors automatically.
     * @return this Chart object.
     */
    public Chart setAutoColors(boolean autoColors) {
        this.autoColors = autoColors;
        return this;
    }

    /**
     *  Draws this chart on the specified page.
     *
     *  @param page the page to draw on.
     *  @return the bottom-right corner coordinates [x, y].
     */
    public float[] drawOn(Page page) throws Exception {
        // Guard against null or empty data
        if (chartData == null || chartData.isEmpty()) {
            return new float[] { this.x1 + this.w, this.y1 + this.h };
        }

        nf.setMinimumFractionDigits(minFractionDigits);
        nf.setMaximumFractionDigits(maxFractionDigits);

        // Compute outer rectangle corners
        x2 = x1 + w;
        y2 = y1;
        x3 = x2;
        y3 = y1 + h;
        x4 = x1;
        y4 = y3;

        // Compute and round axis ranges
        setXAxisMinAndMaxChartValues();
        setYAxisMinAndMaxChartValues();
        roundXAxisMinAndMaxValues();
        roundYAxisMinAndMaxValues();

        // Guard against flat data (all same X or Y)
        if (xMax == xMin) { xMax = xMin + 1f; }
        if (yMax == yMin) { yMax = yMin + 1f; }

        // Draw chart title (centered, top)
        page.drawString(
                f1,
                fontSize,
                title,
                x1 + ((w - f1.stringWidth(title)) / 2),
                y1 + 1.5f * f1.bodyHeight);

        // Compute margins and inner plot area
        float topMargin = 2.5f * f1.bodyHeight;
        float leftMargin = getLongestAxisYLabelWidth() + 2f * f2.bodyHeight;
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

        drawChartBorder(page);
        drawInnerBorder(page);

        if (drawXAxisLines) {
            drawHorizontalGridLines(page);
        }
        if (drawYAxisLines) {
            drawVerticalGridLines(page);
        }
        if (drawXAxisLabels) {
            drawXAxisLabels(page);
        }
        if (drawYAxisLabels) {
            drawYAxisLabels(page);
        }

        // Defensive copy so the user's data is never mutated
        List<List<Point>> plotData = new ArrayList<List<Point>>(chartData.size());
        for (List<Point> original : chartData) {
            List<Point> copy = new ArrayList<Point>(original.size());
            for (Point p : original) {
                copy.add(new Point(p));
            }
            plotData.add(copy);
        }

        // Translate data coordinates to page coordinates (on the copies)
        for (List<Point> points : plotData) {
            for (Point point : points) {
                if (xyChart) {
                    point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin);
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                    point.setStrokeWidth(point.getStrokeWidth() * (x6 - x5) / w);
                } else {
                    point.x = x5 + (point.x / w) * (x6 - x5);
                    point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                }
                if (point.getURIAction() != null) {
                    page.addAnnotation(new Annotation(
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
                            point.getURIAction(),
                            null,
                            null,
                            null,
                            null));
                }
            }
        }

        // Draw paths and point markers using the copies
        drawPathsAndPoints(page, plotData);

        // Draw Y axis title (rotated 90 degrees)
        page.setBrushColor(Color.black);
        page.setTextDirection(90);
        page.drawString(
                f2,
                fontSize,
                yAxisTitle,
                x1 + f2.bodyHeight,
                y8 - ((y8 - y5) - f2.stringWidth(yAxisTitle)) / 2);

        // Draw X axis title
        page.setTextDirection(0);
        page.setBrushColor(Color.black);
        page.drawString(
                f2,
                fontSize,
                xAxisTitle,
                x5 + ((x6 - x5) - f2.stringWidth(xAxisTitle)) / 2,
                y4 - f2.bodyHeight / 2);

        // Restore default pen/brush state
        page.setDefaultLineWidth();
        page.setDefaultStrokeDashPattern();
        page.setPenColor(Color.black);

        return new float[] {this.x1 + this.w, this.y1 + this.h};
    }

    /** Returns the width of the widest Y axis label (for left margin). */
    private float getLongestAxisYLabelWidth() {
        float minLabelWidth = f2.stringWidth(nf.format(yMin) + "0");
        float maxLabelWidth = f2.stringWidth(nf.format(yMax) + "0");
        if (maxLabelWidth > minLabelWidth) {
            return maxLabelWidth;
        }
        return minLabelWidth;
    }

    /** Scans all data points to find X axis min/max (skipped if manual). */
    private void setXAxisMinAndMaxChartValues() {
        if (xAxisGridLines != 0) {
            return;
        }
        for (List<Point> points : chartData) {
            for (Point point : points) {
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
    private void setYAxisMinAndMaxChartValues() {
        if (yAxisGridLines != 0) {
            return;
        }
        for (List<Point> points : chartData) {
            for (Point point : points) {
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
    private void roundXAxisMinAndMaxValues() {
        if (xAxisGridLines != 0) {
            return;
        }
        Round round = roundMaxAndMinValues(xMax, xMin);
        xMax = round.maxValue;
        xMin = round.minValue;
        xAxisGridLines = round.numOfGridLines;
    }

    /** Rounds Y axis range to "nice" values and sets grid line count. */
    private void roundYAxisMinAndMaxValues() {
        if (yAxisGridLines != 0) {
            return;
        }
        Round round = roundMaxAndMinValues(yMax, yMin);
        yMax = round.maxValue;
        yMin = round.minValue;
        yAxisGridLines = round.numOfGridLines;
    }

    /** Draws the outer chart border. */
    private void drawChartBorder(Page page) {
        page.setPenWidth(chartBorderWidth);
        page.setPenColor(Color.black);
        page.moveTo(x1, y1);
        page.lineTo(x2, y2);
        page.lineTo(x3, y3);
        page.lineTo(x4, y4);
        page.closePath();
    }

    /** Draws the inner plot area border. */
    private void drawInnerBorder(Page page) {
        page.setPenWidth(innerBorderWidth);
        page.setPenColor(Color.black);
        page.moveTo(x5, y5);
        page.lineTo(x6, y6);
        page.lineTo(x7, y7);
        page.lineTo(x8, y8);
        page.closePath();
    }

    /** Draws horizontal grid lines across the plot area. */
    private void drawHorizontalGridLines(Page page) {
        page.setPenWidth(hGridLineWidth);
        page.setPenColor(Color.black);
        page.setStrokeDashPattern(hGridLinePattern);
        float x = x8;
        float y = y8;
        float step = (y8 - y5) / yAxisGridLines;
        for (int i = 0; i < yAxisGridLines; i++) {
            page.drawLine(x, y, x6, y);
            y -= step;
        }
    }

    /** Draws vertical grid lines across the plot area. */
    private void drawVerticalGridLines(Page page) {
        page.setPenWidth(vGridLineWidth);
        page.setPenColor(Color.black);
        page.setStrokeDashPattern(vGridLinePattern);
        float x = x5;
        float y = y5;
        float step = (x6 - x5) / xAxisGridLines;
        for (int i = 0; i < xAxisGridLines; i++) {
            page.drawLine(x, y, x, y8);
            x += step;
        }
    }

    /** Draws X axis labels (one per grid line interval). */
    private void drawXAxisLabels(Page page) {
        float x = x5;
        float y = y8 + f2.getBodyHeight(f2.getSize());
        float step = (x6 - x5) / xAxisGridLines;
        page.setBrushColor(Color.black);
        for (int i = 0; i < (xAxisGridLines + 1); i++) {
            String label = nf.format(xMin + ((xMax - xMin) / xAxisGridLines) * i);
            page.drawString(f2, fontSize, label, x - (f2.stringWidth(label) / 2), y);
            x += step;
        }
    }

    /** Draws Y axis labels (one per grid line interval). */
    private void drawYAxisLabels(Page page) {
        float x = x5 - getLongestAxisYLabelWidth();
        float y = y8 + f2.ascent / 3;
        float step = (y8 - y5) / yAxisGridLines;
        page.setBrushColor(Color.black);
        for (int i = 0; i < (yAxisGridLines + 1); i++) {
            String label = nf.format(yMin + ((yMax - yMin) / yAxisGridLines) * i);
            page.drawString(f2, fontSize, label, x, y);
            y -= step;
        }
    }

    /**
     * Converts a 0xRRGGBB color to its red, green and blue components.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] toFloatArray(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        return new float[] {r, g, b};
    }

    /** Draws connecting paths, point markers, and point text. */
    private void drawPathsAndPoints(
            Page page, List<List<Point>> chartData) throws Exception {
        int seriesIndex = 0;
        for (List<Point> points : chartData) {
            Point p0 = points.get(0);
            if (p0.drawPath) {
                if (autoColors && p0.strokeColor == null) {
                    int index = seriesIndex % DEFAULT_PALETTE.length;
                    p0.strokeColor = toFloatArray(DEFAULT_PALETTE[index]);
                }
                page.setPenColor(p0.strokeColor);
                page.setPenWidth(p0.strokeWidth);
                page.setStrokeDashPattern(p0.strokeDashPattern);
                page.drawPath(points, PathOperator.STROKE);
                if (p0.getText() != null) {
                    page.setBrushColor(p0.getTextColor());
                    page.setTextDirection(p0.getTextDirection());
                    page.drawString(
                            f2,
                            null,
                            fontSize,
                            p0.getText(),
                            p0.x + (p0.strokeWidth - f2.getAscent())/2f,
                            p0.y,
                            p0.getTextColor(),
                            null);
                }
            }
            for (Point point : points) {
                if (point.getShape() != Point.INVISIBLE) {
                    page.setPenColor(point.strokeColor);
                    page.setPenWidth(point.strokeWidth);
                    page.setStrokeDashPattern(point.strokeDashPattern);
                    page.setBrushColor(point.fillColor);
                    page.drawPoint(point);
                }
            }
            seriesIndex++;
        }
    }

    /**
     * Rounds axis range to "nice" values for clean grid lines.
     * Uses the span (max - min) to support negative values and
     * zero crossings. Rounds max up and min down to step multiples.
     */
    private Round roundMaxAndMinValues(float maxValue, float minValue) {
        float span = maxValue - minValue;
        if (span <= 0f) { span = 1f; }  // guard against flat data

        int exponent = (int) Math.floor(Math.log(span) / Math.log(10));
        float normalizedSpan = span * (float) Math.pow(10, -exponent);

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

        // Scale back to original magnitude and compute step
        float step = niceSpan * (float) Math.pow(10, exponent) / numOfGridLines;

        Round round = new Round();

        // Round max up, min down to nearest step multiple
        round.maxValue = (float) Math.ceil(maxValue / step) * step;
        round.minValue = (float) Math.floor(minValue / step) * step;

        // Recount grid lines from actual rounded range
        round.numOfGridLines = Math.round((round.maxValue - round.minValue) / step);

        return round;
    }

    /** Returns [mean_x, mean_y] for the given points. */
    private float[] mean(List<Point> points) {
        float[] _mean = new float[2];
        for (Point point : points) {
            _mean[0] += point.x;
            _mean[1] += point.y;
        }
        _mean[0] /= points.size();
        _mean[1] /= points.size();
        return _mean;
    }

    /** Returns the covariance of x and y. */
    private float covar(List<Point> points) {
        float covariance = 0f;
        float[] _mean = mean(points);
        for (Point point : points) {
            covariance += (point.x - _mean[0]) * (point.y - _mean[1]);
        }
        return (covariance / (points.size() - 1));
    }

    /** Returns the sum of squared deviations of x from mean_x. */
    private float devsq(List<Point> points) {
        float _devsq = 0f;
        float[] _mean = mean(points);
        for (Point point : points) {
            _devsq = _devsq + (float) Math.pow((point.x - _mean[0]), 2);
        }
        return _devsq;
    }

    /**
     *  Manually sets X axis range and grid line count.
     *  Skips auto-computation when grid lines > 0.
     *
     *  @param xMin for the X axis.
     *  @param xMax for the X axis.
     *  @param xAxisGridLines the number of X axis grid lines.
     *  @return this Chart object.
     */
    public Chart setXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.xMin = xMin;
        this.xMax = xMax;
        this.xAxisGridLines = xAxisGridLines;
        return this;
    }

    /**
     *  Manually sets Y axis range and grid line count.
     *  Skips auto-computation when grid lines > 0.
     *
     *  @param yMin for the Y axis.
     *  @param yMax for the Y axis.
     *  @param yAxisGridLines the number of Y axis grid lines.
     *  @return this Chart object.
     */
    public Chart setYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.yMin = yMin;
        this.yMax = yMax;
        this.yAxisGridLines = yAxisGridLines;
        return this;
    }
}   // End of Chart.java
