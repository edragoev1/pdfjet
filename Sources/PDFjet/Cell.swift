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
    var lineWidth: Float = 0.0

    var backgroundColor: [Float] = [1.0, 1.0, 1.0]
    var hasBackground: Bool = false
    var textColor: [Float] = [0.0, 0.0, 0.0]
    var strokeWidth: Float = 0.0
    var strokeColor: [Float]?
    var strokeDashPattern: String = "[] 0"  // Solid

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
     * Creates a cell object and sets the font and the cell text.
     *
     * @param font the font.
     * @param text the text.
     */
    public init(_ font: Font?, _ text: String?) {
        self.font = font
        self.text = text
        self.underline = false
        self.strikeout = false
    }

    /**
     * Sets the font for this cell.
     *
     * @param font the font.
     * @return this Cell object.
     */
    @discardableResult
    public func setFont(_ font: Font?) -> Cell {
        self.font = font
        return self
    }

    /**
     * Sets the fallback font for this cell.
     *
     * @param fallbackFont the fallback font.
     * @return this Cell object.
     */
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> Cell {
        self.fallbackFont = fallbackFont
        return self
    }

    /**
     * Returns the font used by this cell.
     *
     * @return the font.
     */
    public func getFont() -> Font? {
        return self.font
    }

    /**
     * Returns the fallback font used by this cell.
     *
     * @return the fallback font.
     */
    public func getFallbackFont() -> Font? {
        return self.fallbackFont
    }

    /**
     * Sets the cell text.
     *
     * @param text the cell text.
     */
    @discardableResult
    public func setText(_ text: String?) -> Cell {
        self.text = text
        return self
    }

    /**
     * Returns the cell text.
     *
     * @return the cell text.
     */
    public func getText() -> String? {
        return self.text
    }

    /**
     * Sets the image inside this cell.
     *
     * @param image the image.
     * @return this Cell object.
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
     * @return the image.
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

    /**
     * Sets the point inside this cell.
     * See the Point class and Example_09 for more information.
     *
     * @param point the point.
     */
    @discardableResult
    public func setPoint(_ point: Point?) -> Cell {
        self.point = point
        return self
    }

    /**
     * Returns the cell point.
     *
     * @return the point.
     */
    public func getPoint() -> Point? {
        return self.point
    }

    /**
     * Sets the composite text object.
     *
     * @param compositeTextLine the composite text object.
     * @return this Cell object.
     */
    @discardableResult
    public func setCompositeTextLine(_ compositeTextLine: CompositeTextLine?) -> Cell {
        self.compositeTextLine = compositeTextLine
        return self
    }

    /**
     * Returns the composite text object.
     *
     * @return the composite text object.
     */
    public func getCompositeTextLine() -> CompositeTextLine? {
        return self.compositeTextLine
    }

    /**
     * Sets the width of this cell.
     *
     * @param width the specified width.
     * @return this Cell object.
     */
    @discardableResult
    public func setWidth(_ width: Float) -> Cell {
        self.width = width
        if self.textBlock != nil {
            self.textBlock!.setWidth(self.width - (self.leftPadding + self.rightPadding))
        }
        return self
    }

    ///
    /// Sets the text column that this cell holds.
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
    /// Sets the text box that this cell holds.
    ///
    @discardableResult
    public func setTextBox(_ textBox: TextBox) -> Cell {
        self.textBox = textBox
        return self
    }

    ///
    /// Returns the text box that this cell holds.
    ///
    public func getTextBox() -> TextBox? {
        return self.textBox
    }

    /**
     * Returns the cell width.
     *
     * @return the cell width.
     */
    public func getWidth() -> Float {
        return self.width
    }

    /**
     * Sets the top padding of this cell.
     *
     * @param padding the top padding.
     * @return this Cell object.
     */
    @discardableResult
    public func setTopPadding(_ padding: Float) -> Cell {
        self.topPadding = padding
        return self
    }

    /**
     * Sets the bottom padding of this cell.
     *
     * @param padding the bottom padding.
     * @return this Cell object.
     */
    @discardableResult
    public func setBottomPadding(_ padding: Float) -> Cell {
        self.bottomPadding = padding
        return self
    }

    /**
     * Sets the left padding of this cell.
     *
     * @param padding the left padding.
     * @return this Cell object.
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
     * @param padding the right padding.
     * @return this Cell object.
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
     * @param padding the right padding.
     * @return this Cell object.
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
     * @return the cell height.
     */
    public func getHeight(_ width: Float) -> Float {
        var cellHeight = Float(0.0)
        if textBox != nil {
            textBox!.setWidth(width)
            cellHeight = (textBox!.drawOn(nil)[1] - textBox!.y) + topPadding + bottomPadding
        } else if textColumn != nil {
            cellHeight = (textColumn!.drawOn(nil)[1] - textColumn!.y) + topPadding + bottomPadding
        } else if textBlock != nil {
            textBlock!.setWidth(width)
            cellHeight = (textBlock!.drawOn(nil)[1] - textBlock!.y) + topPadding + bottomPadding
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
        self.hasBackground = true
        return self
    }

    /// Sets the background color from red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setBackgroundColor(_ r: Float, _ g: Float, _ b: Float) -> Cell {
        self.backgroundColor = [r, g, b]
        self.hasBackground = true
        return self
    }

    /// Sets the background color from an array of red, green and blue values.
    @discardableResult
    public func setBackgroundColor(_ backgroundColor: [Float]) -> Cell {
        self.backgroundColor = backgroundColor
        self.hasBackground = true
        return self
    }

    /// Returns the background color.
    public func getBackgroundColor() -> [Float] {
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

    /// Sets the stroke width.
    @discardableResult
    public func setLineWidth(_ width: Float) -> Cell {
        self.strokeWidth = width
        return self
    }

    func setStrokeWidth(_ strokeWidth: Float) {
        self.strokeWidth = strokeWidth
    }

    func getStrokeWidth() -> Float {
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
     * @param colspan the specified column span value.
     * @return this Cell object.
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
     * @return the column span value.
     */
    public func getColSpan() -> UInt32 {
        return (self.properties & 0x0000FFFF)
    }

    /// Sets whether all four borders of this cell are drawn.
    @discardableResult
    public func setAllBorders(_ visible: Bool) -> Cell {
        self.topBorder = visible
        self.bottomBorder = visible
        self.leftBorder = visible
        self.rightBorder = visible
        return self
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
     * @param alignment the alignment code.
     * Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     * @return this Cell object.
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
     * @return the text horizontal alignment code.
     */
    public func getTextAlignment() -> UInt32{
        return (self.properties & 0x00300000)
    }

    /**
     * Sets the cell text vertical alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.
     * @return this Cell object.
     */
    @discardableResult
    public func setVerTextAlignment(_ alignment: UInt32) -> Cell {
        self.valign = alignment
        return self
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * @return the vertical alignment code.
     */
    public func getVerTextAlignment() -> UInt32 {
        return self.valign
    }

    /**
     * Sets the underline text parameter.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline text parameter.
     * @return this Cell object.
     */
    @discardableResult
    public func setUnderline(_ underline: Bool) -> Cell {
        self.underline = underline
        return self
    }

    /**
     * Returns the underline text parameter.
     *
     * @return the underline text parameter.
     */
    public func getUnderline() -> Bool {
        return self.underline
    }

    /**
     * Sets the strikeout text parameter.
     *
     * @param strikeout the strikeout text parameter.
     * @return this Cell object.
     */
    @discardableResult
    public func setStrikeout(_ strikeout: Bool) -> Cell {
        self.strikeout = strikeout
        return self
    }

    /**
     * Returns the strikeout text parameter.
     *
     * @return the strikeout text parameter.
     */
    public func getStrikeout() -> Bool{
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
        if hasBackground == true {
            drawBackground(page, x, y, w, h)
        }

        if text != nil && text != "" {
            drawText(page, x, y, w, h)
        } else if textBox != nil {
            textBox!.setLocation(x + leftPadding, y + topPadding)
            textBox!.setWidth(w - (leftPadding + rightPadding))
            textBox!.drawOn(page)
        } else if textColumn != nil {
            textColumn!.setLocation(x + leftPadding, y + topPadding)
            textColumn!.drawOn(page)
        } else if textBlock != nil {
            textBlock!.setLocation(x + leftPadding, y + topPadding)
            textBlock!.setWidth(w - (leftPadding + rightPadding))
            textBlock!.drawOn(page)
        } else if image != nil {
            if (getTextAlignment() == Align.LEFT) {
                image!.setLocation(x + leftPadding, y + topPadding)
                image!.drawOn(page)
            } else if (getTextAlignment() == Align.CENTER) {
                image!.setLocation((x + w/2.0) - image!.getWidth()/2.0, y + topPadding)
                image!.drawOn(page)
            } else if (getTextAlignment() == Align.RIGHT) {
                image!.setLocation((x + w) - (image!.getWidth() + leftPadding), y + topPadding)
                image!.drawOn(page)
            }
        } else if barcode != nil {
            if (getTextAlignment() == Align.LEFT) {
                barcode!.drawOnPageAtLocation(page, x + leftPadding, y + topPadding)
            } else if (getTextAlignment() == Align.CENTER) {
                let barcodeWidth = barcode!.drawOn(nil)[0]
                barcode!.drawOnPageAtLocation(page, (x + w/2.0) - barcodeWidth/2.0, y + topPadding)
            } else if (getTextAlignment() == Align.RIGHT) {
                let barcodeWidth = barcode!.drawOn(nil)[0]
                barcode!.drawOnPageAtLocation(page, (x + w) - (barcodeWidth + leftPadding), y + topPadding)
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
        page.setBrushColor(backgroundColor)
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

        var xText: Float?
        var yText: Float?
        if valign == Align.TOP {
            yText = y + font!.ascent + self.topPadding
        } else if valign == Align.CENTER {
            yText = y + cellH/2 + font!.ascent/2
        } else if valign == Align.BOTTOM {
            yText = (y + cellH) - self.bottomPadding
        } else {
            Swift.print("Invalid vertical text alignment option.")
        }

        page.setPenColor(strokeColor)
        if getTextAlignment() == Align.RIGHT {
            if compositeTextLine == nil {
                xText = (x + cellW) - (font!.stringWidth(text) + self.rightPadding)
                page.addBMC(StructElem.P, text!, text!)
                page.drawString(font!, fallbackFont, font!.size, text!, xText!, yText!, textColor, nil) // TODO
                page.addEMC()
                if getUnderline() {
                    underlineText(page, font!, text!, xText!, yText!)
                }
                if getStrikeout() {
                    strikeoutText(page, font!, text!, xText!, yText!)
                }
            } else {
                xText = (x + cellW) - (compositeTextLine!.getWidth() + self.rightPadding)
                compositeTextLine!.setLocation(xText!, yText!)
                page.addBMC(StructElem.P, text!, text!)
                compositeTextLine!.drawOn(page)
                page.addEMC()
            }
        } else if getTextAlignment() == Align.CENTER {
            if compositeTextLine == nil {
                xText = x + self.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - font!.stringWidth(text)) / 2)
                page.addBMC(StructElem.P, text!, text!)
                page.drawString(font!, fallbackFont, font!.size, text!, xText!, yText!, textColor, nil) // TODO
                page.addEMC()
                if getUnderline() {
                    underlineText(page, font!, text!, xText!, yText!)
                }
                if getStrikeout() {
                    strikeoutText(page, font!, text!, xText!, yText!)
                }
            } else {
                xText = x + self.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - compositeTextLine!.getWidth()) / 2)
                compositeTextLine!.setLocation(xText!, yText!)
                page.addBMC(StructElem.P, text!, text!)
                compositeTextLine!.drawOn(page)
                page.addEMC()
            }
        } else if getTextAlignment() == Align.LEFT {
            xText = x + self.leftPadding
            if compositeTextLine == nil {
                page.addBMC(StructElem.P, text!, text!)
                page.drawString(font!, fallbackFont, font!.size, text!, xText!, yText!, textColor, nil) // TODO
                page.addEMC()
                if getUnderline() {
                    underlineText(page, font!, text!, xText!, yText!)
                }
                if getStrikeout() {
                    strikeoutText(page, font!, text!, xText!, yText!)
                }
            } else {
                compositeTextLine!.setLocation(xText!, yText!)
                page.addBMC(StructElem.P, text!, text!)
                compositeTextLine!.drawOn(page)
                page.addEMC()
            }
        } else {
            print("Invalid Text Alignment!")
        }

        if uri != nil {
            let w = (compositeTextLine != nil) ?
                    compositeTextLine!.getWidth() : font!.stringWidth(text)
            page.addAnnotation(Annotation(
                    Annotation.Link,
                    xText!,
                    (page.height - yText!) - font!.ascent,
                    xText! + w,
                    (page.height - yText!) + font!.descent,
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

    private func underlineText(
            _ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float) {
        page.addBMC(StructElem.P, "underline", "underline")
        page.setPenWidth(font.underlineThickness)
        page.moveTo(x, y + font.descent)
        page.lineTo(x + font.stringWidth(text), y + font.descent)
        page.strokePath()
        page.addEMC()
    }

    private func strikeoutText(
            _ page: Page, _ font: Font, _ text: String, _ x: Float, _ y: Float) {
        page.addBMC(StructElem.P, "strike out", "strike out")
        page.setPenWidth(font.underlineThickness)
        page.moveTo(x, y - font.getAscent()/3.0)
        page.lineTo(x + font.stringWidth(text), y - font.getAscent()/3.0)
        page.strokePath()
        page.addEMC()
    }

    /// Returns the text block drawn in this cell.
    public func getTextBlock() -> TextBlock? {
        return textBlock
    }
}   // End of Cell.swift
