// state.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
)

// savedState describes the collection of drawing parameters.
type savedState struct {
	pen               [3]float32
	penWritten        bool
	brush             [3]float32
	brushWritten      bool
	penWidth          float32
	penWidthWritten   bool
	writtenFont       *Font
	writtenFontSize   float32
	lineCapStyle      capstyle.CapStyle
	lineJoinStyle     joinstyle.JoinStyle
	strokeDashPattern string
	// The height of the page, which Transform divides by the vertical scale
	// and which every y coordinate is measured from.
	height float32
}

// newSavedState constructs state objects.
func newSavedState(
	brush [3]float32,
	brushWritten bool,
	pen [3]float32,
	penWritten bool,
	penWidth float32,
	penWidthWritten bool,
	writtenFont *Font,
	writtenFontSize float32,
	lineCapStyle capstyle.CapStyle,
	lineJoinStyle joinstyle.JoinStyle,
	strokeDashPattern string,
	height float32) *savedState {
	state := new(savedState)
	state.pen = [3]float32{pen[0], pen[1], pen[2]}
	state.brush = [3]float32{brush[0], brush[1], brush[2]}
	state.penWritten = penWritten
	state.brushWritten = brushWritten
	state.penWidth = penWidth
	state.penWidthWritten = penWidthWritten
	state.writtenFont = writtenFont
	state.writtenFontSize = writtenFontSize
	state.lineCapStyle = lineCapStyle
	state.lineJoinStyle = lineJoinStyle
	state.strokeDashPattern = strokeDashPattern
	state.height = height
	return state
}

// GetPen returns the penColor.
func (state *savedState) getPen() [3]float32 {
	return state.pen
}

// GetBrush returns the brushColor.
func (state *savedState) getBrush() [3]float32 {
	return state.brush
}

// GetPenWidth returns the penColor width.
func (state *savedState) getPenWidth() float32 {
	return state.penWidth
}

// GetLineCapStyle returns the line cap style.
func (state *savedState) getLineCapStyle() capstyle.CapStyle {
	return state.lineCapStyle
}

// GetLineJoinStyle returns the line join style.
func (state *savedState) getLineJoinStyle() joinstyle.JoinStyle {
	return state.lineJoinStyle
}

// GetStrokeDashPattern returns the line pattern.
func (state *savedState) getStrokeDashPattern() string {
	return state.strokeDashPattern
}

// getHeight returns the height of the page.
func (state *savedState) getHeight() float32 {
	return state.height
}
