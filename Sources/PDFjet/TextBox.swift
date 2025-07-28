/**
 *  TextBox.swift
 *
©2025 PDFjet Software

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
/// A box containing line-wrapped text.
///
/// <p>Defaults:<br />
/// x = 0.0<br />
/// y = 0.0<br />
/// width = 300f<br />
/// height = 0.0<br />
/// alignment = Align.LEFT<br />
/// valign = Align.TOP<br />
/// spacing = 0.0<br />
/// margin = 0.0<br />
/// </p>
///
/// This class was originally developed by Ronald Bourret.
/// It was completely rewritten in 2013 by Eugene Dragoev.
///
public class TextBox : Drawable {
    var font: Font?
    var fallbackFont: Font?
    var text: String?
    var x: Float = 0.0
    var y: Float = 0.0
    var width: Float = 300.0
    var height: Float = 0.0
    var spacing: Float = 0.0
    var margin: Float = 0.0
    var lineWidth: Float = 0.0

    private var background = Color.transparent
    private var pen = Color.black
    private var brush = Color.black
    private var valign = Align.TOP
    private var colors: [String : Int32]?
    // TextBox properties
    // Future use:
    // bits 0 to 15
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
    // Future use:
    // bits 24 to 31
    private var properties: UInt32 = 0x00000001
    private var language = "en-US"
    private var altDescription = ""
    private var uri: String?
    private var key: String?
    private var uriLanguage: String?
    private var uriActualText: String?
    private var uriAltDescription: String?
    private var textDirection = Direction.LEFT_TO_RIGHT

    ///
    /// Creates a text box and sets the font.
    ///
    /// @param font the font.
    ///
    public init(_ font: Font) {
        self.font = font
    }

    ///
    /// Creates a text box and sets the font.
    ///
    /// @param text the text.
    /// @param font the font.
    ///
    public init(_ font: Font, _ text: String) {
        self.font = font
        self.text = text
    }

    ///
    /// Creates a text box and sets the font and the text.
    ///
    /// @param font the font.
    /// @param text the text.
    /// @param width the width.
    /// @param height the height.
    ///
    public init(_ font: Font, _ text: String, _ width: Float, _ height: Float) {
        self.font = font
        self.text = text
        self.width = width
        self.height = height
    }

    ///
    /// Sets the font for this text box.
    ///
    /// @param font the font.
    ///
    public func setFont(_ font: Font?) -> TextBox {
        self.font = font
        return self
    }

    ///
    /// Returns the font used by this text box.
    ///
    /// @return the font.
    ///
    public func getFont() -> Font? {
        return self.font
    }

    ///
    /// Sets the text box text.
    ///
    /// @param text the text box text.
    ///
    @discardableResult
    public func setText(_ text: String?) -> TextBox {
        self.text = text
        return self
    }

    ///
    /// Returns the text box text.
    ///
    /// @return the text box text.
    ///
    public func getText() -> String? {
        return self.text
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    ///
    /// Sets the location where this text box will be drawn on the page.
    ///
    /// @param x the x coordinate of the top left corner of the text box.
    /// @param y the y coordinate of the top left corner of the text box.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> TextBox {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the size of this text box.
    ///
    /// @param x the x coordinate of the top left corner of the text box.
    /// @param y the y coordinate of the top left corner of the text box.
    ///
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> TextBox {
        self.width = w
        self.height = h
        return self
    }

    ///
    /// Gets the location where this text box will be drawn on the page.
    ///
    @discardableResult
    public func getLocation() -> [Float] {
        return [self.x , self.y]
    }

    ///
    /// Sets the width of this text box.
    ///
    /// @param width the specified width.
    ///
    @discardableResult
    public func setWidth(_ width: Float) -> TextBox {
        self.width = width
        return self
    }

    ///
    /// Returns the text box width.
    ///
    /// @return the text box width.
    ///
    public func getWidth() -> Float {
        return width
    }

    ///
    /// Sets the height of this text box.
    ///
    /// @param height the specified height.
    ///
    @discardableResult
    public func setHeight(_ height: Float) -> TextBox {
        self.height = height
        return self
    }

    ///
    /// Returns the text box height.
    ///
    /// @return the text box height.
    ///
    public func getHeight() -> Float {
        return height
    }

    ///
    /// Sets the margin of this text box.
    ///
    /// @param margin the margin between the text and the box
    ///
    @discardableResult
    public func setMargin(_ margin: Float) -> TextBox {
        self.margin = margin
        return self
    }

    ///
    /// Returns the text box margin.
    ///
    /// @return the margin between the text and the box
    ///
    public func getMargin() -> Float {
        return margin
    }

    ///
    /// Sets the border line width.
    ///
    /// @param lineWidth float
    ///
    @discardableResult
    public func setLineWidth(_ lineWidth: Float) -> TextBox {
        self.lineWidth = lineWidth
        return self
    }

    ///
    /// Returns the border line width.
    ///
    /// @return float the line width.
    ///
    public func getLineWidth() -> Float {
        return lineWidth
    }

    ///
    /// Sets the spacing between lines of text.
    ///
    ///  @param spacing
    ///
    @discardableResult
    public func setSpacing(_ spacing: Float) -> TextBox {
        self.spacing = spacing
        return self
    }

    ///
    /// Returns the spacing between lines of text.
    ///
    /// @return float the spacing.
    ///
    public func getSpacing() -> Float {
        return spacing
    }

    ///
    /// Sets the background to the specified color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    ///
    @discardableResult
    public func setBgColor(_ color: Int32) -> TextBox {
        self.background = color
        return self
    }

    ///
    /// Sets the background to the specified color.
    ///
    /// @param color the color specified as array of integer values from 0x00 to 0xFF.
    ///
    @discardableResult
    public func setBgColor(_ color: [Int32]) -> TextBox {
        self.background = color[0] << 16 | color[1] << 8 | color[2]
        return self
    }

    ///
    /// Returns the background color.
    ///
    /// @return int the color as 0xRRGGBB integer.
    ///
    public func getBgColor() -> Int32 {
        return self.background
    }

    ///
    /// Sets the pen and brush colors to the specified color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    ///
    @discardableResult
    public func setFgColor(_ color: Int32) -> TextBox {
        self.pen = color
        self.brush = color
        return self
    }

    ///
    /// Sets the pen and brush colors to the specified color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    ///
    @discardableResult
    public func setFgColor(_ color: [Int32]) -> TextBox {
        self.pen = color[0] << 16 | color[1] << 8 | color[2]
        self.brush = pen
        return self
    }

    ///
    /// Sets the pen color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    ///
    @discardableResult
    public func setPenColor(_ color: Int32) -> TextBox {
        self.pen = color
        return self
    }

    ///
    /// Sets the pen color.
    ///
    /// @param color the color specified as an array of int values from 0x00 to 0xFF.
    ///
    @discardableResult
    public func setPenColor(_ color: [Int32]) -> TextBox {
        self.pen = color[0] << 16 | color[1] << 8 | color[2]
        return self
    }

    ///
    /// Returns the pen color as 0xRRGGBB integer.
    ///
    /// @return int the pen color.
    ///
    public func getPenColor() -> Int32 {
        return self.pen
    }

    ///
    /// Sets the brush color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    ///
    @discardableResult
    public func setBrushColor(_ color: Int32) -> TextBox {
        self.brush = color
        return self
    }

    ///
    /// Sets the brush color.
    ///
    /// @param color the color specified as an array of int values from 0x00 to 0xFF.
    ///
    @discardableResult
    public func setBrushColor(_ color: [Int32]) -> TextBox {
        self.brush = color[0] << 16 | color[1] << 8 | color[2]
        return self
    }

    ///
    /// Returns the brush color.
    ///
    /// @return int the brush color specified as 0xRRGGBB integer.
    ///
    public func getBrushColor() -> Int32 {
        return self.brush
    }

    ///
    /// Sets the TextBox border properties.
    ///
    /// @param border the border properties value.
    ///
    @discardableResult
    public func setBorder(_ border: UInt32) -> TextBox {
        self.properties |= border
        return self
    }

    ///
    /// Returns the text box border.
    ///
    /// @return Bool the text border object.
    ///
    public func getBorder(_ border: UInt32) -> Bool {
        if border == Border.NONE {
            if ((properties >> 16) & 0xF) == 0x0 {
                return true
            }
        } else if border == Border.TOP {
            if ((properties >> 16) & 0x1) == 0x1 {
                return true
            }
        } else if border == Border.BOTTOM {
            if ((properties >> 16) & 0x2) == 0x2 {
                return true
            }
        } else if border == Border.LEFT {
            if ((properties >> 16) & 0x4) == 0x4 {
                return true
            }
        } else if border == Border.RIGHT {
            if ((properties >> 16) & 0x8) == 0x8 {
                return true
            }
        } else if border == Border.ALL {
            if ((properties >> 16) & 0xF) == 0xF {
                return true
            }
        }
        return false
    }

    ///
    /// Sets the TextBox borders on and off.
    ///
    /// @param borders the borders flag.
    ///
    public func setBorders(_ borders: Bool) {
        if (borders) {
            setBorder(Border.ALL);
        } else {
            setBorder(Border.NONE);
        }
    }

    ///
    /// Sets the cell text alignment.
    ///
    /// @param alignment the alignment code.
    /// Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
    ///
    @discardableResult
    public func setTextAlignment(_ alignment: UInt32) -> TextBox {
        self.properties &= 0x00CFFFFF
        self.properties |= (alignment & 0x00300000)
        return self
    }

    ///
    /// Returns the text alignment.
    ///
    /// @return alignment the alignment code. Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
    ///
    public func getTextAlignment() -> UInt32 {
        return (self.properties & 0x00300000)
    }

    ///
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    ///
    /// @param underline the underline flag.
    ///
    @discardableResult
    public func setUnderline(_ underline: Bool) -> TextBox {
        if underline {
            self.properties |= 0x00400000
        } else {
            self.properties &= 0x00BFFFFF
        }
        return self
    }

    ///
    /// Whether the text will be underlined.
    ///
    /// @return whether the text will be underlined
    ///
    public func getUnderline() -> Bool {
        return (properties & 0x00400000) != 0
    }

    ///
    /// Sets the srikeout flag.
    /// In the flag is true - draw strikeout line through the text.
    ///
    /// @param strikeout the strikeout flag.
    ///
    @discardableResult
    public func setStrikeout(_ strikeout: Bool) -> TextBox {
        if strikeout {
            self.properties |= 0x00800000
        } else {
            self.properties &= 0x007FFFFF
        }
        return self
    }

    ///
    /// Returns the strikeout flag.
    ///
    /// @return Bool the strikeout flag.
    ///
    public func getStrikeout() -> Bool {
        return (properties & 0x00800000) != 0
    }

    @discardableResult
    public func setFallbackFont(_ font: Font?) -> TextBox {
        self.fallbackFont = font
        return self
    }

    public func getFallbackFont() -> Font? {
        return self.fallbackFont
    }

    ///
    /// Sets the vertical alignment of the text in this TextBox.
    ///
    /// @param valign - valid values areAlign.TOP, Align.BOTTOM and Align.CENTER
    ///
    @discardableResult
    public func setVerticalAlignment(_ valign: UInt32) -> TextBox {
        self.valign = valign
        return self
    }

    public func getVerticalAlignment() -> UInt32 {
        return self.valign
    }

    @discardableResult
    public func setTextColors(_ colors: [String : Int32]?) -> TextBox {
        self.colors = colors
        return self
    }

    public func getTextColors() -> [String : Int32]? {
        return self.colors
    }

    private func drawBorders(_ page: Page) {
        page.setPenColor(pen)
        page.setPenWidth(lineWidth)
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
    }

    private func textIsCJK(_ str: String) -> Bool {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        var numOfCJK = 0
        let scalars = [UnicodeScalar](str.unicodeScalars)
        for scalar in scalars {
            if (scalar.value >= 0x4E00 && scalar.value <= 0x9FD5) ||
                    (scalar.value >= 0x3040 && scalar.value <= 0x309F) ||
                    (scalar.value >= 0x30A0 && scalar.value <= 0x30FF) ||
                    (scalar.value >= 0x1100 && scalar.value <= 0x11FF) {
                numOfCJK += 1
            }
        }
        return (numOfCJK > (scalars.count / 2))
    }

    private func getTextLines() -> [String] {
        var list = [String]()
        var textAreaWidth: Float
        if textDirection == Direction.LEFT_TO_RIGHT {
            textAreaWidth = width - 2*margin
        } else {
            textAreaWidth = height - 2*margin
        }
        let lines = text!.components(separatedBy: "\n")

        for line in lines {
            if font!.stringWidth(fallbackFont, line) <= textAreaWidth {
                list.append(line)
            } else {
                if textIsCJK(line) {
                    var sb = String()
                    for scalar in line.unicodeScalars {
                        if font!.stringWidth(fallbackFont, sb + String(scalar)) <= textAreaWidth {
                            sb.append(String(scalar))
                        } else {
                            list.append(sb)
                            sb = ""
                            sb.append(String(scalar))
                        }
                    }
                    if sb.count > 0 {
                        list.append(sb)
                    }
                } else {
                    var sb = String()
                    let tokens = line.components(separatedBy: .whitespaces)
                    for token in tokens {
                        if font!.stringWidth(fallbackFont, sb + token) <= textAreaWidth {
                            sb.append(token + " ")
                        } else {
                            list.append(sb.trim())
                            sb = ""
                            sb.append(token + " ")
                        }
                    }
                    if sb.trim().count > 0 {
                        list.append(sb.trim())
                    }
                }
            }
        }

        return list
    }

    ///
    /// Draws this text box on the specified page.
    ///
    /// @param page the Page where the TextBox is to be drawn.
    /// @param draw flag specifying if this component should actually be drawn on the page.
    /// @return x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float32] {
        var lines = getTextLines()
        let leading = font!.ascent + font!.descent + spacing
        if height > 0.0 {   // TextBox with fixed height
            if Float32(lines.count)*leading - spacing > (height - 2*margin) {
                var list = [String]()
                for line in lines {
                    if (Float32(list.count + 1)*leading - spacing > (height - 2*margin)) {
                        break
                    }
                    list.append(line)
                }
                if (list.count > 0) {
                    var lastLine = list[list.count - 1]
                    var scalars = [Unicode.Scalar]()
                    for scalar in lastLine.unicodeScalars {
                        scalars.append(scalar)
                    }
                    if scalars.count > 3 {
                        scalars.removeLast(3)
                    }
                    lastLine = ""
                    for scalar in scalars {
                        lastLine += String(scalar)
                    }
                    list[list.count - 1] = lastLine + "..."
                    lines = list
                }
            }
            if page != nil {
                if getBgColor() != Color.transparent {
                    page!.setBrushColor(background)
                    page!.fillRect(x, y, width, height)
                }
                page!.setPenColor(self.pen)
                page!.setBrushColor(self.brush)
                page!.setPenWidth(self.font!.underlineThickness)
            }
            var xText = x + margin
            var yText = y + margin + font!.ascent
            if textDirection == Direction.LEFT_TO_RIGHT {
                if valign == Align.TOP {
                    yText = y + margin + font!.ascent
                } else if valign == Align.BOTTOM {
                    yText = (y + height) - ((Float(lines.count)*leading) + margin)
                    yText += font!.ascent
                } else if valign == Align.CENTER {
                    yText = y + (height - Float(lines.count)*leading)/2
                    yText += font!.ascent
                }
            } else {
                yText = x + margin + font!.ascent
            }
            for line in lines {
                if textDirection == Direction.LEFT_TO_RIGHT {
                    if getTextAlignment() == Align.LEFT {
                        xText = x + margin
                    } else if getTextAlignment() == Align.RIGHT {
                        xText = (x + width) - (font!.stringWidth(fallbackFont, line) + margin)
                    } else if getTextAlignment() == Align.CENTER {
                        xText = x + (width - font!.stringWidth(fallbackFont, line))/2
                    }
                } else {
                    xText = y + margin
                }
                if page != nil {
                    drawTextLine(page, font!, fallbackFont, line, xText, yText, brush, colors)
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
                if getBgColor() != Color.transparent {
                    page!.setBrushColor(background)
                    page!.fillRect(x, y, width, (Float32(lines.count) * leading - spacing) + 2*margin)
                }
                page!.setPenColor(self.pen)
                page!.setBrushColor(self.brush)
                page!.setPenWidth(self.font!.underlineThickness)
            }
            var xText = x + margin
            var yText = y + margin + font!.ascent
            for line in lines {
                if textDirection == Direction.LEFT_TO_RIGHT {
                    if (getTextAlignment() == Align.LEFT) {
                        xText = x + margin
                    } else if (getTextAlignment() == Align.RIGHT) {
                        xText = (x + width) - (font!.stringWidth(fallbackFont, line) + margin)
                    } else if (getTextAlignment() == Align.CENTER) {
                        xText = x + (width - font!.stringWidth(fallbackFont, line))/2
                    }
                } else {
                    xText = x + margin
                }
                if page != nil {
                    drawTextLine(page, font!, fallbackFont, line, xText, yText, brush, colors)
                }
                if textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP {
                    yText += leading
                } else {
                    yText -= leading
                }
            }
            height = ((yText - y) - (font!.ascent + spacing)) + margin
        }
        if page != nil {
            drawBorders(page!)
            if textDirection == Direction.LEFT_TO_RIGHT && (uri != nil || key != nil) {
                page!.addAnnotation(Annotation(
                        uri,
                        key,    // The destination name
                        x,
                        y,
                        x + width,
                        y + height,
                        uriLanguage,
                        uriActualText,
                        uriAltDescription))
            }
            page!.setTextDirection(0)
        }
        return [x + width, y + height]
    }

    private func drawTextLine(
            _ page: Page?,
            _ font: Font,
            _ fallbackFont: Font?,
            _ text: String,
            _ xText: Float,
            _ yText: Float,
            _ color: Int32,
            _ colors: [String : Int32]?) {
        page!.addBMC(StructElem.P, language, text, altDescription);
        if (textDirection == Direction.LEFT_TO_RIGHT) {
            page!.drawString(font, fallbackFont, text, xText, yText, color, colors);
        } else if (textDirection == Direction.BOTTOM_TO_TOP) {
            page!.setTextDirection(90);
            page!.drawString(font, fallbackFont, text, yText, xText + height, color, colors);
        } else if (textDirection == Direction.TOP_TO_BOTTOM) {
            page!.setTextDirection(270);
            page!.drawString(font, fallbackFont, text,
                    (yText + width) - (margin + 2*font.ascent), xText, color, colors);
        }
        page!.addEMC();
        if textDirection == Direction.LEFT_TO_RIGHT {
            let lineLength = font.stringWidth(fallbackFont, text)
            if getUnderline() {
			    page!.addArtifactBMC()
                page!.moveTo(xText, yText + font.underlinePosition)
                page!.lineTo(xText + lineLength, yText + font.underlinePosition)
                page!.strokePath()
			    page!.addEMC()
            }
            if getStrikeout() {
			    page!.addArtifactBMC()
                page!.moveTo(xText, yText - (font.bodyHeight/4))
                page!.lineTo(xText + lineLength, yText - (font.bodyHeight/4))
                page!.strokePath()
			    page!.addEMC()
            }
        }
    }

    /**
     *  Sets the URI for the "click text line" action.
     *
     *  @param uri the URI
     *  @return this TextBox.
     */
    public func setURIAction(_ uri: String) {
        self.uri = uri
    }

    public func setTextDirection(_ textDirection: Direction) {
        self.textDirection = textDirection
    }
}   // End of TextBox.swift
