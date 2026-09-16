// squareannotation.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// SquareAnnotation extends BaseAnnotation to represent a square.
type SquareAnnotation struct {
	BaseAnnotation
}

// NewSquareAnnotation acts as the constructor.
func NewSquareAnnotation() *SquareAnnotation {
	s := &SquareAnnotation{BaseAnnotation: *newBaseAnnotation()}
	s.BaseAnnotation.annotationType = annotationSquare
	return s
}
