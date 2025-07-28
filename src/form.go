package pdfjet

/**
 * form.go
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
	"strings"

	"github.com/edragoev1/pdfjet/src/color"
)

// Form describes form object.
// Please see Example_45
type Form struct {
	fields        []*Field
	x             float32
	y             float32
	f1, f2        *Font
	labelFontSize float32 // = 8f
	valueFontSize float32 // = 10f
	numberOfRows  int
	rowWidth      float32 // = 500f
	rowHeight     float32 // = 12f
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

// SetRowWidth sets the row width.
func (form *Form) SetRowWidth(rowWidth float32) *Form {
	form.rowWidth = rowWidth
	return form
}

// SetRowHeight sets the height of the rows.
func (form *Form) SetRowHeight(rowHeight float32) *Form {
	form.rowHeight = rowHeight
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

// SetValueFontSize sets the font size for falue value text.
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

// DrawOn draws form Form on the specified page.
// @param page the page to draw form form on.
// @return x and y coordinates of the bottom right corner of form component.
func (form *Form) DrawOn(page *Page) []float32 {
	for _, field := range form.fields {
		if field.format {
			field.values = form.format(field.values[0], field.values[1], form.f2, form.rowWidth)
			field.altDescription = make([]string, 0)
			field.actualText = make([]string, 0)
			for _, value := range field.values {
				field.altDescription = append(field.altDescription, value)
				field.actualText = append(field.actualText, value)
			}
		}
		if field.x == 0.0 {
			form.numberOfRows += len(field.values)
		}
	}

	if form.numberOfRows == 0 {
		return []float32{form.x, form.y}
	}

	boxHeight := form.rowHeight * float32(form.numberOfRows)
	box := NewBox()
	box.SetLocation(form.x, form.y)
	box.SetSize(form.rowWidth, boxHeight)
	box.DrawOn(page)

	var yField float32
	var yRow float32
	rowSpan := 1
	for _, field := range form.fields {
		if field.x == 0.0 {
			yRow += float32(rowSpan) * form.rowHeight
			rowSpan = len(field.values)
		}
		yField = yRow

		var font *Font
		var fontSize float32
		var color int32
		var altDescription string
		var actualText string
		i := 0
		for i < len(field.values) {
			if i == 0 {
				font = form.f1
				fontSize = form.labelFontSize
				color = form.labelColor
				altDescription = field.altDescription[i]
				actualText = field.actualText[i]
			} else {
				font = form.f2
				fontSize = form.valueFontSize
				color = form.valueColor
				altDescription = field.altDescription[i] + ","
				actualText = field.actualText[i] + ","
			}

			textLine := NewTextLine(font, field.values[i])
			textLine.SetFontSize(fontSize)
			textLine.SetColor(color)
			textLine.PlaceIn(box, field.x+font.descent, yField-font.descent)
			textLine.SetAltDescription(altDescription)
			textLine.SetActualText(actualText)
			textLine.DrawOn(page)

			if i == len(field.values)-1 {
				line := NewLine(0.0, 0.0, form.rowWidth, 0.0)
				line.PlaceIn(box, 0.0, yField)
				line.DrawOn(page)
				if field.x != 0.0 {
					line = NewLine(0.0, -(float32(len(field.values)-1) * form.rowHeight), 0.0, 0.0)
					line.PlaceIn(box, field.x, yField)
					line.DrawOn(page)
				}
			}
			yField += form.rowHeight

			i++
		}
	}

	return []float32{form.x + form.rowWidth, form.y + boxHeight}
}

// format formats the form text.
func (form *Form) format(title, text string, font *Font, width float32) []string {
	original := strings.Fields(text)
	lines := make([]string, 0)
	var buf strings.Builder
	for i := 0; i < len(original); i++ {
		line := original[i]
		if font.stringWidth(line) < width {
			lines = append(lines, line)
			continue
		}
		buf.Reset()

		runes := []rune(line)
		for j := 0; j < len(runes); j++ {
			buf.WriteRune(runes[j])
			if font.stringWidth(buf.String()) > (width - font.stringWidth("   ")) {
				for j > 0 && runes[j] != ' ' {
					j--
				}
				str := strings.TrimSpace(line[0:j])
				lines = append(lines, str)
				buf.Reset()
				for j < len(runes) && runes[j] == ' ' {
					j++
				}
				line = line[j:]
				j = 0
			}
		}

		if line != "" {
			lines = append(lines, line)
		}
	}

	count := len(lines)
	data := make([]string, count+1)
	data[0] = title
	for i := 0; i < count; i++ {
		data[i+1] = lines[i]
	}

	return data
}
