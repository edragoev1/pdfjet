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

	"github.com/edragoev1/pdfjet/v9/src/color"
)

// DonutChart is a donut or pie chart: each slice is its value's share of the
// sum of the values, with a label next to it and its percentage inside it. A
// chart with an inner radius of 0 is a pie chart. See Example_25.
type DonutChart struct {
	f1     *Font
	f2     *Font
	x      float32
	y      float32
	r1     float32
	r2     float32
	slices []*Slice
}

// NewDonutChart creates a donut chart with f1 as the font for the slice labels
// and f2 as the font for the percentages drawn inside the slices. With an inner
// radius of 0 it is a pie chart.
func NewDonutChart(f1, f2 *Font) *DonutChart {
	return &DonutChart{
		f1:     f1,
		f2:     f2,
		slices: make([]*Slice, 0),
	}
}

// SetLocation sets the top left corner of the outer circle of this chart. The
// center is one outer radius to the right of it and one below. The slice labels
// can extend past the circle.
// It returns the chart as a Drawable, so in a chain of setter calls
// SetLocation goes last, right before DrawOn.
func (dc *DonutChart) SetLocation(x, y float32) Drawable {
	dc.x = x
	dc.y = y
	return dc
}

// SetRadii sets the outer and inner radius of this chart; an inner radius of 0 makes a pie chart.
func (dc *DonutChart) SetRadii(outerRadius, innerRadius float32) *DonutChart {
	dc.r1 = outerRadius
	dc.r2 = innerRadius
	return dc
}

// AddSlice adds a slice to this chart.
func (dc *DonutChart) AddSlice(slice *Slice) *DonutChart {
	dc.slices = append(dc.slices, slice)
	return dc
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
	// An arc of radius zero, the center of a pie chart, or of no angle has
	// its control points at its ends; the formula would divide 0 by 0.
	cross := ax*by - ay*bx
	var k2 float32
	if cross != 0 {
		k2 = float32(4.0/3.0) * (float32(math.Sqrt(float64(2*q1*q2))) - q2) / cross
	}

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

// DrawOn draws this chart on the specified page.
// It returns the x and y coordinates of the bottom right corner of the outer
// circle of this chart. The slice labels can extend past it.
func (dc *DonutChart) DrawOn(page *Page) [2]float32 {
	xc := dc.x + dc.r1 // the center of the chart
	yc := dc.y + dc.r1

	// The slices with a value above 0 share the circle
	total := float32(0.0)
	for _, slice := range dc.slices {
		if slice.value > 0.0 {
			total += slice.value
		}
	}
	if total <= 0.0 {
		return [2]float32{xc + dc.r1, yc + dc.r1}
	}

	angle := float32(0.0)
	for _, slice := range dc.slices {
		if slice.value <= 0.0 {
			continue
		}
		sweep := slice.value * 360.0 / total
		angle = dc.drawSlice(
			page, slice.color,
			xc, yc,
			dc.r1, dc.r2,
			angle, angle+sweep,
		)
		dc.drawLinePointer(
			page, slice.text,
			xc, yc,
			dc.r1,
			angle-sweep, angle,
		)

		// The percentage fits inside a slice of 15 degrees or more
		if dc.f2 != nil && sweep >= 15.0 {
			pct := int(math.Floor(float64(slice.value*100.0/total) + 0.5))
			pctStr := fmt.Sprintf("%d%%", pct)
			label := NewTextLine(dc.f2, pctStr)
			label.SetTextColor(color.White)
			midAngle := angle - sweep/2.0 - 90.0
			midR := (dc.r1 + dc.r2) / 2.0
			pos := getPoint(xc, yc, midR, midAngle)
			label.SetLocation(
				pos[0]-dc.f2.StringWidth(dc.f2.size, pctStr)/2.0,
				pos[1]+dc.f2.GetAscent(dc.f2.size)/3.0,
			)
			label.DrawOn(page)
		}
	}

	return [2]float32{xc + dc.r1, yc + dc.r1}
}
