/*
 * SimpleLineChart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A chart of a few lines over time, or over any categories in their order:
 * the categories left to right, each line labeled at its right end with its
 * name and its last value, so that it needs no legend; one line highlighted in
 * navy, its label bold, the others gray. A light horizontal grid alone, no
 * border, on a light panel, with a title and a subtitle at the top left, in
 * the look of SimpleBarChart and SimpleScatterChart. A value that is NaN
 * leaves a gap in its line.
 * <p>
 * In a PDF/UA document the chart is one figure, described from its data: the
 * lines, their first and last values and their highest, unless an alternate
 * description is set. It draws with the page alone, and needs no other chart.
 * Please see Example_25.
 */
public class SimpleLineChart implements Drawable {
    // The color of the lines that are not highlighted, unless set
    static final int LINE = 0x8C96A3;

    // A line of the chart: its name and a value for each category, NaN where
    // it has none.
    private static final class Line {
        final String name;
        final float[] values;

        Line(String name, float[] values) {
            this.name = name;
            this.values = values;
        }

        // Whether the line has a value at the category.
        boolean has(int i) {
            return i < values.length && ChartScale.isValue(values[i]);
        }
    }

    // A label at the end of a line: its text, the index of its line and the
    // baseline it is drawn at.
    private static final class EndLabel {
        final String text;
        final int index;
        float y;

        EndLabel(String text, int index, float y) {
            this.text = text;
            this.index = index;
            this.y = y;
        }
    }

    private final Font f1;      // The title and the highlighted label
    private final Font f2;      // The rest
    private float x;
    private float y;
    private float w = 400f;
    private float h = 300f;
    private String title = "";
    private String subtitle = "";
    private String xAxisTitle = "";
    private String yAxisTitle = "";
    private String altDescription = "";
    private String[] categories = new String[0];
    private final List<Line> lines = new ArrayList<Line>();
    private int highlight = -1;     // The index of the highlighted line, or -1
    private boolean markers = false;
    private float yMin;
    private float yMax;
    private boolean yRangeSet = false;
    private int panelColor = SimpleBarChart.PANEL;
    private int lineColor = LINE;
    private int highlightColor = SimpleBarChart.HIGHLIGHT;
    private final int maxFractionDigits = 2;

    /**
     * Creates a simple line chart, 400 by 300 points.
     *
     * @param f1 the font of the title and of the highlighted line's label, a
     *     bold one.
     * @param f2 the font of the subtitle, the axis titles, the numbers, the
     *     categories and the other labels.
     */
    public SimpleLineChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /**
     * Sets the title, drawn at the top left of the chart.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleLineChart setTitle(String title) {
        this.title = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the subtitle, drawn small and gray under the title.
     *
     * @param subtitle the subtitle.
     * @return this chart.
     */
    public SimpleLineChart setSubtitle(String subtitle) {
        this.subtitle = (subtitle == null) ? "" : subtitle;
        return this;
    }

    /**
     * Sets the title of the categories, drawn under them.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleLineChart setXAxisTitle(String title) {
        this.xAxisTitle = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the title of the vertical axis, drawn up its left side.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleLineChart setYAxisTitle(String title) {
        this.yAxisTitle = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the alternate description of the chart in a PDF/UA document.
     * Without one the chart is described from its data.
     *
     * @param altDescription the alternate description.
     * @return this chart.
     */
    public SimpleLineChart setAltDescription(String altDescription) {
        this.altDescription = (altDescription == null) ? "" : altDescription;
        return this;
    }

    /**
     * Sets the categories, the years or the months, in their order left to
     * right.
     *
     * @param categories the categories.
     * @return this chart.
     */
    public SimpleLineChart setCategories(String... categories) {
        this.categories = (categories == null) ? new String[0] : categories;
        return this;
    }

    /**
     * Adds a line: its name, and a value for each category, NaN where it has
     * none.
     *
     * @param name the name of the line.
     * @param values its values.
     * @return this chart.
     */
    public SimpleLineChart addLine(String name, float[] values) {
        lines.add(new Line(name == null ? "" : name, values == null ? new float[0] : values));
        return this;
    }

    /**
     * Highlights the line of the index, the first line 0: navy and a little
     * wider, its label bold.
     *
     * @param index the index of the line.
     * @return this chart.
     */
    public SimpleLineChart setHighlight(int index) {
        this.highlight = index;
        return this;
    }

    /**
     * Draws a dot at each value of the lines.
     *
     * @param markers true to draw them.
     * @return this chart.
     */
    public SimpleLineChart setMarkers(boolean markers) {
        this.markers = markers;
        return this;
    }

