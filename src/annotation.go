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
}

// NewAnnotation creates a new Annotation instance.
// It mimics the C# constructor behavior including the fallback logic for actualText and altDescription.
func NewAnnotation(
	annotationType string,
	x1, y1, x2, y2 float32,
	vertices []float32,
	fillColor [3]float32,
	transparency float32,
	title, contents, uri, key, language, actualText, altDescription string,
) *Annotation {
	// Handle fallback logic: if actualText/altDescription are nil (empty in Go), use uri
	finalActualText := actualText
	if finalActualText == "" {
		finalActualText = uri
	}

	finalAltDesc := altDescription
	if finalAltDesc == "" {
		finalAltDesc = uri
	}

	return &Annotation{
		annotationType: annotationType,
		x1:             x1,
		y1:             y1,
		x2:             x2,
		y2:             y2,
		vertices:       vertices,
		fillColor:      fillColor,
		transparency:   transparency,
		title:          title,
		contents:       contents,
		uri:            uri,
		key:            key,
		language:       language,
		actualText:     finalActualText,
		altDescription: finalAltDesc,
	}
}
