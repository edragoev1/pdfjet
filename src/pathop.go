package pdfjet

/**
 * pathop.go
 *
Copyright 2023 Innovatics Inc.

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

import "fmt"

// Path is used to create path objects.
// The path objects may consist of lines, splines or both.
// Please see Example_02.
type PathOp struct {
	cmd  rune
	x1q  float32 // Original quadratic control
	y1q  float32 // point coordinates
	x1   float32 // Control point x1
	y1   float32 // Control point y1
	x2   float32 // Control point x2
	y2   float32 // Control point y2
	x    float32 // Initial point x
	y    float32 // Initial point y
	args []string
}

func NewPathOp(cmd rune) *PathOp {
	pathOp := new(PathOp)
	pathOp.cmd = cmd
	pathOp.args = make([]string, 0)
	return pathOp
}

func NewPathOpXY(cmd rune, x, y float32) *PathOp {
	pathOp := new(PathOp)
	pathOp.cmd = cmd
	pathOp.x = x
	pathOp.y = y
	pathOp.args = make([]string, 0)
	return pathOp
}

func (path *PathOp) addCubicPoints(x1, y1, x2, y2, x, y float32) {
	path.x1 = x1
	path.y1 = y1
	path.x2 = x2
	path.y2 = y2
	path.x = x
	path.y = y
	path.args = append(path.args, fmt.Sprintf("%.3f", x1))
	path.args = append(path.args, fmt.Sprintf("%.3f", y1))
	path.args = append(path.args, fmt.Sprintf("%.3f", x2))
	path.args = append(path.args, fmt.Sprintf("%.3f", y2))
	path.args = append(path.args, fmt.Sprintf("%.3f", x))
	path.args = append(path.args, fmt.Sprintf("%.3f", y))
}
