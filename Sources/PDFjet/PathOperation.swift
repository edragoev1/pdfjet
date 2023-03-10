/**
 *  PathOperation.swift
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

public class PathOperation {
    var command: String
    var arguments: [String]

    init(_ command: String) {
        self.command = command
        self.arguments = [String]()
    }

    func getPathOperations() -> [PathOperation] {
        var operations = [PathOperation]()
        let n = getNumberOfArguments()
        var operation = PathOperation(command)
        for argument in arguments {
            operation.arguments.append(argument)
            if operation.arguments.count % n == 0 {
                operations.append(operation)
                operation = PathOperation(command)
            }
        }
        if operation.arguments.count == n {
            operations.append(operation)
        }
        return operations
    }

    func getNumberOfArguments() -> Int {
        if command == "M" || command == "m" {       // moveto
            return 2
        }
        else if command == "L" || command == "l" {  // lineto
            return 2
        }
        else if command == "H" || command == "h" {  // horizontal lineto
            return 1
        }
        else if command == "V" || command == "v" {  // vertical lineto
            return 1
        }
        else if command == "C" || command == "c" {  // curveto
            return 6
        }
        else if command == "S" || command == "s" {  // smooth curveto
            return 4
        }
        else if command == "Q" || command == "q" {  // quadratic curve
            return 4
        }
        else if command == "T" || command == "t" {  // smooth quadratic curveto
            return 2
        }
        else if command == "A" || command == "a" {  // elliptical arc
            return 7
        }
        else if command == "Z" || command == "z" {  // closepath
            return 0
        }
        return 0
    }

}
