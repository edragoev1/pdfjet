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
import Foundation

/**
 * QRUtil
 * @author Kazuhiko Arase
 */
class QRUtil {
    static let singleton = QRUtil()

    private var G15: Int
    private var G15_MASK: Int

    private init() {
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
            a = a.multiply(Polynomial([1, QRMath.singleton.gexp(i)], 0))
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
                Swift.print("mask: " + String(describing: maskPattern))
        }
        return false
    }

    func getLostPoint(_ qrCode: QRCode) -> Int {
        let moduleCount = qrCode.getModuleCount()
        var lostPoint = 0

        // LEVEL1
        for row in 0..<moduleCount {
            for col in 0..<moduleCount {
                var sameCount = 0
                let dark = qrCode.isDark(row, col)

                for r in -1...1 {
                    if row + r < 0 || moduleCount <= row + r {
                        continue
                    }
                    for c in -1...1 {
                        if col + c < 0 || moduleCount <= col + c {
                            continue
                        }
                        if r == 0 && c == 0 {
                            continue
                        }
                        if dark == qrCode.isDark(row + r, col + c) {
                            sameCount += 1
                        }
                    }
                }
                if sameCount > 5 {
                    lostPoint += (3 + sameCount - 5)
                }
            }
        }

        // LEVEL2
        for row in 0..<(moduleCount - 1) {
            for col in 0..<(moduleCount - 1) {
                var count = 0
                if qrCode.isDark(row, col) {
                    count += 1
                }
                if qrCode.isDark(row + 1, col) {
                    count += 1
                }
                if qrCode.isDark(row, col + 1) {
                    count += 1
                }
                if qrCode.isDark(row + 1, col + 1) {
                    count += 1
                }
                if count == 0 || count == 4 {
                    lostPoint += 3
                }
            }
        }

        // LEVEL3
        for row in 0..<moduleCount {
            for col in 0..<(moduleCount - 6) {
                if qrCode.isDark(row, col)
                        && !qrCode.isDark(row, col + 1)
                        &&  qrCode.isDark(row, col + 2)
                        &&  qrCode.isDark(row, col + 3)
                        &&  qrCode.isDark(row, col + 4)
                        && !qrCode.isDark(row, col + 5)
                        &&  qrCode.isDark(row, col + 6) {
                    lostPoint += 40
                }
            }
        }

        for col in 0..<moduleCount {
            for row in 0..<(moduleCount - 6) {
                if qrCode.isDark(row, col)
                        && !qrCode.isDark(row + 1, col)
                        &&  qrCode.isDark(row + 2, col)
                        &&  qrCode.isDark(row + 3, col)
                        &&  qrCode.isDark(row + 4, col)
                        && !qrCode.isDark(row + 5, col)
                        &&  qrCode.isDark(row + 6, col) {
                    lostPoint += 40
                }
            }
        }

        // LEVEL4
        var darkCount = 0
        for col in 0..<moduleCount {
            for row in 0..<moduleCount {
                if qrCode.isDark(row, col) {
                    darkCount += 1
                }
            }
        }

        let ratio = abs(100 * darkCount / moduleCount / moduleCount - 50) / 5
        lostPoint += ratio * 10

        return lostPoint
    }

    func getBCHTypeInfo(_ data: Int) -> Int {
        var d = data << 10
        while (getBCHDigit(d) - getBCHDigit(G15)) >= 0 {
            d ^= (G15 << (getBCHDigit(d) - getBCHDigit(G15)))
        }
        return ((data << 10) | d) ^ G15_MASK
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
