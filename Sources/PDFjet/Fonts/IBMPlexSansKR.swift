/**
 * IBMPlexSansKR.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans KR fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. These fonts are not in the product
/// PDFjet: they are in the product PDFjetFontsCJK, which an app that draws in
/// them adds to the dependencies of its target in Package.swift,
/// `.product(name: "PDFjetFontsCJK", package: "pdfjet")`. The path is read
/// from the file of that path, else from the folder resources/fonts/cjk of
/// the working directory, else from the bundle of PDFjetFontsCJK, and a font
/// found in none says what to add.
///
public struct IBMPlexSansKR {
    /// IBM Plex Sans KR Bold
    public static let Bold       = "fonts/IBMPlexSansKR/IBMPlexSansKR-Bold.otf"

    ///
    /// IBM Plex Sans KR ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansKR/IBMPlexSansKR-ExtraLight.ttf"

    ///
    /// IBM Plex Sans KR Light, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Light      = "fonts/IBMPlexSansKR/IBMPlexSansKR-Light.ttf"

    ///
    /// IBM Plex Sans KR Medium, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Medium     = "fonts/IBMPlexSansKR/IBMPlexSansKR-Medium.ttf"

    /// IBM Plex Sans KR Regular
    public static let Regular    = "fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf"
    /// IBM Plex Sans KR SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansKR/IBMPlexSansKR-SemiBold.otf"

    ///
    /// IBM Plex Sans KR Text, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Text       = "fonts/IBMPlexSansKR/IBMPlexSansKR-Text.ttf"

    ///
    /// IBM Plex Sans KR Thin, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Thin       = "fonts/IBMPlexSansKR/IBMPlexSansKR-Thin.ttf"
}
