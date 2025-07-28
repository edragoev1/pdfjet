/**
 *  Title.swift
 *
©2025 PDFjet Software

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
/// Please see Example_51 and Example_52
///
public class Title : Drawable {
    public var prefix: TextLine?
    public var textLine: TextLine?

    public init(_ font: Font, _ title: String, _ x: Float, _ y: Float) {
        self.prefix = TextLine(font)
        self.prefix!.setLocation(x, y)
        self.textLine = TextLine(font, title)
        self.textLine!.setLocation(x, y)
    }

    public func setPrefix(_ text: String) -> Title {
        prefix!.setText(text)
        return self
    }

    public func setOffset(_ offset: Float) -> Title {
        self.textLine!.setLocation(textLine!.x + offset, textLine!.y)
        return self
    }

    public func setPosition(_ x: Float, _ y: Float) {
        self.prefix!.setLocation(x, y)
        self.textLine!.setLocation(x, y)
    }

    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if prefix!.getText() != "" {
            self.prefix!.drawOn(page)
        }
        return self.textLine!.drawOn(page)
    }
}
