// barchart.go
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
	"math"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// barSeries is one series of a bar chart: a name, a value per category and a
// color, or a color per category.
type barSeries struct {
	name   string
	values []float32
	color  int32
	colors []int32 // nil unless each bar has its own color
}

// noColor marks a series drawn in the next color of the default palette.
const noColor int32 = -1

// The look of the simple charts: ink for the title and the values, a muted gray
// for the rest of the text, light grid lines, and the colors of the series from
// the Tableau 10 palette, which D3 draws its charts with: ten colors chosen to
// stay apart from one another.
const (
	barChartInk      = simpleBarInk
	barChartMuted    = simpleBarMuted
	barChartGrid     = 0xE1E4E8
	barChartBaseline = 0x8C96A3
)

var barChartPalette = []int32{
	0x4E79A7, // blue
	0xF28E2C, // orange
	0xE15759, // red
	0x76B7B2, // teal
	0x59A14F, // green
	0xEDC949, // yellow
	0xAF7AA1, // purple
	0xFF9DA7, // pink
	0x9C755F, // brown
	0xBAB0AB, // gray
}

// BarChart is a bar chart renderer for PDF pages: one slot per category, the
// bars of the series grouped inside each slot. A value of a line series outside
// a range that is set cuts its line at the edge of the plot, and the chart says
// how many are: "2 values outside the range", small, under the plot at its
// right end, and in its description, in that alone when the plot would not be
// left room to draw. See Example_27, of horizontal and vertical bars.
type BarChart struct {
	x1, y1 float32
	w, h   float32

	title    string
	subtitle string
	// The alternate description of the chart, the title when it is empty
	altDescription string
	xAxisTitle     string
	yAxisTitle     string

	categories []string
	series     []*barSeries
	lineSeries []*barSeries // Drawn as lines over the bars

	horizontal bool
	stacked    bool
	groupGap   float32
	barGap     float32

	drawGridLines     bool
	drawValueLabels   bool
	valueLabelsInside bool
	drawLegend        bool
	groupingUsed      bool

	gridLineColor int32

	minFractionDigits int
	maxFractionDigits int
	fractionDigitsSet bool

	// The range of the value axis, from the data unless it is set
	min      float32
	max      float32
	rangeSet bool

	// f1 = chart title font, f2 = axis titles, labels and legend font
	f1 *Font
	f2 *Font
}

// NewBarChart creates a bar chart.
//   - f1: the font for the chart title.
//   - f2: the font for the axis titles, the labels and the legend.
func NewBarChart(f1, f2 *Font) *BarChart {
	chart := new(BarChart)
	chart.f1 = f1
	chart.f2 = f2
	chart.w = 300.0
	chart.h = 200.0
	chart.categories = make([]string, 0)
	chart.series = make([]*barSeries, 0)
	chart.lineSeries = make([]*barSeries, 0)
	chart.groupGap = 0.3
	chart.barGap = 0.0
	chart.drawGridLines = true
	chart.drawValueLabels = false
	chart.drawLegend = true
	chart.groupingUsed = true
	chart.gridLineColor = barChartGrid
	chart.minFractionDigits = 0
	chart.maxFractionDigits = 2
	return chart
}

// SetTitle sets the chart title.
func (chart *BarChart) SetTitle(title string) *BarChart {
	chart.title = title
	return chart
}

// SetSubtitle sets the subtitle, written in gray under the title in the second font.
func (chart *BarChart) SetSubtitle(subtitle string) *BarChart {
	chart.subtitle = subtitle
	return chart
}

// SetAltDescription sets the alternate description of the chart, which a
// screen reader reads in a PDF/UA document, where the chart is a figure. The
// default is the title and the subtitle, or "Bar chart" without them, and the
// values of each series by category. Describe what the chart shows. The number
// of values of the line series outside a range that is set follows it, as the
// chart prints it.
func (chart *BarChart) SetAltDescription(altDescription string) *BarChart {
	chart.altDescription = altDescription
	return chart
}

// SetXAxisTitle sets the X axis title.
func (chart *BarChart) SetXAxisTitle(title string) *BarChart {
	chart.xAxisTitle = title
	return chart
}

// SetYAxisTitle sets the Y axis title.
func (chart *BarChart) SetYAxisTitle(title string) *BarChart {
	chart.yAxisTitle = title
	return chart
}

// SetCategories sets the categories, one per group of bars, in the order they
// are drawn: left to right in a vertical chart, top to bottom in a horizontal one.
func (chart *BarChart) SetCategories(categories ...string) *BarChart {
	chart.categories = make([]string, 0, len(categories))
	chart.categories = append(chart.categories, categories...)
	return chart
}

// AddSeries adds a series drawn in the next color of the default palette.
// A value that is NaN or infinite has no bar.
//   - name: the series name, shown in the legend; empty for none.
//   - values: one value per category.
func (chart *BarChart) AddSeries(name string, values []float32) *BarChart {
	return chart.AddSeriesWithColor(name, values, noColor)
}

// AddSeriesWithColor adds a series drawn in the specified color.
//   - name: the series name, shown in the legend; empty for none.
//   - values: one value per category.
//   - color: the bar color as a 0xRRGGBB value, for example color.Blue.
func (chart *BarChart) AddSeriesWithColor(name string, values []float32, color int32) *BarChart {
	copied := make([]float32, len(values))
	copy(copied, values)
	chart.series = append(chart.series, &barSeries{name: name, values: copied, color: color})
	return chart
}

