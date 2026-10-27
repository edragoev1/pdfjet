/*
 * BarChart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Bar chart renderer for PDF pages: one slot per category, the bars of the
 * series grouped inside each slot. See Example_27, of horizontal and
 * vertical bars.
 */
public class BarChart implements Drawable {
    /**
     * One series of the chart: a name, a value per category and a color, or
     * a color per category.
     */
    private static final class Series {
        final String name;
        final float[] values;
        final int color;
        final int[] colors;     // null unless each bar has its own color
        Series(String name, float[] values, int color, int[] colors) {
            this.name = name;
            this.values = values;
            this.color = color;
            this.colors = colors;
        }
    }

    private static final int NO_COLOR = -1;

    // The look of the simple charts: ink for the title and the values, a
    // muted gray for the rest of the text, light grid lines, and a palette
    // whose colors differ in lightness, so that they stay apart in grayscale
    // and for a reader who does not tell red from green.
    private static final int INK = SimpleBarChart.INK;
    private static final int MUTED = SimpleBarChart.MUTED;
    private static final int GRID = 0xE1E4E8;
    private static final int BASELINE = 0x8C96A3;
    static final int[] PALETTE = {
        0x1F3A5F,   // navy
        0x6F95C2,   // steel blue
        0xD08C3C,   // amber
        0x76987C,   // sage
        0xA7AFB8,   // gray
        0x9C5B5B,   // brick
        0x4F7A8C,   // slate
        0xC9B458    // ochre
    };

    private float x1;
    private float y1;
    private float w = 300f;
    private float h = 200f;

    private String title = "";
    private String subtitle = "";
    private String altDescription = null;
    private String xAxisTitle = "";
    private String yAxisTitle = "";

    private final List<String> categories = new ArrayList<String>();
    private final List<Series> series = new ArrayList<Series>();
    private final List<Series> lineSeries = new ArrayList<Series>();   // Drawn as lines over the bars

    private boolean horizontal = false;
    private boolean stacked = false;
    private float groupGap = 0.3f;
    private float barGap = 0f;

    private boolean drawGridLines = true;
    private boolean drawValueLabels = false;
    private boolean valueLabelsInside = false;
    private boolean drawLegend = true;
    private boolean groupingUsed = true;

    private int gridLineColor = GRID;

    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;

    // The range of the value axis, from the data unless it is set
    private float min;
    private float max;
    private boolean rangeSet = false;

    // f1 = chart title font, f2 = axis titles, labels and legend font
    private final Font f1;
    private final Font f2;

