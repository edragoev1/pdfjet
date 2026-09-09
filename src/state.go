// state.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// State describes the collection of drawing parameters.
type State struct {
	pen               [3]float32
	brush             [3]float32
	penWidth          float32
	lineCapStyle      int
	lineJoinStyle     int
	strokeDashPattern string
}

// NewState constructs state objects.
func NewState(
	pen [3]float32,
	brush [3]float32,
	penWidth float32,
	lineCapStyle int,
	lineJoinStyle int,
	strokeDashPattern string) *State {
	state := new(State)
	state.pen = [3]float32{pen[0], pen[1], pen[2]}
	state.brush = [3]float32{brush[0], brush[1], brush[2]}
	state.penWidth = penWidth
	state.lineCapStyle = lineCapStyle
	state.lineJoinStyle = lineJoinStyle
	state.strokeDashPattern = strokeDashPattern
	return state
}

// GetPen returns the penColor.
func (state *State) GetPen() [3]float32 {
	return state.pen
}

// GetBrush returns the brushColor.
func (state *State) GetBrush() [3]float32 {
	return state.brush
}

// GetPenWidth returns the penColor width.
func (state *State) GetPenWidth() float32 {
	return state.penWidth
}

// GetLineCapStyle returns the line cap style.
func (state *State) GetLineCapStyle() int {
	return state.lineCapStyle
}

// GetLineJoinStyle returns the line join style.
func (state *State) GetLineJoinStyle() int {
	return state.lineJoinStyle
}

// GetStrokeDashPattern returns the line pattern.
func (state *State) GetStrokeDashPattern() string {
	return state.strokeDashPattern
}
