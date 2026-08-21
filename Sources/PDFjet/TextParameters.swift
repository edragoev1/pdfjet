//
//  TextParameters.swift
//
//  Copyright (c) 2026 PDFjet Software
//  Licensed under the MIT License. See LICENSE file in the project root.
//

import Foundation

public class TextParameters {
    var font: Font?
    var fontSize: Float = 12.0
    var x: Float = 0.0
    var y: Float = 0.0
    var text: String?

    public init() {
        self.fontSize = 12.0
        self.x = 0.0
        self.y = 0.0
    }

    @discardableResult
    public func setFont(_ font: Font?) -> TextParameters {
        self.font = font
        return self
    }

    @discardableResult
    public func setFontSize(_ fontSize: Float) -> TextParameters {
        self.fontSize = fontSize
        return self
    }

    @discardableResult
    public func setTextLocation(x: Float, y: Float) -> TextParameters {
        self.x = x
        self.y = y
        return self
    }

    @discardableResult
    public func setText(_ text: String?) -> TextParameters {
        self.text = text
        return self
    }
}
