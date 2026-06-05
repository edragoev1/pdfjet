package pdfjet

/**
 * structelem.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

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

// NewStructElem constructor
func NewStructElem() *StructElem {
	structElem := new(StructElem)
	return structElem
}

func (structElem *StructElem) GetPageObjNumber() int {
	return structElem.pageObjNumber
}
