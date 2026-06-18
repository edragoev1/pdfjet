import Foundation

public class TextAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Text
    }
}
