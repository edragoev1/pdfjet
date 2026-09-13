/**
 * SVGPath.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// One SVG path with its PDF path operations, colors and stroke width.
class SVGPath {
    var data: String?                       // The SVG path data
    var operations: [PathOp]?               // The PDF path operations
    var fill: Int32 = Color.transparent     // The fill color or nil (don't fill)
    var stroke: Int32 = Color.transparent   // The stroke color or nil (don't stroke)
    var fillNone = false                    // fill="none": not filled, whatever the svg element says
    var strokeNone = false                  // stroke="none": not stroked, whatever the svg element says
    var strokeWidth: Float = 0.0            // The stroke width
}
