// glyphoutline.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"math"
	"strconv"
	"strings"
)

// glyphOutline holds the outlines of the glyphs of a font, read from the font
// file itself: the glyf table of a TrueType font, its quadratic contours
// drawn exactly as cubic curves, composite glyphs with the transforms of their
// parts; or the Type 2 charstrings of the CFF table of an OpenType font, with
// their subroutines, the hints left out. Page.DrawGlyphs draws them as paths.
//
// An outline is written as the path operators of PDF in the units of the
// font, y up, filled with the nonzero winding rule; the matrix takes the
// units to the em, so that the size of the font times the matrix, moved to
// the glyph's place, draws it.
type glyphOutline struct {
	data      []byte
	cff       bool
	matrix    [6]float64 // The units of the font to the em: a b c d e f
	numGlyphs int

	// TrueType
	glyfOffset  int
	glyfLength  int
	locaOffset  int
	locaLength  int
	longOffsets bool

	// CFF
	charStrings cffIndex
	globalSubrs cffIndex
	localSubrs  []*cffIndex  // Of each Font DICT, or of the font
	fdOf        []int        // The Font DICT of each glyph, or nil
	fdMatrix    [][6]float64 // The FontMatrix of each Font DICT, or nil
}

// glyphPath is an outline: its path operators and the box of its points.
type glyphPath struct {
	operators              string // Empty for a glyph that draws nothing, as the space
	xMin, yMin, xMax, yMax float64
}

// outlinesUnreadable is the message of a font whose outlines cannot be read.
const outlinesUnreadable = "The outlines of the font cannot be read: "

// cffThousandth is the default FontMatrix of a CFF font.
var cffThousandth = [6]float64{0.001, 0, 0, 0.001, 0, 0}

// errSeac is the error of a CFF glyph of the seac operator.
type errSeac struct{ message string }

func (e *errSeac) Error() string { return e.message }

// newGlyphOutline reads the tables of the outlines of the font program: a
// TrueType font, or the CFF table of an OpenType font.
func newGlyphOutline(data []byte, cff bool) (outline *glyphOutline, err error) {
	defer func() {
		if recover() != nil {
			outline = nil
			err = errors.New(outlinesUnreadable + "its tables are damaged.")
		}
	}()
	o := &glyphOutline{data: data, cff: cff}
	if cff {
		if err := o.readCFF(); err != nil {
			return nil, errors.New(outlinesUnreadable + "its tables are damaged.")
		}
		o.numGlyphs = len(o.charStrings.objects) - 1
	} else {
		unitsPerEm, err := o.readTrueType()
		if err != nil {
			return nil, errors.New(outlinesUnreadable + "its tables are damaged.")
		}
		em := 1.0 / float64(unitsPerEm)
		o.matrix = [6]float64{em, 0, 0, em, 0, 0}
		step := 2
		if o.longOffsets {
			step = 4
		}
		o.numGlyphs = (o.locaLength - step) / step
	}
	return o, nil
}

// readTrueType reads the head, loca and glyf tables, and returns the units per em.
func (o *glyphOutline) readTrueType() (int, error) {
	numTables, err := o.u16(4)
	if err != nil {
		return 0, err
	}
	headOffset, locaAt, glyfAt := -1, -1, -1
	for i := 0; i < numTables; i++ {
		entry := 12 + 16*i
		if entry+16 > len(o.data) {
			return 0, errNotSubset
		}
		tag := string(o.data[entry : entry+4])
		offset, err1 := o.u32(entry + 8)
		length, err2 := o.u32(entry + 12)
		if err1 != nil || err2 != nil || int64(offset)+int64(length) > int64(len(o.data)) {
			return 0, errNotSubset
		}
		switch tag {
		case "head":
			headOffset = offset
		case "loca":
			locaAt = offset
			o.locaLength = length
		case "glyf":
			glyfAt = offset
			o.glyfLength = length
		}
	}
	if headOffset < 0 || locaAt < 0 || glyfAt < 0 {
		return 0, errNotSubset
	}
	unitsPerEm, err := o.u16(headOffset + 18)
	if err != nil {
		return 0, err
	}
	format, err := o.u16(headOffset + 50)
	if err != nil {
		return 0, err
	}
	o.longOffsets = format == 1
	o.locaOffset = locaAt
	o.glyfOffset = glyfAt
	if unitsPerEm == 0 {
		return 0, errNotSubset
	}
	return unitsPerEm, nil
}

