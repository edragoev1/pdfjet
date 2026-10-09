/**
 * JPGScan.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Walks the scans of a sequential JPEG that has no end-of-image marker.
///
enum JPGScan {
    private final class Component {
        let id: Int
        let h: Int
        let v: Int
        var seen = false

        init(_ id: Int, _ h: Int, _ v: Int) {
            self.id = id
            self.h = h
            self.v = v
        }
    }

    // Returns whether the scans of a sequential JPEG that has no end-of-image
    // marker hold every block of its image: a JPEG whose only fault is the
    // missing marker, which viewers draw, rather than one cut short in its
    // upload or its copy. It reads the header from the start for the Huffman
    // tables, the restart interval and the components, and walks the
    // entropy-coded data of each scan, decoding the Huffman codes of each block
    // and passing over the bits of their values, without decoding the image.
    // Every step reads at least one bit, so the walk is no longer than the data.
    // A table, a scan or a header it cannot read makes the JPEG not whole.
    static func scansWhole(_ buffer: [UInt8]) -> Bool {
        var dc = [Huffman?](repeating: nil, count: 4)
        var ac = [Huffman?](repeating: nil, count: 4)
        var components: [Component]? = nil
        var width = 0
        var height = 0
        var maxH = 1
        var maxV = 1
        var restartInterval = 0
        var index = 2   // After the start of the image
        var scans = 0
        while true {
            guard let ch = marker(buffer, &index) else {
                // The data ends after a whole scan, where the end-of-image
                // marker would be: every component must have been in one.
                if scans == 0 {
                    return false
                }
                for c in components ?? [] where !c.seen {
                    return false
                }
                return true
            }
            if ch == 0x01 || ch == 0xD8 || (ch >= 0xD0 && ch <= 0xD7) {
                continue
            }
            if ch == 0xD9 {
                return scans > 0
            }
            if ch == 0xC0 || ch == 0xC1 {
                guard let data = segment(buffer, &index), data.count >= 6, components == nil else {
                    return false
                }
                height = Int(data[1]) << 8 | Int(data[2])
                width = Int(data[3]) << 8 | Int(data[4])
                let n = Int(data[5])
                if width == 0 || height == 0 || data.count < 6 + 3*n || n == 0 {
                    return false
                }
                var list = [Component]()
                for i in 0..<n {
                    let c = Component(Int(data[6 + 3*i]), Int(data[7 + 3*i] >> 4), Int(data[7 + 3*i] & 15))
                    if c.h < 1 || c.h > 4 || c.v < 1 || c.v > 4 {
                        return false
                    }
                    maxH = max(maxH, c.h)
                    maxV = max(maxV, c.v)
                    list.append(c)
                }
                components = list
            } else if ch == 0xC4 {  // Define Huffman tables
                guard let data = segment(buffer, &index) else {
                    return false
                }
                var offset = 0
                while offset < data.count {
                    if data.count - offset < 17 {
                        return false
                    }
                    let tableClass = Int(data[offset] >> 4)
                    let id = Int(data[offset] & 15)
                    var count = 0
                    for i in 1...16 {
                        count += Int(data[offset + i])
                    }
                    if tableClass > 1 || id > 3 || count > 256 || data.count - offset < 17 + count {
                        return false
                    }
                    guard let table = Huffman(Array(data[(offset + 1)..<(offset + 17)]),
                            Array(data[(offset + 17)..<(offset + 17 + count)])) else {
                        return false
                    }
                    if tableClass == 0 {
                        dc[id] = table
                    } else {
                        ac[id] = table
                    }
                    offset += 17 + count
                }
            } else if ch == 0xDD {  // Define restart interval
                guard let data = segment(buffer, &index), data.count >= 2 else {
                    return false
                }
                restartInterval = Int(data[0]) << 8 | Int(data[1])
            } else if ch == 0xDA {
                guard let data = segment(buffer, &index), let all = components, data.count >= 1 else {
                    return false
                }
                let n = Int(data[0])
                if n < 1 || n > 4 || data.count < 1 + 2*n + 3 {
                    return false
                }
                var parts = [(c: Component, dc: Huffman, ac: Huffman)]()
                for i in 0..<n {
                    let id = Int(data[1 + 2*i])
                    let tables = Int(data[2 + 2*i])
                    guard let c = all.last(where: { $0.id == id }),
                            tables >> 4 <= 3, tables & 15 <= 3,
                            let dcTable = dc[tables >> 4], let acTable = ac[tables & 15] else {
                        return false
                    }
                    parts.append((c, dcTable, acTable))
                }
                // The blocks of each unit of the scan, and the units across
                // and down: an interleaved scan's unit is an MCU, the blocks
                // of each component by its sampling; a scan of one component
                // has a unit of one block.
                let units: Int
                if n == 1 {
                    let c = parts[0].c
                    let w = ceilDiv(ceilDiv(width*c.h, maxH), 8)
                    let h = ceilDiv(ceilDiv(height*c.v, maxV), 8)
                    units = w*h
                } else {
                    units = ceilDiv(width, 8*maxH)*ceilDiv(height, 8*maxV)
                }
                let reader = BitReader(buffer, index)
                for u in 0..<units {
                    if restartInterval > 0 && u > 0 && u % restartInterval == 0 && !reader.restart() {
                        return false
                    }
                    for p in parts {
                        let blocks = n > 1 ? p.c.h*p.c.v : 1
                        for _ in 0..<blocks {
                            if !reader.block(p.dc, p.ac) {
                                return false
                            }
                        }
                    }
                }
                for p in parts {
                    p.c.seen = true
                }
                scans += 1
                index = reader.index
            } else {
                // SOF2 and the other frames are not walked: the caller asks only
                // of a sequential JPEG.
                if ch >= 0xC0 && ch <= 0xCF && ch != 0xC4 && ch != 0xC8 && ch != 0xCC {
                    return false
                }
                if segment(buffer, &index) == nil {
                    return false
                }
            }
        }
    }

    private static func ceilDiv(_ a: Int, _ b: Int) -> Int {
        return (a + b - 1)/b
    }

    // The next marker after the index, or nil at the end of the data.
    private static func marker(_ buffer: [UInt8], _ index: inout Int) -> UInt8? {
        while index < buffer.count {
            if buffer[index] != 0xFF {
                index += 1
                continue
            }
            while index < buffer.count && buffer[index] == 0xFF {
                index += 1
            }
            if index >= buffer.count {
                return nil
            }
            let ch = buffer[index]
            index += 1
            if ch != 0x00 {
                return ch
            }
        }
        return nil
    }

    // The parameter segment at the index, without its length, or nil.
    private static func segment(_ buffer: [UInt8], _ index: inout Int) -> [UInt8]? {
        if index + 2 > buffer.count {
            return nil
        }
        let length = Int(buffer[index]) << 8 | Int(buffer[index + 1])
        if length < 2 || index + length > buffer.count {
            return nil
        }
        let data = Array(buffer[(index + 2)..<(index + length)])
        index += length
        return data
    }

    // A Huffman table of a JPEG, as the JPEG standard decodes it (Annex
    // F.2.2.3): for each length, the smallest and the largest code, and where
    // its symbols begin.
    private final class Huffman {
        var minCode = [Int](repeating: 0, count: 17)
        var maxCode = [Int](repeating: 0, count: 17)
        var valPtr = [Int](repeating: 0, count: 17)
        let symbols: [UInt8]

        init?(_ counts: [UInt8], _ symbols: [UInt8]) {
            self.symbols = symbols
            var code = 0
            var k = 0
            for length in 1...16 {
                let n = Int(counts[length - 1])
                valPtr[length] = k
                minCode[length] = code
                code += n
                k += n
                maxCode[length] = n == 0 ? -1 : code - 1
                if code > 1 << length {
                    return nil  // More codes than the length has
                }
                code <<= 1
            }
        }
    }

    // Reads the entropy-coded data of a scan a bit at a time: a 0xFF is
    // followed by a 0x00 that is not data, and any other marker ends the data,
    // which a block that needs more bits does not survive.
    private final class BitReader {
        let buffer: [UInt8]
        var index: Int
        var bits = 0
        var count = 0

        init(_ buffer: [UInt8], _ index: Int) {
            self.buffer = buffer
            self.index = index
        }

        // The next bit, or nil at a marker or the end of the data.
        func bit() -> Int? {
            if count == 0 {
                if index >= buffer.count {
                    return nil
                }
                let b = buffer[index]
                if b == 0xFF {
                    if index + 1 >= buffer.count || buffer[index + 1] != 0x00 {
                        return nil  // A marker, or the end of the data
                    }
                    index += 1
                }
                index += 1
                bits = Int(b)
                count = 8
            }
            count -= 1
            return (bits >> count) & 1
        }

        func skip(_ n: Int) -> Bool {
            var n = n
            while n > 0 {
                if bit() == nil {
                    return false
                }
                n -= 1
            }
            return true
        }

        // The symbol of the next code, or nil.
        func decode(_ h: Huffman) -> Int? {
            var code = 0
            for length in 1...16 {
                guard let b = bit() else {
                    return nil
                }
                code = code << 1 | b
                if code <= h.maxCode[length] {
                    let k = h.valPtr[length] + code - h.minCode[length]
                    if k < 0 || k >= h.symbols.count {
                        return nil
                    }
                    return Int(h.symbols[k])
                }
            }
            return nil
        }

        // Passes over the codes of one block of 64 coefficients.
        func block(_ dc: Huffman, _ ac: Huffman) -> Bool {
            guard let s = decode(dc), s <= 11, skip(s) else {
                return false
            }
            var k = 1
            while k < 64 {
                guard let rs = decode(ac) else {
                    return false
                }
                let run = rs >> 4
                let size = rs & 15
                if size == 0 {
                    if run != 15 {
                        return true     // The end of the block
                    }
                    k += 16
                    continue
                }
                k += run
                if k > 63 || !skip(size) {
                    return false
                }
                k += 1
            }
            return true
        }

        // Goes past the restart marker that ends an interval, the bits left in
        // the byte before it being fill.
        func restart() -> Bool {
            count = 0
            while index + 1 < buffer.count && buffer[index] == 0xFF && buffer[index + 1] == 0xFF {
                index += 1  // A fill byte before the marker
            }
            if index + 1 >= buffer.count || buffer[index] != 0xFF ||
                    buffer[index + 1] < 0xD0 || buffer[index + 1] > 0xD7 {
                return false
            }
            index += 2
            return true
        }
    }
}   // End of JPGScan.swift
