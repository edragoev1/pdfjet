//
// donutchart.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

import (
	"fmt"
	"math"

	"github.com/edragoev1/pdfjet/src/color"
)

// DonutChart is used to create donut chart objects and draw them on a page.
//
// Please see Example_25.go
type DonutChart struct {
	f1           *Font
	f2           *Font
	xc           float32
	yc           float32
	r1           float32
	r2           float32
	slices       []*Slice
	isDonutChart bool
}

// NewDonutChart creates a new DonutChart.
// Pass isDonutChart=false to render a pie chart (no inner radius).
func NewDonutChart(f1, f2 *Font, isDonutChart bool) *DonutChart {
	return &DonutChart{
		f1:           f1,
		f2:           f2,
		isDonutChart: isDonutChart,
		slices:       make([]*Slice, 0),
	}
}

func (dc *DonutChart) SetLocation(xc, yc float32) {
	dc.xc = xc
	dc.yc = yc
}

func (dc *DonutChart) SetR1AndR2(r1, r2 float32) {
	dc.r1 = r1
	dc.r2 = r2
}

func (dc *DonutChart) AddSlice(slice *Slice) {
	dc.slices = append(dc.slices, slice)
}

// getControlPoints computes the four Bézier control points for a
func getControlPoints(xc, yc, x0, y0, x3, y3 float32) [][2]float32 {
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

func getPoint(xc, yc, radius, angle float32) [2]float32 {
	x := xc + radius*float32(math.Cos(float64(angle)*math.Pi/180.0))
	y := yc + radius*float32(math.Sin(float64(angle)*math.Pi/180.0))
	return [2]float32{x, y}
}

// drawSlice draws one wedge of the donut/pie and returns the new
// cumulative angle (a2).
func (dc *DonutChart) drawSlice(
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
			p0 := getPoint(xc, yc, r1, angle1) // Start point
			p3 := getPoint(xc, yc, r1, angle2) // End point
			s1 := getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points1 = append(points1, s1...)
			p0 = getPoint(xc, yc, r2, angle1) // Start point
			p3 = getPoint(xc, yc, r2, angle2) // End point
			s1 = getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points2 = append(points2, s1...)
			break
		} else {
			p0 := getPoint(xc, yc, r1, angle1)
			p3 := getPoint(xc, yc, r1, angle1+90.0)
			s1 := getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
			points1 = append(points1, s1...)
			p0 = getPoint(xc, yc, r2, angle1)
			p3 = getPoint(xc, yc, r2, angle1+90.0)
			s1 = getControlPoints(xc, yc, p0[0], p0[1], p3[0], p3[1])
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

// drawLinePointer draws the leader line and label for a slice.
func (dc *DonutChart) drawLinePointer(
	page *Page,
	text string,
	xc, yc, r1, a1, a2 float32,
) {
	midAngle := (a1+a2)/2.0 - 90.0

	// Point on the outer edge of the donut
	p1 := getPoint(xc, yc, r1, midAngle)

	// Elbow point — 15pt beyond the outer edge
	r3 := r1 + 15.0
	p2 := getPoint(xc, yc, r3, midAngle)

	// Draw the pointer line: edge → elbow → horizontal end
	page.SetPenColor(color.Black)
	page.SetPenWidth(1.0)
	page.MoveTo(p1[0], p1[1])
	page.LineTo(p2[0], p2[1])

	if dc.f1 != nil && text != "" {
		textWidth := dc.f1.StringWidth(dc.f1.size, text)
		onRightSide := math.Cos(float64(midAngle)*math.Pi/180.0) >= 0

		padding := float32(4.0)
		lineLength := textWidth + padding

		var xEnd, yEnd float32
		if onRightSide {
			xEnd = p2[0] + lineLength
		} else {
			xEnd = p2[0] - lineLength
		}
		yEnd = p2[1]

		// Continue the path to the horizontal end
		page.LineTo(xEnd, yEnd)
		page.StrokePath()

		// Draw the label text just above the horizontal line
		label := NewTextLine(dc.f1, text)
		label.SetTextColor(color.Black)
		if onRightSide {
			label.SetLocation(p2[0]+2.0, yEnd-dc.f1.GetAscent(dc.f1.size)/3.0)
		} else {
			label.SetLocation(xEnd+2.0, yEnd-dc.f1.GetAscent(dc.f1.size)/3.0)
		}
		label.DrawOn(page)
	} else {
		// No text — short horizontal stub
		onRightSide := math.Cos(float64(midAngle)*math.Pi/180.0) >= 0
		var xEnd float32
		if onRightSide {
			xEnd = p2[0] + 20.0
		} else {
			xEnd = p2[0] - 20.0
		}
		page.LineTo(xEnd, p2[1])
		page.StrokePath()
	}
}

// DrawOn renders the donut chart onto the given page.
func (dc *DonutChart) DrawOn(page *Page) error {
	if len(dc.slices) == 0 {
		return nil
	}

	var innerR float32
	if dc.isDonutChart {
		innerR = dc.r2
	}

	angle := float32(0.0)
	for _, slice := range dc.slices {
		angle = dc.drawSlice(
			page, slice.color,
			dc.xc, dc.yc,
			dc.r1, innerR,
			angle, angle+slice.angle,
		)
		dc.drawLinePointer(
			page, slice.text,
			dc.xc, dc.yc,
			dc.r1,
			angle-slice.angle, angle,
		)

		// Percent label inside the slice
		if dc.f2 != nil && slice.angle >= 15.0 {
			pct := int(float64(slice.angle) / 360.0 * 100.0)
			pctStr := fmt.Sprintf("%d%%", pct)
			label := NewTextLine(dc.f2, pctStr)
			label.SetTextColor(color.White)
			midAngle := angle - slice.angle/2.0 - 90.0
			midR := (dc.r1 + innerR) / 2.0
			pos := getPoint(dc.xc, dc.yc, midR, midAngle)
			label.SetLocation(
				pos[0]-dc.f2.StringWidth(dc.f2.size, pctStr)/2.0,
				pos[1]+dc.f2.GetAscent(dc.f2.size)/3.0,
			)
			label.DrawOn(page)
		}
	}

	return nil
}
