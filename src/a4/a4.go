// a4.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package a4 defines the A4 page size in points.
package a4

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the A4 page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(595.0, 842.0)
}

// Landscape returns the A4 page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(842.0, 595.0)
}
