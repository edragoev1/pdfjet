/**
 * GlyphMap.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// Maps the characters to the glyphs of a font, as its cmap table does: those of
// the Basic Multilingual Plane in two levels, 256 blocks of 256 characters, a
// block made only where the font has a character in it, and the characters past
// U+FFFF, as the Chinese characters of Hong Kong that IBM Plex Sans TC has, in a
// dictionary of their own. A font of 1,000 characters in
// a few blocks takes some kilobytes, where an array of an Int for each of the
// 65,536 characters took 512 KB for every font of every document; a lookup is
// two reads of an array still. The blocks are kept one after another in one
// array, and the index of each, or -1, in another, so that a copy of the map
// in a local constant, as the hot loops make one, copies two references.
struct GlyphMap {
    private var index = [Int16](repeating: -1, count: 256)
    private var blocks = [Int32]()
    // The characters past U+FFFF
    private(set) var astral = [Int: Int32]()

    // The most characters past U+FFFF a map takes, a font has a glyph for at
    // most 65,535: a font of groups of a million characters each is read no
    // further.
    static let maxAstral = 0x10000

    // Returns the glyph of the character, or 0 for one the font does not have.
    @inline(__always)
    subscript(c: Int) -> Int {
        if c < 0 || c > 0x10FFFF {
            return 0
        }
        if c > 0xFFFF {
            return Int(astral[c] ?? 0)
        }
        let block = Int(index[c >> 8])
        if block < 0 {
            return 0
        }
        return Int(blocks[block << 8 | (c & 0xFF)])
    }

    // Sets the glyph of the character. The map is changed in place: a font's
    // own map is not shared while it is read, so no copy is made.
    mutating func set(_ c: Int, _ gid: Int) {
        if c > 0xFFFF {
            setAstral(c, gid, ifAbsent: false)
            return
        }
        var block = Int(index[c >> 8])
        if block < 0 {
            if gid == 0 {
                return
            }
            block = blocks.count >> 8
            index[c >> 8] = Int16(block)
            blocks.append(contentsOf: repeatElement(Int32(0), count: 256))
        }
        blocks[block << 8 | (c & 0xFF)] = Int32(gid)
    }

    // Returns, by glyph number, true for a glyph the font draws for more than
    // one character, as one glyph for U+3000 and U+2003: only such a glyph
    // needs the character it was drawn for recorded, for the ToUnicode map.
    // The arrays go to the last glyph of a character, not to 65,536 glyphs.
    func sharedGlyphs() -> [Bool] {
        let count = Int(min(blocks.max() ?? 0, 0xFFFF)) + 1
        var seen = [Bool](repeating: false, count: count)
        var shared = [Bool](repeating: false, count: count)
        let astralMax = Int(min(astral.values.max() ?? 0, 0xFFFF))
        let total = max(count, astralMax + 1)
        if total > count {
            seen.append(contentsOf: repeatElement(false, count: total - count))
            shared.append(contentsOf: repeatElement(false, count: total - count))
        }
        for gid in blocks where gid > 0 && gid < total {
            if seen[Int(gid)] {
                shared[Int(gid)] = true
            } else {
                seen[Int(gid)] = true
            }
        }
        for gid in astral.values where gid > 0 && gid < total {
            if seen[Int(gid)] {
                shared[Int(gid)] = true
            } else {
                seen[Int(gid)] = true
            }
        }
        return shared
    }

    // Sets the glyph of the character unless it has one: as get and then
    // set, with one lookup of its block, for the format 12 subtable of a
    // font, which maps again the characters its format 4 subtable mapped
    // (12 October 2026).
    mutating func setIfAbsent(_ c: Int, _ gid: Int) {
        if c > 0xFFFF {
            setAstral(c, gid, ifAbsent: true)
            return
        }
        var block = Int(index[c >> 8])
        if block < 0 {
            if gid == 0 {
                return
            }
            block = blocks.count >> 8
            index[c >> 8] = Int16(block)
            blocks.append(contentsOf: repeatElement(Int32(0), count: 256))
        }
        if blocks[block << 8 | (c & 0xFF)] == 0 {
            blocks[block << 8 | (c & 0xFF)] = Int32(gid)
        }
    }

    // Maps a character past U+FFFF to the glyph, or only if it has none yet,
    // within maxAstral characters.
    private mutating func setAstral(_ c: Int, _ gid: Int, ifAbsent: Bool) {
        if c > 0x10FFFF {
            return
        }
        if astral[c] != nil {
            if !ifAbsent {
                astral[c] = Int32(gid)
            }
            return
        }
        if gid != 0 && astral.count < GlyphMap.maxAstral {
            astral[c] = Int32(gid)
        }
    }

    // Calls the function for each character that has a glyph, in the order
    // of the characters.
    func forEach(_ body: (_ c: Int, _ gid: Int) -> Void) {
        for b in 0..<256 {
            let block = Int(index[b])
            if block < 0 {
                continue
            }
            for i in 0..<256 {
                let gid = Int(blocks[block << 8 | i])
                if gid != 0 {
                    body(b << 8 | i, gid)
                }
            }
        }
        for c in astral.keys.sorted() {
            if let gid = astral[c], gid != 0 {
                body(c, Int(gid))
            }
        }
    }

    // Folds each block of 256 characters the font has glyphs in into the
    // hash, its number and the glyph of each character, 0 for those the font
    // has not; the blocks it has none in are left out.
    func fold(_ hash: UInt64, _ step: (UInt64, Int) -> UInt64) -> UInt64 {
        var hash = hash
        for b in 0..<256 {
            let block = Int(index[b])
            if block < 0 {
                continue
            }
            hash = step(hash, b)
            for i in 0..<256 {
                hash = step(hash, Int(blocks[block << 8 | i]))
            }
        }
        return hash
    }
}
