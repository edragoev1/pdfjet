package pdfjet

// AnnotationType constants
const (
	AnnotationLink           = "Link"
	AnnotationFileAttachment = "FileAttachment"
	AnnotationPolygon        = "Polygon"
	AnnotationCircle         = "Circle"
	AnnotationSquare         = "Square"
	AnnotationText           = "Text"
)

// Annotation represents a PDF annotation object.
type Annotation struct {
	objNumber      int
	annotationType string
	x1             float32
	y1             float32
	x2             float32
	y2             float32
	vertices       []float32
	fillColor      [3]float32
	transparency   float32
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
