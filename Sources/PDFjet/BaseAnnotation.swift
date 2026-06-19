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

    public func setLocation(_ x: Float, _ y: Float) {
        self.point1 = [x, y]
    }

    public func setPosition(_ x: Float, _ y: Float) {
        self.point1 = [x, y]
    }

    public func setSize(_ width: Float, _ height: Float) {
        self.point2 = [point1[0] + width, point1[1] + height]
    }

    public func setFillColor(_ color: [Float]) {
        self.fillColor = color
    }

    public func setFillColor(_ color: Int32) {
        let r = Float((color >> 16) & 0xff) / 255.0
        let g = Float((color >> 8) & 0xff) / 255.0
        let b = Float(color & 0xff) / 255.0
        setFillColor([r, g, b])
    }

    public func setTransparency(_ transparency: Float) {
        self.transparency = transparency
    }

    public func setTitle(_ title: String?) {
        self.title = title
    }

    public func setContents(_ contents: String?) {
        self.contents = contents
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
        let annotation = Annotation(
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
            altDescription
        )

        page!.addAnnotation(annotation)
        return point2
    }
}
