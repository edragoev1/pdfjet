/**
 * Point.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// A point with a marker: a shape drawn around its (x, y) coordinates, which
/// are the center of the marker. A point is drawn on a page on its own, as the
/// marker of a table cell, or in a chart Series, and an array of points is a
/// path for Page.drawPath.
///
/// Please see Example_05.
///
public class Point : Drawable {

    /// A control point of a curve drawn with the c operator, which uses both control points.
    public static let CONTROL_POINT_C: String = "c"
    /// A control point of a curve drawn with the v operator, where the first control point is the start point.
    public static let CONTROL_POINT_V: String = "v"
    /// A control point of a curve drawn with the y operator, where the second control point is the end point.
    public static let CONTROL_POINT_Y: String = "y"

    var x: Float = 0.0
    var y: Float = 0.0
    var r: Float = 2.0
    var shape = Shape.CIRCLE

    var fillColor: [Float]?
    var strokeWidth: Float = 1.0
    var strokeColor: [Float]?
    var pathOperator = PathOperator.CLOSE_AND_STROKE

    var controlPoint: String = ""
    private var uri: String?

    /// Creates a point.
    public init() {
    }

    ///
    /// Constructor for creating point objects.
    ///
    /// - Parameter x: the x coordinate of this point when drawn on the page.
    /// - Parameter y: the y coordinate of this point when drawn on the page.
    ///
    public init(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }

    ///
    /// Constructor for creating point objects.
    ///
    /// - Parameter x: the x coordinate of this point when drawn on the page.
    /// - Parameter y: the y coordinate of this point when drawn on the page.
    /// - Parameter controlPoint: the control point type: Point.CONTROL_POINT_C, Point.CONTROL_POINT_V or Point.CONTROL_POINT_Y.
    ///
    public init(
            _ x: Float,
            _ y: Float,
            _ controlPoint: String) {
        self.x = x
        self.y = y
        self.controlPoint = controlPoint
    }

    ///
    /// Creates a copy of the specified point, including its marker and its URI action.
    ///
    /// - Parameter point: the point to copy.
    ///
    public init(_ point: Point) {
        self.x = point.x
        self.y = point.y
        self.r = point.r
        self.shape = point.shape
        self.fillColor = point.fillColor
        self.strokeWidth = point.strokeWidth
        self.strokeColor = point.strokeColor
        self.pathOperator = point.pathOperator
        self.controlPoint = point.controlPoint
        self.uri = point.uri
    }

    ///
    /// Sets the location (x, y) of this point.
    ///
    /// - Parameter x: the x coordinate of this point when drawn on the page.
    /// - Parameter y: the y coordinate of this point when drawn on the page.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Sets the x coordinate of this point.
    ///
    /// - Parameter x: the x coordinate of this point when drawn on the page.
    ///
    @discardableResult
    public func setX(_ x: Float) -> Point {
        self.x = x
        return self
    }

    ///
    /// Returns the x coordinate of this point.
    ///
    /// - Returns: the x coordinate of this point.
    ///
    public func getX() -> Float {
        return self.x
    }

    ///
    /// Sets the y coordinate of this point.
    ///
    /// - Parameter y: the y coordinate of this point when drawn on the page.
    ///
    @discardableResult
    public func setY(_ y: Float) -> Point {
        self.y = y
        return self
    }

    ///
    /// Returns the y coordinate of this point.
    ///
    /// - Returns: the y coordinate of this point.
    ///
    public func getY() -> Float {
        return self.y
    }

    ///
    /// Sets the radius of this point.
    ///
    /// - Parameter r: the radius.
    ///
    @discardableResult
    public func setRadius(_ r: Float) -> Point {
        self.r = r
        return self
    }

    ///
    /// Returns the radius of this point.
    ///
    /// - Returns: the radius of this point.
    ///
    public func getRadius() -> Float {
        return self.r
    }

    ///
    /// Sets the shape of the marker drawn at this point.
    ///
    /// - Parameter shape: the shape, for example Shape.CIRCLE; Shape.INVISIBLE draws no marker.
    ///
    @discardableResult
    public func setShape(_ shape: Shape) -> Point {
        self.shape = shape
        return self
    }

    ///
    /// Returns the shape of the marker drawn at this point.
    ///
    public func getShape() -> Shape {
        return self.shape
    }

    ///
    /// Sets the fill color for this point.
    ///
    /// - Parameter fillColor: the color specified as Int32.
    ///
    @discardableResult
    public func setFillColor(_ fillColor: Int32) -> Point {
        let r = Float((fillColor >> 16) & 0xff)/255.0
        let g = Float((fillColor >>  8) & 0xff)/255.0
        let b = Float((fillColor)       & 0xff)/255.0
        return setFillColor([r, g, b])
    }

    ///
    /// Sets the fill color for this point.
    ///
    /// - Parameter fillColor: the red, green and blue values, or nil for no fill.
    ///
    @discardableResult
    public func setFillColor(_ fillColor: [Float]?) -> Point {
        self.fillColor = fillColor
        return self
    }

    ///
    /// Returns the point fill color, or nil when no fill color was set.
    ///
    /// - Returns: the color.
    ///
    public func getFillColor() -> [Float]? {
        return self.fillColor
    }

    /// Sets the stroke color as a 0xRRGGBB value.
    @discardableResult
    public func setStrokeColor(_ strokeColor: Int32) -> Point {
        let r = Float((strokeColor >> 16) & 0xff)/255.0
        let g = Float((strokeColor >>  8) & 0xff)/255.0
        let b = Float((strokeColor)       & 0xff)/255.0
        return setStrokeColor([r, g, b])
    }

    ///
    /// Sets the stroke color for this point.
    ///
    /// - Parameter strokeColor: the red, green and blue values, or nil for no stroke.
    ///
    @discardableResult
    public func setStrokeColor(_ strokeColor: [Float]?) -> Point {
        self.strokeColor = strokeColor
        return self
    }

    ///
    /// Returns the stroke color, or nil when no stroke color was set.
    ///
    /// - Returns: the stroke color.
    ///
    public func getStrokeColor() -> [Float]? {
        return self.strokeColor
    }

    ///
    /// Sets the stroke width.
    ///
    /// - Parameter strokeWidth: the stroke width.
    ///
    @discardableResult
    public func setStrokeWidth(_ strokeWidth: Float) -> Point {
        self.strokeWidth = strokeWidth
        return self
    }

    ///
    /// Returns the width of the lines used to draw this point.
    ///
    /// - Returns: the width of the lines used to draw this point.
    ///
    public func getStrokeWidth() -> Float {
        return self.strokeWidth
    }

    /// Returns the path operator the marker is painted with.
    func getPathOperator() -> PathOperator {
        return self.pathOperator
    }

    ///
    /// Sets the URI of the link opened by a click on this point.
    ///
    /// - Parameter uri: the URI.
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> Point {
        self.uri = uri
        return self
    }

    ///
    /// Returns the URI of the link opened by a click on this point.
    ///
    /// - Returns: the URI, or nil.
    ///
    public func getURIAction() -> String? {
        return self.uri
    }

    ///
    /// Draws this point on the specified page.
    ///
    /// - Parameter page: the page to draw this point on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [self.x + self.r, self.y + self.r]
        }

        // A point is a marker and carries no text, so it is decorative content.
        page.addArtifactBMC()
        page.saveGraphicsState()
        if fillColor != nil && strokeColor != nil {
            page.setBrushColor(fillColor)
            page.setPenColor(strokeColor)
            page.setPenWidth(strokeWidth)
            self.pathOperator = PathOperator.FILL_AND_STROKE
        } else if fillColor != nil && strokeColor == nil {
            page.setBrushColor(fillColor)
            self.pathOperator = PathOperator.FILL
        } else if fillColor == nil && strokeColor != nil {
            page.setPenColor(strokeColor)
            page.setPenWidth(strokeWidth)
            self.pathOperator = PathOperator.CLOSE_AND_STROKE
        }
        page.drawPoint(self)
        page.restoreGraphicsState()
        page.addEMC()
        return [self.x + self.r, self.y + self.r]
    }
}   // End of Point.swift
