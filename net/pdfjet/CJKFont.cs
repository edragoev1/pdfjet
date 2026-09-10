/*
 * CJKFont.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>
/// Used to select Chinese, Japanese or Korean fonts.
/// </summary>
public enum CJKFont {
    /// <summary>Adobe Ming Std Light, a Chinese (Traditional) font.</summary>
    ADOBE_MING_STD_LIGHT,       // Chinese (Traditional) font
    /// <summary>ST Heiti SC Light, a Chinese (Simplified) font.</summary>
    ST_HEITI_SC_LIGHT,          // Chinese (Simplified) font
    /// <summary>Kozuka Mincho Pro VI Regular, a Japanese font.</summary>
    KOZ_MIN_PRO_VI_REGULAR,     // Japanese font
    /// <summary>Adobe Myungjo Std Medium, a Korean font.</summary>
    ADOBE_MYUNGJO_STD_MEDIUM,   // Korean font
}   // End of CJKFont.cs
}   // End of namespace PDFjet.NET
