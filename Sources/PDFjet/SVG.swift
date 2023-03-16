/**
 *  SVG.swift
 *
 Copyright 2023 Innovatics Inc.

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
    public func getSVGPaths(_ fileName: String) throws -> [String] {
        var paths = [String]()
        var inPath = false
        var buffer = try String(contentsOfFile: fileName, encoding: .utf8)
        for ch in buffer {
            if !inPath && buffer.hasSuffix("<path d=") {
                inPath = true
                buffer = ""
            } else if inPath && ch == "\"" {
                inPath = false
                paths.append(buffer)
                buffer = ""
            } else {
                buffer.append(ch)
            }
        }
        return paths
    }

    public func isCommand(_ ch: Character) -> Bool {
        // Please note:
        // Capital letter commands use absolute coordinates
        // Small letter commands use relative coordinates
        if ch == "M" || ch == "m" {           // moveto
            return true
        } else if ch == "L" || ch == "l" {    // lineto
            return true
        } else if ch == "H" || ch == "h" {    // horizontal lineto
            return true
        } else if ch == "V" || ch == "v" {    // vertical lineto
            return true
        } else if ch == "Q" || ch == "q" {    // quadratic curveto
            return true
        } else if ch == "T" || ch == "t" {    // smooth quadratic curveto
            return true
        } else if ch == "C" || ch == "c" {    // cubic curveto
            return true
        } else if ch == "S" || ch == "s" {    // smooth cubic curveto
            return true
        } else if ch == "A" || ch == "a" {    // elliptical arc
            return true
        } else if ch == "Z" || ch == "z" {    // close path
            return true
        }
        return false
    }

    public func getSVGPathOps(_ paths: [String]) -> [PathOp] {
        var operations = [PathOp]()
        var op: PathOp?
        for path in paths {
            // Path example:
            // "M22.65 34h3v-8.3H34v-3h-8.35V14h-3v8.7H14v3h8.65ZM24 44z"
            // System.out.println(path);
            // System.out.println();
            var buf = String()
            var token = false
            for ch in path {
                if (isCommand(ch)) {                    // open path
                    if (token) {
                        op!.args.append(buf)
                        buf = ""
                    }
                    token = false
                    op = PathOp(ch)
                    operations.append(op!);
                } else if (ch == " " || ch == ",") {
                    if (token) {
                        op!.args.append(buf)
                        buf = ""
                    }
                    token = false;
                } else if (ch == "-") {
                    if (token) {
                        op!.args.append(buf)
                        buf = ""
                    }
                    token = true;
                    buf.append(ch);
                } else if (ch == ".") {
                    if (buf.contains(".")) {
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
        }
        return operations
    }
}
