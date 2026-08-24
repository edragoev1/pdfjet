/**
 * Slice.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

public class Slice {
    public var angle: Float = 0.0
    public var color: Int32 = 0
    public var text: String = ""
    public var tooltip: String = ""

    public init(_ angle: Float, _ color: Int32, _ text: String, _ tooltip: String) {
        self.angle = angle
        self.color = color
        self.text = text
        self.tooltip = tooltip
    }
}
