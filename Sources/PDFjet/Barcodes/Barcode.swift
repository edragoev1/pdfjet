/**
 * Barcode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create one dimensional barcodes - EAN-13, UPC-A, Code 39 and Code 128.
///
/// Please see Example_11.
///
public class Barcode : Drawable {
    /// EAN-13 barcode.
    public static let EAN_13 = 0
    /// UPC-A barcode.
    public static let UPC_A = 1
    /// Code 128 barcode.
    public static let CODE_128 = 2
    /// Code 39 barcode.
    public static let CODE_39 = 3

    private var barcodeType = 0
    private var text: String
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var m1: Float = 0.75    // Module length
    private var barHeightFactor: Float = 50.0
    private var direction = Direction.LEFT_TO_RIGHT
    private var font: Font?

    private let lCode = [
        "3211", "2221", "2122", "1411", "1132",
        "1231", "1114", "1312", "1213", "3112"]
    private var gCode = [String]()
    private let lgMap = [
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"]

    private var tableB = [String : String]()

    ///
    /// The constructor.
    ///
    /// - Parameter barcodeType: the type of the barcode.
    /// - Parameter text: the content string of the barcode.
    ///
    public init(
            _ barcodeType: Int,
            _ text: String) {
        self.barcodeType = barcodeType
        self.text = text

        if barcodeType == Barcode.UPC_A && (text.count != 11 || !Barcode.hasOnlyDigits(text)) {
            fatalError("UPC-A barcodes must have exactly 11 digits!")
        } else if barcodeType == Barcode.EAN_13 && (text.count != 12 || !Barcode.hasOnlyDigits(text)) {
            fatalError("EAN-13 barcodes must have exactly 12 digits!")
        }

        for code in lCode {
            gCode.append(String(code.reversed()))
        }

        tableB["*"] = "bWbwBwBwb"
        tableB["-"] = "bWbwbwBwB"
        tableB["$"] = "bWbWbWbwb"
        tableB["%"] = "bwbWbWbWb"
        tableB[" "] = "bWBwbwBwb"
        tableB["."] = "BWbwbwBwb"
        tableB["/"] = "bWbWbwbWb"
        tableB["+"] = "bWbwbWbWb"
        tableB["0"] = "bwbWBwBwb"
        tableB["1"] = "BwbWbwbwB"
        tableB["2"] = "bwBWbwbwB"
        tableB["3"] = "BwBWbwbwb"
        tableB["4"] = "bwbWBwbwB"
        tableB["5"] = "BwbWBwbwb"
        tableB["6"] = "bwBWBwbwb"
        tableB["7"] = "bwbWbwBwB"
        tableB["8"] = "BwbWbwBwb"
        tableB["9"] = "bwBWbwBwb"
        tableB["A"] = "BwbwbWbwB"
        tableB["B"] = "bwBwbWbwB"
        tableB["C"] = "BwBwbWbwb"
        tableB["D"] = "bwbwBWbwB"
        tableB["E"] = "BwbwBWbwb"
        tableB["F"] = "bwBwBWbwb"
        tableB["G"] = "bwbwbWBwB"
        tableB["H"] = "BwbwbWBwb"
        tableB["I"] = "bwBwbWBwb"
        tableB["J"] = "bwbwBWBwb"
        tableB["K"] = "BwbwbwbWB"
        tableB["L"] = "bwBwbwbWB"
        tableB["M"] = "BwBwbwbWb"
        tableB["N"] = "bwbwBwbWB"
        tableB["O"] = "BwbwBwbWb"
        tableB["P"] = "bwBwBwbWb"
        tableB["Q"] = "bwbwbwBWB"
        tableB["R"] = "BwbwbwBWb"
        tableB["S"] = "bwBwbwBWb"
        tableB["T"] = "bwbwBwBWb"
        tableB["U"] = "BWbwbwbwB"
        tableB["V"] = "bWBwbwbwB"
        tableB["W"] = "BWBwbwbwb"
        tableB["X"] = "bWbwBwbwB"
        tableB["Y"] = "BWbwBwbwb"
        tableB["Z"] = "bWBwBwbwb"
    }

    ///
    /// Sets the location where this barcode will be drawn on the page.
    ///
    /// - Parameter x1: the x coordinate of the top left corner of the barcode.
    /// - Parameter y1: the y coordinate of the top left corner of the barcode.
    ///
    @discardableResult
    public func setLocation(_ x1: Float, _ y1: Float) -> Self {
        self.x1 = x1
        self.y1 = y1
        return self
    }

    ///
    /// Sets the module length of this barcode.
    /// The default value is 0.75
    ///
    /// - Parameter moduleLength: the specified module length.
    ///
    @discardableResult
    public func setModuleLength(_ moduleLength: Double) -> Barcode {
        self.m1 = Float(moduleLength)
        return self
    }

    ///
    /// Sets the module length of this barcode.
    /// The default value is 0.75
    ///
    /// - Parameter moduleLength: the specified module length.
    ///
    @discardableResult
    public func setModuleLength(_ moduleLength: Float) -> Barcode {
        self.m1 = moduleLength
        return self
    }

    ///
    /// Sets the bar height factor.
    /// The height of the bars is the moduleLength * barHeightFactor
    /// The default value is 50.0
    ///
    /// - Parameter barHeightFactor: the specified bar height factor.
    ///
    @discardableResult
    public func setBarHeightFactor(_ barHeightFactor: Double) -> Barcode {
        self.barHeightFactor = Float(barHeightFactor)
        return self
    }

    ///
    /// Sets the bar height factor.
    /// The height of the bars is the moduleLength * barHeightFactor
    /// The default value is 50.0f
    ///
    /// - Parameter barHeightFactor: the specified bar height factor.
    ///
    @discardableResult
    public func setBarHeightFactor(_ barHeightFactor: Float) -> Barcode {
        self.barHeightFactor = barHeightFactor
        return self
    }

    ///
    /// Sets the direction in which this barcode is drawn.
    ///
    /// - Parameter direction: the specified direction.
    ///
    @discardableResult
    public func setDirection(_ direction: Direction) -> Barcode {
        self.direction = direction
        return self
    }

    ///
    /// Sets the font to be used with this barcode.
    ///
    /// - Parameter font: the specified font.
    ///
    @discardableResult
    public func setFont(_ font: Font) -> Barcode {
        self.font = font
        return self
    }

    private static func hasOnlyDigits(_ text: String) -> Bool {
        for ch in text.unicodeScalars {
            if ch < "0" || ch > "9" {
                return false
            }
        }
        return true
    }

    ///
    /// Draws this barcode on the specified page.
    ///
    /// - Parameter page: the specified page.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if barcodeType == Barcode.EAN_13 {
            return drawCodeEAN13(page, x1, y1)
        } else if barcodeType == Barcode.UPC_A {
            return drawCodeUPC(page, x1, y1)
        } else if barcodeType == Barcode.CODE_128 {
            return drawCode128(page, x1, y1)
        } else if barcodeType == Barcode.CODE_39 {
            return drawCode39(page, x1, y1)
        } else {
            fatalError("Unsupported Barcode Type.")
        }
    }

    @discardableResult
    func drawOnPageAtLocation(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        if barcodeType == Barcode.EAN_13 {
            return drawCodeEAN13(page, x1, y1)
        } else if (barcodeType == Barcode.UPC_A) {
            return drawCodeUPC(page, x1, y1)
        } else if (barcodeType == Barcode.CODE_128) {
            return drawCode128(page, x1, y1)
        } else if (barcodeType == Barcode.CODE_39) {
            return drawCode39(page, x1, y1)
        } else {
            fatalError("Unsupported Barcode Type.")
        }
    }

    private func drawCodeUPC(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        var x: Float = x1
        let h: Float = m1 * barHeightFactor     // Barcode height when drawn horizontally

        // Calculate the check digit:
        // 1. Add the digits in the odd-numbered positions (first, third, fifth, etc.)
        // together and multiply by three.
        // 2. Add the digits in the even-numbered positions (second, fourth, sixth, etc.)
        // to the result.
        // 3. Subtract the result modulo 10 from ten.
        // 4. The answer modulo 10 is the check digit.
        let scalars = Array(text.unicodeScalars)
        var sum = 0
        var i = 0
        while i < 11 {
            sum += Int(scalars[i].value) - 48
            i += 2
        }
        sum *= 3
        i = 1
        while i < 11 {
            sum += Int(scalars[i].value) - 48
            i += 2
        }
        let reminder = sum % 10
        let checkDigit = UInt16((10 - reminder) % 10)
        // fullScalars is a local copy - drawOn() must be safe to call more
        // than once on the same Barcode instance (e.g. drawing the same
        // barcode on several pages).
        // The check digit must be appended as its ASCII character (0x30 +
        // digit), matching how the rest of the digits are represented -
        // not as a raw scalar value 0-9 (which are unprintable control
        // characters and would corrupt any indexing done against them).
        var fullScalars = scalars
        fullScalars.append(UnicodeScalar(checkDigit + 0x30)!)
        let bars = Bars(x1: x1, y1: y1, length: 95.0 * m1, height: h + 8.0, direction: direction)  // 95 modules

        x = drawEGuard(page, bars, x, h + 8)
        var xGroup1Start = x

        i = 0
        while i < 6 {
            let digit = Int(fullScalars[i].value) - 0x30
            let symbols = Array(lCode[digit].unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 != 0 {
                    drawBar(page, bars, x, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            if i == 0 {
                xGroup1Start = x   // Start of the 2nd-6th digit bars (digit 0 is drawn outside)
            }
            i += 1
        }
        let xLeftGroupEnd = x
        x = drawMGuard(page, bars, x, h + 8)
        let xRightGroupStart = x
        var xGroup2End: Float = 0.0

        i = 6
        while i < 12 {
            if i == 11 {
                xGroup2End = x     // End of the 7th-11th digit bars (digit 11 is drawn outside)
            }
            let digit = Int(fullScalars[i].value) - 0x30
            let symbols = Array(lCode[digit].unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 == 0 {
                    drawBar(page, bars, x, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        x = drawEGuard(page, bars, x, h + 8)

        var left = x1
        var right = x
        var bottom = y1 + h + 8
        if font != nil {
            // Standard UPC-A layout: the leading (number system) digit and
            // the trailing check digit are printed in the quiet zones
            // outside the guard bars, not centered under them together with
            // the rest of the label. The two groups of 5 digits are each
            // centered under their own bar section.
            let firstDigit = String(fullScalars[0])
            var group1 = ""
            for k in 1..<6 {
                group1 += String(fullScalars[k])
            }
            var group2 = ""
            for k in 6..<11 {
                group2 += String(fullScalars[k])
            }
            let lastDigit = String(fullScalars[11])

            let fontSize = font!.getSize()
            font!.setSize(10.0)
            let yText = y1 + h + font!.getBodyHeight(font!.getSize())
            let gap = font!.stringWidth(font!.getSize(), " ")

            left = x1 - gap - font!.stringWidth(font!.getSize(), firstDigit)
            drawText(page, bars, firstDigit, left, yText)
            drawText(page, bars, group1,
                    xGroup1Start + ((xLeftGroupEnd - xGroup1Start) - font!.stringWidth(font!.getSize(), group1))/2,
                    yText)
            drawText(page, bars, group2,
                    xRightGroupStart + ((xGroup2End - xRightGroupStart) - font!.stringWidth(font!.getSize(), group2))/2,
                    yText)
            let xy = drawText(page, bars, lastDigit, x + gap, yText)
            right = xy[0]
            bottom = max(bottom, xy[1])

            font!.setSize(fontSize)
        }

        return bars.getBottomRight(left, right, bottom)
    }

    private func drawEGuard(
            _ page: Page?,
            _ bars: Bars,
            _ x: Float,
            _ h: Float) -> Float {
        if page != nil {
            // 101
            page!.addArtifactBMC()
            strokeBar(page, bars, x + (0.5 * m1), m1, h)
            strokeBar(page, bars, x + (2.5 * m1), m1, h)
            page!.addEMC()
        }
        return (x + (3.0 * m1))
    }

    private func drawMGuard(
            _ page: Page?,
            _ bars: Bars,
            _ x: Float,
            _ h: Float) -> Float {
        if page != nil {
            // 01010
            page!.addArtifactBMC()
            strokeBar(page, bars, x + (1.5 * m1), m1, h)
            strokeBar(page, bars, x + (3.5 * m1), m1, h)
            page!.addEMC()
        }
        return (x + (5.0 * m1))
    }

    // Draws the bar of width w and height h that starts at x.
    private func drawBar(
            _ page: Page?,
            _ bars: Bars,
            _ x: Float,
            _ w: Float,
            _ h: Float) {
        if page != nil {
            page!.addArtifactBMC()
            strokeBar(page, bars, x + w/2, w, h)
            page!.addEMC()
        }
    }

    // Strokes the bar of width w and height h centered on x.
    private func strokeBar(
            _ page: Page?,
            _ bars: Bars,
            _ x: Float,
            _ w: Float,
            _ h: Float) {
        if page != nil {
            let top = bars.turn(x, bars.y1)
            let bottom = bars.turn(x, bars.y1 + h)
            page!.setPenWidth(w)
            page!.moveTo(top[0], top[1])
            page!.lineTo(bottom[0], bottom[1])
            page!.strokePath()
        }
    }

    // Draws the text with its baseline starting at (x, y). Returns the end of
    // the baseline and the bottom of the text, before the turn.
    @discardableResult
    private func drawText(
            _ page: Page?,
            _ bars: Bars,
            _ str: String,
            _ x: Float,
            _ y: Float) -> [Float] {
        let textLine = TextLine(font!, str)
        let xy = bars.turn(x, y)
        textLine.setLocation(xy[0], xy[1])
        if direction == Direction.TOP_TO_BOTTOM {
            textLine.setTextRotation(270)
        } else if direction == Direction.BOTTOM_TO_TOP {
            textLine.setTextRotation(90)
        }
        textLine.drawOn(page)
        return [x + font!.stringWidth(font!.getSize(), str), y + font!.getDescent(font!.getSize())]
    }

    private func drawCode128(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        let h: Float = m1 * barHeightFactor     // Barcode height when drawn horizontally

        var list = [UInt16]()
        for symchar in text.unicodeScalars {
            // Some characters need two codewords (SHIFT/FNC_4 + value), so
            // checking list.count == 48 only *after* adding them could skip
            // right over 48 (e.g. 47 -> 49) and never trip again, silently
            // encoding an unbounded number of characters past the documented
            // limit. Check before adding instead, so the cap always holds.
            let codewordsNeeded = (symchar.value < 32 || (symchar.value >= 128 && symchar.value < 256)) ? 2 : 1
            if list.count + codewordsNeeded > 48 {
                // Maximum number of data characters is 48
                break
            }
            if symchar.value < 32 {
                list.append(UInt16(GS1_128.SHIFT))
                list.append(UInt16(symchar.value + 64))
            } else if symchar.value < 128 {
                list.append(UInt16(symchar.value - 32))
            } else if symchar.value < 256 {
                list.append(UInt16(GS1_128.FNC_4))
                list.append(UInt16(symchar.value - 160))    // 128 + 32
            } else {
                list.append(UInt16(256))                    // This will generate an exception.
            }
        }

        var buf = String()
        var checkDigit = GS1_128.START_B
        buf.append(String(UnicodeScalar(checkDigit)!))
        for i in 0..<list.count {
            let codeword = list[i]
            buf.append(String(UnicodeScalar(codeword)!))
            checkDigit += Int(codeword) * Int(i + 1)
        }
        checkDigit %= GS1_128.START_A
        buf.append(String(UnicodeScalar(checkDigit)!))
        buf.append(String(UnicodeScalar(GS1_128.STOP)!))

        let scalars = [UnicodeScalar](buf.unicodeScalars)
        var length: Float = 0.0
        for scalar in scalars {
            for symbolScalar in String(GS1_128.TABLE[Int(scalar.value)]).unicodeScalars {
                length += Float(Int(symbolScalar.value) - 0x30) * m1
            }
        }

        let bars = Bars(x1: x1, y1: y1, length: length, height: h, direction: direction)
        var x: Float = x1
        for scalar in scalars {
            let symbol = String(GS1_128.TABLE[Int(scalar.value)])
            var j = 0
            for scalar in symbol.unicodeScalars {
                let n = Int(scalar.value) - 0x30
                if j%2 == 0 {
                    drawBar(page, bars, x, m1 * Float(n), h)
                }
                x += Float(n) * m1
                j += 1
            }
        }

        var right = x
        var bottom = y1 + h
        if font != nil {
            let xy = drawText(page, bars, text,
                    x1 + ((x - x1) - font!.stringWidth(text))/2,
                    y1 + h + font!.bodyHeight)
            right = max(right, xy[0])
            bottom = xy[1]
        }

        return bars.getBottomRight(x1, right, bottom)
    }

    private func drawCode39(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        // Use a local variable instead of mutating the text field - drawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        let fullText = "*" + text + "*"
        let h: Float = m1 * barHeightFactor     // Barcode height when drawn horizontally

        var length: Float = 0.0
        for symchar in fullText.unicodeScalars {
            guard let code = tableB[String(symchar)] else {
                fatalError("The input string '" + fullText +
                        "' contains characters that are invalid in a Code39 barcode.")
            }
            for ch in code.unicodeScalars {
                length += (ch == "W" || ch == "B") ? 3 * m1 : m1
            }
            length += m1
        }
        length -= m1    // There is no gap after the last character

        let bars = Bars(x1: x1, y1: y1, length: length, height: h, direction: direction)
        var x: Float = x1
        for symchar in fullText.unicodeScalars {
            let scalars = Array(tableB[String(symchar)]!.unicodeScalars)
            for i in 0..<9 {
                let ch = String(scalars[i])
                if ch == "w" {
                    x += m1
                } else if ch == "W" {
                    x += m1 * 3
                } else if ch == "b" {
                    drawBar(page, bars, x, m1, h)
                    x += m1
                } else if ch == "B" {
                    drawBar(page, bars, x, m1 * 3, h)
                    x += m1 * 3
                }
            }
            x += m1
        }

        var right = x1 + length
        var bottom = y1 + h
        if font != nil {
            let xy = drawText(page, bars, fullText,
                    x1 + (length - font!.stringWidth(fullText))/2,
                    y1 + h + font!.bodyHeight)
            right = max(right, xy[0])
            bottom = xy[1]
        }

        return bars.getBottomRight(x1, right, bottom)
    }

    private func drawCodeEAN13(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        var x: Float = x1
        let h: Float = m1 * barHeightFactor     // Barcode height when drawn horizontally

        let scalars = Array(text.unicodeScalars)
        var sum = 0
        var i = 0
        while i < 12 {
            sum += Int(scalars[i].value) - 0x30
            i += 2
        }
        i = 1
        while i < 12 {
            sum += (Int(scalars[i].value) - 0x30) * 3
            i += 2
        }
        var checkDigit = 0
        let remainder = sum % 10
        if remainder > 0 {
            checkDigit = 10 - remainder
        }
        // fullScalars is a local copy - drawOn() must be safe to call more
        // than once on the same Barcode instance (e.g. drawing the same
        // barcode on several pages).
        // The check digit must be appended as its ASCII character (0x30 +
        // digit), matching how the rest of the digits are represented -
        // not as a raw scalar value 0-9 (which are unprintable control
        // characters and would corrupt any indexing done against them).
        var fullScalars = scalars
        fullScalars.append(UnicodeScalar(UInt16(checkDigit) + 0x30)!)
        let bars = Bars(x1: x1, y1: y1, length: 95.0 * m1, height: h + 8.0, direction: direction)  // 95 modules

        x = drawEGuard(page, bars, x, h + 8)
        let xLeftGroupStart = x
        let group1 = Array(lgMap[Int(fullScalars[0].value) - 0x30].unicodeScalars)

        i = 1
        while i < 7 {
            let digit = Int(fullScalars[i].value) - 0x30
            var str = gCode[digit]
            if group1[i - 1] == "L" {
                str = lCode[digit]
            }
            let symbols = Array(str.unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 != 0 {
                    drawBar(page, bars, x, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        let xLeftGroupEnd = x
        x = drawMGuard(page, bars, x, h + 8)
        let xRightGroupStart = x

        i = 7
        while i < 13 {
            let digit = Int(fullScalars[i].value) - 0x30
            let symbols = Array(lCode[digit].unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 == 0 {
                    drawBar(page, bars, x, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        let xRightGroupEnd = x
        x = drawEGuard(page, bars, x, h + 8)

        var left = x1
        var right = x
        var bottom = y1 + h + 8
        if font != nil {
            // Standard EAN-13 layout: the leading (number system) digit sits
            // in the quiet zone to the left of the start guard bars, not
            // centered under them together with the rest of the label. The
            // two groups of 6 digits are each centered under their own bar
            // section (left group / right group), not under the barcode as
            // a whole.
            let firstDigit = String(fullScalars[0])
            var leftGroup = ""
            for k in 1..<7 {
                leftGroup += String(fullScalars[k])
            }
            var rightGroup = ""
            for k in 7..<13 {
                rightGroup += String(fullScalars[k])
            }

            let fontSize = font!.getSize()
            font!.setSize(10.0)
            let yText = y1 + h + font!.getBodyHeight(font!.getSize())
            let gap = font!.stringWidth(font!.getSize(), " ")

            left = x1 - gap - font!.stringWidth(font!.getSize(), firstDigit)
            drawText(page, bars, firstDigit, left, yText)
            drawText(page, bars, leftGroup,
                    xLeftGroupStart + ((xLeftGroupEnd - xLeftGroupStart) - font!.stringWidth(font!.getSize(), leftGroup))/2,
                    yText)
            let xy = drawText(page, bars, rightGroup,
                    xRightGroupStart + ((xRightGroupEnd - xRightGroupStart) - font!.stringWidth(font!.getSize(), rightGroup))/2,
                    yText)
            right = max(right, xy[0])
            bottom = max(bottom, xy[1])

            font!.setSize(fontSize)
        }

        return bars.getBottomRight(left, right, bottom)
    }

    // The bars of a barcode, length long and height high, drawn left to right from
    // (x1, y1) and turned to the direction of the barcode: top to bottom is a quarter
    // turn clockwise and bottom to top a quarter turn counter-clockwise, and the bars
    // stay right of x1 and below y1. The draw methods take the coordinates of the
    // barcode drawn left to right.
    private struct Bars {
        let x1: Float
        let y1: Float
        let length: Float
        let height: Float
        let direction: Direction

        // Returns the point (x, y) turned to the direction of the barcode.
        func turn(_ x: Float, _ y: Float) -> [Float] {
            if direction == Direction.TOP_TO_BOTTOM {
                return [x1 + height - (y - y1), y1 + (x - x1)]
            } else if direction == Direction.BOTTOM_TO_TOP {
                return [x1 + (y - y1), y1 + length - (x - x1)]
            }
            return [x, y]
        }

        // Returns the bottom right corner, turned to the direction of the barcode, of
        // a barcode that spans from left to right and from y1 to bottom.
        func getBottomRight(_ left: Float, _ right: Float, _ bottom: Float) -> [Float] {
            if direction == Direction.TOP_TO_BOTTOM {
                return [x1 + height, y1 + (right - x1)]
            } else if direction == Direction.BOTTOM_TO_TOP {
                return [x1 + (bottom - y1), y1 + length + (x1 - left)]
            }
            return [right, bottom]
        }
    }

    /// Returns the height of this barcode.
    public func getHeight() -> Float {
        if font == nil {
            return m1 * barHeightFactor
        }
        return m1 * barHeightFactor + font!.getBodyHeight()
    }
}   // End of Barcode.swift
