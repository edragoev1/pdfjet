/*
 * BlendMode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// The blend modes of a graphics state: how what is drawn is mixed with what
/// is already on the page. MULTIPLY, for example, draws like a highlighter pen
/// that leaves the text under it readable. See the GraphicsState class.
/// </summary>
public enum BlendMode {
    /// <summary>Paints over what is under it: the default.</summary>
    NORMAL,
    /// <summary>Multiplies the colors: always darker, like a highlighter.</summary>
    MULTIPLY,
    /// <summary>Multiplies the inverse of the colors: always lighter.</summary>
    SCREEN,
    /// <summary>MULTIPLY or SCREEN, by the color under it.</summary>
    OVERLAY,
    /// <summary>The darker of the two colors.</summary>
    DARKEN,
    /// <summary>The lighter of the two colors.</summary>
    LIGHTEN,
    /// <summary>Brightens the color under it.</summary>
    COLOR_DODGE,
    /// <summary>Darkens the color under it.</summary>
    COLOR_BURN,
    /// <summary>MULTIPLY or SCREEN, by the color drawn.</summary>
    HARD_LIGHT,
    /// <summary>A softer HARD_LIGHT.</summary>
    SOFT_LIGHT,
    /// <summary>The difference of the two colors.</summary>
    DIFFERENCE,
    /// <summary>Like DIFFERENCE, with less contrast.</summary>
    EXCLUSION,
    /// <summary>The hue of the color drawn, the rest of the color under it.</summary>
    HUE,
    /// <summary>The saturation of the color drawn, the rest of the color under it.</summary>
    SATURATION,
    /// <summary>The hue and saturation of the color drawn.</summary>
    COLOR,
    /// <summary>The luminosity of the color drawn.</summary>
    LUMINOSITY
}

internal static class BlendModeExtensions {
    // The name of the blend mode in the PDF, without the slash.
    internal static String ToPDFName(this BlendMode blendMode) {
        switch (blendMode) {
            case BlendMode.NORMAL: return "Normal";
            case BlendMode.MULTIPLY: return "Multiply";
            case BlendMode.SCREEN: return "Screen";
            case BlendMode.OVERLAY: return "Overlay";
            case BlendMode.DARKEN: return "Darken";
            case BlendMode.LIGHTEN: return "Lighten";
            case BlendMode.COLOR_DODGE: return "ColorDodge";
            case BlendMode.COLOR_BURN: return "ColorBurn";
            case BlendMode.HARD_LIGHT: return "HardLight";
            case BlendMode.SOFT_LIGHT: return "SoftLight";
            case BlendMode.DIFFERENCE: return "Difference";
            case BlendMode.EXCLUSION: return "Exclusion";
            case BlendMode.HUE: return "Hue";
            case BlendMode.SATURATION: return "Saturation";
            case BlendMode.COLOR: return "Color";
            case BlendMode.LUMINOSITY: return "Luminosity";
            default: throw new ArgumentException("Invalid blend mode: " + blendMode);
        }
    }
}
}   // End of namespace PDFjet.NET
