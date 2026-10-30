/**
 * IBMPlexSansTC.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans TC fonts. Each constant is the path of a font file,
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
public struct IBMPlexSansTC {
    /// IBM Plex Sans TC Bold
    public static let Bold       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Bold.otf"

    ///
    /// IBM Plex Sans TC ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansTC/IBMPlexSansTC-ExtraLight.ttf"

    ///
    /// IBM Plex Sans TC Light, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Light      = "fonts/IBMPlexSansTC/IBMPlexSansTC-Light.ttf"

    ///
    /// IBM Plex Sans TC Medium, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Medium     = "fonts/IBMPlexSansTC/IBMPlexSansTC-Medium.ttf"

    /// IBM Plex Sans TC Regular
    public static let Regular    = "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf"
    /// IBM Plex Sans TC SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansTC/IBMPlexSansTC-SemiBold.otf"

    ///
    /// IBM Plex Sans TC Text, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Text       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Text.ttf"

    ///
    /// IBM Plex Sans TC Thin, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Thin       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Thin.ttf"
}
