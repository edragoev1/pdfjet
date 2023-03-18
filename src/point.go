package pdfjet

/**
 * point.go
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Point is used to create point objects with different shapes and draw them on a page.
// Please note: When we are mentioning (x, y) coordinates of a point,
// we are talking about the coordinates of the center of the point.
// Please see Example_05.
type Point struct {
	controlPoint   bool
	x, y           float32
	r              float32
	shape          int
	color          int32
	align          int
	lineWidth      float32
	linePattern    string
	fillShape      bool
	isControlPoint bool
	drawPath       bool
	text           string
	textColor      int32
	textDirection  int
	uri, key       *string
	xBox           float32
	yBox           float32
}

// NewPoint constructor for creating point objects.
// @param x the x coordinate of this point when drawn on the page.
// @param y the y coordinate of this point when drawn on the page.
func NewPoint(x, y float32) *Point {
	point := new(Point)
	point.isControlPoint = false
	point.x = x
	point.y = y
	point.r = 2.0
	point.shape = shape.Circle
	point.color = color.Black
	point.align = align.Right
	point.lineWidth = 0.3
	point.linePattern = "[] 0"
	return point
}

// NewControlPoint constructor for creating control point objects.
// @param x the x coordinate of this point when drawn on the page.
// @param y the y coordinate of this point when drawn on the page.
// @param isControlPoint true if this point is one of the points specifying a curve.
func NewControlPoint(x, y float32) *Point {
	point := NewPoint(x, y)
	point.isControlPoint = true
	return point
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
// @param fillShape if true - fill the point with the specified brush color.
func (point *Point) SetFillShape(fillShape bool) {
	point.fillShape = fillShape
}

// GetFillShape returns the value of the fillShape private variable.
// @return the value of the private fillShape variable.
func (point *Point) GetFillShape() bool {
	return point.fillShape
}

// SetColor sets the pen color for this point.
// @param color the color specified as an integer.
func (point *Point) SetColor(color int32) *Point {
	point.color = color
	return point
}

// GetColor returns the point color as an integer.
// @return the color.
func (point *Point) GetColor() int32 {
	return point.color
}

// SetLineWidth sets the width of the lines of this point.
// @param lineWidth the line width.
func (point *Point) SetLineWidth(lineWidth float32) *Point {
	point.lineWidth = lineWidth
	return point
}

// GetLineWidth returns the width of the lines used to draw this point.
// @return the width of the lines used to draw this point.
func (point *Point) GetLineWidth() float32 {
	return point.lineWidth
}

// SetLinePattern sets the line dash pattern that controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
// <pre>
// Examples of line dash patterns:
//
//	"[Array] Phase"     Appearance          Description
//	_______________     _________________   ____________________________________
//
//	"[] 0"              -----------------   Solid line
//	"[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//	"[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//	"[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//	"[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//	"[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
//
// </pre>
//
// @param linePattern the line dash pattern.
func (point *Point) SetLinePattern(linePattern string) {
	point.linePattern = linePattern
}

// GetLinePattern returns the line dash pattern.
// @return the line dash pattern.
func (point *Point) GetLinePattern() string {
	return point.linePattern
}

// SetDrawPath sets this point as the start of a path that will be drawn on the chart.
func (point *Point) SetDrawPath() *Point {
	point.drawPath = true
	return point
}

// SetURIAction sets the URI for the "click point" action.
// @param uri the URI
func (point *Point) SetURIAction(uri *string) {
	point.uri = uri
}

// GetURIAction returns the URI for the "click point" action.
// @return the URI for the "click point" action.
func (point *Point) GetURIAction() *string {
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
func (point *Point) SetTextColor(textColor int32) {
	point.textColor = textColor
}

// GetTextColor returns the point's text color.
// @return the text color.
func (point *Point) GetTextColor() int32 {
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

// PlaceAtZeroZeroIn places this point in the specified box at position (0f, 0f).
// @param box the specified box.
func (point *Point) PlaceAtZeroZeroIn(box *Box) {
	point.PlaceIn(box, 0.0, 0.0)
}

// PlaceIn places this point in the specified box.
// @param box the specified box.
// @param xOffset the x offset from the top left corner of the box.
// @param yOffset the y offset from the top left corner of the box.
func (point *Point) PlaceIn(box *Box, xOffset, yOffset float32) {
	point.xBox = box.x + xOffset
	point.yBox = box.y + yOffset
}

// DrawOn draws this point on the specified page.
// @param page the page to draw this point on.
// @return x and y coordinates of the bottom right corner of this component.
func (point *Point) DrawOn(page *Page) []float32 {
	page.SetPenWidth(point.lineWidth)
	page.SetLinePattern(point.linePattern)

	if point.fillShape {
		page.SetBrushColor(point.color)
	} else {
		page.SetPenColor(point.color)
	}

	point.x += point.xBox
	point.y += point.yBox
	page.DrawPoint(point)
	point.x -= point.xBox
	point.y -= point.yBox

	return []float32{point.x + point.xBox + point.r, point.y + point.yBox + point.r}
}
