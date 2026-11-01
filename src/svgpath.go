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
	evenOdd     bool         // Filled, or clipped to, with the even-odd rule
	lineCap     capstyle.CapStyle
	lineJoin    joinstyle.JoinStyle
	miterLimit  float32
	fillAlpha   float32
	strokeAlpha float32
	dashArray   []float32 // The dashes and the gaps, in the space of the svg element; nil is solid
	dashOffset  float32
	strokeFirst bool        // paint-order: the stroke drawn before the fill
	clips       []*svgPath  // The clip paths it is drawn inside, all of them
	choices     []svgChoice // The children of the switches it is in
	text        *svgText    // A text, drawn instead of the operations
	parts       []*svgPath  // Of a clip path that clips with texts: one for each, with its text
	glyphs      int         // Of such a clip path: what is written for its texts, as operations
}

// isChosen reports whether it is in the children chosen of the switches it is in.
func (path *svgPath) isChosen() bool {
	return svgAreChosen(path.choices)
}

// svgAreChosen reports whether each of the choices is the child chosen.
func svgAreChosen(choices []svgChoice) bool {
	for _, choice := range choices {
		if choice.of.chosen != choice.child {
			return false
		}
	}
	return true
}
