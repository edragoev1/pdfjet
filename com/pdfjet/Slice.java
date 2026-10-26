/*
 * Slice.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * A slice of a DonutChart: a value, a color and a label. The slice's share
 * of the sum of the values of the chart sets its angle.
 */
public class Slice {
    final float value;
    final int color;
    final String text;

    // The color of a slice that takes the next color of the palette
    static final int PALETTE = -1;

    /**
     * Creates a slice.
     *
     * @param value the value of the slice, above 0; the chart draws its share of the sum.
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @param text the label drawn next to the slice.
     */
    public Slice(float value, int color, String text) {
        this.value = value;
        this.color = color;
        this.text = text == null ? "" : text;
    }

    /**
     * Creates a slice in the next color of the chart's palette, navy first,
     * the palette of BarChart, so that the charts of a document match.
     *
     * @param value the value of the slice.
     * @param text the label of the slice.
     */
    public Slice(float value, String text) {
        this(value, PALETTE, text);
    }
}
