/**
 * Dimension.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Encapsulates the width and height of a component.
///
public class Dimension {
    var w: Float?
    var h: Float?

    ///
    /// Constructor for creating dimension objects.
    ///
    /// @param width the width.
    /// @param height the height.
    ///
    public init(_ width: Float, _ height: Float) {
        self.w = width
        self.h = height
    }

    public func getWidth() -> Float? {
        return self.w
    }

    public func getHeight() -> Float? {
        return self.h
    }
}   // End of Dimension.swift
