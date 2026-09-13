/*
 * PageSize.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The width and height of a page in points; 1 point is 1/72 inch.
 * A page size cannot be changed once it is created, so the PORTRAIT and LANDSCAPE
 * constants of A3, A4, A5, B5, Executive, Legal, Letter and Tabloid are the same
 * for every page made with them. Use the constructor for any other page size.
 */
public final class PageSize {
    private final float width;
    private final float height;

    /**
     * Creates a page size.
     *
     * @param width the width of the page in points.
     * @param height the height of the page in points.
     */
    public PageSize(float width, float height) {
        this.width = width;
        this.height = height;
    }

    /**
     * Returns the width of the page.
     *
     * @return the width of the page in points.
     */
    public float getWidth() {
        return width;
    }

    /**
     * Returns the height of the page.
     *
     * @return the height of the page in points.
     */
    public float getHeight() {
        return height;
    }
}