    /**
     * Creates a bar chart.
     *
     * @param f1 the font for the chart title.
     * @param f2 the font for the axis titles, the labels and the legend.
     */
    public BarChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /**
     * Sets the chart title.
     *
     * @param title the title.
     * @return this BarChart object.
     */
    public BarChart setTitle(String title) {
        this.title = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the subtitle, written in gray under the title in the second font.
     *
     * @param subtitle the subtitle.
     * @return this BarChart object.
     */
    public BarChart setSubtitle(String subtitle) {
        this.subtitle = (subtitle == null) ? "" : subtitle;
        return this;
    }

    /**
     * Sets the alternate description of the chart, which a screen reader reads
     * in a PDF/UA document, where the chart is a figure. The default is the
     * title and the subtitle, or "Bar chart" without them, and the values of
     * each series by category. Describe what the chart shows.
     *
     * @param altDescription the alternate description.
     * @return this BarChart object.
     */
    public BarChart setAltDescription(String altDescription) {
        this.altDescription = (altDescription == null) ? "" : altDescription;
        return this;
    }

    /**
     * Sets the X axis title.
     *
     * @param title the title.
     * @return this BarChart object.
     */
    public BarChart setXAxisTitle(String title) {
        this.xAxisTitle = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the Y axis title.
     *
     * @param title the title.
     * @return this BarChart object.
     */
    public BarChart setYAxisTitle(String title) {
        this.yAxisTitle = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the categories, one per group of bars, in the order they are drawn:
     * left to right in a vertical chart, top to bottom in a horizontal one.
     *
     * @param categories the category labels.
     * @return this BarChart object.
     */
    public BarChart setCategories(String... categories) {
        this.categories.clear();
        if (categories != null) {
            Collections.addAll(this.categories, categories);
        }
        return this;
    }

    /**
     * Adds a series drawn in the next color of the default palette. A value
     * that is NaN or infinite has no bar.
     *
     * @param name the series name, shown in the legend; empty for none.
     * @param values one value per category.
     * @return this BarChart object.
     */
    public BarChart addSeries(String name, float[] values) {
        return addSeries(name, values, NO_COLOR);
    }

    /**
     * Adds a series drawn in the specified color.
     *
     * @param name the series name, shown in the legend; empty for none.
     * @param values one value per category.
     * @param color the bar color as a 0xRRGGBB value, for example Color.blue.
     * @return this BarChart object.
     */
    public BarChart addSeries(String name, float[] values, int color) {
        series.add(new Series(name == null ? "" : name, values == null ? new float[0] : values.clone(), color, null));
        return this;
    }

    /**
     * Adds a series with a color per category, for a chart whose bars each
     * have their own color. A bar past the end of the colors has the next
     * color of the default palette.
     *
     * @param name the series name, shown in the legend; empty for none.
     * @param values one value per category.
     * @param colors one 0xRRGGBB color per category.
     * @return this BarChart object.
     */
    public BarChart addSeries(String name, float[] values, int[] colors) {
        series.add(new Series(name == null ? "" : name, values == null ? new float[0] : values.clone(), NO_COLOR,
                colors == null ? null : colors.clone()));
        return this;
    }

    /**
     * Adds a series drawn as a line over the bars, in ink, a dot at each
     * category: a target, the average, or last year. It shares the value
     * axis with the bars, is not stacked, and is in the legend and the
     * description. A value that is NaN or infinite leaves a gap in the line.
     *
     * @param name the series name, shown in the legend; empty for none.
     * @param values one value per category.
     * @return this BarChart object.
     */
    public BarChart addLineSeries(String name, float[] values) {
        return addLineSeries(name, values, INK);
    }

    /**
     * Adds a series drawn as a line over the bars, in the specified color.
     *
     * @param name the series name, shown in the legend; empty for none.
     * @param values one value per category.
     * @param color the line color as a 0xRRGGBB value.
     * @return this BarChart object.
     */
    public BarChart addLineSeries(String name, float[] values, int color) {
        lineSeries.add(new Series(name == null ? "" : name, values == null ? new float[0] : values.clone(), color, null));
        return this;
    }

    /**
     * Sets the location of the top left corner of this chart.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this BarChart object.
     */
    public BarChart setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the size of this chart.
     *
     * @param w the width.
     * @param h the height.
     * @return this BarChart object.
     */
    public BarChart setSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /**
     * Sets whether the bars are horizontal. The default is vertical bars.
     *
     * @param horizontal true for horizontal bars.
     * @return this BarChart object.
     */
    public BarChart setHorizontal(boolean horizontal) {
        this.horizontal = horizontal;
        return this;
    }

    /**
     * Sets whether the series are stacked: one bar per category, with the
     * value of each series as a segment of it. The values above 0 stack up
     * from 0 and the values below 0 stack down. The default is grouped bars.
     *
     * @param stacked true for stacked bars.
     * @return this BarChart object.
     */
    public BarChart setStacked(boolean stacked) {
        this.stacked = stacked;
        return this;
    }

    /**
     * Sets the gap between the groups of bars as a fraction of the category
     * slot, from 0.0 to 0.9. The default is 0.3.
     *
     * @param gap the gap.
     * @return this BarChart object.
     */
    public BarChart setGroupGap(float gap) {
        // From 0 to 0.9 of a slot: more left the bars no width, or less than none
        if (ChartScale.isValue(gap)) {
            this.groupGap = Math.min(Math.max(gap, 0f), 0.9f);
        }
        return this;
    }

    /**
     * Sets the gap between the bars of a group as a fraction of the bar width.
     * The default is 0.0, so the bars of a group touch.
     *
     * @param gap the gap.
     * @return this BarChart object.
     */
    public BarChart setBarGap(float gap) {
        // 0 or more: a negative gap made bars of no width, and -1 divided by 0
        if (ChartScale.isValue(gap)) {
            this.barGap = Math.max(gap, 0f);
        }
        return this;
    }

    /**
     * Sets whether the grid lines of the value axis are drawn.
     *
     * @param drawGridLines true to draw them.
     * @return this BarChart object.
     */
    public BarChart setGridLines(boolean drawGridLines) {
        this.drawGridLines = drawGridLines;
        return this;
    }

    /**
     * Sets whether the value of each bar is written at its end, or, in a
     * stacked chart, inside each segment that has room for it.
     *
     * @param drawValueLabels true to write the values.
     * @return this BarChart object.
     */
    public BarChart setValueLabels(boolean drawValueLabels) {
        this.drawValueLabels = drawValueLabels;
        return this;
    }

    /**
     * Sets whether the value labels are written inside the bars, at the end
     * of each bar, in white or in ink, whichever reads on the bar's color,
     * instead of next to the bar ends. A bar too short for its label gets it
     * next to its end. The default is false.
     *
     * @param valueLabelsInside true to write the values inside the bars.
     * @return this BarChart object.
     */
    public BarChart setValueLabelsInside(boolean valueLabelsInside) {
        this.valueLabelsInside = valueLabelsInside;
        return this;
    }

    /**
     * Sets whether the labels group the digits in thousands with a comma, as
     * in 6,650. The default is true.
     *
     * @param groupingUsed true to group the digits.
     * @return this BarChart object.
     */
    public BarChart setGroupingUsed(boolean groupingUsed) {
        this.groupingUsed = groupingUsed;
        return this;
    }

    /**
     * Sets whether the legend is drawn. The legend lists the series that have
     * a name, under the title.
     *
     * @param drawLegend true to draw the legend.
     * @return this BarChart object.
     */
    public BarChart setLegend(boolean drawLegend) {
        this.drawLegend = drawLegend;
        return this;
    }

    /**
     * Sets the color of the grid lines. The default is light gray, 0xE1E4E8.
     * Color.transparent leaves it unchanged.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.lightgray.
     * @return this BarChart object.
     */
    public BarChart setGridLineColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        this.gridLineColor = color;
        return this;
    }

    /**
     * Sets the number of digits after the decimal point of the values written
     * by the bars, all of them alike, as SimpleBarChart's. Without it a value
     * has as many as it needs, two at most. The numbers of the value axis have
     * the digits of its step.
     *
     * @param digits the number of digits.
     * @return this BarChart object.
     */
    public BarChart setFractionDigits(int digits) {
        this.minFractionDigits = digits;
        this.maxFractionDigits = digits;
        return this;
    }

    /**
     * Sets the range of the value axis, with grid lines on a round step, as
     * setYAxisRange does in SimpleLineChart and SimpleScatterChart. Without it
     * the range is that of the data, rounded to steps, and always includes 0.
     * A range whose end is not past its start is not set.
     *
     * @param min the value at the start of the axis.
     * @param max the value at the end of the axis.
     * @return this BarChart object.
     */
    public BarChart setValueAxisRange(float min, float max) {
        this.min = min;
        this.max = max;
        this.rangeSet = max > min;
        return this;
    }

    /**
     * Draws this chart on the specified page. A category name wider than
     * two fifths of a horizontal chart is shortened with an ellipsis, and a
     * chart too small for its plot draws nothing.
     *
     * @param page the page to draw on.
     * @return the bottom right corner coordinates [x, y].
     */
    public float[] drawOn(Page page) throws Exception {
        int n = numberOfCategories();
        if (page == null || n == 0 || f1 == null || f2 == null) {  // Measured, or nothing to draw
            return new float[] {x1 + w, y1 + h};
        }

        float vMin;
        float vMax;
        int lines;
        if (rangeSet) {
            vMin = min;
            vMax = max;
            lines = ChartScale.roundLines(min, max,
                    Math.max(ChartScale.roundMaxAndMinValues(max, min).numOfGridLines, 1));
        } else {
            float lo = 0f;
            float hi = 0f;
            if (stacked) {
                // The sums of the values above and below 0 in each category
                for (int i = 0; i < n; i++) {
                    float up = 0f;
                    float down = 0f;
                    for (Series s : series) {
                        if (i < s.values.length && ChartScale.isValue(s.values[i])) {
                            if (s.values[i] >= 0f) { up += s.values[i]; } else { down += s.values[i]; }
                        }
                    }
                    lo = Math.min(lo, down);
                    hi = Math.max(hi, up);
                }
                // The values of a line series up to the last category, which
                // are all it draws
                for (Series s : lineSeries) {
                    for (int i = 0; i < Math.min(s.values.length, n); i++) {
                        if (ChartScale.isValue(s.values[i])) {
                            lo = Math.min(lo, s.values[i]);
                            hi = Math.max(hi, s.values[i]);
                        }
                    }
                }
            } else {
                for (Series s : allSeries()) {
                    for (int i = 0; i < Math.min(s.values.length, n); i++) {
                        if (ChartScale.isValue(s.values[i])) {
                            lo = Math.min(lo, s.values[i]);
                            hi = Math.max(hi, s.values[i]);
                        }
                    }
                }
            }
            if (!ChartScale.isValue(hi - lo)) {
                // Values, or sums of a stack, whose span is past the largest
                // float, which no axis can draw: they were drawn on 0 to 1
                return new float[] {x1 + w, y1 + h};
            }
            if (hi == lo) {
                float[] flat = ChartScale.flatRange(lo);
                lo = flat[0];
                hi = flat[1];
            }
            Round round = ChartScale.roundMaxAndMinValues(hi, lo);
            vMin = round.minValue;
            vMax = round.maxValue;
            lines = round.numOfGridLines;
        }
        if (vMax == vMin) {
            float[] flat = ChartScale.flatRange(vMin);
            vMin = flat[0];
            vMax = flat[1];
        }
        float step = (vMax - vMin) / lines;
        // The digits of the step, which setFractionDigits does not cap: with 0
        // an axis of 0.25 was written 0, 0, 1, 1
        int axisDigits = Math.max(minFractionDigits, ChartScale.stepDigits(step));
        float base = Math.min(Math.max(0f, vMin), vMax);

        float x2 = x1 + w;
        float y2 = y1 + h;
        float bodyHeight = f2.getBodyHeight();
        float ascent = f2.getAscent();
        float pad = bodyHeight / 2f;
        boolean legend = drawLegend && hasSeriesNames();

        // Widest labels on the value axis and next to the bars
        float widestAxisLabel = 0f;
        for (int i = 0; i <= lines; i++) {
            String label = axisLabel(vMin + step * i, axisDigits);
            widestAxisLabel = Math.max(widestAxisLabel, f2.stringWidth(label));
        }
        float widestValueLabel = 0f;
        float widestNegative = 0f;      // Of the bars below 0, written past their ends
        // Of the labels that are written: not those of the bars outside a
        // range that is set, which made room for labels that were not there
        for (int i = 0; drawValueLabels && i < n; i++) {
            float up = 0f;
            float down = 0f;
            for (Series s : series) {
                if (i >= s.values.length || !ChartScale.isValue(s.values[i])) {
                    continue;
                }
                float v = s.values[i];
                float from = base;
                if (stacked) {
                    if (v >= 0f) { from = up; up += v; } else { from = down; down += v; }
                }
                float to = stacked ? from + v : v;
                if (outside(from, to, vMin, vMax)) {
                    continue;
                }
                float width = f2.stringWidth(valueLabel(v));
                widestValueLabel = Math.max(widestValueLabel, width);
                if (v < 0f) {
                    widestNegative = Math.max(widestNegative, width);
                }
            }
        }
        // The totals of the stacks, written past their ends
        boolean totals = drawValueLabels && stacked && series.size() > 1;
        float widestTotal = 0f;
        for (int i = 0; totals && i < n; i++) {
            float up = 0f;
            for (Series s : series) {
                if (i < s.values.length && ChartScale.isValue(s.values[i]) && s.values[i] >= 0f) {
                    up += s.values[i];
                }
            }
            if (up > 0f && ChartScale.isValue(up)) {    // A sum past the largest float is not written
                widestTotal = Math.max(widestTotal, f2.stringWidth(valueLabel(up)));
            }
        }
        // The names of the categories, shortened at the left of horizontal bars
        List<String> names = new ArrayList<>();
        float widestCategory = 0f;
        for (String category : categories) {
            // Two fifths of a horizontal chart at most, a quarter under vertical bars
            String name = ChartScale.shorten(f2, f2.getSize(), category, (horizontal ? 0.4f : 0.25f) * w);
            names.add(name);
            widestCategory = Math.max(widestCategory, f2.stringWidth(name));
        }

        // Margins and the plot area
        // The title, the subtitle and the legend, its rows wrapped at the
        // right edge; no room for a title the chart has not
        float titleBaseline = y1 + (title.isEmpty() ? 0f : 1.5f * f1.getBodyHeight());
        float subtitleHeight = subtitle.isEmpty() ? 0f : bodyHeight;
        int legendRows = legend ? drawLegend(null, 0f) : 0;
        float topMargin = (title.isEmpty() ? f1.getBodyHeight() : 2.5f * f1.getBodyHeight()) + subtitleHeight
                + legendRows * (1.5f * bodyHeight);
        // Room between the plot and the categories for the label of a bar
        // below 0 that reaches the end of the axis: it was written over the
        // category's name
        float negative = 0f;
        if (drawValueLabels && !stacked && widestNegative > 0f) {
            negative = horizontal ? widestNegative + pad / 2f : bodyHeight;
        }
        float leftMargin = 1.5f * bodyHeight + pad + (horizontal ? widestCategory + negative : widestAxisLabel);
        float rightMargin = pad;
        float bottomMargin = 2.5f * bodyHeight + (horizontal ? 0f : negative);
        if (horizontal) {
            float past = totals ? widestTotal + pad : (valueLabelsInside ? 0f : widestValueLabel + pad);
            rightMargin += Math.max(widestAxisLabel / 2f, past);
        } else if (totals || (drawValueLabels && !stacked && !valueLabelsInside)) {
            topMargin += bodyHeight;
        }
        float x5 = x1 + leftMargin;
        float y5 = y1 + topMargin;
        float x6 = x2 - rightMargin;
        float y8 = y2 - bottomMargin;
        // No room for the plot, or an axis whose span is past the largest
        // float, as a set range of -3e38 to 3e38, whose steps are infinite
        if (x6 <= x5 || y8 <= y5 || !ChartScale.isValue(step)) {
            return new float[] {x1 + w, y1 + h};
        }

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, null, altDescription());
        // The pen, the brush and the dash pattern of the caller are kept, as
        // a Stamp keeps them.
        page.saveGraphicsState();
        page.setDefaultStrokeDashPattern();     // Not the caller's, which dashed the legend's lines

        // Title, the subtitle and then the legend under it, at the left
        // The title and the subtitle shortened to the width of the chart
        drawText(page, f1, ChartScale.shorten(f1, f1.getSize(), title, w), x1, titleBaseline, INK);
        if (!subtitle.isEmpty()) {
            drawText(page, f2, ChartScale.shorten(f2, f2.getSize(), subtitle, w),
                    x1, titleBaseline + subtitleHeight, MUTED);
        }
        if (legend) {
            drawLegend(page, titleBaseline + subtitleHeight + 1.5f * bodyHeight);
        }

        // Grid lines and value axis labels. A label is drawn at every grid line
        // that leaves it room, the widest label and a pad across, or a line high
        // up the vertical axis: at every second or third line, or more, when
        // the labels would run into each other, the lines all drawn.
        int every = horizontal
                ? ChartScale.axisLabelStep((x6 - x5) / lines, widestAxisLabel + pad, lines)
                : ChartScale.axisLabelStep((y8 - y5) / lines, bodyHeight, lines);
        if (horizontal) {
            // Inside the chart, and apart when the first and the last are moved in
            float[] centers = new float[lines + 1];
            float[] widths = new float[lines + 1];
            for (int i = 0; i <= lines; i++) {
                float v = vMin + step * i;
                centers[i] = x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin);
                widths[i] = f2.stringWidth(axisLabel(v, axisDigits));
            }
            every = ChartScale.placedStep(every, centers, widths, x1, x2, pad / 2f);
        }
        for (int i = 0; i <= lines; i++) {
            float v = vMin + step * i;
            String label = axisLabel(v, axisDigits);
            boolean labeled = i % every == 0;
            if (horizontal) {
                float x = x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin);
                if (drawGridLines) {
                    gridLine(page, x, y5, x, y8);
                }
                if (labeled) {
                    drawText(page, f2, label, ChartScale.placed(x, f2.stringWidth(label), x1, x2),
                            y8 + bodyHeight, MUTED);
                }
            } else {
                float y = y8 - ChartScale.scaled(v - vMin, y8 - y5, vMax - vMin);
                if (drawGridLines) {
                    gridLine(page, x5, y, x6, y);
                }
                if (labeled) {
                    drawText(page, f2, label, x5 - pad - f2.stringWidth(label), y + ascent / 2f, MUTED);
                }
            }
        }