// AddSeriesWithColors adds a series with a color per category, for a chart
// whose bars each have their own color. A bar past the end of the colors has
// the next color of the default palette.
//   - name: the series name, shown in the legend; empty for none.
//   - values: one value per category.
//   - colors: one 0xRRGGBB color per category.
func (chart *BarChart) AddSeriesWithColors(name string, values []float32, colors []int32) *BarChart {
	copied := make([]float32, len(values))
	copy(copied, values)
	copiedColors := make([]int32, len(colors))
	copy(copiedColors, colors)
	chart.series = append(chart.series, &barSeries{name: name, values: copied, color: noColor, colors: copiedColors})
	return chart
}

// AddLineSeries adds a series drawn as a line over the bars, in ink, a dot at
// each category: a target, the average, or last year. It shares the value axis
// with the bars, is not stacked, and is in the legend and the description. A
// value that is NaN or infinite leaves a gap in the line, and the chart says
// how many values are outside a range set with SetValueAxisRange.
//   - name: the series name, shown in the legend; empty for none.
//   - values: one value per category.
func (chart *BarChart) AddLineSeries(name string, values []float32) *BarChart {
	return chart.AddLineSeriesWithColor(name, values, barChartInk)
}

// AddLineSeriesWithColor adds a series drawn as a line over the bars, in the
// specified color.
//   - name: the series name, shown in the legend; empty for none.
//   - values: one value per category.
//   - color: the line color as a 0xRRGGBB value, for example color.Blue.
func (chart *BarChart) AddLineSeriesWithColor(name string, values []float32, color int32) *BarChart {
	copied := make([]float32, len(values))
	copy(copied, values)
	chart.lineSeries = append(chart.lineSeries, &barSeries{name: name, values: copied, color: color})
	return chart
}

// SetLocation sets the location of the top left corner of this chart.
// It returns the chart as a Drawable, so in a chain of setter calls
// SetLocation goes last, right before DrawOn.
func (chart *BarChart) SetLocation(x, y float32) Drawable {
	chart.x1 = x
	chart.y1 = y
	return chart
}

// SetSize sets the size of this chart.
func (chart *BarChart) SetSize(w, h float32) *BarChart {
	chart.w = w
	chart.h = h
	return chart
}

// SetHorizontal sets whether the bars are horizontal. The default is vertical bars.
func (chart *BarChart) SetHorizontal(horizontal bool) *BarChart {
	chart.horizontal = horizontal
	return chart
}

// SetStacked sets whether the series are stacked: one bar per category, with
// the value of each series as a segment of it. The values above 0 stack up
// from 0 and the values below 0 stack down. The default is grouped bars.
func (chart *BarChart) SetStacked(stacked bool) *BarChart {
	chart.stacked = stacked
	return chart
}

// SetGroupGap sets the gap between the groups of bars as a fraction of the
// category slot, from 0.0 to 0.9. The default is 0.3.
func (chart *BarChart) SetGroupGap(gap float32) *BarChart {
	// From 0 to 0.9 of a slot: more left the bars no width, or less than none
	if isValue(gap) {
		chart.groupGap = min(max(gap, 0.0), 0.9)
	}
	return chart
}

// SetBarGap sets the gap between the bars of a group as a fraction of the bar
// width. The default is 0.0, so the bars of a group touch.
func (chart *BarChart) SetBarGap(gap float32) *BarChart {
	// 0 or more: a negative gap made bars of no width, and -1 divided by 0
	if isValue(gap) {
		chart.barGap = max(gap, 0.0)
	}
	return chart
}

// SetGridLines sets whether the grid lines of the value axis are drawn.
func (chart *BarChart) SetGridLines(drawGridLines bool) *BarChart {
	chart.drawGridLines = drawGridLines
	return chart
}

// SetValueLabels sets whether the value of each bar is written at its end,
// or, in a stacked chart, inside each segment that has room for it.
func (chart *BarChart) SetValueLabels(drawValueLabels bool) *BarChart {
	chart.drawValueLabels = drawValueLabels
	return chart
}

// SetValueLabelsInside sets whether the value labels are written inside the
// bars, at the end of each bar, in white or in ink, whichever reads on the
// bar's color, instead of next to the bar ends. A bar too short for its label
// gets it next to its end. The default is false.
func (chart *BarChart) SetValueLabelsInside(valueLabelsInside bool) *BarChart {
	chart.valueLabelsInside = valueLabelsInside
	return chart
}

// SetGroupingUsed sets whether the labels group the digits in thousands with
// a comma, as in 6,650. The default is true.
func (chart *BarChart) SetGroupingUsed(groupingUsed bool) *BarChart {
	chart.groupingUsed = groupingUsed
	return chart
}

// SetLegend sets whether the legend is drawn. The legend lists the series
// that have a name, under the title.
func (chart *BarChart) SetLegend(drawLegend bool) *BarChart {
	chart.drawLegend = drawLegend
	return chart
}

// SetGridLineColor sets the color of the grid lines. The default is light gray,
// 0xE1E4E8.
//   - color: the color as a 0xRRGGBB value, for example color.LightGray.
//
// color.Transparent leaves it unchanged.
func (chart *BarChart) SetGridLineColor(c int32) *BarChart {
	if c == color.Transparent {
		return chart
	}
	chart.gridLineColor = c
	return chart
}

// SetFractionDigits sets the number of digits after the decimal point of the
// values written by the bars, all of them alike, as SimpleBarChart's. Without
// it a value has as many as it needs, two at most, or as many as the step of
// the value axis has. The numbers of the value axis have the digits of its
// step.
func (chart *BarChart) SetFractionDigits(digits int) *BarChart {
	chart.minFractionDigits = digits
	chart.maxFractionDigits = digits
	chart.fractionDigitsSet = true
	return chart
}

