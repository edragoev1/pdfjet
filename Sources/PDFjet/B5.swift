/**
 * B5.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to specify PDF page with size **B5**, the ISO 216 B5 of 176 by 250 mm.
/// For more information about the page size classes - A3, A4, A5, B5, JISB5, Executive,
/// Letter, Legal and Tabloid - see the Page class.
///
public class B5 {
    /// The B5 page size in portrait orientation.
    public static let PORTRAIT = PageSize(499.0, 709.0)
    /// The B5 page size in landscape orientation.
    public static let LANDSCAPE = PageSize(709.0, 499.0)
}
