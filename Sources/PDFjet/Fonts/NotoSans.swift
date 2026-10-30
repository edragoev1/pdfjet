/**
 * NotoSans.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The Noto Sans fonts, which PDFjet does not carry from 9.1.7: each
/// constant is the path of a font file of the packages of PDFjet before 9.1.7,
/// which the Font constructor that takes a font path reads when the file is
/// there.
///
@available(*, deprecated, message: "PDFjet carries IBM Plex Sans, which every constant of IBMPlexSans reads with nothing to download.")
public struct NotoSans {
    /// Noto Sans Black
    public static let Black            = "fonts/NotoSans/NotoSans-Black.ttf"
    /// Noto Sans BlackItalic
    public static let BlackItalic      = "fonts/NotoSans/NotoSans-BlackItalic.ttf"
    /// Noto Sans Bold
    public static let Bold             = "fonts/NotoSans/NotoSans-Bold.ttf"
    /// Noto Sans BoldItalic
    public static let BoldItalic       = "fonts/NotoSans/NotoSans-BoldItalic.ttf"
    /// Noto Sans ExtraBold
    public static let ExtraBold        = "fonts/NotoSans/NotoSans-ExtraBold.ttf"
    /// Noto Sans ExtraBoldItalic
    public static let ExtraBoldItalic  = "fonts/NotoSans/NotoSans-ExtraBoldItalic.ttf"
    /// Noto Sans ExtraLight
    public static let ExtraLight       = "fonts/NotoSans/NotoSans-ExtraLight.ttf"
    /// Noto Sans ExtraLightItalic
    public static let ExtraLightItalic = "fonts/NotoSans/NotoSans-ExtraLightItalic.ttf"
    /// Noto Sans Italic
    public static let Italic           = "fonts/NotoSans/NotoSans-Italic.ttf"
    /// Noto Sans Light
    public static let Light            = "fonts/NotoSans/NotoSans-Light.ttf"
    /// Noto Sans LightItalic
    public static let LightItalic      = "fonts/NotoSans/NotoSans-LightItalic.ttf"
    /// Noto Sans Medium
    public static let Medium           = "fonts/NotoSans/NotoSans-Medium.ttf"
    /// Noto Sans MediumItalic
    public static let MediumItalic     = "fonts/NotoSans/NotoSans-MediumItalic.ttf"
    /// Noto Sans Regular
    public static let Regular          = "fonts/NotoSans/NotoSans-Regular.ttf"
    /// Noto Sans SemiBold
    public static let SemiBold         = "fonts/NotoSans/NotoSans-SemiBold.ttf"
    /// Noto Sans SemiBoldItalic
    public static let SemiBoldItalic   = "fonts/NotoSans/NotoSans-SemiBoldItalic.ttf"
    /// Noto Sans Thin
    public static let Thin             = "fonts/NotoSans/NotoSans-Thin.ttf"
    /// Noto Sans ThinItalic
    public static let ThinItalic       = "fonts/NotoSans/NotoSans-ThinItalic.ttf"
}
