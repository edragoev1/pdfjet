package pdfjet

/**
 * chart.go
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
	"fmt"
	"math"

	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/operation"
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
	chart.chartBorderWidth = 0.3
	chart.innerBorderWidth = 0.3
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

// SetData sets the data that will be used to draw chart chart.
func (chart *Chart) SetData(chartData [][]*Point) {
	chart.chartData = chartData
}

// GetData returns the chart data.
func (chart *Chart) GetData() [][]*Point {
	return chart.chartData
}

// SetLocation sets the location of chart chart on the page.
func (chart *Chart) SetLocation(x, y float32) {
	chart.x1 = x
	chart.y1 = y
}

// SetSize sets the size of chart chart.
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
	return (chart.covar(points) / chart.devsq(points) * float32(len(points)-1))
}

// Intercept calculates the intercept of a trend line given a list of points.
// See Example_09.
func (chart *Chart) Intercept(points []*Point, slope float32) float32 {
	_mean := chart.mean(points)
	return (_mean[1] - slope*_mean[0])
}

// SetDrawXAxisLines -- TODO:
func (chart *Chart) SetDrawXAxisLines(drawXAxisLines bool) {
	chart.drawXAxisLines = drawXAxisLines
}

// SetDrawYAxisLines -- TODO:
func (chart *Chart) SetDrawYAxisLines(drawYAxisLines bool) {
	chart.drawYAxisLines = drawYAxisLines
}

// SetDrawXAxisLabels -- TODO:
func (chart *Chart) SetDrawXAxisLabels(drawXAxisLabels bool) {
	chart.drawXAxisLabels = drawXAxisLabels
}

// SetDrawYAxisLabels -- TODO:
func (chart *Chart) SetDrawYAxisLabels(drawYAxisLabels bool) {
	chart.drawYAxisLabels = drawYAxisLabels
}

// SetXYChart -- TODO:
func (chart *Chart) SetXYChart(xyChart bool) {
	chart.xyChart = xyChart
}

// DrawOn draws chart chart on the specified page.
// @param page the page to draw chart chart on.
func (chart *Chart) DrawOn(page *Page) {
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

	// Draw chart title
	page.drawString(
		chart.f1,
		chart.title,
		chart.x1+((chart.w-chart.f1.stringWidth(chart.title))/2),
		chart.y1+1.5*chart.f1.bodyHeight,
		color.Black,
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

	// Translate the point coordinates
	for _, points := range chart.chartData {
		for _, point := range points {
			if chart.xyChart {
				point.x = chart.x5 + (point.x-chart.xMin)*(chart.x6-chart.x5)/(chart.xMax-chart.xMin)
				point.y = chart.y8 - (point.y-chart.yMin)*(chart.y8-chart.y5)/(chart.yMax-chart.yMin)
				point.lineWidth *= (chart.x6 - chart.x5) / chart.w
			} else {
				point.x = chart.x5 + point.x*(chart.x6-chart.x5)/chart.w
				point.y = chart.y8 - (point.y-chart.yMin)*(chart.y8-chart.y5)/(chart.yMax-chart.yMin)
			}
			if point.uri != nil || point.key != nil {
				page.AddAnnotation(NewAnnotation(
					point.uri,
					nil,
					point.x-point.r,
					page.height-(point.y-point.r),
					point.x+point.r,
					page.height-(point.y+point.r),
					"",
					"",
					""))
			}
		}
	}

	chart.drawPathsAndPoints(page, chart.chartData)

	// Draw the Y axis title
	page.SetBrushColor(color.Black)
	page.SetTextDirection(90)
	page.drawString(
		chart.f1,
		chart.yAxisTitle,
		chart.x1+chart.f1.bodyHeight,
		chart.y8-((chart.y8-chart.y5)-chart.f1.stringWidth(chart.yAxisTitle))/2,
		color.Black,
		nil)

	// Draw the X axis title
	page.SetTextDirection(0)
	page.drawString(
		chart.f1,
		chart.xAxisTitle,
		chart.x5+((chart.x6-chart.x5)-chart.f1.stringWidth(chart.xAxisTitle))/2,
		chart.y4-chart.f1.bodyHeight/2,
		color.Black,
		nil)

	page.SetDefaultLineWidth()
	page.SetDefaultLinePattern()
	page.SetPenColor(color.Black)
}

func (chart *Chart) getLongestAxisYLabelWidth() float32 {
	minLabelWidth := chart.f2.stringWidth(fmt.Sprintf("%.2f", chart.yMin) + "0")
	maxLabelWidth := chart.f2.stringWidth(fmt.Sprintf("%.2f", chart.yMax) + "0")
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
	page.StrokePath()
}

func (chart *Chart) drawInnerBorder(page *Page) {
	page.SetPenWidth(chart.innerBorderWidth)
	page.SetPenColor(color.Black)
	page.MoveTo(chart.x5, chart.y5)
	page.LineTo(chart.x6, chart.y6)
	page.LineTo(chart.x7, chart.y7)
	page.LineTo(chart.x8, chart.y8)
	page.ClosePath()
	page.StrokePath()
}

func (chart *Chart) drawHorizontalGridLines(page *Page) {
	page.SetPenWidth(chart.hGridLineWidth)
	page.SetPenColor(color.Black)
	page.SetLinePattern(chart.hGridLinePattern)
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
	page.SetLinePattern(chart.vGridLinePattern)
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
	x := chart.x5
	y := chart.y8 + chart.f2.bodyHeight
	step := (chart.x6 - chart.x5) / float32(chart.xAxisGridLines)
	page.SetBrushColor(color.Black)
	for i := 0; i < (chart.xAxisGridLines + 1); i++ {
		label := fmt.Sprintf("%.2f", chart.xMin+((chart.xMax-chart.xMin)/float32(chart.xAxisGridLines))*float32(i))
		page.drawString(chart.f2, label, x-(chart.f2.stringWidth(label)/2), y, color.Black, nil)
		x += step
	}
}

// DrawYAxisLabels draws the Y axis labels.
func (chart *Chart) DrawYAxisLabels(page *Page) {
	x := chart.x5 - chart.getLongestAxisYLabelWidth()
	y := chart.y8 + chart.f2.ascent/3
	step := (chart.y8 - chart.y5) / float32(chart.yAxisGridLines)
	page.SetBrushColor(color.Black)
	for i := 0; i < (chart.yAxisGridLines + 1); i++ {
		label := fmt.Sprintf("%.2f", chart.yMin+((chart.yMax-chart.yMin)/float32(chart.yAxisGridLines))*float32(i))
		page.drawString(chart.f2, label, x, y, color.Black, nil)
		y -= step
	}
}

func (chart *Chart) drawPathsAndPoints(page *Page, chartData [][]*Point) {
	for i := 0; i < len(chartData); i++ {
		points := chartData[i]
		point := points[0]
		if point.drawPath {
			page.SetPenColor(point.color)
			page.SetPenWidth(point.lineWidth)
			page.SetLinePattern(point.linePattern)
			page.DrawPath(points, operation.Stroke)
			if point.GetText() != "" {
				page.SetBrushColor(point.GetTextColor())
				page.SetTextDirection(point.GetTextDirection())
				page.drawString(chart.f2, point.text, point.x, point.y, color.Black, nil)
			}
		}
		for j := 0; j < len(points); j++ {
			point = points[j]
			if point.GetShape() != shape.Invisible {
				page.SetPenWidth(point.lineWidth)
				page.SetLinePattern(point.linePattern)
				page.SetPenColor(point.color)
				page.SetBrushColor(point.color)
				page.DrawPoint(point)
			}
		}
	}
}

func (chart *Chart) roundMaxAndMinValues(maxValue, minValue float32) *Round {
	maxExponent := int(math.Floor(float64(math.Log(float64(maxValue))) / float64(math.Log(10))))
	maxValue *= float32(math.Pow(10, float64(-maxExponent)))

	if maxValue > 9.00 {
		maxValue = 10.0
	} else if maxValue > 8.00 {
		maxValue = 9.00
	} else if maxValue > 7.00 {
		maxValue = 8.00
	} else if maxValue > 6.00 {
		maxValue = 7.00
	} else if maxValue > 5.00 {
		maxValue = 6.00
	} else if maxValue > 4.00 {
		maxValue = 5.00
	} else if maxValue > 3.50 {
		maxValue = 4.00
	} else if maxValue > 3.00 {
		maxValue = 3.50
	} else if maxValue > 2.50 {
		maxValue = 3.00
	} else if maxValue > 2.00 {
		maxValue = 2.50
	} else if maxValue > 1.75 {
		maxValue = 2.00
	} else if maxValue > 1.50 {
		maxValue = 1.75
	} else if maxValue > 1.25 {
		maxValue = 1.50
	} else if maxValue > 1.00 {
		maxValue = 1.25
	} else {
		maxValue = 1.00
	}

	round := NewRound()

	if maxValue == 10.0 {
		round.numOfGridLines = 10
	} else if maxValue == 9.00 {
		round.numOfGridLines = 9
	} else if maxValue == 8.00 {
		round.numOfGridLines = 8
	} else if maxValue == 7.00 {
		round.numOfGridLines = 7
	} else if maxValue == 6.00 {
		round.numOfGridLines = 6
	} else if maxValue == 5.00 {
		round.numOfGridLines = 5
	} else if maxValue == 4.00 {
		round.numOfGridLines = 8
	} else if maxValue == 3.50 {
		round.numOfGridLines = 7
	} else if maxValue == 3.00 {
		round.numOfGridLines = 6
	} else if maxValue == 2.50 {
		round.numOfGridLines = 5
	} else if maxValue == 2.00 {
		round.numOfGridLines = 8
	} else if maxValue == 1.75 {
		round.numOfGridLines = 7
	} else if maxValue == 1.50 {
		round.numOfGridLines = 6
	} else if maxValue == 1.25 {
		round.numOfGridLines = 5
	} else if maxValue == 1.00 {
		round.numOfGridLines = 10
	}

	round.maxValue = maxValue * float32(math.Pow(float64(10), float64(maxExponent)))
	step := round.maxValue / float32(round.numOfGridLines)
	temp := round.maxValue
	round.numOfGridLines = 0
	for {
		round.numOfGridLines++
		temp -= step
		if temp <= minValue {
			round.minValue = temp
			break
		}
	}

	return round
}

func (chart *Chart) mean(points []*Point) []float32 {
	_mean := make([]float32, 2)
	for i := 0; i < len(points); i++ {
		point := points[i]
		_mean[0] += point.x
		_mean[1] += point.y
	}
	_mean[0] /= float32(len(points) - 1)
	_mean[1] /= float32(len(points) - 1)
	return _mean
}

func (chart *Chart) covar(points []*Point) float32 {
	var covariance float32
	_mean := chart.mean(points)
	for _, point := range points {
		covariance += (point.x - _mean[0]) * (point.y - _mean[1])
	}
	return (covariance / float32((len(points) - 1)))
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
