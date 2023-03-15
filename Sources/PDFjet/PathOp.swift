/**
 *  PathOp.swift
 *
 Copyright 2023 Innovatics Inc.

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of self software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and self permission notice shall be included in all
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

public class PathOp {
    var cmd: Character
    
    var x1q: Float = 0.0    // Original quadratic control
    var y1q: Float = 0.0    // point coordinates

    var x1: Float = 0.0     // Control point x1
    var y1: Float = 0.0     // Control point y1
    var x2: Float = 0.0     // Control point x2
    var y2: Float = 0.0     // Control point y2
    var x: Float = 0.0      // Initial point x
    var y: Float = 0.0      // Initial point y
    var args: [String]

    let formatter = NumberFormatter()

    init(_ cmd: Character) {
        formatter.maximumFractionDigits = 3
        self.cmd = cmd
        self.args = [String]()
    }

    init(_ cmd: Character, _ x: Float, _ y: Float) {
        formatter.maximumFractionDigits = 3
        self.cmd = cmd
        self.x = x
        self.y = y
        self.args = [String]()
        self.args.append(formatter.string(from: NSNumber(value: x))!)
        self.args.append(formatter.string(from: NSNumber(value: y))!)
    }

    func appendCubicPoints(
            _ x1: Float, _ y1: Float,
            _ x2: Float, _ y2: Float,
            _ x: Float, _ y: Float) {
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
        self.x = x
        self.y = y
        self.args.append(formatter.string(from: NSNumber(value: x1))!)
        self.args.append(formatter.string(from: NSNumber(value: y1))!)
        self.args.append(formatter.string(from: NSNumber(value: x2))!)
        self.args.append(formatter.string(from: NSNumber(value: y2))!)
        self.args.append(formatter.string(from: NSNumber(value: x))!)
        self.args.append(formatter.string(from: NSNumber(value: y))!)
    }
}
