// drawable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Drawable is interface that is required for components that can be drawn on a PDF page as part of Optional Content Group.
// @author Mark Paxton, Evgeni Dragoev
//
// Unlike the Java, C# and Swift ports, it does not declare SetLocation: each
// type's SetLocation returns that type so calls can be chained, and a Go type
// only satisfies an interface with an exact signature match.
type Drawable interface {
	// DrawOn draws the component implementing this interface on the PDF page.
	// @param page the page to draw on.
	// @return x and y coordinates of the bottom right corner of this component.
	DrawOn(page *Page) [2]float32
}
