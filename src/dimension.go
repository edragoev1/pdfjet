// dimension.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Dimension encapsulates the width and height of a component.
type Dimension struct {
	w float32
	h float32
}

// NewDimension Constructor for creating dimension objects.
//
// @param width the width.
// @param height the height.
func NewDimension(width, height float32) *Dimension {
	dimension := new(Dimension)
	dimension.w = width
	dimension.h = height
	return dimension
}

// GetWidth gets the width of the component.
func (dimension *Dimension) GetWidth() float32 {
	return dimension.w
}

// GetHeight gets the height of the component.
func (dimension *Dimension) GetHeight() float32 {
	return dimension.h
}
