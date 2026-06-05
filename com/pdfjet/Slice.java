/**
 * Slice.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * This class is used the from the Pie Chart.
 */
public class Slice {
    Float angle;
    int color;

    /**
     * Creates slice object to be used with the pie chart.
     *
     * @param percent the percent of the total.
     * @param color the slice color.
     */
    public Slice(Float percent, int color) {
        this.angle = percent*3.6f;
        this.color = color;
    }

    /**
     * Creates slice object to be used with the pie chart.
     *
     * @param percent the percent of the total.
     * @param color the slice color.
     */
    public Slice(String percent, int color) {
        Float value = Float.valueOf(
                percent.substring(0, percent.length() - 1));
        this.angle = value*3.6f;
        this.color = color;
    }
}
