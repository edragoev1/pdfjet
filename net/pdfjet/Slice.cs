/*
 * Slice.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>A single slice of a donut or pie chart.</summary>
public class Slice {
    /// <summary>The angle of the slice in degrees.</summary>
    public float angle = 0.0f;
    /// <summary>The color of the slice as a 0xRRGGBB value.</summary>
    public int color = 0;
    /// <summary>The label drawn next to the slice.</summary>
    public String text = "";
    /// <summary>The tooltip of the slice.</summary>
    public String tooltip = "";

    /// <summary>Creates a slice with the angle in degrees, the 0xRRGGBB color, the label and the tooltip.</summary>
    public Slice(float angle, int color, String text, String tooltip) {
        this.angle = angle;
        this.color = color;
        this.text = text;
        this.tooltip = tooltip;
    }
}
}
