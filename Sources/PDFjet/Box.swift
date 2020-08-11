/**
 *  Box.swift
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


/**
 *  Used to create rectangular boxes on a page.
 *  Also used to for layout purposes. See the placeIn method in the Image and TextLine classes.
 *
 */
public class Box : Drawable {

    var x: Float = 0.0
    var y: Float = 0.0

    var uri: String?
    var key: String?

    private var w: Float = 0.0
    private var h: Float = 0.0

    private var color = Color.black

    private var width: Float = 0.3
    private var pattern: String = "[] 0"
    private var fillShape: Bool = false

    private var language: String?
    private var altDescription: String = Single.space
    private var actualText: String = Single.space


    public init() {
    }


    /**
     *  Creates a box object.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public init(
            _ x: Float,
            _ y: Float,
            _ w: Float,
            _ h: Float) {
        self.x = x
        self.y = y
        self.w = w
        self.h = h
    }


    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }


    /**
     *  Sets the location of this box on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     */
    @discardableResult
    public func setLocation(
            _ x: Float,
            _ y: Float) -> Box {
        self.x = x
        self.y = y
        return self
    }


    /**
     *  Sets the size of this box.
     *
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    @discardableResult
    public func setSize(
            _ w: Float,
            _ h: Float) -> Box {
        self.w = w
        self.h = h
        return self
    }


    /**
     *  Sets the color for this box.
     *
     *  @param color the color specified as an integer.
     */
    @discardableResult
    public func setColor(_ color: UInt32) -> Box {
        self.color = color
        return self
    }


    /**
     *  Sets the width of this line.
     *
     *  @param width the width.
     */
    @discardableResult
    public func setLineWidth(_ width: Float) -> Box {
        self.width = width
        return self
    }


    /**
     *  Sets the URI for the "click box" action.
     *
     *  @param uri the URI
     */
    @discardableResult
    public func setURIAction(_ uri: String) -> Box {
        self.uri = uri
        return self
    }


    /**
     *  Sets the destination key for the action.
     *
     *  @param key the destination name.
     */
    @discardableResult
    public func setGoToAction(_ key: String) -> Box {
        self.key = key
        return self
    }


    /**
     *  Sets the alternate description of this box.
     *
     *  @param altDescription the alternate description of the box.
     *  @return this Box.
     */
    @discardableResult
    public func setAltDescription(
            _ altDescription: String) -> Box {
        self.altDescription = altDescription
        return self
    }


    /**
     *  Sets the actual text for this box.
     *
     *  @param actualText the actual text for the box.
     *  @return this Box.
     */
    @discardableResult
    public func setActualText(
            _ actualText: String) -> Box {
        self.actualText = actualText
        return self
    }


    /**
     *  The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
     *  It is specified by a dash array and a dash phase.
     *  The elements of the dash array are positive numbers that specify the lengths of
     *  alternating dashes and gaps.
     *  The dash phase specifies the distance into the dash pattern at which to start the dash.
     *  The elements of both the dash array and the dash phase are expressed in user space units.
     *  <pre>
     *  Examples of line dash patterns:
     *
     *      "[Array] Phase"     Appearance          Description
     *      _______________     _________________   ____________________________________
     *
     *      "[] 0"              -----------------   Solid line
     *      "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *      "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *      "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *      "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *      "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     *  </pre>
     *
     *  @param pattern the line dash pattern.
     */
    @discardableResult
    public func setPattern(
            _ pattern: String) -> Box {
        self.pattern = pattern
        return self
    }


    /**
     *  Sets the private fillShape variable.
     *  If the value of fillShape is true - the box is filled with the current brush color.
     *
     *  @param fillShape the value used to set the private fillShape variable.
     */
    @discardableResult
    public func setFillShape(
            _ fillShape: Bool) -> Box {
        self.fillShape = fillShape
        return self
    }


    /**
     *  Places this box in the another box.
     *
     *  @param box the other box.
     *  @param xOffset the x offset from the top left corner of the box.
     *  @param yOffset the y offset from the top left corner of the box.
     */
    @discardableResult
    public func placeIn(
            _ box: Box,
            _ xOffset: Float,
            _ yOffset: Float) -> Box {
        self.x = box.x + xOffset
        self.y = box.y + yOffset
        return self
    }


    /**
     *  Scales this box by the spacified factor.
     *
     *  @param factor the factor used to scale the box.
     */
    @discardableResult
    public func scaleBy(_ factor: Float) -> Box {
        self.x = self.x * factor
        self.y = self.y * factor
        return self
    }


    /**
     *  Draws this box on the specified page.
     *
     *  @param page the page to draw this box on.
     *  @return x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.addBMC(StructElem.SPAN, language, altDescription, actualText)
        page!.setPenWidth(width)
        page!.setLinePattern(pattern)
        if fillShape {
            page!.setBrushColor(color)
        }
        else {
            page!.setPenColor(color)
        }
        page!.moveTo(x, y)
        page!.lineTo(x + w, y)
        page!.lineTo(x + w, y + h)
        page!.lineTo(x, y + h)
        if fillShape {
            page!.fillPath()
        }
        else {
            page!.closePath()
        }
        page!.addEMC()

        if uri != nil || key != nil {
            page!.addAnnotation(Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    language,
                    altDescription,
                    actualText))
        }

        return [x + w, y + h]
    }

}   // End of Box.swift
