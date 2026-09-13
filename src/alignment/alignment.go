// alignment.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package alignment defines the horizontal and vertical alignments.
package alignment

// Alignment specifies the horizontal or vertical alignment.
type Alignment int

// Left, Right, Center and Justify align horizontally; Top, Center and Bottom
// align vertically.
const (
	Left Alignment = iota
	Right
	Center
	Justify
	Top
	Bottom
)
