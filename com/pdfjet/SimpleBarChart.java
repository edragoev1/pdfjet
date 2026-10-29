/*
 * SimpleBarChart.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A small chart of horizontal bars that compares a few values: a name at the
 * left of each bar, the value at its end, and no axis, grid or legend, on a
 * light panel, with a title and a subtitle at the top. One bar can be
 * highlighted, in navy, its name and value bold, as the best of the values:
 * the bar of an index, or the lowest or the highest value, which the chart
 * finds. Each bar is as long as its share of the largest value. A dashed line
 * can mark a value across the bars, a target or an average, and the bars can
 * have rounded ends.
 * <p>
 * It is meant to stand beside a Table of the same numbers: the chart for the
 * picture, the table for the record and for a screen reader. In a PDF/UA
 * document the chart is one figure, described from its title and its values
 * unless an alternate description is set. For grouped, stacked or vertical
 * bars with an axis, use BarChart. Please see Example_20.
 */
public class SimpleBarChart implements Drawable {
    // The colors of a simple bar chart unless set: a light panel, gray bars,
    // the highlighted bar navy, the names dark and the values gray.
    static final int PANEL = 0xF1F3F6;
    static final int BAR = 0xA7AFB8;
    static final int HIGHLIGHT = 0x1F3A5F;
    static final int INK = 0x22262B;
    static final int MUTED = 0x5F6873;

    private final Font f1;      // The title, and the bold name and value
    private final Font f2;      // The rest
    private float x;
    private float y;
    private float w = 300f;
    private String title = "";
    private String subtitle = "";
    private String altDescription = "";
    private final List<String> names = new ArrayList<String>();
    private final List<Float> values = new ArrayList<Float>();
    private int highlight = -1;     // The index of the highlighted bar, or -1
    // The bar the chart highlights itself: 0 none, -1 the lowest value, 1 the
    // highest
    private int highlightValue = 0;
    private float reference = Float.NaN;    // The value of the reference line, or NaN
    private String referenceLabel = "";
    private boolean roundedBars = false;
    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;
    private int panelColor = PANEL;
    private int barColor = BAR;
    private int highlightColor = HIGHLIGHT;

    /**
     * Creates a simple bar chart, 300 points wide.
     *
     * @param f1 the font of the title and of the highlighted bar's name and
     *     value, a bold one.
     * @param f2 the font of the subtitle and of the other names and values.
     */
    public SimpleBarChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /**
     * Sets the title, drawn at the top left of the chart.
     *
     * @param title the title.
     * @return this chart.
     */
    public SimpleBarChart setTitle(String title) {
        this.title = (title == null) ? "" : title;
        return this;
    }

    /**
     * Sets the subtitle, drawn small and gray under the title: the unit of the
     * values, say.
     *
     * @param subtitle the subtitle.
     * @return this chart.
     */
    public SimpleBarChart setSubtitle(String subtitle) {
        this.subtitle = (subtitle == null) ? "" : subtitle;
        return this;
    }

    /**
     * Sets the alternate description of the chart in a PDF/UA document.
     * Without one the chart is described from its title, its subtitle and its
     * values.
     *
     * @param altDescription the alternate description.
     * @return this chart.
     */
    public SimpleBarChart setAltDescription(String altDescription) {
        this.altDescription = (altDescription == null) ? "" : altDescription;
        return this;
    }

    /**
     * Adds a bar: its name and its value. A value below zero is drawn as no
     * bar, with its value written. A value that is NaN or infinite has no
     * bar and no value, as in the other charts, and the description leaves
     * it out.
     *
     * @param name the name of the bar.
     * @param value its value.
     * @return this chart.
     */
    public SimpleBarChart add(String name, float value) {
        names.add(name == null ? "" : name);
        values.add(value);
        return this;
    }

    /**
     * Highlights the bar of the index, the first bar 0: it is drawn in the
     * highlight color, its name and value in the bold font. Which bar is the
     * best, the lowest value or the highest, is the caller's to say.
     *
     * @param index the index of the bar.
     * @return this chart.
     */
    public SimpleBarChart setHighlight(int index) {
        this.highlight = index;
        this.highlightValue = 0;
        return this;
    }

