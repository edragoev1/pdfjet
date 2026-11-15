/**
 * CFFSubsetTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The tests of the subsets of the fonts with CFF outlines.
@Suite struct CFFSubsetTests {
    static let plex = "tests/fonts/IBMPlexSans-Regular.otf"
    static let han = "tests/fonts/SourceHanSansJP-Regular.otf"

    private func otf(_ path: String) throws -> OTF {
        return try OTF(InputStream(data: try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(path)))))
    }

    private func cff(_ otf: OTF) -> [UInt8] {
        return Array(otf.buf[otf.cffOff!..<(otf.cffOff! + otf.cffLen!)])
    }

    // The charstrings, the global subroutines and the Top DICT of a CFF table.
    private func parts(_ cff: [UInt8]) throws -> ([[UInt8]], [[UInt8]], [CFFSubset.Entry]) {
        let names = try CFFSubset.readIndex(cff, Int(cff[2]))
        let tops = try CFFSubset.readIndex(cff, names.end)
        let strings = try CFFSubset.readIndex(cff, tops.end)
        let globals = try CFFSubset.readIndex(cff, strings.end)
        let top = try CFFSubset.readDict(cff, tops.objects[0], tops.objects[1])
        let charStrings = try CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.charStrings)![0])
        return (CFFSubset.emptied(cff, charStrings, nil), CFFSubset.emptied(cff, globals, nil), top)
    }

    private func used(_ otf: OTF, _ text: String) -> [Bool] {
        var used = [Bool](repeating: false, count: 0x10000)
        for scalar in text.unicodeScalars {
            used[otf.unicodeToGID[Int(scalar.value)]] = true
        }
        return used
    }

    // The CIDs the charset of the subset gives its glyphs after .notdef, of
    // formats 0, 1 and 2.
    private func charsetCIDs(_ cff: [UInt8], _ top: [CFFSubset.Entry], _ glyphs: Int) -> [Int] {
        var at = CFFSubset.entryOf(top, CFFSubset.charset)![0]
        let format = cff[at]
        at += 1
        var cids = [Int]()
        while cids.count < glyphs - 1 {
            switch format {
            case 0:
                cids.append(Int(cff[at]) << 8 | Int(cff[at + 1]))
                at += 2
            case 1, 2:
                let first = Int(cff[at]) << 8 | Int(cff[at + 1])
                var left = Int(cff[at + 2])
                at += 3
                if format == 2 {
                    left = left << 8 | Int(cff[at])
                    at += 1
                }
                cids.append(contentsOf: first...(first + left))
            default:
                Issue.record("charset format \(format)")
                return cids
            }
        }
        return cids
    }

    @Test func isACIDFontOfTheGlyphsUsedAlone() throws {
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let font = try otf(path)
            let table = cff(font)
            let used = used(font, "Hello 日本語")
            let (subset, kept) = try CFFSubset.subset(table, used)
            let (whole, _, _) = try parts(table)
            let (glyphs, globals, top) = try parts(subset)
            try #require(kept.count == whole.count, "\(path): kept is of \(kept.count) glyphs")
            // The glyphs kept, .notdef and those used, alone, in their order
            var keptGIDs = [Int]()
            for gid in 0..<kept.count {
                #expect(kept[gid] == (gid == 0 || used[gid]), "\(path) glyph \(gid)")
                if kept[gid] && gid > 0 {
                    keptGIDs.append(gid)
                }
            }
            try #require(glyphs.count == keptGIDs.count + 1, "\(path): \(glyphs.count) glyphs")
            // A CIDFont, whose charset gives each glyph its number in the font
            // as its CID, which the content streams draw it by
            #expect(CFFSubset.entryOf(top, CFFSubset.ros) != nil && CFFSubset.entryOf(top, CFFSubset.fdArray) != nil,
                    "\(path): the subset is not a CIDFont")
            #expect(CFFSubset.entryOf(top, CFFSubset.encoding) == nil, "\(path): the subset has an Encoding")
            #expect(charsetCIDs(subset, top, glyphs.count) == keptGIDs, "\(path): the CIDs")
            // No subroutines: the glyphs are written with them in place, and
            // drawn as in the whole font (withItsSubroutinesInPlaceDrawsThe
            // SameOutlines)
            #expect(globals.isEmpty, "\(path): \(globals.count) global subroutines")
            #expect(subset.count <= table.count / 10, "\(path): \(subset.count) bytes of \(table.count)")
        }
    }

    @Test func ofEveryGlyphHasTheIdentityCharset() throws {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID:
        // written whole, each glyph's CID is its number.
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let table = cff(try otf(path))
            let (whole, _, _) = try parts(table)
            let (subset, kept) = try CFFSubset.subset(table, nil)
            let (glyphs, _, top) = try parts(subset)
            try #require(glyphs.count == whole.count && kept.count == whole.count, "\(path): \(glyphs.count) glyphs")
            #expect(charsetCIDs(subset, top, glyphs.count) == Array(1..<glyphs.count), "\(path): the CIDs")
            #expect(glyphs == whole, "\(path)")
        }
    }

    private func document(_ compliance: Compliance, _ path: String, _ subset: Bool, _ text: String) throws -> String {
        let memory = MemoryPDF(compliance)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.path(path))
        font.setSubset(subset)
        TextLine(font, text).setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    private func tagged(_ raw: String) -> Bool {
        return raw.range(of: "/FontName /[A-Z]{6}\\+", options: .regularExpression) != nil
    }

    @Test func aFontWithCFFOutlinesIsEmbeddedAsASubset() throws {
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let raw = try document(Compliance.PDF_A_1B, path, true, "Hello 日本語")
            #expect(tagged(raw), "\(path)")
            #expect(raw.contains("/Subtype /CIDFontType0C\n") && raw.contains("/FontFile3 "), "\(path)")
            #expect(!raw.contains("/Length1 "), "\(path)")
            #expect(raw.contains("/CIDSet "), "\(path)")
        }
        let raw = try document(Compliance.PDF_1_7, CFFSubsetTests.plex, false, "Hello")
        #expect(!tagged(raw) && raw.contains("/FontFile3 "))
    }

    @Test func aFontWhoseLicenseForbidsSubsettingIsEmbeddedWhole() throws {
        var data = [UInt8](try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(CFFSubsetTests.plex))))
        let tables = Int(data[4]) << 8 | Int(data[5])
        for i in 0..<tables {
            let entry = 12 + 16 * i
            if TestSupport.latin1(Array(data[entry..<(entry + 4)])) == "OS/2" {
                let at = Int(data[entry + 8]) << 24 | Int(data[entry + 9]) << 16 |
                        Int(data[entry + 10]) << 8 | Int(data[entry + 11])
                data[at + 8] = 0x01
                data[at + 9] = 0x00
            }
        }
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, InputStream(data: Data(data)))
        TextLine(font, "Hello").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        #expect(!tagged(TestSupport.latin1(memory.bytes)))
    }

    @Test func anAccentedLetterDrawnWithSeacIsRefused() throws {
        // Á's charstring made "0 0 65 194 endchar", seac: the A and the acute
        // it is drawn from were emptied, and it drew blank (the review of
        // 9 October 2026). The font is embedded whole.
        let font = try otf(CFFSubsetTests.plex)
        var table = cff(font)
        let names = try CFFSubset.readIndex(table, Int(table[2]))
        let tops = try CFFSubset.readIndex(table, names.end)
        let top = try CFFSubset.readDict(table, tops.objects[0], tops.objects[1])
        let charStrings = try CFFSubset.readIndex(table, CFFSubset.entryOf(top, CFFSubset.charStrings)![0])
        let gid = font.unicodeToGID[Int(("Á" as Unicode.Scalar).value)]
        let at = charStrings.objects[gid]
        try #require(charStrings.objects[gid + 1] - at >= 6, "the charstring of Á is too short")
        table.replaceSubrange(at..<(at + 6), with: [139, 139, 204, 247, 86, 14])
        #expect(throws: Subset.NotSubset.self) {
            _ = try CFFSubset.subset(table, used(font, "Á"))
        }
    }

    // The CFF table of a name-keyed font with its Top DICT written again with
    // the entries, before its own, and its offsets moved by as many bytes.
    private func withTopDict(_ cff: [UInt8], _ extra: [CFFSubset.Entry]) throws -> [UInt8] {
        let names = try CFFSubset.readIndex(cff, Int(cff[2]))
        let tops = try CFFSubset.readIndex(cff, names.end)
        let top = try CFFSubset.readDict(cff, tops.objects[0], tops.objects[1])
        func written(_ delta: Int) -> [UInt8] {
            var offsets = [Int: Int]()
            var privateSize = 0
            for entry in top {
                switch entry.op {
                case CFFSubset.charset, CFFSubset.charStrings:
                    offsets[entry.op] = entry.integers![0] + delta
                case CFFSubset.privateOp:
                    privateSize = entry.integers![0]
                    offsets[entry.op] = entry.integers![1] + delta
                default:
                    break
                }
            }
            return CFFSubset.writeDict(extra + top, offsets, privateSize)
        }
        let delta = CFFSubset.index([written(0)]).count - (tops.end - tops.start)
        var out = Array(cff[0..<tops.start])
        CFFSubset.appendIndex(&out, [written(delta)])
        out.append(contentsOf: cff[tops.end...])
        return out
    }

    // The FontMatrix entries of the Top DICT and of the one Font DICT.
    private func matrices(_ cff: [UInt8]) throws -> [[UInt8]] {
        let (_, _, top) = try parts(cff)
        var raws = top.filter { $0.op == CFFSubset.fontMatrix }.map { $0.raw }
        let fdArray = try CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.fdArray)![0])
        try #require(fdArray.count == 1, "not one Font DICT")
        let fd = try CFFSubset.readDict(cff, fdArray.objects[0], fdArray.objects[1])
        raws += fd.filter { $0.op == CFFSubset.fontMatrix }.map { $0.raw }
        return raws
    }

    @Test func ofANameKeyedFontWithAFontMatrixOfItsOwn() throws {
        // As Cascadia Mono's .otf, of 2048 units to the em, has: the matrix
        // stays in the Top DICT, and the Font DICT has the identity matrix,
        // the two concatenated the font's.
        let font = try otf(CFFSubsetTests.plex)
        let table = cff(font)
        let matrix: [UInt8] = [30, 0xa0, 0x00, 0x48, 0x82, 0x8f]     // .00048828
        let raw = matrix + [139, 139] + matrix + [139, 139, 12, 7]
        let withMatrix = try withTopDict(table, [CFFSubset.Entry(op: CFFSubset.fontMatrix, raw: raw, integers: nil)])
        let (whole, _, _) = try parts(withMatrix)
        let want: [[UInt8]] = [raw, [140, 139, 139, 140, 139, 139, 12, 7]]
        var subset = [UInt8]()
        for used in [used(font, "Hello"), nil] {
            (subset, _) = try CFFSubset.subset(withMatrix, used)
            let (glyphs, _, _) = try parts(subset)
            if used == nil {
                #expect(glyphs.count == whole.count, "\(glyphs.count) glyphs")
            }
            #expect(try matrices(subset) == want)
        }
        // A CID-keyed font's matrices are kept as they are: the subset's
        // subset has them both again.
        let (again, _) = try CFFSubset.subset(subset, used(font, "He"))
        #expect(try matrices(again) == want, "the matrices of a CID-keyed font")
        // A name-keyed font of no FontMatrix, as IBM Plex Sans, has the
        // identity matrix in its Font DICT too, and the default written in
        // its Top DICT: no reader takes the default of 1000 units to the em
        // twice, or the Font DICT's matrix as the whole one.
        for used in [used(font, "Hello"), nil] {
            (subset, _) = try CFFSubset.subset(table, used)
            #expect(try matrices(subset) == [CFFSubset.thousandthMatrix, CFFSubset.identityMatrix])
        }
    }

    // The outline of each glyph of the CFF table, the points of its path
    // operators in order, drawn by its Type 2 charstring run with its
    // subroutines.
    private func outlines(_ cff: [UInt8]) throws -> [String] {
        let (charStrings, globals, top) = try parts(cff)
        var locals: [[[UInt8]]?] = [nil]
        var fdOf: [Int]?
        if let at = CFFSubset.entryOf(top, CFFSubset.fdArray) {
            let fdArray = try CFFSubset.readIndex(cff, at[0])
            locals = []
            for i in 0..<fdArray.count {
                let fd = try CFFSubset.readDict(cff, fdArray.objects[i], fdArray.objects[i + 1])
                locals.append(try localsOf(cff, CFFSubset.entryOf(fd, CFFSubset.privateOp)!))
            }
            let at = CFFSubset.entryOf(top, CFFSubset.fdSelect)![0]
            let length = try CFFSubset.fdSelectLength(cff, at, charStrings.count)
            fdOf = try CFFSubset.fdOf(Array(cff[at..<(at + length)]), charStrings.count, locals.count)
        } else {
            locals[0] = try localsOf(cff, CFFSubset.entryOf(top, CFFSubset.privateOp)!)
        }
        var outlines = [String]()
        for (gid, cs) in charStrings.enumerated() {
            let p = Type2(globals, locals[fdOf?[gid] ?? 0] ?? [])
            p.run(cs, 0)
            outlines.append(p.path)
        }
        return outlines
    }

    // The local subroutines of the Private DICT.
    private func localsOf(_ cff: [UInt8], _ privateEntry: [Int]) throws -> [[UInt8]]? {
        let block = try CFFSubset.readPrivate(cff, privateEntry[0], privateEntry[1])
        guard let subrs = block.subrs else {
            return nil
        }
        return CFFSubset.emptied(cff, subrs, nil)
    }

    /// Draws a Type 2 charstring (Technical Note #5177) as the points of its
    /// path, apart from the subsetter's code.
    final class Type2 {
        let globals: [[UInt8]]
        let locals: [[UInt8]]
        var stack = [Double]()
        var x = 0.0
        var y = 0.0
        var stems = 0
        var width = false   // Looked for already
        var ended = false
        var path = ""

        init(_ globals: [[UInt8]], _ locals: [[UInt8]]) {
            self.globals = globals
            self.locals = locals
        }

        func point(_ op: String, _ dx: Double, _ dy: Double) {
            x += dx
            y += dy
            path += "\(op) \(x) \(y) "
        }

        // Takes the width off the stack, the first time, when it has more
        // operands than even, for even, or than n.
        func args(_ even: Bool, _ n: Int) {
            if !width {
                width = true
                if (even && stack.count % 2 == 1) || (!even && stack.count > n) {
                    stack.removeFirst()
                }
            }
        }

        func curve(_ dx1: Double, _ dy1: Double, _ dx2: Double, _ dy2: Double, _ dx3: Double, _ dy3: Double) {
            point("C", dx1, dy1)
            point("", dx2, dy2)
            point("", dx3, dy3)
        }

        func run(_ cs: [UInt8], _ depth: Int) {
            var i = 0
            while i < cs.count && !ended {
                let b0 = Int(cs[i])
                if b0 == 28 {
                    stack.append(Double(Int16(bitPattern: UInt16(cs[i + 1]) << 8 | UInt16(cs[i + 2]))))
                    i += 3
                    continue
                } else if b0 >= 32 && b0 <= 246 {
                    stack.append(Double(b0 - 139))
                    i += 1
                    continue
                } else if b0 >= 247 && b0 <= 250 {
                    stack.append(Double((b0 - 247) * 256 + Int(cs[i + 1]) + 108))
                    i += 2
                    continue
                } else if b0 >= 251 && b0 <= 254 {
                    stack.append(Double(-(b0 - 251) * 256 - Int(cs[i + 1]) - 108))
                    i += 2
                    continue
                } else if b0 == 255 {
                    let v = Int32(bitPattern: UInt32(cs[i + 1]) << 24 | UInt32(cs[i + 2]) << 16 |
                            UInt32(cs[i + 3]) << 8 | UInt32(cs[i + 4]))
                    stack.append(Double(v) / 65536)
                    i += 5
                    continue
                }
                let s = stack
                i += 1
                switch b0 {
                case 1, 3, 18, 23:  // hstem, vstem, hstemhm, vstemhm
                    args(true, 0)
                    stems += stack.count / 2
                    path += "stem\(b0) \(stack) "
                case 19, 20:        // hintmask, cntrmask
                    args(true, 0)
                    stems += stack.count / 2
                    let n = (stems + 7) / 8
                    path += "mask\(b0) \(stack) \(Array(cs[i..<(i + n)])) "
                    i += n
                case 21:            // rmoveto
                    args(false, 2)
                    point("M", stack[0], stack[1])
                case 22:            // hmoveto
                    args(false, 1)
                    point("M", stack[0], 0)
                case 4:             // vmoveto
                    args(false, 1)
                    point("M", 0, stack[0])
                case 5:             // rlineto
                    var j = 0
                    while j + 1 < s.count {
                        point("L", s[j], s[j + 1])
                        j += 2
                    }
                case 6, 7:          // hlineto, vlineto
                    var h = b0 == 6
                    for j in 0..<s.count {
                        if h {
                            point("L", s[j], 0)
                        } else {
                            point("L", 0, s[j])
                        }
                        h = !h
                    }
                case 8:             // rrcurveto
                    var j = 0
                    while j + 5 < s.count {
                        curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5])
                        j += 6
                    }
                case 24:            // rcurveline
                    var j = 0
                    while j + 5 < s.count - 2 {
                        curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5])
                        j += 6
                    }
                    point("L", s[j], s[j + 1])
                case 25:            // rlinecurve
                    var j = 0
                    while j + 1 < s.count - 6 {
                        point("L", s[j], s[j + 1])
                        j += 2
                    }
                    curve(s[j], s[j + 1], s[j + 2], s[j + 3], s[j + 4], s[j + 5])
                case 26, 27:        // vvcurveto, hhcurveto
                    var j = 0
                    var d = 0.0
                    if s.count % 4 == 1 {
                        d = s[0]
                        j = 1
                    }
                    while j + 3 < s.count {
                        if b0 == 26 {
                            curve(d, s[j], s[j + 1], s[j + 2], 0, s[j + 3])
                        } else {
                            curve(s[j], d, s[j + 1], s[j + 2], s[j + 3], 0)
                        }
                        d = 0
                        j += 4
                    }
                case 30, 31:        // vhcurveto, hvcurveto
                    var h = b0 == 31
                    var j = 0
                    while j + 3 < s.count {
                        let last = s.count - j == 5 ? s[j + 4] : 0.0
                        if h {
                            curve(s[j], 0, s[j + 1], s[j + 2], last, s[j + 3])
                        } else {
                            curve(0, s[j], s[j + 1], s[j + 2], s[j + 3], last)
                        }
                        j += 4
                        h = !h
                    }
                case 10, 29:        // callsubr, callgsubr
                    let subrs = b0 == 29 ? globals : locals
                    let number = Int(s[s.count - 1]) + CFFSubset.bias(subrs.count)
                    stack.removeLast()
                    run(subrs[number], depth + 1)
                    continue
                case 11:            // return
                    return
                case 14:            // endchar
                    args(false, 0)
                    path += "Z"
                    ended = true
                    return
                case 12:
                    let b1 = cs[i]
                    i += 1
                    switch b1 {
                    case 35:        // flex
                        curve(s[0], s[1], s[2], s[3], s[4], s[5])
                        curve(s[6], s[7], s[8], s[9], s[10], s[11])
                    case 34:        // hflex
                        curve(s[0], 0, s[1], s[2], s[3], 0)
                        curve(s[4], 0, s[5], -s[2], s[6], 0)
                    case 36:        // hflex1
                        curve(s[0], s[1], s[2], s[3], s[4], 0)
                        curve(s[5], 0, s[6], s[7], s[8], -(s[1] + s[3] + s[7]))
                    case 37:        // flex1
                        let dx = abs(s[0] + s[2] + s[4] + s[6] + s[8])
                        let dy = abs(s[1] + s[3] + s[5] + s[7] + s[9])
                        curve(s[0], s[1], s[2], s[3], s[4], s[5])
                        if dx > dy {
                            curve(s[6], s[7], s[8], s[9], s[10], -(s[1] + s[3] + s[5] + s[7] + s[9]))
                        } else {
                            curve(s[6], s[7], s[8], s[9], -(s[0] + s[2] + s[4] + s[6] + s[8]), s[10])
                        }
                    default:
                        path += "op12.\(b1) "
                    }
                default:
                    path += "op\(b0) "
                }
                stack.removeAll(keepingCapacity: true)
            }
        }
    }

    @Test func withItsSubroutinesInPlaceDrawsTheSameOutlines() throws {
        for path in [CFFSubsetTests.plex, "tests/fonts/IBMPlexSans-Italic.otf", CFFSubsetTests.han] {
            let font = try otf(path)
            let table = cff(font)
            let used = used(font, "Hello, Wörld! Ýou ﬁnd 日本語の文字 ½ € @ & ŞŢ")
            var every = [Bool](repeating: false, count: font.numGlyphs)
            for gid in stride(from: 0, to: font.numGlyphs, by: 3) {
                every[gid] = true
            }
            let want = try outlines(table)
            for used in [used, every] {
                let (subset, kept) = try CFFSubset.subset(table, used)
                // No subroutines, global or local
                let (_, globals, top) = try parts(subset)
                #expect(globals.isEmpty, "\(path): \(globals.count) global subroutines")
                let fdArray = try CFFSubset.readIndex(subset, CFFSubset.entryOf(top, CFFSubset.fdArray)![0])
                for i in 0..<fdArray.count {
                    let fd = try CFFSubset.readDict(subset, fdArray.objects[i], fdArray.objects[i + 1])
                    let p = CFFSubset.entryOf(fd, CFFSubset.privateOp)!
                    let block = try CFFSubset.readPrivate(subset, p[0], p[1])
                    #expect(block.subrs == nil, "\(path): local subroutines in Font DICT \(i)")
                }
                // Each glyph drawn as in the whole font, its hints and masks too
                let got = try outlines(subset)
                var i = 0
                for (gid, keep) in kept.enumerated() where keep {
                    #expect(got[i] == want[gid], "\(path): glyph \(gid) drawn otherwise")
                    #expect(got[i].hasSuffix("Z") && (got[i].contains("M ") || gid == 0 || got[i] == "Z"),
                            "\(path): glyph \(gid): \(got[i])")
                    i += 1
                }
                #expect(i == got.count, "\(path): \(got.count) glyphs, not \(i)")
            }
        }
    }

    @Test func ofAGlyphThatComputesANumberKeepsItsSubroutines() throws {
        // "0 0 add 0 endchar": a number computed, by add, which the
        // subroutines are not written in place for. The subset keeps the
        // glyphs as they were and the subroutines they call, the others
        // emptied to no bytes, keeping their numbers.
        let font = try otf(CFFSubsetTests.plex)
        var table = cff(font)
        let names = try CFFSubset.readIndex(table, Int(table[2]))
        let tops = try CFFSubset.readIndex(table, names.end)
        let top = try CFFSubset.readDict(table, tops.objects[0], tops.objects[1])
        let charStrings = try CFFSubset.readIndex(table, CFFSubset.entryOf(top, CFFSubset.charStrings)![0])
        let gid = font.unicodeToGID[Int(("Á" as Unicode.Scalar).value)]
        let at = charStrings.objects[gid]
        try #require(charStrings.objects[gid + 1] - at >= 6, "the charstring of Á is too short")
        table.replaceSubrange(at..<(at + 6), with: [139, 139, 12, 10, 139, 14])
        let (subset, kept) = try CFFSubset.subset(table, used(font, "Hello AÁ"))
        let (glyphs, globals, _) = try parts(subset)
        let (whole, wholeGlobals, _) = try parts(table)
        var i = 0
        for (g, keep) in kept.enumerated() where keep {
            #expect(glyphs[i] == whole[g], "glyph \(g) is not as it was")
            i += 1
        }
        try #require(globals.count == wholeGlobals.count, "\(globals.count) global subroutines")
        var emptied = 0
        for i in 0..<globals.count {
            if globals[i].isEmpty && !wholeGlobals[i].isEmpty {
                emptied += 1
            } else {
                #expect(globals[i] == wholeGlobals[i], "global subroutine \(i) changed")
            }
        }
        #expect(emptied > 0 && emptied < globals.count, "\(emptied) of \(globals.count) global subroutines emptied")
    }
}
