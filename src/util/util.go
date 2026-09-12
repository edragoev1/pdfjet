// util.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package util provides utility functions.
package util

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/content"
)

// ReadLines reads the lines of a UTF-8 text file, without carriage returns.
// It exits the program if the file cannot be read.
func ReadLines(filePath string) []string {
	lines := make([]string, 0)
	var buffer strings.Builder
	for _, ch := range content.OfTextFile(filePath) {
		if ch == '\n' {
			lines = append(lines, buffer.String())
			buffer.Reset()
		} else {
			buffer.WriteRune(ch)
		}
	}
	if buffer.Len() > 0 {
		lines = append(lines, buffer.String())
	}
	return lines
}
