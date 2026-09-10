import Foundation

public class PolygonAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Polygon
    }

    @discardableResult
    public func setVertices(_ vertices: [Float]) -> PolygonAnnotation {
        super.vertices = vertices
        return self
    }
}