        // Bars, category labels and value labels
        int m = series.size();
        float slot = (horizontal ? (y8 - y5) : (x6 - x5)) / n;
        float groupWidth = slot * (1f - groupGap);
        float barWidth = (m == 0 || stacked) ? groupWidth : groupWidth / (m + (m - 1) * barGap);
        // Under vertical bars a category is written where it leaves the one
        // before it room: at every second or third bar, or more
        int everyCategory = horizontal ? 1 : ChartScale.axisLabelStep(slot, widestCategory + pad, n);
        if (!horizontal) {
            // Inside the chart, and apart when the first and the last are moved in
            float[] centers = new float[n];
            float[] widths = new float[n];
            for (int i = 0; i < n; i++) {
                centers[i] = x5 + i * slot + slot / 2f;
                widths[i] = i < names.size() ? f2.stringWidth(names.get(i)) : 0f;
            }
            everyCategory = ChartScale.placedStep(everyCategory, centers, widths, x1, x2, pad / 2f);
        }
        for (int i = 0; i < n; i++) {
            float slotStart = (horizontal ? y5 : x5) + i * slot;
            float groupStart = slotStart + slot * groupGap / 2f;
            String category = i < names.size() ? names.get(i) : "";
            if (horizontal) {
                drawText(page, f2, category, x5 - pad - negative - f2.stringWidth(category),
                        slotStart + slot / 2f + ascent / 2f, MUTED);
            } else if (i % everyCategory == 0) {
                // Centered under its slot, inside the chart: the last one
                // went past the chart's right edge
                drawText(page, f2, category, ChartScale.placed(slotStart + slot / 2f, f2.stringWidth(category), x1, x2),
                        y8 + bodyHeight + negative, MUTED);
            }
            float up = 0f;      // the stacked values above 0 so far
            float down = 0f;    // the stacked values below 0 so far
            for (int j = 0; j < m; j++) {
                Series s = series.get(j);
                if (i >= s.values.length || !ChartScale.isValue(s.values[i])) {
                    continue;
                }
                // A bar runs from the base to its value; a segment of a stack
                // from the sum of the segments before it to that sum plus its value
                float from = base;
                float to = s.values[i];
                if (stacked) {
                    if (to >= 0f) { from = up; up += to; } else { from = down; down += to; }
                    to = from + s.values[i];
                }
                // A bar wholly outside a range that is set has no length,
                // and its value is not written by it
                boolean outside = outside(from, to, vMin, vMax);
                from = Math.min(Math.max(from, vMin), vMax);
                to = Math.min(Math.max(to, vMin), vMax);
                float barStart = stacked ? groupStart : groupStart + j * barWidth * (1f + barGap);
                int color = barColor(s, j, i);
                page.setBrushColor(color);
                String label = (drawValueLabels && !outside) ? valueLabel(s.values[i]) : null;
                int on = labelColorOn(color);
                if (horizontal) {
                    float x0 = x5 + ChartScale.scaled(from - vMin, x6 - x5, vMax - vMin);
                    float x = x5 + ChartScale.scaled(to - vMin, x6 - x5, vMax - vMin);
                    page.fillRect(Math.min(x0, x), barStart, Math.abs(x - x0), barWidth);
                    float baseline = barStart + barWidth / 2f + ascent / 2f;
                    if (label != null && stacked) {
                        if (Math.abs(x - x0) >= f2.stringWidth(label) + pad) {
                            drawText(page, f2, label,
                                    Math.min(x0, x) + (Math.abs(x - x0) - f2.stringWidth(label)) / 2f, baseline, on);
                        }
                    } else if (label != null && valueLabelsInside && Math.abs(x - x0) >= f2.stringWidth(label) + pad) {
                        drawText(page, f2, label,
                                to >= base ? x - pad / 2f - f2.stringWidth(label) : x + pad / 2f, baseline, on);
                    } else if (label != null) {
                        drawText(page, f2, label,
                                to >= base ? x + pad / 2f : x - pad / 2f - f2.stringWidth(label), baseline, INK);
                    }
                } else {
                    float y0 = y8 - ChartScale.scaled(from - vMin, y8 - y5, vMax - vMin);
                    float y = y8 - ChartScale.scaled(to - vMin, y8 - y5, vMax - vMin);
                    page.fillRect(barStart, Math.min(y0, y), barWidth, Math.abs(y - y0));
                    float x = barStart + (barWidth - (label != null ? f2.stringWidth(label) : 0f)) / 2f;
                    if (label != null && stacked) {
                        if (Math.abs(y - y0) >= bodyHeight) {
                            drawText(page, f2, label, x, (y + y0) / 2f + ascent / 2f, on);
                        }
                    } else if (label != null && valueLabelsInside && Math.abs(y - y0) >= bodyHeight + pad) {
                        drawText(page, f2, label, x, to >= base ? y + ascent + pad / 2f : y - pad / 2f, on);
                    } else if (label != null) {
                        drawText(page, f2, label, x, to >= base ? y - pad / 2f : y + ascent + pad / 2f, INK);
                    }
                }
            }
            // The total of a stack above it, or past its end
            if (totals && up > 0f && ChartScale.isValue(up)) {     // A sum past the largest float is not written
                String total = valueLabel(up);
                float groupMiddle = groupStart + groupWidth / 2f;
                if (horizontal) {
                    float x = x5 + ChartScale.scaled(Math.min(up, vMax) - vMin, x6 - x5, vMax - vMin);
                    drawText(page, f2, total, x + pad / 2f, groupMiddle + ascent / 2f, INK);
                } else {
                    float y = y8 - ChartScale.scaled(Math.min(up, vMax) - vMin, y8 - y5, vMax - vMin);
                    drawText(page, f2, total, groupMiddle - f2.stringWidth(total) / 2f, y - pad / 2f, INK);
                }
            }
        }

