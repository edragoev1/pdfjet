package pdfjet

/**
 * rect.go
 *
©2025 PDFjet Software

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
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/operation"
	"github.com/edragoev1/pdfjet/src/single"
	"github.com/edragoev1/pdfjet/src/structuretype"
)

// Rect is used to create rectangular shapes on a page.
// Also used to for layout purposes. See the placeIn method in the Image and TextLine classes.
type Rect struct {
	x, y, w, h     float32
	r              float32
	color          int32
	width          float32
	pattern        string
	fillShape      bool
	uri            *string
	key            *string
	language       string
	altDescription string
	actualText     string
	structureType  string
}

// NewRect creates new Rect object.
func NewRect() *Rect {
	rect := new(Rect)
	rect.color = color.Black
	rect.width = 0.0
	rect.pattern = "[] 0"
	rect.altDescription = single.Space
	rect.actualText = single.Space
	rect.structureType = structuretype.P
	return rect
}

// NewRectAt creates a rect object.
// @param x the x coordinate of the top left corner of this rect when drawn on the page.
// @param y the y coordinate of the top left corner of this rect when drawn on the page.
// @param w the width of this rect.
// @param h the height of this rect.
func NewRectAt(x, y, w, h float32) *Rect {
	rect := NewRect()
	rect.x = x
	rect.y = y
	rect.w = w
	rect.h = h
	return rect
}

// SetLocation sets the location of this rect on the page.
// @param x the x coordinate of the top left corner of this rect when drawn on the page.
// @param y the y coordinate of the top left corner of this rect when drawn on the page.
func (rect *Rect) SetLocation(x, y float32) *Rect {
	rect.x = x
	rect.y = y
	return rect
}

// SetSize sets the size of this rect.
// @param w the width of this rect.
// @param h the height of this rect.
func (rect *Rect) SetSize(w, h float32) {
	rect.w = w
	rect.h = h
}

// SetColor sets the color for this rect.
// @param color the color specified as an integer.
func (rect *Rect) SetBorderColor(color int32) {
	rect.color = color
}

// SetLineWidth sets the width of this line.
// @param width the width.
func (rect *Rect) SetLineWidth(width float32) {
	rect.width = width
}

// SetCornerRadius sets the corner radious.
// @param width the width.
func (rect *Rect) SetCornerRadius(r float32) {
	rect.r = r
}

// SetURIAction sets the URI for the "click rect" action.
// @param uri the URI
func (rect *Rect) SetURIAction(uri *string) {
	rect.uri = uri
}

// SetGoToAction sets the destination key for the action.
// @param key the destination name.
func (rect *Rect) SetGoToAction(key *string) {
	rect.key = key
}

// SetAltDescription sets the alternate description of this rect.
// @param altDescription the alternate description of the rect.
// @return this Rect.
func (rect *Rect) SetAltDescription(altDescription string) *Rect {
	rect.altDescription = altDescription
	return rect
}

// SetActualText sets the actual text for this rect.
// @param actualText the actual text for the rect.
// @return this Rect.
func (rect *Rect) SetActualText(actualText string) *Rect {
	rect.actualText = actualText
	return rect
}

// SetStructureType sets the type of the structure.
func (rect *Rect) SetStructureType(structureType string) *Rect {
	rect.structureType = structureType
	return rect
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
func (rect *Rect) SetPattern(pattern string) {
	rect.pattern = pattern
}

// SetFillShape sets the private fillShape variable.
// If the value of fillShape is true - the rect is filled with the current brush color.
// @param fillShape the value used to set the private fillShape variable.
func (rect *Rect) SetFillShape(fillShape bool) {
	rect.fillShape = fillShape
}

// PlaceIn places this rect in the another rect.
// @param rect the other rect.
// @param xOffset the x offset from the top left corner of the rect.
// @param yOffset the y offset from the top left corner of the rect.
func (rect *Rect) PlaceIn(rect2 *Rect, xOffset, yOffset float32) {
	rect.x = rect2.x + xOffset
	rect.y = rect2.y + yOffset
}

// ScaleBy scales this rect by the spacified factor.
// @param factor the factor used to scale the rect.
func (rect *Rect) ScaleBy(factor float32) {
	rect.x *= factor
	rect.y *= factor
}

// DrawOn draws this rect on the specified page.
// @param page the page to draw this rect on.
// @return x and y coordinates of the bottom right corner of this component.
func (rect *Rect) DrawOn(page *Page) []float32 {
	const k float32 = 0.5517

	page.AddBMC(rect.structureType, rect.language, rect.actualText, rect.altDescription)
	if rect.r == 0.0 {
		page.MoveTo(rect.x, rect.y)
		page.LineTo(rect.x+rect.w, rect.y)
		page.LineTo(rect.x+rect.w, rect.y+rect.h)
		page.LineTo(rect.x, rect.y+rect.h)
		if rect.fillShape {
			page.SetBrushColor(rect.color)
			page.FillPath()
		} else {
			page.SetPenWidth(rect.width)
			page.SetPenColor(rect.color)
			page.SetLinePattern(rect.pattern)
			page.ClosePath()
		}
	} else {
		page.SetPenWidth(rect.width)
		page.SetPenColor(rect.color)
		page.SetLinePattern(rect.pattern)

		points := make([]*Point, 0)
		points = append(points, NewPathPoint((rect.x+rect.r), rect.y, false))
		points = append(points, NewPathPoint((rect.x+rect.w)-rect.r, rect.y, false))
		points = append(points, NewPathPoint((rect.x+rect.w-rect.r)+rect.r*k, rect.y, true))
		points = append(points, NewPathPoint((rect.x+rect.w), (rect.y+rect.r)-rect.r*k, true))
		points = append(points, NewPathPoint((rect.x+rect.w), (rect.y+rect.r), false))
		points = append(points, NewPathPoint((rect.x+rect.w), (rect.y+rect.h)-rect.r, false))
		points = append(points, NewPathPoint((rect.x+rect.w), ((rect.y+rect.h)-rect.r)+rect.r*k, true))
		points = append(points, NewPathPoint(((rect.x+rect.w)-rect.r)+rect.r*k, (rect.y+rect.h), true))
		points = append(points, NewPathPoint(((rect.x+rect.w)-rect.r), (rect.y+rect.h), false))
		points = append(points, NewPathPoint((rect.x+rect.r), (rect.y+rect.h), false))
		points = append(points, NewPathPoint(((rect.x+rect.r)-rect.r*k), (rect.y+rect.h), true))
		points = append(points, NewPathPoint(rect.x, ((rect.y+rect.h)-rect.r)+rect.r*k, true))
		points = append(points, NewPathPoint(rect.x, (rect.y+rect.h)-rect.r, false))
		points = append(points, NewPathPoint(rect.x, (rect.y+rect.r), false))
		points = append(points, NewPathPoint(rect.x, (rect.y+rect.r)-rect.r*k, true))
		points = append(points, NewPathPoint((rect.x+rect.r)-rect.r*k, rect.y, true))
		points = append(points, NewPathPoint((rect.x+rect.r), rect.y, false))

		page.DrawPath(points, operation.Stroke)
	}
	page.AddEMC()

	if rect.uri != nil || rect.key != nil {
		page.AddAnnotation(NewAnnotation(
			rect.uri,
			rect.key, // The destination name
			rect.x,
			rect.y,
			rect.x+rect.w,
			rect.y+rect.h,
			rect.language,
			rect.actualText,
			rect.altDescription))
	}

	return []float32{rect.x + rect.w, rect.y + rect.h}
}
