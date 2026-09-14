// chart.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// Chart is used to create XY chart objects and draw them on a page.
// Please see Example_09.
type Chart struct {
	f1, f2                         *Font
	series                         []*Series
	drawLegend                     bool
	w                              float32
	h                              float32
	x1, x2, x3, x4, x5, x6, x7, x8 float32
	y1, y2, y3, y4, y5, y6, y7, y8 float32
	xMax                           float32 // = Float.MIN_VALUE
	xMin                           float32 // = Float.MAX_VALUE
	yMax                           float32 // = Float.MIN_VALUE
	yMin                           float32 // = Float.MAX_VALUE
	xAxisGridLines                 int
	yAxisGridLines                 int
	title                          string
	xAxisTitle                     string
	yAxisTitle                     string
	drawHGridLines                 bool
	drawVGridLines                 bool
	drawXAxisLabels                bool
	drawYAxisLabels                bool
	hGridLineWidth                 float32
	vGridLineWidth                 float32
	hGridLinePattern               string
	vGridLinePattern               string
	chartBorderWidth               float32
	innerBorderWidth               float32
	minFractionDigits              int
	maxFractionDigits              int
}

// defaultPalette holds the stroke colors of the series that have no color.
var defaultPalette = [...]int32{
	color.Blue,
	color.Red,
	color.Green,
	color.Orange,
	color.Purple,
	color.DarkCyan,
	color.Magenta,
	color.Olive,
}

// NewChart creates XY chart objects.
// @param f1 the font used for the chart title.
// @param f2 the font used for the X and Y axis titles.
func NewChart(f1, f2 *Font) *Chart {
	chart := new(Chart)
	chart.f1 = f1
	chart.f2 = f2
	chart.w = 300.0
	chart.h = 200.0

	chart.xMax = -math.MaxFloat32
	chart.xMin = math.MaxFloat32
	chart.yMax = -math.MaxFloat32
	chart.yMin = math.MaxFloat32

	chart.series = make([]*Series, 0)
	chart.drawLegend = true
	chart.drawHGridLines = true
	chart.drawVGridLines = true
	chart.drawXAxisLabels = true
	chart.drawYAxisLabels = true
	chart.hGridLinePattern = "[1 1] 0"
	chart.vGridLinePattern = "[1 1] 0"
	chart.chartBorderWidth = 0.0
	chart.innerBorderWidth = 0.0
	chart.minFractionDigits = 0
	chart.maxFractionDigits = 2
	return chart
}

// SetTitle sets the title of the chart.
func (chart *Chart) SetTitle(title string) *Chart {
	chart.title = title
	return chart
}

// SetXAxisTitle sets the title for the X axis.
func (chart *Chart) SetXAxisTitle(title string) *Chart {
	chart.xAxisTitle = title
	return chart
}

// SetYAxisTitle sets the title for the Y axis.
func (chart *Chart) SetYAxisTitle(title string) *Chart {
	chart.yAxisTitle = title
	return chart
}

// AddSeries adds a series and returns it, to add its points and set its line
// and marker. A series without a stroke color has the next color of the
// palette. The legend lists the series that have a name.
// @param name the series name, shown in the legend; empty for none.
func (chart *Chart) AddSeries(name string) *Series {
	series := newSeries(name)
	chart.series = append(chart.series, series)
	return series
}

// SetDrawLegend sets whether the legend is drawn. The legend lists the series
// that have a name, under the title, each with its line or its marker.
func (chart *Chart) SetDrawLegend(drawLegend bool) *Chart {
	chart.drawLegend = drawLegend
	return chart
}

// SetLocation sets the location of chart on the page.
func (chart *Chart) SetLocation(x, y float32) Drawable {
	chart.x1 = x
	chart.y1 = y
	return chart
}

// SetSize sets the size of chart.
func (chart *Chart) SetSize(w, h float32) *Chart {
	chart.w = w
	chart.h = h
	return chart
}

// SetMinimumFractionDigits sets the minimum number of decimal places in the
// axis labels. The labels of an axis have at least the decimal places of its
// step, so an axis with a whole number step has whole number labels. The
// default is 0.
func (chart *Chart) SetMinimumFractionDigits(minFractionDigits int) *Chart {
	chart.minFractionDigits = minFractionDigits
	return chart
}

// SetMaximumFractionDigits sets the maximum number of decimal places in the
// axis labels. The default is 2.
func (chart *Chart) SetMaximumFractionDigits(maxFractionDigits int) *Chart {
	chart.maxFractionDigits = maxFractionDigits
	return chart
}

