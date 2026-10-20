// graphicsstate.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"

	"github.com/edragoev1/pdfjet/v9/src/blendmode"
	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
)

// GraphicsState is used to create graphics state objects.
type GraphicsState struct {
	strokingAlpha    float32
	nonStrokingAlpha float32
	// "" writes no /BM entry, as before there was a blend mode (suggested by
	// Stefan Ostermann, of his fork of PDFjet, 19 October 2026).
	blendMode blendmode.BlendMode
}

// NewGraphicsState is the constructor.
func NewGraphicsState() *GraphicsState {
	state := new(GraphicsState)
	state.strokingAlpha = 1.0
	state.nonStrokingAlpha = 1.0
	return state
}

// SetAlphaStroking sets the stroking alpha.
func (state *GraphicsState) SetAlphaStroking(strokingAlpha float32) *GraphicsState {
	if strokingAlpha >= 0.0 && strokingAlpha <= 1.0 {
		state.strokingAlpha = strokingAlpha
	}
	return state
}

// GetAlphaStroking returns the stroking alpha.
func (state *GraphicsState) GetAlphaStroking() float32 {
	return state.strokingAlpha
}

// SetAlphaNonStroking sets the non stroking alpha.
func (state *GraphicsState) SetAlphaNonStroking(nonStrokingAlpha float32) *GraphicsState {
	if nonStrokingAlpha >= 0.0 && nonStrokingAlpha <= 1.0 {
		state.nonStrokingAlpha = nonStrokingAlpha
	}
	return state
}

// GetAlphaNonStroking returns the non stroking alpha.
func (state *GraphicsState) GetAlphaNonStroking() float32 {
	return state.nonStrokingAlpha
}

// SetBlendMode sets the blend mode: how what is drawn is mixed with what is
// already on the page. blendmode.Multiply draws like a highlighter pen. The
// blend mode stays until a graphics state sets another one, so set
// blendmode.Normal to go back to painting over. A PDF/A-1 document takes only
// blendmode.Normal. "" sets none. A blend mode that is not one of
// blendmode's panics.
func (state *GraphicsState) SetBlendMode(blendMode blendmode.BlendMode) *GraphicsState {
	// Any text was written as the name, "multiply" or "Multiply /SMask 9 0 R"
	// into the /ExtGState, where the enums of the other ports take only the
	// sixteen (review of 20 October 2026)
	switch blendMode {
	case "", blendmode.Normal, blendmode.Multiply, blendmode.Screen, blendmode.Overlay,
		blendmode.Darken, blendmode.Lighten, blendmode.ColorDodge, blendmode.ColorBurn,
		blendmode.HardLight, blendmode.SoftLight, blendmode.Difference, blendmode.Exclusion,
		blendmode.Hue, blendmode.Saturation, blendmode.Color, blendmode.Luminosity:
	default:
		panic("The blend mode " + strconv.Quote(string(blendMode)) +
			" is not one: use one of package blendmode, as blendmode.Multiply.")
	}
	state.blendMode = blendMode
	return state
}

// GetBlendMode returns the blend mode, or "" if none was set.
func (state *GraphicsState) GetBlendMode() blendmode.BlendMode {
	return state.blendMode
}

// entries returns the entries of the state's /ExtGState dictionary, as the
// page and an imported page write them: the alphas with a dot whatever the
// locale, at most two decimals and no exponent, then the blend mode if it has
// one.
func (state *GraphicsState) entries() string {
	entries := "/CA " + string(fastfloat.ToByteArray(state.strokingAlpha)) +
		" /ca " + string(fastfloat.ToByteArray(state.nonStrokingAlpha))
	if state.blendMode != "" {
		entries += " /BM /" + string(state.blendMode)
	}
	return entries
}
