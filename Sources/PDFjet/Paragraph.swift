/**
 *  Paragraph.swift
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

///
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
///
public class Paragraph {
    public var xText: Float32?
    public var yText: Float32?
    public var x1: Float32?
    public var y1: Float32?
    public var x2: Float32?
    public var y2: Float32?
    var lines: [TextLine]?
    var alignment: UInt32 = Align.LEFT

    ///
    /// Constructor for creating paragraph objects.
    ///
    public init() {
        lines = [TextLine]()
    }

    public init(_ text: TextLine) {
        lines = [TextLine]()
        lines!.append(text)
    }

    ///
    /// Adds a text line to this paragraph.
    ///
    /// @param text the text line to add to this paragraph.
    /// @return this paragraph.
    ///
    @discardableResult
    public func add(_ text: TextLine) -> Paragraph {
        lines!.append(text)
        return self
    }

    ///
    /// Sets the alignment of the text in this paragraph.
    ///
    /// @param alignment the alignment code.
    /// @return this paragraph.
    ///
    /// <pre>Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.</pre>
    ///
    @discardableResult
    public func setAlignment(_ alignment: UInt32) -> Paragraph {
        self.alignment = alignment
        return self
    }

    public func getTextLines() -> [TextLine] {
        return lines!
    }

    public func startsWith(_ token: String) -> Bool {
        return lines![0].getText()!.hasPrefix(token)
    }

    public func setColor(_ color: Int32) {
        for line in lines! {
            line.setColor(color)
        }
    }

    public func setColorMap(_ colorMap: [String: Int32]) {
        for line in lines! {
            line.setColorMap(colorMap)
        }
    }
}   // End of Paragraph.swift
