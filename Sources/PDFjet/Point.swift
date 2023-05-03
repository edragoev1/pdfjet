/**
 *  Point.swift
 *
Copyright 2023 Innovatics Inc.

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


///
/// Used to create point objects with different shapes and draw them on a page.
/// Please note: When we are mentioning (x, y) coordinates of a point - we are
/// talking about the coordinates of the center of the point.
///
/// Please see Example_05.
///
public class Point : Drawable {

    public static let INVISIBLE: Int = -1
    public static let CIRCLE: Int = 0
    public static let DIAMOND: Int = 1
    public static let BOX: Int = 2
    public static let PLUS: Int = 3
    public static let H_DASH: Int = 4
    public static let V_DASH: Int = 5
    public static let MULTIPLY: Int = 6
    public static let STAR: Int = 7
    public static let X_MARK: Int = 8
    public static let UP_ARROW: Int = 9
    public static let DOWN_ARROW: Int = 10
    public static let LEFT_ARROW: Int = 11
    public static let RIGHT_ARROW: Int = 12

    public static let CONTROL_POINT: Bool = true

    var x: Float = 0.0
    var y: Float = 0.0
    var r: Float = 2.0
    var shape = Point.CIRCLE
    var color = Color.black
    var align = Align.RIGHT
    var lineWidth: Float = 0.3
    var linePattern: String = "[] 0"
    var fillShape = false
    var isControlPoint = false
    var drawPath = false

    private var text: String?
    private var textColor = Color.black
    private var textDirection: Int = 0
    private var uri: String?
    private var xBox: Float = 0.0
    private var yBox: Float = 0.0


    public init() {
    }


    ///
    /// Constructor for creating point objects.
    ///
    /// - Parameter x the x coordinate of this point when drawn on the page.
    /// - Parameter y the y coordinate of this point when drawn on the page.
    ///
    public init(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }


    ///
    /// Constructor for creating point objects.
    ///
    /// - Parameter x the x coordinate of this point when drawn on the page.
    /// - Parameter y the y coordinate of this point when drawn on the page.
    /// - Parameter isControlPoint true if this point is one of the points specifying a curve.
    ///
    public init(
            _ x: Float,
            _ y: Float,
            _ isControlPoint: Bool) {
        self.x = x
        self.y = y
        self.isControlPoint = isControlPoint
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    ///
    /// Sets the location (x, y) of this point.
    ///
    /// - Parameter x the x coordinate of this point when drawn on the page.
    /// - Parameter y the y coordinate of this point when drawn on the page.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Point {
        self.x = x
        self.y = y
        return self
    }


    ///
    /// Sets the x coordinate of this point.
    ///
    /// - Parameter x the x coordinate of this point when drawn on the page.
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
    /// - Parameter y the y coordinate of this point when drawn on the page.
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
    /// - Parameter r the radius.
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
    /// Sets the shape of this point.
    ///
    /// - Parameter shape the shape of this point. Supported values:
    /// <pre>
    /// Point.INVISIBLE
    /// Point.CIRCLE
    /// Point.DIAMOND
    /// Point.BOX
    /// Point.PLUS
    /// Point.H_DASH
    /// Point.V_DASH
    /// Point.MULTIPLY
    /// Point.STAR
    /// Point.X_MARK
    /// Point.UP_ARROW
    /// Point.DOWN_ARROW
    /// Point.LEFT_ARROW
    /// Point.RIGHT_ARROW
    /// </pre>
    ///
    @discardableResult
    public func setShape(_ shape: Int) -> Point {
        self.shape = shape
        return self
    }


    ///
    /// Returns the point shape code value.
    ///
    /// - Returns: the shape code value.
    ///
    public func getShape() -> Int {
        return self.shape
    }


    ///
    /// Sets the private fillShape variable.
    ///
    /// - Parameter fillShape if true - fill the point with the specified brush color.
    ///
    @discardableResult
    public func setFillShape(_ fillShape: Bool) -> Point {
        self.fillShape = fillShape
        return self
    }


    ///
    /// Returns the value of the fillShape private variable.
    ///
    /// - Returns: the value of the private fillShape variable.
    ///
    public func getFillShape() -> Bool {
        return self.fillShape
    }


    ///
    /// Sets the pen color for this point.
    ///
    /// - Parameter color the color specified as an integer.
    ///
    @discardableResult
    public func setColor(_ color: Int32) -> Point {
        self.color = color
        return self
    }


    ///
    /// Returns the point color as an integer.
    ///
    /// - Returns: the color.
    ///
    public func getColor() -> Int32 {
        return self.color
    }


    ///
    /// Sets the width of the lines of this point.
    ///
    /// - Parameter lineWidth the line width.
    ///
    @discardableResult
    public func setLineWidth(_ lineWidth: Float) -> Point {
        self.lineWidth = lineWidth
        return self
    }


    ///
    /// Returns the width of the lines used to draw this point.
    ///
    /// - Returns: the width of the lines used to draw this point.
    ///
    public func getLineWidth() -> Float {
        return self.lineWidth
    }


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
    /// - Parameter linePattern the line dash pattern.
    ///
    @discardableResult
    public func setLinePattern(_ linePattern: String) -> Point {
        self.linePattern = linePattern
        return self
    }


    ///
    /// Returns the line dash pattern.
    ///
    /// - Returns: the line dash pattern.
    ///
    public func getLinePattern() -> String {
        return self.linePattern
    }


    ///
    /// Sets this point as the start of a path that will be drawn on the chart.
    ///
    @discardableResult
    public func setDrawPath() -> Point {
        self.drawPath = true
        return self
    }


    ///
    /// Sets the URI for the "click point" action.
    ///
    /// - Parameter uri the URI
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> Point {
        self.uri = uri
        return self
    }


    ///
    /// Returns the URI for the "click point" action.
    ///
    /// - Returns: the URI for the "click point" action.
    ///
    public func getURIAction() -> String? {
        return self.uri
    }


    ///
    /// Sets the point text.
    ///
    /// - Parameter text the text.
    ///
    @discardableResult
    public func setText(_ text: String) -> Point {
        self.text = text
        return self
    }


    ///
    /// Returns the text associated with this point.
    ///
    /// - Returns: the text.
    ///
    public func getText() -> String? {
        return self.text
    }


    ///
    /// Sets the point's text color.
    ///
    /// - Parameter textColor the text color.
    ///
    @discardableResult
    public func setTextColor(_ textColor: Int32) -> Point {
        self.textColor = textColor
        return self
    }


    ///
    /// Returns the point's text color.
    ///
    /// - Returns: the text color.
    ///
    public func getTextColor() -> Int32 {
        return self.textColor
    }


    ///
    /// Sets the point's text direction.
    ///
    /// - Parameter textDirection the text direction.
    ///
    @discardableResult
    public func setTextDirection(_ textDirection: Int) -> Point {
        self.textDirection = textDirection
        return self
    }


    ///
    /// Returns the point's text direction.
    ///
    /// - Returns: the text direction.
    ///
    public func getTextDirection() -> Int {
        return self.textDirection
    }


    ///
    /// Sets the point alignment inside table cell.
    ///
    /// - Parameter align the alignment value.
    ///
    @discardableResult
    public func setAlignment(_ align: UInt32) -> Point {
        self.align = align
        return self
    }


    ///
    /// Returns the point alignment.
    ///
    /// - Returns: align the alignment value.
    ///
    public func getAlignment() -> UInt32 {
        return self.align
    }


    ///
    /// Places this point in the specified box at position (0f, 0f).
    ///
    /// - Parameter box the specified box.
    ///
    @discardableResult
    public func placeIn(_ box: Box) -> Point {
        placeIn(box, 0.0, 0.0)
        return self
    }


    ///
    /// Places this point in the specified box.
    ///
    /// - Parameter box the specified box.
    /// - Parameter xOffset the x offset from the top left corner of the box.
    /// - Parameter yOffset the y offset from the top left corner of the box.
    ///
    @discardableResult
    public func placeIn(
            _ box: Box,
            _ xOffset: Float,
            _ yOffset: Float) -> Point {
        self.xBox = box.x + xOffset
        self.yBox = box.y + yOffset
        return self
    }


    ///
    /// Draws this point on the specified page.
    ///
    /// - Parameter page the page to draw this point on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    /// @throws Exception
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.setPenWidth(lineWidth)
        page!.setLinePattern(linePattern)

        if fillShape {
            page!.setBrushColor(color)
        } else {
            page!.setPenColor(color)
        }

        self.x += xBox
        self.y += yBox
        page!.drawPoint(self)
        self.x -= xBox
        self.y -= yBox

        return [self.x + self.xBox + self.r, self.y + self.yBox + self.r]
    }

}   // End of Point.swift
