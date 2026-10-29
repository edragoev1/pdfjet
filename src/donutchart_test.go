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
	chart := testDonutChart(pdf).AddWithColor("none", 0, color.Red)
	testAssertXY(t, 300, 300, chart.DrawOn(page))
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestDonutChartSlicesArePercentagesOfTheSumOfTheValues(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf)
	chart.AddWithColor("a", 1, color.Red)
	chart.AddWithColor("b", 1, color.Green)
	chart.AddWithColor("c", 2, color.Blue)
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
	chart.AddWithColor("a", 3, color.Red).AddWithColor("b", 1, color.Blue)
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
	donut := testDonutChart(pdf).AddWithColor("Apples", 25, color.Red).
		AddWithColor("Oranges", 75, color.Blue)
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
	testDonutChart(pdf).SetRadii(100, 0).AddWithColor("", 1, color.Red).DrawOn(page)
	testDonutChart(pdf).SetAltDescription("Most are oranges.").
		AddWithColor("Apples", 1, color.Red).DrawOn(page)
	for i, want := range []string{"Donut chart: Apples 25%, Oranges 75%", "Pie chart: 100%", "Most are oranges."} {
		if got := page.structures[i].altDescription; got != want {
			t.Errorf("chart %d is described as %q, not %q", i, got, want)
		}
	}
}

// Slices in the palette, blue first; thin white lines between them; thin
// gray leader lines; the percentage white on blue, ink on orange; and no
// black line or text.
func TestDonutChartTheDefaultLookIsQuiet(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).Add("a", 60).Add("b", 40).DrawOn(page)
	content := testContent(page)
	for _, want := range []string{
		"0.31 0.47 0.65 rg",           // blue
		"0.95 0.56 0.17 rg",           // orange
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
	testAssertXY(t, 300, 315, testDonutChart(pdf).Add("a", 1).DrawOn(page))
	xy := testDonutChart(pdf).SetTitle("Fruit").SetSubtitle("This week").
		Add("a", 1).DrawOn(page)
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
	if got := page.structures[1].altDescription; got != "Fruit. This week: a 100%" {
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
		chart.AddWithColor("bad", bad, color.Red)
		chart.AddWithColor("a", 1, color.Green)
		chart.AddWithColor("b", 3, color.Blue)
		chart.DrawOn(page)
		content := testContent(page)
		if !strings.Contains(content, testHex("25%")) || !strings.Contains(content, testHex("75%")) {
			t.Errorf("%v: the other slices are not 25%% and 75%% in %q", bad, content)
		}
	}
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).Add("a", 3e38).Add("b", 3e38).DrawOn(page)
	if content := testContent(page); !strings.Contains(content, testHex("50%")) {
		t.Errorf("the slices are not 50%% in %q", content)
	}
}

