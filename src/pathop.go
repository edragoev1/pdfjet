package pdfjet

/**
 * pathop.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// PathOp is used to create path objects.
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

func (path *PathOp) setCubicPoints(x1, y1, x2, y2, x, y float32) {
	path.x1 = x1
	path.y1 = y1
	path.x2 = x2
	path.y2 = y2
	path.x = x
	path.y = y
}
