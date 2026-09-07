/*
 * Dimension.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Encapsulates the width and height of a component.
 */
public class Dimension {
    protected float w;
    protected float h;

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

    /**
     * Returns the width.
     *
     * @return the width.
     */
    public float getWidth() {
        return w;
    }

    /**
     * Returns the height.
     *
     * @return the height.
     */
    public float getHeight() {
        return h;
    }

}   // End of Dimension.java
