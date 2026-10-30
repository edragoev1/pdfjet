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
    var evenOdd = false                     // Filled, or clipped to, with the even-odd rule
    var lineCap = CapStyle.BUTT
    var lineJoin = JoinStyle.MITER
    var miterLimit: Float = 4.0
    var fillAlpha: Float = 1.0
    var strokeAlpha: Float = 1.0
    var dashArray: [Float]? = nil           // The dashes and the gaps, in the space of the svg element; nil is solid
    var dashOffset: Float = 0.0
    var strokeFirst = false                 // paint-order: the stroke drawn before the fill
    var clips: [SVGPath]? = nil             // The clip paths it is drawn inside, all of them
    var choices: [Choice]? = nil            // The children of the switches it is in
    var text: SVGText? = nil                // A text, drawn instead of the operations
    var parts: [SVGPath]? = nil             // Of a clip path that clips with texts: one for each, with its text
    var glyphs = 0                          // Of such a clip path: what is written for its texts, as operations

    // Returns true when it is in the children chosen of the switches it is in.
    func isChosen() -> Bool {
        return SVGPath.areChosen(choices)
    }

    // Returns true when each of the choices is the child chosen.
    static func areChosen(_ choices: [Choice]?) -> Bool {
        return choices?.allSatisfy { $0.isChosen() } ?? true
    }
}
