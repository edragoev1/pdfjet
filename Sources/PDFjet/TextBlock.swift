/**
 *  TextBlock.swift
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
/// Class for creating blocks of text.
///
public class TextBlock : Drawable {
    internal var font: Font
    internal var fallbackFont: Font?
    internal var text: String?

    private var spaceBetweenLines: Float = 0.0
    private var textAlign = Align.LEFT

    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 300.0
    private var h: Float = 200.0

    private var background = Color.white
    private var brush = Color.black
    private var drawBorder = false

    private var uri: String?
    private var key: String?
    private var uriLanguage: String?
    private var uriActualText: String?
    private var uriAltDescription: String?

    ///
    /// Creates a text block.
    ///
    /// @param font the text font.
    ///
    public init(_ font: Font) {
        self.font = font
    }

    public init(_ font: Font, _ text: String) {
        self.font = font
        self.text = text
    }

    ///
    /// Sets the fallback font.
    ///
    /// @param fallbackFont the fallback font.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setFallbackFont(_ fallbackFont: Font?) -> TextBlock {
        self.fallbackFont = fallbackFont
        return self
    }

    ///
    /// Sets the block text.
    ///
    /// @param text the block text.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setText(_ text: String) -> TextBlock {
        self.text = text
        return self
    }

    ///
    /// Sets the location where this text block will be drawn on the page.
    ///
    /// @param x the x coordinate of the top left corner of the text block.
    /// @param y the y coordinate of the top left corner of the text block.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> TextBlock {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the width of this text block.
    ///
    /// @param width the specified width.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setWidth(_ width: Float) -> TextBlock {
        self.w = width
        return self
    }

    ///
    /// Returns the text block width.
    ///
    /// @return the text block width.
    ///
    public func getWidth() -> Float {
        return self.w
    }

    ///
    /// Sets the height of this text block.
    ///
    /// @param height the specified height.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setHeight(float height: Float) -> TextBlock {
        self.h = height
        return self
    }

    ///
    /// Returns the text block height.
    ///
    /// @return the text block height.
    ///
    public func getHeight() -> Float {
        return drawOn(nil)[1]
    }

    ///
    /// Sets the space between two lines of text.
    ///
    /// @param spaceBetweenLines the space between two lines.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setSpaceBetweenLines(_ spaceBetweenLines: Float) -> TextBlock {
        self.spaceBetweenLines = spaceBetweenLines
        return self
    }

    ///
    /// Returns the space between two lines of text.
    ///
    /// @return float the space.
    ///
    public func getSpaceBetweenLines() -> Float {
        return self.spaceBetweenLines
    }

    ///
    /// Sets the text alignment.
    ///
    /// @param textAlign the alignment parameter.
    /// Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
    ///
    @discardableResult
    public func setTextAlignment(_ textAlign: UInt32) -> TextBlock {
        self.textAlign = textAlign
        return self
    }

    ///
    /// Returns the text alignment.
    ///
    /// @return the alignment code.
    ///
    public func getTextAlignment() -> UInt32 {
        return self.textAlign
    }

    ///
    /// Sets the background to the specified color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setBgColor(int color: Int32) -> TextBlock {
        self.background = color
        return self
    }

    ///
    /// Returns the background color.
    ///
    /// @return int the color as 0xRRGGBB integer.
    ///
    public func getBgColor() -> Int32 {
        return self.background
    }

    ///
    /// Sets the brush color.
    ///
    /// @param color the color specified as 0xRRGGBB integer.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func setBrushColor(_ color: Int32) -> TextBlock {
        self.brush = color
        return self
    }

    ///
    /// Returns the brush color.
    ///
    /// @return int the brush color specified as 0xRRGGBB integer.
    ///
    public func getBrushColor() -> Int32 {
        return self.brush
    }

    @discardableResult
    public func setDrawBorder(_ drawBorder: Bool) -> TextBlock {
        self.drawBorder = drawBorder
        return self
    }

    // Is the text Chinese, Japanese or Korean?
    private func isCJK(_ text: String) -> Bool {
        var cjk: Int = 0
        var other: Int = 0
        for scalar in text.unicodeScalars {
            if scalar >= "\u{4E00}" && scalar <= "\u{9FFF}" ||          // Unified CJK
                    scalar >= "\u{AC00}" && scalar <= "\u{D7AF}" ||     // Hangul (Korean)
                    scalar >= "\u{30A0}" && scalar <= "\u{30FF}" ||     // Katakana (Japanese)
                    scalar >= "\u{3040}" && scalar <= "\u{309F}" {      // Hiragana (Japanese)
                cjk += 1
            } else {
                other += 1
            }
        }
        return cjk > other
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    ///
    /// Draws this text block on the specified page.
    ///
    /// @param page the page to draw this text block on.
    /// @return the TextBlock object.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if page != nil {
            if getBgColor() != Color.white {
                page!.setBrushColor(self.background)
                page!.fillRect(x, y, w, h)
            }
            page!.setBrushColor(self.brush)
        }
        return drawText(page)
    }

    private func drawText(_ page: Page?) -> [Float] {
        var list = [String]()
        var buf = String()
        var lines = text!.components(separatedBy: "\r?\n")
        for line in lines {
            if isCJK(line) {
                buf = ""
                for ch in line {
                    if font.stringWidth(fallbackFont, buf + String(ch)) <= self.w {
                        buf.append(ch)
                    } else {
                        list.append(buf)
                        buf = ""
                        buf.append(ch)
                    }
                }
                if !buf.trim().isEmpty {
                    list.append(buf.trim())
                }
            } else {
                if font.stringWidth(fallbackFont, line) < self.w {
                    list.append(line)
                } else {
                    buf = ""
                    let tokens = TextUtils.splitTextIntoTokens(line, font, fallbackFont, self.w)
                    for token in tokens {
                        if font.stringWidth(fallbackFont, (buf + " " + token).trim()) < self.w {
                            buf.append(" " + token)
                        } else {
                            list.append(buf.trim())
                            buf = ""
                            buf.append(token)
                        }
                    }
                    let str = buf.trim()
                    if str != "" {
                        list.append(str)
                    }
                }
            }
        }
        lines = list

        var xText: Float = 0.0
        var yText: Float = y + font.getAscent()
        for i in 0..<lines.count {
            if textAlign == Align.LEFT {
                xText = x
            } else if textAlign == Align.RIGHT {
                xText = (x + self.w) - (font.stringWidth(fallbackFont, lines[i]))
            } else if textAlign == Align.CENTER {
                xText = x + (self.w - font.stringWidth(fallbackFont, lines[i]))/2
            } else {
                Swift.print("Invalid text alignment option.")
            }
            if page != nil {
                page!.drawString(font, fallbackFont, lines[i], xText, yText)
            }
            if i < (lines.count - 1) {
                yText += font.bodyHeight + spaceBetweenLines
            }
        }
        self.h = (yText - y) + font.descent
        if page != nil && drawBorder {
            let box = Box()
            box.setLocation(x, y)
            box.setSize(w, h)
            box.drawOn(page)
        }

        if page != nil && (uri != nil || key != nil) {
            page!.addAnnotation(Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    uriLanguage,
                    uriActualText,
                    uriAltDescription))
        }

        return [self.x + self.w, self.y + self.h];
    }

    /// Sets the URI for the "click text line" action.
    ///
    /// @param uri the URI
    /// @return this TextBlock.
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> TextBlock {
        self.uri = uri
        return self
    }
}   // End of TextBlock.swift
