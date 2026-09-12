// svg.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"log"
	"math"
	"strconv"
	"strings"
)

// SVG converts SVG path data to PDF path operations.
type SVG struct {
}

// NewSVG creates an SVG object.
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

// GetOperations parses SVG path data into a list of path operations.
func (svg *SVG) GetOperations(path string) []*PathOp {
	operations := make([]*PathOp, 0)
	var op = NewPathOp(' ')
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
	if token { // The last number of a path that does not end with Z
		op.args = append(op.args, buf.String())
	}
	return operations
}

// ToPDF converts SVG path operations to PDF path operations.
func (svg *SVG) ToPDF(list []*PathOp) []*PathOp {
	operations := make([]*PathOp, 0)
	var lastOp = NewPathOp(' ')
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
					x1, y1,
					float32(x2), float32(y2),
					float32(x), float32(y))
				operations = append(operations, pathOp)
				lastOp = pathOp
			}
		case 'A', 'a':
			for i := 0; i <= len(op.args)-7; i += 7 {
				rx := svgFloat(op.args[i])
				ry := svgFloat(op.args[i+1])
				rotation := svgFloat(op.args[i+2])
				largeArc := op.args[i+3] != "0"
				sweep := op.args[i+4] != "0"
				x := svgFloat(op.args[i+5])
				y := svgFloat(op.args[i+6])
				if op.cmd == 'a' {
					x += lastOp.x
					y += lastOp.y
				}
				lastOp = addArc(&operations, lastOp, rx, ry, rotation, largeArc, sweep, x, y)
			}
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

func svgFloat(arg string) float32 {
	value, err := strconv.ParseFloat(arg, 32)
	if err != nil {
		log.Fatal(err)
	}
	return float32(value)
}

// addArc appends the cubic curves that draw the elliptical arc from the
// current point to (x, y), as SVG 1.1 section F.6.5 describes: the arc is
// split into pieces of at most a quarter turn, each approximated by one curve.
// It returns the last operation appended, or lastOp when the arc is empty.
func addArc(operations *[]*PathOp, lastOp *PathOp,
	rx, ry, rotation float32, largeArc, sweep bool, x, y float32) *PathOp {
	x1 := lastOp.x
	y1 := lastOp.y
	if x1 == x && y1 == y {
		return lastOp
	}
	rx = float32(math.Abs(float64(rx)))
	ry = float32(math.Abs(float64(ry)))
	if rx == 0.0 || ry == 0.0 {
		line := NewPathOpXY('L', x, y)
		*operations = append(*operations, line)
		return line
	}
	phi := float64(rotation) * math.Pi / 180.0
	cosPhi := math.Cos(phi)
	sinPhi := math.Sin(phi)
	dx := float64(x1-x) / 2.0
	dy := float64(y1-y) / 2.0
	x1p := cosPhi*dx + sinPhi*dy
	y1p := -sinPhi*dx + cosPhi*dy
	lambda := (x1p*x1p)/(float64(rx)*float64(rx)) + (y1p*y1p)/(float64(ry)*float64(ry))
	if lambda > 1.0 {
		rx *= float32(math.Sqrt(lambda))
		ry *= float32(math.Sqrt(lambda))
	}
	rx2 := float64(rx) * float64(rx)
	ry2 := float64(ry) * float64(ry)
	num := rx2*ry2 - rx2*y1p*y1p - ry2*x1p*x1p
	den := rx2*y1p*y1p + ry2*x1p*x1p
	coef := math.Sqrt(math.Max(0.0, num/den))
	if largeArc == sweep {
		coef = -coef
	}
	cxp := coef * float64(rx) * y1p / float64(ry)
	cyp := -coef * float64(ry) * x1p / float64(rx)
	cx := cosPhi*cxp - sinPhi*cyp + float64(x1+x)/2.0
	cy := sinPhi*cxp + cosPhi*cyp + float64(y1+y)/2.0
	ux := (x1p - cxp) / float64(rx)
	uy := (y1p - cyp) / float64(ry)
	vx := (-x1p - cxp) / float64(rx)
	vy := (-y1p - cyp) / float64(ry)
	theta := math.Atan2(uy, ux)
	delta := math.Atan2(ux*vy-uy*vx, ux*vx+uy*vy)
	if !sweep && delta > 0.0 {
		delta -= 2.0 * math.Pi
	} else if sweep && delta < 0.0 {
		delta += 2.0 * math.Pi
	}
	segments := int(math.Ceil(math.Abs(delta) / (math.Pi / 2.0)))
	if segments == 0 {
		return lastOp
	}
	step := delta / float64(segments)
	t := 4.0 / 3.0 * math.Tan(step/4.0)
	pathOp := lastOp
	for i := 0; i < segments; i++ {
		a1 := theta + float64(i)*step
		a2 := a1 + step
		p1x := math.Cos(a1)
		p1y := math.Sin(a1)
		p2x := math.Cos(a2)
		p2y := math.Sin(a2)
		c1 := onEllipse(cx, cy, float64(rx), float64(ry), cosPhi, sinPhi, p1x-t*p1y, p1y+t*p1x)
		c2 := onEllipse(cx, cy, float64(rx), float64(ry), cosPhi, sinPhi, p2x+t*p2y, p2y-t*p2x)
		p2 := [2]float32{x, y}
		if i < segments-1 {
			p2 = onEllipse(cx, cy, float64(rx), float64(ry), cosPhi, sinPhi, p2x, p2y)
		}
		pathOp = NewPathOp('C')
		pathOp.setCubicPoints(c1[0], c1[1], c2[0], c2[1], p2[0], p2[1])
		*operations = append(*operations, pathOp)
	}
	return pathOp
}

// onEllipse maps a point of the unit circle to the ellipse with the center
// (cx, cy), the radii rx and ry and the rotation with the given cosine and sine.
func onEllipse(cx, cy, rx, ry, cosPhi, sinPhi, u, v float64) [2]float32 {
	return [2]float32{
		float32(cx + rx*u*cosPhi - ry*v*sinPhi),
		float32(cy + rx*u*sinPhi + ry*v*cosPhi)}
}
