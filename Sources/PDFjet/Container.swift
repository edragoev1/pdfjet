import Foundation

/// A group of drawable elements that are moved, rotated and scaled together:
/// shapes, text, images, annotations and nested containers. The elements are
/// drawn into the page on every drawOn.
///
/// Use a Container to lay out a group once and place it on a page, or to
/// rotate and scale elements that have no rotation of their own. Use a Stamp
/// for content that repeats on many pages, like a header, a footer or a
/// watermark: it is written once as a form XObject and each placement is a
/// single operator. Please see Example_06 and Example_35.
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
    var parent: Container?

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
    @discardableResult
    public func setBorderColor(_ borderColor: Int32) -> Container {
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
        if let container = element as? Container {
            container.parent = self
        }
        self.elements.append(element)
        return self
    }

    /// Rotates a point around a center by a specified number of degrees.
    /// - Parameters:
    ///   - point: The point to rotate as [x, y].
    ///   - center: The center point of rotation as [x, y].
    ///   - degrees: The angle of rotation in degrees.
    /// - Returns: A new array [x, y] representing the rotated point.
    static func rotateAroundCenter(_ point: [Float], _ center: [Float], _ degrees: Double) -> [Float] {
        // Convert degrees to radians
        let rad = degrees * .pi / 180.0

        // Translate point to origin (relative to center)
        let dx = Double(point[0]) - Double(center[0])
        let dy = Double(point[1]) - Double(center[1])

        // Calculate cosine and sine
        let cosValue = cos(rad)
        let sinValue = sin(rad)

        // Apply rotation matrix
        let dxRot = dx * cosValue - dy * sinValue
        let dyRot = dx * sinValue + dy * cosValue

        // Translate back to original coordinate system
        let nx = Double(center[0]) + dxRot
        let ny = Double(center[1]) + dyRot

        // Return as Float array (matching float[] return type in Java)
        return [Float(nx), Float(ny)]
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

        let rad = rotateDegrees * Float.pi / 180.0
        let cosVal = cos(rad)
        let sinVal = sin(rad)
        page!.append(cosVal)
        page!.append(Token.space)
        page!.append(sinVal)
        page!.append(Token.space)
        page!.append(-sinVal)
        page!.append(Token.space)
        page!.append(cosVal)
        page!.append(" 0 0 cm\n")

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

        for element in elements {
            if let annot = element as? BaseAnnotation {
                annot.container = self
                annot.point1[0] += x
                annot.point1[1] += y
                annot.point2[0] += x
                annot.point2[1] += y
                if let parent = parent {
                    annot.point1[0] += parent.x
                    annot.point1[1] += parent.y
                    annot.point2[0] += parent.x
                    annot.point2[1] += parent.y
                }
                annot.rotate(Double(-rotateDegrees))
            }
            element.drawOn(page)
        }

        page!.restoreGraphicsState()

        return [self.x + width, self.y + height]
    }
}
