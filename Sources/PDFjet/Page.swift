/**
 * Page.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

struct PDFjetError: Error {
    let message: String
}

///
/// Used to create PDF page objects.
///
/// Please note:
///
/// - The coordinate (0.0, 0.0) is the top left corner of the page.
/// - Page sizes are in points; 1 point is 1/72 inch.
///
public class Page {
    /// Pass to the Page initializer to create a page that is not added to the PDF right away.
    public static let DETACHED = false

    internal var pdf: PDF
    internal var pageObj: PDFobj?
    internal var objNumber = 0
    internal var renderingMode = 0
    internal var width: Float = 0.0
    internal var height: Float = 0.0
    internal var contents = [Int]()
    internal var annots: [Annotation] = []
    internal var destinations: [Destination] = []
    internal var structures = [StructElem]()
    internal var buf = [UInt8]()

    internal var cropBox: [Float]?
    internal var bleedBox: [Float]?
    internal var trimBox: [Float]?
    internal var artBox: [Float]?

    internal var tmx: [Float] = [1.0, 0.0, 0.0, 1.0]
    private var textFontSize: Float = 0.0   // The font size of the text drawn last
    private var textRise: Float = 0.0
    internal var tm0: [UInt8]
    internal var tm1: [UInt8]
    internal var tm2: [UInt8]
    internal var tm3: [UInt8]

    private var penColor: [Float] = [0.0, 0.0, 0.0]
    private var brushColor: [Float] = [0.0, 0.0, 0.0]

    private var penWidth: Float = 0.5

    private var lineCapStyle = CapStyle.BUTT
    private var lineJoinStyle = JoinStyle.MITER
    private var strokeDashPattern: String = "[] 0"
    private var mcid = 0
    private let hexadecimal = Hexadecimal()

    ///
    /// Creates page object and add it to the PDF document.
    ///
    /// Please note:
    ///
    /// - The coordinate (0.0, 0.0) is the top left corner of the page.
    /// - Page sizes are in points; 1 point is 1/72 inch.
    ///
    /// - Parameter pdf: the pdf object.
    /// - Parameter pageSize: the page size of this page.
    /// - Parameter addPageToPDF: Bool flag.
    ///
    public init(
            _ pdf: PDF,
            _ pageSize: [Float],
            _ addPageToPDF: Bool) {
        self.pdf = pdf
        self.annots = [Annotation]()
        self.destinations = [Destination]()
        self.width = pageSize[0]
        self.height = pageSize[1]
        self.tm0 = FastFloat.toByteArray(tmx[0])
        self.tm1 = FastFloat.toByteArray(tmx[1])
        self.tm2 = FastFloat.toByteArray(tmx[2])
        self.tm3 = FastFloat.toByteArray(tmx[3])
        if addPageToPDF {
            pdf.addPage(self)
        }
    }

    /// Creates a page from a page object read from an existing PDF.
    public init(_ pdf: PDF, _ pageObj: PDFobj) {
        self.pdf = pdf
        self.pageObj = pageObj
        self.width = pageObj.getPageSize()[0]
        self.height = pageObj.getPageSize()[1]
        self.tm0 = FastFloat.toByteArray(tmx[0])
        self.tm1 = FastFloat.toByteArray(tmx[1])
        self.tm2 = FastFloat.toByteArray(tmx[2])
        self.tm3 = FastFloat.toByteArray(tmx[3])
        self.pageObj = removeComments(self.pageObj!)
        saveGraphicsState()
        if pageObj.gsNumber != -1 {
            append("/GS")
            append(pageObj.gsNumber + 1)
            append(" gs\n")
        }
    }

    /// Finishes a page read from an existing PDF by adding its new content to the objects.
    public func complete(_ objects: inout [PDFobj]) {
        restoreGraphicsState()
        pageObj!.addContent(&self.buf, &objects)
    }

    private func removeComments(_ obj: PDFobj) -> PDFobj {
        var list = [String]()
        var comment: Bool = false
        for token in obj.dict {
            if token == "%" {
                comment = true
            } else {
                if token.hasPrefix("/") {
                    comment = false
                    list.append(token)
                } else {
                    if !comment {
                        list.append(token)
                    }
                }
            }
        }
        obj.dict = list
        return obj
    }

    ///
    /// Creates page object and add it to the PDF document.
    ///
    /// Please note:
    ///
    /// - The coordinate (0.0, 0.0) is the top left corner of the page.
    /// - Page sizes are in points; 1 point is 1/72 inch.
    ///
    /// - Parameter pdf: the pdf object.
    /// - Parameter pageSize: the page size of this page.
    ///
    public convenience init(_ pdf: PDF, _ pageSize: [Float]) {
        self.init(pdf, pageSize, true)
    }

    /// Adds a core font to the resources of this page and returns the font.
    public func addResource(_ coreFont: Int, _ objects: inout [PDFobj]) -> Font {
        return pageObj!.addResource(coreFont, &objects)
    }

    /// Adds an image to the resources of this page.
    public func addResource(_ image: Image, _ objects: inout [PDFobj]) {
        pageObj!.addResource(image, &objects)
    }

    /// Adds a font to the resources of this page.
    public func addResource(_ font: Font, _ objects: inout [PDFobj]) {
        pageObj!.addResource(font, &objects)
    }

    /// Returns the content stream of this page.
    public func getContent() -> [UInt8] {
        return self.buf
    }

    ///
    /// Adds destination to this page.
    ///
    /// - Parameter name: The destination name.
    /// - Parameter xPosition: The horizontal position of the destination on this page.
    /// - Parameter yPosition: The vertical position of the destination on this page.
    ///
    /// - Returns: the destination.
    ///
    @discardableResult
    public func addDestination(
            _ name: String,
            _ xPosition: Float,
            _ yPosition: Float) -> Destination {
        let dest = Destination(name, xPosition, height - yPosition)
        destinations.append(dest)
        return dest
    }

    ///
    /// Adds destination to this page.
    ///
    /// - Parameter name: The destination name.
    /// - Parameter yPosition: The vertical position of the destination on this page.
    ///
    /// - Returns: the destination.
    ///
    @discardableResult
    public func addDestination(
            _ name: String,
            _ yPosition: Float) -> Destination {
        let dest = Destination(name, height - yPosition)
        destinations.append(dest)
        return dest
    }

    ///
    /// Returns the width of this page.
    ///
    /// - Returns: the width of the page.
    ///
    public func getWidth() -> Float {
        return self.width
    }

    ///
    /// Returns the height of this page.
    ///
    /// - Returns: the height of the page.
    ///
    public func getHeight() -> Float {
        return self.height
    }

    ///
    /// Draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
    ///
    /// - Parameter x1: the first point's x coordinate.
    /// - Parameter y1: the first point's y coordinate.
    /// - Parameter x2: the second point's x coordinate.
    /// - Parameter y2: the second point's y coordinate.
    ///
    public func drawLine(
            _ x1: Float,
            _ y1: Float,
            _ x2: Float,
            _ y2: Float) {
        moveTo(x1, y1)
        lineTo(x2, y2)
        strokePath()
    }

    /// Draws the string in black. The fallback font is used for characters the main font does not have.
    public final func drawString(
            _ font: Font,
            _ fallbackFont: Font?,
            _ fontSize: Float,
            _ str: String?,
            _ xOrig: Float,
            _ yOrig: Float) {
        drawString(font, fallbackFont, fontSize, str, xOrig, yOrig, [0.0, 0.0, 0.0], nil)
    }

    ///
    /// Draws the text given by the specified string,
    /// using the specified main font and text color.
    /// If the main font is missing some glyphs - the fallback font is used.
    /// The baseline of the leftmost character is at position (xOrig, yOrig) on the page.
    ///
    /// - Parameter font: the main font.
    /// - Parameter fallbackFont: the fallback font.
    /// - Parameter fontSize: the font size.
    /// - Parameter str: the string to be drawn.
    /// - Parameter xOrig: the x coordinate.
    /// - Parameter yOrig: the y coordinate.
    /// - Parameter textColor: the text color as an array of red, green and blue values.
    /// - Parameter highlightColors: the words to highlight and their colors, or nil.
    ///
    public final func drawString(
            _ font: Font,
            _ fallbackFont: Font?,
            _ fontSize: Float,
            _ str: String?,
            _ xOrig: Float,
            _ yOrig: Float,
            _ textColor: [Float],
            _ highlightColors: [String : Int32]?) {
        var x = xOrig
        let y = yOrig
        if (font.isCoreFont ||
                font.isCJK ||
                fallbackFont == nil ||
                fallbackFont!.isCoreFont ||
                fallbackFont!.isCJK) {
            drawString(font, fontSize, str, x, y, textColor, highlightColors)
        } else {
            var activeFont = font
            var buf = String()
            for scalar in str!.unicodeScalars {
                if activeFont.unicodeToGID[Int(scalar.value)] == 0 {
                    drawString(activeFont, fontSize, buf, x, y, textColor, highlightColors)
                    x += activeFont.stringWidth(fontSize, buf)
                    buf = ""
                    // Switch the font
                    if activeFont === font {
                        activeFont = fallbackFont!
                    } else {
                        activeFont = font
                    }
                }
                buf.append(String(scalar))
            }
            drawString(activeFont, fontSize, buf, x, y, textColor, highlightColors)
        }
    }

    /// Draws the string in black at the current size of the font.
    public final func drawString(
            _ font: Font,
            _ text: String?,
            _ x: Float,
            _ y: Float) {
        drawString(font, font.size, text, x, y, [0.0, 0.0, 0.0], nil)
    }

    /// Draws the string in black at the specified font size.
    public final func drawString(
            _ font: Font,
            _ fontSize: Float,
            _ text: String?,
            _ x: Float,
            _ y: Float) {
        drawString(font, fontSize, text, x, y, [0.0, 0.0, 0.0], nil)
    }

    ///
    /// Draws the text given by the specified string,
    /// using the specified font and text color.
    /// The baseline of the leftmost character is at position (x, y) on the page.
    ///
    /// - Parameter font: the font to use.
    /// - Parameter fontSize: the font size.
    /// - Parameter text: the string to be drawn.
    /// - Parameter x: the x coordinate.
    /// - Parameter y: the y coordinate.
    /// - Parameter textColor: the text color as an array of red, green and blue values.
    /// - Parameter highlightColors: the words to highlight and their colors, or nil.
    ///
    public final func drawString(
            _ font: Font,
            _ fontSize: Float,
            _ text: String?,
            _ x: Float,
            _ y: Float,
            _ textColor: [Float],
            _ highlightColors: [String : Int32]?) {
        if text == nil || text! == "" {
            return
        }

        append(Token.beginText)
        setTextFont(font, fontSize)
        if self.renderingMode != 0 {
            append(renderingMode)
            append(" Tr\n")
        }

        if font.skew15 &&
                self.tmx[0] == 1.0 &&
                self.tmx[1] == 0.0 &&
                self.tmx[2] == 0.0 &&
                self.tmx[3] == 1.0 {
            let skew = Float(0.26)
            append(self.tmx[0])
            append(Token.space)
            append(self.tmx[1])
            append(Token.space)
            append(self.tmx[2] + skew)
            append(Token.space)
            append(self.tmx[3])
        } else {
            append(self.tm0)
            append(Token.space)
            append(self.tm1)
            append(Token.space)
            append(self.tm2)
            append(Token.space)
            append(self.tm3)
        }
        append(Token.space)
        append(x)
        append(Token.space)
        append(self.height - y)
        append(" Tm\n")

        if highlightColors == nil {
            setBrushColor(textColor)
            if font.isCoreFont {
                append("[<")
                drawASCIIString(font, text!)
                append(">] TJ\n")
            } else {
                append("<")
                drawUnicodeString(font, text!)
                append("> Tj\n")
            }
        } else {
            drawColoredString(font, text!, textColor, highlightColors!)
        }
        append(Token.endText)
    }

    private final func drawASCIIString(_ font: Font, _ text: String) {
        let scalars = Array(text.unicodeScalars)
        for i in 0..<scalars.count {
            let c1 = scalars[i]
            if c1 < Unicode.Scalar(font.firstChar)! ||
                    c1 > Unicode.Scalar(font.lastChar)! {
                appendTwoHexDigits(0x20, &self.buf)
                continue
            }
            appendTwoHexDigits(Int(c1.value), &self.buf)
            if font.isCoreFont && font.kernPairs && i < (scalars.count - 1) {
                var c2 = scalars[i + 1]
                if c2 < Unicode.Scalar(font.firstChar)! ||
                        c2 > Unicode.Scalar(font.lastChar)! {
                    c2 = Unicode.Scalar(32)
                }
                let index = Int(c1.value - 32)
                var j = 2
                while j < font.metrics![index].count {
                    if Unicode.Scalar(Int(font.metrics![index][j])) == c2 {
                        append(">")
                        append(Int(-font.metrics![index][j + 1]))
                        append("<")
                        break
                    }
                    j += 2
                }
            }
        }
    }

    private final func drawUnicodeString(_ font: Font, _ text: String) {
        let scalars = Array(text.unicodeScalars)
        if font.isCJK {
            for scalar in scalars {
                if scalar.value != 0xFEFF {     // BOM
                    if scalar < Unicode.Scalar(font.firstChar)! ||
                            scalar > Unicode.Scalar(font.lastChar)! {
                        Page.appendCodePointAsHex(0x0020, &self.buf)
                    } else {
                        Page.appendCodePointAsHex(Int(scalar.value), &self.buf)
                    }
                }
            }
        } else if font.markAnchors == nil {
            for scalar in scalars where scalar.value != 0xFEFF {    // BOM
                Page.appendCodePointAsHex(glyphOf(font, Int(scalar.value)), &self.buf)
            }
        } else {
            // The font has a GPOS table, so the marks are moved to where it puts them.
            var codePoints = [Int]()
            var gids = [Int]()
            var hasMarks = false
            for scalar in scalars where scalar.value != 0xFEFF {    // BOM
                let codePoint = Int(scalar.value)
                codePoints.append(codePoint)
                gids.append(glyphOf(font, codePoint))
                hasMarks = hasMarks || isMark(codePoint)
            }
            if !hasMarks {
                for gid in gids {
                    Page.appendCodePointAsHex(gid, &self.buf)
                }
                return
            }
            let offsets = markOffsets(font, codePoints, gids)
            let n = gids.count
            var i = 0
            while i < n {
                var end = i + 1
                if codePoints[i] != 0x20 {
                    while end < n && codePoints[end] != 0x20 {
                        end += 1
                    }
                }
                if isMoved(offsets, i, end) {
                    appendWordWithMovedMarks(font, codePoints, gids, offsets, i, end)
                } else {
                    for k in i..<end {
                        Page.appendCodePointAsHex(gids[k], &self.buf)
                    }
                }
                i = end
            }
        }
    }

    private func glyphOf(_ font: Font, _ codePoint: Int) -> Int {
        if codePoint < Int(font.firstChar) || codePoint > Int(font.lastChar) {
            return font.unicodeToGID[0x0020]
        }
        return font.unicodeToGID[codePoint]
    }

    // Returns the offsets that move the marks to where the GPOS table of the
    // font puts them, dx and dy in font units for each glyph. A mark goes on
    // the letter or ligature before it. Right to left text is drawn in visual
    // order, with the marks before their letter, so a Hebrew or Arabic mark
    // goes on the letter after it. A mark that attaches to the mark before it,
    // like a Thai tone mark above an upper vowel, goes on that mark instead.
    // The marks of a letter are taken in logical order, sorted as HarfBuzz
    // sorts them, so a fatha goes above a shadda whichever was typed first.
    private func markOffsets(_ font: Font, _ codePoints: [Int], _ gids: [Int]) -> [Int] {
        let n = gids.count
        // Where each glyph is drawn before it is moved, in font units.
        var x = [Int](repeating: 0, count: n)
        for i in stride(from: 1, to: n, by: 1) {
            x[i] = x[i - 1] + Int(font.advanceWidth[gids[i - 1]])
        }
        var offsets = [Int](repeating: 0, count: 2*n)
        for i in 0..<n where isMark(codePoints[i]) {
            let step = isRightToLeft(codePoints[i]) ? 1 : -1
            var base = i + step
            while base >= 0 && base < n && isMark(codePoints[base]) {
                base += step
            }
            if base >= 0 && base < n,
                    let offset = markToBaseOffset(font, codePoints[base], gids[base], gids[i]) {
                offsets[2*i] = x[base] + offset[0] - x[i]
                offsets[2*i + 1] = offset[1]
            }
        }
        var order = [Int](repeating: 0, count: n)
        var start = 0
        while start < n {
            if !isMark(codePoints[start]) {
                start += 1
                continue
            }
            // The marks from start to end - 1 go on the same letter.
            let rightToLeft = isRightToLeft(codePoints[start])
            var end = start + 1
            while end < n && isMark(codePoints[end]) && isRightToLeft(codePoints[end]) == rightToLeft {
                end += 1
            }
            let count = end - start
            for k in 0..<count {
                let mark = rightToLeft ? end - 1 - k : start + k
                // Moves the mark back past the marks that sort after it. A mark
                // of order 0 is never moved, and no mark moves past it.
                let rank = markOrder(codePoints[mark])
                var l = k - 1
                while rank != 0 && l >= 0 && markOrder(codePoints[order[l]]) > rank {
                    order[l + 1] = order[l]
                    l -= 1
                }
                order[l + 1] = mark
            }
            for k in stride(from: 1, to: count, by: 1) {
                placeOnMark(font, gids, x, &offsets, order[k], order[k - 1])
            }
            start = end
        }
        return offsets
    }

    // Returns the offset of the mark from the letter or ligature it goes on, in
    // font units, or nil. A font can have the anchors of an isolated Arabic
    // letter form only for its letter, which looks the same.
    private func markToBaseOffset(
            _ font: Font, _ baseCodePoint: Int, _ baseGID: Int, _ markGID: Int) -> [Int]? {
        if let offset = anchorOffset(font, baseGID, markGID) {
            return offset
        }
        if let letter = Bidi.letterOfIsolatedForm(UInt32(baseCodePoint)) {
            return anchorOffset(font, font.unicodeToGID[Int(letter)], markGID)
        }
        return nil
    }

    // Returns the offset of the mark from the glyph it goes on, from the first
    // MarkToBase or MarkToLigature subtable that has an anchor for both, or nil.
    private func anchorOffset(_ font: Font, _ baseGID: Int, _ markGID: Int) -> [Int]? {
        for (i, marks) in font.markAnchors!.enumerated() {
            guard let mark = marks[markGID] else {
                continue
            }
            let c = 3*mark[0]
            if let anchors = font.baseAnchors![i][baseGID], c + 2 < anchors.count, anchors[c] == 1 {
                return [anchors[c + 1] - mark[1], anchors[c + 2] - mark[2]]
            }
        }
        return nil
    }

    // Moves the mark onto the other mark, if the font has an offset for the two.
    private func placeOnMark(
            _ font: Font, _ gids: [Int], _ x: [Int], _ offsets: inout [Int], _ mark: Int, _ other: Int) {
        if let offset = font.markToMarkOffsets?[(gids[other] << 16) | gids[mark]] {
            offsets[2*mark] = offsets[2*other] + x[other] + offset[0] - x[mark]
            offsets[2*mark + 1] = offsets[2*other + 1] + offset[1]
        }
    }

    private func isMark(_ codePoint: Int) -> Bool {
        guard let scalar = Unicode.Scalar(UInt32(codePoint)) else {
            return false
        }
        switch scalar.properties.generalCategory {
        case .nonspacingMark, .enclosingMark:
            return true
        default:
            return false
        }
    }

    // Returns true if the character is in the blocks of the right to left scripts.
    private func isRightToLeft(_ codePoint: Int) -> Bool {
        return (codePoint >= 0x0590 && codePoint <= 0x08FF) ||
                (codePoint >= 0xFB1D && codePoint <= 0xFDFF) ||
                (codePoint >= 0xFE70 && codePoint <= 0xFEFF) ||
                (codePoint >= 0x10800 && codePoint <= 0x10FFF) ||
                (codePoint >= 0x1E800 && codePoint <= 0x1EFFF)
    }

    // Returns the order of a mark among the marks of its letter, or 0 for a
    // mark that stays where it is: the canonical combining class of the mark,
    // changed as in HarfBuzz to put the Hebrew points in the order of the SBL
    // Hebrew manual, and the Arabic shadda before the other Arabic vowel marks.
    private func markOrder(_ codePoint: Int) -> Int {
        if codePoint >= 0x05B0 && codePoint <= 0x05C7 {     // Hebrew points
            return Page.hebrewMarkOrder[codePoint - 0x05B0]
        }
        if codePoint >= 0x064B && codePoint <= 0x0652 {     // Arabic fathatan to sukun
            return Page.arabicMarkOrder[codePoint - 0x064B]
        }
        switch codePoint {
        case 0x0670: return 35      // ARABIC LETTER SUPERSCRIPT ALEF
        case 0x0E38, 0x0E39: return 103     // THAI SARA U, SARA UU
        case 0x0E3A: return 9       // THAI PHINTHU
        case 0x0E48, 0x0E49, 0x0E4A, 0x0E4B: return 107    // THAI tone marks
        case 0x0EB8, 0x0EB9: return 118     // LAO VOWEL SIGN U, UU
        case 0x0EC8, 0x0EC9, 0x0ECA, 0x0ECB: return 122    // LAO tone marks
        default: return 0
        }
    }

    private static let hebrewMarkOrder: [Int] = [
        22, 15, 16, 17, 23, 18, 19, 20,     // U+05B0 sheva to U+05B7 patah
        21, 14, 14, 24, 12, 25, 0, 13,      // U+05B8 qamats to U+05BF rafe
        0, 10, 11, 0, 230, 220, 0, 21       // U+05C0 to U+05C7 qamats qatan
    ]

    private static let arabicMarkOrder: [Int] = [
        28, 29, 30, 31, 32, 33, 27, 34      // U+064B fathatan to U+0652 sukun
    ]

    private func isMoved(_ offsets: [Int], _ start: Int, _ end: Int) -> Bool {
        for k in start..<end where offsets[2*k] != 0 || offsets[2*k + 1] != 0 {
            return true
        }
        return false
    }

    // Draws a word with moved marks in a marked content span that has the text
    // of the word as its actual text. Text extraction would otherwise take a
    // moved mark for text above or below the line, and break the word there.
    // Poppler puts the actual text where the first glyph of the span is drawn,
    // so a word that starts with a moved mark, like a Hebrew or Arabic word
    // drawn in visual order, starts with a space drawn back over the space
    // before it, and its actual text starts with a space. MuPDF takes the
    // glyphs that match the actual text as they are, and leaves out a space
    // drawn over a space.
    private func appendWordWithMovedMarks(
            _ font: Font, _ codePoints: [Int], _ gids: [Int], _ offsets: [Int], _ start: Int, _ end: Int) {
        let leadingSpace = isMoved(offsets, start, start + 1)
        var text = leadingSpace ? " " : ""
        for k in start..<end {
            // The text the glyphs map to: a glyph missing from the font is a
            // space, and an Arabic letter form is its letter.
            let codePoint = codePoints[k]
            if codePoint < Int(font.firstChar) || codePoint > Int(font.lastChar) {
                text.append(" ")
            } else if let letters = Bidi.lettersOf(UInt32(codePoint)) {
                for letter in letters {
                    text.unicodeScalars.append(Unicode.Scalar(letter)!)
                }
            } else if let scalar = Unicode.Scalar(UInt32(codePoint)) {
                text.unicodeScalars.append(scalar)
            }
        }
        append("> Tj\n/Span <</ActualText <")
        append(toUTF16Hex(text))
        append(">>> BDC\n")
        if leadingSpace {
            let space = font.unicodeToGID[0x0020]
            append("[")
            append(1000.0 * Float(font.advanceWidth[space]) / Float(font.unitsPerEm))
            append(" <")
            Page.appendCodePointAsHex(space, &self.buf)
            append(">] TJ\n")
        }
        append("<")
        for k in start..<end {
            if offsets[2*k] == 0 && offsets[2*k + 1] == 0 {
                Page.appendCodePointAsHex(gids[k], &self.buf)
            } else {
                appendMovedGlyph(font, gids[k], offsets[2*k], offsets[2*k + 1])
            }
        }
        append("> Tj\nEMC\n<")
    }

    // Ends the string of glyphs, draws the glyph moved by dx and dy font units,
    // and starts the string again.
    private func appendMovedGlyph(_ font: Font, _ gid: Int, _ dx: Int, _ dy: Int) {
        let fontSize = (textFontSize != 0.0) ? textFontSize : font.size
        append("> Tj\n")
        append(textRise + Float(dy) * fontSize / Float(font.unitsPerEm))
        append(" Ts\n")
        if dx == 0 {
            append("<")
            Page.appendCodePointAsHex(gid, &self.buf)
            append("> Tj\n")
        } else {
            let adjustment = 1000.0 * Float(dx) / Float(font.unitsPerEm)
            append("[")
            append(-adjustment)
            append(" <")
            Page.appendCodePointAsHex(gid, &self.buf)
            append("> ")
            append(adjustment)
            append("] TJ\n")
        }
        append(textRise)
        append(" Ts\n<")
    }

    /// Saves the current graphics state. Please see Example_31.
    public func saveGraphicsState() {
        append("q\n")
    }

    ///
    /// Sets the graphics state. Please see Example_31.
    ///
    /// - Parameter gs: the graphics state to use.
    ///
    public final func setGraphicsState(_ gs: GraphicsState) {
        var sb = String()
        sb.append("/CA ")
        sb.append(String(gs.getAlphaStroking()))
        sb.append(" ")
        sb.append("/ca ")
        sb.append(String(gs.getAlphaNonStroking()))
        var n = pdf.states[sb]
        if n == nil {
            n = pdf.states.count + 1
            pdf.states[sb] = n
        }
        append("/GS")
        append(n!)
        append(" gs\n")
    }

    /// Restores the last saved graphics state. Please see Example_31.
    public func restoreGraphicsState() {
        append("Q\n")
    }

    // setPenColor sets the pen color using a 24-bit RGB color integer.
    // - The integer color is expected in the format 0xRRGGBB,
    //   where R, G, and B are the red, green, and blue components respectively.
    // - The method converts the integer color to normalized float values
    //   between 0 and 1 for each RGB component and appends the color
    //   to the drawing context.
    /// Sets the pen color as a 0xRRGGBB value.
    @discardableResult
    public func setPenColor(_ color: Int32) -> Page {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        append(r)
        append(Token.space)
        append(g)
        append(Token.space)
        append(b)
        append(" RG\n")
        return self
    }

    // setPenColor sets the pen color using an RGB color array.
    // Each element in the array represents the red, green, and blue components
    // of the color as floating-point values between 0.0 and 1.0.
    //
    // Parameters:
    //   rgbColor: An optional array of 3 Float values representing the
    //   red, green, and blue color components respectively. Each value should
    //   be between 0.0 (no intensity) and 1.0 (full intensity). If the value is
    //   nil, a warning is printed and the method exits early without modifying the color.
    //
    // Notes:
    //   - The method performs a range check to ensure that each color component
    //     is within the valid range [0.0, 1.0]. If any component is out of range,
    //     the method prints a warning and exits early without modifying the color.
    //   - The method then sets the penColor property and appends the color values
    //     to the output stream (e.g., for a PDF or graphics context).
    func setPenColor(_ rgbColor: [Float]?) {
        if rgbColor == nil {
            return // Early exit if null
        }

        if rgbColor![0] < 0.0 || rgbColor![0] > 1.0 ||
           rgbColor![1] < 0.0 || rgbColor![1] > 1.0 ||
           rgbColor![2] < 0.0 || rgbColor![2] > 1.0 {
            print("Warning: RGB color values must be between 0f and 1f. Ignoring request.")
            return // Early exit if out of range
        }

        // Now set the pen color
        penColor = rgbColor!

        // Proceed with setting the color (example)
        append(rgbColor![0])
        append(Token.space)
        append(rgbColor![1])
        append(Token.space)
        append(rgbColor![2])
        append(" RG\n")
    }

    // getPenColor retrieves the current pen color as an array of float values.
    // - The returned array contains the normalized RGB values (in the range 0.0 to 1.0)
    //   representing the current pen color.
    // - The array format is [r, g, b], where r, g, and b are the red, green, and blue
    //   components of the pen color respectively.
    /// Returns the pen color as red, green and blue values between 0.0 and 1.0.
    public final func getPenColor() -> [Float] {
        return penColor
    }

    // setBrushColor sets the brush color using a 24-bit RGB color integer.
    // - The integer color is expected in the format 0xRRGGBB,
    //   where R, G, and B are the red, green, and blue components respectively.
    // - The method converts the integer color to normalized float values
    //   between 0 and 1 for each RGB component and appends the color
    //   to the drawing context for brush-related operations.
    /// Sets the brush color as a 0xRRGGBB value.
    @discardableResult
    public func setBrushColor(_ color: Int32) -> Page {
        let r = Float((color >> 16) & 0xff)/255.0
        let g = Float((color >>  8) & 0xff)/255.0
        let b = Float((color)       & 0xff)/255.0
        append(r)
        append(Token.space)
        append(g)
        append(Token.space)
        append(b)
        append(" rg\n")
        return self
    }

    // setBrushColor sets the brush color using an RGB color array.
    // Each element in the array represents the red, green, and blue components
    // of the color as floating-point values between 0.0 and 1.0.
    //
    // Parameters:
    //   rgbColor: An optional array of 3 Float values representing the
    //   red, green, and blue color components respectively. Each value should
    //   be between 0.0 (no intensity) and 1.0 (full intensity). If the value is
    //   nil, a warning is printed and the method exits early without modifying the color.
    //
    // Notes:
    //   - The method performs a range check to ensure that each color component
    //     is within the valid range [0.0, 1.0]. If any component is out of range,
    //     the method prints a warning and exits early without modifying the color.
    //   - The method then sets the brushColor property and appends the color values
    //     to the output stream (e.g., for a PDF or graphics context).
    func setBrushColor(_ rgbColor: [Float]?) {
        if rgbColor == nil {
            return // Early exit if null
        }

        if rgbColor![0] < 0.0 || rgbColor![0] > 1.0 ||
           rgbColor![1] < 0.0 || rgbColor![1] > 1.0 ||
           rgbColor![2] < 0.0 || rgbColor![2] > 1.0 {
            print("Warning: RGB color values must be between 0f and 1f. Ignoring request.")
            return // Early exit if out of range
        }

        // Now set the brush color
        brushColor = rgbColor!

        // Proceed with setting the color (example)
        append(rgbColor![0])
        append(Token.space)
        append(rgbColor![1])
        append(Token.space)
        append(rgbColor![2])
        append(" rg\n")
    }

    // getBrushColor retrieves the current brush color as an array of float values.
    // - The returned array contains the normalized RGB values (in the range 0.0 to 1.0)
    //   representing the current brush color.
    // - The array format is [r, g, b], where r, g, and b are the red, green, and blue
    //   components of the brush color respectively.
    /// Returns the brush color as red, green and blue values between 0.0 and 1.0.
    public func getBrushColor() -> [Float] {
        return brushColor
    }

    ///
    /// Sets the color for stroking operations using CMYK.
    /// The pen color is used when drawing lines and splines.
    ///
    /// - Parameter c: the cyan component is Float value from 0.0 to 1.0.
    /// - Parameter m: the magenta component is Float value from 0.0 to 1.0.
    /// - Parameter y: the yellow component is Float value from 0.0 to 1.0.
    /// - Parameter k: the black component is Float value from 0.0 to 1.0.
    ///
    public final func setPenColorCMYK(_ c: Float, _ m: Float, _ y: Float, _ k: Float) {
        append(c)
        append(Token.space)
        append(m)
        append(Token.space)
        append(y)
        append(Token.space)
        append(k)
        append(" K\n")
    }

    ///
    /// Sets the color for brush operations using CMYK.
    /// This is the color used when drawing regular text and filling shapes.
    ///
    /// - Parameter c: the cyan component is Float value from 0.0 to 1.0.
    /// - Parameter m: the magenta component is Float value from 0.0 to 1.0.
    /// - Parameter y: the yellow component is Float value from 0.0 to 1.0.
    /// - Parameter k: the black component is Float value from 0.0 to 1.0.
    ///
    public final func setBrushColorCMYK(_ c: Float, _ m: Float, _ y: Float, _ k: Float) {
        append(c)
        append(Token.space)
        append(m)
        append(Token.space)
        append(y)
        append(Token.space)
        append(k)
        append(" k\n")
    }

    ///
    /// Sets the line width to the default.
    /// The default is the finest line width.
    ///
    @discardableResult
    public func setDefaultLineWidth() -> Page {
        append("0 w\n")
        return self
    }

    ///
    /// The stroke dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    /// Examples of line dash patterns:
    ///
    /// ```
    ///   "[Array] Phase"     Appearance          Description
    ///   _______________     _________________   ____________________________________
    ///
    ///   "[] 0"              -----------------   Solid line
    ///   "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///   "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///   "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///   "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///   "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// ```
    ///
    /// - Parameter pattern: the line dash pattern.
    ///
    @discardableResult
    public func setStrokeDashPattern(_ pattern: String) -> Page {
        self.strokeDashPattern = pattern
        append(self.strokeDashPattern)
        append(" d\n")
        return self
    }

    ///
    /// Sets the default line dash pattern - solid line.
    ///
    @discardableResult
    public func setDefaultStrokeDashPattern() -> Page {
        self.strokeDashPattern = "[] 0"
        append(self.strokeDashPattern)
        append(" d\n")
        return self
    }

    ///
    /// Sets the pen width that will be used to draw lines and splines on this page.
    ///
    /// - Parameter width: the pen width.
    ///
    @discardableResult
    public func setPenWidth(_ width: Float) -> Page {
        self.penWidth = width
        append(width)
        append(" w\n")
        return self
    }

    /// Returns the current pen width.
    public func getPenWidth() -> Float {
        return self.penWidth
    }

    ///
    /// Sets the current line cap style.
    ///
    /// - Parameter style: the cap style of the current line.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
    ///
    @discardableResult
    public func setLineCapStyle(_ style: CapStyle) -> Page {
        self.lineCapStyle = style
        append(self.lineCapStyle.rawValue)
        append(" J\n")
        return self
    }

    ///
    /// Sets the line join style.
    ///
    /// - Parameter style: the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
    ///
    @discardableResult
    public func setLineJoinStyle(_ style: JoinStyle) -> Page {
        self.lineJoinStyle = style
        append(self.lineJoinStyle.rawValue)
        append(" j\n")
        return self
    }

    ///
    /// Moves the pen to the point with coordinates (x, y) on the page.
    ///
    /// - Parameter x: the x coordinate of new pen position.
    /// - Parameter y: the y coordinate of new pen position.
    ///
    public func moveTo(_ x: Float, _ y: Float) {
        append(x)
        append(Token.space)
        append(height - y)
        append(" m\n")
    }

    ///
    /// Draws a line from the current pen position to the point with coordinates (x, y),
    /// using the current pen width and stroke color.
    /// Make sure you call strokePath(), closePath() or fillPath() after the last call to this method.
    ///
    public func lineTo(_ x: Float, _ y: Float) {
        append(x)
        append(Token.space)
        append(height - y)
        append(" l\n")
    }

    ///
    /// Draws the path using the current pen color.
    ///
    public func strokePath() {
        append("S\n")
    }

    ///
    /// Closes the path and draws it using the current pen color.
    ///
    public func closePath() {
        append("s\n")
    }

    ///
    /// Closes and fills the path with the current brush color.
    ///
    public func fillPath() {
        append("f\n")
    }

    ///
    /// Draws the outline of the specified rectangle on the page.
    /// The left and right edges of the rectangle are at x and x + w.
    /// The top and bottom edges are at y and y + h.
    /// The rectangle is drawn using the current pen color.
    ///
    /// - Parameter x: the x coordinate of the rectangle to be drawn.
    /// - Parameter y: the y coordinate of the rectangle to be drawn.
    /// - Parameter w: the width of the rectangle to be drawn.
    /// - Parameter h: the height of the rectangle to be drawn.
    ///
    public func drawRect(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        closePath()
    }

    ///
    /// Fills the specified rectangle on the page.
    /// The left and right edges of the rectangle are at x and x + w.
    /// The top and bottom edges are at y and y + h.
    /// The rectangle is drawn using the current pen color.
    ///
    /// - Parameter x: the x coordinate of the rectangle to be drawn.
    /// - Parameter y: the y coordinate of the rectangle to be drawn.
    /// - Parameter w: the width of the rectangle to be drawn.
    /// - Parameter h: the height of the rectangle to be drawn.
    ///
    public func fillRect(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        fillPath()
    }

    ///
    /// Draws or fills the specified path using the current pen or brush.
    ///
    /// - Parameter path: the path.
    /// - Parameter pathOperator: the path operator, for example PathOperator.stroke or PathOperator.fill.
    ///
    public func drawPath(
            _ path: [Point],
            _ pathOperator: PathOperator) {
        if path.count < 2 {
            fatalError("The Path object must contain at least 2 points")
        }
        var point = path[0]
        moveTo(point.x, point.y)
        var controlPoint: String = ""
        for i in 1..<path.count {
            point = path[i]
            if point.controlPoint != "" {
                controlPoint = point.controlPoint
                append(point)
            } else {
                if controlPoint != "" {
                    append(point)
                    append(controlPoint)
                    append("\n")
                    controlPoint = ""
                } else {
                    lineTo(point.x, point.y)
                }
            }
        }
        append(pathOperator.rawValue)
        append("\n")
    }

    ///
    /// Draws a circle on the page.
    ///
    /// The outline of the circle is drawn using the current pen color.
    ///
    /// - Parameter x: the x coordinate of the center of the circle to be drawn.
    /// - Parameter y: the y coordinate of the center of the circle to be drawn.
    /// - Parameter r: the radius of the circle to be drawn.
    ///
    public func drawCircle(
            _ x: Float,
            _ y: Float,
            _ r: Float) {
        drawEllipse(x, y, r, r, PathOperator.stroke)
    }

    ///
    /// Draws the specified circle on the page and fills it with the current brush color.
    ///
    /// - Parameter x: the x coordinate of the center of the circle to be drawn.
    /// - Parameter y: the y coordinate of the center of the circle to be drawn.
    /// - Parameter r: the radius of the circle to be drawn.
    /// - Parameter pathOperator: the path operator, for example PathOperator.stroke or PathOperator.fill.
    ///
    public func drawCircle(
            _ x: Float,
            _ y: Float,
            _ r: Float,
            _ pathOperator: PathOperator) {
        drawEllipse(x, y, r, r, pathOperator)
    }

    ///
    /// Draws an ellipse on the page using the current pen color.
    ///
    /// - Parameter x: the x coordinate of the center of the ellipse to be drawn.
    /// - Parameter y: the y coordinate of the center of the ellipse to be drawn.
    /// - Parameter r1: the horizontal radius of the ellipse to be drawn.
    /// - Parameter r2: the vertical radius of the ellipse to be drawn.
    ///
    public func drawEllipse(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float) {
        drawEllipse(x, y, r1, r2, PathOperator.stroke)
    }

    ///
    /// Fills an ellipse on the page using the current pen color.
    ///
    /// - Parameter x: the x coordinate of the center of the ellipse to be drawn.
    /// - Parameter y: the y coordinate of the center of the ellipse to be drawn.
    /// - Parameter r1: the horizontal radius of the ellipse to be drawn.
    /// - Parameter r2: the vertical radius of the ellipse to be drawn.
    ///
    public func fillEllipse(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float) {
        drawEllipse(x, y, r1, r2, PathOperator.fill)
    }

    ///
    /// Draws an ellipse on the page and fills it using the current brush color.
    ///
    /// - Parameter x: the x coordinate of the center of the ellipse to be drawn.
    /// - Parameter y: the y coordinate of the center of the ellipse to be drawn.
    /// - Parameter r1: the horizontal radius of the ellipse to be drawn.
    /// - Parameter r2: the vertical radius of the ellipse to be drawn.
    /// - Parameter operation: the operation.
    ///
    private func drawEllipse(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float,
            _ pathOperator: PathOperator) {
        // The best 4-spline magic number
        let m4: Float = 0.55228

        // Starting point
        moveTo(x, y - r2)

        appendPointXY(x + m4*r1, y - r2)
        appendPointXY(x + r1, y - m4*r2)
        appendPointXY(x + r1, y)
        append("c\n")

        appendPointXY(x + r1, y + m4*r2)
        appendPointXY(x + m4*r1, y + r2)
        appendPointXY(x, y + r2)
        append("c\n")

        appendPointXY(x - m4*r1, y + r2)
        appendPointXY(x - r1, y + m4*r2)
        appendPointXY(x - r1, y)
        append("c\n")

        appendPointXY(x - r1, y - m4*r2)
        appendPointXY(x - m4*r1, y - r2)
        appendPointXY(x, y - r2)
        append("c\n")

        append(pathOperator.rawValue)
        append("\n")
    }

    ///
    /// Draws a point on the page using the current pen color.
    ///
    /// - Parameter p: the point.
    ///
    public func drawPoint(_ p: Point) {
        if p.shape != Point.INVISIBLE  {
            var list: [Point]
            if p.shape == Point.CIRCLE {
                drawCircle(p.x, p.y, p.r, p.getPathOperator())
            } else if p.shape == Point.DIAMOND {
                list = [Point]()
                list.append(Point(p.x, p.y - p.r*1.2))
                list.append(Point(p.x + p.r*1.2, p.y))
                list.append(Point(p.x, p.y + p.r*1.2))
                list.append(Point(p.x - p.r*1.2, p.y))
                drawPath(list, p.getPathOperator())
            } else if p.shape == Point.BOX {
                list = [Point]()
                list.append(Point(p.x - p.r*0.886, p.y - p.r*0.886))
                list.append(Point(p.x + p.r*0.886, p.y - p.r*0.886))
                list.append(Point(p.x + p.r*0.886, p.y + p.r*0.886))
                list.append(Point(p.x - p.r*0.886, p.y + p.r*0.886))
                drawPath(list, p.getPathOperator())
            } else if p.shape == Point.PLUS {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            } else if p.shape == Point.UP_ARROW {
                list = [Point]()
                list.append(Point(p.x, p.y - p.r))
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y + p.r))
                list.append(Point(p.x, p.y - p.r))
                drawPath(list, p.getPathOperator())
            } else if p.shape == Point.DOWN_ARROW {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y - p.r))
                list.append(Point(p.x, p.y + p.r))
                list.append(Point(p.x - p.r, p.y - p.r))
                drawPath(list, p.getPathOperator())
            } else if p.shape == Point.LEFT_ARROW {
                list = [Point]()
                list.append(Point(p.x + p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y))
                list.append(Point(p.x + p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y + p.r))
                drawPath(list, p.getPathOperator())
            } else if p.shape == Point.RIGHT_ARROW {
                list = [Point]()
                list.append(Point(p.x - p.r, p.y - p.r))
                list.append(Point(p.x + p.r, p.y))
                list.append(Point(p.x - p.r, p.y + p.r))
                list.append(Point(p.x - p.r, p.y - p.r))
                drawPath(list, p.getPathOperator())
            } else if p.shape == Point.H_DASH {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
            } else if p.shape == Point.V_DASH {
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            } else if p.shape == Point.X_MARK {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r)
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r)
            } else if p.shape == Point.MULTIPLY {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r)
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r)
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y)
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r)
            } else if p.shape == Point.STAR {
                list = [Point]()
                for i in 0..<10 {
                    let theta = Double(i) * 36.0 * (Double.pi / 180.0)
                    let radius = (i % 2 == 0)
                    ? Double(p.r) * 1.147
                    : Double(p.r) * 0.38196 * 1.147
                    let x = p.x + Float(radius * sin(theta))
                    let y = p.y - Float(radius * cos(theta))  // minus because y grows down
                    list.append(Point(x, y))
                }
                drawPath(list, p.getPathOperator())
            }
        }
    }

    ///
    /// Sets the text rendering mode.
    ///
    /// - Parameter mode: the rendering mode.
    ///
    @discardableResult
    public func setTextRenderingMode(_ mode: Int) throws -> Page {
        if mode >= 0 && mode <= 7 {
            self.renderingMode = mode
        } else {
            throw PDFjetError(message: "Invalid text rendering mode: \(mode)")
        }
        return self
    }

    ///
    /// Sets the text direction.
    ///
    /// - Parameter angleInDegrees: the angle in degrees.
    ///
    @discardableResult
    public func setTextDirection(_ angleInDegrees: Int) -> Page {
        var degrees: Int = angleInDegrees
        if degrees > 360 {
            degrees %= 360
        }
        if degrees == 0 {
            self.tmx = [ 1.0,  0.0,  0.0,  1.0 ]
        } else if degrees == 90 {
            self.tmx = [ 0.0,  1.0, -1.0,  0.0 ]
        } else if degrees == 180 {
            self.tmx = [-1.0,  0.0,  0.0, -1.0 ]
        } else if degrees == 270 {
            self.tmx = [ 0.0, -1.0,  1.0,  0.0 ]
        } else if degrees == 360 {
            self.tmx = [ 1.0,  0.0,  0.0,  1.0 ]
        } else {
            let sinOfAngle = Float(sin(Float(degrees) * (Float.pi / 180.0)))
            let cosOfAngle = Float(cos(Float(degrees) * (Float.pi / 180.0)))
            self.tmx = [cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle]
        }
        self.tm0 = FastFloat.toByteArray(tmx[0])
        self.tm1 = FastFloat.toByteArray(tmx[1])
        self.tm2 = FastFloat.toByteArray(tmx[2])
        self.tm3 = FastFloat.toByteArray(tmx[3])
        return self
    }

    ///
    /// Draws a cubic bezier curve starting from the current point to the end point p3
    ///
    /// - Parameter x1: first control point x
    /// - Parameter y1: first control point y
    /// - Parameter x2: second control point x
    /// - Parameter y2: second control point y
    /// - Parameter x3: end point x
    /// - Parameter y3: end point y
    ///
    public func curveTo(
        _ x1: Float, _ y1: Float,
        _ x2: Float, _ y2: Float,
        _ x3: Float, _ y3: Float) {
        append(x1)
        append(Token.space)
        append(height - y1)
        append(Token.space)
        append(x2)
        append(Token.space)
        append(height - y2)
        append(Token.space)
        append(x3)
        append(Token.space)
        append(height - y3)
        append(" c\n")
    }

    ///
    /// Adds a circular arc to the current path.
    ///
    /// - Parameter x: the x coordinate of the center.
    /// - Parameter y: the y coordinate of the center.
    /// - Parameter r: the radius.
    /// - Parameter startAngle: the start angle in degrees.
    /// - Parameter sweepDegrees: the sweep angle in degrees.
    /// - Returns: the control points and the end point of the last curve segment.
    ///
    public func drawCircularArc(
        _ x: Float, _ y: Float, _ r: Float, _ startAngle: Float, _ sweepDegrees: Float) -> [Float] {
        return drawArc(x, y, r, r, startAngle, sweepDegrees)
    }

    ///
    /// Adds an elliptical arc to the current path.
    ///
    /// - Parameter x: the x coordinate of the center.
    /// - Parameter y: the y coordinate of the center.
    /// - Parameter rx: the horizontal radius.
    /// - Parameter ry: the vertical radius.
    /// - Parameter startAngle: the start angle in degrees.
    /// - Parameter sweepDegrees: the sweep angle in degrees.
    /// - Returns: the control points and the end point of the last curve segment.
    ///
    public func drawArc(
            _ x: Float,
            _ y: Float,
            _ rx: Float,
            _ ry: Float,
            _ startAngle: Float,
            _ sweepDegrees: Float) -> [Float] {
        var x1: Float = 0.0
        var y1: Float = 0.0
        var x2: Float = 0.0
        var y2: Float = 0.0
        var x3: Float = 0.0
        var y3: Float = 0.0

        let numSegments = Int(ceil(abs(sweepDegrees) / 90.0))
        var angleRad = Double(startAngle) * .pi / 180.0
        let deltaPerSeg = Double(sweepDegrees / Float(numSegments)) * .pi / 180.0

        for i in 0..<numSegments {
            let segStart = angleRad
            let segEnd = angleRad + deltaPerSeg
            let deltaRad = segEnd - segStart // guaranteed ≤ ±π/2

            // Calculate safe κ
            let k = Float(4.0 / 3.0 * tan(deltaRad / 4.0))

            let cosStart = Float(cos(segStart))
            let sinStart = Float(sin(segStart))
            let cosEnd = Float(cos(segEnd))
            let sinEnd = Float(sin(segEnd))

            // End points
            let x0 = x + rx * cosStart
            let y0 = y + ry * sinStart
            x3 = x + rx * cosEnd
            y3 = y + ry * sinEnd

            // Control points
            x1 = x0 - (k * rx * sinStart)
            y1 = y0 + (k * ry * cosStart)
            x2 = x3 + (k * rx * sinEnd)
            y2 = y3 - (k * ry * cosEnd)

            if i == 0 {
                moveTo(x0, y0)
            }
            curveTo(x1, y1, x2, y2, x3, y3)

            angleRad = segEnd
        }

        return [x1, y1, x2, y2, x3, y3]
    }

    ///
    /// Draws a bezier curve starting from the current point.
    /// **Please note:** You must call the fillPath,
    /// closePath or strokePath method after the last bezierCurveTo call.
    ///
    /// *Author:* **Pieter Libin**, pieter@emweb.be
    ///
    /// - Parameter p1: first control point
    /// - Parameter p2: second control point
    /// - Parameter p3: end point
    ///
    public func bezierCurveTo(
            _ p1: Point,
            _ p2: Point,
            _ p3: Point) {
        append(p1)
        append(p2)
        append(p3)
        append("c\n")
    }

    internal func setTextFont(_ font: Font, _ fontSize: Float) {
        if font.fontID != nil {
            append("/")
            append(font.fontID!)
        } else {
            append("/F")
            append(font.objNumber)
        }
        append(Token.space)
        append(fontSize)
        append(" Tf\n")
        self.textFontSize = fontSize
    }

    // Original code provided by:
    // Dominique Andre Gunia <contact@dgunia.de>
    // >>
    /// Draws a rectangle with rounded corners.
    public func drawRectRoundCorners(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float,
            _ r1: Float,
            _ r2: Float,
            _ pathOperator: PathOperator) {
        // The best 4-spline magic number
        let m4: Float = 0.55228
        var list = [Point]()
        // Starting point
        list.append(Point(x + w - r1, y))
        list.append(Point(x + w - r1 + m4*r1, y, Point.controlPointC))
        list.append(Point(x + w, y + r2 - m4*r2, Point.controlPointC))
        list.append(Point(x + w, y + r2))

        list.append(Point(x + w, y + h - r2))
        list.append(Point(x + w, y + h - r2 + m4*r2, Point.controlPointC))
        list.append(Point(x + w - m4*r1, y + h, Point.controlPointC))
        list.append(Point(x + w - r1, y + h))

        list.append(Point(x + r1, y + h))
        list.append(Point(x + r1 - m4*r1, y + h, Point.controlPointC))
        list.append(Point(x, y + h - m4*r2, Point.controlPointC))
        list.append(Point(x, y + h - r2))

        list.append(Point(x, y + r2))
        list.append(Point(x, y + r2 - m4*r2, Point.controlPointC))
        list.append(Point(x + m4*r1, y, Point.controlPointC))
        list.append(Point(x + r1, y))
        list.append(Point(x + w - r1, y))

        drawPath(list, pathOperator)
    }

    ///
    /// Clips the path.
    ///
    public func clipPath() {
        append("W\n")
        append("n\n")   // Close the path without painting it.
    }

    /// Sets the clipping path to the specified rectangle.
    public func clipRect(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        clipPath()
    }

    // <<

    ///
    /// Sets the page CropBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX: the top left X coordinate of the CropBox.
    /// - Parameter upperLeftY: the top left Y coordinate of the CropBox.
    /// - Parameter lowerRightX: the bottom right X coordinate of the CropBox.
    /// - Parameter lowerRightY: the bottom right Y coordinate of the CropBox.
    ///
    @discardableResult
    public func setCropBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) -> Page {
        self.cropBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
        return self
    }

    ///
    /// Sets the page BleedBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX: the top left X coordinate of the BleedBox.
    /// - Parameter upperLeftY: the top left Y coordinate of the BleedBox.
    /// - Parameter lowerRightX: the bottom right X coordinate of the BleedBox.
    /// - Parameter lowerRightY: the bottom right Y coordinate of the BleedBox.
    ///
    @discardableResult
    public func setBleedBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) -> Page {
        self.bleedBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
        return self
    }

    ///
    /// Sets the page TrimBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX: the top left X coordinate of the TrimBox.
    /// - Parameter upperLeftY: the top left Y coordinate of the TrimBox.
    /// - Parameter lowerRightX: the bottom right X coordinate of the TrimBox.
    /// - Parameter lowerRightY: the bottom right Y coordinate of the TrimBox.
    ///
    @discardableResult
    public func setTrimBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) -> Page {
        self.trimBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
        return self
    }

    ///
    /// Sets the page ArtBox.
    /// See page 77 of the PDF32000_2008.pdf specification.
    ///
    /// - Parameter upperLeftX: the top left X coordinate of the ArtBox.
    /// - Parameter upperLeftY: the top left Y coordinate of the ArtBox.
    /// - Parameter lowerRightX: the bottom right X coordinate of the ArtBox.
    /// - Parameter lowerRightY: the bottom right Y coordinate of the ArtBox.
    ///
    @discardableResult
    public func setArtBox(
            _ upperLeftX: Float,
            _ upperLeftY: Float,
            _ lowerRightX: Float,
            _ lowerRightY: Float) -> Page {
        self.artBox = [upperLeftX, upperLeftY, lowerRightX, lowerRightY]
        return self
    }

    private func appendPointXY(_ x: Float, _ y: Float) {
        append(x)
        append(Token.space)
        append(height - y)
        append(Token.space)
    }

    private func append(_ point: Point) {
        append(point.x)
        append(Token.space)
        append(height - point.y)
        append(Token.space)
    }

    func append(_ str: String) {
        self.buf.append(contentsOf: str.utf8)
    }

    func append(_ num: UInt32) {
        append(String(num))
    }

    func append(_ num: Int) {
        append(String(num))
    }

    func append(_ val: Float) {
        append(FastFloat.toByteArray(val))
    }

    func append(_ byte: UInt8) {
        self.buf.append(byte)
    }

    /// Appends the bytes to the content stream of this page.
    public func append(_ buffer: [UInt8]) {
        self.buf.append(contentsOf: buffer)
    }

    private func drawWord(
            _ font: Font,
            _ str: inout String,
            _ textColor: [Float],
            _ highlightColors: [String : Int32]) {
        if str != "" {
            if highlightColors[str] != nil {
                setBrushColor(highlightColors[str]!)
            } else {
                setBrushColor(textColor)
            }

            if font.isCoreFont {
                append("[<")
                drawASCIIString(font, str)
                append(">] TJ\n")
            } else {
                append("<")
                drawUnicodeString(font, str)
                append("> Tj\n")
            }
            str = ""
        }
    }

    func drawColoredString(
            _ font: Font,
            _ text: String,
            _ color: [Float],
            _ highlightColors: [String : Int32]) {
        var buf1 = String()
        var buf2 = String()
        for scalar in text.unicodeScalars {
            if isLetterOrDigit(scalar) {
                drawWord(font, &buf2, color, highlightColors)
                buf1.append(String(scalar))
            } else {
                drawWord(font, &buf1, color, highlightColors)
                buf2.append(String(scalar))
            }
        }
        drawWord(font, &buf1, color, highlightColors)
        drawWord(font, &buf2, color, highlightColors)
    }

    func setStructElementsPageObjNumber(
            _ pageObjNumber: Int) {
        for element in structures {
            element.pageObjNumber = pageObjNumber
        }
    }

    /// Begins marked content for a structure element when the document is PDF/UA compliant.
    public func addBMC(
            _ structure: String,
            _ actualText: String,
            _ altDescription: String) {
        addBMC(structure, nil, actualText, altDescription)
    }

    /// Begins marked content in the specified language for a structure element when the document is PDF/UA compliant.
    public func addBMC(
            _ structure: String,
            _ language: String?,
            _ actualText: String,
            _ altDescription: String) {
        if pdf.compliance == Compliance.PDF_UA_1 {
            let element = StructElem()
            element.structure = structure
            element.mcid = mcid
            element.language = language
            element.actualText = actualText
            element.altDescription = altDescription
            structures.append(element)
            pdf.structElements.append(element)
            append("/")
            append(structure)
            append(" <</MCID ")
            append(mcid)
            append(Token.endDictionary)
            append("BDC\n")
            mcid += 1
        }
    }

    /// Begins marked content for an artifact when the document is PDF/UA compliant.
    public func addArtifactBMC() {
        if pdf.compliance == Compliance.PDF_UA_1 {
            append("/Artifact BMC\n")
        }
    }

    /// Ends the current marked content when the document is PDF/UA compliant.
    public func addEMC() {
        if pdf.compliance == Compliance.PDF_UA_1 {
            append("EMC\n")
        }
    }

    func addAnnotation(_ annotation: Annotation) {
        annotation.y1 = self.height - annotation.y1
        annotation.y2 = self.height - annotation.y2
        self.annots.append(annotation)
        if pdf.compliance == Compliance.PDF_UA_1 {
            let element = StructElem()
            element.structure = StructElem.Link
            element.language = annotation.language
            element.actualText = annotation.actualText
            element.altDescription = annotation.altDescription
            element.annotation = annotation
            self.structures.append(element)
            pdf.structElements.append(element)
        }
    }

    func beginTransform(
            _ x: Float,
            _ y: Float,
            _ xScale: Float,
            _ yScale: Float) {
        saveGraphicsState()

        append(xScale)
        append(" 0 0 ")
        append(yScale)
        append(Token.space)
        append(x)
        append(Token.space)
        append(y)
        append(" cm\n")

        append(xScale)
        append(" 0 0 ")
        append(yScale)
        append(Token.space)
        append(x)
        append(Token.space)
        append(y)
        append(" Tm\n")
    }

    func endTransform() {
        restoreGraphicsState()
    }

    /// Draws the content stream scaled and placed at the specified location.
    public func drawContents(
            _ content: [UInt8],
            _ h: Float,     // The height of the graphics object in points.
            _ x: Float,
            _ y: Float,
            _ xScale: Float,
            _ yScale: Float) {
        beginTransform(x, (self.height - yScale * h) - y, xScale, yScale)
        append(content)
        endTransform()
    }

    /// Draws the characters of the string one at a time, dx apart.
    public func drawString(
            _ font: Font,
            _ str: String,
            _ x: Float,
            _ y: Float,
            _ dx: Float) {
        let scalars = Array(str.unicodeScalars)
        var x1 = x
        for scalar in scalars {
            drawString(font, String(scalar), x1, y)
            x1 += dx
        }
    }

    private func isLetterOrDigit(_ scalar: UnicodeScalar) -> Bool {
        if (scalar.value >= 65 && scalar.value <= 90) ||
            (scalar.value >= 97 && scalar.value <= 122) ||
            (scalar.value >= 48 && scalar.value <= 57) {
            return true
        }
        return false
    }

    private func isLetterOrDigit(_ value: Int) -> Bool {
        if (value >= 65 && value <= 90) ||
            (value >= 97 && value <= 122) ||
            (value >= 48 && value <= 57) {
            return true
        }
        return false
    }

    private func appendTwoHexDigits(_ number: Int, _ buffer: inout [UInt8]) {
        let index = (number & 0xFF) << 1
        buffer.append(hexadecimal.digits[index])
        buffer.append(hexadecimal.digits[index + 1])
    }

    /// The hexadecimal digits.
    public static let HEX: [UInt8] = [
        UInt8(ascii: "0"), UInt8(ascii: "1"), UInt8(ascii: "2"), UInt8(ascii: "3"),
        UInt8(ascii: "4"), UInt8(ascii: "5"), UInt8(ascii: "6"), UInt8(ascii: "7"),
        UInt8(ascii: "8"), UInt8(ascii: "9"), UInt8(ascii: "A"), UInt8(ascii: "B"),
        UInt8(ascii: "C"), UInt8(ascii: "D"), UInt8(ascii: "E"), UInt8(ascii: "F")
    ]

    private static func appendCodePointAsHex(_ codePoint: Int, _ buf: inout [UInt8]) {
        if codePoint <= 0xFFFF {
            // Basic Multilingual Plane (BMP) character
            buf.append(HEX[(codePoint >> 12) & 0xF])
            buf.append(HEX[(codePoint >> 8)  & 0xF])
            buf.append(HEX[(codePoint >> 4)  & 0xF])
            buf.append(HEX[(codePoint)       & 0xF])
        } else {
            // Supplementary character (needs surrogate pair in UTF-16)
            // Write as 6 hex digits (max Unicode code point is 0x10FFFF)
            buf.append(HEX[(codePoint >> 20) & 0xF])
            buf.append(HEX[(codePoint >> 16) & 0xF])
            buf.append(HEX[(codePoint >> 12) & 0xF])
            buf.append(HEX[(codePoint >> 8)  & 0xF])
            buf.append(HEX[(codePoint >> 4)  & 0xF])
            buf.append(HEX[(codePoint)       & 0xF])
        }
    }

    /// Draws the text as a watermark diagonally across the page.
    public func addWatermark(
            _ font: Font,
            _ text: String) throws {
        let hypotenuse: Float =
                sqrt(self.height * self.height + self.width * self.width)
        let stringWidth = font.stringWidth(text)
        let offset = (hypotenuse - stringWidth) / 2.0
        let angle = atan(self.height / self.width)
        let watermark = TextLine(font)
        watermark.setTextColor(Color.lightgrey)
        watermark.setText(text)
        watermark.setLocation(
                Float(offset * cos(angle)),
                (self.height - Float(offset * sin(angle))))
        watermark.setTextDirection(Int((angle * (180.0 / Float.pi))))
        watermark.drawOn(self)
    }

    /// Flips the y axis, so the origin is the top left corner of the page and y grows downward.
    public func invertYAxis() {
        append("1 0 0 -1 0 ")
        append(self.height)
        append(" cm\n")
    }

    /// Draws the text line centered at the top of the page.
    @discardableResult
    public func addHeader(_ textLine: TextLine) throws -> [Float] {
        return try addHeader(textLine, 1.5*textLine.font!.ascent)
    }

    /// Draws the text line centered at the top of the page, with its baseline at the specified offset.
    @discardableResult
    public func addHeader(_ textLine: TextLine, _ offset: Float) throws -> [Float] {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, offset)
        var xy = textLine.drawOn(self)
        xy[1] += textLine.font!.descent
        return xy
    }

    /// Draws the text line centered at the bottom of the page.
    @discardableResult
    public func addFooter(_ textLine: TextLine) throws -> [Float] {
        return try addFooter(textLine, textLine.font!.ascent)
    }

    /// Draws the text line centered at the bottom of the page, with its baseline at the specified offset from the bottom.
    @discardableResult
    public func addFooter(_ textLine: TextLine, _ offset: Float) throws -> [Float] {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, getHeight() - offset)
        return textLine.drawOn(self)
    }

    /**
     * Begin text block.
     */
    func beginText() {
        append(Token.beginText)
    }

    /**
     * End the text block.
     */
    func endText() {
        append(Token.endText)
    }

    /**
     * Sets the text location.
     *
     * - Parameter x: the x coordinate of new text location.
     * - Parameter y: the y coordinate of new text location.
     */
    func setTextLocation(_ x: Float, _ y: Float) {
        append(x)
        append(Token.space)
        append(height - y)
        append(" Td\n")
    }

    /**
     * Sets the text leading.
     * - Parameter leading: the leading.
     */
    func setTextLeading(_ leading: Float) {
        append(leading)
        append(" TL\n")
    }

    /**
     * Advance to the next line.
     */
    func nextLine() {
        append("T*\n")
    }

    func setTextScaling(_ scaling: Float) {
        append(scaling)
        append(" Tz\n")
    }

    func setTextRise(_ rise: Float) {
        append(rise)
        append(" Ts\n")
        self.textRise = rise
    }

    /**
     * Draws a string at the correct location.
     * - Parameter str: the string.
     */
    internal func drawTextLine(_ font: Font, _ str: String, _ x: Float, _ y: Float) {
        append(Token.beginText)
        setTextLocation(x, y)
        setTextFont(font, font.size)
        if font.isCoreFont {
            append("[<")
            drawASCIIString(font, str)
            append(">] TJ\n")
        } else {
            append("<")
            drawUnicodeString(font, str)
            append("> Tj\n")
        }
        append(Token.endText)
    }

    func scaleAndRotate(_ x: Float, _ y: Float, _ w: Float, _ h: Float, _ degrees: Float) {
        // PDF transformations apply LAST-TO-FIRST (like a stack: last command = first applied)

        // [FINAL POSITIONING - Applied First]
        // Moves rotated/scaled image to target (x,y) on page
        append("1 0 0 1 ")
        append(x + w/2)
        append(Token.space)
        append((height - y) - h/2)
        append(" cm\n")

        // [ROTATION - Applied Second]
        // Rotates around current origin (0,0) by 'degrees'
        let radians = degrees * (Float.pi / 180)
        let cosValue = Float(cos(radians))
        let sinValue = Float(sin(radians))
        append(FastFloat.toByteArray(cosValue))
        append(Token.space)
        append(FastFloat.toByteArray(sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(-sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(cosValue))
        append(" 0 0 cm\n")

        // [ORIGIN SETUP - Applied Last]
        // Centers image at (0,0) and sets scale
        append(w)
        append(" 0 0 ")
        append(h)
        append(Token.space)
        append(-w/2)
        append(Token.space)
        append(-h/2)
        append(" cm\n")
    }

    func rotateAroundCenter(_ centerX: Float, _ centerY: Float, _ degrees: Float) {
        append("1 0 0 1 ")
        append(centerX)
        append(Token.space)
        append(centerY)
        append(" cm\n")

        let radians = degrees * Float.pi / 180
        let cosValue = Float(cos(radians))
        let sinValue = Float(sin(radians))
        append(FastFloat.toByteArray(cosValue))
        append(Token.space)
        append(FastFloat.toByteArray(sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(-sinValue))
        append(Token.space)
        append(FastFloat.toByteArray(cosValue))
        append(" 0 0 cm\n")

        append("1 0 0 1 ")
        append(-centerX)
        append(Token.space)
        append(-centerY)
        append(" cm\n")
    }

    func drawTextBlock(
            _ font: Font,
            _ fontSize: Float,
            _ textLines: [TextLine],
            _ x: Float,
            _ y: Float,
            _ leading: Float,
            _ color: [Float],
            _ highlightColors: Dictionary<String, Int32>?,
            _ language: String?) {
        if textLines.count == 0 {
            return
        }

        // A span gives the language of the text, for screen readers and text extraction.
        let hasLanguage = language != nil && !language!.isEmpty
        if hasLanguage {
            append("/Span <</Lang <")
            append(toUTF16Hex(language!))
            append(">>> BDC\n")
        }
        append("BT\n")
        setBrushColor(color)
        setTextFont(font, fontSize)
        var yText: Float = y
        for textLine in textLines {
            append("1 0 0 1 ")
            append(x + textLine.xOffset)
            append(" ")
            append(height - (yText + font.getAscent(fontSize)))
            append(" Tm\n")
            if highlightColors == nil {
                if font.isCoreFont {
                    append("[<")
                    drawASCIIString(font, textLine.text!)
                    append(">] TJ\n")
                } else {
                    append("<")
                    drawUnicodeString(font, textLine.text!)
                    append("> Tj\n")
                }
            } else {
                drawColoredString(font, textLine.text!, color, highlightColors!)
            }
            yText += leading
        }
        append("ET\n")
        if hasLanguage {
            append("EMC\n")
        }

        var yLine = y + font.getBodyHeight(fontSize)
        for textLine in textLines {
            if textLine.underline {
                moveTo(x + textLine.xOffset, yLine)
                lineTo(x + textLine.xOffset + font.stringWidth(fontSize, textLine.text), yLine)
                strokePath()
            }
            yLine += leading
        }
    }

    /// Returns the string as a PDF text string, in UTF-16BE with a byte order
    /// mark, written in hexadecimal.
    private func toUTF16Hex(_ str: String) -> String {
        let digits = Array("0123456789ABCDEF")
        var hex = "FEFF"
        for unit in str.utf16 {
            hex.append(digits[Int((unit >> 12) & 0xF)])
            hex.append(digits[Int((unit >> 8) & 0xF)])
            hex.append(digits[Int((unit >> 4) & 0xF)])
            hex.append(digits[Int(unit & 0xF)])
        }
        return hex
    }
}   // End of Page.swift
