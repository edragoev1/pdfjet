/**
 * GlyphOutline.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The outlines of the glyphs of a font, read from the font file itself: the
/// glyf table of a TrueType font, its quadratic contours drawn exactly as
/// cubic curves, composite glyphs with the transforms of their parts; or the
/// Type 2 charstrings of the CFF table of an OpenType font, with their
/// subroutines, the hints left out. Page.drawGlyphs draws them as paths.
///
/// An outline is written as the path operators of PDF in the units of the
/// font, y up, filled with the nonzero winding rule; the matrix takes the
/// units to the em, so that the size of the font times the matrix, moved to
/// the glyph's place, draws it.
final class GlyphOutline {
    // The message of a font whose outlines cannot be read.
    private static let unreadable = "The outlines of the font cannot be read: "

    /// An outline: its path operators and the box of its points.
    struct Path {
        let operators: String   // Empty for a glyph that draws nothing, as the space
        let xMin: Double
        let yMin: Double
        let xMax: Double
        let yMax: Double
    }

    // The data, the length of the glyf table and the charstrings are
    // variables, so that a test can give a glyph bytes of its own
    var data: [UInt8]
    private let cff: Bool
    // The units of the font to the em: a b c d e f
    private(set) var matrix = [Double]()
    private var numGlyphs = 0

    // TrueType
    private(set) var glyfOffset = 0
    var glyfLength = 0
    private(set) var locaOffset = 0
    private var locaLength = 0
    private(set) var longOffsets = false

    // CFF
    var charStrings = CFFSubset.Index(start: 0, end: 0, objects: [0])
    private var globalSubrs = CFFSubset.Index(start: 0, end: 0, objects: [0])
    private var localSubrs = [CFFSubset.Index?]()   // Of each Font DICT, or of the font
    private var fdOf: [Int]?                        // The Font DICT of each glyph, or nil
    private var fdMatrix: [[Double]]?               // The FontMatrix of each Font DICT, or nil

    // The default FontMatrix of a CFF font.
    private static let thousandth: [Double] = [0.001, 0, 0, 0.001, 0, 0]

    /// Reads the tables of the outlines of the font program: a TrueType font,
    /// or the CFF table of an OpenType font.
    init(_ data: [UInt8], _ cff: Bool) throws {
        self.data = data
        self.cff = cff
        do {
            if cff {
                matrix = try readCFF()
                numGlyphs = charStrings.count
            } else {
                let unitsPerEm = try readTrueType()
                matrix = [1.0 / Double(unitsPerEm), 0, 0, 1.0 / Double(unitsPerEm), 0, 0]
                numGlyphs = (locaLength - (longOffsets ? 4 : 2)) / (longOffsets ? 4 : 2)
            }
        } catch {
            throw PDFjetError(message: GlyphOutline.unreadable + "its tables are damaged.")
        }
    }

    // Reads the head, loca and glyf tables, and returns the units per em.
    private func readTrueType() throws -> Int {
        let numTables = try u16(4)
        var headOffset = -1
        var locaAt = -1
        var glyfAt = -1
        for i in 0..<numTables {
            let entry = 12 + 16 * i
            if entry + 16 > data.count {
                throw Subset.NotSubset()
            }
            let tag = String(decoding: data[entry..<(entry + 4)], as: UTF8.self)
            let offset = try u32(entry + 8)
            let length = try u32(entry + 12)
            if offset + length > data.count {
                throw Subset.NotSubset()
            }
            if tag == "head" {
                headOffset = offset
            } else if tag == "loca" {
                locaAt = offset
                locaLength = length
            } else if tag == "glyf" {
                glyfAt = offset
                glyfLength = length
            }
        }
        if headOffset < 0 || locaAt < 0 || glyfAt < 0 {
            throw Subset.NotSubset()
        }
        let unitsPerEm = try u16(headOffset + 18)
        longOffsets = try u16(headOffset + 50) == 1
        locaOffset = locaAt
        glyfOffset = glyfAt
        if unitsPerEm == 0 {
            throw Subset.NotSubset()
        }
        return unitsPerEm
    }

    // Reads the CFF table: its charstrings, its subroutines and the Font DICT
    // of each glyph, and returns the FontMatrix of the font.
    private func readCFF() throws -> [Double] {
        if data.count < 4 || data[0] != 1 {
            throw Subset.NotSubset()    // Not CFF version 1
        }
        let names = try CFFSubset.readIndex(data, Int(data[2]))
        let topDicts = try CFFSubset.readIndex(data, names.end)
        if topDicts.count != 1 {
            throw Subset.NotSubset()
        }
        let strings = try CFFSubset.readIndex(data, topDicts.end)
        globalSubrs = try CFFSubset.readIndex(data, strings.end)
        let top = try CFFSubset.readDict(data, topDicts.objects[0], topDicts.objects[1])
        guard let at = CFFSubset.entryOf(top, CFFSubset.charStrings), at.count == 1 else {
            throw Subset.NotSubset()
        }
        charStrings = try CFFSubset.readIndex(data, at[0])
        let topMatrix = try GlyphOutline.matrixOf(top)
        if CFFSubset.entryOf(top, CFFSubset.ros) != nil {
            guard let fdArrayAt = CFFSubset.entryOf(top, CFFSubset.fdArray), fdArrayAt.count == 1,
                    let fdSelectAt = CFFSubset.entryOf(top, CFFSubset.fdSelect), fdSelectAt.count == 1 else {
                throw Subset.NotSubset()
            }
            let fdArray = try CFFSubset.readIndex(data, fdArrayAt[0])
            var matrices = [[Double]]()
            localSubrs = []
            for i in 0..<fdArray.count {
                let fd = try CFFSubset.readDict(data, fdArray.objects[i], fdArray.objects[i + 1])
                localSubrs.append(try subrsOf(fd))
                // The glyph space of a CIDFont: the FontMatrix of the glyph's
                // Font DICT and then the Top DICT's; a CIDFont whose Top DICT
                // has none has its Font DICTs' taken as the whole ones, as
                // readers do.
                if let m = try GlyphOutline.matrixOf(fd) {
                    matrices.append(topMatrix == nil ? m : GlyphOutline.multiply(m, topMatrix!))
                } else {
                    matrices.append(topMatrix ?? GlyphOutline.thousandth)
                }
            }
            fdMatrix = matrices
            let fdSelect = try CFFSubset.block(data, fdSelectAt[0],
                    try CFFSubset.fdSelectLength(data, fdSelectAt[0], charStrings.count))
            fdOf = try CFFSubset.fdOf(fdSelect, charStrings.count, fdArray.count)
            return GlyphOutline.thousandth  // Of a glyph, its Font DICT's
        }
        localSubrs = [try subrsOf(top)]
        return topMatrix ?? GlyphOutline.thousandth
    }

    // The local subroutines of the Private DICT of the Top DICT or Font DICT.
    private func subrsOf(_ dict: [CFFSubset.Entry]) throws -> CFFSubset.Index? {
        guard let p = CFFSubset.entryOf(dict, CFFSubset.privateOp), p.count == 2 else {
            throw Subset.NotSubset()
        }
        return try CFFSubset.readPrivate(data, p[0], p[1]).subrs
    }

    // The FontMatrix of the DICT, or nil when it has none.
    private static func matrixOf(_ dict: [CFFSubset.Entry]) throws -> [Double]? {
        for entry in dict where entry.op == CFFSubset.fontMatrix {
            let m = try operands(entry.raw)
            if m.count != 6 {
                throw Subset.NotSubset()
            }
            for v in m where !(abs(v) <= 1000) {    // NaN too
                throw Subset.NotSubset()
            }
            return m
        }
        return nil
    }

    // The product of the matrices: a, then b.
    private static func multiply(_ a: [Double], _ b: [Double]) -> [Double] {
        return [
            a[0] * b[0] + a[1] * b[2],
            a[0] * b[1] + a[1] * b[3],
            a[2] * b[0] + a[3] * b[2],
            a[2] * b[1] + a[3] * b[3],
            a[4] * b[0] + a[5] * b[2] + b[4],
            a[4] * b[1] + a[5] * b[3] + b[5]
        ]
    }

    // The operands of an entry of a DICT, integers and reals, before its operator.
    private static func operands(_ raw: [UInt8]) throws -> [Double] {
        var values = [Double]()
        var i = 0
        func byte(_ at: Int) throws -> Int {
            if at >= raw.count {
                throw Subset.NotSubset()
            }
            return Int(raw[at])
        }
        while i < raw.count {
            let b0 = Int(raw[i])
            if b0 <= 21 {
                break   // The operator
            } else if b0 == 28 {
                values.append(Double(Int16(truncatingIfNeeded: try byte(i + 1) << 8 | byte(i + 2))))
                i += 3
            } else if b0 == 29 {
                let v = try byte(i + 1) << 24 | byte(i + 2) << 16 | byte(i + 3) << 8 | byte(i + 4)
                values.append(Double(Int32(truncatingIfNeeded: v)))
                i += 5
            } else if b0 == 30 {
                var s = ""
                i += 1
                var end = false
                while !end && i < raw.count {
                    let b = Int(raw[i])
                    i += 1
                    for nibble in [b >> 4, b & 0x0F] {
                        if nibble <= 9 {
                            s.append(Character(UnicodeScalar(UInt8(48 + nibble))))
                        } else if nibble == 0xA {
                            s.append(".")
                        } else if nibble == 0xB {
                            s.append("E")
                        } else if nibble == 0xC {
                            s.append("E-")
                        } else if nibble == 0xE {
                            s.append("-")
                        } else if nibble == 0xF {
                            end = true
                            break
                        }
                    }
                }
                guard let value = Double(s) else {
                    throw Subset.NotSubset()
                }
                values.append(value)
            } else if b0 >= 32 && b0 <= 246 {
                values.append(Double(b0 - 139))
                i += 1
            } else if b0 >= 247 && b0 <= 250 {
                values.append(Double((b0 - 247) * 256 + (try byte(i + 1)) + 108))
                i += 2
            } else if b0 >= 251 && b0 <= 254 {
                values.append(Double(-(b0 - 251) * 256 - (try byte(i + 1)) - 108))
                i += 2
            } else {
                throw Subset.NotSubset()
            }
        }
        return values
    }

    /// The number of glyphs of the font.
    func count() -> Int {
        return numGlyphs
    }

    /// The matrix that takes the units of the glyph to the em: of the font,
    /// or of the glyph's Font DICT in a CID-keyed CFF font.
    func matrixOf(_ gid: Int) -> [Double] {
        if let fdMatrix = fdMatrix, let fdOf = fdOf, gid >= 0 && gid < fdOf.count {
            return fdMatrix[fdOf[gid]]
        }
        return matrix
    }

    /// The outline of the glyph.
    func outline(_ gid: Int) throws -> Path {
        if gid < 0 || gid >= numGlyphs {
            throw PDFjetError(message: "The font has no glyph number \(gid).")
        }
        let pen = Pen()
        do {
            if cff {
                let charString = CharString(self, pen, gid)
                try charString.run(charStrings.objects[gid], charStrings.objects[gid + 1], 0)
            } else {
                var points = Points(work: Points.Work())
                try trueTypePoints(gid, &points, 0)
                points.draw(pen)
            }
        } catch let error as PDFjetError {
            throw error
        } catch {
            throw PDFjetError(message: GlyphOutline.unreadable + "glyph number \(gid) is damaged.")
        }
        if pen.outOfRange {
            throw PDFjetError(message: GlyphOutline.unreadable + "glyph number \(gid) is damaged.")
        }
        return pen.path()
    }

    // ------------------------------------------------------------------
    // The path: the operators of PDF, in the units of the font

    // The bound of the points of an outline, in the units of the font: a
    // font's are within ±32,768, and a charstring's arithmetic can make
    // larger ones, or ones not finite, which a path cannot have.
    static let maxCoordinate = 1e6

    /// Writes the path operators and finds the box of the points.
    final class Pen {
        private var sb = ""
        private var xMin = Double.greatestFiniteMagnitude
        private var yMin = Double.greatestFiniteMagnitude
        private var xMax = -Double.greatestFiniteMagnitude
        private var yMax = -Double.greatestFiniteMagnitude
        private var startX = 0.0
        private var startY = 0.0
        private(set) var x = 0.0
        private(set) var y = 0.0
        private var open = false    // A contour is begun and has a segment
        private(set) var outOfRange = false // A point past maxCoordinate, or not finite

        private func point(_ px: Double, _ py: Double) {
            var px = px
            var py = py
            if !(abs(px) <= GlyphOutline.maxCoordinate && abs(py) <= GlyphOutline.maxCoordinate) {
                outOfRange = true   // NaN too
                px = 0
                py = 0
            }
            if px < xMin { xMin = px }
            if py < yMin { yMin = py }
            if px > xMax { xMax = px }
            if py > yMax { yMax = py }
            sb += GlyphOutline.number(px)
            sb += " "
            sb += GlyphOutline.number(py)
        }

        func moveTo(_ px: Double, _ py: Double) {
            startX = px
            startY = py
            x = px
            y = py
            open = false
        }

        // Writes the m of the contour before its first segment, so that a
        // contour of a moveto alone writes nothing.
        private func begin() {
            if !open {
                point(startX, startY)
                sb += " m\n"
                open = true
            }
        }

        func lineTo(_ px: Double, _ py: Double) {
            begin()
            point(px, py)
            sb += " l\n"
            x = px
            y = py
        }

        func curveTo(_ x1: Double, _ y1: Double, _ x2: Double, _ y2: Double, _ x3: Double, _ y3: Double) {
            begin()
            point(x1, y1)
            sb += " "
            point(x2, y2)
            sb += " "
            point(x3, y3)
            sb += " c\n"
            x = x3
            y = y3
        }

        // The quadratic curve from the current point drawn exactly as a cubic one.
        func quadTo(_ qx: Double, _ qy: Double, _ px: Double, _ py: Double) {
            curveTo(x + 2.0 / 3.0 * (qx - x), y + 2.0 / 3.0 * (qy - y),
                    px + 2.0 / 3.0 * (qx - px), py + 2.0 / 3.0 * (qy - py),
                    px, py)
        }

        func path() -> Path {
            if sb.isEmpty {
                return Path(operators: "", xMin: 0, yMin: 0, xMax: 0, yMax: 0)
            }
            return Path(operators: sb, xMin: xMin, yMin: yMin, xMax: xMax, yMax: yMax)
        }
    }

    /// A number of a path, in the units of the font: whole units, the same in
    /// the four ports, rounded half away from zero. The font's own points are
    /// whole units; the control points of a quadratic curve drawn as a cubic
    /// one are not, and move by at most half a unit, 0.05% of the em, unseen
    /// at any size, for outlines 15% smaller (9.2.1).
    static func number(_ value: Double) -> String {
        return decimals(value, 0)
    }

    /// The value with at most the decimals given, rounded half away from zero.
    static func decimals(_ value: Double, _ decimals: Int) -> String {
        var scale: Int64 = 1
        for _ in 0..<decimals {
            scale *= 10
        }
        // A value too large for an Int64, or not finite, as a font size can
        // make the matrix of a glyph, is the largest one, and NaN 0: made an
        // integer, it would trap.
        let d = (abs(value) * Double(scale) + 0.5).rounded(.down)
        let n: Int64 = d < 9223372036854775808.0 ? Int64(d) : (d.isNaN ? 0 : Int64.max)
        if n == 0 {
            return "0"
        }
        var s = ""
        if value < 0 {
            s += "-"
        }
        s += String(n / scale)
        let fraction = n % scale
        if fraction != 0 {
            var digits = Array(String(fraction + scale).dropFirst())
            while digits.last == "0" {
                digits.removeLast()
            }
            s += "." + String(digits)
        }
        return s
    }

    // ------------------------------------------------------------------
    // TrueType

    /// The points of a glyph, with the last point of each contour.
    struct Points {
        var x = [Double]()
        var y = [Double]()
        var on = [Bool]()
        var ends = [Int]()
        let work: Work  // The glyphs and points read for the outline, its parts' too

        // The work of an outline, shared by the points of its parts.
        final class Work {
            var count = 0
        }

        var count: Int { x.count }

        mutating func add(_ px: Double, _ py: Double, _ onCurve: Bool) {
            x.append(px)
            y.append(py)
            on.append(onCurve)
        }

        // Draws the contours: the points on the curve joined by lines, and by
        // quadratic curves through the points off it, with a point on the
        // curve halfway between two off it.
        func draw(_ pen: Pen) {
            var first = 0
            for end in ends {
                let n = end - first + 1
                if n >= 2 {
                    contour(pen, first, n)
                }
                first = end + 1
            }
        }

        private func contour(_ pen: Pen, _ first: Int, _ n: Int) {
            var sx: Double
            var sy: Double
            var from: Int   // The first point after the start
            var steps: Int  // The points after the start, the start not again
            if on[first] {
                sx = x[first]
                sy = y[first]
                from = 1
                steps = n - 1
            } else if on[first + n - 1] {
                sx = x[first + n - 1]
                sy = y[first + n - 1]
                from = 0
                steps = n - 1
            } else {
                sx = (x[first] + x[first + n - 1]) / 2
                sy = (y[first] + y[first + n - 1]) / 2
                from = 0
                steps = n
            }
            pen.moveTo(sx, sy)
            var off = false
            var qx = 0.0
            var qy = 0.0
            for k in 0..<steps {
                let i = first + (from + k) % n
                if on[i] {
                    if off {
                        pen.quadTo(qx, qy, x[i], y[i])
                    } else {
                        pen.lineTo(x[i], y[i])
                    }
                    off = false
                } else {
                    if off {
                        pen.quadTo(qx, qy, (qx + x[i]) / 2, (qy + y[i]) / 2)
                    }
                    qx = x[i]
                    qy = y[i]
                    off = true
                }
            }
            // Back to the start: a curve, or the line that filling draws
            if off {
                pen.quadTo(qx, qy, sx, sy)
            }
        }
    }

    // Adds the points of the glyph, a simple or a composite one, to the points.
    private func trueTypePoints(_ gid: Int, _ points: inout Points, _ depth: Int) throws {
        if depth > 16 {
            throw Subset.NotSubset()    // Composite glyphs nested too deep, or in a loop
        }
        // A composite glyph of many parts, each of many parts, 16 deep, would
        // take time and memory that grow exponentially: the work of a glyph is
        // bounded, as that of a charstring is.
        points.work.count += 1
        if points.work.count > GlyphOutline.maxWork {
            throw Subset.NotSubset()
        }
        var start: Int
        var end: Int
        if longOffsets {
            start = try u32(locaOffset + 4 * gid)
            end = try u32(locaOffset + 4 * gid + 4)
        } else {
            start = 2 * (try u16(locaOffset + 2 * gid))
            end = 2 * (try u16(locaOffset + 2 * gid + 2))
        }
        if start == end {
            return      // No outline, as the space
        }
        if start > end || end > glyfLength || end - start < 10 {
            throw Subset.NotSubset()
        }
        let at = glyfOffset + start
        let limit = glyfOffset + end
        let contours = Int(Int16(truncatingIfNeeded: try u16(at)))
        if contours >= 0 {
            try simplePoints(at, limit, contours, &points)
        } else {
            try compositePoints(at + 10, limit, &points, depth)
        }
    }

    private func simplePoints(_ at: Int, _ limit: Int, _ contours: Int, _ points: inout Points) throws {
        var p = at + 10
        var ends = [Int](repeating: 0, count: contours)
        var n = 0
        for i in 0..<contours {
            ends[i] = try u16(p + 2 * i)
            if ends[i] < n - 1 || (i > 0 && ends[i] <= ends[i - 1]) {
                throw Subset.NotSubset()
            }
            n = ends[i] + 1
        }
        points.work.count += n
        if points.work.count > GlyphOutline.maxWork {
            throw Subset.NotSubset()
        }
        p += 2 * contours
        let instructions = try u16(p)
        p += 2 + instructions
        var flags = [UInt8](repeating: 0, count: n)
        var i = 0
        while i < n {
            let flag = try byteAt(p, limit)
            p += 1
            flags[i] = UInt8(flag)
            i += 1
            if (flag & 0x08) != 0 {     // REPEAT_FLAG
                let repeatCount = try byteAt(p, limit)
                p += 1
                for _ in 0..<repeatCount {
                    if i >= n {
                        throw Subset.NotSubset()
                    }
                    flags[i] = UInt8(flag)
                    i += 1
                }
            }
        }
        var xs = [Int](repeating: 0, count: n)
        var value = 0
        for i in 0..<n {
            let flag = Int(flags[i])
            if (flag & 0x02) != 0 {             // X_SHORT_VECTOR
                let dx = try byteAt(p, limit)
                p += 1
                value += (flag & 0x10) != 0 ? dx : -dx
            } else if (flag & 0x10) == 0 {      // Not X_IS_SAME
                value += Int(Int16(truncatingIfNeeded: try byteAt(p, limit) << 8 | byteAt(p + 1, limit)))
                p += 2
            }
            xs[i] = value
        }
        value = 0
        let base = points.count
        for i in 0..<n {
            let flag = Int(flags[i])
            if (flag & 0x04) != 0 {             // Y_SHORT_VECTOR
                let dy = try byteAt(p, limit)
                p += 1
                value += (flag & 0x20) != 0 ? dy : -dy
            } else if (flag & 0x20) == 0 {      // Not Y_IS_SAME
                value += Int(Int16(truncatingIfNeeded: try byteAt(p, limit) << 8 | byteAt(p + 1, limit)))
                p += 2
            }
            points.add(Double(xs[i]), Double(value), (flag & 0x01) != 0)
        }
        for end in ends {
            points.ends.append(base + end)
        }
    }

    private func compositePoints(_ start: Int, _ limit: Int, _ points: inout Points, _ depth: Int) throws {
        var p = start
        while true {
            let flags = try u16(p)
            let component = try u16(p + 2)
            p += 4
            if component >= numGlyphs {
                throw Subset.NotSubset()
            }
            var arg1: Int
            var arg2: Int
            let xy = (flags & 0x0002) != 0      // ARGS_ARE_XY_VALUES
            if (flags & 0x0001) != 0 {          // ARG_1_AND_2_ARE_WORDS
                arg1 = try u16(p)
                arg2 = try u16(p + 2)
                if xy {
                    arg1 = Int(Int16(truncatingIfNeeded: arg1))
                    arg2 = Int(Int16(truncatingIfNeeded: arg2))
                }
                p += 4
            } else {
                arg1 = try byteAt(p, limit)
                arg2 = try byteAt(p + 1, limit)
                if xy {
                    arg1 = Int(Int8(truncatingIfNeeded: arg1))
                    arg2 = Int(Int8(truncatingIfNeeded: arg2))
                }
                p += 2
            }
            var a = 1.0
            var b = 0.0
            var c = 0.0
            var d = 1.0
            if (flags & 0x0008) != 0 {          // WE_HAVE_A_SCALE
                a = try f2dot14(p)
                d = a
                p += 2
            } else if (flags & 0x0040) != 0 {   // WE_HAVE_AN_X_AND_Y_SCALE
                a = try f2dot14(p)
                d = try f2dot14(p + 2)
                p += 4
            } else if (flags & 0x0080) != 0 {   // WE_HAVE_A_TWO_BY_TWO
                a = try f2dot14(p)
                b = try f2dot14(p + 2)
                c = try f2dot14(p + 4)
                d = try f2dot14(p + 6)
                p += 8
            }
            if p > limit {
                throw Subset.NotSubset()
            }
            var part = Points(work: points.work)
            try trueTypePoints(component, &part, depth + 1)
            for i in 0..<part.count {
                let px = part.x[i]
                let py = part.y[i]
                part.x[i] = a * px + c * py
                part.y[i] = b * px + d * py
            }
            var dx: Double
            var dy: Double
            if xy {
                dx = Double(arg1)
                dy = Double(arg2)
                // SCALED_COMPONENT_OFFSET, and not UNSCALED_COMPONENT_OFFSET
                if (flags & 0x0800) != 0 && (flags & 0x1000) == 0 {
                    let tx = a * dx + c * dy
                    let ty = b * dx + d * dy
                    dx = tx
                    dy = ty
                }
            } else {
                // The point of the part placed on the point of the glyph
                if arg1 >= points.count || arg2 >= part.count {
                    throw Subset.NotSubset()
                }
                dx = points.x[arg1] - part.x[arg2]
                dy = points.y[arg1] - part.y[arg2]
            }
            let base = points.count
            for i in 0..<part.count {
                points.add(part.x[i] + dx, part.y[i] + dy, part.on[i])
            }
            for end in part.ends {
                points.ends.append(base + end)
            }
            if (flags & 0x0020) == 0 {          // MORE_COMPONENTS
                break
            }
        }
    }

    private func f2dot14(_ at: Int) throws -> Double {
        return Double(Int16(truncatingIfNeeded: try u16(at))) / 16384.0
    }

    private func byteAt(_ at: Int, _ limit: Int) throws -> Int {
        if at < 0 || at >= limit || at >= data.count {
            throw Subset.NotSubset()
        }
        return Int(data[at])
    }

    private func u16(_ at: Int) throws -> Int {
        return try Subset.u16(data, at)
    }

    private func u32(_ at: Int) throws -> Int {
        return try Subset.u32(data, at)
    }

    // ------------------------------------------------------------------
    // CFF: the Type 2 charstrings (Adobe Technical Note #5177)

    // The most bytes of charstrings a glyph runs, subroutines and all; and the
    // most glyphs and points a TrueType glyph reads, its parts and theirs.
    private static let maxWork = 1 << 20

    /// The interpreter of the charstring of one glyph.
    private final class CharString {
        private let outline: GlyphOutline
        private let data: [UInt8]
        private let pen: Pen
        private let local: CFFSubset.Index?
        private var stack = [Double](repeating: 0, count: 48)
        private var sp = 0
        private var storage = [Double](repeating: 0, count: 32)
        private var stems = 0
        private var widthDone = false
        private var ended = false
        private var x = 0.0
        private var y = 0.0
        private var work = 0
        private let gid: Int

        init(_ outline: GlyphOutline, _ pen: Pen, _ gid: Int) {
            self.outline = outline
            self.data = outline.data
            self.pen = pen
            self.gid = gid
            if let fdOf = outline.fdOf {
                self.local = outline.localSubrs[fdOf[gid]]
            } else {
                self.local = outline.localSubrs[0]
            }
        }

        private func push(_ value: Double) throws {
            if sp == stack.count {
                throw Subset.NotSubset()
            }
            stack[sp] = value
            sp += 1
        }

        private func pop() throws -> Double {
            if sp == 0 {
                throw Subset.NotSubset()
            }
            sp -= 1
            return stack[sp]
        }

        // The first operator that clears the stack may have the width of the
        // glyph before its operands, which is not drawn.
        private func width(_ extra: Bool) -> Int {
            if !widthDone {
                widthDone = true
                if extra {
                    return 1
                }
            }
            return 0
        }

        private func moveTo(_ dx: Double, _ dy: Double) {
            x += dx
            y += dy
            pen.moveTo(x, y)
        }

        private func lineTo(_ dx: Double, _ dy: Double) {
            x += dx
            y += dy
            pen.lineTo(x, y)
        }

        private func curveTo(_ dx1: Double, _ dy1: Double, _ dx2: Double, _ dy2: Double,
                _ dx3: Double, _ dy3: Double) {
            let x1 = x + dx1
            let y1 = y + dy1
            let x2 = x1 + dx2
            let y2 = y1 + dy2
            x = x2 + dx3
            y = y2 + dy3
            pen.curveTo(x1, y1, x2, y2, x, y)
        }

        private func need(_ count: Int) throws {
            if sp < count {
                throw Subset.NotSubset()
            }
        }

        func run(_ from: Int, _ to: Int, _ depth: Int) throws {
            if depth > 10 {     // Type 2 nests subroutines 10 deep at most
                throw Subset.NotSubset()
            }
            work += to - from
            if work > GlyphOutline.maxWork {
                throw Subset.NotSubset()
            }
            var i = from
            while i < to && !ended {
                let b0 = Int(data[i])
                if b0 == 28 {
                    if i > to - 3 {
                        throw Subset.NotSubset()
                    }
                    try push(Double(Int16(truncatingIfNeeded: Int(data[i + 1]) << 8 | Int(data[i + 2]))))
                    i += 3
                    continue
                } else if b0 >= 32 && b0 <= 246 {
                    try push(Double(b0 - 139))
                    i += 1
                    continue
                } else if b0 >= 247 && b0 <= 250 {
                    if i > to - 2 {
                        throw Subset.NotSubset()
                    }
                    try push(Double((b0 - 247) * 256 + Int(data[i + 1]) + 108))
                    i += 2
                    continue
                } else if b0 >= 251 && b0 <= 254 {
                    if i > to - 2 {
                        throw Subset.NotSubset()
                    }
                    try push(Double(-(b0 - 251) * 256 - Int(data[i + 1]) - 108))
                    i += 2
                    continue
                } else if b0 == 255 {   // A 16.16 fixed number
                    if i > to - 5 {
                        throw Subset.NotSubset()
                    }
                    let fixed = Int32(truncatingIfNeeded: Int(data[i + 1]) << 24 | Int(data[i + 2]) << 16
                            | Int(data[i + 3]) << 8 | Int(data[i + 4]))
                    try push(Double(fixed) / 65536.0)
                    i += 5
                    continue
                }
                i += 1
                var k = 0
                switch b0 {
                case 1, 3, 18, 23:  // hstem, vstem, hstemhm, vstemhm
                    _ = width(sp % 2 == 1)
                    stems += sp / 2
                    sp = 0
                case 19, 20:        // hintmask, cntrmask
                    // The vstem hints before it, given with no operator
                    _ = width(sp % 2 == 1)
                    stems += sp / 2
                    sp = 0
                    i += (stems + 7) / 8
                    if i > to {
                        throw Subset.NotSubset()
                    }
                case 21:            // rmoveto
                    k = width(sp > 2)
                    try need(k + 2)
                    moveTo(stack[k], stack[k + 1])
                    sp = 0
                case 22:            // hmoveto
                    k = width(sp > 1)
                    try need(k + 1)
                    moveTo(stack[k], 0)
                    sp = 0
                case 4:             // vmoveto
                    k = width(sp > 1)
                    try need(k + 1)
                    moveTo(0, stack[k])
                    sp = 0
                case 5:             // rlineto
                    k = 0
                    while k + 1 < sp {
                        lineTo(stack[k], stack[k + 1])
                        k += 2
                    }
                    sp = 0
                case 6, 7:          // hlineto, vlineto
                    for k in 0..<sp {
                        if (k % 2 == 0) == (b0 == 6) {
                            lineTo(stack[k], 0)
                        } else {
                            lineTo(0, stack[k])
                        }
                    }
                    sp = 0
                case 8:             // rrcurveto
                    k = 0
                    while k + 5 < sp {
                        curveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5])
                        k += 6
                    }
                    sp = 0
                case 24:            // rcurveline
                    k = 0
                    while k + 5 < sp - 2 {
                        curveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5])
                        k += 6
                    }
                    try need(k + 2)
                    lineTo(stack[k], stack[k + 1])
                    sp = 0
                case 25:            // rlinecurve
                    k = 0
                    while k + 1 < sp - 6 {
                        lineTo(stack[k], stack[k + 1])
                        k += 2
                    }
                    try need(k + 6)
                    curveTo(stack[k], stack[k + 1], stack[k + 2], stack[k + 3], stack[k + 4], stack[k + 5])
                    sp = 0
                case 26:            // vvcurveto
                    k = 0
                    var dx1 = 0.0
                    if sp % 2 == 1 {
                        dx1 = stack[0]
                        k = 1
                    }
                    while k + 3 < sp {
                        curveTo(dx1, stack[k], stack[k + 1], stack[k + 2], 0, stack[k + 3])
                        dx1 = 0
                        k += 4
                    }
                    sp = 0
                case 27:            // hhcurveto
                    k = 0
                    var dy1 = 0.0
                    if sp % 2 == 1 {
                        dy1 = stack[0]
                        k = 1
                    }
                    while k + 3 < sp {
                        curveTo(stack[k], dy1, stack[k + 1], stack[k + 2], stack[k + 3], 0)
                        dy1 = 0
                        k += 4
                    }
                    sp = 0
                case 30, 31:        // vhcurveto, hvcurveto
                    var horizontal = (b0 == 31)
                    k = 0
                    while k + 3 < sp {
                        let last = (sp - k == 5) ? stack[k + 4] : 0
                        if horizontal {
                            curveTo(stack[k], 0, stack[k + 1], stack[k + 2], last, stack[k + 3])
                        } else {
                            curveTo(0, stack[k], stack[k + 1], stack[k + 2], stack[k + 3], last)
                        }
                        horizontal = !horizontal
                        k += 4
                    }
                    sp = 0
                case 10, 29:        // callsubr, callgsubr
                    guard let subrs = (b0 == 29) ? outline.globalSubrs : local else {
                        throw Subset.NotSubset()
                    }
                    // Compared as a number before it is made an integer, as put and get
                    let n = try pop() + Double(CFFSubset.bias(subrs.count))
                    if !(n >= 0 && n < Double(subrs.count)) {   // NaN too
                        throw Subset.NotSubset()
                    }
                    let number = Int(n)
                    try run(subrs.objects[number], subrs.objects[number + 1], depth + 1)
                case 11:            // return
                    return
                case 14:            // endchar
                    k = width(sp == 1 || sp == 5)
                    if sp - k >= 4 {
                        throw PDFjetError(message: "The CFF glyph number \(gid)"
                                + " is an accented letter of the seac operator, which drawGlyphs does not draw.")
                    }
                    sp = 0
                    ended = true
                    return
                case 12:
                    if i >= to {
                        throw Subset.NotSubset()
                    }
                    let b1 = Int(data[i])
                    i += 1
                    try escaped(b1)
                default:
                    throw Subset.NotSubset()    // A reserved operator
                }
            }
        }

        // The operators of two bytes: the flex curves and the arithmetic.
        private func escaped(_ b1: Int) throws {
            var a: Double
            var b: Double
            switch b1 {
            case 35:    // flex
                try need(13)
                curveTo(stack[0], stack[1], stack[2], stack[3], stack[4], stack[5])
                curveTo(stack[6], stack[7], stack[8], stack[9], stack[10], stack[11])
                sp = 0
            case 34:    // hflex
                try need(7)
                let startY = y
                curveTo(stack[0], 0, stack[1], stack[2], stack[3], 0)
                curveTo(stack[4], 0, stack[5], startY - y, stack[6], 0)
                sp = 0
            case 36:    // hflex1
                try need(9)
                let startY = y
                curveTo(stack[0], stack[1], stack[2], stack[3], stack[4], 0)
                curveTo(stack[5], 0, stack[6], stack[7], stack[8], startY - (y + stack[7]))
                sp = 0
            case 37:    // flex1
                try need(11)
                let dx = stack[0] + stack[2] + stack[4] + stack[6] + stack[8]
                let dy = stack[1] + stack[3] + stack[5] + stack[7] + stack[9]
                curveTo(stack[0], stack[1], stack[2], stack[3], stack[4], stack[5])
                if abs(dx) > abs(dy) {
                    curveTo(stack[6], stack[7], stack[8], stack[9], stack[10], -dy)
                } else {
                    curveTo(stack[6], stack[7], stack[8], stack[9], -dx, stack[10])
                }
                sp = 0
            case 3:     // and
                b = try pop()
                a = try pop()
                try push((a != 0 && b != 0) ? 1 : 0)
            case 4:     // or
                b = try pop()
                a = try pop()
                try push((a != 0 || b != 0) ? 1 : 0)
            case 5:     // not
                try push(try pop() == 0 ? 1 : 0)
            case 9:     // abs
                try push(abs(try pop()))
            case 10:    // add
                b = try pop()
                a = try pop()
                try push(a + b)
            case 11:    // sub
                b = try pop()
                a = try pop()
                try push(a - b)
            case 12:    // div
                b = try pop()
                a = try pop()
                if b == 0 {
                    throw Subset.NotSubset()
                }
                try push(a / b)
            case 14:    // neg
                try push(-(try pop()))
            case 15:    // eq
                b = try pop()
                a = try pop()
                try push(a == b ? 1 : 0)
            case 18:    // drop
                _ = try pop()
            case 20:    // put
                // Compared as a number before it is made an integer, as index
                let index = try pop()
                a = try pop()
                if !(index >= 0 && index < Double(storage.count)) {  // NaN too
                    throw Subset.NotSubset()
                }
                storage[Int(index)] = a
            case 21:    // get
                let index = try pop()
                if !(index >= 0 && index < Double(storage.count)) {  // NaN too
                    throw Subset.NotSubset()
                }
                try push(storage[Int(index)])
            case 22:    // ifelse
                let v2 = try pop()
                let v1 = try pop()
                let s2 = try pop()
                let s1 = try pop()
                try push(v1 <= v2 ? s1 : s2)
            case 23:    // random, the same number in every port
                try push(0.5)
            case 24:    // mul
                b = try pop()
                a = try pop()
                try push(a * b)
            case 26:    // sqrt
                try push(abs(try pop()).squareRoot())
            case 27:    // dup
                a = try pop()
                try push(a)
                try push(a)
            case 28:    // exch
                b = try pop()
                a = try pop()
                try push(b)
                try push(a)
            case 29:    // index
                let v = try pop()
                // Compared as a number before it is made an integer, which differs
                // from language to language and from processor to processor for
                // one too large
                if v.isNaN || v >= Double(sp) {
                    throw Subset.NotSubset()
                }
                var i = 0
                if v > 0 {
                    i = Int(v)
                }
                try push(stack[sp - 1 - i])
            case 30:    // roll
                let jv = try pop()
                let nv = try pop()
                // Compared as numbers before they are made integers, as index
                if !(nv >= 1 && nv <= Double(sp)) || !(abs(jv) <= 1e9) {
                    throw Subset.NotSubset()
                }
                let j = Int(jv)
                let n = Int(nv)
                var part = [Double](repeating: 0, count: n)
                for m in 0..<n {
                    part[((m + j) % n + n) % n] = stack[sp - n + m]
                }
                for m in 0..<n {
                    stack[sp - n + m] = part[m]
                }
            default:
                // dotsection (0), deprecated, and the reserved ones: no drawing
                sp = 0
            }
        }
    }
}   // End of GlyphOutline.swift
