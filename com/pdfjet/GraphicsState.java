/**
 * GraphicsState.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The graphics state class
 */
public class GraphicsState {
    /** Default Constructor */
    public GraphicsState() {
    }

    // Default values
    private float CA = 1f;
    private float ca = 1f;

    /**
     * Set the alpha stroking color
     *
     * @param CA the alpha stroking color
     */
    public void setAlphaStroking(float CA) {
        if (CA >= 0f && CA <= 1f) {
            this.CA = CA;
        }
    }

    /**
     * Get the stroking alpha color
     *
     * @return the stroking alpha color
     */
    public float getAlphaStroking() {
        return this.CA;
    }

    /**
     * Set the non stroking alpha color
     *
     * @param ca the non stroking alpha color
     */
    public void setAlphaNonStroking(float ca) {
        if (ca >= 0f && ca <= 1f) {
            this.ca = ca;
        }
    }

    /**
     * Get the non stroking alpha color
     *
     * @return the non stroking alpha color
     */
    public float getAlphaNonStroking() {
        return this.ca;
    }
}
