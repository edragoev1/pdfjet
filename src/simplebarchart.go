// simplebarchart.go
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

	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The colors of a simple bar chart unless set: a light panel, gray bars, the
// highlighted bar navy, the names dark and the values gray.
const (
	simpleBarPanel     int32 = 0xF1F3F6
	simpleBarBar       int32 = 0xA7AFB8
	simpleBarHighlight int32 = 0x1F3A5F
	simpleBarInk       int32 = 0x22262B
	simpleBarMuted     int32 = 0x5F6873
)

// SimpleBarChart is a small chart of horizontal bars that compares a few
// values: a name at the left of each bar, the value at its end, and no axis,
// grid or legend, on a light panel, with a title and a subtitle at the top.
// One bar can be highlighted, in navy, its name and value bold, as the best
// of the values: the bar of an index, or the lowest or the highest value,
// which the chart finds. Each bar is as long as its share of the largest
// value. A dashed line can mark a value across the bars, a target or an
// average, and the bars can have rounded ends.
//
// It is meant to stand beside a Table of the same numbers: the chart for the
// picture, the table for the record and for a screen reader. In a PDF/UA
// document the chart is one figure, described from its title and its values
// unless an alternate description is set. For grouped, stacked or vertical
// bars with an axis, use BarChart. Please see Example_20.
type SimpleBarChart struct {
	f1, f2            *Font // the title, and the bold name and value; the rest
	x, y, w           float32
	title, subtitle   string
	altDescription    string
	names             []string
	values            []float32
	highlight         int     // the index of the highlighted bar, or -1
	highlightValue    int     // the bar the chart highlights itself: 0 none, -1 the lowest value, 1 the highest
	reference         float32 // the value of the reference line, or NaN
	referenceLabel    string
	roundedBars       bool
	minFractionDigits int
	maxFractionDigits int
	panelColor        int32
	barColor          int32
	highlightColor    int32
}

// NewSimpleBarChart creates a simple bar chart, 300 points wide.
//   - f1: the font of the title and of the highlighted bar's name and value,
//     a bold one.
//   - f2: the font of the subtitle and of the other names and values.
func NewSimpleBarChart(f1, f2 *Font) *SimpleBarChart {
	return &SimpleBarChart{
		f1: f1, f2: f2, w: 300, highlight: -1, maxFractionDigits: 2, reference: float32(math.NaN()),
		panelColor: simpleBarPanel, barColor: simpleBarBar, highlightColor: simpleBarHighlight,
	}
}

// SetTitle sets the title, drawn at the top left of the chart.
func (chart *SimpleBarChart) SetTitle(title string) *SimpleBarChart {
	chart.title = title
	return chart
}

// SetSubtitle sets the subtitle, drawn small and gray under the title: the
// unit of the values, say.
func (chart *SimpleBarChart) SetSubtitle(subtitle string) *SimpleBarChart {
	chart.subtitle = subtitle
	return chart
}

// SetAltDescription sets the alternate description of the chart in a PDF/UA
// document. Without one the chart is described from its title, its subtitle
// and its values.
func (chart *SimpleBarChart) SetAltDescription(altDescription string) *SimpleBarChart {
	chart.altDescription = altDescription
	return chart
}

// Add adds a bar: its name and its value. A value below zero is drawn as no
// bar, with its value written. A value that is NaN or infinite has no bar and
// no value, as in the other charts, and the description leaves it out.
func (chart *SimpleBarChart) Add(name string, value float32) *SimpleBarChart {
	chart.names = append(chart.names, name)
	chart.values = append(chart.values, value)
	return chart
}

// SetHighlight highlights the bar of the index, the first bar 0: it is drawn
// in the highlight color, its name and value in the bold font. Which bar is
// the best, the lowest value or the highest, is the caller's to say.
func (chart *SimpleBarChart) SetHighlight(index int) *SimpleBarChart {
	chart.highlight = index
	chart.highlightValue = 0
	return chart
}

// SetHighlightLowest highlights the bar of the lowest value, the best of times
// or prices; the first of them when two are lowest. Bars added later count
// too, as the chart finds the bar when it is drawn.
func (chart *SimpleBarChart) SetHighlightLowest() *SimpleBarChart {
	chart.highlightValue = -1
	return chart
}

// SetHighlightHighest highlights the bar of the highest value, the best of
// sales or scores; the first of them when two are highest. Bars added later
// count too, as the chart finds the bar when it is drawn.
func (chart *SimpleBarChart) SetHighlightHighest() *SimpleBarChart {
	chart.highlightValue = 1
	return chart
}

