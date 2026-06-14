/**
 * Text.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
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
    private var border = false

    public init(_ paragraphs: [Paragraph]) {
        self.paragraphs = paragraphs
        self.font = paragraphs[0].lines![0].getFont()
        self.fallbackFont = paragraphs[0].lines![0].getFallbackFont()
        self.leading = font!.getBodyHeight()
        self.paragraphLeading = 2*leading
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

    @discardableResult
    public func setBorder(_ border: Bool) -> Text {
        self.border = border
        return self
    }

    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        self.xText = x1
        self.yText = y1 + self.paragraphs![0].lines![0].font!.getAscent()
        for paragraph in self.paragraphs! {
            paragraph.x1 = x1
            paragraph.y1 = yText - paragraph.lines![0].font!.getAscent()
            paragraph.xText = self.xText
            paragraph.yText = self.yText
            for textLine in paragraph.lines! {
                let point = drawTextLine(page, xText, yText, textLine)
                xText = point[0]
                yText = point[1]
                paragraph.x2 = self.xText
                paragraph.y2 = self.yText + textLine.font!.getDescent(textLine.font!.size)
            }
            self.xText = x1
            self.yText += self.paragraphLeading
        }

        let height = ((self.yText - paragraphLeading) - self.y1) + font!.descent
        if page != nil && border {
            let rect = Rect(x1, y1, self.width, height)
            rect.drawOn(page)
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
                            .setFallbackFont(textLine.getFallbackFont())
                            .setFontSize(textLine.getFontSize())
                            .setLocation(xText, yText + textLine.getVerticalOffset())
                            .setTextColor(textLine.getTextColor())
                            .setColorMap(textLine.getColorMap())
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
                    .setFallbackFont(textLine.getFallbackFont())
                    .setFontSize(textLine.getFontSize())
                    .setLocation(xText, yText + textLine.getVerticalOffset())
                    .setTextColor(textLine.getTextColor())
                    .setColorMap(textLine.getColorMap())
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
            } else {
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
        if (sb != "") {
            textLine.setText(sb)
            paragraph.add(textLine)
            paragraphs.append(paragraph)
        }
        return paragraphs
    }

    public static func readLines(_ filePath: String) throws -> [String] {
        var lines = [String]()
        let contents = try String(contentsOf: URL(fileURLWithPath: filePath), encoding: .utf8)
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
