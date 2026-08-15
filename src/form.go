package pdfjet

/**
 * form.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"github.com/edragoev1/pdfjet/src/color"
)

// Form describes form object.
// Please see Example_42
type Form struct {
	fields        []*Field
	x             float32
	y             float32
	f1, f2        *Font
	labelFontSize float32 // = 8f
	valueFontSize float32 // = 10f
	formWidth     float32 // = 500f
	lineWidth     float32 // = 0f
	labelColor    int32   // = Color.black
	valueColor    int32   // = Color.blue
}

// NewForm constructs new form object.
func NewForm(fields []*Field) *Form {
	form := new(Form)
	form.fields = fields
	form.labelColor = color.Black
	form.valueColor = color.Blue
	return form
}

// SetLocation sets location x and y.
func (form *Form) SetLocation(x, y float32) *Form {
	form.x = x
	form.y = y
	return form
}

// SetFormWidth sets the form width.
func (form *Form) SetFormWidth(formWidth float32) *Form {
	form.formWidth = formWidth
	return form
}

// SetLineWidth sets the line width.
func (form *Form) SetLineWidth(lineWidth float32) *Form {
	form.lineWidth = lineWidth
	return form
}

// SetLabelFont sets the font for the label text.
func (form *Form) SetLabelFont(f1 *Font) *Form {
	form.f1 = f1
	return form
}

// SetLabelFontSize sets the font size for the label text.
func (form *Form) SetLabelFontSize(labelFontSize float32) *Form {
	form.labelFontSize = labelFontSize
	return form
}

// SetValueFont sets the font for the value text.
func (form *Form) SetValueFont(f2 *Font) *Form {
	form.f2 = f2
	return form
}

// SetValueFontSize sets the font size for value text.
func (form *Form) SetValueFontSize(valueFontSize float32) *Form {
	form.valueFontSize = valueFontSize
	return form
}

// SetLabelColor sets the color for the label.
func (form *Form) SetLabelColor(labelColor int32) *Form {
	form.labelColor = labelColor
	return form
}

// SetValueColor sets the color for the value string.
func (form *Form) SetValueColor(valueColor int32) *Form {
	form.valueColor = valueColor
	return form
}

// DrawOn draws the form on the specified page.
// @param page the page to draw form on.
// @return x and y coordinates of the bottom right corner of form component.
func (form *Form) DrawOn(page *Page) []float32 {
	if page == nil {
		return []float32{}
	}

	yField := float32(0.0)
	xOffset := float32(3.0)
	for i, field := range form.fields {
		if field.x == 0.0 {
			if field.label != "" {
				if i > 0 {
					hLine := NewLine(
						form.x,
						form.y+yField,
						form.x+form.formWidth,
						form.y+yField)
					hLine.SetWidth(form.lineWidth).DrawOn(page)
				}
				yField += form.f1.GetAscent(form.labelFontSize) + 4.0*form.f1.GetDescent(form.labelFontSize)
			}
			yField += form.f2.GetAscent(form.valueFontSize) + form.f2.GetDescent(form.valueFontSize)
		}

		if field.label != "" {
			yOffset := 2*form.f1.GetDescent(form.labelFontSize) +
				form.f2.GetAscent(form.valueFontSize) + form.f2.GetDescent(form.valueFontSize)
			textLine := NewTextLine(form.f1, field.label)
			textLine.SetFontSize(form.labelFontSize)
			textLine.SetTextColor(form.labelColor)
			textLine.SetLocation(form.x+field.x+xOffset, form.y+yField-yOffset).DrawOn(page)
		}

		textLine := NewTextLine(form.f2, field.value)
		textLine.SetFontSize(form.valueFontSize)
		textLine.SetTextColor(form.valueColor)
		textLine.SetLocation(xOffset+form.x+field.x, form.y+yField-form.f2.GetDescent(form.valueFontSize))
		textLine.DrawOn(page)

		if field.x != 0.0 {
			vLine := NewLine(
				form.x+field.x,
				form.y+yField-(form.f2.GetAscent(form.valueFontSize)+form.f2.GetDescent(form.valueFontSize)),
				form.x+field.x,
				form.y+yField)
			vLine.SetWidth(form.lineWidth).DrawOn(page)
		}
	}

	rect := NewRect(form.x, form.y, form.formWidth, yField)
	rect.SetBorderWidth(form.lineWidth)
	rect.SetBorderColor(color.Black)
	rect.DrawOn(page)

	return []float32{form.x + form.formWidth, form.y + yField}
}
