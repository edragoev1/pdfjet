package pdfjet

/**
 * graphicsstate.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

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
