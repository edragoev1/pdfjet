// polygonannotation.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

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
