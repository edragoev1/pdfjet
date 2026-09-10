/**
 * GraphicsState.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/// Holds the alpha values of stroking and non-stroking operations.
public class GraphicsState {
    // Default values
    private var CA: Float = 1.0
    private var ca: Float = 1.0

    /// Creates a graphics state with both alpha values set to 1.0.
    public init() {

    }

    /// Sets the alpha of stroking operations, from 0.0 to 1.0. Other values are ignored.
    @discardableResult
    public func setAlphaStroking(_ CA: Float) -> GraphicsState {
        if CA >= 0.0 && CA <= 1.0 {
            self.CA = CA
        }
        return self
    }

    /// Returns the alpha of stroking operations.
    public func getAlphaStroking() -> Float {
        return self.CA
    }

    /// Sets the alpha of non-stroking operations, such as fills, from 0.0 to 1.0. Other values are ignored.
    @discardableResult
    public func setAlphaNonStroking(_ ca: Float) -> GraphicsState {
        if ca >= 0.0 && ca <= 1.0 {
            self.ca = ca
        }
        return self
    }

    /// Returns the alpha of non-stroking operations.
    public func getAlphaNonStroking() -> Float {
        return self.ca
    }
}
