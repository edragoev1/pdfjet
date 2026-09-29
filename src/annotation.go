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

// The icons that the appearances of a file attachment and of a note draw in a
// box of 1 by 1, which is scaled to the rectangle of the annotation, as a
// viewer draws them: a push pin, a paperclip, and a speech bubble with two
// lines of text for a note.
const (
	pushPinIcon = "0 g\n0.34 0.7 0.32 0.1 re\n0.42 0.5 0.16 0.2 re\n0.3 0.44 0.4 0.06 re\nf\n" +
		"0.04 w 1 J\n0.5 0.44 m\n0.5 0.16 l\nS\n"
	paperclipIcon = "0.04 w 1 J 1 j\n0.44 0.62 m\n0.44 0.34 l\n0.44 0.26 0.56 0.26 0.56 0.34 c\n" +
		"0.56 0.76 l\n0.56 0.88 0.32 0.88 0.32 0.76 c\n0.32 0.28 l\n0.32 0.12 0.68 0.12 0.68 0.28 c\n" +
		"0.68 0.66 l\nS\n"
	noteIcon = "0.04 w 1 j\n0.2 0.8 m\n0.8 0.8 l\n0.8 0.4 l\n0.48 0.4 l\n0.3 0.22 l\n0.34 0.4 l\n" +
		"0.2 0.4 l\nh\n0.3 0.66 m\n0.7 0.66 l\n0.3 0.53 m\n0.7 0.53 l\nS\n"
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
	// The Link element of the content the link is drawn on, which the
	// annotation joins, so that the text of a link and its annotation are one
	// element, as PDF/UA asks; nil for a link over content that is not tagged
	linkElement *structElement
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
