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
        for family in try FileManager.default.contentsOfDirectory(atPath: root) {
            var isDirectory: ObjCBool = false
            guard FileManager.default.fileExists(atPath: root + "/" + family, isDirectory: &isDirectory),
                    isDirectory.boolValue else {
                continue
            }
            for font in try FileManager.default.contentsOfDirectory(atPath: root + "/" + family)
                    where font.hasSuffix(".ttf") || font.hasSuffix(".otf") {
                carried.insert("fonts/" + family + "/" + font)
            }
        }
        #expect(carried.count == 82)
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
        // The tests depend on PDFjetFonts: its bundle is found by its name
        let bundle = try #require(BundledFonts.bundle)
        #expect(bundle.bundleURL.lastPathComponent.hasPrefix("PDFjet_PDFjetFonts."))
        // From the working directory, the repository, resources/fonts first
        let onDisk = try #require(BundledFonts.url(IBMPlexSans.Regular))
        #expect(onDisk.path.hasSuffix("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"), "\(onDisk.path)")
        #expect(!onDisk.path.contains("PDFjet_PDFjetFonts"), "\(onDisk.path)")
        // With no such folder, as in an app, the bundle
        let missing = "no-such-folder/resources/fonts/"
        let inBundle = try #require(BundledFonts.url(IBMPlexSans.Regular, missing))
        #expect(inBundle.path.contains("PDFjet_PDFjetFonts"), "\(inBundle.path)")
        #expect(try Data(contentsOf: inBundle) == Data(contentsOf: onDisk))
        #expect(BundledFonts.url(IBMPlexSansJP.Bold, missing) != nil)
        #expect(BundledFonts.url("fonts/NotoSans/NotoSans-Regular.ttf", missing) == nil)
        #expect(BundledFonts.available(missing))
        // The fonts and their licenses, without the Go port's files
        let files = try FileManager.default.subpathsOfDirectory(atPath: bundle.bundleURL.path)
        #expect(files.filter { $0.hasSuffix(".ttf") || $0.hasSuffix(".otf") }.count == 82)
        #expect(!files.contains { $0.hasSuffix(".go") }, "\(files.filter { $0.hasSuffix(".go") })")
        for license in ["license.txt", "LICENSE", "OFL.txt"] {
            #expect(files.contains { $0.hasSuffix(license) }, "\(license)")
        }
        // With neither, as in an app without PDFjetFonts, nothing, and the
        // error says to add the product, with its line for Package.swift
        #expect(BundledFonts.url(IBMPlexSans.Regular, missing, nil) == nil)
        #expect(!BundledFonts.available(missing, nil))
        let message = Font.notCarried(IBMPlexSans.Regular, false)
        #expect(message.contains("add the product PDFjetFonts"), "\(message)")
        #expect(message.contains(".product(name: \"PDFjetFonts\", package: \"pdfjet\")"), "\(message)")
        #expect(!message.contains("pdfjet-fonts"), "\(message)")
    }

    @Test func cssWeightsMatchFacesThatLookAlikeAcrossTheFamilies() {
        // Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it
        // stands for 400, and Regular for 500
        let cases: [(Int, Int, String)] = [
            (0, 400, "IBMPlexSans/IBMPlexSans-Regular.ttf"),
            (0, 200, "IBMPlexSans/IBMPlexSans-Thin.ttf"),
            (0, 800, "IBMPlexSans/IBMPlexSans-Bold.ttf"),
            (1, 100, "SourceSerif4/SourceSerif4-Light.ttf"),
            (1, 900, "SourceSerif4/SourceSerif4-Bold.ttf"),
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
