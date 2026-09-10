/**
 * TextFrame.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import Foundation

/**
 * TextFrame Please see Example_47
 */
public class TextFrame : Drawable {
    private var f1: Font
    private var x: Float?
    private var y: Float?
    private var w: Float?
    private var h: Float?
    private var leading: Float
    private var border: Bool = false
    private var borderColor: Int32 = Color.blue
    private var paragraphs: [[String]]

    /// Creates a text frame from a list of paragraphs.
    public init(_ f1: Font, _ inputList: [String]) {
        self.f1 = f1
        self.leading = f1.getAscent() + f1.getDescent()
        var list = inputList
        list = list.reversed()

        self.paragraphs = [[String]]()
        for text in list {
            let split = text.trimmingCharacters(
                    in: .whitespaces).components(separatedBy: .whitespacesAndNewlines)
            var tokens = [String]()
            for token in split {
                if !token.isEmpty { // Filter empty tokens
                    tokens.append(token)
                }
            }
            tokens = tokens.reversed()
            paragraphs.append(tokens)
        }
    }

    /// Sets the location of the top left corner of this text frame.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the width of this text frame.
    @discardableResult
    public func setWidth(_ w: Float) -> TextFrame {
        self.w = w
        return self
    }

    /// Sets the height of this text frame.
    @discardableResult
    public func setHeight(_ h: Float) -> TextFrame {
        self.h = h
        return self
    }

    /// Returns the width of this text frame.
    public func getWidth() -> Float {
        return self.w!
    }

    /// Returns the height of this text frame.
    public func getHeight() -> Float {
        return self.h!
    }

    /// Sets whether a border is drawn around this text frame.
    @discardableResult
    public func setBorder(_ border: Bool) -> TextFrame {
        self.border = border
        return self
    }

    /// Sets the border color as a 0xRRGGBB value.
    @discardableResult
    public func setBorderColor(_ borderColor: Int32) -> TextFrame {
        self.borderColor = borderColor
        return self
    }

    /// Returns true if some of the text has not been drawn yet.
    public func hasMoreText() -> Bool {
        return paragraphs.count > 0
    }

    private func drawBorder(_ page: Page) {
        if border {
            let rect = Rect(self.x!, self.y!, self.w!, self.h!)
            rect.setBorderColor(borderColor)
            rect.drawOn(page)
        }
    }

    ///
    /// Draws as much of the text as fits in this frame on the page.
    /// Call hasMoreText to check whether text is left for another page.
    ///
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [0.0, 0.0]
        }

        var yText = self.y! + f1.getAscent()
        while paragraphs.count > 0 {
            var tokens = paragraphs.removeLast()
            var textLine: TextLine?
            var sb = ""
            var token: String?

            while tokens.count > 0 {
                if yText + f1.getDescent() < (self.y! + self.h!) {
                    token = tokens.removeLast()
                    if f1.stringWidth(sb + (token ?? "")) < self.w! {
                        sb.append(token ?? "")
                        sb.append(" ") // Single.space equivalent
                    } else {
                        textLine = TextLine(f1, sb.trimmingCharacters(in: .whitespaces))
                        textLine!.setLocation(self.x!, yText)
                        textLine!.drawOn(page)
                        sb = ""
                        tokens.append(token!)
                        yText += leading
                    }
                } else {
                    paragraphs.append(tokens)
                    drawBorder(page)
                    return [self.x! + self.w!, self.y! + self.h!]
                }
            }

            if !sb.trimmingCharacters(in: .whitespaces).isEmpty {
                textLine = TextLine(f1, sb.trimmingCharacters(in: .whitespaces))
                textLine!.setLocation(self.x!, yText)
                textLine!.drawOn(page)
                yText += leading
            }
            yText += leading
        }

        drawBorder(page)
        return [self.x! + self.w!, self.y! + self.h!]
    }
}
