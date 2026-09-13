/**
 * Cell.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Used to create table cell objects.
 * See the Table class for more information.
 */
public class Cell {
    internal var font: Font?
    internal var fallbackFont: Font?
    internal var fontSize: Float = 12.0
    var text: String?
    var image: Image?
    var barcode: Barcode?
    var textBlock: TextBlock?
    var textColumn: TextColumn?
    var textBox: TextBox?
    var point: Point?
    var compositeTextLine: CompositeTextLine?
    var width: Float = 75.0
    var topPadding: Float = 2.0
    var bottomPadding: Float = 2.0
    var leftPadding: Float = 2.0
    var rightPadding: Float = 2.0

    var backgroundColor: [Float]?
    var textColor: [Float] = [0.0, 0.0, 0.0]
    var strokeWidth: Float = 0.0
    var strokeColor: [Float]?

    // Cell properties
    // Colspan:
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
    private var properties: UInt32 = 0x00050001 // Set only left and top borders!
    private var uri: String?
    private var valign = Align.TOP

    // Java's Cell defaults its properties to 0x00050001 - only the top and
    // left borders are on.
    internal var topBorder: Bool = true
    internal var bottomBorder: Bool = false
    internal var leftBorder: Bool = true
    internal var rightBorder: Bool = false

    private var underline: Bool
    private var strikeout: Bool

    /**
     * Creates a cell object and sets the font and, optionally, the cell text.
     * The font is also the fallback font until setFallbackFont changes it.
     *
     * - Parameter font: the font.
     * - Parameter text: the text.
     */
    public init(_ font: Font?, _ text: String? = nil) {
        self.font = font
        self.fallbackFont = font
        if font != nil {
            self.fontSize = font!.size
        }
        self.text = text
        self.underline = false
        self.strikeout = false
    }

    /**
     * Sets the font for this cell. The font size does not change; set it with setFontSize.
     *
     * - Parameter font: the font.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setFont(_ font: Font?) -> Cell {
        self.font = font
        return self
    }

    /**
     * Sets the fallback font for this cell.
     *
     * - Parameter fallbackFont: the fallback font.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> Cell {
        self.fallbackFont = fallbackFont
        return self
    }

    /**
     * Returns the font used by this cell.
     *
     * - Returns: the font.
     */
    public func getFont() -> Font? {
        return self.font
    }

    /**
     * Returns the fallback font used by this cell.
     *
     * - Returns: the fallback font.
     */
    public func getFallbackFont() -> Font? {
        return self.fallbackFont
    }

    /**
     * Sets the cell text.
     *
     * - Parameter text: the cell text.
     */
    @discardableResult
    public func setText(_ text: String?) -> Cell {
        self.text = text
        return self
    }

    /**
     * Returns the cell text.
     *
     * - Returns: the cell text.
     */
    public func getText() -> String? {
        return self.text
    }

    /// Sets the font size of the cell text.
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> Cell {
        self.fontSize = fontSize
        return self
    }

    /**
     * Sets the image inside this cell and clears the cell text.
     *
     * - Parameter image: the image.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setImage(_ image: Image?) -> Cell {
        self.image = image
        self.text = nil
        return self
    }

    /**
     * Returns the cell image.
     *
     * - Returns: the image.
     */
    public func getImage() -> Image? {
        return self.image
    }

    /// Sets the barcode drawn in this cell and clears the cell text.
    @discardableResult
    public func setBarcode(_ barcode: Barcode) -> Cell {
        self.barcode = barcode
        self.text = nil
        return self
    }

    /// Returns the barcode drawn in this cell.
    public func getBarcode() -> Barcode? {
        return self.barcode
    }

    /**
     * Sets the point inside this cell.
     * See the Point class and Example_09 for more information.
     *
     * - Parameter point: the point.
     */
    @discardableResult
    public func setPoint(_ point: Point?) -> Cell {
        self.point = point
        return self
    }

