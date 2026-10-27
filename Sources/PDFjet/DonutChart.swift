/**
 * DonutChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A donut or pie chart: each slice is its value's share of the sum of the
/// values, with a label next to it and its percentage inside it. A chart with
/// an inner radius of 0 is a pie chart. In the look of the other charts: a
/// title and a subtitle at the top left, thin white lines between the slices,
/// gray leader lines, the labels in ink, and each percentage white or ink by
/// its slice's color. See Example_28.
///
public class DonutChart : Drawable {
    var f1: Font?   // The regular font: the labels, the subtitle and the legend
    var f2: Font?   // The bold font: the percentages, the title and the text in the hole
    var x: Float = 0.0
    var y: Float = 0.0
    var r1: Float = 0.0
    var r2: Float = 0.0
    private var slices: [Slice]?
    private var altDescription: String?
    private var title = ""
    private var subtitle = ""
    private var centerText = ""
    private var legend = false

    // The look of the simple charts
    private static let INK = SimpleBarChart.INK
    private static let MUTED = SimpleBarChart.MUTED
    private static let LEADER: Int32 = 0x8C96A3

    ///
    /// Creates a donut chart. With an inner radius of 0 it is a pie chart. Its
    /// fonts are in the order of the other charts': the bold one first.
    ///
    /// - Parameter bold: the bold font: the percentages inside the slices, the
    ///   title and the text in the hole; or nil to draw no percentages.
    /// - Parameter regular: the regular font: the slice labels, the subtitle
    ///   and the legend, and the size of the title; or nil to draw no labels.
    ///
    public init(_ bold: Font?, _ regular: Font?) {
        self.f1 = regular
        self.f2 = bold
        self.slices = [Slice]()
    }

    ///
    /// Sets the top left corner of this chart: of its outer circle, or of its
    /// title when it has one, the circle under it. The center is one outer
    /// radius to the right of the corner of the circle and one below. The
    /// slice labels can extend past the circle.
    ///
    /// - Parameter x: the x coordinate of the top left corner.
    /// - Parameter y: the y coordinate of the top left corner.
    /// - Returns: this DonutChart object.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the outer and inner radius of this chart. An inner radius of 0 makes a pie chart.
    @discardableResult
    public func setRadii(_ outerRadius: Float, _ innerRadius: Float) -> DonutChart {
        // The larger is the outer radius: an inner radius past the outer one
        // drew a ring inside out
        self.r1 = Swift.max(abs(outerRadius), abs(innerRadius))
        self.r2 = Swift.min(abs(outerRadius), abs(innerRadius))
        return self
    }

    /// Adds a slice in the next color of the palette, navy first, the palette
    /// of BarChart, so that the charts of a document match; its eight colors
    /// repeat from the ninth slice on. The slice's share of the sum of the
    /// values sets its angle; a value that is not above 0 has no slice.
    ///
    /// - Parameter name: the label of the slice; empty for none.
    /// - Parameter value: the value of the slice.
    /// - Returns: this DonutChart object.
    @discardableResult
    public func add(_ name: String, _ value: Float) -> DonutChart {
        return add(name, value, Slice.PALETTE)
    }

    /// Adds a slice in the color.
    ///
    /// - Parameter name: the label of the slice; empty for none.
    /// - Parameter value: the value of the slice.
    /// - Parameter color: the color as a 0xRRGGBB value, for example Color.blue.
    /// - Returns: this DonutChart object.
    @discardableResult
    public func add(_ name: String, _ value: Float, _ color: Int32) -> DonutChart {
        self.slices!.append(Slice(value, color, name))
        return self
    }

    // A slice: its value, its color, or PALETTE for the next of the palette,
    // and its label.
    private struct Slice {
        static let PALETTE: Int32 = -1

        let value: Float
        let color: Int32
        let text: String

        init(_ value: Float, _ color: Int32, _ text: String) {
            self.value = value
            self.color = color
            self.text = text
        }
    }

    /// Sets the title, drawn at the top left of the chart's location in the bold
    /// font at the size of the labels; the circle is drawn under it. A chart
    /// without the bold font, which draws no percentages, draws its title in the
    /// regular one. A chart without a title has the circle at its location.
    @discardableResult
    public func setTitle(_ title: String) -> DonutChart {
        self.title = title
        return self
    }

    /// Sets the subtitle, drawn under the title in a muted gray.
    @discardableResult
    public func setSubtitle(_ subtitle: String) -> DonutChart {
        self.subtitle = subtitle
        return self
    }

    /// Sets the text written in the hole of a donut chart, in the font of the
    /// title, as large as one and a half labels or as fits: the total, "100
    /// crates". A pie chart, with no hole, does not write it.
    @discardableResult
    public func setCenterText(_ centerText: String) -> DonutChart {
        self.centerText = centerText
        return self
    }

    /// Draws a legend under the circle, a swatch, the label and the percentage
    /// of each slice, in rows as wide as the circle, in place of the leader
    /// lines and the labels beside the circle: for many slices, or small ones
    /// whose labels would crowd.
    @discardableResult
    public func setLegend(_ legend: Bool) -> DonutChart {
        self.legend = legend
        return self
    }

    /// Sets the alternate description of the chart, which a screen reader reads
    /// in a PDF/UA document, where the chart is a figure. The default is the
    /// title, the subtitle and the text in the hole, as sentences, and the
    /// label and the percentage of each slice.
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> DonutChart {
        self.altDescription = altDescription
        return self
    }

    private func getControlPoints(
            _ xc: Float, _ yc: Float,
            _ x0: Float, _ y0: Float,
            _ x3: Float, _ y3: Float) -> [[Float]] {
        var points = [[Float]]()

        let ax = x0 - xc
        let ay = y0 - yc
        let bx = x3 - xc
        let by = y3 - yc
        let q1 = ax*ax + ay*ay
        let q2 = q1 + ax*bx + ay*by
        let cross = ax*by - ay*bx
        // An arc of radius zero, the center of a pie chart, or of no angle has
        // its control points at its ends; the formula would divide 0 by 0.
        let k2: Float = (cross == 0.0) ? 0.0 : 4.0/3.0 * ((2.0*q1*q2).squareRoot() - q2) / cross

        // Control points coordinates
        let x1 = xc + ax - k2*ay
        let y1 = yc + ay + k2*ax
        let x2 = xc + bx + k2*by
        let y2 = yc + by - k2*bx

        points.append([x0, y0])
        points.append([x1, y1])
        points.append([x2, y2])
        points.append([x3, y3])

        return points
    }

    private func getPoint(
            _ xc: Float, _ yc: Float, _ radius: Float, _ angle: Float) -> [Float] {
        // The cosine and the sine of the angle in double precision, as Go
        // computes them, for the same chart in every port (review of
        // 11 October 2026, #25)
        let x = xc + radius*Float(cos(Double(angle)*Double.pi/180.0))
        let y = yc + radius*Float(sin(Double(angle)*Double.pi/180.0))
        return [x, y]
    }

    private func drawSlice(
            _ page: Page,
            _ fillColor: Int32,
            _ xc: Float, _ yc: Float,
            _ r1: Float, _ r2: Float,               // r1 > r2
            _ a1: Float, _ a2: Float) -> Float {    // a1 > a2
        page.setBrushColor(fillColor)

        // No arc of angles that are not numbers, whose loop never ended
        if !ChartScale.isValue(a1) || !ChartScale.isValue(a2) {
            return a1
        }
        var angle1 = a1 - 90.0
        let angle2 = a2 - 90.0

        var points1 = [[Float]]()
        var points2 = [[Float]]()
        while true {
            if (angle2 - angle1) <= 90.0 {
                var p0 = getPoint(xc, yc, r1, angle1)           // Start point
                var p3 = getPoint(xc, yc, r1, angle2)           // End point
                points1.append(contentsOf: getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]))
                p0 = getPoint(xc, yc, r2, angle1)               // Start point
                p3 = getPoint(xc, yc, r2, angle2)               // End point
                points2.append(contentsOf: getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]))
                break
            } else {
                var p0 = getPoint(xc, yc, r1, angle1)
                var p3 = getPoint(xc, yc, r1, angle1 + 90.0)
                points1.append(contentsOf: getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]))
                p0 = getPoint(xc, yc, r2, angle1)
                p3 = getPoint(xc, yc, r2, angle1 + 90.0)
                points2.append(contentsOf: getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1]))
                angle1 += 90.0
            }
        }
        points2.reverse()

        page.moveTo(points1[0][0], points1[0][1])
        var i = 0
        while i <= (points1.count - 4) {
            page.curveTo(
                    points1[i + 1][0], points1[i + 1][1],
                    points1[i + 2][0], points1[i + 2][1],
                    points1[i + 3][0], points1[i + 3][1])
            i += 4
        }
        page.lineTo(points2[0][0], points2[0][1])
        i = 0
        while i <= (points2.count - 4) {
            page.curveTo(
                    points2[i + 1][0], points2[i + 1][1],
                    points2[i + 2][0], points2[i + 2][1],
                    points2[i + 3][0], points2[i + 3][1])
            i += 4
        }
        page.fillPath()

        return a2
    }

    // The leader line and the label of a slice: the point on the edge of the
    // circle, the elbow 15 points past it, the y of the label's line, moved
    // down from the elbow's when it would meet the label above it, and the
    // side of the circle it is on.
    private final class Pointer {
        let text: String
        let p1: [Float]
        let p2: [Float]
        let right: Bool
        var xElbow: Float
        var yEnd: Float

        init(_ text: String, _ p1: [Float], _ p2: [Float], _ right: Bool) {
            self.text = text
            self.p1 = p1
            self.p2 = p2
            self.right = right
            self.xElbow = p2[0]
            self.yEnd = p2[1]
        }
    }

    // Returns the pointer of a slice from the angle a1 to a2.
    private func pointerOf(_ text: String, _ xc: Float, _ yc: Float, _ r1: Float, _ a1: Float, _ a2: Float) -> Pointer {
        let midAngle = (a1 + a2) / 2.0 - 90.0
        let p1 = getPoint(xc, yc, r1, midAngle)            // On the outer edge
        let p2 = getPoint(xc, yc, r1 + 15.0, midAngle)     // The elbow
        // A label wider than the radius, or than six of its letters, is
        // shortened: a long one at the left ran off the page
        var text = text
        if let f1 = f1 {
            text = ChartScale.shorten(f1, f1.getSize(), text, Swift.max(r1, 6.0 * f1.getSize()))
        }
        return Pointer(text, p1, p2, cos(Double(midAngle) * Double.pi / 180.0) >= 0)
    }

    // Moves the labels on each side of the circle apart, top to bottom, where
    // one would meet the one above it, and each moved label's elbow out to
    // where its height leaves the circle, 15 points past its edge: thin slices
    // side by side stacked their labels down a column across the ring.
    private func moveApart(_ pointers: [Pointer], _ xc: Float, _ yc: Float) {
        guard let f1 = f1 else {
            return
        }
        for side in 0..<2 {
            // The indexes of the pointers with a label on the side
            var labels = [Int]()
            for i in 0..<pointers.count {
                if pointers[i].right == (side == 0) && !pointers[i].text.isEmpty {
                    labels.append(i)
                }
            }
            // Top to bottom, and labels at one height in the order of their
            // slices: Swift's sort is not stable
            labels.sort { a, b in
                pointers[a].yEnd != pointers[b].yEnd ? pointers[a].yEnd < pointers[b].yEnd : a < b
            }
            if labels.count > 1 {
                for k in 1..<labels.count {
                    pointers[labels[k]].yEnd = Swift.max(pointers[labels[k]].yEnd,
                            pointers[labels[k - 1]].yEnd + f1.getBodyHeight())
                }
            }
            let r3 = r1 + 15.0
            for k in labels {
                let label = pointers[k]
                if label.yEnd == label.p2[1] {
                    continue    // Where it was
                }
                let dy = label.yEnd - yc
                let out: Float = (abs(dy) < r3)
                        ? Float((Double(r3) * Double(r3) - Double(dy) * Double(dy)).squareRoot()) : 0.0
                label.xElbow = label.right ? Swift.max(label.p2[0], xc + out) : Swift.min(label.p2[0], xc - out)
            }
        }
    }

    // Returns the box of the leader line and the label of the pointer: its
    // left, top, right and bottom; and draws them when there is a page.
    @discardableResult
    private func drawPointer(_ page: Page?, _ pointer: Pointer) -> [Float] {
        let p1 = pointer.p1
        let x2 = pointer.xElbow
        let yEnd = pointer.yEnd
        if let page = page {
            // The line: edge, elbow, and along to the end, thin and gray
            page.setPenColor(DonutChart.LEADER)
            page.setPenWidth(0.75)
            page.moveTo(p1[0], p1[1])
            page.lineTo(x2, yEnd)
        }
        if let f1 = f1, !pointer.text.isEmpty {
            let textWidth = f1.stringWidth(pointer.text)
            let lineLength = textWidth + 4.0
            let xEnd: Float = pointer.right ? x2 + lineLength : x2 - lineLength
            // The label just above the line
            let x = pointer.right ? x2 + 2.0 : xEnd + 2.0
            let baseline = yEnd - f1.getAscent() / 3.0
            if let page = page {
                page.lineTo(xEnd, yEnd)
                page.strokePath()
                page.drawString(f1, f1.getSize(), pointer.text, x, baseline, Util.toRGB(DonutChart.INK), nil)
            }
            return [Swift.min(p1[0], x2, xEnd, x),
                    Swift.min(p1[1], yEnd, baseline - f1.getAscent()),
                    Swift.max(p1[0], x2, xEnd, x + textWidth),
                    Swift.max(p1[1], yEnd, baseline + f1.getDescent(f1.getSize()))]
        }
        // No text: a short line along
        let xEnd: Float = pointer.right ? x2 + 20.0 : x2 - 20.0
        if let page = page {
            page.lineTo(xEnd, yEnd)
            page.strokePath()
        }
        return [Swift.min(p1[0], x2, xEnd), Swift.min(p1[1], yEnd),
                Swift.max(p1[0], x2, xEnd), Swift.max(p1[1], yEnd)]
    }

    /// Draws this chart on the specified page.
    ///
    /// - Parameter page: the page to draw on, or nil to measure the chart.
    /// - Returns: x and y coordinates of the bottom right corner of the chart:
    ///   of its title, its circle, and its leader lines and labels, which can
    ///   reach past the circle, or its legend. A chart with no slice above 0
    ///   draws nothing, not its title either, and returns the corner of its
    ///   circle.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        // The title and the subtitle above the circle, when there are any
        let titleFont = f2 ?? f1
        let titleSize: Float = f1?.getSize() ?? titleFont?.getSize() ?? 0.0
        var top = y
        var titleBaseline: Float = 0.0
        var subtitleBaseline: Float = 0.0
        if let titleFont = titleFont, !title.isEmpty {
            titleBaseline = top + titleFont.getAscent(titleSize)
            top = titleBaseline + titleFont.getDescent(titleSize)
        }
        if let f1 = f1, !subtitle.isEmpty {
            subtitleBaseline = top + f1.getAscent()
            top = subtitleBaseline + f1.getDescent()
        }
        if top > y {
            // Room for a label at the top of the circle: its leader line goes
            // 15 points up, the label sits above its end; and half a line
            let label: Float = (f1 != nil && !legend) ? f1!.getAscent() / 3 + f1!.getAscent() : 0
            top += (legend ? 0 : 15) + label + titleSize / 2
        }
        let xc = x + r1     // the center of the chart
        let yc = top + r1

        // The slices with a value above 0 share the circle
        let values = self.values()
        var total: Float = 0.0
        for value in values {
            total += value
        }
        if total <= 0.0 {   // Nothing to draw
            return [xc + r1, yc + r1]
        }
        let percentages = DonutChart.percentages(values, total)

        // The leader lines and the labels, moved apart, and the box of the
        // figure: the title, the outer circle, the leader lines and the labels
        var pointers = [Pointer]()
        var colors = [Int32](repeating: 0, count: slices!.count)
        var angle: Float = 0.0
        var count = 0
        for i in 0..<slices!.count {
            if values[i] > 0.0 {
                let sweep = values[i] * 360.0 / total
                if !legend {
                    pointers.append(pointerOf(slices![i].text, xc, yc, r1, angle, angle + sweep))
                }
                angle += sweep
                let color = slices![i].color
                colors[i] = (color == Slice.PALETTE) ? BarChart.PALETTE[count % BarChart.PALETTE.count] : color
                count += 1
            }
        }
        // The last slice, next to the first around the circle, in the palette's
        // next color when it would have the first's: the ninth of nine was navy
        // by the navy first
        var first = -1
        var last = -1
        for i in 0..<slices!.count {
            if values[i] > 0.0 {
                first = (first < 0) ? i : first
                last = i
            }
        }
        if count > 1 && slices![last].color == Slice.PALETTE && colors[last] == colors[first] {
            colors[last] = BarChart.PALETTE[count % BarChart.PALETTE.count]
        }
        moveApart(pointers, xc, yc)
        var box: [Float] = [x, y, x + 2 * r1, yc + r1]
        if titleBaseline > 0.0, let titleFont = titleFont {
            box[2] = Swift.max(box[2], x + titleFont.stringWidth(titleSize, title))
        }
        if subtitleBaseline > 0.0, let f1 = f1 {
            box[2] = Swift.max(box[2], x + f1.stringWidth(subtitle))
        }
        for pointer in pointers {
            box = DonutChart.union(box, drawPointer(nil, pointer))
        }
        if legend {
            box = DonutChart.union(box, drawLegend(nil, values, percentages, colors, yc + r1))
        }
        guard let page = page else {    // Measured
            return [box[2], box[3]]
        }

        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, nil, getAltDescription(values, percentages))
        // The pen, the brush and the dash pattern of the caller are kept,
        // as a Stamp keeps them: the chart left the page
        // with the black pen of its pointers and the color of its last slice.
        page.saveGraphicsState()
        page.setDefaultStrokeDashPattern()      // Not the caller's, which dashed the leader lines
        if titleBaseline > 0.0, let titleFont = titleFont {
            page.drawString(titleFont, titleSize, title, x, titleBaseline, Util.toRGB(DonutChart.INK), nil)
        }
        if subtitleBaseline > 0.0, let f1 = f1 {
            page.drawString(f1, f1.getSize(), subtitle, x, subtitleBaseline, Util.toRGB(DonutChart.MUTED), nil)
        }
        angle = 0.0
        var drawn = 0
        var edges = [Float]()
        var sweeps = [Float]()
        for i in 0..<slices!.count {
            if values[i] <= 0.0 {
                continue
            }
            let color = colors[i]
            let sweep = values[i] * 360.0 / total
            angle = drawSlice(
                    page, color,
                    xc, yc,
                    r1, r2,
                    angle, angle + sweep)
            edges.append(angle)
            sweeps.append(sweep)
            if !legend {
                drawPointer(page, pointers[drawn])
            }
            drawn += 1
            // The percentage fits inside a slice of 15 degrees or more
            if f2 != nil && sweep >= 15.0 {
                let pctStr = "\(percentages[i])%"
                let midAngle = angle - sweep / 2.0 - 90.0
                let midR = (r1 + r2) / 2.0
                let pos = getPoint(xc, yc, midR, midAngle)
                page.drawString(f2!, f2!.getSize(), pctStr,
                        pos[0] - f2!.stringWidth(pctStr) / 2.0,
                        pos[1] + f2!.getAscent() / 3.0,
                        Util.toRGB(BarChart.labelColorOn(color)), nil)
            }
        }
        // A thin white line between the slices, from the hole to the edge, at
        // most a quarter as wide as the thinner of the two slices is across
        // the middle of the ring, so that it does not cover a thin slice
        if drawn > 1 {
            page.setPenColor(Color.white)
            let midR = (r1 + r2) / 2.0
            for k in 0..<drawn {
                let thinner = Swift.min(sweeps[k], sweeps[(k + 1) % drawn])
                let across = midR * Float(Double(thinner) * Double.pi / 180.0)
                page.setPenWidth(Swift.min(1.5, 0.25 * across))
                let edge = edges[k]
                let inner = getPoint(xc, yc, r2, edge - 90.0)
                let outer = getPoint(xc, yc, r1, edge - 90.0)
                page.drawLine(inner[0], inner[1], outer[0], outer[1])
            }
        }
        if legend {
            drawLegend(page, values, percentages, colors, yc + r1)
        }
        // The text in the hole, as large as one and a half labels or as fits
        if r2 > 0.0, let titleFont = titleFont, !centerText.isEmpty {
            // At three quarters of a label at least, shortened to fit there:
            // a long text was drawn as small as it took, too small to read
            var size = 1.5 * titleSize
            var text = centerText
            var width = titleFont.stringWidth(size, text)
            if width > 1.6 * r2 {
                size = Swift.max(size * (1.6 * r2) / width, 0.75 * titleSize)
                text = ChartScale.shorten(titleFont, size, text, 1.6 * r2)
                width = titleFont.stringWidth(size, text)
            }
            let baseline = yc + (titleFont.getAscent(size) - titleFont.getDescent(size)) / 2.0
            page.drawString(titleFont, size, text, xc - width / 2.0, baseline, Util.toRGB(DonutChart.INK), nil)
        }
        page.restoreGraphicsState()
        page.setFigureBoundingBox(box[0], box[1], box[2] - box[0], box[3] - box[1])
        page.addEMC()
        return [box[2], box[3]]
    }

    // Returns the box of the legend under the circle, whose bottom is at the
    // y: a swatch, the label and the percentage of each slice, in rows as wide
    // as the circle; and draws it when there is a page.
    @discardableResult
    private func drawLegend(_ page: Page?, _ values: [Float], _ percentages: [Int], _ colors: [Int32],
            _ y: Float) -> [Float] {
        var box: [Float] = [x, y, x, y]
        guard let f1 = f1 else {
            return box
        }
        let swatch = f1.getAscent()
        let gap = swatch / 2.0
        var left = x
        var baseline = y + 1.5 * f1.getBodyHeight()
        for i in 0..<slices!.count {
            if values[i] <= 0.0 {
                continue
            }
            // A label too long for a row is shortened, its percentage kept
            let percentage = "\(percentages[i])%"
            var text = percentage
            if !slices![i].text.isEmpty {
                let room = 2.0 * r1 - swatch - gap - f1.stringWidth(" " + percentage)
                text = ChartScale.shorten(f1, f1.getSize(), slices![i].text, room) + " " + percentage
            }
            let width = f1.stringWidth(text)
            if left > x && left + swatch + gap + width > x + 2.0 * r1 {
                left = x
                baseline += 1.5 * f1.getBodyHeight()
            }
            if let page = page {
                page.setBrushColor(colors[i])
                page.fillRect(left, baseline - swatch, swatch, swatch)
                page.drawString(f1, f1.getSize(), text, left + swatch + gap, baseline, Util.toRGB(DonutChart.INK), nil)
            }
            box[2] = Swift.max(box[2], left + swatch + gap + width)
            box[3] = baseline + f1.getDescent()
            left += swatch + gap + width + f1.getBodyHeight()
        }
        return box
    }

    // Returns the box that holds the two boxes, each left, top, right, bottom.
    private static func union(_ a: [Float], _ b: [Float]) -> [Float] {
        return [Swift.min(a[0], b[0]), Swift.min(a[1], b[1]),
                Swift.max(a[2], b[2]), Swift.max(a[3], b[3])]
    }

    // Returns the value of each slice that is drawn, and 0 for the others: a
    // slice is drawn when its value is a number above 0. A NaN, from 0/0 in
    // an empty category of real data, or an infinity made the arc's loop
    // endless, until memory ran out (review of 11 October 2026, #2). Values
    // whose 360ths or whose sum are past the largest Float are divided by the
    // largest of them, so that they are drawn as they compare.
    private func values() -> [Float] {
        var values = [Float](repeating: 0.0, count: slices!.count)
        var largest: Float = 0.0
        var total: Float = 0.0
        for i in 0..<values.count {
            let value = slices![i].value
            if value > 0.0 && ChartScale.isValue(value) {
                values[i] = value
                largest = Swift.max(largest, value)
                total += value
            }
        }
        if largest > 0.0 && (!ChartScale.isValue(largest * 360.0) || !ChartScale.isValue(total)) {
            for i in 0..<values.count {
                values[i] = values[i] / largest
            }
        }
        return values
    }

    // Returns the whole percentage of each slice that is drawn, by the largest
    // remainder, so that they add up to 100: each rounded alone, 90 and six of
    // 1.5 were 91 and six of 2.
    private static func percentages(_ values: [Float], _ total: Float) -> [Int] {
        var percentages = [Int](repeating: 0, count: values.count)
        var remainders = [Double](repeating: 0.0, count: values.count)
        var sum = 0
        for i in 0..<values.count {
            remainders[i] = -1.0
            if values[i] > 0.0 {
                let exact = Double(values[i]) * 100.0 / Double(total)
                percentages[i] = Int(floor(exact))
                remainders[i] = exact - Double(percentages[i])
                sum += percentages[i]
            }
        }
        // The points left over to the largest remainders, the first of equal ones
        while sum < 100 {
            var largest = -1
            for i in 0..<values.count {
                if remainders[i] >= 0.0 && (largest < 0 || remainders[i] > remainders[largest]) {
                    largest = i
                }
            }
            if largest < 0 {
                break
            }
            percentages[largest] += 1
            remainders[largest] = -1.0
            sum += 1
        }
        return percentages
    }

    // Returns the alternate description, or the title, the subtitle, and the
    // label and the percentage of each slice when none is set.
    private func getAltDescription(_ values: [Float], _ percentages: [Int]) -> String {
        if let altDescription, !altDescription.isEmpty {
            return altDescription
        }
        var name = ChartScale.join(title, subtitle)
        if r2 > 0.0 && (f1 != nil || f2 != nil) {
            name = ChartScale.join(name, centerText)    // Drawn in the hole
        }
        if name.isEmpty {
            name = (r2 > 0.0) ? "Donut chart" : "Pie chart"
        }
        var description = name + ":"
        var separator = " "
        for i in 0..<slices!.count where values[i] > 0.0 {
            description += separator
            if !slices![i].text.isEmpty {
                description += slices![i].text + " "
            }
            description += "\(percentages[i])%"
            separator = ", "
        }
        return description
    }
}   // End of DonutChart.swift
