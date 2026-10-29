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
/// constructor that takes a font path. PDFjet carries these fonts in itself:
/// the path is read from the library when there is no such file.
///
public struct IBMPlexSansJP {
    /// IBM Plex Sans JP Bold
    public static let Bold       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.otf"

    ///
    /// IBM Plex Sans JP ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansJP/IBMPlexSansJP-ExtraLight.ttf"

    ///
    /// IBM Plex Sans JP Light, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Light      = "fonts/IBMPlexSansJP/IBMPlexSansJP-Light.ttf"

    ///
    /// IBM Plex Sans JP Medium, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Medium     = "fonts/IBMPlexSansJP/IBMPlexSansJP-Medium.ttf"

    /// IBM Plex Sans JP Regular
    public static let Regular    = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf"
    /// IBM Plex Sans JP SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansJP/IBMPlexSansJP-SemiBold.otf"

    ///
    /// IBM Plex Sans JP Text, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Text       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Text.ttf"

    ///
    /// IBM Plex Sans JP Thin, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Thin       = "fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf"
}