// SetValueAxisRange sets the range of the value axis, with grid lines on a
// round step, as SetYAxisRange does in SimpleLineChart and SimpleScatterChart.
// Without it the range is that of the data, rounded to steps, and always
// includes 0. A range whose end is not past its start is not set. A value of a
// line series outside the range is drawn past the plot, which clips it, and the
// chart says how many are, under the plot, which is that much shorter, and in
// its description.
//   - min: the value at the start of the axis.
//   - max: the value at the end of the axis.
func (chart *BarChart) SetValueAxisRange(min, max float32) *BarChart {
	chart.min = min
	chart.max = max
	chart.rangeSet = max > min
	return chart
}

// DrawOn draws this chart on the specified page and returns the bottom right
// corner coordinates. A category name wider than two fifths of a horizontal
// chart is shortened with an ellipsis, and a chart too small for its plot
// draws nothing.
func (chart *BarChart) DrawOn(page *Page) [2]float32 {
	n := chart.numberOfCategories()
	if page == nil || n == 0 || chart.f1 == nil || chart.f2 == nil { // Measured, or nothing to draw
		return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
	}
	if chart.maxFractionDigits > maxChartDigits {
		page.pdf.fail(fractionDigitsMessage)
		return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
	}
	f1 := chart.f1
	f2 := chart.f2

	var vMin, vMax float32
	var lines int
	if chart.rangeSet {
		vMin = chart.min
		vMax = chart.max
		lines = roundLines(chart.min, chart.max,
			max(roundMaxAndMinValues(chart.max, chart.min).numOfGridLines, 1))
	} else {
		lo := float32(0.0)
		hi := float32(0.0)
		if chart.stacked {
			// The sums of the values above and below 0 in each category
			for i := 0; i < n; i++ {
				up := float32(0.0)
				down := float32(0.0)
				for _, s := range chart.series {
					if i < len(s.values) && isValue(s.values[i]) {
						if s.values[i] >= 0.0 {
							up += s.values[i]
						} else {
							down += s.values[i]
						}
					}
				}
				lo = min(lo, down)
				hi = max(hi, up)
			}
			// The values of a line series up to the last category, which
			// are all it draws
			for _, s := range chart.lineSeries {
				for i := 0; i < min(len(s.values), n); i++ {
					if isValue(s.values[i]) {
						lo = min(lo, s.values[i])
						hi = max(hi, s.values[i])
					}
				}
			}
		} else {
			for _, s := range chart.allSeries() {
				for i := 0; i < min(len(s.values), n); i++ {
					if isValue(s.values[i]) {
						lo = min(lo, s.values[i])
						hi = max(hi, s.values[i])
					}
				}
			}
		}
		if !isValue(hi - lo) {
			// Values, or sums of a stack, whose span is past the largest
			// float32, which no axis can draw: they were drawn on 0 to 1
			return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
		}
		if hi == lo {
			lo, hi = flatRange(lo)
		}
		round := roundMaxAndMinValues(hi, lo)
		vMin = round.minValue
		vMax = round.maxValue
		lines = round.numOfGridLines
	}
	if vMax == vMin {
		vMin, vMax = flatRange(vMin)
	}
	step := (vMax - vMin) / float32(lines)
	// The digits of the step, which SetFractionDigits does not cap: with 0
	// an axis of 0.25 was written 0, 0, 1, 1
	axisDigits := max(chart.minFractionDigits, stepDigits(step))
	// The significant digits of the numbers of the axis written with a power
	// of ten, as many as write them apart
	significant := axisSignificant(vMin, step, lines, axisDigits)
	// The digits of the values: those set, or as many as the step has, two
	// at least, as the simple charts write them. Values under 0.01 were
	// written 0 (the review of 28 October 2026)
	digits := chart.maxFractionDigits
	if !chart.fractionDigitsSet {
		digits = max(digits, stepDigits(step))
	}
	base := min(max(0.0, vMin), vMax)

	x2 := chart.x1 + chart.w
	y2 := chart.y1 + chart.h
	bodyHeight := f2.bodyHeight
	ascent := f2.ascent
	pad := float32(bodyHeight / 2.0)
	legend := chart.drawLegend && chart.hasSeriesNames()

	// Widest labels on the value axis and next to the bars
	widestAxisLabel := float32(0.0)
	for i := 0; i <= lines; i++ {
		label := chart.axisLabel(vMin+float32(step*float32(i)), axisDigits, significant)
		widestAxisLabel = max(widestAxisLabel, f2.StringWidth(f2.size, label))
	}
	widestValueLabel := float32(0.0)
	widestNegative := float32(0.0) // Of the bars below 0, written past their ends
	// Of the labels that are written: not those of the bars outside a range
	// that is set, which made room for labels that were not there
	for i := 0; chart.drawValueLabels && i < n; i++ {
		up := float32(0.0)
		down := float32(0.0)
		for _, s := range chart.series {
			if i >= len(s.values) || !isValue(s.values[i]) {
				continue
			}
			v := s.values[i]
			from := base
			if chart.stacked {
				if v >= 0.0 {
					from = up
					up += v
				} else {
					from = down
					down += v
				}
			}
			to := v
			if chart.stacked {
				to = from + v
			}
			if outsideAxis(from, to, vMin, vMax) {
				continue
			}
			width := f2.StringWidth(f2.size, chart.valueLabel(v, digits))
			widestValueLabel = max(widestValueLabel, width)
			if v < 0.0 {
				widestNegative = max(widestNegative, width)
			}
		}
	}
	// The totals of the stacks, written past their ends
	totals := chart.drawValueLabels && chart.stacked && len(chart.series) > 1
	widestTotal := float32(0.0)
	for i := 0; totals && i < n; i++ {
		up := float32(0.0)
		for _, s := range chart.series {
			if i < len(s.values) && isValue(s.values[i]) && s.values[i] >= 0.0 {
				up += s.values[i]
			}
		}
		// A sum past the largest float32, or below a range that is set, is
		// not written
		if up > 0.0 && isValue(up) && up >= vMin {
			widestTotal = max(widestTotal, f2.StringWidth(f2.size, chart.valueLabel(up, digits)))
		}
	}
	// The names of the categories, shortened at the left of horizontal bars
	names := make([]string, 0, len(chart.categories))
	widestCategory := float32(0.0)
	for _, category := range chart.categories {
		// Two fifths of a horizontal chart at most, a quarter under vertical bars
		fraction := float32(0.25)
		if chart.horizontal {
			fraction = 0.4
		}
		name := shorten(f2, f2.size, category, float32(fraction*chart.w))
		names = append(names, name)
		widestCategory = max(widestCategory, f2.StringWidth(f2.size, name))
	}

	// Margins and the plot area
	// The title, the subtitle and the legend, its rows wrapped at the right
	// edge; no room for a title the chart has not
	titleBaseline := chart.y1
	if chart.title != "" {
		titleBaseline = chart.y1 + float32(1.5*f1.bodyHeight)
	}
	subtitleHeight := float32(0.0)
	if chart.subtitle != "" {
		subtitleHeight = bodyHeight
	}
	legendRows := 0
	if legend {
		legendRows = chart.drawLegendOn(nil, 0.0)
	}
	topMargin := f1.bodyHeight
	if chart.title != "" {
		topMargin = float32(2.5 * f1.bodyHeight)
	}
	topMargin = topMargin + subtitleHeight + float32(float32(legendRows)*float32(1.5*bodyHeight))
	// Room between the plot and the categories for the label of a bar
	// below 0 that reaches the end of the axis: it was written over the
	// category's name
	negative := float32(0.0)
	if chart.drawValueLabels && !chart.stacked && widestNegative > 0.0 {
		if chart.horizontal {
			negative = widestNegative + float32(pad/2.0)
		} else {
			negative = bodyHeight
		}
	}
	var leftMargin float32
	if chart.horizontal {
		leftMargin = float32(1.5*bodyHeight) + pad + (widestCategory + negative)
	} else {
		leftMargin = float32(1.5*bodyHeight) + pad + widestAxisLabel
	}
	rightMargin := pad
	bottomMargin := float32(2.5 * bodyHeight)
	if !chart.horizontal {
		bottomMargin = float32(2.5*bodyHeight) + negative
	}
	if chart.horizontal {
		past := float32(0.0)
		if totals {
			past = widestTotal + pad
		} else if !chart.valueLabelsInside {
			past = widestValueLabel + pad
		}
		rightMargin += max(float32(widestAxisLabel/2.0), past)
	} else if totals || (chart.drawValueLabels && !chart.stacked && !chart.valueLabelsInside) {
		topMargin += bodyHeight
	}
	x5 := chart.x1 + leftMargin
	y5 := chart.y1 + topMargin
	x6 := x2 - rightMargin
	y8 := y2 - bottomMargin
	// Room for the note, a row of its own under the rest; a chart that has
	// not the room keeps its plot, and its description says the number
	note := chart.outsideNote()
	small := float32(0.8 * f2.size)
	noteRow := float32(0.0)
	if note != "" {
		if above := y8 - float32(1.4*small); above > y5 {
			noteRow = float32(1.4 * small)
			y8 = above
		} else {
			note = ""
		}
	}
	// No room for the plot, or an axis whose span is past the largest
	// float, as a set range of -3e38 to 3e38, whose steps are infinite
	if x6 <= x5 || y8 <= y5 || !isValue(step) {
		return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
	}

	// The chart is one figure, described by its alternate description.
	page.AddBDC(structelem.Figure, "", "", chart.getAltDescription(digits))
	// The pen, the brush and the dash pattern of the caller are kept, as a
	// Stamp keeps them.
	page.SaveGraphicsState()
	page.SetDefaultStrokeDashPattern() // Not the caller's, which dashed the legend's lines

	// Title, the subtitle and then the legend under it, at the left
	// The title and the subtitle shortened to the width of the chart
	barChartText(page, f1, shorten(f1, f1.size, chart.title, chart.w), chart.x1, titleBaseline, barChartInk)
	if chart.subtitle != "" {
		barChartText(page, f2, shorten(f2, f2.size, chart.subtitle, chart.w),
			chart.x1, titleBaseline+subtitleHeight, barChartMuted)
	}
	if legend {
		chart.drawLegendOn(page, titleBaseline+subtitleHeight+float32(1.5*bodyHeight))
	}

	// Grid lines and value axis labels. A label is drawn at every grid line
	// that leaves it room, the widest label and a pad across, or a line high
	// up the vertical axis: at every second or third line, or more, when
	// the labels would run into each other, the lines all drawn.
	every := 1
	if chart.horizontal {
		every = axisLabelStep((x6-x5)/float32(lines), widestAxisLabel+pad, lines)
	} else {
		every = axisLabelStep((y8-y5)/float32(lines), bodyHeight, lines)
	}
	if chart.horizontal {
		// Inside the chart, and apart when the first and the last are moved in
		centers := make([]float32, lines+1)
		widths := make([]float32, lines+1)
		for i := 0; i <= lines; i++ {
			v := vMin + float32(step*float32(i))
			centers[i] = x5 + scaled(v-vMin, x6-x5, vMax-vMin)
			widths[i] = f2.StringWidth(f2.size, chart.axisLabel(v, axisDigits, significant))
		}
		every = placedStep(every, centers, widths, chart.x1, x2, float32(pad/2.0))
	}
	for i := 0; i <= lines; i++ {
		v := vMin + float32(step*float32(i))
		label := chart.axisLabel(v, axisDigits, significant)
		labeled := i%every == 0
		if chart.horizontal {
			x := x5 + scaled(v-vMin, x6-x5, vMax-vMin)
			if chart.drawGridLines {
				chart.gridLine(page, x, y5, x, y8)
			}
			if labeled {
				barChartText(page, f2, label, placed(x, f2.StringWidth(f2.size, label), chart.x1, x2),
					y8+bodyHeight, barChartMuted)
			}
		} else {
			y := y8 - scaled(v-vMin, y8-y5, vMax-vMin)
			if chart.drawGridLines {
				chart.gridLine(page, x5, y, x6, y)
			}
			if labeled {
				barChartText(page, f2, label, x5-pad-f2.StringWidth(f2.size, label), y+float32(ascent/2.0), barChartMuted)
			}
		}
	}

	// Bars, category labels and value labels
	m := len(chart.series)
	var slot float32
	if chart.horizontal {
		slot = (y8 - y5) / float32(n)
	} else {
		slot = (x6 - x5) / float32(n)
	}
	groupWidth := float32(slot * (1.0 - chart.groupGap))
	barWidth := groupWidth
	if m != 0 && !chart.stacked {
		barWidth = groupWidth / (float32(m) + float32(float32(m-1)*chart.barGap))
	}
	// A category is written where it leaves the one before it room: at every
	// second or third bar, or more. By horizontal bars every name was written,
	// and forty of them ran into each other (the review of 28 October 2026)
	everyCategory := axisLabelStep(slot, bodyHeight, n)
	if !chart.horizontal {
		everyCategory = axisLabelStep(slot, widestCategory+pad, n)
		// Inside the chart, and apart when the first and the last are moved in
		centers := make([]float32, n)
		widths := make([]float32, n)
		for i := 0; i < n; i++ {
			centers[i] = x5 + float32(float32(i)*slot) + float32(slot/2.0)
			if i < len(names) {
				widths[i] = f2.StringWidth(f2.size, names[i])
			}
		}
		everyCategory = placedStep(everyCategory, centers, widths, chart.x1, x2, float32(pad/2.0))
	}
	// The values past the ends of grouped bars and the totals of the stacks
	// are written, as the categories are, where they leave the one before
	// them room, the nearest bars apart: at every second or third bar, or
	// more. Over narrow bars they ran into each other (the review of 28
	// October 2026)
	apart := slot
	if m > 1 && !chart.stacked {
		barStep := float32(barWidth * (1.0 + chart.barGap))
		apart = min(barStep, slot-float32(float32(m-1)*barStep))
	}
	everyLabel := axisLabelStep(apart, bodyHeight, n*m)
	everyTotal := axisLabelStep(slot, bodyHeight, n)
	if !chart.horizontal {
		everyLabel = axisLabelStep(apart, widestValueLabel+pad, n*m)
		everyTotal = axisLabelStep(slot, widestTotal+pad, n)
	}
	// The values are written over all the bars: a label wider than its bar
	// was covered by the bar after it
	labels := make([]barChartLabel, 0)
	for i := 0; i < n; i++ {
		var slotStart float32
		if chart.horizontal {
			slotStart = y5 + float32(float32(i)*slot)
		} else {
			slotStart = x5 + float32(float32(i)*slot)
		}
		groupStart := slotStart + float32(float32(slot*chart.groupGap)/2.0)
		category := ""
		if i < len(names) {
			category = names[i]
		}
		if chart.horizontal && i%everyCategory == 0 {
			barChartText(page, f2, category, x5-pad-negative-f2.StringWidth(f2.size, category),
				slotStart+float32(slot/2.0)+float32(ascent/2.0), barChartMuted)
		} else if !chart.horizontal && i%everyCategory == 0 {
			// Centered under its slot, inside the chart: the last one went
			// past the chart's right edge
			barChartText(page, f2, category, placed(slotStart+float32(slot/2.0), f2.StringWidth(f2.size, category), chart.x1, x2),
				y8+bodyHeight+negative, barChartMuted)
		}
		up := float32(0.0)   // the stacked values above 0 so far
		down := float32(0.0) // the stacked values below 0 so far
		for j := 0; j < m; j++ {
			s := chart.series[j]
			if i >= len(s.values) || !isValue(s.values[i]) {
				continue
			}
			// A bar runs from the base to its value; a segment of a stack
			// from the sum of the segments before it to that sum plus its value
			from := base
			to := s.values[i]
			if chart.stacked {
				if to >= 0.0 {
					from = up
					up += to
				} else {
					from = down
					down += to
				}
				to = from + s.values[i]
			}
			// A bar wholly outside a range that is set has no length,
			// and its value is not written by it
			outside := outsideAxis(from, to, vMin, vMax)
			from = min(max(from, vMin), vMax)
			to = min(max(to, vMin), vMax)
			barStart := groupStart
			if !chart.stacked {
				barStart = groupStart + float32(float32(float32(j)*barWidth)*(1.0+chart.barGap))
			}
			barColor := chart.barColor(s, j, i)
			page.SetBrushColor(barColor)
			hasLabel := chart.drawValueLabels && !outside
			label := ""
			if hasLabel {
				label = chart.valueLabel(s.values[i], digits)
			}
			on := labelColorOn(barColor)
			if chart.horizontal {
				x0 := x5 + scaled(from-vMin, x6-x5, vMax-vMin)
				x := x5 + scaled(to-vMin, x6-x5, vMax-vMin)
				page.FillRect(min(x0, x), barStart, abs32(x-x0), barWidth)
				baseline := barStart + float32(barWidth/2.0) + float32(ascent/2.0)
				// A label inside a bar as thick as a line of it: a stacked
				// one is left out, and one past the end is written instead
				if hasLabel && chart.stacked {
					if abs32(x-x0) >= f2.StringWidth(f2.size, label)+pad && barWidth >= bodyHeight {
						labels = append(labels, barChartLabel{label,
							min(x0, x) + float32((abs32(x-x0)-f2.StringWidth(f2.size, label))/2.0), baseline, on})
					}
				} else if hasLabel && chart.valueLabelsInside &&
					abs32(x-x0) >= f2.StringWidth(f2.size, label)+pad && barWidth >= bodyHeight {
					lx := x + float32(pad/2.0)
					if to >= base {
						lx = x - float32(pad/2.0) - f2.StringWidth(f2.size, label)
					}
					labels = append(labels, barChartLabel{label, lx, baseline, on})
				} else if hasLabel && (i*m+j)%everyLabel == 0 {
					lx := x - float32(pad/2.0) - f2.StringWidth(f2.size, label)
					if to >= base {
						lx = x + float32(pad/2.0)
					}
					labels = append(labels, barChartLabel{label, lx, baseline, barChartInk})
				}
			} else {
				y0 := y8 - scaled(from-vMin, y8-y5, vMax-vMin)
				y := y8 - scaled(to-vMin, y8-y5, vMax-vMin)
				page.FillRect(barStart, min(y0, y), barWidth, abs32(y-y0))
				lx := barStart + float32((barWidth-f2.StringWidth(f2.size, label))/2.0)
				// A label inside a bar as wide as it: a stacked one is left
				// out, and one past the end is written instead. Labels wider
				// than their bars were written over the bars next to them
				// (the review of 28 October 2026)
				if hasLabel && chart.stacked {
					if abs32(y-y0) >= bodyHeight && barWidth >= f2.StringWidth(f2.size, label) {
						labels = append(labels, barChartLabel{label, lx, float32((y+y0)/2.0) + float32(ascent/2.0), on})
					}
				} else if hasLabel && chart.valueLabelsInside && abs32(y-y0) >= bodyHeight+pad &&
					barWidth >= f2.StringWidth(f2.size, label) {
					ly := y - float32(pad/2.0)
					if to >= base {
						ly = y + ascent + float32(pad/2.0)
					}
					labels = append(labels, barChartLabel{label, lx, ly, on})
				} else if hasLabel && (i*m+j)%everyLabel == 0 {
					ly := y + ascent + float32(pad/2.0)
					if to >= base {
						ly = y - float32(pad/2.0)
					}
					labels = append(labels, barChartLabel{label, lx, ly, barChartInk})
				}
			}
		}
		// The total of a stack above it, or past its end. A sum past the
		// largest float is not written, nor one below a range that is set,
		// which was written under the plot (the review of 28 October 2026)
		if totals && up > 0.0 && isValue(up) && up >= vMin && i%everyTotal == 0 {
			total := chart.valueLabel(up, digits)
			groupMiddle := groupStart + float32(groupWidth/2.0)
			if chart.horizontal {
				x := x5 + scaled(min(up, vMax)-vMin, x6-x5, vMax-vMin)
				labels = append(labels, barChartLabel{total, x + float32(pad/2.0), groupMiddle + float32(ascent/2.0), barChartInk})
			} else {
				y := y8 - scaled(min(up, vMax)-vMin, y8-y5, vMax-vMin)
				labels = append(labels, barChartLabel{total, groupMiddle - float32(f2.StringWidth(f2.size, total)/2.0),
					y - float32(pad/2.0), barChartInk})
			}
		}
	}
	for _, label := range labels {
		barChartText(page, f2, label.text, label.x, label.y, label.color)
	}

	// The axis line at the base of the bars, which is the zero line of
	// negative values
	page.SetPenColor(barChartBaseline)
	page.SetPenWidth(0.75)
	if chart.horizontal {
		x0 := x5 + scaled(base-vMin, x6-x5, vMax-vMin)
		page.DrawLine(x0, y5, x0, y8)
	} else {
		y0 := y8 - scaled(base-vMin, y8-y5, vMax-vMin)
		page.DrawLine(x5, y0, x6, y0)
	}

	// The lines over the bars, through the middles of the categories, a
	// dot at each value, broken where a value is missing; each on a white
	// line a little wider, so that it stays clear where it crosses a bar.
	// A value outside a range that is set is drawn past the plot, which
	// clips the lines at its edges and as far past them as a dot is wide,
	// as in SimpleLineChart: a target of 150 over a range of 0 to 100 had
	// its dot at 100
	axis := chartAxis{vMin, vMax, lines}
	outsideAxis := false
	for _, s := range chart.lineSeries {
		for i := 0; i < min(len(s.values), n); i++ {
			outsideAxis = outsideAxis || (isValue(s.values[i]) && !onAxis(s.values[i], axis))
		}
	}
	if outsideAxis {
		edge := float32(0.2*bodyHeight) + 1.75
		page.SaveGraphicsState()
		page.ClipRect(x5-edge, y5-edge, x6-x5+float32(2.0*edge), y8-y5+float32(2.0*edge))
	}
	for _, s := range chart.lineSeries {
		runs := make([][][2]float32, 0)
		var run [][2]float32
		for i := 0; i < n; i++ {
			if i >= len(s.values) || !isValue(s.values[i]) {
				run = nil
				continue
			}
			v := held(s.values[i], axis)
			start := x5
			if chart.horizontal {
				start = y5
			}
			middle := start + float32(float32(i)*slot) + float32(slot/2.0)
			var at float32
			if chart.horizontal {
				at = x5 + scaled(v-vMin, x6-x5, vMax-vMin)
			} else {
				at = y8 - scaled(v-vMin, y8-y5, vMax-vMin)
			}
			if run == nil {
				runs = append(runs, nil)
			}
			if chart.horizontal {
				run = append(run, [2]float32{at, middle})
			} else {
				run = append(run, [2]float32{middle, at})
			}
			runs[len(runs)-1] = run
		}
		page.SetLineJoinStyle(joinstyle.Round)
		dot := float32(0.2 * bodyHeight)
		for pass := 0; pass < 2; pass++ {
			c := s.color
			width := float32(2.0)
			radius := dot
			if pass == 0 {
				c = color.White
				width = 3.5
				radius = dot + 0.75
			}
			page.SetPenColor(c)
			page.SetPenWidth(width)
			for _, points := range runs {
				page.MoveTo(points[0][0], points[0][1])
				for k := 1; k < len(points); k++ {
					page.LineTo(points[k][0], points[k][1])
				}
				page.StrokePath()
			}
			page.SetBrushColor(c)
			for _, points := range runs {
				for _, point := range points {
					page.FillCircle(point[0], point[1], radius)
				}
			}
		}
	}
	if outsideAxis {
		page.RestoreGraphicsState()
	}

	// Axis titles
	page.SetTextRotation(-90)
	yTitle := shorten(f2, f2.size, chart.yAxisTitle, y8-y5)
	barChartText(page, f2, yTitle, chart.x1+bodyHeight,
		y8-float32(((y8-y5)-f2.StringWidth(f2.size, yTitle))/2.0), barChartMuted)
	page.SetTextRotation(0)
	xTitle := shorten(f2, f2.size, chart.xAxisTitle, x6-x5)
	barChartText(page, f2, xTitle, x5+float32(((x6-x5)-f2.StringWidth(f2.size, xTitle))/2.0),
		y2-float32(bodyHeight/2.0)-noteRow, barChartMuted)
	if note != "" {
		drawOutsideNote(page, f2, small, note, x6, chart.x1, x2, y2-float32(bodyHeight/2.0))
	}

	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(chart.x1, chart.y1, chart.w, chart.h)
	page.AddEMC()

	return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
}

