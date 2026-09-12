// checkbox.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/mark"
	"github.com/edragoev1/pdfjet/src/single"
	"github.com/edragoev1/pdfjet/src/structtype"
)

// CheckBox creates a CheckBox, which can be set checked or unchecked.
// By default, the checkbox is unchecked.
type CheckBox struct {
	x, y, w, h     float32
	boxColor       int32
	checkColor     int32
	penWidth       float32
	checkWidth     float32
	mark           int
	font           *Font
	fontSize       float32
	label          string
	uri, key       string
	language       string
	altDescription string
	actualText     string
}

// NewCheckBox creates a CheckBox with black check mark.
func NewCheckBox(font *Font, label string) *CheckBox {
	checkBox := new(CheckBox)
	checkBox.boxColor = color.Black
	checkBox.checkColor = color.Black
	checkBox.font = font
	checkBox.fontSize = 12.0
	checkBox.label = label
	checkBox.altDescription = single.Space
	checkBox.actualText = single.Space
	return checkBox
}

// SetFontSize sets the font size to use for checkBox text line.
// @param fontSize the fontSize to use.
// @return the CheckBox.
func (checkBox *CheckBox) SetFontSize(fontSize float32) *CheckBox {
	checkBox.fontSize = fontSize
	return checkBox
}

// SetBoxColor sets the color of the checkbox.
// @param boxColor the checkbox color specified as an 0xRRGGBB integer.
// @return the CheckBox.
func (checkBox *CheckBox) SetBoxColor(boxColor int32) *CheckBox {
	checkBox.boxColor = boxColor
	return checkBox
}

// SetCheckmark sets the color of the check mark.
// @param checkColor the check mark color specified as an 0xRRGGBB integer.
// @return the CheckBox.
func (checkBox *CheckBox) SetCheckmark(checkColor int32) *CheckBox {
	checkBox.checkColor = checkColor
	return checkBox
}

// SetLocation sets the x,y location on the Page.
// @param x the x coordinate on the Page.
// @param y the y coordinate on the Page.
// @return the CheckBox.
func (checkBox *CheckBox) SetLocation(x, y float32) Drawable {
	checkBox.x = x
	checkBox.y = y
	return checkBox
}

// GetHeight gets the height of the CheckBox.
func (checkBox *CheckBox) GetHeight() float32 {
	return checkBox.h
}

// GetWidth gets the width of the CheckBox.
func (checkBox *CheckBox) GetWidth() float32 {
	return checkBox.w
}

// Check checks or unchecks this check box. See the mark package for the options.
func (checkBox *CheckBox) Check(mark int) *CheckBox {
	checkBox.mark = mark
	return checkBox
}

// SetURIAction sets the URI for the "click text line" action.
// @param uri the URI.
// @return the CheckBox.
func (checkBox *CheckBox) SetURIAction(uri string) *CheckBox {
	checkBox.uri = uri
	return checkBox
}

// SetAltDescription sets the alternate description of checkBox check box.
// @param altDescription the alternate description of the checkbox.
// @return the Checkbox.
func (checkBox *CheckBox) SetAltDescription(altDescription string) *CheckBox {
	checkBox.altDescription = altDescription
	return checkBox
}

// SetActualText sets the actual text for checkBox check box.
// @param actualText the actual text for the checkbox.
// @return the CheckBox.
func (checkBox *CheckBox) SetActualText(actualText string) *CheckBox {
	checkBox.actualText = actualText
	return checkBox
}

// XMarkCheckBox draws a blue X mark of the specified size at x, y.
func XMarkCheckBox(page *Page, x, y, size float32) {
	page.SetPenColor(color.Blue)
	page.SetPenWidth(size / 5)
	page.MoveTo(x, y)
	page.LineTo(x+size, y+size)
	page.MoveTo(x, y+size)
	page.LineTo(x+size, y)
	page.StrokePath()
}

