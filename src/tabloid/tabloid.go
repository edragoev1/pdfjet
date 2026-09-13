// tabloid.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package tabloid defines the tabloid page size in points.
package tabloid

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the tabloid page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(792.0, 1224.0)
}

// Landscape returns the tabloid page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(1224.0, 792.0)
}
