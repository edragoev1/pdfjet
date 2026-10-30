/**
 * BundledFontsTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The fonts PDFjet carries, in resources/fonts: each has a constant in the
/// font structs, and each constant that is not deprecated names one of them,
/// which the library has.
@Suite struct BundledFontsTests {
    private static let structs = [
        "IBMPlexMath", "IBMPlexMono", "IBMPlexSans", "IBMPlexSansArabic", "IBMPlexSansHebrew",
        "IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC", "IBMPlexSansThai",
        "IBMPlexSerif", "JetBrainsMono", "CascadiaMono", "SourceSerif4", "NotoSans",
    ]

    // The constants of a font struct, read from its source, as Swift has no
    // reflection of static members: the name and the path of each, and
    // whether it, or the struct, is deprecated.
    private func constants(_ name: String) throws -> [(name: String, path: String, deprecated: Bool)] {
        let source = try String(contentsOfFile: TestSupport.path("Sources/PDFjet/Fonts/" + name + ".swift"),
                encoding: .utf8)
        var found = [(name: String, path: String, deprecated: Bool)]()
        var deprecated = false
        var structDeprecated = false
        for line in source.components(separatedBy: "\n") {
            let trimmed = line.trimmingCharacters(in: .whitespaces)
            if trimmed.hasPrefix("@available(*, deprecated") {
                deprecated = true
            } else if trimmed.hasPrefix("public struct ") {
                structDeprecated = deprecated
                deprecated = false
            } else if trimmed.hasPrefix("public static let ") {
                let parts = trimmed.dropFirst("public static let ".count).components(separatedBy: "=")
                let constant = parts[0].trimmingCharacters(in: .whitespaces)
                let path = parts[1].trimmingCharacters(in: CharacterSet(charactersIn: " \";"))
                found.append((constant, path, deprecated || structDeprecated))
                deprecated = false
            }
        }
        return found
    }

    @Test func everyFontCarriedHasAConstantAndEveryConstantNotDeprecatedIsCarried() throws {
        var carried = Set<String>()
        let root = TestSupport.path("resources/fonts")
        // The family folders, and those of the other scripts in the folder
        // world; the folder cjk is the Go port's deprecated package alone
        var folders = [String]()
        for family in try FileManager.default.contentsOfDirectory(atPath: root) where family != "world" {
            folders.append(family)
        }
        for family in try FileManager.default.contentsOfDirectory(atPath: root + "/world") {
            folders.append("world/" + family)
        }
        for folder in folders {
            var isDirectory: ObjCBool = false
            guard FileManager.default.fileExists(atPath: root + "/" + folder, isDirectory: &isDirectory),
                    isDirectory.boolValue else {
                continue
            }
            let family = (folder as NSString).lastPathComponent
            for font in try FileManager.default.contentsOfDirectory(atPath: root + "/" + folder)
                    where font.hasSuffix(".ttf") || font.hasSuffix(".otf") {
                carried.insert("fonts/" + family + "/" + font)
            }
        }
        #expect(carried.count == 71)
        var named = Set<String>()
        for name in BundledFontsTests.structs {
            let found = try constants(name)
            #expect(!found.isEmpty, "\(name) has no constants")
            for constant in found {
                if constant.deprecated {
                    #expect(!carried.contains(constant.path), "\(name).\(constant.name) is carried")
                } else {
                    #expect(carried.contains(constant.path), "\(name).\(constant.name) is not carried")
                    // In the library, as the module has it, not only in the folder
                    #expect(BundledFonts.url(constant.path) != nil, "\(constant.path)")
                    named.insert(constant.path)
                }
            }
        }
        #expect(carried == named)
        // The source read is the constants the library has
        #expect(IBMPlexSans.Regular == "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")
        #expect(IBMPlexSansJP.Regular == "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")
        #expect(CascadiaMono.Regular == "fonts/CascadiaMono/CascadiaMono-Regular.ttf")
    }

    @Test func aFontAddedToAnExistingPDFIsReadFromTheLibraryByItsPath() throws {
        // The objects of a PDF of one page, and the constant of a font PDFjet
        // carries, with no fonts folder: the font is read from the library,
        // as Font(pdf, path) reads it
        let memory = MemoryPDF()
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        var objects = try TestSupport.read(memory.bytes)
        let font = try Font(&objects, IBMPlexSans.Bold)
        #expect(font.name == "IBMPlexSans-Bold")
        // A font it does not carry says so, and to use a font of one's own
        do {
            _ = try Font(&objects, "fonts/NotoSans/NotoSans-Regular.ttf")
            Issue.record("a font PDFjet does not carry was read")
        } catch {
            let message = TestSupport.message(error)
            #expect(message.contains("nor one of the fonts PDFjet carries"), "\(message)")
            #expect(message.contains("use a font file of your own"), "\(message)")
            #expect(!message.contains("pdfjet-fonts"), "\(message)")
        }
    }

    @Test func aFontIsReadFromResourcesFontsElseFromTheBundleOfPDFjetFonts() throws {
        // PDFjet depends on PDFjetFonts, whose bundle has the European fonts
        let bundle = try #require(BundledFonts.bundle)
        #expect(bundle.bundleURL.lastPathComponent.hasPrefix("PDFjet_PDFjetFonts."))
        // Found by its name, as a lookup that returns nil can stop no process:
        // Bundle.module does when the bundle is missing
        #expect(BundledFonts.findBundle(BundledFonts.bundleNames)?.bundleURL == bundle.bundleURL)
        #expect(BundledFonts.findBundle(["PDFjet_NoSuchFonts.bundle"]) == nil)
        #expect(BundledFonts.url("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "no-such-folder/", nil, nil) == nil)
        // From the working directory, the repository, resources/fonts first
        let onDisk = try #require(BundledFonts.url(IBMPlexSans.Regular))
        #expect(onDisk.path.hasSuffix("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"), "\(onDisk.path)")
        #expect(!onDisk.path.contains("PDFjet_PDFjetFonts"), "\(onDisk.path)")
        // With no such folder, as in an app, the bundle
        let missing = "no-such-folder/resources/fonts/"
        let inBundle = try #require(BundledFonts.url(IBMPlexSans.Regular, missing))
        #expect(inBundle.path.contains("PDFjet_PDFjetFonts."), "\(inBundle.path)")
        #expect(try Data(contentsOf: inBundle) == Data(contentsOf: onDisk))
        #expect(BundledFonts.url("fonts/NotoSans/NotoSans-Regular.ttf", missing) == nil)
        // The fonts and their licenses: IBM Plex Sans and Serif and Cascadia
        // Mono alone, without the Go port's files
        let files = try FileManager.default.subpathsOfDirectory(atPath: bundle.bundleURL.path)
        #expect(files.filter { $0.hasSuffix(".ttf") || $0.hasSuffix(".otf") }.count == 38)
        #expect(!files.contains { $0.contains("IBMPlexSansJP") || $0.contains("IBMPlexSansArabic") })
        #expect(!files.contains { $0.hasSuffix(".go") }, "\(files.filter { $0.hasSuffix(".go") })")
        for license in ["license.txt", "LICENSE"] {
            #expect(files.contains { $0.hasSuffix(license) }, "\(license)")
        }
    }

    @Test func aWorldFontIsReadFromResourcesFontsWorldElseFromTheBundleOfPDFjetFontsWorld() throws {
        // The tests depend on PDFjetFontsWorld: its bundle is found by its name
        let worldBundle = try #require(BundledFonts.worldBundle)
        #expect(worldBundle.bundleURL.lastPathComponent.hasPrefix("PDFjet_PDFjetFontsWorld."))
        // From the working directory, the repository, resources/fonts/world first
        let onDisk = try #require(BundledFonts.url(IBMPlexSansJP.Bold))
        #expect(onDisk.path.hasSuffix("resources/fonts/world/IBMPlexSansJP/IBMPlexSansJP-Bold.otf"), "\(onDisk.path)")
        // With no such folder, the bundle of PDFjetFontsWorld
        let missing = "no-such-folder/resources/fonts/"
        let inBundle = try #require(BundledFonts.url(IBMPlexSansJP.Bold, missing))
        #expect(inBundle.path.contains("PDFjet_PDFjetFontsWorld."), "\(inBundle.path)")
        #expect(try Data(contentsOf: inBundle) == Data(contentsOf: onDisk))
        for path in [IBMPlexSansKR.Regular, IBMPlexSansSC.SemiBold, IBMPlexSansTC.Bold,
                IBMPlexSansArabic.Regular, IBMPlexSansHebrew.Thin, IBMPlexSansThai.Text] {
            #expect(BundledFonts.url(path, missing) != nil, "\(path)")
        }
        let files = try FileManager.default.subpathsOfDirectory(atPath: worldBundle.bundleURL.path)
        #expect(files.filter { $0.hasSuffix(".otf") }.count == 12)
        #expect(files.filter { $0.hasSuffix(".ttf") }.count == 21)
        #expect(files.contains { $0.hasSuffix("license.txt") })
        #expect(!files.contains { $0.hasSuffix(".go") })
    }

    @Test func aWorldFontNotFoundNamesTheProductAndTheLineToAdd() throws {
        // Neither the folder nor the bundle of PDFjetFontsWorld, as in an app
        // without that product: not found, while the European fonts are
        let missing = "no-such-folder/resources/fonts/"
        #expect(BundledFonts.url(IBMPlexSansJP.Regular, missing, BundledFonts.bundle, nil) == nil)
        #expect(BundledFonts.url(IBMPlexSansArabic.Regular, missing, BundledFonts.bundle, nil) == nil)
        #expect(BundledFonts.url(IBMPlexSans.Regular, missing, BundledFonts.bundle, nil) != nil)
        let message = try #require(BundledFonts.worldMissing(IBMPlexSansArabic.Regular))
        #expect(message == "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf is not a file, nor in "
                + "resources/fonts/world. IBM Plex Sans Arabic is in the product PDFjetFontsWorld, with PDFjet's "
                + "fonts for Chinese, Japanese, Korean, Arabic, Hebrew and Thai: add it to the dependencies of "
                + "your target in Package.swift, .product(name: \"PDFjetFontsWorld\", package: \"pdfjet\")",
                "\(message)")
        #expect(BundledFonts.worldMissing(IBMPlexSansJP.Regular)?.contains("IBM Plex Sans JP is in") == true)
        #expect(BundledFonts.worldMissing(IBMPlexSansThai.Thin)?.contains("IBM Plex Sans Thai is in") == true)
        // Not for the European fonts, nor for a face PDFjet does not carry,
        // nor for IBM Plex Math, which it no longer carries
        #expect(BundledFonts.worldMissing("fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf.stream")?
                .contains("IBM Plex Sans KR is in") == true)
        #expect(BundledFonts.worldMissing("fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.ttf.stream")?
                .contains("IBM Plex Sans Thai is in") == true)
        #expect(BundledFonts.worldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.ttf.stream")?
                .contains("IBM Plex Sans JP is in") == true)
        #expect(BundledFonts.worldMissing(IBMPlexSans.Regular) == nil)
        #expect(BundledFonts.worldMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf") == nil)
        #expect(BundledFonts.worldMissing("fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf") == nil)
        #expect(BundledFonts.worldMissing("fonts/IBMPlexMath/IBMPlexMath-Regular.ttf") == nil)
        #expect(!Font.notCarried(IBMPlexSans.Regular).contains("PDFjetFonts"))
    }

    @Test func cssWeightsMatchFacesThatLookAlikeAcrossTheFamilies() {
        // Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it
        // stands for 400, and Regular for 500
        let cases: [(Int, Int, String)] = [
            (0, 400, "IBMPlexSans/IBMPlexSans-Regular.ttf"),
            (0, 200, "IBMPlexSans/IBMPlexSans-Thin.ttf"),
            (0, 800, "IBMPlexSans/IBMPlexSans-Bold.ttf"),
            (1, 100, "IBMPlexSerif/IBMPlexSerif-Thin.ttf"),
            (1, 900, "IBMPlexSerif/IBMPlexSerif-Bold.ttf"),
            (2, 400, "CascadiaMono/CascadiaMono-SemiLight.ttf"),
            (2, 500, "CascadiaMono/CascadiaMono-Regular.ttf"),
            (2, 600, "CascadiaMono/CascadiaMono-SemiBold.ttf"),
            (2, 700, "CascadiaMono/CascadiaMono-Bold.ttf"),
        ]
        for (family, weight, file) in cases {
            #expect(BundledFonts.fileName(family, weight, false) == file, "\(family) \(weight)")
        }
        #expect(BundledFonts.fileName(2, 400, true) == "CascadiaMono/CascadiaMono-SemiLightItalic.ttf")
        #expect(BundledFonts.fileName(2, 500, true) == "CascadiaMono/CascadiaMono-Italic.ttf")
        #expect(BundledFonts.fileName(0, 400, true) == "IBMPlexSans/IBMPlexSans-Italic.ttf")
    }
}   // End of BundledFontsTests.swift
