// pagesize.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pagesize defines the PageSize type, the width and height of a page
// in points. The Portrait and Landscape functions of the a3, a4, a5, b5,
// executive, legal, letter and tabloid packages return it.
package pagesize

// PageSize is the width and height of a page in points; 1 point is 1/72 inch.
// Its fields are unexported, so a page size cannot be changed once it is
// created. Use NewPageSize for a size that has no package.
type PageSize struct {
	width  float32
	height float32
}

// NewPageSize returns the page size with the specified width and height in points.
func NewPageSize(width, height float32) PageSize {
	return PageSize{width: width, height: height}
}

// GetWidth returns the width of the page in points.
func (pageSize PageSize) GetWidth() float32 {
	return pageSize.width
}

// GetHeight returns the height of the page in points.
func (pageSize PageSize) GetHeight() float32 {
	return pageSize.height
}
