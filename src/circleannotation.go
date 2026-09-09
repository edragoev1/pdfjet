package pdfjet

// CircleAnnotation extends BaseAnnotation to represent a circle.
type CircleAnnotation struct {
	BaseAnnotation
}

// NewCircleAnnotation acts as the constructor.
func NewCircleAnnotation() *CircleAnnotation {
	c := &CircleAnnotation{}
	c.BaseAnnotation.annotationType = AnnotationCircle
	return c
}
