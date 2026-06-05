/**
 * QRMath.swift
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

class QRMath {
    var EXP_TABLE = [Int](repeating: 0, count: 256)
    var LOG_TABLE = [Int](repeating: 0, count: 256)

    init() {
        for i in 0..<8 {
            self.EXP_TABLE[i] = (1 << i)
        }
        for i in 8..<256 {
            self.EXP_TABLE[i] =
                    self.EXP_TABLE[i - 4] ^
                    self.EXP_TABLE[i - 5] ^
                    self.EXP_TABLE[i - 6] ^
                    self.EXP_TABLE[i - 8]
        }
        for i in 0..<255 {
            self.LOG_TABLE[self.EXP_TABLE[i]] = i
        }
    }

    public func glog(_ n: Int) -> Int {
        if n < 1 {
            Swift.print("log(" + String(describing: n) + ")")
        }
        return self.LOG_TABLE[n]
    }

    public func gexp(_ i: Int) -> Int {
        var n = i
        while n < 0 {
            n += 255
        }
        while n >= 256 {
            n -= 255
        }
        return self.EXP_TABLE[n]
    }
}
