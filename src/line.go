// line.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Line is used to create line objects.
// Please see Example_23 and Example_46.
type Line struct {
	x1             float32
	y1             float32
	x2             float32
	y2             float32
	color          [3]float32
	width          float32
	pattern        string
	capStyle       capstyle.CapStyle
	language       string
	altDescription string
	actualText     string
}

// NewLine creates a line object.
//
//   - x1: the x coordinate of the start point.
//   - y1: the y coordinate of the start point.
//   - x2: the x coordinate of the end point.
//   - y2: the y coordinate of the end point.
func NewLine(x1, y1, x2, y2 float32) *Line {
	line := new(Line)
	line.x1 = x1
	line.y1 = y1
	line.x2 = x2
	line.y2 = y2
	line.color = colorToRGB(color.Black)
	line.width = 0.0
	line.pattern = "[] 0"
	return line
}

// SetStrokeDashPattern sets the line dash pattern that controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
//
// Examples of line dash patterns:
//
//		"[Array] Phase"     Appearance          Description
//		_______________     _________________   ____________________________________
//
//		"[] 0"              -----------------   Solid line
//		"[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//		"[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//		"[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//		"[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//		"[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
//
//	  - pattern: the line dash pattern.
//
// Returns this Line object.
func (line *Line) SetStrokeDashPattern(pattern string) *Line {
	line.pattern = pattern
	return line
}

// SetStartPoint sets the x and y coordinates of the start point.
//   - x: the x coordinate of the start point.
//   - y: the y coordinate of the start point.
//
// Returns this Line object.
func (line *Line) SetStartPoint(x, y float32) *Line {
	line.x1 = x
	line.y1 = y
	return line
}

// SetLocation moves this line so that it starts at the specified point. The end
// point moves with it.
//   - x: the x coordinate of the start point.
//   - y: the y coordinate of the start point.
//
// Returns this Line object.
func (line *Line) SetLocation(x, y float32) Drawable {
	line.x2 += x - line.x1
	line.y2 += y - line.y1
	line.x1 = x
	line.y1 = y
	return line
}

// GetStartPoint returns the start point of this line.
// Returns Point the point.
func (line *Line) GetStartPoint() *Point {
	return NewPoint(line.x1, line.y1)
}

// SetEndPoint sets the x and y coordinates of the end point.
//   - x: the x coordinate of the end point.
//   - y: the y coordinate of the end point.
//
// Returns this Line object.
func (line *Line) SetEndPoint(x, y float32) *Line {
	line.x2 = x
	line.y2 = y
	return line
}

// GetEndPoint returns the end point of this line.
// Returns Point the point.
func (line *Line) GetEndPoint() *Point {
	return NewPoint(line.x2, line.y2)
}

// SetStrokeWidth sets the stroke width of this line.
//   - width: the width.
//
// Returns this Line object.
func (line *Line) SetStrokeWidth(width float32) *Line {
	line.width = width
	return line
}

// SetStrokeColor sets the stroke color of this line.
//   - color: the color specified as an integer.
//
// Returns this Line object.
func (line *Line) SetStrokeColor(color int32) *Line {
	line.color = colorToRGB(color)
	return line
}

// SetStrokeColorRGB sets the color of this line from red, green and blue values.
func (line *Line) SetStrokeColorRGB(rgbColor [3]float32) *Line {
	line.color = rgbColor
	return line
}

// SetLineCapStyle sets the line cap style.
//   - style: the cap style of the current line. Supported values: capstyle.Butt, capstyle.Round and capstyle.ProjectingSquare
//
// Returns this Line object.
func (line *Line) SetLineCapStyle(style capstyle.CapStyle) *Line {
	line.capStyle = style
	return line
}

// GetLineCapStyle returns the line cap style.
// Returns the cap style.
func (line *Line) GetLineCapStyle() capstyle.CapStyle {
	return line.capStyle
}

// SetAltDescription sets the alternate description of this line.
//
//   - altDescription: the alternate description of the line.
//
// Returns this Line.
func (line *Line) SetAltDescription(altDescription string) *Line {
	line.altDescription = altDescription
	return line
}

// SetActualText sets the actual text for this line.
//   - actualText: the actual text for the line.
//
// Returns this Line.
func (line *Line) SetActualText(actualText string) *Line {
	line.actualText = actualText
	return line
}

// ScaleBy scales this line by the specified factor.
//
//   - factor: the factor used to scale the line.
//
// Returns this Line object.
func (line *Line) ScaleBy(factor float32) *Line {
	line.x1 *= factor
	line.x2 *= factor
	line.y1 *= factor
	line.y2 *= factor
	return line
}

// DrawOn draws this line on the specified page.
//
//   - page: the page to draw this line on.
//
// Returns x and y coordinates of the bottom right corner of this component.
func (line *Line) DrawOn(page *Page) [2]float32 {
	if page == nil {
		return [2]float32{max(line.x1, line.x2), max(line.y1, line.y2)} // Measured, not drawn
	}
	page.AddBDC(structelem.P, line.language, line.actualText, line.altDescription)
	page.SaveGraphicsState()
	page.SetPenColorRGB(line.color)
	page.SetPenWidth(line.width)
	page.SetLineCapStyle(line.capStyle)
	page.SetStrokeDashPattern(line.pattern)
	page.DrawLine(line.x1, line.y1, line.x2, line.y2)
	page.RestoreGraphicsState()
	page.AddEMC()

	xMax := math.Max(float64(line.x1), float64(line.x2))
	yMax := math.Max(float64(line.y1), float64(line.y2))
	return [2]float32{float32(xMax), float32(yMax)}
}
