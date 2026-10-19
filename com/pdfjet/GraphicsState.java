/*
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
     * @return this GraphicsState object.
     */
    public GraphicsState setAlphaStroking(float CA) {
        if (CA >= 0f && CA <= 1f) {
            this.CA = CA;
        }
        return this;
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
     * @return this GraphicsState object.
     */
    public GraphicsState setAlphaNonStroking(float ca) {
        if (ca >= 0f && ca <= 1f) {
            this.ca = ca;
        }
        return this;
    }

    /**
     * Get the non stroking alpha color
     *
     * @return the non stroking alpha color
     */
    public float getAlphaNonStroking() {
        return this.ca;
    }

    // Null writes no /BM entry, as before there was a blend mode (suggested
    // by Stefan Ostermann, of his fork of PDFjet, 19 October 2026).
    private BlendMode blendMode = null;

    /**
     * Sets the blend mode: how what is drawn is mixed with what is already on
     * the page. BlendMode.MULTIPLY draws like a highlighter pen. The blend
     * mode stays until a graphics state sets another one, so set
     * BlendMode.NORMAL to go back to painting over. A PDF/A-1 document takes
     * only BlendMode.NORMAL.
     *
     * @param blendMode the blend mode, or null for none
     * @return this GraphicsState object.
     */
    public GraphicsState setBlendMode(BlendMode blendMode) {
        this.blendMode = blendMode;
        return this;
    }

    /**
     * Returns the blend mode, or null if none was set.
     *
     * @return the blend mode.
     */
    public BlendMode getBlendMode() {
        return this.blendMode;
    }

    // The entries of the state's /ExtGState dictionary, as the page and an
    // imported page write them: the alphas with a dot whatever the locale, at
    // most two decimals and no exponent, then the blend mode if it has one.
    String entries() {
        String state = "/CA " +
                new String(FastFloat.toByteArray(CA), java.nio.charset.StandardCharsets.US_ASCII) +
                " /ca " +
                new String(FastFloat.toByteArray(ca), java.nio.charset.StandardCharsets.US_ASCII);
        if (blendMode != null) {
            state += " /BM /" + blendMode.name;
        }
        return state;
    }
}
