import Foundation

/// Represents a drawable container that can hold and transform child elements.
///
/// The container maintains its own position, dimensions, rotation, and scaling factors,
/// and can draw its child elements onto a PDF `Page`.
public class Container: Drawable {
    /// The x coordinate of this container on the page.
    public var x: Float
    /// The y coordinate of this container on the page.
    public var y: Float
    /// The width of this container.
    public var width: Float
    /// The height of this container.
    public var height: Float
    /// The rotation angle in degrees.
    public var rotateDegrees: Float
    /// The horizontal scale factor.
    public var scaleX: Float
    /// The vertical scale factor.
    public var scaleY: Float
    private var elements: [Drawable]
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

    /// Sets the rotation angle of this container.
    ///
    /// - Parameter degrees: The rotation angle in degrees.
    public func rotate(_ degrees: Double) {
        self.rotateDegrees = Float(degrees)
    }

    /// Sets the rotation angle.
    ///
    /// - Parameter degrees: The rotation angle in degrees.
    @discardableResult
    public func setRotation(_ degrees: Double) -> Container {
        self.rotateDegrees = Float(degrees)
        return self
    }

    /// Sets clockwise rotation.
    ///
    /// - Parameter degrees: The rotation angle in degrees (clockwise).
    @discardableResult
    public func setRotationClockwise(_ degrees: Double) -> Container {
        self.rotateDegrees = Float(-degrees)
        return self
    }

    /// Sets counter-clockwise rotation.
    ///
    /// - Parameter degrees: The rotation angle in degrees (counter-clockwise).
    @discardableResult
    public func setRotationCounterClockwise(_ degrees: Double) -> Container {
        self.rotateDegrees = Float(degrees)
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
    public func setScaleFactor(_ factor: Float) -> Container {
        setScaleFactorXY(factor, factor)
        return self
    }

    /// Sets non-uniform scaling factors for the X and Y axes.
    ///
    /// - Parameters:
    ///   - sx: The scaling factor for X.
    ///   - sy: The scaling factor for Y.
    @discardableResult
    public func setScaleFactorXY(_ sx: Float, _ sy: Float) -> Container {
        self.scaleX = sx
        self.scaleY = sy
        return self
    }

    /// Adds a border in the specified 0xRRGGBB color around this container.
    @discardableResult
    public func setBorderColor(_ borderColor: Int32) -> Container {
        let rect = Rect(0.0, 0.0, width, height)
        rect.setBorderColor(borderColor)
        self.add(rect)
        return self
    }

    /// Adds a black border around this container.
    public func addBorder() {
        let rect = Rect(0.0, 0.0, width, height)
        rect.setBorderColor(Color.black)
        self.add(rect)
    }

    /// Adds a drawable element to this container.
    ///
    /// - Parameter element: The element to add.
    public func add(_ element: Drawable) {
        self.elements.append(element)
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
        page!.append(FastFloat.toByteArray(cosVal))
        page!.append(Token.space)
        page!.append(FastFloat.toByteArray(sinVal))
        page!.append(Token.space)
        page!.append(FastFloat.toByteArray(-sinVal))
        page!.append(Token.space)
        page!.append(FastFloat.toByteArray(cosVal))
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
