// compliance.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package compliance defines the PDF/UA and PDF/A compliance levels.
package compliance

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
)
