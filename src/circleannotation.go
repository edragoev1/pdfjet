// circleannotation.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// CircleAnnotation extends BaseAnnotation to represent a circle.
type CircleAnnotation struct {
	BaseAnnotation
}

// NewCircleAnnotation acts as the constructor.
func NewCircleAnnotation() *CircleAnnotation {
	c := &CircleAnnotation{BaseAnnotation: *newBaseAnnotation()}
	c.BaseAnnotation.annotationType = annotationCircle
	return c
}
