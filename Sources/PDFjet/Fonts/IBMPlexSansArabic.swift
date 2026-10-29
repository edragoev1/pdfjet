/**
 * IBMPlexSansArabic.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans Arabic fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. PDFjet carries these fonts in itself:
/// the path is read from the library when there is no such file.
///
public struct IBMPlexSansArabic {
    /// IBM Plex Sans Arabic Bold
    public static let Bold       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Bold.ttf"

    ///
    /// IBM Plex Sans Arabic ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf"

    /// IBM Plex Sans Arabic Light
    public static let Light      = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Light.ttf"
    /// IBM Plex Sans Arabic Medium
    public static let Medium     = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Medium.ttf"
    /// IBM Plex Sans Arabic Regular
    public static let Regular    = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"
    /// IBM Plex Sans Arabic SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-SemiBold.ttf"
    /// IBM Plex Sans Arabic Text
    public static let Text       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Text.ttf"
    /// IBM Plex Sans Arabic Thin
    public static let Thin       = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Thin.ttf"
}
