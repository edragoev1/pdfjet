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
/// an inner radius of 0 is a pie chart. See Example_25.
///
public class DonutChart : Drawable {
    var f1: Font?
    var f2: Font?
    var x: Float = 0.0
    var y: Float = 0.0
    var r1: Float = 0.0
    var r2: Float = 0.0
    var slices: [Slice]?
    private var altDescription: String?

    ///
    /// Creates a donut chart. With an inner radius of 0 it is a pie chart.
    ///
    /// - Parameter f1: the font for the slice labels, or nil to draw no labels.
    /// - Parameter f2: the font for the percentages drawn inside the slices, or nil to draw no percentages.
    ///
    public init(_ f1: Font?, _ f2: Font?) {
        self.f1 = f1
        self.f2 = f2
        self.slices = [Slice]()
    }

    ///
    /// Sets the top left corner of the outer circle of this chart. The center
    /// is one outer radius to the right of it and one below. The slice labels
    /// can extend past the circle.
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
        self.r1 = outerRadius
        self.r2 = innerRadius
        return self
    }

    /// Adds a slice to this chart.
    @discardableResult
    public func addSlice(_ slice: Slice) -> DonutChart {
        self.slices!.append(slice)
        return self
    }

    /// Sets the alternate description of the chart, which a screen reader reads
    /// in a PDF/UA document, where the chart is a figure. The default lists the
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
        let k2: Float = (cross == 0.0) ? 0.0 : 4.0/3.0 * (sqrt(2.0*q1*q2) - q2) / cross

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
        let x = xc + radius*(cos(angle*Float.pi/180.0))
        let y = yc + radius*(sin(angle*Float.pi/180.0))
        return [x, y]
    }

    private func drawSlice(
            _ page: Page,
            _ fillColor: Int32,
            _ xc: Float, _ yc: Float,
            _ r1: Float, _ r2: Float,               // r1 > r2
            _ a1: Float, _ a2: Float) -> Float {    // a1 > a2
        page.setBrushColor(fillColor)

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

    private func drawLinePointer(
            _ page: Page,
            _ text: String,
            _ xc: Float, _ yc: Float,
            _ r1: Float,
            _ a1: Float, _ a2: Float) {
        let midAngle = (a1 + a2) / 2.0 - 90.0

        // Point on the outer edge of the donut
        let p1 = getPoint(xc, yc, r1, midAngle)

        // Elbow point — 15pt beyond the outer edge
        let r3 = r1 + 15.0
        let p2 = getPoint(xc, yc, r3, midAngle)

        // Draw the pointer line: edge → elbow → horizontal end
        page.setPenColor(Color.black)
        page.setPenWidth(1.0)
        page.moveTo(p1[0], p1[1])
        page.lineTo(p2[0], p2[1])

        if f1 != nil && !text.isEmpty {
            let textWidth = f1!.stringWidth(text)
            let onRightSide = cos(midAngle * Float.pi / 180.0) >= 0

            let padding: Float = 4.0
            let lineLength = textWidth + padding

            let xEnd: Float = onRightSide ? p2[0] + lineLength : p2[0] - lineLength
            let yEnd: Float = p2[1]

            // Continue the path to the horizontal end
            page.lineTo(xEnd, yEnd)
            page.strokePath()

            // Draw the label text just above the horizontal line
            page.drawString(f1!, f1!.getSize(), text,
                    onRightSide ? p2[0] + 2.0 : xEnd + 2.0, yEnd - f1!.getAscent() / 3.0,
                    Util.toRGB(Color.black), nil)
        } else {
            // No text — short horizontal stub
            let onRightSide = cos(midAngle * Float.pi / 180.0) >= 0
            let xEnd: Float = onRightSide ? p2[0] + 20.0 : p2[0] - 20.0
            page.lineTo(xEnd, p2[1])
            page.strokePath()
        }
    }

    /// Draws this chart on the specified page.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: x and y coordinates of the bottom right corner of the outer
    ///   circle of this chart. The slice labels can extend past it.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let xc = x + r1     // the center of the chart
        let yc = y + r1

        // The slices with a value above 0 share the circle
        var total: Float = 0.0
        for slice in slices! {
            if slice.value > 0.0 {
                total += slice.value
            }
        }
        guard let page = page, total > 0.0 else {
            return [xc + r1, yc + r1]   // Measured, or nothing to draw
        }
        // The chart is one figure, described by its alternate description.
        page.addBDC(StructElem.FIGURE, nil, getAltDescription(total))
        // The pen, the brush and the dash pattern of the caller are kept,
        // as a Stamp and a CalendarMonth keep them: the chart left the page
        // with the black pen of its pointers and the color of its last slice.
        page.saveGraphicsState()
        var angle: Float = 0.0
        for slice in slices! {
            if slice.value <= 0.0 {
                continue
            }
            let sweep = slice.value * 360.0 / total
            angle = drawSlice(
                    page, slice.color,
                    xc, yc,
                    r1, r2,
                    angle, angle + sweep)
            drawLinePointer(
                    page, slice.text,
                    xc, yc,
                    r1,
                    angle - sweep, angle)
            // The percentage fits inside a slice of 15 degrees or more
            if f2 != nil && sweep >= 15.0 {
                let pctStr = DonutChart.percentage(slice, total)
                let midAngle = angle - sweep / 2.0 - 90.0
                let midR = (r1 + r2) / 2.0
                let pos = getPoint(xc, yc, midR, midAngle)
                page.drawString(f2!, f2!.getSize(), pctStr,
                        pos[0] - f2!.stringWidth(pctStr) / 2.0,
                        pos[1] + f2!.getAscent() / 3.0,
                        Util.toRGB(Color.white), nil)
            }
        }
        page.restoreGraphicsState()
        page.addEMC()
        return [xc + r1, yc + r1]
    }

    // Returns the share of the slice in the total, as a whole percentage.
    private static func percentage(_ slice: Slice, _ total: Float) -> String {
        return "\(Int((slice.value * 100.0 / total).rounded(.toNearestOrAwayFromZero)))%"
    }

    // Returns the alternate description, or the label and the percentage of
    // each slice when none is set.
    private func getAltDescription(_ total: Float) -> String {
        if let altDescription, !altDescription.isEmpty {
            return altDescription
        }
        var description = (r2 > 0.0) ? "Donut chart:" : "Pie chart:"
        var separator = " "
        for slice in slices! where slice.value > 0.0 {
            description += separator
            if !slice.text.isEmpty {
                description += slice.text + " "
            }
            description += DonutChart.percentage(slice, total)
            separator = ", "
        }
        return description
    }
}   // End of DonutChart.swift
