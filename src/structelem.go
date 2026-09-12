// structelem.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// StructElem is used to create PDF structure element objects.
type StructElem struct {
	objNumber      int
	structure      string
	pageObjNumber  int
	mcid           int
	language       string
	actualText     string
	altDescription string
	annotation     *Annotation
}

// newStructElem constructor
func newStructElem() *StructElem {
	structElem := new(StructElem)
	return structElem
}

// getPageObjNumber returns the object number of the page this element is on.
func (structElem *StructElem) getPageObjNumber() int {
	return structElem.pageObjNumber
}
