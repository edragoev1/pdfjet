/**
 * Rect.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A rectangle that can be drawn on a page.
public class Rect : Drawable {
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var width: Float = 0.0
    private var height: Float = 0.0
    private var r: Float = 0.0

    private var fillColor: [Float]?
    private var borderColor: [Float]?
    private var borderWidth: Float = 0.0
    private var borderPattern: String = "[] 0"

    private var uri: String?
    private var key: String?
    private var language: String?
    private var altDescription: String?
    private var actualText: String?

    /**
     * Creates new Rect object.
     */
    public init() {
    }

    /**
     * Creates a rect object.
     * - Parameters:
     *   - x: the x coordinate of the top left corner of this rect when drawn on the page.
     *   - y: the y coordinate of the top left corner of this rect when drawn on the page.
     *   - w: the width of this rect.
     *   - h: the height of this rect.
     */
    public convenience init(_ x: Float, _ y: Float, _ w: Float, _ h: Float) {
        self.init()
        self.x = x
        self.y = y
        self.width = w
        self.height = h
    }

    /**
     * Sets the location of this rect on the page.
     * - Parameters:
     *   - x: the x coordinate of the top left corner of this rect when drawn on the page.
     *   - y: the y coordinate of the top left corner of this rect when drawn on the page.
     * - Returns: this Rect.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /**
     * Sets the size of this rect.
     * - Parameters:
     *   - w: the width of this rect.
     *   - h: the height of this rect.
     *
     * - Returns: this Rect object.
     */
    @discardableResult
    public func setSize(_ w: Float, _ h: Float) -> Rect {
        self.width = w
        self.height = h
        return self
    }

    /// Sets the fill color from an array of red, green and blue values, or nil for no fill.
    @discardableResult
    public func setFillColor(_ fillColor: [Float]?) -> Rect {
        self.fillColor = fillColor
        return self
    }

    /// Sets the fill color as a 0xRRGGBB value.
    @discardableResult
    public func setFillColor(_ color: Int32) -> Rect {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.fillColor = [r, g, b]
        return self
    }

    /// Sets the border color as a 0xRRGGBB value.
    @discardableResult
    public func setBorderColor(_ color: Int32) -> Rect {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.borderColor = [r, g, b]
        return self
    }

    /// Sets the border color from an array of red, green and blue values.
    @discardableResult
    public func setBorderColor(_ borderColor: [Float]?) -> Rect {
        self.borderColor = borderColor!
        return self
    }

    /// Sets the border width.
    @discardableResult
    public func setBorderWidth(_ borderWidth: Float) -> Rect {
        self.borderWidth = borderWidth
        return self
    }

    /**
     * Sets the corner radius.
     * - Parameter r: the radius.
     *
     * - Returns: this Rect object.
     */
    @discardableResult
    public func setCornerRadius(_ r: Float) -> Rect {
        self.r = r
        return self
    }

    /**
     * Sets the URI for the "click rect" action.
     * - Parameter uri: the URI
     *
     * - Returns: this Rect object.
     */
    @discardableResult
    public func setURIAction(_ uri: String) -> Rect {
        self.uri = uri
        return self
    }

    /**
     * Sets the destination key for the action.
     * - Parameter key: the destination name.
     *
     * - Returns: this Rect object.
     */
    @discardableResult
    public func setGoToAction(_ key: String) -> Rect {
        self.key = key
        return self
    }

    /**
     * Sets the language of this rect, used for accessibility.
     * - Parameter language: the language, for example "en-US".
     *
     * - Returns: this Rect object.
     */
    @discardableResult
    public func setLanguage(_ language: String) -> Rect {
        self.language = language
        return self
    }

    /**
     * Sets the alternate description of this rect.
     * - Parameter altDescription: the alternate description of the rect.
     * - Returns: this Rect.
     */
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Rect {
        self.altDescription = altDescription
        return self
    }

    /**
     * Sets the actual text for this rect.
     * - Parameter actualText: the actual text for the rect.
     * - Returns: this Rect.
     */
    @discardableResult
    public func setActualText(_ actualText: String) -> Rect {
        self.actualText = actualText
        return self
    }


    /// Sets the dash pattern of the border.
    @discardableResult
    public func setBorderPattern(_ borderPattern: String) -> Rect {
        self.borderPattern = borderPattern
        return self
    }

    /**
     * Scales this rect by the specified factor.
     * - Parameter factor: the factor used to scale the rect.
     */
    public func scaleBy(_ factor: Float) {
        self.x *= factor
        self.y *= factor
    }

    /**
     * Draws this rect on the specified page.
     * - Parameter page: the page to draw this rect on.
     * - Throws: an exception if drawing fails.
     * - Returns: x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let k: Float = 0.55228

        // A rectangle carries no text, so it is decorative content.
        page!.addArtifactBMC()
        page!.saveGraphicsState()

        if self.r == 0.0 {
            if fillColor != nil {
                page!.moveTo(self.x, self.y)
                page!.lineTo(self.x + self.width, self.y)
                page!.lineTo(self.x + self.width, self.y + self.height)
                page!.lineTo(self.x, self.y + self.height)
                page!.lineTo(self.x, self.y)
                page!.setBrushColor(self.fillColor)
                page!.fillPath()
            }
            if borderColor != nil {
                page!.moveTo(self.x, self.y)
                page!.lineTo(self.x + self.width, self.y)
                page!.lineTo(self.x + self.width, self.y + self.height)
                page!.lineTo(self.x, self.y + self.height)
                page!.setPenColor(self.borderColor)
                page!.setPenWidth(self.borderWidth)
                page!.setStrokeDashPattern(self.borderPattern)
                page!.closePath()
            }
        } else {
            // The pen and brush must be set before the path is painted,
            // otherwise the rounded rectangle is drawn with whatever state
            // the page happened to be left in.
            if borderColor != nil {
                page!.setStrokeDashPattern(self.borderPattern)
            }
            if fillColor != nil {
                page!.setBrushColor(self.fillColor)
            }
            if borderColor != nil {
                page!.setPenWidth(self.borderWidth)
                page!.setPenColor(self.borderColor)
            }

            var points: [Point] = []
            points.append(Point(self.x + self.r, self.y))
            points.append(Point((self.x + self.width) - self.r, self.y))
            points.append(Point((self.x + self.width - self.r) + self.r * k, self.y, Point.controlPointC))
            points.append(Point(self.x + self.width, (self.y + self.r) - self.r * k, Point.controlPointC))
            points.append(Point(self.x + self.width, self.y + self.r))
            points.append(Point(self.x + self.width, (self.y + self.height) - self.r))
            points.append(Point(self.x + self.width, ((self.y + self.height) - self.r) + self.r * k, Point.controlPointC))
            points.append(Point(((self.x + self.width) - self.r) + self.r * k, self.y + self.height, Point.controlPointC))
            points.append(Point(((self.x + self.width) - self.r), self.y + self.height))
            points.append(Point(self.x + self.r, self.y + self.height))
            points.append(Point(((self.x + self.r) - self.r * k), self.y + self.height, Point.controlPointC))
            points.append(Point(self.x, ((self.y + self.height) - self.r) + self.r * k, Point.controlPointC))
            points.append(Point(self.x, (self.y + self.height) - self.r))
            points.append(Point(self.x, self.y + self.r))
            points.append(Point(self.x, (self.y + self.r) - self.r * k, Point.controlPointC))
            points.append(Point((self.x + self.r) - self.r * k, self.y, Point.controlPointC))
            points.append(Point(self.x + self.r, self.y))

            if fillColor != nil && borderColor == nil {
                page!.drawPath(points, PathOperator.fill)
            } else if fillColor == nil && borderColor != nil {
                page!.drawPath(points, PathOperator.stroke)
            } else if fillColor != nil && borderColor != nil {
                page!.drawPath(points, PathOperator.fillAndStroke)
            }
        }
        page!.restoreGraphicsState()
        page!.addEMC()

        if self.uri != nil || self.key != nil {
            page!.addAnnotation(Annotation(
                    Annotation.Link,
                    self.x,
                    self.y,
                    self.x + self.width,
                    self.y + self.height,
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Transparency
                    nil,    // Title
                    nil,    // Contents
                    self.uri,
                    self.key,
                    self.language,
                    self.actualText,
                    self.altDescription))
        }

        return [self.x + self.width, self.y + self.height]
    }
}
