/**
 * IBMPlexMath.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The IBM Plex Math fonts, which PDFjet does not carry from 9.1.7: each constant
/// is the path of a font file of the packages of PDFjet before 9.1.7,
/// which the Font constructor that takes a font path reads when the file is
/// there.
///
@available(*, deprecated, message: "PDFjet carries IBM Plex Sans and Serif, whose math signs serve business documents.")
public struct IBMPlexMath {
    /// IBM Plex Math Regular
    public static let Regular = "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf"
}
