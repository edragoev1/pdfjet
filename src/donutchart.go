//
// donutchart.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"fmt"
	"math"
	"sort"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// DonutChart is a donut or pie chart: each slice is its value's share of the
// sum of the values, with a label next to it and its percentage inside it. A
// chart with an inner radius of 0 is a pie chart. In the look of the other
// charts: a title and a subtitle at the top left, thin white lines between the
// slices, gray leader lines, the labels in ink, and each percentage white or
// ink by its slice's color. See Example_28.
type DonutChart struct {
	f1     *Font // The regular font: the labels, the subtitle and the legend
	f2     *Font // The bold font: the percentages, the title and the text in the hole
	x      float32
	y      float32
	r1     float32
	r2     float32
	slices []*donutSlice
	// The alternate description; the label and the percentage of each slice
	// when it is empty
	altDescription string
	title          string
	subtitle       string
	centerText     string
	legend         bool
}

// donutLeader is the color of the leader lines, in the look of the simple charts.
const donutLeader int32 = 0x8C96A3

// NewDonutChart creates a donut chart. With an inner radius of 0 it is a pie
// chart. Its fonts are in the order of the other charts': the bold one first.
//   - bold: the percentages inside the slices, the title and the text in the
//     hole.
//   - regular: the slice labels, the subtitle and the legend, and the size of
//     the title.
func NewDonutChart(bold, regular *Font) *DonutChart {
	return &DonutChart{
		f1:     regular,
		f2:     bold,
		slices: make([]*donutSlice, 0),
	}
}

// SetLocation sets the top left corner of this chart: of its outer circle, or
// of its title when it has one, the circle under it. The center is one outer
// radius to the right of the corner of the circle and one below. The slice
// labels can extend past the circle.
// It returns the chart as a Drawable, so in a chain of setter calls
// SetLocation goes last, right before DrawOn.
func (dc *DonutChart) SetLocation(x, y float32) Drawable {
	dc.x = x
	dc.y = y
	return dc
}

// SetRadii sets the outer and inner radius of this chart; an inner radius of 0 makes a pie chart.
func (dc *DonutChart) SetRadii(outerRadius, innerRadius float32) *DonutChart {
	// The larger is the outer radius: an inner radius past the outer one drew
	// a ring inside out
	// A radius that is not a number is 0, in the four ports alike
	a := float32(0.0)
	if isValue(outerRadius) {
		a = abs32(outerRadius)
	}
	b := float32(0.0)
	if isValue(innerRadius) {
		b = abs32(innerRadius)
	}
	dc.r1 = max(a, b)
	dc.r2 = min(a, b)
	return dc
}

// Add adds a slice in the next color of the palette, blue first, the palette
// of BarChart, so that the charts of a document match; its ten colors repeat
// from the eleventh slice on. The slice's share of the sum of the values sets its
// angle; a value that is not above 0 has no slice.
//   - name: the label of the slice; empty for none.
//   - value: the value of the slice.
func (dc *DonutChart) Add(name string, value float32) *DonutChart {
	return dc.AddWithColor(name, value, slicePalette)
}

// AddWithColor adds a slice in the color.
//   - name: the label of the slice; empty for none.
//   - value: the value of the slice.
//   - c: the color as a 0xRRGGBB value, for example color.Blue.
func (dc *DonutChart) AddWithColor(name string, value float32, c int32) *DonutChart {
	dc.slices = append(dc.slices, &donutSlice{value: value, color: c, text: name})
	return dc
}

// donutSlice is a slice: its value, its color, or slicePalette for the next of
// the palette, and its label.
type donutSlice struct {
	value float32
	color int32
	text  string
}

// slicePalette is the color of a slice that takes the next color of the palette.
const slicePalette int32 = -1

// SetTitle sets the title, drawn at the top left of the chart's location in the
// bold font at the size of the labels; the circle is drawn under it. A chart
// without the bold font, which draws no percentages, draws its title in the
// regular one. A chart without a title has the circle at its location.
func (dc *DonutChart) SetTitle(title string) *DonutChart {
	dc.title = title
	return dc
}

// SetSubtitle sets the subtitle, drawn under the title in a muted gray.
func (dc *DonutChart) SetSubtitle(subtitle string) *DonutChart {
	dc.subtitle = subtitle
	return dc
}

