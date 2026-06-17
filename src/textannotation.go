package pdfjet

// TextAnnotation extends BaseAnnotation to represent a text label.
type TextAnnotation struct {
	BaseAnnotation
}

// NewTextAnnotation acts as the constructor.
func NewTextAnnotation() *TextAnnotation {
	t := &TextAnnotation{}
	t.BaseAnnotation.annotationType = AnnotationText
	return t
}
