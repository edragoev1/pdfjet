package pdfjet

/**
 * donutchart.go
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
)

// DonutChart is used for donut chart objects.
type DonutChart struct {
	// f1, f2         *Font
	xc, yc, r1, r2 float32
	slices         []*Slice
}

// NewDonutChart creates donut chart object.
func NewDonutChart( /* f1, f2 *Font */ ) *DonutChart {
	chart := new(DonutChart)
	chart.slices = make([]*Slice, 0)
	// chart.f1 = f1
	// chart.f2 = f2
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
	return chart
}

func (chart *DonutChart) AddSlice(slice *Slice) {
	chart.slices = append(chart.slices, slice)
}

func GetControlPoints(xc, yc, x0, y0, x3, y3 float32) [][2]float32 {
	points := make([][2]float32, 0)

	ax := x0 - xc
	ay := y0 - yc
	bx := x3 - xc
	by := y3 - yc
	q1 := ax*ax + ay*ay
	q2 := q1 + ax*bx + ay*by
	k2 := float32(4.0/3.0) * (float32(math.Sqrt(float64(2*q1*q2))) - q2) / (ax*by - ay*bx)

	// Control points coordinates
	x1 := xc + ax - k2*ay
	y1 := yc + ay + k2*ax
	x2 := xc + bx + k2*by
	y2 := yc + by - k2*bx

	points = append(points, [2]float32{x0, y0})
	points = append(points, [2]float32{x1, y1})
	points = append(points, [2]float32{x2, y2})
	points = append(points, [2]float32{x3, y3})

	return points
}

func GetPoint(xc, yc, radius, angle float32) [2]float32 {
	x := xc + radius*float32(math.Cos(float64(angle)*math.Pi/180.0))
	y := yc + radius*float32(math.Sin(float64(angle)*math.Pi/180.0))
	return [2]float32{x, y}
}

func DrawSlice(
	page *Page,
	fillColor int32,
	xc, yc, r1, r2, a1, a2 float32) float32 { // a1 > a2
	page.SetBrushColor(fillColor)

	angle1 := a1 - 90.0
	angle2 := a2 - 90.0

	points1 := make([][2]float32, 0)
	points2 := make([][2]float32, 0)
	for {
		if (angle2 - angle1) <= 90.0 {
			p0 := GetPoint(xc, yc, r1, angle1) // Start point
			p3 := GetPoint(xc, yc, r1, angle2) // End point
			s1 := GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points1 = append(points1, s1...)
			p0 = GetPoint(xc, yc, r2, angle1) // Start point
			p3 = GetPoint(xc, yc, r2, angle2) // End point
			s1 = GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points2 = append(points2, s1...)
			break
		} else {
			p0 := GetPoint(xc, yc, r1, angle1)
			p3 := GetPoint(xc, yc, r1, angle1+90.0)
			s1 := GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points1 = append(points1, s1...)
			p0 = GetPoint(xc, yc, r2, angle1)
			p3 = GetPoint(xc, yc, r2, angle1+90.0)
			s1 = GetControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points2 = append(points2, s1...)
			angle1 += 90.0
		}
	}
	// Reverse the points2 slice
	for i, j := 0, len(points2)-1; i < j; i, j = i+1, j-1 {
		points2[i], points2[j] = points2[j], points2[i]
	}

	page.MoveTo(points1[0][0], points1[0][1])
	for i := 0; i <= (len(points1) - 4); i += 4 {
		page.CurveTo(
			points1[i+1][0], points1[i+1][1],
			points1[i+2][0], points1[i+2][1],
			points1[i+3][0], points1[i+3][1])
	}
	page.LineTo(points2[0][0], points2[0][1])
	for i := 0; i <= (len(points2) - 4); i += 4 {
		page.CurveTo(
			points2[i+1][0], points2[i+1][1],
			points2[i+2][0], points2[i+2][1],
			points2[i+3][0], points2[i+3][1])
	}
	page.FillPath()

	return a2
}

// DrawOn draws donut chart on the specified page.
func (chart *DonutChart) DrawOn(page *Page) {
	var angle float32 = 0.0
	for _, slice := range chart.slices {
		angle = DrawSlice(
			page, slice.color,
			chart.xc, chart.yc,
			chart.r1, chart.r2,
			slice.angle, angle+slice.angle)
	}
}
