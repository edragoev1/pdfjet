// letter.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package letter defines the letter page size in points.
package letter

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the letter page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(612.0, 792.0)
}

// Landscape returns the letter page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(792.0, 612.0)
}
