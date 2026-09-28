/**
 * PDF417.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 *  Used to create PDF417 2D barcodes.
 *
 *  The bars are drawn from the location set with setLocation. ISO/IEC 15438
 *  asks for a quiet zone of at least two modules (twice the module width) on
 *  all four sides of the symbol, so leave that much space around it; scanners
 *  reject symbols with less.
 *
 *  Please see Example_12.
 */
public class PDF417 : Drawable {
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
    private var rows = 0
    private var cols = 18
    private var codewords = [Int]()
    private var str: String = ""
    private var altDescription: String?

    // The codeword that shifts from text compaction to byte compaction for the
    // one codeword after it.
    private static let BYTE_SHIFT = 913

    /**
     *  Constructor for 2D barcodes.
     *  The symbol has 18 columns and as many rows as the string needs, up to the
     *  928 codewords a PDF417 symbol can hold: 864 data codewords, or about 1,300
     *  characters of mixed text, with the error correction level 5 used here.
     *  The string is ASCII, and a control character other than HT, LF and CR
     *  takes a codeword of its own and one more, in byte compaction.
     *  Throws if there are unencodable characters or the string does not fit in a symbol.
     *
     *  - Parameter str: the specified string.
     */
    public init(_ str: String) throws {
        self.str = str
        self.h1 = 3 * w1

        let scalars = str.unicodeScalars
        for scalar in scalars {
            if scalar.value > 126 {
                throw PDFjetError(message: "The string contains unencodable characters.")
            }
        }

        // The data codewords, after the symbol length descriptor
        let list = dataCodewords()
        let dataCodewords = 1 + list.count
        rows = (dataCodewords + L5ECC.table.count + cols - 1) / cols
        if rows < 3 {
            rows = 3
        }
        if rows * cols > 928 {
            throw PDFjetError(message: "The string is too long for a PDF417 barcode.")
        }
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
            } else if k == 2 {
                lf = cf + 3*compression + ((rows - 1) % 3)
                lr = cf + ((rows - 1) / 3)
            } else if k == 3 {
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

        let dataLen = (rows * cols) - L5ECC.table.count
        for i in 0..<dataLen {
            buffer[i] = 900     // The default pad codeword
        }
        buffer[0] = dataLen
        buffer.replaceSubrange(1..<(1 + list.count), with: list)

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

    /**
     *  Sets the location of this barcode on the page.
     *
     *  - Parameter x: the x coordinate of the top left corner of the barcode.
     *  - Parameter y: the y coordinate of the top left corner of the barcode.
     *  - Returns: this PDF417 object.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x1 = x
        self.y1 = y
        return self
    }

    /**
     *  Sets the module length of this barcode, the width of its narrowest bar.
     *  This changes the barcode size while preserving the aspect.
     *  Use value between 0.5f and 0.75f.
     *  If the value is too small some scanners may have difficulty reading the barcode.
     *
     *  - Parameter moduleLength: the module length of the barcode.
     *  - Returns: this PDF417 object.
     */
    @discardableResult
    public func setModuleLength(_ moduleLength: Float) -> PDF417 {
        self.w1 = moduleLength
        self.h1 = 3 * w1
        return self
    }

    /**
     *  Sets what the barcode says for a screen reader: a tagged document,
     *  PDF/UA or a PDF/A of level A, then has the barcode as a figure of that
     *  description. Without one, its bars are decoration, which a screen
     *  reader skips.
     *
     *  - Parameter altDescription: the description.
     *  - Returns: this PDF417 object.
     */
    @discardableResult
    public func setAltDescription(_ altDescription: String?) -> PDF417 {
        self.altDescription = altDescription
        return self
    }

    /**
     *  Draws this barcode on the specified page. The bars are black, and the
     *  pen of the page is as it was after it.
     *
     *  - Parameter page: the page to draw this barcode on.
     *  - Returns: x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let described = altDescription != nil && !altDescription!.isEmpty
        if let page = page {
            // Described, the barcode is a figure of a tagged document; not
            // described, its bars, which carry no text, are decoration.
            if described {
                page.addBDC(StructElem.FIGURE, nil, nil, altDescription!)
            } else {
                page.addArtifactBMC()
            }
            page.saveGraphicsState()
            page.setPenColor(Color.black)
        }
        let xy = drawPdf417(page)
        if let page = page {
            page.restoreGraphicsState()
            if described {
                page.setFigureBoundingBox(x1, y1, xy[0] - x1, xy[1] - y1)
            }
            page.addEMC()
        }
        return xy
    }

    private func textToArrayOfIntegers() -> [Int] {
        var list = [Int]()

        var currentMode = PDF417.ALPHA
        for scalar in str.unicodeScalars {
            if scalar == Unicode.Scalar(0x20) {
                list.append(26)
                continue
            }
            if PDF417.isByteShifted(scalar) {
                list.append(-1 - Int(scalar.value))     // Below 0, see dataCodewords
                continue
            }

            let value = TextCompact.table[Int(scalar.value)][1]
            let mode = TextCompact.table[Int(scalar.value)][2]
            if mode == currentMode {
                list.append(value)
            } else {
                if mode == PDF417.ALPHA && currentMode == PDF417.LOWER {
                    list.append(PDF417.SHIFT_TO_ALPHA)
                    list.append(value)
                } else if mode == PDF417.ALPHA && currentMode == PDF417.MIXED {
                    list.append(PDF417.LATCH_TO_ALPHA)
                    list.append(value)
                    currentMode = mode
                } else if mode == PDF417.LOWER && currentMode == PDF417.ALPHA {
                    list.append(PDF417.LATCH_TO_LOWER)
                    list.append(value)
                    currentMode = mode
                } else if mode == PDF417.LOWER && currentMode == PDF417.MIXED {
                    list.append(PDF417.LATCH_TO_LOWER)
                    list.append(value)
                    currentMode = mode
                } else if mode == PDF417.MIXED && currentMode == PDF417.ALPHA {
                    list.append(PDF417.LATCH_TO_MIXED)
                    list.append(value)
                    currentMode = mode
                } else if mode == PDF417.MIXED && currentMode == PDF417.LOWER {
                    list.append(PDF417.LATCH_TO_MIXED)
                    list.append(value)
                    currentMode = mode
                } else if mode == PDF417.PUNCT && currentMode == PDF417.ALPHA {
                    list.append(PDF417.SHIFT_TO_PUNCT)
                    list.append(value)
                } else if mode == PDF417.PUNCT && currentMode == PDF417.LOWER {
                    list.append(PDF417.SHIFT_TO_PUNCT)
                    list.append(value)
                } else if mode == PDF417.PUNCT && currentMode == PDF417.MIXED {
                    list.append(PDF417.SHIFT_TO_PUNCT)
                    list.append(value)
                }
            }
        }

        return list
    }

    // Tells if the character is a control character that text compaction has
    // no value for, and that is so encoded in byte compaction.
    private static func isByteShifted(_ scalar: Unicode.Scalar) -> Bool {
        return scalar.value < 0x20 && scalar != "\t" && scalar != "\n" && scalar != "\r"
    }

    // Returns the data codewords of the string in text compaction, two values
    // to a codeword; a control character other than HT, LF and CR is the byte
    // compaction shift and its byte, which starts a codeword, so the value
    // before it may be padded, and after which text compaction goes on in the
    // submode it was in.
    func dataCodewords() -> [Int] {
        var codewords = [Int]()
        var hi = -1     // The first value of a codeword, if any
        for value in textToArrayOfIntegers() {
            if value < 0 {
                if hi != -1 {
                    codewords.append(30*hi + PDF417.SHIFT_TO_PUNCT)     // Pad
                    hi = -1
                }
                codewords.append(PDF417.BYTE_SHIFT)
                codewords.append(-1 - value)
            } else if hi == -1 {
                hi = value
            } else {
                codewords.append(30*hi + value)
                hi = -1
            }
        }
        if hi != -1 {
            codewords.append(30*hi + PDF417.SHIFT_TO_PUNCT)     // Pad
        }
        return codewords
    }

    private func addECC(_ buf: inout [Int]) {
        var ecc = [Int](repeating: 0, count: L5ECC.table.count)
        var t1 = 0
        var t2 = 0
        var t3 = 0

        let dataLen = buf.count - ecc.count
        for i in 0..<dataLen {
            t1 = (buf[i] + ecc[ecc.count - 1]) % 929
            var j = ecc.count - 1
            while j > 0 {
                t2 = (t1 * L5ECC.table[j]) % 929
                t3 = 929 - t2
                ecc[j] = (ecc[j - 1] + t3) % 929
                j -= 1
            }
            t2 = (t1 * L5ECC.table[0]) % 929
            t3 = 929 - t2
            ecc[0] = t3 % 929
        }

        for i in 0..<ecc.count {
            if ecc[i] != 0 {
                buf[(buf.count - 1) - i] = 929 - ecc[i]
            }
        }
    }

    private func drawPdf417(_ page: Page?) -> [Float] {
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
        let x0 = x              // Where the codewords of each row start

        var k = 1               // Cluster index
        for i in 0..<codewords.count {
            let row = codewords[i]
            let symbol = String(Pattern.table[row][k])
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
                x = x0
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
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ w: Float,    // Bar width
            _ h: Float) {
        guard let page = page else {
            return      // Measured, not drawn
        }
        page.setPenWidth(w)
        page.moveTo(x + w/2, y)
        page.lineTo(x + w/2, y + h)
        page.strokePath()
    }
}   // End of PDF417.swift
