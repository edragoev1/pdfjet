/**
 * Legal.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to specify PDF page with size **Legal**.
///
/// For more information about the page size classes - A3, A4, A5, B5, Executive,
/// Letter, Legal and Tabloid - see the Page class.
///
public class Legal {
    /// The legal page size in portrait orientation.
    public static let PORTRAIT: [Float] = [612.0, 1008.0]
    /// The legal page size in landscape orientation.
    public static let LANDSCAPE: [Float] = [1008.0, 612.0]
}
