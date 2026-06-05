/**
 * Slice.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

public class Slice {
    var angle: Float
    var color: Int32

    public init(_ percent: Float32, _ color: Int32) {
        self.angle = percent*3.6
        self.color = color
    }
}
