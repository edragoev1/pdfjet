/**
 * svgpath.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package pdfjet

import "github.com/edragoev1/pdfjet/src/color"

type SVGPath struct {
	data        string    // The SVG path data
	operations  []*PathOp // The PDF path operations
	fill        int32     // The fill color
	stroke      int32     // The stroke color
	strokeWidth float32   // The stroke width
}

func NewSVGPath() *SVGPath {
	path := new(SVGPath)
	path.fill = color.Transparent
	path.stroke = color.Transparent
	path.strokeWidth = 0.0
	return path
}
