/**
 * CoreTextShaper.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
#if canImport(CoreText)
import CoreText
import Foundation

// The shaper of Apple's platforms: Core Text. A typesetter breaks the text
// into lines and shapes each, each character in the first font that has it.
final class CoreTextShaper: Shaper {
    // Core Text's font of each font file, made once, for every thread: the
    // lock guards the map
    private final class Descriptors: @unchecked Sendable {
        static let shared = Descriptors()
        private var map = [Data: CTFontDescriptor]()
        private let lock = NSLock()

        func descriptor(_ file: [UInt8]) throws -> CTFontDescriptor {
            let data = Data(file)
            lock.lock()
            defer { lock.unlock() }
            if let descriptor = map[data] {
                return descriptor
            }
            guard let descriptor = CTFontManagerCreateFontDescriptorFromData(data as CFData) else {
                throw PDFjetError(message: "Core Text cannot read the font file.")
            }
            map[data] = descriptor
            return descriptor
        }
    }

    func shape(_ request: ShapingRequest) throws -> [ShapedLine] {
        let text = request.text
        let language = request.language
        let fontFiles = request.fontFiles
        let fontSize = request.fontSize
        let width = request.width
        let fonts = try fontFiles.map {
            CTFontCreateWithFontDescriptor(try Descriptors.shared.descriptor($0), CGFloat(fontSize), nil)
        }
        let names = fonts.map { CTFontCopyPostScriptName($0) as String }
        var attributes: [NSAttributedString.Key: Any] = [
            NSAttributedString.Key(kCTFontAttributeName as String): fonts[0],
        ]
        if let language = language, !language.isEmpty {
            attributes[NSAttributedString.Key(kCTLanguageAttributeName as String)] = language
        }
        let attributed = NSMutableAttributedString(string: text, attributes: attributes)
        // Each character in the first font that has it, else in the first
        let utf16 = Array(text.utf16)
        var i = 0
        while i < utf16.count {
            let n = UTF16.isLeadSurrogate(utf16[i]) && i + 1 < utf16.count ? 2 : 1
            for k in 1..<max(fonts.count, 1) {
                var characters = Array(utf16[i..<(i + n)])
                var glyphs = [CGGlyph](repeating: 0, count: n)
                if CTFontGetGlyphsForCharacters(fonts[0], &characters, &glyphs, n) {
                    break
                }
                if CTFontGetGlyphsForCharacters(fonts[k], &characters, &glyphs, n) {
                    attributed.addAttribute(NSAttributedString.Key(kCTFontAttributeName as String),
                            value: fonts[k], range: NSRange(location: i, length: n))
                    break
                }
            }
            i += n
        }
        let typesetter = CTTypesetterCreateWithAttributedString(attributed as CFAttributedString)

        var lines = [ShapedLine]()
        var start = 0
        while start < utf16.count {
            let count = CTTypesetterSuggestLineBreak(typesetter, start, Double(width))
            if count <= 0 {
                break
            }
            let line = CTTypesetterCreateLine(typesetter, CFRange(location: start, length: count))
            var fontOf = [Int]()
            var glyphs = [Int]()
            var positions = [Float]()
            for run in (CTLineGetGlyphRuns(line) as? [CTRun]) ?? [] {
                let n = CTRunGetGlyphCount(run)
                if n == 0 {
                    continue
                }
                // A run in a font Core Text fell back to, of the system's, is not drawn
                var index = -1
                let runAttributes = CTRunGetAttributes(run) as NSDictionary
                if let value = runAttributes[kCTFontAttributeName as String] {
                    let name = CTFontCopyPostScriptName(value as! CTFont) as String
                    index = names.firstIndex(of: name) ?? -1
                }
                var runGlyphs = [CGGlyph](repeating: 0, count: n)
                var points = [CGPoint](repeating: .zero, count: n)
                CTRunGetGlyphs(run, CFRange(location: 0, length: 0), &runGlyphs)
                CTRunGetPositions(run, CFRange(location: 0, length: 0), &points)
                for k in 0..<n {
                    fontOf.append(index)
                    glyphs.append(Int(runGlyphs[k]))
                    positions.append(Float(points[k].x))
                    positions.append(-Float(points[k].y))
                }
            }
            // The width of the line without the white space at its end
            let visible = Float(CTLineGetTypographicBounds(line, nil, nil, nil)
                    - CTLineGetTrailingWhitespaceWidth(line))
            lines.append(ShapedLine(start, start + count, fontOf, glyphs, positions, visible))
            start += count
        }
        return lines
    }
}   // End of CoreTextShaper.swift
#endif
