// svgpath.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
)

// svgPath holds a path or a shape of an SVG file, as PDF path operations in
// the space of the svg element, and what it is drawn with.
type svgPath struct {
	operations  []*svgPathOp // The PDF path operations
	fill        int32        // The fill color, or color.Transparent for none
	stroke      int32        // The stroke color, or color.Transparent for none
	strokeWidth float32      // The stroke width, in the space of the svg element
	evenOdd     bool         // Filled with the even-odd rule
	lineCap     capstyle.CapStyle
	lineJoin    joinstyle.JoinStyle
	fillAlpha   float32
	strokeAlpha float32
}
