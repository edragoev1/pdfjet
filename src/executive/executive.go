// executive.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package executive defines the executive page size in points.
package executive

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the executive page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(522.0, 756.0)
}

// Landscape returns the executive page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(756.0, 522.0)
}
