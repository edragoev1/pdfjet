/**
 *  Path.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
import Foundation


///
/// Used to create path objects.
/// The path objects may consist of lines, splines or both.
///
/// Please see Example_02.
///
public class Path : Drawable {

    private var color = Color.black
    private var width: Float = 0.3
    private var pattern: String = "[] 0"
    private var fillShape = false
    private var closePath = false

    private var points: [Point]?

    private var xBox: Float = 0.0
    private var yBox: Float = 0.0

    private var lineCapStyle = 0
    private var lineJoinStyle = 0


    ///
    /// The default constructor.
    ///
    public init() {
        points = Array<Point>()
    }


    ///
    /// Adds a point to this path.
    ///
    /// - Parameter point the point to add.
    ///
    public func add(_ point: Point) {
        points!.append(point)
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
    /// <pre>
    /// Examples of line dash patterns:
    ///
    ///     "[Array] Phase"     Appearance          Description
    ///     _______________     _________________   ____________________________________
    ///
    ///     "[] 0"              -----------------   Solid line
    ///     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// </pre>
    ///
    /// - Parameter pattern the line dash pattern.
    ///
    public func setPattern(_ pattern: String) {
        self.pattern = pattern
    }


    ///
    /// Sets the pen width that will be used to draw the lines and splines that are part of this path.
    ///
    /// - Parameter width the pen width.
    ///
    public func setWidth(_ width: Float) {
        self.width = width
    }


    ///
    /// Sets the pen color that will be used to draw this path.
    ///
    /// - Parameter color the color is specified as an integer.
    ///
    public func setColor(_ color: UInt32) {
        self.color = color
    }


    ///
    /// Sets the closePath variable.
    ///
    /// - Parameter closePath if closePath is true a line will be draw between the first and last point of this path.
    ///
    public func setClosePath(_ closePath: Bool) {
        self.closePath = closePath
    }


    ///
    /// Sets the fillShape private variable.
    /// If fillShape is true - the shape of the path will be filled with the current brush color.
    ///
    /// - Parameter fillShape the fillShape flag.
    ///
    public func setFillShape(_ fillShape: Bool) {
        self.fillShape = fillShape
    }


    ///
    /// Sets the line cap style.
    ///
    /// - Parameter style the cap style of this path. Supported values: Cap.BUTT, Cap.ROUND and Cap.PROJECTING_SQUARE
    ///
    public func setLineCapStyle(_ style: Int) {
        self.lineCapStyle = style
    }


    ///
    /// Returns the line cap style for this path.
    ///
    /// - Returns: the line cap style for this path.
    ///
    public func getLineCapStyle() -> Int {
        return self.lineCapStyle
    }


    ///
    /// Sets the line join style.
    ///
    /// - Parameter style the line join style code. Supported values: Join.MITER, Join.ROUND and Join.BEVEL
    ///
    public func setLineJoinStyle(_ style: Int) {
        self.lineJoinStyle = style
    }


    ///
    /// Returns the line join style.
    ///
    /// - Returns: the line join style.
    ///
    public func getLineJoinStyle() -> Int {
        return self.lineJoinStyle
    }


    ///
    /// Places this path in the specified box at position (0.0, 0.0).
    ///
    /// - Parameter box the specified box.
    ///
    public func placeIn(_ box: Box) {
        placeIn(box, 0.0, 0.0)
    }


    ///
    /// Places the path inside the spacified box at coordinates (xOffset, yOffset) of the top left corner.
    ///
    /// - Parameter box the specified box.
    /// - Parameter xOffset the xOffset.
    /// - Parameter yOffset the yOffset.
    ///
    public func placeIn(
            _ box: Box,
            _ xOffset: Float,
            _ yOffset: Float) {
        xBox = box.x + xOffset
        yBox = box.y + yOffset
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Path {
        xBox += x
        yBox += y
        return self
    }


    ///
    /// Scales the path using the specified factor.
    ///
    /// - Parameter factor the specified factor.
    ///
    public func scaleBy(_ factor: Float) {
        for i in 0..<points!.count {
            let point = points![i]
            point.x *= factor
            point.y *= factor
        }
    }


    ///
    /// Returns a list containing the start point, first control point,
    /// second control point and the end point of elliptical curve segment.
    /// Please see Example_18.
    ///
    /// - Parameter x the x coordinate of the center of the ellipse.
    /// - Parameter y the y coordinate of the center of the ellipse.
    /// - Parameter r1 the horizontal radius of the ellipse.
    /// - Parameter r2 the vertical radius of the ellipse.
    /// - Parameter segment the segment to draw - please see the Segment class.
    /// - Returns: a list of the curve points.
    ///
    public static func getCurvePoints(
            _ x: Float,
            _ y: Float,
            _ r1: Float,
            _ r2: Float,
            _ segment: Int) -> [Point] {
        // The best 4-spline magic number
        let m4: Float = 0.551784
        var list = Array<Point>()

        if segment == 0 {
            list.append(Point(x, y - r2))
            list.append(Point(x + m4*r1, y - r2, Point.CONTROL_POINT))
            list.append(Point(x + r1, y - m4*r2, Point.CONTROL_POINT))
            list.append(Point(x + r1, y))
        }
        else if segment == 1 {
            list.append(Point(x + r1, y))
            list.append(Point(x + r1, y + m4*r2, Point.CONTROL_POINT))
            list.append(Point(x + m4*r1, y + r2, Point.CONTROL_POINT))
            list.append(Point(x, y + r2))
        }
        else if segment == 2 {
            list.append(Point(x, y + r2))
            list.append(Point(x - m4*r1, y + r2, Point.CONTROL_POINT))
            list.append(Point(x - r1, y + m4*r2, Point.CONTROL_POINT))
            list.append(Point(x - r1, y))
        }
        else if segment == 3 {
            list.append(Point(x - r1, y))
            list.append(Point(x - r1, y - m4*r2, Point.CONTROL_POINT))
            list.append(Point(x - m4*r1, y - r2, Point.CONTROL_POINT))
            list.append(Point(x, y - r2))
        }

        return list
    }


    ///
    /// Draws this path on the page using the current selected color, pen width, line pattern and line join style.
    ///
    /// - Parameter page the page to draw this path on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if fillShape {
            page!.setBrushColor(self.color)
        }
        else {
            page!.setPenColor(self.color)
        }
        page!.setPenWidth(self.width)
        page!.setLinePattern(self.pattern)
        page!.setLineCapStyle(self.lineCapStyle)
        page!.setLineJoinStyle(self.lineJoinStyle)

        for i in 0..<points!.count {
            let point = points![i]
            point.x += xBox
            point.y += yBox
        }

        if fillShape {
            page!.drawPath(points!, "f")
        }
        else {
            if closePath {
                page!.drawPath(points!, "s")
            }
            else {
                page!.drawPath(points!, "S")
            }
        }

        var xMax: Float = 0.0
        var yMax: Float = 0.0
        for i in 0..<points!.count {
            let point = points![i]
            if point.x > xMax { xMax = point.x }
            if point.y > yMax { yMax = point.y }
            point.x -= xBox
            point.y -= yBox
        }

        return [xMax, yMax]
    }

}   // End of Path.swift
