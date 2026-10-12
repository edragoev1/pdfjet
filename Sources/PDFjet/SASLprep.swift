/**
 * SASLprep.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Prepares a password as SASLprep, RFC 4013, does, which PDF 2.0 asks of the
/// password of a document encrypted with AES-256 (ISO 32000-2, 7.6.4.3.3,
/// algorithm 2.A): the non-ASCII spaces mapped to a space, the characters
/// "commonly mapped to nothing" left out, the text in normalization form KC,
/// and the prohibited characters and a mix of right to left and left to right
/// text refused. The password is a query in the words of stringprep, RFC 3454:
/// a character Unicode 3.2 did not assign is kept. The tables are those of
/// Unicode 3.2, which both RFCs fix, in SASLprepTables.swift. The work is done
/// on the code points, never on the Characters, which Swift compares in a
/// normalization of its own Unicode version.
///
enum SASLprep {
    // The tables, parsed once, when first used.
    private static let space = parse32(SASLprepTables.space)
    private static let nothing = parse32(SASLprepTables.nothing)
    private static let prohibited = parse32(SASLprepTables.prohibited)
    private static let randal = parse32(SASLprepTables.randal)
    private static let lcat = parse32(SASLprepTables.lcat)
    private static let ccc = parse32(SASLprepTables.ccc)
    private static let decompCodes = parse32(SASLprepTables.decompCodes)
    private static let decompStarts = parse32(SASLprepTables.decompStarts)
    private static let decompValues = parse32(SASLprepTables.decompValues)
    private static let composeKeys = parse64(SASLprepTables.composeKeys)
    private static let composeValues = parse32(SASLprepTables.composeValues)

    private static func parse64(_ table: [String]) -> [Int64] {
        var values = [Int64]()
        for chunk in table {
            for field in chunk.split(separator: " ") {
                values.append(Int64(field, radix: 16)!)
            }
        }
        return values
    }

    private static func parse32(_ table: [String]) -> [Int32] {
        return parse64(table).map { Int32($0) }
    }

    ///
    /// Returns the password prepared, or throws an error that names the
    /// character that SASLprep prohibits, or that says its text mixes right to
    /// left with left to right.
    ///
    static func prepare(_ password: String) throws -> String {
        var mapped = [Int32]()
        mapped.reserveCapacity(password.unicodeScalars.count)
        for scalar in password.unicodeScalars {
            let c = Int32(scalar.value)
            if inRanges(space, c) {
                mapped.append(0x20)
            } else if !inRanges(nothing, c) {
                mapped.append(c)
            }
        }
        let prepared = nfkc(mapped)
        var hasRandAL = false
        var hasL = false
        for c in prepared {
            if inRanges(prohibited, c) {
                throw PDFjetError(message:
                        "The password has the character U+\(hex4(c)), which PDF 2.0 does not allow in one.")
            }
            hasRandAL = hasRandAL || inRanges(randal, c)
            hasL = hasL || inRanges(lcat, c)
        }
        // Right to left text has no left to right character, and starts and
        // ends with a right to left one (RFC 3454, 6).
        if hasRandAL && (hasL || !inRanges(randal, prepared[0]) || !inRanges(randal, prepared[prepared.count - 1])) {
            throw PDFjetError(message:
                    "The password mixes right to left with left to right text, which PDF 2.0 does not allow in one.")
        }
        var result = String.UnicodeScalarView()
        for c in prepared {
            result.append(Unicode.Scalar(UInt32(c))!)
        }
        return String(result)
    }

    ///
    /// Returns the bytes of the password as PDF 2.0 asks of AES-256: prepared,
    /// in UTF-8, at most 127 of them.
    ///
    static func passwordBytes(_ password: String) throws -> [UInt8] {
        return truncate(try prepare(password))
    }

    ///
    /// Returns the first 127 UTF-8 bytes of the password, as PDF 2.0 asks,
    /// even when that cuts a character in two, as other readers and writers do.
    ///
    static func truncate(_ password: String) -> [UInt8] {
        return Array(password.utf8.prefix(127))
    }

    // Returns the code point as at least four hexadecimal digits, in capitals.
    private static func hex4(_ c: Int32) -> String {
        let digits = String(c, radix: 16, uppercase: true)
        return String(repeating: "0", count: max(0, 4 - digits.count)) + digits
    }

    // Returns the index of the first of count items for which the predicate
    // is true, the predicate false and then true over them, or count.
    private static func search(_ count: Int, _ predicate: (Int) -> Bool) -> Int {
        var low = 0
        var high = count
        while low < high {
            let middle = (low + high) / 2
            if predicate(middle) {
                high = middle
            } else {
                low = middle + 1
            }
        }
        return low
    }

    // Returns true if the character is in one of the ranges, first and last,
    // in order.
    private static func inRanges(_ ranges: [Int32], _ c: Int32) -> Bool {
        let n = ranges.count / 2
        let i = search(n) { ranges[2 * $0 + 1] >= c }
        return i < n && ranges[2 * i] <= c
    }

    // The Hangul syllables, which decompose and compose by arithmetic.
    private static let sBase: Int32 = 0xAC00
    private static let lBase: Int32 = 0x1100
    private static let vBase: Int32 = 0x1161
    private static let tBase: Int32 = 0x11A7
    private static let lCount: Int32 = 19
    private static let vCount: Int32 = 21
    private static let tCount: Int32 = 28
    private static let nCount: Int32 = vCount * tCount
    private static let sCount: Int32 = lCount * nCount

    // Returns the characters in normalization form KC: decomposed, the
    // combining marks of each run put in the order of their classes, and
    // composed again, as UAX #15 of Unicode 3.2 says.
    private static func nfkc(_ chars: [Int32]) -> [Int32] {
        var decomposed = [Int32]()
        decomposed.reserveCapacity(chars.count)
        for c in chars {
            appendDecomposition(&decomposed, c)
        }
        // The marks of each run of non-zero classes, in the order of their
        // classes, the marks of one class kept in the order they are: an
        // insertion sort, which is stable.
        var i = 0
        while i < decomposed.count {
            if combiningClass(decomposed[i]) == 0 {
                i += 1
                continue
            }
            var j = i
            while j < decomposed.count && combiningClass(decomposed[j]) != 0 {
                j += 1
            }
            if j - i > 1 {
                for k in (i + 1)..<j {
                    let mark = decomposed[k]
                    let markClass = combiningClass(mark)
                    var m = k
                    while m > i && combiningClass(decomposed[m - 1]) > markClass {
                        decomposed[m] = decomposed[m - 1]
                        m -= 1
                    }
                    decomposed[m] = mark
                }
            }
            i = j
        }
        return compose(decomposed)
    }

    // Appends the full compatibility decomposition of the character.
    private static func appendDecomposition(_ out: inout [Int32], _ c: Int32) {
        if c >= sBase && c < sBase + sCount {
            let s = c - sBase
            out.append(lBase + s / nCount)
            out.append(vBase + (s % nCount) / tCount)
            let t = s % tCount
            if t != 0 {
                out.append(tBase + t)
            }
            return
        }
        let codes = decompCodes
        let i = search(codes.count) { codes[$0] >= c }
        if i < codes.count && codes[i] == c {
            let values = decompValues
            for k in Int(decompStarts[i])..<Int(decompStarts[i + 1]) {
                out.append(values[k])
            }
            return
        }
        out.append(c)
    }

    // Returns the canonical combining class of the character.
    private static func combiningClass(_ c: Int32) -> Int32 {
        let classes = ccc
        let n = classes.count / 2
        let i = search(n) { classes[2 * $0] >= c }
        if i < n && classes[2 * i] == c {
            return classes[2 * i + 1]
        }
        return 0
    }

    // Returns what the pair composes to, or -1.
    private static func composeTo(_ first: Int32, _ second: Int32) -> Int32 {
        if first >= lBase && first < lBase + lCount && second >= vBase && second < vBase + vCount {
            return sBase + ((first - lBase) * vCount + (second - vBase)) * tCount
        }
        if first >= sBase && first < sBase + sCount && (first - sBase) % tCount == 0 &&
                second > tBase && second < tBase + tCount {
            return first + (second - tBase)
        }
        let key = Int64(first) * 0x110000 + Int64(second)
        let keys = composeKeys
        let i = search(keys.count) { keys[$0] >= key }
        if i < keys.count && keys[i] == key {
            return composeValues[i]
        }
        return -1
    }

    // Composes the decomposed characters, each with the last starter before
    // it when nothing between them blocks it, as the sample code of UAX #15
    // does: a mark is blocked by a mark of its class or a higher one before it.
    private static func compose(_ chars: [Int32]) -> [Int32] {
        if chars.isEmpty {
            return chars
        }
        var out = [chars[0]]
        var starter = 0     // The index in out of the last starter
        var lastClass = combiningClass(chars[0])
        if lastClass != 0 {
            lastClass = 256 // A mark first has no starter to compose with
        }
        for c in chars.dropFirst() {
            let charClass = combiningClass(c)
            let composite = composeTo(out[starter], c)
            if composite != -1 && (lastClass < charClass || lastClass == 0) {
                out[starter] = composite
                continue
            }
            if charClass == 0 {
                starter = out.count
            }
            lastClass = charClass
            out.append(c)
        }
        return out
    }
}
