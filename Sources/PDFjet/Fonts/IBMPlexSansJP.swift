/**
 * IBMPlexSansJP.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans JP fonts. Each constant is the path of a font file,
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
public struct IBMPlexSansJP {
    /// IBM Plex Sans JP Bold
    public static let Bold       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.otf"

    ///
    /// IBM Plex Sans JP ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansJP/IBMPlexSansJP-ExtraLight.ttf"

    ///
    /// IBM Plex Sans JP Light, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Light      = "fonts/IBMPlexSansJP/IBMPlexSansJP-Light.ttf"

    ///
    /// IBM Plex Sans JP Medium, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Medium     = "fonts/IBMPlexSansJP/IBMPlexSansJP-Medium.ttf"

    /// IBM Plex Sans JP Regular
    public static let Regular    = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf"
    /// IBM Plex Sans JP SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansJP/IBMPlexSansJP-SemiBold.otf"

    ///
    /// IBM Plex Sans JP Text, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Text       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Text.ttf"

    ///
    /// IBM Plex Sans JP Thin, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Thin       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf"
}
