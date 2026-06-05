/**
 * Chunk.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class Chunk {
    var length: UInt32?
    var type: [UInt8]?
    var data: [UInt8]?
    var crc: UInt32?

    func getData() -> [UInt8]? {
        return self.data
    }

    func setData(_ data: [UInt8]?) {
        self.data = data
    }

}   // End of Chunk.swift
