/**
 * BitBuffer.swift
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

class BitBuffer {
    private var buffer: [UInt8]
    private var length = 0
    private var increments = 32

    public init() {
        buffer = [UInt8](repeating: 0, count: increments)
    }

    public func getBuffer() -> [UInt8]? {
        return self.buffer
    }

    public func getLengthInBits() -> Int {
        return self.length
    }

    public func put(_ num: UInt32, _ length: Int) {
        for i in 0..<length {
            put(((num >> (length - i - 1)) & 1) == 1)
        }
    }

    public func put(_ bit: Bool) {
        if length == buffer.count * 8 {
            var newBuffer = [UInt8](repeating: 0, count: (buffer.count + increments))
            for i in 0..<buffer.count {
                newBuffer[i] = buffer[i]
            }
            buffer = newBuffer
        }
        if bit {
            buffer[length / 8] |= (UInt8(0x80) >> (length % 8))
        }
        length += 1
    }
}
