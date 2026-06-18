import Foundation

/// Represents a drawable container that can hold and transform child elements.
///
/// The container maintains its own position, dimensions, rotation, and scaling factors,
/// and can draw its child elements onto a PDF `Page`.
public class Container: Drawable {
    public var x: Float
    public var y: Float
    public var width: Float
    public var height: Float
    public var rotateDegrees: Float
    public var scaleX: Float
    public var scaleY: Float
    private var elements: [Drawable]
    public var parent: Container?

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

    /// Sets the position of the container on the page.
    ///
    /// - Parameters:
    ///   - x: The X coordinate.
    ///   - y: The Y coordinate.
    public func setPosition(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }

    /// Sets the location of the container on the page (alias for `setPosition`).
    ///
    /// - Parameters:
    ///   - x: The X coordinate.
    ///   - y: The Y coordinate.
    public func setLocation(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }

    /// Sets the rotation angle.
    ///
    /// - Parameter degrees: The rotation angle in degrees.
    public func setRotation(_ degrees: Double) {
        self.rotateDegrees = Float(degrees)
    }

    /// Sets clockwise rotation.
    ///
    /// - Parameter degrees: The rotation angle in degrees (clockwise).
    public func setRotationClockwise(_ degrees: Double) {
        self.rotateDegrees = Float(-degrees)
    }

    /// Sets counter-clockwise rotation.
    ///
    /// - Parameter degrees: The rotation angle in degrees (counter-clockwise).
    public func setRotationCounterClockwise(_ degrees: Double) {
        self.rotateDegrees = Float(degrees)
    }

    public func getRotationCenter() -> [Float] {
        return [self.x + self.width/2.0, self.y + self.height/2.0]
    }

    /// Sets a uniform scaling factor for both X and Y axes.
    ///
    /// - Parameter factor: The scaling factor to apply.
    public func setScaleFactor(_ factor: Float) {
        setScaleFactorXY(factor, factor)
    }

    /// Sets non-uniform scaling factors for the X and Y axes.
    ///
    /// - Parameters:
    ///   - sx: The scaling factor for X.
    ///   - sy: The scaling factor for Y.
    public func setScaleFactorXY(_ sx: Float, _ sy: Float) {
        self.scaleX = sx
        self.scaleY = sy
    }

    public func setBorderColor(_ borderColor: Int32) {
        let rect = Rect(0.0, 0.0, width, height)
        rect.setBorderColor(borderColor)
        self.add(rect)
    }

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
    static func rotateAroundCenter(_ point: [Float], _ center: [Float], _ degrees: Float) -> [Float] {
        // Convert degrees to radians
        let rad = Double(degrees) * .pi / 180.0

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
        page!.append("q\n") // Save the graphics state

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
            element.drawOn(page)
        }

        page!.append("Q\n") // Restore the graphics state

        return [self.x + width, self.y + height]
    }
}
