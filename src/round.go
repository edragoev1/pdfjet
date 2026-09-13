// round.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// roundedRange is used in the Chart module.
type roundedRange struct {
	minValue       float32
	maxValue       float32
	numOfGridLines int
}

// newRoundedRange constructor.
func newRoundedRange() *roundedRange {
	round := new(roundedRange)
	return round
}
