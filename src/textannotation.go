// textannotation.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// TextAnnotation extends BaseAnnotation to represent a text label.
type TextAnnotation struct {
	BaseAnnotation
}

// NewTextAnnotation acts as the constructor.
func NewTextAnnotation() *TextAnnotation {
	t := &TextAnnotation{BaseAnnotation: *newBaseAnnotation()}
	t.BaseAnnotation.annotationType = annotationText
	return t
}
