/**
 *  Path.swift
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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

    private var lineCapStyle = CapStyle.BUTT
    private var lineJoinStyle = JoinStyle.MITER

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
    public func setColor(_ color: Int32) {
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
    /// - Parameter style the cap style of this path.
    /// Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
    ///
    public func setLineCapStyle(_ style: CapStyle) {
        self.lineCapStyle = style
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
    /// - Parameter style the line join style code. Supported values: Join.MITER, Join.ROUND and Join.BEVEL
    ///
    public func setLineJoinStyle(_ style: JoinStyle) {
        self.lineJoinStyle = style
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
    /// Draws this path on the page using the current selected color, pen width, line pattern and line join style.
    ///
    /// - Parameter page the page to draw this path on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        for i in 0..<points!.count {
            let point = points![i]
            point.x += xBox
            point.y += yBox
        }

        if fillShape {
            page!.setBrushColor(self.color)
            page!.drawPath(points!, Operation.FILL)
        } else {
            page!.setPenWidth(self.width)
            page!.setPenColor(self.color)
            page!.setLinePattern(self.pattern)
            page!.setLineCapStyle(self.lineCapStyle)
            page!.setLineJoinStyle(self.lineJoinStyle)
            if closePath {
                page!.drawPath(points!, Operation.CLOSE)
            } else {
                page!.drawPath(points!, Operation.STROKE)
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
