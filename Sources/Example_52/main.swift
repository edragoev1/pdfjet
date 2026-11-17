/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet
#if canImport(CoreText)
import CoreText
#endif

/**
 * Example_52.swift
 *
 * Draws Hindi and Khmer, scripts PDFjet does not shape itself: Apple's Core
 * Text shapes them -- conjuncts, vowel signs, stacked consonants -- and
 * Page.drawGlyphs draws the glyphs it found, at their places, as outlines
 * read from the font file: sharp on screen and in print, and read by screen
 * readers, as each paragraph is tagged with its text and its language. Any
 * engine that finds the glyphs can do the shaping: Core Text here,
 * java.awt.font in the Java example, Android's TextShaper or HarfBuzz
 * elsewhere. A page of each, filled with the Universal Declaration of Human
 * Rights from its beginning: Hindi on the first page, Khmer on the second.
 * Core Text is Apple's: on Linux the example says so and draws nothing.
 */
#if canImport(CoreText)
public class Example_52 {
    private static let devanagari = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
    private static let khmer = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf"
    private static let latin = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

    private static let size: Float = 10.5
    private static let left: Float = 50.0
    private static let width: Float = 512.0
    private static let bottom: Float = 742.0    // The page's 792 points less the margin

    // A font of Core Text, which shapes, and the same font file loaded by
    // PDFjet, which reads the outlines, so that the glyph numbers match
    private struct ShapingFont {
        let shaper: CTFont
        let name: String
        let font: Font
    }

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_52.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The Universal Declaration of Human Rights in Hindi and Khmer")    // Required for PDF/UA !
        pdf.setLanguage("en")

        let heading = try Font(pdf, IBMPlexSans.SemiBold)
        heading.setSize(16.0)
        let body = try Font(pdf, IBMPlexSans.Regular)
        body.setSize(9.0)

        let hindi = try Example_52.drawPage(pdf, heading, body, "Hindi", "hi", Example_52.devanagari,
                "resources/data/udhr/udhr_hin.txt")
        let khmer = try Example_52.drawPage(pdf, heading, body, "Khmer", "km", Example_52.khmer,
                "resources/data/udhr/udhr_khm.txt")

        // The pages draw the text as outlines, which cannot be selected or
        // searched; the exact text is attached for anyone who needs it.
        try pdf.attachText("udhr-hindi.txt", hindi)
        try pdf.attachText("udhr-khmer.txt", khmer)

