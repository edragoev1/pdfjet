/**
 * Slice.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
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
}
