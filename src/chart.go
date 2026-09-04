// chart.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"

	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/pathoperator"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Chart is used to create XY chart objects and draw them on a page.
// Please see Example_09.
type Chart struct {
	f1, f2                         *Font
	chartData                      [][]*Point
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
	drawXAxisLines                 bool
	drawYAxisLines                 bool
	drawXAxisLabels                bool
	drawYAxisLabels                bool
	xyChart                        bool
	hGridLineWidth                 float32
	vGridLineWidth                 float32
	hGridLinePattern               string
	vGridLinePattern               string
	chartBorderWidth               float32
	innerBorderWidth               float32
	minFractionDigits              int
	maxFractionDigits              int
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

	chart.drawXAxisLines = true
	chart.drawYAxisLines = true
	chart.drawXAxisLabels = true
	chart.drawYAxisLabels = true
	chart.xyChart = true
	chart.hGridLinePattern = "[1 1] 0"
	chart.vGridLinePattern = "[1 1] 0"
	chart.chartBorderWidth = 0.0
	chart.innerBorderWidth = 0.0
	chart.minFractionDigits = 2
	chart.maxFractionDigits = 2
	return chart
}

// SetTitle sets the title of the chart.
func (chart *Chart) SetTitle(title string) {
	chart.title = title
}

// SetXAxisTitle sets the title for the X axis.
func (chart *Chart) SetXAxisTitle(title string) {
	chart.xAxisTitle = title
}

// SetYAxisTitle sets the title for the Y axis.
func (chart *Chart) SetYAxisTitle(title string) {
	chart.yAxisTitle = title
}

// SetData sets the data that will be used to draw chart.
func (chart *Chart) SetData(chartData [][]*Point) {
	chart.chartData = chartData
}

// GetData returns the chart data.
func (chart *Chart) GetData() [][]*Point {
	return chart.chartData
}

// SetLocation sets the location of chart on the page.
func (chart *Chart) SetLocation(x, y float32) {
	chart.x1 = x
	chart.y1 = y
}

// SetSize sets the size of chart.
func (chart *Chart) SetSize(w, h float32) {
	chart.w = w
	chart.h = h
}

// SetMinimumFractionDigits sets the minimum number of fractions digits do display for the X and Y axis labels.
func (chart *Chart) SetMinimumFractionDigits(minFractionDigits int) {
	chart.minFractionDigits = minFractionDigits
}

// SetMaximumFractionDigits sets the maximum number of fractions digits do display for the X and Y axis labels.
func (chart *Chart) SetMaximumFractionDigits(maxFractionDigits int) {
	chart.maxFractionDigits = maxFractionDigits
}

// Slope calculates the slope of a trend line given a list of points.
// See Example_09.
func (chart *Chart) Slope(points []*Point) float32 {
	return chart.covar(points) / chart.devsq(points) * float32(len(points)-1)
}

// Intercept calculates the intercept of a trend line given a list of points.
// See Example_09.
func (chart *Chart) Intercept(points []*Point, slope float32) float32 {
	_mean := chart.mean(points)
	return _mean[1] - slope*_mean[0]
}

// SetDrawXAxisLines sets whether to draw horizontal grid lines on the chart.
func (chart *Chart) SetDrawXAxisLines(drawXAxisLines bool) {
	chart.drawXAxisLines = drawXAxisLines
}

// SetDrawYAxisLines sets whether to draw vertical grid lines on the chart.
func (chart *Chart) SetDrawYAxisLines(drawYAxisLines bool) {
	chart.drawYAxisLines = drawYAxisLines
}

// SetDrawXAxisLabels sets whether to draw X axis labels on the chart.
func (chart *Chart) SetDrawXAxisLabels(drawXAxisLabels bool) {
	chart.drawXAxisLabels = drawXAxisLabels
}

// SetDrawYAxisLabels sets whether to draw Y axis labels on the chart.
func (chart *Chart) SetDrawYAxisLabels(drawYAxisLabels bool) {
	chart.drawYAxisLabels = drawYAxisLabels
}

// SetXYChart sets whether this is an XY chart (true) or a category chart (false).
func (chart *Chart) SetXYChart(xyChart bool) {
	chart.xyChart = xyChart
}

