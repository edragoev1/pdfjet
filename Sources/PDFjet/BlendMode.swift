/**
 * BlendMode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// The blend modes of a graphics state: how what is drawn is mixed with what
/// is already on the page. MULTIPLY, for example, draws like a highlighter pen
/// that leaves the text under it readable. See the GraphicsState class. The raw
/// value is the name of the blend mode in the PDF, without the slash.
///
public enum BlendMode: String {
    /// Paints over what is under it: the default.
    case NORMAL = "Normal"
    /// Multiplies the colors: always darker, like a highlighter.
    case MULTIPLY = "Multiply"
    /// Multiplies the inverse of the colors: always lighter.
    case SCREEN = "Screen"
    /// MULTIPLY or SCREEN, by the color under it.
    case OVERLAY = "Overlay"
    /// The darker of the two colors.
    case DARKEN = "Darken"
    /// The lighter of the two colors.
    case LIGHTEN = "Lighten"
    /// Brightens the color under it.
    case COLOR_DODGE = "ColorDodge"
    /// Darkens the color under it.
    case COLOR_BURN = "ColorBurn"
    /// MULTIPLY or SCREEN, by the color drawn.
    case HARD_LIGHT = "HardLight"
    /// A softer HARD_LIGHT.
    case SOFT_LIGHT = "SoftLight"
    /// The difference of the two colors.
    case DIFFERENCE = "Difference"
    /// Like DIFFERENCE, with less contrast.
    case EXCLUSION = "Exclusion"
    /// The hue of the color drawn, the rest of the color under it.
    case HUE = "Hue"
    /// The saturation of the color drawn, the rest of the color under it.
    case SATURATION = "Saturation"
    /// The hue and saturation of the color drawn.
    case COLOR = "Color"
    /// The luminosity of the color drawn.
    case LUMINOSITY = "Luminosity"
}
