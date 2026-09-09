// graphicsstate.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// GraphicsState is used to create graphics state objects.
type GraphicsState struct {
	strokingAlpha    float32
	nonStrokingAlpha float32
}

// NewGraphicsState is the constructor.
func NewGraphicsState() *GraphicsState {
	state := new(GraphicsState)
	state.strokingAlpha = 1.0
	state.nonStrokingAlpha = 1.0
	return state
}

// SetAlphaStroking sets the stroking alpha.
func (state *GraphicsState) SetAlphaStroking(strokingAlpha float32) {
	if strokingAlpha >= 0.0 && strokingAlpha <= 1.0 {
		state.strokingAlpha = strokingAlpha
	}
}

// GetAlphaStroking returns the stroking alpha.
func (state *GraphicsState) GetAlphaStroking() float32 {
	return state.strokingAlpha
}

// SetAlphaNonStroking sets the non stroking alpha.
func (state *GraphicsState) SetAlphaNonStroking(nonStrokingAlpha float32) {
	if nonStrokingAlpha >= 0.0 && nonStrokingAlpha <= 1.0 {
		state.nonStrokingAlpha = nonStrokingAlpha
	}
}

// GetAlphaNonStroking returns the non stroking alpha.
func (state *GraphicsState) GetAlphaNonStroking() float32 {
	return state.nonStrokingAlpha
}
