// simplelinechart.go
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
	"sort"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The colors of a simple line chart unless set, those of SimpleBarChart: a
// light panel, gray lines, the highlighted line navy, the grid light gray.
const simpleLineLine int32 = 0x8C96A3

// simpleLine is a line of a simple line chart: its name and a value for each
// category, NaN where it has none.
type simpleLine struct {
	name   string
	values []float32
}

// SimpleLineChart is a chart of a few lines over time, or over any
// categories in their order: the categories left to right, each line labeled
// at its right end with its name and its last value, so that it needs no
// legend; one line highlighted in navy, its label bold, the others gray. A
// light horizontal grid alone, no border, on a light panel, with a title and a
// subtitle at the top left, in the look of SimpleBarChart and
// SimpleScatterChart. A value that is NaN leaves a gap in its line.
//
// In a PDF/UA document the chart is one figure, described from its data: the
// lines, their first and last values and their highest, unless an alternate
// description is set. It draws with the page alone, and needs no other chart.
// Please see Example_25.
type SimpleLineChart struct {
	f1, f2                 *Font // the title and the highlighted label; the rest
	x, y, w, h             float32
	title, subtitle        string
	xAxisTitle, yAxisTitle string
	altDescription         string
	categories             []string
	lines                  []*simpleLine
	highlight              int // the index of the highlighted line, or -1
	markers                bool
	valueLabels            bool
	areaFilled             bool
	yMin, yMax             float32
	yRangeSet              bool
	panelColor, lineColor  int32
	highlightColor         int32
	maxFractionDigits      int
}

// NewSimpleLineChart creates a simple line chart, 400 by 300 points.
//   - f1: the font of the title and of the highlighted line's label, a bold
//     one.
//   - f2: the font of the subtitle, the axis titles, the numbers, the
//     categories and the other labels.
func NewSimpleLineChart(f1, f2 *Font) *SimpleLineChart {
	return &SimpleLineChart{
		f1: f1, f2: f2, w: 400, h: 300, highlight: -1, maxFractionDigits: 2,
		panelColor: simpleBarPanel, lineColor: simpleLineLine, highlightColor: simpleBarHighlight,
	}
}

// SetTitle sets the title, drawn at the top left of the chart.
func (chart *SimpleLineChart) SetTitle(title string) *SimpleLineChart {
	chart.title = title
	return chart
}

// SetSubtitle sets the subtitle, drawn small and gray under the title.
func (chart *SimpleLineChart) SetSubtitle(subtitle string) *SimpleLineChart {
	chart.subtitle = subtitle
	return chart
}

// SetXAxisTitle sets the title of the categories, drawn under them.
func (chart *SimpleLineChart) SetXAxisTitle(title string) *SimpleLineChart {
	chart.xAxisTitle = title
	return chart
}

// SetYAxisTitle sets the title of the vertical axis, drawn up its left side.
func (chart *SimpleLineChart) SetYAxisTitle(title string) *SimpleLineChart {
	chart.yAxisTitle = title
	return chart
}

// SetAltDescription sets the alternate description of the chart in a PDF/UA
// document. Without one the chart is described from its data.
func (chart *SimpleLineChart) SetAltDescription(altDescription string) *SimpleLineChart {
	chart.altDescription = altDescription
	return chart
}

// SetCategories sets the categories, the years or the months, in their order
// left to right.
func (chart *SimpleLineChart) SetCategories(categories ...string) *SimpleLineChart {
	chart.categories = categories
	return chart
}

// AddLine adds a line: its name, and a value for each category, NaN where it
// has none.
func (chart *SimpleLineChart) AddLine(name string, values []float32) *SimpleLineChart {
	chart.lines = append(chart.lines, &simpleLine{name, values})
	return chart
}

// SetHighlight highlights the line of the index, the first line 0: navy and
// a little wider, its label bold.
func (chart *SimpleLineChart) SetHighlight(index int) *SimpleLineChart {
	chart.highlight = index
	return chart
}

