// simplescatterchart.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The colors of a simple scatter chart unless set, those of SimpleBarChart:
// a light panel, gray dots a little transparent, the highlighted points navy,
// the grid a light gray, the trend line and the numbers dark gray.
const (
	simpleScatterDot   int32 = 0x8C96A3
	simpleScatterGrid  int32 = 0xD5D9DE
	simpleScatterTrend int32 = 0x5F6873
	simpleScatterAlpha       = 0.55
)

// scatterPoint is a point of a simple scatter chart, highlighted when it has
// a name.
type scatterPoint struct {
	x, y float32
	name string
}

// SimpleScatterChart is a chart of points on two axes that shows how two
// values go together: many points as small gray dots, a little transparent so
// that a cluster shows darker, a few of them highlighted, navy and labeled
// with their names beside them, and the trend line of the points if asked,
// labeled at its end. A light horizontal grid alone, no border or legend, on
// a light panel, with a title and a subtitle at the top left, in the look of
// SimpleBarChart.
//
// In a PDF/UA document the chart is one figure, described from its data: the
// number of points, the highlighted ones and their values, and which way the
// trend goes, unless an alternate description is set. It draws with the page
// alone, and needs no other chart. In PDF/A-1, which has no transparency, the
// dots are drawn opaque.
type SimpleScatterChart struct {
	f1, f2                 *Font // the title and the names; the rest
	x, y, w, h             float32
	title, subtitle        string
	xAxisTitle, yAxisTitle string
	altDescription         string
	points                 []scatterPoint
	trendLine              bool
	xMin, xMax, yMin, yMax float32
	xRangeSet, yRangeSet   bool
	panelColor, dotColor   int32
	highlightColor         int32
	maxFractionDigits      int
}

// NewSimpleScatterChart creates a simple scatter chart, 400 by 300 points.
//   - f1: the font of the title and of the names of the highlighted points,
//     a bold one.
//   - f2: the font of the subtitle, the axis titles and the numbers.
func NewSimpleScatterChart(f1, f2 *Font) *SimpleScatterChart {
	return &SimpleScatterChart{
		f1: f1, f2: f2, w: 400, h: 300, maxFractionDigits: 2,
		panelColor: simpleBarPanel, dotColor: simpleScatterDot, highlightColor: simpleBarHighlight,
	}
}

// SetTitle sets the title, drawn at the top left of the chart.
func (chart *SimpleScatterChart) SetTitle(title string) *SimpleScatterChart {
	chart.title = title
	return chart
}

// SetSubtitle sets the subtitle, drawn small and gray under the title.
func (chart *SimpleScatterChart) SetSubtitle(subtitle string) *SimpleScatterChart {
	chart.subtitle = subtitle
	return chart
}

// SetXAxisTitle sets the title of the horizontal axis, drawn under it.
func (chart *SimpleScatterChart) SetXAxisTitle(title string) *SimpleScatterChart {
	chart.xAxisTitle = title
	return chart
}

// SetYAxisTitle sets the title of the vertical axis, drawn up its left side.
func (chart *SimpleScatterChart) SetYAxisTitle(title string) *SimpleScatterChart {
	chart.yAxisTitle = title
	return chart
}

// SetAltDescription sets the alternate description of the chart in a PDF/UA
// document. Without one the chart is described from its data.
func (chart *SimpleScatterChart) SetAltDescription(altDescription string) *SimpleScatterChart {
	chart.altDescription = altDescription
	return chart
}

// Add adds a point, drawn as a small gray dot. A point whose x or y is not a
// number is left out.
func (chart *SimpleScatterChart) Add(x, y float32) *SimpleScatterChart {
	chart.points = append(chart.points, scatterPoint{x: x, y: y})
	return chart
}

// AddNamed adds a point that is highlighted: a navy dot with its name beside
// it.
func (chart *SimpleScatterChart) AddNamed(name string, x, y float32) *SimpleScatterChart {
	chart.points = append(chart.points, scatterPoint{x: x, y: y, name: name})
	return chart
}

// SetTrendLine draws the trend line of the points, their least squares line,
// thin and dark gray, labeled "trend" at its right end.
func (chart *SimpleScatterChart) SetTrendLine(trendLine bool) *SimpleScatterChart {
	chart.trendLine = trendLine
	return chart
}

// SetXAxisRange sets the range of the horizontal axis. Without it the range
// is that of the points, rounded to steps.
func (chart *SimpleScatterChart) SetXAxisRange(lo, hi float32) *SimpleScatterChart {
	chart.xMin, chart.xMax, chart.xRangeSet = lo, hi, hi > lo
	return chart
}

