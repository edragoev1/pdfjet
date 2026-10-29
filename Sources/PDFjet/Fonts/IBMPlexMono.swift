/**
 * IBMPlexMono.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Mono fonts, which PDFjet does not carry from 9.1.7: each constant
/// is the path of a font file of pdfjet-fonts,
/// https://github.com/edragoev1/pdfjet-fonts, which the Font constructor that
/// takes a font path reads when the file is there.
///
@available(*, deprecated, message: "PDFjet carries Cascadia Mono, which has all of Greek and of Cyrillic in every face, as IBM Plex Mono does not, and which every constant of CascadiaMono reads with nothing to download.")
public struct IBMPlexMono {
    /// IBM Plex Mono BoldItalic
    public static let BoldItalic       = "fonts/IBMPlexMono/IBMPlexMono-BoldItalic.ttf"
    /// IBM Plex Mono Bold
    public static let Bold             = "fonts/IBMPlexMono/IBMPlexMono-Bold.ttf"
    /// IBM Plex Mono ExtraLightItalic
    public static let ExtraLightItalic = "fonts/IBMPlexMono/IBMPlexMono-ExtraLightItalic.ttf"
    /// IBM Plex Mono ExtraLight
    public static let ExtraLight       = "fonts/IBMPlexMono/IBMPlexMono-ExtraLight.ttf"
    /// IBM Plex Mono Italic
    public static let Italic           = "fonts/IBMPlexMono/IBMPlexMono-Italic.ttf"
    /// IBM Plex Mono LightItalic
    public static let LightItalic      = "fonts/IBMPlexMono/IBMPlexMono-LightItalic.ttf"
    /// IBM Plex Mono Light
    public static let Light            = "fonts/IBMPlexMono/IBMPlexMono-Light.ttf"
    /// IBM Plex Mono MediumItalic
    public static let MediumItalic     = "fonts/IBMPlexMono/IBMPlexMono-MediumItalic.ttf"
    /// IBM Plex Mono Medium
    public static let Medium           = "fonts/IBMPlexMono/IBMPlexMono-Medium.ttf"
    /// IBM Plex Mono Regular
    public static let Regular          = "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf"
    /// IBM Plex Mono SemiBoldItalic
    public static let SemiBoldItalic   = "fonts/IBMPlexMono/IBMPlexMono-SemiBoldItalic.ttf"
    /// IBM Plex Mono SemiBold
    public static let SemiBold         = "fonts/IBMPlexMono/IBMPlexMono-SemiBold.ttf"
    /// IBM Plex Mono TextItalic
    public static let TextItalic       = "fonts/IBMPlexMono/IBMPlexMono-TextItalic.ttf"
    /// IBM Plex Mono Text
    public static let Text             = "fonts/IBMPlexMono/IBMPlexMono-Text.ttf"
    /// IBM Plex Mono ThinItalic
    public static let ThinItalic       = "fonts/IBMPlexMono/IBMPlexMono-ThinItalic.ttf"
    /// IBM Plex Mono Thin
    public static let Thin             = "fonts/IBMPlexMono/IBMPlexMono-Thin.ttf"
}