// SetDrawHGridLines sets whether to draw horizontal grid lines on the chart.
func (chart *Chart) SetDrawHGridLines(drawHGridLines bool) *Chart {
	chart.drawHGridLines = drawHGridLines
	return chart
}

// SetDrawVGridLines sets whether to draw vertical grid lines on the chart.
func (chart *Chart) SetDrawVGridLines(drawVGridLines bool) *Chart {
	chart.drawVGridLines = drawVGridLines
	return chart
}

// SetChartBorderWidth sets the width of the outer chart border. A width of 0,
// the default, draws the thinnest line a viewer shows.
func (chart *Chart) SetChartBorderWidth(width float32) *Chart {
	chart.chartBorderWidth = width
	return chart
}

// SetInnerBorderWidth sets the width of the plot area border. A width of 0,
// the default, draws the thinnest line a viewer shows.
func (chart *Chart) SetInnerBorderWidth(width float32) *Chart {
	chart.innerBorderWidth = width
	return chart
}

// SetHGridLineWidth sets the width of the horizontal grid lines.
func (chart *Chart) SetHGridLineWidth(width float32) *Chart {
	chart.hGridLineWidth = width
	return chart
}

// SetVGridLineWidth sets the width of the vertical grid lines.
func (chart *Chart) SetVGridLineWidth(width float32) *Chart {
	chart.vGridLineWidth = width
	return chart
}

// SetHGridLineDashPattern sets the horizontal grid line dash pattern, e.g. "[1 1] 0".
func (chart *Chart) SetHGridLineDashPattern(pattern string) *Chart {
	chart.hGridLinePattern = pattern
	return chart
}

// SetVGridLineDashPattern sets the vertical grid line dash pattern, e.g. "[1 1] 0".
func (chart *Chart) SetVGridLineDashPattern(pattern string) *Chart {
	chart.vGridLinePattern = pattern
	return chart
}

// toFloatArray converts an RGB color to the float array used internally.
func (chart *Chart) toFloatArray(color int32) [3]float32 {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32(color&0xff) / 255.0
	return [3]float32{r, g, b}
}

// SetDrawXAxisLabels sets whether to draw X axis labels on the chart.
func (chart *Chart) SetDrawXAxisLabels(drawXAxisLabels bool) *Chart {
	chart.drawXAxisLabels = drawXAxisLabels
	return chart
}

// SetDrawYAxisLabels sets whether to draw Y axis labels on the chart.
func (chart *Chart) SetDrawYAxisLabels(drawYAxisLabels bool) *Chart {
	chart.drawYAxisLabels = drawYAxisLabels
	return chart
}

