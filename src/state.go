package pdfjet

/**
 * state.go
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
