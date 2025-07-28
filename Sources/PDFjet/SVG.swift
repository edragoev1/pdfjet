/**
 *  SVG.swift
 *
 ©2025 PDFjet Software

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
 */
import Foundation

public class SVG {

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

    public static func getOperations(_ path: String) -> [PathOp] {
        var operations = [PathOp]()
        var op: PathOp?
        var buf = String()
        var token = false
        for ch in path {
            if isCommand(ch) {          // open path
                if token {
                    op!.args.append(buf)
                    buf = ""
                }
                token = false
                op = PathOp(ch)
                operations.append(op!)
            } else if ch == " " || ch == "," {
                if token {
                    op!.args.append(buf)
                    buf = ""
                }
                token = false
            } else if ch == "-" {
                if token {
                    op!.args.append(buf)
                    buf = ""
                }
                token = true
                buf.append(ch)
            } else if ch == "." {
                if buf.contains(".") {
                    op!.args.append(buf)
                    buf = ""
                }
                token = true
                buf.append(ch)
            } else {
                token = true
                buf.append(ch)
            }
        }
        return operations
    }

    public static func toPDF(_ list: [PathOp]) -> [PathOp] {
        var operations = [PathOp]()
        var lastOp: PathOp?
        var x0: Float = 0.0 // Start of subpath
        var y0: Float = 0.0
        for op in list {
            if op.cmd == "M" || op.cmd == "m" {
                var i: Int = 0
                while i <= (op.args.count - 2) {
                    let pathOp: PathOp
                    var x = Float(op.args[i])!
                    var y = Float(op.args[i + 1])!
                    if op.cmd == "m" && lastOp != nil {
                        x += lastOp!.x
                        y += lastOp!.y
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
                    var x = Float(op.args[i])!
                    var y = Float(op.args[i + 1])!
                    if op.cmd == "l" && lastOp != nil {
                        x += lastOp!.x
                        y += lastOp!.y
                    }
                    let pathOp = PathOp("L", x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 2
                }
            } else if op.cmd == "H" || op.cmd == "h" {
                var i: Int = 0
                while i < op.args.count {
                    var x = Float(op.args[i])!
                    if op.cmd == "h" && lastOp != nil {
                        x += lastOp!.x
                    }
                    let pathOp = PathOp("L", x, lastOp!.y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 1
                }
            } else if op.cmd == "V" || op.cmd == "v" {
                var i: Int = 0
                while i < op.args.count {
                    var y = Float(op.args[i])!
                    if op.cmd == "v" && lastOp != nil {
                        y += lastOp!.y
                    }
                    let pathOp = PathOp("L", lastOp!.x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 1
                }
            } else if op.cmd == "Q" || op.cmd == "q" {
                var i: Int = 0
                while i <= (op.args.count - 4) {
                    let pathOp = PathOp("C")
                    var x1 = Float(op.args[i])!
                    var y1 = Float(op.args[i + 1])!
                    var x = Float(op.args[i + 2])!
                    var y = Float(op.args[i + 3])!
                    if op.cmd == "q" {
                        x1 += lastOp!.x
                        y1 += lastOp!.y
                        x += lastOp!.x
                        y += lastOp!.y
                    }
                    // Save the original control point
                    pathOp.x1q = x1
                    pathOp.y1q = y1
                    // Calculate the coordinates of the cubic control points
                    let x1c = lastOp!.x + (2.0/3.0)*(x1 - lastOp!.x)
                    let y1c = lastOp!.y + (2.0/3.0)*(y1 - lastOp!.y)
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
                    var x1 = lastOp!.x
                    var y1 = lastOp!.y
                    if lastOp!.cmd == "C" {
                        // Find the reflection control point
                        x1 = 2*lastOp!.x - lastOp!.x1q
                        y1 = 2*lastOp!.y - lastOp!.y1q
                    }
                    var x = Float(op.args[i])!
                    var y = Float(op.args[i + 1])!
                    if op.cmd == "t" {
                        x += lastOp!.x
                        y += lastOp!.y
                    }
                    // Calculate the coordinates of the cubic control points
                    let x1c = lastOp!.x + (2.0/3.0)*(x1 - lastOp!.x)
                    let y1c = lastOp!.y + (2.0/3.0)*(y1 - lastOp!.y)
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
                    var x1 = Float(op.args[i])!
                    var y1 = Float(op.args[i + 1])!
                    var x2 = Float(op.args[i + 2])!
                    var y2 = Float(op.args[i + 3])!
                    var x = Float(op.args[i + 4])!
                    var y = Float(op.args[i + 5])!
                    if op.cmd == "c" {
                        x1 += lastOp!.x
                        y1 += lastOp!.y
                        x2 += lastOp!.x
                        y2 += lastOp!.y
                        x += lastOp!.x
                        y += lastOp!.y
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
                    var x1 = lastOp!.x
                    var y1 = lastOp!.y
                    if lastOp!.cmd == "C" {
                        // Find the reflection control point
                        x1 = 2*lastOp!.x - lastOp!.x2
                        y1 = 2*lastOp!.y - lastOp!.y2
                    }
                    var x2 = Float(op.args[i])!
                    var y2 = Float(op.args[i + 1])!
                    var x = Float(op.args[i + 2])!
                    var y = Float(op.args[i + 3])!
                    if op.cmd == "s" {
                        x2 += lastOp!.x
                        y2 += lastOp!.y
                        x += lastOp!.x
                        y += lastOp!.y
                    }
                    pathOp.setCubicPoints(x1, y1, x2, y2, x, y)
                    operations.append(pathOp)
                    lastOp = pathOp
                    i += 4
                }
            } else if op.cmd == "A" || op.cmd == "a" {
                // Elliptical Arc
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
}
