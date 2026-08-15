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
	labelFontSize float32    // = 9f
	valueFontSize float32    // = 9f
	formWidth     float32    // = 500f
	lineWidth     float32    // = 0f
	labelColor    [3]float32 // = Color.black
	valueColor    [3]float32
}

// NewForm constructs new form object.
func NewForm(fields []*Field) *Form {
	form := new(Form)
	form.fields = fields
	form.labelColor = [3]float32{0.0, 0.0, 0.0}
	form.valueColor = [3]float32{0.33, 0.33, 0.66}
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

// parseColor converts an int32 color value (0xRRGGBB) to RGB floats in range 0.0-1.0.
func parseColor(color int32) [3]float32 {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32(color&0xff) / 255.0
	return [3]float32{r, g, b}
}

// SetLabelColor sets the color for the label.
func (form *Form) SetLabelColor(labelColor int32) *Form {
	form.labelColor = parseColor(labelColor)
	return form
}

// SetValueColor sets the color for the value string.
func (form *Form) SetValueColor(valueColor int32) *Form {
	form.valueColor = parseColor(valueColor)
	return form
}

// SetLabelColorRGB sets the color for the label using RGB float values (0.0-1.0).
func (form *Form) SetLabelColorRGB(color [3]float32) *Form {
	form.labelColor = color
	return form
}

// SetValueColorRGB sets the color for the value string using RGB float values (0.0-1.0).
func (form *Form) SetValueColorRGB(color [3]float32) *Form {
	form.valueColor = color
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
				yField += form.f1.GetAscent(form.labelFontSize) + 3.0*form.f1.GetDescent(form.labelFontSize)
			}
			yField += form.f2.GetAscent(form.valueFontSize) + form.f2.GetDescent(form.valueFontSize)
		}

		if field.label != "" {
			yOffset := 2*form.f1.GetDescent(form.labelFontSize) +
				form.f2.GetAscent(form.valueFontSize) + form.f2.GetDescent(form.valueFontSize)
			textLine := NewTextLine(form.f1, field.label)
			textLine.SetFontSize(form.labelFontSize)
			textLine.SetTextColorRGB(form.labelColor)
			textLine.SetLocation(form.x+field.x+xOffset, form.y+yField-yOffset).DrawOn(page)
		}

		textLine := NewTextLine(form.f2, field.value)
		textLine.SetFontSize(form.valueFontSize)
		textLine.SetTextColorRGB(form.valueColor)
		textLine.SetLocation(xOffset+form.x+field.x, form.y+yField-form.f2.GetDescent(form.valueFontSize))
		textLine.DrawOn(page)

		if field.x != 0.0 {
			rowHeight := form.f1.GetAscent(form.labelFontSize) + 3.0*form.f1.GetDescent(form.labelFontSize)
			rowHeight += form.f2.GetAscent(form.valueFontSize) + form.f2.GetDescent(form.valueFontSize)
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
