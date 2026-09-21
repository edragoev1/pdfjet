/**
 * SVG.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// Converts SVG path data to PDF path operations.
class SVG {

    static func isCommand(_ ch: Character) -> Bool {
        // Capital letter commands use absolute coordinates
        // Small letter commands use relative coordinates
        switch ch {
        case "M", "m":  // moveto
            return true
        case "L", "l":  // lineto
            return true
        case "H", "h":  // horizontal lineto
            return true
        case "V", "v":  // vertical lineto
            return true
        case "Q", "q":  // quadratic curveto
            return true
        case "T", "t":  // smooth quadratic curveto
            return true
        case "C", "c":  // cubic curveto
            return true
        case "S", "s":  // smooth cubic curveto
            return true
        case "A", "a":  // elliptical arc
            return true
        case "Z", "z":  // close path
            return true
        default:
            return false
        }
    }

    /// Parses SVG path data into a list of path operations.
    // Returns true when the character separates the numbers of path data: the
    // white space of SVG 1.1 section 8.3.9, which is the white space of XML.
    // Path data is often written over several lines.
    private static func isSpace(_ ch: Character) -> Bool {
        return ch == " " || ch == "\t" || ch == "\n" || ch == "\r"
    }

    // Returns true when the number so far ends with the e of an exponent, so
    // that the sign of the exponent belongs to it: the numbers of path data
    // are written as SVG 1.1 section 8.3.9 gives them, sign, digits, point
    // and exponent.
    private static func afterExponent(_ buf: String) -> Bool {
        return buf.hasSuffix("e") || buf.hasSuffix("E")
    }

    static func getOperations(_ path: String) -> [PathOp] {
        var operations = [PathOp]()
        // Path data starts with a command; the numbers before the first one
        // belong to this operation, which is not added to the list.
        var op = PathOp(" ")
        var buf = String()
        var token = false
        for ch in path {
            if isCommand(ch) {          // open path
                if token {
                    op.args.append(buf)
                    buf = ""
                }
                token = false
                op = PathOp(ch)
                operations.append(op)
            } else if isSpace(ch) || ch == "," {
                if token {
                    op.args.append(buf)
                    buf = ""
                }
                token = false
            } else if ch == "-" || ch == "+" {
                // The sign starts a number, unless it is the sign of an
                // exponent: 1e-3 is one number, and 10-3 is two.
                if token && !afterExponent(buf) {
                    op.args.append(buf)
                    buf = ""
                }
                token = true
                buf.append(ch)
            } else if ch == "." {
                if buf.contains(".") {
                    op.args.append(buf)
                    buf = ""
                }
                token = true
                buf.append(ch)
            } else {
                token = true
                buf.append(ch)
            }
        }
        if token {  // The last number of a path that does not end with Z
            op.args.append(buf)
        }
        return operations
    }

    /// Returns the argument as a number, and throws when it is not one, as the
    /// other ports fail on such path data rather than stopping the program.
    private static func number(_ args: [String], _ i: Int) throws -> Float {
        guard i < args.count, let value = Float(args[i]) else {
            throw PDFjetError(message: "Invalid path data: " +
                    (i < args.count ? args[i] : "missing argument"))
        }
        return value
    }

    /// Converts SVG path operations to PDF path operations.
    static func toPDF(_ list: [PathOp]) throws -> [PathOp] {
        var operations = [PathOp]()
        // The current point starts at the origin: path data that begins with a
        // command that needs one is drawn from there rather than stopping.
        var lastOp = PathOp(" ")
        var x0: Float = 0.0 // Start of subpath
        var y0: Float = 0.0
        for op in list {
            if op.cmd == "M" || op.cmd == "m" {
                var i: Int = 0
                while i <= (op.args.count - 2) {
                    let pathOp: PathOp
                    var x = try number(op.args, i)
                    var y = try number(op.args, i + 1)
                    if op.cmd == "m" {
                        x += lastOp.x
                        y += lastOp.y
                    }
                    if i == 0 {
                        x0 = x
                        y0 = y
                        pathOp = PathOp("M", x, y)
                    } else {
                        pathOp = PathOp("L", x, y)
                    }
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 2
                }
            } else if op.cmd == "L" || op.cmd == "l" {
                var i: Int = 0
                while i <= (op.args.count - 2) {
                    var x = try number(op.args, i)
                    var y = try number(op.args, i + 1)
                    if op.cmd == "l" {
                        x += lastOp.x
                        y += lastOp.y
                    }
                    let pathOp = PathOp("L", x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 2
                }
            } else if op.cmd == "H" || op.cmd == "h" {
                var i: Int = 0
                while i < op.args.count {
                    var x = try number(op.args, i)
                    if op.cmd == "h" {
                        x += lastOp.x
                    }
                    let pathOp = PathOp("L", x, lastOp.y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 1
                }
            } else if op.cmd == "V" || op.cmd == "v" {
                var i: Int = 0
                while i < op.args.count {
                    var y = try number(op.args, i)
                    if op.cmd == "v" {
                        y += lastOp.y
                    }
                    let pathOp = PathOp("L", lastOp.x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 1
                }
            } else if op.cmd == "Q" || op.cmd == "q" {
                var i: Int = 0
                while i <= (op.args.count - 4) {
                    let pathOp = PathOp("C")
                    var x1 = try number(op.args, i)
                    var y1 = try number(op.args, i + 1)
                    var x = try number(op.args, i + 2)
                    var y = try number(op.args, i + 3)
                    if op.cmd == "q" {
                        x1 += lastOp.x
                        y1 += lastOp.y
                        x += lastOp.x
                        y += lastOp.y
                    }
                    // Save the original control point
                    pathOp.x1q = x1
                    pathOp.y1q = y1
                    // Calculate the coordinates of the cubic control points
                    let x1c = lastOp.x + (2.0/3.0)*(x1 - lastOp.x)
                    let y1c = lastOp.y + (2.0/3.0)*(y1 - lastOp.y)
                    let x2c = x + (2.0/3.0)*(x1 - x)
                    let y2c = y + (2.0/3.0)*(y1 - y)
                    pathOp.setCubicPoints(x1c, y1c, x2c, y2c, x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 4
                }
            } else if op.cmd == "T" || op.cmd == "t" {
                var i: Int = 0
                while i <= (op.args.count - 2) {
                    let pathOp = PathOp("C")
                    var x1 = lastOp.x
                    var y1 = lastOp.y
                    if lastOp.cmd == "C" {
                        // Find the reflection control point
                        x1 = 2*lastOp.x - lastOp.x1q
                        y1 = 2*lastOp.y - lastOp.y1q
                    }
                    var x = try number(op.args, i)
                    var y = try number(op.args, i + 1)
                    if op.cmd == "t" {
                        x += lastOp.x
                        y += lastOp.y
                    }
                    // Calculate the coordinates of the cubic control points
                    let x1c = lastOp.x + (2.0/3.0)*(x1 - lastOp.x)
                    let y1c = lastOp.y + (2.0/3.0)*(y1 - lastOp.y)
                    let x2c = x + (2.0/3.0)*(x1 - x)
                    let y2c = y + (2.0/3.0)*(y1 - y)
                    pathOp.setCubicPoints(x1c, y1c, x2c, y2c, x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 2
                }
            } else if op.cmd == "C" || op.cmd == "c" {
                var i: Int = 0
                while i <= (op.args.count - 6) {
                    let pathOp = PathOp("C")
                    var x1 = try number(op.args, i)
                    var y1 = try number(op.args, i + 1)
                    var x2 = try number(op.args, i + 2)
                    var y2 = try number(op.args, i + 3)
                    var x = try number(op.args, i + 4)
                    var y = try number(op.args, i + 5)
                    if op.cmd == "c" {
                        x1 += lastOp.x
                        y1 += lastOp.y
                        x2 += lastOp.x
                        y2 += lastOp.y
                        x += lastOp.x
                        y += lastOp.y
                    }
                    pathOp.setCubicPoints(x1, y1, x2, y2, x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 6
                }
            } else if op.cmd == "S" || op.cmd == "s" {
                var i: Int = 0
                while i <= (op.args.count - 4) {
                    let pathOp = PathOp("C")
                    var x1 = lastOp.x
                    var y1 = lastOp.y
                    if lastOp.cmd == "C" {
                        // Find the reflection control point
                        x1 = 2*lastOp.x - lastOp.x2
                        y1 = 2*lastOp.y - lastOp.y2
                    }
                    var x2 = try number(op.args, i)
                    var y2 = try number(op.args, i + 1)
                    var x = try number(op.args, i + 2)
                    var y = try number(op.args, i + 3)
                    if op.cmd == "s" {
                        x2 += lastOp.x
                        y2 += lastOp.y
                        x += lastOp.x
                        y += lastOp.y
                    }
                    pathOp.setCubicPoints(x1, y1, x2, y2, x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 4
                }
            } else if op.cmd == "A" || op.cmd == "a" {
                var i: Int = 0
                while i <= (op.args.count - 7) {
                    let rx = try number(op.args, i)
                    let ry = try number(op.args, i + 1)
                    let rotation = try number(op.args, i + 2)
                    let largeArc = op.args[i + 3] != "0"
                    let sweep = op.args[i + 4] != "0"
                    var x = try number(op.args, i + 5)
                    var y = try number(op.args, i + 6)
                    if op.cmd == "a" {
                        x += lastOp.x
                        y += lastOp.y
                    }
                    lastOp = addArc(&operations, lastOp, rx, ry, rotation, largeArc, sweep, x, y)
                    i += 7
                }
            } else if op.cmd == "Z" || op.cmd == "z" {
                let pathOp = PathOp("Z")
                pathOp.x = x0
                pathOp.y = y0
                operations.append(pathOp)
                lastOp = pathOp
            }
        }
        return operations
    }

    /// Appends the cubic curves that draw the elliptical arc from the current
    /// point to (x, y), as SVG 1.1 section F.6.5 describes: the arc is split
    /// into pieces of at most a quarter turn, each approximated by one curve.
    /// Returns the last operation appended, or lastOp when the arc is empty.
    private static func addArc(_ operations: inout [PathOp], _ lastOp: PathOp,
            _ rx: Float, _ ry: Float, _ rotation: Float, _ largeArc: Bool, _ sweep: Bool,
            _ x: Float, _ y: Float) -> PathOp {
        let x1 = lastOp.x
        let y1 = lastOp.y
        if x1 == x && y1 == y {
            return lastOp
        }
        var rx = abs(rx)
        var ry = abs(ry)
        if rx == 0.0 || ry == 0.0 {
            let line = PathOp("L", x, y)
            operations.append(line)
            return line
        }
        let phi = Double(rotation) * Double.pi / 180.0
        let cosPhi = cos(phi)
        let sinPhi = sin(phi)
        let dx = Double(x1 - x) / 2.0
        let dy = Double(y1 - y) / 2.0
        let x1p = cosPhi * dx + sinPhi * dy
        let y1p = -sinPhi * dx + cosPhi * dy
        let lambda = (x1p * x1p) / (Double(rx) * Double(rx)) + (y1p * y1p) / (Double(ry) * Double(ry))
        if lambda > 1.0 {
            rx *= Float(lambda.squareRoot())
            ry *= Float(lambda.squareRoot())
        }
        let rx2 = Double(rx) * Double(rx)
        let ry2 = Double(ry) * Double(ry)
        let num = rx2 * ry2 - rx2 * y1p * y1p - ry2 * x1p * x1p
        let den = rx2 * y1p * y1p + ry2 * x1p * x1p
        var coef = max(0.0, num / den).squareRoot()
        if largeArc == sweep {
            coef = -coef
        }
        let cxp = coef * Double(rx) * y1p / Double(ry)
        let cyp = -coef * Double(ry) * x1p / Double(rx)
        let cx = cosPhi * cxp - sinPhi * cyp + Double(x1 + x) / 2.0
        let cy = sinPhi * cxp + cosPhi * cyp + Double(y1 + y) / 2.0
        let ux = (x1p - cxp) / Double(rx)
        let uy = (y1p - cyp) / Double(ry)
        let vx = (-x1p - cxp) / Double(rx)
        let vy = (-y1p - cyp) / Double(ry)
        let theta = atan2(uy, ux)
        var delta = atan2(ux * vy - uy * vx, ux * vx + uy * vy)
        if !sweep && delta > 0.0 {
            delta -= 2.0 * Double.pi
        } else if sweep && delta < 0.0 {
            delta += 2.0 * Double.pi
        }
        let segments = Int((abs(delta) / (Double.pi / 2.0)).rounded(.up))
        if segments == 0 {
            return lastOp
        }
        let step = delta / Double(segments)
        let t = 4.0 / 3.0 * tan(step / 4.0)
        var pathOp = lastOp
        for i in 0..<segments {
            let a1 = theta + Double(i) * step
            let a2 = a1 + step
            let p1x = cos(a1)
            let p1y = sin(a1)
            let p2x = cos(a2)
            let p2y = sin(a2)
            let c1 = onEllipse(cx, cy, Double(rx), Double(ry), cosPhi, sinPhi, p1x - t * p1y, p1y + t * p1x)
            let c2 = onEllipse(cx, cy, Double(rx), Double(ry), cosPhi, sinPhi, p2x + t * p2y, p2y - t * p2x)
            let p2 = (i == segments - 1) ?
                    [x, y] : onEllipse(cx, cy, Double(rx), Double(ry), cosPhi, sinPhi, p2x, p2y)
            pathOp = PathOp("C")
            pathOp.setCubicPoints(c1[0], c1[1], c2[0], c2[1], p2[0], p2[1])
            operations.append(pathOp)
        }
        return pathOp
    }

    // Maps a point of the unit circle to the ellipse with the center
    // (cx, cy), the radii rx and ry and the rotation with the given cosine and sine.
    private static func onEllipse(_ cx: Double, _ cy: Double, _ rx: Double, _ ry: Double,
            _ cosPhi: Double, _ sinPhi: Double, _ u: Double, _ v: Double) -> [Float] {
        return [
                Float(cx + rx * u * cosPhi - ry * v * sinPhi),
                Float(cy + rx * u * sinPhi + ry * v * cosPhi)]
    }
}
