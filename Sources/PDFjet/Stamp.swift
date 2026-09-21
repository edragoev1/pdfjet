//
//  Stamp.swift
//
//  Copyright (c) 2026 PDFjet Software
//  Licensed under the MIT License. See LICENSE file in the project root.
//

import Foundation

///
/// Content that is drawn once with the path and text methods of this class,
/// written to the document as a PDF form XObject by complete, and placed on
/// pages with drawOn, at a location, a rotation and a scale, as a Container
/// is.
///
/// What a stamp is good at: the content is in the file once, and each
/// placement is the q, the transform and the Do that name it -- 83 bytes for a
/// 200 by 50 point box with two lines of text, against the 367 a Container
/// writes into every page for the same drawing. From the fourth page on the
/// stamp is the smaller of the two: over 100 pages it saves 12,855 bytes of a
/// 104,530 byte file, and over 500 pages 66,052 of 275,553. The whole stamp is
/// one structure element in a PDF/UA document, with the alternate description,
/// the language and the actual text it is given, which is what a logo or a
/// watermark wants.
///
/// What it costs: a stamp draws with the methods of this class -- moveTo,
/// lineTo, curveTo, drawRect, fillRect and drawText -- and holds no
/// drawable of its own, so it has no images, no annotations, no tables and no
/// barcodes. Its text needs a font that is embedded in the document: a core
/// font and a CJK font are refused, since a stamp has no map from their
/// characters to their glyphs. It has to be completed before it is drawn, and
/// completed once. On fewer than four pages it is the larger of the two, since
/// its XObject costs about 300 bytes of its own.
///
/// Use a Container instead to group drawable elements -- Rect, TextLine,
/// Image, Table, a chart, a barcode, an annotation -- that are moved, rotated
/// and scaled together, and to lay a group out once for one page. A container
/// can hold a stamp, so a header written once can be placed inside a group
/// that is rotated with the rest of it.
///
/// Please see Example_35.
///
public class Stamp : Drawable {
    internal var objNumber: Int?

    private let pdf: PDF
    private var x: Float = 0
    private var y: Float = 0
    private var width: Float = 0
    private var height: Float = 0
    private var fillColor: [Float]?
    private var strokeColor: [Float]?
    private var strokeWidth: Float = 1.0
    private var rotateDegrees: Float = 0
    private var scaleX: Float = 1.0
    private var scaleY: Float = 1.0
    private var buf = [UInt8]()
    private var fonts: [Font] = []
    private var language: String?
    private var actualText: String?
    private var altDescription: String?
    private var completed = false

    /// Creates a stamp for the specified document.
    public init(_ pdf: PDF) {
        self.pdf = pdf
    }

    /// Sets the size of this stamp.
    @discardableResult
    public func setSize(_ width: Float, _ height: Float) -> Stamp {
        self.width = width
        self.height = height
        return self
    }

    /// Adds a font used by the text on this stamp.
    @discardableResult
    public func addFont(_ font: Font) -> Stamp {
        if let identity = font.pdfIdentity, identity != pdf.identity {
            pdf.fail("The font belongs to another PDF.")
            return self
        }
        if !fonts.contains(where: { $0 === font }) {
            fonts.append(font)
        }
        return self
    }

    /// Sets the location of the top left corner of this stamp on the page.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the language of this stamp, used for accessibility, for example "en-US".
    @discardableResult
    public func setLanguage(_ language: String) -> Stamp {
        self.language = language
        return self
    }

    /// Sets the alternate description of this stamp.
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Stamp {
        self.altDescription = altDescription
        return self
    }

    /// Sets the actual text for this stamp.
    @discardableResult
    public func setActualText(_ actualText: String) -> Stamp {
        self.actualText = actualText
        return self
    }

    /// Sets the fill color for the content drawn after it, from an array of red, green and blue values.
    @discardableResult
    public func setFillColor(_ rgbColor: [Float]) -> Stamp {
        append(rgbColor[0])
        append(" ")
        append(rgbColor[1])
        append(" ")
        append(rgbColor[2])
        append(" rg\n")
        self.fillColor = rgbColor
        return self
    }

