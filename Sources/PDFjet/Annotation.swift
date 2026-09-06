/**
 * Annotation.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to create PDF annotation objects.
///
// Annotation.swift
// Used to create PDF annotation objects.
class Annotation {
    static let Link = "Link"
    static let FileAttachment = "FileAttachment"
    static let Polygon = "Polygon"
    static let Circle = "Circle"
    static let Square = "Square"
    static let Text = "Text"

    var objNumber: Int = 0
    var annotationType: String?
    var x1: Float = 0.0
    var y1: Float = 0.0
    var x2: Float = 0.0
    var y2: Float = 0.0
    var vertices: [Float]?
    var fillColor: [Float]?
    var transparency: Float = 0.0
    var title: String?
    var contents: String?
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String?
    var altDescription: String?
    var fileAttachment: FileAttachment?
    // Set once the annotation has been written with a /StructParent key.
    var structParentWritten = false

    /// Creates an annotation object.
    /// - Parameters:
    ///   - annotationType: The annotation type.
    ///   - x1: The x coordinate of the top left corner.
    ///   - y1: The y coordinate of the top left corner.
    ///   - x2: The x coordinate of the bottom right corner.
    ///   - y2: The y coordinate of the bottom right corner.
    ///   - vertices: The polygon annotation vertices.
    ///   - fillColor: The fill color as RGB floats.
    ///   - transparency: The transparency value (0.0 to 1.0).
    ///   - title: The annotation title.
    ///   - contents: The annotation content/description.
    ///   - uri: The URI string.
    ///   - key: The destination name.
    ///   - language: The language code.
    ///   - actualText: The actual text content. Defaults to uri if nil.
    ///   - altDescription: Alternative description. Defaults to uri if nil.
    init(
        _ annotationType: String?,
        _ x1: Float,
        _ y1: Float,
        _ x2: Float,
        _ y2: Float,
        _ vertices: [Float]?,
        _ fillColor: [Float]?,
        _ transparency: Float,
        _ title: String?,
        _ contents: String?,
        _ uri: String?,
        _ key: String?,
        _ language: String?,
        _ actualText: String?,
        _ altDescription: String?
    ) {
        self.annotationType = annotationType
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
        self.vertices = vertices
        self.fillColor = fillColor
        self.transparency = transparency
        self.title = title
        self.contents = contents
        self.uri = uri
        self.key = key
        self.language = language

        // Match Java ternary logic: (actualText == null) ? uri : actualText
        // A link created from a destination name has no uri to fall back on,
        // so use the name itself rather than leaving the link undescribed.
        let fallback = uri ?? key
        self.actualText = actualText ?? fallback
        self.altDescription = altDescription ?? fallback

        self.fileAttachment = nil // Will be set externally if needed
    }
}
