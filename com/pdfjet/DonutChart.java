/*
 * DonutChart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * A donut or pie chart: each slice is its value's share of the sum of the
 * values, with a label next to it and its percentage inside it. A chart with
 * an inner radius of 0 is a pie chart. In the look of the other charts: a
 * title and a subtitle at the top left, thin white lines between the slices,
 * gray leader lines, the labels in ink, and each percentage white or ink by
 * its slice's color. See Example_28.
 */
public class DonutChart implements Drawable {
    Font f1;    // The regular font: the labels, the subtitle and the legend
    Font f2;    // The bold font: the percentages, the title and the text in the hole
    float x = 0.0f;
    float y = 0.0f;
    float r1 = 0.0f;
    float r2 = 0.0f;
    List<Slice> slices;
    private String altDescription = null;
    private String title = "";
    private String subtitle = "";
    private String centerText = "";
    private boolean legend = false;

    // The look of the simple charts
    private static final int INK = SimpleBarChart.INK;
    private static final int MUTED = SimpleBarChart.MUTED;
    private static final int LEADER = 0x8C96A3;

    /**
     * Creates a donut chart. With an inner radius of 0 it is a pie chart. Its
     * fonts are in the order of the other charts': the bold one first.
     *
     * @param bold the bold font: the percentages inside the slices, the title
     *     and the text in the hole.
     * @param regular the regular font: the slice labels, the subtitle and the
     *     legend, and the size of the title.
     */
    public DonutChart(Font bold, Font regular) {
        this.f1 = regular;
        this.f2 = bold;
        this.slices = new ArrayList<>();
    }

    /**
     * Sets the top left corner of this chart: of its outer circle, or of its
     * title when it has one, the circle under it. The center is one outer
     * radius to the right of the corner of the circle and one below. The
     * slice labels can extend past the circle.
     *
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @return this DonutChart object.
     */
    public DonutChart setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the outer and inner radius of this chart.
     *
     * @param outerRadius the outer radius.
     * @param innerRadius the inner radius; 0 for a pie chart.
     * @return this DonutChart object.
     */
    public DonutChart setRadii(float outerRadius, float innerRadius) {
        // The larger is the outer radius: an inner radius past the outer one
        // drew a ring inside out
        // A radius that is not a number is 0, in the four ports alike
        float a = isValue(outerRadius) ? Math.abs(outerRadius) : 0f;
        float b = isValue(innerRadius) ? Math.abs(innerRadius) : 0f;
        this.r1 = Math.max(a, b);
        this.r2 = Math.min(a, b);
        return this;
    }

    /**
     * Adds a slice in the next color of the palette, navy first, the palette
     * of BarChart, so that the charts of a document match; its eight colors
     * repeat from the ninth slice on. The slice's share of the sum of the
     * values sets its angle; a value that is not above 0 has no slice.
     *
     * @param name the label of the slice; empty for none.
     * @param value the value of the slice.
     * @return this DonutChart object.
     */
    public DonutChart add(String name, float value) {
        return add(name, value, Slice.PALETTE);
    }

    /**
     * Adds a slice in the color.
     *
     * @param name the label of the slice; empty for none.
     * @param value the value of the slice.
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this DonutChart object.
     */
    public DonutChart add(String name, float value, int color) {
        slices.add(new Slice(value, color, name));
        return this;
    }

    // A slice: its value, its color, or PALETTE for the next of the palette,
    // and its label.
    private static final class Slice {
        static final int PALETTE = -1;

        final float value;
        final int color;
        final String text;

        Slice(float value, int color, String text) {
            this.value = value;
            this.color = color;
            this.text = (text == null) ? "" : text;
        }
    }