// outsideNote returns what the chart says of the values of its line series
// outside a range that is set, or "" when there are none.
func (chart *BarChart) outsideNote() string {
	count := 0
	if chart.rangeSet {
		axis := chartAxis{min: chart.min, max: chart.max}
		n := chart.numberOfCategories()
		for _, s := range chart.lineSeries {
			for i := 0; i < min(len(s.values), n); i++ {
				if isValue(s.values[i]) && !onAxis(s.values[i], axis) {
					count++
				}
			}
		}
	}
	return outsideNote(count, "value")
}

// getAltDescription returns the alternate description: the one set, or the
// title, the subtitle and, for each series, its name and its values by
// category, with the digits of the labels, or, without SetFractionDigits, with
// two significant digits at least; and the values of the line series outside a
// range that is set.
func (chart *BarChart) getAltDescription(digits int) string {
	if chart.altDescription != "" {
		return withNote(chart.altDescription, chart.outsideNote())
	}
	var sb strings.Builder
	sb.WriteString(join(chart.title, chart.subtitle))
	if sb.Len() == 0 {
		sb.WriteString("Bar chart")
	}
	separator := ": "
	n := chart.numberOfCategories()
	for j, s := range chart.allSeries() {
		var values strings.Builder
		for i, v := range s.values {
			if i >= n {
				break
			}
			if !isValue(v) {
				continue
			}
			if values.Len() > 0 {
				values.WriteString(", ")
			}
			category := ""
			if i < len(chart.categories) {
				category = chart.categories[i]
			}
			if category != "" {
				values.WriteString(category)
				values.WriteString(" ")
			}
			values.WriteString(chart.valueLabel(v, chart.describedDigits(v, digits)))
		}
		if values.Len() == 0 {
			continue
		}
		sb.WriteString(separator)
		if j >= len(chart.series) { // A line series
			if s.name == "" {
				sb.WriteString("line: ")
			} else {
				sb.WriteString(s.name)
				sb.WriteString(", a line: ")
			}
		} else if s.name != "" {
			sb.WriteString(s.name)
			sb.WriteString(": ")
		}
		sb.WriteString(values.String())
		separator = "; "
	}
	if note := chart.outsideNote(); note != "" {
		sb.WriteString("; " + note)
	}
	sb.WriteString(".")
	return sb.String()
}

