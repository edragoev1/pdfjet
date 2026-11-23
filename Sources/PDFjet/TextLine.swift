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
public class TextLine : BaselineDrawable {
    var x: Float = 0.0
    var y: Float = 0.0

    var font: Font?
    var fallbackFont: Font?
    var fontSize: Float

    var text: String?
    private var uri: String?
    private var key: String?
    private var destination: String?
    // Code: a TextFrame keeps the runs of spaces of the text.
    internal var keepSpaces = false

    var isLastToken: Bool = false
    var xOffset: Float = 0.0
    var underline = false
    var strikeout = false
    // True when a TextBlock wrapped its text at a space after this line: the
    // space is drawn after the line, so that a screen reader and a text
    // extractor see the word break, but is not part of its text, so that
    // the alignment and the underline measure the line without it.
    var trailingSpace = false
    // The glyphs a shaper found, of text such as Hindi or Khmer, drawn as
    // outlines in place of the text; nil for text drawn as text
    var shaped: ShapedText?             // A TextBlock's line, shaped with its paragraph
    private var pieces: ShapedPieces?   // The texts this line and its copies shaped
    // True for a line of a right to left paragraph of a TextBlock, aligned to the right
    var rightToLeft = false
    // The hard line breaks after the text line, of Markdown's two spaces, \
    // or <br> at the end of a line: TextFrame starts a new row after it, and
    // leaves an empty row for each break after the first, as <br><br> does.
    var lineBreaks = 0

    private var degrees = 0

    private var scriptPosition = ScriptPosition.NORMAL
    private var verticalOffset: Float = 0.0
    var explicitOffset = false      // True after setVerticalOffset

    private var language: String?
    private var altDescription: String?

    private var uriLanguage: String?
    private var uriActualText: String?
    private var uriAltDescription: String?

    private var structureType = StructElem.P
    // The text line this one is a piece of, drawn row by row or word by word
    // in a TextFrame or TextColumn, or nil: the pieces of a Figure are one.
    private var pieceOf: TextLine?

    private var textColor: [Float] = [0.0, 0.0, 0.0]
    private var decorationColor: [Float] = [0.0, 0.0, 0.0]
    private var colorMap: [String: Int32]?

    ///
    /// Constructor for creating text line objects.
    ///
    /// - Parameter font: the font to use.
    ///
    public init(_ font: Font) {
        self.font = font
        self.fallbackFont = font
        self.fontSize = font.size
    }

    ///
    /// Constructor for creating text line objects.
    ///
    /// - Parameter font: the font to use.
    /// - Parameter text: the text.
    ///
    public init(_ font: Font, _ text: String) {
        self.font = font
        self.fallbackFont = font
        self.fontSize = font.size
        self.text = text
        self.altDescription = text
    }

