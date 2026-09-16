// annotation.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// annotationType constants
const (
	annotationLink           = "Link"
	annotationFileAttachment = "FileAttachment"
	annotationPolygon        = "Polygon"
	annotationCircle         = "Circle"
	annotationSquare         = "Square"
	annotationText           = "Text"
)

// annotationObject represents a PDF annotation object.
type annotationObject struct {
	objNumber      int
	annotationType string
	x1             float32
	y1             float32
	x2             float32
	y2             float32
	vertices       []float32
	fillColor      [3]float32
	opacity        float32
	title          string
	contents       string
	uri            string
	key            string
	language       string
	actualText     string
	altDescription string
	fileAttachment *FileAttachment // Assuming FileAttachment type exists elsewhere
	// Set once the annotation has been written with a /StructParent key.
	structParentWritten bool
}

// setDescriptionFallback fills in the actual text and the alternative
// description of an annotation that was created without them. The other ports
// do this in the annotationObject constructor; annotations here are built as struct
// literals, so Page.AddAnnotation applies it instead.
// A link created from a destination name has no uri to fall back on,
// so use the name itself rather than leaving the link undescribed.
func (annotation *annotationObject) setDescriptionFallback() {
	fallback := annotation.uri
	if fallback == "" {
		fallback = annotation.key
	}
	if annotation.actualText == "" {
		annotation.actualText = fallback
	}
	if annotation.altDescription == "" {
		annotation.altDescription = fallback
	}
}
