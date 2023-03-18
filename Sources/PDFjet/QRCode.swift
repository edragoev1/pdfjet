/**
 *
Copyright 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/

///
/// Modified and adapted for use in PDFjet by Eugene Dragoev
///
import Foundation


///
/// Used to create 2D QR Code barcodes. Please see Example_21.
///
/// @author Kazuhiko Arase
///
public class QRCode : Drawable {

    private let PAD0: UInt32 = 0xEC
    private let PAD1: UInt32 = 0x11
    private var modules: [[Bool?]]?
    private var moduleCount = 33            // Magic Number
    private var errorCorrectLevel = ErrorCorrectLevel.M

    private var x: Float = 0.0
    private var y: Float = 0.0

    private var qrData: [UInt8]?
    private var m1: Float = 2.0             // Module length

    private var color: Int32 = Color.black


    ///
    /// Used to create 2D QR Code barcodes.
    ///
    /// @param str the string to encode.
    /// @param errorCorrectLevel the desired error correction level.
    /// @throws UnsupportedEncodingException
    ///
    public init(
            _ str: String,
            _ errorCorrectLevel: Int) {
        self.qrData = Array(str.utf8)
        self.errorCorrectLevel = errorCorrectLevel
        self.make(false, getBestMaskPattern())
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    ///
    /// Sets the location where this barcode will be drawn on the page.
    ///
    /// @param x the x coordinate of the top left corner of the barcode.
    /// @param y the y coordinate of the top left corner of the barcode.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> QRCode {
        self.x = x
        self.y = y
        return self
    }


    ///
    /// Sets the module length of this barcode.
    /// The default value is 2.0
    ///
    /// @param moduleLength the specified module length.
    ///
    @discardableResult
    public func setModuleLength(_ moduleLength: Float) -> QRCode {
        self.m1 = moduleLength
        return self
    }


    public func setColor(_ color: Int32) {
        self.color = color
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
        page!.setBrushColor(self.color)
        for row in 0..<modules!.count {
            for col in 0..<modules!.count {
                if isDark(row, col) {
                    page!.fillRect(x + Float(col)*m1, y + Float(row)*m1, m1, m1)
                }
            }
        }
        let w = m1*Float(modules!.count)
        let h = m1*Float(modules!.count)
        return [self.x + w, self.y + h]
    }

    public func getData() -> [[Bool?]]? {
        return self.modules
    }

    ///
    /// @param row the row.
    /// @param col the column.
    ///
    func isDark(_ row: Int, _ col: Int) -> Bool {
        if modules![row][col] != nil {
            return modules![row][col]!
        }
        return false
    }

    func getModuleCount() -> Int {
        return self.moduleCount
    }

    func getBestMaskPattern() -> Int {
        var minLostPoint = 0
        var pattern = 0
        for i in 0..<8 {
            make(true, i)
            let lostPoint = QRUtil.singleton.getLostPoint(self)
            if i == 0 || minLostPoint > lostPoint {
                minLostPoint = lostPoint
                pattern = i
            }
        }
        return pattern
    }

    func make(
            _ test: Bool,
            _ maskPattern: Int) {
        modules = [[Bool?]]()
        for _ in 0..<moduleCount {
            modules!.append([Bool?](repeating: nil, count: moduleCount))
        }

        setupPositionProbePattern(0, 0)
        setupPositionProbePattern(moduleCount - 7, 0)
        setupPositionProbePattern(0, moduleCount - 7)

        setupPositionAdjustPattern()
        setupTimingPattern()
        setupTypeInfo(test, maskPattern)
        mapData(createData(errorCorrectLevel), maskPattern)
    }

    private func mapData(
            _ data: [UInt8],
            _ maskPattern: Int) {
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
                    if modules![row][col - c] == nil {
                        var dark = false
                        if byteIndex < data.count {
                            dark = (((data[byteIndex] >> bitIndex) & 1) == 1)
                        }
                        let mask = QRUtil.singleton.getMask(maskPattern, row, col - c)
                        if mask {
                            dark = !dark
                        }
                        modules![row][col - c] = dark
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

    private func setupPositionAdjustPattern() {
        let pos = [6, 26]               // Magic Numbers
        for i in 0..<pos.count {
            for j in 0..<pos.count {
                let row = pos[i]
                let col = pos[j]

                if modules![row][col] != nil {
                    continue
                }

                var r = -2
                while r <= 2 {
                    var c = -2
                    while c <= 2 {
                        modules![row + r][col + c] =
                                r == -2 || r == 2 ||
                                c == -2 || c == 2 ||
                                (r == 0 && c == 0)
                        c += 1
                    }
                    r += 1
                }
            }
        }
    }

    private func setupPositionProbePattern(_ row: Int, _ col: Int) {
        for r in -1...7 {
            for c in -1...7 {
                if (row + r <= -1 || moduleCount <= row + r ||
                        col + c <= -1 || moduleCount <= col + c) {
                    continue
                }
                if (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
                        (0 <= c && c <= 6 && (r == 0 || r == 6)) ||
                        (2 <= r && r <= 4 && 2 <= c && c <= 4) {
                    modules![row + r][col + c] = true
                }
                else {
                    modules![row + r][col + c] = false
                }
            }
        }
    }

    private func setupTimingPattern() {
        var r = 8
        while r < (moduleCount - 8) {
            if modules![r][6] == nil {
                modules![r][6] = (r % 2 == 0)
                r += 1
            }
        }
        var c = 8
        while c < (moduleCount - 8) {
            if modules![6][c] == nil {
                modules![6][c] = (c % 2 == 0)
                c += 1
            }
        }
    }

    private func setupTypeInfo(
            _ test: Bool,
            _ maskPattern: Int) {
        let data = (errorCorrectLevel << 3) | maskPattern
        let bits = QRUtil.singleton.getBCHTypeInfo(data)

        for i in 0..<15 {
            let mod = (!test && ((bits >> i) & 1) == 1)
            if i < 6 {
                modules![i][8] = mod
            }
            else if i < 8 {
                modules![i + 1][8] = mod
            }
            else {
                modules![moduleCount - 15 + i][8] = mod
            }
        }

        for i in 0..<15 {
            let mod = (!test && ((bits >> i) & 1) == 1)
            if i < 8 {
                modules![8][moduleCount - i - 1] = mod
            }
            else if i < 9 {
                modules![8][15 - i - 1 + 1] = mod
            }
            else {
                modules![8][15 - i - 1] = mod
            }
        }

        modules![moduleCount - 8][8] = !test
    }

    private func createData(_ errorCorrectLevel: Int) -> [UInt8] {
        let rsBlocks = RSBlock.getRSBlocks(errorCorrectLevel)
        let buffer = BitBuffer()
        buffer.put(UInt32(4), 4)
        buffer.put(UInt32(qrData!.count), 8)
        for i in 0..<qrData!.count {
            buffer.put(UInt32(qrData![i]), 8)
        }

        var totalDataCount = 0
        for block in rsBlocks {
            totalDataCount += block.getDataCount()
        }

        if buffer.getLengthInBits() > totalDataCount * 8 {
            Swift.print("code length overflow. ( " +
                    String(describing: buffer.getLengthInBits()) + " > " +
                    String(describing: (totalDataCount * 8)) + " )")
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

            let rsPoly = QRUtil.singleton.getErrorCorrectPolynomial(ecCount)
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
