/**
 * PDFjetFontsCJK.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The CJK fonts PDFjet carries, IBM Plex Sans JP, KR, SC and TC, Regular,
/// SemiBold and Bold, the folders of resources/fonts/cjk, each family in a
/// folder of its name with its license: the resources of this module, which
/// SwiftPM copies as they are, so that the one copy of them in the repository
/// serves Java, C#, Go and Swift alike. An app that draws in them, about 48
/// MB, adds the product PDFjetFontsCJK to the dependencies of its target,
/// .product(name: "PDFjetFontsCJK", package: "pdfjet"); no code is needed,
/// as PDFjet, which does not depend on this module, finds its bundle,
/// PDFjet_PDFjetFontsCJK, by its name. The bundle has the files of the family
/// folders side by side, as .process puts them, and the license of IBM Plex
/// once.
public enum PDFjetFontsCJK {
    /// The resource bundle of the CJK fonts.
    public static let bundle: Bundle = Bundle.module
}