// SetCenterText sets the text written in the hole of a donut chart, in the
// font of the title, as large as one and a half labels or as fits: the total,
// "100 crates". A text too wide for the hole at three quarters of a label is
// shortened with an ellipsis. A pie chart, with no hole, does not write it.
func (dc *DonutChart) SetCenterText(centerText string) *DonutChart {
	dc.centerText = centerText
	return dc
}

// SetLegend draws a legend under the circle, a swatch, the label and the
// percentage of each slice, in rows as wide as the circle, in place of the
// leader lines and the labels beside the circle: for many slices, or small
// ones whose labels would crowd.
func (dc *DonutChart) SetLegend(legend bool) *DonutChart {
	dc.legend = legend
	return dc
}

// SetAltDescription sets the alternate description of the chart, which a
// screen reader reads in a PDF/UA document, where the chart is a figure. The
// default is the title, the subtitle and the text in the hole, as sentences,
// and the label and the percentage of each slice.
func (dc *DonutChart) SetAltDescription(altDescription string) *DonutChart {
	dc.altDescription = altDescription
	return dc
}

// getControlPoints computes the four Bézier control points for a
func getControlPoints(xc, yc, x0, y0, x3, y3 float32) [][2]float32 {
	points := make([][2]float32, 0)

	ax := x0 - xc
	ay := y0 - yc
	bx := x3 - xc
	by := y3 - yc
	q1 := float32(ax*ax) + float32(ay*ay)
	q2 := q1 + float32(ax*bx) + float32(ay*by)
	// An arc of radius zero, the center of a pie chart, or of no angle has
	// its control points at its ends; the formula would divide 0 by 0.
	cross := float32(ax*by) - float32(ay*bx)
	var k2 float32
	if cross != 0 {
		k2 = float32(float32(4.0/3.0)*(float32(math.Sqrt(float64(float32(float32(2*q1)*q2))))-q2)) / cross
	}

	// Control points coordinates
	x1 := xc + ax - float32(k2*ay)
	y1 := yc + ay + float32(k2*ax)
	x2 := xc + bx + float32(k2*by)
	y2 := yc + by - float32(k2*bx)

	points = append(points, [2]float32{x0, y0})
	points = append(points, [2]float32{x1, y1})
	points = append(points, [2]float32{x2, y2})
	points = append(points, [2]float32{x3, y3})

	return points
}

func getPoint(xc, yc, radius, angle float32) [2]float32 {
	x := xc + float32(radius*float32(math.Cos(float64(float64(float64(angle)*math.Pi)/180.0))))
	y := yc + float32(radius*float32(math.Sin(float64(float64(float64(angle)*math.Pi)/180.0))))
	return [2]float32{x, y}
}

