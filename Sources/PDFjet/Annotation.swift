/**
 *  Annotation.swift
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
/// Used to create PDF annotation objects.
///
class Annotation {

    var objNumber = 0
    var uri: String?
    var key: String?
    var x1: Float = 0.0
    var y1: Float = 0.0
    var x2: Float = 0.0
    var y2: Float = 0.0

    var language: String?
    var actualText: String?
    var altDescription: String?

    var fileAttachment: FileAttachment?


    ///
    /// This class is used to create annotation objects.
    ///
    /// @param uri the URI string.
    /// @param key the destination name.
    /// @param x1 the x coordinate of the top left corner.
    /// @param y1 the y coordinate of the top left corner.
    /// @param x2 the x coordinate of the bottom right corner.
    /// @param y2 the y coordinate of the bottom right corner.
    ///
    init(
            _ uri: String?,
            _ key: String?,
            _ x1: Float,
            _ y1: Float,
            _ x2: Float,
            _ y2: Float,
            _ language: String?,
            _ actualText: String?,
            _ altDescription: String?) {
        self.uri = uri
        self.key = key
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
        self.language = language
        self.actualText = (actualText == nil) ? uri : actualText
        self.altDescription = (altDescription == nil) ? uri : altDescription
    }

}
