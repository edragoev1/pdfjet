/**
 * BundledFonts.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjetFonts

/// The fonts PDFjet carries, in resources/fonts, each family in a folder of
/// its name, with its license: IBM Plex Sans and Serif, from Thin to Bold,
/// and Cascadia Mono, from Light to Bold, upright and italic;
/// IBM Plex Sans Arabic, Hebrew and Thai, Thin to Bold; IBM Plex Math; and,
/// in resources/fonts/cjk, IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold
/// and Bold, as .otf. All are under the SIL Open Font License 1.1. A path to
/// one of them, such as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the
/// constants of the font structs, is read, when there is no such file, from
/// the folder resources/fonts of the working directory, or else from the
/// resource bundle of the module PDFjetFonts, which PDFjet depends on, or of
/// the product PDFjetFontsCJK for the CJK fonts, and the text of an SVG image
/// is drawn in them unless a font is set.
///
/// The library does not depend on PDFjetFontsCJK, so that an app carries the
/// 48 MB of the CJK fonts only when its developer adds that product (the
/// owner, 30 October 2026). Swift has no code run on import, so PDFjet finds
/// the bundle by its name, which SwiftPM gives it, PackageName_TargetName:
/// from the command line, PDFjet_PDFjetFontsCJK.bundle, or .resources on
/// Linux with the native build system, beside the executable; in an app that
/// Xcode builds, PDFjet_PDFjetFontsCJK.bundle among the resources of the app,
/// or of the framework that links PDFjet. Those are the places the bundle's
/// own accessor, Bundle.module, looks in, so the bundle is found where the
/// module would find it, and the developer writes no code.
enum BundledFonts {
    // Where the fonts are on disk: the folder resources/fonts of the working
    // directory, as in the repository and the packages of the releases.
    static let FOLDER = "resources/fonts/"

    // The families of the CJK fonts, which are in resources/fonts/cjk and in
    // the product PDFjetFontsCJK, apart from the others, as they are about
    // 48 MB.
    private static let cjkFamilies = ["IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC"]

    // The names SwiftPM gives the resource bundle of the target
    // PDFjetFontsCJK of the package PDFjet, on Apple's systems and on Linux.
    private static let cjkBundleNames = ["PDFjet_PDFjetFontsCJK.bundle", "PDFjet_PDFjetFontsCJK.resources"]

    // The resource bundle of the fonts but the CJK ones, of the module
    // PDFjetFonts, which PDFjet depends on.
    static let bundle: Bundle? = PDFjetFonts.bundle

    // The resource bundle of the product PDFjetFontsCJK, or nil when the
    // program was built without it, found once.
    static let cjkBundle: Bundle? = findBundle(cjkBundleNames)

    // Looks for a bundle of one of the names where SwiftPM and Xcode put it.
    private static func findBundle(_ names: [String]) -> Bundle? {
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
            for name in names {
                let url = folder.appendingPathComponent(name)
                if FileManager.default.fileExists(atPath: url.path), let bundle = Bundle(url: url) {
                    return bundle
                }
            }
        }
        return nil
    }

    // The line of the product PDFjetFontsCJK for Package.swift.
    static let CJK_PRODUCT = ".product(name: \"PDFjetFontsCJK\", package: \"pdfjet\")"

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

    // Returns the family of a name, Family/File, if it is one of the CJK
    // families, or nil.
    private static func cjkFamilyOf(_ name: String) -> String? {
        let family = name.split(separator: "/", maxSplits: 1, omittingEmptySubsequences: false).first.map(String.init)
        return cjkFamilies.first { $0 == family }
    }

    // Returns the path of a font under resources/fonts by its name,
    // Family/File: the name, or cjk/ and the name for a CJK font, such as
    // cjk/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.
    static func folderPathOf(_ name: String) -> String {
        return (cjkFamilyOf(name) != nil) ? "cjk/" + name : name
    }

    // Returns the URL of the font of the path that PDFjet carries, or nil:
    // in the folder resources/fonts, or else in the bundle of PDFjetFonts,
    // or of PDFjetFontsCJK for a CJK font.
    static func url(_ fontPath: String, _ folder: String = FOLDER,
            _ bundle: Bundle? = BundledFonts.bundle, _ cjkBundle: Bundle? = BundledFonts.cjkBundle) -> URL? {
        guard let name = nameOf(fontPath), !name.contains(".."), !name.hasPrefix("/") else {
            return nil
        }
        let file = folder + folderPathOf(name)
        var isDirectory: ObjCBool = false
        if FileManager.default.fileExists(atPath: file, isDirectory: &isDirectory), !isDirectory.boolValue {
            return URL(fileURLWithPath: file)
        }
        // The bundle has the files of the family folders side by side, as
        // .process puts them, each font's name its own
        guard let bundle = (cjkFamilyOf(name) != nil) ? cjkBundle : bundle,
                let slash = name.firstIndex(of: "/") else {
            return nil
        }
        let base = String(name[name.index(after: slash)...]) as NSString
        return bundle.url(forResource: base.deletingPathExtension, withExtension: base.pathExtension)
    }

    // Returns what to do when the font of the path is one of the CJK fonts
    // PDFjet carries, Regular, SemiBold or Bold, which is in neither the
    // folder resources/fonts/cjk nor the program, or nil for any other font:
    // add the product PDFjetFontsCJK.
    static func cjkMissing(_ fontPath: String) -> String? {
        guard let name = nameOf(fontPath), let family = cjkFamilyOf(name) else {
            return nil
        }
        let file = String(name.dropFirst(family.count + 1))
        guard ["Regular", "SemiBold", "Bold"].contains(where: { file == family + "-" + $0 + ".otf" }) else {
            return nil
        }
        return fontPath + " is not a file, nor in resources/fonts/cjk. IBM Plex Sans "
                + String(family.dropFirst("IBMPlexSans".count))
                + " is in the product PDFjetFontsCJK: add it to the dependencies of your target in "
                + "Package.swift, " + CJK_PRODUCT
    }

    static let SANS = 0
    static let SERIF = 1
    static let MONO = 2
    private static let families = ["IBMPlexSans", "IBMPlexSerif", "CascadiaMono"]
    // The CSS weights the faces of each family stand for, and their styles.
    // Cascadia Mono's strokes are heavier than IBM Plex Sans's at the same
    // weight: its SemiLight is as thick as Plex Sans Regular, so it stands
    // for 400, and its Regular for 500, as in PDFjet Forms (the owner, 29
    // October 2026); its SemiBold and Bold match Plex Sans's as they are.
    private static let weights: [[Int]] = [
        [100, 300, 400, 450, 500, 600, 700],
        [100, 300, 400, 450, 500, 600, 700],
        [300, 400, 500, 600, 700],
    ]
    private static let styles: [[String]] = [
        ["Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"],
        ["Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"],
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
                "source serif pro", "ibm plex serif"],
        ["monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
                "sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
                "jetbrains mono", "ibm plex mono", "source code pro", "cascadia mono", "cascadia code"],
    ]
}

// A class of the module, whose bundle is that of the framework that has it.
private final class BundleFinder {}
// End of BundledFonts.swift