// outsideAxis tells if a bar from one value to another is wholly outside the
// axis, which gives it no length.
func outsideAxis(from, to, vMin, vMax float32) bool {
	return min(max(from, vMin), vMax) == min(max(to, vMin), vMax) && from != to
}

// numberOfCategories returns the number of category slots: the categories or the longest series.
func (chart *BarChart) numberOfCategories() int {
	n := len(chart.categories)
	for _, s := range chart.series {
		n = max(n, len(s.values))
	}
	// A line series is drawn over the categories of the bars, and adds none
	// of its own when there are bars: a longer target line added empty slots
	if n == 0 {
		for _, s := range chart.lineSeries {
			n = max(n, len(s.values))
		}
	}
	return n
}

// allSeries returns the series of the bars, then those of the lines.
func (chart *BarChart) allSeries() []*barSeries {
	all := make([]*barSeries, 0, len(chart.series)+len(chart.lineSeries))
	all = append(all, chart.series...)
	return append(all, chart.lineSeries...)
}

// hasSeriesNames returns true if a series has a name to list in the legend.
func (chart *BarChart) hasSeriesNames() bool {
	for _, s := range chart.allSeries() {
		if s.name != "" {
			return true
		}
	}
	return false
}

// barColor returns the color of the bar at the index: the series' own, its
// color for the bar, or the palette's.
func (chart *BarChart) barColor(s *barSeries, seriesIndex, index int) int32 {
	if s.colors != nil && index >= 0 && index < len(s.colors) {
		return s.colors[index]
	}
	if s.color == noColor {
		return barChartPalette[seriesIndex%len(barChartPalette)]
	}
	return s.color
}

