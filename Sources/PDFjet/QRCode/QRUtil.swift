/**
 * QRUtil.swift
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

class QRUtil {
    // The centers of the alignment patterns of each version, in rows and columns.
    private static let PATTERN_POSITION_TABLE: [[Int]] = [
        [],
        [6, 18],
        [6, 22],
        [6, 26],
        [6, 30],
        [6, 34],
        [6, 22, 38],
        [6, 24, 42],
        [6, 26, 46],
        [6, 28, 50],
        [6, 30, 54],
        [6, 32, 58],
        [6, 34, 62],
        [6, 26, 46, 66],
        [6, 26, 48, 70],
        [6, 26, 50, 74],
        [6, 30, 54, 78],
        [6, 30, 56, 82],
        [6, 30, 58, 86],
        [6, 34, 62, 90],
        [6, 28, 50, 72, 94],
        [6, 26, 50, 74, 98],
        [6, 30, 54, 78, 102],
        [6, 28, 54, 80, 106],
        [6, 32, 58, 84, 110],
        [6, 30, 58, 86, 114],
        [6, 34, 62, 90, 118],
        [6, 26, 50, 74, 98, 122],
        [6, 30, 54, 78, 102, 126],
        [6, 26, 52, 78, 104, 130],
        [6, 30, 56, 82, 108, 134],
        [6, 34, 60, 86, 112, 138],
        [6, 30, 58, 86, 114, 142],
        [6, 34, 62, 90, 118, 146],
        [6, 30, 54, 78, 102, 126, 150],
        [6, 24, 50, 76, 102, 128, 154],
        [6, 28, 54, 80, 106, 132, 158],
        [6, 32, 58, 84, 110, 136, 162],
        [6, 26, 54, 82, 110, 138, 166],
        [6, 30, 58, 86, 114, 142, 170],
    ]

    // The generator polynomial of the BCH(18, 6) code of the version information.
    private let G18 = (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0)

    private var G15: Int
    var G15_MASK: Int
    private static let qrmath = QRMath()
    private var qrmath: QRMath { return QRUtil.qrmath }

    init() {
        G15 = 1 << 10
        G15 |= 1 << 8
        G15 |= 1 << 5
        G15 |= 1 << 4
        G15 |= 1 << 2
        G15 |= 1 << 1
        G15 |= 1
        G15_MASK = 1 << 14
        G15_MASK |= 1 << 12
        G15_MASK |= 1 << 10
        G15_MASK |= 1 << 4
        G15_MASK |= 1 << 1
    }

    func getErrorCorrectPolynomial(_ errorCorrectLength: Int) -> Polynomial {
        var a = Polynomial([1], 0)
        for i in 0..<errorCorrectLength {
            a = a.multiply(Polynomial([1, qrmath.gexp(i)], 0))
        }
        return a
    }

    func getMask(
            _ maskPattern: Int,
            _ i: Int,
            _ j: Int) -> Bool {
        switch maskPattern {
            case MaskPattern.PATTERN000 : return (i + j) % 2 == 0
            case MaskPattern.PATTERN001 : return (i % 2) == 0
            case MaskPattern.PATTERN010 : return (j % 3) == 0
            case MaskPattern.PATTERN011 : return (i + j) % 3 == 0
            case MaskPattern.PATTERN100 : return (i / 2 + j / 3) % 2 == 0
            case MaskPattern.PATTERN101 : return (i * j) % 2 + (i * j) % 3 == 0
            case MaskPattern.PATTERN110 : return ((i * j) % 2 + (i * j) % 3) % 2 == 0
            case MaskPattern.PATTERN111 : return ((i * j) % 3 + (i + j) % 2) % 2 == 0
            default :
                fatalError("mask: " + String(describing: maskPattern))
        }
    }

    // Returns the penalty of the modules with the rules of ISO/IEC 18004
    // 7.8.3.1: N1 = 3 for five modules of a color in a row or a column, and 1
    // for each one more; N2 = 3 for each block of 2 by 2 modules of a color; N3
    // = 40 for each dark-light-dark-dark-dark-light-dark pattern of a row or a
    // column with 4 light modules before or after it, the light quiet zone
    // counting; and N4 = 10 for each 5% that the dark modules are away from
    // half the modules.
    func getLostPoint(_ modules: [[Bool]]) -> Int {
        let moduleCount = modules.count
        var lostPoint = 0
        // The module of the row and the column, or of the column and the row
        // across; outside the symbol is the quiet zone, which is light.
        func dark(_ across: Bool, _ i: Int, _ j: Int) -> Bool {
            if j < 0 || j >= moduleCount {
                return false
            }
            return across ? modules[j][i] : modules[i][j]
        }
        // Tells if the modules from j to k, not including k, are light.
        func isLight(_ across: Bool, _ i: Int, _ j: Int, _ k: Int) -> Bool {
            for n in j..<k where dark(across, i, n) {
                return false
            }
            return true
        }

        for across in [false, true] {
            for i in 0..<moduleCount {
                // N1
                var run = 1
                for j in 1...moduleCount {
                    if j < moduleCount && dark(across, i, j) == dark(across, i, j - 1) {
                        run += 1
                        continue
                    }
                    if run >= 5 {
                        lostPoint += 3 + run - 5
                    }
                    run = 1
                }
                // N3
                var j = 0
                while j + 6 < moduleCount {
                    if dark(across, i, j) &&
                            !dark(across, i, j + 1) &&
                            dark(across, i, j + 2) &&
                            dark(across, i, j + 3) &&
                            dark(across, i, j + 4) &&
                            !dark(across, i, j + 5) &&
                            dark(across, i, j + 6) &&
                            (isLight(across, i, j - 4, j) || isLight(across, i, j + 7, j + 11)) {
                        lostPoint += 40
                    }
                    j += 1
                }
            }
        }

        // N2
        for row in 0..<(moduleCount - 1) {
            for col in 0..<(moduleCount - 1) {
                let d = modules[row][col]
                if d == modules[row + 1][col] && d == modules[row][col + 1] && d == modules[row + 1][col + 1] {
                    lostPoint += 3
                }
            }
        }

        // N4
        var darkCount = 0
        for row in modules {
            for module in row where module {
                darkCount += 1
            }
        }
        let total = moduleCount * moduleCount
        lostPoint += abs(2 * darkCount - total) * 10 / total * 10

        return lostPoint
    }

    func getBCHTypeInfo(_ data: Int) -> Int {
        var d = data << 10
        while (getBCHDigit(d) - getBCHDigit(G15)) >= 0 {
            d ^= (G15 << (getBCHDigit(d) - getBCHDigit(G15)))
        }
        return ((data << 10) | d) ^ G15_MASK
    }

    func getPatternPosition(_ typeNumber: Int) -> [Int] {
        return QRUtil.PATTERN_POSITION_TABLE[typeNumber - 1]
    }

    func getBCHTypeNumber(_ data: Int) -> Int {
        var d = data << 12
        while (getBCHDigit(d) - getBCHDigit(G18)) >= 0 {
            d ^= (G18 << (getBCHDigit(d) - getBCHDigit(G18)))
        }
        return (data << 12) | d
    }

    func getBCHDigit(_ input: Int) -> Int {
        var data = input
        var digit = 0
        while data != 0 {
            digit += 1
            data >>= 1
        }
        return digit
    }
}
