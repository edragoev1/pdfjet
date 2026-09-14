/**
 * Title.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Please see Example_48
///
public class Title : Drawable {
    var prefix: TextLine
    var textLine: TextLine

    private var offset: Float = 0.0

    /// Creates a title at the specified location.
    public init(_ font: Font, _ title: String, _ x: Float, _ y: Float) {
        self.prefix = TextLine(font)
        self.prefix.setLocation(x, y)
        self.textLine = TextLine(font, title)
        self.textLine.setLocation(x, y)
    }

    /// Sets the prefix text.
    public func setPrefix(_ text: String) -> Title {
        prefix.setText(text)
        return self
    }

    /// Sets the distance from the start of the prefix to the start of the title text, to make room for the prefix.
    @discardableResult
    public func setOffset(_ offset: Float) -> Title {
        self.offset = offset
        self.textLine.setLocation(prefix.x + offset, prefix.y)
        return self
    }

    /// Sets the location of the prefix; the title text keeps its offset from it.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.prefix.setLocation(x, y)
        self.textLine.setLocation(x + offset, y)
        return self
    }

    /// Returns the prefix drawn before the title text, such as a section number.
    public func getPrefix() -> TextLine {
        return prefix
    }

    /// Returns the title text.
    public func getTextLine() -> TextLine {
        return textLine
    }

    /// Draws the prefix and the title text on the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if prefix.getText() != "" {
            self.prefix.drawOn(page)
        }
        return self.textLine.drawOn(page)
    }
}
