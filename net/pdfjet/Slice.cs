/**
 * Slice.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
public class Slice {
    public float angle;
    public int color;

    public Slice(float percent, int color) {
        this.angle = percent*3.6f;
        this.color = color;
    }

    public Slice(String percent, int color) {
        float value = float.Parse(
                percent.Substring(0, percent.Length - 1));
        this.angle = value*3.6f;
        this.color = color;
    }
}
}
