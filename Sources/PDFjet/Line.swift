/**
 * Line.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create line objects.
///
/// Please see Example_23 and Example_46.
///
public class Line : Drawable {
    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var x2: Float = 0.0
    private var y2: Float = 0.0

    private var color = Color.black
    private var width: Float = 0.0
    private var pattern: String = "[] 0"
    private var capStyle = CapStyle.BUTT

    private var language: String?
    private var actualText: String = Single.space
    private var altDescription: String = Single.space

    ///
    /// The default constructor.
    ///
    public init() {
    }

    ///
    /// Create a line object.
    ///
    /// - Parameter x1: the x coordinate of the start point.
    /// - Parameter y1: the y coordinate of the start point.
    /// - Parameter x2: the x coordinate of the end point.
    /// - Parameter y2: the y coordinate of the end point.
    ///
    public init(_ x1: Float, _ y1: Float, _ x2: Float, _ y2: Float) {
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
    }

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
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setPattern(_ pattern: String) -> Line {
        self.pattern = pattern
        return self
    }

    ///
    /// Sets the start point of this line. The end point does not move.
    ///
    /// - Parameter x: the x coordinate of the start point.
    /// - Parameter y: the y coordinate of the start point.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        setStartPoint(x, y)
        return self
    }

    ///
    /// Sets the x and y coordinates of the start point.
    ///
    /// - Parameter x: the x coordinate of the start point.
    /// - Parameter y: the y coordinate of the start point.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setStartPoint(_ x: Float, _ y: Float) -> Line {
        self.x1 = x
        self.y1 = y
        return self
    }

    ///
    /// Sets the x and y coordinates of the start point.
    ///
    /// - Parameter x: the x coordinate of the start point.
    /// - Parameter y: the y coordinate of the start point.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setPointA(_ x: Float, _ y: Float) -> Line {
        self.x1 = x
        self.y1 = y
        return self
    }

    ///
    /// Returns the start point of this line.
    ///
    /// - Returns: Point the point.
    ///
    public func getStartPoint() -> Point {
        return Point(x1, y1)
    }

    ///
    /// Sets the x and y coordinates of the end point.
    ///
    /// - Parameter x: the x coordinate of the end point.
    /// - Parameter y: the y coordinate of the end point.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setEndPoint(_ x: Float, _ y: Float) -> Line {
        self.x2 = x
        self.y2 = y
        return self
    }

    ///
    /// Sets the x and y coordinates of the end point.
    ///
    /// - Parameter x: the x coordinate of the end point.
    /// - Parameter y: the y coordinate of the end point.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setPointB(_ x: Float, _ y: Float) -> Line {
        self.x2 = x
        self.y2 = y
        return self
    }

    ///
    /// Returns the end point of this line.
    ///
    /// - Returns: Point the point.
    ///
    public func getEndPoint() -> Point {
        return Point(x2, y2)
    }

    ///
    /// Sets the stroke width of this line.
    ///
    /// - Parameter width: the width.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setStrokeWidth(_ width: Float) -> Line {
        self.width = width
        return self
    }

    ///
    /// Sets the stroke color of this line.
    ///
    /// - Parameter color: the color specified as an integer.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setStrokeColor(_ color: Int32) -> Line {
        self.color = color
        return self
    }

    ///
    /// Sets the line cap style.
    ///
    /// - Parameter style: the cap style of the current line.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func setCapStyle(_ style: CapStyle) -> Line {
        self.capStyle = style
        return self
    }

    ///
    /// Returns the line cap style.
    ///
    /// - Returns: the cap style.
    ///
    public func getCapStyle() -> CapStyle {
        return self.capStyle
    }

    ///
    /// Sets the alternate description of this line.
    ///
    /// - Parameter altDescription: the alternate description of the line.
    /// - Returns: this Line.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Line {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the actual text for this line.
    ///
    /// - Parameter actualText: the actual text for the line.
    /// - Returns: this Line.
    ///
    @discardableResult
    public func setActualText(_ actualText: String) -> Line {
        self.actualText = actualText
        return self
    }

    ///
    /// Scales this line by the specified factor.
    ///
    /// - Parameter factor: the factor used to scale the line.
    /// - Returns: this Line object.
    ///
    @discardableResult
    public func scaleBy(_ factor: Float) -> Line {
        self.x1 *= factor
        self.x2 *= factor
        self.y1 *= factor
        self.y2 *= factor
        return self
    }

    ///
    /// Draws this line on the specified page.
    ///
    /// - Parameter page: the page to draw this line on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.addBMC(StructElem.P, language, actualText, altDescription)
        page!.saveGraphicsState()
        page!.setPenColor(color)
        page!.setPenWidth(width)
        page!.setLineCapStyle(capStyle)
        page!.setStrokeDashPattern(pattern)
        page!.drawLine(x1, y1, x2, y2)
        page!.restoreGraphicsState()
        page!.addEMC()

        let xMax = max(x1, x2)
        let yMax = max(y1, y2)
        return [xMax, yMax]
    }
}   // End of Line.swift
