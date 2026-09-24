// compliance.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package compliance defines the PDF/UA and PDF/A compliance levels.
package compliance

import "strconv"

// Compliance specifies the PDF/UA or PDF/A compliance level.
type Compliance int

// Used to set PDF/UA and PDF/A compliance.
// See PDF.SetCompliance.
const (
	PDF_1_7 Compliance = iota // Do not remove PDF_1_7!
	PDF_UA_1
	PDF_A_1A
	PDF_A_1B
	PDF_A_2A
	PDF_A_2B
	PDF_A_3A
	PDF_A_3B
	// PDF_A_3A_UA_1 is PDF/A-3a and PDF/UA-1 in one document: tagged as
	// PDF/UA asks, and able to carry files, as PDF/A-3 is.
	PDF_A_3A_UA_1
)

// The names of the compliance levels, in the order of the constants.
var names = [...]string{
	"PDF_1_7", "PDF_UA_1",
	"PDF_A_1A", "PDF_A_1B",
	"PDF_A_2A", "PDF_A_2B",
	"PDF_A_3A", "PDF_A_3B",
	"PDF_A_3A_UA_1",
}

// String returns the name of the compliance level, as the messages of the
// library and the other ports name it.
func (level Compliance) String() string {
	if level < 0 || int(level) >= len(names) {
		return "Compliance(" + strconv.Itoa(int(level)) + ")"
	}
	return names[level]
}
