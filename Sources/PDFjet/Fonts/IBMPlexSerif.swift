/**
 * IBMPlexSerif.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Serif fonts. Each constant is the path of a font file,
/// embedded as a subset of the glyphs a document draws; pass it to the Font
/// constructor that takes a font path. PDFjet carries these fonts in itself:
/// the path is read from the library when there is no such file.
///
public struct IBMPlexSerif {
    /// IBM Plex Serif Bold
    public static let Bold           = "fonts/IBMPlexSerif/IBMPlexSerif-Bold.otf"
    /// IBM Plex Serif BoldItalic
    public static let BoldItalic     = "fonts/IBMPlexSerif/IBMPlexSerif-BoldItalic.otf"
    /// IBM Plex Serif Italic
    public static let Italic         = "fonts/IBMPlexSerif/IBMPlexSerif-Italic.otf"
    /// IBM Plex Serif Light
    public static let Light          = "fonts/IBMPlexSerif/IBMPlexSerif-Light.otf"
    /// IBM Plex Serif LightItalic
    public static let LightItalic    = "fonts/IBMPlexSerif/IBMPlexSerif-LightItalic.otf"
    /// IBM Plex Serif Medium
    public static let Medium         = "fonts/IBMPlexSerif/IBMPlexSerif-Medium.otf"
    /// IBM Plex Serif MediumItalic
    public static let MediumItalic   = "fonts/IBMPlexSerif/IBMPlexSerif-MediumItalic.otf"
    /// IBM Plex Serif Regular
    public static let Regular        = "fonts/IBMPlexSerif/IBMPlexSerif-Regular.otf"
    /// IBM Plex Serif SemiBold
    public static let SemiBold       = "fonts/IBMPlexSerif/IBMPlexSerif-SemiBold.otf"
    /// IBM Plex Serif SemiBoldItalic
    public static let SemiBoldItalic = "fonts/IBMPlexSerif/IBMPlexSerif-SemiBoldItalic.otf"
    /// IBM Plex Serif Text
    public static let Text           = "fonts/IBMPlexSerif/IBMPlexSerif-Text.otf"
    /// IBM Plex Serif TextItalic
    public static let TextItalic     = "fonts/IBMPlexSerif/IBMPlexSerif-TextItalic.otf"
    /// IBM Plex Serif Thin
    public static let Thin           = "fonts/IBMPlexSerif/IBMPlexSerif-Thin.otf"
    /// IBM Plex Serif ThinItalic
    public static let ThinItalic     = "fonts/IBMPlexSerif/IBMPlexSerif-ThinItalic.otf"
}
