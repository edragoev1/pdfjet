//
//  TextParameters.swift
//
//  Copyright (c) 2026 PDFjet Software
//  Licensed under the MIT License. See LICENSE file in the project root.
//

import Foundation

/// The font, font size, location and text for Stamp.drawText.
public class TextParameters {
    var font: Font?
    var fontSize: Float = 12.0
    var x: Float = 0.0
    var y: Float = 0.0
    var text: String?

    /// Creates text parameters with a font size of 12, located at (0, 0).
    public init() {
        self.fontSize = 12.0
        self.x = 0.0
        self.y = 0.0
    }

    /// Sets the font.
    @discardableResult
    public func setFont(_ font: Font?) -> TextParameters {
        self.font = font
        return self
    }

    /// Sets the font size.
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> TextParameters {
        self.fontSize = fontSize
        return self
    }

    /// Sets the location of the text.
    @discardableResult
    public func setTextLocation(_ x: Float, _ y: Float) -> TextParameters {
        self.x = x
        self.y = y
        return self
    }

    /// Sets the text.
    @discardableResult
    public func setText(_ text: String?) -> TextParameters {
        self.text = text
        return self
    }
}
