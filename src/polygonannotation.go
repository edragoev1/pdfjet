package pdfjet

type AnnotationType string

// PolygonAnnotation extends BaseAnnotation to represent a polygon.
type PolygonAnnotation struct {
	BaseAnnotation
}

// NewPolygonAnnotation acts as the constructor.
func NewPolygonAnnotation() *PolygonAnnotation {
	p := &PolygonAnnotation{}
	p.BaseAnnotation.annotationType = AnnotationPolygon
	return p
}

// SetVertices sets the vertices for the polygon.
func (p *PolygonAnnotation) SetVertices(vertices []float32) {
	p.BaseAnnotation.vertices = vertices
}
