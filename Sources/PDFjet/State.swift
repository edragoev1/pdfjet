/**
 * State.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class State {
    private var pen: [Float]
    private var penWritten: Bool
    private var brush: [Float]
    private var brushWritten: Bool
    private var penWidth: Float
    private var penWidthWritten: Bool
    private var writtenFont: Font?
    private var writtenFontSize: Float
    private var lineCapStyle: CapStyle
    private var lineJoinStyle: JoinStyle
    private var linePattern: String
    // The height of the page, which transform divides by the vertical scale
    // and which every y coordinate is measured from.
    private var height: Float

    /// Creates a snapshot of the pen and brush colors, pen width, line cap and join styles and dash pattern.
    public init(
            _ brush: [Float],
            _ brushWritten: Bool,
            _ pen: [Float],
            _ penWritten: Bool,
            _ penWidth: Float,
            _ penWidthWritten: Bool,
            _ writtenFont: Font?,
            _ writtenFontSize: Float,
            _ lineCapStyle: CapStyle,
            _ lineJoinStyle: JoinStyle,
            _ linePattern: String,
            _ height: Float) {
        self.pen = [pen[0], pen[1], pen[2]]
        self.brush = [brush[0], brush[1], brush[2]]
        self.penWritten = penWritten
        self.brushWritten = brushWritten
        self.penWidth = penWidth
        self.penWidthWritten = penWidthWritten
        self.writtenFont = writtenFont
        self.writtenFontSize = writtenFontSize
        self.lineCapStyle = lineCapStyle
        self.lineJoinStyle = lineJoinStyle
        self.linePattern = linePattern
        self.height = height
    }

    /// Returns the height of the page.
    public func getHeight() -> Float {
        return self.height
    }

    /// Returns the pen color.
    public func getPen() -> [Float] {
        return self.pen
    }

    /// Returns the brush color.
    public func getBrush() -> [Float] {
        return self.brush
    }

    /// Returns the pen width.
    public func getPenWritten() -> Bool {
        return self.penWritten
    }

    public func getBrushWritten() -> Bool {
        return self.brushWritten
    }

    public func getPenWidthWritten() -> Bool {
        return self.penWidthWritten
    }

    public func getWrittenFont() -> Font? {
        return self.writtenFont
    }

    public func getWrittenFontSize() -> Float {
        return self.writtenFontSize
    }

    public func getPenWidth() -> Float {
        return self.penWidth
    }

    /// Returns the line cap style.
    public func getLineCapStyle() -> CapStyle {
        return self.lineCapStyle
    }

    /// Returns the line join style.
    public func getLineJoinStyle() -> JoinStyle {
        return self.lineJoinStyle
    }

    /// Returns the line dash pattern.
    public func getLinePattern() -> String {
        return self.linePattern
    }
}   // End of State.swift
