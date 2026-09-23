// gs1.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package datamatrix

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/internal/gs1"
)

// gs1ElementString returns the element string of the GS1 data written as
// people read it: the Application Identifiers without their parentheses, each
// followed by its data, and GS after a field of no set length that another
// follows. It panics if the data is not GS1; see gs1.Parse.
func gs1ElementString(str string) string {
	var sb strings.Builder
	for _, field := range gs1.Parse(str) {
		sb.WriteString(field.AI)
		sb.WriteString(field.Data)
		if field.Separator {
			sb.WriteString("\x1d") // GS
		}
	}
	return sb.String()
}
