import Foundation

/// A square annotation.
public class SquareAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Square
    }
}
