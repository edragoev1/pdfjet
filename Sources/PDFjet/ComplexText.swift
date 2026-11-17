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
        var top = y
        for line in shapedLines {
            let lineText = String(decoding: utf16[line.start..<line.end], as: UTF16.self)
            let baseline = top + ascent
            top += height
            if lineText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
                continue
            }
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
                        places.append(x + line.positions[2 * i])
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

    private func lineHeight() -> Float {
        // The line's ascent and descent, and a quarter of the size between lines
        return font.getAscent() + font.getDescent() + 0.25 * font.getSize()
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
