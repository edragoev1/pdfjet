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
 * Draws Hindi and Khmer as real text: Apple's Core Text shapes them --
 * conjuncts, vowel signs, stacked consonants -- and Page.drawGlyphs draws the
 * glyphs it found, at their places, with the text they stand for, so that
 * copying, searching and screen readers get it. PDFjet does not shape these
 * scripts itself; any engine that finds the glyphs can: Core Text here,
 * java.awt.font in the Java example, Android's TextShaper or HarfBuzz
 * elsewhere. The text is Article 1 of the Universal Declaration of Human
 * Rights. Core Text is Apple's: on Linux the example says so and draws
 * nothing.
 */
#if canImport(CoreText)
public class Example_52 {
    private static let devanagari = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf"
    private static let khmer = "examples/fonts/NotoSansKhmer/NotoSansKhmer-Regular.ttf"

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_52.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Hindi and Khmer as real text")    // Required for PDF/UA !
        pdf.setLanguage("en")

        let heading = try Font(pdf, IBMPlexSans.SemiBold)
        heading.setSize(16.0)
        let body = try Font(pdf, IBMPlexSans.Regular)
        body.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(heading, "Article 1 of the Universal Declaration of Human Rights")
                .setStructureType(StructElem.H1)
                .setLocation(50.0, 60.0)
                .drawOn(page)
        TextLine(body, "Shaped by Core Text, drawn by PDFjet with drawGlyphs: real text, in Hindi and in Khmer.")
                .setLocation(50.0, 82.0)
                .drawOn(page)

        var y: Float = 100.0
        y = try Example_52.drawShaped(pdf, page,
                "सभी मनुष्यों को गौरव और अधिकारों के मामले में जन्मजात स्वतन्त्रता और समानता प्राप्त है । "
                + "उन्हें बुद्धि और अन्तरात्मा की देन प्राप्त है और परस्पर उन्हें भाईचारे के भाव से बर्ताव करना चाहिए ।",
                "hi", Example_52.devanagari, 12.0, 50.0, y, 512.0)
        _ = try Example_52.drawShaped(pdf, page,
                "មនុស្សទាំងអស់ កើតមកមានសេរីភាព និងសមភាព ក្នុងផ្នែកសេចក្ដីថ្លៃថ្នូរនិងសិទ្ធិ។ "
                + "មនុស្ស មានវិចារណញ្ញាណនិងសតិសម្បជញ្ញៈជាប់ពីកំណើត "
                + "ហើយគប្បីប្រព្រឹត្ដចំពោះគ្នាទៅវិញទៅមកក្នុងស្មារតីភាតរភាពជាបងប្អូន។",
                "km", Example_52.khmer, 12.0, 50.0, y + 14.0, 512.0)

        try pdf.complete()
    }

    // Draws a paragraph in a script PDFjet does not shape, wrapped to the
    // width, as real text, and returns the y below its last line. The same
    // font file is loaded by Core Text, which shapes, and by PDFjet, which
    // embeds it, so that the glyph numbers match.
    private static func drawShaped(_ pdf: PDF, _ page: Page, _ text: String, _ language: String,
            _ fontFile: String, _ size: Float, _ x: Float, _ y: Float, _ width: Float) throws -> Float {
        let embedded = try Font(pdf, fontFile)
        embedded.setSize(size)
        guard let descriptors = CTFontManagerCreateFontDescriptorsFromURL(
                URL(fileURLWithPath: fontFile) as CFURL) as? [CTFontDescriptor],
              let descriptor = descriptors.first else {
            throw NSError(domain: "Example_52", code: 1,
                    userInfo: [NSLocalizedDescriptionKey: "Core Text cannot read \(fontFile)"])
        }
        let font = CTFontCreateWithFontDescriptor(descriptor, CGFloat(size), nil)
        let fontName = CTFontCopyPostScriptName(font) as String

        let attributed = NSAttributedString(string: text, attributes: [
            NSAttributedString.Key(kCTFontAttributeName as String): font,
            NSAttributedString.Key(kCTLanguageAttributeName as String): language,
        ])
        let typesetter = CTTypesetterCreateWithAttributedString(attributed as CFAttributedString)
        let utf16 = Array(text.utf16)

        page.addBDC(StructElem.P, language, nil, nil)
        var top = y
        var start = 0
        while start < utf16.count {
            let count = CTTypesetterSuggestLineBreak(typesetter, start, Double(width))
            if count <= 0 {
                break
            }
            let line = CTTypesetterCreateLine(typesetter, CFRange(location: start, length: count))
            var ascent: CGFloat = 0
            var descent: CGFloat = 0
            var leading: CGFloat = 0
            CTLineGetTypographicBounds(line, &ascent, &descent, &leading)
            let baseline = top + Float(ascent)
            // The line, a run at a time: its glyphs, their places and the
            // characters each one comes from (its cluster)
            for run in (CTLineGetGlyphRuns(line) as? [CTRun]) ?? [] {
                let n = CTRunGetGlyphCount(run)
                let attributes = CTRunGetAttributes(run) as NSDictionary
                guard n > 0, let value = attributes[kCTFontAttributeName as String] else {
                    continue
                }
                // A run in a fallback font would need that font embedded too;
                // the Noto fonts have every character of these texts
                let runFont = value as! CTFont
                if CTFontCopyPostScriptName(runFont) as String != fontName {
                    throw NSError(domain: "Example_52", code: 2,
                            userInfo: [NSLocalizedDescriptionKey: "Core Text fell back to another font"])
                }
                var glyphs = [CGGlyph](repeating: 0, count: n)
                var points = [CGPoint](repeating: .zero, count: n)
                var indices = [CFIndex](repeating: 0, count: n)
                CTRunGetGlyphs(run, CFRange(location: 0, length: 0), &glyphs)
                CTRunGetPositions(run, CFRange(location: 0, length: 0), &points)
                CTRunGetStringIndices(run, CFRange(location: 0, length: 0), &indices)
                let range = CTRunGetStringRange(run)
                var positions = [Float]()
                for p in points {
                    positions.append(x + Float(p.x))
                    positions.append(baseline - Float(p.y))
                }
                let runText = String(decoding: utf16[range.location..<(range.location + range.length)],
                        as: UTF16.self)
                page.drawGlyphs(embedded, glyphs.map { Int($0) }, positions,
                        indices.map { $0 - range.location }, runText, language)
            }
            top = baseline + Float(descent + leading)
            start += count
        }
        page.addEMC()
        return top
    }
}   // End of Example_52.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_52()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_52 => \(String(format: "%4lld", time1 - time0)) ms")
#else
print("Example_52 needs Core Text, Apple's: run it on a Mac.")
#endif
