/**
 *  TextFrame.swift
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
/// Please see Example_47
///
public class TextFrame : Drawable {

    private var paragraphs: Array<TextLine>?
    private var font: Font?
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 0.0
    private var h: Float = 0.0
    private var leading: Float = 0.0
    private var paragraphLeading: Float = 0.0
    private var beginParagraphPoints: [[Float]]?
    private var drawBorder = false


    public init(_ paragraphs: Array<TextLine>) {
        self.paragraphs = Array(paragraphs)
        self.font = paragraphs[0].getFont()
        self.leading = font!.getBodyHeight()
        self.paragraphLeading = 2*leading
        self.beginParagraphPoints = [[Float]]()
        let fallbackFont = paragraphs[0].getFallbackFont()
        if fallbackFont != nil && (fallbackFont!.getBodyHeight() > self.leading) {
            self.leading = fallbackFont!.getBodyHeight()
        }
        self.paragraphs!.reverse()
    }


    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> TextFrame {
        self.x = x
        self.y = y
        return self
    }


    @discardableResult
    public func setWidth(_ w: Float) -> TextFrame {
        self.w = w
        return self
    }


    @discardableResult
    public func setHeight(_ h: Float) -> TextFrame {
        self.h = h
        return self
    }


    @discardableResult
    public func setLeading(_ leading: Float) -> TextFrame {
        self.leading = leading
        return self
    }


    @discardableResult
    public func setParagraphLeading(_ paragraphLeading: Float) -> TextFrame {
        self.paragraphLeading = paragraphLeading
        return self
    }


    public func setParagraphs(_ paragraphs: Array<TextLine>) {
        self.paragraphs = paragraphs
    }


    public func getParagraphs() -> Array<TextLine>? {
        return self.paragraphs
    }


    @discardableResult
    public func getBeginParagraphPoints() -> [[Float]]? {
        return self.beginParagraphPoints
    }


    public func setDrawBorder(_ drawBorder: Bool) {
        self.drawBorder = drawBorder
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        var xText = self.x
        var yText = self.y + self.font!.ascent

        while paragraphs!.count > 0 {
            // The paragraphs are reversed so we can efficiently remove the first one:
            var textLine = paragraphs!.removeLast()
            textLine.setLocation(xText, yText)
            beginParagraphPoints!.append([xText, yText])

            while true {
                textLine = drawLineOnPage(textLine, page)
                if textLine.getText() == "" {
                    break
                }
                yText = textLine.advance(leading)
                if yText + font!.descent >= (self.y + self.h) {
                    // The paragraphs are reversed so we can efficiently add new first paragraph:
                    paragraphs!.append(textLine)

                    if page != nil && drawBorder {
                        let box = Box()
                        box.setLocation(x, y)
                        box.setSize(w, h)
                        box.drawOn(page)
                    }

                    return [x + w, y + h]
                }
            }
            xText = x
            yText += paragraphLeading
        }

        if page != nil && drawBorder {
            let box = Box()
            box.setLocation(x, y)
            box.setSize(w, h)
            box.drawOn(page)
        }

        return [x + w, y + h]
    }


    private func drawLineOnPage(_ textLine: TextLine, _ page: Page?) -> TextLine {
        var sb1 = String()
        var sb2 = String()
        let tokens = textLine.text!.components(separatedBy: .whitespaces)
        var testForFit = true
        var i = 0
        while i < tokens.count {
            let token = tokens[i] + Single.space
            if testForFit && textLine.getStringWidth((sb1 + token).trim()) < self.w {
                sb1.append(token)
            }
            else {
                if testForFit {
                    testForFit = false
                }
                sb2.append(token)
            }
            i += 1
        }
        textLine.setText(sb1.trim())
        if page != nil {
            textLine.drawOn(page!)
        }

        textLine.setText(sb2.trim())
        return textLine
    }


    public func isNotEmpty() -> Bool {
        return paragraphs!.count > 0
    }

}   // End of TextFrame.swift
