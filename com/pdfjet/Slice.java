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
    public float angle = 0.0f;
    public int color = 0;
    public String text = "";
    public String tooltip = "";

    public Slice(float angle, int color, String text, String tooltip) {
        this.angle = angle;
        this.color = color;
        this.text = text;
        this.tooltip = tooltip;
    }
}