// DrawOn draws chart on the specified page.
// @param page the page to draw chart on.
func (chart *Chart) DrawOn(page *Page) {
	// Guard against null or empty data
	if chart.chartData == nil || len(chart.chartData) == 0 {
		return
	}

	chart.x2 = chart.x1 + chart.w
	chart.y2 = chart.y1

	chart.x3 = chart.x2
	chart.y3 = chart.y1 + chart.h

	chart.x4 = chart.x1
	chart.y4 = chart.y3

	chart.setXAxisMinAndMaxChartValues()
	chart.setYAxisMinAndMaxChartValues()
	chart.roundXAxisMinAndMaxValues()
	chart.roundYAxisMinAndMaxValues()

	// Guard against flat data (all same X or Y)
	if chart.xMax == chart.xMin {
		chart.xMax = chart.xMin + 1.0
	}
	if chart.yMax == chart.yMin {
		chart.yMax = chart.yMin + 1.0
	}

	// Draw chart title
	page.drawString(
		chart.f1,
		chart.f1.size,
		chart.title,
		chart.x1+((chart.w-chart.f1.StringWidth(chart.f1.size, chart.title))/2),
		chart.y1+1.5*chart.f1.bodyHeight,
		[3]float32{0.0, 0.0, 0.0},
		nil)

	topMargin := 2.5 * chart.f1.bodyHeight
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

	if chart.drawXAxisLines {
		chart.drawHorizontalGridLines(page)
	}
	if chart.drawYAxisLines {
		chart.drawVerticalGridLines(page)
	}

	if chart.drawXAxisLabels {
		chart.DrawXAxisLabels(page)
	}
	if chart.drawYAxisLabels {
		chart.DrawYAxisLabels(page)
	}

	// Defensive copy so the user's data is never mutated
	plotData := make([][]*Point, len(chart.chartData))
	for i, original := range chart.chartData {
		pointCopy := make([]*Point, len(original))
		for j, p := range original {
			// Create a shallow copy of the Point
			copyPoint := *p
			pointCopy[j] = &copyPoint
		}
		plotData[i] = pointCopy
	}

	// Translate the point coordinates (on the copies)
	for _, points := range plotData {
		for _, point := range points {
			if chart.xyChart {
				point.x = chart.x5 + (point.x-chart.xMin)*(chart.x6-chart.x5)/(chart.xMax-chart.xMin)
				point.y = chart.y8 - (point.y-chart.yMin)*(chart.y8-chart.y5)/(chart.yMax-chart.yMin)
				point.strokeWidth *= (chart.x6 - chart.x5) / chart.w
			} else {
				point.x = chart.x5 + point.x*(chart.x6-chart.x5)/chart.w
				point.y = chart.y8 - (point.y-chart.yMin)*(chart.y8-chart.y5)/(chart.yMax-chart.yMin)
			}
			if point.uri != "" || point.key != "" {
				page.AddAnnotation(&Annotation{
					annotationType: AnnotationLink,
					x1:             point.x - point.r,
					y1:             page.height - (point.y - point.r),
					x2:             point.x + point.r,
					y2:             page.height - (point.y + point.r),
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
	page.SetTextDirection(90)
	page.drawString(
		chart.f1,
		chart.f1.size,
		chart.yAxisTitle,
		chart.x1+chart.f1.bodyHeight,
		chart.y8-((chart.y8-chart.y5)-chart.f1.StringWidth(chart.f1.size, chart.yAxisTitle))/2,
		[3]float32{0.0, 0.0, 0.0},
		nil)

	// Draw the X axis title
	page.SetTextDirection(0)
	page.drawString(
		chart.f1,
		chart.f1.size,
		chart.xAxisTitle,
		chart.x5+((chart.x6-chart.x5)-chart.f1.StringWidth(chart.f1.size, chart.xAxisTitle))/2,
		chart.y4-chart.f1.bodyHeight/2,
		[3]float32{0.0, 0.0, 0.0},
		nil)

	page.SetDefaultLineWidth()
	page.SetDefaultStrokeDashPattern()
	page.SetPenColor(color.Black)
}

func (chart *Chart) formatString() string {
	return fmt.Sprintf("%%.%df", chart.maxFractionDigits)
}

func (chart *Chart) getLongestAxisYLabelWidth() float32 {
	format := chart.formatString()
	minLabelWidth := chart.f2.StringWidth(chart.f2.size, fmt.Sprintf(format, chart.yMin)+"0")
	maxLabelWidth := chart.f2.StringWidth(chart.f2.size, fmt.Sprintf(format, chart.yMax)+"0")
	if maxLabelWidth > minLabelWidth {
		return maxLabelWidth
	}
	return minLabelWidth
}

func (chart *Chart) setXAxisMinAndMaxChartValues() {
	if chart.xAxisGridLines != 0 {
		return
	}
	for _, points := range chart.chartData {
		for _, point := range points {
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
	for _, points := range chart.chartData {
		for _, point := range points {
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
	round := chart.roundMaxAndMinValues(chart.xMax, chart.xMin)
	chart.xMax = round.maxValue
	chart.xMin = round.minValue
	chart.xAxisGridLines = round.numOfGridLines
}

func (chart *Chart) roundYAxisMinAndMaxValues() {
	if chart.yAxisGridLines != 0 {
		return
	}
	round := chart.roundMaxAndMinValues(chart.yMax, chart.yMin)
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

// DrawXAxisLabels draws the X axis labels.
func (chart *Chart) DrawXAxisLabels(page *Page) {
	format := chart.formatString()
	x := chart.x5
	y := chart.y8 + chart.f2.bodyHeight
	step := (chart.x6 - chart.x5) / float32(chart.xAxisGridLines)
	page.SetBrushColor(color.Black)
	for i := 0; i < (chart.xAxisGridLines + 1); i++ {
		label := fmt.Sprintf(format, chart.xMin+((chart.xMax-chart.xMin)/float32(chart.xAxisGridLines))*float32(i))
		page.drawString(
			chart.f2, chart.f2.size, label, x-(chart.f2.StringWidth(chart.f2.size, label)/2), y, [3]float32{0.0, 0.0, 0.0}, nil)
		x += step
	}
}

// DrawYAxisLabels draws the Y axis labels.
func (chart *Chart) DrawYAxisLabels(page *Page) {
	format := chart.formatString()
	x := chart.x5 - chart.getLongestAxisYLabelWidth()
	y := chart.y8 + chart.f2.ascent/3
	step := (chart.y8 - chart.y5) / float32(chart.yAxisGridLines)
	page.SetBrushColor(color.Black)
	for i := 0; i < (chart.yAxisGridLines + 1); i++ {
		label := fmt.Sprintf(format, chart.yMin+((chart.yMax-chart.yMin)/float32(chart.yAxisGridLines))*float32(i))
		page.drawString(chart.f2, chart.f2.size, label, x, y, [3]float32{0.0, 0.0, 0.0}, nil)
		y -= step
	}
}

func (chart *Chart) drawPathsAndPoints(page *Page, chartData [][]*Point) {
	for _, points := range chartData {
		p0 := points[0]
		if p0.drawPath {
			page.SetPenColorRGB(p0.strokeColor)
			page.SetPenWidth(p0.strokeWidth)
			page.SetStrokeDashPattern(p0.strokeDashPattern)
			page.DrawPath(points, pathoperator.Stroke)
			if p0.GetText() != "" {
				page.SetTextDirection(p0.GetTextDirection())
				page.drawString(
					chart.f2,
					chart.f2.size,
					p0.text,
					p0.x+(p0.strokeWidth-chart.f2.ascent)/2.0,
					p0.y,
					p0.textColor,
					nil)
			}
		}
		for _, point := range points {
			if point.GetShape() != shape.Invisible {
				page.SetPenColorRGB(point.strokeColor)
				page.SetPenWidth(point.strokeWidth)
				page.SetStrokeDashPattern(point.strokeDashPattern)
				page.SetBrushColorRGB(point.fillColor)
				page.DrawPoint(point)
			}
		}
	}
}

// roundMaxAndMinValues rounds the axis range to "nice" values for clean grid lines.
// Uses the span (max - min) to support negative values and zero crossings.
// Rounds max up and min down to step multiples, then recomputes grid lines
// to ensure they match the final rounded range.
func (chart *Chart) roundMaxAndMinValues(maxValue, minValue float32) *Round {
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

	round := NewRound()

	// Round max up, min down to nearest step multiple
	round.maxValue = float32(math.Ceil(float64(maxValue/step))) * step
	round.minValue = float32(math.Floor(float64(minValue/step))) * step

	// Recount grid lines from actual rounded range
	round.numOfGridLines = int(math.Round(float64((round.maxValue - round.minValue) / step)))

	return round
}

func (chart *Chart) mean(points []*Point) []float32 {
	_mean := make([]float32, 2)
	n := float32(len(points))
	for _, point := range points {
		_mean[0] += point.x
		_mean[1] += point.y
	}
	_mean[0] /= n
	_mean[1] /= n
	return _mean
}

func (chart *Chart) covar(points []*Point) float32 {
	var covariance float32
	_mean := chart.mean(points)
	for _, point := range points {
		covariance += (point.x - _mean[0]) * (point.y - _mean[1])
	}
	return covariance / float32(len(points)-1)
}

// devsq returns the sum of squares of deviations.
func (chart *Chart) devsq(points []*Point) float32 {
	var _devsq float32
	_mean := chart.mean(points)
	for _, point := range points {
		_devsq += float32(math.Pow(float64(point.x-_mean[0]), float64(2)))
	}
	return _devsq
}

// SetXAxisMinMax sets xMin and xMax for the X axis and the number of X grid lines.
func (chart *Chart) SetXAxisMinMax(xMin, xMax float32, xAxisGridLines int) {
	chart.xMin = xMin
	chart.xMax = xMax
	chart.xAxisGridLines = xAxisGridLines
}

// SetYAxisMinMax sets yMin and yMax for the Y axis and the number of Y grid lines.
func (chart *Chart) SetYAxisMinMax(yMin, yMax float32, yAxisGridLines int) {
	chart.yMin = yMin
	chart.yMax = yMax
	chart.yAxisGridLines = yAxisGridLines
}
