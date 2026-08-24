/**
 * DonutChart.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create Donut chart objects and draw them on a page
///
/// Please see Example_25.swift
///
public class DonutChart {
    var f1: Font?
    var f2: Font?
    var xc: Float = 0.0
    var yc: Float = 0.0
    var r1: Float = 0.0
    var r2: Float = 0.0
    var slices: [Slice]?
    var isDonutChart = true

    public init(_ f1: Font, _ f2: Font, _ isDonutChart: Bool) {
        self.f1 = f1
        self.f2 = f2
        self.isDonutChart = isDonutChart
        self.slices = [Slice]()
    }

    public func setLocation(_ xc: Float, _ yc: Float) {
        self.xc = xc
        self.yc = yc
    }

    public func setR1AndR2(_ r1: Float, _ r2: Float) {
        self.r1 = r1
        self.r2 = r2
    }

    public func addSlice(_ slice: Slice) {
        self.slices!.append(slice)
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
        let k2 = 4.0/3.0 * (sqrt(2.0*q1*q2) - q2) / (ax*by - ay*bx)

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

        // Draw the first segment: edge → elbow
        page.setPenColor(Color.black)
        page.drawLine(p1[0], p1[1], p2[0], p2[1])

        if f1 != nil && !text.isEmpty {
            let textWidth = f1!.stringWidth(text)
            let onRightSide = cos(midAngle * Float.pi / 180.0) >= 0

            // Horizontal line length = text width + small padding
            let padding: Float = 4.0
            let lineLength = textWidth + padding

            let xEnd: Float = onRightSide ? p2[0] + lineLength : p2[0] - lineLength
            let yEnd: Float = p2[1]

            // Draw the horizontal segment
            page.drawLine(p2[0], p2[1], xEnd, yEnd)

            // Draw the label text just above the horizontal line
            let label = TextLine(f1!, text)
            label.setColor(Color.black)
            if onRightSide {
                label.setLocation(p2[0] + 2.0, yEnd - f1!.getAscent() / 3.0)
            } else {
                label.setLocation(xEnd + 2.0, yEnd - f1!.getAscent() / 3.0)
            }
            label.drawOn(page)
        } else {
            // No text — just draw a short horizontal stub
            let onRightSide = cos(midAngle * Float.pi / 180.0) >= 0
            let xEnd: Float = onRightSide ? p2[0] + 20.0 : p2[0] - 20.0
            page.drawLine(p2[0], p2[1], xEnd, p2[1])
        }
    }

    public func drawOn(_ page: Page) {
        var angle: Float = 0.0
        for slice in slices! {
            angle = drawSlice(
                    page, slice.color,
                    xc, yc,
                    r1, r2,
                    angle, angle + slice.angle)

            drawLinePointer(
                    page, slice.text,
                    xc, yc,
                    r1,
                    angle - slice.angle, angle)
        }
    }
}   // End of DonutChart.swift
