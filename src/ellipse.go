package pdfjet

// ellipse.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Ellipse represents an ellipse shape, implemented as a full circle arc.
type Ellipse struct {
	Arc // Embedding enables inheriting Arc's methods
}

// NewEllipse creates a new Ellipse instance with default values
// (a complete 360-degree arc representing an ellipse/circle).
func NewEllipse() *Ellipse {
	ellipse := new(Ellipse)
	ellipse.SetStartAngle(0.0)
	ellipse.SetSweepDegreesCW(360.0)
	return ellipse
}
