/**
 * Dimension.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Encapsulates the width and height of a component.
 */
namespace PDFjet.NET {
public class Dimension {
    internal float w;
    internal float h;

    /**
     * Constructor for creating dimension objects.
     *
     * @param width the width.
     * @param height the height.
     */
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
