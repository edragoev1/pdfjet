/**
 * TextBox.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A box containing line-wrapped text.
///
/// Please see Example_19 and Example_30.
///
public class TextBox : Drawable {
    var font: Font
    var fallbackFont: Font?
    var fontSize: Float = 12.0
    var text: String?
    var x: Float = 0.0
    var y: Float = 0.0
    var width: Float = 300.0
    var height: Float = 0.0
    var spacing: Float = 0.0
    var margin: Float = 0.0
    var lineWidth: Float = 0.0

    private var fillColor: [Float]?         // The background fill color
    private var textColor: [Float] = [0.0, 0.0, 0.0]
    private var strokeWidth: Float = 0.5
    private var strokeColor: [Float]?

    private var valign = Align.TOP
    private var colors: [String : Int32]?
    // TextBox properties
    // Border:
    // bit 16 - top
    // bit 17 - bottom
    // bit 18 - left
    // bit 19 - right
    // Text Alignment:
    // bit 20
    // bit 21
    // Text Decoration:
    // bit 22 - underline
    // bit 23 - strikeout
    private var properties: UInt32 = 0x00000001
    private var language: String = "en-US"
    private var altDescription: String? = ""
    private var uri: String?
    private var key: String?
    private var uriLanguage: String?
    private var uriActualText: String?
    private var uriAltDescription: String?
    private var textDirection = Direction.LEFT_TO_RIGHT

    ///
    /// Creates a text box and sets the font.
    ///
    /// - Parameter font the font.
    ///
    public init(_ font: Font) {
        self.font = font
        self.fontSize = font.getSize()
    }

    ///
    /// Creates a text box and sets the font and the text.
    ///
    /// - Parameter font the font.
    /// - Parameter text the text.
    ///
    public init(_ font: Font, _ text: String?) {
        self.font = font
        self.text = text
        self.fontSize = font.getSize()
    }

    ///
    /// Creates a text box and sets the font, text, width and height.
    ///
    public init(_ font: Font, _ text: String?, _ width: Float, _ height: Float) {
        self.font = font
        self.text = text
        self.width = width
        self.height = height
        self.fontSize = font.getSize()
    }

    /// Sets the font and takes the font size from it.
    @discardableResult
    public func setFont(_ font: Font) -> TextBox {
        self.font = font
        self.fontSize = font.getSize()
        return self
    }

    /// Returns the font.
    public func getFont() -> Font {
        return self.font
    }

    /// Sets the font size.
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> TextBox {
        self.fontSize = fontSize
        return self
    }

    /// Sets the text.
    @discardableResult
    public func setText(_ text: String?) -> TextBox {
        self.text = text
        return self
    }

    /// Returns the text.
    public func getText() -> String? {
        return self.text
    }

    /// Sets the size of this text box.
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> TextBox {
        self.width = w
        self.height = h
        return self
    }

    /// Sets the location of the top left corner of this text box.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Returns the x and y coordinates of the top left corner of this text box.
    public func getLocation() -> [Float] {
        return [self.x, self.y]
    }

    /// Sets the width of this text box.
    @discardableResult
    public func setWidth(_ width: Float) -> TextBox {
        self.width = width
        return self
    }

    /// Returns the width of this text box.
    public func getWidth() -> Float {
        return self.width
    }

    /// Sets the height of this text box.
    @discardableResult
    public func setHeight(_ height: Float) -> TextBox {
        self.height = height
        return self
    }

    /// Returns the height of this text box.
    public func getHeight() -> Float {
        return self.height
    }

    /// Sets the margin between the text and the border.
    @discardableResult
    public func setMargin(_ margin: Float) -> TextBox {
        self.margin = margin
        return self
    }

    /// Returns the margin between the text and the border.
    public func getMargin() -> Float {
        return self.margin
    }

    /// Sets the width of the border lines.
    @discardableResult
    public func setLineWidth(_ lineWidth: Float) -> TextBox {
        self.lineWidth = lineWidth
        return self
    }

    /// Returns the width of the border lines.
    public func getLineWidth() -> Float {
        return self.lineWidth
    }

    /// Sets the spacing between lines of text.
    @discardableResult
    public func setSpacing(_ spacing: Float) -> TextBox {
        self.spacing = spacing
        return self
    }

    /// Returns the spacing between lines of text.
    public func getSpacing() -> Float {
        return self.spacing
    }

    /// Sets the background color as a 0xRRGGBB value.
    @discardableResult
    public func setFillColor(_ color: Int32) -> TextBox {
        self.fillColor = colorArray(color)
        return self
    }

    /// Sets the background color from an array of red, green and blue values, or nil for no background.
    @discardableResult
    public func setFillColor(_ rgbColor: [Float]?) -> TextBox {
        self.fillColor = rgbColor
        return self
    }

    /// Sets the background color as a 0xRRGGBB value.
    @discardableResult
    public func setBackgroundColor(_ color: Int32) -> TextBox {
        self.fillColor = colorArray(color)
        return self
    }

    /// Sets the background color from an array of red, green and blue values, or nil for no background.
    @discardableResult
    public func setBackgroundColor(_ rgbColor: [Float]?) -> TextBox {
        self.fillColor = rgbColor
        return self
    }

    /// Sets the text color as a 0xRRGGBB value.
    @discardableResult
    public func setTextColor(_ color: Int32) -> TextBox {
        self.textColor = colorArray(color)
        return self
    }

    /// Sets the text color from an array of red, green and blue values.
    @discardableResult
    public func setTextColor(_ rgbColor: [Float]) -> TextBox {
        self.textColor = rgbColor
        return self
    }

    /// Returns the text color.
    public func getTextColor() -> [Float] {
        return self.textColor
    }

    /// Sets the width of the borders.
    @discardableResult
    public func setStrokeWidth(_ strokeWidth: Float) -> TextBox {
        self.strokeWidth = strokeWidth
        return self
    }

    /// Sets the color of the borders as a 0xRRGGBB value.
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> TextBox {
        self.strokeColor = colorArray(color)
        return self
    }

    /// Sets the color of the borders from an array of red, green and blue values.
    @discardableResult
    public func setStrokeColor(_ rgbColor: [Float]?) -> TextBox {
        self.strokeColor = rgbColor
        return self
    }

    /// Returns the color of the borders.
    public func getStrokeColor() -> [Float]? {
        return self.strokeColor
    }

    ///
    /// Sets the border with the specified bit mask.
    ///
    @discardableResult
    public func setBorder(_ border: UInt32) -> TextBox {
        self.properties |= border
        return self
    }

    ///
    /// Returns true if the specified border is set.
    ///
    public func getBorder(_ border: UInt32) -> Bool {
        if border == Border.NONE {
            return (properties & 0x000F0000) == 0x00000000
        } else if border == Border.TOP {
            return (properties & 0x00010000) != 0x00000000
        } else if border == Border.BOTTOM {
            return (properties & 0x00020000) != 0x00000000
        } else if border == Border.LEFT {
            return (properties & 0x00040000) != 0x00000000
        } else if border == Border.RIGHT {
            return (properties & 0x00080000) != 0x00000000
        } else if border == Border.ALL {
            return (properties & 0x000F0000) == 0x000F0000
        }
        return false
    }

    ///
    /// Sets all borders on or off.
    ///
    @discardableResult
    public func setBorders(_ borders: Bool) -> TextBox {
        if borders {
            setBorder(Border.ALL)
        } else {
            self.properties &= 0x00F0FFFF
        }
        return self
    }

    ///
    /// Sets the text alignment: Align.LEFT, Align.RIGHT or Align.CENTER.
    ///
    @discardableResult
    public func setTextAlignment(_ alignment: UInt32) -> TextBox {
        self.properties = (self.properties & 0x00CFFFFF) | (alignment & 0x00300000)
        return self
    }

    /// Returns the horizontal text alignment, for example Align.LEFT.
    public func getTextAlignment() -> UInt32 {
        return (self.properties & 0x00300000)
    }

    /// Sets whether the text is underlined.
    @discardableResult
    public func setUnderline(_ underline: Bool) -> TextBox {
        if underline {
            self.properties |= 0x00400000
        } else {
            self.properties &= 0x00BFFFFF
        }
        return self
    }

    /// Returns true if the text is underlined.
    public func getUnderline() -> Bool {
        return (properties & 0x00400000) != 0x00000000
    }

    /// Sets whether the text is struck out.
    @discardableResult
    public func setStrikeout(_ strikeout: Bool) -> TextBox {
        if strikeout {
            self.properties |= 0x00800000
        } else {
            self.properties &= 0x007FFFFF
        }
        return self
    }

    /// Returns true if the text is struck out.
    public func getStrikeout() -> Bool {
        return (properties & 0x00800000) != 0x00000000
    }

    /// Sets the font used for characters the main font does not have.
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> TextBox {
        self.fallbackFont = fallbackFont
        return self
    }

    /// Returns the fallback font.
    public func getFallbackFont() -> Font? {
        return self.fallbackFont
    }

    /// Sets the vertical alignment of the text: Align.TOP, Align.CENTER or Align.BOTTOM.
    @discardableResult
    public func setVerticalAlignment(_ valign: UInt32) -> TextBox {
        self.valign = valign
        return self
    }

    /// Returns the vertical alignment of the text.
    public func getVerticalAlignment() -> UInt32 {
        return self.valign
    }

    /// Sets the colors used to highlight words in the text.
    @discardableResult
    public func setTextColors(_ colors: [String : Int32]?) -> TextBox {
        self.colors = colors
        return self
    }

    /// Returns the colors used to highlight words in the text.
    public func getTextColors() -> [String : Int32]? {
        return self.colors
    }

    /// Sets the language of the text, for example "en-US".
    @discardableResult
    public func setLanguage(_ language: String) -> TextBox {
        self.language = language
        return self
    }

    /// Returns the language of the text.
    public func getLanguage() -> String {
        return self.language
    }

    /// Sets the alternate description of this text box, used for accessibility.
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> TextBox {
        self.altDescription = altDescription
        return self
    }

    /// Returns the alternate description of this text box.
    public func getAltDescription() -> String? {
        return self.altDescription
    }

    private func colorArray(_ color: Int32) -> [Float] {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        return [r, g, b]
    }

    private func drawBorders(_ page: Page) {
        page.addArtifactBMC()
        page.setPenColor(strokeColor)
        page.setPenWidth(strokeWidth)
        if getBorder(Border.ALL) {
            page.drawRect(x, y, width, height)
        } else {
            if getBorder(Border.TOP) {
                page.moveTo(x, y)
                page.lineTo(x + width, y)
                page.strokePath()
            }
            if getBorder(Border.BOTTOM) {
                page.moveTo(x, y + height)
                page.lineTo(x + width, y + height)
                page.strokePath()
            }
            if getBorder(Border.LEFT) {
                page.moveTo(x, y)
                page.lineTo(x, y + height)
                page.strokePath()
            }
            if getBorder(Border.RIGHT) {
                page.moveTo(x + width, y)
                page.lineTo(x + width, y + height)
                page.strokePath()
            }
        }
        page.addEMC()
    }

    private func textIsCJK(_ str: String) -> Bool {
        // CJK Unified Ideographs Range: 4E00-9FD5
        // Hiragana Range: 3040-309F
        // Katakana Range: 30A0-30FF
        // Hangul Jamo Range: 1100-11FF
        var numOfCJK = 0
        var count = 0
        for scalar in str.unicodeScalars {
            count += 1
            let ch = scalar.value
            if (ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF) {
                numOfCJK += 1
            }
        }
        return numOfCJK > (count / 2)
    }

    private func getTextLines() -> [String] {
        var list = [String]()

        var textAreaWidth: Float
        if textDirection == Direction.LEFT_TO_RIGHT {
            textAreaWidth = width - 2*margin
        } else {
            textAreaWidth = height - 2*margin
        }
        // Only the core fonts apply kerning between adjacent characters, so
        // for every other font the width of a line is the sum of the widths of
        // its parts. That lets the loops below track the wrapped line's width
        // incrementally instead of re-measuring the whole accumulated line on
        // every token, which made wrapping a long paragraph O(n^2).
        let additive = !font.isCoreFont
        let lines = (text ?? "").components(separatedBy: CharacterSet.newlines)
        for line in lines {
            if font.stringWidth(fallbackFont, fontSize, line) <= textAreaWidth {
                list.append(line)
            } else {
                if textIsCJK(line) {
                    var sb = String()
                    var sbWidth: Float = 0.0
                    for ch in line {
                        let chWidth = additive ?
                                font.stringWidth(fallbackFont, fontSize, String(ch)) : 0.0
                        let lineWidth = additive ? sbWidth + chWidth :
                                font.stringWidth(fallbackFont, fontSize, sb + String(ch))
                        if lineWidth <= textAreaWidth {
                            sb.append(ch)
                            sbWidth = lineWidth
                        } else {
                            if !sb.isEmpty {    // Don't emit an empty line
                                list.append(sb)
                            }
                            sb = String(ch)
                            sbWidth = chWidth
                        }
                    }
                    if !sb.isEmpty {
                        list.append(sb)
                    }
                } else {
                    var sb = String()
                    var sbWidth: Float = 0.0
                    let spaceWidth = additive ?
                            font.stringWidth(fallbackFont, fontSize, Single.space) : 0.0
                    let tokens = line.split(whereSeparator: { $0 == " " || $0 == "\t" })
                    for token in tokens {
                        let tokenText = String(token)
                        let tokenWidth = additive ?
                                font.stringWidth(fallbackFont, fontSize, tokenText) : 0.0
                        var lineWidth: Float
                        if additive {
                            lineWidth = sb.isEmpty ?
                                    tokenWidth : sbWidth + spaceWidth + tokenWidth
                        } else {
                            lineWidth = font.stringWidth(fallbackFont, fontSize, sb + tokenText)
                        }
                        if lineWidth <= textAreaWidth {
                            sb.append(tokenText)
                            sb.append(Single.space)
                            sbWidth = lineWidth
                        } else {
                            if !sb.isEmpty {    // Don't emit an empty line
                                list.append(sb.trimmingCharacters(in: .whitespaces))
                            }
                            sb = tokenText + Single.space
                            sbWidth = tokenWidth
                        }
                    }
                    let last = sb.trimmingCharacters(in: .whitespaces)
                    if !last.isEmpty {
                        list.append(last)
                    }
                }
            }
        }

        return list
    }

    ///
    /// Draws this text box on the specified page.
    ///
    /// - Parameter page the Page where the TextBox is to be drawn.
    /// - Returns x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        var lines = getTextLines()
        let leading = font.getAscent(fontSize) + font.getDescent(fontSize) + spacing
        if height > 0.0 {   // TextBox with fixed height
            if (Float(lines.count)*leading - spacing) > (height - 2*margin) {
                var list = [String]()
                for i in 0..<lines.count {
                    if (Float(i + 1)*leading - spacing) > (height - 2*margin) {
                        break
                    }
                    list.append(lines[i])
                }
                if list.count > 0 {  // At least one line must fit in the text box
                    var lastLine = list[list.count - 1]
                    if lastLine.count > 3 {
                        lastLine = String(lastLine.prefix(lastLine.count - 3))
                    }
                    list[list.count - 1] = lastLine + "..."
                    lines = list
                }
            }
            if page != nil {
                if fillColor != nil {
                    page!.setBrushColor(fillColor)
                    page!.addArtifactBMC()
                    page!.fillRect(x, y, width, height)
                    page!.addEMC()
                }
                page!.setPenColor(self.strokeColor)
                page!.setBrushColor(self.fillColor)
                page!.setPenWidth(self.font.getUnderlineThickness(fontSize))
            }
            var xText = x + margin
            var yText = y + margin + font.getAscent(fontSize)
            if textDirection == Direction.LEFT_TO_RIGHT {
                if valign == Align.TOP {
                    yText = y + margin + font.getAscent(fontSize)
                } else if valign == Align.BOTTOM {
                    yText = (y + height) - (Float(lines.count)*leading + margin)
                    yText += font.getAscent(fontSize)
                } else if valign == Align.CENTER {
                    yText = y + (height - Float(lines.count)*leading)/2
                    yText += font.getAscent(fontSize)
                }
            } else {
                yText = x + margin + font.getAscent(fontSize)
            }
            for line in lines {
                if textDirection == Direction.LEFT_TO_RIGHT {
                    if getTextAlignment() == Align.LEFT {
                        xText = x + margin
                    } else if getTextAlignment() == Align.RIGHT {
                        xText = (x + width) -
                                (font.stringWidth(fallbackFont, fontSize, line) + margin)
                    } else if getTextAlignment() == Align.CENTER {
                        xText = x + (width - font.stringWidth(fallbackFont, fontSize, line))/2
                    }
                } else {
                    xText = y + margin
                }
                if page != nil {
                    drawTextLine(page!, font, fallbackFont, line, xText, yText, textColor, colors)
                }
                if textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP {
                    yText += leading
                } else {
                    yText -= leading
                }
            }
        } else {    // TextBox that expands to fit the content
            if page != nil {
                if fillColor != nil {
                    page!.setBrushColor(fillColor)
                    page!.addArtifactBMC()
                    page!.fillRect(x, y, width,
                            (Float(lines.count)*leading - spacing) + 2*margin)
                    page!.addEMC()
                }
                page!.setBrushColor(self.textColor)
                page!.setPenColor(self.strokeColor)
                page!.setPenWidth(self.font.getUnderlineThickness(fontSize))
            }
            var xText = x + margin
            var yText = y + margin + font.getAscent(fontSize)
            for line in lines {
                if textDirection == Direction.LEFT_TO_RIGHT {
                    if getTextAlignment() == Align.LEFT {
                        xText = x + margin
                    } else if getTextAlignment() == Align.RIGHT {
                        xText = (x + width) -
                                (font.stringWidth(fallbackFont, fontSize, line) + margin)
                    } else if getTextAlignment() == Align.CENTER {
                        xText = x + (width - font.stringWidth(fallbackFont, fontSize, line))/2
                    }
                } else {
                    xText = x + margin
                }
                if page != nil {
                    drawTextLine(page!, font, fallbackFont, line, xText, yText, textColor, colors)
                }
                if textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP {
                    yText += leading
                } else {
                    yText -= leading
                }
            }
            height = ((yText - y) - (font.getAscent(fontSize) + spacing)) + margin
        }
        if page != nil {
            drawBorders(page!)
            if textDirection == Direction.LEFT_TO_RIGHT && (uri != nil || key != nil) {
                page!.addAnnotation(Annotation(
                        Annotation.Link,
                        x,
                        y,
                        x + width,
                        y + height,
                        nil,    // Vertices
                        nil,    // Fill Color
                        0.0,    // Transparency
                        nil,    // Title
                        nil,    // Contents
                        uri,
                        key,    // The destination name
                        uriLanguage,
                        uriActualText,
                        uriAltDescription))
            }
            page!.setTextDirection(0)
        }
        return [x + width, y + height]
    }

    private func drawTextLine(
            _ page: Page,
            _ font: Font,
            _ fallbackFont: Font?,
            _ text: String,
            _ xText: Float,
            _ yText: Float,
            _ color: [Float],
            _ colors: [String : Int32]?) {
        if altDescription != nil {
            page.addBMC(StructElem.P, language, text, altDescription!)
        }

        if textDirection == Direction.LEFT_TO_RIGHT {
            page.drawString(font, fallbackFont, fontSize, text, xText, yText, color, colors)
        } else if textDirection == Direction.BOTTOM_TO_TOP {
            page.setTextDirection(90)
            page.drawString(
                    font, fallbackFont, fontSize, text, yText, xText + height, color, colors)
        } else if textDirection == Direction.TOP_TO_BOTTOM {
            page.setTextDirection(270)
            page.drawString(font, fallbackFont, fontSize, text,
                    (yText + width) - (margin + 2*font.getAscent(fontSize)),
                    xText, color, colors)
        }

        if altDescription != nil {
            page.addEMC()
        }

        if textDirection == Direction.LEFT_TO_RIGHT {
            let lineLength = font.stringWidth(fallbackFont, fontSize, text)
            if getUnderline() {
                page.addArtifactBMC()
                page.moveTo(xText, yText + font.getUnderlinePosition(fontSize))
                page.lineTo(xText + lineLength, yText + font.getUnderlinePosition(fontSize))
                page.strokePath()
                page.addEMC()
            }
            if getStrikeout() {
                page.addArtifactBMC()
                page.moveTo(xText, yText - (font.getBodyHeight(fontSize)/4))
                page.lineTo(xText + lineLength, yText - (font.getBodyHeight(fontSize)/4))
                page.strokePath()
                page.addEMC()
            }
        }
    }

    ///
    /// Sets the URI for the "click text line" action.
    ///
    @discardableResult
    public func setURIAction(_ uri: String?) -> TextBox {
        self.uri = uri
        return self
    }

    /// Sets the direction of the text.
    @discardableResult
    public func setTextDirection(_ textDirection: Direction) -> TextBox {
        self.textDirection = textDirection
        return self
    }
}   // End of TextBox.swift