    /**
     * Sets the range of the vertical axis. Without it the range is that of
     * the values, rounded to steps.
     *
     * @param min the first value of the axis.
     * @param max the last value of the axis.
     * @return this chart.
     */
    public SimpleLineChart setYAxisRange(float min, float max) {
        this.yMin = min;
        this.yMax = max;
        this.yRangeSet = max > min;
        return this;
    }

    /**
     * Sets the color of the panel behind the chart, or -1 for none. It is
     * light gray, 0xF1F3F6, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleLineChart setPanelColor(int color) {
        this.panelColor = color;
        return this;
    }

    /**
     * Sets the color of the lines that are not highlighted. It is gray,
     * 0x8C96A3, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleLineChart setLineColor(int color) {
        this.lineColor = color;
        return this;
    }

    /**
     * Sets the color of the highlighted line and of its label. It is navy,
     * 0x1F3A5F, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleLineChart setHighlightColor(int color) {
        this.highlightColor = color;
        return this;
    }

    /**
     * Sets the top left corner of the chart.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this chart.
     */
    public SimpleLineChart setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the width and the height of the chart.
     *
     * @param w the width.
     * @param h the height.
     * @return this chart.
     */
    public SimpleLineChart setSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    // The space around the chart's content, a size of its numbers.
    private float pad() {
        return f2.getSize();
    }

    // The size of the subtitle.
    private float subtitleSize() {
        return 0.8f * f2.getSize();
    }

    // The height above the plot: the padding, the title and the subtitle.
    private float top() {
        float top = pad();
        if (!title.isEmpty()) {
            top += f1.getAscent(f1.getSize()) + f1.getDescent(f1.getSize());
        }
        if (!subtitle.isEmpty()) {
            top += 1.4f * subtitleSize();
        }
        if (!title.isEmpty() || !subtitle.isEmpty()) {
            top += f2.getSize();
        }
        return top;
    }

    // The number of categories the chart draws: the categories set, or the
    // longest line when there are none.
    private int count() {
        int n = categories.length;
        if (n == 0) {
            for (Line line : lines) {
                n = Math.max(n, line.values.length);
            }
        }
        return n;
    }

    // A number of the axis, with the decimals its step needs, its digits
    // grouped.
    private String label(float value, float step) {
        return ChartScale.groupDigits(
                ChartScale.format(value, ChartScale.fractionDigitsOf(step, maxFractionDigits), maxFractionDigits));
    }

    // A value of a line, as its label writes it.
    private String value(float v) {
        return ChartScale.groupDigits(ChartScale.format(v, 0, maxFractionDigits));
    }

    // The name of the category of the index.
    private String category(int i) {
        return i < categories.length ? categories[i] : "";
    }

    // The font of the label of the line of the index.
    private Font fontOf(int index) {
        return index == highlight ? f1 : f2;
    }

    // The category of the last value of the line, or -1 when it has none.
    private int lastOf(Line line) {
        for (int i = count() - 1; i >= 0; i--) {
            if (line.has(i)) {
                return i;
            }
        }
        return -1;
    }

    // The alternate description: the one set, or the title, the categories
    // and, for each line, its first and last values and its highest.
    String altDescription() {
        if (!altDescription.isEmpty()) {
            return altDescription;
        }
        StringBuilder sb = new StringBuilder(title);
        if (!subtitle.isEmpty()) {
            if (sb.length() > 0) {
                sb.append(", ");
            }
            sb.append(subtitle);
        }
        if (sb.length() == 0) {
            sb.append("Line chart");
        }
        int n = count();
        if (n > 0 && !category(0).isEmpty()) {
            sb.append(", ").append(category(0)).append(" to ").append(category(n - 1));
        }
        sb.append(':');
        for (int i = 0; i < lines.size(); i++) {
            Line line = lines.get(i);
            int first = -1;
            int last = -1;
            int high = -1;
            for (int j = 0; j < n; j++) {
                if (!line.has(j)) {
                    continue;
                }
                if (first < 0) {
                    first = j;
                }
                last = j;
                if (high < 0 || line.values[j] > line.values[high]) {
                    high = j;
                }
            }
            if (i > 0) {
                sb.append(';');
            }
            sb.append(' ').append(line.name);
            if (first < 0) {
                sb.append(" has no values");
                continue;
            }
            sb.append(" from ").append(value(line.values[first])).append(" to ").append(value(line.values[last]));
            if (high != first && high != last) {
                sb.append(", highest ").append(value(line.values[high]));
                if (!category(high).isEmpty()) {
                    sb.append(" in ").append(category(high));
                }
            }
        }
        return sb.append('.').toString();
    }

