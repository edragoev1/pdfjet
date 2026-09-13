// mark.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package mark defines the check marks of a CheckBox.
package mark

// Mark specifies the check mark of a CheckBox.
type Mark int

// Constants used to specify the check mark in CheckBox.
const (
	Uncheck Mark = iota
	Check
	X
)