        try pdf.complete()
    }

    private static func loadFont(_ pdf: PDF, _ path: String) throws -> ShapingFont {
        guard let descriptors = CTFontManagerCreateFontDescriptorsFromURL(
                URL(fileURLWithPath: path) as CFURL) as? [CTFontDescriptor],
              let descriptor = descriptors.first else {
            throw NSError(domain: "Example_52", code: 1,
                    userInfo: [NSLocalizedDescriptionKey: "Core Text cannot read \(path)"])
        }
        let shaper = CTFontCreateWithFontDescriptor(descriptor, CGFloat(size), nil)
        let font = try Font(pdf, path)
        font.setSize(size)
        return ShapingFont(shaper: shaper, name: CTFontCopyPostScriptName(shaper) as String, font: font)
    }

    // Draws a page of the Declaration in the language, its paragraphs from
    // the beginning until the page is full, and returns the text drawn.
    private static func drawPage(_ pdf: PDF, _ heading: Font, _ body: Font, _ name: String,
            _ language: String, _ scriptFile: String, _ textFile: String) throws -> String {
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(heading, "Universal Declaration of Human Rights, in " + name)
                .setStructureType(StructElem.H1)
                .setLocation(left, 60.0)
                .drawOn(page)
        TextLine(body, "Shaped by Core Text, drawn by PDFjet with drawGlyphs as outlines;"
                + " the text is attached to the document.")
                .setLocation(left, 80.0)
                .drawOn(page)

        let fonts = [try loadFont(pdf, scriptFile), try loadFont(pdf, latin)]
        let ascent = fonts[0].font.getAscent()
        let descent = fonts[0].font.getDescent()

        var drawn = ""
        var y: Float = 96.0     // The top of the next line
        let paragraphs = try String(contentsOfFile: textFile, encoding: .utf8)
                .components(separatedBy: "\n")
                .map { $0.trimmingCharacters(in: .whitespaces) }
                .filter { !$0.isEmpty }
        for paragraph in paragraphs {
            let text = try drawParagraph(page, paragraph, language, fonts, &y, ascent, descent)
            if text.isEmpty {
                break       // The page is full
            }
            // A blank line between paragraphs, as plain text has them
            if !drawn.isEmpty {
                drawn += "\n"
            }
            drawn += text + "\n"
            if text.utf16.count < paragraph.utf16.count {
                break       // The page is full
            }
            y += 0.5 * size
        }
        return drawn
    }

    // Draws the paragraph, wrapped to the width, as many of its lines as fit
    // above the bottom of the page, from the top y, which it moves below
    // them, and returns the text of the lines drawn. The script's font is
    // used where it has the characters, IBM Plex Sans elsewhere.
    private static func drawParagraph(_ page: Page, _ text: String, _ language: String,
            _ fonts: [ShapingFont], _ y: inout Float, _ ascent: Float, _ descent: Float) throws -> String {
        // Each character in the script's font where it has it, else in the Latin one
        let attributed = NSMutableAttributedString(string: text, attributes: [
            NSAttributedString.Key(kCTFontAttributeName as String): fonts[0].shaper,
            NSAttributedString.Key(kCTLanguageAttributeName as String): language,
        ])
        let utf16 = Array(text.utf16)
        var i = 0
        while i < utf16.count {
            let n = UTF16.isLeadSurrogate(utf16[i]) && i + 1 < utf16.count ? 2 : 1
            var characters = Array(utf16[i..<(i + n)])
            var glyphs = [CGGlyph](repeating: 0, count: n)
            if !CTFontGetGlyphsForCharacters(fonts[0].shaper, &characters, &glyphs, n) {
                attributed.addAttribute(NSAttributedString.Key(kCTFontAttributeName as String),
                        value: fonts[1].shaper, range: NSRange(location: i, length: n))
            }
            i += n
        }
        let typesetter = CTTypesetterCreateWithAttributedString(attributed as CFAttributedString)

        var start = 0
        while start < utf16.count {
            if y + ascent + descent > bottom {
                break
            }
            let count = CTTypesetterSuggestLineBreak(typesetter, start, Double(width))
            if count <= 0 {
                break
            }
            if start == 0 {
                page.addBDC(StructElem.P, language, nil, nil)
            }
            let line = CTTypesetterCreateLine(typesetter, CFRange(location: start, length: count))
            let baseline = y + ascent
            // The line, a run of one font at a time: its glyphs and their places
            for run in (CTLineGetGlyphRuns(line) as? [CTRun]) ?? [] {
                let n = CTRunGetGlyphCount(run)
                let attributes = CTRunGetAttributes(run) as NSDictionary
                guard n > 0, let value = attributes[kCTFontAttributeName as String] else {
                    continue
                }
                // A run in a font Core Text fell back to would need that font
                // file too; the two fonts have every character of these texts
                let runName = CTFontCopyPostScriptName(value as! CTFont) as String
                guard let runFont = fonts.first(where: { $0.name == runName }) else {
                    throw NSError(domain: "Example_52", code: 2,
                            userInfo: [NSLocalizedDescriptionKey: "Core Text fell back to \(runName)"])
                }
                var glyphs = [CGGlyph](repeating: 0, count: n)
                var points = [CGPoint](repeating: .zero, count: n)
                CTRunGetGlyphs(run, CFRange(location: 0, length: 0), &glyphs)
                CTRunGetPositions(run, CFRange(location: 0, length: 0), &points)
                let range = CTRunGetStringRange(run)
                var positions = [Float]()
                for p in points {
                    positions.append(left + Float(p.x))
                    positions.append(baseline - Float(p.y))
                }
                let runText = String(decoding: utf16[range.location..<(range.location + range.length)],
                        as: UTF16.self)
                page.drawGlyphs(runFont.font, glyphs.map { Int($0) }, positions, runText, language)
            }
            y = baseline + descent + 0.25 * size
            start += count
        }
        if start > 0 {
            page.addEMC()
        }
        return String(decoding: utf16[0..<start], as: UTF16.self)
    }
}   // End of Example_52.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_52()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_52 => \(String(format: "%4lld", time1 - time0)) ms")
#else
print("Example_52 needs Core Text, Apple's: run it on a Mac.")
#endif
