//
//  Stamp.swift
//
//  Copyright (c) 2026 PDFjet Software
//  Licensed under the MIT License. See LICENSE file in the project root.
//

import Foundation

public class Stamp {
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
    private var buf = [UInt8]()
    private var fonts: [Font] = []

    public init(_ pdf: PDF) {
        self.pdf = pdf
    }

    @discardableResult
    public func withSize(_ width: Float, _ height: Float) -> Stamp {
        self.width = width
        self.height = height
        return self
    }

    @discardableResult
    public func withFont(_ font: Font) -> Stamp {
        fonts.append(font)
        return self
    }

    public func setPosition(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }

    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Stamp {
        self.x = x
        self.y = y
        return self
    }

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

    @discardableResult
    public func setFillColor(_ color: Int) -> Stamp {
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

    @discardableResult
    public func setStrokeColor(_ color: Int) -> Stamp {
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

    @discardableResult
    public func setStrokeWidth(_ width: Float) -> Stamp {
        append(width)
        append(" w\n")
        self.strokeWidth = width
        return self
    }

    @discardableResult
    public func moveTo(_ x: Float, _ y: Float) -> Stamp {
        append(x)
        append(" ")
        append(height - y)
        append(" m\n")
        return self
    }

    @discardableResult
    public func lineTo(_ x: Float, _ y: Float) -> Stamp {
        append(x)
        append(" ")
        append(height - y)
        append(" l\n")
        return self
    }

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

    @discardableResult
    public func strokePath() -> Stamp {
        append("S\n")
        return self
    }

    @discardableResult
    public func closePath() -> Stamp {
        append("s\n")
        return self
    }

    @discardableResult
    public func fillPath() -> Stamp {
        append("f\n")
        return self
    }

    @discardableResult
    public func closeFillAndStrokePath() -> Stamp {
        append("b\n")
        return self
    }

    // TODO: Implement
    @discardableResult
    public func rectangle() -> Stamp {
        return self
    }

    @discardableResult
    public func drawRect(_ x: Float, _ y: Float, _ w: Float, _ h: Float) -> Stamp {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        lineTo(x, y + h)
        closePath()
        return self
    }

    @discardableResult
    public func fillRect(_ x: Float, _ y: Float, _ w: Float, _ h: Float) -> Stamp {
        moveTo(x, y)
        lineTo(x + w, y)
        lineTo(x + w, y + h)
        fillPath()
        return self
    }

    @discardableResult
    public func drawText(_ parameters: TextParameters) -> Stamp {
        return drawText(
            parameters.font!,
            parameters.fontSize,
            parameters.x,
            parameters.y,
            parameters.text!
        )
    }

    @discardableResult
    public func drawText(
        _ font: Font,
        _ fontSize: Float,
        _ x: Float,
        _ y: Float,
        _ text: String
    ) -> Stamp {
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

    @discardableResult
    public func rotate(_ degrees: Float) -> Stamp {
        self.rotateDegrees = degrees
        return self
    }

    @discardableResult
    public func setRotation(_ degrees: Float) -> Stamp {
        self.rotateDegrees = degrees
        return self
    }

    @discardableResult
    public func setRotationClockwise(_ degrees: Float) -> Stamp {
        self.rotateDegrees = -degrees
        return self
    }

    @discardableResult
    public func setRotationCounterClockwise(_ degrees: Float) -> Stamp {
        self.rotateDegrees = degrees
        return self
    }

    public func complete() throws {
        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Form\n")

        pdf.append("/BBox [0 0 ")
        pdf.append(FastFloat.toByteArray(width))
        pdf.append(" ")
        pdf.append(FastFloat.toByteArray(height))
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
        pdf.endobj()

        // pdf.stamps.append(self)  // TODO:
        objNumber = pdf.getObjNumber()
    }

    private func drawText(_ font: Font, _ str: String) {
        // Iterate over Unicode scalars (equivalent to Java's codePoint iteration)
        for scalar in str.unicodeScalars {
            let codePoint = Int(scalar.value)
            // Skip the Byte Order Mark (U+FEFF)
            if codePoint != 0xFEFF {
                let gid: Int
                // Map code point to glyph ID, fall back to space if out of range
                if codePoint < font.firstChar || codePoint > font.lastChar {
                    gid = font.unicodeToGID[0x0020] // Space fallback
                } else {
                    gid = font.unicodeToGID[codePoint]
                }
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

    public func drawPath(_ path: [Point], _ pathOperator: String) throws {
        guard path.count >= 2 else {
            throw NSError(domain: "Stamp", code: 1,
                    userInfo: [NSLocalizedDescriptionKey: "The Path object must contain at least 2 points"])
        }

        var point = path[0]
        moveTo(point.x, point.y)
        var controlPoint: UInt16 = 0

        for i in 1..<path.count {
            point = path[i]
            if point.controlPoint != 0 {
                controlPoint = point.controlPoint
                append(point)
            } else {
                if controlPoint != 0 {
                    append(point)
                    append(controlPoint)
                    append("\n")
                    controlPoint = 0
                } else {
                    lineTo(point.x, point.y)
                }
            }
        }

        append(pathOperator)
        append("\n")
    }

    private func appendCodePointAsHex(_ codePoint: Int) {
        buf.append(Page.HEX[(codePoint >> 12) & 0xF])
        buf.append(Page.HEX[(codePoint >> 8) & 0xF])
        buf.append(Page.HEX[(codePoint >> 4) & 0xF])
        buf.append(Page.HEX[codePoint & 0xF])
    }

    @discardableResult
    public func drawOn(_ page: Page) -> [Float] {
        // Save graphics state
        page.append("q\n")

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
        let cos = Float(cos(radians))
        let sin = Float(sin(radians))
        page.append("\(cos) \(sin) \(-sin) \(cos) 0 0 cm\n")

        // 2. MOVE: move center to origin
        page.append("1 0 0 1 ")
        page.append(-width / 2)
        page.append(" ")
        page.append(-height / 2)
        page.append(" cm\n")

        // 1. DRAW: draw the object
        page.append("/Fm\(objNumber ?? 0) Do\n")

        // Restore graphics state
        page.append("Q\n")

        return [self.x + width, self.y + height]
    }

    private func append(_ value: Float) {
        self.buf.append(contentsOf: FastFloat.toByteArray(value))
    }

    private func append(_ str: String) {
        self.buf.append(contentsOf: str.utf8)
    }

    private func append(_ value: UInt16) {
        self.buf.append(contentsOf: value)
    }
}
