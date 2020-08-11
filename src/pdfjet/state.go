package pdfjet

/**
 * state.go
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

// State describes the collection of drawing parameters.
type State struct {
	pen           [3]float32
	brush         [3]float32
	penWidth      float32
	lineCapStyle  int
	lineJoinStyle int
	linePattern   string
}

// NewState constructs state objects.
func NewState(
	pen [3]float32,
	brush [3]float32,
	penWidth float32,
	lineCapStyle int,
	lineJoinStyle int,
	linePattern string) *State {
	state := new(State)
	state.pen = [3]float32{pen[0], pen[1], pen[2]}
	state.brush = [3]float32{brush[0], brush[1], brush[2]}
	state.penWidth = penWidth
	state.lineCapStyle = lineCapStyle
	state.lineJoinStyle = lineJoinStyle
	state.linePattern = linePattern
	return state
}

// GetPen returns the pen.
func (state *State) GetPen() [3]float32 {
	return state.pen
}

// GetBrush returns the brush.
func (state *State) GetBrush() [3]float32 {
	return state.brush
}

// GetPenWidth returns the pen width.
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

// GetLinePattern returns the line pattern.
func (state *State) GetLinePattern() string {
	return state.linePattern
}
