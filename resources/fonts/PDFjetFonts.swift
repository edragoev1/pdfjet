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
/// serves Java, C#, Go and Swift alike. PDFjet reads them through it.
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
        return Bundle.module.url(forResource: file.deletingPathExtension,
                withExtension: file.pathExtension, subdirectory: String(parts[0]))
    }
}
