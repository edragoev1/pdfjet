/**
 * Text.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Paragraphs of text lines, wrapped at a width, with an optional border.
/// Please see Example_03, Example_41 and Example_49.
///
public class Text : Drawable {
    private var paragraphs: [Paragraph]
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var width: Float = 0.0
    private var xText: Float = 0.0
    private var yText: Float = 0.0
    private var paragraphLeading: Float = 24.0
    private var borderColor: [Float]?
    private var borderWidth: Float = 0.5
    private var borderPattern: String = "[] 0"

    /// Creates a text object from the paragraphs.
    public init(_ paragraphs: [Paragraph]) {
        self.paragraphs = paragraphs
    }

    /// Sets the location of the top left corner of this text.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x1 = x
        self.y1 = y
        return self
    }

    /// Sets the width at which the lines wrap.
    @discardableResult
    public func setWidth(_ width: Float) -> Text {
        self.width = width
        return self
    }

    /// Sets the vertical distance between paragraphs.
    @discardableResult
    public func setParagraphLeading(_ paragraphLeading: Float) -> Text {
        self.paragraphLeading = paragraphLeading
        return self
    }

    /// Sets the border width.
    @discardableResult
    public func setBorderWidth(_ borderWidth: Float) -> Text {
        self.borderWidth = borderWidth
        return self
    }

    /// Sets the dash pattern of the border, for example "[3] 0".
    @discardableResult
    public func setBorderPattern(_ borderPattern: String) -> Text {
        self.borderPattern = borderPattern
        return self
    }

    /// Sets the border color as a 0xRRGGBB value and draws a border around this text. Color.transparent removes the border.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> Text {
        if color == Color.transparent {
            self.borderColor = nil
            return self
        }
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.setBorderColor([r, g, b])
        return self
    }

    /// Sets the border color from red, green and blue values between 0.0 and 1.0 and draws a border around this text.
    @discardableResult
    public func setBorderColor(_ r: Float, _ g: Float, _ b: Float) -> Text {
        self.borderColor = [r, g, b]
        return self
    }

    /// Sets the border color from an array of red, green and blue values and draws a border around this text.
    @discardableResult
    public func setBorderColor(_ borderColor: [Float]) -> Text {
        self.borderColor = borderColor
        return self
    }

    /// Draws the paragraphs on the specified page. With no page nothing is drawn
    /// and the paragraphs get their coordinates.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        var firstLine = paragraphs[0].lines[0]
        xText = x1
        yText = y1 + firstLine.font!.getAscent(firstLine.fontSize)
        for paragraph in paragraphs {
            firstLine = paragraph.lines[0]
            paragraph.x1 = x1
            paragraph.y1 = yText - firstLine.font!.getAscent(firstLine.fontSize)
            paragraph.xText = xText
            paragraph.yText = yText
            for textLine in paragraph.lines {
                let point = drawTextLine(page, xText, yText, textLine)
                xText = point[0]
                yText = point[1]
                paragraph.x2 = xText
                paragraph.y2 = yText + textLine.font!.getDescent(textLine.fontSize)
            }
            xText = x1
            yText += paragraphLeading
        }

        let lastParagraph = paragraphs[paragraphs.count - 1]
        let lastTextLine = lastParagraph.getTextLines()[lastParagraph.getTextLines().count - 1]
        let height = ((yText - paragraphLeading) - y1) +
                lastTextLine.font!.getDescent(lastTextLine.fontSize)
        if borderColor != nil {
            let rect = Rect(x1, y1, width, height)
            rect.setBorderColor(borderColor)
            rect.setBorderWidth(borderWidth)
            rect.setBorderPattern(borderPattern)
            rect.drawOn(page)
        }

        return [x1 + width, y1 + height]
    }

    // Draws the text line, wrapping it at the width of this text, and returns
    // where the next text starts.
    private func drawTextLine(
            _ page: Page?,
            _ x: Float,
            _ y: Float,
            _ textLine: TextLine) -> [Float] {
        self.xText = x
        self.yText = y

        var tokens: [String]
        if textLine.text!.isCJK() {
            tokens = tokenizeCJK(textLine, self.width)
        } else {
            tokens = textLine.text!.splitOnWhitespace()
        }

        let font = textLine.font!
        let fallbackFont = textLine.fallbackFont
        let fontSize = textLine.fontSize
        var buf = String()
        for token in tokens {
            let runLength = font.stringWidth(fallbackFont, fontSize, buf)
            let tokenWidth = font.stringWidth(fallbackFont, fontSize, token + Single.space)
            if (runLength + tokenWidth) < (self.x1 + self.width) - self.xText {
                buf.append(token)
                buf.append(Single.space)
            } else {
                drawLine(page, textLine, buf)
                xText = x1
                yText += textLine.getHeight()
                buf = token + Single.space
            }
        }
        drawLine(page, textLine, buf)

        return [xText + font.stringWidth(fallbackFont, fontSize, buf), yText]
    }

    // Draws one wrapped line of the text line at the current location, with
    // the text line's font, colors, decorations, vertical offset and link, as
    // TextColumn does.
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

    // Splits CJK text, which has no spaces between its words, into tokens that
    // fit the width, one character at a time. No token is empty.
    private func tokenizeCJK(
            _ textLine: TextLine,
            _ textWidth: Float) -> [String] {
        var list = [String]()
        var buf = String()
        for scalar in textLine.text!.unicodeScalars {
            if textLine.font!.stringWidth(textLine.fallbackFont, textLine.fontSize, buf + String(scalar)) < textWidth {
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

    /// Reads a text file and returns its paragraphs. An empty line separates the paragraphs.
    public static func paragraphsFromFile(_ f1: Font, _ filePath: String) throws -> [Paragraph] {
        var paragraphs = [Paragraph]()
        let contents = try Content.ofTextFile(filePath)
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
        if !sb.isEmpty {
            textLine.setText(sb)
            paragraph.add(textLine)
            paragraphs.append(paragraph)
        }
        return paragraphs
    }
}   // End of Text.swift
