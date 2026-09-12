/**
 * TextFrame.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Paragraphs of text lines, wrapped at the width of the frame, with an optional
/// border. A frame with a height draws as much of the text as fits and keeps the
/// rest for the next frame, so text flows from frame to frame. A frame without a
/// height draws all of the text, as Text does. Please see Example_47.
///
public class TextFrame : Drawable {
    private var paragraphs: [Paragraph]
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 0.0
    private var h: Float = 0.0
    private var paragraphLeading: Float = 24.0
    private var border = false
    private var borderColor: [Float] = [0.0, 0.0, 1.0]
    private var borderWidth: Float = 0.5
    private var borderPattern = "[] 0"

    // The text that is not drawn yet starts at this paragraph, at this text line
    // of the paragraph and at this token of the text line. The tokens are nil
    // when the text line has not been started.
    private var paragraphIndex = 0
    private var lineIndex = 0
    private var tokens: [String]?
    private var tokenIndex = 0

    // The row of text being drawn: where the text goes next, whether the row can
    // take more text, whether the frame has a row yet, and where the next row goes.
    private var xText: Float = 0.0
    private var yText: Float = 0.0
    private var rowOpen = false
    private var rowPlaced = false
    private var nextBaseline: Float = 0.0

    /// Creates a text frame from paragraphs of text lines. The paragraphs are 24
    /// points apart unless setParagraphLeading says otherwise.
    public init(_ paragraphs: [Paragraph]) {
        self.paragraphs = paragraphs
    }

    /// Creates a text frame from strings, one paragraph each, in the font at its
    /// size. An empty line separates the paragraphs.
    public init(_ f1: Font, _ inputList: [String]) {
        self.paragraphs = inputList.map { Paragraph(TextLine(f1, $0)) }
        self.paragraphLeading = 2.0 * f1.getBodyHeight()
    }

    /// Sets the location of the top left corner of this text frame.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the width at which the lines wrap.
    @discardableResult
    public func setWidth(_ w: Float) -> TextFrame {
        self.w = w
        return self
    }

    /// Sets the height of this text frame. With a height of 0, the default, the
    /// frame draws all of its text.
    @discardableResult
    public func setHeight(_ h: Float) -> TextFrame {
        self.h = h
        return self
    }

    /// Returns the width of this text frame.
    public func getWidth() -> Float {
        return self.w
    }

    /// Returns the height of this text frame.
    public func getHeight() -> Float {
        return self.h
    }

    /// Sets the vertical distance between paragraphs, from the baseline of the
    /// last line of a paragraph to the baseline of the first line of the next.
    @discardableResult
    public func setParagraphLeading(_ paragraphLeading: Float) -> TextFrame {
        self.paragraphLeading = paragraphLeading
        return self
    }

    /// Sets whether a border is drawn around this text frame.
    @discardableResult
    public func setBorder(_ border: Bool) -> TextFrame {
        self.border = border
        return self
    }

    /// Sets the border color as a 0xRRGGBB value and draws a border around this text frame. Color.transparent removes the border.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> TextFrame {
        if color == Color.transparent {
            self.border = false
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        return setBorderColor(r, g, b)
    }

    /// Sets the border color from red, green and blue values between 0.0 and 1.0 and draws a border around this text frame.
    @discardableResult
    public func setBorderColor(_ r: Float, _ g: Float, _ b: Float) -> TextFrame {
        self.borderColor = [r, g, b]
        self.border = true
        return self
    }

    /// Sets the border color from an array of red, green and blue values and draws a border around this text frame.
    @discardableResult
    public func setBorderColor(_ borderColor: [Float]) -> TextFrame {
        self.borderColor = borderColor
        self.border = true
        return self
    }

    /// Sets the border width.
    @discardableResult
    public func setBorderWidth(_ borderWidth: Float) -> TextFrame {
        self.borderWidth = borderWidth
        return self
    }

    /// Sets the dash pattern of the border, for example "[3] 0".
    @discardableResult
    public func setBorderPattern(_ borderPattern: String) -> TextFrame {
        self.borderPattern = borderPattern
        return self
    }

    /// Returns true if some of the text has not been drawn yet.
    public func hasMoreText() -> Bool {
        return paragraphIndex < paragraphs.count
    }

    ///
    /// Draws the text on the page: all of it when this frame has no height, or as
    /// much as fits in the height, keeping the rest for the next frame. The first
    /// line of a frame is drawn even when it does not fit, so the text always
    /// flows, and a word wider than the frame is broken. With no page nothing is
    /// drawn, the paragraphs get their coordinates and the text is kept.
    ///
    /// - Parameter page: the page to draw on.
    /// - Returns: the x and y coordinates of the bottom right corner of this frame,
    ///   or of the text when the frame has no height.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let startParagraph = paragraphIndex
        let startLine = lineIndex
        let startTokens = tokens
        let startToken = tokenIndex

        var bottom = drawParagraphs(page)
        if h > 0.0 {
            bottom = y + h
        }
        if border {
            let rect = Rect(x, y, w, bottom - y)
            rect.setBorderColor(borderColor)
            rect.setBorderWidth(borderWidth)
            rect.setBorderPattern(borderPattern)
            rect.drawOn(page)
        }

        if page == nil {
            paragraphIndex = startParagraph
            lineIndex = startLine
            tokens = startTokens
            tokenIndex = startToken
        }
        return [x + w, bottom]
    }

    // Draws the text that is left, as much of it as fits in the height of the
    // frame, and returns the bottom of the text drawn.
    private func drawParagraphs(_ page: Page?) -> Float {
        xText = x
        rowOpen = false
        rowPlaced = false
        var bottom = y
        while paragraphIndex < paragraphs.count {
            let paragraph = paragraphs[paragraphIndex]
            while lineIndex < paragraph.lines.count {
                let textLine = paragraph.lines[lineIndex]
                if !rowOpen && !openRow(textLine) {
                    return bottom
                }
                if tokens == nil {
                    if lineIndex == 0 {
                        paragraph.x1 = x
                        paragraph.y1 = yText - textLine.font!.getAscent(textLine.fontSize)
                        paragraph.xText = xText
                        paragraph.yText = yText
                    }
                    tokens = tokenize(textLine)
                    tokenIndex = 0
                }
                if !drawTokens(page, textLine) {
                    return bottom
                }
                paragraph.x2 = xText
                paragraph.y2 = yText + textLine.font!.getDescent(textLine.fontSize)
                bottom = paragraph.y2
                tokens = nil
                tokenIndex = 0
                lineIndex += 1
            }
            xText = x
            rowOpen = false
            nextBaseline = yText + paragraphLeading
            paragraphIndex += 1
            lineIndex = 0
        }
        return bottom
    }

    // Starts a row of text for the text line, below the previous row or at the
    // top of the frame. Returns false when the row does not fit in the height of
    // the frame. The first row of a frame always fits, so the text keeps flowing.
    private func openRow(_ textLine: TextLine) -> Bool {
        let baseline = rowPlaced ? nextBaseline : y + textLine.font!.getAscent(textLine.fontSize)
        if h > 0.0 && rowPlaced &&
                (baseline + textLine.font!.getDescent(textLine.fontSize)) > (y + h) {
            return false
        }
        xText = x
        yText = baseline
        rowOpen = true
        rowPlaced = true
        return true
    }

    // Draws the tokens of the text line that are left, wrapping them at the width
    // of the frame. Returns false when a row does not fit in the height of the
    // frame; the tokens that are left stay for the next frame.
    private func drawTokens(_ page: Page?, _ textLine: TextLine) -> Bool {
        let font = textLine.font!
        let fallbackFont = textLine.fallbackFont
        let fontSize = textLine.fontSize
        var buf = String()
        while tokenIndex < tokens!.count {
            if !rowOpen && !openRow(textLine) {
                return false
            }
            let token = tokens![tokenIndex]
            let runLength = font.stringWidth(fallbackFont, fontSize, buf)
            let tokenWidth = font.stringWidth(fallbackFont, fontSize, token + Single.space)
            if (runLength + tokenWidth) < ((x + w) - xText) {
                buf.append(token)
                buf.append(Single.space)
                tokenIndex += 1
                continue
            }
            if buf.isEmpty && xText == x {
                // The token does not fit in an empty row, so the row takes as much of it as fits.
                let head = headThatFits(textLine, token)
                buf.append(head)
                let headCount = head.unicodeScalars.count
                if headCount == token.unicodeScalars.count {
                    tokenIndex += 1
                } else {
                    tokens![tokenIndex] = String(String.UnicodeScalarView(token.unicodeScalars.dropFirst(headCount)))
                }
            }
            drawLine(page, textLine, buf)
            buf = ""
            xText = x
            rowOpen = false
            nextBaseline = yText + textLine.getHeight()
        }
        drawLine(page, textLine, buf)
        xText += font.stringWidth(fallbackFont, fontSize, buf)
        return true
    }

    // Returns the longest start of the token that is narrower than the frame, and
    // at least the first character of the token.
    private func headThatFits(_ textLine: TextLine, _ token: String) -> String {
        let scalars = Array(token.unicodeScalars)
        var end = 1
        while end < scalars.count {
            let next = String(String.UnicodeScalarView(scalars[0...end]))
            if textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, next) >= w {
                break
            }
            end += 1
        }
        return String(String.UnicodeScalarView(scalars[0..<end]))
    }

    // Draws the string at the current text position, with the text line's font,
    // colors, decorations, vertical offset and link, as Text does.
    private func drawLine(_ page: Page?, _ textLine: TextLine, _ str: String) {
        TextLine(textLine.font!, str)
                .setFallbackFont(textLine.getFallbackFont())
                .setFontSize(textLine.getFontSize())
                .setTextColor(textLine.getTextColor())
                .setColorMap(textLine.getColorMap())
                .setUnderline(textLine.getUnderline())
                .setStrikeout(textLine.getStrikeout())
                .setLanguage(textLine.getLanguage())
                .setVerticalOffset(textLine.getVerticalOffset())
                .setURIAction(textLine.getURIAction())
                .setGoToAction(textLine.getGoToAction())
                .setLocation(xText, yText)
                .drawOn(page)
    }

    // Splits the text of the text line into words, or, for CJK text, which has no
    // spaces between its words, into runs of characters that fit in the width.
    private func tokenize(_ textLine: TextLine) -> [String] {
        if !textLine.text!.isCJK() {
            return textLine.text!.splitOnWhitespace()
        }
        var list = [String]()
        var buf = String()
        for scalar in textLine.text!.unicodeScalars {
            if textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, buf + String(scalar)) < w {
                buf.unicodeScalars.append(scalar)
            } else {
                if !buf.isEmpty {
                    list.append(buf)
                }
                buf = String(scalar)
            }
        }
        if !buf.isEmpty {
            list.append(buf)
        }
        return list
    }
}   // End of TextFrame.swift
