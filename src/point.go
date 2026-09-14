// point.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// Point is a point with a marker: a shape drawn around its (x, y) coordinates,
// which are the center of the marker. A point is drawn on a page on its own,
// as the marker of a table cell, or in a chart Series, and a slice of points
// is a path for Page.DrawPath.
// Please see Example_05.
type Point struct {
	x, y           float32
	r              float32
	shape          shape.Shape
	fillColor      [3]float32
	hasFillColor   bool
	strokeWidth    float32
	strokeColor    [3]float32
	hasStrokeColor bool
	pathOperator   pathoperator.PathOperator
	controlPoint   byte
	uri            string
}

// NewPoint constructor for creating point objects.
//   - x: the x coordinate of this point when drawn on the page.
//   - y: the y coordinate of this point when drawn on the page.
func NewPoint(x, y float32) *Point {
	point := new(Point)
	point.controlPoint = 0
	point.x = x
	point.y = y
	point.r = 2.0
	point.shape = shape.Circle
	point.strokeWidth = 1.0
	point.pathOperator = pathoperator.CloseAndStroke
	return point
}

// NewControlPointC creates a "c" type control point for cubic Bézier curves.
//   - x: the x coordinate of this point.
//   - y: the y coordinate of this point.
func NewControlPointC(x, y float32) *Point {
	point := NewPoint(x, y)
	point.controlPoint = 'c'
	return point
}

// NewControlPointV creates a "v" type control point for cubic Bézier curves.
//   - x: the x coordinate of this point.
//   - y: the y coordinate of this point.
func NewControlPointV(x, y float32) *Point {
	point := NewPoint(x, y)
	point.controlPoint = 'v'
	return point
}

// NewControlPointY creates a "y" type control point for cubic Bézier curves.
//   - x: the x coordinate of this point.
//   - y: the y coordinate of this point.
func NewControlPointY(x, y float32) *Point {
	point := NewPoint(x, y)
	point.controlPoint = 'y'
	return point
}

// copyPoint returns a copy of the point, including its marker and its URI action.
func copyPoint(point *Point) *Point {
	copied := *point
	return &copied
}

// SetLocation sets the location (x, y) of this point.
//   - x: the x coordinate of this point when drawn on the page.
//   - y: the y coordinate of this point when drawn on the page.
func (point *Point) SetLocation(x, y float32) Drawable {
	point.x = x
	point.y = y
	return point
}

// SetX sets the x coordinate of this point.
//   - x: the x coordinate of this point when drawn on the page.
func (point *Point) SetX(x float32) *Point {
	point.x = x
	return point
}

// GetX returns the x coordinate of this point.
// Returns the x coordinate of this point.
func (point *Point) GetX() float32 {
	return point.x
}

// SetY sets the y coordinate of this point.
//   - y: the y coordinate of this point when drawn on the page.
func (point *Point) SetY(y float32) *Point {
	point.y = y
	return point
}

// GetY returns the y coordinate of this point.
// Returns the y coordinate of this point.
func (point *Point) GetY() float32 {
	return point.y
}

// SetRadius sets the radius of this point.
//   - r: the radius.
func (point *Point) SetRadius(r float32) *Point {
	point.r = r
	return point
}

// GetRadius returns the radius of this point.
// Returns the radius of this point.
func (point *Point) GetRadius() float32 {
	return point.r
}

// SetShape sets the shape of the marker drawn at this point, for example
// shape.Circle; shape.Invisible draws no marker.
func (point *Point) SetShape(pointShape shape.Shape) *Point {
	point.shape = pointShape
	return point
}

// GetShape returns the shape of the marker drawn at this point.
func (point *Point) GetShape() shape.Shape {
	return point.shape
}

// SetFillColor sets the fill color as a 0xRRGGBB value.
func (point *Point) SetFillColor(fillColor int32) *Point {
	r := float32((fillColor>>16)&0xff) / 255.0
	g := float32((fillColor>>8)&0xff) / 255.0
	b := float32((fillColor)&0xff) / 255.0
	point.fillColor = [3]float32{r, g, b}
	point.hasFillColor = true
	return point
}

// SetFillColorRGB sets the fill color from red, green and blue values.
func (point *Point) SetFillColorRGB(rgbColor [3]float32) *Point {
	point.fillColor = rgbColor
	point.hasFillColor = true
	return point
}

// GetFillColor returns the fill color as red, green and blue values.
func (point *Point) GetFillColor() [3]float32 {
	return point.fillColor
}

// SetStrokeColor sets the stroke color as a 0xRRGGBB value.
func (point *Point) SetStrokeColor(strokeColor int32) *Point {
	r := float32((strokeColor>>16)&0xff) / 255.0
	g := float32((strokeColor>>8)&0xff) / 255.0
	b := float32((strokeColor)&0xff) / 255.0
	point.strokeColor = [3]float32{r, g, b}
	point.hasStrokeColor = true
	return point
}

// SetStrokeColorRGB sets the stroke color from red, green and blue values.
func (point *Point) SetStrokeColorRGB(rgbColor [3]float32) *Point {
	point.strokeColor = rgbColor
	point.hasStrokeColor = true
	return point
}

// GetStrokeColor returns the stroke color as red, green and blue values.
func (point *Point) GetStrokeColor() [3]float32 {
	return point.strokeColor
}

// SetStrokeWidth sets the width of the lines used to draw this point.
func (point *Point) SetStrokeWidth(strokeWidth float32) *Point {
	point.strokeWidth = strokeWidth
	return point
}

// GetStrokeWidth returns the width of the lines used to draw this point.
// Returns the stroke width.
func (point *Point) GetStrokeWidth() float32 {
	return point.strokeWidth
}

// SetURIAction sets the URI of the link opened by a click on this point.
//   - uri: the URI.
func (point *Point) SetURIAction(uri string) *Point {
	point.uri = uri
	return point
}

// GetURIAction returns the URI of the link opened by a click on this point.
// Returns the URI, or "".
func (point *Point) GetURIAction() string {
	return point.uri
}

// DrawOn draws this point on the specified page.
//   - page: the page to draw this point on.
//
// Returns x and y coordinates of the bottom right corner of this component.
func (point *Point) DrawOn(page *Page) [2]float32 {
	if page == nil {
		return [2]float32{point.x + point.r, point.y + point.r}
	}

	page.SaveGraphicsState()
	if point.hasFillColor && point.hasStrokeColor {
		page.SetBrushColorRGB(point.fillColor)
		page.SetPenColorRGB(point.strokeColor)
		page.SetPenWidth(point.strokeWidth)
		point.pathOperator = pathoperator.FillAndStroke
	} else if point.hasFillColor && !point.hasStrokeColor {
		page.SetBrushColorRGB(point.fillColor)
		point.pathOperator = pathoperator.Fill
	} else if !point.hasFillColor && point.hasStrokeColor {
		page.SetPenColorRGB(point.strokeColor)
		page.SetPenWidth(point.strokeWidth)
		point.pathOperator = pathoperator.CloseAndStroke
	}
	page.DrawPoint(point)
	page.RestoreGraphicsState()

	return [2]float32{point.x + point.r, point.y + point.r}
}