// valueLabel formats the value written at the end of a bar, with the digits
// at most.
func (chart *BarChart) valueLabel(value float32, digits int) string {
	return chart.group(format(value, chart.minFractionDigits, digits))
}

// describedDigits returns the digits of the value in the description: those
// of the labels, or, without SetFractionDigits, more for a small value, two
// significant digits. A value of 0.00012 on an axis of 1,000 was described
// as 0 (the review of 28 October 2026).
func (chart *BarChart) describedDigits(value float32, digits int) int {
	if chart.fractionDigitsSet || value == 0.0 {
		return digits
	}
	return max(digits, 1-int(math.Floor(math.Log10(float64(abs32(value))))))
}

// axisLabel formats a label of the value axis, with the significant digits
// when it is written with a power of ten.
func (chart *BarChart) axisLabel(value float32, digits, significant int) string {
	return chart.group(formatSignificant(value, digits, digits, significant))
}

// group groups the digits before the decimal point in thousands with commas,
// if grouping is used.
func (chart *BarChart) group(label string) string {
	if !chart.groupingUsed {
		return label
	}
	return groupDigits(label)
}

// barChartLabel is a value written by a bar, at its place, in its color.
type barChartLabel struct {
	text  string
	x, y  float32
	color int32
}

// barChartText draws the text in the font at its size, in the color.
func barChartText(page *Page, font *Font, text string, x, y float32, c int32) {
	page.drawString(font, font.size, text, x, y, colorToRGB(c), nil)
}

