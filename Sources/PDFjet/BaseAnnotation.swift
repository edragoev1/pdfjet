import Foundation

public class BaseAnnotation: Drawable {
    var annotationType: String?
    var point1: [Float] = [0, 0]
    var point2: [Float] = [0, 0]
    var vertices: [Float]?
    var fillColor: [Float] = [0.5, 0.5, 0.5]
    var transparency: Float = 1.0
    var title: String?
    var contents: String?
    var uri: String?
    var key: String?
    var language: String?
    var actualText: String?
    var altDescription: String?
    weak var container: Container?

    public init() {
    }

    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.point1 = [x, y]
        return self
    }

    @discardableResult
    public func setSize(_ width: Float, _ height: Float) -> BaseAnnotation {
        self.point2 = [point1[0] + width, point1[1] + height]
        return self
    }

    @discardableResult
    public func setFillColor(_ color: [Float]) -> BaseAnnotation {
        self.fillColor = color
        return self
    }

    @discardableResult
    public func setFillColor(_ color: Int32) -> BaseAnnotation {
        let r = Float((color >> 16) & 0xff) / 255.0
        let g = Float((color >> 8) & 0xff) / 255.0
        let b = Float(color & 0xff) / 255.0
        setFillColor([r, g, b])
        return self
    }

    @discardableResult
    public func setTransparency(_ transparency: Float) -> BaseAnnotation {
        self.transparency = transparency
        return self
    }

    @discardableResult
    public func setTitle(_ title: String?) -> BaseAnnotation {
        self.title = title
        return self
    }

    @discardableResult
    public func setContents(_ contents: String?) -> BaseAnnotation {
        self.contents = contents
        return self
    }

    public func rotate(_ degrees: Double) {
        if container == nil { return }
        var center = container!.getRotationCenter()
        if container!.parent != nil {
            center[0] += container!.parent!.x
            center[1] += container!.parent!.y
        }
        point1 = Container.rotateAroundCenter(point1, center, degrees)
        point2 = Container.rotateAroundCenter(point2, center, degrees)
        if annotationType == Annotation.Polygon {
            var i = 0
            while i < vertices!.count {
                let point = Container.rotateAroundCenter(
                    [vertices![i], vertices![i + 1]], [0.0, 0.0], degrees)
                vertices![i] = point[0]
                vertices![i + 1] = point[1]
                i += 2
            }
        }
    }

    public func drawOn(_ page: Page?) -> [Float] {
        page!.addAnnotation(Annotation(
            annotationType,
            point1[0],
            point1[1],
            point2[0],
            point2[1],
            vertices,
            fillColor,
            transparency,
            title,
            contents,
            uri,
            key,
            language,
            actualText,
            altDescription))
        return point2
    }
}
