/**
 *  State.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
import Foundation


class State {

    private var pen: [Float]
    private var brush: [Float]
    private var penWidth: Float
    private var lineCapStyle: Int
    private var lineJoinStyle: Int
    private var linePattern: String


    public init(
            _ pen: [Float],
            _ brush: [Float],
            _ penWidth: Float,
            _ lineCapStyle: Int,
            _ lineJoinStyle: Int,
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


    public func getLineCapStyle() -> Int {
        return self.lineCapStyle
    }


    public func getLineJoinStyle() -> Int {
        return self.lineJoinStyle
    }


    public func getLinePattern() -> String {
        return self.linePattern
    }

}   // End of State.swift
