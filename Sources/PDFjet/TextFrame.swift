/**
 * TextFrame.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A frame that draws as much of its paragraphs as fits, so text flows from
/// frame to frame. Please see Example_47.
///
public class TextFrame : Drawable {
    private var f1: Font
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var w: Float = 0.0
    private var h: Float = 0.0
    private var leading: Float
    private var border: Bool = false
    private var borderColor: Int32 = Color.blue
    private var paragraphs = [[String]]()

    /// Creates a text frame from a list of paragraphs.
    public init(_ f1: Font, _ inputList: [String]) {
        self.f1 = f1
        self.leading = f1.getAscent() + f1.getDescent()
        for text in inputList.reversed() {
            paragraphs.append(text.splitOnWhitespace().reversed())
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
        return self.w
    }

    /// Returns the height of this text frame.
    public func getHeight() -> Float {
        return self.h
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
            let rect = Rect(x, y, w, h)
            rect.setBorderColor(borderColor)
            rect.drawOn(page)
        }
    }

    ///
    /// Draws as much of the text as fits in this frame on the page and returns
    /// the x and y coordinates of the bottom right corner of the frame.
    /// Call hasMoreText to check whether text is left for another frame.
    /// The page must not be nil.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            fatalError("Page cannot be nil")
        }

        var yText = y + f1.getAscent()
        while paragraphs.count > 0 {
            var tokens = paragraphs.removeLast()
            var sb = ""

            while tokens.count > 0 {
                if yText + f1.getDescent() < (y + h) {
                    let token = tokens.removeLast()
                    if f1.stringWidth(sb + token) < w {
                        sb.append(token)
                        sb.append(Single.space)
                    } else {
                        TextLine(f1, sb.trim()).setLocation(x, yText).drawOn(page)
                        sb = ""
                        tokens.append(token)
                        yText += leading
                    }
                } else {
                    paragraphs.append(tokens)
                    drawBorder(page)
                    return [x + w, y + h]
                }
            }

            if !sb.trim().isEmpty {
                TextLine(f1, sb.trim()).setLocation(x, yText).drawOn(page)
                yText += leading
            }
            yText += leading
        }

        drawBorder(page)
        return [x + w, y + h]
    }
}   // End of TextFrame.swift