    /// Sets the fill color for the content drawn after it, as a 0xRRGGBB value.
    @discardableResult
    public func setFillColor(_ color: Int32) -> Stamp {
        let r = Float((color >> 16) & 0xff) / 255.0
        let g = Float((color >> 8) & 0xff) / 255.0
        let b = Float(color & 0xff) / 255.0

        append(r)
        append(" ")
        append(g)
        append(" ")
        append(b)
        append(" rg\n")

        self.fillColor = [r, g, b]
        return self
    }

    /// Sets the stroke color for the content drawn after it, from an array of red, green and blue values.
    @discardableResult
    public func setStrokeColor(_ rgbColor: [Float]) -> Stamp {
        append(rgbColor[0])
        append(" ")
        append(rgbColor[1])
        append(" ")
        append(rgbColor[2])
        append(" RG\n")
        self.strokeColor = rgbColor
        return self
    }

    /// Sets the stroke color for the content drawn after it, as a 0xRRGGBB value.
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> Stamp {
        let r = Float((color >> 16) & 0xff) / 255.0
        let g = Float((color >> 8) & 0xff) / 255.0
        let b = Float(color & 0xff) / 255.0

        append(r)
        append(" ")
        append(g)
        append(" ")
        append(b)
        append(" RG\n")

        self.strokeColor = [r, g, b]
        return self
    }

    /// Sets the stroke width for the content drawn after it.
    @discardableResult
    public func setStrokeWidth(_ width: Float) -> Stamp {
        if width < 0.0 {
            pdf.fail("The stroke width cannot be negative.")
            return self
        }
        append(width)
        append(" w\n")
        self.strokeWidth = width
        return self
    }

    /// Begins a new path at the specified point.
    @discardableResult
    public func moveTo(_ x: Float, _ y: Float) -> Stamp {
        append(x)
        append(" ")
        append(height - y)
        append(" m\n")
        return self
    }

    /// Adds a straight line from the current point to the specified point.
    @discardableResult
    public func lineTo(_ x: Float, _ y: Float) -> Stamp {
        append(x)
        append(" ")
        append(height - y)
        append(" l\n")
        return self
    }

    /// Adds a cubic Bézier curve from the current point to x3, y3, using x1, y1 and x2, y2 as control points.
    @discardableResult
    public func curveTo(
        _ x1: Float, _ y1: Float,
        _ x2: Float, _ y2: Float,
        _ x3: Float, _ y3: Float
    ) -> Stamp {
        append(x1)
        append(" ")
        append(height - y1)
        append(" ")
        append(x2)
        append(" ")
        append(height - y2)
        append(" ")
        append(x3)
        append(" ")
        append(height - y3)
        append(" c\n")
        return self
    }

    /// Strokes the current path.
    @discardableResult
    public func strokePath() -> Stamp {
        append("S\n")
        return self
    }

    /// Closes and strokes the current path.
    @discardableResult
    public func closePath() -> Stamp {
        append("s\n")
        return self
    }

    /// Fills the current path.
    @discardableResult
    public func fillPath() -> Stamp {
        append("f\n")
        return self
    }

    /// Closes, fills and strokes the current path.
    @discardableResult
    public func closeFillAndStrokePath() -> Stamp {
        append("b\n")
        return self
    }

    /// Draws the outline of a rectangle.
    @discardableResult
    public func drawRect(_ x: Float, _ y: Float, _ w: Float, _ h: Float) -> Stamp {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        closePath()
        return self
    }

    /// Draws a filled rectangle.
    @discardableResult
    public func fillRect(_ x: Float, _ y: Float, _ w: Float, _ h: Float) -> Stamp {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        fillPath()
        return self
    }

    /// Draws text using the font, font size, location and text in the parameters.
    @discardableResult
    public func drawText(_ parameters: TextParameters) -> Stamp {
        guard let font = parameters.font, let text = parameters.text else {
            pdf.fail("Stamp text needs a font and a text.")
            return self
        }

        return drawText(
            font,
            parameters.fontSize,
            parameters.x,
            parameters.y,
            text
        )
    }