// readCFF reads the CFF table: its charstrings, its subroutines and the Font
// DICT of each glyph, and the FontMatrix of the font.
func (o *glyphOutline) readCFF() error {
	cff := o.data
	if len(cff) < 4 || cff[0] != 1 {
		return errNotSubset // Not CFF version 1
	}
	names, err := readCFFIndex(cff, int(cff[2]))
	if err != nil {
		return err
	}
	topDicts, err := readCFFIndex(cff, names.end)
	if err != nil || len(topDicts.objects) != 2 {
		return errNotSubset
	}
	strings, err := readCFFIndex(cff, topDicts.end)
	if err != nil {
		return err
	}
	o.globalSubrs, err = readCFFIndex(cff, strings.end)
	if err != nil {
		return err
	}
	top, err := readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	if err != nil {
		return err
	}
	at := cffEntryOf(top, cffCharStrings)
	if len(at) != 1 {
		return errNotSubset
	}
	o.charStrings, err = readCFFIndex(cff, at[0])
	if err != nil {
		return err
	}
	topMatrix, err := cffMatrixOf(top)
	if err != nil {
		return err
	}
	numGlyphs := len(o.charStrings.objects) - 1
	if cffEntryOf(top, cffROS) != nil {
		fdArrayAt := cffEntryOf(top, cffFDArray)
		fdSelectAt := cffEntryOf(top, cffFDSelect)
		if len(fdArrayAt) != 1 || len(fdSelectAt) != 1 {
			return errNotSubset
		}
		fdArray, err := readCFFIndex(cff, fdArrayAt[0])
		if err != nil {
			return err
		}
		count := len(fdArray.objects) - 1
		o.localSubrs = make([]*cffIndex, count)
		matrices := make([]*[6]float64, count)
		for i := 0; i < count; i++ {
			fd, err := readCFFDict(cff[fdArray.objects[i]:fdArray.objects[i+1]])
			if err != nil {
				return err
			}
			if o.localSubrs[i], err = o.subrsOf(fd); err != nil {
				return err
			}
			if matrices[i], err = cffMatrixOf(fd); err != nil {
				return err
			}
		}
		length, err := cffFDSelectLength(cff, fdSelectAt[0], numGlyphs)
		if err != nil {
			return err
		}
		fdSelect, err := cffBlock(cff, fdSelectAt[0], length)
		if err != nil {
			return err
		}
		if o.fdOf, err = cffFDOf(fdSelect, numGlyphs, count); err != nil {
			return err
		}
		// The glyph space of a CIDFont: the FontMatrix of the glyph's Font
		// DICT and then the Top DICT's; a CIDFont whose Top DICT has none
		// has its Font DICTs' taken as the whole ones, as readers do.
		o.fdMatrix = make([][6]float64, count)
		for i := range matrices {
			switch {
			case matrices[i] == nil && topMatrix == nil:
				o.fdMatrix[i] = cffThousandth
			case matrices[i] == nil:
				o.fdMatrix[i] = *topMatrix
			case topMatrix != nil:
				o.fdMatrix[i] = multiplyMatrices(*matrices[i], *topMatrix)
			default:
				o.fdMatrix[i] = *matrices[i]
			}
		}
		o.matrix = cffThousandth // Of a glyph, its Font DICT's
		return nil
	}
	subrs, err := o.subrsOf(top)
	if err != nil {
		return err
	}
	o.localSubrs = []*cffIndex{subrs}
	if topMatrix == nil {
		o.matrix = cffThousandth
	} else {
		o.matrix = *topMatrix
	}
	return nil
}

// subrsOf returns the local subroutines of the Private DICT of the Top DICT
// or Font DICT.
func (o *glyphOutline) subrsOf(dict []cffEntry) (*cffIndex, error) {
	p := cffEntryOf(dict, cffPrivate)
	if len(p) != 2 {
		return nil, errNotSubset
	}
	private, err := readCFFPrivate(o.data, p[0], p[1])
	if err != nil {
		return nil, err
	}
	return private.subrs, nil
}

// cffMatrixOf returns the FontMatrix of the DICT, or nil when it has none.
func cffMatrixOf(dict []cffEntry) (*[6]float64, error) {
	for _, entry := range dict {
		if entry.op == cffFontMatrix {
			m, err := cffOperands(entry.raw)
			if err != nil || len(m) != 6 {
				return nil, errNotSubset
			}
			for _, v := range m {
				if !(math.Abs(v) <= 1000) { // NaN too
					return nil, errNotSubset
				}
			}
			return &[6]float64{m[0], m[1], m[2], m[3], m[4], m[5]}, nil
		}
	}
	return nil, nil
}

// multiplyMatrices returns the product of the matrices: a, then b.
func multiplyMatrices(a, b [6]float64) [6]float64 {
	return [6]float64{
		float64(a[0]*b[0]) + float64(a[1]*b[2]),
		float64(a[0]*b[1]) + float64(a[1]*b[3]),
		float64(a[2]*b[0]) + float64(a[3]*b[2]),
		float64(a[2]*b[1]) + float64(a[3]*b[3]),
		float64(a[4]*b[0]) + float64(a[5]*b[2]) + b[4],
		float64(a[4]*b[1]) + float64(a[5]*b[3]) + b[5],
	}
}

// cffOperands returns the operands of an entry of a DICT, integers and reals,
// before its operator.
func cffOperands(raw []byte) ([]float64, error) {
	var values []float64
	for i := 0; i < len(raw); {
		b0 := int(raw[i])
		switch {
		case b0 <= 21:
			return values, nil // The operator
		case b0 == 28:
			values = append(values, float64(int16(uint16(raw[i+1])<<8|uint16(raw[i+2]))))
			i += 3
		case b0 == 29:
			values = append(values, float64(int32(uint32(raw[i+1])<<24|uint32(raw[i+2])<<16|
				uint32(raw[i+3])<<8|uint32(raw[i+4]))))
			i += 5
		case b0 == 30:
			var sb strings.Builder
			i++
			end := false
			for !end && i < len(raw) {
				b := int(raw[i])
				i++
				for _, nibble := range []int{b >> 4, b & 0x0F} {
					if nibble <= 9 {
						sb.WriteByte(byte('0' + nibble))
					} else if nibble == 0xA {
						sb.WriteByte('.')
					} else if nibble == 0xB {
						sb.WriteByte('E')
					} else if nibble == 0xC {
						sb.WriteString("E-")
					} else if nibble == 0xE {
						sb.WriteByte('-')
					} else if nibble == 0xF {
						end = true
						break
					}
				}
			}
			value, err := strconv.ParseFloat(sb.String(), 64)
			if err != nil {
				return nil, errNotSubset
			}
			values = append(values, value)
		case b0 >= 32 && b0 <= 246:
			values = append(values, float64(b0-139))
			i++
		case b0 >= 247 && b0 <= 250:
			values = append(values, float64((b0-247)*256+int(raw[i+1])+108))
			i += 2
		case b0 >= 251 && b0 <= 254:
			values = append(values, float64(-(b0-251)*256-int(raw[i+1])-108))
			i += 2
		default:
			return nil, errNotSubset
		}
	}
	return values, nil
}

