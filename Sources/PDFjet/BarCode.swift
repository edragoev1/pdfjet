/**
 *  BarCode.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
import Foundation


///
/// Used to create one dimentional barcodes - UPC, Code 39 and Code 128.
///
/// Please see Example_11.
///
public class BarCode : Drawable {

    public static let UPC = 0
    public static let CODE128 = 1
    public static let CODE39 = 2

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

    private let tableA = [3211,2221,2122,1411,1132,1231,1114,1312,1213,3112]
    private var tableB = [String : String]()

    ///
    /// The constructor.
    ///
    /// @param type the type of the barcode.
    /// @param text the content string of the barcode.
    ///
    public init(
            _ barcodeType: Int,
            _ text: String) {
        self.barcodeType = barcodeType
        self.text = text

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
        if barcodeType == BarCode.UPC {
            return drawCodeUPC(page, x1, y1)
        }
        else if barcodeType == BarCode.CODE128 {
            return drawCode128(page, x1, y1)
        }
        else if barcodeType == BarCode.CODE39 {
            return drawCode39(page, x1, y1)
        }
        else {
            Swift.print("Unsupported Barcode Type.")
        }
        return [Float]()
    }

    @discardableResult
    func drawOnPageAtLocation(_ page: Page?, _ x1: Float, _ y1: Float) -> [Float] {
        if (barcodeType == BarCode.UPC) {
            return drawCodeUPC(page, x1, y1)
        }
        else if (barcodeType == BarCode.CODE128) {
            return drawCode128(page, x1, y1)
        }
        else if (barcodeType == BarCode.CODE39) {
            return drawCode39(page, x1, y1)
        }
        else {
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

        var scalars = Array(text.unicodeScalars)
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
        scalars.append(UnicodeScalar(checkDigit)!)

        x = drawEGuard(page, x, y, m1, h + 8)

        i = 0
        while i < 6 {
            let digit = Int(scalars[i].value) - 0x30
            let symbols = Array(String(tableA[digit]).unicodeScalars)
            for j in 0..<symbols.count {
                let n = symbols[j].value - 0x30
                if j%2 != 0 {
                    drawVertBar(page, x, y, Float(n)*m1, h)
                }
                x += Float(n)*m1
            }
            i += 1
        }
        x = drawMGuard(page, x, y, m1, h + 8)

        i = 6
        while i < 12 {
            let digit = Int(scalars[i].value) - 0x30
            let symbols = Array(String(tableA[digit]).unicodeScalars)
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
            var label = String(scalars[0])
            label += "  "
            label += String(scalars[1])
            label += String(scalars[2])
            label += String(scalars[3])
            label += String(scalars[4])
            label += String(scalars[5])
            label += "   "
            label += String(scalars[6])
            label += String(scalars[7])
            label += String(scalars[8])
            label += String(scalars[9])
            label += String(scalars[10])
            label += "  "
            label += String(scalars[11])

            let fontSize = font!.getSize()
            font!.setSize(10.0)

            let text = TextLine(font!, label)
                    .setLocation(
                            x1 + ((x - x1) - font!.stringWidth(label))/2,
                            y1 + h + font!.bodyHeight)
            xy = text.drawOn(page)
            xy[0] = max(x, xy[0])
            xy[1] = max(y, xy[1])

            font!.setSize(fontSize)
            return [xy[0], xy[1] + font!.descent]
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
            drawBar(page, x + (0.5 * m1), y, m1, h)
            drawBar(page, x + (2.5 * m1), y, m1, h)
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
            drawBar(page, x + (1.5 * m1), y, m1, h)
            drawBar(page, x + (3.5 * m1), y, m1, h)
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

        if direction == BarCode.TOP_TO_BOTTOM {
            w *= barHeightFactor
        }
        else if direction == BarCode.LEFT_TO_RIGHT {
            h *= barHeightFactor
        }

        var list = [UInt16]()
        for symchar in text.unicodeScalars {
            if symchar.value < 32 {
                list.append(UInt16(GS1_128.SHIFT))
                list.append(UInt16(symchar.value + 64))
            }
            else if symchar.value < 128 {
                list.append(UInt16(symchar.value - 32))
            }
            else if symchar.value < 256 {
                list.append(UInt16(GS1_128.FNC_4))
                list.append(UInt16(symchar.value - 160))    // 128 + 32
            }
            else {
                // list.append(UInt16(31))                  // '?'
                list.append(UInt16(256))                    // This will generate an exception.
            }
            if list.count == 48 {
                // Maximum number of data characters is 48
                break
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
                    if direction == BarCode.LEFT_TO_RIGHT {
                        drawVertBar(page, x, y, m1 * Float(n), h)
                    }
                    else if direction == BarCode.TOP_TO_BOTTOM {
                        drawHorzBar(page, x, y, m1 * Float(n), w)
                    }
                }
                if direction == BarCode.LEFT_TO_RIGHT {
                    x += Float(n) * m1
                }
                else if direction == BarCode.TOP_TO_BOTTOM {
                    y += Float(n) * m1
                }
                j += 1
            }
        }

        var xy = [x, y]
        if font != nil {
            if direction == BarCode.LEFT_TO_RIGHT {
                let textLine = TextLine(font!, text)
                        .setLocation(x1 + ((x - x1) - font!.stringWidth(text))/2, y1 + h + font!.bodyHeight)
                xy = textLine.drawOn(page)
                xy[0] = max(x, xy[0])
                return [xy[0], xy[1] + font!.descent]
            }
            else if direction == BarCode.TOP_TO_BOTTOM {
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
        text = "*" + text + "*"

        var x: Float = x1
        var y: Float = y1
        let w: Float = m1 * barHeightFactor     // Barcode width when drawn vertically
        let h: Float = m1 * barHeightFactor     // Barcode height when drawn horizontally

        var xy: [Float] = [0.0, 0.0]

        if direction == BarCode.LEFT_TO_RIGHT {
            for symchar in text.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.")
                }
                else {
                    let scalars = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalars[i])
                        if ch == "w" {
                            x += m1
                        }
                        else if ch == "W" {
                            x += m1 * 3
                        }
                        else if ch == "b" {
                            drawVertBar(page, x, y, m1, h)
                            x += m1
                        }
                        else if ch == "B" {
                            drawVertBar(page, x, y, m1 * 3, h)
                            x += m1 * 3
                        }
                    }
                    x += m1
                }
            }

            if font != nil {
                let textLine = TextLine(font!, text)
                        .setLocation(
                                x1 + ((x - x1) - font!.stringWidth(text))/2,
                                y1 + h + font!.bodyHeight)
                xy = textLine.drawOn(page)
                xy[0] = max(x, xy[0])
            }
        }
        else if direction == BarCode.TOP_TO_BOTTOM {
            for symchar in text.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.")
                }
                else {
                    let scalars = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalars[i])
                        if ch == "w" {
                            y += m1
                        }
                        else if ch == "W" {
                            y += 3 * m1
                        }
                        else if ch == "b" {
                            drawHorzBar(page, x, y, m1, h)
                            y += m1
                        }
                        else if ch == "B" {
                            drawHorzBar(page, x, y, 3 * m1, h)
                            y += 3 * m1
                        }
                    }
                    y += m1
                }
            }

            if font != nil {
                let textLine = TextLine(font!, text)
                        .setLocation(
                                x - font!.bodyHeight,
                                y1 + ((y - y1) - font!.stringWidth(text))/2)
                        .setTextDirection(270)
                xy = textLine.drawOn(page)
                xy[0] = max(x, xy[0]) + w
                xy[1] = max(y, xy[1])
            }
        }
        else if direction == BarCode.BOTTOM_TO_TOP {
            var height: Float = 0.0

            for symchar in text.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.")
                }
                else {
                    let scalar = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalar[i])
                        if ch == "w" || ch == "b" {
                            height += m1
                        }
                        else if ch == "W" || ch == "B" {
                            height += 3 * m1
                        }
                    }
                    height += m1
                }
            }

            y += height - m1

            for symchar in text.unicodeScalars {
                let code = tableB[String(symchar)]
                if code == nil {
                    Swift.print("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.")
                }
                else {
                    let scalars = Array(code!.unicodeScalars)
                    for i in 0..<9 {
                        let ch = String(scalars[i])
                        if ch == "w" {
                            y -= m1
                        }
                        else if ch == "W" {
                            y -= 3 * m1
                        }
                        else if ch == "b" {
                            drawHorzBar2(page, x, y, m1, h)
                            y -= m1
                        }
                        else if ch == "B" {
                            drawHorzBar2(page, x, y, 3 * m1, h)
                            y -= 3 * m1
                        }
                    }
                    y -= m1
                }
            }

            if font != nil {
                y = y1 + ( height - m1)

                let textLine = TextLine(font!, text)
                        .setLocation(
                                x + w + font!.bodyHeight,
                                y - ((y - y1) - font!.stringWidth(text))/2)
                        .setTextDirection(90)
                xy = textLine.drawOn(page)
                xy[1] = max(y, xy[1])
                return [xy[0], xy[1] + font!.descent]
            }
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
            page!.setPenWidth(m1)
            page!.moveTo(x + m1/2, y)
            page!.lineTo(x + m1/2, y + h)
            page!.strokePath()
        }
    }


    private func drawHorzBar(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,    // Module length
            _ w: Float) {
        if page != nil {
            page!.setPenWidth(m1)
            page!.moveTo(x, y + m1/2)
            page!.lineTo(x + w, y + m1/2)
            page!.strokePath()
        }
    }


    private func drawHorzBar2(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ m1: Float,    // Module length
            _ w: Float) {
        if page != nil {
            page!.setPenWidth(m1)
            page!.moveTo(x, y - m1/2)
            page!.lineTo(x + w, y - m1/2)
            page!.strokePath()
        }
    }


    public func getHeight() -> Float {
        if font == nil {
            return m1 * barHeightFactor
        }
        return m1 * barHeightFactor + font!.getHeight()
    }

}   // End of BarCode.swift
