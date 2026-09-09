package pdfjet

// SquareAnnotation extends BaseAnnotation to represent a square.
type SquareAnnotation struct {
	BaseAnnotation
}

// NewSquareAnnotation acts as the constructor.
func NewSquareAnnotation() *SquareAnnotation {
	s := &SquareAnnotation{}
	s.BaseAnnotation.annotationType = AnnotationSquare
	return s
}
