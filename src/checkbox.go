package pdfjet

/**
 * checkbox.go
 *
 * Copyright (c) 2025 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/mark"
	"github.com/edragoev1/pdfjet/src/single"
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
	label          string
	uri, key       *string
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
	checkBox.label = label
	checkBox.altDescription = single.Space
	checkBox.actualText = single.Space
	return checkBox
}

// SetFontSize sets the font size to use for checkBox text line.
// @param fontSize the fontSize to use.
// @return the CheckBox.
func (checkBox *CheckBox) SetFontSize(fontSize float32) *CheckBox {
	checkBox.font.SetSize(fontSize)
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
func (checkBox *CheckBox) SetLocation(x, y float32) *CheckBox {
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

func (checkBox *CheckBox) Check(mark int) *CheckBox {
	checkBox.mark = mark
	return checkBox
}

// SetURIAction sets the URI for the "click text line" action.
// @param uri the URI.
// @return the CheckBox.
func (checkBox *CheckBox) SetURIAction(uri *string) *CheckBox {
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
	page.AddBMC("Span", checkBox.language, checkBox.actualText, checkBox.altDescription)

	checkBox.w = checkBox.font.ascent
	checkBox.h = checkBox.w
	checkBox.penWidth = checkBox.w / 15
	checkBox.checkWidth = checkBox.w / 5

	yBox := checkBox.y - checkBox.font.ascent
	page.SetPenWidth(checkBox.penWidth)
	page.SetPenColor(checkBox.boxColor)
	page.SetLinePattern("[] 0")
	page.DrawRect(checkBox.x, yBox, checkBox.w, checkBox.h)

	if checkBox.mark == mark.Check || checkBox.mark == mark.X {
		page.SetPenWidth(checkBox.checkWidth)
		page.SetPenColor(checkBox.checkColor)
		switch checkBox.mark {
		case mark.Check:
			// Draw check mark
			page.MoveTo(checkBox.x+checkBox.checkWidth, yBox+checkBox.h/2)
			page.LineTo(checkBox.x+checkBox.w/6+checkBox.checkWidth, (yBox+checkBox.h)-4.0*checkBox.checkWidth/3.0)
			page.LineTo((checkBox.x+checkBox.w)-checkBox.checkWidth, yBox+checkBox.checkWidth)
			page.StrokePath()
		case mark.X:
			// Draw 'X' mark
			page.MoveTo(checkBox.x+checkBox.checkWidth, yBox+checkBox.checkWidth)
			page.LineTo((checkBox.x+checkBox.w)-checkBox.checkWidth, (yBox+checkBox.h)-checkBox.checkWidth)
			page.MoveTo((checkBox.x+checkBox.w)-checkBox.checkWidth, yBox+checkBox.checkWidth)
			page.LineTo(checkBox.x+checkBox.checkWidth, (yBox+checkBox.h)-checkBox.checkWidth)
			page.StrokePath()
		}
	}

	if checkBox.uri != nil {
		page.SetBrushColor(color.Blue)
	}
	page.DrawStringUsingColorMap(
		checkBox.font, checkBox.font, checkBox.font.size,
		checkBox.label, checkBox.x+3.0*checkBox.w/2.0, checkBox.y, [3]float32{0.0, 0.0, 0.0}, nil)
	page.SetPenWidth(0.0)
	page.SetPenColor(color.Black)
	page.SetBrushColor(color.Black)

	page.AddEMC()
	if checkBox.uri != nil || checkBox.key != nil {
		page.AddAnnotation(NewAnnotation(
			"Link",
			checkBox.x+3.0*checkBox.w/2.0,
			checkBox.y,
			checkBox.x+3.0*checkBox.w/2.0+checkBox.font.stringWidth(checkBox.font.size, checkBox.label),
			checkBox.y+checkBox.font.bodyHeight,
			nil,
			nil,
			0.0,
			"",
			"",
			checkBox.uri,
			nil,
			checkBox.language,
			checkBox.actualText,
			checkBox.altDescription))
	}

	return []float32{
		checkBox.x + 3.0*checkBox.w + checkBox.font.stringWidth(checkBox.font.size, checkBox.label),
		checkBox.y + checkBox.font.descent,
	}
}