// count returns the number of glyphs of the font.
func (o *glyphOutline) count() int {
	return o.numGlyphs
}

// matrixOf returns the matrix that takes the units of the glyph to the em:
// of the font, or of the glyph's Font DICT in a CID-keyed CFF font.
func (o *glyphOutline) matrixOf(gid int) [6]float64 {
	if o.fdMatrix != nil && gid >= 0 && gid < len(o.fdOf) {
		return o.fdMatrix[o.fdOf[gid]]
	}
	return o.matrix
}

// outline returns the outline of the glyph.
func (o *glyphOutline) outline(gid int) (path glyphPath, err error) {
	if gid < 0 || gid >= o.numGlyphs {
		return glyphPath{}, errors.New("The font has no glyph number " + strconv.Itoa(gid) + ".")
	}
	damaged := errors.New(outlinesUnreadable + "glyph number " + strconv.Itoa(gid) + " is damaged.")
	defer func() {
		if recover() != nil {
			path = glyphPath{}
			err = damaged
		}
	}()
	pen := &glyphPen{xMin: math.MaxFloat64, yMin: math.MaxFloat64, xMax: -math.MaxFloat64, yMax: -math.MaxFloat64}
	if o.cff {
		cs := o.newCharString(pen, gid)
		if err := cs.run(o.charStrings.objects[gid], o.charStrings.objects[gid+1], 0); err != nil {
			if seac, ok := err.(*errSeac); ok {
				return glyphPath{}, errors.New(seac.message)
			}
			return glyphPath{}, damaged
		}
	} else {
		points := &glyphPoints{work: new(int)}
		if err := o.trueTypePoints(gid, points, 0); err != nil {
			return glyphPath{}, damaged
		}
		points.draw(pen)
	}
	if pen.outOfRange {
		return glyphPath{}, damaged
	}
	return pen.path(), nil
}

// ------------------------------------------------------------------
// The path: the operators of PDF, in the units of the font

// glyphPen writes the path operators and finds the box of the points.
type glyphPen struct {
	sb                     strings.Builder
	xMin, yMin, xMax, yMax float64
	startX, startY         float64
	x, y                   float64
	open                   bool // A contour is begun and has a segment
	outOfRange             bool // A point past glyphMaxCoordinate, or not finite
}

// glyphMaxCoordinate bounds the points of an outline, in the units of the
// font: a font's are within ±32,768, and a charstring's arithmetic can make
// larger ones, or ones not finite, which a path cannot have.
const glyphMaxCoordinate = 1e6

func (pen *glyphPen) point(px, py float64) {
	if !(math.Abs(px) <= glyphMaxCoordinate && math.Abs(py) <= glyphMaxCoordinate) {
		pen.outOfRange = true // NaN too
		px, py = 0, 0
	}
	if px < pen.xMin {
		pen.xMin = px
	}
	if py < pen.yMin {
		pen.yMin = py
	}
	if px > pen.xMax {
		pen.xMax = px
	}
	if py > pen.yMax {
		pen.yMax = py
	}
	pen.sb.WriteString(glyphNumber(px))
	pen.sb.WriteByte(' ')
	pen.sb.WriteString(glyphNumber(py))
}

func (pen *glyphPen) moveTo(px, py float64) {
	pen.startX = px
	pen.startY = py
	pen.x = px
	pen.y = py
	pen.open = false
}

// begin writes the m of the contour before its first segment, so that a
// contour of a moveto alone writes nothing.
func (pen *glyphPen) begin() {
	if !pen.open {
		pen.point(pen.startX, pen.startY)
		pen.sb.WriteString(" m\n")
		pen.open = true
	}
}

func (pen *glyphPen) lineTo(px, py float64) {
	pen.begin()
	pen.point(px, py)
	pen.sb.WriteString(" l\n")
	pen.x = px
	pen.y = py
}

func (pen *glyphPen) curveTo(x1, y1, x2, y2, x3, y3 float64) {
	pen.begin()
	pen.point(x1, y1)
	pen.sb.WriteByte(' ')
	pen.point(x2, y2)
	pen.sb.WriteByte(' ')
	pen.point(x3, y3)
	pen.sb.WriteString(" c\n")
	pen.x = x3
	pen.y = y3
}

// quadTo draws the quadratic curve from the current point exactly as a cubic one.
func (pen *glyphPen) quadTo(qx, qy, px, py float64) {
	const twoThirds = 2.0 / 3.0
	pen.curveTo(pen.x+float64(twoThirds*(qx-pen.x)), pen.y+float64(twoThirds*(qy-pen.y)),
		px+float64(twoThirds*(qx-px)), py+float64(twoThirds*(qy-py)),
		px, py)
}

