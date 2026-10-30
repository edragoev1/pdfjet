/**
 * IBMPlexSans.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. PDFjet carries these fonts in itself:
/// the path is read from the library when there is no such file.
///
public struct IBMPlexSans {
    /// IBM Plex Sans BoldItalic
    public static let BoldItalic       = "fonts/IBMPlexSans/IBMPlexSans-BoldItalic.ttf"
    /// IBM Plex Sans Bold
    public static let Bold             = "fonts/IBMPlexSans/IBMPlexSans-Bold.ttf"

    ///
    /// IBM Plex Sans ExtraLightItalic, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLightItalic = "fonts/IBMPlexSans/IBMPlexSans-ExtraLightItalic.ttf"

    ///
    /// IBM Plex Sans ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight       = "fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf"

    /// IBM Plex Sans Italic
    public static let Italic           = "fonts/IBMPlexSans/IBMPlexSans-Italic.ttf"
    /// IBM Plex Sans LightItalic
    public static let LightItalic      = "fonts/IBMPlexSans/IBMPlexSans-LightItalic.ttf"
    /// IBM Plex Sans Light
    public static let Light            = "fonts/IBMPlexSans/IBMPlexSans-Light.ttf"
    /// IBM Plex Sans MediumItalic
    public static let MediumItalic     = "fonts/IBMPlexSans/IBMPlexSans-MediumItalic.ttf"
    /// IBM Plex Sans Medium
    public static let Medium           = "fonts/IBMPlexSans/IBMPlexSans-Medium.ttf"
    /// IBM Plex Sans Regular
    public static let Regular          = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"
    /// IBM Plex Sans SemiBoldItalic
    public static let SemiBoldItalic   = "fonts/IBMPlexSans/IBMPlexSans-SemiBoldItalic.ttf"
    /// IBM Plex Sans SemiBold
    public static let SemiBold         = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf"
    /// IBM Plex Sans TextItalic
    public static let TextItalic       = "fonts/IBMPlexSans/IBMPlexSans-TextItalic.ttf"
    /// IBM Plex Sans Text
    public static let Text             = "fonts/IBMPlexSans/IBMPlexSans-Text.ttf"
    /// IBM Plex Sans ThinItalic
    public static let ThinItalic       = "fonts/IBMPlexSans/IBMPlexSans-ThinItalic.ttf"
    /// IBM Plex Sans Thin
    public static let Thin             = "fonts/IBMPlexSans/IBMPlexSans-Thin.ttf"
}
