package pdfjet

/**
 * field.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Field describes field object that is used from the Form class.
// Please see Example_45
type Field struct {
	x              float32
	values         []string
	altDescription []string
	actualText     []string
	format         bool
}

// NewField constructs field object.
func NewField(x float32, values []string, format bool) *Field {
	field := new(Field)
	field.x = x
	field.values = values
	field.altDescription = make([]string, 0)
	field.actualText = make([]string, 0)
	field.format = format
	if values != nil {
		for i := 0; i < len(values); i++ {
			field.altDescription = append(field.altDescription, values[i])
			field.actualText = append(field.actualText, values[i])
		}
	}
	return field
}

// SetAltDescription sets the alt description.
func (field *Field) SetAltDescription(altDescription string) *Field {
	field.altDescription[0] = altDescription
	return field
}

// SetActualText sets the alt description.
func (field *Field) SetActualText(actualText string) *Field {
	field.actualText[0] = actualText
	return field
}
