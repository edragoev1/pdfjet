/*
 * Slice.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// A slice of a DonutChart: a value, a color and a label. The slice's share
/// of the sum of the values of the chart sets its angle.
/// </summary>
public class Slice {
    internal readonly float value;
    internal readonly int color;
    internal readonly String text;

    /// <summary>Creates a slice.</summary>
    /// <param name="value">the value of the slice, above 0; the chart draws its share of the sum.</param>
    /// <param name="color">the color as a 0xRRGGBB value, for example Color.blue.</param>
    /// <param name="text">the label drawn next to the slice.</param>
    public Slice(float value, int color, String text) {
        this.value = value;
        this.color = color;
        this.text = text == null ? "" : text;
    }
}
}