    /**
     * Sets the title, drawn at the top left of the chart's location in the
     * bold font at the size of the labels; the circle is drawn under it. A
     * chart without the bold font, which draws no percentages, draws its
     * title in the regular one. A chart without a title
     * has the circle at its location.
     *
     * @param title the title.
     * @return this DonutChart object.
     */
    public DonutChart setTitle(String title) {
        this.title = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the subtitle, drawn under the title in a muted gray.
     *
     * @param subtitle the subtitle.
     * @return this DonutChart object.
     */
    public DonutChart setSubtitle(String subtitle) {
        this.subtitle = (subtitle == null) ? "" : subtitle;
        return this;
    }

    /**
     * Sets the text written in the hole of a donut chart, in the font of the
     * title, as large as one and a half labels or as fits: the total, "100
     * crates". A text too wide for the hole at three quarters of a label is
     * shortened with an ellipsis. A pie chart, with no hole, does not write
     * it.
     *
     * @param centerText the text.
     * @return this DonutChart object.
     */
    public DonutChart setCenterText(String centerText) {
        this.centerText = (centerText == null) ? "" : centerText;
        return this;
    }

    /**
     * Draws a legend under the circle, a swatch, the label and the percentage
     * of each slice, in rows as wide as the circle, in place of the leader
     * lines and the labels beside the circle: for many slices, or small ones
     * whose labels would crowd.
     *
     * @param legend true to draw the legend.
     * @return this DonutChart object.
     */
    public DonutChart setLegend(boolean legend) {
        this.legend = legend;
        return this;
    }

    /**
     * Sets the alternate description of the chart, which a screen reader reads
     * in a PDF/UA document, where the chart is a figure. The default is the
     * title, the subtitle and the text in the hole, as sentences, and the
     * label and the percentage of each slice.
     *
     * @param altDescription the alternate description.
     * @return this DonutChart object.
     */
    public DonutChart setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    private float[][] getControlPoints(
            float xc, float yc,
            float x0, float y0,
            float x3, float y3) {
        List<float[]> points = new ArrayList<>();

        float ax = x0 - xc;
        float ay = y0 - yc;
        float bx = x3 - xc;
        float by = y3 - yc;
        float q1 = ax * ax + ay * ay;
        float q2 = q1 + ax * bx + ay * by;
        float cross = ax * by - ay * bx;
        // An arc of radius zero, the center of a pie chart, or of no angle has
        // its control points at its ends; the formula would divide 0 by 0.
        // In floats, rounded at each step as Go rounds its float32s, which
        // drew some charts 0.01 apart from it (review of 11 October 2026, #25).
        float k2 = (cross == 0f) ? 0f : (4.0f / 3.0f) * ((float) Math.sqrt(2f * q1 * q2) - q2) / cross;

        // Control points coordinates
        float x1 = xc + ax - k2 * ay;
        float y1 = yc + ay + k2 * ax;
        float x2 = xc + bx + k2 * by;
        float y2 = yc + by - k2 * bx;

        points.add(new float[]{x0, y0});
        points.add(new float[]{x1, y1});
        points.add(new float[]{x2, y2});
        points.add(new float[]{x3, y3});

        return points.toArray(new float[0][]);
    }

    private float[] getPoint(float xc, float yc, float radius, float angle) {
        float x = xc + radius * ((float) Math.cos(angle * Math.PI / 180.0));
        float y = yc + radius * ((float) Math.sin(angle * Math.PI / 180.0));
        return new float[]{x, y};
    }

    private float drawSlice(
            Page page,
            int fillColor,
            float xc, float yc,
            float r1, float r2,                  // r1 > r2
            float a1, float a2) {                // a1 > a2
        page.setBrushColor(fillColor);

        // No arc of angles that are not numbers, whose loop never ended
        if (!isValue(a1) || !isValue(a2)) {
            return a1;
        }
        float angle1 = a1 - 90.0f;
        float angle2 = a2 - 90.0f;

        List<float[]> points1 = new ArrayList<>();
        List<float[]> points2 = new ArrayList<>();
        while (true) {
            if (angle2 - angle1 <= 90.0f) {
                float[] p0 = getPoint(xc, yc, r1, angle1);   // Start point
                float[] p3 = getPoint(xc, yc, r1, angle2);   // End point
                points1.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                p0 = getPoint(xc, yc, r2, angle1);           // Start point
                p3 = getPoint(xc, yc, r2, angle2);           // End point
                points2.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                break;
            } else {
                float[] p0 = getPoint(xc, yc, r1, angle1);
                float[] p3 = getPoint(xc, yc, r1, angle1 + 90.0f);
                points1.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                p0 = getPoint(xc, yc, r2, angle1);
                p3 = getPoint(xc, yc, r2, angle1 + 90.0f);
                points2.addAll(toList(getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])));
                angle1 += 90.0f;
            }
        }
        Collections.reverse(points2);