func (pen *glyphPen) path() glyphPath {
	if pen.sb.Len() == 0 {
		return glyphPath{}
	}
	return glyphPath{pen.sb.String(), pen.xMin, pen.yMin, pen.xMax, pen.yMax}
}

// glyphNumber returns a number of a path, in the units of the font: in
// hundredths, trailing zeros left out, the same in the four ports. Rounded
// half away from zero.
func glyphNumber(value float64) string {
	return glyphDecimals(value, 2)
}

// glyphDecimals returns the value with at most the decimals given, rounded
// half away from zero.
func glyphDecimals(value float64, decimals int) string {
	scale := int64(1)
	for i := 0; i < decimals; i++ {
		scale *= 10
	}
	n := int64(math.Floor(float64(math.Abs(value)*float64(scale)) + 0.5))
	if n == 0 {
		return "0"
	}
	var sb strings.Builder
	if value < 0 {
		sb.WriteByte('-')
	}
	sb.WriteString(strconv.FormatInt(n/scale, 10))
	fraction := n % scale
	if fraction != 0 {
		digits := strconv.FormatInt(fraction+scale, 10)[1:]
		digits = strings.TrimRight(digits, "0")
		sb.WriteByte('.')
		sb.WriteString(digits)
	}
	return sb.String()
}

// ------------------------------------------------------------------
// TrueType

// glyphPoints holds the points of a glyph, with the last point of each contour.
type glyphPoints struct {
	x, y []float64
	on   []bool
	ends []int
	work *int // The glyphs and points read for the outline, its parts' too
}

func (p *glyphPoints) add(px, py float64, onCurve bool) {
	p.x = append(p.x, px)
	p.y = append(p.y, py)
	p.on = append(p.on, onCurve)
}

func (p *glyphPoints) count() int {
	return len(p.x)
}

// draw draws the contours: the points on the curve joined by lines, and by
// quadratic curves through the points off it, with a point on the curve
// halfway between two off it.
func (p *glyphPoints) draw(pen *glyphPen) {
	first := 0
	for _, end := range p.ends {
		n := end - first + 1
		if n >= 2 {
			p.contour(pen, first, n)
		}
		first = end + 1
	}
}

func (p *glyphPoints) contour(pen *glyphPen, first, n int) {
	var sx, sy float64
	var from, steps int // The first point after the start; the points after it, the start not again
	if p.on[first] {
		sx, sy = p.x[first], p.y[first]
		from, steps = 1, n-1
	} else if p.on[first+n-1] {
		sx, sy = p.x[first+n-1], p.y[first+n-1]
		from, steps = 0, n-1
	} else {
		sx = (p.x[first] + p.x[first+n-1]) / 2
		sy = (p.y[first] + p.y[first+n-1]) / 2
		from, steps = 0, n
	}
	pen.moveTo(sx, sy)
	off := false
	var qx, qy float64
	for k := 0; k < steps; k++ {
		i := first + (from+k)%n
		if p.on[i] {
			if off {
				pen.quadTo(qx, qy, p.x[i], p.y[i])
			} else {
				pen.lineTo(p.x[i], p.y[i])
			}
			off = false
		} else {
			if off {
				pen.quadTo(qx, qy, (qx+p.x[i])/2, (qy+p.y[i])/2)
			}
			qx, qy = p.x[i], p.y[i]
			off = true
		}
	}
	// Back to the start: a curve, or the line that filling draws
	if off {
		pen.quadTo(qx, qy, sx, sy)
	}
}

// trueTypePoints adds the points of the glyph, a simple or a composite one,
// to the points.
func (o *glyphOutline) trueTypePoints(gid int, points *glyphPoints, depth int) error {
	if depth > 16 {
		return errNotSubset // Composite glyphs nested too deep, or in a loop
	}
	// A composite glyph of many parts, each of many parts, 16 deep, would
	// take time and memory that grow exponentially: the work of a glyph is
	// bounded, as that of a charstring is.
	*points.work++
	if *points.work > glyphMaxWork {
		return errNotSubset
	}
	var start, end int
	var err1, err2 error
	if o.longOffsets {
		start, err1 = o.u32(o.locaOffset + 4*gid)
		end, err2 = o.u32(o.locaOffset + 4*gid + 4)
	} else {
		start, err1 = o.u16(o.locaOffset + 2*gid)
		end, err2 = o.u16(o.locaOffset + 2*gid + 2)
		start *= 2
		end *= 2
	}
	if err1 != nil || err2 != nil {
		return errNotSubset
	}
	if start == end {
		return nil // No outline, as the space
	}
	if start > end || end > o.glyfLength || end-start < 10 {
		return errNotSubset
	}
	at := o.glyfOffset + start
	limit := o.glyfOffset + end
	contours, err := o.u16(at)
	if err != nil {
		return err
	}
	if int16(contours) >= 0 {
		return o.simplePoints(at, limit, int(int16(contours)), points)
	}
	return o.compositePoints(at+10, limit, points, depth)
}

