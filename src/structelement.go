// structelement.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// structElement is a structure element of the structure tree that PDF writes
// for a tagged document: the marked content it refers to.
type structElement struct {
	objNumber      int
	structure      string
	pageObjNumber  int
	mcid           int
	language       string
	actualText     string
	altDescription string
	annotation     *annotationObject
}

func newStructElement() *structElement {
	return new(structElement)
}

func (element *structElement) getPageObjNumber() int {
	return element.pageObjNumber
}