    /**
     * Draws the chart on the page. A chart too small for its plot, its
     * numbers and its labels draws nothing.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of the chart.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        final int n = count();
        List<Float> all = new ArrayList<Float>();
        for (Line line : lines) {
            for (int j = 0; j < n; j++) {
                if (line.has(j)) {
                    all.add(line.values[j]);
                }
            }
        }
        if (page == null || all.isEmpty()) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float[] values = new float[all.size()];
        for (int i = 0; i < values.length; i++) {
            values[i] = all.get(i);
        }
        float size = f2.getSize();
        float pad = pad();
        float ascent = f2.getAscent(size);

        ChartScale.Axis axis = ChartScale.axisOf(values, yMin, yMax, yRangeSet);
        float step = (axis.max - axis.min) / axis.lines;

        // The plot: right of the y title and the widest number, left of the
        // widest label at the end of a line, above the categories and the x
        // title
        float widest = 0f;
        for (int i = 0; i <= axis.lines; i++) {
            widest = Math.max(widest, f2.stringWidth(size, label(axis.min + step * i, step)));
        }
        float endWidth = 0f;
        for (int i = 0; i < lines.size(); i++) {
            Line line = lines.get(i);
            int last = lastOf(line);
            if (last >= 0) {
                endWidth = Math.max(endWidth, fontOf(i).stringWidth(size, line.name + " " + value(line.values[last])));
            }
        }
        float left = x + pad + widest + size / 2f;
        if (!yAxisTitle.isEmpty()) {
            left += 1.8f * size;    // The title, as far from the numbers as the x title is
        }
        float right = x + w - pad - endWidth - size / 2f;
        float top = y + top() + ascent / 2f;     // Room for the top number
        float bottom = y + h - pad - 1.4f * size;
        if (!xAxisTitle.isEmpty()) {
            bottom -= 1.4f * size;
        }
        if (right <= left || bottom <= top || !ChartScale.drawable(axis)) {
            return new float[] {x + w, y + h};
        }

        page.addBDC(StructElem.FIGURE, null, altDescription());
        page.saveGraphicsState();

        if (panelColor >= 0) {
            page.setBrushColor(panelColor);
            page.fillRect(x, y, w, h);
        }
        float baseline = y + pad;
        if (!title.isEmpty()) {
            baseline += f1.getAscent(f1.getSize());
            page.drawString(f1, f1.getSize(), title, x + pad, baseline, Util.toRGB(SimpleBarChart.INK), null);
            baseline += f1.getDescent(f1.getSize());
        }
        if (!subtitle.isEmpty()) {
            baseline += 1.4f * subtitleSize();
            page.drawString(f2, subtitleSize(), subtitle, x + pad, baseline, Util.toRGB(SimpleBarChart.MUTED), null);
        }

        // The horizontal grid and its numbers, and the categories under the
        // plot, each where it leaves the one before it room
        page.setPenColor(SimpleScatterChart.GRID);
        page.setPenWidth(0.5f);
        int every = ChartScale.axisLabelStep((bottom - top) / axis.lines, f2.getBodyHeight(), axis.lines);
        for (int i = 0; i <= axis.lines; i++) {
            float v = axis.min + step * i;
            float gy = yAt(v, axis, top, bottom);
            page.drawLine(left, gy, right, gy);
            if (i % every == 0) {
                String label = label(v, step);
                page.drawString(f2, size, label, left - size / 2f - f2.stringWidth(size, label),
                        gy + ascent / 2f, Util.toRGB(SimpleBarChart.MUTED), null);
            }
        }
        float widestCategory = 0f;
        for (int i = 0; i < n; i++) {
            widestCategory = Math.max(widestCategory, f2.stringWidth(size, category(i)));
        }
        every = 1;
        if (n > 1) {
            every = ChartScale.axisLabelStep((right - left) / (n - 1), widestCategory + pad, n - 1);
        }
        for (int i = 0; i < n; i += every) {
            String label = category(i);
            page.drawString(f2, size, label, xAt(i, n, left, right) - f2.stringWidth(size, label) / 2f,
                    bottom + 1.4f * size, Util.toRGB(SimpleBarChart.MUTED), null);
        }
        if (!xAxisTitle.isEmpty()) {
            page.drawString(f2, size, xAxisTitle, left + (right - left - f2.stringWidth(size, xAxisTitle)) / 2f,
                    bottom + 2.8f * size, Util.toRGB(SimpleBarChart.INK), null);
        }
        if (!yAxisTitle.isEmpty()) {
            page.setTextRotation(-90);
            page.drawString(f2, size, yAxisTitle, x + pad + ascent,
                    bottom - (bottom - top - f2.stringWidth(size, yAxisTitle)) / 2f,
                    Util.toRGB(SimpleBarChart.INK), null);
            page.setTextRotation(0);
        }

        // The lines, the highlighted one last, over the others, broken where a
        // value is missing
        List<Integer> order = new ArrayList<Integer>();
        for (int i = 0; i < lines.size(); i++) {
            if (i != highlight) {
                order.add(i);
            }
        }
        if (highlight >= 0 && highlight < lines.size()) {
            order.add(highlight);
        }
        // A value outside a range that is set is drawn past the plot, and the
        // lines are clipped at its edges, and as far past them as a dot is wide
        boolean outside = false;
        for (float v : values) {
            outside |= !ChartScale.onAxis(v, axis);
        }
        if (outside) {
            float edge = 0.25f * size + 1f;
            page.saveGraphicsState();
            page.clipRect(left - edge, top - edge, right - left + 2f * edge, bottom - top + 2f * edge);
        }
        for (int i : order) {
            Line line = lines.get(i);
            int color = (i == highlight) ? highlightColor : lineColor;
            page.setPenColor(color);
            page.setPenWidth((i == highlight) ? 2f : 1.5f);
            page.setLineJoinStyle(JoinStyle.ROUND);     // No spike at a sharp peak
            boolean drawing = false;
            for (int j = 0; j < n; j++) {
                if (!line.has(j)) {
                    if (drawing) {
                        page.strokePath();
                    }
                    drawing = false;
                    continue;
                }
                if (drawing) {
                    page.lineTo(xAt(j, n, left, right), yAt(line.values[j], axis, top, bottom));
                } else {
                    page.moveTo(xAt(j, n, left, right), yAt(line.values[j], axis, top, bottom));
                    drawing = true;
                }
            }
            if (drawing) {
                page.strokePath();
            }
            if (markers) {
                page.setBrushColor(color);
                for (int j = 0; j < n; j++) {
                    if (line.has(j)) {
                        page.fillCircle(xAt(j, n, left, right), yAt(line.values[j], axis, top, bottom), 0.25f * size);
                    }
                }
            }
        }
        if (outside) {
            page.restoreGraphicsState();
        }

        // The labels at the ends of the lines, moved apart where they would
        // meet
        List<EndLabel> labels = new ArrayList<EndLabel>();
        for (int i = 0; i < lines.size(); i++) {
            Line line = lines.get(i);
            int last = lastOf(line);
            if (last >= 0) {
                // At the end of its line, or at the edge of the plot it is past
                float end = Math.max(top, Math.min(bottom, yAt(line.values[last], axis, top, bottom)));
                labels.add(new EndLabel(line.name + " " + value(line.values[last]), i, end + ascent / 2f));
            }
        }
        Collections.sort(labels, new Comparator<EndLabel>() {
            @Override public int compare(EndLabel a, EndLabel b) {
                return Float.compare(a.y, b.y);
            }
        });
        for (int k = 1; k < labels.size(); k++) {
            labels.get(k).y = Math.max(labels.get(k).y, labels.get(k - 1).y + f2.getBodyHeight());
        }
        // and up from the bottom of the plot, when the last is pushed past it
        if (!labels.isEmpty()) {
            int last = labels.size() - 1;
            labels.get(last).y = Math.min(labels.get(last).y, bottom + ascent / 2f);
            for (int k = last - 1; k >= 0; k--) {
                labels.get(k).y = Math.min(labels.get(k).y, labels.get(k + 1).y - f2.getBodyHeight());
            }
        }
        for (EndLabel label : labels) {
            int color = (label.index == highlight) ? highlightColor : SimpleBarChart.MUTED;
            page.drawString(fontOf(label.index), size, label.text, right + size / 2f, label.y, Util.toRGB(color), null);
        }

        page.restoreGraphicsState();
        page.setFigureBoundingBox(x, y, w, h);
        page.addEMC();
        return new float[] {x + w, y + h};
    }

    // The x on the page of the category of the index.
    private static float xAt(int i, int n, float left, float right) {
        if (n == 1) {
            return (left + right) / 2f;
        }
        return left + i * (right - left) / (n - 1);
    }

    // The y on the page of a value, held near the plot.
    private static float yAt(float v, ChartScale.Axis axis, float top, float bottom) {
        v = ChartScale.held(v, axis);
        return bottom - (v - axis.min) * (bottom - top) / (axis.max - axis.min);
    }
}   // End of SimpleLineChart.java