// SetYAxisRange sets the range of the vertical axis. Without it the range is
// that of the points, rounded to steps.
func (chart *SimpleScatterChart) SetYAxisRange(lo, hi float32) *SimpleScatterChart {
	chart.yMin, chart.yMax, chart.yRangeSet = lo, hi, hi > lo
	return chart
}

// SetPanelColor sets the color of the panel behind the chart, or -1 for
// none. It is light gray, 0xF1F3F6, unless set.
func (chart *SimpleScatterChart) SetPanelColor(c int32) *SimpleScatterChart {
	chart.panelColor = c
	return chart
}

// SetDotColor sets the color of the points that are not highlighted. It is
// gray, 0x8C96A3, unless set.
func (chart *SimpleScatterChart) SetDotColor(c int32) *SimpleScatterChart {
	chart.dotColor = c
	return chart
}

// SetHighlightColor sets the color of the highlighted points and of their
// names. It is navy, 0x1F3A5F, unless set.
func (chart *SimpleScatterChart) SetHighlightColor(c int32) *SimpleScatterChart {
	chart.highlightColor = c
	return chart
}

// SetLocation sets the top left corner of the chart.
func (chart *SimpleScatterChart) SetLocation(x, y float32) Drawable {
	chart.x = x
	chart.y = y
	return chart
}

// SetSize sets the width and the height of the chart.
func (chart *SimpleScatterChart) SetSize(w, h float32) *SimpleScatterChart {
	chart.w = w
	chart.h = h
	return chart
}

// The space around the chart's content, a size of its numbers.
func (chart *SimpleScatterChart) pad() float32 {
	return chart.f2.size
}

// The size of the subtitle.
func (chart *SimpleScatterChart) subtitleSize() float32 {
	return float32(0.8 * chart.f2.size)
}

// The height above the plot: the padding, the title and the subtitle.
func (chart *SimpleScatterChart) top() float32 {
	top := chart.pad()
	if chart.title != "" {
		top += chart.f1.GetAscent(chart.f1.size) + chart.f1.GetDescent(chart.f1.size)
	}
	if chart.subtitle != "" {
		top += float32(1.4 * chart.subtitleSize())
	}
	if chart.title != "" || chart.subtitle != "" {
		top += chart.f2.size
	}
	return top
}

// The axis of a range of values: its first and last value, and its steps.
type scatterAxis struct {
	min, max float32
	lines    int
}

// axisOf returns the axis of the values of the points: the range set, or the
// range of the values rounded to steps.
func axisOf(values []float32, lo, hi float32, set bool) scatterAxis {
	if set {
		return scatterAxis{lo, hi, max(roundMaxAndMinValues(hi, lo).numOfGridLines, 1)}
	}
	lo, hi = values[0], values[0]
	for _, v := range values[1:] {
		lo, hi = min(lo, v), max(hi, v)
	}
	if hi == lo {
		hi = lo + flatDataSpan(lo)
	}
	round := roundMaxAndMinValues(hi, lo)
	return scatterAxis{round.minValue, round.maxValue, round.numOfGridLines}
}

// label formats a number of an axis with the decimals its step needs.
func (chart *SimpleScatterChart) label(value, step float32) string {
	return format(value, fractionDigitsOf(step, chart.maxFractionDigits), chart.maxFractionDigits)
}

// The points whose x and y are numbers.
func (chart *SimpleScatterChart) valid() []scatterPoint {
	valid := make([]scatterPoint, 0, len(chart.points))
	for _, p := range chart.points {
		if isValue(p.x) && isValue(p.y) {
			valid = append(valid, p)
		}
	}
	return valid
}

// trend returns the slope and the intercept of the least squares line of the
// points, and false when there is none: fewer than two points, or all at one
// x.
func trend(points []scatterPoint) (float64, float64, bool) {
	n := float64(len(points))
	if n < 2 {
		return 0, 0, false
	}
	var sx, sy, sxx, sxy float64
	for _, p := range points {
		x, y := float64(p.x), float64(p.y)
		sx += x
		sy += y
		sxx += float64(x * x)
		sxy += float64(x * y)
	}
	d := float64(n*sxx) - float64(sx*sx)
	if d == 0 {
		return 0, 0, false
	}
	slope := (float64(n*sxy) - float64(sx*sy)) / d
	return slope, (sy - float64(slope*sx)) / n, true
}