    /**
     * Returns the cell point.
     *
     * - Returns: the point.
     */
    public func getPoint() -> Point? {
        return self.point
    }

    /**
     * Sets the composite text object.
     *
     * - Parameter compositeTextLine: the composite text object.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setCompositeTextLine(_ compositeTextLine: CompositeTextLine?) -> Cell {
        self.compositeTextLine = compositeTextLine
        return self
    }

    /**
     * Returns the composite text object.
     *
     * - Returns: the composite text object.
     */
    public func getCompositeTextLine() -> CompositeTextLine? {
        return self.compositeTextLine
    }

    /**
     * Sets the width of this cell.
     *
     * - Parameter width: the specified width.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setWidth(_ width: Float) -> Cell {
        self.width = width
        if self.textBox != nil {
            self.textBox!.setWidth(self.width - (self.leftPadding + self.rightPadding))
        } else if self.textBlock != nil {
            self.textBlock!.setWidth(self.width - (self.leftPadding + self.rightPadding))
        }
        return self
    }

    ///
    /// Sets the text column that this cell holds, and widens the cell to fit it.
    ///
    @discardableResult
    public func setTextColumn(_ textColumn: TextColumn) -> Cell {
        self.textColumn = textColumn
        self.width = textColumn.getWidth() + self.leftPadding + self.rightPadding
        return self
    }

    ///
    /// Returns the text column that this cell holds.
    ///
    public func getTextColumn() -> TextColumn? {
        return self.textColumn
    }

    ///
    /// Sets the text box that this cell holds and clears the cell text.
    ///
    @discardableResult
    public func setTextBox(_ textBox: TextBox) -> Cell {
        self.textBox = textBox
        self.text = nil
        return self
    }

    ///
    /// Returns the text box that this cell holds.
    ///
    public func getTextBox() -> TextBox? {
        return self.textBox
    }

    /// Sets the text block drawn in this cell.
    @discardableResult
    public func setTextBlock(_ textBlock: TextBlock) -> Cell {
        self.textBlock = textBlock
        return self
    }

    /**
     * Returns the cell width.
     *
     * - Returns: the cell width.
     */
    public func getWidth() -> Float {
        return self.width
    }

    /**
     * Sets the top padding of this cell.
     *
     * - Parameter padding: the top padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setTopPadding(_ padding: Float) -> Cell {
        self.topPadding = padding
        return self
    }

    /// Returns the top padding.
    public func getTopPadding() -> Float {
        return self.topPadding
    }

    /**
     * Sets the bottom padding of this cell.
     *
     * - Parameter padding: the bottom padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setBottomPadding(_ padding: Float) -> Cell {
        self.bottomPadding = padding
        return self
    }

    /// Returns the bottom padding.
    public func getBottomPadding() -> Float {
        return self.bottomPadding
    }

    /**
     * Sets the left padding of this cell.
     *
     * - Parameter padding: the left padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setLeftPadding(_ padding: Float) -> Cell {
        self.leftPadding = padding
        return self
    }

    /// Returns the left padding.
    public func getLeftPadding() -> Float {
        return self.leftPadding
    }

    /**
     * Sets the right padding of this cell.
     *
     * - Parameter padding: the right padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setRightPadding(_ padding: Float) -> Cell {
        self.rightPadding = padding
        return self
    }

    /// Returns the right padding.
    public func getRightPadding() -> Float {
        return self.rightPadding
    }

    /**
     * Sets the top, bottom, left and right paddings of this cell.
     *
     * - Parameter padding: the right padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setPadding(_ padding: Float) -> Cell {
        self.topPadding = padding
        self.bottomPadding = padding
        self.leftPadding = padding
        self.rightPadding = padding
        return self
    }

    /**
     * Returns the cell height.
     *
     * - Returns: the cell height.
     */
    public func getHeight(_ width: Float) -> Float {
        var cellHeight = Float(0.0)
        if textBox != nil {
            textBox!.setWidth(width)
            cellHeight = (textBox!.drawOn(nil)[1] - textBox!.y) + topPadding + bottomPadding
        } else if textBlock != nil {
            textBlock!.setWidth(width)
            cellHeight = (textBlock!.drawOn(nil)[1] - textBlock!.y) + topPadding + bottomPadding
        } else if textColumn != nil {
            cellHeight = (textColumn!.drawOn(nil)[1] - textColumn!.y) + topPadding + bottomPadding
        } else if image != nil {
            cellHeight = image!.getHeight() + topPadding + bottomPadding
        } else if barcode != nil {
            cellHeight = barcode!.getHeight() + topPadding + bottomPadding
        } else if text != nil {
            var fontHeight = font!.getHeight()
            if fallbackFont != nil && fallbackFont!.getHeight() > fontHeight {
                fontHeight = fallbackFont!.getHeight()
            }
            cellHeight = fontHeight + topPadding + bottomPadding
        }
        return cellHeight
    }

