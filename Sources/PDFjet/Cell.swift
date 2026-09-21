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
    var width: Float = 75.0
    // The four paddings are the four bytes of one UInt32, 4 bytes instead of
    // 16 for every cell. A padding is kept to the nearest quarter of a point,
    // between 0 and 63.75, which is more than the text of a cell needs.
    private var padding: UInt32 = 0

    // The colors are packed 0xRRGGBB values, 4 bytes each instead of an array
    // for every cell; NO_COLOR marks a background or a border that is not set.
    static let NO_COLOR: Int32 = -1
    var backgroundColor: Int32 = Cell.NO_COLOR
    var textColor: Int32 = 0x000000 // Black until it is set
    var borderWidth: Float = 0.0
    var borderColor: Int32 = Cell.NO_COLOR

    private var colspan: Int = 1
    private var rowspan: Int = 1
    // The rows of the table as it is drawn that the cell spans, which is its
    // row span with the rows the wrapped text of each of them needs.
    var rowsSpanned: Int = 1
    private var uri: String?

    // The borders and the underline and strikeout of the text are the bits of
    // one UInt32, 4 bytes instead of 6 Bools and the padding they need. The
    // four borders are the bits Border gives them; only the top and the left
    // are drawn unless setBorder says otherwise.
    private static let UNDERLINE: UInt32 = 0x00100000
    private static let STRIKEOUT: UInt32 = 0x00200000
    // A cell that a table adds below another to hold the next line of its
    // wrapped text, which is the same table cell in a PDF/UA document.
    internal static let CONTINUED: UInt32 = 0x00400000
    // A cell that the cell above it spans over, which draws nothing: the cell
    // that spans the rows draws its text, background and borders over it.
    internal static let COVERED: UInt32 = 0x00800000
    internal var properties: UInt32 = Border.TOP | Border.LEFT

    // Where the three alignments of a cell are in properties, three bits
    // each, and where the four paddings are in padding, a byte each.
    private static let markerAlignmentShift: UInt32 = 0
    private static let textAlignmentShift: UInt32 = 3
    private static let valignShift: UInt32 = 6
    private static let alignmentBits: UInt32 = 0x7

    private static let topPaddingShift: UInt32 = 0
    private static let bottomPaddingShift: UInt32 = 8
    private static let leftPaddingShift: UInt32 = 16
    private static let rightPaddingShift: UInt32 = 24
    private static let paddingBits: UInt32 = 0xFF
    // A padding is kept in quarters of a point.
    private static let paddingScale: Float = 4.0

    // The alignment at the bits of properties.
    private func alignmentAt(_ shift: UInt32) -> Alignment {
        return Alignment(rawValue: (properties >> shift) & Cell.alignmentBits) ?? Alignment.LEFT
    }

    // Keeps the alignment in the bits of properties.
    private func setAlignmentAt(_ shift: UInt32, _ alignment: Alignment) {
        properties = (properties & ~(Cell.alignmentBits << shift))
                | ((alignment.rawValue & Cell.alignmentBits) << shift)
    }

    // The padding at the byte of padding, in points.
    private func paddingAt(_ shift: UInt32) -> Float {
        return Float((padding >> shift) & Cell.paddingBits) / Cell.paddingScale
    }

    // Keeps the padding in the byte of padding, to the nearest quarter of a
    // point and between 0 and 63.75.
    private func setPaddingAt(_ shift: UInt32, _ points: Float) {
        var quarters = Int(points * Cell.paddingScale + 0.5)
        if quarters < 0 {
            quarters = 0
        } else if quarters > Int(Cell.paddingBits) {
            quarters = Int(Cell.paddingBits)
        }
        padding = (padding & ~(Cell.paddingBits << shift)) | (UInt32(quarters) << shift)
    }

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
        setPaddingAt(Cell.topPaddingShift, 2.0)
        setPaddingAt(Cell.bottomPaddingShift, 2.0)
        setPaddingAt(Cell.leftPaddingShift, 2.0)
        setPaddingAt(Cell.rightPaddingShift, 2.0)
        setAlignmentAt(Cell.markerAlignmentShift, Alignment.RIGHT)
        setAlignmentAt(Cell.textAlignmentShift, Alignment.LEFT)
        setAlignmentAt(Cell.valignShift, Alignment.TOP)
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
        setAlignmentAt(Cell.markerAlignmentShift, alignment)
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
            textBlock.setWidth(self.width - (paddingAt(Cell.leftPaddingShift) + paddingAt(Cell.rightPaddingShift)))
        }
        return self
    }

    ///
    /// Sets the text column that this cell holds, widens the cell to fit it
    /// and clears the cell text.
    ///
    @discardableResult
    public func setTextColumn(_ textColumn: TextColumn) -> Cell {
        self.width = textColumn.getWidth() + paddingAt(Cell.leftPaddingShift) + paddingAt(Cell.rightPaddingShift)
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
        setPaddingAt(Cell.topPaddingShift, padding)
        return self
    }

    /// Returns the top padding.
    public func getTopPadding() -> Float {
        return paddingAt(Cell.topPaddingShift)
    }

    /**
     * Sets the bottom padding of this cell.
     *
     * - Parameter padding: the bottom padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setBottomPadding(_ padding: Float) -> Cell {
        setPaddingAt(Cell.bottomPaddingShift, padding)
        return self
    }

    /// Returns the bottom padding.
    public func getBottomPadding() -> Float {
        return paddingAt(Cell.bottomPaddingShift)
    }

    /**
     * Sets the left padding of this cell.
     *
     * - Parameter padding: the left padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setLeftPadding(_ padding: Float) -> Cell {
        setPaddingAt(Cell.leftPaddingShift, padding)
        return self
    }

    /// Returns the left padding.
    public func getLeftPadding() -> Float {
        return paddingAt(Cell.leftPaddingShift)
    }

    /**
     * Sets the right padding of this cell.
     *
     * - Parameter padding: the right padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setRightPadding(_ padding: Float) -> Cell {
        setPaddingAt(Cell.rightPaddingShift, padding)
        return self
    }

    /// Returns the right padding.
    public func getRightPadding() -> Float {
        return paddingAt(Cell.rightPaddingShift)
    }

    /**
     * Sets the top, bottom, left and right paddings of this cell.
     *
     * - Parameter padding: the right padding.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setPadding(_ padding: Float) -> Cell {
        setPaddingAt(Cell.topPaddingShift, padding)
        setPaddingAt(Cell.bottomPaddingShift, padding)
        setPaddingAt(Cell.leftPaddingShift, padding)
        setPaddingAt(Cell.rightPaddingShift, padding)
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
            return ascentOfCell() + descentOfCell() + paddingAt(Cell.topPaddingShift) + paddingAt(Cell.bottomPaddingShift)
        }
        if let drawable = drawable, text == nil || text == "" {   // The text is drawn first
            if let textBlock = drawable as? TextBlock {
                textBlock.setWidth(width)
            }
            cellHeight = Cell.measure(drawable)[1] + paddingAt(Cell.topPaddingShift) + paddingAt(Cell.bottomPaddingShift)
        } else if text != nil {
            var fontHeight = font.getBodyHeight(fontSize)
            if fallbackFont != nil && fallbackFont!.getBodyHeight(fontSize) > fontHeight {
                fontHeight = fallbackFont!.getBodyHeight(fontSize)
            }
            cellHeight = fontHeight + paddingAt(Cell.topPaddingShift) + paddingAt(Cell.bottomPaddingShift)
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

    /**
     * Sets the number of rows this cell spans, counted from this one, so that
     * a row span of 2 covers this row and the one under it. The cells the span
     * covers are not drawn: this cell draws its text, its background and its
     * borders once over all of them, and a page break moves the whole of it to
     * the next page. The table keeps its shape, so the rows under this one
     * still hold a cell at this column, which is left empty. A row span of 1,
     * the default, spans nothing. Please see Example_38.
     *
     * - Parameter rowspan: the number of rows, from 1.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setRowSpan(_ rowspan: Int) -> Cell {
        self.rowspan = (rowspan < 1) ? 1 : rowspan
        return self
    }

    /**
     * Returns the number of rows this cell spans.
     *
     * - Returns: the row span value.
     */
    public func getRowSpan() -> Int {
        return self.rowspan
    }

    /// Sets whether the specified borders, for example Border.TOP | Border.BOTTOM, are drawn.
    @discardableResult
    public func setBorder(_ border: UInt32, _ visible: Bool) -> Cell {
        if visible {
            self.properties |= (border & Border.ALL)
        } else {
            self.properties &= ~(border & Border.ALL)
        }
        return self
    }

    /// Returns true if any of the specified borders, for example Border.TOP, is drawn.
    public func getBorder(_ border: UInt32) -> Bool {
        return self.properties & border & Border.ALL != 0
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
        setAlignmentAt(Cell.textAlignmentShift, alignment)
        return self
    }

    /**
     * Returns the text alignment.
     *
     * - Returns: the horizontal text alignment.
     */
    public func getTextAlignment() -> Alignment {
        return alignmentAt(Cell.textAlignmentShift)
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
        setAlignmentAt(Cell.valignShift, alignment)
        return self
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * - Returns: the vertical alignment.
     */
    public func getVerticalAlignment() -> Alignment {
        return alignmentAt(Cell.valignShift)
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
        if underline {
            self.properties |= Cell.UNDERLINE
        } else {
            self.properties &= ~Cell.UNDERLINE
        }
        return self
    }

    /**
     * Returns the underline text parameter.
     *
     * - Returns: the underline text parameter.
     */
    public func getUnderline() -> Bool {
        return self.properties & Cell.UNDERLINE != 0
    }

    /**
     * Sets the strikeout text parameter.
     *
     * - Parameter strikeout: the strikeout text parameter.
     * - Returns: this Cell object.
     */
    @discardableResult
    public func setStrikeout(_ strikeout: Bool) -> Cell {
        if strikeout {
            self.properties |= Cell.STRIKEOUT
        } else {
            self.properties &= ~Cell.STRIKEOUT
        }
        return self
    }

    /**
     * Returns the strikeout text parameter.
     *
     * - Returns: the strikeout text parameter.
     */
    public func getStrikeout() -> Bool {
        return self.properties & Cell.STRIKEOUT != 0
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
            textBlock.setLocation(x + paddingAt(Cell.leftPaddingShift), y + paddingAt(Cell.topPaddingShift))
            textBlock.setWidth(w - (paddingAt(Cell.leftPaddingShift) + paddingAt(Cell.rightPaddingShift)))
            textBlock.drawOn(page)
        } else if let drawable = drawable {
            if getTextAlignment() == Alignment.RIGHT {
                let drawableWidth = Cell.measure(drawable)[0]
                drawable.setLocation((x + w) - (drawableWidth + paddingAt(Cell.rightPaddingShift)), y + paddingAt(Cell.topPaddingShift))
            } else if getTextAlignment() == Alignment.CENTER {
                let drawableWidth = Cell.measure(drawable)[0]
                drawable.setLocation((x + w/2.0) - drawableWidth/2.0, y + paddingAt(Cell.topPaddingShift))
            } else {
                drawable.setLocation(x + paddingAt(Cell.leftPaddingShift), y + paddingAt(Cell.topPaddingShift))
            }
            drawable.drawOn(page)
        }

        drawBorders(page, x, y, w, h)
        if point != nil {
            if alignmentAt(Cell.markerAlignmentShift) == Alignment.LEFT {
                point!.x = x + 2*point!.r
            } else if alignmentAt(Cell.markerAlignmentShift) == Alignment.RIGHT {
                point!.x = (x + w) - paddingAt(Cell.rightPaddingShift)/2
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
            page.addArtifactBMC()
            page.drawPoint(point!)
            page.addEMC()
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
        if properties & Border.ALL == 0 {
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
        if properties & Border.TOP != 0 {
            page.moveTo(x - hWidth, y)
            page.lineTo(x + cellW, y)
        }
        if properties & Border.BOTTOM != 0 {
            page.moveTo(x - hWidth, y + cellH)
            page.lineTo(x + cellW, y + cellH)
        }
        if properties & Border.LEFT != 0 {
            page.moveTo(x, y - hWidth)
            page.lineTo(x, y + cellH + hWidth)
        }
        if properties & Border.RIGHT != 0 {
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
        if alignmentAt(Cell.valignShift) == Alignment.TOP {
            yText = y + ascent + paddingAt(Cell.topPaddingShift)
        } else if alignmentAt(Cell.valignShift) == Alignment.CENTER {
            yText = y + cellH/2 + ascent/2
        } else if alignmentAt(Cell.valignShift) == Alignment.BOTTOM {
            yText = (y + cellH) - paddingAt(Cell.bottomPaddingShift)
        } else {
            fatalError("Invalid vertical text alignment option.")
        }

        var xText: Float
        if getTextAlignment() == Alignment.RIGHT {
            xText = (x + cellW) - (getTextWidth() + paddingAt(Cell.rightPaddingShift))
        } else if getTextAlignment() == Alignment.CENTER {
            xText = x + paddingAt(Cell.leftPaddingShift) +
                    (((cellW - (paddingAt(Cell.leftPaddingShift) + paddingAt(Cell.rightPaddingShift))) - getTextWidth()) / 2)
        } else {
            // Alignment.LEFT, and Alignment.JUSTIFY, which a single line of text cannot use.
            xText = x + paddingAt(Cell.leftPaddingShift)
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
        // The line is decoration, and the text says what the cell holds.
        page.addArtifactBMC()
        page.setPenColor(textColor)
        page.setPenWidth(font.getUnderlineThickness(fontSize))
        page.moveTo(x, y + descent)
        page.lineTo(x + getTextWidth(), y + descent)
        page.strokePath()
        page.addEMC()
    }

    private func strikeoutText(_ page: Page, _ x: Float, _ y: Float) {
        let ascent = font.getAscent(fontSize)
        page.addArtifactBMC()
        page.setPenColor(textColor)
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
