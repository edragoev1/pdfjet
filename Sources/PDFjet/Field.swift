/**
 * Field.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Please see Example_45
 */
public class Field {
    var x: Float
    var values: [String]
    var actualText: [String]
    var altDescription: [String]
    var format: Bool = false

    public init(_ x: Float, _ values: [String], _ format: Bool) {
        self.x = x
        self.values = values
        self.actualText = [String]()
        self.altDescription = [String]()
        self.format = format
        for value in self.values {
            self.actualText.append(value)
            self.altDescription.append(value)
        }
    }

    public convenience init(_ x: Float, _ values: [String]) {
        self.init(x, values, false)
    }

    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Field {
        self.altDescription[0] = altDescription
        return self
    }

    @discardableResult
    public func setActualText(_ actualText: String) -> Field {
        self.actualText[0] = actualText
        return self
    }
}
