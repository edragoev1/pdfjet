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
// of the values. Each bar is as long as its share of the largest value.
//
// It is meant to stand beside a Table of the same numbers: the chart for the
// picture, the table for the record and for a screen reader. In a PDF/UA
// document the chart is one figure, described from its title and its values
// unless an alternate description is set. For grouped, stacked or vertical
// bars with an axis, use BarChart.
type SimpleBarChart struct {
	f1, f2            *Font // the title, and the bold name and value; the rest
	x, y, w           float32
	title, subtitle   string
	altDescription    string
	names             []string
	values            []float32
	highlight         int // the index of the highlighted bar, or -1
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
		f1: f1, f2: f2, w: 300, highlight: -1, maxFractionDigits: 2,
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
// bar, with its value written.
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
	return chart
}

// SetFractionDigits sets the number of digits after the decimal point of the
// values, all of them alike. Without it a value has as many as it needs, two
// at most.
func (chart *SimpleBarChart) SetFractionDigits(digits int) *SimpleBarChart {
	chart.minFractionDigits = digits
	chart.maxFractionDigits = digits
	return chart
}

// SetPanelColor sets the color of the panel behind the chart, or -1 for
// none. It is light gray, 0xF1F3F6, unless set.
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
	return chart.top() + float32(float32(len(chart.values))*chart.rowHeight()) + chart.pad()
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
	return format(value, chart.minFractionDigits, chart.maxFractionDigits)
}

// The font of the name and the value of the bar of the index.
func (chart *SimpleBarChart) fontOf(index int) *Font {
	if index == chart.highlight {
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
	sb.WriteString(chart.title)
	if chart.subtitle != "" {
		if sb.Len() > 0 {
			sb.WriteString(", ")
		}
		sb.WriteString(chart.subtitle)
	}
	if sb.Len() == 0 {
		sb.WriteString("Bar chart")
	}
	for i, name := range chart.names {
		if i == 0 {
			sb.WriteString(": ")
		} else {
			sb.WriteString(", ")
		}
		sb.WriteString(name + " " + chart.label(chart.values[i]))
	}
	sb.WriteString(".")
	return sb.String()
}

// DrawOn draws the chart on the page and returns the x and y coordinates of
// its bottom right corner.
func (chart *SimpleBarChart) DrawOn(page *Page) [2]float32 {
	h := chart.GetHeight()
	if page == nil || len(chart.values) == 0 { // Measured, or nothing to draw
		return [2]float32{chart.x + chart.w, chart.y + h}
	}
	f1, f2 := chart.f1, chart.f2
	size := f2.size
	pad := chart.pad()

	// The widest name and value, and the room left for the bars
	nameWidth := float32(0)
	valueWidth := float32(0)
	largest := float32(0)
	for i, name := range chart.names {
		font := chart.fontOf(i)
		nameWidth = max(nameWidth, font.StringWidth(size, name))
		valueWidth = max(valueWidth, font.StringWidth(size, chart.label(chart.values[i])))
		if isValue(chart.values[i]) {
			largest = max(largest, chart.values[i])
		}
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
		page.drawString(f1, f1.size, chart.title, chart.x+pad, baseline, colorToRGB(simpleBarInk), nil)
		baseline += f1.GetDescent(f1.size)
	}
	if chart.subtitle != "" {
		baseline += float32(1.4 * chart.subtitleSize())
		page.drawString(f2, chart.subtitleSize(), chart.subtitle, chart.x+pad, baseline, colorToRGB(simpleBarMuted), nil)
	}

	rowTop := chart.y + chart.top()
	barHeight := float32(1.6 * size)
	ascent := f2.GetAscent(size)
	for i, name := range chart.names {
		font := chart.fontOf(i)
		top := rowTop + float32(float32(i)*chart.rowHeight())
		textBaseline := top + float32((barHeight+ascent-f2.GetDescent(size))/2.0)
		nameColor := simpleBarInk
		valueColor := simpleBarMuted
		bar := chart.barColor
		if i == chart.highlight {
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
			page.FillRect(barX, top, max(length, 1.5), barHeight)
		}
		page.drawString(font, size, chart.label(chart.values[i]), barX+length+float32(size/2.0), textBaseline,
			colorToRGB(valueColor), nil)
	}

	page.RestoreGraphicsState()
	page.SetFigureBoundingBox(chart.x, chart.y, chart.w, h)
	page.AddEMC()
	return [2]float32{chart.x + chart.w, chart.y + h}
}
