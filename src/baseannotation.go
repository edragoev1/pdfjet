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

// newBaseAnnotation creates the BaseAnnotation that the circle, square, polygon
// and text annotations embed.
func newBaseAnnotation() *BaseAnnotation {
	return &BaseAnnotation{
		fillColor:    [3]float32{0.5, 0.5, 0.5},
		transparency: 1.0,
		point1:       [2]float32{0, 0},
		point2:       [2]float32{0, 0},
	}
}

// annotation is implemented by BaseAnnotation and by the types that embed it,
// so Container can offset and rotate any of them.
type annotation interface {
	baseAnnotation() *BaseAnnotation
}

// baseAnnotation returns this BaseAnnotation.
func (b *BaseAnnotation) baseAnnotation() *BaseAnnotation {
	return b
}

// SetLocation sets the first point of the annotation.
func (b *BaseAnnotation) SetLocation(x, y float32) Drawable {
	b.point1 = [2]float32{x, y}
	return b
}

// SetSize sets the second point relative to the first point.
func (b *BaseAnnotation) SetSize(w, h float32) *BaseAnnotation {
	b.point2 = [2]float32{b.point1[0] + w, b.point1[1] + h}
	return b
}

// SetFillColor sets the fill color as a 0xRRGGBB value, for example color.Blue.
func (b *BaseAnnotation) SetFillColor(color int32) *BaseAnnotation {
	return b.SetFillColorRGB(colorToRGB(color))
}

// SetFillColorRGB sets the fill color from the red, green and blue components, from 0.0 to 1.0.
func (b *BaseAnnotation) SetFillColorRGB(fillColor [3]float32) *BaseAnnotation {
	b.fillColor = fillColor
	return b
}

// SetOpacity sets the opacity, from 0.0 (invisible) to 1.0 (opaque, the default).
func (b *BaseAnnotation) SetOpacity(opacity float32) *BaseAnnotation {
	b.transparency = opacity
	return b
}

// SetTitle sets the title of the annotation.
func (b *BaseAnnotation) SetTitle(title string) *BaseAnnotation {
	b.title = title
	return b
}

// SetContents sets the contents of the annotation.
func (b *BaseAnnotation) SetContents(contents string) *BaseAnnotation {
	b.contents = contents
	return b
}

// rotate rotates the annotation around its center by the given degrees.
func (b *BaseAnnotation) rotate(degrees float64) *BaseAnnotation {
	if b.container == nil {
		return b
	}
	center := b.container.GetRotationCenter()
	if b.container.parent != nil {
		center[0] += b.container.parent.X
		center[1] += b.container.parent.Y
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
	return b
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
	page.addAnnotation(&Annotation{
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
	return b.point2
}
