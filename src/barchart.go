// barchart.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
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

// BarChart is a bar chart renderer for PDF pages: one slot per category, the
// bars of the series grouped inside each slot. See Example_39 (horizontal
// bars) and Example_40 (vertical bars).
type BarChart struct {
	x1, y1 float32
	w, h   float32

	title      string
	subtitle   string
	xAxisTitle string
	yAxisTitle string

	categories []string
	series     []*barSeries

	horizontal bool
	stacked    bool
	groupGap   float32
	barGap     float32

	drawGridLines     bool
	drawValueLabels   bool
	valueLabelsInside bool
	drawLegend        bool
	groupingUsed      bool

	gridLineColor       int32
	gridLineWidth       float32
	gridLineDashPattern string
	axisLineWidth       float32
	chartBorderWidth    float32
	innerBorderWidth    float32

	minFractionDigits int
	maxFractionDigits int

	// Value axis range; auto-computed from the data unless gridLines > 0
	min       float32
	max       float32
	gridLines int

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
	chart.groupGap = 0.3
	chart.barGap = 0.0
	chart.drawGridLines = true
	chart.drawValueLabels = false
	chart.drawLegend = true
	chart.gridLineColor = color.Black
	chart.gridLineWidth = 0.0
	chart.gridLineDashPattern = "[1 1] 0"
	chart.axisLineWidth = 0.5
	chart.chartBorderWidth = 0.0
	chart.innerBorderWidth = 0.0
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
// category slot, from 0.0 to below 1.0. The default is 0.3.
func (chart *BarChart) SetGroupGap(gap float32) *BarChart {
	chart.groupGap = gap
	return chart
}

// SetBarGap sets the gap between the bars of a group as a fraction of the bar
// width. The default is 0.0, so the bars of a group touch.
func (chart *BarChart) SetBarGap(gap float32) *BarChart {
	chart.barGap = gap
	return chart
}

// SetDrawGridLines sets whether the grid lines of the value axis are drawn.
func (chart *BarChart) SetDrawGridLines(drawGridLines bool) *BarChart {
	chart.drawGridLines = drawGridLines
	return chart
}

// SetDrawValueLabels sets whether the value of each bar is written at its end,
// or, in a stacked chart, inside each segment that has room for it.
func (chart *BarChart) SetDrawValueLabels(drawValueLabels bool) *BarChart {
	chart.drawValueLabels = drawValueLabels
	return chart
}

// SetValueLabelsInside sets whether the value labels are written inside the
// bars, in white at the end of each bar, instead of next to the bar ends. A
// bar too short for its label gets it next to its end. The default is false.
func (chart *BarChart) SetValueLabelsInside(valueLabelsInside bool) *BarChart {
	chart.valueLabelsInside = valueLabelsInside
	return chart
}

// SetGroupingUsed sets whether the labels group the digits in thousands with
// a comma, as in 6,650. The default is false.
func (chart *BarChart) SetGroupingUsed(groupingUsed bool) *BarChart {
	chart.groupingUsed = groupingUsed
	return chart
}

// SetDrawLegend sets whether the legend is drawn. The legend lists the series
// that have a name, under the title.
func (chart *BarChart) SetDrawLegend(drawLegend bool) *BarChart {
	chart.drawLegend = drawLegend
	return chart
}

// SetGridLineWidth sets the width of the grid lines. A width of 0 draws the
// thinnest line a viewer shows; SetDrawGridLines(false) hides them.
func (chart *BarChart) SetGridLineWidth(width float32) *BarChart {
	chart.gridLineWidth = width
	return chart
}

// SetGridLineColor sets the color of the grid lines. The default is black.
//   - color: the color as a 0xRRGGBB value, for example color.LightGray.
func (chart *BarChart) SetGridLineColor(color int32) *BarChart {
	chart.gridLineColor = color
	return chart
}

// SetGridLineDashPattern sets the dash pattern of the grid lines, for example "[1 1] 0".
func (chart *BarChart) SetGridLineDashPattern(pattern string) *BarChart {
	chart.gridLineDashPattern = pattern
	return chart
}

// SetAxisLineWidth sets the width of the axis lines. The default is 0.5; 0 hides them.
func (chart *BarChart) SetAxisLineWidth(width float32) *BarChart {
	chart.axisLineWidth = width
	return chart
}

// SetChartBorderWidth sets the width of the outer chart border. A width of 0,
// the default, hides it.
func (chart *BarChart) SetChartBorderWidth(width float32) *BarChart {
	chart.chartBorderWidth = width
	return chart
}

// SetInnerBorderWidth sets the width of the plot area border. A width of 0,
// the default, hides it.
func (chart *BarChart) SetInnerBorderWidth(width float32) *BarChart {
	chart.innerBorderWidth = width
	return chart
}

// SetMinimumFractionDigits sets the minimum number of decimal places in the
// value labels. The axis labels have at least the decimal places of the axis
// step. The default is 0.
func (chart *BarChart) SetMinimumFractionDigits(minFractionDigits int) *BarChart {
	chart.minFractionDigits = minFractionDigits
	return chart
}

// SetMaximumFractionDigits sets the maximum number of decimal places in the
// value labels. The default is 2.
func (chart *BarChart) SetMaximumFractionDigits(maxFractionDigits int) *BarChart {
	chart.maxFractionDigits = maxFractionDigits
	return chart
}

// SetValueAxisMinMax sets the range and the number of grid lines of the value
// axis. Without it the range is computed from the data and always includes 0.
//   - min: the value at the start of the axis.
//   - max: the value at the end of the axis.
//   - gridLines: the number of grid lines, at least 1.
func (chart *BarChart) SetValueAxisMinMax(min, max float32, gridLines int) *BarChart {
	chart.min = min
	chart.max = max
	chart.gridLines = gridLines
	return chart
}

// DrawOn draws this chart on the specified page and returns the bottom right corner coordinates.
func (chart *BarChart) DrawOn(page *Page) [2]float32 {
	n := chart.numberOfCategories()
	if n == 0 {
		return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
	}
	f1 := chart.f1
	f2 := chart.f2

	var vMin, vMax float32
	var lines int
	if chart.gridLines > 0 {
		vMin = chart.min
		vMax = chart.max
		lines = chart.gridLines
	} else {
		lo := float32(0.0)
		hi := float32(0.0)
		if chart.stacked {
			// The sums of the values above and below 0 in each category
			for i := 0; i < n; i++ {
				up := float32(0.0)
				down := float32(0.0)
				for _, s := range chart.series {
					if i < len(s.values) {
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
		} else {
			for _, s := range chart.series {
				for _, v := range s.values {
					lo = min(lo, v)
					hi = max(hi, v)
				}
			}
		}
		if hi == lo {
			hi = lo + 1.0
		}
		round := roundMaxAndMinValues(hi, lo)
		vMin = round.minValue
		vMax = round.maxValue
		lines = round.numOfGridLines
	}
	if vMax == vMin {
		vMax = vMin + 1.0
	}
	step := (vMax - vMin) / float32(lines)
	axisDigits := max(chart.minFractionDigits, fractionDigitsOf(step, chart.maxFractionDigits))
	base := min(max(0.0, vMin), vMax)

	x2 := chart.x1 + chart.w
	y2 := chart.y1 + chart.h
	bodyHeight := f2.bodyHeight
	ascent := f2.ascent
	pad := bodyHeight / 2.0
	legend := chart.drawLegend && chart.hasSeriesNames()

	// Widest labels on the value axis and next to the bars
	widestAxisLabel := float32(0.0)
	for i := 0; i <= lines; i++ {
		label := chart.axisLabel(vMin+step*float32(i), axisDigits)
		widestAxisLabel = max(widestAxisLabel, f2.StringWidth(f2.size, label))
	}
	widestValueLabel := float32(0.0)
	if chart.drawValueLabels {
		for _, s := range chart.series {
			for _, v := range s.values {
				widestValueLabel = max(widestValueLabel, f2.StringWidth(f2.size, chart.valueLabel(v)))
			}
		}
	}
	widestCategory := float32(0.0)
	for _, category := range chart.categories {
		widestCategory = max(widestCategory, f2.StringWidth(f2.size, category))
	}

	// Margins and the plot area
	titleBaseline := chart.y1 + 1.5*f1.bodyHeight
	subtitleHeight := float32(0.0)
	if chart.subtitle != "" {
		subtitleHeight = bodyHeight
	}
	topMargin := 2.5*f1.bodyHeight + subtitleHeight
	if legend {
		topMargin += 1.5 * bodyHeight
	}
	var leftMargin float32
	if chart.horizontal {
		leftMargin = 1.5*bodyHeight + pad + widestCategory
	} else {
		leftMargin = 1.5*bodyHeight + pad + widestAxisLabel
	}
	rightMargin := pad
	bottomMargin := 2.5 * bodyHeight
	if chart.horizontal {
		if chart.valueLabelsInside {
			rightMargin += widestAxisLabel / 2.0
		} else {
			rightMargin += max(widestAxisLabel/2.0, widestValueLabel+pad)
		}
	} else if chart.drawValueLabels && !chart.stacked && !chart.valueLabelsInside {
		topMargin += bodyHeight
	}
	x5 := chart.x1 + leftMargin
	y5 := chart.y1 + topMargin
	x6 := x2 - rightMargin
	y8 := y2 - bottomMargin

	// Title, the subtitle and then the legend under it
	page.SetBrushColor(color.Black)
	page.drawString(f1, f1.size, chart.title,
		chart.x1+(chart.w-f1.StringWidth(f1.size, chart.title))/2.0, titleBaseline,
		[3]float32{0.0, 0.0, 0.0}, nil)
	if chart.subtitle != "" {
		page.SetBrushColor(color.DimGray)
		page.drawString(f2, f2.size, chart.subtitle,
			chart.x1+(chart.w-f2.StringWidth(f2.size, chart.subtitle))/2.0, titleBaseline+subtitleHeight,
			[3]float32{0.0, 0.0, 0.0}, nil)
	}
	if legend {
		chart.drawLegendOn(page, titleBaseline+subtitleHeight+1.5*bodyHeight)
	}

	if chart.chartBorderWidth > 0.0 {
		page.SetPenColor(color.Black)
		page.SetPenWidth(chart.chartBorderWidth)
		page.SetDefaultStrokeDashPattern()
		page.DrawRect(chart.x1, chart.y1, chart.w, chart.h)
	}

	// Grid lines and value axis labels
	for i := 0; i <= lines; i++ {
		v := vMin + step*float32(i)
		label := chart.axisLabel(v, axisDigits)
		if chart.horizontal {
			x := x5 + (v-vMin)*(x6-x5)/(vMax-vMin)
			if chart.drawGridLines {
				chart.gridLine(page, x, y5, x, y8)
			}
			page.drawString(f2, f2.size, label, x-f2.StringWidth(f2.size, label)/2.0, y8+bodyHeight,
				[3]float32{0.0, 0.0, 0.0}, nil)
		} else {
			y := y8 - (v-vMin)*(y8-y5)/(vMax-vMin)
			if chart.drawGridLines {
				chart.gridLine(page, x5, y, x6, y)
			}
			page.drawString(f2, f2.size, label, x5-pad-f2.StringWidth(f2.size, label), y+ascent/2.0,
				[3]float32{0.0, 0.0, 0.0}, nil)
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
	groupWidth := slot * (1.0 - chart.groupGap)
	barWidth := groupWidth
	if m != 0 && !chart.stacked {
		barWidth = groupWidth / (float32(m) + float32(m-1)*chart.barGap)
	}
	for i := 0; i < n; i++ {
		var slotStart float32
		if chart.horizontal {
			slotStart = y5 + float32(i)*slot
		} else {
			slotStart = x5 + float32(i)*slot
		}
		groupStart := slotStart + slot*chart.groupGap/2.0
		category := ""
		if i < len(chart.categories) {
			category = chart.categories[i]
		}
		page.SetBrushColor(color.Black)
		if chart.horizontal {
			page.drawString(f2, f2.size, category, x5-pad-f2.StringWidth(f2.size, category),
				slotStart+slot/2.0+ascent/2.0, [3]float32{0.0, 0.0, 0.0}, nil)
		} else {
			page.drawString(f2, f2.size, category, slotStart+(slot-f2.StringWidth(f2.size, category))/2.0,
				y8+bodyHeight, [3]float32{0.0, 0.0, 0.0}, nil)
		}
		up := float32(0.0)   // the stacked values above 0 so far
		down := float32(0.0) // the stacked values below 0 so far
		for j := 0; j < m; j++ {
			s := chart.series[j]
			if i >= len(s.values) {
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
			from = min(max(from, vMin), vMax)
			to = min(max(to, vMin), vMax)
			barStart := groupStart
			if !chart.stacked {
				barStart = groupStart + float32(j)*barWidth*(1.0+chart.barGap)
			}
			page.SetBrushColor(chart.barColor(s, j, i))
			label := ""
			if chart.drawValueLabels {
				label = chart.valueLabel(s.values[i])
			}
			if chart.horizontal {
				x0 := x5 + (from-vMin)*(x6-x5)/(vMax-vMin)
				x := x5 + (to-vMin)*(x6-x5)/(vMax-vMin)
				page.FillRect(min(x0, x), barStart, abs32(x-x0), barWidth)
				if chart.drawValueLabels && chart.stacked {
					if abs32(x-x0) >= f2.StringWidth(f2.size, label)+pad {
						page.SetBrushColor(color.Black)
						page.drawString(f2, f2.size, label,
							min(x0, x)+(abs32(x-x0)-f2.StringWidth(f2.size, label))/2.0,
							barStart+barWidth/2.0+ascent/2.0, [3]float32{0.0, 0.0, 0.0}, nil)
					}
				} else if chart.drawValueLabels && chart.valueLabelsInside &&
					abs32(x-x0) >= f2.StringWidth(f2.size, label)+pad {
					page.SetBrushColor(color.White)
					var lx float32
					if to >= base {
						lx = x - pad/2.0 - f2.StringWidth(f2.size, label)
					} else {
						lx = x + pad/2.0
					}
					page.drawString(f2, f2.size, label, lx, barStart+barWidth/2.0+ascent/2.0,
						[3]float32{0.0, 0.0, 0.0}, nil)
				} else if chart.drawValueLabels {
					page.SetBrushColor(color.Black)
					var lx float32
					if to >= base {
						lx = x + pad/2.0
					} else {
						lx = x - pad/2.0 - f2.StringWidth(f2.size, label)
					}
					page.drawString(f2, f2.size, label, lx, barStart+barWidth/2.0+ascent/2.0,
						[3]float32{0.0, 0.0, 0.0}, nil)
				}
			} else {
				y0 := y8 - (from-vMin)*(y8-y5)/(vMax-vMin)
				y := y8 - (to-vMin)*(y8-y5)/(vMax-vMin)
				page.FillRect(barStart, min(y0, y), barWidth, abs32(y-y0))
				if chart.drawValueLabels && chart.stacked {
					if abs32(y-y0) >= bodyHeight {
						page.SetBrushColor(color.Black)
						page.drawString(f2, f2.size, label, barStart+(barWidth-f2.StringWidth(f2.size, label))/2.0,
							(y+y0)/2.0+ascent/2.0, [3]float32{0.0, 0.0, 0.0}, nil)
					}
				} else if chart.drawValueLabels && chart.valueLabelsInside && abs32(y-y0) >= bodyHeight+pad {
					page.SetBrushColor(color.White)
					var ly float32
					if to >= base {
						ly = y + ascent + pad/2.0
					} else {
						ly = y - pad/2.0
					}
					page.drawString(f2, f2.size, label, barStart+(barWidth-f2.StringWidth(f2.size, label))/2.0, ly,
						[3]float32{0.0, 0.0, 0.0}, nil)
				} else if chart.drawValueLabels {
					page.SetBrushColor(color.Black)
					var ly float32
					if to >= base {
						ly = y - pad/2.0
					} else {
						ly = y + ascent + pad/2.0
					}
					page.drawString(f2, f2.size, label, barStart+(barWidth-f2.StringWidth(f2.size, label))/2.0, ly,
						[3]float32{0.0, 0.0, 0.0}, nil)
				}
			}
		}
	}

	// Axis lines: along the labels and at the base of the bars
	page.SetPenColor(color.Black)
	page.SetDefaultStrokeDashPattern()
	if chart.axisLineWidth > 0.0 {
		page.SetPenWidth(chart.axisLineWidth)
		if chart.horizontal {
			x0 := x5 + (base-vMin)*(x6-x5)/(vMax-vMin)
			page.DrawLine(x5, y8, x6, y8)
			page.DrawLine(x0, y5, x0, y8)
		} else {
			y0 := y8 - (base-vMin)*(y8-y5)/(vMax-vMin)
			page.DrawLine(x5, y5, x5, y8)
			page.DrawLine(x5, y0, x6, y0)
		}
	}
	if chart.innerBorderWidth > 0.0 {
		page.SetPenWidth(chart.innerBorderWidth)
		page.DrawRect(x5, y5, x6-x5, y8-y5)
	}

	// Axis titles
	page.SetBrushColor(color.Black)
	page.SetTextRotation(90)
	page.drawString(f2, f2.size, chart.yAxisTitle, chart.x1+bodyHeight,
		y8-((y8-y5)-f2.StringWidth(f2.size, chart.yAxisTitle))/2.0, [3]float32{0.0, 0.0, 0.0}, nil)
	page.SetTextRotation(0)
	page.drawString(f2, f2.size, chart.xAxisTitle, x5+((x6-x5)-f2.StringWidth(f2.size, chart.xAxisTitle))/2.0,
		y2-bodyHeight/2.0, [3]float32{0.0, 0.0, 0.0}, nil)

	page.SetDefaultPenWidth()
	page.SetDefaultStrokeDashPattern()
	page.SetPenColor(color.Black)

	return [2]float32{chart.x1 + chart.w, chart.y1 + chart.h}
}

// numberOfCategories returns the number of category slots: the categories or the longest series.
func (chart *BarChart) numberOfCategories() int {
	n := len(chart.categories)
	for _, s := range chart.series {
		n = max(n, len(s.values))
	}
	return n
}

// hasSeriesNames returns true if a series has a name to list in the legend.
func (chart *BarChart) hasSeriesNames() bool {
	for _, s := range chart.series {
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
		return defaultPalette[seriesIndex%len(defaultPalette)]
	}
	return s.color
}

// valueLabel formats the value written at the end of a bar.
func (chart *BarChart) valueLabel(value float32) string {
	return chart.group(format(value, chart.minFractionDigits, chart.maxFractionDigits))
}

// axisLabel formats a label of the value axis.
func (chart *BarChart) axisLabel(value float32, digits int) string {
	return chart.group(format(value, digits, chart.maxFractionDigits))
}

// group groups the digits before the decimal point in thousands with commas,
// if grouping is used.
func (chart *BarChart) group(label string) string {
	if !chart.groupingUsed {
		return label
	}
	start := 0
	if strings.HasPrefix(label, "-") {
		start = 1
	}
	end := strings.IndexByte(label, '.')
	if end < 0 {
		end = len(label)
	}
	for i := end - 3; i > start; i -= 3 {
		label = label[:i] + "," + label[i:]
	}
	return label
}

// gridLine draws one dotted grid line.
func (chart *BarChart) gridLine(page *Page, xa, ya, xb, yb float32) {
	page.SetPenColor(chart.gridLineColor)
	page.SetPenWidth(chart.gridLineWidth)
	page.SetStrokeDashPattern(chart.gridLineDashPattern)
	page.DrawLine(xa, ya, xb, yb)
}

// drawLegendOn draws the legend centered on the chart, with a swatch before each series name.
func (chart *BarChart) drawLegendOn(page *Page, baseline float32) {
	f2 := chart.f2
	swatch := f2.ascent
	gap := swatch / 2.0
	width := float32(0.0)
	entries := 0
	for _, s := range chart.series {
		if s.name != "" {
			width += swatch + gap + f2.StringWidth(f2.size, s.name)
			entries++
		}
	}
	width += float32(entries-1) * f2.bodyHeight
	x := chart.x1 + (chart.w-width)/2.0
	for j, s := range chart.series {
		if s.name == "" {
			continue
		}
		page.SetBrushColor(chart.barColor(s, j, -1))
		page.FillRect(x, baseline-swatch, swatch, swatch)
		x += swatch + gap
		page.SetBrushColor(color.Black)
		page.drawString(f2, f2.size, s.name, x, baseline, [3]float32{0.0, 0.0, 0.0}, nil)
		x += f2.StringWidth(f2.size, s.name) + f2.bodyHeight
	}
}

// abs32 returns the absolute value of a float32.
func abs32(value float32) float32 {
	if value < 0 {
		return -value
	}
	return value
}