func (o *glyphOutline) simplePoints(at, limit, contours int, points *glyphPoints) error {
	p := at + 10
	ends := make([]int, contours)
	n := 0
	for i := 0; i < contours; i++ {
		end, err := o.u16(p + 2*i)
		if err != nil {
			return err
		}
		ends[i] = end
		if ends[i] < n-1 || (i > 0 && ends[i] <= ends[i-1]) {
			return errNotSubset
		}
		n = ends[i] + 1
	}
	*points.work += n
	if *points.work > glyphMaxWork {
		return errNotSubset
	}
	p += 2 * contours
	instructions, err := o.u16(p)
	if err != nil {
		return err
	}
	p += 2 + instructions
	flags := make([]byte, n)
	for i := 0; i < n; {
		flag, err := o.byteAt(p, limit)
		if err != nil {
			return err
		}
		p++
		flags[i] = byte(flag)
		i++
		if flag&0x08 != 0 { // REPEAT_FLAG
			repeat, err := o.byteAt(p, limit)
			if err != nil {
				return err
			}
			p++
			for r := 0; r < repeat; r++ {
				if i >= n {
					return errNotSubset
				}
				flags[i] = byte(flag)
				i++
			}
		}
	}
	xs := make([]int, n)
	value := 0
	for i := 0; i < n; i++ {
		flag := flags[i]
		if flag&0x02 != 0 { // X_SHORT_VECTOR
			dx, err := o.byteAt(p, limit)
			if err != nil {
				return err
			}
			p++
			if flag&0x10 != 0 {
				value += dx
			} else {
				value -= dx
			}
		} else if flag&0x10 == 0 { // Not X_IS_SAME
			v, err := o.int16At(p, limit)
			if err != nil {
				return err
			}
			value += v
			p += 2
		}
		xs[i] = value
	}
	value = 0
	base := points.count()
	for i := 0; i < n; i++ {
		flag := flags[i]
		if flag&0x04 != 0 { // Y_SHORT_VECTOR
			dy, err := o.byteAt(p, limit)
			if err != nil {
				return err
			}
			p++
			if flag&0x20 != 0 {
				value += dy
			} else {
				value -= dy
			}
		} else if flag&0x20 == 0 { // Not Y_IS_SAME
			v, err := o.int16At(p, limit)
			if err != nil {
				return err
			}
			value += v
			p += 2
		}
		points.add(float64(xs[i]), float64(value), flag&0x01 != 0)
	}
	for _, end := range ends {
		points.ends = append(points.ends, base+end)
	}
	return nil
}

func (o *glyphOutline) compositePoints(p, limit int, points *glyphPoints, depth int) error {
	for {
		flags, err1 := o.u16(p)
		component, err2 := o.u16(p + 2)
		if err1 != nil || err2 != nil {
			return errNotSubset
		}
		p += 4
		if component >= o.numGlyphs {
			return errNotSubset
		}
		var arg1, arg2 int
		xy := flags&0x0002 != 0 // ARGS_ARE_XY_VALUES
		if flags&0x0001 != 0 {  // ARG_1_AND_2_ARE_WORDS
			a1, e1 := o.u16(p)
			a2, e2 := o.u16(p + 2)
			if e1 != nil || e2 != nil {
				return errNotSubset
			}
			arg1, arg2 = a1, a2
			if xy {
				arg1 = int(int16(a1))
				arg2 = int(int16(a2))
			}
			p += 4
		} else {
			a1, e1 := o.byteAt(p, limit)
			a2, e2 := o.byteAt(p+1, limit)
			if e1 != nil || e2 != nil {
				return errNotSubset
			}
			arg1, arg2 = a1, a2
			if xy {
				arg1 = int(int8(a1))
				arg2 = int(int8(a2))
			}
			p += 2
		}
		a, b, c, d := 1.0, 0.0, 0.0, 1.0
		var err error
		if flags&0x0008 != 0 { // WE_HAVE_A_SCALE
			if a, err = o.f2dot14(p); err != nil {
				return err
			}
			d = a
			p += 2
		} else if flags&0x0040 != 0 { // WE_HAVE_AN_X_AND_Y_SCALE
			if a, err = o.f2dot14(p); err != nil {
				return err
			}
			if d, err = o.f2dot14(p + 2); err != nil {
				return err
			}
			p += 4
		} else if flags&0x0080 != 0 { // WE_HAVE_A_TWO_BY_TWO
			if a, err = o.f2dot14(p); err != nil {
				return err
			}
			if b, err = o.f2dot14(p + 2); err != nil {
				return err
			}
			if c, err = o.f2dot14(p + 4); err != nil {
				return err
			}
			if d, err = o.f2dot14(p + 6); err != nil {
				return err
			}
			p += 8
		}
		if p > limit {
			return errNotSubset
		}
		part := &glyphPoints{work: points.work}
		if err := o.trueTypePoints(component, part, depth+1); err != nil {
			return err
		}
		for i := range part.x {
			px, py := part.x[i], part.y[i]
			part.x[i] = float64(a*px) + float64(c*py)
			part.y[i] = float64(b*px) + float64(d*py)
		}
		var dx, dy float64
		if xy {
			dx, dy = float64(arg1), float64(arg2)
			// SCALED_COMPONENT_OFFSET, and not UNSCALED_COMPONENT_OFFSET
			if flags&0x0800 != 0 && flags&0x1000 == 0 {
				tx := float64(a*dx) + float64(c*dy)
				ty := float64(b*dx) + float64(d*dy)
				dx, dy = tx, ty
			}
		} else {
			// The point of the part placed on the point of the glyph
			if arg1 >= points.count() || arg2 >= part.count() {
				return errNotSubset
			}
			dx = points.x[arg1] - part.x[arg2]
			dy = points.y[arg1] - part.y[arg2]
		}
		base := points.count()
		for i := range part.x {
			points.add(part.x[i]+dx, part.y[i]+dy, part.on[i])
		}
		for _, end := range part.ends {
			points.ends = append(points.ends, base+end)
		}
		if flags&0x0020 == 0 { // MORE_COMPONENTS
			return nil
		}
	}
}

