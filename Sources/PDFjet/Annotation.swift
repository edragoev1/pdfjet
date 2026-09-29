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

    // The icons that the appearances of a file attachment and of a note draw
    // in a box of 1 by 1, which is scaled to the rectangle of the annotation,
    // as a viewer draws them: a push pin, a paperclip, and a speech bubble
    // with two lines of text for a note.
    static let pushPinIcon =
            "0 g\n0.34 0.7 0.32 0.1 re\n0.42 0.5 0.16 0.2 re\n0.3 0.44 0.4 0.06 re\nf\n" +
            "0.04 w 1 J\n0.5 0.44 m\n0.5 0.16 l\nS\n"
    static let paperclipIcon =
            "0.04 w 1 J 1 j\n0.44 0.62 m\n0.44 0.34 l\n0.44 0.26 0.56 0.26 0.56 0.34 c\n" +
            "0.56 0.76 l\n0.56 0.88 0.32 0.88 0.32 0.76 c\n0.32 0.28 l\n0.32 0.12 0.68 0.12 0.68 0.28 c\n" +
            "0.68 0.66 l\nS\n"
    static let noteIcon =
            "0.04 w 1 j\n0.2 0.8 m\n0.8 0.8 l\n0.8 0.4 l\n0.48 0.4 l\n0.3 0.22 l\n0.34 0.4 l\n" +
            "0.2 0.4 l\nh\n0.3 0.66 m\n0.7 0.66 l\n0.3 0.53 m\n0.7 0.53 l\nS\n"

    var objNumber: Int = 0
    var annotationType: String?
    var x1: Float = 0.0
    var y1: Float = 0.0
    var x2: Float = 0.0
    var y2: Float = 0.0
    var vertices: [Float]?
    var fillColor: [Float]?
    var opacity: Float = 0.0
    var title: String?
    var contents: String?
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String?
    var altDescription: String?
    var fileAttachment: FileAttachment?
    // The Link element of the content the link is drawn on, which the
    // annotation joins, so that the text of a link and its annotation are one
    // element, as PDF/UA asks; nil for a link over content that is not tagged
    var linkElement: StructElement?
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
    ///   - opacity: The opacity value (0.0 to 1.0).
    ///   - title: The annotation title.
    ///   - contents: The annotation content/description.
    ///   - uri: The URI string.
    ///   - key: The destination name.
    ///   - language: The language code.
    ///   - actualText: The actual text content. Defaults to uri if nil.
    ///   - altDescription: Alternative description. Defaults to uri if nil.
    // Returns the annotation, joining the Link element of the content it is
    // drawn on, or none for nil.
    func joining(_ link: StructElement?) -> Annotation {
        linkElement = link
        return self
    }

    init(
        _ annotationType: String?,
        _ x1: Float,
        _ y1: Float,
        _ x2: Float,
        _ y2: Float,
        _ vertices: [Float]?,
        _ fillColor: [Float]?,
        _ opacity: Float,
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
        self.opacity = opacity
        self.title = title
        self.contents = contents
        self.uri = uri
        self.key = key
        self.language = language

        // Match Java ternary logic: (actualText == null) ? uri : actualText
        // A link created from a destination name has no uri to fall back on,
        // so use the name itself rather than leaving the link undescribed.
        let fallback = !(uri ?? "").isEmpty ? uri : key
        self.actualText = !(actualText ?? "").isEmpty ? actualText : fallback
        self.altDescription = !(altDescription ?? "").isEmpty ? altDescription : fallback

        self.fileAttachment = nil // Will be set externally if needed
    }
}
