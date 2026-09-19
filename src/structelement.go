// structelement.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// structElement is a structure element of the structure tree that PDF writes
// for a tagged document: the marked content it refers to and its kids.
type structElement struct {
	objNumber      int
	structure      string
	pageObjNumber  int
	mcid           int            // The marked content, or -1 for an element that groups its kids, like a table row
	attributes     string         // The attributes dictionary, like <</O /Table /Scope /Column>>, or ""
	parent         *structElement // The parent element, or nil for a child of the Document element
	language       string
	actualText     string
	altDescription string
	annotation     *annotationObject
	kids           []*structElement
}

func newStructElement() *structElement {
	return new(structElement)
}

func (element *structElement) getPageObjNumber() int {
	return element.pageObjNumber
}
