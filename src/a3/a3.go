// a3.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package a3 defines the A3 page size in points.
package a3

import "github.com/edragoev1/pdfjet/v9/src/pagesize"

// Portrait returns the A3 page size in portrait orientation.
func Portrait() pagesize.PageSize {
	return pagesize.NewPageSize(842.0, 1191.0)
}

// Landscape returns the A3 page size in landscape orientation.
func Landscape() pagesize.PageSize {
	return pagesize.NewPageSize(1191.0, 842.0)
}