    /// Sets the text color as a 0xRRGGBB value.
    @discardableResult
    public func setTextColor(_ color: Int32) -> Cell {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.textColor = [r, g, b]
        return self
    }

    /// Sets the text color from red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setTextColor(_ r: Float, _ g: Float, _ b: Float) -> Cell {
        self.textColor = [r, g, b]
        return self
    }

    /// Sets the text color from an array of red, green and blue values.
    @discardableResult
    public func setTextColor(_ textColor: [Float]) -> Cell {
        self.textColor = textColor
        return self
    }

    /// Returns the text color.
    public func getTextColor() -> [Float] {
        return self.textColor
    }

    /// Sets the background color as a 0xRRGGBB value.
    @discardableResult
    public func setBackgroundColor(_ color: Int32) -> Cell {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.backgroundColor = [r, g, b]
        return self
    }

    /// Sets the background color from red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setBackgroundColor(_ r: Float, _ g: Float, _ b: Float) -> Cell {
        self.backgroundColor = [r, g, b]
        return self
    }

    /// Sets the background color from an array of red, green and blue values,
    /// or removes the background with nil.
    @discardableResult
    public func setBackgroundColor(_ backgroundColor: [Float]?) -> Cell {
        self.backgroundColor = backgroundColor
        return self
    }

    /// Returns the background color, or nil if the cell has no background.
    public func getBackgroundColor() -> [Float]? {
        return self.backgroundColor
    }

    /// Sets the stroke color as a 0xRRGGBB value.
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> Cell {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.strokeColor = [r, g, b]
        return self
    }

    /// Sets the stroke color from red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setStrokeColor(_ r: Float, _ g: Float, _ b: Float) -> Cell {
        self.strokeColor = [r, g, b]
        return self
    }

    /// Sets the stroke color from an array of red, green and blue values.
    @discardableResult
    public func setStrokeColor(_ rgbColor: [Float]?) -> Cell {
        self.strokeColor = rgbColor
        return self
    }

    /// Returns the stroke color.
    public func getStrokeColor() -> [Float]? {
        return self.strokeColor
    }

    /// Sets the width of the cell borders.
    @discardableResult
    public func setStrokeWidth(_ strokeWidth: Float) -> Cell {
        self.strokeWidth = strokeWidth
        return self
    }

    /// Returns the width of the cell borders.
    public func getStrokeWidth() -> Float {
        return self.strokeWidth
    }

    func setProperties(_ properties: UInt32) {
        self.properties = properties
    }

    func getProperties() -> UInt32 {
        return self.properties
    }

    /**
     * Sets the column span private variable.
     *
     * - Parameter colspan: the specified column span value.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setColSpan(_ colspan: UInt32) -> Cell {
        self.properties &= 0x00FF0000
        self.properties |= (colspan & 0x0000FFFF)
        return self
    }

    /**
     * Returns the column span private variable value.
     *
     * - Returns: the column span value.
     */
    public func getColSpan() -> UInt32 {
        return (self.properties & 0x0000FFFF)
    }

    /// Sets whether the specified borders, for example Border.TOP | Border.BOTTOM, are drawn.
    @discardableResult
    public func setBorder(_ border: UInt32, _ visible: Bool) -> Cell {
        if border & Border.TOP != 0 {
            self.topBorder = visible
        }
        if border & Border.BOTTOM != 0 {
            self.bottomBorder = visible
        }
        if border & Border.LEFT != 0 {
            self.leftBorder = visible
        }
        if border & Border.RIGHT != 0 {
            self.rightBorder = visible
        }
        return self
    }

    /// Returns true if any of the specified borders, for example Border.TOP, is drawn.
    public func getBorder(_ border: UInt32) -> Bool {
        return (border & Border.TOP != 0 && self.topBorder) ||
                (border & Border.BOTTOM != 0 && self.bottomBorder) ||
                (border & Border.LEFT != 0 && self.leftBorder) ||
                (border & Border.RIGHT != 0 && self.rightBorder)
    }

    /// Sets whether all four borders of this cell are drawn.
    @discardableResult
    public func setBorders(_ visible: Bool) -> Cell {
        return setBorder(Border.ALL, visible)
    }

    /// Sets whether the top border of this cell is drawn.
    @discardableResult
    public func setTopBorder(_ topBorder: Bool) -> Cell {
        self.topBorder = topBorder
        return self
    }

    /// Returns true if the top border of this cell is drawn.
    public func getTopBorder() -> Bool {
        return self.topBorder
    }

    /// Sets whether the bottom border of this cell is drawn.
    @discardableResult
    public func setBottomBorder(_ bottomBorder: Bool) -> Cell {
        self.bottomBorder = bottomBorder
        return self
    }

    /// Returns true if the bottom border of this cell is drawn.
    public func getBottomBorder() -> Bool {
        return self.bottomBorder
    }

    /// Sets whether the left border of this cell is drawn.
    @discardableResult
    public func setLeftBorder(_ leftBorder: Bool) -> Cell {
        self.leftBorder = leftBorder
        return self
    }

    /// Returns true if the left border of this cell is drawn.
    public func getLeftBorder() -> Bool {
        return self.leftBorder
    }

    /// Sets whether the right border of this cell is drawn.
    @discardableResult
    public func setRightBorder(_ rightBorder: Bool) -> Cell {
        self.rightBorder = rightBorder
        return self
    }

    /// Returns true if the right border of this cell is drawn.
    public func getRightBorder() -> Bool {
        return self.rightBorder
    }

    /**
     * Sets the cell text alignment.
     *
     * - Parameter alignment: the alignment code.
     * Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY,
     * which draws the single line of cell text left aligned.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setTextAlignment(_ alignment: UInt32) -> Cell {
        self.properties &= 0x00CFFFFF
        self.properties |= (alignment & 0x00300000)
        return self
    }

    /**
     * Returns the text alignment.
     *
     * - Returns: the text horizontal alignment code.
     */
    public func getTextAlignment() -> UInt32 {
        return (self.properties & 0x00300000)
    }

    /**
     * Sets the cell text vertical alignment.
     *
     * - Parameter alignment: the alignment code.
     * Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setVerTextAlignment(_ alignment: UInt32) -> Cell {
        self.valign = alignment
        return self
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * - Returns: the vertical alignment code.
     */
    public func getVerTextAlignment() -> UInt32 {
        return self.valign
    }

    /**
     * Sets the underline text parameter.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * - Parameter underline: the underline text parameter.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setUnderline(_ underline: Bool) -> Cell {
        self.underline = underline
        return self
    }

    /**
     * Returns the underline text parameter.
     *
     * - Returns: the underline text parameter.
     */
    public func getUnderline() -> Bool {
        return self.underline
    }

    /**
     * Sets the strikeout text parameter.
     *
     * - Parameter strikeout: the strikeout text parameter.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setStrikeout(_ strikeout: Bool) -> Cell {
        self.strikeout = strikeout
        return self
    }

    /**
     * Returns the strikeout text parameter.
     *
     * - Returns: the strikeout text parameter.
     */
    public func getStrikeout() -> Bool {
        return self.strikeout
    }

    /// Sets the URI opened when this cell is clicked.
    @discardableResult
    public func setURIAction(_ uri: String) -> Cell {
        self.uri = uri
        return self
    }

    /**
     * Draws the point, text and borders of this cell.
     */
    func drawOn(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        if backgroundColor != nil {
            drawBackground(page, x, y, w, h)
        }

        if text != nil && text != "" {
            drawText(page, x, y, w, h)
        } else if textBox != nil {
            textBox!.setLocation(x + leftPadding, y + topPadding)
            textBox!.setWidth(w - (leftPadding + rightPadding))
            textBox!.drawOn(page)
        } else if textBlock != nil {
            textBlock!.setLocation(x + leftPadding, y + topPadding)
            textBlock!.setWidth(w - (leftPadding + rightPadding))
            textBlock!.drawOn(page)
        } else if textColumn != nil {
            textColumn!.setLocation(x + leftPadding, y + topPadding)
            textColumn!.drawOn(page)
        } else if image != nil {
            if getTextAlignment() == Align.RIGHT {
                image!.setLocation((x + w) - (image!.getWidth() + rightPadding), y + topPadding)
            } else if getTextAlignment() == Align.CENTER {
                image!.setLocation((x + w/2.0) - image!.getWidth()/2.0, y + topPadding)
            } else {
                image!.setLocation(x + leftPadding, y + topPadding)
            }
            image!.drawOn(page)
        } else if barcode != nil {
            if getTextAlignment() == Align.RIGHT {
                let barcodeWidth = barcode!.drawOn(nil)[0]
                barcode!.drawOnPageAtLocation(page, (x + w) - (barcodeWidth + rightPadding), y + topPadding)
            } else if getTextAlignment() == Align.CENTER {
                let barcodeWidth = barcode!.drawOn(nil)[0]
                barcode!.drawOnPageAtLocation(page, (x + w/2.0) - barcodeWidth/2.0, y + topPadding)
            } else {
                barcode!.drawOnPageAtLocation(page, x + leftPadding, y + topPadding)
            }
        }

        drawBorders(page, x, y, w, h)
        if point != nil {
            if point!.align == Align.LEFT {
                point!.x = x + 2*point!.r
            } else if point!.align == Align.RIGHT {
                point!.x = (x + w) - self.rightPadding/2
            }
            point!.y = y + h/2
            page.setBrushColor(point!.getFillColor())
            if point!.getURIAction() != nil {
                page.addAnnotation(Annotation(
                        Annotation.Link,
                        point!.x - point!.r,
                        point!.y - point!.r,
                        point!.x + point!.r,
                        point!.y + point!.r,
                        nil,    // Vertices
                        nil,    // Fill Color
                        0.0,    // Transparency
                        nil,    // Title
                        nil,    // Contents
                        point!.getURIAction(),
                        nil,
                        nil,
                        nil,
                        nil))
            }
            page.drawPoint(point!)
        }
    }

    private func drawBackground(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ cellW: Float,
            _ cellH: Float) {
        page.addArtifactBMC()
        page.setBrushColor(backgroundColor!)
        page.fillRect(x, y + strokeWidth/2, cellW, cellH)
        page.addEMC()
    }

    private func drawBorders(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ cellW: Float,
            _ cellH: Float) {
        page.addArtifactBMC()
        page.setPenColor(strokeColor)
        page.setPenWidth(strokeWidth)
        let qWidth: Float = strokeWidth / 4.0
        if topBorder {
            page.moveTo(x - qWidth, y)
            page.lineTo(x + cellW, y)
            page.strokePath()
        }
        if bottomBorder {
            page.moveTo(x - qWidth, y + cellH)
            page.lineTo(x + cellW, y + cellH)
            page.strokePath()
        }
        if leftBorder {
            page.moveTo(x, y - qWidth)
            page.lineTo(x, y + cellH + qWidth)
            page.strokePath()
        }
        if rightBorder {
            page.moveTo(x + cellW, y - qWidth)
            page.lineTo(x + cellW, y + cellH + qWidth)
            page.strokePath()
        }
        page.addEMC()
    }

    private func drawText(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ cellW: Float,
            _ cellH: Float) {
        let ascent = font!.getAscent(fontSize)
        var yText: Float
        if valign == Align.TOP {
            yText = y + ascent + self.topPadding
        } else if valign == Align.CENTER {
            yText = y + cellH/2 + ascent/2
        } else if valign == Align.BOTTOM {
            yText = (y + cellH) - self.bottomPadding
        } else {
            fatalError("Invalid vertical text alignment option.")
        }

        page.setPenColor(strokeColor)
        var xText: Float
        if getTextAlignment() == Align.RIGHT {
            xText = (x + cellW) - (getTextWidth() + self.rightPadding)
        } else if getTextAlignment() == Align.CENTER {
            xText = x + self.leftPadding +
                    (((cellW - (leftPadding + rightPadding)) - getTextWidth()) / 2)
        } else {
            // Align.LEFT, and Align.JUSTIFY, which a single line of text cannot use.
            xText = x + self.leftPadding
        }
        if compositeTextLine == nil {
            page.addBMC(StructElem.P, text!, text!)
            page.drawString(font!, fallbackFont, fontSize, text!, xText, yText, textColor, nil)
            page.addEMC()
            if getUnderline() {
                underlineText(page, xText, yText)
            }
            if getStrikeout() {
                strikeoutText(page, xText, yText)
            }
        } else {
            compositeTextLine!.setLocation(xText, yText)
            // The text lines of the composite mark their own text.
            compositeTextLine!.drawOn(page)
        }

        if uri != nil {
            page.addAnnotation(Annotation(
                    Annotation.Link,
                    xText,
                    yText - ascent,
                    xText + getTextWidth(),
                    yText + font!.getDescent(fontSize),
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Transparency
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    nil,
                    nil,
                    nil,
                    nil))
        }
    }

    // Returns the width of the composite text line, or of the cell text drawn
    // with the font and the fallback font at the font size of this cell.
    private func getTextWidth() -> Float {
        if compositeTextLine != nil {
            return compositeTextLine!.getWidth()
        }
        return font!.stringWidth(fallbackFont, fontSize, text)
    }

    private func underlineText(_ page: Page, _ x: Float, _ y: Float) {
        let descent = font!.getDescent(fontSize)
        page.addBMC(StructElem.P, "underline", "underline")
        page.setPenWidth(font!.getUnderlineThickness(fontSize))
        page.moveTo(x, y + descent)
        page.lineTo(x + getTextWidth(), y + descent)
        page.strokePath()
        page.addEMC()
    }

    private func strikeoutText(_ page: Page, _ x: Float, _ y: Float) {
        let ascent = font!.getAscent(fontSize)
        page.addBMC(StructElem.P, "strike out", "strike out")
        page.setPenWidth(font!.getUnderlineThickness(fontSize))
        page.moveTo(x, y - ascent/3.0)
        page.lineTo(x + getTextWidth(), y - ascent/3.0)
        page.strokePath()
        page.addEMC()
    }

    /// Returns the text block drawn in this cell.
    public func getTextBlock() -> TextBlock? {
        return textBlock
    }
}   // End of Cell.swift
