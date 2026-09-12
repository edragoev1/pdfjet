// path.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
)

// Path is used to create path objects.
// The path objects may consist of lines, splines or both.
// Please see Example_20 and Example_22.
type Path struct {
	points        []*Point
	color         int32
	width         float32
	pattern       string
	fillShape     bool
	closePath     bool
	xBox          float32
	yBox          float32
	lineCapStyle  int
	lineJoinStyle int
}

// NewPath creates an empty path.
func NewPath() *Path {
	path := new(Path)
	path.points = []*Point{}
	path.color = color.Black
	path.width = 0.0
	path.pattern = "[] 0"
	return path
}

// Add adds a point to this path.
// @param point the point to add.
// @return this Path object.
func (path *Path) Add(point *Point) *Path {
	path.points = append(path.points, point)
	return path
}

// SetPattern sets the line dash pattern for this path.
//
// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
// It is specified by a dash array and a dash phase.
// The elements of the dash array are positive numbers that specify the lengths of
// alternating dashes and gaps.
// The dash phase specifies the distance into the dash pattern at which to start the dash.
// The elements of both the dash array and the dash phase are expressed in user space units.
//
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
// @param pattern the line dash pattern.
// @return this Path object.
func (path *Path) SetPattern(pattern string) *Path {
	path.pattern = pattern
	return path
}

// SetStrokeWidth sets the stroke width that will be used to draw the lines and splines that are part of this path.
// @param width the stroke width.
// @return this Path object.
func (path *Path) SetStrokeWidth(width float32) *Path {
	path.width = width
	return path
}

// SetStrokeColor sets the stroke color that will be used to draw this path.
// @param color the color specified as an integer.
// @return this Path object.
func (path *Path) SetStrokeColor(color int32) *Path {
	path.color = color
	return path
}

// SetClosePath sets whether a line is drawn from the last point of this path back to the first.
// @param closePath true to close the path.
// @return this Path object.
func (path *Path) SetClosePath(closePath bool) *Path {
	path.closePath = closePath
	return path
}

// SetFillShape sets whether the shape of this path is filled with the stroke color instead of stroked.
// @param fillShape true to fill the shape.
// @return this Path object.
func (path *Path) SetFillShape(fillShape bool) *Path {
	path.fillShape = fillShape
	return path
}

// SetLineCapStyle sets the line cap style.
// @param style the cap style of this path. Supported values: capstyle.Butt, capstyle.Round and capstyle.ProjectingSquare
// @return this Path object.
func (path *Path) SetLineCapStyle(style int) *Path {
	path.lineCapStyle = style
	return path
}

// GetLineCapStyle returns the line cap style for this path.
// @return the line cap style for this path.
func (path *Path) GetLineCapStyle() int {
	return path.lineCapStyle
}

// SetLineJoinStyle sets the line join style.
// @param style the line join style. Supported values: joinstyle.Miter, joinstyle.Round and joinstyle.Bevel
// @return this Path object.
func (path *Path) SetLineJoinStyle(style int) *Path {
	path.lineJoinStyle = style
	return path
}

// GetLineJoinStyle returns the line join style.
// @return the line join style.
func (path *Path) GetLineJoinStyle() int {
	return path.lineJoinStyle
}

// SetLocation sets the location of this path: its points are drawn offset by x and y.
// @param x the x offset.
// @param y the y offset.
// @return this Path object.
func (path *Path) SetLocation(x, y float32) Drawable {
	path.xBox = x
	path.yBox = y
	return path
}

// ScaleBy scales the points of this path by the specified factor.
// @param factor the factor used to scale the path.
// @return this Path object.
func (path *Path) ScaleBy(factor float32) *Path {
	for _, point := range path.points {
		point.x *= factor
		point.y *= factor
	}
	return path
}

// DrawOn draws this path on the specified page. If fillShape is set the shape is
// filled with the stroke color; otherwise the path is stroked with its
// width, dash pattern, cap style and join style.
// @param page the page to draw this path on.
// @return x and y coordinates of the bottom right corner of this component.
func (path *Path) DrawOn(page *Page) [2]float32 {
	for _, point := range path.points {
		point.x += path.xBox
		point.y += path.yBox
	}

	// A path carries no text, so it is decorative content.
	page.AddArtifactBMC()
	if path.fillShape {
		page.SetBrushColor(path.color)
		page.DrawPath(path.points, pathoperator.Fill)
	} else {
		page.SetPenWidth(path.width)
		page.SetPenColor(path.color)
		page.SetStrokeDashPattern(path.pattern)
		page.SetLineCapStyle(path.lineCapStyle)
		page.SetLineJoinStyle(path.lineJoinStyle)
		if path.closePath {
			page.DrawPath(path.points, pathoperator.CloseAndStroke)
		} else {
			page.DrawPath(path.points, pathoperator.Stroke)
		}
	}
	page.AddEMC()

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

	return [2]float32{xMax, yMax}
}
