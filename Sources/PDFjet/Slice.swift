/**
 * Slice.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A single slice of a donut or pie chart.
public class Slice {
    /// The angle of the slice in degrees.
    public var angle: Float = 0.0
    /// The color of the slice as a 0xRRGGBB value.
    public var color: Int32 = 0
    /// The label drawn next to the slice.
    public var text: String = ""
    /// The tooltip of the slice.
    public var tooltip: String = ""

    /// Creates a slice with the angle in degrees, the 0xRRGGBB color, the label and the tooltip.
    public init(_ angle: Float, _ color: Int32, _ text: String, _ tooltip: String) {
        self.angle = angle
        self.color = color
        self.text = text
        self.tooltip = tooltip
    }
}
