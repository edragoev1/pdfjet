package com.pdfjet;

/*
 * Slice.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Represents a single slice in a Donut or Pie chart.
 */
public class Slice {
    /** The angle of the slice in degrees. */
    public float angle = 0.0f;
    /** The color of the slice as a 0xRRGGBB value. */
    public int color = 0;
    /** The label drawn next to the slice. */
    public String text = "";
    /** The tooltip of the slice. */
    public String tooltip = "";

    /**
     * Creates a slice.
     *
     * @param angle the angle of the slice in degrees.
     * @param color the color as a 0xRRGGBB value.
     * @param text the label drawn next to the slice.
     * @param tooltip the tooltip of the slice.
     */
    public Slice(float angle, int color, String text, String tooltip) {
        this.angle = angle;
        this.color = color;
        this.text = text;
        this.tooltip = tooltip;
    }
}
