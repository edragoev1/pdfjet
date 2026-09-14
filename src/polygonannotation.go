package pdfjet

// annotationType is the subtype of an annotation, for example annotationPolygon.
type annotationType string

// PolygonAnnotation extends BaseAnnotation to represent a polygon.
type PolygonAnnotation struct {
	BaseAnnotation
}

// NewPolygonAnnotation acts as the constructor.
func NewPolygonAnnotation() *PolygonAnnotation {
	p := &PolygonAnnotation{BaseAnnotation: *newBaseAnnotation()}
	p.BaseAnnotation.annotationType = annotationPolygon
	return p
}

// SetVertices sets the vertices for the polygon.
func (p *PolygonAnnotation) SetVertices(vertices []float32) *PolygonAnnotation {
	p.BaseAnnotation.vertices = vertices
	return p
}
