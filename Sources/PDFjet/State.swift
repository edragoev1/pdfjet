/**
 * State.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class State {
    private var pen: [Float]
    private var brush: [Float]
    private var penWidth: Float
    private var lineCapStyle: CapStyle
    private var lineJoinStyle: JoinStyle
    private var linePattern: String

    public init(
            _ pen: [Float],
            _ brush: [Float],
            _ penWidth: Float,
            _ lineCapStyle: CapStyle,
            _ lineJoinStyle: JoinStyle,
            _ linePattern: String) {
        self.pen = [pen[0], pen[1], pen[2]]
        self.brush = [brush[0], brush[1], brush[2]]
        self.penWidth = penWidth
        self.lineCapStyle = lineCapStyle
        self.lineJoinStyle = lineJoinStyle
        self.linePattern = linePattern
    }

    public func getPen() -> [Float] {
        return self.pen
    }

    public func getBrush() -> [Float] {
        return self.brush
    }

    public func getPenWidth() -> Float {
        return self.penWidth
    }

    public func getLineCapStyle() -> CapStyle {
        return self.lineCapStyle
    }

    public func getLineJoinStyle() -> JoinStyle {
        return self.lineJoinStyle
    }

    public func getLinePattern() -> String {
        return self.linePattern
    }
}   // End of State.swift
