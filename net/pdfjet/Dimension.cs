/*
 * Dimension.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>
/// Encapsulates the width and height of a component.
/// </summary>
public class Dimension {
    internal float w;
    internal float h;

    /// <summary>
    /// Constructor for creating dimension objects.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <param name="height">the height.</param>
    public Dimension(float width, float height) {
        this.w = width;
        this.h = height;
    }

    public float GetWidth() {
        return w;
    }

    public float GetHeight() {
        return h;
    }
}   // End of Dimension.cs
}   // End of namespace PDFjet.NET
