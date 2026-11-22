/**
 * HBShapeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
#if os(macOS) || os(Linux)
import Foundation
import Testing
@testable import PDFjet

// The tests of HBShape need hb-shape, HarfBuzz's program: they pass without
// testing anything where it is not installed.
@Suite struct HBShapeTests {
    private static let devanagari = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf"
    private static let khmer = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf"

    private static func installed() -> HBShape? {
        let hb = HBShape()
        return ((try? hb.getVersion())?.contains("HarfBuzz") ?? false) ? hb : nil
    }

    private static func udhr(_ code: String) throws -> [String] {
        return try String(contentsOfFile: TestSupport.path("resources/data/udhr/udhr_\(code).txt"), encoding: .utf8)
                .split(separator: "\n").map(String.init)
    }

    // Khmer breaks at the ends of words, as ICU's dictionary has them; Hindi
    // at its spaces; each line starts where the last ended
    @Test func breaksBetweenWords() throws {
        guard let hb = HBShapeTests.installed() else {
            return
        }
        for (code, lang, path) in [("khm", "km", HBShapeTests.khmer), ("hin", "hi", HBShapeTests.devanagari)] {
            let font = [UInt8](try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(path))))
            var breaks = 0
            for paragraph in try HBShapeTests.udhr(code) {
                let lines = try hb.shape(ShapingRequest(paragraph, lang, [font], 12, 300))
                let cps = DictionaryBreaks.codePoints(paragraph)
                let ends = Set(DictionaryBreaks.wordEnds(cps))
                let utf16 = Array(paragraph.utf16)
                for (i, line) in lines.enumerated() {
                    if i > 0 {
                        #expect(line.start == lines[i - 1].end)
                    }
                    if i == lines.count - 1 {
                        #expect(line.end == utf16.count)
                        continue
                    }
                    breaks += 1
                    let at = String(decoding: utf16[0..<line.end], as: UTF16.self).unicodeScalars.count
                    #expect(cps[at - 1] == 0x20 || ends.contains(at), "\(code): a break inside a word at \(at)")
                }
            }
            #expect(breaks >= 50, "\(code): \(breaks) breaks")
        }
    }

#if os(Linux)
    // On Linux, without a shaper set, ComplexText does not run hb-shape, even
    // where it is installed: it says to set HBShape, so that a program
    // shapes the same wherever it runs
    @Test func complexTextWithoutAShaperRunsNoHBShape() throws {
        let pdf = TestSupport.newPDF()
        let khmer = try Font(pdf, TestSupport.path(HBShapeTests.khmer))
        _ = khmer.setSize(12)
        let text = ComplexText(khmer, try HBShapeTests.udhr("khm")[2]).setLanguage("km").setWidth(300)
        do {
            _ = try text.getTextHeight()
            Issue.record("shaped without a shaper")
        } catch {
            #expect("\(error)".contains(".setShaper(HBShape())"), "\(error)")
        }
    }
#endif

    // ComplexText draws Hindi with an English word, in two fonts, and Khmer
    @Test func drawsComplexText() throws {
        guard let hb = HBShapeTests.installed() else {
            return
        }
        let pdf = TestSupport.newPDF()
        let hindi = try Font(pdf, TestSupport.path(HBShapeTests.devanagari))
        let khmer = try Font(pdf, TestSupport.path(HBShapeTests.khmer))
        let latin = try Font(pdf, TestSupport.path("resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"))
        for (font, text, lang) in [(hindi, try HBShapeTests.udhr("hin")[2] + " PDFjet", "hi"),
                (khmer, try HBShapeTests.udhr("khm")[2], "km")] {
            _ = font.setSize(12)
            let page = Page(pdf, Letter.PORTRAIT)
            let xy = ComplexText(font, text).setFallbackFont(latin).setLanguage(lang).setShaper(hb)
                    .setWidth(300).setLocation(50, 50).drawOn(page)
            #expect(xy[1] > 50 + 3 * 12, "\(lang): drawn down to \(xy[1])")
        }
    }
}
#endif
