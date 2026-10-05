/**
 * Container.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A group of drawable elements that are moved, rotated and scaled together:
/// shapes, text, images, annotations, stamps and nested containers.
///
/// What a container is good at: it takes anything that implements Drawable,
/// so a group can hold an Image, a Table, a chart, a barcode, a link or
/// another annotation, and each element keeps everything it can do. Each
/// element also tags itself in a PDF/UA document, so a TextLine in a container
/// is a paragraph of its own and an Image is a figure with its alternate
/// description. Laying a group out once and placing it, rotated or scaled, is
/// what it is for; elements that have no rotation of their own get one this
/// way.
///
/// What it costs: the elements are drawn into the page on every drawOn, so a
/// container on 100 pages writes its drawing 100 times over.
///
/// Use a Stamp instead for content that repeats on many pages -- a header, a
/// footer, a logo, a watermark -- which is written once as a form XObject and
/// placed with a few bytes. Of a 200 by 50 point box with two lines of text, a
/// container writes 367 bytes into every page and a stamp 83, so the stamp is
/// the smaller of the two from the fourth page on: over 100 pages it saves
/// 12,855 bytes of a 104,530 byte file, and over 500 pages 66,052 of 275,553.
/// On one page the stamp is the larger, since its XObject costs about 300
/// bytes of its own, and it draws only paths and text in an embedded font.
///
/// Please see Example_06 and Example_35.
public class Container: Drawable {
    var x: Float
    var y: Float
    var width: Float
    var height: Float
    var rotateDegrees: Float
    var scaleX: Float
    var scaleY: Float
    private var elements: [Drawable]
    private var border: Rect?
    /// The container that holds this container, or nil.

    /// Creates a new container with the specified width and height.
    ///
    /// The container is initialized with:
    /// - Rotation set to `0` degrees
    /// - Scaling factors set to `1.0` for both axes
    /// - An empty list of drawable elements
    ///
    /// - Parameters:
    ///   - width: The width of the container.
    ///   - height: The height of the container.
    public init(_ width: Float, _ height: Float) {
        self.width = width
        self.height = height
        self.rotateDegrees = 0.0
        self.scaleX = 1.0
        self.scaleY = 1.0
        self.x = 0.0
        self.y = 0.0
        self.elements = [Drawable]()
    }

    /// Sets the location of the container on the page.
    ///
    /// - Parameters:
    ///   - x: The X coordinate.
    ///   - y: The Y coordinate.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Rotates this container around its center: clockwise for a positive angle, as every rotation
    /// in PDFjet turns, and counterclockwise for a negative angle.
    ///
    /// - Parameter degrees: The rotation angle in degrees.
    @discardableResult
    public func setRotation(_ degrees: Double) -> Container {
        // The rotation of the page turns counterclockwise.
        self.rotateDegrees = Float(-degrees)
        return self
    }

    /// Returns the center of this container, which it rotates around.
    public func getRotationCenter() -> [Float] {
        return [self.x + self.width/2.0, self.y + self.height/2.0]
    }

    /// Sets a uniform scaling factor for both X and Y axes.
    ///
    /// - Parameter factor: The scaling factor to apply.
    @discardableResult
    public func scaleBy(_ factor: Float) -> Container {
        scaleBy(factor, factor)
        return self
    }

    /// Sets non-uniform scaling factors for the X and Y axes.
    ///
    /// - Parameters:
    ///   - sx: The scaling factor for X.
    ///   - sy: The scaling factor for Y.
    @discardableResult
    public func scaleBy(_ sx: Float, _ sy: Float) -> Container {
        self.scaleX = sx
        self.scaleY = sy
        return self
    }

    /// Sets the 0xRRGGBB color of the border around this container.
    /// Color.transparent leaves it unchanged.
    @discardableResult
    public func setBorderColor(_ borderColor: Int32) -> Container {
        if borderColor == Color.transparent {
            return self
        }
        if border == nil {
            border = Rect(0.0, 0.0, width, height)
            self.add(border!)
        }
        border!.setBorderColor(borderColor)
        return self
    }

    /// Sets the color of the border around this container from an array of red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setBorderColor(_ rgbColor: [Float]) -> Container {
        if border == nil {
            border = Rect(0.0, 0.0, width, height)
            self.add(border!)
        }
        border!.setBorderColor(rgbColor)
        return self
    }

    /// Adds a drawable element to this container.
    ///
    /// - Parameter element: The element to add.
    @discardableResult
    public func add(_ element: Drawable) -> Container {
        self.elements.append(element)
        return self
    }

    /// Draws this container and its child elements onto the page.
    ///
    /// - Parameter page: The `Page` to draw on.
    /// - Returns: An array containing the bottom-right position of the container.
    public func drawOn(_ page: Page?) -> [Float] {
        if page == nil || scaleX == 0.0 || scaleY == 0.0 {
            return [self.x + width, self.y + height]    // Measured, or nothing to paint.
        }
        page!.saveGraphicsState()

        page!.append("1 0 0 1 ")
        page!.append(self.x)
        page!.append(Token.space)
        page!.append(-self.y)
        page!.append(" cm\n")

        let cx = width / 2.0
        let cy = height / 2.0

        page!.append("1 0 0 1 ")
        page!.append(cx)
        page!.append(Token.space)
        page!.append(page!.height - cy)
        page!.append(" cm\n")

        // The trigonometry is done in double precision, as in the other ports
        let rad = Double(rotateDegrees) * (Double.pi / 180.0)
        let cosVal = Float(cos(rad))
        let sinVal = Float(sin(rad))
        page!.appendRotation(cosVal, sinVal)

        page!.append(scaleX)
        page!.append(Token.space)
        page!.append("0")
        page!.append(Token.space)
        page!.append("0")
        page!.append(Token.space)
        page!.append(scaleY)
        page!.append(Token.space)
        page!.append("0")
        page!.append(Token.space)
        page!.append("0")
        page!.append(" cm\n")

        page!.append("1 0 0 1 ")
        page!.append(-cx)
        page!.append(Token.space)
        page!.append(-(page!.height - cy))
        page!.append(" cm\n")

        // What the page does not move with the cm operators above, the
        // rectangles of the links and of the annotations and the bounding
        // boxes of the figures, it moves with the same transform, from the top
        // left corner of the page: turned and scaled around the center of the
        // container, and moved to its location.
        let m0 = scaleX * cosVal
        let m1 = -scaleX * sinVal
        let m2 = scaleY * sinVal
        let m3 = scaleY * cosVal
        let centerX = self.x + cx
        let centerY = self.y + cy
        let m4 = centerX - m0*cx - m2*cy
        let m5 = centerY - m1*cx - m3*cy
        let saved = page!.pushTransform([m0, m1, m2, m3, m4, m5])
        for element in elements {
            element.drawOn(page)
        }
        page!.popTransform(saved)

        page!.restoreGraphicsState()

        return [self.x + width, self.y + height]
    }
}
