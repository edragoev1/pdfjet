/**
 * CascadiaMono.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The Cascadia Mono fonts, Microsoft's monospace font, with all of Latin,
/// Greek and Cyrillic in every face, the italic ones too. Each constant is
/// the path of a font file, embedded as a subset of the glyphs a document
/// draws; pass it to the Font constructor that takes a font path. PDFjet
/// carries these fonts in itself: the path is read from the library when
/// there is no such file.
///
public struct CascadiaMono {
    /// Cascadia Mono Bold
    public static let Bold            = "fonts/CascadiaMono/CascadiaMono-Bold.ttf"
    /// Cascadia Mono BoldItalic
    public static let BoldItalic      = "fonts/CascadiaMono/CascadiaMono-BoldItalic.ttf"
    /// Cascadia Mono Italic
    public static let Italic          = "fonts/CascadiaMono/CascadiaMono-Italic.ttf"
    /// Cascadia Mono Light
    public static let Light           = "fonts/CascadiaMono/CascadiaMono-Light.ttf"
    /// Cascadia Mono LightItalic
    public static let LightItalic     = "fonts/CascadiaMono/CascadiaMono-LightItalic.ttf"
    /// Cascadia Mono Regular
    public static let Regular         = "fonts/CascadiaMono/CascadiaMono-Regular.ttf"
    /// Cascadia Mono SemiBold
    public static let SemiBold        = "fonts/CascadiaMono/CascadiaMono-SemiBold.ttf"
    /// Cascadia Mono SemiBoldItalic
    public static let SemiBoldItalic  = "fonts/CascadiaMono/CascadiaMono-SemiBoldItalic.ttf"
    /// Cascadia Mono SemiLight
    public static let SemiLight       = "fonts/CascadiaMono/CascadiaMono-SemiLight.ttf"
    /// Cascadia Mono SemiLightItalic
    public static let SemiLightItalic = "fonts/CascadiaMono/CascadiaMono-SemiLightItalic.ttf"
}
