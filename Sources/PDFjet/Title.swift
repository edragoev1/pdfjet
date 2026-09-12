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
    /// The prefix drawn before the title text, such as a section number.
    public var prefix: TextLine?
    /// The title text.
    public var textLine: TextLine?

    /// Creates a title at the specified location.
    public init(_ font: Font, _ title: String, _ x: Float, _ y: Float) {
        self.prefix = TextLine(font)
        self.prefix!.setLocation(x, y)
        self.textLine = TextLine(font, title)
        self.textLine!.setLocation(x, y)
    }

    /// Sets the prefix text.
    public func setPrefix(_ text: String) -> Title {
        prefix!.setText(text)
        return self
    }

    /// Moves the title text right by the offset, to make room for the prefix.
    public func setOffset(_ offset: Float) -> Title {
        self.textLine!.setLocation(textLine!.x + offset, textLine!.y)
        return self
    }

    /// Sets the location of this title.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.prefix!.setLocation(x, y)
        self.textLine!.setLocation(x, y)
        return self
    }

    /// Draws the prefix and the title text on the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if prefix!.getText() != "" {
            self.prefix!.drawOn(page)
        }
        return self.textLine!.drawOn(page)
    }
}
