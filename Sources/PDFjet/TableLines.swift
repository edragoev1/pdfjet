/**
 * TableLines.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// The borders of the cells that a table draws on a page, kept while the cells
// are drawn and written as continuous lines when the table is done with the
// page: each cell stroked its own top and left, one short line for each edge,
// three or four times the lines of one line across each row. Lines on the
// same y, or the same x, that touch or overlap and have the same pen become
// one line, which covers the same part of the page as they did.
//
// A line is drawn later than its cell drew it, so what was drawn over it in
// between must be drawn over it still: before a background is filled over a
// line that is kept, or next to it, the lines are written; and before a
// drawable or a point is drawn, as either can draw anything anywhere, with
// the pen the cell left. Lines of different pens that overlap are written in
// the order the cells drew them, so the one on top is on top still.
final class TableLines {
    // A line: horizontal at y = at, from x = from to x = to, or vertical at
    // x = at, from y = from to y = to, with the pen of the page when the cell
    // drew it, and how far its caps reach past its ends.
    final class Segment {
        let vertical: Bool
        let at: Float
        var from: Float
        var to: Float
        let r: Float
        let g: Float
        let b: Float
        let width: Float
        let cap: Float

        init(_ vertical: Bool, _ at: Float, _ from: Float, _ to: Float,
                _ r: Float, _ g: Float, _ b: Float, _ width: Float, _ cap: Float) {
            self.vertical = vertical
            self.at = at
            self.from = from
            self.to = to
            self.r = r
            self.g = g
            self.b = b
            self.width = width
            self.cap = cap
        }

        func samePen(_ other: Segment) -> Bool {
            return r == other.r && g == other.g && b == other.b && width == other.width
        }

        // The part of the page the line covers: left, top, right, bottom.
        func x0() -> Float { return vertical ? at - width/2 : from - cap }
        func y0() -> Float { return vertical ? from - cap : at - width/2 }
        func x1() -> Float { return vertical ? at + width/2 : to + cap }
        func y1() -> Float { return vertical ? to + cap : at + width/2 }

        // True when the lines may paint a pixel in common: they are then
        // stroked in the order the cells drew them.
        func overlaps(_ other: Segment) -> Bool {
            let m = 2 * TableLines.tableLineMargin
            return x0() < other.x1() + m && other.x0() < x1() + m &&
                    y0() < other.y1() + m && other.y0() < y1() + m
        }
    }

    // The margin around a line, in points, within which another line may
    // paint a pixel of its own: a line covers whole pixels of a screen or a
    // printer, the hairline of width 0 one at least, so two lines that only
    // meet, end to end or edge to edge, may paint one pixel.
    static let tableLineMargin: Float = 1

    // The lines in the order the cells drew them.
    private var pending = [Segment]()

    // The pen of the borders of the last cell, which a cell drawing its
    // borders itself would have left the page with, and the count of the
    // page's pen changes then: when the pen was not set after it, the lines
    // leave the page with this pen, as the cell would have. The cells do not
    // set the pen of the page themselves, which would write it once for the
    // cell and again for its lines.
    private var hasCellPen = false
    private var cellPenColor: [Float] = [0.0, 0.0, 0.0]
    private var cellPenWidth: Float = 0.0
    private var penChangesAtCell = 0

    // Keeps the borders of a cell that drawBorders would stroke, with the
    // pen of the cell, color and width: its top, bottom, left and right, as
    // it draws them.
    func addBorders(_ page: Page, _ properties: UInt32, _ color: Int32, _ width: Float,
            _ x: Float, _ y: Float, _ cellW: Float, _ cellH: Float) {
        // As setPenColor(Int32) makes the color
        cellPenColor = Util.rgb(color)
        cellPenWidth = width
        hasCellPen = true
        penChangesAtCell = page.penChanges
        // Half the pen width, so that the corners of the borders close.
        let hWidth: Float = width / 2.0
        if properties & Border.TOP != 0 {
            add(page, false, y, x - hWidth, x + cellW)
        }
        if properties & Border.BOTTOM != 0 {
            add(page, false, y + cellH, x - hWidth, x + cellW)
        }
        if properties & Border.LEFT != 0 {
            add(page, true, x, y - hWidth, y + cellH + hWidth)
        }
        if properties & Border.RIGHT != 0 {
            add(page, true, x + cellW, y - hWidth, y + cellH + hWidth)
        }
    }

    private func add(_ page: Page, _ vertical: Bool, _ at: Float, _ from: Float, _ to: Float) {
        let width = cellPenWidth
        let cap: Float = (page.lineCapStyle == CapStyle.BUTT) ? 0.0 : width/2
        pending.append(Segment(vertical, at, from, to,
                cellPenColor[0], cellPenColor[1], cellPenColor[2], width, cap))
    }

    // Writes the lines before a background is filled over the rectangle
    // left, top, right, bottom, when it covers a line, or meets it along an
    // edge: drawn over the background, the line would look different where it
    // is not a whole pixel wide. A line that only touches the rectangle at a
    // corner, as the lines of the row above meet the background of a cell
    // where they end, is kept; one that goes into it, however little, as the
    // end of a wider line does, is written first, so the background covers
    // it as it did.
    func beforeFill(_ page: Page, _ left: Float, _ top: Float, _ right: Float, _ bottom: Float) {
        for line in pending {
            let x0 = TableLines.larger(line.x0(), left)
            let y0 = TableLines.larger(line.y0(), top)
            let x1 = TableLines.smaller(line.x1(), right)
            let y1 = TableLines.smaller(line.y1(), bottom)
            if x0 > x1 || y0 > y1 {
                continue    // Apart
            }
            if (x0 == x1 && y1 - y0 <= line.width) || (y0 == y1 && x1 - x0 <= line.width) {
                continue    // At a corner
            }
            flush(page)
            return
        }
    }

    // The larger and the smaller of two numbers, NaN when either is, as in
    // the other ports: Swift's max and min return the other number.
    static func larger(_ a: Float, _ b: Float) -> Float {
        if a.isNaN || b.isNaN {
            return Float.nan
        }
        return a > b ? a : b
    }

    static func smaller(_ a: Float, _ b: Float) -> Float {
        if a.isNaN || b.isNaN {
            return Float.nan
        }
        return a < b ? a : b
    }

    // Writes the lines kept so far, joined, and leaves the page with the pen
    // it would have had, had each cell drawn its own borders: what is drawn
    // next finds the pen the last cell left, or the one set after it.
    func flush(_ page: Page) {
        if pending.isEmpty {
            return
        }
        var color: [Float]
        var width: Float
        if hasCellPen && page.penChanges == penChangesAtCell {
            color = cellPenColor
            width = cellPenWidth
        } else {
            color = page.getPenColor()
            width = page.getPenWidth()
        }
        hasCellPen = false
        // The lines of a pen go together, unless a line of another pen that
        // a cell drew after them overlaps the line: it starts a new group,
        // after the line of the other pen.
        var groups = [[Segment]]()
        for line in pending {
            var target = -1
            var i = groups.count - 1
            while i >= 0 {
                if groups[i][0].samePen(line) {
                    target = i
                    break
                }
                if TableLines.overlapsAny(groups[i], line) {
                    break
                }
                i -= 1
            }
            if target == -1 {
                groups.append([Segment]())
                target = groups.count - 1
            }
            groups[target].append(line)
        }
        pending.removeAll()

        page.addArtifactBMC()
        for group in groups {
            let first = group[0]
            page.setPenColor([first.r, first.g, first.b])
            page.setPenWidth(first.width)
            // The horizontal lines from the top down, each from the left,
            // then the vertical lines from the left, each from the top.
            let lines = group.sorted(by: { TableLines.compare($0, $1) < 0 })
            // A line that two cells drew, such as the line under the header,
            // which the first row draws over, was stroked twice, darker where
            // it is not a whole pixel wide: it is stroked twice still, once
            // in each of two layers. The ends of the lines of two cells next
            // to each other overlap by no more than the width of the pen.
            var layers = [[Segment]]()
            for next in lines {
                var layer = -1
                var last: Segment? = nil
                for (index, candidate) in layers.enumerated() {
                    let end = candidate[candidate.count - 1]
                    if end.vertical != next.vertical || end.at != next.at ||
                            end.to - next.from <= 1.5*next.width {
                        layer = index
                        last = end
                        break
                    }
                }
                if layer == -1 {
                    layers.append([Segment]())
                    layer = layers.count - 1
                    last = nil
                }
                if let last = last, last.vertical == next.vertical,
                        last.at == next.at, next.from <= last.to {
                    if next.to > last.to {
                        last.to = next.to
                    }
                } else {
                    layers[layer].append(Segment(next.vertical, next.at, next.from, next.to,
                            next.r, next.g, next.b, next.width, next.cap))
                }
            }
            for layer in layers {
                for line in layer {
                    TableLines.draw(page, line)
                }
                page.strokePath()
            }
        }
        page.setPenColor(color)
        page.setPenWidth(width)
        page.addEMC()
    }

    private static func overlapsAny(_ group: [Segment], _ line: Segment) -> Bool {
        for other in group where other.overlaps(line) {
            return true
        }
        return false
    }

    private static func draw(_ page: Page, _ line: Segment) {
        if line.vertical {
            page.moveTo(line.at, line.from)
            page.lineTo(line.at, line.to)
        } else {
            page.moveTo(line.from, line.at)
            page.lineTo(line.to, line.at)
        }
    }

    // Horizontal before vertical, then by where the line is and where it
    // starts and ends: the same order in the four ports.
    private static func compare(_ a: Segment, _ b: Segment) -> Int {
        if a.vertical != b.vertical {
            return a.vertical ? 1 : -1
        }
        var c = compare(a.at, b.at)
        if c == 0 {
            c = compare(a.from, b.from)
        }
        if c == 0 {
            c = compare(a.to, b.to)
        }
        return c
    }

    private static func compare(_ a: Float, _ b: Float) -> Int {
        return (a < b) ? -1 : ((a > b) ? 1 : 0)
    }
}   // End of TableLines.swift
