/**
 * Destination.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// A destination on a page, made by Page.addDestination.
///
public class Destination {
    var name: String?
    var pageObjNumber = 0
    var xPosition: Float = 0.0
    var yPosition: Float = 0.0

    ///
    /// This initializer is used to create destination objects.
    ///
    /// - Parameter name: the name of this destination object.
    /// - Parameter xPosition: the x coordinate of the top left corner.
    /// - Parameter yPosition: the y coordinate of the top left corner.
    ///
    init(_ name: String, _ xPosition: Float, _ yPosition: Float) {
        self.name = name
        self.xPosition = xPosition
        self.yPosition = yPosition
    }

    ///
    /// This initializer is used to create destination objects.
    ///
    /// - Parameter name: the name of this destination object.
    /// - Parameter yPosition: the y coordinate of the top left corner.
    ///
    init(_ name: String, _ yPosition: Float) {
        self.name = name
        self.xPosition = 0.0
        self.yPosition = yPosition
    }

    func setPageObjNumber(_ pageObjNumber: Int) {
        self.pageObjNumber = pageObjNumber
    }
}
