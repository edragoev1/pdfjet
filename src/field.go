/**
 * field.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package pdfjet

// Field describes field object that is used from the Form class.
// Please see Example_45
type Field struct {
	x     float32
	label string
	value string
}

// NewField constructs field object.
func NewField(x float32, label, value string) *Field {
	field := new(Field)
	field.x = x
	field.label = label
	field.value = value
	return field
}