// DrawOn draws chart on the specified page.
// @param page the page to draw chart on.
func (chart *Chart) DrawOn(page *Page) [2]float32 {
	// Guard against null or empty data
	if !chart.hasPoints() {
		return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
	}

	chart.x2 = chart.x1 + chart.w
	chart.y2 = chart.y1

	chart.x3 = chart.x2
	chart.y3 = chart.y1 + chart.h

	chart.x4 = chart.x1
	chart.y4 = chart.y3

	chart.setXAxisMinAndMaxChartValues()
	chart.setYAxisMinAndMaxChartValues()

	// Guard against flat data (all same X or Y) before rounding,
	// so the rounded ranges have grid lines
	if chart.xMax == chart.xMin {
		chart.xMax = chart.xMin + 1.0
	}
	if chart.yMax == chart.yMin {
		chart.yMax = chart.yMin + 1.0
	}

	chart.roundXAxisMinAndMaxValues()
	chart.roundYAxisMinAndMaxValues()

	// Draw chart title, then the legend under it
	page.SetBrushColor(color.Black)
	page.drawString(
		chart.f1,
		chart.f1.GetSize(),
		chart.title,
		chart.x1+((chart.w-chart.f1.StringWidth(chart.f1.GetSize(), chart.title))/2),
		chart.y1+1.5*chart.f1.bodyHeight,
		[3]float32{0.0, 0.0, 0.0},
		nil)
	legend := chart.drawLegend && chart.hasSeriesNames()
	topMargin := 2.5 * chart.f1.bodyHeight
	if legend {
		chart.drawLegendOn(page, chart.y1+1.5*chart.f1.bodyHeight+1.5*chart.f2.bodyHeight)
		topMargin += 1.5 * chart.f2.bodyHeight
	}
	leftMargin := chart.getLongestAxisYLabelWidth() + 2.0*chart.f2.bodyHeight
	rightMargin := 2.0 * chart.f2.bodyHeight
	bottomMargin := 2.5 * chart.f2.bodyHeight

	chart.x5 = chart.x1 + leftMargin
	chart.y5 = chart.y1 + topMargin

	chart.x6 = chart.x2 - rightMargin
	chart.y6 = chart.y5

	chart.x7 = chart.x6
	chart.y7 = chart.y3 - bottomMargin

	chart.x8 = chart.x5
	chart.y8 = chart.y7

	chart.drawChartBorder(page)
	chart.drawInnerBorder(page)

	if chart.drawHGridLines {
		chart.drawHorizontalGridLines(page)
	}
	if chart.drawVGridLines {
		chart.drawVerticalGridLines(page)
	}

	if chart.drawXAxisLabels {
		chart.drawXAxisLabelsOn(page)
	}
	if chart.drawYAxisLabels {
		chart.drawYAxisLabelsOn(page)
	}

	// Defensive copy so the user's data is never mutated
	plotData := make([][]*Point, len(chart.series))
	for i, s := range chart.series {
		pointCopy := make([]*Point, len(s.points))
		for j, p := range s.points {
			pointCopy[j] = copyPoint(p)
		}
		plotData[i] = pointCopy
	}

	// Translate the point coordinates (on the copies)
	for _, points := range plotData {
		for _, point := range points {
			point.x = chart.x5 + (point.x-chart.xMin)*(chart.x6-chart.x5)/(chart.xMax-chart.xMin)
			point.y = chart.y8 - (point.y-chart.yMin)*(chart.y8-chart.y5)/(chart.yMax-chart.yMin)
			if point.uri != "" {
				// AddAnnotation flips y into PDF space; do not pre-flip here.
				page.addAnnotation(&annotationObject{
					annotationType: annotationLink,
					x1:             point.x - point.r,
					y1:             point.y - point.r,
					x2:             point.x + point.r,
					y2:             point.y + point.r,
					vertices:       nil,
					fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
					transparency:   0.0,
					title:          "",
					contents:       "",
					uri:            point.uri,
					key:            "",
					language:       "",
					actualText:     "",
					altDescription: "",
				})
			}
		}
	}

	chart.drawPathsAndPoints(page, plotData)

	// Draw the Y axis title
	page.SetBrushColor(color.Black)
	page.SetTextRotation(90)
	page.drawString(
		chart.f2,
		chart.f2.GetSize(),
		chart.yAxisTitle,
		chart.x1+chart.f2.bodyHeight,
		chart.y8-((chart.y8-chart.y5)-chart.f2.StringWidth(chart.f2.GetSize(), chart.yAxisTitle))/2,
		[3]float32{0.0, 0.0, 0.0},
		nil)

	// Draw the X axis title
	page.SetTextRotation(0)
	page.SetBrushColor(color.Black)
	page.drawString(
		chart.f2,
		chart.f2.GetSize(),
		chart.xAxisTitle,
		chart.x5+((chart.x6-chart.x5)-chart.f2.StringWidth(chart.f2.GetSize(), chart.xAxisTitle))/2,
		chart.y4-chart.f2.bodyHeight/2,
		[3]float32{0.0, 0.0, 0.0},
		nil)

	page.SetDefaultPenWidth()
	page.SetDefaultStrokeDashPattern()
	page.SetPenColor(color.Black)

	return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
}

// hasPoints returns true if at least one series has points.
func (chart *Chart) hasPoints() bool {
	for _, s := range chart.series {
		if len(s.points) > 0 {
			return true
		}
	}
	return false
}

// hasSeriesNames returns true if a series has a name to list in the legend.
func (chart *Chart) hasSeriesNames() bool {
	for _, s := range chart.series {
		if s.name != "" {
			return true
		}
	}
	return false
}

// seriesColor returns the color of the series at the index: its own or the palette's.
func (chart *Chart) seriesColor(s *Series, index int) [3]float32 {
	if s.hasStrokeColor {
		return s.strokeColor
	}
	return chart.toFloatArray(defaultPalette[index%len(defaultPalette)])
}

