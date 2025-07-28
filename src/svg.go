package pdfjet

/**
 * svg.go
 *
©2025 PDFjet Software

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

import (
	"log"
	"strconv"
	"strings"
)

type SVG struct {
}

func NewSVG() *SVG {
	return new(SVG)
}

func isCommand(ch rune) bool {
	// Capital letter commands use absolute coordinates
	// Small letter commands use relative coordinates
	switch ch {
	case 'M', 'm': // moveto
		return true
	case 'L', 'l': // lineto
		return true
	case 'H', 'h': // horizontal lineto
		return true
	case 'V', 'v': // vertical lineto
		return true
	case 'Q', 'q': // quadratic curveto
		return true
	case 'T', 't': // smooth quadratic curveto
		return true
	case 'C', 'c': // cubic curveto
		return true
	case 'S', 's': // smooth cubic curveto
		return true
	case 'A', 'a': // elliptical arc
		return true
	case 'Z', 'z': // close path
		return true
	}
	return false
}

func (svg *SVG) GetOperations(path string) []*PathOp {
	operations := make([]*PathOp, 0)
	var op *PathOp
	var buf = strings.Builder{}
	var token = false
	for _, ch := range path {
		if isCommand(ch) { // open path
			if token {
				op.args = append(op.args, buf.String())
				buf.Reset()
			}
			token = false
			op = NewPathOp(ch)
			operations = append(operations, op)
		} else if ch == ' ' || ch == ',' {
			if token {
				op.args = append(op.args, buf.String())
				buf.Reset()
			}
			token = false
		} else if ch == '-' {
			if token {
				op.args = append(op.args, buf.String())
				buf.Reset()
			}
			token = true
			buf.WriteRune(ch)
		} else if ch == '.' {
			if strings.Contains(buf.String(), ".") {
				op.args = append(op.args, buf.String())
				buf.Reset()
			}
			token = true
			buf.WriteRune(ch)
		} else {
			token = true
			buf.WriteRune(ch)
		}
	}
	return operations
}

func (svg *SVG) ToPDF(list []*PathOp) []*PathOp {
	operations := make([]*PathOp, 0)
	var lastOp *PathOp
	var x0 float32 = 0.0 // Start of subpath
	var y0 float32 = 0.0
	for _, op := range list {
		switch op.cmd {
		case 'M', 'm':
			for i := 0; i <= len(op.args)-2; i += 2 {
				var pathOp *PathOp
				x, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				y, err := strconv.ParseFloat(op.args[i+1], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 'm' && lastOp != nil {
					x += float64(lastOp.x)
					y += float64(lastOp.y)
				}
				if i == 0 {
					x0 = float32(x)
					y0 = float32(y)
					pathOp = NewPathOpXY('M', float32(x), float32(y))
				} else {
					pathOp = NewPathOpXY('L', float32(x), float32(y))
				}
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'L', 'l':
			for i := 0; i <= len(op.args)-2; i += 2 {
				var pathOp *PathOp
				x, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				y, err := strconv.ParseFloat(op.args[i+1], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 'l' && lastOp != nil {
					x += float64(lastOp.x)
					y += float64(lastOp.y)
				}
				pathOp = NewPathOpXY('L', float32(x), float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'H', 'h':
			for i := 0; i < len(op.args); i++ {
				var pathOp *PathOp
				x, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 'h' && lastOp != nil {
					x += float64(lastOp.x)
				}
				pathOp = NewPathOpXY('L', float32(x), lastOp.y)
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'V', 'v':
			for i := 0; i < len(op.args); i++ {
				var pathOp *PathOp
				y, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 'v' && lastOp != nil {
					y += float64(lastOp.y)
				}
				pathOp = NewPathOpXY('L', lastOp.x, float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'Q', 'q':
			for i := 0; i <= len(op.args)-4; i += 4 {
				pathOp := NewPathOp('C')
				x1, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				y1, err := strconv.ParseFloat(op.args[i+1], 32)
				if err != nil {
					log.Fatal(err)
				}
				x, err := strconv.ParseFloat(op.args[i+2], 32)
				if err != nil {
					log.Fatal(err)
				}
				y, err := strconv.ParseFloat(op.args[i+3], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 'q' {
					x1 += float64(lastOp.x)
					y1 += float64(lastOp.y)
					x += float64(lastOp.x)
					y += float64(lastOp.y)
				}
				// Save the original control point
				pathOp.x1q = float32(x1)
				pathOp.y1q = float32(y1)
				// Calculate the coordinates of the cubic control points
				x1c := lastOp.x + (2.0/3.0)*(float32(x1)-lastOp.x)
				y1c := lastOp.y + (2.0/3.0)*(float32(y1)-lastOp.y)
				x2c := float32(x) + (2.0/3.0)*(float32(x1)-float32(x))
				y2c := float32(y) + (2.0/3.0)*(float32(y1)-float32(y))
				pathOp.setCubicPoints(x1c, y1c, x2c, y2c, float32(x), float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'T', 't':
			for i := 0; i <= len(op.args)-2; i += 2 {
				pathOp := NewPathOp('C')
				x1 := lastOp.x
				y1 := lastOp.y
				if lastOp.cmd == 'C' {
					// Find the reflection control point
					x1 = 2*lastOp.x - lastOp.x1q
					y1 = 2*lastOp.y - lastOp.y1q
				}
				x, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				y, err := strconv.ParseFloat(op.args[i+1], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 't' {
					x += float64(lastOp.x)
					y += float64(lastOp.y)
				}
				// Calculate the coordinates of the cubic control points
				x1c := lastOp.x + (2.0/3.0)*(x1-lastOp.x)
				y1c := lastOp.y + (2.0/3.0)*(y1-lastOp.y)
				x2c := float32(x) + (2.0/3.0)*(x1-float32(x))
				y2c := float32(y) + (2.0/3.0)*(y1-float32(y))
				pathOp.setCubicPoints(x1c, y1c, x2c, y2c, float32(x), float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'C', 'c':
			for i := 0; i <= len(op.args)-6; i += 6 {
				pathOp := NewPathOp('C')
				x1, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				y1, err := strconv.ParseFloat(op.args[i+1], 32)
				if err != nil {
					log.Fatal(err)
				}
				x2, err := strconv.ParseFloat(op.args[i+2], 32)
				if err != nil {
					log.Fatal(err)
				}
				y2, err := strconv.ParseFloat(op.args[i+3], 32)
				if err != nil {
					log.Fatal(err)
				}
				x, err := strconv.ParseFloat(op.args[i+4], 32)
				if err != nil {
					log.Fatal(err)
				}
				y, err := strconv.ParseFloat(op.args[i+5], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 'c' {
					x1 += float64(lastOp.x)
					y1 += float64(lastOp.y)
					x2 += float64(lastOp.x)
					y2 += float64(lastOp.y)
					x += float64(lastOp.x)
					y += float64(lastOp.y)
				}
				pathOp.setCubicPoints(
					float32(x1), float32(y1),
					float32(x2), float32(y2),
					float32(x), float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'S', 's':
			for i := 0; i <= len(op.args)-4; i += 4 {
				pathOp := NewPathOp('C')
				x1 := lastOp.x
				y1 := lastOp.y
				if lastOp.cmd == 'C' {
					// Find the reflection control point
					x1 = 2*lastOp.x - lastOp.x2
					y1 = 2*lastOp.y - lastOp.y2
				}
				x2, err := strconv.ParseFloat(op.args[i], 32)
				if err != nil {
					log.Fatal(err)
				}
				y2, err := strconv.ParseFloat(op.args[i+1], 32)
				if err != nil {
					log.Fatal(err)
				}
				x, err := strconv.ParseFloat(op.args[i+2], 32)
				if err != nil {
					log.Fatal(err)
				}
				y, err := strconv.ParseFloat(op.args[i+3], 32)
				if err != nil {
					log.Fatal(err)
				}
				if op.cmd == 's' {
					x2 += float64(lastOp.x)
					y2 += float64(lastOp.y)
					x += float64(lastOp.x)
					y += float64(lastOp.y)
				}
				pathOp.setCubicPoints(
					float32(x1), float32(y1),
					float32(x2), float32(y2),
					float32(x), float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'A', 'a':
			// Elliptical Arc
		case 'Z', 'z':
			pathOp := NewPathOp('Z')
			pathOp.x = x0
			pathOp.y = y0
			operations = append(operations, pathOp)
			lastOp = pathOp
		}
	}
	return operations
}
