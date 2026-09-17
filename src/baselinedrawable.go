// baselinedrawable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// BaselineDrawable is the interface of a drawable whose location is the
// baseline of its text, not the top left corner of a box: a line of text,
// such as TextLine and CompositeTextLine.
//
// A Cell draws such a drawable where it draws its own text, on the baseline
// that its vertical alignment asks for, and makes room for the ascent and the
// descent below. A drawable that is not one of these is placed by its top
// left corner, at the padding of the cell.
type BaselineDrawable interface {
	Drawable

	// GetAscent returns how far above its baseline this drawable reaches, in points.
	GetAscent() float32

	// GetDescent returns how far below its baseline this drawable reaches, in points.
	GetDescent() float32

	// GetWidth returns the width of this drawable, in points.
	GetWidth() float32
}