// SetReferenceLine draws a thin dashed line across the bars at a value, a
// target or an average, with its label and value under the bars: "Target 50".
// The bars are scaled so that the line fits, should it be past the largest
// value. A value that is NaN or infinite draws no line.
func (chart *SimpleBarChart) SetReferenceLine(value float32, label string) *SimpleBarChart {
	chart.reference = value
	chart.referenceLabel = label
	return chart
}

// SetRoundedBars draws the bars with slightly rounded ends. They are square
// unless set.
func (chart *SimpleBarChart) SetRoundedBars(roundedBars bool) *SimpleBarChart {
	chart.roundedBars = roundedBars
	return chart
}

// The index of the highlighted bar: the one set, or the one of the lowest or
// highest value, or -1.
func (chart *SimpleBarChart) highlighted() int {
	if chart.highlightValue == 0 {
		return chart.highlight
	}
	best := -1
	for i, value := range chart.values {
		if !isValue(value) {
			continue
		}
		if best < 0 || (chart.highlightValue < 0 && value < chart.values[best]) ||
			(chart.highlightValue > 0 && value > chart.values[best]) {
			best = i
		}
	}
	return best
}

// True when a reference line is drawn.
func (chart *SimpleBarChart) hasReference() bool {
	return isValue(chart.reference)
}

// The text under the reference line: its label and its value, "Target 50", or
// its value alone.
func (chart *SimpleBarChart) referenceText() string {
	if chart.referenceLabel == "" {
		return chart.label(chart.reference)
	}
	return chart.referenceLabel + " " + chart.label(chart.reference)
}

// SetFractionDigits sets the number of digits after the decimal point of the
// values, all of them alike. Without it a value has as many as it needs, two
// at most.
func (chart *SimpleBarChart) SetFractionDigits(digits int) *SimpleBarChart {
	chart.minFractionDigits = digits
	chart.maxFractionDigits = digits
	return chart
}

// SetPanelColor sets the color of the panel behind the chart. It is light
// gray, 0xF1F3F6, unless set. Color.transparent, -1, draws no panel: the
// one color setter where it does not leave the color as it was.
func (chart *SimpleBarChart) SetPanelColor(c int32) *SimpleBarChart {
	chart.panelColor = c
	return chart
}

// SetBarColor sets the color of the bars that are not highlighted. It is
// gray, 0xA7AFB8, unless set.
func (chart *SimpleBarChart) SetBarColor(c int32) *SimpleBarChart {
	chart.barColor = c
	return chart
}

// SetHighlightColor sets the color of the highlighted bar and of its name.
// It is navy, 0x1F3A5F, unless set.
func (chart *SimpleBarChart) SetHighlightColor(c int32) *SimpleBarChart {
	chart.highlightColor = c
	return chart
}

// SetLocation sets the top left corner of the chart.
func (chart *SimpleBarChart) SetLocation(x, y float32) Drawable {
	chart.x = x
	chart.y = y
	return chart
}

// SetWidth sets the width of the chart. Its height follows from the fonts and
// the number of bars; see GetHeight.
func (chart *SimpleBarChart) SetWidth(w float32) *SimpleBarChart {
	chart.w = w
	return chart
}

// GetHeight returns the height of the chart: its padding, its title and
// subtitle, and a row for each bar.
func (chart *SimpleBarChart) GetHeight() float32 {
	if chart.f1 == nil || chart.f2 == nil {
		return 0 // Without its fonts the chart draws nothing
	}
	h := chart.top() + float32(float32(len(chart.values))*chart.rowHeight()) + chart.pad()
	if chart.hasReference() {
		h += float32(1.4 * chart.subtitleSize()) // The line's label under the bars
	}
	return h
}

// The space around the chart's content, a size of the text of the bars.
func (chart *SimpleBarChart) pad() float32 {
	return chart.f2.size
}

// The height of a row, a bar and the space under it.
func (chart *SimpleBarChart) rowHeight() float32 {
	return float32(2.4 * chart.f2.size)
}

// The size of the subtitle.
func (chart *SimpleBarChart) subtitleSize() float32 {
	return float32(0.8 * chart.f2.size)
}

