/**
 * TextLine.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create text line objects.
///
public class TextLine : Drawable {
    var x: Float = 0.0
    var y: Float = 0.0

    var font: Font?
    var fallbackFont: Font?
    var fontSize: Float

    var text: String?
    private var uri: String?
    private var key: String?

    var isLastToken: Bool = false
    var xOffset: Float = 0.0
    var underline = false
    var strikeout = false

    private var degrees = 0

    private var xBox: Float = 0.0
    private var yBox: Float = 0.0

    private var textEffect = Effect.NORMAL
    private var verticalOffset: Float = 0.0

    private var language: String?
    private var altDescription: String?

    private var uriLanguage: String?
    private var uriActualText: String?
    private var uriAltDescription: String?

    private var structureType = StructElem.P

    private var textColor: [Float] = [0.0, 0.0, 0.0]
    private var lineColor: [Float] = [0.0, 0.0, 0.0]
    private var colorMap: [String: Int32]

    ///
    /// Constructor for creating text line objects.
    ///
    /// - Parameter font the font to use.
    ///
    public init(_ font: Font) {
        self.font = font
        self.fallbackFont = font
        self.fontSize = font.size
        self.colorMap = [String: Int32]()
    }

    ///
    /// Constructor for creating text line objects.
    ///
    /// - Parameter font the font to use.
    /// - Parameter text the text.
    ///
    public init(_ font: Font, _ text: String) {
        self.font = font
        self.fallbackFont = font
        self.fontSize = font.size
        self.text = text
        self.altDescription = text
        self.colorMap = [String: Int32]()
    }

    ///
    /// Sets the text.
    ///
    /// - Parameter text the text.
    /// - Returns: self.TextLine.
    ///
    @discardableResult
    public func setText(_ text: String) -> TextLine {
        self.text = text
        self.altDescription = text
        return self
    }

    ///
    /// Returns the text.
    ///
    /// - Returns: the text.
    ///
    public func getText() -> String? {
        return self.text
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    ///
    /// Sets the location where the text line will be drawn on the page.
    ///
    /// - Parameter x the x coordinate of the text line.
    /// - Parameter y the y coordinate of the text line.
    /// - Returns: text TextLine.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> TextLine {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the text line font.
    ///
    /// - Parameter font the font to use.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setFont(_ font: Font) -> TextLine {
        self.font = font
        return self
    }

    ///
    /// Gets the text line font.
    ///
    /// - Returns: font the font to use.
    ///
    public func getFont() -> Font {
        return self.font!
    }

    ///
    /// Sets the text line font size.
    ///
    /// - Parameter fontSize the fontSize to use.
    /// - Returns: text TextLine.
    ///
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> TextLine {
        self.fontSize = fontSize
        return self
    }

    public func getFontSize() -> Float {
        return self.fontSize
    }

    ///
    /// Sets the fallback font.
    ///
    /// - Parameter fallbackFont the fallback font.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> TextLine {
        self.fallbackFont = fallbackFont
        return self
    }

    ///
    /// Sets the text line fallback font size.
    ///
    /// - Parameter fallbackFontSize the fallback font size.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setFallbackFontSize(_ fallbackFontSize: Float) -> TextLine {
        self.fallbackFont!.setSize(fallbackFontSize)
        return self
    }

    ///
    /// Returns the fallback font.
    ///
    /// - Returns: the fallback font.
    ///
    public func getFallbackFont() -> Font? {
        return self.fallbackFont
    }

    @discardableResult
    public func setColor(_ color: Int32) -> TextLine {
        return setTextColor(color)
    }

    @discardableResult
    public func setTextColor(_ color: Int32) -> TextLine {
        if color == Color.transparent {
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.textColor = [r, g, b]
        return self
    }

    @discardableResult
    public func setTextColor(_ r: Float, _ g: Float, _ b: Float) -> TextLine {
        self.textColor = [r, g, b]
        return self
    }

    @discardableResult
    public func setTextColor(_ textColor: [Float]) -> TextLine {
        self.textColor = textColor
        return self
    }

    public func getTextColor() -> [Float] {
        return self.textColor
    }

    @discardableResult
    public func setLineColor(_ color: Int32) -> TextLine {
        if color == Color.transparent {
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.lineColor = [r, g, b]
        return self
    }

    @discardableResult
    public func setLineColor(_ r: Float, _ g: Float, _ b: Float) -> TextLine {
        self.lineColor = [r, g, b]
        return self
    }

    @discardableResult
    public func setLineColor(_ lineColor: [Float]) -> TextLine {
        self.lineColor = lineColor
        return self
    }

    public func getLineColor() -> [Float] {
        return self.lineColor
    }

    ///
    /// Returns the x coordinate of the destination.
    ///
    /// - Returns: the x coordinate of the destination.
    ///
    public func getDestinationX() -> Float {
        return x
    }

    ///
    /// Returns the y coordinate of the destination.
    ///
    /// - Returns: the y coordinate of the destination.
    ///
    public func getDestinationY() -> Float {
        return y - font!.getSize()
    }

    ///
    /// Returns the width of the text line.
    ///
    /// - Returns: the width.
    ///
    public func getWidth() -> Float {
        return font!.stringWidth(fallbackFont, fontSize, text!)
    }

    ///
    /// Returns the width of the specified string.
    ///
    /// - Returns: the width.
    ///
    public func getStringWidth(_ text: String) -> Float {
        return font!.stringWidth(fallbackFont, fontSize, text)
    }

    ///
    /// Returns the height of the text line.
    ///
    /// - Returns: the height.
    ///
    public func getHeight() -> Float {
        return font!.getBodyHeight(self.fontSize)
    }

    ///
    /// Sets the URI for the "click text line" action.
    ///
    /// - Parameter uri the URI
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setURIAction(_ uri: String?) -> TextLine {
        self.uri = uri
        return self
    }

    ///
    /// Returns the action URI.
    ///
    /// - Returns: the action URI.
    ///
    public func getURIAction() -> String? {
        return self.uri
    }

    ///
    /// Sets the destination key for the action.
    ///
    /// - Parameter key the destination name.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setGoToAction(_ key: String?) -> TextLine {
        self.key = key
        return self
    }

    ///
    /// Returns the GoTo action string.
    ///
    /// - Returns: the GoTo action string.
    ///
    public func getGoToAction() -> String? {
        return self.key
    }

    ///
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    ///
    /// - Parameter underline the underline flag.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setUnderline(_ underline: Bool) -> TextLine {
        self.underline = underline
        return self
    }

    ///
    /// Returns the underline flag.
    ///
    /// - Returns: the underline flag.
    ///
    public func getUnderline() -> Bool {
        return self.underline
    }

    ///
    /// Sets the strike variable.
    /// If the value of the strike variable is 'true' - a strike line is drawn through the text.
    ///
    /// - Parameter strikeout the strikeout flag.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setStrikeout(_ strikeout: Bool) -> TextLine {
        self.strikeout = strikeout
        return self
    }

    ///
    /// Returns the strikeout flag.
    ///
    /// - Returns: the strikeout flag.
    ///
    public func getStrikeout() -> Bool {
        return self.strikeout
    }

    ///
    /// Sets the direction in which to draw the text.
    ///
    /// - Parameter degrees the number of degrees.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setTextDirection(_ degrees: Int) -> TextLine {
        self.degrees = degrees
        return self
    }

    ///
    /// Returns the text direction.
    ///
    /// - Returns: the text direction.
    ///
    public func getTextDirection() -> Int {
        return degrees
    }

    ///
    /// Sets the text effect.
    ///
    /// - Parameter textEffect Effect.NORMAL, Effect.SUBSCRIPT or Effect.SUPERSCRIPT.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setTextEffect(_ textEffect: Int) -> TextLine {
        self.textEffect = textEffect
        if textEffect == Effect.NORMAL {
            verticalOffset = 0.0
        } else if textEffect == Effect.SUPERSCRIPT {
            verticalOffset = -font!.getBodyHeight(self.fontSize)/2.0
        } else if textEffect == Effect.SUBSCRIPT {
            verticalOffset = font!.getBodyHeight(self.fontSize)/3.0
        }
        return self
    }

    ///
    /// Returns the text effect.
    ///
    /// - Returns: the text effect.
    ///
    public func getTextEffect() -> Int {
        return self.textEffect
    }

    ///
    /// Sets the vertical offset of the text.
    ///
    /// - Parameter verticalOffset the vertical offset.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setVerticalOffset(_ verticalOffset: Float) -> TextLine {
        self.verticalOffset = verticalOffset
        return self
    }

    ///
    /// Returns the vertical text offset.
    ///
    /// - Returns: the vertical text offset.
    ///
    public func getVerticalOffset() -> Float {
        return self.verticalOffset
    }

    @discardableResult
    public func setLanguage(_ language: String?) -> TextLine {
        self.language = language
        return self
    }

    public func getLanguage() -> String? {
        return self.language
    }

    ///
    /// Sets the alternate description of the text line.
    ///
    /// - Parameter altDescription the alternate description of the text line.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String?) -> TextLine {
        self.altDescription = altDescription
        return self
    }

    public func getAltDescription() -> String? {
        return self.altDescription
    }

    @discardableResult
    public func setURILanguage(_ uriLanguage: String?) -> TextLine {
        self.uriLanguage = uriLanguage
        return self
    }

    @discardableResult
    public func setURIAltDescription(_ uriAltDescription: String?) -> TextLine {
        self.uriAltDescription = uriAltDescription
        return self
    }

    @discardableResult
    public func setURIActualText(_ uriActualText: String?) -> TextLine {
        self.uriActualText = uriActualText
        return self
    }

    @discardableResult
    public func setStructureType(_ structureType: String) -> TextLine {
        self.structureType = structureType
        return self
    }

    public func advance(_ leading: Float) -> Float {
        self.y += leading
        return self.y
    }

    public func getTextY() -> Float {
        return self.y
    }

    @discardableResult
    public func setColorMap(_ colorMap: [String: Int32]) -> TextLine {
        self.colorMap = colorMap
        return self
    }

    public func getColorMap() -> [String: Int32] {
        return self.colorMap
    }

    ///
    /// Draws the text line on the specified page if the draw parameter is true.
    ///
    /// - Parameter page the page to draw text line on.
    /// - Parameter draw if draw is false - no action is performed.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if text == nil || text == "" {
            return [x, y]
        }

        page!.setTextDirection(degrees)

        self.x += xBox
        self.y += yBox

        page!.setBrushColor(textColor)
        page!.addBMC(structureType, language, text!, altDescription!)
        page!.drawString(font!, fallbackFont, fontSize, text, self.x, self.y + verticalOffset, textColor, colorMap)
        page!.addEMC()

        let radians = Float.pi * Float(degrees) / 180.0
        if underline {
            page!.setPenWidth(font!.underlineThickness)
            page!.setPenColor(lineColor)
            var lineLength = font!.stringWidth(fallbackFont, fontSize, text!)
            if (self.isLastToken) {
                lineLength -= font!.stringWidth(fallbackFont, fontSize, Single.space)
            }
            let xAdjust = font!.underlinePosition * Float(sin(radians))
            let yAdjust = font!.underlinePosition * Float(cos(radians)) + verticalOffset
            let x2 = x + lineLength * Float(cos(radians))
            let y2 = y - lineLength * Float(sin(radians))
            page!.addBMC(structureType, language, text!, "Underlined text: " + text!)
            page!.moveTo(x + xAdjust, y + yAdjust)
            page!.lineTo(x2 + xAdjust, y2 + yAdjust)
            page!.strokePath()
            page!.addEMC()
        }

        if strikeout {
            page!.setPenWidth(font!.underlineThickness)
            page!.setPenColor(lineColor)
            var lineLength = font!.stringWidth(fallbackFont, fontSize, text!)
            if (self.isLastToken) {
                lineLength -= font!.stringWidth(fallbackFont, fontSize, Single.space)
            }
            let xAdjust = (font!.bodyHeight / 4.0) * Float(sin(radians))
            let yAdjust = (font!.bodyHeight / 4.0) * Float(cos(radians)) + verticalOffset
            let x2 = x + lineLength * Float(cos(radians))
            let y2 = y - lineLength * Float(sin(radians))
            page!.addBMC(structureType, language, text!, "Strikethrough text: " + text!)
            page!.moveTo(x - xAdjust, y - yAdjust)
            page!.lineTo(x2 - xAdjust, y2 - yAdjust)
            page!.strokePath()
            page!.addEMC()
        }

        if uri != nil || key != nil {
            page!.addAnnotation(Annotation(
                    Annotation.Link,
                    self.x,
                    (self.y + verticalOffset) - font!.ascent,
                    self.x + font!.stringWidth(fallbackFont, fontSize, text!),
                    (self.y  + verticalOffset) + font!.descent,
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

        let len = font!.stringWidth(fallbackFont, fontSize, text!)
        let xMax = max(x, x + len*Float(cos(radians)))
        let yMax = max(y + verticalOffset, (y + verticalOffset) - len*Float(sin(radians)))

        return [xMax, yMax]
    }
}   // End of TextLine.swift
