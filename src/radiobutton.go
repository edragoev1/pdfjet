// radiobutton.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/single"
)

// RadioButton is used to create radio button, which can be set selected or unselected.
type RadioButton struct {
	selected       bool
	x, y, r1, r2   float32
	penWidth       float32
	font           *Font
	label          string
	uri, key       string
	language       string
	altDescription string // = Single.space
	actualText     string // = Single.space
}

// NewRadioButton creates RadioButton that is not selected.
func NewRadioButton(font *Font, label string) *RadioButton {
	radioButton := new(RadioButton)
	radioButton.font = font
	radioButton.label = label
	radioButton.altDescription = single.Space
	radioButton.actualText = single.Space
	return radioButton
}

// SetFontSize sets the font size to use for this text line.
// @param fontSize the fontSize to use.
// @return this RadioButton.
func (radioButton *RadioButton) SetFontSize(fontSize float32) *RadioButton {
	radioButton.font.SetSize(fontSize)
	return radioButton
}

// SetLocation sets the x,y location on the Page.
// @param x the x coordinate on the Page.
// @param y the y coordinate on the Page.
// @return this RadioButton.
func (radioButton *RadioButton) SetLocation(x, y float32) *RadioButton {
	radioButton.x = x
	radioButton.y = y
	return radioButton
}

// SetURIAction sets the URI for the "click text line" action.
// @param uri the URI.
// @return this RadioButton.
func (radioButton *RadioButton) SetURIAction(uri string) *RadioButton {
	radioButton.uri = uri
	return radioButton
}

// SelectButton selects or deselects this radio button.
// @param selected the selection flag.
// @return this RadioButton.
func (radioButton *RadioButton) SelectButton(selected bool) *RadioButton {
	radioButton.selected = selected
	return radioButton
}

// SetAltDescription sets the alternate description of this radio button.
// @param altDescription the alternate description of the radio button.
// @return this RadioButton.
func (radioButton *RadioButton) SetAltDescription(altDescription string) *RadioButton {
	radioButton.altDescription = altDescription
	return radioButton
}

// SetActualText sets the actual text for this radio button.
// @param actualText the actual text for the radio button.
// @return this RadioButton.
func (radioButton *RadioButton) SetActualText(actualText string) *RadioButton {
	radioButton.actualText = actualText
	return radioButton
}

// DrawOn draws this RadioButton on the specified Page.
// @param page the Page where the RadioButton is to be drawn.
// @return x and y coordinates of the bottom right corner of this component.
func (radioButton *RadioButton) DrawOn(page *Page) []float32 {
	page.AddBMC("Span", radioButton.language, radioButton.actualText, radioButton.altDescription)

	radioButton.r1 = radioButton.font.GetAscent(radioButton.font.GetSize()) / 2
	radioButton.r2 = radioButton.r1 / 2
	radioButton.penWidth = radioButton.r1 / 10

	yBox := radioButton.y - radioButton.font.GetAscent(radioButton.font.size)
	page.SetPenWidth(1.0)
	page.SetPenColor(color.Black)
	page.SetStrokeDashPattern("[] 0")
	page.SetBrushColor(color.Black)
	page.DrawCircle(radioButton.x+radioButton.r1, yBox+radioButton.r1, radioButton.r1)

	if radioButton.selected {
		page.FillCircle(radioButton.x+radioButton.r1, yBox+radioButton.r1, radioButton.r2)
	}

	if radioButton.uri != "" {
		page.SetBrushColor(color.Blue)
	}
	page.DrawStringUsingColorMap(
		radioButton.font, radioButton.font, radioButton.font.size, radioButton.label,
		radioButton.x+3*radioButton.r1, radioButton.y, [3]float32{0.0, 0.0, 0.0}, nil)
	page.SetPenWidth(0.0)
	page.SetBrushColor(color.Black)

	page.AddEMC()

	if radioButton.uri != "" || radioButton.key != "" {
		page.AddAnnotation(&Annotation{
			annotationType: AnnotationLink,
			x1:             radioButton.x + 3*radioButton.r1,
			y1:             radioButton.y,
			x2:             radioButton.x + 3*radioButton.r1 + radioButton.font.StringWidth(radioButton.font.size, radioButton.label),
			y2:             radioButton.y + radioButton.font.bodyHeight,
			vertices:       nil,
			fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
			transparency:   0.0,
			title:          "",
			contents:       "",
			uri:            radioButton.uri,
			key:            "",
			language:       radioButton.language,
			actualText:     radioButton.actualText,
			altDescription: radioButton.altDescription,
		})
	}

	return []float32{
		radioButton.x + 6*radioButton.r1 + radioButton.font.StringWidth(radioButton.font.size, radioButton.label),
		radioButton.y - radioButton.font.GetDescent(radioButton.font.size)}
}