// barChartDark is the label on a color that neither white nor ink contrasts
// with as WCAG asks, 4.5:1: the red, the purple and the brown of the palette.
const barChartDark int32 = 0x111111

// labelColorOn returns the color of a label on a bar of the color: white or
// ink, whichever contrasts more with it, by the WCAG contrast ratio, or a near
// black when that is less than 4.5:1.
func labelColorOn(c int32) int32 {
	// White contrasts more when (1 + 0.05) / (L + 0.05) is the larger of the
	// two ratios, (L + 0.05) / (ink + 0.05) the other
	l := luminance(c) + 0.05
	ink := luminance(barChartInk) + 0.05
	if float64(l*l) <= float64(1.05*ink) {
		if 1.05/l >= 4.5 {
			return color.White
		}
		return barChartDark
	}
	if l/ink >= 4.5 {
		return barChartInk
	}
	return barChartDark
}

// luminance returns the relative luminance of the color, as WCAG defines it.
func luminance(c int32) float64 {
	return float64(0.2126*linearComponent((c>>16)&0xFF)) +
		float64(0.7152*linearComponent((c>>8)&0xFF)) +
		float64(0.0722*linearComponent(c&0xFF))
}

// linearComponent returns the sRGB component, 0 to 255, as a linear value
// from 0 to 1.
func linearComponent(component int32) float64 {
	c := float64(component) / 255.0
	if c <= 0.04045 {
		return c / 12.92
	}
	return math.Pow((c+0.055)/1.055, 2.4)
}

