/**
 * Slice.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A slice of a DonutChart: a value, a color and a label. The slice's share
/// of the sum of the values of the chart sets its angle.
public class Slice {
    let value: Float
    let color: Int32
    let text: String

    /// Creates a slice.
    ///
    /// - Parameter value: the value of the slice, above 0; the chart draws its share of the sum.
    /// - Parameter color: the color as a 0xRRGGBB value, for example Color.blue.
    /// - Parameter text: the label drawn next to the slice.
    public init(_ value: Float, _ color: Int32, _ text: String) {
        self.value = value
        self.color = color
        self.text = text
    }

    // The color of a slice that takes the next color of the palette
    static let PALETTE: Int32 = -1

    /// Creates a slice in the next color of the chart's palette, navy first, the
    /// palette of BarChart, so that the charts of a document match.
    ///
    /// - Parameter value: the value of the slice, above 0; the chart draws its share of the sum.
    /// - Parameter text: the label drawn next to the slice.
    public convenience init(_ value: Float, _ text: String) {
        self.init(value, Slice.PALETTE, text)
    }
}
