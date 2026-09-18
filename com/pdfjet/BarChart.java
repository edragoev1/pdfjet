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
 * series grouped inside each slot. See Example_39 (horizontal bars) and
 * Example_40 (vertical bars).
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

    private boolean horizontal = false;
    private boolean stacked = false;
    private float groupGap = 0.3f;
    private float barGap = 0f;

    private boolean drawGridLines = true;
    private boolean drawValueLabels = false;
    private boolean valueLabelsInside = false;
    private boolean drawLegend = true;
    private boolean groupingUsed = false;

    private int gridLineColor = Color.black;
    private float gridLineWidth = 0f;
    private String gridLineDashPattern = "[1 1] 0";
    private float axisLineWidth = 0.5f;
    private float chartBorderWidth = 0f;
    private float innerBorderWidth = 0f;

    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;

    // Value axis range; auto-computed from the data unless gridLines > 0
    private float min;
    private float max;
    private int gridLines = 0;

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
        this.title = title;
        return this;
    }

    /**
     * Sets the subtitle, written in gray under the title in the second font.
     *
     * @param subtitle the subtitle.
     * @return this BarChart object.
     */
    public BarChart setSubtitle(String subtitle) {
        this.subtitle = subtitle;
        return this;
    }

    /**
     * Sets the alternate description of the chart, which a screen reader reads
     * in a PDF/UA document, where the chart is a figure. The default is the
     * title, or "Bar chart" without one. Describe what the chart shows.
     *
     * @param altDescription the alternate description.
     * @return this BarChart object.
     */
    public BarChart setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the X axis title.
     *
     * @param title the title.
     * @return this BarChart object.
     */
    public BarChart setXAxisTitle(String title) {
        this.xAxisTitle = title;
        return this;
    }

    /**
     * Sets the Y axis title.
     *
     * @param title the title.
     * @return this BarChart object.
     */
    public BarChart setYAxisTitle(String title) {
        this.yAxisTitle = title;
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
        Collections.addAll(this.categories, categories);
        return this;
    }

    /**
     * Adds a series drawn in the next color of the default palette.
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
        series.add(new Series(name == null ? "" : name, values.clone(), color, null));
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
        series.add(new Series(name == null ? "" : name, values.clone(), NO_COLOR, colors.clone()));
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
     * slot, from 0.0 to below 1.0. The default is 0.3.
     *
     * @param gap the gap.
     * @return this BarChart object.
     */
    public BarChart setGroupGap(float gap) {
        this.groupGap = gap;
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
        this.barGap = gap;
        return this;
    }

    /**
     * Sets whether the grid lines of the value axis are drawn.
     *
     * @param drawGridLines true to draw them.
     * @return this BarChart object.
     */
    public BarChart setDrawGridLines(boolean drawGridLines) {
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
    public BarChart setDrawValueLabels(boolean drawValueLabels) {
        this.drawValueLabels = drawValueLabels;
        return this;
    }

    /**
     * Sets whether the value labels are written inside the bars, in white at
     * the end of each bar, instead of next to the bar ends. A bar too short
     * for its label gets it next to its end. The default is false.
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
     * in 6,650. The default is false.
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
    public BarChart setDrawLegend(boolean drawLegend) {
        this.drawLegend = drawLegend;
        return this;
    }

    /**
     * Sets the width of the grid lines. A width of 0 draws the thinnest line
     * a viewer shows; setDrawGridLines(false) hides them.
     *
     * @param width the line width.
     * @return this BarChart object.
     */
    public BarChart setGridLineWidth(float width) {
        this.gridLineWidth = width;
        return this;
    }

    /**
     * Sets the color of the grid lines. The default is black.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.lightgray.
     * @return this BarChart object.
     */
    public BarChart setGridLineColor(int color) {
        this.gridLineColor = color;
        return this;
    }

    /**
     * Sets the dash pattern of the grid lines, for example "[1 1] 0".
     *
     * @param pattern the dash pattern.
     * @return this BarChart object.
     */
    public BarChart setGridLineDashPattern(String pattern) {
        this.gridLineDashPattern = pattern;
        return this;
    }

    /**
     * Sets the width of the axis lines. The default is 0.5; 0 hides them.
     *
     * @param width the line width.
     * @return this BarChart object.
     */
    public BarChart setAxisLineWidth(float width) {
        this.axisLineWidth = width;
        return this;
    }

    /**
     * Sets the width of the outer chart border. A width of 0, the default,
     * hides it.
     *
     * @param width the border width.
     * @return this BarChart object.
     */
    public BarChart setChartBorderWidth(float width) {
        this.chartBorderWidth = width;
        return this;
    }

    /**
     * Sets the width of the plot area border. A width of 0, the default,
     * hides it.
     *
     * @param width the border width.
     * @return this BarChart object.
     */
    public BarChart setInnerBorderWidth(float width) {
        this.innerBorderWidth = width;
        return this;
    }

    /**
     * Sets the minimum number of decimal places in the value labels. The axis
     * labels have at least the decimal places of the axis step. The default is 0.
     *
     * @param minFractionDigits the minimum number of decimal places.
     * @return this BarChart object.
     */
    public BarChart setMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
        return this;
    }

    /**
     * Sets the maximum number of decimal places in the value labels. The
     * default is 2.
     *
     * @param maxFractionDigits the maximum number of decimal places.
     * @return this BarChart object.
     */
    public BarChart setMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
        return this;
    }

    /**
     * Sets the range and the number of grid lines of the value axis. Without
     * it the range is computed from the data and always includes 0.
     *
     * @param min the value at the start of the axis.
     * @param max the value at the end of the axis.
     * @param gridLines the number of grid lines, at least 1.
     * @return this BarChart object.
     */
    public BarChart setValueAxisMinMax(float min, float max, int gridLines) {
        this.min = min;
        this.max = max;
        this.gridLines = gridLines;
        return this;
    }

    /**
     * Draws this chart on the specified page.
     *
     * @param page the page to draw on.
     * @return the bottom right corner coordinates [x, y].
     */
    public float[] drawOn(Page page) throws Exception {
        int n = numberOfCategories();
        if (page == null || n == 0) {  // Measured, or nothing to draw
            return new float[] {x1 + w, y1 + h};
        }

        float vMin;
        float vMax;
        int lines;
        if (gridLines > 0) {
            vMin = min;
            vMax = max;
            lines = gridLines;
        } else {
            float lo = 0f;
            float hi = 0f;
            if (stacked) {
                // The sums of the values above and below 0 in each category
                for (int i = 0; i < n; i++) {
                    float up = 0f;
                    float down = 0f;
                    for (Series s : series) {
                        if (i < s.values.length) {
                            if (s.values[i] >= 0f) { up += s.values[i]; } else { down += s.values[i]; }
                        }
                    }
                    lo = Math.min(lo, down);
                    hi = Math.max(hi, up);
                }
            } else {
                for (Series s : series) {
                    for (float v : s.values) {
                        lo = Math.min(lo, v);
                        hi = Math.max(hi, v);
                    }
                }
            }
            if (hi == lo) { hi = lo + 1f; }
            Round round = Chart.roundMaxAndMinValues(hi, lo);
            vMin = round.minValue;
            vMax = round.maxValue;
            lines = round.numOfGridLines;
        }
        if (vMax == vMin) { vMax = vMin + 1f; }
        float step = (vMax - vMin) / lines;
        int axisDigits = Math.max(minFractionDigits, Chart.fractionDigitsOf(step, maxFractionDigits));
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
        if (drawValueLabels) {
            for (Series s : series) {
                for (float v : s.values) {
                    widestValueLabel = Math.max(widestValueLabel, f2.stringWidth(valueLabel(v)));
                }
            }
        }
        float widestCategory = 0f;
        for (String category : categories) {
            widestCategory = Math.max(widestCategory, f2.stringWidth(category));
        }

        // Margins and the plot area
        float titleBaseline = y1 + 1.5f * f1.getBodyHeight();
        float subtitleHeight = subtitle.isEmpty() ? 0f : bodyHeight;
        float topMargin = 2.5f * f1.getBodyHeight() + subtitleHeight + (legend ? 1.5f * bodyHeight : 0f);
        float leftMargin = 1.5f * bodyHeight + pad + (horizontal ? widestCategory : widestAxisLabel);
        float rightMargin = pad;
        float bottomMargin = 2.5f * bodyHeight;
        if (horizontal) {
            rightMargin += Math.max(widestAxisLabel / 2f, valueLabelsInside ? 0f : widestValueLabel + pad);
        } else if (drawValueLabels && !stacked && !valueLabelsInside) {
            topMargin += bodyHeight;
        }
        float x5 = x1 + leftMargin;
        float y5 = y1 + topMargin;
        float x6 = x2 - rightMargin;
        float y8 = y2 - bottomMargin;

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, null, altDescription());

        // Title, the subtitle and then the legend under it
        page.setBrushColor(Color.black);
        page.drawString(f1, f1.getSize(), title, x1 + (w - f1.stringWidth(title)) / 2f, titleBaseline);
        if (!subtitle.isEmpty()) {
            page.drawString(f2, f2.getSize(), subtitle, x1 + (w - f2.stringWidth(subtitle)) / 2f,
                    titleBaseline + subtitleHeight, Util.toRGB(Color.dimgray), null);
        }
        if (legend) {
            drawLegend(page, titleBaseline + subtitleHeight + 1.5f * bodyHeight);
        }

        if (chartBorderWidth > 0f) {
            page.setPenColor(Color.black);
            page.setPenWidth(chartBorderWidth);
            page.setDefaultStrokeDashPattern();
            page.drawRect(x1, y1, w, h);
        }

        // Grid lines and value axis labels
        for (int i = 0; i <= lines; i++) {
            float v = vMin + step * i;
            String label = axisLabel(v, axisDigits);
            if (horizontal) {
                float x = x5 + (v - vMin) * (x6 - x5) / (vMax - vMin);
                if (drawGridLines) {
                    gridLine(page, x, y5, x, y8);
                }
                page.drawString(f2, f2.getSize(), label, x - f2.stringWidth(label) / 2f, y8 + bodyHeight);
            } else {
                float y = y8 - (v - vMin) * (y8 - y5) / (vMax - vMin);
                if (drawGridLines) {
                    gridLine(page, x5, y, x6, y);
                }
                page.drawString(f2, f2.getSize(), label, x5 - pad - f2.stringWidth(label), y + ascent / 2f);
            }
        }

        // Bars, category labels and value labels
        int m = series.size();
        float slot = (horizontal ? (y8 - y5) : (x6 - x5)) / n;
        float groupWidth = slot * (1f - groupGap);
        float barWidth = (m == 0 || stacked) ? groupWidth : groupWidth / (m + (m - 1) * barGap);
        for (int i = 0; i < n; i++) {
            float slotStart = (horizontal ? y5 : x5) + i * slot;
            float groupStart = slotStart + slot * groupGap / 2f;
            String category = i < categories.size() ? categories.get(i) : "";
            page.setBrushColor(Color.black);
            if (horizontal) {
                page.drawString(f2, f2.getSize(), category, x5 - pad - f2.stringWidth(category),
                        slotStart + slot / 2f + ascent / 2f);
            } else {
                page.drawString(f2, f2.getSize(), category, slotStart + (slot - f2.stringWidth(category)) / 2f,
                        y8 + bodyHeight);
            }
            float up = 0f;      // the stacked values above 0 so far
            float down = 0f;    // the stacked values below 0 so far
            for (int j = 0; j < m; j++) {
                Series s = series.get(j);
                if (i >= s.values.length) {
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
                from = Math.min(Math.max(from, vMin), vMax);
                to = Math.min(Math.max(to, vMin), vMax);
                float barStart = stacked ? groupStart : groupStart + j * barWidth * (1f + barGap);
                page.setBrushColor(barColor(s, j, i));
                String label = drawValueLabels ? valueLabel(s.values[i]) : null;
                if (horizontal) {
                    float x0 = x5 + (from - vMin) * (x6 - x5) / (vMax - vMin);
                    float x = x5 + (to - vMin) * (x6 - x5) / (vMax - vMin);
                    page.fillRect(Math.min(x0, x), barStart, Math.abs(x - x0), barWidth);
                    if (label != null && stacked) {
                        if (Math.abs(x - x0) >= f2.stringWidth(label) + pad) {
                            page.setBrushColor(Color.black);
                            page.drawString(f2, f2.getSize(), label,
                                    Math.min(x0, x) + (Math.abs(x - x0) - f2.stringWidth(label)) / 2f,
                                    barStart + barWidth / 2f + ascent / 2f);
                        }
                    } else if (label != null && valueLabelsInside && Math.abs(x - x0) >= f2.stringWidth(label) + pad) {
                        page.drawString(f2, f2.getSize(), label,
                                to >= base ? x - pad / 2f - f2.stringWidth(label) : x + pad / 2f,
                                barStart + barWidth / 2f + ascent / 2f, Util.toRGB(Color.white), null);
                    } else if (label != null) {
                        page.setBrushColor(Color.black);
                        page.drawString(f2, f2.getSize(), label,
                                to >= base ? x + pad / 2f : x - pad / 2f - f2.stringWidth(label),
                                barStart + barWidth / 2f + ascent / 2f);
                    }
                } else {
                    float y0 = y8 - (from - vMin) * (y8 - y5) / (vMax - vMin);
                    float y = y8 - (to - vMin) * (y8 - y5) / (vMax - vMin);
                    page.fillRect(barStart, Math.min(y0, y), barWidth, Math.abs(y - y0));
                    if (label != null && stacked) {
                        if (Math.abs(y - y0) >= bodyHeight) {
                            page.setBrushColor(Color.black);
                            page.drawString(f2, f2.getSize(), label, barStart + (barWidth - f2.stringWidth(label)) / 2f,
                                    (y + y0) / 2f + ascent / 2f);
                        }
                    } else if (label != null && valueLabelsInside && Math.abs(y - y0) >= bodyHeight + pad) {
                        page.drawString(f2, f2.getSize(), label, barStart + (barWidth - f2.stringWidth(label)) / 2f,
                                to >= base ? y + ascent + pad / 2f : y - pad / 2f, Util.toRGB(Color.white), null);
                    } else if (label != null) {
                        page.setBrushColor(Color.black);
                        page.drawString(f2, f2.getSize(), label, barStart + (barWidth - f2.stringWidth(label)) / 2f,
                                to >= base ? y - pad / 2f : y + ascent + pad / 2f);
                    }
                }
            }
        }

        // Axis lines: along the labels and at the base of the bars
        page.setPenColor(Color.black);
        page.setDefaultStrokeDashPattern();
        if (axisLineWidth > 0f) {
            page.setPenWidth(axisLineWidth);
            if (horizontal) {
                float x0 = x5 + (base - vMin) * (x6 - x5) / (vMax - vMin);
                page.drawLine(x5, y8, x6, y8);
                page.drawLine(x0, y5, x0, y8);
            } else {
                float y0 = y8 - (base - vMin) * (y8 - y5) / (vMax - vMin);
                page.drawLine(x5, y5, x5, y8);
                page.drawLine(x5, y0, x6, y0);
            }
        }
        if (innerBorderWidth > 0f) {
            page.setPenWidth(innerBorderWidth);
            page.drawRect(x5, y5, x6 - x5, y8 - y5);
        }

        // Axis titles
        page.setBrushColor(Color.black);
        page.setTextRotation(-90);
        page.drawString(f2, f2.getSize(), yAxisTitle, x1 + bodyHeight, y8 - ((y8 - y5) - f2.stringWidth(yAxisTitle)) / 2f);
        page.setTextRotation(0);
        page.drawString(f2, f2.getSize(), xAxisTitle, x5 + ((x6 - x5) - f2.stringWidth(xAxisTitle)) / 2f, y2 - bodyHeight / 2f);

        page.setDefaultPenWidth();
        page.setDefaultStrokeDashPattern();
        page.setPenColor(Color.black);
        page.addEMC();

        return new float[] {x1 + w, y1 + h};
    }

    // Returns the alternate description, or the title when none is set.
    private String altDescription() {
        if (altDescription != null && !altDescription.isEmpty()) {
            return altDescription;
        }
        return title.isEmpty() ? "Bar chart" : title;
    }

    /** Returns the number of category slots: the categories or the longest series. */
    private int numberOfCategories() {
        int n = categories.size();
        for (Series s : series) {
            n = Math.max(n, s.values.length);
        }
        return n;
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

    /** Returns the color of the bar at the index: the series' own, its color for the bar, or the palette's. */
    private int barColor(Series s, int seriesIndex, int index) {
        if (s.colors != null && index >= 0 && index < s.colors.length) {
            return s.colors[index];
        }
        return s.color == NO_COLOR ? Chart.DEFAULT_PALETTE[seriesIndex % Chart.DEFAULT_PALETTE.length] : s.color;
    }

    /** Formats the value written at the end of a bar. */
    private String valueLabel(float value) {
        return group(Chart.format(value, minFractionDigits, maxFractionDigits));
    }

    /** Formats a label of the value axis. */
    private String axisLabel(float value, int digits) {
        return group(Chart.format(value, digits, maxFractionDigits));
    }

    /** Groups the digits before the decimal point in thousands with commas, if grouping is used. */
    private String group(String label) {
        if (!groupingUsed) {
            return label;
        }
        int start = label.startsWith("-") ? 1 : 0;
        int end = label.indexOf('.');
        if (end < 0) {
            end = label.length();
        }
        StringBuilder sb = new StringBuilder(label);
        for (int i = end - 3; i > start; i -= 3) {
            sb.insert(i, ',');
        }
        return sb.toString();
    }

    /** Draws one dotted grid line. */
    private void gridLine(Page page, float xa, float ya, float xb, float yb) {
        page.setPenColor(gridLineColor);
        page.setPenWidth(gridLineWidth);
        page.setStrokeDashPattern(gridLineDashPattern);
        page.drawLine(xa, ya, xb, yb);
    }

    /** Draws the legend centered on the chart, with a swatch before each series name. */
    private void drawLegend(Page page, float baseline) {
        float swatch = f2.getAscent();
        float gap = swatch / 2f;
        float width = 0f;
        int entries = 0;
        for (Series s : series) {
            if (!s.name.isEmpty()) {
                width += swatch + gap + f2.stringWidth(s.name);
                entries++;
            }
        }
        width += (entries - 1) * f2.getBodyHeight();
        float x = x1 + (w - width) / 2f;
        for (int j = 0; j < series.size(); j++) {
            Series s = series.get(j);
            if (s.name.isEmpty()) {
                continue;
            }
            page.setBrushColor(barColor(s, j, -1));
            page.fillRect(x, baseline - swatch, swatch, swatch);
            x += swatch + gap;
            page.setBrushColor(Color.black);
            page.drawString(f2, f2.getSize(), s.name, x, baseline);
            x += f2.stringWidth(s.name) + f2.getBodyHeight();
        }
    }
}   // End of BarChart.java
