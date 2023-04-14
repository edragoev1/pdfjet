/**
 *  Text.swift
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
/// Please see Example_45
///
public class Text : Drawable {
    private var paragraphs: [Paragraph]?
    private var font: Font?
    private var fallbackFont: Font?
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var width: Float = 0.0
    private var xText: Float = 0.0
    private var yText: Float = 0.0
    private var leading: Float = 0.0
    private var paragraphLeading: Float = 0.0
    private var beginParagraphPoints: [[Float]]?
    private var spaceBetweenTextLines: Float = 0.0
    private var border = false

    public init(_ paragraphs: [Paragraph]) {
        self.paragraphs = paragraphs
        self.font = paragraphs[0].list![0].getFont()
        self.fallbackFont = paragraphs[0].list![0].getFallbackFont()
        self.leading = font!.getBodyHeight()
        self.paragraphLeading = 2*leading
        self.beginParagraphPoints = [[Float]]()
        self.spaceBetweenTextLines = font!.stringWidth(fallbackFont, Single.space)
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Text {
        self.x1 = x
        self.y1 = y
        return self
    }

    @discardableResult
    public func setWidth(_ width: Float) -> Text {
        self.width = width
        return self
    }

    @discardableResult
    public func setLeading(_ leading: Float) -> Text {
        self.leading = leading
        return self
    }

    @discardableResult
    public func setParagraphLeading(
            _ paragraphLeading: Float) -> Text {
        self.paragraphLeading = paragraphLeading
        return self
    }

    public func getBeginParagraphPoints() -> [[Float]] {
        return self.beginParagraphPoints!
    }

    @discardableResult
    public func setSpaceBetweenTextLines(
            _ spaceBetweenTextLines: Float) -> Text {
        self.spaceBetweenTextLines = spaceBetweenTextLines
        return self
    }

    @discardableResult
    public func setBorder(_ border: Bool) -> Text {
        self.border = border
        return self
    }

    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        self.xText = x1
        self.yText = y1 + font!.ascent
        var textLine: TextLine?
        for paragraph in paragraphs! {
            let numberOfTextLines = paragraph.list!.count
            var buf = String()
            for i in 0..<numberOfTextLines {
                textLine = paragraph.list![i]
                buf.append(textLine!.text!)
            }
            for i in 0..<numberOfTextLines {
                textLine = paragraph.list![i]
                if i == 0 {
                    beginParagraphPoints!.append([xText, yText])
                }
                let xy = drawTextLine(page, self.xText, self.yText, textLine!)
                self.xText = xy[0]
                if textLine!.getTrailingSpace() {
                    self.xText += spaceBetweenTextLines
                }
                self.yText = xy[1]
            }
            self.xText = x1
            self.yText += paragraphLeading
        }

        let height = ((self.yText - paragraphLeading) - self.y1) + font!.descent
        if page != nil && border {
            let box = Box()
            box.setLocation(x1, y1)
            box.setSize(self.width, height)
            box.drawOn(page)
        }

        return [self.x1 + self.width, self.y1 + height]
    }

    private func drawTextLine(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ textLine: TextLine) -> [Float] {
        self.xText = x
        self.yText = y

        var tokens: [String]
        if stringIsCJK(textLine.text!) {
            tokens = tokenizeCJK(textLine, self.width)
        } else {
            tokens = textLine.text!.components(separatedBy: .whitespaces)
        }

        var buf = String()
        for i in 0..<tokens.count {
            let token = (i == 0) ? tokens[i] : (Single.space + tokens[i])
            let lineWidth = textLine.font!.stringWidth(textLine.fallbackFont, buf)
            let tokenWidth = textLine.font!.stringWidth(textLine.fallbackFont, token)
            if (lineWidth + tokenWidth) < (self.x1 + self.width) - self.xText {
                buf.append(token)
            } else {
                if page != nil {
                    TextLine(textLine.font!, buf)
                            .setFallbackFont(textLine.fallbackFont)
                            .setLocation(xText, yText + textLine.getVerticalOffset())
                            .setColor(textLine.getColor())
                            .setUnderline(textLine.getUnderline())
                            .setStrikeout(textLine.getStrikeout())
                            .setLanguage(textLine.getLanguage())
                            .drawOn(page)
                }
                xText = x1
                yText += leading
                buf = ""
                buf.append(tokens[i])
            }
        }
        if page != nil {
            TextLine(textLine.font!, buf)
                    .setFallbackFont(textLine.fallbackFont)
                    .setLocation(xText, yText + textLine.getVerticalOffset())
                    .setColor(textLine.getColor())
                    .setUnderline(textLine.getUnderline())
                    .setStrikeout(textLine.getStrikeout())
                    .setLanguage(textLine.getLanguage())
                    .drawOn(page)
        }

        return [xText + textLine.font!.stringWidth(textLine.fallbackFont, buf), yText]
    }

    private func stringIsCJK(_ str: String) -> Bool {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        var numOfCJK = 0
        let scalars = [UnicodeScalar](str.unicodeScalars)
        for scalar in scalars {
            if (scalar.value >= 0x4E00 && scalar.value <= 0x9FD5) ||
                    (scalar.value >= 0x3040 && scalar.value <= 0x309F) ||
                    (scalar.value >= 0x30A0 && scalar.value <= 0x30FF) ||
                    (scalar.value >= 0x1100 && scalar.value <= 0x11FF) {
                numOfCJK += 1
            }
        }
        return (numOfCJK > (scalars.count / 2))
    }

    private func tokenizeCJK(
            _ textLine: TextLine,
            _ textWidth: Float) -> [String] {
        var list = [String]()
        var buf = String()
        let scalars = Array(textLine.text!.unicodeScalars)
        for scalar in scalars {
            if textLine.font!.stringWidth(textLine.fallbackFont, buf + String(scalar)) < textWidth {
                buf.append(String(scalar))
            }
            else {
                list.append(buf)
                buf = ""
                buf.append(String(scalar))
            }
        }
        if buf != "" {
            list.append(buf)
        }
        return list
    }

    public static func paragraphsFromFile(_ f1: Font, _ filePath: String) throws -> [Paragraph] {
        var paragraphs = [Paragraph]()
        let contents = try Contents.ofTextFile(filePath)
        var paragraph = Paragraph()
        var textLine = TextLine(f1)
        var sb = String()
        let scalars = Array(contents.unicodeScalars)
        var i = 0
        while i < scalars.count {
            let ch = scalars[i]
            // We need at least one character after the \n\n to begin new paragraph!
            if i < (scalars.count - 2) &&
                    ch == "\n" && scalars[i + 1] == "\n" {
                textLine.setText(sb)
                paragraph.add(textLine)
                paragraphs.append(paragraph)
                paragraph = Paragraph()
                textLine = TextLine(f1)
                sb = ""
                i += 1
            } else {
                sb.append(String(ch))
            }
            i += 1
        }
        if (sb != "") {
            textLine.setText(sb)
            paragraph.add(textLine)
            paragraphs.append(paragraph)
        }
        return paragraphs
    }

    public static func readLines(_ filePath: String) throws -> [String] {
        var lines = [String]()
        let contents = try String(contentsOf: URL(fileURLWithPath: filePath))
        var buffer = String()
        for scalar in contents.unicodeScalars {
            if scalar == "\r" {
                continue
            } else if scalar == "\n" {
                lines.append(buffer)
                buffer = ""
            } else {
                buffer.append(String(scalar))
            }
        }
        if buffer.count > 0 {
            lines.append(buffer)
        }
        return lines
    }
}   // End of Text.swift