// gridLine draws one grid line, thin and solid.
func (chart *BarChart) gridLine(page *Page, xa, ya, xb, yb float32) {
	page.SetPenColor(chart.gridLineColor)
	page.SetPenWidth(0.5)
	page.DrawLine(xa, ya, xb, yb)
}

// drawLegendOn draws the legend at the left under the title, with a swatch
// before each series name, from the baseline of its first row, its rows
// wrapped at the right edge of the chart, when there is a page; and returns
// the number of rows. A series with a color per bar has the color of its
// first bar.
func (chart *BarChart) drawLegendOn(page *Page, baseline float32) int {
	f2 := chart.f2
	swatch := f2.ascent
	gap := float32(swatch / 2.0)
	x := chart.x1
	rows := 1
	for j, s := range chart.allSeries() {
		if s.name == "" {
			continue
		}
		// A name too long for a row is shortened
		name := shorten(f2, f2.size, s.name, chart.w-swatch-gap)
		if x > chart.x1 && x+swatch+gap+f2.StringWidth(f2.size, name) > chart.x1+chart.w {
			x = chart.x1
			baseline += float32(1.5 * f2.bodyHeight)
			rows++
		}
		if page != nil && j >= len(chart.series) {
			// A line: its color across the swatch, a dot in the middle
			page.SetPenColor(s.color)
			page.SetPenWidth(1.5)
			page.DrawLine(x, baseline-float32(swatch/2.0), x+swatch, baseline-float32(swatch/2.0))
			page.SetBrushColor(s.color)
			page.FillCircle(x+float32(swatch/2.0), baseline-float32(swatch/2.0), float32(swatch/4.0))
		} else if page != nil {
			if len(s.colors) > 0 {
				page.SetBrushColor(s.colors[0])
			} else {
				page.SetBrushColor(chart.barColor(s, j, -1))
			}
			page.FillRect(x, baseline-swatch, swatch, swatch)
		}
		x += swatch + gap
		if page != nil {
			barChartText(page, f2, name, x, baseline, barChartInk)
		}
		x += f2.StringWidth(f2.size, name) + f2.bodyHeight
	}
	return rows
}
