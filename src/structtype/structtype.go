/**
 * structtype.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package structtype

// Structure Elements for Tagged PDF
const (
	Document = "Document"
	Part     = "Part"
	Div      = "Div"
	Sect     = "Sect"

	H1 = "H1"
	H2 = "H2"
	H3 = "H3"
	H4 = "H4"
	H5 = "H5"
	H6 = "H6"

	P     = "P"
	Title = "Title"
	Lbl   = "Lbl"

	Span   = "Span"
	Em     = "Em"
	Strong = "Strong"

	Link  = "Link"
	Annot = "Annot"

	L  = "L"  // List
	LI = "LI" // List Item

	// Table elements
	Table   = "Table"
	TR      = "TR"    // Table Row
	TH      = "TH"    // Table Header
	TD      = "TD"    // Table Data
	THead   = "THead" // Table Header group
	TBody   = "TBody" // Table Body group
	TFoot   = "TFoot" // Table Footer group
	Caption = "Caption"

	// Figure and special elements
	Figure   = "Figure"
	Artifact = "Artifact"
)
