/**
 * DataMatrix.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create 2D Data Matrix barcodes: ECC 200 symbols as specified in
/// ISO/IEC 16022. The text is encoded as UTF-8 in the smallest symbol that holds
/// it. Please see Example_51.
///
public final class DataMatrix : Drawable {
    /// Square symbols, from 10x10 to 144x144 modules.
    public static let SQUARE: Int = 0

    /// Rectangular symbols, from 8x18 to 16x48 modules. Data that does not fit
    /// in a rectangular symbol gets a square symbol.
    public static let RECTANGLE: Int = 1

    // The symbols, smallest first: the rows and columns of modules, the rows and
    // columns of modules in each data region, the data codewords, the error
    // correction codewords and the number of Reed-Solomon blocks.
    private static let SQUARES: [[Int]] = [
        [10, 10, 8, 8, 3, 5, 1],
        [12, 12, 10, 10, 5, 7, 1],
        [14, 14, 12, 12, 8, 10, 1],
        [16, 16, 14, 14, 12, 12, 1],
        [18, 18, 16, 16, 18, 14, 1],
        [20, 20, 18, 18, 22, 18, 1],
        [22, 22, 20, 20, 30, 20, 1],
        [24, 24, 22, 22, 36, 24, 1],
        [26, 26, 24, 24, 44, 28, 1],
        [32, 32, 14, 14, 62, 36, 1],
        [36, 36, 16, 16, 86, 42, 1],
        [40, 40, 18, 18, 114, 48, 1],
        [44, 44, 20, 20, 144, 56, 1],
        [48, 48, 22, 22, 174, 68, 1],
        [52, 52, 24, 24, 204, 84, 2],
        [64, 64, 14, 14, 280, 112, 2],
        [72, 72, 16, 16, 368, 144, 4],
        [80, 80, 18, 18, 456, 192, 4],
        [88, 88, 20, 20, 576, 224, 4],
        [96, 96, 22, 22, 696, 272, 4],
        [104, 104, 24, 24, 816, 336, 6],
        [120, 120, 18, 18, 1050, 408, 6],
        [132, 132, 20, 20, 1304, 496, 8],
        [144, 144, 22, 22, 1558, 620, 10],
    ]

    private static let RECTANGLES: [[Int]] = [
        [8, 18, 6, 16, 5, 7, 1],
        [8, 32, 6, 14, 10, 11, 1],
        [12, 26, 10, 24, 16, 14, 1],
        [12, 36, 10, 16, 22, 18, 1],
        [16, 36, 14, 16, 32, 24, 1],
        [16, 48, 14, 22, 49, 28, 1],
    ]

    private static let PAD = 129
    private static let BASE256_LATCH = 231
    private static let UPPER_SHIFT = 235
    private static let ECI = 241
    private static let ECI_UTF8 = 26

    // Powers and logarithms of 2 in GF(256) with the prime polynomial 301.
    private static let GF: (exp: [Int], log: [Int]) = {
        var exp = [Int](repeating: 0, count: 255)
        var log = [Int](repeating: 0, count: 256)
        var value = 1
        for i in 0..<255 {
            exp[i] = value
            log[value] = i
            value <<= 1
            if value > 255 {
                value ^= 301
            }
        }
        return (exp, log)
    }()

    private var modules = [[Bool]]()
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var m1: Float = 2.0     // Module length
    private var color: Int32 = Color.black

    // The codewords being placed in the mapping matrix, and the matrix.
    private var codewords = [Int]()
    private var nrow = 0
    private var ncol = 0
    private var dark = [[Bool]]()
    private var placed = [[Bool]]()

    /// Creates a square Data Matrix barcode.
    public convenience init(_ str: String) {
        self.init(str, DataMatrix.SQUARE)
    }

    /// Creates a Data Matrix barcode with the shape DataMatrix.SQUARE or
    /// DataMatrix.RECTANGLE. Stops the program if the text does not fit in the
    /// largest symbol.
    public init(_ str: String, _ shape: Int) {
        let data = DataMatrix.encode(Array(str.utf8))
        let symbol = DataMatrix.selectSymbol(data.count, shape)
        let allCodewords = DataMatrix.addErrorCorrection(DataMatrix.pad(data, symbol[4]), symbol)
        let rows = symbol[0]
        let cols = symbol[1]
        let regionRows = symbol[2]
        let regionCols = symbol[3]
        placeCodewords(allCodewords, (rows / (regionRows + 2)) * regionRows, (cols / (regionCols + 2)) * regionCols)

        modules = [[Bool]](repeating: [Bool](repeating: false, count: cols), count: rows)
        // Each data region has a solid line on its left and bottom sides and a
        // line of alternating modules on its top and right sides.
        for row in stride(from: 0, to: rows, by: regionRows + 2) {
            for col in 0..<cols {
                modules[row][col] = (col % 2 == 0)
                modules[row + regionRows + 1][col] = true
            }
        }
        for col in stride(from: 0, to: cols, by: regionCols + 2) {
            for row in 0..<rows {
                modules[row][col] = true
                modules[row][col + regionCols + 1] = (row % 2 == 1)
            }
        }
        for row in 0..<nrow {
            for col in 0..<ncol {
                modules[row + 1 + 2*(row / regionRows)][col + 1 + 2*(col / regionCols)] = dark[row][col]
            }
        }
    }

    /// Sets the location of the top left corner of this barcode.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the module length of this barcode. The default value is 2.0.
    /// Leave a margin of at least one module around the barcode.
    @discardableResult
    public func setModuleLength(_ moduleLength: Float) -> DataMatrix {
        self.m1 = moduleLength
        return self
    }

    /// Sets the color of the barcode as a 0xRRGGBB value.
    @discardableResult
    public func setColor(_ color: Int32) -> DataMatrix {
        self.color = color
        return self
    }

    /// Returns the modules of this barcode, by row and column; true is dark.
    public func getData() -> [[Bool]] {
        return modules
    }

    ///
    /// Draws this barcode on the specified page. With no page nothing is drawn.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of this barcode.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let rows = modules.count
        let cols = modules[0].count
        if let page = page {
            page.setBrushColor(color)
            for row in 0..<rows {
                var col = 0
                while col < cols {
                    if !modules[row][col] {
                        col += 1
                        continue
                    }
                    let start = col
                    while col < cols && modules[row][col] {
                        col += 1
                    }
                    page.fillRect(x + Float(start)*m1, y + Float(row)*m1, Float(col - start)*m1, m1)
                }
            }
        }
        return [x + Float(cols)*m1, y + Float(rows)*m1]
    }

    // Encodes the bytes in ASCII encodation, or in Base 256 encodation when that
    // takes fewer codewords. Text that is not all ASCII starts with the ECI that
    // tells the reader the bytes are UTF-8.
    private static func encode(_ bytes: [UInt8]) -> [Int] {
        var data = [Int]()
        let ascii = !bytes.contains(where: { $0 > 127 })
        if !ascii {
            data.append(ECI)
            data.append(ECI_UTF8 + 1)
        }
        let base256Length = 1 + ((bytes.count <= 249) ? 1 : 2) + bytes.count
        if asciiLength(bytes) <= base256Length {
            var i = 0
            while i < bytes.count {
                let c = Int(bytes[i])
                if isDigit(c) && i + 1 < bytes.count && isDigit(Int(bytes[i + 1])) {
                    data.append(130 + 10*(c - 48) + (Int(bytes[i + 1]) - 48))
                    i += 2
                } else if c < 128 {
                    data.append(c + 1)
                    i += 1
                } else {
                    data.append(UPPER_SHIFT)
                    data.append(c - 127)
                    i += 1
                }
            }
        } else {
            data.append(BASE256_LATCH)
            if bytes.count <= 249 {
                data.append(randomize255(bytes.count, data.count + 1))
            } else {
                data.append(randomize255(bytes.count/250 + 249, data.count + 1))
                data.append(randomize255(bytes.count % 250, data.count + 1))
            }
            for b in bytes {
                data.append(randomize255(Int(b), data.count + 1))
            }
        }
        return data
    }

    // Returns the number of codewords of the bytes in ASCII encodation.
    private static func asciiLength(_ bytes: [UInt8]) -> Int {
        var length = 0
        var i = 0
        while i < bytes.count {
            let c = Int(bytes[i])
            if isDigit(c) && i + 1 < bytes.count && isDigit(Int(bytes[i + 1])) {
                length += 1
                i += 2
            } else {
                length += (c < 128) ? 1 : 2
                i += 1
            }
        }
        return length
    }

    private static func isDigit(_ c: Int) -> Bool {
        return c >= 48 && c <= 57
    }

    // Scrambles a Base 256 codeword at the position, counted from 1, in the data.
    private static func randomize255(_ value: Int, _ position: Int) -> Int {
        let result = value + ((149*position) % 255) + 1
        return (result <= 255) ? result : result - 256
    }

    // Returns the smallest symbol of the shape that holds the data codewords.
    private static func selectSymbol(_ length: Int, _ shape: Int) -> [Int] {
        if shape == RECTANGLE {
            for symbol in RECTANGLES where symbol[4] >= length {
                return symbol
            }
        }
        for symbol in SQUARES where symbol[4] >= length {
            return symbol
        }
        fatalError("The text takes \(length) codewords; a Data Matrix symbol holds 1558 at most.")
    }

    // Fills the rest of the symbol's data capacity with pad codewords: the first
    // one as it is, and the others scrambled by their position.
    private static func pad(_ data: [Int], _ capacity: Int) -> [Int] {
        var padded = data
        for i in data.count..<capacity {
            if i == data.count {
                padded.append(PAD)
            } else {
                let result = PAD + ((149*(i + 1)) % 253) + 1
                padded.append((result <= 254) ? result : result - 254)
            }
        }
        return padded
    }

    // Appends the error correction codewords. The codewords are spread over the
    // blocks in turn, the error correction codewords continuing where the data
    // codewords stop, and each block gets its own error correction codewords.
    private static func addErrorCorrection(_ data: [Int], _ symbol: [Int]) -> [Int] {
        let blocks = symbol[6]
        let eccPerBlock = symbol[5] / blocks
        let gen = generator(eccPerBlock)
        var ecc = [[Int]]()
        for block in 0..<blocks {
            var blockData = [Int]()
            for i in stride(from: block, to: data.count, by: blocks) {
                blockData.append(data[i])
            }
            ecc.append(reedSolomon(blockData, gen))
        }
        var codewords = data
        var next = [Int](repeating: 0, count: blocks)
        for i in data.count..<(data.count + symbol[5]) {
            let block = i % blocks
            codewords.append(ecc[block][next[block]])
            next[block] += 1
        }
        return codewords
    }

    // Returns the coefficients, lowest degree first, of the generator polynomial
    // (x + 2^1)(x + 2^2)...(x + 2^degree).
    private static func generator(_ degree: Int) -> [Int] {
        var g = [Int](repeating: 0, count: degree + 1)
        g[0] = 1
        for j in 1...degree {
            let root = GF.exp[j]
            for i in stride(from: j, to: 0, by: -1) {
                g[i] = g[i - 1] ^ multiply(g[i], root)
            }
            g[0] = multiply(g[0], root)
        }
        return g
    }

    // Returns the error correction codewords of the data: the remainder of the
    // data polynomial times x^degree divided by the generator, highest degree first.
    private static func reedSolomon(_ data: [Int], _ generator: [Int]) -> [Int] {
        let degree = generator.count - 1
        var remainder = [Int](repeating: 0, count: degree)
        for codeword in data {
            let feedback = codeword ^ remainder[degree - 1]
            for i in stride(from: degree - 1, to: 0, by: -1) {
                remainder[i] = remainder[i - 1] ^ multiply(feedback, generator[i])
            }
            remainder[0] = multiply(feedback, generator[0])
        }
        return Array(remainder.reversed())
    }

    private static func multiply(_ a: Int, _ b: Int) -> Int {
        if a == 0 || b == 0 {
            return 0
        }
        return GF.exp[(GF.log[a] + GF.log[b]) % 255]
    }

    // Places the codewords in the mapping matrix, the data regions of the symbol
    // without their borders, along the diagonals of ISO/IEC 16022 Annex F.
    private func placeCodewords(_ codewords: [Int], _ nrow: Int, _ ncol: Int) {
        self.codewords = codewords
        self.nrow = nrow
        self.ncol = ncol
        self.dark = [[Bool]](repeating: [Bool](repeating: false, count: ncol), count: nrow)
        self.placed = self.dark
        var chr = 0
        var row = 4
        var col = 0
        repeat {
            if row == nrow && col == 0 {
                corner1(chr)
                chr += 1
            }
            if row == nrow - 2 && col == 0 && ncol % 4 != 0 {
                corner2(chr)
                chr += 1
            }
            if row == nrow - 2 && col == 0 && ncol % 8 == 4 {
                corner3(chr)
                chr += 1
            }
            if row == nrow + 4 && col == 2 && ncol % 8 == 0 {
                corner4(chr)
                chr += 1
            }
            // Up and to the right
            repeat {
                if row < nrow && col >= 0 && !placed[row][col] {
                    utah(row, col, chr)
                    chr += 1
                }
                row -= 2
                col += 2
            } while row >= 0 && col < ncol
            row += 1
            col += 3
            // Down and to the left
            repeat {
                if row >= 0 && col < ncol && !placed[row][col] {
                    utah(row, col, chr)
                    chr += 1
                }
                row += 2
                col -= 2
            } while row < nrow && col >= 0
            row += 3
            col += 1
        } while row < nrow || col < ncol
        // The bottom right corner is not always used by the codewords.
        if !placed[nrow - 1][ncol - 1] {
            dark[nrow - 1][ncol - 1] = true
            dark[nrow - 2][ncol - 2] = true
        }
    }

    // Places bit 1 (the most significant) to bit 8 of a codeword at the module,
    // wrapping positions outside the matrix around to the other side.
    private func module(_ row: Int, _ col: Int, _ chr: Int, _ bit: Int) {
        var row = row
        var col = col
        if row < 0 {
            row += nrow
            col += 4 - ((nrow + 4) % 8)
        }
        if col < 0 {
            col += ncol
            row += 4 - ((ncol + 4) % 8)
        }
        placed[row][col] = true
        dark[row][col] = ((codewords[chr] >> (8 - bit)) & 1) == 1
    }

    // The usual shape of a codeword, with its last bit at the row and column.
    private func utah(_ row: Int, _ col: Int, _ chr: Int) {
        module(row - 2, col - 2, chr, 1)
        module(row - 2, col - 1, chr, 2)
        module(row - 1, col - 2, chr, 3)
        module(row - 1, col - 1, chr, 4)
        module(row - 1, col, chr, 5)
        module(row, col - 2, chr, 6)
        module(row, col - 1, chr, 7)
        module(row, col, chr, 8)
    }

    private func corner1(_ chr: Int) {
        module(nrow - 1, 0, chr, 1)
        module(nrow - 1, 1, chr, 2)
        module(nrow - 1, 2, chr, 3)
        module(0, ncol - 2, chr, 4)
        module(0, ncol - 1, chr, 5)
        module(1, ncol - 1, chr, 6)
        module(2, ncol - 1, chr, 7)
        module(3, ncol - 1, chr, 8)
    }

    private func corner2(_ chr: Int) {
        module(nrow - 3, 0, chr, 1)
        module(nrow - 2, 0, chr, 2)
        module(nrow - 1, 0, chr, 3)
        module(0, ncol - 4, chr, 4)
        module(0, ncol - 3, chr, 5)
        module(0, ncol - 2, chr, 6)
        module(0, ncol - 1, chr, 7)
        module(1, ncol - 1, chr, 8)
    }

    private func corner3(_ chr: Int) {
        module(nrow - 3, 0, chr, 1)
        module(nrow - 2, 0, chr, 2)
        module(nrow - 1, 0, chr, 3)
        module(0, ncol - 2, chr, 4)
        module(0, ncol - 1, chr, 5)
        module(1, ncol - 1, chr, 6)
        module(2, ncol - 1, chr, 7)
        module(3, ncol - 1, chr, 8)
    }

    private func corner4(_ chr: Int) {
        module(nrow - 1, 0, chr, 1)
        module(nrow - 1, ncol - 1, chr, 2)
        module(0, ncol - 3, chr, 3)
        module(0, ncol - 2, chr, 4)
        module(0, ncol - 1, chr, 5)
        module(1, ncol - 3, chr, 6)
        module(1, ncol - 2, chr, 7)
        module(1, ncol - 1, chr, 8)
    }
}   // End of DataMatrix.swift
