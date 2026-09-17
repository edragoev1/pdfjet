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
    internal var font: Font
    internal var fallbackFont: Font?
    internal var fontSize: Float = 12.0
    var text: String?
    var drawable: Drawable?     // The image, barcode, text block, text column or other drawable
    var point: Point?
    private var markerAlignment = Alignment.RIGHT
    var width: Float = 75.0
    var topPadding: Float = 2.0
    var bottomPadding: Float = 2.0
    var leftPadding: Float = 2.0
    var rightPadding: Float = 2.0

    // The colors are packed 0xRRGGBB values, 4 bytes each instead of an array
    // for every cell; NO_COLOR marks a color that is not set.
    static let NO_COLOR: Int32 = -1
    var backgroundColor: Int32 = Cell.NO_COLOR
    var textColor: Int32 = 0x000000
    var borderWidth: Float = 0.0
    var borderColor: Int32 = Cell.NO_COLOR

    private var colspan: Int = 1
    private var uri: String?
    private var textAlignment = Alignment.LEFT
    private var valign = Alignment.TOP

    // Only the top and left borders are drawn unless setBorder says otherwise.
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
    public init(_ font: Font, _ text: String? = nil) {
        self.font = font
        self.fallbackFont = font
        self.fontSize = font.size
        self.text = text
        self.underline = false
        self.strikeout = false
    }

    /**
     * Sets the font for this cell. The font size does not change; set it with setFontSize.
     * The fallback font changes with the font, unless a different fallback font was set.
     *
     * - Parameter font: the font.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setFont(_ font: Font) -> Cell {
        if self.fallbackFont === self.font {
            self.fallbackFont = font
        }
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
    public func getFont() -> Font {
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
        return setDrawable(image)
    }

    /**
     * Returns the cell image.
     *
     * - Returns: the image, or nil when the cell holds none.
     */
    public func getImage() -> Image? {
        return drawable as? Image
    }

    ///
    /// Sets the drawable inside this cell and clears the cell text. A cell holds one drawable,
    /// so this replaces the image, barcode, text block or text column set before. The drawable
    /// is placed by its top left corner, at the padding, and aligned in the cell as the text is;
    /// it is measured with drawOn(nil). A text block gets the width of the cell.
    ///
    /// - Parameter drawable: the drawable, for example a QRCode, an SVGImage or a Table.
    /// - Returns: this Cell object.
    ///
    @discardableResult
    public func setDrawable(_ drawable: Drawable?) -> Cell {
        self.drawable = drawable
        self.text = nil
        return self
    }

    /// Returns the drawable inside this cell, or nil.
    public func getDrawable() -> Drawable? {
        return drawable
    }

    /// Sets the barcode drawn in this cell and clears the cell text.
    @discardableResult
    public func setBarcode(_ barcode: Barcode) -> Cell {
        return setDrawable(barcode)
    }

    /// Returns the barcode drawn in this cell, or nil when the cell holds none.
    public func getBarcode() -> Barcode? {
        return drawable as? Barcode
    }

    /**
     * Sets the marker drawn in this cell: a Point, placed at the left or the
     * right of the cell and centered vertically.
     * See the Point class and Example_09 for more information.
     *
     * - Parameter point: the point.
     * - Parameter alignment: Alignment.LEFT or Alignment.RIGHT.
     */
    @discardableResult
    public func setMarker(_ point: Point?, _ alignment: Alignment) -> Cell {
        self.point = point
        self.markerAlignment = alignment
        return self
    }

    /**
     * Returns the marker drawn in this cell.
     *
     * - Returns: the point.
     */
    public func getMarker() -> Point? {
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
        // A composite text line is the content of the cell, in the drawable
        // it holds, and is drawn where the cell text would be. The cell text
        // is left as it is, and the composite is drawn instead of it.
        self.drawable = compositeTextLine
        return self
    }

    /**
     * Returns the composite text object.
     *
     * - Returns: the composite text object.
     */
    public func getCompositeTextLine() -> CompositeTextLine? {
        return drawable as? CompositeTextLine
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
        if let textBlock = drawable as? TextBlock {
            textBlock.setWidth(self.width - (self.leftPadding + self.rightPadding))
        }
        return self
    }

    ///
    /// Sets the text column that this cell holds, widens the cell to fit it
    /// and clears the cell text.
    ///
    @discardableResult
    public func setTextColumn(_ textColumn: TextColumn) -> Cell {
        self.width = textColumn.getWidth() + self.leftPadding + self.rightPadding
        return setDrawable(textColumn)
    }

    ///
    /// Returns the text column that this cell holds, or nil when it holds none.
    ///
    public func getTextColumn() -> TextColumn? {
        return drawable as? TextColumn
    }

    /// Sets the text block drawn in this cell and clears the cell text.
    @discardableResult
    public func setTextBlock(_ textBlock: TextBlock) -> Cell {
        return setDrawable(textBlock)
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
        if drawable is BaselineDrawable {
            // A line of text is drawn on the baseline of the cell text, so the
            // cell makes room for the ascent and the descent of both.
            return ascentOfCell() + descentOfCell() + topPadding + bottomPadding
        }
        if let drawable = drawable, text == nil || text == "" {   // The text is drawn first
            if let textBlock = drawable as? TextBlock {
                textBlock.setWidth(width)
            }
            cellHeight = Cell.measure(drawable)[1] + topPadding + bottomPadding
        } else if text != nil {
            var fontHeight = font.getBodyHeight(fontSize)
            if fallbackFont != nil && fallbackFont!.getBodyHeight(fontSize) > fontHeight {
                fontHeight = fallbackFont!.getBodyHeight(fontSize)
            }
            cellHeight = fontHeight + topPadding + bottomPadding
        }
        return cellHeight
    }

    /// Sets the text color as a 0xRRGGBB value. Color.transparent leaves the text color unchanged.
    @discardableResult
    public func setTextColor(_ color: Int32) -> Cell {
        if color == Color.transparent {
            return self
        }
        self.textColor = color & 0xFFFFFF
        return self
    }

    /// Sets the text color from an array of red, green and blue values.
    /// The cell keeps each component to the nearest of 256 steps.
    @discardableResult
    public func setTextColor(_ textColor: [Float]) -> Cell {
        self.textColor = Util.toPackedRGB(textColor)
        return self
    }

    /// Returns the text color.
    public func getTextColor() -> [Float] {
        return Util.toRGB(self.textColor)
    }

    /// Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.
    @discardableResult
    public func setBackgroundColor(_ color: Int32) -> Cell {
        if color == Color.transparent {
            self.backgroundColor = Cell.NO_COLOR
            return self
        }
        self.backgroundColor = color & 0xFFFFFF
        return self
    }

    /// Sets the background color from an array of red, green and blue values,
    /// or removes the background with nil. The cell keeps each component to the
    /// nearest of 256 steps.
    @discardableResult
    public func setBackgroundColor(_ backgroundColor: [Float]?) -> Cell {
        self.backgroundColor = Util.toPackedRGB(backgroundColor)
        return self
    }

    /// Returns the background color, or nil if the cell has no background.
    public func getBackgroundColor() -> [Float]? {
        return (backgroundColor == Cell.NO_COLOR) ? nil : Util.toRGB(backgroundColor)
    }

    /// Sets the border color as a 0xRRGGBB value. Color.transparent leaves the
    /// borders the color of the pen the page draws with.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> Cell {
        if color == Color.transparent {
            self.borderColor = Cell.NO_COLOR
            return self
        }
        self.borderColor = color & 0xFFFFFF
        return self
    }

    /// Sets the border color from an array of red, green and blue values.
    /// The cell keeps each component to the nearest of 256 steps.
    @discardableResult
    public func setBorderColor(_ rgbColor: [Float]?) -> Cell {
        self.borderColor = Util.toPackedRGB(rgbColor)
        return self
    }

    /// Returns the border color, or nil if it is not set.
    public func getBorderColor() -> [Float]? {
        return (borderColor == Cell.NO_COLOR) ? nil : Util.toRGB(borderColor)
    }

    /// Sets the width of the cell borders.
    @discardableResult
    public func setBorderWidth(_ borderWidth: Float) -> Cell {
        self.borderWidth = borderWidth
        return self
    }

    /// Returns the width of the cell borders.
    public func getBorderWidth() -> Float {
        return self.borderWidth
    }

    /**
     * Sets the column span private variable.
     *
     * - Parameter colspan: the specified column span value.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setColSpan(_ colspan: Int) -> Cell {
        self.colspan = colspan
        return self
    }

    /**
     * Returns the column span private variable value.
     *
     * - Returns: the column span value.
     */
    public func getColSpan() -> Int {
        return self.colspan
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

    /**
     * Sets the cell text alignment.
     *
     * - Parameter alignment: the alignment.
     * Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY,
     * which draws the single line of cell text left aligned.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setTextAlignment(_ alignment: Alignment) -> Cell {
        self.textAlignment = alignment
        return self
    }

    /**
     * Returns the text alignment.
     *
     * - Returns: the horizontal text alignment.
     */
    public func getTextAlignment() -> Alignment {
        return self.textAlignment
    }

    /**
     * Sets the cell text vertical alignment.
     *
     * - Parameter alignment: the alignment.
     * Supported values: Alignment.TOP, Alignment.CENTER and Alignment.BOTTOM.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setVerticalAlignment(_ alignment: Alignment) -> Cell {
        self.valign = alignment
        return self
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * - Returns: the vertical alignment.
     */
    public func getVerticalAlignment() -> Alignment {
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
        if backgroundColor != Cell.NO_COLOR {
            drawBackground(page, x, y, w, h)
        }

        if drawable is BaselineDrawable || (text != nil && text != "") {
            // A line of text is drawn instead of the cell text, on its baseline.
            drawText(page, x, y, w, h)
        } else if let textBlock = drawable as? TextBlock {
            textBlock.setLocation(x + leftPadding, y + topPadding)
            textBlock.setWidth(w - (leftPadding + rightPadding))
            textBlock.drawOn(page)
        } else if let drawable = drawable {
            if getTextAlignment() == Alignment.RIGHT {
                let drawableWidth = Cell.measure(drawable)[0]
                drawable.setLocation((x + w) - (drawableWidth + rightPadding), y + topPadding)
            } else if getTextAlignment() == Alignment.CENTER {
                let drawableWidth = Cell.measure(drawable)[0]
                drawable.setLocation((x + w/2.0) - drawableWidth/2.0, y + topPadding)
            } else {
                drawable.setLocation(x + leftPadding, y + topPadding)
            }
            drawable.drawOn(page)
        }

        drawBorders(page, x, y, w, h)
        if point != nil {
            if markerAlignment == Alignment.LEFT {
                point!.x = x + 2*point!.r
            } else if markerAlignment == Alignment.RIGHT {
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
                        0.0,    // Opacity
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
        page.fillRect(x, y + borderWidth/2, cellW, cellH)
        page.addEMC()
    }

    private func drawBorders(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ cellW: Float,
            _ cellH: Float) {
        if !topBorder && !bottomBorder && !leftBorder && !rightBorder {
            return      // Nothing to draw, so nothing to write.
        }
        page.addArtifactBMC()
        if borderColor != Cell.NO_COLOR {
            page.setPenColor(borderColor)
        }
        page.setPenWidth(borderWidth)
        // Half the pen width, so that the corners of the borders close.
        let hWidth: Float = borderWidth / 2.0
        // The borders of a cell are the subpaths of one path, stroked once.
        if topBorder {
            page.moveTo(x - hWidth, y)
            page.lineTo(x + cellW, y)
        }
        if bottomBorder {
            page.moveTo(x - hWidth, y + cellH)
            page.lineTo(x + cellW, y + cellH)
        }
        if leftBorder {
            page.moveTo(x, y - hWidth)
            page.lineTo(x, y + cellH + hWidth)
        }
        if rightBorder {
            page.moveTo(x + cellW, y - hWidth)
            page.lineTo(x + cellW, y + cellH + hWidth)
        }
        page.strokePath()
        page.addEMC()
    }

    private func drawText(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ cellW: Float,
            _ cellH: Float) {
        let ascent = ascentOfCell()
        var yText: Float
        if valign == Alignment.TOP {
            yText = y + ascent + self.topPadding
        } else if valign == Alignment.CENTER {
            yText = y + cellH/2 + ascent/2
        } else if valign == Alignment.BOTTOM {
            yText = (y + cellH) - self.bottomPadding
        } else {
            fatalError("Invalid vertical text alignment option.")
        }

        var xText: Float
        if getTextAlignment() == Alignment.RIGHT {
            xText = (x + cellW) - (getTextWidth() + self.rightPadding)
        } else if getTextAlignment() == Alignment.CENTER {
            xText = x + self.leftPadding +
                    (((cellW - (leftPadding + rightPadding)) - getTextWidth()) / 2)
        } else {
            // Alignment.LEFT, and Alignment.JUSTIFY, which a single line of text cannot use.
            xText = x + self.leftPadding
        }
        let line = drawable as? BaselineDrawable
        if line == nil {
            page.addBDC(StructElem.P, text!, text!)
            page.drawString(font, fallbackFont, fontSize, text!, xText, yText, Util.toRGB(textColor), nil)
            page.addEMC()
            if getUnderline() {
                underlineText(page, xText, yText)
            }
            if getStrikeout() {
                strikeoutText(page, xText, yText)
            }
        } else {
            // A text line and a composite text line mark their own text.
            _ = line!.setLocation(xText, yText)
            // The text lines of the composite mark their own text.
            line!.drawOn(page)
        }

        if uri != nil {
            page.addAnnotation(Annotation(
                    Annotation.Link,
                    xText,
                    yText - ascent,
                    xText + getTextWidth(),
                    yText + descentOfCell(),
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Opacity
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
        if let line = drawable as? BaselineDrawable {
            return line.getWidth()
        }
        return font.stringWidth(fallbackFont, fontSize, text)
    }

    private func underlineText(_ page: Page, _ x: Float, _ y: Float) {
        let descent = font.getDescent(fontSize)
        page.addBDC(StructElem.P, "underline", "underline")
        if textColor != Cell.NO_COLOR {
            page.setPenColor(textColor)
        }
        page.setPenWidth(font.getUnderlineThickness(fontSize))
        page.moveTo(x, y + descent)
        page.lineTo(x + getTextWidth(), y + descent)
        page.strokePath()
        page.addEMC()
    }

    private func strikeoutText(_ page: Page, _ x: Float, _ y: Float) {
        let ascent = font.getAscent(fontSize)
        page.addBDC(StructElem.P, "strike out", "strike out")
        if textColor != Cell.NO_COLOR {
            page.setPenColor(textColor)
        }
        page.setPenWidth(font.getUnderlineThickness(fontSize))
        page.moveTo(x, y - ascent/3.0)
        page.lineTo(x + getTextWidth(), y - ascent/3.0)
        page.strokePath()
        page.addEMC()
    }

    /// Returns the text block drawn in this cell, or nil when it holds none.
    public func getTextBlock() -> TextBlock? {
        return drawable as? TextBlock
    }

    // Returns the width and the height of the drawable: its corner when it is
    // placed at 0, 0 and measured without a page.
    static func measure(_ drawable: Drawable) -> [Float] {
        drawable.setLocation(0.0, 0.0)
        return drawable.drawOn(nil)
    }

    // How far above the baseline the cell draws: the ascent of its font, and
    // of the line of text it holds, whichever reaches higher.
    private func ascentOfCell() -> Float {
        var ascent = font.getAscent(fontSize)
        if let line = drawable as? BaselineDrawable {
            let lineAscent = line.getAscent()
            if lineAscent > ascent {
                ascent = lineAscent
            }
        }
        return ascent
    }

    // How far below the baseline the cell draws: the descent of its font, and
    // of the line of text it holds, whichever reaches lower.
    private func descentOfCell() -> Float {
        var descent = font.getDescent(fontSize)
        if let line = drawable as? BaselineDrawable {
            let lineDescent = line.getDescent()
            if lineDescent > descent {
                descent = lineDescent
            }
        }
        return descent
    }
}   // End of Cell.swift
