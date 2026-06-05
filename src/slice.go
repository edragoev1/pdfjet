package pdfjet

/**
 * slice.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

type Slice struct {
	angle float32
	color int32
}

func NewSlice(percent float32, color int32) *Slice {
	slice := new(Slice)
	slice.angle = percent * 3.6
	slice.color = color
	return slice
}
