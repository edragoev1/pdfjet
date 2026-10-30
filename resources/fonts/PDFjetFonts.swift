/**
 * PDFjetFonts.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The fonts PDFjet carries, the folders of resources/fonts, each family in a
/// folder of its name with its license: the resources of this module, which
/// SwiftPM copies as they are, so that the one copy of them in the repository
/// serves Java, C#, Go and Swift alike. An app that wants the fonts built in,
/// about 63 MB, adds the product PDFjetFonts to the dependencies of its
/// target, .product(name: "PDFjetFonts", package: "pdfjet"); no code is
/// needed, as PDFjet, which does not depend on this module, finds its bundle,
/// PDFjet_PDFjetFonts, by its name. The bundle has the files of the family
/// folders side by side, as .process puts them, and the license of IBM Plex
/// once.
public enum PDFjetFonts {
    /// Returns the URL of a font PDFjet carries, by its name under
    /// resources/fonts, Family/File, such as IBMPlexSans/IBMPlexSans-Regular.ttf,
    /// or nil when there is no such font.
    public static func url(_ name: String) -> URL? {
        let parts = name.split(separator: "/", omittingEmptySubsequences: false)
        guard parts.count == 2, !parts[0].isEmpty, !parts[1].isEmpty,
                !name.contains(".."), !name.contains("\\") else {
            return nil
        }
        let file = String(parts[1]) as NSString
        return Bundle.module.url(forResource: file.deletingPathExtension, withExtension: file.pathExtension)
    }
}
