/**
 * SVGPath.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A path or a shape of an SVG file, as PDF path operations in the space of
/// the svg element, and what it is drawn with.
class SVGPath {
    var operations = [PathOp]()             // The PDF path operations
    var fill: Int32 = Color.transparent     // The fill color, or Color.transparent for none
    var stroke: Int32 = Color.transparent   // The stroke color, or Color.transparent for none
    var strokeWidth: Float = 0.0            // The stroke width, in the space of the svg element
    var evenOdd = false                     // Filled with the even-odd rule
    var lineCap = CapStyle.BUTT
    var lineJoin = JoinStyle.MITER
    var fillAlpha: Float = 1.0
    var strokeAlpha: Float = 1.0
}
