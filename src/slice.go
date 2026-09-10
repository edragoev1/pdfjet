// slice.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Slice is a single slice of a donut or pie chart.
type Slice struct {
	angle   float32
	color   int32
	text    string
	tooltip string
}

// NewSlice creates a slice with the angle in degrees, the 0xRRGGBB color, the label and the tooltip.
func NewSlice(angle float32, color int32, text string, tooltip string) *Slice {
	slice := new(Slice)
	slice.angle = angle
	slice.color = color
	slice.text = text
	slice.tooltip = tooltip
	return slice
}
