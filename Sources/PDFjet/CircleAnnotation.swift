import Foundation

/// A circle annotation.
public class CircleAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Circle
    }
}
