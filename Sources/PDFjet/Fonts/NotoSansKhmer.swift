/**
 * NotoSansKhmer.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The Noto Sans Khmer fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. These fonts are not in the product
/// PDFjet: they are in the product PDFjetFontsWorld, with PDFjet's fonts for
/// Chinese, Japanese, Korean, Arabic, Hebrew, Thai and others, which an app that
/// draws in them adds to the dependencies of its target in Package.swift,
/// `.product(name: "PDFjetFontsWorld", package: "pdfjet")`. The path is read
/// from the file of that path, else from the folder resources/fonts/world of
/// the working directory, else from the bundle of PDFjetFontsWorld, and a
/// font found in none says what to add.
///
/// PDFjet does not shape Khmer: these fonts are for TextBlock and TextLine,
/// which draw the text as Core Text shapes it with the same font file, for
/// Khmer. Regular and Bold, from Noto's release, unmodified.
///
public struct NotoSansKhmer {
    /// Noto Sans Khmer Bold
    public static let Bold       = "fonts/NotoSansKhmer/NotoSansKhmer-Bold.otf"

    /// Noto Sans Khmer Regular
    public static let Regular    = "fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf"
}
