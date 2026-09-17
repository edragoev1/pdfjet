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

// The tables never change after they are built, so one instance can be shared.
final class QRMath: Sendable {
    let EXP_TABLE: [Int]
    let LOG_TABLE: [Int]

    init() {
        var expTable = [Int](repeating: 0, count: 256)
        var logTable = [Int](repeating: 0, count: 256)
        for i in 0..<8 {
            expTable[i] = (1 << i)
        }
        for i in 8..<256 {
            expTable[i] =
                    expTable[i - 4] ^
                    expTable[i - 5] ^
                    expTable[i - 6] ^
                    expTable[i - 8]
        }
        for i in 0..<255 {
            logTable[expTable[i]] = i
        }
        self.EXP_TABLE = expTable
        self.LOG_TABLE = logTable
    }

    /// Returns the logarithm of n in GF(256).
    public func glog(_ n: Int) -> Int {
        if n < 1 {
            fatalError("log(" + String(describing: n) + ")")
        }
        return self.LOG_TABLE[n]
    }

    /// Returns 2 raised to the power of i in GF(256).
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