// DrawOn draws the CheckBox on the specified Page.
//
// @param page the Page where the CheckBox is to be drawn.
func (checkBox *CheckBox) DrawOn(page *Page) []float32 {
	page.AddBMC(structtype.P, checkBox.language, checkBox.actualText, checkBox.altDescription)

	checkBox.w = checkBox.font.ascent
	checkBox.h = checkBox.w
	checkBox.penWidth = checkBox.w / 15
	checkBox.checkWidth = checkBox.w / 5

	yBox := checkBox.y
	page.SetPenWidth(checkBox.penWidth)
	page.SetPenColor(checkBox.boxColor)
	page.SetStrokeDashPattern("[] 0")
	page.DrawRect(checkBox.x+checkBox.penWidth, yBox+checkBox.penWidth, checkBox.w, checkBox.h)

	if checkBox.mark == mark.Check || checkBox.mark == mark.X {
		page.SetPenWidth(checkBox.checkWidth)
		page.SetPenColor(checkBox.checkColor)
		switch checkBox.mark {
		case mark.Check:
			// Draw check mark
			page.MoveTo(checkBox.x+checkBox.checkWidth+checkBox.penWidth, yBox+checkBox.h/2+checkBox.penWidth)
			page.LineTo((checkBox.x+checkBox.w/6+checkBox.checkWidth)+checkBox.penWidth,
				((yBox+checkBox.h)-4.0*checkBox.checkWidth/3.0)+checkBox.penWidth)
			page.LineTo((checkBox.x+checkBox.w)-checkBox.checkWidth+checkBox.penWidth,
				yBox+checkBox.checkWidth+checkBox.penWidth)
			page.StrokePath()
		case mark.X:
			// Draw 'X' mark
			page.MoveTo(checkBox.x+checkBox.checkWidth+checkBox.penWidth, yBox+checkBox.checkWidth+checkBox.penWidth)
			page.LineTo((checkBox.x+checkBox.w)-checkBox.checkWidth+checkBox.penWidth,
				((yBox+checkBox.h)-checkBox.checkWidth)+checkBox.penWidth)
			page.MoveTo((checkBox.x+checkBox.w)-checkBox.checkWidth+checkBox.penWidth,
				yBox+checkBox.checkWidth+checkBox.penWidth)
			page.LineTo(checkBox.x+checkBox.checkWidth+checkBox.penWidth,
				((yBox+checkBox.h)-checkBox.checkWidth)+checkBox.penWidth)
			page.StrokePath()
		}
	}

	if checkBox.uri != "" {
		page.SetBrushColor(color.Blue)
	}
	page.drawString(
		checkBox.font, checkBox.fontSize, checkBox.label,
		checkBox.x+3.0*checkBox.w/2.0, checkBox.y+checkBox.font.ascent,
		[3]float32{0.0, 0.0, 0.0}, nil)
	page.SetPenWidth(0.0)
	page.SetPenColor(color.Black)
	page.SetBrushColor(color.Black)

	page.AddEMC()
	if checkBox.uri != "" || checkBox.key != "" {
		page.AddAnnotation(&Annotation{
			annotationType: AnnotationLink,
			x1:             checkBox.x + 3.0*checkBox.w/2.0,
			y1:             checkBox.y,
			x2:             checkBox.x + 3.0*checkBox.w/2.0 + checkBox.font.StringWidth(checkBox.font.size, checkBox.label),
			y2:             checkBox.y + checkBox.font.bodyHeight,
			vertices:       nil,
			fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
			transparency:   0.0,
			title:          "",
			contents:       "",
			uri:            checkBox.uri,
			key:            "",
			language:       checkBox.language,
			actualText:     checkBox.actualText,
			altDescription: checkBox.altDescription,
		})
	}

	return []float32{
		checkBox.x + 3.0*checkBox.w + checkBox.font.StringWidth(checkBox.font.size, checkBox.label),
		checkBox.y + checkBox.font.bodyHeight,
	}
}
