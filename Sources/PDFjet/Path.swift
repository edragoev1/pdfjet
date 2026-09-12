/**
 * Path.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create path objects.
/// The path objects may consist of lines, splines or both.
///
/// Please see Example_20 and Example_22.
///
public class Path : Drawable {
    private var color = Color.black
    private var width: Float = 0.0
    private var pattern: String = "[] 0"
    private var fillShape = false
    private var closePath = false

    private var points = [Point]()

    private var xBox: Float = 0.0
    private var yBox: Float = 0.0

    private var lineCapStyle = CapStyle.BUTT
    private var lineJoinStyle = JoinStyle.MITER

    ///
    /// The default constructor.
    ///
    public init() {
    }

    ///
    /// Adds a point to this path.
    ///
    /// - Parameter point: the point to add.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func add(_ point: Point) -> Path {
        points.append(point)
        return self
    }

    ///
    /// Sets the line dash pattern for this path.
    ///
    /// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    /// Examples of line dash patterns:
    ///
    /// ```
    ///     "[Array] Phase"     Appearance          Description
    ///     _______________     _________________   ____________________________________
    ///
    ///     "[] 0"              -----------------   Solid line
    ///     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// ```
    ///
    /// - Parameter pattern: the line dash pattern.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setPattern(_ pattern: String) -> Path {
        self.pattern = pattern
        return self
    }

    ///
    /// Sets the stroke width that will be used to draw the lines and splines that are part of this path.
    ///
    /// - Parameter width: the stroke width.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setStrokeWidth(_ width: Float) -> Path {
        self.width = width
        return self
    }

    ///
    /// Sets the stroke color that will be used to draw this path.
    ///
    /// - Parameter color: the color specified as an integer.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> Path {
        self.color = color
        return self
    }

    ///
    /// Sets whether a line is drawn from the last point of this path back to the first.
    ///
    /// - Parameter closePath: true to close the path.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setClosePath(_ closePath: Bool) -> Path {
        self.closePath = closePath
        return self
    }

    ///
    /// Sets whether the shape of this path is filled with the stroke color instead of stroked.
    ///
    /// - Parameter fillShape: true to fill the shape.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setFillShape(_ fillShape: Bool) -> Path {
        self.fillShape = fillShape
        return self
    }

    ///
    /// Sets the line cap style.
    ///
    /// - Parameter style: the cap style of this path.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setLineCapStyle(_ style: CapStyle) -> Path {
        self.lineCapStyle = style
        return self
    }

    ///
    /// Returns the line cap style for this path.
    ///
    /// - Returns: the line cap style for this path.
    ///
    public func getLineCapStyle() -> CapStyle {
        return self.lineCapStyle
    }

    ///
    /// Sets the line join style.
    ///
    /// - Parameter style: the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setLineJoinStyle(_ style: JoinStyle) -> Path {
        self.lineJoinStyle = style
        return self
    }

    ///
    /// Returns the line join style.
    ///
    /// - Returns: the line join style.
    ///
    public func getLineJoinStyle() -> JoinStyle {
        return self.lineJoinStyle
    }

    ///
    /// Sets the location of this path: its points are drawn offset by x and y.
    ///
    /// - Parameter x: the x offset.
    /// - Parameter y: the y offset.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        xBox = x
        yBox = y
        return self
    }

    ///
    /// Scales the points of this path by the specified factor.
    ///
    /// - Parameter factor: the factor used to scale the path.
    /// - Returns: this Path object.
    ///
    @discardableResult
    public func scaleBy(_ factor: Float) -> Path {
        for point in points {
            point.x *= factor
            point.y *= factor
        }
        return self
    }

    ///
    /// Draws this path on the specified page. If fillShape is set the shape is
    /// filled with the stroke color; otherwise the path is stroked with its
    /// width, dash pattern, cap style and join style.
    ///
    /// - Parameter page: the page to draw this path on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        for point in points {
            point.x += xBox
            point.y += yBox
        }

        // A path carries no text, so it is decorative content.
        page!.addArtifactBMC()
        if fillShape {
            page!.setBrushColor(self.color)
            page!.drawPath(points, PathOperator.fill)
        } else {
            page!.setPenWidth(self.width)
            page!.setPenColor(self.color)
            page!.setStrokeDashPattern(self.pattern)
            page!.setLineCapStyle(self.lineCapStyle)
            page!.setLineJoinStyle(self.lineJoinStyle)
            if closePath {
                page!.drawPath(points, PathOperator.closeAndStroke)
            } else {
                page!.drawPath(points, PathOperator.stroke)
            }
        }
        page!.addEMC()

        var xMax: Float = 0.0
        var yMax: Float = 0.0
        for point in points {
            if point.x > xMax { xMax = point.x }
            if point.y > yMax { yMax = point.y }
            point.x -= xBox
            point.y -= yBox
        }

        return [xMax, yMax]
    }
}   // End of Path.swift
