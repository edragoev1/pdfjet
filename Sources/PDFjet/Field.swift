/**
 * Field.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Please see Example_42
 */
public class Field {
    var x: Float
    var label: String
    var value: String

    public init(_ x: Float, _ label: String, _ value: String) {
        self.x = x
        self.label = label
        self.value = value
    }
}