        // The axis line at the base of the bars, which is the zero line of
        // negative values
        page.setPenColor(BASELINE);
        page.setPenWidth(0.75f);
        if (horizontal) {
            float x0 = x5 + ChartScale.scaled(base - vMin, x6 - x5, vMax - vMin);
            page.drawLine(x0, y5, x0, y8);
        } else {
            float y0 = y8 - ChartScale.scaled(base - vMin, y8 - y5, vMax - vMin);
            page.drawLine(x5, y0, x6, y0);
        }

        // The lines over the bars, through the middles of the categories, a
        // dot at each value, broken where a value is missing; each on a white
        // line a little wider, so that it stays clear where it crosses a bar
        for (Series s : lineSeries) {
            List<List<float[]>> runs = new ArrayList<>();
            List<float[]> run = null;
            for (int i = 0; i < n; i++) {
                if (i >= s.values.length || !ChartScale.isValue(s.values[i])) {
                    run = null;
                    continue;
                }
                float v = Math.min(Math.max(s.values[i], vMin), vMax);
                float middle = (horizontal ? y5 : x5) + i * slot + slot / 2f;
                float at = horizontal
                        ? x5 + ChartScale.scaled(v - vMin, x6 - x5, vMax - vMin)
                        : y8 - ChartScale.scaled(v - vMin, y8 - y5, vMax - vMin);
                if (run == null) {
                    run = new ArrayList<>();
                    runs.add(run);
                }
                run.add(horizontal ? new float[] {at, middle} : new float[] {middle, at});
            }
            page.setLineJoinStyle(JoinStyle.ROUND);
            float dot = 0.2f * bodyHeight;
            for (int pass = 0; pass < 2; pass++) {
                int color = (pass == 0) ? Color.white : s.color;
                page.setPenColor(color);
                page.setPenWidth((pass == 0) ? 3.5f : 2f);
                for (List<float[]> points : runs) {
                    page.moveTo(points.get(0)[0], points.get(0)[1]);
                    for (int k = 1; k < points.size(); k++) {
                        page.lineTo(points.get(k)[0], points.get(k)[1]);
                    }
                    page.strokePath();
                }
                page.setBrushColor(color);
                for (List<float[]> points : runs) {
                    for (float[] point : points) {
                        page.fillCircle(point[0], point[1], (pass == 0) ? dot + 0.75f : dot);
                    }
                }
            }
        }

