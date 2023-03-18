package pdfjet

/**
 * docutchart.go
 *
Copyright 2023 Innovatics Inc.

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
	"math"

	"github.com/edragoev1/pdfjet/src/operation"
)

// DonutChart is used for donut chart objects.
type DonutChart struct {
	f1, f2         *Font
	chartData      [][]*Point
	xc, yc, r1, r2 float32
	angles         []float32
	colors         []int32
	isDonutChart   bool
}

// NewDonutChart creates donut chart object.
func NewDonutChart(f1, f2 *Font) *DonutChart {
	chart := new(DonutChart)
	chart.f1 = f1
	chart.f2 = f2
	chart.isDonutChart = true
	return chart
}

// SetLocation sets the chart location.
func (chart *DonutChart) SetLocation(xc, yc float32) *DonutChart {
	chart.xc = xc
	chart.yc = yc
	return chart
}

// SetR1AndR2 sets the inner r1 and the outer r2 radius of the donut.
func (chart *DonutChart) SetR1AndR2(r1, r2 float32) *DonutChart {
	chart.r1 = r1
	chart.r2 = r2
	if chart.r1 < 1.0 {
		chart.isDonutChart = false
	}
	return chart
}

// GetBezierCurvePoints calculates the bezier curve points for a given arc of a circle.
// @param xc the x-coordinate of the circle's centre.
// @param yc the y-coordinate of the circle's centre.
// @param angle1 the start angle of the arc in degrees.
// @param angle2 the end angle of the arc in degrees.
func (chart *DonutChart) GetBezierCurvePoints(xc, yc, r, angle1, angle2 float32) []*Point {
	angle1 *= -1.0
	angle2 *= -1.0
	// Start point coordinates
	x1 := xc + r*float32(math.Cos(float64(angle1)*(math.Pi/180)))
	y1 := yc + r*float32(math.Sin(float64(angle1)*(math.Pi/180)))
	// End point coordinates
	x4 := xc + r*float32(math.Cos(float64(angle2)*(math.Pi/180)))
	y4 := yc + r*float32(math.Sin(float64(angle2)*(math.Pi/180)))

	ax := x1 - xc
	ay := y1 - yc
	bx := x4 - xc
	by := y4 - yc
	q1 := ax*ax + ay*ay
	q2 := q1 + ax*bx + ay*by

	k2 := float32(4.0/3.0) * (float32(math.Sqrt(float64(2*q1*q2))) - q2) / (ax*by - ay*bx)

	x2 := xc + ax - k2*ay
	y2 := yc + ay + k2*ax
	x3 := xc + bx + k2*by
	y3 := yc + by - k2*bx

	list := make([]*Point, 0)
	list = append(list, NewPoint(x1, y1))
	list = append(list, NewControlPoint(x2, y2))
	list = append(list, NewControlPoint(x3, y3))
	list = append(list, NewPoint(x4, y4))

	return list
}

// GetArcPoints calculates a list of points for a given arc of a circle
// @param xc the x-coordinate of the circle's centre.
// @param yc the y-coordinate of the circle's centre
// @param r the radius of the circle.
// @param angle1 the start angle of the arc in degrees.
// @param angle2 the end angle of the arc in degrees.
// @param includeOrigin whether the origin should be included in the list (thus creating a pie shape).
func (chart *DonutChart) GetArcPoints(xc, yc, r, angle1, angle2 float32, includeOrigin bool) []*Point {
	list := make([]*Point, 0)

	if includeOrigin {
		list = append(list, NewPoint(xc, yc))
	}
	if angle1 <= angle2 {
		startAngle := angle1
		endAngle := angle1 + 90
		for endAngle < angle2 {
			list = append(list, chart.GetBezierCurvePoints(xc, yc, r, startAngle, endAngle)...)
			startAngle += 90
			endAngle += 90
		}
		endAngle -= 90
		list = append(list, chart.GetBezierCurvePoints(xc, yc, r, endAngle, angle2)...)
	} else {
		startAngle := angle1
		endAngle := angle1 - 90
		for endAngle > angle2 {
			list = append(list, chart.GetBezierCurvePoints(xc, yc, r, startAngle, endAngle)...)
			startAngle -= 90
			endAngle -= 90
		}
		endAngle += 90
		list = append(list, chart.GetBezierCurvePoints(xc, yc, r, endAngle, angle2)...)
	}

	return list
}

// GetDonutPoints calculates a list of points for a given donut sector of a circle.
// @param xc the x-coordinate of the circle's centre.
// @param yc the y-coordinate of the circle's centre.
// @param r1 the inner radius of the donut.
// @param r2 the outer radius of the donut.
// @param angle1 the start angle of the donut sector in degrees.
// @param angle2 the end angle of the donut sector in degrees.
func (chart *DonutChart) GetDonutPoints(xc, yc, r1, r2, angle1, angle2 float32) []*Point {
	list := make([]*Point, 0)
	list = append(list, chart.GetArcPoints(xc, yc, r1, angle1, angle2, false)...)
	list = append(list, chart.GetArcPoints(xc, yc, r2, angle2, angle1, false)...)
	return list
}

// AddSector -- TODO:
func (chart *DonutChart) AddSector(angle float32, color int32) {
	chart.angles = append(chart.angles, angle)
	chart.colors = append(chart.colors, color)
}

// DrawOn draws donut chart on the specified page.
func (chart *DonutChart) DrawOn(page *Page) {
	startAngle := float32(0.0)
	endAngle := float32(0.0)
	lastColorIndex := 0
	for i, angle := range chart.angles {
		endAngle = startAngle + float32(angle)
		list := make([]*Point, 0)
		if chart.isDonutChart {
			list = append(list, chart.GetDonutPoints(chart.xc, chart.yc, chart.r1, chart.r2, startAngle, endAngle)...)
		} else {
			list = append(list, chart.GetArcPoints(chart.xc, chart.yc, chart.r2, startAngle, endAngle, true)...)
		}
		// for _, point := range list {
		// 	point.DrawOn(page)
		// }
		page.SetBrushColor(chart.colors[i])
		page.DrawPath(list, operation.Fill)
		startAngle = endAngle
		lastColorIndex = i
	}
	if endAngle < 360 {
		endAngle = 360
		list := make([]*Point, 0)
		if chart.isDonutChart {
			list = append(list, chart.GetDonutPoints(chart.xc, chart.yc, chart.r1, chart.r2, startAngle, endAngle)...)
		} else {
			list = append(list, chart.GetArcPoints(chart.xc, chart.yc, chart.r2, startAngle, endAngle, true)...)
		}
		// for _, point := range list {
		// 	point.DrawOn(page)
		// }
		page.SetBrushColor(chart.colors[lastColorIndex+1])
		page.DrawPath(list, operation.Fill)
	}
}
