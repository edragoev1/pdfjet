// drawable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Drawable is the interface of the components that can be drawn on a page,
// which a Container or an OptionalContentGroup can hold.
// @author Mark Paxton, Evgeni Dragoev
type Drawable interface {
	// DrawOn draws the component implementing this interface on the PDF page.
	// @param page the page to draw on.
	// @return x and y coordinates of the bottom right corner of this component.
	DrawOn(page *Page) []float32

	// SetLocation sets the location of the component on the page.
	// It returns the component as a Drawable, so in a chain of setter calls
	// SetLocation goes last, right before DrawOn.
	// @param x the x coordinate of the top left corner of the component.
	// @param y the y coordinate of the top left corner of the component.
	// @return this component.
	SetLocation(x, y float32) Drawable
}
