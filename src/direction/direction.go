// direction.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package direction

type Direction int

// Used to specify the text writing direction in textblock.go
const (
	LeftToRight Direction = iota
	TopToBottom
	BottomToTop
)