// The height above the first row: the padding, the title and the subtitle.
func (chart *SimpleBarChart) top() float32 {
	top := chart.pad()
	if chart.title != "" {
		top += chart.f1.GetAscent(chart.f1.size) + chart.f1.GetDescent(chart.f1.size)
	}
	if chart.subtitle != "" {
		top += float32(1.4 * chart.subtitleSize())
	}
	if chart.title != "" || chart.subtitle != "" {
		top += chart.f2.size
	}
	return top
}

// The value written at the end of a bar.
func (chart *SimpleBarChart) label(value float32) string {
	if !isValue(value) {
		return "" // No value, as in the other charts: it was written NaN or In,fin,ity
	}
	// Its digits grouped in thousands, 23,507, as in the other charts
	return groupDigits(format(value, chart.minFractionDigits, chart.maxFractionDigits))
}

// The font of the name and the value of the bar of the index.
func (chart *SimpleBarChart) fontOf(index int) *Font {
	if index == chart.highlighted() {
		return chart.f1
	}
	return chart.f2
}

// The alternate description: the one set, or the title, the subtitle and the
// values.
func (chart *SimpleBarChart) getAltDescription() string {
	if chart.altDescription != "" {
		return chart.altDescription
	}
	var sb strings.Builder
	sb.WriteString(join(chart.title, chart.subtitle))
	if sb.Len() == 0 {
		sb.WriteString("Bar chart")
	}
	separator := punctuationAfter(sb.String(), ":") + " "
	for i, name := range chart.names {
		if !isValue(chart.values[i]) {
			continue
		}
		sb.WriteString(separator + name + " " + chart.label(chart.values[i]))
		separator = ", "
	}
	if chart.hasReference() {
		if chart.referenceLabel == "" {
			sb.WriteString("; reference line at " + chart.label(chart.reference))
		} else {
			sb.WriteString("; " + chart.referenceText())
		}
	}
	sb.WriteString(punctuationAfter(sb.String(), "."))
	return sb.String()
}

// punctuationAfter returns the mark to write after the text of a description,
// or nothing when the text ends a sentence already, in ".", "?" or "!": a
// title "Sales." was described as "Sales.: a 5", and in a line chart as
// "Sales., 2019 to 2024:" (the review of 28 October 2026).
func punctuationAfter(text, mark string) string {
	if text != "" {
		end := text[len(text)-1]
		if end == '.' || end == '?' || end == '!' {
			return ""
		}
	}
	return mark
}

