// simplelinechart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

func testSimpleLineChart(pdf *PDF) *SimpleLineChart {
	font := testHelvetica(pdf)
	chart := NewSimpleLineChart(font, font).SetSize(400, 300)
	chart.SetLocation(50, 50)
	return chart.SetTitle("Vehicles").SetSubtitle("by model year").
		SetCategories("2021", "2022", "2023").
		AddLine("Battery", []float32{14755, 23507, 11853}).AddLine("Hybrid", []float32{3541, 4015, 1500})
}

func TestSimpleLineChartDescribesItselfFromItsData(t *testing.T) {
	chart := testSimpleLineChart(testNewPDF())
	want := "Vehicles, by model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;" +
		" Hybrid from 3,541 to 1,500, highest 4,015 in 2022."
	if got := chart.getAltDescription(); got != want {
		t.Errorf("got %q, want %q", got, want)
	}
	chart.SetAltDescription("Two lines")
	if got := chart.getAltDescription(); got != "Two lines" {
		t.Errorf("the description set: got %q", got)
	}
}

func TestSimpleLineChartTheHighlightedLineHasItsOwnColor(t *testing.T) {
	for _, highlighted := range []bool{true, false} {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		chart := testSimpleLineChart(pdf).SetHighlightColor(color.Red)
		if highlighted {
			chart.SetHighlight(0)
		}
		chart.DrawOn(page)
		if got := strings.Contains(testContent(page), "1 0 0 RG"); got != highlighted {
			t.Errorf("highlighted %v: the highlight color stroked %v", highlighted, got)
		}
	}
}

func TestSimpleLineChartLabelsTheEndsOfItsLines(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testSimpleLineChart(pdf).DrawOn(page)
	content := testContent(page)
	for _, label := range []string{"Battery 11,853", "Hybrid 1,500"} {
		if !strings.Contains(content, testHex(label)) {
			t.Errorf("%q missing from %q", label, content)
		}
	}
}

func TestSimpleLineChartAMissingValueLeavesAGap(t *testing.T) {
	strokes := func(values []float32) int {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		font := testHelvetica(pdf)
		NewSimpleLineChart(font, font).SetCategories("a", "b", "c", "d", "e").AddLine("line", values).DrawOn(page)
		return strings.Count(testContent(page), "\nS\n")
	}
	nan := float32(math.NaN())
	whole := strokes([]float32{1, 2, 3, 4, 5})
	broken := strokes([]float32{1, 2, nan, 4, 5})
	if broken != whole+1 {
		t.Errorf("a line of five values stroked %d times, with a gap %d times", whole, broken)
	}
}

func TestSimpleLineChartEndLabelsDoNotMeet(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	chart := NewSimpleLineChart(font, font).SetCategories("a", "b").
		AddLine("one", []float32{1, 5}).AddLine("two", []float32{2, 5}) // Both end at 5
	page := NewPage(pdf, testLetterPortrait())
	chart.DrawOn(page)
	// The two labels are drawn at two baselines a line apart
	content := testContent(page)
	if !strings.Contains(content, testHex("one 5")) || !strings.Contains(content, testHex("two 5")) {
		t.Fatalf("the labels missing from %q", content)
	}
	one, two := testPositionOf(t, content, "one 5"), testPositionOf(t, content, "two 5")
	if d := float32(math.Abs(float64(one[1] - two[1]))); d < font.bodyHeight-testDelta {
		t.Errorf("the labels %v points apart, less than a line, %v", d, font.bodyHeight)
	}
}

func TestSimpleLineChartWithoutValuesDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	nan := float32(math.NaN())
	xy := NewSimpleLineChart(font, font).SetTitle("Nothing").AddLine("line", []float32{nan}).DrawOn(page)
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
	testAssertXY(t, 400, 300, xy)
}