func (o *glyphOutline) f2dot14(at int) (float64, error) {
	v, err := o.u16(at)
	if err != nil {
		return 0, err
	}
	return float64(int16(v)) / 16384.0, nil
}

func (o *glyphOutline) byteAt(at, limit int) (int, error) {
	if at < 0 || at >= limit {
		return 0, errNotSubset
	}
	return int(o.data[at]), nil
}

func (o *glyphOutline) int16At(at, limit int) (int, error) {
	hi, err1 := o.byteAt(at, limit)
	lo, err2 := o.byteAt(at+1, limit)
	if err1 != nil || err2 != nil {
		return 0, errNotSubset
	}
	return int(int16(uint16(hi)<<8 | uint16(lo))), nil
}

func (o *glyphOutline) u16(at int) (int, error) {
	if at < 0 || at > len(o.data)-2 {
		return 0, errNotSubset
	}
	return int(o.data[at])<<8 | int(o.data[at+1]), nil
}

func (o *glyphOutline) u32(at int) (int, error) {
	if at < 0 || at > len(o.data)-4 {
		return 0, errNotSubset
	}
	value := int64(o.data[at])<<24 | int64(o.data[at+1])<<16 | int64(o.data[at+2])<<8 | int64(o.data[at+3])
	if value > math.MaxInt32 {
		return 0, errNotSubset
	}
	return int(value), nil
}

// ------------------------------------------------------------------
// CFF: the Type 2 charstrings (Adobe Technical Note #5177)

// glyphMaxWork is the most bytes of charstrings a glyph runs, subroutines and
// all; and the most glyphs and points a TrueType glyph reads, its parts and
// theirs.
const glyphMaxWork = 1 << 20

// charString is the interpreter of the charstring of one glyph.
type charString struct {
	o         *glyphOutline
	pen       *glyphPen
	local     *cffIndex
	stack     [48]float64
	sp        int
	storage   [32]float64
	stems     int
	widthDone bool
	ended     bool
	x, y      float64
	work      int
	gid       int
}

func (o *glyphOutline) newCharString(pen *glyphPen, gid int) *charString {
	cs := &charString{o: o, pen: pen, gid: gid}
	if o.fdOf != nil {
		cs.local = o.localSubrs[o.fdOf[gid]]
	} else {
		cs.local = o.localSubrs[0]
	}
	return cs
}

func (cs *charString) push(value float64) error {
	if cs.sp == len(cs.stack) {
		return errNotSubset
	}
	cs.stack[cs.sp] = value
	cs.sp++
	return nil
}

func (cs *charString) pop() (float64, error) {
	if cs.sp == 0 {
		return 0, errNotSubset
	}
	cs.sp--
	return cs.stack[cs.sp], nil
}

// width returns 1 when the first operator that clears the stack has the width
// of the glyph before its operands, which is not drawn, and 0 otherwise.
func (cs *charString) width(extra bool) int {
	if !cs.widthDone {
		cs.widthDone = true
		if extra {
			return 1
		}
	}
	return 0
}

func (cs *charString) moveTo(dx, dy float64) {
	cs.x += dx
	cs.y += dy
	cs.pen.moveTo(cs.x, cs.y)
}

func (cs *charString) lineTo(dx, dy float64) {
	cs.x += dx
	cs.y += dy
	cs.pen.lineTo(cs.x, cs.y)
}

func (cs *charString) curveTo(dx1, dy1, dx2, dy2, dx3, dy3 float64) {
	x1 := cs.x + dx1
	y1 := cs.y + dy1
	x2 := x1 + dx2
	y2 := y1 + dy2
	cs.x = x2 + dx3
	cs.y = y2 + dy3
	cs.pen.curveTo(x1, y1, x2, y2, cs.x, cs.y)
}

func (cs *charString) need(count int) error {
	if cs.sp < count {
		return errNotSubset
	}
	return nil
}

func (cs *charString) run(from, to, depth int) error {
	if depth > 10 { // Type 2 nests subroutines 10 deep at most
		return errNotSubset
	}
	cs.work += to - from
	if cs.work > glyphMaxWork {
		return errNotSubset
	}
	data := cs.o.data
	s := &cs.stack
	i := from
	for i < to && !cs.ended {
		b0 := int(data[i])
		var err error
		switch {
		case b0 == 28:
			if i > to-3 {
				return errNotSubset
			}
			err = cs.push(float64(int16(uint16(data[i+1])<<8 | uint16(data[i+2]))))
			i += 3
		case b0 >= 32 && b0 <= 246:
			err = cs.push(float64(b0 - 139))
			i++
		case b0 >= 247 && b0 <= 250:
			if i > to-2 {
				return errNotSubset
			}
			err = cs.push(float64((b0-247)*256 + int(data[i+1]) + 108))
			i += 2
		case b0 >= 251 && b0 <= 254:
			if i > to-2 {
				return errNotSubset
			}
			err = cs.push(float64(-(b0-251)*256 - int(data[i+1]) - 108))
			i += 2
		case b0 == 255: // A 16.16 fixed number
			if i > to-5 {
				return errNotSubset
			}
			fixed := int32(uint32(data[i+1])<<24 | uint32(data[i+2])<<16 | uint32(data[i+3])<<8 | uint32(data[i+4]))
			err = cs.push(float64(fixed) / 65536.0)
			i += 5
		default:
			i++
			err = cs.operator(b0, &i, to, depth, s)
			if err == errReturn {
				return nil
			}
		}
		if err != nil {
			return err
		}
	}
	return nil
}

