/**
 * BundledFonts.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The fonts PDFjet carries, in resources/fonts, each family in a folder of
/// its name, with its license: IBM Plex Sans and Serif, from Thin to Bold,
/// Source Serif 4 and Cascadia Mono, from Light to Bold, upright and italic;
/// IBM Plex Sans Arabic, Hebrew and Thai, Thin to Bold; IBM Plex Math; and IBM
/// Plex Sans JP, KR, SC and TC, Regular, SemiBold and Bold, as .otf. All are
/// under the SIL Open Font License 1.1. A path to one of them, such as
/// fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the constants of the font
/// structs, is read, when there is no such file, from the folder
/// resources/fonts of the working directory, or else from the resource bundle
/// of the product PDFjetFonts, and the text of an SVG image is drawn in them
/// unless a font is set.
///
/// The library does not depend on PDFjetFonts, so that an app carries the 66
/// MB of the fonts only when its developer adds that product (the owner, 29
/// October 2026). Swift has no code run on import, so PDFjet finds the bundle
/// by its name, which SwiftPM gives it, PackageName_TargetName: from the
/// command line, PDFjet_PDFjetFonts.bundle, or .resources on Linux with the
/// native build system, beside the executable; in an app that Xcode builds,
/// PDFjet_PDFjetFonts.bundle among the resources of the app, or of the
/// framework that links PDFjet. Those are the places the bundle's own
/// accessor, Bundle.module, looks in, so the bundle is found where the module
/// would find it, and the developer writes no code.
enum BundledFonts {
    // Where the fonts are on disk: the folder resources/fonts of the working
    // directory, as in the repository and the packages of the releases.
    static let FOLDER = "resources/fonts/"

    // The names SwiftPM gives the resource bundle of the target PDFjetFonts
    // of the package PDFjet, on Apple's systems and on Linux.
    private static let bundleNames = ["PDFjet_PDFjetFonts.bundle", "PDFjet_PDFjetFonts.resources"]

    // The resource bundle of the product PDFjetFonts, or nil when the program
    // was built without it, found once.
    static let bundle: Bundle? = findBundle()

    // Looks for the bundle of PDFjetFonts where SwiftPM and Xcode put it.
    private static func findBundle() -> Bundle? {
        var folders = [URL]()
        if let url = Bundle.main.resourceURL {
            folders.append(url)
        }
        folders.append(Bundle.main.bundleURL)
        if let url = Bundle.main.executableURL {
            folders.append(url.deletingLastPathComponent())
        }
        // The framework or the test bundle that has PDFjet, and the folder it
        // is in, as for the tests on macOS
        for other in [Bundle(for: BundleFinder.self)] + Bundle.allBundles + Bundle.allFrameworks {
            if let url = other.resourceURL {
                folders.append(url)
            }
            folders.append(other.bundleURL)
            folders.append(other.bundleURL.deletingLastPathComponent())
        }
        for folder in folders {
            for name in bundleNames {
                let url = folder.appendingPathComponent(name)
                if FileManager.default.fileExists(atPath: url.path), let bundle = Bundle(url: url) {
                    return bundle
                }
            }
        }
        return nil
    }

    // Says whether the fonts are there, in resources/fonts or in the bundle:
    // a font that is in neither, of a program that has them, is one PDFjet
    // does not carry.
    static func available(_ folder: String = FOLDER, _ bundle: Bundle? = BundledFonts.bundle) -> Bool {
        return bundle != nil || FileManager.default.fileExists(atPath: folder)
    }

    // What to do when a font PDFjet carries is not there: the line of the
    // product PDFjetFonts for Package.swift.
    static let ADD_PDFJET_FONTS = "add the product PDFjetFonts, which has them, to the dependencies of your target "
            + "in Package.swift, .product(name: \"PDFjetFonts\", package: \"pdfjet\"), or keep the folder "
            + "resources/fonts in the working directory"

    // Returns the name of a font under resources/fonts, Family/File, of a
    // path to it, such as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf: its
    // last two parts.
    static func nameOf(_ fontPath: String) -> String? {
        let path = fontPath.replacingOccurrences(of: "\\", with: "/")
        guard let file = path.range(of: "/", options: .backwards), file.lowerBound > path.startIndex else {
            return nil
        }
        if let family = path.range(of: "/", options: .backwards, range: path.startIndex..<file.lowerBound) {
            return String(path[family.upperBound...])
        }
        return path
    }

    // Returns the URL of the font of the path that PDFjet carries, or nil:
    // in the folder resources/fonts, or else in the bundle of PDFjetFonts.
    static func url(_ fontPath: String, _ folder: String = FOLDER,
            _ bundle: Bundle? = BundledFonts.bundle) -> URL? {
        guard let name = nameOf(fontPath), !name.contains(".."), !name.hasPrefix("/") else {
            return nil
        }
        let file = folder + name
        var isDirectory: ObjCBool = false
        if FileManager.default.fileExists(atPath: file, isDirectory: &isDirectory), !isDirectory.boolValue {
            return URL(fileURLWithPath: file)
        }
        // The bundle has the files of the family folders side by side, as
        // .process puts them, each font's name its own
        guard let bundle = bundle, let slash = name.firstIndex(of: "/") else {
            return nil
        }
        let base = String(name[name.index(after: slash)...]) as NSString
        return bundle.url(forResource: base.deletingPathExtension, withExtension: base.pathExtension)
    }

    static let SANS = 0
    static let SERIF = 1
    static let MONO = 2
    private static let families = ["IBMPlexSans", "SourceSerif4", "CascadiaMono"]
    // The CSS weights the faces of each family stand for, and their styles.
    // Cascadia Mono's strokes are heavier than IBM Plex Sans's at the same
    // weight: its SemiLight is as thick as Plex Sans Regular, so it stands
    // for 400, and its Regular for 500, as in PDFjet Forms (the owner, 29
    // October 2026); its SemiBold and Bold match Plex Sans's as they are.
    private static let weights: [[Int]] = [
        [100, 300, 400, 450, 500, 600, 700],
        [300, 400, 500, 600, 700],
        [300, 400, 500, 600, 700],
    ]
    private static let styles: [[String]] = [
        ["Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"],
        ["Light", "Regular", "Medium", "SemiBold", "Bold"],
        ["Light", "SemiLight", "Regular", "SemiBold", "Bold"],
    ]

    /// Returns the name of a face of an SVG family, Family/File: the weight
    /// CSS matches to the one asked for among those the family has, and
    /// italic or upright.
    static func fileName(_ family: Int, _ weight: Int, _ italic: Bool) -> String {
        let w = match(weights[family], weight)
        var style = "Regular"
        for (i, faceWeight) in weights[family].enumerated() where faceWeight == w {
            style = styles[family][i]
        }
        if italic {
            style = (style == "Regular") ? "Italic" : style + "Italic"
        }
        return families[family] + "/" + families[family] + "-" + style + ".ttf"
    }

    /// Returns the weight CSS matches to the one asked for, of the weights a
    /// family has in order (CSS Fonts 4, 5.2): that weight; from 400 to 500,
    /// the weights up to 500, then the lighter ones, then the heavier ones;
    /// below 400, the lighter ones, then the heavier ones; above 500, the
    /// heavier ones, then the lighter ones; each nearest first.
    static func match(_ weights: [Int], _ weight: Int) -> Int {
        var lighter = -1    // The nearest lighter weight
        var heavier = -1    // The nearest heavier weight
        var upTo500 = -1    // The nearest heavier weight up to 500
        for w in weights {
            if w == weight {
                return w
            } else if w < weight {
                lighter = w
            } else if heavier < 0 {
                heavier = w
            }
            if w > weight && w <= 500 && upTo500 < 0 {
                upTo500 = w
            }
        }
        if weight >= 400 && weight <= 500 && upTo500 >= 0 {
            return upTo500
        }
        if weight <= 500 {
            return (lighter >= 0) ? lighter : heavier
        }
        return (heavier >= 0) ? heavier : lighter
    }

    /// Returns the face for the document, read once for it, or nil when it is
    /// neither in resources/fonts nor in the bundle of PDFjetFonts.
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
            pdf.fail("Reading the font " + name + ": \(error)")
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
                "jetbrains mono", "ibm plex mono", "source code pro", "cascadia mono", "cascadia code"],
    ]
}

// A class of the module, whose bundle is that of the framework that has it.
private final class BundleFinder {}
// End of BundledFonts.swift