// getAltDescription returns the alternate description: the one set, or the
// title, the number of points, the highlighted ones and the trend.
func (chart *SimpleScatterChart) getAltDescription() string {
	if chart.altDescription != "" {
		return chart.altDescription
	}
	points := chart.valid()
	var sb strings.Builder
	sb.WriteString(chart.title)
	if chart.subtitle != "" {
		if sb.Len() > 0 {
			sb.WriteString(", ")
		}
		sb.WriteString(chart.subtitle)
	}
	if sb.Len() == 0 {
		sb.WriteString("Scatter chart")
	}
	sb.WriteString(": " + strconv.Itoa(len(points)) + " points")
	if chart.xAxisTitle != "" && chart.yAxisTitle != "" {
		sb.WriteString(", " + chart.yAxisTitle + " against " + chart.xAxisTitle)
	}
	named := 0
	for _, p := range points {
		if p.name == "" {
			continue
		}
		if named == 0 {
			sb.WriteString("; ")
		} else {
			sb.WriteString(", ")
		}
		sb.WriteString(p.name + " " + format(p.x, 0, chart.maxFractionDigits) + " and " +
			format(p.y, 0, chart.maxFractionDigits))
		named++
	}
	if chart.trendLine {
		if slope, _, ok := trend(points); ok {
			switch {
			case slope > 0:
				sb.WriteString("; the trend rises")
			case slope < 0:
				sb.WriteString("; the trend falls")
			default:
				sb.WriteString("; the trend is flat")
			}
		}
	}
	sb.WriteString(".")
	return sb.String()
}