    ///
    /// Sets the text.
    ///
    /// - Parameter text: the text.
    /// - Returns: this TextLine.
    ///
    @discardableResult
    public func setText(_ text: String) -> TextLine {
        self.text = text
        self.pieces = nil
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

    ///
    /// Sets the location where the text line will be drawn on the page.
    ///
    /// - Parameter x: the x coordinate of the text line.
    /// - Parameter y: the y coordinate of the text line.
    /// - Returns: this TextLine.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the font to use for this text line. The fallback font changes with
    /// it, unless a different fallback font was set.
    ///
    /// - Parameter font: the font to use.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setFont(_ font: Font) -> TextLine {
        if self.fallbackFont === self.font {
            self.fallbackFont = font
        }
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
    /// - Parameter fontSize: the fontSize to use.
    /// - Returns: this TextLine.
    ///
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> TextLine {
        self.fontSize = fontSize
        return self
    }

    /// Returns the font size.
    public func getFontSize() -> Float {
        return self.fontSize
    }

    ///
    /// Sets the fallback font.
    ///
    /// - Parameter fallbackFont: the fallback font.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> TextLine {
        self.fallbackFont = fallbackFont
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

    /// Sets the text color as a 0xRRGGBB value. Color.transparent leaves the color unchanged.
    @discardableResult
    public func setTextColor(_ color: Int32) -> TextLine {
        if color == Color.transparent {
            return self
        }
        self.textColor = Util.rgb(color)
        return self
    }

    /// Sets the text color from an array of red, green and blue values.
    @discardableResult
    public func setTextColor(_ textColor: [Float]) -> TextLine {
        self.textColor = textColor
        return self
    }

    /// Returns the text color.
    public func getTextColor() -> [Float] {
        return self.textColor
    }

    /// Sets the color of the underline and strikeout lines as a 0xRRGGBB value. Color.transparent leaves it unchanged.
    @discardableResult
    public func setDecorationColor(_ color: Int32) -> TextLine {
        if color == Color.transparent {
            return self
        }
        self.decorationColor = Util.rgb(color)
        return self
    }

    /// Sets the color of the underline and strikeout lines from an array of red, green and blue values.
    @discardableResult
    public func setDecorationColor(_ decorationColor: [Float]) -> TextLine {
        self.decorationColor = decorationColor
        return self
    }

    /// Returns the color of the underline and strikeout lines.
    public func getDecorationColor() -> [Float] {
        return self.decorationColor
    }

    // The y coordinate of the destination of the line, a font size above the
    // baseline, which drawOn and Bookmark use.
    func destinationY() -> Float {
        return y - fontSize
    }

    ///
    /// Returns the width of the text line.
    ///
    /// - Returns: the width.
    ///
    public func getWidth() -> Float {
        // Measured as text when it cannot be shaped: drawOn says why
        if let glyphs = try? shapedGlyphs() {
            return glyphs.line.width
        }
        return font!.stringWidth(fallbackFont, fontSize, text ?? "")
    }

    // The glyphs a shaper found for text such as Hindi or Khmer, one line of
    // it, which drawOn draws as outlines; nil for text drawn as text. A
    // TextBlock's line was shaped with its paragraph.
    func shapedGlyphs() throws -> ShapedText? {
        if let shaped = shaped {
            return shaped
        }
        return try shapeOf(text)
    }

    // The text shaped in this line's style, as one line, or nil for text
    // drawn as text; shaped once, kept in the pieces this line shares with
    // its copies, made again when the style changed.
    func shapeOf(_ text: String?) throws -> ShapedText? {
        guard let text = text, Shaping.needsShaper(text), let font = font else {
            return nil
        }
        let style = ShapingStyle(font: font, fallback: fallbackFont, size: fontSize, language: language)
        if pieces == nil || pieces!.style != style {
            pieces = ShapedPieces(style)
        }
        if let glyphs = pieces!.shaped[text] {
            return glyphs
        }
        let glyphs = try Shaping.shapeOneLine(Shaping.shapingFonts(font, fallbackFont), fontSize, language, text)
        pieces!.shaped[text] = glyphs
        return glyphs
    }

    // The width of the text in this line's style: of its glyphs, for text
    // such as Hindi a shaper shapes, else of its characters. What cannot be
    // shaped is measured as text; drawOn says why it cannot be drawn.
    func widthOf(_ text: String) -> Float {
        // A shaper's width leaves out the spaces at the end of the line, which
        // a TextFrame's word is measured with: they are measured in the font
        var core = text
        while core.hasSuffix(" ") {
            core.removeLast()
        }
        if let glyphs = try? shapeOf(core) {
            return glyphs.line.width + font!.stringWidth(fallbackFont, fontSize, String(text.dropFirst(core.count)))
        }
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
    /// Returns how far above its baseline this text line reaches: the ascent
    /// of the font at the font size of this text line.
    ///
    public func getAscent() -> Float {
        return font!.getAscent(self.fontSize)
    }

    ///
    /// Returns how far below its baseline this text line reaches: the descent
    /// of the font at the font size of this text line.
    ///
    public func getDescent() -> Float {
        return font!.getDescent(self.fontSize)
    }

    ///
    /// Sets the URI for the "click text line" action.
    ///
    /// - Parameter uri: the URI
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
    /// - Parameter key: the destination name.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setGoToAction(_ key: String?) -> TextLine {
        self.key = key
        return self
    }

    ///
    /// Sets the name of a destination that drawOn adds to the page, a font size
    /// above the baseline, so that a GoTo action with the name, which
    /// setGoToAction sets, goes to this line.
    ///
    /// - Parameter name: the destination name, or nil for none.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setDestination(_ name: String?) -> TextLine {
        self.destination = name
        return self
    }

    ///
    /// Returns the name of the destination that drawOn adds to the page.
    ///
    /// - Returns: the destination name, or nil.
    ///
    public func getDestination() -> String? {
        return self.destination
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
    /// - Parameter underline: the underline flag.
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
    /// - Parameter strikeout: the strikeout flag.
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
    /// Sets the rotation of the text. A positive angle turns clockwise, as every
    /// rotation in PDFjet turns, and a negative angle counterclockwise.
    ///
    /// - Parameter degrees: the angle in degrees.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setTextRotation(_ degrees: Int) -> TextLine {
        self.degrees = degrees
        return self
    }

