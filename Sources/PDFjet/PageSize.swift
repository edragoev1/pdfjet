/**
 * PageSize.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// The width and height of a page in points; 1 point is 1/72 inch.
/// A page size cannot be changed once it is created, so the PORTRAIT and LANDSCAPE
/// constants of A3, A4, A5, B5, Executive, Legal, Letter and Tabloid are the same
/// for every page made with them. Use the initializer for any other page size.
///
public struct PageSize: Sendable {
    private let width: Float
    private let height: Float

    ///
    /// Creates a page size.
    ///
    /// - Parameter width: the width of the page in points.
    /// - Parameter height: the height of the page in points.
    ///
    public init(_ width: Float, _ height: Float) {
        self.width = width
        self.height = height
    }

    /// Returns the width of the page in points.
    public func getWidth() -> Float {
        return width
    }

    /// Returns the height of the page in points.
    public func getHeight() -> Float {
        return height
    }
}
