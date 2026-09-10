import Foundation

/// A polygon annotation.
public class PolygonAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Polygon
    }

    /// Sets the vertices of the polygon as pairs of x and y coordinates.
    @discardableResult
    public func setVertices(_ vertices: [Float]) -> PolygonAnnotation {
        super.vertices = vertices
        return self
    }
}
