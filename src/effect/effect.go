// effect.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package effect defines the text effects: normal, subscript and superscript.
package effect

// Effect specifies the text effect.
type Effect int

// Used to specify the text effects.
const (
	Normal Effect = iota
	Subscript
	Superscript
)
