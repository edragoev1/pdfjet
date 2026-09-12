// rect.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/single"
)

// Rect is used to create rectangular shapes on a page.
type Rect struct {
	x              float32
	y              float32
	width          float32
	height         float32
	cornerRadius   float32
	fillColor      [3]float32
	hasFillColor   bool
	borderWidth    float32
	borderColor    [3]float32
	hasBorderColor bool
	borderPattern  string
	uri            string
	key            string
	language       string
	altDescription string
	actualText     string
}

// NewRect creates new Rect object.
// @param x the x coordinate of the top left corner of this rect when drawn on the page.
// @param y the y coordinate of the top left corner of this rect when drawn on the page.
// @param w the width of this rect.
// @param h the height of this rect.
func NewRect(x, y, w, h float32) *Rect {
	rect := new(Rect)
	rect.x = x
	rect.y = y
	rect.width = w
	rect.height = h

	rect.borderWidth = 0.0
	rect.borderPattern = "[] 0"

	rect.altDescription = single.Space
	rect.actualText = single.Space
	return rect
}

// SetLocation sets the location of this rect on the page.
// @param x the x coordinate of the top left corner of this rect when drawn on the page.
// @param y the y coordinate of the top left corner of this rect when drawn on the page.
func (rect *Rect) SetLocation(x, y float32) Drawable {
	rect.x = x
	rect.y = y
	return rect
}

// SetSize sets the size of this rect.
// @param w the width of this rect.
// @param h the height of this rect.
func (rect *Rect) SetSize(w, h float32) *Rect {
	rect.width = w
	rect.height = h
	return rect
}

// SetBorderColor sets the border color as a 0xRRGGBB value.
func (rect *Rect) SetBorderColor(color int32) *Rect {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	rect.SetBorderColorRGB([3]float32{r, g, b})
	return rect
}

// SetBorderColorRGB sets the color for this rectangle.
// @param color the color specified as an integer.
func (rect *Rect) SetBorderColorRGB(borderColor [3]float32) *Rect {
	rect.borderColor = borderColor
	rect.hasBorderColor = true
	return rect
}

// SetFillColor sets the fill color as a 0xRRGGBB value.
func (rect *Rect) SetFillColor(color int32) *Rect {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	rect.SetFillColorRGB([3]float32{r, g, b})
	return rect
}

// SetFillColorRGB sets the fill color from red, green and blue values.
func (rect *Rect) SetFillColorRGB(fillColor [3]float32) *Rect {
	rect.fillColor = fillColor
	rect.hasFillColor = true
	return rect
}

// SetBorderWidth sets the width of this line.
// @param width the width.
func (rect *Rect) SetBorderWidth(borderWidth float32) *Rect {
	rect.borderWidth = borderWidth
	return rect
}

// SetBorderPattern sets the line dash pattern of the border.
func (rect *Rect) SetBorderPattern(borderPattern string) *Rect {
	rect.borderPattern = borderPattern
	return rect
}

// SetCornerRadius sets the corner radius.
// @param width the width.
func (rect *Rect) SetCornerRadius(cornerRadius float32) *Rect {
	rect.cornerRadius = cornerRadius
	return rect
}

// SetURIAction sets the URI for the "click rect" action.
// @param uri the URI
func (rect *Rect) SetURIAction(uri string) *Rect {
	rect.uri = uri
	return rect
}

// SetGoToAction sets the destination key for the action.
// @param key the destination name.
func (rect *Rect) SetGoToAction(key string) *Rect {
	rect.key = key
	return rect
}

