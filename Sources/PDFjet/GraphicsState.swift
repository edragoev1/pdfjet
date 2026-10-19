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

    // Nil writes no /BM entry, as before there was a blend mode (suggested by
    // Stefan Ostermann, of his fork of PDFjet, 19 October 2026).
    private var blendMode: BlendMode? = nil

    /// Sets the blend mode: how what is drawn is mixed with what is already on
    /// the page. BlendMode.MULTIPLY draws like a highlighter pen. The blend
    /// mode stays until a graphics state sets another one, so set
    /// BlendMode.NORMAL to go back to painting over. A PDF/A-1 document takes
    /// only BlendMode.NORMAL. Nil sets none.
    @discardableResult
    public func setBlendMode(_ blendMode: BlendMode?) -> GraphicsState {
        self.blendMode = blendMode
        return self
    }

    /// Returns the blend mode, or nil if none was set.
    public func getBlendMode() -> BlendMode? {
        return self.blendMode
    }

    // The entries of the state's /ExtGState dictionary, as the page and an
    // imported page write them: the alphas with a dot whatever the locale, at
    // most two decimals and no exponent, then the blend mode if it has one.
    func entries() -> String {
        var state = "/CA " +
                String(decoding: FastFloat.toByteArray(CA), as: UTF8.self) +
                " /ca " +
                String(decoding: FastFloat.toByteArray(ca), as: UTF8.self)
        if let blendMode = blendMode {
            state += " /BM /" + blendMode.rawValue
        }
        return state
    }
}
