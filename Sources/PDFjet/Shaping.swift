/**
 *  Shaping.swift
 *
 *  Copyright (c) 2026 PDFjet Software
 *  Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// The shaping of text such as Hindi or Khmer, for TextBlock and TextLine:
// which text needs a shaper, which shaper, and the shaping, checked.
enum Shaping {
    // The shaper of TextBlock.setDefaultShaper, under its lock
    private final class DefaultShaper: @unchecked Sendable {
        private let lock = NSLock()
        private var shaper: Shaper?

        func get() -> Shaper? {
            lock.lock()
            defer { lock.unlock() }
            return shaper
        }

        func set(_ shaper: Shaper?) {
            lock.lock()
            defer { lock.unlock() }
            self.shaper = shaper
        }
    }

    private static let defaultShaperSet = DefaultShaper()

    static func setDefaultShaper(_ shaper: Shaper?) {
        defaultShaperSet.set(shaper)
    }

    // The shaper of TextBlock and TextLine when none is set: setDefaultShaper's,
    // else Core Text on Apple's platforms; else an error that says what to give
    static func defaultShaper() throws -> Shaper {
        if let shaper = defaultShaperSet.get() {
            return shaper
        }
#if canImport(CoreText)
        return CoreTextShaper()
#elseif os(Linux)
        // No text engine of its own; hb-shape only when asked for, so that a
        // program shapes the same wherever it runs (22 November 2026)
        throw PDFjetError(message: "Text such as Hindi or Khmer on Linux needs a shaper: "
                + ".setShaper(HBShape()) or TextBlock.setDefaultShaper(HBShape()), which runs HarfBuzz's "
                + "program hb-shape (Ubuntu: apt install libharfbuzz-bin; brew install harfbuzz), "
                + "or a Shaper of your own.")
#else
        throw PDFjetError(message: "Text such as Hindi or Khmer needs a text engine to shape the text: "
                + "Core Text, on Apple's platforms, or a Shaper given with setShaper.")
#endif
    }

    // Whether the text has a character of a script whose letters change
    // shape and place, which a shaper draws: Devanagari, Bengali, Gurmukhi,
    // Gujarati, Oriya, Tamil, Telugu, Kannada, Malayalam, Sinhala, Lao,
    // Myanmar and Khmer. Thai is not one: TextBlock draws it as text, its
    // lines broken by ICU's dictionary.
    static func needsShaper(_ text: String) -> Bool {
        for scalar in text.unicodeScalars {
            let c = scalar.value
            if c < 0x0900 {
                continue
            }
            if (c >= 0x0900 && c <= 0x0DFF)                                 // Devanagari to Sinhala
                    || (c >= 0x0E80 && c <= 0x0EFF)                         // Lao
                    || (c >= 0x1000 && c <= 0x109F)                         // Myanmar
                    || (c >= 0x1780 && c <= 0x17FF) || (c >= 0x19E0 && c <= 0x19FF)    // Khmer
                    || (c >= 0xA8E0 && c <= 0xA8FF)                         // Devanagari Extended
                    || (c >= 0xA9E0 && c <= 0xA9FF) || (c >= 0xAA60 && c <= 0xAA7F)    // Myanmar Extended
                    || (c >= 0x111E0 && c <= 0x111FF) {                     // Sinhala Archaic Numbers
                return true
            }
        }
        return false
    }

    // Shapes the text with the shaper, or the default one, in the fonts, at
    // the size, wrapped to the width; the lines checked, so that a shaper of
    // the program's own cannot make PDFjet draw past them. The lines' start
    // and end are in UTF-16 code units.
    static func shapeText(_ shaper: Shaper?, _ fonts: [Font], _ size: Float, _ language: String?,
            _ text: String, _ width: Float) throws -> [ShapedLine] {
        let engine = try shaper ?? defaultShaper()
        var files = [[UInt8]]()
        for font in fonts {
            guard let file = try font.shapingFile() else {
                throw PDFjetError(message: "Text such as Hindi or Khmer needs fonts loaded from their font "
                        + "files, by path or from their bytes, as the text engine reads the same file.")
            }
            files.append(file)
        }
        let lines = try engine.shape(ShapingRequest(text, language, files, size, width))
        let length = text.utf16.count
        for line in lines {
            if line.fontIndexes.count != line.glyphs.count || line.positions.count != 2 * line.glyphs.count
                    || line.start < 0 || line.start > line.end || line.end > length {
                throw PDFjetError(message: "A shaped line needs a font and an x and a y for each glyph, "
                        + "and its part of the text.")
            }
        }
        return lines
    }

    // The part of the text a shaped line holds, of the text's UTF-16 code
    // units, made once for all its lines
    static func text(_ utf16: [UInt16], _ line: ShapedLine) -> String {
        return String(decoding: utf16[line.start..<line.end], as: UTF16.self)
    }

    // The fonts text is shaped in: the font, and the fallback font for the
    // characters the font has not, when it has a file the text engine can
    // read; a fallback without one, as a core font, is left out, so that a
    // Latin fallback does not fail Hindi text.
    static func shapingFonts(_ font: Font, _ fallback: Font?) -> [Font] {
        if let fallback = fallback, fallback !== font, (try? fallback.shapingFile()) != nil {
            return [font, fallback]
        }
        return [font]
    }

    // Shapes the text as one line, not wrapped, with the default shaper: the
    // lines of a shaper that broke it anyway, joined.
    static func shapeOneLine(_ fonts: [Font], _ size: Float, _ language: String?, _ text: String) throws -> ShapedText {
        let lines = try shapeText(nil, fonts, size, language, text, Float.greatestFiniteMagnitude / 4)
        var joined: ShapedLine?
        for line in lines {
            guard let first = joined else {
                joined = line
                continue
            }
            let x0 = first.width
            var positions = first.positions
            for j in stride(from: 0, to: line.positions.count, by: 2) {
                positions.append(x0 + line.positions[j])
                positions.append(line.positions[j + 1])
            }
            joined = ShapedLine(first.start, line.end, first.fontIndexes + line.fontIndexes,
                    first.glyphs + line.glyphs, positions, x0 + line.width)
        }
        return ShapedText(joined ?? ShapedLine(0, text.utf16.count, [], [], [], 0), text, fonts, size)
    }

    // The message a document fails with, for an error a shaper threw
    static func message(_ error: Error) -> String {
        return (error as? PDFjetError)?.message ?? "\(error)"
    }
}

// What shaped text depends on besides its text: a cache of shaped text is
// good only for the style it was made in.
struct ShapingStyle: Equatable {
    let font: Font
    let fallback: Font?
    let size: Float
    let language: String?

    static func == (a: ShapingStyle, b: ShapingStyle) -> Bool {
        return a.font === b.font && a.fallback === b.fallback && a.size == b.size && a.language == b.language
    }
}

// A TextLine's cache of the texts it shaped, its own and its words', which
// the copies a TextFrame or a TextColumn draws share, so that each is shaped
// once, however often it is measured and drawn.
final class ShapedPieces {
    let style: ShapingStyle
    var shaped = [String: ShapedText]()

    init(_ style: ShapingStyle) {
        self.style = style
    }
}

// A line of TextBlock or TextLine a shaper shaped: its glyphs, drawn as
// outlines in its fonts, and its text, which tags them.
final class ShapedText {
    let line: ShapedLine
    let text: String
    let fonts: [Font]
    let size: Float

    init(_ line: ShapedLine, _ text: String, _ fonts: [Font], _ size: Float) {
        self.line = line
        self.text = text
        self.fonts = fonts
        self.size = size
    }
}
