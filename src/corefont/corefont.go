// corefont.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package corefont defines the metrics of the 14 standard PDF fonts.
package corefont

// CoreFont structure.
type CoreFont struct {
	Name               string
	Notice             string
	BBoxLLx            int16
	BBoxLLy            int16
	BBoxURx            int16
	BBoxURy            int16
	UnderlinePosition  int16
	UnderlineThickness int16
	Metrics            [][]int
}
