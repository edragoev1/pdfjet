// drawable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Drawable is interface that is required for components that can be drawn on a PDF page as part of Optional Content Group.
// @author Mark Paxton, Evgeni Dragoev
type Drawable interface {
	// SetPosition Draws the component implementing this interface on the PDF page.
	// @param page the page to draw on.
	// @return x and y coordinates of the bottom right corner of this component.
	SetPosition(x, y float32)
	DrawOn(page *Page) [2]float32
}
