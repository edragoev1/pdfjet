// joinstyle.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package joinstyle defines the line join styles.
package joinstyle

// Used to specify the join style when joining two lines.
// See the Page and Line classes for more details.
const (
	Miter = iota
	Round
	Bevel
)
