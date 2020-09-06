/**
 *  BarCode2D.swift
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


/**
 *  Used to create PDF417 2D barcodes.
 *
 *  Please see Example_12.
 */
public class BarCode2D : Drawable {

    private static let ALPHA = 0x08
    private static let LOWER = 0x04
    private static let MIXED = 0x02
    private static let PUNCT = 0x01

    private static let LATCH_TO_LOWER = 27
    private static let SHIFT_TO_ALPHA = 27
    private static let LATCH_TO_MIXED = 28
    private static let LATCH_TO_ALPHA = 28
    private static let SHIFT_TO_PUNCT = 29

    private var x1: Float = 0.0
    private var y1: Float = 0.0

    // Critical defaults!
    private var w1: Float = 0.75
    private var h1: Float = 0.0

    private var rows = 50
    private var cols = 18

    private var codewords = [Int]()

    private var str: String = ""


    /**
     *  Constructor for 2D barcodes.
     *
     *  @param str the specified string.
     */
    public init(_ str: String) {

        self.str = str
        self.h1 = 3 * w1
        self.codewords = [Int](repeating: 0, count: rows * (cols + 2))

        var lfBuffer = [Int](repeating: 0, count: rows)
        var lrBuffer = [Int](repeating: 0, count: rows)
        var buffer = [Int](repeating: 0, count: (rows * cols))

        // Left and right row indicators - see page 34 of the ISO specification
        let compression = 5         // Compression Level
        var k = 1
        for i in 0..<rows {
            var lf = 0
            var lr = 0
            let cf = 30 * (i / 3)
            if k == 1 {
                lf = cf + ((rows - 1) / 3)
                lr = cf + (cols - 1)
            }
            else if k == 2 {
                lf = cf + 3*compression + ((rows - 1) % 3)
                lr = cf + ((rows - 1) / 3)
            }
            else if k == 3 {
                lf = cf + (cols - 1)
                lr = cf + 3*compression + ((rows - 1) % 3)
            }
            lfBuffer[i] = lf
            lrBuffer[i] = lr
            k += 1
            if k == 4 {
                k = 1
            }
        }

        let dataLen = (rows * cols) - ECC_L5.table.count
        for i in 0..<dataLen {
            buffer[i] = 900     // The default pad codeword
        }
        buffer[0] = dataLen

        addData(&buffer, dataLen)
        addECC(&buffer)

        for i in 0..<rows {
            let index = (cols + 2) * i
            codewords[index] = lfBuffer[i]
            for j in 0..<cols {
                codewords[index + j + 1] = buffer[cols*i + j]
            }
            codewords[index + cols + 1] = lrBuffer[i]
        }
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    /**
     *  Sets the location of this barcode on the page.
     *
     *  @param x the x coordinate of the top left corner of the barcode.
     *  @param y the y coordinate of the top left corner of the barcode.
     */
    public func setLocation(_ x: Float, _ y: Float) {
        self.x1 = x
        self.y1 = y
    }


    /**
     *  Sets the module width for this barcode.
     *  This changes the barcode size while preserving the aspect.
     *  Use value between 0.5f and 0.75f.
     *  If the value is too small some scanners may have difficulty reading the barcode.
     *
     *  @param width the module width of the barcode.
     */
    public func setModuleWidth(_ width: Float) {
        self.w1 = width
        self.h1 = 3 * w1
    }


    /**
     *  Draws this barcode on the specified page.
     *
     *  @param page the page to draw this barcode on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        return drawPdf417(page!)
    }


    private func textToArrayOfIntegers() -> [Int] {
        var list = [Int]()

        var currentMode = BarCode2D.ALPHA
        for scalar in str.unicodeScalars {
            if scalar == Unicode.Scalar(0x20) {
                list.append(26)
                continue
            }

            let value = TextCompact.TABLE[Int(scalar.value)][1]
            let mode = TextCompact.TABLE[Int(scalar.value)][2]
            if mode == currentMode {
                list.append(value)
            }
            else {
                if mode == BarCode2D.ALPHA && currentMode == BarCode2D.LOWER {
                    list.append(BarCode2D.SHIFT_TO_ALPHA)
                    list.append(value)
                }
                else if mode == BarCode2D.ALPHA && currentMode == BarCode2D.MIXED {
                    list.append(BarCode2D.LATCH_TO_ALPHA)
                    list.append(value)
                    currentMode = mode
                }
                else if mode == BarCode2D.LOWER && currentMode == BarCode2D.ALPHA {
                    list.append(BarCode2D.LATCH_TO_LOWER)
                    list.append(value)
                    currentMode = mode
                }
                else if mode == BarCode2D.LOWER && currentMode == BarCode2D.MIXED {
                    list.append(BarCode2D.LATCH_TO_LOWER)
                    list.append(value)
                    currentMode = mode
                }
                else if mode == BarCode2D.MIXED && currentMode == BarCode2D.ALPHA {
                    list.append(BarCode2D.LATCH_TO_MIXED)
                    list.append(value)
                    currentMode = mode
                }
                else if mode == BarCode2D.MIXED && currentMode == BarCode2D.LOWER {
                    list.append(BarCode2D.LATCH_TO_MIXED)
                    list.append(value)
                    currentMode = mode
                }
                else if mode == BarCode2D.PUNCT && currentMode == BarCode2D.ALPHA {
                    list.append(BarCode2D.SHIFT_TO_PUNCT)
                    list.append(value)
                }
                else if mode == BarCode2D.PUNCT && currentMode == BarCode2D.LOWER {
                    list.append(BarCode2D.SHIFT_TO_PUNCT)
                    list.append(value)
                }
                else if mode == BarCode2D.PUNCT && currentMode == BarCode2D.MIXED {
                    list.append(BarCode2D.SHIFT_TO_PUNCT)
                    list.append(value)
                }
            }
        }

        return list
    }


    private func addData(_ buffer: inout [Int], _ dataLen: Int) {
        let list = textToArrayOfIntegers()

        // buffer index = 1 to skip the Symbol Length Descriptor
        var bi = 1
        var hi = 0
        var lo = 0
        var i = 0
        while i < list.count {
            hi = list[i]
            if i + 1 == list.count {
                lo = BarCode2D.SHIFT_TO_PUNCT       // Pad
            } else {
                lo = list[i + 1]
            }
            bi += 1
            if bi == dataLen {
                break
            }
            buffer[bi] = 30*hi + lo
            i += 2
        }
    }


    private func addECC(_ buf: inout [Int]) {
        var ecc = [Int](repeating: 0, count: ECC_L5.table.count)
        var t1 = 0
        var t2 = 0
        var t3 = 0

        let dataLen = buf.count - ecc.count
        for i in 0..<dataLen {
            t1 = (buf[i] + ecc[ecc.count - 1]) % 929
            var j = ecc.count - 1
            while j > 0 {
                t2 = (t1 * ECC_L5.table[j]) % 929
                t3 = 929 - t2
                ecc[j] = (ecc[j - 1] + t3) % 929
                j -= 1
            }
            t2 = (t1 * ECC_L5.table[0]) % 929
            t3 = 929 - t2
            ecc[0] = t3 % 929
        }

        for i in 0..<ecc.count {
            if ecc[i] != 0 {
                buf[(buf.count - 1) - i] = 929 - ecc[i]
            }
        }
    }


    private func drawPdf417(_ page: Page) -> [Float] {
        var x: Float = x1
        var y: Float = y1

        let startSymbol = [8, 1, 1, 1, 1, 1, 1, 3]
        for i in 0..<startSymbol.count {
            let n = startSymbol[i]
            if i%2 == 0 {
                drawBar(page, x, y, Float(n) * w1, Float(rows) * h1)
            }
            x += Float(n) * w1
        }
        x1 = x

        var k = 1               // Cluster index
        for i in 0..<codewords.count {
            let row = codewords[i]
            let symbol = String(PDF417.TABLE[row][k])
            for j in 0..<8 {
                let n = Array(symbol.unicodeScalars)[j].value - 0x30
                if j%2 == 0 {
                    drawBar(page, x, y, Float(n) * w1, h1)
                }
                x += Float(n) * w1
            }
            if i == codewords.count - 1 {
                break
            }
            if (i + 1) % (cols + 2) == 0 {
                x = x1
                y += h1
                k += 1
                if k == 4 {
                    k = 1
                }
            }
        }

        y = y1
        let endSymbol = [7, 1, 1, 3, 1, 1, 1, 2, 1]
        for i in 0..<endSymbol.count {
            let n = endSymbol[i]
            if i%2 == 0 {
                drawBar(page, x, y, Float(n) * w1, Float(rows) * h1)
            }
            x += Float(n) * w1
        }

        return [x, y + h1*Float(rows)]
    }


    private func drawBar(
            _ page: Page,
            _ x: Float,
            _ y: Float,
            _ w: Float,    // Bar width
            _ h: Float) {
        page.setPenWidth(w)
        page.moveTo(x + w/2, y)
        page.lineTo(x + w/2, y + h)
        page.strokePath()
    }

}   // End of BarCode2D.swift
