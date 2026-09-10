import Foundation

/// A text note annotation.
public class TextAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Text
    }
}