    /**
     * Highlights the bar of the lowest value, the best of times or prices; the
     * first of them when two are lowest. Bars added later count too, as the
     * chart finds the bar when it is drawn.
     *
     * @return this chart.
     */
    public SimpleBarChart setHighlightLowest() {
        this.highlightValue = -1;
        return this;
    }

    /**
     * Highlights the bar of the highest value, the best of sales or scores;
     * the first of them when two are highest. Bars added later count too, as
     * the chart finds the bar when it is drawn.
     *
     * @return this chart.
     */
    public SimpleBarChart setHighlightHighest() {
        this.highlightValue = 1;
        return this;
    }

    /**
     * Draws a thin dashed line across the bars at a value, a target or an
     * average, with its label and value under the bars: "Target 50". The bars
     * are scaled so that the line fits, should it be past the largest value.
     * A value that is NaN or infinite draws no line.
     *
     * @param value the value of the line.
     * @param label the label written before the value, or "".
     * @return this chart.
     */
    public SimpleBarChart setReferenceLine(float value, String label) {
        this.reference = value;
        this.referenceLabel = (label == null) ? "" : label;
        return this;
    }

    /**
     * Draws the bars with slightly rounded ends. They are square unless set.
     *
     * @param roundedBars true for rounded ends.
     * @return this chart.
     */
    public SimpleBarChart setRoundedBars(boolean roundedBars) {
        this.roundedBars = roundedBars;
        return this;
    }

    // The index of the highlighted bar: the one set, or the one of the lowest
    // or highest value, or -1.
    int highlighted() {
        if (highlightValue == 0) {
            return highlight;
        }
        int best = -1;
        for (int i = 0; i < values.size(); i++) {
            float value = values.get(i);
            if (!ChartScale.isValue(value)) {
                continue;
            }
            if (best < 0 || (highlightValue < 0 ? value < values.get(best) : value > values.get(best))) {
                best = i;
            }
        }
        return best;
    }

    // True when a reference line is drawn.
    private boolean hasReference() {
        return ChartScale.isValue(reference);
    }

    // The text under the reference line: its label and its value, "Target 50",
    // or its value alone.
    String referenceText() {
        return referenceLabel.isEmpty() ? label(reference) : referenceLabel + " " + label(reference);
    }

    /**
     * Sets the number of digits after the decimal point of the values, all of
     * them alike. Without it a value has as many as it needs, two at most.
     *
     * @param digits the number of digits.
     * @return this chart.
     */
    public SimpleBarChart setFractionDigits(int digits) {
        this.minFractionDigits = digits;
        this.maxFractionDigits = digits;
        return this;
    }

