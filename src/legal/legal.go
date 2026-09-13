// legal.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package legal defines the legal page size in points.
package legal

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the legal page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(612.0, 1008.0)
}

// Landscape returns the legal page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(1008.0, 612.0)
}
