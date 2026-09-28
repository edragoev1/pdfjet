/**
 * BaseAnnotation.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The base class of the circle, square, polygon and text annotations.
public class BaseAnnotation: Drawable {
    var annotationType: String?
    var point1: [Float] = [0, 0]
    var vertices: [Float]?
    var fillColor: [Float] = [0.5, 0.5, 0.5]
    var opacity: Float = 1.0
    var title: String?
    var contents: String?
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String?
    var altDescription: String?
    // The size setSize sets, which the second point is at from the first when
    // the annotation is drawn, wherever setLocation puts the first.
    var width: Float = 0.0
    var height: Float = 0.0
    var hasSize = false

    /// Creates an annotation. The circle, square, polygon and text annotations call it.
    init() {
    }

    /// Sets the location of this annotation.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.point1 = [x, y]
        return self
    }

    /// Sets the size of this annotation, measured from its location: its second
    /// point is width to the right of its location and height below it, whether
    /// setLocation is called before or after.
    @discardableResult
    public func setSize(_ width: Float, _ height: Float) -> BaseAnnotation {
        self.width = width
        self.height = height
        self.hasSize = true
        return self
    }

    // Returns the second point of the annotation, which is the origin of the
    // page until setSize is called.
    private func getCorner() -> [Float] {
        if !hasSize {
            return [0.0, 0.0]
        }
        return [point1[0] + width, point1[1] + height]
    }

    /// Sets the fill color from an array of red, green and blue values.
    @discardableResult
    public func setFillColor(_ color: [Float]) -> BaseAnnotation {
        self.fillColor = color
        return self
    }

    /// Sets the fill color as a 0xRRGGBB value.
    @discardableResult
    public func setFillColor(_ color: Int32) -> BaseAnnotation {
        let r = Float((color >> 16) & 0xff) / 255.0
        let g = Float((color >> 8) & 0xff) / 255.0
        let b = Float(color & 0xff) / 255.0
        setFillColor([r, g, b])
        return self
    }

    /// Sets the opacity of this annotation, from 0.0 (invisible) to 1.0 (opaque, the default).
    @discardableResult
    public func setOpacity(_ opacity: Float) -> BaseAnnotation {
        self.opacity = opacity
        return self
    }

    /// Sets the title of this annotation.
    @discardableResult
    public func setTitle(_ title: String?) -> BaseAnnotation {
        self.title = title
        return self
    }

    /// Sets the text contents of this annotation.
    @discardableResult
    public func setContents(_ contents: String?) -> BaseAnnotation {
        self.contents = contents
        return self
    }

    /// Adds this annotation to the specified page.
    public func drawOn(_ page: Page?) -> [Float] {
        let point2 = getCorner()
        if page == nil {
            return point2   // Measured, not drawn
        }
        page!.addAnnotation(Annotation(
            annotationType,
            point1[0],
            point1[1],
            point2[0],
            point2[1],
            vertices,
            fillColor,
            opacity,
            title,
            contents,
            uri,
            key,
            language,
            actualText,
            altDescription))
        return point2
    }
}