    /**
     * Sets the color of the panel behind the chart. It is light gray,
     * 0xF1F3F6, unless set. Color.transparent, -1, draws no panel: the one
     * color setter where it does not leave the color as it was.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleBarChart setPanelColor(int color) {
        this.panelColor = color;
        return this;
    }

    /**
     * Sets the color of the bars that are not highlighted. It is gray,
     * 0xA7AFB8, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleBarChart setBarColor(int color) {
        this.barColor = color;
        return this;
    }

    /**
     * Sets the color of the highlighted bar and of its name. It is navy,
     * 0x1F3A5F, unless set.
     *
     * @param color the color.
     * @return this chart.
     */
    public SimpleBarChart setHighlightColor(int color) {
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
    public SimpleBarChart setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the width of the chart. Its height follows from the fonts and the
     * number of bars; see getHeight.
     *
     * @param w the width.
     * @return this chart.
     */
    public SimpleBarChart setWidth(float w) {
        this.w = w;
        return this;
    }

    /**
     * Returns the height of the chart: its padding, its title and subtitle,
     * and a row for each bar.
     *
     * @return the height of the chart.
     */
    public float getHeight() {
        if (f1 == null || f2 == null) {
            return 0f;      // Without its fonts the chart draws nothing
        }
        float h = top() + values.size() * rowHeight() + pad();
        if (hasReference()) {
            h += 1.4f * subtitleSize();     // The line's label under the bars
        }
        return h;
    }

    // The space around the chart's content, a size of the text of the bars.
    private float pad() {
        return f2.getSize();
    }

    // The height of a row, a bar and the space under it.
    float rowHeight() {
        return 2.4f * f2.getSize();
    }

    // The size of the subtitle.
    private float subtitleSize() {
        return 0.8f * f2.getSize();
    }

    // The height above the first row: the padding, the title and the subtitle.
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

    // The value written at the end of a bar.
    String label(float value) {
        if (!ChartScale.isValue(value)) {
            return "";      // No value, as in the other charts: it was written NaN or In,fin,ity
        }
        // Its digits grouped in thousands, 23,507, as in the other charts
        return ChartScale.groupDigits(ChartScale.format(value, minFractionDigits, maxFractionDigits));
    }

    // The font of the name and the value of the bar of the index.
    private Font fontOf(int index) {
        return index == highlighted() ? f1 : f2;
    }

    // The alternate description: the one set, or the title, the subtitle and
    // the values.
    String altDescription() {
        if (!altDescription.isEmpty()) {
            return altDescription;
        }
        StringBuilder sb = new StringBuilder(ChartScale.join(title, subtitle));
        if (sb.length() == 0) {
            sb.append("Bar chart");
        }
        String separator = punctuationAfter(sb, ":") + " ";
        for (int i = 0; i < names.size(); i++) {
            if (!ChartScale.isValue(values.get(i))) {
                continue;
            }
            sb.append(separator).append(names.get(i)).append(' ').append(label(values.get(i)));
            separator = ", ";
        }
        if (hasReference()) {
            sb.append("; ").append(referenceLabel.isEmpty() ? "reference line at " + label(reference) : referenceText());
        }
        return sb.append(punctuationAfter(sb, ".")).toString();
    }

    // The mark to write after the text of a description, or nothing when the
    // text ends a sentence already, in ".", "?" or "!": a title "Sales." was
    // described as "Sales.: a 5", and in a line chart as "Sales., 2019 to
    // 2024:" (the review of 28 October 2026).
    static String punctuationAfter(CharSequence text, String mark) {
        if (text.length() > 0) {
            char end = text.charAt(text.length() - 1);
            if (end == '.' || end == '?' || end == '!') {
                return "";
            }
        }
        return mark;
    }

    /**
     * Draws the chart on the page.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of the chart.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        float h = getHeight();
        if (page == null || values.isEmpty() || f1 == null || f2 == null) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.getSize();
        float pad = pad();

        // The widest name and value, and the room left for the bars
        // A name wider than two fifths of the chart is shortened with an ellipsis
        List<String> shown = new ArrayList<String>();
        float nameWidth = 0f;
        float valueWidth = 0f;
        float largest = 0f;
        for (int i = 0; i < names.size(); i++) {
            valueWidth = Math.max(valueWidth, fontOf(i).stringWidth(size, label(values.get(i))));
            if (ChartScale.isValue(values.get(i))) {
                largest = Math.max(largest, values.get(i));
            }
        }
        if (hasReference()) {
            largest = Math.max(largest, reference);     // The line within the bars' room
        }
        // The names in what the values leave of the width, two fifths of it at
        // most: in a chart narrower than its values they were written past it
        float nameRoom = Math.max(Math.min(0.4f * (w - 2f * pad), w - 3f * pad - valueWidth - size / 2f), 0f);
        for (int i = 0; i < names.size(); i++) {
            Font font = fontOf(i);
            // No name where not even an ellipsis fits; the description has it
            shown.add(nameRoom < font.stringWidth(size, "\u2026")
                    ? "" : ChartScale.shorten(font, size, names.get(i), nameRoom));
            nameWidth = Math.max(nameWidth, font.stringWidth(size, shown.get(i)));
        }
        float barX = x + pad + nameWidth + pad;
        float barRoom = Math.max(x + w - pad - (valueWidth + size / 2f) - barX, 0f);

        page.addBDC(StructElem.FIGURE, null, altDescription());
        page.saveGraphicsState();

        if (panelColor >= 0) {
            page.setBrushColor(panelColor);
            page.fillRect(x, y, w, h);
        }
        float baseline = y + pad;
        if (!title.isEmpty()) {
            baseline += f1.getAscent(f1.getSize());
            page.drawString(f1, f1.getSize(), ChartScale.shorten(f1, f1.getSize(), title, w - 2f * pad), x + pad, baseline, Util.toRGB(INK), null);
            baseline += f1.getDescent(f1.getSize());
        }
        if (!subtitle.isEmpty()) {
            baseline += 1.4f * subtitleSize();
            page.drawString(f2, subtitleSize(), ChartScale.shorten(f2, subtitleSize(), subtitle, w - 2f * pad),
                    x + pad, baseline, Util.toRGB(MUTED), null);
        }

        float rowTop = y + top();
        float barHeight = 1.6f * size;
        float ascent = f2.getAscent(size);
        int highlight = highlighted();
        for (int i = 0; i < names.size(); i++) {
            Font font = fontOf(i);
            float value = values.get(i);
            float top = rowTop + i * rowHeight();
            float textBaseline = top + (barHeight + ascent - f2.getDescent(size)) / 2f;
            boolean highlighted = i == highlight;
            page.drawString(font, size, shown.get(i), x + pad, textBaseline,
                    Util.toRGB(highlighted ? highlightColor : INK), null);
            float length = 0f;
            if (largest > 0f && ChartScale.isValue(value) && value > 0f) {
                length = barRoom * (value / largest);
            }
            if (length > 0f) {
                page.setBrushColor(highlighted ? highlightColor : barColor);
                float barW = Math.max(length, 1.5f);
                // The radius a fifth of the bar's height, or half a short bar
                float r = Math.min(barHeight / 5f, barW / 2f);
                if (roundedBars && r > 0.25f) {
                    page.fillRoundedRect(barX, top, barW, barHeight, r, r);
                } else {
                    page.fillRect(barX, top, barW, barHeight);
                }
            }
            page.drawString(font, size, label(value), barX + length + size / 2f, textBaseline,
                    Util.toRGB(highlighted ? INK : MUTED), null);
        }

        // The reference line, dashed, over the bars, from a little above the
        // first to a little below the last, and its label under it. It leaves
        // out the rows where it would cross a bar's value.
        if (hasReference() && largest > 0f && reference >= 0f) {
            float lineX = barX + barRoom * (reference / largest);
            float lineBottom = rowTop + (names.size() - 1) * rowHeight() + barHeight + size / 4f;
            page.setPenColor(INK);
            page.setPenWidth(0.75f);
            page.setStrokeDashPattern("[2 2] 0");
            float from = rowTop - size / 4f;
            for (int i = 0; i < names.size(); i++) {
                float value = values.get(i);
                float length = (ChartScale.isValue(value) && value > 0f) ? barRoom * (value / largest) : 0f;
                float labelX = barX + length + size / 2f;
                float labelW = fontOf(i).stringWidth(size, label(value));
                if (labelW > 0f && lineX > labelX - size / 2f && lineX < labelX + labelW + size / 2f) {
                    float top = rowTop + i * rowHeight();
                    if (top > from) {
                        page.drawLine(lineX, from, lineX, top);
                    }
                    from = top + barHeight;
                }
            }
            if (lineBottom > from) {
                page.drawLine(lineX, from, lineX, lineBottom);
            }
            page.setDefaultStrokeDashPattern();
            String text = referenceText();
            float width = f2.stringWidth(subtitleSize(), text);
            // Centered under the line, within the chart
            float textX = Math.max(x + pad, Math.min(lineX - width / 2f, x + w - pad - width));
            page.drawString(f2, subtitleSize(), text, textX, lineBottom + 1.2f * subtitleSize(),
                    Util.toRGB(INK), null);
        }

        page.restoreGraphicsState();
        page.setFigureBoundingBox(x, y, w, h);
        page.addEMC();
        return new float[] {x + w, y + h};
    }
}   // End of SimpleBarChart.java
