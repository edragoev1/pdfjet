/**
 *  PlainText.swift
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
public class PlainText : Drawable {

    private var font: Font
    private var textLines: [String]
    private var fontSize: Float
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 500.0
    private var leading: Float = 0.0
    private var backgroundColor = Color.white
    private var borderColor = Color.white
    private var textColor = Color.black
    private var language: String?
    private var altDescription: String?
    private var actualText: String?


    public init(_ font: Font, _ textLines: [String]) {
        self.font = font
        self.fontSize = font.getSize()
        self.textLines = textLines
        var buf = String()
        for str in textLines {
            buf.append(str)
            buf.append(" ")
        }
        self.altDescription = buf
        self.actualText = buf
    }


    public func setFontSize(_ fontSize: Float) -> PlainText {
        self.fontSize = fontSize
        return self
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> PlainText {
        self.x = x
        self.y = y
        return self
    }


    public func setWidth(_ w: Float) -> PlainText {
        self.w = w
        return self
    }


    public func setLeading(_ leading: Float) -> PlainText {
        self.leading = leading
        return self
    }


    public func setBackgroundColor(_ backgroundColor: UInt32) -> PlainText {
        self.backgroundColor = backgroundColor
        return self
    }


    public func setBorderColor(_ borderColor: UInt32) -> PlainText {
        self.borderColor = borderColor
        return self
    }


    public func setTextColor(_ textColor: UInt32) -> PlainText {
        self.textColor = textColor
        return self
    }


    ///
    /// Draws this PlainText on the specified page.
    ///
    /// @param page the page to draw this PlainText on.
    /// @return x and y coordinates of the bottom right corner of this component.
    /// @throws Exception
    ///
    public func drawOn(_ page: Page?) -> [Float] {
        let originalSize = font.getSize()
        font.setSize(fontSize)
        var yText: Float = y + font.getAscent()

        page!.addBMC(StructElem.SPAN, language, Single.space, Single.space)
        page!.setBrushColor(backgroundColor)
        self.leading = font.getBodyHeight()
        let h = font.getBodyHeight() * Float(textLines.count)
        page!.fillRect(x, y, w, h)
        page!.setPenColor(borderColor)
        page!.setPenWidth(0.0)
        page!.drawRect(x, y, w, h)
        page!.addEMC()

        page!.addBMC(StructElem.SPAN, language, altDescription!, actualText!)
        page!.setTextStart()
        page!.setTextFont(font)
        page!.setBrushColor(textColor)
        page!.setTextLeading(leading)
        page!.setTextLocation(x, yText)
        for str in textLines {
            if font.skew15 {
                setTextSkew(page!, 0.26, x, yText)
            }
            page!.printString(str)
            page!.newLine()
            yText += leading
        }
        page!.setTextEnd()
        page!.addEMC()

        font.setSize(originalSize)

        return [x + w, y + h]
    }


    private func setTextSkew(
            _ page: Page, _ skew: Float, _ x: Float, _ y: Float) {
        page.append(1.0)
        page.append(" ")
        page.append(0.0)
        page.append(" ")
        page.append(skew)
        page.append(" ")
        page.append(1.0)
        page.append(" ")
        page.append(x)
        page.append(" ")
        page.append(page.height - y)
        page.append(" Tm\n")
    }

}   // End of PlainText.swift
