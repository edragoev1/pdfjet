// tablelines.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) on purpose, so that Go's arm64 compiler does not fuse it with
// an addition and round otherwise than amd64 and the other ports. Keep the
// wrapping; see "Floating point on ARM" in README.md. check-no-fma.sh fails
// if one is removed.

import (
	"slices"

	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/capstyle"
)

// tableLines holds the borders of the cells that a table draws on a page,
// kept while the cells are drawn and written as continuous lines when the
// table is done with the page: each cell stroked its own top and left, one
// short line for each edge, three or four times the lines of one line across
// each row. Lines on the same y, or the same x, that touch or overlap and
// have the same pen become one line, which covers the same part of the page
// as they did.
//
// A line is drawn later than its cell drew it, so what was drawn over it in
// between must be drawn over it still: before a background is filled over a
// line that is kept, or next to it, the lines are written; and before a
// drawable or a point is drawn, as either can draw anything anywhere, with
// the pen the cell left. Lines of different pens that overlap are written in
// the order the cells drew them, so the one on top is on top still.
type tableLines struct {
	pending []*tableSegment // The lines in the order the cells drew them
	// The pen of the borders of the last cell, which a cell drawing its
	// borders itself would have left the page with, and the count of the
	// page's pen changes then: when the pen was not set after it, the lines
	// leave the page with this pen, as the cell would have. The cells do not
	// set the pen of the page themselves, which would write it once for the
	// cell and again for its lines.
	hasCellPen       bool
	cellPenColor     [3]float32
	cellPenWidth     float32
	penChangesAtCell int
}

// tableSegment is a line: horizontal at y = at, from x = from to x = to, or
// vertical at x = at, from y = from to y = to, with the pen of the page when
// the cell drew it, and how far its caps reach past its ends.
type tableSegment struct {
	vertical bool
	at       float32
	from     float32
	to       float32
	color    [3]float32
	width    float32
	cap      float32
}

func (line *tableSegment) samePen(other *tableSegment) bool {
	return line.color == other.color && line.width == other.width
}

// The part of the page the line covers: left, top, right, bottom.
func (line *tableSegment) x0() float32 {
	if line.vertical {
		return line.at - float32(line.width/2)
	}
	return line.from - line.cap
}

func (line *tableSegment) y0() float32 {
	if line.vertical {
		return line.from - line.cap
	}
	return line.at - float32(line.width/2)
}

func (line *tableSegment) x1() float32 {
	if line.vertical {
		return line.at + float32(line.width/2)
	}
	return line.to + line.cap
}

func (line *tableSegment) y1() float32 {
	if line.vertical {
		return line.to + line.cap
	}
	return line.at + float32(line.width/2)
}

// tableLineMargin is the margin around a line, in points, within which
// another line may paint a pixel of its own: a line covers whole pixels of a
// screen or a printer, the hairline of width 0 one at least, so two lines
// that only meet, end to end or edge to edge, may paint one pixel.
const tableLineMargin = 1

// overlaps returns true when the lines may paint a pixel in common: they are
// then stroked in the order the cells drew them.
func (line *tableSegment) overlaps(other *tableSegment) bool {
	const m = 2 * tableLineMargin
	return line.x0() < other.x1()+m && other.x0() < line.x1()+m &&
		line.y0() < other.y1()+m && other.y0() < line.y1()+m
}

// addBorders keeps the borders of a cell that drawBorders would stroke, with
// the pen of the cell, color and width: its top, bottom, left and right, as
// it draws them.
func (lines *tableLines) addBorders(page *Page, properties uint32, c int32, width, x, y, cellW, cellH float32) {
	// As SetPenColor makes the color
	r := float32(float32((c>>16)&0xff) / 255.0)
	g := float32(float32((c>>8)&0xff) / 255.0)
	b := float32(float32((c)&0xff) / 255.0)
	lines.cellPenColor = [3]float32{r, g, b}
	lines.cellPenWidth = width
	lines.hasCellPen = true
	lines.penChangesAtCell = page.penChanges
	// Half the pen width, so that the corners of the borders close.
	hWidth := float32(width / 2)
	if properties&border.Top != 0 {
		lines.add(page, false, y, x-hWidth, x+cellW)
	}
	if properties&border.Bottom != 0 {
		lines.add(page, false, y+cellH, x-hWidth, x+cellW)
	}
	if properties&border.Left != 0 {
		lines.add(page, true, x, y-hWidth, y+cellH+hWidth)
	}
	if properties&border.Right != 0 {
		lines.add(page, true, x+cellW, y-hWidth, y+cellH+hWidth)
	}
}

func (lines *tableLines) add(page *Page, vertical bool, at, from, to float32) {
	width := lines.cellPenWidth
	var cap float32
	if page.lineCapStyle != capstyle.Butt {
		cap = width / 2
	}
	lines.pending = append(lines.pending, &tableSegment{
		vertical: vertical, at: at, from: from, to: to,
		color: lines.cellPenColor, width: width, cap: cap})
}

