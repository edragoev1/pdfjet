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
    public var xy: [Float32]?
    var list: [TextLine]?
    var alignment: UInt32 = Align.LEFT

    ///
    /// Constructor for creating paragraph objects.
    ///
    public init() {
        list = [TextLine]()
    }

    public init(_ text: TextLine) {
        list = [TextLine]()
        list!.append(text)
    }

    ///
    /// Adds a text line to this paragraph.
    ///
    /// @param text the text line to add to this paragraph.
    /// @return this paragraph.
    ///
    @discardableResult
    public func add(_ text: TextLine) -> Paragraph {
        list!.append(text)
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
        return list!
    }

    public func startsWith(_ token: String) -> Bool {
        return list![0].getText()!.hasPrefix(token)
    }
}   // End of Paragraph.swift
