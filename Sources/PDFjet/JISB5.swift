/**
 * JISB5.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to specify PDF page with size **JIS B5**, the Japanese B5 of 182 by 257 mm.
/// For more information about the page size classes - A3, A4, A5, JISB5, Executive,
/// Letter, Legal and Tabloid - see the Page class.
///
public class JISB5 {
    /// The JISB5 page size in portrait orientation.
    public static let PORTRAIT = PageSize(516.0, 729.0)
    /// The JISB5 page size in landscape orientation.
    public static let LANDSCAPE = PageSize(729.0, 516.0)
}
