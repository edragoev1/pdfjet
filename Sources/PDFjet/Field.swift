/**
 *  Field.swift
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


/**
 *  Please see Example_45
 */
public class Field {

    var x: Float
    var values: [String]
    var actualText: [String]
    var altDescription: [String]
    var format: Bool = false


    public init(_ x: Float, _ values: [String], _ format: Bool) {
        self.x = x
        self.values = values
        self.actualText = [String]()
        self.altDescription = [String]()
        self.format = format
        for value in self.values {
            self.actualText.append(value)
            self.altDescription.append(value)
        }
    }


    public convenience init(_ x: Float, _ values: [String]) {
        self.init(x, values, false)
    }


    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Field {
        self.altDescription[0] = altDescription
        return self
    }


    @discardableResult
    public func setActualText(_ actualText: String) -> Field {
        self.actualText[0] = actualText
        return self
    }

}
