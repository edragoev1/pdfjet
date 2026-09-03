// point.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/pathoperator"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Point is used to create point objects with different shapes and draw them on a page.
// Please note: When we are mentioning (x, y) coordinates of a point,
// we are talking about the coordinates of the center of the point.
// Please see Example_05.
type Point struct {
	x, y           float32
	r              float32
	shape          int
	align          int
	fillColor      [3]float32
	hasFillColor   bool
	strokeWidth    float32
	strokeColor    [3]float32
	hasStrokeColor bool
	strokePattern  string
	pathOperator   string
	controlPoint   byte
	drawPath       bool
	text           string
	textColor      [3]float32
	hasTextColor   bool
	textDirection  int
	uri, key       string
	fillShape      bool
}

// NewPoint constructor for creating point objects.
// @param x the x coordinate of this point when drawn on the page.
// @param y the y coordinate of this point when drawn on the page.
func NewPoint(x, y float32) *Point {
	point := new(Point)
	point.controlPoint = 0
	point.x = x
	point.y = y
	point.r = 2.0
	point.shape = shape.Circle
	point.textColor = [3]float32{0, 0, 0}
	point.align = alignment.Right
	point.strokeWidth = 1.0
	point.strokePattern = "[] 0"
	point.pathOperator = "s" // CLOSE_AND_STROKE
	return point
}

// NewControlPointC creates a "c" type control point for cubic Bézier curves.
// @param x the x coordinate of this point.
// @param y the y coordinate of this point.
func NewControlPointC(x, y float32) *Point {
	point := NewPoint(x, y)
	point.controlPoint = 'c'
	return point
}

// NewControlPointV creates a "v" type control point for cubic Bézier curves.
// @param x the x coordinate of this point.
// @param y the y coordinate of this point.
func NewControlPointV(x, y float32) *Point {
	point := NewPoint(x, y)
	point.controlPoint = 'v'
	return point
}

// NewControlPointY creates a "y" type control point for cubic Bézier curves.
// @param x the x coordinate of this point.
// @param y the y coordinate of this point.
func NewControlPointY(x, y float32) *Point {
	point := NewPoint(x, y)
	point.controlPoint = 'y'
	return point
}

// Copy returns a new Point with the same properties as this point.
// Because all Point fields are value types, the returned copy is
// fully independent of the original — modifying it will not affect
// the original point and vice versa.
// @return a copy of this point.
func (point *Point) Copy() *Point {
	cp := *point
	return &cp
}

// SetLocation sets the location (x, y) of this point.
// @param x the x coordinate of this point when drawn on the page.
// @param y the y coordinate of this point when drawn on the page.
func (point *Point) SetLocation(x, y float32) {
	point.x = x
	point.y = y
}

// SetX sets the x coordinate of this point.
// @param x the x coordinate of this point when drawn on the page.
func (point *Point) SetX(x float32) {
	point.x = x
}

// GetX returns the x coordinate of this point.
// @return the x coordinate of this point.
func (point *Point) GetX() float32 {
	return point.x
}

// SetY sets the y coordinate of this point.
// @param y the y coordinate of this point when drawn on the page.
func (point *Point) SetY(y float32) {
	point.y = y
}

// GetY returns the y coordinate of this point.
// @return the y coordinate of this point.
func (point *Point) GetY() float32 {
	return point.y
}

// SetRadius sets the radius of this point.
// @param r the radius.
func (point *Point) SetRadius(r float32) {
	point.r = r
}

// GetRadius returns the radius of this point.
// @return the radius of this point.
func (point *Point) GetRadius() float32 {
	return point.r
}

// SetShape sets the shape of this point.
//
// @param shape the shape of this point. Supported values:
// <pre>
//
//	shape.Invisible
//	shape.Circle
//	shape.Diamond
//	shape.Box
//	shape.Plus
//	shape.HDash
//	shape.VDash
//	shape.Multiply
//	shape.Star
//	shape.XMark
//	shape.UpArrow
//	shape.DownArrow
//	shape.LeftArrow
//	shape.RightArrow
//
// </pre>
func (point *Point) SetShape(shape int) *Point {
	point.shape = shape
	return point
}

// GetShape returns the point shape code value.
// @return the shape code value.
func (point *Point) GetShape() int {
	return point.shape
}

// SetFillShape sets the private fillShape variable.
// @param fillShape if true - fill the point with the specified brushColor color.
func (point *Point) SetFillShape(fillShape bool) {
	point.fillShape = fillShape
}

