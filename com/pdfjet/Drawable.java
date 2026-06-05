/**
 * Drawable.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Interface that is required for components that can be drawn on a PDF page as part of Optional Content Group.
 *
 * @author Mark Paxton
 */
public interface Drawable {
    /**
     * Draw the component implementing this interface on the PDF page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception if the draw method did not succeed.
     */
    public float[] drawOn(Page page) throws Exception;

    /**
     * Set the x and y coordinates of the drawable object
     *
     * @param x the x location
     * @param y the y location
     */
    public void setPosition(float x, float y);
}
