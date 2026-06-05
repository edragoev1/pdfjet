/**
 * Title.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Please see Example_51 and Example_52
///
public class Title : Drawable {
    public var prefix: TextLine?
    public var textLine: TextLine?

    public init(_ font: Font, _ title: String, _ x: Float, _ y: Float) {
        self.prefix = TextLine(font)
        self.prefix!.setLocation(x, y)
        self.textLine = TextLine(font, title)
        self.textLine!.setLocation(x, y)
    }

    public func setPrefix(_ text: String) -> Title {
        prefix!.setText(text)
        return self
    }

    public func setOffset(_ offset: Float) -> Title {
        self.textLine!.setLocation(textLine!.x + offset, textLine!.y)
        return self
    }

    public func setPosition(_ x: Float, _ y: Float) {
        self.prefix!.setLocation(x, y)
        self.textLine!.setLocation(x, y)
    }

    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if prefix!.getText() != "" {
            self.prefix!.drawOn(page)
        }
        return self.textLine!.drawOn(page)
    }
}
