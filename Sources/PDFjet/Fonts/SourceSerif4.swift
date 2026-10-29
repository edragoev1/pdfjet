/**
 * SourceSerif4.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The Source Serif 4 fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. PDFjet carries these fonts in itself:
/// the path is read from the library when there is no such file.
///
public struct SourceSerif4 {
    ///
    /// Source Serif 4 Black, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let Black            = "fonts/SourceSerif4/SourceSerif4-Black.ttf"

    ///
    /// Source Serif 4 BlackItalic, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let BlackItalic      = "fonts/SourceSerif4/SourceSerif4-BlackItalic.ttf"

    /// Source Serif 4 Bold
    public static let Bold             = "fonts/SourceSerif4/SourceSerif4-Bold.ttf"
    /// Source Serif 4 BoldItalic
    public static let BoldItalic       = "fonts/SourceSerif4/SourceSerif4-BoldItalic.ttf"

    ///
    /// Source Serif 4 ExtraLight, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLight       = "fonts/SourceSerif4/SourceSerif4-ExtraLight.ttf"

    ///
    /// Source Serif 4 ExtraLightItalic, which PDFjet does not carry
    /// from 9.1.7: a font file of its packages before, read when the file is there.
    ///
    @available(*, deprecated, message: "PDFjet carries the other weights of the family.")
    public static let ExtraLightItalic = "fonts/SourceSerif4/SourceSerif4-ExtraLightItalic.ttf"

    /// Source Serif 4 Italic
    public static let Italic           = "fonts/SourceSerif4/SourceSerif4-Italic.ttf"
    /// Source Serif 4 Light
    public static let Light            = "fonts/SourceSerif4/SourceSerif4-Light.ttf"
    /// Source Serif 4 LightItalic
    public static let LightItalic      = "fonts/SourceSerif4/SourceSerif4-LightItalic.ttf"
    /// Source Serif 4 Medium
    public static let Medium           = "fonts/SourceSerif4/SourceSerif4-Medium.ttf"
    /// Source Serif 4 MediumItalic
    public static let MediumItalic     = "fonts/SourceSerif4/SourceSerif4-MediumItalic.ttf"
    /// Source Serif 4 Regular
    public static let Regular          = "fonts/SourceSerif4/SourceSerif4-Regular.ttf"
    /// Source Serif 4 SemiBold
    public static let SemiBold         = "fonts/SourceSerif4/SourceSerif4-SemiBold.ttf"
    /// Source Serif 4 SemiBoldItalic
    public static let SemiBoldItalic   = "fonts/SourceSerif4/SourceSerif4-SemiBoldItalic.ttf"
}
