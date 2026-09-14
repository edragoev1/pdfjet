// checkbox.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/mark"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// CheckBox creates a CheckBox, which can be set checked or unchecked.
// By default, the checkbox is unchecked.
type CheckBox struct {
	x, y, w, h     float32
	borderColor    [3]float32
	checkmarkColor [3]float32
	penWidth       float32
	checkWidth     float32
	mark           mark.Mark
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
	checkBox.borderColor = colorToRGB(color.Black)
	checkBox.checkmarkColor = colorToRGB(color.Black)
	checkBox.font = font
	checkBox.fontSize = 12.0
	checkBox.label = label
	return checkBox
}

// SetFontSize sets the font size to use for checkBox text line.
//   - fontSize: the fontSize to use.
//
// Returns the CheckBox.
func (checkBox *CheckBox) SetFontSize(fontSize float32) *CheckBox {
	checkBox.fontSize = fontSize
	return checkBox
}

// SetBorderColor sets the color of the checkbox.
//   - borderColor: the checkbox color specified as an 0xRRGGBB integer.
//
// Returns the CheckBox.
func (checkBox *CheckBox) SetBorderColor(borderColor int32) *CheckBox {
	checkBox.borderColor = colorToRGB(borderColor)
	return checkBox
}

// SetBorderColorRGB sets the color of the box from red, green and blue values.
func (checkBox *CheckBox) SetBorderColorRGB(rgbColor [3]float32) *CheckBox {
	checkBox.borderColor = rgbColor
	return checkBox
}

// SetCheckmarkColor sets the color of the check mark.
//   - checkmarkColor: the check mark color specified as an 0xRRGGBB integer.
//
// Returns the CheckBox.
func (checkBox *CheckBox) SetCheckmarkColor(checkmarkColor int32) *CheckBox {
	checkBox.checkmarkColor = colorToRGB(checkmarkColor)
	return checkBox
}

// SetCheckmarkColorRGB sets the color of the check mark from red, green and blue values.
func (checkBox *CheckBox) SetCheckmarkColorRGB(rgbColor [3]float32) *CheckBox {
	checkBox.checkmarkColor = rgbColor
	return checkBox
}

// SetLocation sets the x,y location on the Page.
//   - x: the x coordinate on the Page.
//   - y: the y coordinate on the Page.
//
// Returns the CheckBox.
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
func (checkBox *CheckBox) Check(checkMark mark.Mark) *CheckBox {
	checkBox.mark = checkMark
	return checkBox
}

// SetURIAction sets the URI for the "click text line" action.
//   - uri: the URI.
//
// Returns the CheckBox.
func (checkBox *CheckBox) SetURIAction(uri string) *CheckBox {
	checkBox.uri = uri
	return checkBox
}

// SetAltDescription sets the alternate description of checkBox check box.
//   - altDescription: the alternate description of the checkbox.
//
// Returns the Checkbox.
func (checkBox *CheckBox) SetAltDescription(altDescription string) *CheckBox {
	checkBox.altDescription = altDescription
	return checkBox
}

// SetActualText sets the actual text for checkBox check box.
//   - actualText: the actual text for the checkbox.
//
// Returns the CheckBox.
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
//   - page: the Page where the CheckBox is to be drawn.
func (checkBox *CheckBox) DrawOn(page *Page) [2]float32 {
	page.AddBDC(structelem.P, checkBox.language, checkBox.actualText, checkBox.altDescription)

	checkBox.w = checkBox.font.ascent
	checkBox.h = checkBox.w
	checkBox.penWidth = checkBox.w / 15
	checkBox.checkWidth = checkBox.w / 5

	yBox := checkBox.y
	page.SetPenWidth(checkBox.penWidth)
	page.SetPenColorRGB(checkBox.borderColor)
	page.SetStrokeDashPattern("[] 0")
	page.DrawRect(checkBox.x+checkBox.penWidth, yBox+checkBox.penWidth, checkBox.w, checkBox.h)

	if checkBox.mark == mark.Check || checkBox.mark == mark.X {
		page.SetPenWidth(checkBox.checkWidth)
		page.SetPenColorRGB(checkBox.checkmarkColor)
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

	// A linked label is blue.
	textColor := [3]float32{0.0, 0.0, 0.0}
	if checkBox.uri != "" {
		textColor = [3]float32{0.0, 0.0, 1.0}
	}
	page.drawString(
		checkBox.font, checkBox.fontSize, checkBox.label,
		checkBox.x+3.0*checkBox.w/2.0, checkBox.y+checkBox.font.GetAscentAt(checkBox.fontSize),
		textColor, nil)
	page.SetPenWidth(0.0)
	page.SetPenColor(color.Black)
	page.SetBrushColor(color.Black)

	page.AddEMC()
	if checkBox.uri != "" || checkBox.key != "" {
		// The link is a structure element of its own, see Page.AddAnnotation.
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             checkBox.x + 3.0*checkBox.w/2.0,
			y1:             checkBox.y,
			x2:             checkBox.x + 3.0*checkBox.w/2.0 + checkBox.font.StringWidth(checkBox.fontSize, checkBox.label),
			y2:             checkBox.y + checkBox.font.GetBodyHeightAt(checkBox.fontSize),
			vertices:       nil,
			opacity:        0.0,
			title:          "",
			contents:       "",
			uri:            checkBox.uri,
			key:            "",
			language:       checkBox.language,
			actualText:     checkBox.actualText,
			altDescription: checkBox.altDescription,
		})
	}

	return [2]float32{
		checkBox.x + 3.0*checkBox.w + checkBox.font.StringWidth(checkBox.fontSize, checkBox.label),
		checkBox.y + checkBox.font.GetBodyHeightAt(checkBox.fontSize),
	}
}
