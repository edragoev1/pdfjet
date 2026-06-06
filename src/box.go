package pdfjet

/**
 * box.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/operator"
	"github.com/edragoev1/pdfjet/src/single"
	"github.com/edragoev1/pdfjet/src/structtype"
)

// Box is used to create rectangular boxes on a page.
// Also used to for layout purposes. See the placeIn method in the Image and TextLine classes.
type Box struct {
	x, y, w, h     float32
	r              float32
	color          int32
	width          float32
	pattern        string
	fillShape      bool
	uri            string
	key            string
	language       string
	altDescription string
	actualText     string
	structureType  string
}

// NewBox creates new Box object.
func NewBox() *Box {
	box := new(Box)
	box.color = color.Black
	box.width = 0.0
	box.pattern = "[] 0"
	box.altDescription = single.Space
	box.actualText = single.Space
	box.structureType = structtype.P
	return box
}

// NewBoxAt creates a box object.
// @param x the x coordinate of the top left corner of this box when drawn on the page.
// @param y the y coordinate of the top left corner of this box when drawn on the page.
// @param w the width of this box.
// @param h the height of this box.
func NewBoxAt(x, y, w, h float32) *Box {
	box := NewBox()
	box.x = x
	box.y = y
	box.w = w
	box.h = h
	return box
}

// SetLocation sets the location of this box on the page.
// @param x the x coordinate of the top left corner of this box when drawn on the page.
// @param y the y coordinate of the top left corner of this box when drawn on the page.
func (box *Box) SetLocation(x, y float32) *Box {
	box.x = x
	box.y = y
	return box
}

// SetSize sets the size of this box.
// @param w the width of this box.
// @param h the height of this box.
func (box *Box) SetSize(w, h float32) {
	box.w = w
	box.h = h
}

// SetColor sets the color for this box.
// @param color the color specified as an integer.
func (box *Box) SetColor(color int32) {
	box.color = color
}

// SetLineWidth sets the width of this line.
// @param width the width.
func (box *Box) SetLineWidth(width float32) {
	box.width = width
}

// SetCornerRadius sets the corner radius.
// @param width the width.
func (box *Box) SetCornerRadius(r float32) {
	box.r = r
}

// SetURIAction sets the URI for the "click box" action.
// @param uri the URI
func (box *Box) SetURIAction(uri string) {
	box.uri = uri
}

// SetGoToAction sets the destination key for the action.
// @param key the destination name.
func (box *Box) SetGoToAction(key string) {
	box.key = key
}

// SetAltDescription sets the alternate description of this box.
// @param altDescription the alternate description of the box.
// @return this Box.
func (box *Box) SetAltDescription(altDescription string) *Box {
	box.altDescription = altDescription
	return box
}

// SetActualText sets the actual text for this box.
// @param actualText the actual text for the box.
// @return this Box.
func (box *Box) SetActualText(actualText string) *Box {
	box.actualText = actualText
	return box
}

// SetStructureType sets the type of the structure.
func (box *Box) SetStructureType(structureType string) *Box {
	box.structureType = structureType
	return box
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
// @param pattern the line dash pattern.
func (box *Box) SetPattern(pattern string) {
	box.pattern = pattern
}

// SetFillShape sets the private fillShape variable.
// If the value of fillShape is true - the box is filled with the current brushColor color.
// @param fillShape the value used to set the private fillShape variable.
func (box *Box) SetFillShape(fillShape bool) {
	box.fillShape = fillShape
}

// PlaceIn places this box in the another box.
// @param box the other box.
// @param xOffset the x offset from the top left corner of the box.
// @param yOffset the y offset from the top left corner of the box.
func (box *Box) PlaceIn(box2 *Box, xOffset, yOffset float32) {
	box.x = box2.x + xOffset
	box.y = box2.y + yOffset
}

// ScaleBy scales this box by the specified factor.
// @param factor the factor used to scale the box.
func (box *Box) ScaleBy(factor float32) {
	box.x *= factor
	box.y *= factor
}

// DrawOn draws this box on the specified page.
// @param page the page to draw this box on.
// @return x and y coordinates of the bottom right corner of this component.
func (box *Box) DrawOn(page *Page) []float32 {
	const k float32 = 0.5517

	page.AddBMC(box.structureType, box.language, box.actualText, box.altDescription)
	if box.r == 0.0 {
		page.MoveTo(box.x, box.y)
		page.LineTo(box.x+box.w, box.y)
		page.LineTo(box.x+box.w, box.y+box.h)
		page.LineTo(box.x, box.y+box.h)
		if box.fillShape {
			page.SetBrushColor(box.color)
			page.FillPath()
		} else {
			page.SetPenWidth(box.width)
			page.SetPenColor(box.color)
			page.SetStrokeDashPattern(box.pattern)
			page.ClosePath()
		}
	} else {
		page.SetPenWidth(box.width)
		page.SetPenColor(box.color)
		page.SetStrokeDashPattern(box.pattern)

		points := make([]*Point, 0)
		points = append(points, NewPoint(box.x+box.r, box.y))
		points = append(points, NewPoint((box.x+box.w)-box.r, box.y))
		points = append(points, NewControlPointC(((box.x+box.w)-box.r)+box.r*k, box.y))
		points = append(points, NewControlPointC(box.x+box.w, (box.y+box.r)-box.r*k))
		points = append(points, NewPoint(box.x+box.w, box.y+box.r))
		points = append(points, NewPoint(box.x+box.w, (box.y+box.h)-box.r))
		points = append(points, NewControlPointC(box.x+box.w, ((box.y+box.h)-box.r)+box.r*k))
		points = append(points, NewControlPointC(((box.x+box.w)-box.r)+box.r*k, box.y+box.h))
		points = append(points, NewPoint((box.x+box.w)-box.r, box.y+box.h))
		points = append(points, NewPoint(box.x+box.r, box.y+box.h))
		points = append(points, NewControlPointC((box.x+box.r)-box.r*k, box.y+box.h))
		points = append(points, NewControlPointC(box.x, ((box.y+box.h)-box.r)+box.r*k))
		points = append(points, NewPoint(box.x, (box.y+box.h)-box.r))
		points = append(points, NewPoint(box.x, box.y+box.r))
		points = append(points, NewControlPointC(box.x, (box.y+box.r)-box.r*k))
		points = append(points, NewControlPointC((box.x+box.r)-box.r*k, box.y))
		points = append(points, NewPoint(box.x+box.r, box.y))

		page.DrawPath(points, operator.Stroke)
	}
	page.AddEMC()

	if box.uri != "" || box.key != "" {
		page.AddAnnotation(NewAnnotation(
			"Link",
			box.x,
			box.y,
			box.x+box.w,
			box.y+box.h,
			nil,
			nil,
			0.0,
			"",
			"",
			box.uri,
			box.key, // The destination name
			box.language,
			box.actualText,
			box.altDescription))
	}

	return []float32{box.x + box.w, box.y + box.h}
}
