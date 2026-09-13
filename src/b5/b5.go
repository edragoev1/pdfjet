// b5.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package b5 defines the B5 page size, the ISO 216 B5 of 176 by 250 mm, in points.
package b5

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the B5 page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(499.0, 709.0)
}

// Landscape returns the B5 page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(709.0, 499.0)
}
