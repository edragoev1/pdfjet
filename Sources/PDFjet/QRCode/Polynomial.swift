/**
 * Polynomial.swift
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

class Polynomial {
    private var num: [Int]
    private let qrmath = QRMath()

    init(_ num: [Int], _ shift: Int) {
        var offset = 0
        while offset < num.count && num[offset] == 0 {
            offset += 1
        }
        self.num = [Int](repeating: 0, count: num.count - offset + shift)
        for i in 0..<(num.count - offset) {
            self.num[i] = num[offset + i]
        }
    }

    func get(_ index: Int) -> Int {
        return self.num[index]
    }

    func getLength() -> Int {
        return self.num.count
    }

    func multiply(_ polynomial: Polynomial) -> Polynomial {
        var num = [Int](repeating: 0, count: ((getLength() + polynomial.getLength()) - 1))
        for i in 0..<getLength() {
            for j in 0..<polynomial.getLength() {
                num[i + j] ^=
                        qrmath.gexp(qrmath.glog(get(i)) +
                        qrmath.glog(polynomial.get(j)))
            }
        }
        return Polynomial(num, 0)
    }

    func mod(_ polynomial: Polynomial) -> Polynomial {
        if (getLength() - polynomial.getLength()) < 0 {
            return self
        }

        let ratio = qrmath.glog(get(0)) - qrmath.glog(polynomial.get(0))
        var num = [Int](repeating: 0, count: getLength())
        for i in 0..<getLength() {
            num[i] = get(i)
        }

        for i in 0..<polynomial.getLength() {
            num[i] ^= qrmath.gexp(qrmath.glog(polynomial.get(i)) + ratio)
        }

        return Polynomial(num, 0).mod(polynomial)
    }
}
