/**
 * IBMPlexSansDevanagari.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Sans Devanagari fonts. Each constant is the path of a font file,
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
/// PDFjet does not shape Devanagari: these fonts are for Page.drawGlyphs,
/// Core Text shaping the text with the same font file. Complete for Hindi,
/// Marathi, Nepali and Sanskrit (without the Vedic accents).
///
public struct IBMPlexSansDevanagari {
    /// IBM Plex Sans Devanagari Bold
    public static let Bold       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Bold.otf"


    /// IBM Plex Sans Devanagari Light
    public static let Light      = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Light.otf"
    /// IBM Plex Sans Devanagari Medium
    public static let Medium     = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Medium.otf"
    /// IBM Plex Sans Devanagari Regular
    public static let Regular    = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
    /// IBM Plex Sans Devanagari SemiBold
    public static let SemiBold   = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-SemiBold.otf"
    /// IBM Plex Sans Devanagari Text
    public static let Text       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Text.otf"
    /// IBM Plex Sans Devanagari Thin
    public static let Thin       = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Thin.otf"
}