// errReturn ends a subroutine at its return operator.
var errReturn = errors.New("return")

// operator runs the operator of one byte, or of two, at *i.
func (cs *charString) operator(b0 int, i *int, to, depth int, s *[48]float64) error {
	var k int
	switch b0 {
	case 1, 3, 18, 23: // hstem, vstem, hstemhm, vstemhm
		cs.width(cs.sp%2 == 1)
		cs.stems += cs.sp / 2
		cs.sp = 0
	case 19, 20: // hintmask, cntrmask
		// The vstem hints before it, given with no operator
		cs.width(cs.sp%2 == 1)
		cs.stems += cs.sp / 2
		cs.sp = 0
		*i += (cs.stems + 7) / 8
		if *i > to {
			return errNotSubset
		}
	case 21: // rmoveto
		k = cs.width(cs.sp > 2)
		if err := cs.need(k + 2); err != nil {
			return err
		}
		cs.moveTo(s[k], s[k+1])
		cs.sp = 0
	case 22: // hmoveto
		k = cs.width(cs.sp > 1)
		if err := cs.need(k + 1); err != nil {
			return err
		}
		cs.moveTo(s[k], 0)
		cs.sp = 0
	case 4: // vmoveto
		k = cs.width(cs.sp > 1)
		if err := cs.need(k + 1); err != nil {
			return err
		}
		cs.moveTo(0, s[k])
		cs.sp = 0
	case 5: // rlineto
		for k = 0; k+1 < cs.sp; k += 2 {
			cs.lineTo(s[k], s[k+1])
		}
		cs.sp = 0
	case 6, 7: // hlineto, vlineto
		for k = 0; k < cs.sp; k++ {
			if (k%2 == 0) == (b0 == 6) {
				cs.lineTo(s[k], 0)
			} else {
				cs.lineTo(0, s[k])
			}
		}
		cs.sp = 0
	case 8: // rrcurveto
		for k = 0; k+5 < cs.sp; k += 6 {
			cs.curveTo(s[k], s[k+1], s[k+2], s[k+3], s[k+4], s[k+5])
		}
		cs.sp = 0
	case 24: // rcurveline
		for k = 0; k+5 < cs.sp-2; k += 6 {
			cs.curveTo(s[k], s[k+1], s[k+2], s[k+3], s[k+4], s[k+5])
		}
		if err := cs.need(k + 2); err != nil {
			return err
		}
		cs.lineTo(s[k], s[k+1])
		cs.sp = 0
	case 25: // rlinecurve
		for k = 0; k+1 < cs.sp-6; k += 2 {
			cs.lineTo(s[k], s[k+1])
		}
		if err := cs.need(k + 6); err != nil {
			return err
		}
		cs.curveTo(s[k], s[k+1], s[k+2], s[k+3], s[k+4], s[k+5])
		cs.sp = 0
	case 26: // vvcurveto
		k = 0
		dx1 := 0.0
		if cs.sp%2 == 1 {
			dx1 = s[0]
			k = 1
		}
		for ; k+3 < cs.sp; k += 4 {
			cs.curveTo(dx1, s[k], s[k+1], s[k+2], 0, s[k+3])
			dx1 = 0
		}
		cs.sp = 0
	case 27: // hhcurveto
		k = 0
		dy1 := 0.0
		if cs.sp%2 == 1 {
			dy1 = s[0]
			k = 1
		}
		for ; k+3 < cs.sp; k += 4 {
			cs.curveTo(s[k], dy1, s[k+1], s[k+2], s[k+3], 0)
			dy1 = 0
		}
		cs.sp = 0
	case 30, 31: // vhcurveto, hvcurveto
		horizontal := b0 == 31
		for k = 0; k+3 < cs.sp; k += 4 {
			last := 0.0
			if cs.sp-k == 5 {
				last = s[k+4]
			}
			if horizontal {
				cs.curveTo(s[k], 0, s[k+1], s[k+2], last, s[k+3])
			} else {
				cs.curveTo(0, s[k], s[k+1], s[k+2], s[k+3], last)
			}
			horizontal = !horizontal
		}
		cs.sp = 0
	case 10, 29: // callsubr, callgsubr
		subrs := cs.local
		if b0 == 29 {
			subrs = &cs.o.globalSubrs
		}
		if subrs == nil {
			return errNotSubset
		}
		v, err := cs.pop()
		if err != nil {
			return err
		}
		count := len(subrs.objects) - 1
		// Compared as a number before it is made an integer, as put and get
		n := v + float64(cffBias(count))
		if !(n >= 0 && n < float64(count)) { // NaN too
			return errNotSubset
		}
		number := int(n)
		return cs.run(subrs.objects[number], subrs.objects[number+1], depth+1)
	case 11: // return
		return errReturn
	case 14: // endchar
		w := 0
		if cs.sp == 1 || cs.sp == 5 {
			w = cs.width(true)
		} else {
			cs.width(false)
		}
		if cs.sp-w >= 4 {
			return &errSeac{"The CFF glyph number " + strconv.Itoa(cs.gid) +
				" is an accented letter of the seac operator, which DrawGlyphs does not draw."}
		}
		cs.sp = 0
		cs.ended = true
		return errReturn
	case 12:
		if *i >= to {
			return errNotSubset
		}
		b1 := int(cs.o.data[*i])
		*i++
		return cs.escaped(b1)
	default:
		return errNotSubset // A reserved operator
	}
	return nil
}

