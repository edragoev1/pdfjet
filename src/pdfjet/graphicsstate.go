package pdfjet

/**
 * graphicsstate.go
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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
