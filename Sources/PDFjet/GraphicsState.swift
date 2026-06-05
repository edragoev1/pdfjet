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

    public func setAlphaStroking(_ CA: Float) {
        if CA >= 0.0 && CA <= 1.0 {
            self.CA = CA
        }
    }

    public func getAlphaStroking() -> Float {
        return self.CA
    }

    public func setAlphaNonStroking(_ ca: Float) {
        if ca >= 0.0 && ca <= 1.0 {
            self.ca = ca
        }
    }

    public func getAlphaNonStroking() -> Float {
        return self.ca
    }
}
