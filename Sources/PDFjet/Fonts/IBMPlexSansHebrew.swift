/**
 * IBMPlexSansHebrew.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans Hebrew fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. These fonts are not in the product
/// PDFjet: they are in the product PDFjetFontsWorld, with PDFjet's fonts for
/// Chinese, Japanese, Korean, Arabic, Hebrew and Thai, which an app that
/// draws in them adds to the dependencies of its target in Package.swift,
/// `.product(name: "PDFjetFontsWorld", package: "pdfjet")`. The path is read
/// from the file of that path, else from the folder resources/fonts/world of
/// the working directory, else from the bundle of PDFjetFontsWorld, and a
/// font found in none says what to add.
///
public struct IBMPlexSansHebrew {
    /// IBM Plex Sans Hebrew Bold
    public static let Bold       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Bold.ttf"

    ///
    /// IBM Plex Sans Hebrew ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-ExtraLight.ttf"

    /// IBM Plex Sans Hebrew Light
    public static let Light      = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Light.ttf"
    /// IBM Plex Sans Hebrew Medium
    public static let Medium     = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Medium.ttf"
    /// IBM Plex Sans Hebrew Regular
    public static let Regular    = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"
    /// IBM Plex Sans Hebrew SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-SemiBold.ttf"
    /// IBM Plex Sans Hebrew Text
    public static let Text       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Text.ttf"
    /// IBM Plex Sans Hebrew Thin
    public static let Thin       = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Thin.ttf"
}