    ///
    /// Returns the text direction.
    ///
    /// - Returns: the text direction.
    ///
    public func getTextRotation() -> Int {
        return degrees
    }

    ///
    /// Sets the script position. The offset of a superscript or subscript follows
    /// the font and font size of this text line when it is drawn.
    ///
    /// - Parameter scriptPosition: ScriptPosition.NORMAL, ScriptPosition.SUBSCRIPT or ScriptPosition.SUPERSCRIPT.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setScriptPosition(_ scriptPosition: ScriptPosition) -> TextLine {
        self.scriptPosition = scriptPosition
        self.explicitOffset = false
        return self
    }

    ///
    /// Returns the script position.
    ///
    /// - Returns: the script position.
    ///
    public func getScriptPosition() -> ScriptPosition {
        return self.scriptPosition
    }

    ///
    /// Sets the vertical offset of the text, which replaces the offset of the
    /// script position until setScriptPosition is called again.
    ///
    /// - Parameter verticalOffset: the vertical offset.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setVerticalOffset(_ verticalOffset: Float) -> TextLine {
        self.verticalOffset = verticalOffset
        self.explicitOffset = true
        return self
    }

    ///
    /// Returns the vertical text offset: the one set with setVerticalOffset, or
    /// that of the script position at the font and font size of this text line.
    ///
    /// - Returns: the vertical text offset.
    ///
    public func getVerticalOffset() -> Float {
        if explicitOffset {
            return self.verticalOffset
        }
        if scriptPosition == ScriptPosition.SUPERSCRIPT {
            return -font!.getBodyHeight(fontSize)/2.0
        } else if scriptPosition == ScriptPosition.SUBSCRIPT {
            return font!.getBodyHeight(fontSize)/3.0
        }
        return 0.0
    }

    /// Sets the language of the text, for example "en-US".
    @discardableResult
    public func setLanguage(_ language: String?) -> TextLine {
        self.language = language
        return self
    }

    /// Returns the language of the text.
    public func getLanguage() -> String? {
        return self.language
    }

    ///
    /// Sets the alternate description of the text line.
    ///
    /// - Parameter altDescription: the alternate description of the text line.
    /// - Returns: the TextLine.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String?) -> TextLine {
        self.altDescription = altDescription
        return self
    }

    /// Returns the alternate description of this text line.
    public func getAltDescription() -> String? {
        return self.altDescription
    }

    /// Sets the language of the link annotation.
    @discardableResult
    public func setURILanguage(_ uriLanguage: String?) -> TextLine {
        self.uriLanguage = uriLanguage
        return self
    }

    /// Sets the alternate description of the link annotation.
    @discardableResult
    public func setURIAltDescription(_ uriAltDescription: String?) -> TextLine {
        self.uriAltDescription = uriAltDescription
        return self
    }

    /// Sets the actual text of the link annotation.
    @discardableResult
    public func setURIActualText(_ uriActualText: String?) -> TextLine {
        self.uriActualText = uriActualText
        return self
    }

    /// Returns the language of the link annotation.
    public func getURILanguage() -> String? {
        return self.uriLanguage
    }

    /// Returns the alternate description of the link annotation.
    public func getURIAltDescription() -> String? {
        return self.uriAltDescription
    }

    /// Returns the actual text of the link annotation.
    public func getURIActualText() -> String? {
        return self.uriActualText
    }

    /// Sets the structure element type, for example StructElem.P or StructElem.H1.
    @discardableResult
    public func setStructureType(_ structureType: StructElem) -> TextLine {
        self.structureType = structureType
        return self
    }

    /// Returns the x coordinate of the start of the text and the y coordinate of its baseline.
    public func getLocation() -> [Float] {
        return [self.x, self.y]
    }

    /// Sets the colors used to highlight words in the text.
    @discardableResult
    public func setHighlightColors(_ colorMap: [String: Int32]?) -> TextLine {
        self.colorMap = colorMap
        return self
    }

    /// Returns the colors used to highlight words in the text.
    public func getHighlightColors() -> [String: Int32]? {
        return self.colorMap
    }

    // Returns a new text line with the text and every setting of this text line,
    // for a part of its text wrapped onto a line of its own. An alternate
    // description that was set is kept; otherwise the new text is its own.
    func copyWithText(_ text: String) -> TextLine {
        let textLine = TextLine(font!, text)
        textLine.fallbackFont = fallbackFont
        textLine.fontSize = fontSize
        textLine.underline = underline
        textLine.strikeout = strikeout
        textLine.degrees = degrees
        textLine.textColor = textColor
        textLine.decorationColor = decorationColor
        textLine.colorMap = colorMap
        textLine.scriptPosition = scriptPosition
        textLine.verticalOffset = verticalOffset
        textLine.explicitOffset = explicitOffset
        textLine.uri = uri
        textLine.key = key
        textLine.destination = destination
        textLine.language = language
        if altDescription != nil && altDescription != self.text {
            textLine.altDescription = altDescription
        }
        textLine.uriLanguage = uriLanguage
        textLine.uriActualText = uriActualText
        textLine.uriAltDescription = uriAltDescription
        textLine.structureType = structureType
        textLine.pieces = pieces    // The words it shaped, shaped once
        textLine.pieceOf = pieceOf ?? self
        return textLine
    }

    ///
    /// Draws the text line on the specified page.
    ///
    /// - Parameter page: the page to draw text line on.
    /// - Returns: the x and y coordinates of the bottom right corner.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        // The destination is where the line is, with a text or without one.
        if let page, let destination, !destination.isEmpty {
            _ = page.addDestination(destination, destinationY())
        }
        if text == nil || text == "" {
            return [x, y]
        }
        if page == nil {
            return getCorner(getVerticalOffset())   // Measured, not drawn
        }

        let verticalOffset = getVerticalOffset()
        page!.setTextRotation(degrees)
        page!.setBrushColor(textColor)
        // The text is drawn, so it is not given again as actual text, or as its
        // own alternate description: right to left text is drawn in visual
        // order, and would be read backwards.
        // The two are compared by their code points, as in the other ports, and
        // not by canonical equivalence, as Swift compares strings.
        let alt = (altDescription ?? "").unicodeScalars.elementsEqual(text!.unicodeScalars) ?
                "" : altDescription ?? ""
        if page!.mcidParent == nil {
            page!.noteHeading(structureType, text, x, destinationY())
        }
        var link: StructElement?
        if !(uri ?? "").isEmpty || !(key ?? "").isEmpty {
            link = page!.addLinkBDC(structureType, language, "", alt)
        } else {
            page!.pieceOf = pieceOf
            page!.addBDC(structureType, language, "", alt)
            page!.pieceOf = nil
        }
        let glyphs: ShapedText?
        do {
            glyphs = try shapedGlyphs()
        } catch {
            // Text such as Hindi that could not be shaped fails the document, saying why
            page!.addEMC()
            page!.pdf.fail(Shaping.message(error))
            return [x, y]
        }
        if let glyphs = glyphs {
            // Glyphs a shaper found, drawn as outlines, turned as the text is
            _ = page!.drawTurnedShapedLine(glyphs, self.x, self.y + verticalOffset, language, degrees)
        } else {
            page!.drawString(font!, fallbackFont, fontSize, text, self.x, self.y + verticalOffset, textColor, colorMap)
        }
        page!.addEMC()

        // The trigonometry is done in double precision, as in the other ports, and
        // turns counterclockwise, where the rotation turns clockwise.
        let radians = Double.pi * -Double(degrees) / 180.0
        if underline || strikeout {
            // The pen of the lines is their own, and the page is left with the one it had.
            page!.saveGraphicsState()
        }
        if underline {
            page!.setPenWidth(font!.getUnderlineThickness(fontSize))
            page!.setPenColor(decorationColor)
            let lineLength = getDecorationLength()
            let xAdjust = Double(font!.getUnderlinePosition(fontSize)) * sin(radians)
            let yAdjust = Double(font!.getUnderlinePosition(fontSize)) * cos(radians) + Double(verticalOffset)
            let x2 = Double(x) + Double(lineLength) * cos(radians)
            let y2 = Double(y) - Double(lineLength) * sin(radians)
            // The line is decoration, and the text says what it is drawn
            // under; a description of its own is read after the text again.
            page!.addArtifactBMC()
            page!.moveTo(Float(Double(x) + xAdjust), Float(Double(y) + yAdjust))
            page!.lineTo(Float(x2 + xAdjust), Float(y2 + yAdjust))
            page!.strokePath()
            page!.addEMC()
        }

        if strikeout {
            page!.setPenWidth(font!.getUnderlineThickness(fontSize))
            page!.setPenColor(decorationColor)
            let lineLength = getDecorationLength()
            let xAdjust = Double(font!.getBodyHeight(fontSize) / 4.0) * sin(radians)
            let yAdjust = Double(font!.getBodyHeight(fontSize) / 4.0) * cos(radians) + Double(verticalOffset)
            let x2 = Double(x) + Double(lineLength) * cos(radians)
            let y2 = Double(y) - Double(lineLength) * sin(radians)
            page!.addArtifactBMC()
            page!.moveTo(Float(Double(x) - xAdjust), Float(Double(y) - yAdjust))
            page!.lineTo(Float(x2 - xAdjust), Float(y2 - yAdjust))
            page!.strokePath()
            page!.addEMC()
        }
        if underline || strikeout {
            page!.restoreGraphicsState()
        }

        if !(uri ?? "").isEmpty || !(key ?? "").isEmpty {
            let box = getLinkBox(verticalOffset)
            page!.addAnnotation(Annotation.link(
                    box[0], box[1], box[2], box[3],
                    uri, key, uriLanguage, uriActualText, uriAltDescription).joining(link))
        }
        page!.setTextRotation(0)

        return getCorner(verticalOffset)
    }

    // Returns the left, top, right and bottom of the box that holds the text,
    // from the ascent above the baseline to the descent below it, turned with
    // the text, which the link of the text covers.
    private func getLinkBox(_ verticalOffset: Float) -> [Float] {
        // The box holds the text that shows, without the spaces it is drawn
        // with at either end: a word of a TextColumn or of a justified
        // TextFrame row is drawn with the space after it, and its link reached
        // one space past it.
        let (start, end) = getVisibleSpan()
        let ascent = font!.getAscent(fontSize)
        let descent = font!.getDescent(fontSize)
        let x0 = x
        let y0 = y + verticalOffset
        if degrees % 360 == 0 {
            return [x0 + start, y0 - ascent, x0 + end, y0 + descent]
        }
        // The corners of the box, along the baseline and across it, turned as
        // the underline is; the trigonometry turns counterclockwise, where the
        // rotation turns clockwise.
        let radians = Double.pi * -Double(degrees) / 180.0
        let cosValue = cos(radians)
        let sinValue = sin(radians)
        var x1 = Double.infinity
        var y1 = Double.infinity
        var x2 = -Double.infinity
        var y2 = -Double.infinity
        for along in [Double(start), Double(end)] {
            for across in [Double(-ascent), Double(descent)] {
                let cornerX = Double(x0) + along*cosValue + across*sinValue
                let cornerY = Double(y0) - along*sinValue + across*cosValue
                x1 = min(x1, cornerX)
                y1 = min(y1, cornerY)
                x2 = max(x2, cornerX)
                y2 = max(y2, cornerY)
            }
        }
        return [Float(x1), Float(y1), Float(x2), Float(y2)]
    }

    // Returns where the text that shows starts and ends along the baseline:
    // past the spaces before it, and before the spaces after it. A text of
    // spaces alone spans its whole width.
    private func getVisibleSpan() -> (Float, Float) {
        let scalars = Array(text!.unicodeScalars)
        let width = widthOf(text!)
        var first = 0
        while first < scalars.count && scalars[first] == " " {
            first += 1
        }
        var last = scalars.count
        while last > first && scalars[last - 1] == " " {
            last -= 1
        }
        if first == last || (first == 0 && last == scalars.count) {
            return (0.0, width)
        }
        var before = String.UnicodeScalarView()
        before.append(contentsOf: scalars[0..<first])
        var through = String.UnicodeScalarView()
        through.append(contentsOf: scalars[0..<last])
        let start = widthOf(String(before))
        let end = widthOf(String(through))
        return (start, end)
    }

    // Returns the length of the underline and of the strikeout: the width of the
    // text, and of the last token without the space after it. The token is
    // measured without the space and not as its width less that of a space,
    // which left out the kerning between the space and the letter before it, so
    // the line stopped short of the word (the review of 28 October 2026).
    private func getDecorationLength() -> Float {
        if shaped != nil {
            return getWidth()
        }
        var drawn = text!
        if isLastToken && drawn.unicodeScalars.last == " " {
            drawn = String(drawn.unicodeScalars.dropLast())
        }
        return widthOf(drawn)
    }

    // Returns the right end of the baseline, or its lower end when the text is rotated.
    private func getCorner(_ verticalOffset: Float) -> [Float] {
        // The trigonometry is done in double precision, as in the other ports, and
        // turns counterclockwise, where the rotation turns clockwise.
        let radians = Double.pi * -Double(degrees) / 180.0
        let len = Double(getWidth())
        let xMax = max(Double(x), Double(x) + len*cos(radians))
        let yMax = max(Double(y + verticalOffset), Double(y + verticalOffset) - len*sin(radians))
        return [Float(xMax), Float(yMax)]
    }
}   // End of TextLine.swift
