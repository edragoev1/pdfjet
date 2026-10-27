/*
 * SimpleScatterChart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A chart of points on two axes that shows how two values go together: many
 * points as small gray dots, a little transparent so that a cluster shows
 * darker, a few of them highlighted, navy and labeled with their names beside
 * them, and the trend line of the points if asked, labeled at its end. A light
 * horizontal grid alone, no border or legend, on a light panel, with a title
 * and a subtitle at the top left, in the look of SimpleBarChart.
 * <p>
 * In a PDF/UA document the chart is one figure, described from its data: the
 * number of points, the highlighted ones and their values, and which way the
 * trend goes, unless an alternate description is set. It draws with the page
 * alone, and needs no other chart. In PDF/A-1, which has no transparency, the
 * dots are drawn opaque. Please see Example_26.
 */
public class SimpleScatterChart implements Drawable {
    // The colors of a simple scatter chart unless set, those of
    // SimpleBarChart: a light panel, gray dots a little transparent, the
    // highlighted points navy, the grid a light gray, the trend line and the
    // numbers dark gray.
    static final int DOT = 0x8C96A3;
    static final int GRID = 0xD5D9DE;
    static final int TREND = 0x5F6873;
    static final float ALPHA = 0.55f;

    // A point of the chart, highlighted when it has a name.
    static final class Point {
        final float x;
        final float y;
        final String name;
        final String uri;       // The link of a highlighted point, or null

        Point(float x, float y, String name) {
            this(x, y, name, null);
        }

        Point(float x, float y, String name, String uri) {
            this.x = x;
            this.y = y;
            this.name = (name == null) ? "" : name;
            this.uri = uri;
        }
    }

    private final Font f1;      // The title and the names
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
    private final List<Point> points = new ArrayList<Point>();
    private boolean trendLine = false;
    private float xMin;
    private float xMax;
    private float yMin;
    private float yMax;
    private boolean xRangeSet = false;
    private boolean yRangeSet = false;
    private int panelColor = SimpleBarChart.PANEL;
    private int dotColor = DOT;
    private int highlightColor = SimpleBarChart.HIGHLIGHT;
    private final int maxFractionDigits = 2;

    /**
     * Creates a simple scatter chart, 400 by 300 points.
     *
     * @param f1 the font of the title and of the names of the highlighted
     *     points, a bold one.
     * @param f2 the font of the subtitle, the axis titles and the numbers.
     */
    public SimpleScatterChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /**
     * Sets the title, drawn at the top left of the chart.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleScatterChart setTitle(String title) {
        this.title = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the subtitle, drawn small and gray under the title.
     *
     * @param subtitle the subtitle.
     * @return this chart.
     */
    public SimpleScatterChart setSubtitle(String subtitle) {
        this.subtitle = (subtitle == null) ? "" : subtitle;
        return this;
    }

    /**
     * Sets the title of the horizontal axis, drawn under it.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleScatterChart setXAxisTitle(String title) {
        this.xAxisTitle = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the title of the vertical axis, drawn up its left side.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleScatterChart setYAxisTitle(String title) {
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
    public SimpleScatterChart setAltDescription(String altDescription) {
        this.altDescription = (altDescription == null) ? "" : altDescription;
        return this;
    }

    /**
     * Adds a point, drawn as a small gray dot. A point whose x or y is not a
     * number is left out.
     *
     * @param x the x of the point.
     * @param y the y of the point.
     * @return this chart.
     */
    public SimpleScatterChart add(float x, float y) {
        points.add(new Point(x, y, ""));
        return this;
    }

    /**
     * Adds a point that is highlighted: a navy dot with its name beside it.
     *
     * @param name the name of the point.
     * @param x the x of the point.
     * @param y the y of the point.
     * @return this chart.
     */
    public SimpleScatterChart add(String name, float x, float y) {
        points.add(new Point(x, y, name));
        return this;
    }

