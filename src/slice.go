// slice.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Slice is a single slice of a donut or pie chart.
type Slice struct {
	Angle   float32 // The angle of the slice in degrees
	Color   int32   // The 0xRRGGBB color of the slice
	Text    string  // The label of the slice
	Tooltip string  // The tooltip of the slice
}

// NewSlice creates a slice with the angle in degrees, the 0xRRGGBB color, the label and the tooltip.
func NewSlice(angle float32, color int32, text string, tooltip string) *Slice {
	slice := new(Slice)
	slice.Angle = angle
	slice.Color = color
	slice.Text = text
	slice.Tooltip = tooltip
	return slice
}
