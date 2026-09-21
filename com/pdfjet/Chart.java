/*
 * Chart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.math.*;
import java.util.*;

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

    // The axis ranges that setXAxisMinMax and setYAxisMinMax set, used when
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

    private boolean drawHorizontalGridLines = true;
    private boolean drawVerticalGridLines = true;
    private boolean drawXAxisLabels = true;
    private boolean drawYAxisLabels = true;

    // Grid line styling (width 0 = the thinnest line, pattern default = dotted)
    private int gridLineColor = Color.black;
    private float horizontalGridLineWidth;
    private float verticalGridLineWidth;
    private String horizontalGridLineDashPattern = "[1 1] 0";
    private String verticalGridLineDashPattern = "[1 1] 0";

    private float axisLineWidth = 0.5f;
    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    // Label number formatting
    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;

    // f1 = chart title font, f2 = axis title/label font
    private Font f1;
    private Font f2;

    private final List<Series> series = new ArrayList<Series>();
    private boolean drawLegend = true;
    private String altDescription = null;

    static final int[] DEFAULT_PALETTE = {
        Color.blue,
        Color.red,
        Color.green,
        Color.orange,
        Color.purple,
        Color.darkcyan,
        Color.magenta,
        Color.olive
    };

    /**
     *  Creates an XY chart.
     *
     *  @param f1 the font for the chart title.
     *  @param f2 the font for axis titles and labels.
     */
    public Chart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
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
     * Sets the subtitle, written in gray under the title in the second font.
     *
     * @param subtitle the subtitle.
     * @return this Chart object.
     */
    public Chart setSubtitle(String subtitle) {
        this.subtitle = subtitle;
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
     * Adds a series and returns it, to add its points and set its line and
     * marker. A series without a stroke color has the next color of the
     * palette. The legend lists the series that have a name.
     *
     * @param name the series name, shown in the legend; empty for none.
     * @return the new Series object.
     */
    public Series addSeries(String name) {
        Series s = new Series(name);
        series.add(s);
        return s;
    }

    /**
     * Sets whether the legend is drawn. The legend lists the series that have
     * a name, under the title, each with its line or its marker.
     *
     * @param drawLegend true to draw the legend.
     * @return this Chart object.
     */
    public Chart setDrawLegend(boolean drawLegend) {
        this.drawLegend = drawLegend;
        return this;
    }

    /**
     * Sets the alternate description of the chart, which a screen reader reads
     * in a PDF/UA document, where the chart is a figure. The default is the
     * title, or "Chart" without one. Describe what the chart shows.
     *
     * @param altDescription the alternate description.
     * @return this Chart object.
     */
    public Chart setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
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
    public Chart setSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /**
     * Sets the minimum number of decimal places in the axis labels. The labels
     * of an axis have at least the decimal places of its step, so an axis with
     * a whole number step has whole number labels. The default is 0.
     *
     * @param minFractionDigits the minimum number of decimal places.
     * @return this Chart object.
     */
    public Chart setMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /**
     * Sets the maximum number of decimal places in the axis labels. The
     * default is 2.
     *
     * @param maxFractionDigits the maximum number of decimal places.
     * @return this Chart object.
     */
    public Chart setMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /**
     * Sets whether the horizontal grid lines are drawn.
     *
     * @param drawHorizontalGridLines true to draw them.
     * @return this Chart object.
     */
    public Chart setDrawHorizontalGridLines(boolean drawHorizontalGridLines) {
        this.drawHorizontalGridLines = drawHorizontalGridLines;
        return this;
    }

    /**
     * Sets whether the vertical grid lines are drawn.
     *
     * @param drawVerticalGridLines true to draw them.
     * @return this Chart object.
     */
    public Chart setDrawVerticalGridLines(boolean drawVerticalGridLines) {
        this.drawVerticalGridLines = drawVerticalGridLines;
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
     * Sets the width of the axis lines, along the left and the bottom sides of
     * the plot area. The default is 0.5; 0 hides them.
     *
     * @param width the line width.
     * @return this Chart object.
     */
    public Chart setAxisLineWidth(float width) {
        this.axisLineWidth = width;
        return this;
    }

    /**
     * Sets the width of the outer chart border. A width of 0, the default,
     * hides it.
     *
     * @param width the border width.
     * @return this Chart object.
     */
    public Chart setChartBorderWidth(float width) {
        this.chartBorderWidth = width;
        return this;
    }

    /**
     * Sets the width of the plot area border. A width of 0, the default,
     * hides it.
     *
     * @param width the border width.
     * @return this Chart object.
     */
    public Chart setInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
        return this;
    }

    /**
     * Sets the width of the horizontal grid lines. A width of 0 draws the thinnest
     * line a viewer shows; setDrawHorizontalGridLines(false) hides them.
     *
     * @param width the line width.
     * @return this Chart object.
     */
    public Chart setHorizontalGridLineWidth(float width) {
        this.horizontalGridLineWidth = width;
        return this;
    }

    /**
     * Sets the width of the vertical grid lines. A width of 0 draws the thinnest
     * line a viewer shows; setDrawVerticalGridLines(false) hides them.
     *
     * @param width the line width.
     * @return this Chart object.
     */
    public Chart setVerticalGridLineWidth(float width) {
        this.verticalGridLineWidth = width;
        return this;
    }

    /**
     * Sets the dash pattern of the horizontal grid lines, for example "[1 1] 0".
     *
     * @param pattern the dash pattern.
     * @return this Chart object.
     */
    public Chart setHorizontalGridLineDashPattern(String pattern) {
        this.horizontalGridLineDashPattern = pattern;
        return this;
    }

    /**
     * Sets the dash pattern of the vertical grid lines, for example "[1 1] 0".
     *
     * @param pattern the dash pattern.
     * @return this Chart object.
     */
    public Chart setVerticalGridLineDashPattern(String pattern) {
        this.verticalGridLineDashPattern = pattern;
        return this;
    }

    /**
     * Sets the color of the grid lines. The default is black.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.lightgray.
     * @return this Chart object.
     */
    public Chart setGridLineColor(int color) {
        this.gridLineColor = color;
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
        if (page == null || !hasPoints()) {    // Measured, or nothing to draw
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
        setXAxisRange();
        setYAxisRange();

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, null, altDescription());
        // The pen, the brush and the dash pattern of the caller are kept, as
        // a Stamp and a CalendarMonth keep them: the chart set them to its
        // own and then to the default of a page, which is not what the
        // caller had.
        page.saveGraphicsState();

        // Draw chart title (centered, top), the subtitle and then the legend under it
        float titleBaseline = y1 + 1.5f * f1.bodyHeight;
        float subtitleHeight = subtitle.isEmpty() ? 0f : f2.bodyHeight;
        page.setBrushColor(Color.black);
        page.drawString(
                f1,
                f1.getSize(),
                title,
                x1 + ((w - f1.stringWidth(title)) / 2),
                titleBaseline);
        if (!subtitle.isEmpty()) {
            page.drawString(
                    f2,
                    f2.getSize(),
                    subtitle,
                    x1 + ((w - f2.stringWidth(subtitle)) / 2),
                    titleBaseline + subtitleHeight, Util.toRGB(Color.dimgray), null);
        }
        boolean legend = drawLegend && hasSeriesNames();
        if (legend) {
            drawLegend(page, titleBaseline + subtitleHeight + 1.5f * f2.bodyHeight);
        }

        // Compute margins and inner plot area
        float topMargin = 2.5f * f1.bodyHeight + subtitleHeight + (legend ? 1.5f * f2.bodyHeight : 0f);
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

        if (drawHorizontalGridLines) {
            drawHorizontalGrid(page);
        }
        if (drawVerticalGridLines) {
            drawVerticalGrid(page);
        }
        if (axisLineWidth > 0f) {
            drawAxisLines(page);
        }
        if (drawXAxisLabels) {
            drawXAxisLabels(page);
        }
        if (drawYAxisLabels) {
            drawYAxisLabels(page);
        }

        // Defensive copy so the user's data is never mutated. A point added by
        // its coordinates has the marker the series has now.
        List<List<Point>> plotData = new ArrayList<List<Point>>(series.size());
        for (Series s : series) {
            List<Point> copy = new ArrayList<Point>(s.points.size());
            for (int i = 0; i < s.points.size(); i++) {
                Point point = new Point(s.points.get(i));
                if (s.seriesMarker.get(i)) {
                    point.shape = s.shape;
                    point.r = s.radius;
                }
                copy.add(point);
            }
            plotData.add(copy);
        }

        // Translate data coordinates to page coordinates (on the copies)
        for (List<Point> points : plotData) {
            for (Point point : points) {
                point.x = x5 + (point.x - xMin) * (x6 - x5) / (xMax - xMin);
                point.y = y8 - (point.y - yMin) * (y8 - y5) / (yMax - yMin);
                if (point.getURIAction() != null) {
                    page.addAnnotation(new Annotation(
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
        page.setTextRotation(-90);
        page.drawString(
                f2,
                f2.getSize(),
                yAxisTitle,
                x1 + f2.bodyHeight,
                y8 - ((y8 - y5) - f2.stringWidth(yAxisTitle)) / 2);

        // Draw X axis title
        page.setTextRotation(0);
        page.setBrushColor(Color.black);
        page.drawString(
                f2,
                f2.getSize(),
                xAxisTitle,
                x5 + ((x6 - x5) - f2.stringWidth(xAxisTitle)) / 2,
                y4 - f2.bodyHeight / 2);

        page.restoreGraphicsState();
        page.addEMC();

        return new float[] {this.x1 + this.w, this.y1 + this.h};
    }

    // Returns the alternate description, or the title when none is set.
    private String altDescription() {
        if (altDescription != null && !altDescription.isEmpty()) {
            return altDescription;
        }
        return title.isEmpty() ? "Chart" : title;
    }

    /** Returns true if at least one series has points. */
    private boolean hasPoints() {
        for (Series s : series) {
            if (!s.points.isEmpty()) {
                return true;
            }
        }
        return false;
    }

    /** Returns true if a series has a name to list in the legend. */
    private boolean hasSeriesNames() {
        for (Series s : series) {
            if (!s.name.isEmpty()) {
                return true;
            }
        }
        return false;
    }

    /** Returns the color of the series at the index: its own or the palette's. */
    private float[] seriesColor(Series s, int index) {
        return s.strokeColor != null ? s.strokeColor : toFloatArray(DEFAULT_PALETTE[index % DEFAULT_PALETTE.length]);
    }

    /**
     * Draws the legend centered on the chart: the line of each named series
     * that draws its path, its marker when it has one, and its name.
     */
    private void drawLegend(Page page, float baseline) throws Exception {
        float ascent = f2.getAscent();
        float sample = 2f * ascent;     // the width of the line or the marker
        float gap = ascent / 2f;
        float width = 0f;
        int entries = 0;
        for (Series s : series) {
            if (!s.name.isEmpty()) {
                width += sample + gap + f2.stringWidth(s.name);
                entries++;
            }
        }
        width += (entries - 1) * f2.bodyHeight;
        float x = x1 + (w - width) / 2f;
        for (int j = 0; j < series.size(); j++) {
            Series s = series.get(j);
            if (s.name.isEmpty()) {
                continue;
            }
            float[] color = seriesColor(s, j);
            float yMid = baseline - ascent / 2f;
            page.setPenColor(color);
            if (s.drawPath) {
                page.setPenWidth(s.strokeWidth);
                page.setStrokeDashPattern(s.strokeDashPattern);
                page.drawLine(x, yMid, x + sample, yMid);
            }
            if (s.shape != Shape.INVISIBLE) {
                page.setPenWidth(1f);
                page.setDefaultStrokeDashPattern();
                page.drawPoint(new Point(x + sample / 2f, yMid).setShape(s.shape).setRadius(s.radius));
            }
            x += sample + gap;
            page.setBrushColor(Color.black);
            page.drawString(f2, f2.getSize(), s.name, x, baseline);
            x += f2.stringWidth(s.name) + f2.bodyHeight;
        }
        page.setDefaultPenWidth();
        page.setDefaultStrokeDashPattern();
    }

    /**
     * Formats a label with minDigits to maxDigits decimal places, rounding the
     * exact value half to even. The label has a "." decimal separator and no
     * grouping whatever the default locale, and a value that rounds to zero
     * has no minus sign.
     */
    static String format(float value, int minDigits, int maxDigits) {
        if (Float.isNaN(value) || Float.isInfinite(value)) {
            return String.valueOf(value);
        }
        // A minimum above the maximum is lowered to it, as in NumberFormat
        maxDigits = Math.max(maxDigits, 0);
        minDigits = Math.min(Math.max(minDigits, 0), maxDigits);
        BigDecimal label = new BigDecimal(value)
                .setScale(maxDigits, RoundingMode.HALF_EVEN)
                .stripTrailingZeros();
        if (label.scale() < minDigits) {
            label = label.setScale(minDigits);
        }
        return label.toPlainString();
    }

    /**
     * Returns the number of decimal places, at most maxDigits, that write the
     * axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
     */
    static int fractionDigitsOf(float step, int maxDigits) {
        for (int digits = 0; digits < maxDigits; digits++) {
            double scaled = step * Math.pow(10, digits);
            if (Math.abs(scaled - Math.round(scaled)) < 1e-4) {
                return digits;
            }
        }
        return Math.max(maxDigits, 0);
    }

    /** Formats the label of an axis with the specified step. */
    private String format(float value, float step) {
        int digits = Math.max(minFractionDigits, fractionDigitsOf(step, maxFractionDigits));
        return format(value, digits, maxFractionDigits);
    }

    /** Returns the width of the widest Y axis label (for left margin). */
    private float getLongestAxisYLabelWidth() {
        float step = (yMax - yMin) / yAxisGridLines;
        float minLabelWidth = f2.stringWidth(format(yMin, step) + "0");
        float maxLabelWidth = f2.stringWidth(format(yMax, step) + "0");
        if (maxLabelWidth > minLabelWidth) {
            return maxLabelWidth;
        }
        return minLabelWidth;
    }

    /**
     * Sets the X axis range of the drawing: the one set with setXAxisMinMax,
     * or the range of the data rounded to "nice" values, with its grid lines.
     */
    private void setXAxisRange() {
        if (manualXGridLines > 0) {
            xMin = manualXMin;
            xMax = manualXMax;
            xAxisGridLines = manualXGridLines;
        } else {
            xMin = Float.MAX_VALUE;
            xMax = -Float.MAX_VALUE;
            for (Series s : series) {
                for (Point point : s.points) {
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
            Round round = roundMaxAndMinValues(xMax, xMin);
            xMax = round.maxValue;
            xMin = round.minValue;
            xAxisGridLines = round.numOfGridLines;
        }
    }

    /**
     * Sets the Y axis range of the drawing: the one set with setYAxisMinMax,
     * or the range of the data rounded to "nice" values, with its grid lines.
     */
    private void setYAxisRange() {
        if (manualYGridLines > 0) {
            yMin = manualYMin;
            yMax = manualYMax;
            yAxisGridLines = manualYGridLines;
        } else {
            yMin = Float.MAX_VALUE;
            yMax = -Float.MAX_VALUE;
            for (Series s : series) {
                for (Point point : s.points) {
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
            Round round = roundMaxAndMinValues(yMax, yMin);
            yMax = round.maxValue;
            yMin = round.minValue;
            yAxisGridLines = round.numOfGridLines;
        }
    }

    /** Draws the outer chart border, unless its width is 0. */
    private void drawChartBorder(Page page) {
        if (chartBorderWidth <= 0f) {
            return;
        }
        page.setPenWidth(chartBorderWidth);
        page.setPenColor(Color.black);
        page.moveTo(x1, y1);
        page.lineTo(x2, y2);
        page.lineTo(x3, y3);
        page.lineTo(x4, y4);
        page.closePath();
    }

    /** Draws the inner plot area border, unless its width is 0. */
    private void drawInnerBorder(Page page) {
        if (innerBorderWidth <= 0f) {
            return;
        }
        page.setPenWidth(innerBorderWidth);
        page.setPenColor(Color.black);
        page.moveTo(x5, y5);
        page.lineTo(x6, y6);
        page.lineTo(x7, y7);
        page.lineTo(x8, y8);
        page.closePath();
    }

    /** Draws the axis lines along the left and the bottom sides of the plot area. */
    private void drawAxisLines(Page page) {
        page.setPenWidth(axisLineWidth);
        page.setPenColor(Color.black);
        page.setDefaultStrokeDashPattern();
        page.drawLine(x5, y5, x5, y8);
        page.drawLine(x8, y8, x6, y8);
    }

    /** Draws horizontal grid lines across the plot area, one at each label. */
    private void drawHorizontalGrid(Page page) {
        page.setPenWidth(horizontalGridLineWidth);
        page.setPenColor(gridLineColor);
        page.setStrokeDashPattern(horizontalGridLineDashPattern);
        float x = x8;
        float y = y8;
        float step = (y8 - y5) / yAxisGridLines;
        for (int i = 0; i <= yAxisGridLines; i++) {
            page.drawLine(x, y, x6, y);
            y -= step;
        }
    }

    /** Draws vertical grid lines across the plot area, one at each label. */
    private void drawVerticalGrid(Page page) {
        page.setPenWidth(verticalGridLineWidth);
        page.setPenColor(gridLineColor);
        page.setStrokeDashPattern(verticalGridLineDashPattern);
        float x = x5;
        float y = y5;
        float step = (x6 - x5) / xAxisGridLines;
        for (int i = 0; i <= xAxisGridLines; i++) {
            page.drawLine(x, y, x, y8);
            x += step;
        }
    }

    /** Draws X axis labels (one per grid line interval). */
    private void drawXAxisLabels(Page page) {
        float x = x5;
        float y = y8 + f2.getBodyHeight(f2.getSize());
        float step = (x6 - x5) / xAxisGridLines;
        float valueStep = (xMax - xMin) / xAxisGridLines;
        page.setBrushColor(Color.black);
        for (int i = 0; i < (xAxisGridLines + 1); i++) {
            String label = format(xMin + valueStep * i, valueStep);
            page.drawString(f2, f2.getSize(), label, x - (f2.stringWidth(label) / 2), y);
            x += step;
        }
    }

    /** Draws Y axis labels (one per grid line interval). */
    private void drawYAxisLabels(Page page) {
        float x = x5 - getLongestAxisYLabelWidth();
        float y = y8 + f2.ascent / 3;
        float step = (y8 - y5) / yAxisGridLines;
        float valueStep = (yMax - yMin) / yAxisGridLines;
        page.setBrushColor(Color.black);
        for (int i = 0; i < (yAxisGridLines + 1); i++) {
            String label = format(yMin + valueStep * i, valueStep);
            page.drawString(f2, f2.getSize(), label, x, y);
            y -= step;
        }
    }

    /**
     * Converts a 0xRRGGBB color to its red, green and blue components.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    private float[] toFloatArray(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        return new float[] {r, g, b};
    }

    /**
     * Draws the line of each series that draws its path, then the markers of
     * its points: a point without a stroke color in the color of the series.
     */
    private void drawPathsAndPoints(
            Page page, List<List<Point>> plotData) throws Exception {
        for (int j = 0; j < series.size(); j++) {
            Series s = series.get(j);
            List<Point> points = plotData.get(j);
            if (points.isEmpty()) {
                continue;
            }
            float[] color = seriesColor(s, j);
            if (s.drawPath) {
                page.setPenColor(color);
                page.setPenWidth(s.strokeWidth);
                page.setStrokeDashPattern(s.strokeDashPattern);
                page.drawPath(points, PathOperator.STROKE);
            }
            for (Point point : points) {
                if (point.shape != Shape.INVISIBLE) {
                    page.setPenColor(point.strokeColor != null ? point.strokeColor : color);
                    page.setPenWidth(point.strokeWidth);
                    page.setDefaultStrokeDashPattern();
                    page.setBrushColor(point.fillColor);
                    page.drawPoint(point);
                }
            }
        }
    }

    /**
     * Rounds axis range to "nice" values for clean grid lines.
     * Uses the span (max - min) to support negative values and
     * zero crossings. Rounds max up and min down to step multiples.
     */
    static Round roundMaxAndMinValues(float maxValue, float minValue) {
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

    /**
     *  Sets the X axis range and its number of grid lines. With 0 grid lines,
     *  the default, the range is the range of the data rounded to "nice"
     *  values, and xMin and xMax are not used; a negative number is taken as 0.
     *
     *  @param xMin for the X axis.
     *  @param xMax for the X axis.
     *  @param xAxisGridLines the number of X axis grid lines.
     *  @return this Chart object.
     */
    public Chart setXAxisMinMax(float xMin, float xMax, int xAxisGridLines) {
        this.manualXMin = xMin;
        this.manualXMax = xMax;
        this.manualXGridLines = Math.max(0, xAxisGridLines);
        return this;
    }

    /**
     *  Sets the Y axis range and its number of grid lines. With 0 grid lines,
     *  the default, the range is the range of the data rounded to "nice"
     *  values, and yMin and yMax are not used; a negative number is taken as 0.
     *
     *  @param yMin for the Y axis.
     *  @param yMax for the Y axis.
     *  @param yAxisGridLines the number of Y axis grid lines.
     *  @return this Chart object.
     */
    public Chart setYAxisMinMax(float yMin, float yMax, int yAxisGridLines) {
        this.manualYMin = yMin;
        this.manualYMax = yMax;
        this.manualYGridLines = Math.max(0, yAxisGridLines);
        return this;
    }
}   // End of Chart.java