// DrawOn draws the chart on the page and returns the x and y coordinates of
// its bottom right corner.
func (chart *SimpleBarChart) DrawOn(page *Page) [2]float32 {
	h := chart.GetHeight()
	if page == nil || len(chart.values) == 0 || chart.f1 == nil || chart.f2 == nil { // Measured, or nothing to draw
		return [2]float32{chart.x + chart.w, chart.y + h}
	}
	if chart.maxFractionDigits > maxChartDigits {
		page.pdf.fail(fractionDigitsMessage)
		return [2]float32{chart.x + chart.w, chart.y + h}
	}
	f1, f2 := chart.f1, chart.f2
	size := f2.size
	pad := chart.pad()

	// The widest name and value, and the room left for the bars
	// A name wider than two fifths of the chart is shortened with an ellipsis
	shown := make([]string, 0, len(chart.names))
	nameWidth := float32(0)
	valueWidth := float32(0)
	largest := float32(0)
	for i := range chart.names {
		valueWidth = max(valueWidth, chart.fontOf(i).StringWidth(size, chart.label(chart.values[i])))
		if isValue(chart.values[i]) {
			largest = max(largest, chart.values[i])
		}
	}
	if chart.hasReference() {
		largest = max(largest, chart.reference) // The line within the bars' room
	}
	// The names in what the values leave of the width, two fifths of it at
	// most: in a chart narrower than its values they were written past it
	nameRoom := max(min(float32(0.4*(chart.w-float32(2.0*pad))),
		chart.w-float32(3.0*pad)-valueWidth-float32(size/2.0)), 0.0)
	for i, name := range chart.names {
		font := chart.fontOf(i)
		// No name where not even an ellipsis fits; the description has it
		if nameRoom < font.StringWidth(size, "…") {
			shown = append(shown, "")
		} else {
			shown = append(shown, shorten(font, size, name, nameRoom))
		}
		nameWidth = max(nameWidth, font.StringWidth(size, shown[i]))
	}
	barX := chart.x + pad + nameWidth + pad
	barRoom := max(chart.x+chart.w-pad-float32(valueWidth+float32(size/2.0))-barX, 0)

	page.AddBDC(structelem.Figure, "", "", chart.getAltDescription())
	page.SaveGraphicsState()

	if chart.panelColor >= 0 {
		page.SetBrushColor(chart.panelColor)
		page.FillRect(chart.x, chart.y, chart.w, h)
	}
	baseline := chart.y + pad
	if chart.title != "" {
		baseline += f1.GetAscent(f1.size)
		page.drawString(f1, f1.size, shorten(f1, f1.size, chart.title, chart.w-float32(2.0*pad)), chart.x+pad, baseline,
			colorToRGB(simpleBarInk), nil)
		baseline += f1.GetDescent(f1.size)
	}
	if chart.subtitle != "" {
		baseline += float32(1.4 * chart.subtitleSize())
		page.drawString(f2, chart.subtitleSize(), shorten(f2, chart.subtitleSize(), chart.subtitle, chart.w-float32(2.0*pad)),
			chart.x+pad, baseline, colorToRGB(simpleBarMuted), nil)
	}

	rowTop := chart.y + chart.top()
	barHeight := float32(1.6 * size)
	ascent := f2.GetAscent(size)
	highlight := chart.highlighted()
	for i, name := range shown {
		font := chart.fontOf(i)
		top := rowTop + float32(float32(i)*chart.rowHeight())
		textBaseline := top + float32((barHeight+ascent-f2.GetDescent(size))/2.0)
		nameColor := simpleBarInk
		valueColor := simpleBarMuted
		bar := chart.barColor
		if i == highlight {
			nameColor = chart.highlightColor
			valueColor = simpleBarInk
			bar = chart.highlightColor
		}
		page.drawString(font, size, name, chart.x+pad, textBaseline, colorToRGB(nameColor), nil)
		length := float32(0)
		if largest > 0 && isValue(chart.values[i]) && chart.values[i] > 0 {
			length = float32(barRoom * (chart.values[i] / largest))
		}
		if length > 0 {
			page.SetBrushColor(bar)
			barW := max(length, 1.5)
			// The radius a fifth of the bar's height, or half a short bar
			r := min(float32(barHeight/5.0), float32(barW/2.0))
			if chart.roundedBars && r > 0.25 {
				page.FillRoundedRect(barX, top, barW, barHeight, r, r)
			} else {
				page.FillRect(barX, top, barW, barHeight)
			}
		}
		page.drawString(font, size, chart.label(chart.values[i]), barX+length+float32(size/2.0), textBaseline,
			colorToRGB(valueColor), nil)
	}

	// The reference line, dashed, over the bars, from a little above the first
	// to a little below the last, and its label under it. It leaves out the
	// rows where it would cross a bar's value.
	if chart.hasReference() && largest > 0 && chart.reference >= 0 {
		lineX := barX + float32(barRoom*(chart.reference/largest))
		lineBottom := rowTop + float32(float32(len(chart.names)-1)*chart.rowHeight()) + barHeight + float32(size/4.0)
		page.SetPenColor(simpleBarInk)
		page.SetPenWidth(0.75)
		page.SetStrokeDashPattern("[2 2] 0")
		from := rowTop - float32(size/4.0)
		for i := range chart.names {
			value := chart.values[i]
			length := float32(0)
			if isValue(value) && value > 0 {
				length = float32(barRoom * (value / largest))
			}
			labelX := barX + length + float32(size/2.0)
			labelW := chart.fontOf(i).StringWidth(size, chart.label(value))
			if labelW > 0 && lineX > labelX-float32(size/2.0) && lineX < labelX+labelW+float32(size/2.0) {
				top := rowTop + float32(float32(i)*chart.rowHeight())
				if top > from {
					page.DrawLine(lineX, from, lineX, top)
				}
				from = top + barHeight
			}
		}
		if lineBottom > from {
			page.DrawLine(lineX, from, lineX, lineBottom)
		}
		page.SetDefaultStrokeDashPattern()
		text := chart.referenceText()
		width := f2.StringWidth(chart.subtitleSize(), text)
		// Centered under the line, within the chart
		textX := max(chart.x+pad, min(lineX-float32(width/2.0), chart.x+chart.w-pad-width))
		page.drawString(f2, chart.subtitleSize(), text, textX, lineBottom+float32(1.2*chart.subtitleSize()),
			colorToRGB(simpleBarInk), nil)
	}

	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(chart.x, chart.y, chart.w, h)
	page.AddEMC()
	return [2]float32{chart.x + chart.w, chart.y + h}
}