// drawSlice draws one wedge of the donut/pie and returns the new
// cumulative angle (a2).
func (dc *DonutChart) drawSlice(
	page *Page,
	fillColor int32,
	xc, yc, r1, r2, a1, a2 float32) float32 { // a1 > a2
	page.SetBrushColor(fillColor)

	// No arc of angles that are not numbers, whose loop never ended
	if !isValue(a1) || !isValue(a2) {
		return a1
	}
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

// donutPointer is the leader line and the label of a slice: the point on the
// edge of the circle, the elbow 15 points past it, the y of the label's line,
// moved down from the elbow's when it would meet the label above it, and the
// side of the circle it is on; and its index among the pointers.
type donutPointer struct {
	text   string
	p1     [2]float32
	p2     [2]float32
	right  bool
	xElbow float32
	yEnd   float32
	index  int
}

// pointerOf returns the pointer of a slice from the angle a1 to a2.
func (dc *DonutChart) pointerOf(text string, xc, yc, r1, a1, a2 float32, index int) *donutPointer {
	midAngle := float32((a1+a2)/2.0) - 90.0
	p1 := getPoint(xc, yc, r1, midAngle)      // On the outer edge
	p2 := getPoint(xc, yc, r1+15.0, midAngle) // The elbow
	// A label wider than both the radius and six of its letters is
	// shortened: a long one at the left ran off the page
	if dc.f1 != nil {
		text = shorten(dc.f1, dc.f1.size, text, max(r1, float32(6.0*dc.f1.size)))
	}
	right := math.Cos(float64(float64(float64(midAngle)*math.Pi)/180.0)) >= 0
	return &donutPointer{text, p1, p2, right, p2[0], p2[1], index}
}

// moveApart moves the labels on each side of the circle apart, top to bottom,
// where one would meet the one above it, and each moved label's elbow out to
// where its height leaves the circle, 15 points past its edge: thin slices
// side by side stacked their labels down a column across the ring.
func (dc *DonutChart) moveApart(pointers []*donutPointer, xc, yc float32) {
	if dc.f1 == nil {
		return
	}
	for side := 0; side < 2; side++ {
		labels := make([]*donutPointer, 0, len(pointers))
		for _, pointer := range pointers {
			if pointer.right == (side == 0) && pointer.text != "" {
				labels = append(labels, pointer)
			}
		}
		// Top to bottom, and labels at one height in the order of their slices
		sort.SliceStable(labels, func(a, b int) bool {
			if labels[a].yEnd != labels[b].yEnd {
				return labels[a].yEnd < labels[b].yEnd
			}
			return labels[a].index < labels[b].index
		})
		for k := 1; k < len(labels); k++ {
			labels[k].yEnd = max(labels[k].yEnd, labels[k-1].yEnd+dc.f1.bodyHeight)
		}
		r3 := dc.r1 + 15.0
		for _, label := range labels {
			if label.yEnd == label.p2[1] {
				continue // Where it was
			}
			dy := label.yEnd - yc
			out := float32(0.0)
			if abs32(dy) < r3 {
				out = float32(math.Sqrt(float64(float64(r3)*float64(r3)) - float64(float64(dy)*float64(dy))))
			}
			if label.right {
				label.xElbow = max(label.p2[0], xc+out)
			} else {
				label.xElbow = min(label.p2[0], xc-out)
			}
		}
	}
}

// drawPointer returns the box of the leader line and the label of the
// pointer: its left, top, right and bottom; and draws them when there is a
// page.
func (dc *DonutChart) drawPointer(page *Page, pointer *donutPointer) [4]float32 {
	p1 := pointer.p1
	x2 := pointer.xElbow
	yEnd := pointer.yEnd
	if page != nil {
		// The line: edge, elbow, and along to the end, thin and gray
		page.SetPenColor(donutLeader)
		page.SetPenWidth(0.75)
		page.MoveTo(p1[0], p1[1])
		page.LineTo(x2, yEnd)
	}
	if dc.f1 != nil && pointer.text != "" {
		textWidth := dc.f1.StringWidth(dc.f1.size, pointer.text)
		lineLength := textWidth + 4.0
		xEnd := x2 - lineLength
		if pointer.right {
			xEnd = x2 + lineLength
		}
		// The label just above the line
		x := xEnd + 2.0
		if pointer.right {
			x = x2 + 2.0
		}
		baseline := yEnd - float32(dc.f1.GetAscent(dc.f1.size)/3.0)
		if page != nil {
			page.LineTo(xEnd, yEnd)
			page.StrokePath()
			page.drawString(dc.f1, dc.f1.size, pointer.text, x, baseline, colorToRGB(simpleBarInk), nil)
		}
		return [4]float32{
			min(p1[0], x2, xEnd, x),
			min(p1[1], yEnd, baseline-dc.f1.GetAscent(dc.f1.size)),
			max(p1[0], x2, xEnd, x+textWidth),
			max(p1[1], yEnd, baseline+dc.f1.GetDescent(dc.f1.size))}
	}
	// No text: a short line along
	xEnd := x2 - 20.0
	if pointer.right {
		xEnd = x2 + 20.0
	}
	if page != nil {
		page.LineTo(xEnd, yEnd)
		page.StrokePath()
	}
	return [4]float32{min(p1[0], x2, xEnd), min(p1[1], yEnd), max(p1[0], x2, xEnd), max(p1[1], yEnd)}
}

// DrawOn draws this chart on the specified page, or measures it when the page
// is nil. It returns the x and y coordinates of the bottom right corner of the
// chart: of its title, its circle, and its leader lines and labels, which can
// reach past the circle, or its legend. A chart with no slice above 0 draws
// nothing, not its title either, and returns the corner of its circle.
func (dc *DonutChart) DrawOn(page *Page) [2]float32 {
	// The title and the subtitle above the circle, when there are any
	titleFont := dc.f2
	if titleFont == nil {
		titleFont = dc.f1
	}
	titleSize := float32(0.0)
	if dc.f1 != nil {
		titleSize = dc.f1.size
	} else if titleFont != nil {
		titleSize = titleFont.size
	}
	top := dc.y
	titleBaseline := float32(0.0)
	subtitleBaseline := float32(0.0)
	if titleFont != nil && dc.title != "" {
		titleBaseline = top + titleFont.GetAscent(titleSize)
		top = titleBaseline + titleFont.GetDescent(titleSize)
	}
	if dc.f1 != nil && dc.subtitle != "" {
		subtitleBaseline = top + dc.f1.GetAscent(dc.f1.size)
		top = subtitleBaseline + dc.f1.GetDescent(dc.f1.size)
	}
	if top > dc.y {
		// Room for a label at the top of the circle: its leader line goes 15
		// points up, the label sits above its end; and half a line
		var label float32
		if dc.f1 != nil && !dc.legend {
			ascent := dc.f1.GetAscent(dc.f1.size)
			label = float32(ascent/3) + ascent
		}
		leader := float32(15)
		if dc.legend {
			leader = 0
		}
		top += leader + label + float32(titleSize/2)
	}
	xc := dc.x + dc.r1 // the center of the chart
	yc := top + dc.r1

	// The slices with a value above 0 share the circle
	values := dc.values()
	total := float32(0.0)
	for _, value := range values {
		total += value
	}
	if total <= 0.0 { // Nothing to draw
		return [2]float32{xc + dc.r1, yc + dc.r1}
	}
	percentages := slicePercentages(values, total)

	// The leader lines and the labels, moved apart, and the box of the
	// figure: the title, the outer circle, the leader lines and the labels
	pointers := make([]*donutPointer, 0, len(dc.slices))
	colors := make([]int32, len(dc.slices))
	angle := float32(0.0)
	count := 0
	for i, slice := range dc.slices {
		if values[i] > 0.0 {
			sweep := float32(values[i]*360.0) / total
			if !dc.legend {
				pointers = append(pointers, dc.pointerOf(slice.text, xc, yc, dc.r1, angle, angle+sweep, len(pointers)))
			}
			angle += sweep
			colors[i] = slice.color
			if colors[i] == slicePalette {
				colors[i] = barChartPalette[count%len(barChartPalette)]
			}
			count++
		}
	}
	// The last slice, next to the first around the circle, in the palette's
	// next color when it would have the first's: the eleventh of eleven was
	// blue by the blue first
	first := -1
	last := -1
	for i := range dc.slices {
		if values[i] > 0.0 {
			if first < 0 {
				first = i
			}
			last = i
		}
	}
	if count > 1 && dc.slices[last].color == slicePalette && colors[last] == colors[first] {
		colors[last] = barChartPalette[count%len(barChartPalette)]
	}
	dc.moveApart(pointers, xc, yc)
	box := [4]float32{dc.x, dc.y, dc.x + float32(2*dc.r1), yc + dc.r1}
	if titleBaseline > 0.0 {
		box[2] = max(box[2], dc.x+titleFont.StringWidth(titleSize, dc.title))
	}
	if subtitleBaseline > 0.0 {
		box[2] = max(box[2], dc.x+dc.f1.StringWidth(dc.f1.size, dc.subtitle))
	}
	for _, pointer := range pointers {
		box = donutUnion(box, dc.drawPointer(nil, pointer))
	}
	if dc.legend {
		box = donutUnion(box, dc.drawLegend(nil, values, percentages, colors, yc+dc.r1))
	}
	if page == nil { // Measured
		return [2]float32{box[2], box[3]}
	}

	// The chart is one figure, described by its alternate description.
	page.AddBDC(structelem.Figure, "", "", dc.getAltDescription(values, percentages))
	// The pen, the brush and the dash pattern of the caller are kept, as a
	// Stamp keeps them: the chart left the page with the black pen of its
	// pointers and the color of its last slice.
	page.SaveGraphicsState()
	page.SetDefaultStrokeDashPattern() // Not the caller's, which dashed the leader lines
	if titleBaseline > 0.0 {
		page.drawString(titleFont, titleSize, dc.title, dc.x, titleBaseline, colorToRGB(simpleBarInk), nil)
	}
	if subtitleBaseline > 0.0 {
		page.drawString(dc.f1, dc.f1.size, dc.subtitle, dc.x, subtitleBaseline, colorToRGB(simpleBarMuted), nil)
	}
	angle = 0.0
	drawn := 0
	edges := make([]float32, 0, len(dc.slices))
	sweeps := make([]float32, 0, len(dc.slices))
	for i := range dc.slices {
		if values[i] <= 0.0 {
			continue
		}
		sliceColor := colors[i]
		sweep := float32(values[i]*360.0) / total
		angle = dc.drawSlice(
			page, sliceColor,
			xc, yc,
			dc.r1, dc.r2,
			angle, angle+sweep,
		)
		edges = append(edges, angle)
		sweeps = append(sweeps, sweep)
		if !dc.legend {
			dc.drawPointer(page, pointers[drawn])
		}
		drawn++

		// The percentage fits inside a slice of 15 degrees or more, as long
		// across the middle of the ring as it is wide, and in a ring as wide
		// as one and a half lines of it, or a pie. A small pie's slices had
		// percentages wider than they were (the review of 28 October 2026)
		pctStr := fmt.Sprintf("%d%%", percentages[i])
		midR := float32((dc.r1 + dc.r2) / 2.0)
		if dc.f2 != nil && sweep >= 15.0 && (dc.r2 == 0.0 || dc.r1-dc.r2 >= float32(1.5*dc.f2.bodyHeight)) &&
			dc.f2.StringWidth(dc.f2.size, pctStr) <= float32(midR*float32(float64(float64(sweep)*math.Pi)/180.0)) {
			midAngle := angle - float32(sweep/2.0) - 90.0
			pos := getPoint(xc, yc, midR, midAngle)
			page.drawString(dc.f2, dc.f2.size, pctStr,
				pos[0]-float32(dc.f2.StringWidth(dc.f2.size, pctStr)/2.0),
				pos[1]+float32(dc.f2.GetAscent(dc.f2.size)/3.0),
				colorToRGB(labelColorOn(sliceColor)), nil)
		}
	}
	// A thin white line between the slices, from the hole to the edge, at
	// most a quarter as wide as the thinner of the two slices is across the
	// middle of the ring, so that it does not cover a thin slice
	if drawn > 1 {
		page.SetPenColor(color.White)
		midR := float32((dc.r1 + dc.r2) / 2.0)
		for k := 0; k < drawn; k++ {
			thinner := min(sweeps[k], sweeps[(k+1)%drawn])
			across := float32(midR * float32(float64(float64(thinner)*math.Pi)/180.0))
			page.SetPenWidth(min(1.5, float32(0.25*across)))
			edge := edges[k]
			inner := getPoint(xc, yc, dc.r2, edge-90.0)
			outer := getPoint(xc, yc, dc.r1, edge-90.0)
			page.DrawLine(inner[0], inner[1], outer[0], outer[1])
		}
	}
	if dc.legend {
		dc.drawLegend(page, values, percentages, colors, yc+dc.r1)
	}
	// The text in the hole, as large as one and a half labels or as fits
	if dc.r2 > 0.0 && titleFont != nil && dc.centerText != "" {
		// At three quarters of a label at least, shortened to fit there: a
		// long text was drawn as small as it took, too small to read
		size := float32(1.5 * titleSize)
		text := dc.centerText
		width := titleFont.StringWidth(size, text)
		if width > float32(1.6*dc.r2) {
			size = max(float32(size*float32(1.6*dc.r2))/width, float32(0.75*titleSize))
			text = shorten(titleFont, size, text, float32(1.6*dc.r2))
			width = titleFont.StringWidth(size, text)
		}
		baseline := yc + float32((titleFont.GetAscent(size)-titleFont.GetDescent(size))/2.0)
		page.drawString(titleFont, size, text, xc-float32(width/2.0), baseline, colorToRGB(simpleBarInk), nil)
	}
	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(box[0], box[1], box[2]-box[0], box[3]-box[1])
	page.AddEMC()

	return [2]float32{box[2], box[3]}
}

// drawLegend returns the box of the legend under the circle, whose bottom is
// at the y: a swatch, the label and the percentage of each slice, in rows as
// wide as the circle; and draws it when there is a page.
func (dc *DonutChart) drawLegend(page *Page, values []float32, percentages []int, colors []int32, y float32) [4]float32 {
	box := [4]float32{dc.x, y, dc.x, y}
	f1 := dc.f1
	if f1 == nil {
		return box
	}
	swatch := f1.ascent
	gap := float32(swatch / 2.0)
	left := dc.x
	baseline := y + float32(1.5*f1.bodyHeight)
	for i, slice := range dc.slices {
		if values[i] <= 0.0 {
			continue
		}
		// A label too long for a row is shortened, its percentage kept
		percentage := fmt.Sprintf("%d%%", percentages[i])
		text := percentage
		if slice.text != "" {
			room := float32(2.0*dc.r1) - swatch - gap - f1.StringWidth(f1.size, " "+percentage)
			text = shorten(f1, f1.size, slice.text, room) + " " + percentage
		}
		width := f1.StringWidth(f1.size, text)
		if left > dc.x && left+swatch+gap+width > dc.x+float32(2.0*dc.r1) {
			left = dc.x
			baseline += float32(1.5 * f1.bodyHeight)
		}
		if page != nil {
			page.SetBrushColor(colors[i])
			page.FillRect(left, baseline-swatch, swatch, swatch)
			page.drawString(f1, f1.size, text, left+swatch+gap, baseline, colorToRGB(simpleBarInk), nil)
		}
		box[2] = max(box[2], left+swatch+gap+width)
		box[3] = baseline + f1.descent
		left += swatch + gap + width + f1.bodyHeight
	}
	return box
}

// donutUnion returns the box that holds the two boxes, each left, top, right,
// bottom.
func donutUnion(a, b [4]float32) [4]float32 {
	return [4]float32{min(a[0], b[0]), min(a[1], b[1]), max(a[2], b[2]), max(a[3], b[3])}
}

// values returns the value of each slice that is drawn, and 0 for the others:
// a slice is drawn when its value is a number above 0. A NaN, from 0/0 in an
// empty category of real data, or an infinity made the arc's loop endless,
// until memory ran out (review of 11 October 2026, #2). Values whose 360ths
// or whose sum are past the largest float32 are divided by the largest of
// them, so that they are drawn as they compare.
func (dc *DonutChart) values() []float32 {
	values := make([]float32, len(dc.slices))
	largest := float32(0.0)
	total := float32(0.0)
	for i, slice := range dc.slices {
		value := slice.value
		if value > 0.0 && isValue(value) {
			values[i] = value
			largest = max(largest, value)
			total += value
		}
	}
	if largest > 0.0 && (!isValue(float32(largest*360.0)) || !isValue(total)) {
		for i := range values {
			values[i] = values[i] / largest
		}
	}
	return values
}

// slicePercentages returns the whole percentage of each slice that is drawn,
// by the largest remainder, so that they add up to 100: each rounded alone,
// 90 and six of 1.5 were 91 and six of 2.
func slicePercentages(values []float32, total float32) []int {
	percentages := make([]int, len(values))
	remainders := make([]float64, len(values))
	sum := 0
	for i := range values {
		remainders[i] = -1.0
		if values[i] > 0.0 {
			exact := float64(float64(values[i])*100.0) / float64(total)
			percentages[i] = int(math.Floor(exact))
			remainders[i] = exact - float64(percentages[i])
			sum += percentages[i]
		}
	}
	// The points left over to the largest remainders, the first of equal ones
	for ; sum < 100; sum++ {
		largest := -1
		for i := range values {
			if remainders[i] >= 0.0 && (largest < 0 || remainders[i] > remainders[largest]) {
				largest = i
			}
		}
		if largest < 0 {
			break
		}
		percentages[largest]++
		remainders[largest] = -1.0
	}
	return percentages
}

// getAltDescription returns the alternate description, or the title, the
// subtitle, and the label and the percentage of each slice when none is set.
func (dc *DonutChart) getAltDescription(values []float32, percentages []int) string {
	if dc.altDescription != "" {
		return dc.altDescription
	}
	name := join(dc.title, dc.subtitle)
	if dc.r2 > 0.0 && (dc.f1 != nil || dc.f2 != nil) {
		name = join(name, dc.centerText) // Drawn in the hole
	}
	if name == "" {
		if dc.r2 > 0.0 {
			name = "Donut chart"
		} else {
			name = "Pie chart"
		}
	}
	var sb strings.Builder
	sb.WriteString(name)
	sb.WriteString(":")
	separator := " "
	for i, slice := range dc.slices {
		if values[i] > 0.0 {
			sb.WriteString(separator)
			if slice.text != "" {
				sb.WriteString(slice.text)
				sb.WriteString(" ")
			}
			sb.WriteString(fmt.Sprintf("%d%%", percentages[i]))
			separator = ", "
		}
	}
	return sb.String()
}
