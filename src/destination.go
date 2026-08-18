/**
 * destination.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package pdfjet

// Destination is used to create PDF destination objects.
type Destination struct {
	name          string
	xPosition     float32
	yPosition     float32
	pageObjNumber int
}

// NewDestination creates new destination objects.
//
// @param name the name of this destination object.
// @param xPosition the x coordinate of the top left corner.
// @param yPosition the y coordinate of the top left corner.
func NewDestination(name string, xPosition float32, yPosition float32) *Destination {
	destination := new(Destination)
	destination.name = name
	destination.yPosition = xPosition
	destination.yPosition = yPosition
	return destination
}

// SetPageObjNumber sets the page object number.
func (destination *Destination) SetPageObjNumber(pageObjNumber int) {
	destination.pageObjNumber = pageObjNumber
}
