import Foundation

class BaseAnnotation: Drawable {
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

    init() {
    }

    func setLocation(_ x: Float, _ y: Float) {
        self.point1 = [x, y]
    }

    func setPosition(_ x: Float, _ y: Float) {
        self.point1 = [x, y]
    }

    func setSize(_ width: Float, _ height: Float) {
        self.point2 = [point1[0] + width, point1[1] + height]
    }

    func setFillColor(_ color: [Float]) {
        self.fillColor = color
    }

    func setFillColor(_ color: Int) {
        let r = Float((color >> 16) & 0xff) / 255.0
        let g = Float((color >> 8) & 0xff) / 255.0
        let b = Float(color & 0xff) / 255.0
        setFillColor([r, g, b])
    }

    func setTransparency(_ transparency: Float) {
        self.transparency = transparency
    }

    func setTitle(_ title: String?) {
        self.title = title
    }

    func setContents(_ contents: String?) {
        self.contents = contents
    }

    func rotate(_ degrees: Double) {
        guard let container = container else { return }

        let center = container.getRotationCenter()
        if let parent = container.parent {
            var mutableCenter = center
            mutableCenter[0] += parent.x
            mutableCenter[1] += parent.y
            
            point1 = Container.rotateAroundCenter(point1, mutableCenter, Float(degrees))
            point2 = Container.rotateAroundCenter(point2, mutableCenter, Float(degrees))
            
            if annotationType == Annotation.Polygon {
                // Rotate polygon vertices in pairs (x, y)
                for i in stride(from: 0, to: vertices?.count ?? 0, by: 2) {
                    guard i + 1 < (vertices?.count ?? 0) else { break }
                    
                    let point: [Float] = [vertices![i], vertices![i + 1]]
                    let rotatedPoint = Container.rotateAroundCenter(point, [0, 0], Float(degrees))
                    vertices?[i] = rotatedPoint[0]
                    vertices?[i + 1] = rotatedPoint[1]
                }
            }
        } else {
            point1 = Container.rotateAroundCenter(point1, center, Float(degrees))
            point2 = Container.rotateAroundCenter(point2, center, Float(degrees))
            
            if annotationType == Annotation.Polygon {
                for i in stride(from: 0, to: vertices?.count ?? 0, by: 2) {
                    guard i + 1 < (vertices?.count ?? 0) else { break }
                    
                    let point: [Float] = [vertices![i], vertices![i + 1]]
                    let rotatedPoint = Container.rotateAroundCenter(point, [0, 0], Float(degrees))
                    vertices?[i] = rotatedPoint[0]
                    vertices?[i + 1] = rotatedPoint[1]
                }
            }
        }
    }

    func drawOn(_ page: Page?) -> [Float] {
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
