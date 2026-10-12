// race_on_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build race

package pdfjet

// Under the race detector, which makes the code slower and allocates for its
// shadow memory, the fuzz targets' limits of time and memory are larger: a
// seed took 242 ms and 120 MB of limits of 200 ms and 96 MB (review of
// 12 October 2026, C5).
const (
	fuzzRaceTime  = 8
	fuzzRaceAlloc = 2
)