// SetMarkers draws a dot at each value of the lines.
func (chart *SimpleLineChart) SetMarkers(markers bool) *SimpleLineChart {
	chart.markers = markers
	return chart
}

// SetValueLabels writes each value of the lines by its point, small, where
// the line leaves it room: above a peak, under a valley, before or after a
// point on a rise or a fall; but the last of each line, which its label at the
// end of the line has.
func (chart *SimpleLineChart) SetValueLabels(valueLabels bool) *SimpleLineChart {
	chart.valueLabels = valueLabels
	return chart
}

// SetAreaFilled fills the area under each line, down to 0 or to the bottom of
// the plot, in a light tint of the line's color, which makes an area chart.
// The highlighted line's area is drawn last, over the others.
func (chart *SimpleLineChart) SetAreaFilled(areaFilled bool) *SimpleLineChart {
	chart.areaFilled = areaFilled
	return chart
}

// SetYAxisRange sets the range of the vertical axis. Without it the range is
// that of the values, rounded to steps.
func (chart *SimpleLineChart) SetYAxisRange(lo, hi float32) *SimpleLineChart {
	chart.yMin, chart.yMax, chart.yRangeSet = lo, hi, hi > lo
	return chart
}

// SetPanelColor sets the color of the panel behind the chart, or -1 for
// none. It is light gray, 0xF1F3F6, unless set.
func (chart *SimpleLineChart) SetPanelColor(c int32) *SimpleLineChart {
	chart.panelColor = c
	return chart
}

// SetLineColor sets the color of the lines that are not highlighted. It is
// gray, 0x8C96A3, unless set.
func (chart *SimpleLineChart) SetLineColor(c int32) *SimpleLineChart {
	chart.lineColor = c
	return chart
}

// SetHighlightColor sets the color of the highlighted line and of its label.
// It is navy, 0x1F3A5F, unless set.
func (chart *SimpleLineChart) SetHighlightColor(c int32) *SimpleLineChart {
	chart.highlightColor = c
	return chart
}

// SetLocation sets the top left corner of the chart.
func (chart *SimpleLineChart) SetLocation(x, y float32) Drawable {
	chart.x = x
	chart.y = y
	return chart
}

// SetSize sets the width and the height of the chart.
func (chart *SimpleLineChart) SetSize(w, h float32) *SimpleLineChart {
	chart.w = w
	chart.h = h
	return chart
}

// The space around the chart's content, a size of its numbers.
func (chart *SimpleLineChart) pad() float32 {
	return chart.f2.size
}

// The size of the subtitle.
func (chart *SimpleLineChart) subtitleSize() float32 {
	return float32(0.8 * chart.f2.size)
}