// GetFillShape returns the value of the fillShape private variable.
// @return the value of the private fillShape variable.
func (point *Point) GetFillShape() bool {
	return point.fillShape
}

// SetFillColor sets the penColor color for this point.
// @param color the color specified as an integer.
func (point *Point) SetFillColor(fillColor int32) *Point {
	r := float32((fillColor>>16)&0xff) / 255.0
	g := float32((fillColor>>8)&0xff) / 255.0
	b := float32((fillColor)&0xff) / 255.0
	point.fillColor = [3]float32{r, g, b}
	point.hasFillColor = true
	return point
}

// GetFillColor returns the point color as an integer.
// @return the color.
func (point *Point) GetFillColor() [3]float32 {
	return point.fillColor
}

// SetStrokeColor sets the penColor color for this point.
// @param color the color specified as an integer.
func (point *Point) SetStrokeColor(strokeColor int32) *Point {
	r := float32((strokeColor>>16)&0xff) / 255.0
	g := float32((strokeColor>>8)&0xff) / 255.0
	b := float32((strokeColor)&0xff) / 255.0
	point.strokeColor = [3]float32{r, g, b}
	point.hasStrokeColor = true
	return point
}

// GetStrokeColor returns the point color as an integer.
// @return the color.
func (point *Point) GetStrokeColor() [3]float32 {
	return point.strokeColor
}

// SetDrawPath sets this point as the start of a path that will be drawn on the chart.
func (point *Point) SetDrawPath() *Point {
	point.drawPath = true
	return point
}

// SetURIAction sets the URI for the "click point" action.
// @param uri the URI
func (point *Point) SetURIAction(uri string) {
	point.uri = uri
}

// GetURIAction returns the URI for the "click point" action.
// @return the URI for the "click point" action.
func (point *Point) GetURIAction() string {
	return point.uri
}

// SetText sets the point text.
// @param text the text.
func (point *Point) SetText(text string) {
	point.text = text
}

// GetText returns the text associated with this point.
// @return the text.
func (point *Point) GetText() string {
	return point.text
}

// SetTextColor sets the point's text color.
// @param textColor the text color.
func (point *Point) SetTextColor(textColor int32) *Point {
	r := float32((textColor>>16)&0xff) / 255.0
	g := float32((textColor>>8)&0xff) / 255.0
	b := float32((textColor)&0xff) / 255.0
	point.textColor = [3]float32{r, g, b}
	point.hasTextColor = true
	return point
}

// GetTextColor returns the point's text color.
// @return the text color.
func (point *Point) GetTextColor() [3]float32 {
	return point.textColor
}

// SetTextDirection sets the point's text direction.
// @param textDirection the text direction.
func (point *Point) SetTextDirection(textDirection int) {
	point.textDirection = textDirection
}

// GetTextDirection returns the point's text direction.
// @return the text direction.
func (point *Point) GetTextDirection() int {
	return point.textDirection
}

// SetAlignment sets the point alignment inside table cell.
// @param align the alignment value.
func (point *Point) SetAlignment(align int) {
	point.align = align
}

// GetAlignment returns the point alignment.
// @return align the alignment value.
func (point *Point) GetAlignment() int {
	return point.align
}

// DrawOn draws this point on the specified page.
// @param page the page to draw this point on.
// @return x and y coordinates of the bottom right corner of this component.
func (point *Point) DrawOn(page *Page) [3]float32 {
	page.SaveGraphicsState()

	if point.hasFillColor == true && point.hasStrokeColor == true {
		page.SetBrushColorRGB(point.fillColor)
		page.SetPenColorRGB(point.strokeColor)
		page.SetPenWidth(point.strokeWidth)
		point.pathOperator = pathoperator.FillAndStroke
	} else if point.hasFillColor == true && point.hasStrokeColor == false {
		page.SetBrushColorRGB(point.fillColor)
		point.pathOperator = pathoperator.Fill
	} else if point.hasFillColor == false && point.hasStrokeColor == true {
		page.SetPenColorRGB(point.strokeColor)
		page.SetPenWidth(point.strokeWidth)
		point.pathOperator = pathoperator.CloseAndStroke
	}
	page.DrawPoint(point)

	page.RestoreGraphicsState()

	return [3]float32{point.x + point.r, point.y + point.r}
}

func (point *Point) SetStrokeWidth(strokeWidth float32) {
	point.strokeWidth = strokeWidth
}
