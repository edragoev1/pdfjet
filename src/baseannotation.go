// baseannotation.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "github.com/edragoev1/pdfjet/v9/src/color"

// BaseAnnotation represents a base annotation in a PDF document.
type BaseAnnotation struct {
	annotationType string
	point1         [2]float32
	vertices       []float32 // Flattened array of x,y pairs
	fillColor      [3]float32
	opacity        float32
	title          string
	contents       string
	uri            string
	key            string
	language       string
	actualText     string
	altDescription string
	// The size SetSize sets, which the second point is at from the first
	// when the annotation is drawn, wherever SetLocation puts the first.
	width, height float32
	hasSize       bool
}

// newBaseAnnotation creates the BaseAnnotation that the circle, square, polygon
// and text annotations embed.
func newBaseAnnotation() *BaseAnnotation {
	return &BaseAnnotation{
		fillColor: [3]float32{0.5, 0.5, 0.5},
		opacity:   1.0,
		point1:    [2]float32{0, 0},
	}
}

// SetLocation sets the first point of the annotation.
func (b *BaseAnnotation) SetLocation(x, y float32) Drawable {
	b.point1 = [2]float32{x, y}
	return b
}

// SetSize sets the size of the annotation: its second point is w to the right
// of its first point and h below it, whether SetLocation is called before or
// after.
func (b *BaseAnnotation) SetSize(w, h float32) *BaseAnnotation {
	b.width = w
	b.height = h
	b.hasSize = true
	return b
}

// corner returns the second point of the annotation, which is the origin of
// the page until SetSize is called.
func (b *BaseAnnotation) corner() [2]float32 {
	if !b.hasSize {
		return [2]float32{0, 0}
	}
	return [2]float32{b.point1[0] + b.width, b.point1[1] + b.height}
}

// SetFillColor sets the fill color as a 0xRRGGBB value, for example color.Blue.
// color.Transparent leaves it unchanged.
func (b *BaseAnnotation) SetFillColor(c int32) *BaseAnnotation {
	if c == color.Transparent {
		return b
	}
	return b.SetFillColorRGB(colorToRGB(c))
}

// SetFillColorRGB sets the fill color from the red, green and blue components, from 0.0 to 1.0.
func (b *BaseAnnotation) SetFillColorRGB(fillColor [3]float32) *BaseAnnotation {
	b.fillColor = fillColor
	return b
}

// SetOpacity sets the opacity, from 0.0 (invisible) to 1.0 (opaque, the default).
// PDF/A-1 has no transparency, so a document of PDF_A_1A or PDF_A_1B ignores
// the opacity and draws the annotation opaque.
func (b *BaseAnnotation) SetOpacity(opacity float32) *BaseAnnotation {
	b.opacity = opacity
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

// DrawOn draws the annotation on the specified page.
func (b *BaseAnnotation) DrawOn(page *Page) [2]float32 {
	point2 := b.corner()
	if page == nil {
		return point2 // Measured, not drawn
	}
	page.addAnnotation(&annotationObject{
		annotationType: b.annotationType,
		x1:             b.point1[0],
		y1:             b.point1[1],
		x2:             point2[0],
		y2:             point2[1],
		// A copy, which the annotation keeps until the page is written
		vertices:       append([]float32(nil), b.vertices...),
		fillColor:      b.fillColor,
		opacity:        b.opacity,
		title:          b.title,
		contents:       b.contents,
		uri:            b.uri,
		key:            b.key,
		language:       b.language,
		actualText:     b.actualText,
		altDescription: b.altDescription,
	})
	return point2
}
