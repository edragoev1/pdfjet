package com.pdfjet;

/*
 * Slice.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * A slice of a DonutChart: a value, a color and a label. The slice's share
 * of the sum of the values of the chart sets its angle.
 */
public class Slice {
    final float value;
    final int color;
    final String text;

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
}
