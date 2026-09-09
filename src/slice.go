// slice.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

type Slice struct {
	angle   float32
	color   int32
	text    string
	tooltip string
}

func NewSlice(angle float32, color int32, text string, tooltip string) *Slice {
	slice := new(Slice)
	slice.angle = angle
	slice.color = color
	slice.text = text
	slice.tooltip = tooltip
	return slice
}
