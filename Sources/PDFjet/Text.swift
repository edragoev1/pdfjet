/**
 *  Text.swift
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
    private var drawBorder = true


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
                if i == 0 {
                    textLine!.setAltDescription(buf)
                    textLine!.setActualText(buf)
                }
                else {
                    textLine!.setAltDescription(Single.space)
                    textLine!.setActualText(Single.space)
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
        if page != nil && drawBorder {
            let box = Box()
            box.setLocation(x1, y1)
            box.setSize(self.width, height)
            box.drawOn(page)
        }

        return [self.x1 + self.width, self.y1 + height];
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
        }
        else {
            tokens = textLine.text!.components(separatedBy: .whitespaces)
        }

        var buf = String()
        var firstTextSegment: Bool = true
        for i in 0..<tokens.count {
            let token = (i == 0) ? tokens[i] : (Single.space + tokens[i])
            let lineWidth = textLine.font!.stringWidth(textLine.fallbackFont, buf)
            let tokenWidth = textLine.font!.stringWidth(textLine.fallbackFont, token)
            if (lineWidth + tokenWidth) < (self.x1 + self.width) - self.xText {
                buf.append(token)
            }
            else {
                if page != nil {
                    var altDescription = Single.space
                    var actualText = Single.space
                    if firstTextSegment {
                        altDescription = textLine.getAltDescription()!
                        actualText = textLine.getActualText()!
                    }
                    TextLine(textLine.font!, buf)
                            .setFallbackFont(textLine.fallbackFont)
                            .setLocation(xText, yText + textLine.getVerticalOffset())
                            .setColor(textLine.getColor())
                            .setUnderline(textLine.getUnderline())
                            .setStrikeout(textLine.getStrikeout())
                            .setLanguage(textLine.getLanguage())
                            .setAltDescription(altDescription)
                            .setActualText(actualText)
                            .drawOn(page)
                }
                firstTextSegment = false
                xText = x1
                yText += leading
                buf = ""
                buf.append(tokens[i])
            }
        }
        if page != nil {
            var altDescription = Single.space
            var actualText = Single.space
            if firstTextSegment {
                altDescription = textLine.getAltDescription()!
                actualText = textLine.getActualText()!
            }
            TextLine(textLine.font!, buf)
                    .setFallbackFont(textLine.fallbackFont)
                    .setLocation(xText, yText + textLine.getVerticalOffset())
                    .setColor(textLine.getColor())
                    .setUnderline(textLine.getUnderline())
                    .setStrikeout(textLine.getStrikeout())
                    .setLanguage(textLine.getLanguage())
                    .setAltDescription(altDescription)
                    .setActualText(actualText)
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

}   // End of Text.swift
