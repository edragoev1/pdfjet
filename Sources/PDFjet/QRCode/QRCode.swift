/**
 * QRCode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Kazuhiko Arase, 2009
 * URL: http://www.d-project.com/
 * Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
 *
 * The word "QR Code" is a registered trademark of
 * DENSO WAVE INCORPORATED
 * http://www.denso-wave.com/qrcode/faqpatent-e.html
 *
 * Modified and adapted for use in PDFjet by PDFjet Software
 */
import Foundation

/**
 * Used to create 2D QR Code barcodes. Please see Example_21.
 */
public class QRCode : Drawable {
    private let PAD0: UInt32 = 0xEC
    private let PAD1: UInt32 = 0x11
    // The dark modules, and the modules of the function patterns and the format information.
    var modules = [[Bool]]()
    var reserved = [[Bool]]()
    // The version of the symbol, from 4 to 40, and its size: 4 * typeNumber + 17 modules.
    private var typeNumber = 4
    var moduleCount = 33
    var errorCorrectionLevel = ErrorCorrectionLevel.M

    private var x: Float = 0.0
    private var y: Float = 0.0

    private var qrData: [UInt8]?
    private var eci = false                 // The data is not all ASCII, and starts with the ECI of UTF-8
    private var m1: Float = 2.0             // Module length

    private var color: Int32 = Color.black
    private var altDescription: String?
    private let qrutil = QRUtil()

    // The bits of the ECI that says the bytes are UTF-8: the mode indicator
    // 0111 and the ECI assignment number 26 in 8 bits.
    private static let ECI_BITS = 12

    ///
    /// Used to create 2D QR Code barcodes. The string is encoded in UTF-8, and
    /// the symbol is the smallest that holds it, from version 4, 33 by 33 modules,
    /// to version 40, 177 by 177 modules. At version 40 it holds up to 2,953 bytes
    /// at level L, 2,331 at M, 1,663 at Q and 1,273 at H, a byte fewer when the
    /// string is not all ASCII: it then starts with the ECI that tells the reader
    /// the bytes are UTF-8.
    ///
    /// Throws a PDFjetError when the data does not fit a version 40 symbol at
    /// that error correction level.
    ///
    /// - Parameter str: the string to encode.
    /// - Parameter errorCorrectionLevel: the desired error correction level.
    ///
    public init(
            _ str: String,
            _ errorCorrectionLevel: ErrorCorrectionLevel) throws {
        self.qrData = Array(str.utf8)
        self.eci = qrData!.contains(where: { $0 > 127 })
        self.errorCorrectionLevel = errorCorrectionLevel
        self.typeNumber = try QRCode.getTypeNumber(qrData!.count, eci, errorCorrectionLevel)
        self.moduleCount = 4 * typeNumber + 17
        self.make(try createData(errorCorrectionLevel))
    }

    // Returns the smallest version, from 4, whose data codewords hold the
    // data, and the ECI before it when there is one.
    private static func getTypeNumber(
            _ dataLength: Int,
            _ eci: Bool,
            _ errorCorrectionLevel: ErrorCorrectionLevel) throws -> Int {
        for typeNumber in 4...40 {
            if getLengthInBits(dataLength, eci, typeNumber) <= getDataCount(typeNumber, errorCorrectionLevel) * 8 {
                return typeNumber
            }
        }
        let maxLength = (getDataCount(40, errorCorrectionLevel) * 8 - getLengthInBits(0, eci, 40)) / 8
        throw PDFjetError(message: "The data is too long for a QR code at level " +
                String(describing: errorCorrectionLevel) + ": " + String(dataLength) +
                " bytes, at most " + String(maxLength) + ".")
    }

    // The bits of the ECI, if any, and of the mode indicator, the character
    // count and the data, in byte mode.
    private static func getLengthInBits(_ dataLength: Int, _ eci: Bool, _ typeNumber: Int) -> Int {
        return (eci ? ECI_BITS : 0) + 4 + getCharacterCountBits(typeNumber) + 8 * dataLength
    }

    // The character count of byte mode has 8 bits up to version 9, and 16 bits after it.
    private static func getCharacterCountBits(_ typeNumber: Int) -> Int {
        return (typeNumber < 10) ? 8 : 16
    }