// drawLegendOn draws the legend centered on the chart: the line of each named
// series that draws its path, its marker when it has one, and its name.
func (chart *Chart) drawLegendOn(page *Page, baseline float32) {
	f2 := chart.f2
	ascent := f2.ascent
	sample := 2.0 * ascent // the width of the line or the marker
	gap := ascent / 2.0
	width := float32(0.0)
	entries := 0
	for _, s := range chart.series {
		if s.name != "" {
			width += sample + gap + f2.StringWidth(f2.size, s.name)
			entries++
		}
	}
	width += float32(entries-1) * f2.bodyHeight
	x := chart.x1 + (chart.w-width)/2.0
	for j, s := range chart.series {
		if s.name == "" {
			continue
		}
		rgb := chart.seriesColor(s, j)
		yMid := baseline - ascent/2.0
		page.SetPenColorRGB(rgb)
		if s.drawPath {
			page.SetPenWidth(s.strokeWidth)
			page.SetStrokeDashPattern(s.strokeDashPattern)
			page.DrawLine(x, yMid, x+sample, yMid)
		}
		if s.shape != shape.Invisible {
			page.SetPenWidth(1.0)
			page.SetDefaultStrokeDashPattern()
			page.DrawPoint(NewPoint(x+sample/2.0, yMid).SetShape(s.shape).SetRadius(s.radius))
		}
		x += sample + gap
		page.SetBrushColor(color.Black)
		page.drawString(f2, f2.size, s.name, x, baseline, [3]float32{0.0, 0.0, 0.0}, nil)
		x += f2.StringWidth(f2.size, s.name) + f2.bodyHeight
	}
	page.SetDefaultPenWidth()
	page.SetDefaultStrokeDashPattern()
}

// format formats a label with minDigits to maxDigits decimal places, rounding
// the exact value half to even. The label has a "." decimal separator and no
// grouping, and a value that rounds to zero has no minus sign.
func format(value float32, minDigits, maxDigits int) string {
	if math.IsNaN(float64(value)) {
		return "NaN"
	}
	if math.IsInf(float64(value), 0) {
		if value < 0 {
			return "-Infinity"
		}
		return "Infinity"
	}
	// A minimum above the maximum is lowered to it, as in Java's NumberFormat
	maxDigits = max(maxDigits, 0)
	minDigits = min(max(minDigits, 0), maxDigits)
	label := strconv.FormatFloat(float64(value), 'f', maxDigits, 64)
	if point := strings.IndexByte(label, '.'); point != -1 {
		end := len(label)
		for end-point-1 > minDigits && label[end-1] == '0' {
			end--
		}
		if end-point-1 == 0 {
			end = point
		}
		label = label[:end]
	}
	if strings.Trim(label, "-0.") == "" {
		label = strings.TrimPrefix(label, "-")
	}
	return label
}

// fractionDigitsOf returns the number of decimal places, at most maxDigits,
// that write the axis step exactly: 0 for 10, 1 for 2.5, 2 for 0.25.
func fractionDigitsOf(step float32, maxDigits int) int {
	for digits := 0; digits < maxDigits; digits++ {
		scaled := float64(step) * math.Pow(10, float64(digits))
		if math.Abs(scaled-math.Round(scaled)) < 1e-4 {
			return digits
		}
	}
	return max(maxDigits, 0)
}

// formatAxis formats the label of an axis with the specified step.
func (chart *Chart) formatAxis(value, step float32) string {
	digits := max(chart.minFractionDigits, fractionDigitsOf(step, chart.maxFractionDigits))
	return format(value, digits, chart.maxFractionDigits)
}

func (chart *Chart) getLongestAxisYLabelWidth() float32 {
	step := (chart.yMax - chart.yMin) / float32(chart.yAxisGridLines)
	minLabelWidth := chart.f2.StringWidth(chart.f2.GetSize(), chart.formatAxis(chart.yMin, step)+"0")
	maxLabelWidth := chart.f2.StringWidth(chart.f2.GetSize(), chart.formatAxis(chart.yMax, step)+"0")
	if maxLabelWidth > minLabelWidth {
		return maxLabelWidth
	}
	return minLabelWidth
}

func (chart *Chart) setXAxisMinAndMaxChartValues() {
	if chart.xAxisGridLines != 0 {
		return
	}
	for _, s := range chart.series {
		for _, point := range s.points {
			if point.x < chart.xMin {
				chart.xMin = point.x
			}
			if point.x > chart.xMax {
				chart.xMax = point.x
			}
		}
	}
}

func (chart *Chart) setYAxisMinAndMaxChartValues() {
	if chart.yAxisGridLines != 0 {
		return
	}
	for _, s := range chart.series {
		for _, point := range s.points {
			if point.y < chart.yMin {
				chart.yMin = point.y
			}
			if point.y > chart.yMax {
				chart.yMax = point.y
			}
		}
	}
}

