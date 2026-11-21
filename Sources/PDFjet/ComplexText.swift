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
///     let hindi = try Font(pdf, NotoSansDevanagari.Regular)
///     let latin = try Font(pdf, IBMPlexSans.Regular)     // The English words
///     hindi.setSize(11.0)
///     let corner = ComplexText(hindi, text)
///             .setFallbackFont(latin)
///             .setLanguage("hi")
///             .setLocation(50.0, 100.0)
///             .setWidth(512.0)
///             .drawOn(page)
///
/// Paragraphs, given as a list, flow from page to page, as a TextFrame's:
/// with a height, drawOn draws the lines that fit and keeps the rest, a
/// paragraph cut where the height ends, for the next drawOn, on the next page.
///
///     let text = ComplexText(hindi, paragraphs)
///             .setFallbackFont(latin)
///             .setLanguage("hi")
///             .setWidth(512.0)
///             .setHeight(692.0)
///     while try text.hasMoreText() {
///         text.setLocation(50.0, 50.0).drawOn(Page(pdf, Letter.PORTRAIT))
///     }
///
/// The engine: Apple's Core Text on iPhone, iPad and Mac; setShaper gives
/// another, as on Linux, which has none. Left-to-right scripts: Arabic and
/// Hebrew are TextLine's and TextBlock's.
///
/// In a tagged document, PDF/UA or a PDF/A of level A, each paragraph is a
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
    private let paragraphs: [String]
    private var language: String?
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var width: Float = 300.0
    private var height: Float = 0.0         // 0: all of the text
    private var paragraphGap: Float = -1.0  // Less than 0: one empty line, as TextFrame's
    private var textColor: Int32 = Color.black
    private var textAlignment = Alignment.LEFT
    private var lineSpacing: Float = 1.0
    private var structureType = StructElem.P
    private var shaper: Shaper?

    // The paragraph drawn next, and its line drawn next
    private var paragraph = 0
    private var line = 0

    // The lines of each paragraph, shaped the first time they are needed,
    // until a font, the language or the width changes; and the font's size
    // they were shaped at
    private var shapedLines: [[ShapedLine]?]?
    private var shapedSize: Float = 0.0

    ///
    /// Text to draw in the font.
    ///
    /// - Parameter font: the font, at its size, loaded from its font file.
    /// - Parameter text: the text.
    ///
    public convenience init(_ font: Font, _ text: String) {
        self.init(font, [text])
    }

    ///
    /// Paragraphs to draw in the font, one after the other.
    ///
    /// - Parameter font: the font, at its size, loaded from its font file.
    /// - Parameter paragraphs: the paragraphs.
    ///
    public init(_ font: Font, _ paragraphs: [String]) {
        self.font = font
        self.paragraphs = paragraphs
    }

    /// Sets the font of the characters the font does not have, such as
    /// English words in Hindi text, drawn at the font's size.
    @discardableResult
    public func setFallbackFont(_ font: Font) -> ComplexText {
        self.fallbackFont = font
        self.shapedLines = nil
        return self
    }

    /// Sets the language of the text, such as "hi" or "km": the engine shapes
    /// by it, and a tagged document gives it to screen readers.
    @discardableResult
    public func setLanguage(_ language: String) -> ComplexText {
        self.language = language
        self.shapedLines = nil
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
        if width != self.width {
            self.width = width
            self.shapedLines = nil
        }
        return self
    }

    /// Sets the height the text is drawn in: drawOn draws the lines that fit
    /// and keeps the rest for the next drawOn. With a height of 0, the
    /// default, drawOn draws all of the text.
    @discardableResult
    public func setHeight(_ height: Float) -> ComplexText {
        self.height = height
        return self
    }

    /// Sets the space between paragraphs, from the bottom of a paragraph's
    /// last line to the top of the next one's first; one empty line by
    /// default, as TextFrame's. A negative gap is taken as 0.
    @discardableResult
    public func setParagraphGap(_ paragraphGap: Float) -> ComplexText {
        self.paragraphGap = max(0, paragraphGap)
        return self
    }

    /// Sets the color of the text, as 0xRRGGBB.
    @discardableResult
    public func setTextColor(_ color: Int32) -> ComplexText {
        self.textColor = color
        return self
    }

    /// Sets the alignment of the lines: LEFT, the default, CENTER, RIGHT or
    /// JUSTIFY, which spreads the words of each line but the last of a
    /// paragraph to the width.
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

    /// Sets the structure type of each paragraph in a tagged document; P, a
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
        self.shapedLines = nil
        return self
    }

    ///
    /// Returns true if some of the text has not been drawn yet.
    ///
    /// - Throws: an error if the text cannot be shaped.
    ///
    public func hasMoreText() throws -> Bool {
        var p = paragraph
        while p < paragraphs.count {
            if try shaped(p).count > (p == paragraph ? line : 0) {
                return true
            }
            p += 1
        }
        return false
    }

    /// Returns the height set with setHeight, as TextFrame's; 0 for none.
    public func getHeight() -> Float {
        return height
    }

    ///
    /// Returns the height of the text not drawn yet, wrapped to the width,
    /// whatever the height set: shapes it, if it is not shaped yet.
    ///
    /// - Returns: the height, in points.
    /// - Throws: an error if the text cannot be shaped.
    ///
    public func getTextHeight() throws -> Float {
        var total: Float = 0
        var first = true
        var p = paragraph
        while p < paragraphs.count {
            let lines = try shaped(p).count - (p == paragraph ? line : 0)
            if lines > 0 {
                total += (first ? 0 : gap()) + Float(lines) * lineHeight()
                first = false
            }
            p += 1
        }
        return total
    }

    ///
    /// Draws the text not drawn yet, wrapped to the width, from its location:
    /// all of it, or with a height, the lines that fit, the rest kept for the
    /// next drawOn. Text that cannot be shaped fails the document, as
    /// complete() says. Without a page, measures them instead: the corner it
    /// returns is the one drawOn would, and nothing is drawn or kept, so that
    /// a Table cell can hold it, as it holds a TextBlock.
    ///
    /// - Parameter page: the page to draw on, or nil to measure.
    /// - Returns: the x and the y of the bottom right corner of the text drawn.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return measure()
        }
        let fonts = self.fonts()
        let size = font.getSize()
        let ascent = font.getAscent()
        let lineHeight = self.lineHeight()
        let bottom = height > 0 ? y + height : Float.greatestFiniteMagnitude
        // The space of each font, for justified lines
        let spaces = fonts.map { $0.unicodeToGID[0x20] }

        page.setBrushColor(textColor)
        var top = y
        var drawn = false       // A line drawn by this drawOn
        while paragraph < paragraphs.count {
            let lines: [ShapedLine]
            do {
                lines = try shaped(paragraph)
            } catch {
                page.pdf.fail((error as? PDFjetError)?.message ?? "\(error)")
                return [x + width, top]
            }
            if line >= lines.count {
                paragraph += 1
                line = 0
                continue
            }
            let gap = drawn && line == 0 ? self.gap() : 0
            // At least one line each drawOn, so that text in a height too
            // small for a line still ends
            if drawn && top + gap + lineHeight > bottom {
                break
            }
            top += gap
            let utf16 = Array(paragraphs[paragraph].utf16)
            let own = page.pdf.isTagged() && !page.insideTaggedContent()
            if own {
                page.addBDC(structureType, language, nil, nil)
            }
            while line < lines.count && (!drawn || top + lineHeight <= bottom) {
                let last = line == lines.count - 1
                if !drawLine(page, lines[line], utf16, top + ascent, last, fonts, spaces, size) {
                    return [x + width, top]
                }
                top += lineHeight
                line += 1
                drawn = true
            }
            if own {
                page.addEMC()
            }
            if line < lines.count {
                break       // The height is full
            }
            paragraph += 1
            line = 0
        }
        return [x + width, top]
    }

    // The corner drawOn would return, the lines it would draw counted as it
    // counts them, from the paragraph and the line drawn next, which stay.
    // Text that cannot be shaped measures to where it stops, drawOn failing
    // the document then.
    private func measure() -> [Float] {
        let lineHeight = self.lineHeight()
        let bottom = height > 0 ? y + height : Float.greatestFiniteMagnitude
        var top = y
        var drawn = false
        var p = paragraph
        var l = line
        while p < paragraphs.count {
            guard let lines = try? shaped(p) else {
                break
            }
            if l >= lines.count {
                p += 1
                l = 0
                continue
            }
            let gap = drawn && l == 0 ? self.gap() : 0
            if drawn && top + gap + lineHeight > bottom {
                break
            }
            top += gap
            while l < lines.count && (!drawn || top + lineHeight <= bottom) {
                top += lineHeight
                l += 1
                drawn = true
            }
            if l < lines.count {
                break
            }
            p += 1
            l = 0
        }
        return [x + width, top]
    }

    // Back to the start of the text, as a Table cell draws it whole each time
    func rewind() {
        paragraph = 0
        line = 0
    }

    // Draws the line at its baseline: the glyphs of each font, one run of
    // them at a time, tagged with the line's text; false if the document failed.
    private func drawLine(_ page: Page, _ line: ShapedLine, _ utf16: [UInt16], _ baseline: Float,
            _ last: Bool, _ fonts: [Font], _ spaces: [Int], _ size: Float) -> Bool {
        let lineText = String(decoding: utf16[line.start..<line.end], as: UTF16.self)
        if lineText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            return true
        }
        let shifts = self.shifts(line, last, spaces)
        page.beginActualText(lineText, language)
        var from = 0
        while from < line.glyphs.count {
            var to = from
            while to < line.glyphs.count && line.fontIndexes[to] == line.fontIndexes[from] {
                to += 1
            }
            let index = line.fontIndexes[from]
            if index >= 0 && index < fonts.count {
                var run = [Int]()
                var places = [Float]()
                for i in from..<to {
                    run.append(line.glyphs[i])
                    places.append(x + line.positions[2 * i] + shifts[i])
                    places.append(baseline + line.positions[2 * i + 1])
                }
                if !page.glyphsDrawable(fonts[index], run, places) {
                    return false
                }
                page.placeGlyphs(fonts[index], size, run, places)
            }
            from = to
        }
        page.endActualText()
        return true
    }

    private func fonts() -> [Font] {
        var fonts = [font]
        if let fallbackFont = fallbackFont {
            fonts.append(fallbackFont)
        }
        return fonts
    }

    // The distance from a line to the next, as TextBlock's
    private func lineHeight() -> Float {
        return (font.getAscent() + font.getDescent() + font.getLineGap(font.getSize())) * lineSpacing
    }

    // One empty line by default, as TextFrame's: the ascent and the descent,
    // and the line gap on each side
    private func gap() -> Float {
        if paragraphGap >= 0 {
            return paragraphGap
        }
        return font.getAscent() + font.getDescent() + 2 * font.getLineGap(font.getSize())
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
            let font = line.fontIndexes[i]
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

    // The lines of the paragraph, shaped the first time they are needed
    private func shaped(_ p: Int) throws -> [ShapedLine] {
        if shapedLines == nil || shapedSize != font.getSize() {
            shapedLines = [[ShapedLine]?](repeating: nil, count: paragraphs.count)
            shapedSize = font.getSize()
        }
        if let lines = shapedLines![p] {
            return lines
        }
        var files = [[UInt8]]()
        for f in fonts() {
            guard let file = try f.shapingFile() else {
                throw PDFjetError(message: "ComplexText needs fonts loaded from their font files, "
                        + "by path or from their bytes, as the text engine reads the same file.")
            }
            files.append(file)
        }
        let lines = try currentShaper().shape(ShapingRequest(paragraphs[p], language, files, shapedSize, width))
        shapedLines![p] = lines
        return lines
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
