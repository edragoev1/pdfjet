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
        #expect(IBMPlexSans.Regular == "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
        #expect(IBMPlexSansJP.Regular == "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")
        #expect(CascadiaMono.Regular == "fonts/CascadiaMono/CascadiaMono-Regular.otf")
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
        #expect(BundledFonts.url("fonts/IBMPlexSans/IBMPlexSans-Regular.otf", "no-such-folder/", nil, nil) == nil)
        // From the working directory, the repository, resources/fonts first
        let onDisk = try #require(BundledFonts.url(IBMPlexSans.Regular))
        #expect(onDisk.path.hasSuffix("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"), "\(onDisk.path)")
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

    // The message the font of the path fails to open with, or nil.
    private func openError(_ path: String) -> String? {
        do {
            _ = try Font.open(path)
            return nil
        } catch {
            return TestSupport.message(error)
        }
    }

    // The fonts PDFjet carries are .otf files, and a path loads the file it
    // names: a path to the .ttf of one of them, which is not a file, is not
    // found, from resources/fonts nor from the bundle of PDFjetFonts, and the
    // error names the .otf path and the constant to write; a .ttf of one's own
    // at that path, or of that name in the folder, is read as any file is.
    @Test func aPathToTheTTFOfAFontPDFjetCarriesAsOTFNamesTheOTF() throws {
        let missing = "no-such-folder/resources/fonts/"
        #expect(openError("fonts/CascadiaMono/CascadiaMono-Regular.ttf") ==
                "No bundled font fonts/CascadiaMono/CascadiaMono-Regular.ttf; PDFjet carries it as "
                + "fonts/CascadiaMono/CascadiaMono-Regular.otf (or use the constant CascadiaMono.Regular).")
        #expect(throws: (any Error).self) {
            _ = try Font(TestSupport.newPDF(), "fonts/CascadiaMono/CascadiaMono-Regular.ttf")
        }
        let faces = [
            (IBMPlexSans.Regular, "IBMPlexSans.Regular"),
            (IBMPlexSans.Italic, "IBMPlexSans.Italic"),
            (IBMPlexSans.ThinItalic, "IBMPlexSans.ThinItalic"),
            (IBMPlexSerif.SemiBold, "IBMPlexSerif.SemiBold"),
            (CascadiaMono.SemiLightItalic, "CascadiaMono.SemiLightItalic"),
            (IBMPlexSansArabic.Regular, "IBMPlexSansArabic.Regular"),
            (IBMPlexSansHebrew.Thin, "IBMPlexSansHebrew.Thin"),
            (IBMPlexSansThai.Bold, "IBMPlexSansThai.Bold"),
            (IBMPlexSansJP.Regular, "IBMPlexSansJP.Regular"),
        ]
        for (otf, constant) in faces {
            let ttf = otf.replacingOccurrences(of: ".otf", with: ".ttf")
            #expect(BundledFonts.url(ttf) == nil, "\(ttf)")
            #expect(BundledFonts.url(ttf, missing) == nil, "\(ttf) in the bundle")
            #expect(openError(ttf) == "No bundled font " + ttf + "; PDFjet carries it as " + otf
                    + " (or use the constant " + constant + ").")
        }
        // A face PDFjet does not carry, and a font of a family it does not
        // carry, are not found, as before, with no .otf to name
        for path in ["fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf", "fonts/CascadiaMono/CascadiaMono-ExtraLight.ttf",
                "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf", "fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.woff2"] {
            #expect(BundledFonts.url(path) == nil, "\(path)")
            #expect(BundledFonts.otfInstead(path) == nil, "\(path)")
            let message = openError(path) ?? ""
            #expect(message.contains("nor one of the fonts PDFjet carries"), "\(message)")
        }
        #expect(BundledFonts.otfInstead(IBMPlexSans.Regular) == nil)
        // A .ttf of one's own at that path, and of that name in the folder
        let ttf = try TestSupport.fontBytes("tests/fonts/IBMPlexSans-Regular.ttf")
        let dir = FileManager.default.temporaryDirectory.appendingPathComponent("pdfjet-" + UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: dir) }
        let own = dir.appendingPathComponent("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")
        let inFolder = dir.appendingPathComponent("resources/fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf")
        for file in [own, inFolder] {
            try FileManager.default.createDirectory(at: file.deletingLastPathComponent(),
                    withIntermediateDirectories: true)
            try Data(ttf).write(to: file)
        }
        #expect(try Font.open(own.path) == ttf)
        let found = try #require(BundledFonts.url("fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf",
                dir.appendingPathComponent("resources/fonts").path + "/"))
        #expect(try [UInt8](Data(contentsOf: found)) == ttf)
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
        #expect(files.filter { $0.hasSuffix(".otf") }.count == 33)
        #expect(files.filter { $0.hasSuffix(".ttf") }.isEmpty)
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
        #expect(message == "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf is not a file, nor in "
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
        #expect(BundledFonts.worldMissing("fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.otf.stream")?
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
            (0, 400, "IBMPlexSans/IBMPlexSans-Regular.otf"),
            (0, 200, "IBMPlexSans/IBMPlexSans-Thin.otf"),
            (0, 800, "IBMPlexSans/IBMPlexSans-Bold.otf"),
            (1, 100, "IBMPlexSerif/IBMPlexSerif-Thin.otf"),
            (1, 900, "IBMPlexSerif/IBMPlexSerif-Bold.otf"),
            (2, 400, "CascadiaMono/CascadiaMono-SemiLight.otf"),
            (2, 500, "CascadiaMono/CascadiaMono-Regular.otf"),
            (2, 600, "CascadiaMono/CascadiaMono-SemiBold.otf"),
            (2, 700, "CascadiaMono/CascadiaMono-Bold.otf"),
        ]
        for (family, weight, file) in cases {
            #expect(BundledFonts.fileName(family, weight, false) == file, "\(family) \(weight)")
        }
        #expect(BundledFonts.fileName(2, 400, true) == "CascadiaMono/CascadiaMono-SemiLightItalic.otf")
        #expect(BundledFonts.fileName(2, 500, true) == "CascadiaMono/CascadiaMono-Italic.otf")
        #expect(BundledFonts.fileName(0, 400, true) == "IBMPlexSans/IBMPlexSans-Italic.otf")
    }
}   // End of BundledFontsTests.swift