func (chart *Chart) roundXAxisMinAndMaxValues() {
	if chart.xAxisGridLines != 0 {
		return
	}
	round := roundMaxAndMinValues(chart.xMax, chart.xMin)
	chart.xMax = round.maxValue
	chart.xMin = round.minValue
	chart.xAxisGridLines = round.numOfGridLines
}

func (chart *Chart) roundYAxisMinAndMaxValues() {
	if chart.yAxisGridLines != 0 {
		return
	}
	round := roundMaxAndMinValues(chart.yMax, chart.yMin)
	chart.yMax = round.maxValue
	chart.yMin = round.minValue
	chart.yAxisGridLines = round.numOfGridLines
}

func (chart *Chart) drawChartBorder(page *Page) {
	page.SetPenWidth(chart.chartBorderWidth)
	page.SetPenColor(color.Black)
	page.MoveTo(chart.x1, chart.y1)
	page.LineTo(chart.x2, chart.y2)
	page.LineTo(chart.x3, chart.y3)
	page.LineTo(chart.x4, chart.y4)
	page.ClosePath()
}

func (chart *Chart) drawInnerBorder(page *Page) {
	page.SetPenWidth(chart.innerBorderWidth)
	page.SetPenColor(color.Black)
	page.MoveTo(chart.x5, chart.y5)
	page.LineTo(chart.x6, chart.y6)
	page.LineTo(chart.x7, chart.y7)
	page.LineTo(chart.x8, chart.y8)
	page.ClosePath()
}

func (chart *Chart) drawHorizontalGridLines(page *Page) {
	page.SetPenWidth(chart.hGridLineWidth)
	page.SetPenColor(color.Black)
	page.SetStrokeDashPattern(chart.hGridLinePattern)
	x := chart.x8
	y := chart.y8
	step := (chart.y8 - chart.y5) / float32(chart.yAxisGridLines)
	for i := 0; i < chart.yAxisGridLines; i++ {
		page.DrawLine(x, y, chart.x6, y)
		y -= step
	}
}

func (chart *Chart) drawVerticalGridLines(page *Page) {
	page.SetPenWidth(chart.vGridLineWidth)
	page.SetPenColor(color.Black)
	page.SetStrokeDashPattern(chart.vGridLinePattern)
	x := chart.x5
	y := chart.y5
	step := (chart.x6 - chart.x5) / float32(chart.xAxisGridLines)
	for i := 0; i < chart.xAxisGridLines; i++ {
		page.DrawLine(x, y, x, chart.y8)
		x += step
	}
}

// drawXAxisLabelsOn draws the X axis labels.
func (chart *Chart) drawXAxisLabelsOn(page *Page) {
	x := chart.x5
	y := chart.y8 + chart.f2.GetBodyHeight()
	step := (chart.x6 - chart.x5) / float32(chart.xAxisGridLines)
	valueStep := (chart.xMax - chart.xMin) / float32(chart.xAxisGridLines)
	page.SetBrushColor(color.Black)
	for i := 0; i < (chart.xAxisGridLines + 1); i++ {
		label := chart.formatAxis(chart.xMin+valueStep*float32(i), valueStep)
		page.drawString(
			chart.f2, chart.f2.GetSize(), label, x-(chart.f2.StringWidth(chart.f2.GetSize(), label)/2), y, [3]float32{0.0, 0.0, 0.0}, nil)
		x += step
	}
}

// drawYAxisLabelsOn draws the Y axis labels.
func (chart *Chart) drawYAxisLabelsOn(page *Page) {
	x := chart.x5 - chart.getLongestAxisYLabelWidth()
	y := chart.y8 + chart.f2.ascent/3
	step := (chart.y8 - chart.y5) / float32(chart.yAxisGridLines)
	valueStep := (chart.yMax - chart.yMin) / float32(chart.yAxisGridLines)
	page.SetBrushColor(color.Black)
	for i := 0; i < (chart.yAxisGridLines + 1); i++ {
		label := chart.formatAxis(chart.yMin+valueStep*float32(i), valueStep)
		page.drawString(chart.f2, chart.f2.GetSize(), label, x, y, [3]float32{0.0, 0.0, 0.0}, nil)
		y -= step
	}
}

