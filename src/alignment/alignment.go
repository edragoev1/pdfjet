// alignment.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package alignment

type Alignment int

// Used to specify the text alignment in textblock.go
const (
	Top = iota
	Bottom
	Left
	Right
	Center
	Justify
)
