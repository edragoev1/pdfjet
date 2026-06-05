/**
 * PathOp.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

public class PathOp {
    var cmd: Character
    var x1q: Float = 0.0    // Original quadratic control
    var y1q: Float = 0.0    // point coordinates

    var x1: Float = 0.0     // Control point x1
    var y1: Float = 0.0     // Control point y1
    var x2: Float = 0.0     // Control point x2
    var y2: Float = 0.0     // Control point y2
    var x: Float = 0.0      // Initial point x
    var y: Float = 0.0      // Initial point y
    var args: [String]

    init(_ cmd: Character) {
        self.cmd = cmd
        self.args = [String]()
    }

    init(_ cmd: Character, _ x: Float, _ y: Float) {
        self.cmd = cmd
        self.x = x
        self.y = y
        self.args = [String]()
    }

    func setCubicPoints(
            _ x1: Float, _ y1: Float,
            _ x2: Float, _ y2: Float,
            _ x: Float, _ y: Float) {
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
        self.x = x
        self.y = y
    }
}