// SetLanguage sets the language of this rect, used for accessibility.
// @param language the language, for example "en-US".
func (rect *Rect) SetLanguage(language string) *Rect {
	rect.language = language
	return rect
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

// ScaleBy scales this rect by the specified factor.
// @param factor the factor used to scale the rect.
func (rect *Rect) ScaleBy(factor float32) {
	rect.x *= factor
	rect.y *= factor
}

// DrawOn draws this rect on the specified page.
// @param page the page to draw this rect on.
// @return x and y coordinates of the bottom right corner of this component.
func (rect *Rect) DrawOn(page *Page) [2]float32 {
	const k float32 = 0.55228

	// A rectangle carries no text, so it is decorative content.
	page.AddArtifactBMC()
	page.SaveGraphicsState()
	if rect.cornerRadius == 0.0 {
		if rect.hasFillColor {
			page.MoveTo(rect.x, rect.y)
			page.LineTo(rect.x+rect.width, rect.y)
			page.LineTo(rect.x+rect.width, rect.y+rect.height)
			page.LineTo(rect.x, rect.y+rect.height)
			page.LineTo(rect.x, rect.y)
			page.SetBrushColorRGB(rect.fillColor)
			page.FillPath()
		}
		if rect.hasBorderColor {
			page.MoveTo(rect.x, rect.y)
			page.LineTo(rect.x+rect.width, rect.y)
			page.LineTo(rect.x+rect.width, rect.y+rect.height)
			page.LineTo(rect.x, rect.y+rect.height)
			page.SetPenColorRGB(rect.borderColor)
			page.SetPenWidth(rect.borderWidth)
			page.SetStrokeDashPattern(rect.borderPattern)
			page.ClosePath()
		}
	} else {
		// The pen and brush must be set before the path is painted,
		// otherwise the rounded rectangle is drawn with whatever state
		// the page happened to be left in.
		if rect.hasBorderColor {
			page.SetStrokeDashPattern(rect.borderPattern)
		}
		if rect.hasFillColor {
			page.SetBrushColorRGB(rect.fillColor)
		}
		if rect.hasBorderColor {
			page.SetPenWidth(rect.borderWidth)
			page.SetPenColorRGB(rect.borderColor)
		}

		points := make([]*Point, 0)
		points = append(points, NewPoint(rect.x+rect.cornerRadius, rect.y))
		points = append(points, NewPoint((rect.x+rect.width)-rect.cornerRadius, rect.y))
		points = append(points, NewControlPointC((rect.x+rect.width-rect.cornerRadius)+rect.cornerRadius*k, rect.y))
		points = append(points, NewControlPointC(rect.x+rect.width, (rect.y+rect.cornerRadius)-rect.cornerRadius*k))
		points = append(points, NewPoint(rect.x+rect.width, rect.y+rect.cornerRadius))
		points = append(points, NewPoint(rect.x+rect.width, (rect.y+rect.height)-rect.cornerRadius))
		points = append(points, NewControlPointC(rect.x+rect.width, ((rect.y+rect.height)-rect.cornerRadius)+rect.cornerRadius*k))
		points = append(points, NewControlPointC(((rect.x+rect.width)-rect.cornerRadius)+rect.cornerRadius*k, rect.y+rect.height))
		points = append(points, NewPoint((rect.x+rect.width)-rect.cornerRadius, rect.y+rect.height))
		points = append(points, NewPoint(rect.x+rect.cornerRadius, rect.y+rect.height))
		points = append(points, NewControlPointC((rect.x+rect.cornerRadius)-rect.cornerRadius*k, rect.y+rect.height))
		points = append(points, NewControlPointC(rect.x, ((rect.y+rect.height)-rect.cornerRadius)+rect.cornerRadius*k))
		points = append(points, NewPoint(rect.x, (rect.y+rect.height)-rect.cornerRadius))
		points = append(points, NewPoint(rect.x, rect.y+rect.cornerRadius))
		points = append(points, NewControlPointC(rect.x, (rect.y+rect.cornerRadius)-rect.cornerRadius*k))
		points = append(points, NewControlPointC((rect.x+rect.cornerRadius)-rect.cornerRadius*k, rect.y))
		points = append(points, NewPoint(rect.x+rect.cornerRadius, rect.y))

		if rect.hasFillColor && !rect.hasBorderColor {
			page.DrawPath(points, pathoperator.Fill)
		} else if !rect.hasFillColor && rect.hasBorderColor {
			page.DrawPath(points, pathoperator.Stroke)
		} else if rect.hasFillColor && rect.hasBorderColor {
			page.DrawPath(points, pathoperator.FillAndStroke)
		}
	}
	page.RestoreGraphicsState()
	page.AddEMC()

	if rect.uri != "" || rect.key != "" {
		page.addAnnotation(&Annotation{
			annotationType: AnnotationLink,
			x1:             rect.x,
			y1:             rect.y,
			x2:             rect.x + rect.width,
			y2:             rect.y + rect.height,
			vertices:       nil,
			fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
			transparency:   0.0,
			title:          "",
			contents:       "",
			uri:            rect.uri,
			key:            rect.key, // The destination name
			language:       rect.language,
			actualText:     rect.actualText,
			altDescription: rect.altDescription,
		})
	}

	return [2]float32{rect.x + rect.width, rect.y + rect.height}
}
