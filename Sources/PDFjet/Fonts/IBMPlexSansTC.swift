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
/// constructor that takes a font path. PDFjet carries these fonts in itself:
/// the path is read from the library when there is no such file.
///
public struct IBMPlexSansTC {
    /// IBM Plex Sans TC Bold
    public static let Bold       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Bold.otf"

    ///
    /// IBM Plex Sans TC ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansTC/IBMPlexSansTC-ExtraLight.ttf"

    ///
    /// IBM Plex Sans TC Light, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Light      = "fonts/IBMPlexSansTC/IBMPlexSansTC-Light.ttf"

    ///
    /// IBM Plex Sans TC Medium, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Medium     = "fonts/IBMPlexSansTC/IBMPlexSansTC-Medium.ttf"

    /// IBM Plex Sans TC Regular
    public static let Regular    = "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf"
    /// IBM Plex Sans TC SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansTC/IBMPlexSansTC-SemiBold.otf"

    ///
    /// IBM Plex Sans TC Text, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Text       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Text.ttf"

    ///
    /// IBM Plex Sans TC Thin, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Thin       = "fonts/IBMPlexSansTC/IBMPlexSansTC-Thin.ttf"
}
