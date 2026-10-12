/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// The Swift port's side of the cross-port check,
// tests/crossport/check-crossport.sh: the shapes of tests/crossport/go, drawn
// with the same random numbers, and the content of each page written.
//
//     swift run -c release CrossPortSwift SEED CASES OUT

import Foundation
import PDFjet

// SplitMix64, which every port computes the same.
struct Random {
    var state: UInt64

    mutating func next() -> UInt64 {
        state = state &+ 0x9E37_79B9_7F4A_7C15
        var z = state
        z = (z ^ (z >> 30)) &* 0xBF58_476D_1CE4_E5B9
        z = (z ^ (z >> 27)) &* 0x94D0_49BB_1331_11EB
        return z ^ (z >> 31)
    }

    // A number from lo up to hi, of 24 bits, which a Float holds exactly.
    mutating func between(_ lo: Float, _ hi: Float) -> Float {
        let unit = Float(next() >> 40) / 16_777_216
        return lo + unit * (hi - lo)
    }

    // A whole number from 0 up to n.
    mutating func below(_ n: Int) -> Int {
        return Int(next() % UInt64(n))
    }

    mutating func color() -> Int32 {
        return Int32(below(0x100_0000))
    }
}

// Gives the arc a stroke width, a dash pattern or none, a fill color or none,
// and a rotation.
func styleArc(_ r: inout Random, _ arc: Arc) {
    arc.setStrokeWidth(r.between(0, 10))
    if r.below(2) == 0 {
        let on = 1 + r.below(9)
        let off = 1 + r.below(9)
        arc.setStrokeDashPattern("[\(on) \(off)] 0")
    }
    if r.below(2) == 0 {
        arc.setFillColor(r.color())
    }
    arc.setRotation(r.between(-360, 360))
}

// Draws the shapes of one case on a page of a document of its own, and returns
// the content of the page, or "error" when the document fails.
func drawCase(_ r: inout Random) -> [UInt8] {
    do {
        let pdf = PDF(OutputStream(toMemory: ()))
        let font = try Font(pdf, CoreFont.HELVETICA)
        let page = Page(pdf, Letter.PORTRAIT)
        let shapes = 1 + r.below(4)
        for k in 0..<shapes {
            switch r.below(5) {
            case 0:
                let arc = Arc()
                arc.setLocation(r.between(0, 612), r.between(0, 792))
                arc.setRadiusX(r.between(0, 300))
                arc.setRadiusY(r.between(0, 300))
                arc.setStartAngle(r.between(-720, 720))
                arc.setSweep(r.between(-720, 720))
                styleArc(&r, arc)
                arc.drawOn(page)
            case 1:
                // The whole of an ellipse, which a random sweep never is
                let ellipse = Ellipse()
                ellipse.setLocation(r.between(0, 612), r.between(0, 792))
                ellipse.setRadiusX(r.between(0, 300))
                ellipse.setRadiusY(r.between(0, 300))
                styleArc(&r, ellipse)
                ellipse.drawOn(page)
            case 2:
                let chart = DonutChart(font, font)
                chart.setLocation(r.between(0, 612), r.between(0, 792))
                let outer = r.between(1, 200)
                chart.setRadii(outer, r.between(0, outer))
                let slices = 1 + r.below(6)
                for s in 0..<slices {
                    chart.addSlice(Slice(r.between(0, 100), r.color(), "s\(s)"))
                }
                chart.drawOn(page)
            case 3:
                let path = Path()
                let points = 2 + r.below(6)
                for p in 0..<points {
                    let x = r.between(0, 612)
                    let y = r.between(0, 792)
                    if p < points - 1 && r.below(3) == 0 {
                        path.add(Point(x, y, Point.CONTROL_POINT_C))
                    } else {
                        path.add(Point(x, y))
                    }
                }
                path.setClosed(r.below(2) == 0)
                path.setFillShape(r.below(2) == 0)
                path.setStrokeWidth(r.between(0, 5))
                path.drawOn(page)
            default:
                let line = TextLine(font, "Text \(k)")
                line.setFontSize(r.between(4, 40))
                line.setLocation(r.between(0, 612), r.between(0, 792))
                line.setTextRotation(r.below(361) - 180)
                line.drawOn(page)
            }
        }
        let content = page.getContent()
        try pdf.complete()
        return content + [UInt8(ascii: "\n")]
    } catch {
        return Array("error\n".utf8)
    }
}

let arguments = CommandLine.arguments
let seed = UInt64(arguments[1])!
let cases = Int(arguments[2])!
var output = [UInt8]()
for i in 0..<cases {
    output += Array("case \(i)\n".utf8)
    var r = Random(state: seed &+ UInt64(i))
    output += drawCase(&r)
}
if !FileManager.default.createFile(atPath: arguments[3], contents: Data(output)) {
    fatalError("cannot write \(arguments[3])")
}