// drawPathsAndPoints draws the line of each series that draws its path, then
// the markers of its points: a point without a stroke color in the color of
// the series.
func (chart *Chart) drawPathsAndPoints(page *Page, plotData [][]*Point) {
	for j, s := range chart.series {
		points := plotData[j]
		if len(points) == 0 {
			continue
		}
		rgb := chart.seriesColor(s, j)
		if s.drawPath {
			page.SetPenColorRGB(rgb)
			page.SetPenWidth(s.strokeWidth)
			page.SetStrokeDashPattern(s.strokeDashPattern)
			page.DrawPath(points, pathoperator.Stroke)
		}
		for _, point := range points {
			if point.shape != shape.Invisible {
				if point.hasStrokeColor {
					page.SetPenColorRGB(point.strokeColor)
				} else {
					page.SetPenColorRGB(rgb)
				}
				page.SetPenWidth(point.strokeWidth)
				page.SetDefaultStrokeDashPattern()
				if point.hasFillColor {
					page.SetBrushColorRGB(point.fillColor)
				}
				page.DrawPoint(point)
			}
		}
	}
}

// roundMaxAndMinValues rounds the axis range to "nice" values for clean grid lines.
// Uses the span (max - min) to support negative values and zero crossings.
// Rounds max up and min down to step multiples, then recomputes grid lines
// to ensure they match the final rounded range.
func roundMaxAndMinValues(maxValue, minValue float32) *roundedRange {
	span := maxValue - minValue
	if span <= 0 {
		span = 1.0 // Guard against flat data
	}

	exponent := int(math.Floor(math.Log(float64(span)) / math.Log(10)))
	normalizedSpan := span * float32(math.Pow(10, float64(-exponent)))

	// Snap span up to a "nice" value with paired grid line count
	var niceSpan float32
	var numOfGridLines int

	if normalizedSpan > 9.00 {
		niceSpan = 10.0
		numOfGridLines = 10
	} else if normalizedSpan > 8.00 {
		niceSpan = 9.00
		numOfGridLines = 9
	} else if normalizedSpan > 7.00 {
		niceSpan = 8.00
		numOfGridLines = 8
	} else if normalizedSpan > 6.00 {
		niceSpan = 7.00
		numOfGridLines = 7
	} else if normalizedSpan > 5.00 {
		niceSpan = 6.00
		numOfGridLines = 6
	} else if normalizedSpan > 4.00 {
		niceSpan = 5.00
		numOfGridLines = 5
	} else if normalizedSpan > 3.50 {
		niceSpan = 4.00
		numOfGridLines = 8
	} else if normalizedSpan > 3.00 {
		niceSpan = 3.50
		numOfGridLines = 7
	} else if normalizedSpan > 2.50 {
		niceSpan = 3.00
		numOfGridLines = 6
	} else if normalizedSpan > 2.00 {
		niceSpan = 2.50
		numOfGridLines = 5
	} else if normalizedSpan > 1.75 {
		niceSpan = 2.00
		numOfGridLines = 8
	} else if normalizedSpan > 1.50 {
		niceSpan = 1.75
		numOfGridLines = 7
	} else if normalizedSpan > 1.25 {
		niceSpan = 1.50
		numOfGridLines = 6
	} else if normalizedSpan > 1.00 {
		niceSpan = 1.25
		numOfGridLines = 5
	} else {
		niceSpan = 1.00
		numOfGridLines = 10
	}

	// Scale back to original magnitude and compute step
	step := niceSpan * float32(math.Pow(10, float64(exponent))) / float32(numOfGridLines)

	round := newRoundedRange()

	// Round max up, min down to nearest step multiple
	round.maxValue = float32(math.Ceil(float64(maxValue/step))) * step
	round.minValue = float32(math.Floor(float64(minValue/step))) * step

	// Recount grid lines from actual rounded range
	round.numOfGridLines = int(math.Round(float64((round.maxValue - round.minValue) / step)))

	return round
}

// SetXAxisMinMax sets xMin and xMax for the X axis and the number of X grid lines.
func (chart *Chart) SetXAxisMinMax(xMin, xMax float32, xAxisGridLines int) *Chart {
	chart.xMin = xMin
	chart.xMax = xMax
	chart.xAxisGridLines = xAxisGridLines
	return chart
}

// SetYAxisMinMax sets yMin and yMax for the Y axis and the number of Y grid lines.
func (chart *Chart) SetYAxisMinMax(yMin, yMax float32, yAxisGridLines int) *Chart {
	chart.yMin = yMin
	chart.yMax = yMax
	chart.yAxisGridLines = yAxisGridLines
	return chart
}
