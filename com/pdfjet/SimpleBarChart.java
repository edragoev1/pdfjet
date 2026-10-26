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
 * highlighted, in navy, its name and value bold, as the best of the values.
 * Each bar is as long as its share of the largest value.
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
     * bar, with its value written.
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
        return this;
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
     * Sets the color of the panel behind the chart, or -1 for none. It is
     * light gray, 0xF1F3F6, unless set.
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
        return top() + values.size() * rowHeight() + pad();
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
        return ChartScale.format(value, minFractionDigits, maxFractionDigits);
    }

    // The font of the name and the value of the bar of the index.
    private Font fontOf(int index) {
        return index == highlight ? f1 : f2;
    }

    // The alternate description: the one set, or the title, the subtitle and
    // the values.
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
            sb.append("Bar chart");
        }
        for (int i = 0; i < names.size(); i++) {
            sb.append(i == 0 ? ": " : ", ");
            sb.append(names.get(i)).append(' ').append(label(values.get(i)));
        }
        return sb.append('.').toString();
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
        if (page == null || values.isEmpty()) {     // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.getSize();
        float pad = pad();

        // The widest name and value, and the room left for the bars
        float nameWidth = 0f;
        float valueWidth = 0f;
        float largest = 0f;
        for (int i = 0; i < names.size(); i++) {
            Font font = fontOf(i);
            nameWidth = Math.max(nameWidth, font.stringWidth(size, names.get(i)));
            valueWidth = Math.max(valueWidth, font.stringWidth(size, label(values.get(i))));
            if (ChartScale.isValue(values.get(i))) {
                largest = Math.max(largest, values.get(i));
            }
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
            page.drawString(f1, f1.getSize(), title, x + pad, baseline, Util.toRGB(INK), null);
            baseline += f1.getDescent(f1.getSize());
        }
        if (!subtitle.isEmpty()) {
            baseline += 1.4f * subtitleSize();
            page.drawString(f2, subtitleSize(), subtitle, x + pad, baseline, Util.toRGB(MUTED), null);
        }

        float rowTop = y + top();
        float barHeight = 1.6f * size;
        float ascent = f2.getAscent(size);
        for (int i = 0; i < names.size(); i++) {
            Font font = fontOf(i);
            float value = values.get(i);
            float top = rowTop + i * rowHeight();
            float textBaseline = top + (barHeight + ascent - f2.getDescent(size)) / 2f;
            boolean highlighted = i == highlight;
            page.drawString(font, size, names.get(i), x + pad, textBaseline,
                    Util.toRGB(highlighted ? highlightColor : INK), null);
            float length = 0f;
            if (largest > 0f && ChartScale.isValue(value) && value > 0f) {
                length = barRoom * (value / largest);
            }
            if (length > 0f) {
                page.setBrushColor(highlighted ? highlightColor : barColor);
                page.fillRect(barX, top, Math.max(length, 1.5f), barHeight);
            }
            page.drawString(font, size, label(value), barX + length + size / 2f, textBaseline,
                    Util.toRGB(highlighted ? INK : MUTED), null);
        }

        page.restoreGraphicsState();
        page.setFigureBoundingBox(x, y, w, h);
        page.addEMC();
        return new float[] {x + w, y + h};
    }
}   // End of SimpleBarChart.java
