// svgpath.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "github.com/edragoev1/pdfjet/v9/src/color"

// svgPath holds one svgParser path with its PDF operations, colors and stroke width.
type svgPath struct {
	data        string       // The svgParser path data
	operations  []*svgPathOp // The PDF path operations
	fill        int32        // The fill color
	stroke      int32        // The stroke color
	fillNone    bool         // fill="none": not filled, whatever the svg element says
	strokeNone  bool         // stroke="none": not stroked, whatever the svg element says
	strokeWidth float32      // The stroke width
}

// newSVGPath creates a path with transparent fill and stroke.
func newSVGPath() *svgPath {
	path := new(svgPath)
	path.fill = color.Transparent
	path.stroke = color.Transparent
	path.strokeWidth = 0.0
	return path
}
