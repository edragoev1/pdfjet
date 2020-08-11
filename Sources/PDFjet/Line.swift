/**
 *  Line.swift
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
/// Used to create line objects.
///
/// Please see Example_01.
///
public class Line : Drawable {

    private var x1: Float = 0.0
    private var y1: Float = 0.0
    private var x2: Float = 0.0
    private var y2: Float = 0.0

    private var xBox: Float = 0.0
    private var yBox: Float = 0.0

    private var color = Color.black
    private var width: Float = 0.3
    private var pattern: String = "[] 0"
    private var capStyle: Int = 0

    private var language: String?
    private var altDescription: String = Single.space
    private var actualText: String = Single.space


    ///
    /// The default constructor.
    ///
    public init() {
    }


    ///
    /// Create a line object.
    ///
    /// @param x1 the x coordinate of the start point.
    /// @param y1 the y coordinate of the start point.
    /// @param x2 the x coordinate of the end point.
    /// @param y2 the y coordinate of the end point.
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
    /// @param pattern the line dash pattern.
    /// @return this Line object.
    ///
    @discardableResult
    public func setPattern(_ pattern: String) -> Line {
        self.pattern = pattern
        return self
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setStartPoint(x, y)
    }


    ///
    /// Sets the x and y coordinates of the start point.
    ///
    /// @param x the x coordinate of the start point.
    /// @param y the y coordinate of the start point.
    /// @return this Line object.
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
    /// @param x the x coordinate of the start point.
    /// @param y the y coordinate of the start point.
    /// @return this Line object.
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
    /// @return Point the point.
    ///
    public func getStartPoint() -> Point {
        return Point(x1, y1)
    }


    ///
    /// Sets the x and y coordinates of the end point.
    ///
    /// @param x the x coordinate of the end point.
    /// @param y the t coordinate of the end point.
    /// @return this Line object.
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
    /// @param x the x coordinate of the end point.
    /// @param y the t coordinate of the end point.
    /// @return this Line object.
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
    /// @return Point the point.
    ///
    public func getEndPoint() -> Point {
        return Point(x2, y2)
    }


    ///
    /// Sets the width of this line.
    ///
    /// @param width the width.
    /// @return this Line object.
    ///
    @discardableResult
    public func setWidth(_ width: Float) -> Line {
        self.width = width
        return self
    }


    ///
    /// Sets the color for this line.
    ///
    /// @param color the color specified as an integer.
    /// @return this Line object.
    ///
    @discardableResult
    public func setColor(_ color: UInt32) -> Line {
        self.color = color
        return self
    }


    ///
    /// Sets the line cap style.
    ///
    /// @param style the cap style of the current line.
    /// Supported values: Cap.BUTT, Cap.ROUND and Cap.PROJECTING_SQUARE
    /// @return this Line object.
    ///
    @discardableResult
    public func setCapStyle(_ style: Int) -> Line {
        self.capStyle = style
        return self
    }


    ///
    /// Returns the line cap style.
    ///
    /// @return the cap style.
    ///
    public func getCapStyle() -> Int {
        return self.capStyle
    }


    ///
    /// Sets the alternate description of this line.
    ///
    /// @param altDescription the alternate description of the line.
    /// @return this Line.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Line {
        self.altDescription = altDescription
        return self
    }


    ///
    /// Sets the actual text for this line.
    ///
    /// @param actualText the actual text for the line.
    /// @return this Line.
    ///
    @discardableResult
    public func setActualText(_ actualText: String) -> Line {
        self.actualText = actualText
        return self
    }


    ///
    /// Places this line in the specified box at position (0.0f, 0.0f).
    ///
    /// @param box the specified box.
    /// @return this Line object.
    ///
    @discardableResult
    public func placeIn(_ box: Box) -> Line {
        return placeIn(box, 0.0, 0.0)
    }


    ///
    /// Places this line in the specified box.
    ///
    /// @param box the specified box.
    /// @param xOffset the x offset from the top left corner of the box.
    /// @param yOffset the y offset from the top left corner of the box.
    /// @return this Line object.
    ///
    @discardableResult
    public func placeIn(
            _ box: Box,
            _ xOffset: Float,
            _ yOffset: Float) -> Line {
        self.xBox = box.x + xOffset
        self.yBox = box.y + yOffset
        return self
    }


    ///
    /// Scales this line by the spacified factor.
    ///
    /// @param factor the factor used to scale the line.
    /// @return this Line object.
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
    /// @param page the page to draw this line on.
    /// @return x and y coordinates of the bottom right corner of this component.
    /// @throws Exception
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.setPenColor(color)
        page!.setPenWidth(width)
        page!.setLineCapStyle(capStyle)
        page!.setLinePattern(pattern)
        page!.addBMC(StructElem.SPAN, language, altDescription, actualText)
        page!.drawLine(
                x1 + xBox,
                y1 + yBox,
                x2 + xBox,
                y2 + yBox)
        page!.addEMC()

        let xMax = Float(max(x1 + xBox, x2 + xBox))
        let yMax = Float(max(y1 + yBox, y2 + yBox))
        return [xMax, yMax]
    }

}   // End of Line.swift
