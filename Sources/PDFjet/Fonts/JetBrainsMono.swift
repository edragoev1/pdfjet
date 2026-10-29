/**
 * JetBrainsMono.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The JetBrains Mono fonts, which PDFjet does not carry from 9.1.7: each constant
/// is the path of a font file of the packages of PDFjet before 9.1.7,
/// which the Font constructor that takes a font path reads when the file is
/// there.
///
@available(*, deprecated, message: "PDFjet carries Cascadia Mono, which has all of Greek and of Cyrillic in every face, as JetBrains Mono does not, and which every constant of CascadiaMono reads with nothing to download.")
public struct JetBrainsMono {
    /// JetBrains Mono Bold
    public static let Bold             = "fonts/JetBrainsMono/JetBrainsMono-Bold.ttf"
    /// JetBrains Mono BoldItalic
    public static let BoldItalic       = "fonts/JetBrainsMono/JetBrainsMono-BoldItalic.ttf"
    /// JetBrains Mono ExtraLight
    public static let ExtraLight       = "fonts/JetBrainsMono/JetBrainsMono-ExtraLight.ttf"
    /// JetBrains Mono ExtraLightItalic
    public static let ExtraLightItalic = "fonts/JetBrainsMono/JetBrainsMono-ExtraLightItalic.ttf"
    /// JetBrains Mono Italic
    public static let Italic           = "fonts/JetBrainsMono/JetBrainsMono-Italic.ttf"
    /// JetBrains Mono Light
    public static let Light            = "fonts/JetBrainsMono/JetBrainsMono-Light.ttf"
    /// JetBrains Mono LightItalic
    public static let LightItalic      = "fonts/JetBrainsMono/JetBrainsMono-LightItalic.ttf"
    /// JetBrains Mono Medium
    public static let Medium           = "fonts/JetBrainsMono/JetBrainsMono-Medium.ttf"
    /// JetBrains Mono MediumItalic
    public static let MediumItalic     = "fonts/JetBrainsMono/JetBrainsMono-MediumItalic.ttf"
    /// JetBrains Mono Regular
    public static let Regular          = "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf"
    /// JetBrains Mono SemiBold
    public static let SemiBold         = "fonts/JetBrainsMono/JetBrainsMono-SemiBold.ttf"
    /// JetBrains Mono SemiBoldItalic
    public static let SemiBoldItalic   = "fonts/JetBrainsMono/JetBrainsMono-SemiBoldItalic.ttf"
}
