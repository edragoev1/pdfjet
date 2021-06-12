/**
 *  TextLine.swift
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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
    var text: String?
    var trailingSpace: Bool = true

    private var uri: String?
    private var key: String?

    private var underline = false
    private var strikeout = false

    private var degrees = 0
    private var color = Color.black

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


    ///
    /// Constructor for creating text line objects.
    ///
    /// - Parameter font the font to use.
    ///
    public init(_ font: Font) {
        self.font = font
    }


    ///
    /// Constructor for creating text line objects.
    ///
    /// - Parameter font the font to use.
    /// - Parameter text the text.
    ///
    public init(_ font: Font, _ text: String) {
        self.font = font
        self.text = text
        self.altDescription = text
    }


    ///
    /// Sets the text.
    ///
    /// - Parameter text the text.
    /// - Returns: selt.TextLine.
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
    /// Sets the location where selt.text line will be drawn on the page.
    ///
    /// - Parameter x the x coordinate of the text line.
    /// - Parameter y the y coordinate of the text line.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> TextLine {
        self.x = x;
        self.y = y;
        return self;
    }


    ///
    /// Sets the font to use for selt.text line.
    ///
    /// - Parameter font the font to use.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setFont(_ font: Font) -> TextLine {
        self.font = font
        return self
    }


    ///
    /// Gets the font to use for selt.text line.
    ///
    /// - Returns: font the font to use.
    ///
    public func getFont() -> Font {
        return self.font!
    }


    ///
    /// Sets the font size to use for selt.text line.
    ///
    /// - Parameter fontSize the fontSize to use.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> TextLine {
        self.font!.setSize(fontSize)
        return self
    }


    ///
    /// Sets the fallback font.
    ///
    /// - Parameter fallbackFont the fallback font.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> TextLine {
        self.fallbackFont = fallbackFont
        return self
    }


    ///
    /// Sets the fallback font size to use for selt.text line.
    ///
    /// - Parameter fallbackFontSize the fallback font size.
    /// - Returns: selt.TextLine.
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


    ///
    /// Sets the color for selt.text line.
    ///
    /// - Parameter color the color is specified as an integer.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setColor(_ color: UInt32) -> TextLine {
        self.color = color
        return self
    }


    ///
    /// Sets the pen color.
    ///
    /// - Parameter color the color.
    ///   See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setColor(_ color: [UInt32]) -> TextLine {
        self.color = color[0] << 16 | color[1] << 8 | color[2]
        return self
    }


    ///
    /// Returns the text line color.
    ///
    /// - Returns: the text line color.
    ///
    public func getColor() -> UInt32 {
        return self.color
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
    /// Returns the width of selt.TextLine.
    ///
    /// - Returns: the width.
    ///
    public func getWidth() -> Float {
        return font!.stringWidth(fallbackFont, text!)
    }


    public func getStringWidth(_ text: String) -> Float {
        return font!.stringWidth(fallbackFont, text)
    }


    ///
    /// Returns the height of selt.TextLine.
    ///
    /// - Returns: the height.
    ///
    public func getHeight() -> Float {
        return font!.getHeight()
    }


    ///
    /// Sets the URI for the "click text line" action.
    ///
    /// - Parameter uri the URI
    /// - Returns: selt.TextLine.
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
    /// - Returns: selt.TextLine.
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
    /// - Returns: selt.TextLine.
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
    /// - Returns: selt.TextLine.
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
    /// - Returns: selt.TextLine.
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
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setTextEffect(_ textEffect: Int) -> TextLine {
        self.textEffect = textEffect
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
    /// - Returns: selt.TextLine.
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


    ///
    /// Sets the trailing space after selt.text line when used in paragraph.
    ///
    /// - Parameter trailingSpace the trailing space.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func setTrailingSpace(_ trailingSpace: Bool) -> TextLine {
        self.trailingSpace = trailingSpace
        return self
    }


    ///
    /// Returns the trailing space.
    ///
    /// - Returns: the trailing space.
    ///
    public func getTrailingSpace() -> Bool {
        return self.trailingSpace
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
    /// Sets the alternate description of selt.text line.
    ///
    /// - Parameter altDescription the alternate description of the text line.
    /// - Returns: selt.TextLine.
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


    ///
    /// Places selt.text line in the specified box.
    ///
    /// - Parameter box the specified box.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func placeIn(_ box: Box) -> TextLine {
        placeIn(box, 0.0, 0.0)
        return self
    }


    ///
    /// Places selt.text line in the box at the specified offset.
    ///
    /// - Parameter box the specified box.
    /// - Parameter xOffset the x offset from the top left corner of the box.
    /// - Parameter yOffset the y offset from the top left corner of the box.
    /// - Returns: selt.TextLine.
    ///
    @discardableResult
    public func placeIn(
            _ box: Box,
            _ xOffset: Float,
            _ yOffset: Float) -> TextLine {
        self.xBox = box.x + xOffset
        self.yBox = box.y + yOffset
        return self
    }


    public func advance(_ leading: Float) -> Float {
        self.y += leading
        return self.y
    }


    public func getTextY() -> Float {
        return self.y
    }


    ///
    /// Draws selt.text line on the specified page if the draw parameter is true.
    ///
    /// - Parameter page the page to draw selt.text line on.
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

        page!.setBrushColor(color)
        page!.addBMC(structureType, language, text!, altDescription!)
        page!.drawString(font!, fallbackFont, text, self.x, self.y)
        page!.addEMC()

        let radians = Float.pi * Float(degrees) / 180.0
        if underline {
            page!.setPenWidth(font!.underlineThickness)
            page!.setPenColor(color)
            let lineLength = font!.stringWidth(fallbackFont, text!)
            let xAdjust = font!.underlinePosition * Float(sin(radians)) + verticalOffset
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
            page!.setPenColor(color)
            let lineLength = font!.stringWidth(fallbackFont, text!)
            let xAdjust = (font!.bodyHeight / 4.0) * Float(sin(radians))
            let yAdjust = (font!.bodyHeight / 4.0) * Float(cos(radians))
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
                    uri,
                    key,    // The destination name
                    self.x,
                    self.y - font!.ascent,
                    self.x + font!.stringWidth(fallbackFont, text!),
                    self.y + font!.descent,
                    uriLanguage,
                    uriActualText,
                    uriAltDescription))
        }
        page!.setTextDirection(0)

        let len = font!.stringWidth(fallbackFont, text!)
        let xMax = max(x, x + len*Float(cos(radians)))
        let yMax = max(y, y - len*Float(sin(radians)))

        return [xMax, yMax]
    }

}   // End of TextLine.swift
