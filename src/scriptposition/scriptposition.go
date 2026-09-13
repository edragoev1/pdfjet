// scriptposition.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package scriptposition defines the positions of text: normal, subscript and
// superscript.
package scriptposition

// ScriptPosition specifies whether text is drawn as normal text, as a
// subscript or as a superscript.
type ScriptPosition int

// The script positions.
const (
	Normal ScriptPosition = iota
	Subscript
	Superscript
)
