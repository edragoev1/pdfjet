/**
 * GraphicsState.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

public class GraphicsState {
    // Default values
    private var CA: Float = 1.0
    private var ca: Float = 1.0

    public init() {

    }

    @discardableResult
    public func setAlphaStroking(_ CA: Float) -> GraphicsState {
        if CA >= 0.0 && CA <= 1.0 {
            self.CA = CA
        }
        return self
    }

    public func getAlphaStroking() -> Float {
        return self.CA
    }

    @discardableResult
    public func setAlphaNonStroking(_ ca: Float) -> GraphicsState {
        if ca >= 0.0 && ca <= 1.0 {
            self.ca = ca
        }
        return self
    }

    public func getAlphaNonStroking() -> Float {
        return self.ca
    }
}