// The height above the plot: the padding, the title and the subtitle.
func (chart *SimpleLineChart) top() float32 {
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

// The number of categories the chart draws: the categories set, or the
// longest line when there are none.
func (chart *SimpleLineChart) count() int {
	n := len(chart.categories)
	if n == 0 {
		for _, line := range chart.lines {
			n = max(n, len(line.values))
		}
	}
	return n
}

// valueOf returns the value of the line at the category, and whether it has
// one there.
func (line *simpleLine) valueOf(i int) (float32, bool) {
	if i < len(line.values) && isValue(line.values[i]) {
		return line.values[i], true
	}
	return 0, false
}

// The number of an axis, with the decimals its step needs, its digits grouped.
func (chart *SimpleLineChart) label(value, step float32) string {
	return groupDigits(format(value, fractionDigitsOf(step, chart.maxFractionDigits), chart.maxFractionDigits))
}

// A value of a line, as its label writes it.
func (chart *SimpleLineChart) value(v float32) string {
	return groupDigits(format(v, 0, chart.maxFractionDigits))
}

// The name of the category of the index.
func (chart *SimpleLineChart) category(i int) string {
	if i < len(chart.categories) {
		return chart.categories[i]
	}
	return ""
}

// The font of the label of the line of the index.
func (chart *SimpleLineChart) fontOf(index int) *Font {
	if index == chart.highlight {
		return chart.f1
	}
	return chart.f2
}

// The label at the end of a line: its name and its last value, and the
// category of that value.
func (chart *SimpleLineChart) endOf(line *simpleLine) (string, int, bool) {
	for i := chart.count() - 1; i >= 0; i-- {
		if v, ok := line.valueOf(i); ok {
			return line.name + " " + chart.value(v), i, true
		}
	}
	return "", 0, false
}

// lastOf returns the index of the last value of the line, or -1 when it has
// none.
func (chart *SimpleLineChart) lastOf(line *simpleLine) int {
	for i := chart.count() - 1; i >= 0; i-- {
		if _, ok := line.valueOf(i); ok {
			return i
		}
	}
	return -1
}

// getAltDescription returns the alternate description: the one set, or the
// title, the categories and, for each line, its first and last values and
// its highest.
func (chart *SimpleLineChart) getAltDescription() string {
	if chart.altDescription != "" {
		return chart.altDescription
	}
	var sb strings.Builder
	sb.WriteString(chart.title)
	if chart.subtitle != "" {
		if sb.Len() > 0 {
			sb.WriteString(", ")
		}
		sb.WriteString(chart.subtitle)
	}
	if sb.Len() == 0 {
		sb.WriteString("Line chart")
	}
	n := chart.count()
	if n > 0 && chart.category(0) != "" {
		sb.WriteString(", " + chart.category(0) + " to " + chart.category(n-1))
	}
	sb.WriteString(":")
	for i, line := range chart.lines {
		first, last, high := -1, -1, -1
		for j := 0; j < n; j++ {
			v, ok := line.valueOf(j)
			if !ok {
				continue
			}
			if first < 0 {
				first = j
			}
			last = j
			if high < 0 || v > line.values[high] {
				high = j
			}
		}
		if i > 0 {
			sb.WriteString(";")
		}
		sb.WriteString(" " + line.name)
		if first < 0 {
			sb.WriteString(" has no values")
			continue
		}
		sb.WriteString(" from " + chart.value(line.values[first]) + " to " + chart.value(line.values[last]))
		if high != first && high != last {
			sb.WriteString(", highest " + chart.value(line.values[high]))
			if chart.category(high) != "" {
				sb.WriteString(" in " + chart.category(high))
			}
		}
	}
	sb.WriteString(".")
	return sb.String()
}

// DrawOn draws the chart on the page and returns the x and y coordinates of
// its bottom right corner. A chart too small for its plot, its numbers and
// its labels draws nothing.
func (chart *SimpleLineChart) DrawOn(page *Page) [2]float32 {
	n := chart.count()
	values := make([]float32, 0)
	for _, line := range chart.lines {
		for j := 0; j < n; j++ {
			if v, ok := line.valueOf(j); ok {
				values = append(values, v)
			}
		}
	}
	if page == nil || len(values) == 0 { // Measured, or nothing to draw
		return [2]float32{chart.x + chart.w, chart.y + chart.h}
	}
	f1, f2 := chart.f1, chart.f2
	size := f2.size
	pad := chart.pad()
	ascent := f2.GetAscent(size)

	axis := axisOf(values, chart.yMin, chart.yMax, chart.yRangeSet)
	step := (axis.max - axis.min) / float32(axis.lines)

	// The plot: right of the y title and the widest number, left of the
	// widest label at the end of a line, above the categories and the x title
	widest := float32(0)
	for i := 0; i <= axis.lines; i++ {
		widest = max(widest, f2.StringWidth(size, chart.label(axis.min+float32(step*float32(i)), step)))
	}
	endWidth := float32(0)
	for i, line := range chart.lines {
		if label, _, ok := chart.endOf(line); ok {
			endWidth = max(endWidth, chart.fontOf(i).StringWidth(size, label))
		}
	}
	left := chart.x + pad + widest + float32(size/2.0)
	if chart.yAxisTitle != "" {
		left += float32(1.8 * size) // The title, as far from the numbers as the x title is
	}
	right := chart.x + chart.w - pad - endWidth - float32(size/2.0)
	top := chart.y + chart.top() + float32(ascent/2.0) // Room for the top number
	bottom := chart.y + chart.h - pad - float32(1.4*size)
	if chart.xAxisTitle != "" {
		bottom -= float32(1.4 * size)
	}
	if right <= left || bottom <= top || !drawable(axis) {
		return [2]float32{chart.x + chart.w, chart.y + chart.h}
	}
	xAt := func(i int) float32 {
		if n == 1 {
			return float32((left + right) / 2.0)
		}
		return left + float32(float32(i)*(right-left))/float32(n-1)
	}
	yAt := func(v float32) float32 { // held near the plot
		v = held(v, axis)
		return bottom - float32((v-axis.min)*(bottom-top))/(axis.max-axis.min)
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

	// The horizontal grid and its numbers, and the categories under the plot,
	// each where it leaves the one before it room
	page.SetPenColor(simpleScatterGrid)
	page.SetPenWidth(0.5)
	every := axisLabelStep((bottom-top)/float32(axis.lines), f2.bodyHeight, axis.lines)
	for i := 0; i <= axis.lines; i++ {
		v := axis.min + float32(step*float32(i))
		gy := yAt(v)
		page.DrawLine(left, gy, right, gy)
		if i%every == 0 {
			label := chart.label(v, step)
			page.drawString(f2, size, label, left-float32(size/2.0)-f2.StringWidth(size, label),
				gy+float32(ascent/2.0), colorToRGB(simpleBarMuted), nil)
		}
	}
	widestCategory := float32(0)
	for i := 0; i < n; i++ {
		widestCategory = max(widestCategory, f2.StringWidth(size, chart.category(i)))
	}
	every = 1
	if n > 1 {
		every = axisLabelStep((right-left)/float32(n-1), widestCategory+pad, n-1)
	}
	for i := 0; i < n; i += every {
		label := chart.category(i)
		page.drawString(f2, size, label, xAt(i)-float32(f2.StringWidth(size, label)/2.0),
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

	// The lines, the highlighted one last, over the others, broken where a
	// value is missing
	order := make([]int, 0, len(chart.lines))
	for i := range chart.lines {
		if i != chart.highlight {
			order = append(order, i)
		}
	}
	if chart.highlight >= 0 && chart.highlight < len(chart.lines) {
		order = append(order, chart.highlight)
	}
	// A value outside a range that is set is drawn past the plot, and the
	// lines are clipped at its edges, and as far past them as a dot is wide
	outside := false
	for _, v := range values {
		outside = outside || !onAxis(v, axis)
	}
	if outside {
		edge := float32(0.25*size) + 1.0
		page.SaveGraphicsState()
		page.ClipRect(left-edge, top-edge, right-left+float32(2.0*edge), bottom-top+float32(2.0*edge))
	}
	// The areas under the lines, each run of values down to 0, or to the
	// bottom of the plot when 0 is under it, in a tint of the line's color
	if chart.areaFilled {
		zero := yAt(min(max(0, axis.min), axis.max))
		for _, i := range order {
			line := chart.lines[i]
			if i == chart.highlight {
				page.SetBrushColor(simpleLineTint(chart.highlightColor))
			} else {
				page.SetBrushColor(simpleLineTint(chart.lineColor))
			}
			start := -1
			for j := 0; j <= n; j++ {
				if j < n {
					if _, ok := line.valueOf(j); ok {
						if start < 0 {
							start = j
						}
						continue
					}
				}
				if start >= 0 && j-1 > start {
					page.MoveTo(xAt(start), zero)
					for k := start; k < j; k++ {
						page.LineTo(xAt(k), yAt(line.values[k]))
					}
					page.LineTo(xAt(j-1), zero)
					page.FillPath()
				}
				start = -1
			}
		}
	}
	for _, i := range order {
		line := chart.lines[i]
		c, width := chart.lineColor, float32(1.5)
		if i == chart.highlight {
			c, width = chart.highlightColor, 2.0
		}
		page.SetPenColor(c)
		page.SetPenWidth(width)
		page.SetLineJoinStyle(joinstyle.Round) // No spike at a sharp peak
		drawing := false
		for j := 0; j < n; j++ {
			v, ok := line.valueOf(j)
			if !ok {
				if drawing {
					page.StrokePath()
				}
				drawing = false
				continue
			}
			if drawing {
				page.LineTo(xAt(j), yAt(v))
			} else {
				page.MoveTo(xAt(j), yAt(v))
				drawing = true
			}
		}
		if drawing {
			page.StrokePath()
		}
		if chart.markers {
			page.SetBrushColor(c)
			for j := 0; j < n; j++ {
				if v, ok := line.valueOf(j); ok {
					page.FillCircle(xAt(j), yAt(v), float32(0.25*size))
				}
			}
		}
	}
	if outside {
		page.RestoreGraphicsState()
	}

	// The values by their points, where the line leaves them room: above
	// a peak, under a valley, above and before a point on a rise, above
	// and after one on a fall; but those outside the axis and the last of
	// each line, which its label at the end has
	if chart.valueLabels {
		small := chart.subtitleSize()
		gap := float32(0.5 * size)
		for _, i := range order {
			line := chart.lines[i]
			last := chart.lastOf(line)
			c := simpleBarMuted
			if i == chart.highlight {
				c = chart.highlightColor
			}
			for j := 0; j < last; j++ {
				v, ok := line.valueOf(j)
				if !ok || !onAxis(v, axis) {
					continue
				}
				before := v
				if j > 0 {
					if b, ok := line.valueOf(j - 1); ok {
						before = b
					}
				}
				after := v
				if a, ok := line.valueOf(j + 1); ok {
					after = a
				}
				text := chart.value(v)
				width := f2.StringWidth(small, text)
				px := xAt(j)
				py := yAt(v)
				lx := px - float32(width/2.0)
				at := py - gap
				if before > v && after > v { // A valley
					at = py + gap + f2.GetAscent(small)
					if at > bottom {
						at = py - gap
					}
				} else if before < v && after > v { // A rise
					lx = px - gap - width
				} else if before > v && after < v { // A fall
					lx = px + gap
				}
				page.drawString(f2, small, text, lx, at, colorToRGB(c), nil)
			}
		}
	}

	// The labels at the ends of the lines, moved apart where they would meet
	type endLabel struct {
		text  string
		index int
		y     float32
	}
	labels := make([]endLabel, 0, len(chart.lines))
	for i, line := range chart.lines {
		if text, j, ok := chart.endOf(line); ok {
			v, _ := line.valueOf(j)
			// At the end of its line, or at the edge of the plot it is past
			end := max(top, min(bottom, yAt(v)))
			labels = append(labels, endLabel{text, i, end + float32(ascent/2.0)})
		}
	}
	sort.SliceStable(labels, func(a, b int) bool { return labels[a].y < labels[b].y })
	for k := 1; k < len(labels); k++ {
		labels[k].y = max(labels[k].y, labels[k-1].y+f2.bodyHeight)
	}
	// and up from the bottom of the plot, when the last is pushed past it
	if len(labels) > 0 {
		last := len(labels) - 1
		labels[last].y = min(labels[last].y, bottom+float32(ascent/2.0))
		for k := last - 1; k >= 0; k-- {
			labels[k].y = min(labels[k].y, labels[k+1].y-f2.bodyHeight)
		}
	}
	for _, label := range labels {
		c := simpleBarMuted
		if label.index == chart.highlight {
			c = chart.highlightColor
		}
		page.drawString(chart.fontOf(label.index), size, label.text, right+float32(size/2.0), label.y,
			colorToRGB(c), nil)
	}

	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(chart.x, chart.y, chart.w, chart.h)
	page.AddEMC()
	return [2]float32{chart.x + chart.w, chart.y + chart.h}
}

// simpleLineTint returns a tint of the color, a quarter of it over three
// quarters of white.
func simpleLineTint(c int32) int32 {
	r := (c >> 16) & 0xFF
	g := (c >> 8) & 0xFF
	b := c & 0xFF
	return (((r + 3*255) / 4) << 16) | (((g + 3*255) / 4) << 8) | ((b + 3*255) / 4)
}
