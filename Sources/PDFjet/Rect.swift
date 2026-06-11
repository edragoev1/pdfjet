/**
 * Rect.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

public class Rect : Drawable {
    private var x: Float = 0.0
    private var y: Float = 0.0
    private var width: Float = 0.0
    private var height: Float = 0.0
    private var r: Float = 0.0

    private var fillColor: [Float]?
    private var borderColor: [Float]?
    private var borderWidth: Float = 0.5
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
    public func setLocation(_ x: Float, _ y: Float) -> Rect {
        self.x = x
        self.y = y
        return self
    }

    /**
     * Sets the position of this rect on the page. Required by the Drawable interface.
     * - Parameters:
     *   - x: the x coordinate of the top left corner of this rect when drawn on the page.
     *   - y: the y coordinate of the top left corner of this rect when drawn on the page.
     * - Returns: this Rect.
     */
    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    /**
     * Sets the size of this rect.
     * - Parameters:
     *   - w: the width of this rect.
     *   - h: the height of this rect.
     */
    public func setSize(_ w: Float, _ h: Float) {
        self.width = w
        self.height = h
    }

    public func setFillColor(_ fillColor: [Float]?) {
        self.fillColor = fillColor
    }

    public func setBorderColor(_ color: Int32) {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        self.borderColor = [r, g, b]
    }

    public func setBorderColor(_ borderColor: [Float]?) {
        self.borderColor = borderColor!
    }

    public func setBorderWidth(_ borderWidth: Float) {
        self.borderWidth = borderWidth
    }

    /**
     * Sets the width of this line.
     * - Parameter width: the width.
     */
    public func setLineWidth(_ borderWidth: Float) {
        self.borderWidth = borderWidth
    }

    /**
     * Sets the corner radius.
     * - Parameter r: the radius.
     */
    public func setCornerRadius(_ r: Float) {
        self.r = r
    }

    /**
     * Sets the URI for the "click rect" action.
     * - Parameter uri: the URI
     */
    public func setURIAction(_ uri: String) {
        self.uri = uri
    }

    /**
     * Sets the destination key for the action.
     * - Parameter key: the destination name.
     */
    public func setGoToAction(_ key: String) {
        self.key = key
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

//     /**
//      * Sets the type of the structure.
//      * - Parameter structureType: the structure type.
//      * - Returns: this Rect.
//      */
//     @discardableResult
//     public func setStructureType(_ structureType: String) -> Rect {
//         self.structureType = structureType
//         return self
//     }

    public func setBorderPattern(_ borderPattern: String) {
        self.borderPattern = borderPattern
    }

    /**
     * Places this rect in the another rect.
     * - Parameters:
     * - rect: the other rect.
     * - xOffset: the x offset from the top left corner of the rect.
     * - yOffset: the y offset from the top left corner of the rect.
     */
    public func placeIn(rect: Rect, xOffset: Float, yOffset: Float) {
        self.x = rect.x + xOffset
        self.y = rect.y + yOffset
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
        let k: Float = 0.5517

//         page!.addBMC(
//                 self.structureType,
//                 self.language,
//                 self.actualText,
//                 self.altDescription)

        if self.r == 0.0 {
            if fillColor != nil {
                page!.setBrushColor(self.fillColor)
                page!.moveTo(self.x, self.y)
                page!.lineTo(self.x + self.width, self.y)
                page!.lineTo(self.x + self.width, self.y + self.height)
                page!.lineTo(self.x, self.y + self.height)
                page!.lineTo(self.x, self.y)
                page!.fillPath()
            }
            if borderColor != nil {
                page!.moveTo(self.x, self.y)
                page!.lineTo(self.x + self.width, self.y)
                page!.lineTo(self.x + self.width, self.y + self.height)
                page!.lineTo(self.x, self.y + self.height)
                page!.setPenColor(self.borderColor)
                page!.setPenWidth(self.borderWidth)
                // page!.setStrokeDashPattern(self.borderPattern) // TODO
                page!.closePath()
            }
        } else {
            page!.setPenWidth(self.borderWidth)
            page!.setPenColor(self.borderColor)
            page!.setStrokeDashPattern(self.borderPattern)

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

            page!.drawPath(points, PathOperator.stroke)
        }
//        page!.addEMC()

//         if self.uri != nil || self.key != nil {
//             page!.addAnnotation(Annotation(
//                 self.uri,
//                 self.key,
//                 self.x,
//                 self.y,
//                 self.x + self.w,
//                 self.y + self.h,
//                 self.language,
//                 self.actualText,
//                 self.altDescription))
//         }

        return [self.x + self.width, self.y + self.height]
    }
}
