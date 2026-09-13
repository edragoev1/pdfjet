// destination.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Destination is a destination on a page, made by Page.AddDestination.
type Destination struct {
	name          string
	xPosition     float32
	yPosition     float32
	pageObjNumber int
}

// newDestination creates a destination on a page for Page.AddDestination.
//
// @param name the name of this destination object.
// @param xPosition the x coordinate of the top left corner.
// @param yPosition the y coordinate of the top left corner.
func newDestination(name string, xPosition float32, yPosition float32) *Destination {
	destination := new(Destination)
	destination.name = name
	destination.xPosition = xPosition
	destination.yPosition = yPosition
	return destination
}

// setPageObjNumber sets the page object number.
func (destination *Destination) setPageObjNumber(pageObjNumber int) *Destination {
	destination.pageObjNumber = pageObjNumber
	return destination
}
