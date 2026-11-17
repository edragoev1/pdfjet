/**
 * ComplexText.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Text in a script PDFjet does not shape itself, such as Hindi or Khmer,
/// wrapped to a width and drawn as simply as a TextBlock. The platform's own
/// text engine shapes it -- conjuncts, vowel signs, stacked consonants -- and
/// PDFjet draws the glyphs it found with Page.drawGlyphs: as outlines read
/// from the font file, sharp on screen and in print, each glyph drawn once and
/// reused.
///
///     let hindi = try Font(pdf, IBMPlexSansDevanagari.Regular)
///     let latin = try Font(pdf, IBMPlexSans.Regular)     // The English words
///     hindi.setSize(11.0)
///     let corner = ComplexText(hindi, text)
///             .setFallbackFont(latin)
///             .setLanguage("hi")
///             .setLocation(50.0, 100.0)
///             .setWidth(512.0)
///             .drawOn(page)
///
/// The engine: Apple's Core Text on iPhone, iPad and Mac; setShaper gives
/// another, as on Linux, which has none. Left-to-right scripts: Arabic and
/// Hebrew are TextLine's and TextBlock's.
///
/// In a tagged document, PDF/UA or a PDF/A of level A, the text is a
/// paragraph in its language, each line with its text, which screen readers
/// read. The text cannot be selected, copied or searched, as it is drawn as
/// outlines; a document that needs its exact text along can carry it with
/// PDF.attachText.
///
/// The fonts must be loaded from their font files, by path, as the fonts
/// PDFjet carries are, or from their bytes, as the engine reads the same file:
/// the glyph numbers it finds are those of that file.
///
public class ComplexText: Drawable {
    private let font: Font
    private var fallbackFont: Font?
    private let text: String
    private var language: String?
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var width: Float = 300.0
    private var textColor: Int32 = Color.black
    private var textAlignment = Alignment.LEFT
    private var lineSpacing: Float = 1.0
    private var structureType = StructElem.P
    private var shaper: Shaper?
    private var lines: [ShapedLine]?    // Shaped, until a font, the language or the width changes
    private var shapedSize: Float = 0.0 // The font's size the lines were shaped at

    ///
    /// Text to draw in the font.
    ///
    /// - Parameter font: the font, at its size, loaded from its font file.
    /// - Parameter text: the text.
    ///
    public init(_ font: Font, _ text: String) {
        self.font = font
        self.text = text
    }

    /// Sets the font of the characters the font does not have, such as
    /// English words in Hindi text, drawn at the font's size.
    @discardableResult
    public func setFallbackFont(_ font: Font) -> ComplexText {
        self.fallbackFont = font
        self.lines = nil
        return self
    }

    /// Sets the language of the text, such as "hi" or "km": the engine shapes
    /// by it, and a tagged document gives it to screen readers.
    @discardableResult
    public func setLanguage(_ language: String) -> ComplexText {
        self.language = language
        self.lines = nil
        return self
    }

    /// Sets the location of the top left corner of the text, in points.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the width the text is wrapped to; 300 points by default.
    @discardableResult
    public func setWidth(_ width: Float) -> ComplexText {
        self.width = width
        self.lines = nil
        return self
    }

    /// Sets the color of the text, as 0xRRGGBB.
    @discardableResult
    public func setTextColor(_ color: Int32) -> ComplexText {
        self.textColor = color
        return self
    }

    /// Sets the alignment of the lines: LEFT, the default, CENTER, RIGHT or
    /// JUSTIFY, which spreads the words of each line but the last to the width.
    @discardableResult
    public func setTextAlignment(_ textAlignment: Alignment) -> ComplexText {
        self.textAlignment = textAlignment
        return self
    }

    /// Sets the line spacing as a multiple of the font's body height, as
    /// TextBlock's: 1.0 by default.
    @discardableResult
    public func setLineSpacing(_ lineSpacing: Float) -> ComplexText {
        self.lineSpacing = lineSpacing
        return self
    }

    /// Sets the structure type of the text in a tagged document; P, a
    /// paragraph, by default.
    @discardableResult
    public func setStructureType(_ structureType: StructElem) -> ComplexText {
        self.structureType = structureType
        return self
    }

    /// Sets the text engine that shapes the text, in place of the platform's.
    @discardableResult
    public func setShaper(_ shaper: Shaper) -> ComplexText {
        self.shaper = shaper
        self.lines = nil
        return self
    }

    ///
    /// Returns the height of the text, wrapped to the width: shapes it, if it
    /// is not shaped yet.
    ///
    /// - Returns: the height, in points.
    /// - Throws: an error if the text cannot be shaped.
    ///
    public func getHeight() throws -> Float {
        return Float(try shaped().count) * lineHeight()
    }

    ///
    /// Draws the text, wrapped to the width, from its location. Text that
    /// cannot be shaped fails the document, as complete() says.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and the y of the bottom right corner of the text.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [x + width, y + ((try? getHeight()) ?? 0.0)]
        }
        let shapedLines: [ShapedLine]
        do {
            shapedLines = try shaped()
        } catch {
            page.pdf.fail((error as? PDFjetError)?.message ?? "\(error)")
            return [x + width, y]
        }
        var fonts = [font]
        if let fallbackFont = fallbackFont {
            fonts.append(fallbackFont)
        }
        let size = font.getSize()
        let ascent = font.getAscent()
        let height = lineHeight()
        let utf16 = Array(text.utf16)

        let own = page.pdf.isTagged() && !page.insideTaggedContent()
        page.setBrushColor(textColor)
        if own {
            page.addBDC(structureType, language, nil, nil)
        }
        // The space of each font, for justified lines
        let spaces = fonts.map { $0.unicodeToGID[0x20] }
        var top = y
        for (n, line) in shapedLines.enumerated() {
            let lineText = String(decoding: utf16[line.start..<line.end], as: UTF16.self)
            let baseline = top + ascent
            top += height
            if lineText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                continue
            }
            let shifts = self.shifts(line, n == shapedLines.count - 1, spaces)
            page.beginActualText(lineText, language)
            // The glyphs of each font, one run of them at a time
            var from = 0
            while from < line.glyphs.count {
                var to = from
                while to < line.glyphs.count && line.fonts[to] == line.fonts[from] {
                    to += 1
                }
                let index = line.fonts[from]
                if index >= 0 && index < fonts.count {
                    var run = [Int]()
                    var places = [Float]()
                    for i in from..<to {
                        run.append(line.glyphs[i])
                        places.append(x + line.positions[2 * i] + shifts[i])
                        places.append(baseline + line.positions[2 * i + 1])
                    }
                    if !page.glyphsDrawable(fonts[index], run, places) {
                        return [x + width, y]
                    }
                    page.placeGlyphs(fonts[index], size, run, places)
                }
                from = to
            }
            page.endActualText()
        }
        if own {
            page.addEMC()
        }
        return [x + width, top]
    }

    // The distance from a line to the next, as TextBlock's
    private func lineHeight() -> Float {
        return (font.getAscent() + font.getDescent() + font.getLineGap(font.getSize())) * lineSpacing
    }

    // How far each glyph of the line moves to the right, for its alignment:
    // all the same for CENTER and RIGHT; for JUSTIFY, each glyph by the room
    // given to the spaces before it, the last line left as it is.
    private func shifts(_ line: ShapedLine, _ last: Bool, _ spaces: [Int]) -> [Float] {
        var shifts = [Float](repeating: 0, count: line.glyphs.count)
        let room = width - line.width
        if line.width <= 0 || room <= 0 {
            return shifts
        }
        func isSpace(_ i: Int) -> Bool {
            let font = line.fonts[i]
            return font >= 0 && font < spaces.count && spaces[font] != 0 && line.glyphs[i] == spaces[font]
        }
        if textAlignment == Alignment.CENTER || textAlignment == Alignment.RIGHT {
            let shift = textAlignment == Alignment.CENTER ? room / 2 : room
            shifts = [Float](repeating: shift, count: line.glyphs.count)
        } else if textAlignment == Alignment.JUSTIFY && !last {
            // The spaces between the words: not those at the end of the line
            var end = line.glyphs.count
            while end > 0 && isSpace(end - 1) {
                end -= 1
            }
            let count = (0..<end).filter { isSpace($0) }.count
            if count > 0 {
                var shift: Float = 0
                for i in 0..<line.glyphs.count {
                    shifts[i] = shift
                    if i < end && isSpace(i) {
                        shift += room / Float(count)
                    }
                }
            }
        }
        return shifts
    }

    // The lines, shaped the first time they are needed
    private func shaped() throws -> [ShapedLine] {
        if let lines = lines, shapedSize == font.getSize() {
            return lines
        }
        var fonts = [font]
        if let fallbackFont = fallbackFont {
            fonts.append(fallbackFont)
        }
        var files = [[UInt8]]()
        for f in fonts {
            guard let file = try f.shapingFile() else {
                throw PDFjetError(message: "ComplexText needs fonts loaded from their font files, "
                        + "by path or from their bytes, as the text engine reads the same file.")
            }
            files.append(file)
        }
        shapedSize = font.getSize()
        let shaped = try currentShaper().shape(text, language, files, shapedSize, width)
        lines = shaped
        return shaped
    }

    private func currentShaper() throws -> Shaper {
        if let shaper = shaper {
            return shaper
        }
#if canImport(CoreText)
        return CoreTextShaper()
#else
        throw PDFjetError(message: "ComplexText needs a text engine to shape the text: "
                + "Core Text, on Apple's platforms, or a Shaper given with setShaper.")
#endif
    }
}   // End of ComplexText.swift
