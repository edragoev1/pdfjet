package pdfjet

/**
 * path.go
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
	"pdfjet/color"
	"pdfjet/operation"
)

// Path is used to create path objects.
// The path objects may consist of lines, splines or both.
// Please see Example_02.
type Path struct {
	points        []*Point
	color         uint32
	width         float32
	pattern       string
	fillShape     bool
	closePath     bool
	xBox          float32
	yBox          float32
	lineCapStyle  int
	lineJoinStyle int
}

// NewPath - the default constructor.
func NewPath() *Path {
	path := new(Path)
	path.points = make([]*Point, 0)
	path.color = color.Black
	path.width = 0.3
	path.pattern = "[] 0"
	return path
}

// Add adds a point to this path.
// @param point the point to add.
func (path *Path) Add(point *Point) {
	path.points = append(path.points, point)
}

// SetPattern sets the line dash pattern for this path.
//
// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
// <pre>
// Examples of line dash patterns:
//
//     "[Array] Phase"     Appearance          Description
//     _______________     _________________   ____________________________________
//
//     "[] 0"              -----------------   Solid line
//     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
//     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
//     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
//     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
//     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
// </pre>
// @param pattern the line dash pattern.
func (path *Path) SetPattern(pattern string) {
	path.pattern = pattern
}

// SetWidth sets the pen width that will be used to draw the lines and splines that are part of this path.
// @param width the pen width.
func (path *Path) SetWidth(width float32) {
	path.width = width
}

// SetColor sets the pen color that will be used to draw this path.
// @param color the color is specified as an integer.
func (path *Path) SetColor(color uint32) {
	path.color = color
}

// SetClosePath sets the closePath variable.
// @param closePath if closePath is true a line will be draw between the first and last point of this path.
func (path *Path) SetClosePath(closePath bool) {
	path.closePath = closePath
}

// SetFillShape sets the fillShape private variable. If fillShape is true - the shape of the path will be filled with the current brush color.
// @param fillShape the fillShape flag.
func (path *Path) SetFillShape(fillShape bool) {
	path.fillShape = fillShape
}

// SetLineCapStyle sets the line cap style.
// @param style the cap style of this path. Supported values: Cap.BUTT, Cap.ROUND and Cap.PROJECTING_SQUARE
func (path *Path) SetLineCapStyle(style int) {
	path.lineCapStyle = style
}

// GetLineCapStyle returns the line cap style for this path.
// @return the line cap style for this path.
func (path *Path) GetLineCapStyle() int {
	return path.lineCapStyle
}

// SetLineJoinStyle sets the line join style.
// Supported values: Join.MITER, Join.ROUND and Join.BEVEL
func (path *Path) SetLineJoinStyle(style int) {
	path.lineJoinStyle = style
}

// GetLineJoinStyle returns the line join style.
func (path *Path) GetLineJoinStyle() int {
	return path.lineJoinStyle
}

// PlaceAtZeroZeroIn places this path in the specified box at position (0.0, 0.0).
func (path *Path) PlaceAtZeroZeroIn(box *Box) {
	path.PlaceIn(box, 0.0, 0.0)
}

// PlaceIn places the path inside the spacified box at coordinates (xOffset, yOffset) of the top left corner.
// @param box the specified box.
// @param xOffset the xOffset.
// @param yOffset the yOffset.
func (path *Path) PlaceIn(box *Box, xOffset, yOffset float32) {
	path.xBox = box.x + xOffset
	path.yBox = box.y + yOffset
}

// SetLocation sets the location of the path.
func (path *Path) SetLocation(x, y float32) {
	path.xBox += x
	path.yBox += y
}

// ScaleBy scales the path using the specified factor.
func (path *Path) ScaleBy(factor float32) {
	for _, point := range path.points {
		point.x *= factor
		point.y *= factor
	}
}

// GetCurvePoints returns a list containing the start point, first control point,
// second control point and the end point of elliptical curve segment.
// Please see Example_18.
//
// @param x the x coordinate of the center of the ellipse.
// @param y the y coordinate of the center of the ellipse.
// @param r1 the horizontal radius of the ellipse.
// @param r2 the vertical radius of the ellipse.
// @param segment the segment to draw - please see the Segment class.
// @return a list of the curve points.
func GetCurvePoints(x, y, r1, r2 float32, segment int) []*Point {
	points := make([]*Point, 0)

	// The best 4-spline magic number
	var m4 float32 = 0.551784

	if segment == 0 {
		points = append(points, NewPoint(x, y-r2))
		points = append(points, NewControlPoint(x+m4*r1, y-r2))
		points = append(points, NewControlPoint(x+r1, y-m4*r2))
		points = append(points, NewPoint(x+r1, y))
	} else if segment == 1 {
		points = append(points, NewPoint(x+r1, y))
		points = append(points, NewControlPoint(x+r1, y+m4*r2))
		points = append(points, NewControlPoint(x+m4*r1, y+r2))
		points = append(points, NewPoint(x, y+r2))
	} else if segment == 2 {
		points = append(points, NewPoint(x, y+r2))
		points = append(points, NewControlPoint(x-m4*r1, y+r2))
		points = append(points, NewControlPoint(x-r1, y+m4*r2))
		points = append(points, NewPoint(x-r1, y))
	} else if segment == 3 {
		points = append(points, NewPoint(x-r1, y))
		points = append(points, NewControlPoint(x-r1, y-m4*r2))
		points = append(points, NewControlPoint(x-m4*r1, y-r2))
		points = append(points, NewPoint(x, y-r2))
	}

	return points
}

// DrawOn draws this path on the page using the current selected color, pen width, line pattern and line join style.
// @param page the page to draw this path on.
// @return x and y coordinates of the bottom right corner of this component.
func (path *Path) DrawOn(page *Page) []float32 {
	if path.fillShape {
		page.SetBrushColor(path.color)
	} else {
		page.SetPenColor(path.color)
	}
	page.SetPenWidth(path.width)
	page.SetLinePattern(path.pattern)
	page.SetLineCapStyle(path.lineCapStyle)
	page.SetLineJoinStyle(path.lineJoinStyle)

	for _, point := range path.points {
		point.x += path.xBox
		point.y += path.yBox
	}

	if path.fillShape {
		page.DrawPath(path.points, operation.Fill)
	} else {
		if path.closePath {
			page.DrawPath(path.points, operation.Close)
		} else {
			page.DrawPath(path.points, operation.Stroke)
		}
	}

	var xMax float32 = 0.0
	var yMax float32 = 0.0
	for _, point := range path.points {
		if point.x > xMax {
			xMax = point.x
		}
		if point.y > yMax {
			yMax = point.y
		}
		point.x -= path.xBox
		point.y -= path.yBox
	}

	return []float32{xMax, yMax}
}