    /// Draws text on this stamp with an embedded font, which the stamp adds to its fonts.
    @discardableResult
    public func drawText(
        _ font: Font,
        _ fontSize: Float,
        _ x: Float,
        _ y: Float,
        _ text: String
    ) -> Stamp {
        if font.isCoreFont || font.isCJK {
            pdf.fail("A stamp draws text with an embedded font, not a core or CJK font.")
            return self
        }
        addFont(font)
        append("BT\n")
        append("/F\(font.objNumber)")
        append(" ")
        append(fontSize)
        append(" Tf\n")
        append(x)
        append(" ")
        append(height - y)
        append(" Td\n")
        append("<")
        drawText(font, text)
        append("> Tj\n")
        append("ET\n")
        return self
    }

    /// Rotates this stamp around its center: clockwise for a positive angle, as every rotation in
    /// PDFjet turns, and counterclockwise for a negative angle.
    @discardableResult
    public func setRotation(_ degrees: Float) -> Stamp {
        // The rotation of the page turns counterclockwise.
        self.rotateDegrees = -degrees
        return self
    }

    /// Scales this stamp around its center when it is placed on a page; 1 is its size.
    @discardableResult
    public func scaleBy(_ factor: Float) -> Stamp {
        return scaleBy(factor, factor)
    }

    /// Scales this stamp around its center when it is placed on a page.
    @discardableResult
    public func scaleBy(_ sx: Float, _ sy: Float) -> Stamp {
        self.scaleX = sx
        self.scaleY = sy
        return self
    }

    ///
    /// Writes this stamp to the document as a form XObject.
    /// Call it once, after drawing the content and before drawOn.
    ///
    public func complete() throws {
        if completed {
            let message = "complete() was already called on the stamp."
            pdf.fail(message)
            throw PDFjetError(message: message)
        }
        completed = true
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Form\n")

        pdf.append("/BBox [0 0 ")
        pdf.append(width)
        pdf.append(" ")
        pdf.append(height)
        pdf.append("]\n")

        pdf.append("/Resources <<\n")
        if !fonts.isEmpty {
            pdf.append("/Font <<\n")
            for font in fonts {
                pdf.append("/F\(font.objNumber) \(font.objNumber) 0 R\n")
            }
            pdf.append(">>\n")
        }
        pdf.append(">>\n")

        pdf.append("/Length \(buf.count)\n")
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf)
        pdf.append(Token.endStream)
        pdf.endObj()

