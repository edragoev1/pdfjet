// donutchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

func testDonutChart(pdf *PDF) *DonutChart {
	font := testHelvetica(pdf)
	chart := NewDonutChart(font, font).SetRadii(100, 50)
	chart.SetLocation(100, 100)
	return chart
}

func TestDonutChartAChartWithoutValuesDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf).AddSlice(NewSlice(0, color.Red, "none"))
	testAssertXY(t, 300, 300, chart.DrawOn(page))
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestDonutChartSlicesArePercentagesOfTheSumOfTheValues(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf)
	chart.AddSlice(NewSlice(1, color.Red, "a"))
	chart.AddSlice(NewSlice(1, color.Green, "b"))
	chart.AddSlice(NewSlice(2, color.Blue, "c"))
	testAssertXY(t, 300, 300, chart.DrawOn(page))
	content := testContent(page)
	if !strings.Contains(content, testHex("25%")) || !strings.Contains(content, testHex("50%")) {
		t.Errorf("percentages missing from %q", content)
	}
	if strings.Contains(content, testHex("100%")) {
		t.Errorf("100%% in %q", content)
	}
}

func TestDonutChartAPieChartHasAnInnerRadiusOfZero(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf).SetRadii(100, 0)
	chart.AddSlice(NewSlice(3, color.Red, "a")).AddSlice(NewSlice(1, color.Blue, "b"))
	chart.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("75%")) {
		t.Errorf("75%% missing from %q", content)
	}
	if !strings.Contains(content, "200 592 l") { // the center, in PDF coordinates
		t.Errorf("center missing from %q", content)
	}
}

func TestDonutChartAChartIsAFigureThatListsItsSlices(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	donut := testDonutChart(pdf).AddSlice(NewSlice(25, color.Red, "Apples")).
		AddSlice(NewSlice(75, color.Blue, "Oranges"))
	donut.DrawOn(page)
	content := testContent(page)
	if !strings.HasPrefix(content, "/Figure <</MCID 0>>\nBDC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the chart is not a figure: %q", content)
	}
	if got := testFillColorBefore(content, "Apples"); got != "0.13 0.15 0.17 rg" {
		t.Errorf("the label is drawn with %q", got)
	}
	if got := testFillColorBefore(content, "25%"); got != "1 1 1 rg" {
		t.Errorf("the percentage is drawn with %q", got)
	}
	testDonutChart(pdf).SetRadii(100, 0).AddSlice(NewSlice(1, color.Red, "")).DrawOn(page)
	testDonutChart(pdf).SetAltDescription("Most are oranges.").
		AddSlice(NewSlice(1, color.Red, "Apples")).DrawOn(page)
	for i, want := range []string{"Donut chart: Apples 25%, Oranges 75%", "Pie chart: 100%", "Most are oranges."} {
		if got := page.structures[i].altDescription; got != want {
			t.Errorf("chart %d is described as %q, not %q", i, got, want)
		}
	}
}

// Slices in the palette, navy first; thin white lines between them; thin
// gray leader lines; the percentage white on navy, ink on the light steel
// blue; and no black line or text.
func TestDonutChartTheDefaultLookIsQuiet(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).AddSlice(NewSliceInPalette(60, "a")).AddSlice(NewSliceInPalette(40, "b")).DrawOn(page)
	content := testContent(page)
	for _, want := range []string{
		"0.12 0.23 0.37 rg",           // navy
		"0.44 0.58 0.76 rg",           // steel blue
		"1 1 1 RG\n1.5 w\n",           // the white lines between
		"0.55 0.59 0.64 RG\n0.75 w\n", // the leader lines
	} {
		if !strings.Contains(content, want) {
			t.Errorf("%q missing from %q", want, content)
		}
	}
	if strings.Contains(content, "0 0 0 RG") || strings.Contains(content, "0 0 0 rg") {
		t.Errorf("black in %q", content)
	}
	if got := testFillColorBefore(content, "60%"); got != "1 1 1 rg" {
		t.Errorf("60%% is drawn with %q", got)
	}
	if got := testFillColorBefore(content, "40%"); got != "0.13 0.15 0.17 rg" {
		t.Errorf("40%% is drawn with %q", got)
	}
}

// The title at the location, the subtitle under it, the circle under both; a
// chart without them has the circle at its location, as before.
func TestDonutChartATitleIsDrawnAboveTheCircle(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	// The circle, and its label 15 under it
	testAssertXY(t, 300, 315, testDonutChart(pdf).AddSlice(NewSliceInPalette(1, "a")).DrawOn(page))
	xy := testDonutChart(pdf).SetTitle("Fruit").SetSubtitle("This week").
		AddSlice(NewSliceInPalette(1, "a")).DrawOn(page)
	testNear(t, "x", 300, xy[0], testDelta)
	if xy[1] <= 315 {
		t.Errorf("the circle is not under the title: %v", xy[1])
	}
	content := testContent(page)
	if !strings.Contains(content, testHex("Fruit")) || !strings.Contains(content, testHex("This week")) {
		t.Errorf("the title missing from %q", content)
	}
	if got := testFillColorBefore(content, "This week"); got != "0.37 0.41 0.45 rg" {
		t.Errorf("the subtitle is drawn with %q", got)
	}
	if got := page.structures[1].altDescription; got != "Fruit, This week: a 100%" {
		t.Errorf("the chart is described as %q", got)
	}
}