// A value outside a range that is set is drawn past the plot, and the lines
// are clipped at its edges; a value far past it, or values whose span is past
// the largest float, are drawn without a panic or an error that lost the
// document (review of 26 October 2026).
func TestSimpleLineChartAValueOutsideTheRangeIsClipped(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	testSimpleLineChart(pdf).DrawOn(page)
	if strings.Contains(testContent(page), "W\nn\n") {
		t.Error("a chart in its range is clipped")
	}
	for _, far := range []float32{60, 1e37, -1e37} {
		page = NewPage(pdf, testLetterPortrait())
		NewSimpleLineChart(font, font).SetYAxisRange(0, 10).AddLine("a", []float32{5, far, 5}).DrawOn(page)
		content := testContent(page)
		if !strings.Contains(content, "W\nn\n") || !strings.Contains(content, testHex("a 5")) {
			t.Errorf("%v: %q", far, content)
		}
	}
	NewSimpleLineChart(font, font).AddLine("wide", []float32{-3e38, 3e38}).DrawOn(NewPage(pdf, testLetterPortrait()))
	NewSimpleLineChart(font, font).SetYAxisRange(-3e38, 3e38).AddLine("a", []float32{1, 2}).
		DrawOn(NewPage(pdf, testLetterPortrait()))
}

// A range that is set has grid lines on a round step: 3 to 17 was written
// 3.00, 5.33, 7.67 in 6 lines, and is 3, 5, 7 in 7; a range of no round step
// in 2 to 10 lines keeps its lines (review of 26 October 2026).
func TestSimpleLineChartASetRangeHasARoundStep(t *testing.T) {
	if got := axisOf([]float32{5}, 3, 17, true).lines; got != 7 {
		t.Errorf("3 to 17 in %d lines", got)
	}
	if got := axisOf([]float32{5}, 0, 10, true).lines; got != 10 {
		t.Errorf("0 to 10 in %d lines", got)
	}
	if got := roundLines(0, float32(1.3*3.7), 6); got != 6 { // No round step in 2 to 10
		t.Errorf("0 to 4.81 in %d lines", got)
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	NewSimpleLineChart(font, font).SetYAxisRange(3, 17).AddLine("a", []float32{5, 9}).DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("15")) || strings.Contains(content, testHex("5.33")) {
		t.Errorf("%q", content)
	}
}

// The labels at the ends of the lines, moved apart, stay over the plot: they
// were pushed down past the bottom of the panel.
func TestSimpleLineChartTheEndLabelsStayOverThePlot(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewSimpleLineChart(font, font).SetSize(400, 120)
	for i := 0; i < 8; i++ {
		chart.AddLine("line "+strconv.Itoa(i), []float32{10, 0})
	}
	chart.AddLine("top", []float32{0, 10})
	chart.DrawOn(page)
	content := testContent(page)
	// The page is 792 high, the chart from 0 to 120 down from its top
	if y := testPositionOf(t, content, "line 7 0")[1]; y <= 792-120 {
		t.Errorf("the last label is at %v, under the chart, in %q", y, content)
	}
}

// The values are written by their points but the last of each line, which
// its end label has; the area under a line is filled in a tint of its
// color, under the line.
func TestSimpleLineChartTheValuesAndTheAreaAreDrawnWhenAsked(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	NewSimpleLineChart(font, font).SetCategories("a", "b", "c").AddLine("x", []float32{12, 37, 23}).
		SetHighlight(0).SetValueLabels(true).SetAreaFilled(true).DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("12")) || !strings.Contains(content, testHex("37")) {
		t.Errorf("the values are not written: %q", content)
	}
	if strings.Contains(content, "<"+testHex("23")+">") { // Only in "x 23"
		t.Errorf("the last value is written: %q", content)
	}
	area := strings.Index(content, "0.78 0.8 0.84 rg\n") // Navy, a quarter over white
	if area <= 0 || area >= strings.Index(content, "0.12 0.23 0.37 RG\n") {
		t.Errorf("the area is not under the line: %q", content)
	}
	page = NewPage(pdf, testLetterPortrait())
	NewSimpleLineChart(font, font).AddLine("x", []float32{12, 37}).DrawOn(page)
	if strings.Contains(testContent(page), testHex("12")) {
		t.Errorf("a value is written unasked: %q", testContent(page))
	}
}
