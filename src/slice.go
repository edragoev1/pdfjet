// slice.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Slice is a slice of a DonutChart: a value, a color and a label. The slice's
// share of the sum of the values of the chart sets its angle.
type Slice struct {
	value float32
	color int32
	text  string
}

// NewSlice creates a slice with its value, above 0, the 0xRRGGBB color and the
// label drawn next to it. The chart draws the value's share of the sum.
func NewSlice(value float32, color int32, text string) *Slice {
	slice := new(Slice)
	slice.value = value
	slice.color = color
	slice.text = text
	return slice
}
