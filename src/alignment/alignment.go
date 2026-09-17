// alignment.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package alignment defines the horizontal and vertical alignments.
package alignment

// Alignment specifies the horizontal or vertical alignment. It is an int32,
// so that the three of them a Cell holds cost 4 bytes each and not 8.
type Alignment int32

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
