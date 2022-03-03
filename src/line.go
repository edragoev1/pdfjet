package pdfjet

/**
 * line.go
 *
Copyright 2020 Innovatics Inc.

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
	"math"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/single"
)

// Line is used to create line objects.
// Please see Example_01 and Example_02
type Line struct {
	x1             float32
	y1             float32
	x2             float32
	y2             float32
	xBox           float32
	yBox           float32
	color          uint32
	width          float32
	pattern        string
	capStyle       int
	language       string
	altDescription string
	actualText     string
}

// NewLine is the contructor used to create a line objects.
//
// @param x1 the x coordinate of the start point.
// @param y1 the y coordinate of the start point.
// @param x2 the x coordinate of the end point.
// @param y2 the y coordinate of the end point.
func NewLine(x1, y1, x2, y2 float32) *Line {
	line := new(Line)
	line.x1 = x1
	line.y1 = y1
	line.x2 = x2
	line.y2 = y2
	line.color = color.Black
	line.width = 0.3
	line.pattern = "[] 0"
	line.actualText = single.Space
	line.altDescription = single.Space
	return line
}

// SetPattern sets the line dash pattern that controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
// <pre>
// Examples of line dash patterns:
//
//      "[Array] Phase"     Appearance          Description
//      _______________     _________________   ____________________________________
//
//      "[] 0"              -----------------   Solid line
//      "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//      "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//      "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//      "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//      "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
//  </pre>
//
//  @param pattern the line dash pattern.
//  @return this Line object.
func (line *Line) SetPattern(pattern string) *Line {
	line.pattern = pattern
	return line
}

// SetStartPoint sets the x and y coordinates of the start point.
// @param x the x coordinate of the start point.
// @param y the y coordinate of the start point.
// @return this Line object.
func (line *Line) SetStartPoint(x, y float32) *Line {
	line.x1 = x
	line.y1 = y
	return line
}

// SetPosition sets the start point of this line.
func (line *Line) SetPosition(x, y float32) {
	line.x1 = x
	line.y1 = y
}

// SetPointA sets the x and y coordinates of the start point.
// @param x the x coordinate of the start point.
// @param y the y coordinate of the start point.
// @return this Line object.
func (line *Line) SetPointA(x, y float32) *Line {
	line.x1 = x
	line.y1 = y
	return line
}

// GetStartPoint returns the start point of this line.
// @return Point the point.
func (line *Line) GetStartPoint() *Point {
	return NewPoint(line.x1, line.y1)
}

// SetEndPoint sets the x and y coordinates of the end point.
// @param x the x coordinate of the end point.
// @param y the t coordinate of the end point.
// @return this Line object.
func (line *Line) SetEndPoint(x, y float32) *Line {
	line.x2 = x
	line.y2 = y
	return line
}

// SetPointB sets the x and y coordinates of the end point.
// @param x the x coordinate of the end point.
// @param y the t coordinate of the end point.
// @return this Line object.
func (line *Line) SetPointB(x, y float32) *Line {
	line.x2 = x
	line.y2 = y
	return line
}

// GetEndPoint returns the end point of this line.
// @return Point the point.
func (line *Line) GetEndPoint() *Point {
	return NewPoint(line.x2, line.y2)
}

// SetWidth sets the width of this line.
// @param width the width.
// @return this Line object.
func (line *Line) SetWidth(width float32) *Line {
	line.width = width
	return line
}

// SetColor sets the color for this line.
// @param color the color specified as an integer.
// @return this Line object.
func (line *Line) SetColor(color uint32) *Line {
	line.color = color
	return line
}

// SetCapStyle sets the line cap style.
// @param style the cap style of the current line. Supported values: Cap.BUTT, Cap.ROUND and Cap.PROJECTING_SQUARE
// @return this Line object.
func (line *Line) SetCapStyle(style int) *Line {
	line.capStyle = style
	return line
}

// GetCapStyle returns the line cap style.
// @return the cap style.
func (line *Line) GetCapStyle() int {
	return line.capStyle
}

// SetAltDescription sets the alternate description of this line.
//
// @param altDescription the alternate description of the line.
// @return this Line.
func (line *Line) SetAltDescription(altDescription string) *Line {
	line.altDescription = altDescription
	return line
}

// SetActualText sets the actual text for this line.
// @param actualText the actual text for the line.
// @return this Line.
func (line *Line) SetActualText(actualText string) *Line {
	line.actualText = actualText
	return line
}

// PlaceInBox places this line in the specified box at position (0.0f, 0.0f).
//
// @param box the specified box.
// @return this Line object.
func (line *Line) PlaceInBox(box *Box) *Line {
	return line.PlaceIn(box, 0.0, 0.0)
}

// PlaceIn places this line in the specified box.
// @param box the specified box.
// @param xOffset the x offset from the top left corner of the box.
// @param yOffset the y offset from the top left corner of the box.
// @return this Line object.
func (line *Line) PlaceIn(box *Box, xOffset, yOffset float32) *Line {
	line.xBox = box.x + xOffset
	line.yBox = box.y + yOffset
	return line
}

// ScaleBy scales this line by the spacified factor.
//
// @param factor the factor used to scale the line.
// @return this Line object.
func (line *Line) ScaleBy(factor float32) *Line {
	line.x1 *= factor
	line.x2 *= factor
	line.y1 *= factor
	line.y2 *= factor
	return line
}

// DrawOn draws this line on the specified page.
//
// @param page the page to draw this line on.
// @return x and y coordinates of the bottom right corner of this component.
// @throws Exception
func (line *Line) DrawOn(page *Page) [2]float32 {
	page.SetPenColor(line.color)
	page.SetPenWidth(line.width)
	page.SetLineCapStyle(line.capStyle)
	page.SetLinePattern(line.pattern)
	page.AddBMC("Span", line.language, line.actualText, line.altDescription)
	page.DrawLine(
		line.x1+line.xBox,
		line.y1+line.yBox,
		line.x2+line.xBox,
		line.y2+line.yBox)
	page.AddEMC()

	xMax := math.Max(float64(line.x1+line.xBox), float64(line.x2+line.xBox))
	yMax := math.Max(float64(line.y1+line.yBox), float64(line.y2+line.yBox))
	return [2]float32{float32(xMax), float32(yMax)}
}