// DrawOn draws the chart on the page and returns the x and y coordinates of
// its bottom right corner.
func (chart *SimpleScatterChart) DrawOn(page *Page) [2]float32 {
	points := chart.valid()
	if page == nil || len(points) == 0 { // Measured, or nothing to draw
		return [2]float32{chart.x + chart.w, chart.y + chart.h}
	}
	f1, f2 := chart.f1, chart.f2
	size := f2.size
	pad := chart.pad()
	ascent := f2.GetAscent(size)

	xs := make([]float32, len(points))
	ys := make([]float32, len(points))
	for i, p := range points {
		xs[i], ys[i] = p.x, p.y
	}
	xAxis := axisOf(xs, chart.xMin, chart.xMax, chart.xRangeSet)
	yAxis := axisOf(ys, chart.yMin, chart.yMax, chart.yRangeSet)
	xStep := (xAxis.max - xAxis.min) / float32(xAxis.lines)
	yStep := (yAxis.max - yAxis.min) / float32(yAxis.lines)

	// The plot: right of the y title and the widest number, above the
	// numbers and the x title
	widest := float32(0)
	for i := 0; i <= yAxis.lines; i++ {
		widest = max(widest, f2.StringWidth(size, chart.label(yAxis.min+float32(yStep*float32(i)), yStep)))
	}
	left := chart.x + pad + widest + float32(size/2.0)
	if chart.yAxisTitle != "" {
		left += float32(1.8 * size) // The title, as far from the numbers as the x title is
	}
	right := chart.x + chart.w - pad
	top := chart.y + chart.top() + float32(ascent/2.0) // Room for the top number
	bottom := chart.y + chart.h - pad - float32(1.4*size)
	if chart.xAxisTitle != "" {
		bottom -= float32(1.4 * size)
	}
	if right <= left || bottom <= top {
		return [2]float32{chart.x + chart.w, chart.y + chart.h}
	}
	xAt := func(v float32) float32 {
		return left + float32((v-xAxis.min)*(right-left))/(xAxis.max-xAxis.min)
	}
	yAt := func(v float32) float32 {
		return bottom - float32((v-yAxis.min)*(bottom-top))/(yAxis.max-yAxis.min)
	}

	page.AddBDC(structelem.Figure, "", "", chart.getAltDescription())
	page.SaveGraphicsState()

	if chart.panelColor >= 0 {
		page.SetBrushColor(chart.panelColor)
		page.FillRect(chart.x, chart.y, chart.w, chart.h)
	}
	baseline := chart.y + pad
	if chart.title != "" {
		baseline += f1.GetAscent(f1.size)
		page.drawString(f1, f1.size, chart.title, chart.x+pad, baseline, colorToRGB(simpleBarInk), nil)
		baseline += f1.GetDescent(f1.size)
	}
	if chart.subtitle != "" {
		baseline += float32(1.4 * chart.subtitleSize())
		page.drawString(f2, chart.subtitleSize(), chart.subtitle, chart.x+pad, baseline,
			colorToRGB(simpleBarMuted), nil)
	}

	// The horizontal grid and the numbers of both axes, each where it
	// leaves the one before it room
	page.SetPenColor(simpleScatterGrid)
	page.SetPenWidth(0.5)
	every := axisLabelStep((bottom-top)/float32(yAxis.lines), f2.bodyHeight, yAxis.lines)
	for i := 0; i <= yAxis.lines; i++ {
		v := yAxis.min + float32(yStep*float32(i))
		y := yAt(v)
		page.DrawLine(left, y, right, y)
		if i%every == 0 {
			label := chart.label(v, yStep)
			page.drawString(f2, size, label, left-float32(size/2.0)-f2.StringWidth(size, label),
				y+float32(ascent/2.0), colorToRGB(simpleBarMuted), nil)
		}
	}
	xWidest := float32(0)
	for i := 0; i <= xAxis.lines; i++ {
		xWidest = max(xWidest, f2.StringWidth(size, chart.label(xAxis.min+float32(xStep*float32(i)), xStep)))
	}
	every = axisLabelStep((right-left)/float32(xAxis.lines), xWidest+pad, xAxis.lines)
	for i := 0; i <= xAxis.lines; i += every {
		v := xAxis.min + float32(xStep*float32(i))
		label := chart.label(v, xStep)
		page.drawString(f2, size, label, xAt(v)-float32(f2.StringWidth(size, label)/2.0),
			bottom+float32(1.4*size), colorToRGB(simpleBarMuted), nil)
	}
	if chart.xAxisTitle != "" {
		page.drawString(f2, size, chart.xAxisTitle,
			left+float32((right-left-f2.StringWidth(size, chart.xAxisTitle))/2.0),
			bottom+float32(2.8*size), colorToRGB(simpleBarInk), nil)
	}
	if chart.yAxisTitle != "" {
		page.SetTextRotation(-90)
		page.drawString(f2, size, chart.yAxisTitle, chart.x+pad+ascent,
			bottom-float32((bottom-top-f2.StringWidth(size, chart.yAxisTitle))/2.0), colorToRGB(simpleBarInk), nil)
		page.SetTextRotation(0)
	}

	// The trend line, from the left of the plot to its right, within its
	// height
	if slope, intercept, ok := trend(points); chart.trendLine && ok {
		x1, x2 := float64(xAxis.min), float64(xAxis.max)
		y1 := float64(float64(slope*x1)) + intercept
		y2 := float64(float64(slope*x2)) + intercept
		lo, hi := float64(yAxis.min), float64(yAxis.max)
		clip := func(x, y float64) (float64, float64) {
			if slope != 0 && y < lo {
				return (lo - intercept) / slope, lo
			}
			if slope != 0 && y > hi {
				return (hi - intercept) / slope, hi
			}
			return x, y
		}
		x1, y1 = clip(x1, y1)
		x2, y2 = clip(x2, y2)
		page.SetPenColor(simpleScatterTrend)
		page.SetPenWidth(0.75)
		px1, py1, px2, py2 := xAt(float32(x1)), yAt(float32(y1)), xAt(float32(x2)), yAt(float32(y2))
		page.DrawLine(px1, py1, px2, py2)
		// The label over the end of the line, lifted by as much as a falling
		// line rises under it
		label := "trend"
		width := f2.StringWidth(chart.subtitleSize(), label)
		lift := float32(size / 3.0)
		if px2 > px1 && py2 > py1 {
			lift += float32(width * ((py2 - py1) / (px2 - px1)))
		}
		page.drawString(f2, chart.subtitleSize(), label, px2-width, py2-lift, colorToRGB(simpleScatterTrend), nil)
	}

	// The dots, then the highlighted points and their names over them
	page.SaveGraphicsState()
	if !page.pdf.isPDFA1() {
		gs := NewGraphicsState()
		gs.SetAlphaNonStroking(simpleScatterAlpha)
		page.SetGraphicsState(gs)
	}
	page.SetBrushColor(chart.dotColor)
	r := float32(0.3 * size)
	for _, p := range points {
		if p.name == "" {
			page.FillCircle(xAt(p.x), yAt(p.y), r)
		}
	}
	page.RestoreGraphicsState()
	big := float32(0.42 * size)
	for _, p := range points {
		if p.name == "" {
			continue
		}
		px, py := xAt(p.x), yAt(p.y)
		page.SetBrushColor(chart.highlightColor)
		page.FillCircle(px, py, big)
		width := f1.StringWidth(size, p.name)
		lx := px + big + float32(size/3.0)
		if lx+width > right {
			lx = px - big - float32(size/3.0) - width // At the left, not past the plot
		}
		page.drawString(f1, size, p.name, lx, py+float32(ascent/2.0), colorToRGB(chart.highlightColor), nil)
	}

	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(chart.x, chart.y, chart.w, chart.h)
	page.AddEMC()
	return [2]float32{chart.x + chart.w, chart.y + chart.h}
}