// beforeFill writes the lines before a background is filled over the
// rectangle left, top, right, bottom, when it covers a line, or meets it
// along an edge: drawn over the background, the line would look different
// where it is not a whole pixel wide. A line that only touches the rectangle
// at a corner, as the lines of the row above meet the background of a cell
// where they end, is kept; one that goes into it, however little, as the end
// of a wider line does, is written first, so the background covers it as it
// did.
func (lines *tableLines) beforeFill(page *Page, left, top, right, bottom float32) {
	for _, line := range lines.pending {
		x0 := max(line.x0(), left)
		y0 := max(line.y0(), top)
		x1 := min(line.x1(), right)
		y1 := min(line.y1(), bottom)
		if x0 > x1 || y0 > y1 {
			continue // Apart
		}
		if (x0 == x1 && y1-y0 <= line.width) || (y0 == y1 && x1-x0 <= line.width) {
			continue // At a corner
		}
		lines.flush(page)
		return
	}
}

// flush writes the lines kept so far, joined, and leaves the page with the
// pen it would have had, had each cell drawn its own borders: what is drawn
// next finds the pen the last cell left, or the one set after it.
func (lines *tableLines) flush(page *Page) {
	if len(lines.pending) == 0 {
		return
	}
	var color [3]float32
	var width float32
	if lines.hasCellPen && page.penChanges == lines.penChangesAtCell {
		color = lines.cellPenColor
		width = lines.cellPenWidth
	} else {
		color = page.GetPenColor()
		width = page.GetPenWidth()
	}
	lines.hasCellPen = false
	// The lines of a pen go together, unless a line of another pen that a
	// cell drew after them overlaps the line: it starts a new group, after
	// the line of the other pen.
	var groups [][]*tableSegment
	for _, line := range lines.pending {
		target := -1
		for i := len(groups) - 1; i >= 0; i-- {
			if groups[i][0].samePen(line) {
				target = i
				break
			}
			if overlapsAny(groups[i], line) {
				break
			}
		}
		if target == -1 {
			groups = append(groups, nil)
			target = len(groups) - 1
		}
		groups[target] = append(groups[target], line)
	}
	lines.pending = nil

	page.AddArtifactBMC()
	for _, group := range groups {
		page.SetPenColorRGB(group[0].color)
		page.SetPenWidth(group[0].width)
		// The horizontal lines from the top down, each from the left, then
		// the vertical lines from the left, each from the top.
		sorted := slices.Clone(group)
		slices.SortStableFunc(sorted, compareSegments)
		// A line that two cells drew, such as the line under the header,
		// which the first row draws over, was stroked twice, darker where it
		// is not a whole pixel wide: it is stroked twice still, once in each
		// of two layers. The ends of the lines of two cells next to each
		// other overlap by no more than the width of the pen.
		var layers [][]*tableSegment
		for _, next := range sorted {
			layer := -1
			var last *tableSegment
			for k, candidate := range layers {
				last = candidate[len(candidate)-1]
				if last.vertical != next.vertical || last.at != next.at ||
					last.to-next.from <= float32(1.5*next.width) {
					layer = k
					break
				}
			}
			if layer == -1 {
				layers = append(layers, nil)
				layer = len(layers) - 1
				last = nil
			}
			if last != nil && last.vertical == next.vertical &&
				last.at == next.at && next.from <= last.to {
				if next.to > last.to {
					last.to = next.to
				}
			} else {
				copied := *next
				layers[layer] = append(layers[layer], &copied)
			}
		}
		for _, layer := range layers {
			for _, line := range layer {
				drawTableLine(page, line)
			}
			page.StrokePath()
		}
	}
	page.SetPenColorRGB(color)
	page.SetPenWidth(width)
	page.AddEMC()
}

func overlapsAny(group []*tableSegment, line *tableSegment) bool {
	for _, other := range group {
		if other.overlaps(line) {
			return true
		}
	}
	return false
}

func drawTableLine(page *Page, line *tableSegment) {
	if line.vertical {
		page.MoveTo(line.at, line.from)
		page.LineTo(line.at, line.to)
	} else {
		page.MoveTo(line.from, line.at)
		page.LineTo(line.to, line.at)
	}
}

// compareSegments puts horizontal before vertical, then orders by where the
// line is and where it starts and ends: the same order in the four ports.
func compareSegments(a, b *tableSegment) int {
	if a.vertical != b.vertical {
		if a.vertical {
			return 1
		}
		return -1
	}
	if c := compareFloats(a.at, b.at); c != 0 {
		return c
	}
	if c := compareFloats(a.from, b.from); c != 0 {
		return c
	}
	return compareFloats(a.to, b.to)
}

func compareFloats(a, b float32) int {
	if a < b {
		return -1
	}
	if a > b {
		return 1
	}
	return 0
}
