// structelement.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "strings"

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
	kids           []int // The object numbers of the kids
	// The marked contents of an element that holds several of them, like a
	// paragraph whose words are drawn one at a time; and, in their order among
	// them, the elements of the words that are links, as the negative of their
	// object numbers.
	mcids []int
	// open is true for an element that a drawable goes on adding to after the
	// page it was made on is written, like the Table of a table that runs over
	// pages. It is written when the document is completed; every other element
	// is written with its page and let go of.
	open bool
}

func newStructElement() *structElement {
	return new(structElement)
}

func (element *structElement) getPageObjNumber() int {
	return element.pageObjNumber
}

// The structure types that PDF makes inline, which stand in a paragraph; one
// that is a kid of an element that groups others, like the Document, stands
// as a block, and says so with the attribute Placement Block, or PAC warns of
// it as a possibly inappropriate use: a figure, a link or an annotation drawn
// on its own, and not inside a paragraph.
var inlineLevelStructures = map[string]bool{
	"Figure": true, "Formula": true, "Form": true, "Note": true, "Link": true, "Annot": true,
}

// The structure types that group others and hold blocks, not text.
var groupingStructures = map[string]bool{
	"Document": true, "Part": true, "Art": true, "Sect": true, "Div": true, "BlockQuote": true,
	"Caption": true, "TOC": true, "TOCI": true, "Index": true, "NonStruct": true, "Private": true,
}

// placedAsBlock tells whether the element is of an inline type and stands as
// a block, a kid of the Document or of another element that groups others.
func placedAsBlock(element *structElement) bool {
	if !inlineLevelStructures[element.structure] {
		return false
	}
	return element.parent == nil || groupingStructures[element.parent.structure]
}

// withPlacementBlock returns the attributes with Placement Block among those
// of the owner Layout: in the Layout attributes the element has, like the
// BBox of a figure, or in their own dictionary beside attributes of another
// owner.
func withPlacementBlock(attributes string) string {
	const layout = "<</O /Layout "
	switch {
	case attributes == "":
		return "<</O /Layout /Placement /Block>>"
	case strings.HasPrefix(attributes, layout):
		return layout + "/Placement /Block " + attributes[len(layout):]
	}
	return "[" + attributes + " <</O /Layout /Placement /Block>>]"
}
