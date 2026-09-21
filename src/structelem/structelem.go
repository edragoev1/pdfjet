// structelem.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package structelem has the structure element types of a tagged (PDF/UA)
// document, as in ISO 32000-1 section 14.8.4. A TextLine is a P by default;
// see TextLine.SetStructureType.
package structelem

// StructElem is a structure element type, with the name written to the PDF.
type StructElem string

const (
	Document StructElem = "Document"
	Part                = "Part"
	Div                 = "Div"
	Sect                = "Sect"
	H1                  = "H1"
	H2                  = "H2"
	H3                  = "H3"
	H4                  = "H4"
	H5                  = "H5"
	H6                  = "H6"
	P                   = "P"
	Title               = "Title"
	Lbl                 = "Lbl"
	Span                = "Span"
	Em                  = "Em"
	Strong              = "Strong"
	Link                = "Link"
	Annot               = "Annot"
	L                   = "L"
	LI                  = "LI"
	LBody               = "LBody"
	Table               = "Table"
	TR                  = "TR"
	TH                  = "TH"
	TD                  = "TD"
	THead               = "THead"
	TBody               = "TBody"
	TFoot               = "TFoot"
	Caption             = "Caption"
	Figure              = "Figure"
	Artifact            = "Artifact"
)
