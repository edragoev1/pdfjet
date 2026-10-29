/**
 * BundledFonts.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The fonts PDFjet carries in itself, those of PDFjet Forms: IBM Plex Sans,
/// Source Serif 4 and JetBrains Mono, each regular, semibold and bold, upright
/// and italic, under the SIL Open Font License 1.1. An SVG image draws its
/// text in them unless a font is set: embedded, as PDF/A and PDF/UA ask, where
/// the core fonts are not. They are the resources of this module, in its
/// bundledfonts folder, copies of those of src/bundledfonts.
enum BundledFonts {
    static let SANS = 0
    static let SERIF = 1
    static let MONO = 2
    private static let families = ["IBMPlexSans", "SourceSerif4", "JetBrainsMono"]

    /// Returns the name of the file of a face: a weight below 600 is regular,
    /// from 600 to 699 semibold, and 700 or more bold.
    static func fileName(_ family: Int, _ weight: Int, _ italic: Bool) -> String {
        var style = (weight >= 700) ? "Bold" : (weight >= 600) ? "SemiBold" : ""
        if italic {
            style += "Italic"
        }
        return families[family] + "-" + (style.isEmpty ? "Regular" : style) + ".ttf"
    }

    /// Returns the URL of a file of the fonts, or nil when the module was
    /// built without it.
    static func url(_ name: String) -> URL? {
        let base = (name as NSString).deletingPathExtension
        let ext = (name as NSString).pathExtension
        return Bundle.module.url(forResource: base, withExtension: ext, subdirectory: "bundledfonts")
                ?? Bundle.module.url(forResource: base, withExtension: ext)
    }

    /// Returns the face for the document, read once for it, or nil when the
    /// library was built without the fonts.
    static func get(_ pdf: PDF, _ family: Int, _ weight: Int, _ italic: Bool) -> Font? {
        let name = fileName(family, weight, italic)
        if let font = pdf.bundledFonts[name] {
            return font
        }
        guard let url = url(name), let stream = InputStream(url: url) else {
            return nil
        }
        defer { stream.close() }
        let font: Font
        do {
            font = try Font(pdf, stream)
        } catch {
            // A file of the module that cannot be read is a library built
            // wrong; it is drawn in Helvetica, and the document says why
            pdf.fail("Reading the bundled font " + name + ": \(error)")
            return nil
        }
        pdf.bundledFonts[name] = font
        return font
    }

    /// Returns the family of a font-family list of an SVG file: the first of
    /// its names that PDFjet knows, as serif, monospace or sans-serif, and
    /// sans-serif for none.
    static func familyOf(_ fontFamily: String) -> Int {
        for part in fontFamily.unicodeScalars.split(separator: ",", omittingEmptySubsequences: false) {
            var name = Array(SVGState.toLower(SVGState.trimSpace(SVGState.string(part))).unicodeScalars)
            if name.count >= 2 && (name[0] == "\"" || name[0] == "'") && name[name.count - 1] == name[0] {
                name = Array(SVGState.trimSpace(SVGState.string(name[1..<(name.count - 1)])).unicodeScalars)
            }
            for (i, known) in names.enumerated() where known.contains(where: { $0.unicodeScalars.elementsEqual(name) }) {
                return i
            }
        }
        return SANS
    }

    // The names of the fonts of each family, in lower case: the generic one,
    // the core fonts and those that systems and charts name most.
    private static let names: [[String]] = [
        ["sans-serif", "helvetica", "arial", "system-ui", "-apple-system", "blinkmacsystemfont",
                "segoe ui", "roboto", "verdana", "tahoma", "trebuchet ms", "open sans", "noto sans",
                "liberation sans", "dejavu sans", "ibm plex sans", "inter"],
        ["serif", "times", "times new roman", "georgia", "garamond", "cambria", "palatino",
                "book antiqua", "liberation serif", "dejavu serif", "noto serif", "source serif 4",
                "source serif pro"],
        ["monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
                "sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
                "jetbrains mono", "ibm plex mono", "source code pro"],
    ]
}   // End of BundledFonts.swift
