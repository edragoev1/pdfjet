package pdfjet

/**
 * checkbox.go
 *
Copyright 2020 Innovatics Inc.

Portions provided by Shirley C. Christenson
Shirley Christenson Consulting

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  checkBox list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  checkBox list of conditions and the following disclaimer in the documentation
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
	"mark"
	"single"
)

// CheckBox creates a CheckBox, which can be set checked or unchecked.
// By default the check box is unchecked.
type CheckBox struct {
	x, y, w, h     float32
	boxColor       uint32
	checkColor     uint32
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
// @return checkBox CheckBox.
func (checkBox *CheckBox) SetFontSize(fontSize float32) *CheckBox {
	checkBox.font.SetSize(fontSize)
	return checkBox
}

// SetBoxColor sets the color of the check box.
// @param boxColor the check box color specified as an 0xRRGGBB integer.
// @return checkBox CheckBox.
func (checkBox *CheckBox) SetBoxColor(boxColor uint32) *CheckBox {
	checkBox.boxColor = boxColor
	return checkBox
}

// SetCheckmark sets the color of the check mark.
// @param checkColor the check mark color specified as an 0xRRGGBB integer.
// @return checkBox CheckBox.
func (checkBox *CheckBox) SetCheckmark(checkColor uint32) *CheckBox {
	checkBox.checkColor = checkColor
	return checkBox
}

// SetLocation sets the x,y location on the Page.
// @param x the x coordinate on the Page.
// @param y the y coordinate on the Page.
// @return checkBox CheckBox.
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

/**
 *  Checks or unchecks checkBox check box. See the Mark class for available options.
 *
 *  @return checkBox CheckBox.
 */
func (checkBox *CheckBox) check(mark int) *CheckBox {
	checkBox.mark = mark
	return checkBox
}

// SetURIAction sets the URI for the "click text line" action.
// @param uri the URI.
// @return checkBox CheckBox.
func (checkBox *CheckBox) SetURIAction(uri *string) *CheckBox {
	checkBox.uri = uri
	return checkBox
}

// SetAltDescription sets the alternate description of checkBox check box.
// @param altDescription the alternate description of the check box.
// @return checkBox Checkbox.
func (checkBox *CheckBox) SetAltDescription(altDescription string) *CheckBox {
	checkBox.altDescription = altDescription
	return checkBox
}

// SetActualText sets the actual text for checkBox check box.
// @param actualText the actual text for the check box.
// @return checkBox CheckBox.
func (checkBox *CheckBox) SetActualText(actualText string) *CheckBox {
	checkBox.actualText = actualText
	return checkBox
}

// DrawOn draws checkBox CheckBox on the specified Page.
//
// @param page the Page where the CheckBox is to be drawn.
func (checkBox *CheckBox) DrawOn(page Page) []float32 {
	page.AddBMC("Span", checkBox.language, checkBox.altDescription, checkBox.actualText)

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
		if checkBox.mark == mark.Check {
			// Draw check mark
			page.MoveTo(checkBox.x+checkBox.checkWidth, yBox+checkBox.h/2)
			page.LineTo(checkBox.x+checkBox.w/6+checkBox.checkWidth, (yBox+checkBox.h)-4.0*checkBox.checkWidth/3.0)
			page.LineTo((checkBox.x+checkBox.w)-checkBox.checkWidth, yBox+checkBox.checkWidth)
			page.StrokePath()
		} else if checkBox.mark == mark.X {
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
	page.DrawStringUsingColorMap(checkBox.font, nil, checkBox.label, checkBox.x+3.0*checkBox.w/2.0, checkBox.y, nil)
	page.SetPenWidth(0.0)
	page.SetPenColor(color.Black)
	page.SetBrushColor(color.Black)

	page.AddEMC()
	if checkBox.uri != nil {
		page.AddAnnotation(NewAnnotation(
			checkBox.uri,
			nil,
			checkBox.x+3.0*checkBox.w/2.0,
			checkBox.y,
			checkBox.x+3.0*checkBox.w/2.0+checkBox.font.stringWidth(checkBox.label),
			checkBox.y+checkBox.font.bodyHeight,
			checkBox.language,
			checkBox.altDescription,
			checkBox.actualText))
	}

	return []float32{checkBox.x + 3.0*checkBox.w + checkBox.font.stringWidth(checkBox.label), checkBox.y + checkBox.font.descent}
}