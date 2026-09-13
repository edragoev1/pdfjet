// capstyle.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package capstyle defines the line cap styles.
package capstyle

// CapStyle specifies the cap style of a line.
type CapStyle int

// Constants used to specify the cap style of a line.
// See the Line class for more information.
const (
	Butt CapStyle = iota
	Round
	ProjectingSquare
)