// escaped runs the operators of two bytes: the flex curves and the arithmetic.
func (cs *charString) escaped(b1 int) error {
	s := &cs.stack
	pop2 := func() (float64, float64, error) {
		b, err := cs.pop()
		if err != nil {
			return 0, 0, err
		}
		a, err := cs.pop()
		return a, b, err
	}
	switch b1 {
	case 35: // flex
		if err := cs.need(13); err != nil {
			return err
		}
		cs.curveTo(s[0], s[1], s[2], s[3], s[4], s[5])
		cs.curveTo(s[6], s[7], s[8], s[9], s[10], s[11])
		cs.sp = 0
	case 34: // hflex
		if err := cs.need(7); err != nil {
			return err
		}
		startY := cs.y
		cs.curveTo(s[0], 0, s[1], s[2], s[3], 0)
		cs.curveTo(s[4], 0, s[5], startY-cs.y, s[6], 0)
		cs.sp = 0
	case 36: // hflex1
		if err := cs.need(9); err != nil {
			return err
		}
		startY := cs.y
		cs.curveTo(s[0], s[1], s[2], s[3], s[4], 0)
		cs.curveTo(s[5], 0, s[6], s[7], s[8], startY-(cs.y+s[7]))
		cs.sp = 0
	case 37: // flex1
		if err := cs.need(11); err != nil {
			return err
		}
		dx := s[0] + s[2] + s[4] + s[6] + s[8]
		dy := s[1] + s[3] + s[5] + s[7] + s[9]
		cs.curveTo(s[0], s[1], s[2], s[3], s[4], s[5])
		if math.Abs(dx) > math.Abs(dy) {
			cs.curveTo(s[6], s[7], s[8], s[9], s[10], -dy)
		} else {
			cs.curveTo(s[6], s[7], s[8], s[9], -dx, s[10])
		}
		cs.sp = 0
	case 3, 4, 10, 11, 12, 15, 24: // and, or, add, sub, div, eq, mul
		a, b, err := pop2()
		if err != nil {
			return err
		}
		var v float64
		switch b1 {
		case 3:
			if a != 0 && b != 0 {
				v = 1
			}
		case 4:
			if a != 0 || b != 0 {
				v = 1
			}
		case 10:
			v = a + b
		case 11:
			v = a - b
		case 12:
			if b == 0 {
				return errNotSubset
			}
			v = a / b
		case 15:
			if a == b {
				v = 1
			}
		case 24:
			v = float64(a * b)
		}
		return cs.push(v)
	case 5, 9, 14, 26: // not, abs, neg, sqrt
		a, err := cs.pop()
		if err != nil {
			return err
		}
		switch b1 {
		case 5:
			if a == 0 {
				a = 1
			} else {
				a = 0
			}
		case 9:
			a = math.Abs(a)
		case 14:
			a = -a
		case 26:
			a = math.Sqrt(math.Abs(a))
		}
		return cs.push(a)
	case 18: // drop
		_, err := cs.pop()
		return err
	case 20: // put
		value, index, err := pop2() // The value, then the index on top of it
		if err != nil {
			return err
		}
		// Compared as a number before it is made an integer, as index
		if !(index >= 0 && index < float64(len(cs.storage))) { // NaN too
			return errNotSubset
		}
		cs.storage[int(index)] = value
	case 21: // get
		v, err := cs.pop()
		if err != nil {
			return err
		}
		if !(v >= 0 && v < float64(len(cs.storage))) { // NaN too
			return errNotSubset
		}
		return cs.push(cs.storage[int(v)])
	case 22: // ifelse
		v1, v2, err := pop2()
		if err != nil {
			return err
		}
		s1, s2, err := pop2()
		if err != nil {
			return err
		}
		if v1 <= v2 {
			return cs.push(s1)
		}
		return cs.push(s2)
	case 23: // random, the same number in every port
		return cs.push(0.5)
	case 27: // dup
		a, err := cs.pop()
		if err != nil {
			return err
		}
		if err := cs.push(a); err != nil {
			return err
		}
		return cs.push(a)
	case 28: // exch
		a, b, err := pop2()
		if err != nil {
			return err
		}
		if err := cs.push(b); err != nil {
			return err
		}
		return cs.push(a)
	case 29: // index
		v, err := cs.pop()
		if err != nil {
			return err
		}
		// Compared as a number before it is made an integer, which differs
		// from language to language and from processor to processor for
		// one too large
		if math.IsNaN(v) || v >= float64(cs.sp) {
			return errNotSubset
		}
		index := 0
		if v > 0 {
			index = int(v)
		}
		return cs.push(s[cs.sp-1-index])
	case 30: // roll
		jv, err := cs.pop()
		if err != nil {
			return err
		}
		nv, err := cs.pop()
		if err != nil {
			return err
		}
		// Compared as numbers before they are made integers, as index
		if !(nv >= 1 && nv <= float64(cs.sp)) || !(math.Abs(jv) <= 1e9) {
			return errNotSubset
		}
		j, n := int(jv), int(nv)
		part := make([]float64, n)
		for m := 0; m < n; m++ {
			part[((m+j)%n+n)%n] = s[cs.sp-n+m]
		}
		copy(s[cs.sp-n:cs.sp], part)
	default:
		// dotsection (0), deprecated, and the reserved ones: no drawing
		cs.sp = 0
	}
	return nil
}
