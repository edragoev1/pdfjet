/**
 * Annotation.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to create PDF annotation objects.
///
class Annotation {
    var objNumber = 0
    var uri: String?
    var key: String?
    var x1: Float = 0.0
    var y1: Float = 0.0
    var x2: Float = 0.0
    var y2: Float = 0.0

    var language: String?
    var actualText: String?
    var altDescription: String?

    var fileAttachment: FileAttachment?

    ///
    /// This class is used to create annotation objects.
    ///
    /// @param uri the URI string.
    /// @param key the destination name.
    /// @param x1 the x coordinate of the top left corner.
    /// @param y1 the y coordinate of the top left corner.
    /// @param x2 the x coordinate of the bottom right corner.
    /// @param y2 the y coordinate of the bottom right corner.
    ///
    init(
            _ uri: String?,
            _ key: String?,
            _ x1: Float,
            _ y1: Float,
            _ x2: Float,
            _ y2: Float,
            _ language: String?,
            _ actualText: String?,
            _ altDescription: String?) {
        self.uri = uri
        self.key = key
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
        self.language = language
        self.actualText = (actualText == nil) ? uri : actualText
        self.altDescription = (altDescription == nil) ? uri : altDescription
    }
}