        pdf.stamps.append(self)
        objNumber = pdf.getObjNumber()
    }

    private func drawText(_ font: Font, _ str: String) {
        for scalar in str.unicodeScalars {
            let codePoint = Int(scalar.value)
            if codePoint == 0xFEFF { continue } // Skip BOM

            // The glyphs Page draws. The .notdef glyph of a character the font
            // does not have is drawn in a marked content span with the
            // character as its actual text, as on a page.
            let gid = Page.glyphOf(font, codePoint)
            if font.lacks(codePoint) {
                append("> Tj\n/Span <</ActualText <")
                append(Page.toUTF16Hex(Page.textOf(font, codePoint)))
                append(">>> BDC\n<")
                appendCodePointAsHex(gid)
                append("> Tj\nEMC\n<")
            } else {
                appendCodePointAsHex(gid)
            }
        }
    }

    private func append(_ point: Point) {
        append(point.x)
        append(" ")
        append(height - point.y)
        append(" ")
    }

    ///
    /// Draws a path through the points. Control points define Bézier curves.
    /// Fewer than two points paint nothing. Throws an error if the path ends with an unconsumed control point.
    ///
    public func drawPath(_ path: [Point], _ pathOperator: PathOperator) throws {
        guard path.count >= 2 else {
            return // A path needs two points to paint anything.
        }

        var point = path[0]
        moveTo(point.x, point.y)
        var controlPoint: String = ""

        for i in 1..<path.count {
            point = path[i]
            if !point.controlPoint.isEmpty {
                controlPoint = point.controlPoint
                append(point)
            } else {
                if !controlPoint.isEmpty {
                    append(point)
                    append(controlPoint)
                    append("\n")
                    controlPoint = ""
                } else {
                    lineTo(point.x, point.y)
                }
            }
        }
        // Catch unflushed control point
        if !controlPoint.isEmpty {
            throw PDFjetError(message: "Path ends with unconsumed control point(s). " +
                    "Each 'c' requires 2 CPs + 1 endpoint, 'v'/'y' require 1 CP + 1 endpoint.")
        }

        append(pathOperator.rawValue)
        append("\n")
    }

    private func appendCodePointAsHex(_ codePoint: Int) {
        buf.append(Page.HEX[(codePoint >> 12) & 0xF])
        buf.append(Page.HEX[(codePoint >> 8) & 0xF])
        buf.append(Page.HEX[(codePoint >> 4) & 0xF])
        buf.append(Page.HEX[codePoint & 0xF])
    }

    /// Draws this stamp on the specified page and returns the x and y coordinates of its bottom right corner.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [self.x + width, self.y + height]    // Measured, not drawn
        }
        if page.pdf !== pdf {
            page.pdf.fail("The stamp belongs to another PDF.")
            return [self.x + width, self.y + height]
        }
        if !completed {
            page.pdf.fail("Call complete() on the stamp before drawing it.")
            return [self.x + width, self.y + height]
        }
        if width == 0.0 || height == 0.0 || scaleX == 0.0 || scaleY == 0.0 {
            return [self.x + width, self.y + height]    // Nothing to paint.
        }

        page.addBDC(StructElem.P, language, actualText, altDescription)
        page.saveGraphicsState()

        let drawX = self.x
        let drawY = (page.height - self.height) - self.y

        // 5. POSITION: move to desired location
        page.append("1 0 0 1 ")
        page.append(drawX)
        page.append(" ")
        page.append(drawY)
        page.append(" cm\n")

        // 4. MOVE BACK: after rotation
        page.append("1 0 0 1 ")
        page.append(width / 2)
        page.append(" ")
        page.append(height / 2)
        page.append(" cm\n")

        // 3. ROTATE: rotate around origin
        let radians = Double(rotateDegrees) * .pi / 180.0
        let cosine = Float(cos(radians))
        let sine = Float(sin(radians))
        page.append(cosine)
        page.append(" ")
        page.append(sine)
        page.append(" ")
        page.append(-sine)
        page.append(" ")
        page.append(cosine)
        page.append(" 0 0 cm\n")

        // SCALE: around the center, like a Container
        if scaleX != 1.0 || scaleY != 1.0 {
            page.append(scaleX)
            page.append(" 0 0 ")
            page.append(scaleY)
            page.append(" 0 0 cm\n")
        }

        // 2. MOVE: move center to origin
        page.append("1 0 0 1 ")
        page.append(-width / 2)
        page.append(" ")
        page.append(-height / 2)
        page.append(" cm\n")

        // 1. DRAW: draw the object
        page.append("/Fm\(objNumber ?? 0) Do\n")

        page.restoreGraphicsState()
        page.addEMC()

        return [self.x + width, self.y + height]
    }

    private func append(_ value: Float) {
        if !notCompleted() {
            return
        }
        if !FastFloat.isWritable(value) {
            pdf.fail(FastFloat.NOT_WRITABLE)
        }
        self.buf.append(contentsOf: FastFloat.toByteArray(value))
    }

    private func append(_ str: String) {
        if !notCompleted() {
            return
        }
        self.buf.append(contentsOf: str.utf8)
    }

    // The content drawn after complete() would be lost: records the misuse and
    // returns false after complete().
    private func notCompleted() -> Bool {
        if completed {
            pdf.fail("The stamp was already completed.")
            return false
        }
        return true
    }
}
