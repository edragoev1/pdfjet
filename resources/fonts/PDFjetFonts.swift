/**
 * PDFjetFonts.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The fonts PDFjet carries for every European language, in Latin, Greek
/// and Cyrillic, IBM Plex Sans and Serif and Cascadia Mono, the folders of
/// resources/fonts, each family in a folder of its name with its license:
/// the resources of this module, which SwiftPM copies as they are, so that
/// the one copy of them in the repository serves Java, C#, Go and Swift
/// alike. The product PDFjet depends on this module, so that these fonts,
/// about 11 MB, work with no step of one's own; the fonts of the other
/// scripts, about 52 MB, are in the module PDFjetFontsWorld, of
/// resources/fonts/world, the product an app adds when it draws in them. The
/// bundle has the files of the family folders side by side, as .process puts
/// them, and the license of IBM Plex once.
public enum PDFjetFonts {
    /// The resource bundle of the fonts.
    public static let bundle: Bundle = Bundle.module

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
