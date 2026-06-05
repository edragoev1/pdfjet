/**
 * Destination.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to create PDF destination objects.
///
public class Destination {
    var name: String?
    var pageObjNumber = 0
    var xPosition: Float = 0.0
    var yPosition: Float = 0.0

    ///
    /// This initializer is used to create destination objects.
    ///
    /// @param name the name of this destination object.
    /// @param xPosition the x coordinate of the top left corner.
    /// @param yPosition the y coordinate of the top left corner.
    ///
    public init(_ name: String, _ xPosition: Float, _ yPosition: Float) {
        self.name = name
        self.xPosition = xPosition
        self.yPosition = yPosition
    }

    ///
    /// This initializer is used to create destination objects.
    ///
    /// @param name the name of this destination object.
    /// @param yPosition the y coordinate of the top left corner.
    ///
    public init(_ name: String, _ yPosition: Float) {
        self.name = name
        self.xPosition = 0.0
        self.yPosition = yPosition
    }

    func setPageObjNumber(_ pageObjNumber: Int) {
        self.pageObjNumber = pageObjNumber
    }
}
