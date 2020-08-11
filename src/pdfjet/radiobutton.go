package pdfjet

/**
 * radiobutton.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

import (
	"color"
	"single"
)

// RadioButton is used to create radio button, which can be set selected or unselected.
type RadioButton struct {
	selected       bool
	x, y, r1, r2   float32
	penWidth       float32
	font           *Font
	label          string
	uri, key       *string
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
func (radioButton *RadioButton) SetURIAction(uri *string) *RadioButton {
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
	page.AddBMC("Span", radioButton.language, radioButton.altDescription, radioButton.actualText)

	radioButton.r1 = radioButton.font.GetAscent() / 2
	radioButton.r2 = radioButton.r1 / 2
	radioButton.penWidth = radioButton.r1 / 10

	yBox := radioButton.y - radioButton.font.GetAscent()
	page.SetPenWidth(1.0)
	page.SetPenColor(color.Black)
	page.SetLinePattern("[] 0")
	page.SetBrushColor(color.Black)
	page.DrawCircle(radioButton.x+radioButton.r1, yBox+radioButton.r1, radioButton.r1)

	if radioButton.selected {
		page.DrawCircle(radioButton.x+radioButton.r1, yBox+radioButton.r1, radioButton.r2)
	}

	if radioButton.uri != nil {
		page.SetBrushColor(color.Blue)
	}
	page.DrawStringUsingColorMap(radioButton.font, nil, radioButton.label, radioButton.x+3*radioButton.r1, radioButton.y, nil)
	page.SetPenWidth(0.0)
	page.SetBrushColor(color.Black)

	page.AddEMC()

	if radioButton.uri != nil || radioButton.key != nil {
		page.AddAnnotation(NewAnnotation(
			radioButton.uri,
			nil,
			radioButton.x+3*radioButton.r1,
			radioButton.y,
			radioButton.x+3*radioButton.r1+radioButton.font.stringWidth(radioButton.label),
			radioButton.y+radioButton.font.bodyHeight,
			radioButton.language,
			radioButton.altDescription,
			radioButton.actualText))
	}

	return []float32{
		radioButton.x + 6*radioButton.r1 + radioButton.font.stringWidth(radioButton.label),
		radioButton.y + radioButton.font.GetDescent()}
}
