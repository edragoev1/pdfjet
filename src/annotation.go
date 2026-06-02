package pdfjet

/**
 * annotation.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Annotation is used to create PDF annotation objects.
type Annotation struct {
	objNumber      int
	annotationType string
	x1, y1, x2, y2 float32
	vertices       []float32
	fillColor      []float32
	transparency   float32
	title          *string
	contents       *string
	uri            *string
	key            *string
	language       *string
	actualText     *string
	altDescription *string
	fileAttachment *FileAttachment
}

// NewAnnotation is the constructor used to create annotation objects.
//
// @param uri the URI string.
// @param key the destination name.
// @param x1 the x coordinate of the top left corner.
// @param y1 the y coordinate of the top left corner.
// @param x2 the x coordinate of the bottom right corner.
// @param y2 the y coordinate of the bottom right corner.
func NewAnnotation(
	annotationType string,
	x1, y1, x2, y2 float32,
	uri *string,
	key *string,
	language string,
	actualText string,
	altDescription string) *Annotation {
	annotation := new(Annotation)
	annotation.annotationType = annotationType
	annotation.x1 = x1
	annotation.y1 = y1
	annotation.x2 = x2
	annotation.y2 = y2

	annotation.title = uri
	annotation.contents = key
	annotation.uri = uri
	annotation.key = key

	annotation.language = &language
	annotation.actualText = &actualText
	annotation.altDescription = &altDescription

	if annotation.actualText == nil {
		annotation.actualText = uri
	}
	if annotation.altDescription == nil {
		annotation.altDescription = uri
	}
	return annotation
}
