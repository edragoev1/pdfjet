package pdfjet

import (
	"math"
)

// BaseAnnotation represents a base annotation in a PDF document.
type BaseAnnotation struct {
	annotationType string
	point1         [2]float32
	point2         [2]float32
	vertices       []float32 // Flattened array of x,y pairs
	fillColor      [3]float32
	transparency   float32
	title          string
	contents       string
	uri            string
	key            string
	language       string
	actualText     string
	altDescription string
	container      *Container
}

// NewBaseAnnotation creates a new BaseAnnotation instance.
func NewBaseAnnotation() *BaseAnnotation {
	return &BaseAnnotation{
		fillColor:    [3]float32{0.5, 0.5, 0.5},
		transparency: 1.0,
		point1:       [2]float32{0, 0},
		point2:       [2]float32{0, 0},
	}
}

// SetLocation sets the first point of the annotation.
func (b *BaseAnnotation) SetLocation(x, y float32) {
	b.point1 = [2]float32{x, y}
}

// SetPosition sets the first point of the annotation (alias for SetLocation).
func (b *BaseAnnotation) SetPosition(x, y float32) {
	b.SetLocation(x, y)
}

// SetSize sets the second point relative to the first point.
func (b *BaseAnnotation) SetSize(w, h float32) {
	b.point2 = [2]float32{b.point1[0] + w, b.point1[1] + h}
}

// SetFillColor sets the fill color using RGB float32 values (0.0-1.0).
func (b *BaseAnnotation) SetFillColor(colorRGB [3]float32) {
	b.fillColor = colorRGB
}

// SetFillColorInt sets the fill color from an integer RGB value (0xRRGGBB).
func (b *BaseAnnotation) SetFillColorInt(color int) {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	blue := float32((color>>0)&0xff) / 255.0
	b.SetFillColor([3]float32{r, g, blue})
}

// SetTransparency sets the transparency level (0.0 = fully transparent, 1.0 = opaque).
func (b *BaseAnnotation) SetTransparency(transparency float32) {
	b.transparency = transparency
}

// SetTitle sets the title of the annotation.
func (b *BaseAnnotation) SetTitle(title string) {
	b.title = title
}

// SetContents sets the contents of the annotation.
func (b *BaseAnnotation) SetContents(contents string) {
	b.contents = contents
}

// Rotate rotates the annotation around its center by the given degrees.
func (b *BaseAnnotation) Rotate(degrees float64) {
	if b.container == nil {
		return
	}

	center := b.container.GetRotationCenter()
	if b.container.Parent != nil {
		center[0] += b.container.Parent.X
		center[1] += b.container.Parent.Y
	}

	b.point1 = rotateAroundCenter(b.point1, center, degrees)
	b.point2 = rotateAroundCenter(b.point2, center, degrees)

	if b.annotationType == AnnotationPolygon {
		for i := 0; i < len(b.vertices); i += 2 {
			point := rotateAroundCenter(
				[2]float32{b.vertices[i], b.vertices[i+1]},
				[2]float32{0, 0},
				degrees,
			)
			b.vertices[i] = point[0]
			b.vertices[i+1] = point[1]
		}
	}
}

// rotateAroundCenter is a helper function to rotate a point around a center.
func rotateAroundCenter(point, center [2]float32, degrees float64) [2]float32 {
	radians := degrees * math.Pi / 180.0
	cos := float32(math.Cos(radians))
	sin := float32(math.Sin(radians))

	dx := point[0] - center[0]
	dy := point[1] - center[1]

	return [2]float32{
		center[0] + (dx*cos - dy*sin),
		center[1] + (dx*sin + dy*cos),
	}
}

// DrawOn draws the annotation on the specified page.
func (b *BaseAnnotation) DrawOn(page *Page) [2]float32 {
	page.AddAnnotation(&Annotation{
		annotationType: b.annotationType,
		x1:             b.point1[0],
		y1:             b.point1[1],
		x2:             b.point2[0],
		y2:             b.point2[1],
		vertices:       b.vertices,
		fillColor:      b.fillColor,
		transparency:   b.transparency,
		title:          b.title,
		contents:       b.contents,
		uri:            b.uri,
		key:            b.key,
		language:       b.language,
		actualText:     b.actualText,
		altDescription: b.altDescription,
	})
	return [2]float32{b.point2[0], b.point2[1]}
}