        // Axis titles
        page.setTextRotation(-90);
        String yTitle = ChartScale.shorten(f2, f2.getSize(), yAxisTitle, y8 - y5);
        drawText(page, f2, yTitle, x1 + bodyHeight, y8 - ((y8 - y5) - f2.stringWidth(yTitle)) / 2f, MUTED);
        page.setTextRotation(0);
        String xTitle = ChartScale.shorten(f2, f2.getSize(), xAxisTitle, x6 - x5);
        drawText(page, f2, xTitle, x5 + ((x6 - x5) - f2.stringWidth(xTitle)) / 2f, y2 - bodyHeight / 2f, MUTED);

        page.restoreGraphicsState();
        page.setFigureBoundingBox(x1, y1, w, h);
        page.addEMC();

        return new float[] {x1 + w, y1 + h};
    }

    // The alternate description: the one set, or the title, the subtitle and,
    // for each series, its name and its values by category.
    private String altDescription() {
        if (altDescription != null && !altDescription.isEmpty()) {
            return altDescription;
        }
        StringBuilder sb = new StringBuilder(ChartScale.join(title, subtitle));
        if (sb.length() == 0) {
            sb.append("Bar chart");
        }
        String separator = ": ";
        for (Series s : allSeries()) {
            StringBuilder values = new StringBuilder();
            for (int i = 0; i < Math.min(s.values.length, numberOfCategories()); i++) {
                if (!ChartScale.isValue(s.values[i])) {
                    continue;
                }
                if (values.length() > 0) {
                    values.append(", ");
                }
                String category = i < categories.size() ? categories.get(i) : "";
                if (!category.isEmpty()) {
                    values.append(category).append(' ');
                }
                values.append(valueLabel(s.values[i]));
            }
            if (values.length() == 0) {
                continue;
            }
            sb.append(separator);
            if (lineSeries.contains(s)) {
                sb.append(s.name.isEmpty() ? "line: " : s.name + ", a line: ");
            } else if (!s.name.isEmpty()) {
                sb.append(s.name).append(": ");
            }
            sb.append(values);
            separator = "; ";
        }
        return sb.append('.').toString();
    }

    /** Tells if a bar from one value to another is wholly outside the axis, which gives it no length. */
    private static boolean outside(float from, float to, float vMin, float vMax) {
        return Math.min(Math.max(from, vMin), vMax) == Math.min(Math.max(to, vMin), vMax) && from != to;
    }

    /** Returns the number of category slots: the categories or the longest series. */
    private int numberOfCategories() {
        int n = categories.size();
        for (Series s : series) {
            n = Math.max(n, s.values.length);
        }
        // A line series is drawn over the categories of the bars, and adds
        // none of its own when there are bars: a longer target line added
        // empty slots
        if (n == 0) {
            for (Series s : lineSeries) {
                n = Math.max(n, s.values.length);
            }
        }
        return n;
    }

    /** Returns the series of the bars, then those of the lines. */
    private List<Series> allSeries() {
        List<Series> all = new ArrayList<Series>(series);
        all.addAll(lineSeries);
        return all;
    }

    /** Returns true if a series has a name to list in the legend. */
    private boolean hasSeriesNames() {
        for (Series s : allSeries()) {
            if (!s.name.isEmpty()) {
                return true;
            }
        }
        return false;
    }

    /** Returns the color of the bar at the index: the series' own, its color for the bar, or the palette's. */
    private int barColor(Series s, int seriesIndex, int index) {
        if (s.colors != null && index >= 0 && index < s.colors.length) {
            return s.colors[index];
        }
        return s.color == NO_COLOR ? PALETTE[seriesIndex % PALETTE.length] : s.color;
    }

    /** Formats the value written at the end of a bar. */
    private String valueLabel(float value) {
        return group(ChartScale.format(value, minFractionDigits, maxFractionDigits));
    }

    /** Formats a label of the value axis. */
    private String axisLabel(float value, int digits) {
        return group(ChartScale.format(value, digits, digits));
    }

    /** Groups the digits before the decimal point in thousands with commas, if grouping is used. */
    private String group(String label) {
        return groupingUsed ? ChartScale.groupDigits(label) : label;
    }

    /** Draws the text in the font at its size, in the color. */
    private static void drawText(Page page, Font font, String text, float x, float y, int color) {
        page.drawString(font, font.getSize(), text, x, y, Util.toRGB(color), null);
    }

    /**
     * Returns the color of a label on a bar of the color: white or ink,
     * whichever contrasts more with it, by the WCAG contrast ratio.
     */
    static int labelColorOn(int color) {
        // White contrasts more when (1 + 0.05) / (L + 0.05) is the larger of
        // the two ratios, (L + 0.05) / (ink + 0.05) the other
        double l = luminance(color) + 0.05;
        return l * l <= 1.05 * (luminance(INK) + 0.05) ? Color.white : INK;
    }

    /** Returns the relative luminance of the color, as WCAG defines it. */
    private static double luminance(int color) {
        return 0.2126 * linear((color >> 16) & 0xFF)
                + 0.7152 * linear((color >> 8) & 0xFF)
                + 0.0722 * linear(color & 0xFF);
    }

    /** Returns the sRGB component, 0 to 255, as a linear value from 0 to 1. */
    private static double linear(int component) {
        double c = component / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    }

    /** Draws one grid line, thin and solid. */
    private void gridLine(Page page, float xa, float ya, float xb, float yb) {
        page.setPenColor(gridLineColor);
        page.setPenWidth(0.5f);
        page.drawLine(xa, ya, xb, yb);
    }

    // Draws the legend from the baseline of its first row, its rows wrapped
    // at the right edge of the chart, when there is a page; and returns the
    // number of rows. A series with a color per bar has the color of its
    // first bar.
    private int drawLegend(Page page, float baseline) {
        float swatch = f2.getAscent();
        float gap = swatch / 2f;
        float x = x1;
        int rows = 1;
        List<Series> all = allSeries();
        for (int j = 0; j < all.size(); j++) {
            Series s = all.get(j);
            if (s.name.isEmpty()) {
                continue;
            }
            // A name too long for a row is shortened
            String name = ChartScale.shorten(f2, f2.getSize(), s.name, w - swatch - gap);
            if (x > x1 && x + swatch + gap + f2.stringWidth(name) > x1 + w) {
                x = x1;
                baseline += 1.5f * f2.getBodyHeight();
                rows++;
            }
            if (page != null && j >= series.size()) {
                // A line: its color across the swatch, a dot in the middle
                page.setPenColor(s.color);
                page.setPenWidth(1.5f);
                page.drawLine(x, baseline - swatch / 2f, x + swatch, baseline - swatch / 2f);
                page.setBrushColor(s.color);
                page.fillCircle(x + swatch / 2f, baseline - swatch / 2f, swatch / 4f);
            } else if (page != null) {
                page.setBrushColor((s.colors != null && s.colors.length > 0) ? s.colors[0] : barColor(s, j, -1));
                page.fillRect(x, baseline - swatch, swatch, swatch);
            }
            x += swatch + gap;
            if (page != null) {
                drawText(page, f2, name, x, baseline, INK);
            }
            x += f2.stringWidth(name) + f2.getBodyHeight();
        }
        return rows;
    }

}
