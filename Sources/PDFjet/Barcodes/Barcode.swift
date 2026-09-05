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
    public static let EAN_13 = 0
    public static let UPC_A = 1
    public static let CODE_128 = 2
    public static let CODE_39 = 3

    public static let LEFT_TO_RIGHT = 0
    public static let TOP_TO_BOTTOM = 1
    public static let BOTTOM_TO_TOP = 2

    private var barcodeType = 0
    private var text: String
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var m1: Float = 0.75    // Module length
    private var barHeightFactor: Float = 50.0
    private var direction = LEFT_TO_RIGHT
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
    /// @param barcodeType the type of the barcode.
    /// @param text the content string of the barcode.
    ///
    public init(
            _ barcodeType: Int,
            _ text: String) {
        self.barcodeType = barcodeType
        self.text = text

        if barcodeType == Barcode.UPC_A && text.count > 11 {
            fatalError("UPC-A barcodes can have maximum of 11 digits!")
        } else if barcodeType == Barcode.EAN_13 && text.count > 12 {
            fatalError("EAN-13 barcodes can have maximum of 12 digits!")
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

    public func setPosition(_ x1: Float, _ y1: Float) {
        setLocation(x1, y1)
    }

    ///
    /// Sets the location where this barcode will be drawn on the page.
    ///
    /// @param x1 the x coordinate of the top left corner of the barcode.
    /// @param y1 the y coordinate of the top left corner of the barcode.
    ///
    public func setLocation(_ x1: Float, _ y1: Float) {
        self.x1 = x1
        self.y1 = y1
    }

    ///
    /// Sets the module length of this barcode.
    /// The default value is 0.75
    ///
    /// @param moduleLength the specified module length.
    ///
    public func setModuleLength(_ moduleLength: Double) {
        self.m1 = Float(moduleLength)
    }

    ///
    /// Sets the module length of this barcode.
    /// The default value is 0.75
    ///
    /// @param moduleLength the specified module length.
    ///
    public func setModuleLength(_ moduleLength: Float) {
        self.m1 = moduleLength
    }

    ///
    /// Sets the bar height factor.
    /// The height of the bars is the moduleLength * barHeightFactor
    /// The default value is 50.0
    ///
    /// @param barHeightFactor the specified bar height factor.
    ///
    public func setBarHeightFactor(_ barHeightFactor: Double) {
        self.barHeightFactor = Float(barHeightFactor)
    }

    ///
    /// Sets the bar height factor.
    /// The height of the bars is the moduleLength * barHeightFactor
    /// The default value is 50.0f
    ///
    /// @param barHeightFactor the specified bar height factor.
    ///
    public func setBarHeightFactor(_ barHeightFactor: Float) {
        self.barHeightFactor = barHeightFactor
    }

    ///
    /// Sets the drawing direction for this font.
    ///
    /// @param direction the specified direction.
    ///
    public func setDirection(_ direction: Int) {
        self.direction = direction
    }

    ///
    /// Sets the font to be used with this barcode.
    ///
    /// @param font the specified font.
    ///
    public func setFont(_ font: Font) {
        self.font = font
    }

    ///
    /// Draws this barcode on the specified page.
    ///
    /// @param page the specified page.
    /// @return x and y coordinates of the bottom right corner of this component.
    /// @throws Exception
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
            Swift.print("Unsupported Barcode Type.")
        }
        return [Float]()
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
            Swift.print("Unsupported Barcode Type.")
        }
        return [Float]()
    }

    private func drawCodeUPC(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        var x: Float = x1
        let y: Float = y1
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

        x = drawEGuard(page, x, y, m1, h + 8)
        var xGroup1Start = x

        i = 0
        while i < 6 {
            let digit = Int(fullScalars[i].value) - 0x30
            let symbols = Array(lCode[digit].unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 != 0 {
                    drawVertBar(page, x, y, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            if i == 0 {
                xGroup1Start = x   // Start of the 2nd-6th digit bars (digit 0 is drawn outside)
            }
            i += 1
        }
        let xLeftGroupEnd = x
        x = drawMGuard(page, x, y, m1, h + 8)
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
                    drawVertBar(page, x, y, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        x = drawEGuard(page, x, y, m1, h + 8)

        var xy = [x, y]
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

            let firstDigitLine = TextLine(font!, firstDigit)
                    .setLocation(x1 - gap - font!.stringWidth(font!.getSize(), firstDigit), yText)
            firstDigitLine.drawOn(page)

            let group1Line = TextLine(font!, group1)
                    .setLocation(
                            xGroup1Start + ((xLeftGroupEnd - xGroup1Start) - font!.stringWidth(font!.getSize(), group1))/2,
                            yText)
            group1Line.drawOn(page)

            let group2Line = TextLine(font!, group2)
                    .setLocation(
                            xRightGroupStart + ((xGroup2End - xRightGroupStart) - font!.stringWidth(font!.getSize(), group2))/2,
                            yText)
            group2Line.drawOn(page)

            let lastDigitLine = TextLine(font!, lastDigit)
                    .setLocation(x + gap, yText)
            let xyLast = lastDigitLine.drawOn(page)

            xy[0] = max(x, xyLast[0])
            xy[1] = max(y, xyLast[1])

            font!.setSize(fontSize)
            return [xy[0], xy[1] + font!.getDescent(font!.getSize())]
        }

        return [xy[0], xy[1]]
    }

    private func drawEGuard(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,
            _ h: Float) -> Float {
        if page != nil {
            // 101
            page!.addArtifactBMC()
            drawBar(page, x + (0.5 * m1), y, m1, h)
            drawBar(page, x + (2.5 * m1), y, m1, h)
            page!.addEMC()
        }
        return (x + (3.0 * m1))
    }

    private func drawMGuard(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,
            _ h: Float) -> Float {
        if page != nil {
            // 01010
            page!.addArtifactBMC()
            drawBar(page, x + (1.5 * m1), y, m1, h)
            drawBar(page, x + (3.5 * m1), y, m1, h)
            page!.addEMC()
        }
        return (x + (5.0 * m1))
    }

    private func drawBar(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,  // Single bar width
            _ h: Float) {
        if page != nil {
            page!.setPenWidth(m1)
            page!.moveTo(x, y)
            page!.lineTo(x, y + h)
            page!.strokePath()
        }
    }

    private func drawCode128(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        var x: Float = x1
        var y: Float = y1

        var w: Float = m1
        var h: Float = m1

        if direction == Barcode.TOP_TO_BOTTOM {
            w *= barHeightFactor
        } else if direction == Barcode.LEFT_TO_RIGHT {
            h *= barHeightFactor
        }

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
                // list.append(UInt16(31))                  // '?'
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
        for scalar in scalars {
            let symbol = String(GS1_128.TABLE[Int(scalar.value)])
            var j = 0
            for scalar in symbol.unicodeScalars {
                let n = Int(scalar.value) - 0x30
                if j%2 == 0 {
                    if direction == Barcode.LEFT_TO_RIGHT {
                        drawVertBar(page, x, y, m1 * Float(n), h)
                    } else if direction == Barcode.TOP_TO_BOTTOM {
                        drawHorzBar(page, x, y, m1 * Float(n), w)
                    }
                }
                if direction == Barcode.LEFT_TO_RIGHT {
                    x += Float(n) * m1
                } else if direction == Barcode.TOP_TO_BOTTOM {
                    y += Float(n) * m1
                }
                j += 1
            }
        }

        var xy = [x, y]
        if font != nil {
            if direction == Barcode.LEFT_TO_RIGHT {
                let textLine = TextLine(font!, text)
                        .setLocation(x1 + ((x - x1) - font!.stringWidth(text))/2, y1 + h + font!.bodyHeight)
                xy = textLine.drawOn(page)
                xy[0] = max(x, xy[0])
                return [xy[0], xy[1] + font!.descent]
            } else if direction == Barcode.TOP_TO_BOTTOM {
                let textLine = TextLine(font!, text)
                        .setLocation(
                                x + w + font!.bodyHeight,
                                y - ((y - y1) - font!.stringWidth(text))/2)
                        .setTextDirection(90)
                xy = textLine.drawOn(page)
                xy[1] = max(y, xy[1])
            }
        }

        return xy
    }

    private func drawCode39(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        // Use a local variable instead of mutating the text field - drawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        let fullText = "*" + text + "*"

        var x: Float = x1
        var y: Float = y1
        let w: Float = m1 * barHeightFactor     // Barcode width when drawn vertically
        let h: Float = m1 * barHeightFactor     // Barcode height when drawn horizontally

        var xy: [Float] = [0.0, 0.0]

        if direction == Barcode.LEFT_TO_RIGHT {
            for symchar in fullText.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + fullText +
                            "' contains characters that are invalid in a Code39 barcode.")
                } else {
                    let scalars = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalars[i])
                        if ch == "w" {
                            x += m1
                        } else if ch == "W" {
                            x += m1 * 3
                        } else if ch == "b" {
                            drawVertBar(page, x, y, m1, h)
                            x += m1
                        } else if ch == "B" {
                            drawVertBar(page, x, y, m1 * 3, h)
                            x += m1 * 3
                        }
                    }
                    x += m1
                }
            }

            if font != nil {
                let textLine = TextLine(font!, fullText)
                        .setLocation(
                                x1 + ((x - x1) - font!.stringWidth(fullText))/2,
                                y1 + h + font!.bodyHeight)
                xy = textLine.drawOn(page)
                xy[0] = max(x, xy[0])
            }
        } else if direction == Barcode.TOP_TO_BOTTOM {
            for symchar in fullText.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + fullText +
                            "' contains characters that are invalid in a Code39 barcode.")
                } else {
                    let scalars = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalars[i])
                        if ch == "w" {
                            y += m1
                        } else if ch == "W" {
                            y += 3 * m1
                        } else if ch == "b" {
                            drawHorzBar(page, x, y, m1, h)
                            y += m1
                        } else if ch == "B" {
                            drawHorzBar(page, x, y, 3 * m1, h)
                            y += 3 * m1
                        }
                    }
                    y += m1
                }
            }

            if font != nil {
                let textLine = TextLine(font!, fullText)
                        .setLocation(
                                x - font!.bodyHeight,
                                y1 + ((y - y1) - font!.stringWidth(fullText))/2)
                        .setTextDirection(270)
                xy = textLine.drawOn(page)
                xy[0] = max(x, xy[0]) + w
                xy[1] = max(y, xy[1])
            }
        } else if direction == Barcode.BOTTOM_TO_TOP {
            var height: Float = 0.0

            for symchar in fullText.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + fullText +
                            "' contains characters that are invalid in a Code39 barcode.")
                } else {
                    let scalar = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalar[i])
                        if ch == "w" || ch == "b" {
                            height += m1
                        } else if ch == "W" || ch == "B" {
                            height += 3 * m1
                        }
                    }
                    height += m1
                }
            }

            y += height - m1

            for symchar in fullText.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + fullText +
                            "' contains characters that are invalid in a Code39 barcode.")
                } else {
                    let scalars = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalars[i])
                        if ch == "w" {
                            y -= m1
                        } else if ch == "W" {
                            y -= 3 * m1
                        } else if ch == "b" {
                            drawHorzBar2(page, x, y, m1, h)
                            y -= m1
                        } else if ch == "B" {
                            drawHorzBar2(page, x, y, 3 * m1, h)
                            y -= 3 * m1
                        }
                    }
                    y -= m1
                }
            }

            if font != nil {
                y = y1 + ( height - m1)

                let textLine = TextLine(font!, fullText)
                        .setLocation(
                                x + w + font!.bodyHeight,
                                y - ((y - y1) - font!.stringWidth(fullText))/2)
                        .setTextDirection(90)
                xy = textLine.drawOn(page)
                xy[1] = max(y, xy[1])
                return [xy[0], xy[1] + font!.descent]
            }
        }

        return [xy[0], xy[1]]
    }

    private func drawCodeEAN13(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        var x: Float = x1
        let y: Float = y1
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

        x = drawEGuard(page, x, y, m1, h + 8)
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
                    drawVertBar(page, x, y, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        let xLeftGroupEnd = x
        x = drawMGuard(page, x, y, m1, h + 8)
        let xRightGroupStart = x

        i = 7
        while i < 13 {
            let digit = Int(fullScalars[i].value) - 0x30
            let symbols = Array(lCode[digit].unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 == 0 {
                    drawVertBar(page, x, y, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        let xRightGroupEnd = x
        x = drawEGuard(page, x, y, m1, h + 8)

        var xy = [x, y]
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

            let firstDigitLine = TextLine(font!, firstDigit)
                    .setLocation(x1 - gap - font!.stringWidth(font!.getSize(), firstDigit), yText)
            firstDigitLine.drawOn(page)

            let leftGroupLine = TextLine(font!, leftGroup)
                    .setLocation(
                            xLeftGroupStart + ((xLeftGroupEnd - xLeftGroupStart) - font!.stringWidth(font!.getSize(), leftGroup))/2,
                            yText)
            leftGroupLine.drawOn(page)

            let rightGroupLine = TextLine(font!, rightGroup)
                    .setLocation(
                            xRightGroupStart + ((xRightGroupEnd - xRightGroupStart) - font!.stringWidth(font!.getSize(), rightGroup))/2,
                            yText)
            let xyRight = rightGroupLine.drawOn(page)

            xy[0] = max(x, xyRight[0])
            xy[1] = max(y, xyRight[1])

            font!.setSize(fontSize)
            return [xy[0], xy[1] + font!.getDescent(font!.getSize())]
        }

        return [xy[0], xy[1]]
    }

    private func drawVertBar(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,    // Module length
            _ h: Float) {
        if page != nil {
            page!.addArtifactBMC()
            page!.setPenWidth(m1)
            page!.moveTo(x + m1/2, y)
            page!.lineTo(x + m1/2, y + h)
            page!.strokePath()
            page!.addEMC()
        }
    }

    private func drawHorzBar(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,    // Module length
            _ w: Float) {
        if page != nil {
            page!.addArtifactBMC()
            page!.setPenWidth(m1)
            page!.moveTo(x, y + m1/2)
            page!.lineTo(x + w, y + m1/2)
            page!.strokePath()
            page!.addEMC()
        }
    }

    private func drawHorzBar2(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,    // Module length
            _ w: Float) {
        if page != nil {
            page!.addArtifactBMC()
            page!.setPenWidth(m1)
            page!.moveTo(x, y - m1/2)
            page!.lineTo(x + w, y - m1/2)
            page!.strokePath()
            page!.addEMC()
        }
    }

    public func getHeight() -> Float {
        if font == nil {
            return m1 * barHeightFactor
        }
        return m1 * barHeightFactor + font!.getBodyHeight()
    }
}   // End of Barcode.swift