// A slice of an empty name has no label.
func TestDonutChartAnEmptyNameIsNoLabel(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).Add("", 1).Add("a", 3).DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("a")) || !strings.Contains(content, testHex("25%")) {
		t.Errorf("the slices are not drawn in %q", content)
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
	chart := testDonutChart(pdf).Add("Big", 90)
	for i := 0; i < 6; i++ {
		chart.Add("Small "+strconv.Itoa(i), 1.5)
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
		Add("a", 60).Add("b", 40)
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
	testDonutChart(pdf).SetRadii(100, 0).SetCenterText("none").Add("a", 1).DrawOn(page)
	if strings.Contains(testContent(page), testHex("none")) {
		t.Errorf("a pie chart writes the text: %q", testContent(page))
	}
}

// The review of 26 October 2026, the second: labels moved apart stay outside
// the ring; long labels, legend items and texts in the hole are shortened; the
// caller's dash pattern does not dash the lines; an inner radius past the
// outer one is the outer one; and the last slice, by the first around the
// circle, is not in the first's color.
func TestDonutChartTheSecondReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	thin := testDonutChart(pdf).Add("Big", 90) // At (100, 100), radii 100 and 50
	for i := 0; i < 8; i++ {
		thin.Add("Small "+strconv.Itoa(i), 1.2)
	}
	thin.DrawOn(page)
	content := testContent(page)
	for i := 0; i < 8; i++ {
		text := "Small " + strconv.Itoa(i)
		at := testPositionOf(t, content, text) // In PDF space, y up
		dy := (792 - at[1]) - 200
		ring := float32(0)
		if abs32(dy) < 100 {
			ring = float32(math.Sqrt(float64(float32(100*100) - float32(dy*dy))))
		}
		if at[0]+font.StringWidth(font.size, text) > 200-ring+0.01 {
			t.Errorf("%s is on the ring: %v", text, at)
		}
	}

	page = NewPage(pdf, testLetterPortrait())
	page.SetStrokeDashPattern("[3 3] 0")
	name := "A label much too long to be drawn whole beside the circle"
	testDonutChart(pdf).SetCenterText("A text in the hole far too long to be written in it at any size").
		Add(name, 60).Add("b", 40).DrawOn(page)
	content = testContent(page)
	// The chart's own solid lines after the caller's dashes
	dashed := strings.Index(content, "[3 3] 0 d\n")
	if dashed < 0 || strings.Index(content, "[] 0 d\n") <= dashed {
		t.Errorf("the caller's dash pattern is kept: %q", content)
	}
	if strings.Contains(content, testHex(name)) {
		t.Errorf("the label is not shortened: %q", content)
	}
	if !strings.Contains(content, " 9 Tf\n") { // The text in the hole at 9, three quarters of 12
		t.Errorf("the text in the hole is not at 9: %q", content)
	}
	page = NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).SetLegend(true).Add(name, 60).
		Add("b", 40).DrawOn(page)
	content = testContent(page)
	if strings.Contains(content, testHex(name)) {
		t.Errorf("the legend item is not shortened: %q", content)
	}
	if !strings.Contains(content, testHex(" 60%")) { // The percentage kept
		t.Errorf("the percentage is not kept: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).Add("a", 1).DrawOn(page)
	outer := testContent(page)
	page = NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).SetRadii(50, 100).Add("a", 1).DrawOn(page)
	// The same but for the names of the fonts, a new one for each chart
	fonts := regexp.MustCompile(`/F\d+ `)
	if fonts.ReplaceAllString(outer, "/F ") != fonts.ReplaceAllString(testContent(page), "/F ") {
		t.Errorf("the radii swapped draw otherwise")
	}

	page = NewPage(pdf, testLetterPortrait())
	eleven := testDonutChart(pdf)
	for i := 0; i < 11; i++ {
		eleven.Add("", 1)
	}
	eleven.DrawOn(page)
	content = testContent(page)
	if strings.Index(content, "0.31 0.47 0.65 rg") != strings.LastIndex(content, "0.31 0.47 0.65 rg") {
		t.Errorf("two slices in blue: %q", content)
	}
}

// The third review of 26 October 2026: a radius that is not a number is 0, in
// the four ports alike; and the percentages are not written in a ring too thin
// for them.
func TestDonutChartTheThirdReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	fonts := regexp.MustCompile(`/F\d+ `)
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).SetRadii(100, float32(math.NaN())).Add("a", 1).Add("b", 3).DrawOn(page)
	nan := testContent(page)
	page = NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).SetRadii(100, 0).Add("a", 1).Add("b", 3).DrawOn(page)
	if got, want := fonts.ReplaceAllString(nan, "/F "), fonts.ReplaceAllString(testContent(page), "/F "); got != want {
		t.Errorf("a radius of NaN is not 0:\n%s\n%s", got, want)
	}
	page = NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).SetRadii(200, 196).Add("a", 1).Add("b", 3).DrawOn(page)
	if content := testContent(page); strings.Contains(content, testHex("75%")) {
		t.Errorf("a percentage in a thin ring: %q", content)
	}
}
