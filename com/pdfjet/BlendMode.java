/*
 * BlendMode.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * The blend modes of a graphics state: how what is drawn is mixed with what
 * is already on the page. MULTIPLY, for example, draws like a highlighter pen
 * that leaves the text under it readable. See the GraphicsState class.
 */
public enum BlendMode {
    /** Paints over what is under it: the default */
    NORMAL("Normal"),
    /** Multiplies the colors: always darker, like a highlighter */
    MULTIPLY("Multiply"),
    /** Multiplies the inverse of the colors: always lighter */
    SCREEN("Screen"),
    /** MULTIPLY or SCREEN, by the color under it */
    OVERLAY("Overlay"),
    /** The darker of the two colors */
    DARKEN("Darken"),
    /** The lighter of the two colors */
    LIGHTEN("Lighten"),
    /** Brightens the color under it */
    COLOR_DODGE("ColorDodge"),
    /** Darkens the color under it */
    COLOR_BURN("ColorBurn"),
    /** MULTIPLY or SCREEN, by the color drawn */
    HARD_LIGHT("HardLight"),
    /** A softer HARD_LIGHT */
    SOFT_LIGHT("SoftLight"),
    /** The difference of the two colors */
    DIFFERENCE("Difference"),
    /** Like DIFFERENCE, with less contrast */
    EXCLUSION("Exclusion"),
    /** The hue of the color drawn, the rest of the color under it */
    HUE("Hue"),
    /** The saturation of the color drawn, the rest of the color under it */
    SATURATION("Saturation"),
    /** The hue and saturation of the color drawn */
    COLOR("Color"),
    /** The luminosity of the color drawn */
    LUMINOSITY("Luminosity");

    // The name of the blend mode in the PDF, without the slash.
    final String name;

    BlendMode(String name) {
        this.name = name;
    }
}
