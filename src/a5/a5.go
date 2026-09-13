// a5.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package a5 defines the A5 page size in points.
package a5

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the A5 page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(420.0, 595.0)
}

// Landscape returns the A5 page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(595.0, 420.0)
}
