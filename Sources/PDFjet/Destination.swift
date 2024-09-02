/**
 *  Destination.swift
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

///
/// Used to create PDF destination objects.
///
public class Destination {
    var name: String?
    var pageObjNumber = 0
    var xPosition: Float = 0.0
    var yPosition: Float = 0.0

    ///
    /// This initializer is used to create destination objects.
    ///
    /// @param name the name of this destination object.
    /// @param xPosition the x coordinate of the top left corner.
    /// @param yPosition the y coordinate of the top left corner.
    ///
    public init(_ name: String, _ xPosition: Float, _ yPosition: Float) {
        self.name = name
        self.xPosition = xPosition
        self.yPosition = yPosition
    }

    ///
    /// This initializer is used to create destination objects.
    ///
    /// @param name the name of this destination object.
    /// @param yPosition the y coordinate of the top left corner.
    ///
    public init(_ name: String, _ yPosition: Float) {
        self.name = name
        self.xPosition = 0.0
        self.yPosition = yPosition
    }

    func setPageObjNumber(_ pageObjNumber: Int) {
        self.pageObjNumber = pageObjNumber
    }
}