        page.moveTo(points1.get(0)[0], points1.get(0)[1]);
        int i = 0;
        while (i <= points1.size() - 4) {
            page.curveTo(
                    points1.get(i + 1)[0], points1.get(i + 1)[1],
                    points1.get(i + 2)[0], points1.get(i + 2)[1],
                    points1.get(i + 3)[0], points1.get(i + 3)[1]);
            i += 4;
        }
        page.lineTo(points2.get(0)[0], points2.get(0)[1]);
        i = 0;
        while (i <= points2.size() - 4) {
            page.curveTo(
                    points2.get(i + 1)[0], points2.get(i + 1)[1],
                    points2.get(i + 2)[0], points2.get(i + 2)[1],
                    points2.get(i + 3)[0], points2.get(i + 3)[1]);
            i += 4;
        }
        page.fillPath();

        return a2;
    }

    // The leader line and the label of a slice: the point on the edge of the
    // circle, the elbow 15 points past it, the y of the label's line, moved
    // down from the elbow's when it would meet the label above it, and the
    // side of the circle it is on.
    private static final class Pointer {
        final String text;
        final float[] p1;
        final float[] p2;
        final boolean right;
        float xElbow;
        float yEnd;

        Pointer(String text, float[] p1, float[] p2, boolean right) {
            this.text = text;
            this.p1 = p1;
            this.p2 = p2;
            this.right = right;
            this.xElbow = p2[0];
            this.yEnd = p2[1];
        }
    }

    // Returns the pointer of a slice from the angle a1 to a2.
    private Pointer pointerOf(String text, float xc, float yc, float r1, float a1, float a2) {
        float midAngle = (a1 + a2) / 2.0f - 90.0f;
        float[] p1 = getPoint(xc, yc, r1, midAngle);            // On the outer edge
        float[] p2 = getPoint(xc, yc, r1 + 15.0f, midAngle);    // The elbow
        // A label wider than both the radius and six of its letters is
        // shortened: a long one at the left ran off the page
        if (f1 != null) {
            text = ChartScale.shorten(f1, f1.getSize(), text, Math.max(r1, 6f * f1.getSize()));
        }
        return new Pointer(text, p1, p2, Math.cos(midAngle * Math.PI / 180.0) >= 0);
    }

    // Moves the labels on each side of the circle apart, top to bottom, where
    // one would meet the one above it, and each moved label's elbow out to
    // where its height leaves the circle, 15 points past its edge: thin slices
    // side by side stacked their labels down a column across the ring.
    private void moveApart(List<Pointer> pointers, float xc, float yc) {
        if (f1 == null) {
            return;
        }
        for (int side = 0; side < 2; side++) {
            List<Pointer> labels = new ArrayList<>();
            for (Pointer pointer : pointers) {
                if (pointer.right == (side == 0) && !pointer.text.isEmpty()) {
                    labels.add(pointer);
                }
            }
            // Top to bottom, and labels at one height in the order of their slices
            Collections.sort(labels, (a, b) -> a.yEnd != b.yEnd
                    ? Float.compare(a.yEnd, b.yEnd) : Integer.compare(pointers.indexOf(a), pointers.indexOf(b)));
            for (int k = 1; k < labels.size(); k++) {
                labels.get(k).yEnd = Math.max(labels.get(k).yEnd, labels.get(k - 1).yEnd + f1.getBodyHeight());
            }
            float r3 = r1 + 15.0f;
            for (Pointer label : labels) {
                if (label.yEnd == label.p2[1]) {
                    continue;   // Where it was
                }
                float dy = label.yEnd - yc;
                float out = (Math.abs(dy) < r3) ? (float) Math.sqrt((double) r3 * r3 - (double) dy * dy) : 0f;
                label.xElbow = label.right ? Math.max(label.p2[0], xc + out) : Math.min(label.p2[0], xc - out);
            }
        }
    }

    // Returns the box of the leader line and the label of the pointer: its
    // left, top, right and bottom; and draws them when there is a page.
    private float[] drawPointer(Page page, Pointer pointer) throws Exception {
        float[] p1 = pointer.p1;
        float x2 = pointer.xElbow;
        float yEnd = pointer.yEnd;
        if (page != null) {
            // The line: edge, elbow, and along to the end, thin and gray
            page.setPenColor(LEADER);
            page.setPenWidth(0.75f);
            page.moveTo(p1[0], p1[1]);
            page.lineTo(x2, yEnd);
        }
        if (f1 != null && !pointer.text.isEmpty()) {
            float textWidth = f1.stringWidth(pointer.text);
            float lineLength = textWidth + 4.0f;
            float xEnd = pointer.right ? x2 + lineLength : x2 - lineLength;
            // The label just above the line
            float x = pointer.right ? x2 + 2.0f : xEnd + 2.0f;
            float baseline = yEnd - f1.getAscent() / 3.0f;
            if (page != null) {
                page.lineTo(xEnd, yEnd);
                page.strokePath();
                page.drawString(f1, f1.getSize(), pointer.text, x, baseline, Util.toRGB(INK), null);
            }
            return new float[] {
                    Math.min(Math.min(p1[0], x2), Math.min(xEnd, x)),
                    Math.min(Math.min(p1[1], yEnd), baseline - f1.getAscent()),
                    Math.max(Math.max(p1[0], x2), Math.max(xEnd, x + textWidth)),
                    Math.max(Math.max(p1[1], yEnd), baseline + f1.getDescent())};
        }
        // No text: a short line along
        float xEnd = pointer.right ? x2 + 20.0f : x2 - 20.0f;
        if (page != null) {
            page.lineTo(xEnd, yEnd);
            page.strokePath();
        }
        return new float[] {
                Math.min(Math.min(p1[0], x2), xEnd), Math.min(p1[1], yEnd),
                Math.max(Math.max(p1[0], x2), xEnd), Math.max(p1[1], yEnd)};
    }

    /**
     * Draws this chart on the specified page.
     *
     * @param page the page to draw on, or null to measure the chart.
     * @return x and y coordinates of the bottom right corner of the chart:
     * of its title, its circle, and its leader lines and labels, which can
     * reach past the circle, or its legend. A chart with no slice above 0
     * draws nothing, not its title either, and returns the corner of its
     * circle.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        // The title and the subtitle above the circle, when there are any
        Font titleFont = (f2 != null) ? f2 : f1;
        float titleSize = (f1 != null) ? f1.getSize() : (titleFont != null ? titleFont.getSize() : 0f);
        float top = y;
        float titleBaseline = 0f;
        float subtitleBaseline = 0f;
        if (titleFont != null && !title.isEmpty()) {
            titleBaseline = top + titleFont.getAscent(titleSize);
            top = titleBaseline + titleFont.getDescent(titleSize);
        }
        if (f1 != null && !subtitle.isEmpty()) {
            subtitleBaseline = top + f1.getAscent();
            top = subtitleBaseline + f1.getDescent();
        }
        if (top > y) {
            // Room for a label at the top of the circle: its leader line goes
            // 15 points up, the label sits above its end; and half a line
            float label = (f1 != null && !legend) ? f1.getAscent() / 3f + f1.getAscent() : 0f;
            top += (legend ? 0f : 15f) + label + titleSize / 2f;
        }
        float xc = x + r1;      // the center of the chart
        float yc = top + r1;

        // The slices with a value above 0 share the circle
        float[] values = values();
        float total = 0.0f;
        for (float value : values) {
            total += value;
        }
        if (total <= 0.0f) {   // Nothing to draw
            return new float[] {xc + r1, yc + r1};
        }
        int[] percentages = percentages(values, total);

        // The leader lines and the labels, moved apart, and the box of the
        // figure: the title, the outer circle, the leader lines and the labels
        List<Pointer> pointers = new ArrayList<>();
        int[] colors = new int[slices.size()];
        float angle = 0.0f;
        int count = 0;
        for (int i = 0; i < slices.size(); i++) {
            if (values[i] > 0.0f) {
                float sweep = values[i] * 360.0f / total;
                if (!legend) {
                    pointers.add(pointerOf(slices.get(i).text, xc, yc, r1, angle, angle + sweep));
                }
                angle += sweep;
                int color = slices.get(i).color;
                colors[i] = (color == Slice.PALETTE) ? BarChart.PALETTE[count % BarChart.PALETTE.length] : color;
                count++;
            }
        }
        // The last slice, next to the first around the circle, in the palette's
        // next color when it would have the first's: the ninth of nine was navy
        // by the navy first
        int first = -1;
        int last = -1;
        for (int i = 0; i < slices.size(); i++) {
            if (values[i] > 0.0f) {
                first = (first < 0) ? i : first;
                last = i;
            }
        }
        if (count > 1 && slices.get(last).color == Slice.PALETTE && colors[last] == colors[first]) {
            colors[last] = BarChart.PALETTE[count % BarChart.PALETTE.length];
        }
        moveApart(pointers, xc, yc);
        float[] box = {x, y, x + 2f * r1, yc + r1};
        if (titleBaseline > 0f) {
            box[2] = Math.max(box[2], x + titleFont.stringWidth(titleSize, title));
        }
        if (subtitleBaseline > 0f) {
            box[2] = Math.max(box[2], x + f1.stringWidth(subtitle));
        }
        for (Pointer pointer : pointers) {
            box = union(box, drawPointer(null, pointer));
        }
        if (legend) {
            box = union(box, drawLegend(null, values, percentages, colors, yc + r1));
        }
        if (page == null) {     // Measured
            return new float[] {box[2], box[3]};
        }

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, null, altDescription(values, percentages));
        // The pen, the brush and the dash pattern of the caller are kept, as
        // a Stamp keeps them: the chart left the page with
        // the black pen of its pointers and the color of its last slice.
        page.saveGraphicsState();
        page.setDefaultStrokeDashPattern();     // Not the caller's, which dashed the leader lines
        if (titleBaseline > 0f) {
            page.drawString(titleFont, titleSize, title, x, titleBaseline, Util.toRGB(INK), null);
        }
        if (subtitleBaseline > 0f) {
            page.drawString(f1, f1.getSize(), subtitle, x, subtitleBaseline, Util.toRGB(MUTED), null);
        }
        angle = 0.0f;
        int drawn = 0;
        List<Float> edges = new ArrayList<>();
        List<Float> sweeps = new ArrayList<>();
        for (int i = 0; i < slices.size(); i++) {
            if (values[i] <= 0.0f) {
                continue;
            }
            int color = colors[i];
            float sweep = values[i] * 360.0f / total;
            angle = drawSlice(
                    page, color,
                    xc, yc,
                    r1, r2,
                    angle, angle + sweep);
            edges.add(angle);
            sweeps.add(sweep);
            if (!legend) {
                drawPointer(page, pointers.get(drawn));
            }
            drawn++;
            // The percentage fits inside a slice of 15 degrees or more, as
            // long across the middle of the ring as it is wide, and in a ring
            // as wide as one and a half lines of it, or a pie. A small pie's
            // slices had percentages wider than they were (the review of 28
            // October 2026)
            String pctStr = percentages[i] + "%";
            float midR = (r1 + r2) / 2.0f;
            if (f2 != null && sweep >= 15.0f && (r2 == 0.0f || r1 - r2 >= 1.5f * f2.getBodyHeight())
                    && f2.stringWidth(pctStr) <= midR * ((float) (sweep * Math.PI / 180.0))) {
                float midAngle = angle - sweep / 2.0f - 90.0f;
                float[] pos = getPoint(xc, yc, midR, midAngle);
                page.drawString(f2, f2.getSize(), pctStr,
                        pos[0] - f2.stringWidth(pctStr) / 2.0f,
                        pos[1] + f2.getAscent() / 3.0f,
                        Util.toRGB(BarChart.labelColorOn(color)), null);
            }
        }
        // A thin white line between the slices, from the hole to the edge, at
        // most a quarter as wide as the thinner of the two slices is across
        // the middle of the ring, so that it does not cover a thin slice
        if (drawn > 1) {
            page.setPenColor(Color.white);
            float midR = (r1 + r2) / 2.0f;
            for (int k = 0; k < drawn; k++) {
                float thinner = Math.min(sweeps.get(k), sweeps.get((k + 1) % drawn));
                float across = midR * ((float) (thinner * Math.PI / 180.0));
                page.setPenWidth(Math.min(1.5f, 0.25f * across));
                float edge = edges.get(k);
                float[] inner = getPoint(xc, yc, r2, edge - 90.0f);
                float[] outer = getPoint(xc, yc, r1, edge - 90.0f);
                page.drawLine(inner[0], inner[1], outer[0], outer[1]);
            }
        }
        if (legend) {
            drawLegend(page, values, percentages, colors, yc + r1);
        }
        // The text in the hole, as large as one and a half labels or as fits
        if (r2 > 0.0f && titleFont != null && !centerText.isEmpty()) {
            // At three quarters of a label at least, shortened to fit there:
            // a long text was drawn as small as it took, too small to read
            float size = 1.5f * titleSize;
            String text = centerText;
            float width = titleFont.stringWidth(size, text);
            if (width > 1.6f * r2) {
                size = Math.max(size * (1.6f * r2) / width, 0.75f * titleSize);
                text = ChartScale.shorten(titleFont, size, text, 1.6f * r2);
                width = titleFont.stringWidth(size, text);
            }
            float baseline = yc + (titleFont.getAscent(size) - titleFont.getDescent(size)) / 2f;
            page.drawString(titleFont, size, text, xc - width / 2f, baseline, Util.toRGB(INK), null);
        }
        page.restoreGraphicsState();
        page.setFigureBoundingBox(box[0], box[1], box[2] - box[0], box[3] - box[1]);
        page.addEMC();
        return new float[] {box[2], box[3]};
    }

    // Returns the box of the legend under the circle, whose bottom is at the
    // y: a swatch, the label and the percentage of each slice, in rows as wide
    // as the circle; and draws it when there is a page.
    private float[] drawLegend(Page page, float[] values, int[] percentages, int[] colors, float y) {
        float[] box = {x, y, x, y};
        if (f1 == null) {
            return box;
        }
        float swatch = f1.getAscent();
        float gap = swatch / 2f;
        float left = x;
        float baseline = y + 1.5f * f1.getBodyHeight();
        for (int i = 0; i < slices.size(); i++) {
            if (values[i] <= 0.0f) {
                continue;
            }
            // A label too long for a row is shortened, its percentage kept
            String percentage = percentages[i] + "%";
            String text = percentage;
            if (!slices.get(i).text.isEmpty()) {
                float room = 2f * r1 - swatch - gap - f1.stringWidth(" " + percentage);
                text = ChartScale.shorten(f1, f1.getSize(), slices.get(i).text, room) + " " + percentage;
            }
            float width = f1.stringWidth(text);
            if (left > x && left + swatch + gap + width > x + 2f * r1) {
                left = x;
                baseline += 1.5f * f1.getBodyHeight();
            }
            if (page != null) {
                page.setBrushColor(colors[i]);
                page.fillRect(left, baseline - swatch, swatch, swatch);
                page.drawString(f1, f1.getSize(), text, left + swatch + gap, baseline, Util.toRGB(INK), null);
            }
            box[2] = Math.max(box[2], left + swatch + gap + width);
            box[3] = baseline + f1.getDescent();
            left += swatch + gap + width + f1.getBodyHeight();
        }
        return box;
    }

    // Returns the box that holds the two boxes, each left, top, right, bottom.
    private static float[] union(float[] a, float[] b) {
        return new float[] {Math.min(a[0], b[0]), Math.min(a[1], b[1]),
                Math.max(a[2], b[2]), Math.max(a[3], b[3])};
    }

    // Returns the value of each slice that is drawn, and 0 for the others: a
    // slice is drawn when its value is a number above 0. A NaN, from 0/0 in
    // an empty category of real data, or an infinity made the arc's loop
    // endless, until memory ran out (review of 11 October 2026, #2). Values
    // whose 360ths or whose sum are past the largest float are divided by the
    // largest of them, so that they are drawn as they compare.
    private float[] values() {
        float[] values = new float[slices.size()];
        float largest = 0.0f;
        float total = 0.0f;
        for (int i = 0; i < values.length; i++) {
            float value = slices.get(i).value;
            if (value > 0.0f && isValue(value)) {
                values[i] = value;
                largest = Math.max(largest, value);
                total += value;
            }
        }
        if (largest > 0.0f && (!isValue(largest * 360.0f) || !isValue(total))) {
            for (int i = 0; i < values.length; i++) {
                values[i] = values[i] / largest;
            }
        }
        return values;
    }

    // Returns the whole percentage of each slice that is drawn, by the largest
    // remainder, so that they add up to 100: each rounded alone, 90 and six of
    // 1.5 were 91 and six of 2.
    private static int[] percentages(float[] values, float total) {
        int[] percentages = new int[values.length];
        double[] remainders = new double[values.length];
        int sum = 0;
        for (int i = 0; i < values.length; i++) {
            remainders[i] = -1.0;
            if (values[i] > 0.0f) {
                double exact = (double) values[i] * 100.0 / (double) total;
                percentages[i] = (int) Math.floor(exact);
                remainders[i] = exact - percentages[i];
                sum += percentages[i];
            }
        }
        // The points left over to the largest remainders, the first of equal ones
        for (; sum < 100; sum++) {
            int largest = -1;
            for (int i = 0; i < values.length; i++) {
                if (remainders[i] >= 0.0 && (largest < 0 || remainders[i] > remainders[largest])) {
                    largest = i;
                }
            }
            if (largest < 0) {
                break;
            }
            percentages[largest]++;
            remainders[largest] = -1.0;
        }
        return percentages;
    }

    // Tells if the value is a number: not NaN, and not infinite.
    private static boolean isValue(float value) {
        return !Float.isNaN(value) && !Float.isInfinite(value);
    }

    // Returns the alternate description, or the title, the subtitle, and the
    // label and the percentage of each slice when none is set.
    private String altDescription(float[] values, int[] percentages) {
        if (altDescription != null && !altDescription.isEmpty()) {
            return altDescription;
        }
        String name = ChartScale.join(title, subtitle);
        if (r2 > 0.0f && (f1 != null || f2 != null)) {
            name = ChartScale.join(name, centerText);   // Drawn in the hole
        }
        if (name.isEmpty()) {
            name = (r2 > 0.0f) ? "Donut chart" : "Pie chart";
        }
        StringBuilder sb = new StringBuilder(name).append(':');
        String separator = " ";
        for (int i = 0; i < slices.size(); i++) {
            if (values[i] > 0.0f) {
                sb.append(separator);
                if (!slices.get(i).text.isEmpty()) {
                    sb.append(slices.get(i).text).append(' ');
                }
                sb.append(percentages[i]).append('%');
                separator = ", ";
            }
        }
        return sb.toString();
    }

    // Utility: convert float[][] into List<float[]> for convenient addAll
    private static List<float[]> toList(float[][] arr) {
        List<float[]> list = new ArrayList<>(arr.length);
        for (float[] a : arr) {
            list.add(a);
        }
        return list;
    }
}   // End of DonutChart.java
