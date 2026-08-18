/**
 * round.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package pdfjet

// Round is used in the Chart module.
type Round struct {
	minValue       float32
	maxValue       float32
	numOfGridLines int
}

// NewRound constructor.
func NewRound() *Round {
	round := new(Round)
	return round
}
