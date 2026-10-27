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

	"github.com/edragoev1/pdfjet/v9/src/color"
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
	uri  string // The link of a highlighted point, or ""
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
// dots are drawn opaque. Please see Example_26.
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
	maxFractionDigits      int // As many as the steps of the axes need
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

// AddLinked adds a point that is highlighted and is a link: a navy dot with
// its name beside it, both of them the link to the URI. In a PDF/UA document
// each such point is a figure of its own, described by its name, in the Link
// that holds its annotation, as PDF/UA asks.
func (chart *SimpleScatterChart) AddLinked(name string, x, y float32, uri string) *SimpleScatterChart {
	chart.points = append(chart.points, scatterPoint{x: x, y: y, name: name, uri: uri})
	return chart
}

// SetTrendLine draws the trend line of the points, their least squares line,
// thin and dark gray, labeled "trend" at its right end.
func (chart *SimpleScatterChart) SetTrendLine(trendLine bool) *SimpleScatterChart {
	chart.trendLine = trendLine
	return chart
}

// SetXAxisRange sets the range of the horizontal axis. Without it the range
// is that of the points, rounded to steps. A range whose end is not past its
// start is not set.
func (chart *SimpleScatterChart) SetXAxisRange(lo, hi float32) *SimpleScatterChart {
	chart.xMin, chart.xMax, chart.xRangeSet = lo, hi, hi > lo
	return chart
}

// SetYAxisRange sets the range of the vertical axis. Without it the range is
// that of the points, rounded to steps. A range whose end is not past its
// start is not set.
func (chart *SimpleScatterChart) SetYAxisRange(lo, hi float32) *SimpleScatterChart {
	chart.yMin, chart.yMax, chart.yRangeSet = lo, hi, hi > lo
	return chart
}

// SetPanelColor sets the color of the panel behind the chart. It is light
// gray, 0xF1F3F6, unless set. Color.transparent, -1, draws no panel: the
// one color setter where it does not leave the color as it was.
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

// label formats a number of an axis with the decimals its step needs.
func (chart *SimpleScatterChart) label(value, step float32) string {
	return groupDigits(format(value, fractionDigitsOf(step, chart.maxFractionDigits), chart.maxFractionDigits))
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
	// From the sums of the distances from the means, which keep their
	// precision where the raw sums lost it, for x near 1e8 of a small spread
	var sx, sy float64
	for _, p := range points {
		sx += float64(p.x)
		sy += float64(p.y)
	}
	mx := sx / n
	my := sy / n
	var sxx, sxy float64
	for _, p := range points {
		dx := float64(p.x) - mx
		sxx += float64(dx * dx)
		sxy += float64(dx * (float64(p.y) - my))
	}
	if sxx == 0 {
		return 0, 0, false
	}
	slope := sxy / sxx
	return slope, my - float64(slope*mx), true
}

// axes returns the horizontal and the vertical axis of the points.
func (chart *SimpleScatterChart) axes(points []scatterPoint) (chartAxis, chartAxis) {
	xs := make([]float32, len(points))
	ys := make([]float32, len(points))
	for i, p := range points {
		xs[i], ys[i] = p.x, p.y
	}
	return axisOf(xs, chart.xMin, chart.xMax, chart.xRangeSet), axisOf(ys, chart.yMin, chart.yMax, chart.yRangeSet)
}

// shownTrend returns the trend of the points as it is drawn: none where it
// misses the plot, both of its ends past one edge, as a flat one over a range
// that is set, which was drawn outside the chart.
func shownTrend(points []scatterPoint, xAxis, yAxis chartAxis) (float64, float64, bool) {
	slope, intercept, ok := trend(points)
	if ok {
		a := float64(slope*float64(xAxis.min)) + intercept
		b := float64(slope*float64(xAxis.max)) + intercept
		if (a > float64(yAxis.max) && b > float64(yAxis.max)) || (a < float64(yAxis.min) && b < float64(yAxis.min)) {
			return slope, intercept, false
		}
	}
	return slope, intercept, ok
}

// onPlot tells if the point is on the plot of the axes: a point outside a
// range that is set is not drawn.
func onPlot(p scatterPoint, xAxis, yAxis chartAxis) bool {
	return onAxis(p.x, xAxis) && onAxis(p.y, yAxis)
}

