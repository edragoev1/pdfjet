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
    internal var height: Float = 500.0
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

    private var language: String = "en-US"
    private var altDescription: String = ""
    private var uri: String?
    private var key: String?
    private var uriLanguage: String?
    private var uriActualText: String?
    private var uriAltDescription: String?
    private var textDirection: Direction = Direction.LEFT_TO_RIGHT
    private var textAlignment: Alignment = Alignment.LEFT
    private var underline: Bool = false
    private var strikeout: Bool = false

    private var lineSpacing: Float = 1.0

    private var highlightColors: [String: Int32]?

    /// Creates a text block with the specified font and text.
    public init(_ font: Font, _ textContent: String) {
        self.font = font
        self.fontSize = font.size
        self.textContent = textContent
    }

    /// Sets the font of the text.
    @discardableResult
    public func setFont(_ font: Font) -> TextBlock {
        self.font = font
        return self
    }

    /// Sets the font used for characters the main font does not have.
    @discardableResult
    public func setFallbackFont(_ font: Font) -> TextBlock {
        self.fallbackFont = font
        return self
    }

    /// Sets the size of the font. This changes the size of the Font object itself.
    @discardableResult
    public func setFontSize(_ size: Float) -> TextBlock {
        self.font.setSize(size)
        return self
    }

    /// Sets the size of the fallback font, if there is one.
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

    /// Sets the width of this text block and resets its height to 0.
    @discardableResult
    public func setWidth(_ w: Float) -> TextBlock {
        self.width = w
        self.height = 0.0
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

    /// Sets the background color as a 0xRRGGBB value.
    @discardableResult
    public func setFillColor(_ color: Int32) -> TextBlock {
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

    /// Sets the border color as a 0xRRGGBB value.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> TextBlock {
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

    /// Sets the colors used to highlight words in the text.
    @discardableResult
    public func setHighlightColors(_ highlightColors: [String: Int32]) -> TextBlock {
        self.highlightColors = highlightColors
        return self
    }

    ///
    /// Sets the colors used to highlight the specified keywords.
    ///
    /// - Parameter map: the keyword to color map.
    ///
    @discardableResult
    public func setKeywordHighlightColors(_ map: [String: Int32]) -> TextBlock {
        var colors = [String: Int32]()
        for (key, value) in map {
            colors[key.lowercased()] = value
        }
        self.highlightColors = colors
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

    private func textIsCJK(_ str: String) -> Bool {
        let chars = Array(str)
        var numOfCJK = 0
        for ch in chars {
            if (ch.unicodeScalars.first!.value >= 0x4E00 && ch.unicodeScalars.first!.value <= 0x9FD5) ||
                (ch.unicodeScalars.first!.value >= 0x3040 && ch.unicodeScalars.first!.value <= 0x309F) ||
                (ch.unicodeScalars.first!.value >= 0x30A0 && ch.unicodeScalars.first!.value <= 0x30FF) ||
                (ch.unicodeScalars.first!.value >= 0x1100 && ch.unicodeScalars.first!.value <= 0x11FF) {
                numOfCJK += 1
            }
        }
        return numOfCJK > chars.count / 2
    }

    private func getTextLines() -> [TextLine] {
        var textLines = [TextLine]()

        let textAreaWidth = self.width - 2 * self.textPadding
        // .newlines matches "\r" and "\n" separately, so Windows line endings
        // are replaced first, or each would split off an empty line.
        let lines = textContent.replacingOccurrences(of: "\r\n", with: "\n")
                .components(separatedBy: .newlines)
        for line in lines {
            if font.stringWidth(fallbackFont, line) <= textAreaWidth {
                textLines.append(TextLine(font, line))
            } else {
                if textIsCJK(line) {
                    var sb = ""
                    for ch in line {
                        if font.stringWidth(fallbackFont, sb + String(ch)) <= textAreaWidth {
                            sb.append(ch)
                        } else {
                            textLines.append(TextLine(font, sb))
                            sb = String(ch)
                        }
                    }
                    if !sb.isEmpty {
                        textLines.append(TextLine(font, sb))
                    }
                } else {
                    var sb = ""
                    let tokens = line.split(whereSeparator: \.isWhitespace).map(String.init)
                    for token in tokens {
                        if font.stringWidth(fallbackFont, sb + token) <= textAreaWidth {
                            sb.append(token)
                            sb.append(" ")
                        } else {
                            textLines.append(TextLine(font, sb.trim()))
                            sb = ""
                            sb = token + " "
                        }
                    }
                    if !sb.trim().isEmpty {
                        textLines.append(TextLine(font, sb.trim()))
                    }
                }
            }
        }
        // We need the following line to match the behaviour of the Java, .NET and Go versions.
        if textLines.last!.text!.isEmpty { textLines.removeLast() }

        return textLines
    }

    /// Sets the URI opened when this text block is clicked.
    @discardableResult
    public func setURIAction(_ uri: String) -> TextBlock {
        self.uri = uri
        return self
    }

    /// Sets the direction of the text.
    @discardableResult
    public func setTextDirection(_ direction: Direction) -> TextBlock {
        self.textDirection = direction
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

    private func rightAlignText(_ textLines: [TextLine]) {
        for textLine in textLines {
            textLine.xOffset = self.width - font.stringWidth(textLine.text)
        }
    }

    private func centerText(_ textLines: [TextLine]) {
        for textLine in textLines {
            textLine.xOffset = (self.width - font.stringWidth(textLine.text)) / 2.0
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
        if page == nil {
            return [width, max(height, Float(textLines.count) * leading + 2 * textPadding)]
        }

        page!.saveGraphicsState()

        page!.setPenWidth(self.borderWidth)
        if textAlignment == Alignment.RIGHT {
            rightAlignText(textLines)
        } else if textAlignment == Alignment.CENTER {
            centerText(textLines)
        }
        if underline {
            underlineText(textLines)
        }

        if self.borderColor != nil || self.fillColor != nil {
            let rect = Rect(
                x,
                y,
                width,
                max(height, Float(textLines.count) * leading + 2 * textPadding))
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
            highlightColors)
        page!.addEMC()

        page!.restoreGraphicsState()

        return [x + width, max(y + height, y + Float(textLines.count) * leading + 2 * textPadding)]
    }
}
