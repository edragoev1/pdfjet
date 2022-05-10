/**
 *  State.swift
 *
Copyright 2020 Innovatics Inc.

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


class State {

    private var pen: [Float]
    private var brush: [Float]
    private var penWidth: Float
    private var lineCapStyle: CapStyle
    private var lineJoinStyle: JoinStyle
    private var linePattern: String


    public init(
            _ pen: [Float],
            _ brush: [Float],
            _ penWidth: Float,
            _ lineCapStyle: CapStyle,
            _ lineJoinStyle: JoinStyle,
            _ linePattern: String) {
        self.pen = [pen[0], pen[1], pen[2]]
        self.brush = [brush[0], brush[1], brush[2]]
        self.penWidth = penWidth
        self.lineCapStyle = lineCapStyle
        self.lineJoinStyle = lineJoinStyle
        self.linePattern = linePattern
    }


    public func getPen() -> [Float] {
        return self.pen
    }


    public func getBrush() -> [Float] {
        return self.brush
    }


    public func getPenWidth() -> Float {
        return self.penWidth
    }


    public func getLineCapStyle() -> CapStyle {
        return self.lineCapStyle
    }


    public func getLineJoinStyle() -> JoinStyle {
        return self.lineJoinStyle
    }


    public func getLinePattern() -> String {
        return self.linePattern
    }

}   // End of State.swift