// A slice that is not a number or an infinity is not drawn, and the others
// are: one made the arc's loop endless (review of 11 October 2026, #2). A
// value whose 360ths are past the largest float32 is drawn, as it compares
// with the others (review of 26 October 2026).
func TestDonutChartASliceThatIsNotANumberIsNotDrawn(t *testing.T) {
	for _, bad := range []float32{float32(math.NaN()), float32(math.Inf(1))} {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		chart := testDonutChart(pdf)
		chart.AddSlice(NewSlice(bad, color.Red, "bad"))
		chart.AddSlice(NewSlice(1, color.Green, "a"))
		chart.AddSlice(NewSlice(3, color.Blue, "b"))
		chart.DrawOn(page)
		content := testContent(page)
		if !strings.Contains(content, testHex("25%")) || !strings.Contains(content, testHex("75%")) {
			t.Errorf("%v: the other slices are not 25%% and 75%% in %q", bad, content)
		}
	}
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).AddSlice(NewSliceInPalette(3e38, "a")).AddSlice(NewSliceInPalette(3e38, "b")).DrawOn(page)
	if content := testContent(page); !strings.Contains(content, testHex("50%")) {
		t.Errorf("the slices are not 50%% in %q", content)
	}
}

// A nil slice is left out: it panicked when the chart was drawn.
func TestDonutChartANilSliceIsLeftOut(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).AddSlice(nil).AddSlice(NewSliceInPalette(1, "a")).DrawOn(page)
	if !strings.Contains(testContent(page), testHex("a")) {
		t.Error("the slice is not drawn")
	}
}

// The labels of thin slices side by side are moved apart, a line apart at
// least; the percentages add up to 100; the white line by a thin slice is
// narrower than it; and DrawOn returns the bottom right corner of the labels
// too, measured as drawn (review of 26 October 2026).
func TestDonutChartThinSlicesKeepTheirLabelsApart(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf).AddSlice(NewSliceInPalette(90, "Big"))
	for i := 0; i < 6; i++ {
		chart.AddSlice(NewSliceInPalette(1.5, "Small "+strconv.Itoa(i)))
	}
	measured := chart.DrawOn(nil)
	xy := chart.DrawOn(page)
	testAssertXY(t, measured[0], measured[1], xy)
	content := testContent(page)
	font := testHelvetica(pdf)
	for i := 1; i < 6; i++ {
		above := testPositionOf(t, content, "Small "+strconv.Itoa(i-1))[1]
		below := testPositionOf(t, content, "Small "+strconv.Itoa(i))[1]
		if abs32(above-below) < font.bodyHeight-0.01 {
			t.Errorf("the labels meet: %v %v", above, below)
		}
	}
	want := "Donut chart: Big 91%, Small 0 2%, Small 1 2%, Small 2 2%, Small 3 1%, Small 4 1%, Small 5 1%"
	if got := page.structures[0].altDescription; got != want {
		t.Errorf("the chart is described as %q", got)
	}
	if !strings.Contains(content, "1.5 w\n") { // By the big slice
		t.Errorf("no white line of 1.5 in %q", content)
	}
	if !regexp.MustCompile(`\n0\.\d+ w\n`).MatchString(content) { // Between two thin ones
		t.Errorf("no thin white line in %q", content)
	}
}

// With a legend the chart has no leader lines: each slice's label and
// percentage is in rows under the circle, which the returned corner
// holds. The text in the hole is drawn and described; a pie chart has no
// hole for it.
func TestDonutChartALegendAndATextInTheHole(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf).SetLegend(true).SetCenterText("100 crates").
		AddSlice(NewSliceInPalette(60, "a")).AddSlice(NewSliceInPalette(40, "b"))
	xy := chart.DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, "0.55 0.59 0.64 RG") { // No leader lines
		t.Errorf("leader lines in %q", content)
	}
	if !strings.Contains(content, testHex("a 60%")) || !strings.Contains(content, testHex("b 40%")) {
		t.Errorf("no legend in %q", content)
	}
	if !strings.Contains(content, testHex("100 crates")) {
		t.Errorf("no text in the hole in %q", content)
	}
	if xy[1] <= 300+15 {
		t.Errorf("the legend is under the circle: %v", xy[1])
	}
	testAssertXY(t, xy[0], xy[1], chart.DrawOn(nil))
	if got := page.structures[0].altDescription; got != "100 crates: a 60%, b 40%" {
		t.Errorf("the chart is described as %q", got)
	}
	page = NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).SetRadii(100, 0).SetCenterText("none").AddSlice(NewSliceInPalette(1, "a")).DrawOn(page)
	if strings.Contains(testContent(page), testHex("none")) {
		t.Errorf("a pie chart writes the text: %q", testContent(page))
	}
}