// getAltDescription returns the alternate description: the one set, or the
// title, the number of points, the highlighted ones and the trend.
func (chart *SimpleScatterChart) getAltDescription() string {
	if chart.altDescription != "" {
		return chart.altDescription
	}
	points := chart.valid()
	var xAxis, yAxis chartAxis
	if len(points) > 0 {
		xAxis, yAxis = chart.axes(points)
		chart.maxFractionDigits = max(digitsOf((xAxis.max-xAxis.min)/float32(xAxis.lines)),
			digitsOf((yAxis.max-yAxis.min)/float32(yAxis.lines)))
	}
	// The points that are drawn: not those outside a range that is set
	var shown []scatterPoint
	for _, p := range points {
		if onPlot(p, xAxis, yAxis) {
			shown = append(shown, p)
		}
	}
	var sb strings.Builder
	sb.WriteString(join(chart.title, chart.subtitle))
	if sb.Len() == 0 {
		sb.WriteString("Scatter chart")
	}
	sb.WriteString(": " + strconv.Itoa(len(shown)) + " points")
	if chart.xAxisTitle != "" && chart.yAxisTitle != "" {
		sb.WriteString(", " + chart.yAxisTitle + " against " + chart.xAxisTitle)
	}
	named := 0
	for _, p := range shown {
		if p.name == "" {
			continue
		}
		if named == 0 {
			sb.WriteString("; ")
		} else {
			sb.WriteString(", ")
		}
		sb.WriteString(p.name + " " + groupDigits(format(p.x, 0, chart.maxFractionDigits)) + " and " +
			groupDigits(format(p.y, 0, chart.maxFractionDigits)))
		named++
	}
	if chart.trendLine && len(points) > 0 {
		if slope, _, ok := shownTrend(points, xAxis, yAxis); ok { // Not one that is not drawn
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
// its bottom right corner. A chart too small for its plot and its numbers
// draws nothing.
func (chart *SimpleScatterChart) DrawOn(page *Page) [2]float32 {
	points := chart.valid()
	if page == nil || len(points) == 0 || chart.f1 == nil || chart.f2 == nil { // Measured, or nothing to draw
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
	if (!chart.xRangeSet && !spanFits(xs)) || (!chart.yRangeSet && !spanFits(ys)) {
		return [2]float32{chart.x + chart.w, chart.y + chart.h} // A span past the largest float, which no axis can draw
	}
	xAxis, yAxis := chart.axes(points)
	xStep := (xAxis.max - xAxis.min) / float32(xAxis.lines)
	yStep := (yAxis.max - yAxis.min) / float32(yAxis.lines)
	chart.maxFractionDigits = max(digitsOf(xStep), digitsOf(yStep))

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
	// Room for the halves of the first and the last number under the plot,
	// centered on its edges: the last, "1,000,000", went past the chart
	firstWidth := f2.StringWidth(size, chart.label(xAxis.min, xStep))
	lastWidth := f2.StringWidth(size, chart.label(xAxis.min+float32(xStep*float32(xAxis.lines)), xStep))
	left = max(left, chart.x+float32(pad/2.0)+float32(firstWidth/2.0))
	right := min(chart.x+chart.w-pad, chart.x+chart.w-float32(pad/2.0)-float32(lastWidth/2.0))
	top := chart.y + chart.top() + float32(ascent/2.0) // Room for the top number
	bottom := chart.y + chart.h - pad - float32(1.4*size)
	if chart.xAxisTitle != "" {
		bottom -= float32(1.4 * size)
	}
	if right <= left || bottom <= top || !drawable(xAxis) || !drawable(yAxis) {
		return [2]float32{chart.x + chart.w, chart.y + chart.h}
	}
	xAt := func(v float32) float32 {
		return left + scaled(v-xAxis.min, right-left, xAxis.max-xAxis.min)
	}
	yAt := func(v float32) float32 {
		return bottom - scaled(v-yAxis.min, bottom-top, yAxis.max-yAxis.min)
	}

	// In a tagged document a point that is a link is drawn after the chart,
	// as a figure of its own in its Link, which holds its annotation, as
	// PDF/UA asks: a link inside the chart's figure has nothing to hold
	linksApart := page.pdf.isTagged() && page.artifactDepth == 0

	page.AddBDC(structelem.Figure, "", "", chart.getAltDescription())
	page.SaveGraphicsState()
	page.SetDefaultStrokeDashPattern() // Not the caller's, which dashed the lines

	if chart.panelColor >= 0 {
		page.SetBrushColor(chart.panelColor)
		page.FillRect(chart.x, chart.y, chart.w, chart.h)
	}
	baseline := chart.y + pad
	if chart.title != "" {
		baseline += f1.GetAscent(f1.size)
		page.drawString(f1, f1.size, shorten(f1, f1.size, chart.title, chart.w-float32(2.0*pad)), chart.x+pad, baseline,
			colorToRGB(simpleBarInk), nil)
		baseline += f1.GetDescent(f1.size)
	}
	if chart.subtitle != "" {
		baseline += float32(1.4 * chart.subtitleSize())
		page.drawString(f2, chart.subtitleSize(), shorten(f2, chart.subtitleSize(), chart.subtitle, chart.w-float32(2.0*pad)),
			chart.x+pad, baseline, colorToRGB(simpleBarMuted), nil)
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
	// Each centered on its line, and inside the chart
	centers := make([]float32, xAxis.lines+1)
	widths := make([]float32, xAxis.lines+1)
	for i := 0; i <= xAxis.lines; i++ {
		v := xAxis.min + float32(xStep*float32(i))
		centers[i] = xAt(v)
		widths[i] = f2.StringWidth(size, chart.label(v, xStep))
	}
	lo := chart.x + float32(pad/2.0)
	hi := chart.x + chart.w - float32(pad/2.0)
	every = placedStep(every, centers, widths, lo, hi, float32(pad/2.0))
	for i := 0; i <= xAxis.lines; i += every {
		label := chart.label(xAxis.min+float32(xStep*float32(i)), xStep)
		page.drawString(f2, size, label, placed(centers[i], widths[i], lo, hi),
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
	slope, intercept, ok := shownTrend(points, xAxis, yAxis)
	if chart.trendLine && ok {
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
		// Not left of the plot, where a short steep line put it
		page.drawString(f2, chart.subtitleSize(), label, max(left, px2-width), py2-lift,
			colorToRGB(simpleScatterTrend), nil)
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
		if p.name == "" && onPlot(p, xAxis, yAxis) {
			page.FillCircle(xAt(p.x), yAt(p.y), r)
		}
	}
	page.RestoreGraphicsState()
	big := float32(0.42 * size)
	// named draws a highlighted point and its name, and returns the box of the
	// two: x, y, w, h
	named := func(p scatterPoint) [4]float32 {
		px, py := xAt(p.x), yAt(p.y)
		page.SetBrushColor(chart.highlightColor)
		page.FillCircle(px, py, big)
		// A name of two fifths of the plot at most, and in the plot
		name := shorten(f1, size, p.name, float32(0.4*(right-left)))
		width := f1.StringWidth(size, name)
		lx := px + big + float32(size/3.0)
		if lx+width > right {
			lx = px - big - float32(size/3.0) - width // At the left, not past the plot
		}
		lx = max(lx, left)
		baseline := py + float32(ascent/2.0)
		// On a patch of the panel's color, which the dots and the grid do not
		// show through
		if chart.panelColor >= 0 {
			page.SetBrushColor(chart.panelColor)
		} else {
			page.SetBrushColor(color.White)
		}
		page.FillRect(lx-1.0, baseline-f1.GetAscent(size)-1.0,
			width+2.0, f1.GetAscent(size)+f1.GetDescent(size)+2.0)
		page.drawString(f1, size, name, lx, baseline, colorToRGB(chart.highlightColor), nil)
		x1 := min(px-big, lx)
		y1 := min(py-big, baseline-f1.GetAscent(size))
		x2 := max(px+big, lx+width)
		y2 := max(py+big, baseline+f1.GetDescent(size))
		return [4]float32{x1, y1, x2 - x1, y2 - y1}
	}
	var linked []scatterPoint
	for _, p := range points {
		if p.name == "" || !onPlot(p, xAxis, yAxis) {
			continue
		}
		if p.uri != "" && linksApart {
			linked = append(linked, p)
			continue
		}
		box := named(p)
		if p.uri != "" {
			page.addAnnotation(&annotationObject{
				annotationType: annotationLink,
				x1:             box[0],
				y1:             box[1],
				x2:             box[0] + box[2],
				y2:             box[1] + box[3],
				uri:            p.uri,
				altDescription: p.name,
			})
		}
	}

	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(chart.x, chart.y, chart.w, chart.h)
	page.AddEMC()

	for _, p := range linked {
		page.SaveGraphicsState()
		link := page.beginLink()
		page.AddBDC(structelem.Figure, "", "", p.name)
		box := named(p)
		page.SetFigureBoundingBox(box[0], box[1], box[2], box[3])
		page.AddEMC()
		page.endLink(link)
		page.RestoreGraphicsState()
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             box[0],
			y1:             box[1],
			x2:             box[0] + box[2],
			y2:             box[1] + box[3],
			uri:            p.uri,
			altDescription: p.name,
			linkElement:    link,
		})
	}
	return [2]float32{chart.x + chart.w, chart.y + chart.h}
}
