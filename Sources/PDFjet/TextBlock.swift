/**
 * TextBlock.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A block of text that wraps at its width, with an optional border, background and padding.
public class TextBlock : Drawable {
    internal var x: Float = 0.0
    internal var y: Float = 0.0
    internal var width: Float = 500.0
    internal var height: Float = 0.0
    internal var font: Font
    internal var fallbackFont: Font?
    internal var fontSize: Float = 12.0
    internal var textContent: String
    internal var textPadding: Float = 0.0

    private var fillColor: [Float]?
    private var textColor: [Float] = [0.0, 0.0, 0.0]
    private var borderColor: [Float]?
    private var borderWidth: Float = 0.5
    private var borderCornerRadius: Float = 0.0

    private var language: String?
    private var uri: String?
    private var textAlignment: Alignment = Alignment.LEFT
    private var underline: Bool = false
    private var rightToLeft: Bool = false

    private var lineSpacing: Float = 1.0

    private var keywordHighlightColors: [String: Int32]?

    /// Creates a text block with the specified font and text. The font is the fallback font too.
    public init(_ font: Font, _ textContent: String) {
        self.font = font
        self.fallbackFont = font
        self.fontSize = font.size
        self.textContent = textContent
    }

    /// Sets the font of the text. It also becomes the fallback font.
    @discardableResult
    public func setFont(_ font: Font) -> TextBlock {
        self.font = font
        self.fallbackFont = font
        return self
    }

    /// Sets the font used for characters the main font does not have.
    @discardableResult
    public func setFallbackFont(_ font: Font?) -> TextBlock {
        self.fallbackFont = font
        return self
    }

    /// Sets the font size of the text.
    @discardableResult
    public func setFontSize(_ size: Float) -> TextBlock {
        self.fontSize = size
        return self
    }

    /// Sets the size of the fallback font.
    @discardableResult
    public func setFallbackFontSize(_ size: Float) -> TextBlock {
        fallbackFont?.setSize(size)
        return self
    }

    /// Sets the text.
    @discardableResult
    public func setText(_ text: String) -> TextBlock {
        self.textContent = text
        return self
    }

    /// Returns the font of the text.
    public func getFont() -> Font {
        return font
    }

    /// Returns the text.
    public func getText() -> String {
        return textContent
    }

    /// Sets the location of the top left corner of this text block.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the size of this text block.
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> TextBlock {
        self.width = w
        self.height = h
        return self
    }

    /// Sets the width of this text block and resets its height, so the height fits the text.
    @discardableResult
    public func setWidth(_ w: Float) -> TextBlock {
        self.width = w
        self.height = 0.0
        return self
    }

    /// Sets the height of this text block.
    @discardableResult
    public func setHeight(_ h: Float) -> TextBlock {
        self.height = h
        return self
    }

    /// Returns the width of this text block.
    public func getWidth() -> Float {
        return self.width
    }

    /// Returns the height of this text block.
    public func getHeight() -> Float {
        return self.height
    }

    /// Sets the radius of the border corners.
    @discardableResult
    public func setBorderCornerRadius(_ radius: Float) -> TextBlock {
        self.borderCornerRadius = radius
        return self
    }

    /// Sets the space between the text and the border.
    @discardableResult
    public func setTextPadding(_ padding: Float) -> TextBlock {
        self.textPadding = padding
        return self
    }

    /// Sets the border width.
    @discardableResult
    public func setBorderWidth(_ borderWidth: Float) -> TextBlock {
        self.borderWidth = borderWidth
        return self
    }

    /// Sets the text color as a 0xRRGGBB value.
    @discardableResult
    public func setTextColor(_ color: Int32) -> TextBlock {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.textColor = [r, g, b]
        return self
    }

    /// Sets the text color from an array of red, green and blue values.
    @discardableResult
    public func setTextColor(_ textColor: [Float]) -> TextBlock {
        self.textColor = textColor
        return self
    }

    /// Sets the text color from red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setTextColor(_ r: Float, _ g: Float, _ b: Float) -> TextBlock {
        self.textColor = [r, g, b]
        return self
    }

    /// Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.
    @discardableResult
    public func setFillColor(_ color: Int32) -> TextBlock {
        if color == Color.transparent {
            self.fillColor = nil
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.fillColor = [r, g, b]
        return self
    }

    /// Sets the background color from an array of red, green and blue values, or nil for no background.
    @discardableResult
    public func setFillColor(_ fillColor: [Float]?) -> TextBlock {
        self.fillColor = fillColor
        return self
    }

    /// Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.
    @discardableResult
    public func setBackgroundColor(_ color: Int32) -> TextBlock {
        return setFillColor(color)
    }

    /// Sets the background color from an array of red, green and blue values, or nil for no background.
    @discardableResult
    public func setBackgroundColor(_ backgroundColor: [Float]?) -> TextBlock {
        return setFillColor(backgroundColor)
    }

    /// Returns the background color, or nil if there is none.
    public func getBackgroundColor() -> [Float]? {
        return self.fillColor
    }

    /// Sets the border color as a 0xRRGGBB value. Color.transparent removes the border.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> TextBlock {
        if color == Color.transparent {
            self.borderColor = nil
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.borderColor = [r, g, b]
        return self
    }

    /// Sets the border color from an array of red, green and blue values, or nil for no border.
    @discardableResult
    public func setBorderColor(_ borderColor: [Float]?) -> TextBlock {
        self.borderColor = borderColor
        return self
    }

    ///
    /// Sets the colors used to highlight the specified keywords.
    /// The keywords are matched ignoring case.
    ///
    /// - Parameter map: the keyword to color map.
    ///
    @discardableResult
    public func setKeywordHighlightColors(_ map: [String: Int32]) -> TextBlock {
        var colors = [String: Int32]()
        for (key, value) in map {
            colors[key.lowercased()] = value
        }
        self.keywordHighlightColors = colors
        return self
    }

    /// Sets the line spacing as a multiple of the font's body height.
    @discardableResult
    public func setLineSpacing(_ lineSpacing: Float) -> TextBlock {
        self.lineSpacing = lineSpacing
        return self
    }

    /// Sets the horizontal alignment of the text.
    @discardableResult
    public func setTextAlignment(_ alignment: Alignment) -> TextBlock {
        self.textAlignment = alignment
        return self
    }

    /// Sets the language of the text, for example "he", "ar" or "fa", as a BCP 47
    /// language tag. The text is marked with it, for screen readers and text
    /// extraction.
    @discardableResult
    public func setLanguage(_ language: String?) -> TextBlock {
        self.language = language
        return self
    }

    /// Sets whether the text is right to left, like Arabic and Hebrew text.
    /// Each paragraph is wrapped at the width in logical order, and each line
    /// is then reordered with Bidi.reorderVisually, which also shapes the
    /// Arabic letters, and aligned to the right, unless the text alignment is
    /// Alignment.CENTER.
    @discardableResult
    public func setRightToLeft(_ rightToLeft: Bool) -> TextBlock {
        self.rightToLeft = rightToLeft
        return self
    }

    private func getTextLines() -> [TextLine] {
        var textLines = [TextLine]()

        let textAreaWidth = self.width - 2 * self.textPadding
        // Like String.split in Java: the trailing empty lines are dropped, but
        // an empty text is one empty line.
        var lines = textContent.replacingOccurrences(of: "\r\n", with: "\n")
                .components(separatedBy: "\n")
        while lines.count > 1 && lines[lines.count - 1].isEmpty {
            lines.removeLast()
        }
        for line in lines {
            if rightToLeft {
                addRightToLeftLines(&textLines, line, textAreaWidth)
                continue
            }
            // A zero width space marks a place where the line may break in text
            // without spaces between its words, like Thai text. It is not drawn.
            let text = line.replacingOccurrences(of: "\u{200B}", with: "")
            if font.stringWidth(fallbackFont, fontSize, text) <= textAreaWidth {
                textLines.append(TextLine(font, text))
            } else {
                if text.isCJK() {
                    var sb = ""
                    for scalar in text.unicodeScalars {
                        if font.stringWidth(fallbackFont, fontSize, sb + String(scalar)) <= textAreaWidth {
                            sb.unicodeScalars.append(scalar)
                        } else {
                            if !sb.isEmpty {    // Don't emit an empty line
                                textLines.append(TextLine(font, sb))
                            }
                            sb = String(scalar)
                        }
                    }
                    if !sb.trim().isEmpty {
                        textLines.append(TextLine(font, sb.trim()))
                    }
                } else {
                    var sb = ""
                    for token in line.splitOnWhitespace() {
                        // The words between the zero width spaces of a token
                        // are joined with no space.
                        let words = token.components(separatedBy: "\u{200B}")
                        for (i, word) in words.enumerated() {
                            let separator = (i == words.count - 1) ? " " : ""
                            if font.stringWidth(fallbackFont, fontSize, sb + word) <= textAreaWidth {
                                sb.append(word)
                                sb.append(separator)
                            } else {
                                if !sb.isEmpty {
                                    textLines.append(TextLine(font, sb.trim()))
                                    sb = ""
                                }
                                // A word too wide for a line by itself is broken.
                                let rest = addBrokenWordLines(&textLines, word, textAreaWidth)
                                if rest < word.unicodeScalars.count {
                                    sb = TextBlock.string(Array(word.unicodeScalars)[rest...]) + separator
                                }
                            }
                        }
                    }
                    if !sb.trim().isEmpty {
                        textLines.append(TextLine(font, sb.trim()))
                    }
                }
            }
        }

        return textLines
    }

    /// Adds the lines of a word too wide for a line by itself, broken between its
    /// characters, and returns the rest of the word, which fits on a line. No
    /// line starts with a combining mark, or with a Thai or Lao vowel or sign
    /// written after its consonant, and none ends with a Thai or Lao vowel
    /// written before its consonant.
    private func addBrokenWordLines(
            _ textLines: inout [TextLine], _ word: String, _ textAreaWidth: Float) -> Int {
        let scalars = Array(word.unicodeScalars)
        var start = 0
        while lineWidth(word, start) > textAreaWidth {
            // Each line gets at least one character, however narrow the block.
            var end = TextBlock.nextCharacterBreak(scalars, start)
            // A single cluster wider than the line is the whole word.
            var next = end < scalars.count ? TextBlock.nextCharacterBreak(scalars, end) : end
            while next < scalars.count && lineWidth(TextBlock.string(scalars[..<next]), start) <= textAreaWidth {
                end = next
                next = TextBlock.nextCharacterBreak(scalars, end)
            }
            textLines.append(newTextLine(TextBlock.string(scalars[..<end]), start))
            start = end
        }
        return start
    }

    // Returns the part of the text from the scalar offset on, reordered and
    // shaped in the context of the whole text if the text is right to left.
    private func part(_ text: String, _ from: Int, _ to: Int) -> String {
        if rightToLeft {
            return Bidi.reorderVisually(text, from, to)
        }
        return TextBlock.string(Array(text.unicodeScalars)[from..<to])
    }

    // Returns the width of a line of text, measured after the line is reordered
    // if the text is right to left.
    private func lineWidth(_ text: String, _ from: Int) -> Float {
        return font.stringWidth(fallbackFont, fontSize, part(text, from, text.unicodeScalars.count))
    }

    // Returns a line of text, reordered if the text is right to left.
    private func newTextLine(_ text: String, _ from: Int) -> TextLine {
        let scalars = Array(text.unicodeScalars)
        var to = scalars.count
        while to > from && String.isJavaWhitespace(scalars[to - 1]) {
            to -= 1
        }
        return TextLine(font, part(text, from, to))
    }

    private static func nextCharacterBreak(_ scalars: [Unicode.Scalar], _ start: Int) -> Int {
        var ch = scalars[start]
        var i = start + 1
        while isLeadingVowel(ch.value) && i < scalars.count {
            ch = scalars[i]
            i += 1
        }
        while i < scalars.count && staysWithPrevious(scalars[i]) {
            i += 1
        }
        return i
    }

    // The Thai and Lao vowels written before the consonant they follow in speech.
    private static func isLeadingVowel(_ ch: UInt32) -> Bool {
        return (ch >= 0x0E40 && ch <= 0x0E44) || (ch >= 0x0EC0 && ch <= 0x0EC4)
    }

    // The combining marks, and the Thai and Lao vowels and signs written after
    // a consonant, like SARA AA and MAI YAMOK, which do not start a line.
    private static func staysWithPrevious(_ ch: Unicode.Scalar) -> Bool {
        if ch.value == 0x200C || ch.value == 0x200D {   // ZWNJ, ZWJ
            return true
        }
        switch ch.properties.generalCategory {
        case .nonspacingMark, .spacingMark, .enclosingMark:
            return true
        default:
            let v = ch.value
            return (v >= 0x0E2F && v <= 0x0E3A) || (v >= 0x0E45 && v <= 0x0E4E) ||
                    (v >= 0x0EAF && v <= 0x0EBC) || (v >= 0x0EC6 && v <= 0x0ECE)
        }
    }

    private static func string(_ scalars: ArraySlice<Unicode.Scalar>) -> String {
        var str = ""
        str.unicodeScalars.append(contentsOf: scalars)
        return str
    }

    /// Wraps a paragraph of right to left text at the spaces between words and
    /// at its zero width spaces, and adds its lines in visual order. The
    /// paragraph is wrapped in logical order, so its first words go on the
    /// first line, and each line is measured after it is reordered, since the
    /// shaped Arabic letters differ in width from the letters they replace. A
    /// word too wide for a line by itself is broken between its characters.
    private func addRightToLeftLines(
            _ textLines: inout [TextLine], _ paragraph: String, _ textAreaWidth: Float) {
        // sb holds the words of the line in logical order. When the line starts
        // with the rest of a word broken over the lines, sb holds the whole word
        // and from is the scalar offset where the rest starts: the part before
        // it is not drawn, but it is the context that gives the first letter
        // of the rest its joined form.
        var sb = ""
        var from = 0
        for token in paragraph.splitOnWhitespace() {
            // The words between the zero width spaces of a token are joined
            // with no space.
            let words = token.components(separatedBy: "\u{200B}")
            for (i, word) in words.enumerated() {
                let separator = (i == words.count - 1) ? " " : ""
                if lineWidth(sb + word, from) <= textAreaWidth {
                    sb.append(word)
                    sb.append(separator)
                } else {
                    if sb.unicodeScalars.count > from {
                        textLines.append(newTextLine(sb, from))
                        sb = ""
                        from = 0
                    }
                    // A word too wide for a line by itself is broken.
                    let rest = addBrokenWordLines(&textLines, word, textAreaWidth)
                    if rest < word.unicodeScalars.count {
                        sb = word + separator
                        from = rest
                    }
                }
            }
        }
        textLines.append(newTextLine(sb, from))
    }

    /// Sets the URI opened when this text block is clicked.
    @discardableResult
    public func setURIAction(_ uri: String?) -> TextBlock {
        self.uri = uri
        return self
    }

    ///
    /// Underlines the text of this text block.
    ///
    /// - Parameter underline: the underline flag.
    ///
    @discardableResult
    public func setUnderline(_ underline: Bool) -> TextBlock {
        self.underline = underline
        return self
    }

    // The offsets are from the left edge of the text, inside the padding.
    private func rightAlignText(_ textLines: [TextLine]) {
        let textAreaWidth = self.width - 2 * self.textPadding
        for textLine in textLines {
            textLine.xOffset = textAreaWidth - font.stringWidth(fallbackFont, fontSize, textLine.text)
        }
    }

    private func centerText(_ textLines: [TextLine]) {
        let textAreaWidth = self.width - 2 * self.textPadding
        for textLine in textLines {
            textLine.xOffset = (textAreaWidth - font.stringWidth(fallbackFont, fontSize, textLine.text)) / 2.0
        }
    }

    private func underlineText(_ textLines: [TextLine]) {
        for textLine in textLines {
            textLine.underline = true
        }
    }

    /// Draws this text block on the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let ascent = font.getAscent(fontSize)
        let descent = font.getDescent(fontSize)
        let leading = (ascent + descent) * lineSpacing
        let textLines = getTextLines()
        let blockHeight = max(height, Float(textLines.count) * leading + 2 * textPadding)
        if page == nil {
            return [x + width, y + blockHeight]
        }

        page!.saveGraphicsState()

        page!.setPenWidth(self.borderWidth)
        if textAlignment == Alignment.CENTER {
            centerText(textLines)
        } else if textAlignment == Alignment.RIGHT || rightToLeft {
            rightAlignText(textLines)
        }
        if underline {
            underlineText(textLines)
        }

        if self.borderColor != nil || self.fillColor != nil {
            let rect = Rect(x, y, width, blockHeight)
            if self.borderColor != nil {
                rect.setBorderColor(self.borderColor)
                rect.setBorderWidth(self.borderWidth)
                rect.setCornerRadius(borderCornerRadius)
            }
            if self.fillColor != nil {
                rect.setFillColor(fillColor)
            }
            rect.drawOn(page)
        }

        page!.addBMC(StructElem.P, language, textContent, "")
        page!.drawTextBlock(
            font,
            fontSize,
            textLines,
            x + textPadding,
            y + textPadding,
            leading,
            textColor,
            keywordHighlightColors,
            language)
        page!.addEMC()

        page!.restoreGraphicsState()

        if uri != nil {
            page!.addAnnotation(Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + width,
                    y + blockHeight,
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Transparency
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    nil,    // The destination name
                    nil,
                    nil,
                    nil))
        }

        return [x + width, y + blockHeight]
    }
}