    /**
     * Adds a point that is highlighted and is a link: a navy dot with its name
     * beside it, both of them the link to the URI. In a PDF/UA document each
     * such point is a figure of its own, described by its name, in the Link
     * that holds its annotation, as PDF/UA asks.
     *
     * @param name the name of the point.
     * @param x the x of the point.
     * @param y the y of the point.
     * @param uri the URI the point links to.
     * @return this chart.
     */
    public SimpleScatterChart add(String name, float x, float y, String uri) {
        points.add(new Point(x, y, name, uri));
        return this;
    }

    /**
     * Draws the trend line of the points, their least squares line, thin and
     * dark gray, labeled "trend" at its right end.
     *
     * @param trendLine true to draw it.
     * @return this chart.
     */
    public SimpleScatterChart setTrendLine(boolean trendLine) {
        this.trendLine = trendLine;
        return this;
    }

    /**
     * Sets the range of the horizontal axis. Without it the range is that of
     * the points, rounded to steps.
     *
     * @param min the first value of the axis.
     * @param max the last value of the axis.
     * @return this chart.
     */
    public SimpleScatterChart setXAxisRange(float min, float max) {
        this.xMin = min;
        this.xMax = max;
        this.xRangeSet = max > min;
        return this;
    }

    /**
     * Sets the range of the vertical axis. Without it the range is that of the
     * points, rounded to steps.
     *
     * @param min the first value of the axis.
     * @param max the last value of the axis.
     * @return this chart.
     */
    public SimpleScatterChart setYAxisRange(float min, float max) {
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
    public SimpleScatterChart setPanelColor(int color) {
        this.panelColor = color;
        return this;
    }

    /**
     * Sets the color of the points that are not highlighted. It is gray,
     * 0x8C96A3, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleScatterChart setDotColor(int color) {
        this.dotColor = color;
        return this;
    }

    /**
     * Sets the color of the highlighted points and of their names. It is navy,
     * 0x1F3A5F, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleScatterChart setHighlightColor(int color) {
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
    public SimpleScatterChart setLocation(float x, float y) {
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
    public SimpleScatterChart setSize(float w, float h) {
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

    // A number of an axis, with the decimals its step needs.
    private String label(float value, float step) {
        return ChartScale.format(value, ChartScale.fractionDigitsOf(step, maxFractionDigits), maxFractionDigits);
    }

    // The points whose x and y are numbers.
    private List<Point> valid() {
        List<Point> valid = new ArrayList<Point>();
        for (Point p : points) {
            if (ChartScale.isValue(p.x) && ChartScale.isValue(p.y)) {
                valid.add(p);
            }
        }
        return valid;
    }

    // The slope and the intercept of the least squares line of the points, or
    // null when there is none: fewer than two points, or all at one x.
    static double[] trend(List<Point> points) {
        double n = points.size();
        if (n < 2) {
            return null;
        }
        double sx = 0;
        double sy = 0;
        double sxx = 0;
        double sxy = 0;
        for (Point p : points) {
            double x = p.x;
            double y = p.y;
            sx += x;
            sy += y;
            sxx += x * x;
            sxy += x * y;
        }
        double d = n * sxx - sx * sx;
        if (d == 0) {
            return null;
        }
        double slope = (n * sxy - sx * sy) / d;
        return new double[] {slope, (sy - slope * sx) / n};
    }

    // The alternate description: the one set, or the title, the number of
    // points, the highlighted ones and the trend.
    String altDescription() {
        if (!altDescription.isEmpty()) {
            return altDescription;
        }
        List<Point> points = valid();
        StringBuilder sb = new StringBuilder(title);
        if (!subtitle.isEmpty()) {
            if (sb.length() > 0) {
                sb.append(", ");
            }
            sb.append(subtitle);
        }
        if (sb.length() == 0) {
            sb.append("Scatter chart");
        }
        sb.append(": ").append(points.size()).append(" points");
        if (!xAxisTitle.isEmpty() && !yAxisTitle.isEmpty()) {
            sb.append(", ").append(yAxisTitle).append(" against ").append(xAxisTitle);
        }
        int named = 0;
        for (Point p : points) {
            if (p.name.isEmpty()) {
                continue;
            }
            sb.append(named == 0 ? "; " : ", ");
            sb.append(p.name).append(' ').append(ChartScale.format(p.x, 0, maxFractionDigits))
                    .append(" and ").append(ChartScale.format(p.y, 0, maxFractionDigits));
            named++;
        }
        if (trendLine) {
            double[] line = trend(points);
            if (line != null) {
                sb.append(line[0] > 0 ? "; the trend rises" : line[0] < 0 ? "; the trend falls" : "; the trend is flat");
            }
        }
        return sb.append('.').toString();
    }

    /**
     * Draws the chart on the page. A chart too small for its plot and its
     * numbers draws nothing.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of the chart.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        List<Point> points = valid();
        if (page == null || points.isEmpty()) {     // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.getSize();
        float pad = pad();
        float ascent = f2.getAscent(size);

        float[] xs = new float[points.size()];
        float[] ys = new float[points.size()];
        for (int i = 0; i < points.size(); i++) {
            xs[i] = points.get(i).x;
            ys[i] = points.get(i).y;
        }
        final ChartScale.Axis xAxis = ChartScale.axisOf(xs, xMin, xMax, xRangeSet);
        final ChartScale.Axis yAxis = ChartScale.axisOf(ys, yMin, yMax, yRangeSet);
        float xStep = (xAxis.max - xAxis.min) / xAxis.lines;
        float yStep = (yAxis.max - yAxis.min) / yAxis.lines;

        // The plot: right of the y title and the widest number, above the
        // numbers and the x title
        float widest = 0f;
        for (int i = 0; i <= yAxis.lines; i++) {
            widest = Math.max(widest, f2.stringWidth(size, label(yAxis.min + yStep * i, yStep)));
        }
        float left = x + pad + widest + size / 2f;
        if (!yAxisTitle.isEmpty()) {
            left += 1.8f * size;     // The title, as far from the numbers as the x title is
        }
        final float right = x + w - pad;
        final float top = y + top() + ascent / 2f;     // Room for the top number
        float bottom = y + h - pad - 1.4f * size;
        if (!xAxisTitle.isEmpty()) {
            bottom -= 1.4f * size;
        }
        if (right <= left || bottom <= top || !ChartScale.drawable(xAxis) || !ChartScale.drawable(yAxis)) {
            return new float[] {x + w, y + h};
        }
        final float plotLeft = left;
        final float plotBottom = bottom;

        // In a tagged document a point that is a link is drawn after the
        // chart, as a figure of its own in its Link, which holds its
        // annotation, as PDF/UA asks: a link inside the chart's figure has
        // nothing to hold
        boolean linksApart = page.tagsContent();

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

        // The horizontal grid and the numbers of both axes, each where it
        // leaves the one before it room
        page.setPenColor(GRID);
        page.setPenWidth(0.5f);
        int every = ChartScale.axisLabelStep((plotBottom - top) / yAxis.lines, f2.getBodyHeight(), yAxis.lines);
        for (int i = 0; i <= yAxis.lines; i++) {
            float v = yAxis.min + yStep * i;
            float gy = yAt(v, yAxis, top, plotBottom);
            page.drawLine(plotLeft, gy, right, gy);
            if (i % every == 0) {
                String label = label(v, yStep);
                page.drawString(f2, size, label, plotLeft - size / 2f - f2.stringWidth(size, label),
                        gy + ascent / 2f, Util.toRGB(SimpleBarChart.MUTED), null);
            }
        }
        float xWidest = 0f;
        for (int i = 0; i <= xAxis.lines; i++) {
            xWidest = Math.max(xWidest, f2.stringWidth(size, label(xAxis.min + xStep * i, xStep)));
        }
        every = ChartScale.axisLabelStep((right - plotLeft) / xAxis.lines, xWidest + pad, xAxis.lines);
        for (int i = 0; i <= xAxis.lines; i += every) {
            float v = xAxis.min + xStep * i;
            String label = label(v, xStep);
            page.drawString(f2, size, label, xAt(v, xAxis, plotLeft, right) - f2.stringWidth(size, label) / 2f,
                    plotBottom + 1.4f * size, Util.toRGB(SimpleBarChart.MUTED), null);
        }
        if (!xAxisTitle.isEmpty()) {
            page.drawString(f2, size, xAxisTitle, plotLeft + (right - plotLeft - f2.stringWidth(size, xAxisTitle)) / 2f,
                    plotBottom + 2.8f * size, Util.toRGB(SimpleBarChart.INK), null);
        }
        if (!yAxisTitle.isEmpty()) {
            page.setTextRotation(-90);
            page.drawString(f2, size, yAxisTitle, x + pad + ascent,
                    plotBottom - (plotBottom - top - f2.stringWidth(size, yAxisTitle)) / 2f,
                    Util.toRGB(SimpleBarChart.INK), null);
            page.setTextRotation(0);
        }

        // The trend line, from the left of the plot to its right, within its
        // height
        double[] line = trend(points);
        if (trendLine && line != null) {
            double slope = line[0];
            double intercept = line[1];
            double[] p1 = clip(xAxis.min, slope * xAxis.min + intercept, slope, intercept, yAxis);
            double[] p2 = clip(xAxis.max, slope * xAxis.max + intercept, slope, intercept, yAxis);
            page.setPenColor(TREND);
            page.setPenWidth(0.75f);
            float x1 = xAt((float) p1[0], xAxis, plotLeft, right);
            float y1 = yAt((float) p1[1], yAxis, top, plotBottom);
            float x2 = xAt((float) p2[0], xAxis, plotLeft, right);
            float y2 = yAt((float) p2[1], yAxis, top, plotBottom);
            page.drawLine(x1, y1, x2, y2);
            // The label over the end of the line, lifted by as much as a
            // falling line rises under it
            String label = "trend";
            float width = f2.stringWidth(subtitleSize(), label);
            float lift = size / 3f;
            if (x2 > x1 && y2 > y1) {
                lift += width * ((y2 - y1) / (x2 - x1));
            }
            page.drawString(f2, subtitleSize(), label, x2 - width, y2 - lift, Util.toRGB(TREND), null);
        }

        // The dots, then the highlighted points and their names over them
        page.saveGraphicsState();
        if (!page.pdf.isPDFA1()) {
            GraphicsState gs = new GraphicsState();
            gs.setAlphaNonStroking(ALPHA);
            page.setGraphicsState(gs);
        }
        page.setBrushColor(dotColor);
        float r = 0.3f * size;
        for (Point p : points) {
            if (p.name.isEmpty() && onPlot(p, xAxis, yAxis)) {
                page.fillCircle(xAt(p.x, xAxis, plotLeft, right), yAt(p.y, yAxis, top, plotBottom), r);
            }
        }
        page.restoreGraphicsState();
        List<Point> linked = new ArrayList<Point>();
        for (Point p : points) {
            if (p.name.isEmpty() || !onPlot(p, xAxis, yAxis)) {
                continue;
            }
            if (p.uri != null && linksApart) {
                linked.add(p);
                continue;
            }
            float[] box = drawNamed(page, p, xAxis, yAxis, plotLeft, right, top, plotBottom);
            if (p.uri != null) {
                page.addAnnotation(linkOf(p, box));
            }
        }

        page.restoreGraphicsState();
        page.setFigureBoundingBox(x, y, w, h);
        page.addEMC();

        for (Point p : linked) {
            page.saveGraphicsState();
            StructElement link = page.beginLink();
            page.addBDC(StructElem.FIGURE, null, p.name);
            float[] box = drawNamed(page, p, xAxis, yAxis, plotLeft, right, top, plotBottom);
            page.setFigureBoundingBox(box[0], box[1], box[2], box[3]);
            page.addEMC();
            page.endLink(link);
            page.restoreGraphicsState();
            Annotation annotation = linkOf(p, box);
            annotation.linkElement = link;
            page.addAnnotation(annotation);
        }
        return new float[] {x + w, y + h};
    }

    // Draws a highlighted point and its name, and returns the box of the two:
    // x, y, w, h.
    private float[] drawNamed(Page page, Point p, ChartScale.Axis xAxis, ChartScale.Axis yAxis,
            float left, float right, float top, float bottom) {
        float size = f2.getSize();
        float ascent = f2.getAscent(size);
        float big = 0.42f * size;
        float px = xAt(p.x, xAxis, left, right);
        float py = yAt(p.y, yAxis, top, bottom);
        page.setBrushColor(highlightColor);
        page.fillCircle(px, py, big);
        float width = f1.stringWidth(size, p.name);
        float lx = px + big + size / 3f;
        if (lx + width > right) {
            lx = px - big - size / 3f - width;  // At the left, not past the plot
        }
        float baseline = py + ascent / 2f;
        // On a patch of the panel's color, which the dots and the grid do not
        // show through
        page.setBrushColor(panelColor >= 0 ? panelColor : Color.white);
        page.fillRect(lx - 1f, baseline - f1.getAscent(size) - 1f,
                width + 2f, f1.getAscent(size) + f1.getDescent(size) + 2f);
        page.drawString(f1, size, p.name, lx, baseline, Util.toRGB(highlightColor), null);
        float x1 = Math.min(px - big, lx);
        float y1 = Math.min(py - big, baseline - f1.getAscent(size));
        float x2 = Math.max(px + big, lx + width);
        float y2 = Math.max(py + big, baseline + f1.getDescent(size));
        return new float[] {x1, y1, x2 - x1, y2 - y1};
    }

    // The link annotation of a point, over the box of its dot and its name,
    // described by its name.
    private static Annotation linkOf(Point p, float[] box) {
        return new Annotation(
                Annotation.Link,
                box[0],
                box[1],
                box[0] + box[2],
                box[1] + box[3],
                null,   // Vertices
                null,   // Fill Color
                0f,     // Opacity
                null,   // Title
                null,   // Contents
                p.uri,
                null,
                null,
                null,
                p.name);
    }

    // Tells if the point is on the plot: a point outside a range that is set
    // is not drawn.
    private static boolean onPlot(Point p, ChartScale.Axis xAxis, ChartScale.Axis yAxis) {
        return ChartScale.onAxis(p.x, xAxis) && ChartScale.onAxis(p.y, yAxis);
    }

    // The x on the page of a value of the horizontal axis.
    private static float xAt(float v, ChartScale.Axis axis, float left, float right) {
        return left + (v - axis.min) * (right - left) / (axis.max - axis.min);
    }

    // The y on the page of a value of the vertical axis.
    private static float yAt(float v, ChartScale.Axis axis, float top, float bottom) {
        return bottom - (v - axis.min) * (bottom - top) / (axis.max - axis.min);
    }

    // The point of the trend line at x, moved along the line to the top or
    // the bottom of the plot when it is past them.
    private static double[] clip(double x, double y, double slope, double intercept, ChartScale.Axis axis) {
        if (slope != 0 && y < axis.min) {
            return new double[] {(axis.min - intercept) / slope, axis.min};
        }
        if (slope != 0 && y > axis.max) {
            return new double[] {(axis.max - intercept) / slope, axis.max};
        }
        return new double[] {x, y};
    }
}   // End of SimpleScatterChart.java