    private static func getDataCount(
            _ typeNumber: Int,
            _ errorCorrectionLevel: ErrorCorrectionLevel) -> Int {
        var dataCount = 0
        for block in RSBlock.getRSBlocks(typeNumber, errorCorrectionLevel) {
            dataCount += block.getDataCount()
        }
        return dataCount
    }

    ///
    /// Sets the location where this barcode will be drawn on the page.
    ///
    /// - Parameter x: the x coordinate of the top left corner of the barcode.
    /// - Parameter y: the y coordinate of the top left corner of the barcode.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the module length of this barcode.
    /// The default value is 2.0
    ///
    /// - Parameter moduleLength: the specified module length.
    ///
    @discardableResult
    public func setModuleLength(_ moduleLength: Float) -> QRCode {
        self.m1 = moduleLength
        return self
    }

    /// Sets the color of the QR code as a 0xRRGGBB value.
    @discardableResult
    public func setModuleColor(_ color: Int32) -> QRCode {
        self.color = color
        return self
    }

    ///
    /// Sets what the QR code says, such as the web address it carries, for a
    /// screen reader: a tagged document, PDF/UA or a PDF/A of level A, then has
    /// the QR code as a figure of that description. Without one, it is
    /// decoration, which a screen reader skips.
    ///
    /// - Parameter altDescription: the description.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String?) -> QRCode {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Draws this barcode on the specified page. The dark modules next to each
    /// other in a row are filled as one rectangle, and the pen and the brush of
    /// the page are as they were after it.
    ///
    /// - Parameter page: the specified page.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let size = m1*Float(moduleCount)
        if let page = page {
            // Described, the QR code is a figure of a tagged document; not
            // described, its modules, which carry no text, are decoration.
            if let altDescription = altDescription, !altDescription.isEmpty {
                page.addBDC(StructElem.FIGURE, nil, nil, altDescription)
            } else {
                page.addArtifactBMC()
            }
            page.saveGraphicsState()
            page.setBrushColor(self.color)
            for row in 0..<moduleCount {
                var col = 0
                while col < moduleCount {
                    if !modules[row][col] {
                        col += 1
                        continue
                    }
                    let start = col
                    while col < moduleCount && modules[row][col] {
                        col += 1
                    }
                    page.fillRect(x + Float(start)*m1, y + Float(row)*m1, Float(col - start)*m1, m1)
                }
            }
            page.restoreGraphicsState()
            if let altDescription = altDescription, !altDescription.isEmpty {
                page.setFigureBoundingBox(x, y, size, size)
            }
            page.addEMC()
        }
        return [self.x + size, self.y + size]
    }

    /// Returns the modules of the QR code: true for dark and false for light modules.
    public func getModules() -> [[Bool?]]? {
        return modules.map { row in row.map { Optional($0) } }
    }

    // Places the function patterns and the codewords, and masks them with the
    // mask pattern of the lowest penalty, with its format information.
    func make(_ data: [UInt8]) {
        modules = QRCode.newMatrix(moduleCount)
        reserved = QRCode.newMatrix(moduleCount)

        setupPositionProbePattern(0, 0)
        setupPositionProbePattern(moduleCount - 7, 0)
        setupPositionProbePattern(0, moduleCount - 7)

        setupPositionAdjustPattern()
        setupTimingPattern()
        setupTypeInfo(&modules, 0)  // Reserves the modules of the format information
        if typeNumber >= 7 {
            setupTypeNumber()
        }
        mapData(data)

        // Each mask is tried with its format information in place, as ISO/IEC
        // 18004 asks, and the first of the lowest penalty is taken.
        var best: [[Bool]]? = nil
        var minLostPoint = 0
        for maskPattern in 0..<8 {
            let masked = applyMask(maskPattern)
            let lostPoint = qrutil.getLostPoint(masked)
            if best == nil || lostPoint < minLostPoint {
                minLostPoint = lostPoint
                best = masked
            }
        }
        modules = best!
        reserved = [[Bool]]()
    }

    // Returns a square matrix of light modules.
    static func newMatrix(_ moduleCount: Int) -> [[Bool]] {
        return [[Bool]](repeating: [Bool](repeating: false, count: moduleCount), count: moduleCount)
    }

    // Sets the module of a function pattern or of the format or version information.
    private func set(_ row: Int, _ col: Int, _ dark: Bool) {
        modules[row][col] = dark
        reserved[row][col] = true
    }

    // Returns the modules with the mask pattern applied to the codewords, and
    // the format information of the mask pattern.
    func applyMask(_ maskPattern: Int) -> [[Bool]] {
        var masked = modules
        for row in 0..<moduleCount {
            for col in 0..<moduleCount {
                if !reserved[row][col] && qrutil.getMask(maskPattern, row, col) {
                    masked[row][col] = !masked[row][col]
                }
            }
        }
        setupTypeInfo(&masked, maskPattern)
        return masked
    }

    // Places the bits of the codewords in the modules that are not reserved,
    // in two module wide columns from the bottom right corner, up and down in
    // turn; the modules left over are light.
    func mapData(_ data: [UInt8]) {
        var inc = -1
        var row = moduleCount - 1
        var bitIndex = 7
        var byteIndex = 0

        var col = moduleCount - 1
        while col > 0 {
            if col == 6 {
                col -= 1
            }
            while true {
                for c in 0..<2 {
                    if !reserved[row][col - c] {
                        var dark = false
                        if byteIndex < data.count {
                            dark = (((data[byteIndex] >> bitIndex) & 1) == 1)
                        }
                        modules[row][col - c] = dark
                        bitIndex -= 1
                        if (bitIndex == -1) {
                            byteIndex += 1
                            bitIndex = 7
                        }
                    }
                }

                row += inc
                if row < 0 || moduleCount <= row {
                    row -= inc
                    inc = -inc
                    break
                }
            }
            col -= 2
        }
    }

    func setupPositionAdjustPattern() {
        let pos = qrutil.getPatternPosition(typeNumber)
        for i in 0..<pos.count {
            for j in 0..<pos.count {
                let row = pos[i]
                let col = pos[j]
                if reserved[row][col] {
                    continue
                }
                for r in -2...2 {
                    for c in -2...2 {
                        set(row + r, col + c, r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0))
                    }
                }
            }
        }
    }

    func setupPositionProbePattern(_ row: Int, _ col: Int) {
        for r in -1...7 {
            for c in -1...7 {
                if (row + r <= -1 || moduleCount <= row + r ||
                        col + c <= -1 || moduleCount <= col + c) {
                    continue
                }
                set(row + r, col + c,
                        (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
                        (0 <= c && c <= 6 && (r == 0 || r == 6)) ||
                        (2 <= r && r <= 4 && 2 <= c && c <= 4))
            }
        }
    }

    func setupTimingPattern() {
        // The alignment patterns of versions 7 and up cross the timing patterns,
        // so the modules they already set are skipped.
        for r in 8..<(moduleCount - 8) where !reserved[r][6] {
            set(r, 6, r % 2 == 0)
        }
        for c in 8..<(moduleCount - 8) where !reserved[6][c] {
            set(6, c, c % 2 == 0)
        }
    }

    // Versions 7 and up carry their version number twice, next to two finder patterns.
    private func setupTypeNumber() {
        let bits = qrutil.getBCHTypeNumber(typeNumber)
        for i in 0..<18 {
            let dark = ((bits >> i) & 1) == 1
            set(i / 3, i % 3 + moduleCount - 8 - 3, dark)
            set(i % 3 + moduleCount - 8 - 3, i / 3, dark)
        }
    }

    // Places the format information, the error correction level and the mask
    // pattern, twice in the modules, and the dark module next to the bottom
    // left finder pattern. The modules it takes are reserved.
    func setupTypeInfo(
            _ matrix: inout [[Bool]],
            _ maskPattern: Int) {
        let data = (errorCorrectionLevel.rawValue << 3) | maskPattern
        let bits = qrutil.getBCHTypeInfo(data)

        for i in 0..<15 {
            let dark = ((bits >> i) & 1) == 1
            if i < 6 {
                matrix[i][8] = dark
                reserved[i][8] = true
            } else if i < 8 {
                matrix[i + 1][8] = dark
                reserved[i + 1][8] = true
            } else {
                matrix[moduleCount - 15 + i][8] = dark
                reserved[moduleCount - 15 + i][8] = true
            }
        }

        for i in 0..<15 {
            let dark = ((bits >> i) & 1) == 1
            if i < 8 {
                matrix[8][moduleCount - i - 1] = dark
                reserved[8][moduleCount - i - 1] = true
            } else if i < 9 {
                matrix[8][15 - i - 1 + 1] = dark
                reserved[8][15 - i - 1 + 1] = true
            } else {
                matrix[8][15 - i - 1] = dark
                reserved[8][15 - i - 1] = true
            }
        }

        matrix[moduleCount - 8][8] = true
        reserved[moduleCount - 8][8] = true
    }

    func createData(_ errorCorrectionLevel: ErrorCorrectionLevel) throws -> [UInt8] {
        let rsBlocks = RSBlock.getRSBlocks(typeNumber, errorCorrectionLevel)
        let buffer = BitBuffer()
        if eci {
            buffer.put(UInt32(7), 4)    // ECI
            buffer.put(UInt32(26), 8)   // UTF-8
        }
        buffer.put(UInt32(4), 4)        // Byte mode
        buffer.put(UInt32(qrData!.count), QRCode.getCharacterCountBits(typeNumber))
        for i in 0..<qrData!.count {
            buffer.put(UInt32(qrData![i]), 8)
        }

        var totalDataCount = 0
        for block in rsBlocks {
            totalDataCount += block.getDataCount()
        }

        if buffer.getLengthInBits() > totalDataCount * 8 {
            throw PDFjetError(message: "String length overflow. (" +
                    String(describing: buffer.getLengthInBits()) + ">" +
                    String(describing: (totalDataCount * 8)) + ")")
        }

        if buffer.getLengthInBits() + 4 <= totalDataCount * 8 {
            buffer.put(0, 4)
        }

        // padding
        while buffer.getLengthInBits() % 8 != 0 {
            buffer.put(false)
        }

        // padding
        while true {
            if buffer.getLengthInBits() >= totalDataCount * 8 {
                break
            }
            buffer.put(PAD0, 8)

            if buffer.getLengthInBits() >= totalDataCount * 8 {
                break
            }
            buffer.put(PAD1, 8)
        }

        return createBytes(buffer, rsBlocks)
    }

    private func createBytes(
            _ buffer: BitBuffer,
            _ rsBlocks: [RSBlock]) -> [UInt8] {
        var offset = 0
        var maxDcCount = 0
        var maxEcCount = 0

        var dcdata = [[Int]?](repeating: nil, count: rsBlocks.count)
        var ecdata = [[Int]?](repeating: nil, count: rsBlocks.count)

        for r in 0..<rsBlocks.count {
            let dcCount = rsBlocks[r].getDataCount()
            let ecCount = rsBlocks[r].getTotalCount() - dcCount

            maxDcCount = max(maxDcCount, dcCount)
            maxEcCount = max(maxEcCount, ecCount)

            dcdata[r] = [Int](repeating: 0, count: dcCount)
            for i in 0..<dcdata[r]!.count {
                dcdata[r]![i] = Int(buffer.getBuffer()![i + offset])
            }
            offset += dcCount

            let rsPoly = qrutil.getErrorCorrectPolynomial(ecCount)
            let rawPoly = Polynomial(dcdata[r]!, rsPoly.getLength() - 1)
            let modPoly = rawPoly.mod(rsPoly)
            ecdata[r] = [Int](repeating: 0, count: (rsPoly.getLength() - 1))
            for i in 0..<ecdata[r]!.count {
                let modIndex = i + modPoly.getLength() - ecdata[r]!.count
                ecdata[r]![i] = (modIndex >= 0) ? modPoly.get(modIndex) : 0
            }
        }

        var totalCodeCount = 0
        for block in rsBlocks {
            totalCodeCount += block.getTotalCount()
        }
        var data = [UInt8](repeating: 0, count: totalCodeCount)
        var index = 0
        for i in 0..<maxDcCount {
            for r in 0..<rsBlocks.count {
                if i < dcdata[r]!.count {
                    data[index] = UInt8(dcdata[r]![i])
                    index += 1
                }
            }
        }

        for i in 0..<maxEcCount {
            for r in 0..<rsBlocks.count {
                if i < ecdata[r]!.count {
                    data[index] = UInt8(ecdata[r]![i])
                    index += 1
                }
            }
        }

        return data
    }
}
