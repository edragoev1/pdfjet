/**
 * SVGPath.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

public class SVGPath {
    var data: String?                       // The SVG path data
    var operations: [PathOp]?               // The PDF path operations
    var fill: Int32 = Color.transparent     // The fill color or nil (don't fill)
    var stroke: Int32 = Color.transparent   // The stroke color or nil (don't stroke)
    var strokeWidth: Float = 0.0            // The stroke width
}
