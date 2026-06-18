import Foundation

public class PolygonAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Polygon
    }
    
    public func setVertices(_ vertices: [Float]) {
        super.vertices = vertices
    }
}
