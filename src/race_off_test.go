// race_off_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build !race

package pdfjet

// Without the race detector the fuzz targets' limits are as they are; see
// race_on_test.go.
const (
	fuzzRaceTime  = 1
	fuzzRaceAlloc = 1
)
