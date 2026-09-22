// relationship.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package relationship defines how a file attached to the document relates to
// what the document shows.
package relationship

// Relationship says how a file attached to the document relates to what the
// document shows. A document of PDF/A-3 names one for each file it carries,
// and a reader shows it to the person who opens the document.
type Relationship string

// Constants used to specify how an attached file relates to the document.
const (
	Source      Relationship = "/Source"      // The file is the source the document was made from
	Data        Relationship = "/Data"        // The file holds the data behind what the document shows, such as the numbers of a chart
	Alternative Relationship = "/Alternative" // The file is the same content in another form, such as the invoice of a bill in XML
	Supplement  Relationship = "/Supplement"  // The file adds to the document without being part of what it shows
	Unspecified Relationship = "/Unspecified" // The relationship is none of the others, or is not known
)
