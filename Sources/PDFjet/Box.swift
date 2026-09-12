/**
 * Box.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Used to create rectangular boxes on a page.
 */
public class Box : Drawable {
    var x: Float = 0.0
    var y: Float = 0.0

    var uri: String?
    var key: String?

    private var w: Float = 0.0
    private var h: Float = 0.0
    private var r: Float = 0.0

    private var color = Color.black

    private var width: Float = 0.0
    private var pattern: String = "[] 0"
    private var fillShape: Bool = false

    private var language: String?
    private var actualText: String = Single.space
    private var altDescription: String = Single.space

    /// Creates a box.
    public init() {
    }

    /**
     * Creates a box object.
     *
     * - Parameter x: the x coordinate of the top left corner of this box when drawn on the page.
     * - Parameter y: the y coordinate of the top left corner of this box when drawn on the page.
     * - Parameter w: the width of this box.
     * - Parameter h: the height of this box.
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

    /**
     * Sets the location of this box on the page.
     *
     * - Parameter x: the x coordinate of the top left corner of this box when drawn on the page.
     * - Parameter y: the y coordinate of the top left corner of this box when drawn on the page.
     */
    @discardableResult
    public func setLocation(
            _ x: Float,
            _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /**
     * Sets the size of this box.
     *
     * - Parameter w: the width of this box.
     * - Parameter h: the height of this box.
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
     * Sets the color for this box.
     *
     * - Parameter color: the color specified as an integer.
     */
    @discardableResult
    public func setColor(_ color: Int32) -> Box {
        self.color = color
        return self
    }

    /**
     * Sets the width of this line.
     *
     * - Parameter width: the width.
     */
    @discardableResult
    public func setLineWidth(_ width: Float) -> Box {
        self.width = width
        return self
    }

    /**
     * Sets the corner radius of this box.
     *
     * - Parameter r: the radius of the rounded corners.
     */
    @discardableResult
    public func setCornerRadius(_ r: Float) -> Box {
        self.r = r
        return self
    }

    /**
     * Sets the URI for the "click box" action.
     *
     * - Parameter uri: the URI
     */
    @discardableResult
    public func setURIAction(_ uri: String) -> Box {
        self.uri = uri
        return self
    }

    /**
     * Sets the destination key for the action.
     *
     * - Parameter key: the destination name.
     */
    @discardableResult
    public func setGoToAction(_ key: String) -> Box {
        self.key = key
        return self
    }

    /**
     * Sets the alternate description of this box.
     *
     * - Parameter altDescription: the alternate description of the box.
     * - Returns: this Box.
     */
    @discardableResult
    public func setAltDescription(
            _ altDescription: String) -> Box {
        self.altDescription = altDescription
        return self
    }

    /**
     * Sets the actual text for this box.
     *
     * - Parameter actualText: the actual text for the box.
     * - Returns: this Box.
     */
    @discardableResult
    public func setActualText(
            _ actualText: String) -> Box {
        self.actualText = actualText
        return self
    }

    /**
     * The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
     * It is specified by a dash array and a dash phase.
     * The elements of the dash array are positive numbers that specify the lengths of
     * alternating dashes and gaps.
     * The dash phase specifies the distance into the dash pattern at which to start the dash.
     * The elements of both the dash array and the dash phase are expressed in user space units.
     * Examples of line dash patterns:
     *
     * ```
     *     "[Array] Phase"     Appearance          Description
     *     _______________     _________________   ____________________________________
     *
     *     "[] 0"              -----------------   Solid line
     *     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     * ```
     *
     * - Parameter pattern: the line dash pattern.
     */
    @discardableResult
    public func setPattern(
            _ pattern: String) -> Box {
        self.pattern = pattern
        return self
    }

    /**
     * Sets the private fillShape variable.
     * If the value of fillShape is true - the box is filled with the current brush color.
     *
     * - Parameter fillShape: the value used to set the private fillShape variable.
     */
    @discardableResult
    public func setFillShape(
            _ fillShape: Bool) -> Box {
        self.fillShape = fillShape
        return self
    }

    /**
     * Scales this box by the specified factor.
     *
     * - Parameter factor: the factor used to scale the box.
     */
    @discardableResult
    public func scaleBy(_ factor: Float) -> Box {
        self.x = self.x * factor
        self.y = self.y * factor
        return self
    }

    /**
     * Draws this box on the specified page.
     *
     * - Parameter page: the page to draw this box on.
     * - Returns: x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        page!.addBMC(StructElem.P, language, actualText, altDescription)
        page!.setPenWidth(width)
        page!.setStrokeDashPattern(pattern)
        if fillShape {
            page!.setBrushColor(color)
        } else {
            page!.setPenColor(color)
        }
        if r == 0.0 {
            page!.moveTo(x, y)
            page!.lineTo(x + w, y)
            page!.lineTo(x + w, y + h)
            page!.lineTo(x, y + h)
            if fillShape {
                page!.fillPath()
            } else {
                page!.closePath()
            }
        } else {
            let k: Float = 0.55228
            var points: [Point] = []
            points.append(Point(x + r, y))
            points.append(Point((x + w) - r, y))
            points.append(Point((x + w - r) + r * k, y, Point.controlPointC))
            points.append(Point(x + w, (y + r) - r * k, Point.controlPointC))
            points.append(Point(x + w, y + r))
            points.append(Point(x + w, (y + h) - r))
            points.append(Point(x + w, ((y + h) - r) + r * k, Point.controlPointC))
            points.append(Point(((x + w) - r) + r * k, y + h, Point.controlPointC))
            points.append(Point((x + w) - r, y + h))
            points.append(Point(x + r, y + h))
            points.append(Point((x + r) - r * k, y + h, Point.controlPointC))
            points.append(Point(x, ((y + h) - r) + r * k, Point.controlPointC))
            points.append(Point(x, (y + h) - r))
            points.append(Point(x, y + r))
            points.append(Point(x, (y + r) - r * k, Point.controlPointC))
            points.append(Point((x + r) - r * k, y, Point.controlPointC))
            points.append(Point(x + r, y))
            page!.drawPath(points, fillShape ? PathOperator.fill : PathOperator.stroke)
        }
        page!.addEMC()

        if uri != nil || key != nil {
            page!.addAnnotation(Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w,
                    y + h,
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Transparency
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription))
        }

        return [x + w, y + h]
    }
}   // End of Box.swift
