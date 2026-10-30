/**
 * IBMPlexSansSC.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans SC fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. These fonts are not in the product
/// PDFjet: they are in the product PDFjetFontsCJK, which an app that draws in
/// them adds to the dependencies of its target in Package.swift,
/// `.product(name: "PDFjetFontsCJK", package: "pdfjet")`. The path is read
/// from the file of that path, else from the folder resources/fonts/cjk of
/// the working directory, else from the bundle of PDFjetFontsCJK, and a font
/// found in none says what to add.
///
public struct IBMPlexSansSC {
    /// IBM Plex Sans SC Bold
    public static let Bold       = "fonts/IBMPlexSansSC/IBMPlexSansSC-Bold.otf"

    ///
    /// IBM Plex Sans SC ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansSC/IBMPlexSansSC-ExtraLight.ttf"

    ///
    /// IBM Plex Sans SC Light, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Light      = "fonts/IBMPlexSansSC/IBMPlexSansSC-Light.ttf"

    ///
    /// IBM Plex Sans SC Medium, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Medium     = "fonts/IBMPlexSansSC/IBMPlexSansSC-Medium.ttf"

    /// IBM Plex Sans SC Regular
    public static let Regular    = "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.otf"
    /// IBM Plex Sans SC SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansSC/IBMPlexSansSC-SemiBold.otf"

    ///
    /// IBM Plex Sans SC Text, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Text       = "fonts/IBMPlexSansSC/IBMPlexSansSC-Text.ttf"

    ///
    /// IBM Plex Sans SC Thin, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Thin       = "fonts/IBMPlexSansSC/IBMPlexSansSC-Thin.ttf"
}
