// barchart_test.go
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
	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

func testBarChart(pdf *PDF) *BarChart {
	font := testHelvetica(pdf)
	chart := NewBarChart(font, font).SetSize(300, 200)
	chart.SetLocation(50, 50)
	return chart
}

func testDrawBarChart(t *testing.T, chart *BarChart, page *Page) string {
	testAssertXY(t, 350, 250, chart.DrawOn(page))
	return testContent(page)
}

func TestBarChartAChartWithoutCategoriesDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDrawBarChart(t, testBarChart(pdf), page)
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestBarChartTheValueAxisStartsAtZeroWithWholeNumberLabels(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("abc", "def", "ghi")
	chart.AddSeries("", []float32{20, 75, 31})
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("80")) || !strings.Contains(content, testHex("ghi")) {
		t.Errorf("labels missing from %q", content)
	}
	if strings.Contains(content, testHex("0.00")) || strings.Contains(content, testHex("80.00")) {
		t.Errorf("decimals in %q", content)
	}
}

func TestBarChartTheLegendListsTheNamedSeriesInTheirColors(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a")
	chart.AddSeriesWithColor("first", []float32{1}, color.Red)
	chart.AddSeriesWithColor("", []float32{2}, color.Blue)
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("first")) {
		t.Errorf("legend missing from %q", content)
	}
	if !strings.Contains(content, "1 0 0 rg") || !strings.Contains(content, "0 0 1 rg") {
		t.Errorf("colors missing from %q", content)
	}
}

func TestBarChartValueLabelsAreWrittenWithTheFractionDigitsSet(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b").SetHorizontal(true)
	chart.AddSeries("", []float32{2.5, -1}).SetDrawValueLabels(true)
	chart.SetMinimumFractionDigits(1)
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("2.5")) || !strings.Contains(content, testHex("-1.0")) {
		t.Errorf("value labels missing from %q", content)
	}
}

func TestBarChartAManualAxisRangeIsUsedAsGiven(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a")
	chart.AddSeries("", []float32{5}).SetValueAxisMinMax(0, 12, 4)
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("12")) || !strings.Contains(content, testHex("3")) {
		t.Errorf("labels missing from %q", content)
	}
	if strings.Contains(content, "NaN") {
		t.Error("NaN in the content")
	}
}

func TestBarChartStackedBarsUseTheSumsOfTheCategoriesForTheValueAxis(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b")
	chart.AddSeries("", []float32{20, 75}).AddSeries("", []float32{30, 10})
	grouped := testDrawBarChart(t, chart, page)
	if strings.Contains(grouped, testHex("90")) {
		t.Errorf("90 in the grouped content %q", grouped)
	}

	page = NewPage(pdf, testLetterPortrait())
	chart.SetStacked(true).SetDrawValueLabels(true)
	stacked := testDrawBarChart(t, chart, page)
	// The total of each stack above it
	for _, want := range []string{"75", "50", "85"} {
		if !strings.Contains(stacked, testHex(want)) {
			t.Errorf("%s missing from the stacked content %q", want, stacked)
		}
	}
}

// The first series in navy, the grid lines light gray and solid, the text gray
// or ink, and no black line or text.
func TestBarChartTheDefaultLookIsQuiet(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b").SetTitle("t").SetSubtitle("s")
	chart.AddSeries("one", []float32{20, 75}).AddSeries("two", []float32{30, 10})
	content := testDrawBarChart(t, chart, page)
	for _, want := range []string{"0.12 0.23 0.37 rg", "0.44 0.58 0.76 rg", "0.88 0.89 0.91 RG\n0.5 w\n[] 0 d"} {
		if !strings.Contains(content, want) {
			t.Errorf("%q missing from %q", want, content)
		}
	}
	if strings.Contains(content, "0 0 0 RG") || strings.Contains(content, "0 0 0 rg") {
		t.Errorf("black in %q", content)
	}
	if got := testFillColorBefore(content, "t"); got != "0.13 0.15 0.17 rg" {
		t.Errorf("the title is drawn with %q", got)
	}
	if got := testFillColorBefore(content, "s"); got != "0.37 0.41 0.45 rg" {
		t.Errorf("the subtitle is drawn with %q", got)
	}
	// A label on a dark bar is white, on a light one ink: whichever contrasts
	// more. On sage and on the old sage, which a rule by brightness gave
	// white, ink contrasts more; on slate white.
	for c, want := range map[int32]int32{
		0x1F3A5F: color.White, 0x6F95C2: 0x22262B, 0x76987C: 0x22262B,
		0x6B8F71: 0x22262B, 0x4F7A8C: color.White,
	} {
		if got := labelColorOn(c); got != want {
			t.Errorf("the label on %06X is %06X", c, got)
		}
	}
}

func TestBarChartBarsHaveTheirOwnColorsAndLabelsInsideWithGroupedDigits(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b").SetHorizontal(true).SetSubtitle("sub")
	chart.AddSeriesWithColors("rivers", []float32{6650, 12}, []int32{color.Red, color.Blue})
	chart.SetValueAxisMinMax(0, 8000, 4).SetDrawValueLabels(true).SetValueLabelsInside(true)
	chart.SetGroupingUsed(true).SetAxisLineWidth(0).SetGridLineColor(color.LightGray)
	content := testDrawBarChart(t, chart, page)
	// the two bars in their colors, the label inside the first bar in white
	// and the label of the bar too short for it next to the bar
	for _, want := range []string{
		testHex("6,650"), testHex("8,000"), testHex("sub"), testHex("12"), "1 0 0 rg", "0 0 1 rg"} {
		if !strings.Contains(content, want) {
			t.Errorf("%q missing from %q", want, content)
		}
	}
	if got := testFillColorBefore(content, "6,650"); got != "1 1 1 rg" {
		t.Errorf("the label inside the first bar is drawn with %q", got)
	}
	if got := testFillColorBefore(content, "sub"); got != "0.37 0.41 0.45 rg" {
		t.Errorf("the subtitle is drawn with %q", got)
	}
}

func TestBarChartAChartIsAFigureDescribedByItsDataOrItsAlternateDescription(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	titled := testBarChart(pdf).SetTitle("Sales").SetCategories("a", "b")
	titled.AddSeries("", []float32{1, 2})
	content := testDrawBarChart(t, titled, page)
	if !strings.HasPrefix(content, "/Figure <</MCID 0>>\nBDC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the chart is not a figure: %q", content)
	}
	described := testBarChart(pdf).SetTitle("Sales").SetAltDescription("Sales rose from 1 to 2.").
		SetCategories("a", "b")
	described.AddSeries("", []float32{1, 2})
	testDrawBarChart(t, described, page)
	if page.structures[0].altDescription != "Sales: a 1, b 2." {
		t.Errorf("the first chart is described as %q", page.structures[0].altDescription)
	}
	if page.structures[1].altDescription != "Sales rose from 1 to 2." {
		t.Errorf("the second chart is described as %q", page.structures[1].altDescription)
	}
}

func TestBarChartAxisLabelsThatWouldMeetAreDrawnAtEverySecondLine(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a").SetHorizontal(true)
	// 16 steps of 5 across about 240 points: 15 points a step, where a
	// label of one decimal, 70.0, needs more
	chart.AddSeries("", []float32{72.6}).SetValueAxisMinMax(0, 80, 16).SetMinimumFractionDigits(1)
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("70.0")) || !strings.Contains(content, testHex("80.0")) {
		t.Errorf("the labels of the even lines missing from %q", content)
	}
	if strings.Contains(content, testHex("75.0")) {
		t.Errorf("75.0, between 70.0 and 80.0, would meet them, in %q", content)
	}
	if strings.Count(content, " l\n")+strings.Count(content, " l ") == 0 {
		t.Errorf("no grid lines in %q", content)
	}
}

func TestBarChartAxisLabelStepIsTheFewestLinesALabelFits(t *testing.T) {
	cases := []struct {
		spacing, needed float32
		lines, want     int
	}{
		{30, 20, 8, 1}, // fits
		{15, 20, 16, 2},
		{10, 25, 16, 3},
		{5, 100, 4, 4}, // never more than the lines
	}
	for _, c := range cases {
		if got := axisLabelStep(c.spacing, c.needed, c.lines); got != c.want {
			t.Errorf("axisLabelStep(%v, %v, %d) = %d, want %d", c.spacing, c.needed, c.lines, got, c.want)
		}
	}
}

// A category name wider than two fifths of a horizontal chart is shortened
// with an ellipsis, and a chart too small for its plot draws nothing: its axis
// ran backwards, the bars over the names (review of 26 October 2026).
func TestBarChartALongNameIsShortenedAndAChartWithNoRoomDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	name := "A very very long category name indeed, and longer"
	chart := testBarChart(pdf).SetSize(200, 200).SetHorizontal(true).
		SetCategories(name).AddSeries("s", []float32{5})
	testAssertXY(t, 250, 250, chart.DrawOn(page))
	content := testContent(page)
	if strings.Contains(content, testHex(name)) {
		t.Errorf("the name is drawn whole in %q", content)
	}
	font := testHelvetica(pdf)
	shortened := shorten(font, font.size, name, float32(0.4*200))
	// The ellipsis is 85 in WinAnsi, the encoding of a core font
	start := strings.TrimSuffix(shortened, "…")
	if !strings.HasSuffix(shortened, "…") || !strings.Contains(content, testHex(start)+"85>") {
		t.Errorf("%q is not drawn in %q", shortened, content)
	}
	page = NewPage(pdf, testLetterPortrait())
	testBarChart(pdf).SetSize(20, 20).SetCategories("a").AddSeries("s", []float32{5}).DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("%q", content)
	}
}

// A chart without a title has no room for one: its plot starts higher, its
// bars taller. The legend wraps at the right edge, and a series with a color
// per bar has the color of its first bar in it.
func TestBarChartTheTopOfTheChartIsWhatItHas(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	titled := testDrawBarChart(t, testBarChart(pdf).SetTitle("t").SetCategories("a").AddSeries("", []float32{1}), page)
	page = NewPage(pdf, testLetterPortrait())
	untitled := testDrawBarChart(t, testBarChart(pdf).SetCategories("a").AddSeries("", []float32{1}), page)
	if testBarHeight(untitled) <= testBarHeight(titled)+10 {
		t.Errorf("%v %v", testBarHeight(untitled), testBarHeight(titled))
	}
	page = NewPage(pdf, testLetterPortrait())
	wide := testBarChart(pdf).SetCategories("a").SetDrawLegend(true)
	for i := 0; i < 8; i++ {
		wide.AddSeries("Series number "+strconv.Itoa(i), []float32{1})
	}
	content := testDrawBarChart(t, wide, page)
	if testPositionOf(t, content, "Series number 0")[1] == testPositionOf(t, content, "Series number 7")[1] {
		t.Errorf("the legend is not wrapped in %q", content)
	}
	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a", "b").SetDrawLegend(true).
		AddSeriesWithColors("own", []float32{1, 2}, []int32{0x123456, 0x654321}), page)
	swatch := strings.Index(content, "0.07 0.2 0.34 rg\n")
	if swatch < 0 || swatch >= strings.Index(content, testHex("own")) {
		t.Errorf("the swatch is not the color of the first bar in %q", content)
	}
}

// testBarHeight returns the height of the first bar the content fills in navy.
func testBarHeight(content string) float32 {
	navy := strings.Index(content, "0.12 0.23 0.37 rg\n")
	i := navy + strings.Index(content[navy:], " re\n")
	parts := strings.Split(content[strings.LastIndex(content[:i], "\n")+1:i], " ")
	height, _ := strconv.ParseFloat(parts[3], 32)
	return float32(math.Abs(height))
}

// Under vertical bars a category is written where it leaves the one before
// it room, as the numbers of the axis are: twelve months in full ran into
// each other.
func TestBarChartCategoriesThatDoNotFitAreSkipped(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	months := []string{"January", "February", "March", "April", "May", "June", "July",
		"August", "September", "October", "November", "December"}
	values := make([]float32, 12)
	for i := range values {
		values[i] = 1
	}
	content := testDrawBarChart(t, testBarChart(pdf).SetCategories(months...).AddSeries("s", values), page)
	if !strings.Contains(content, testHex("January")) || strings.Contains(content, testHex("February")) {
		t.Errorf("%q", content)
	}
	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a", "b").AddSeries("s", []float32{1, 2}), page)
	if !strings.Contains(content, testHex("a")) || !strings.Contains(content, testHex("b")) {
		t.Errorf("%q", content)
	}
}

// A line series is drawn over the bars, in ink, with a dot at each value;
// the axis reaches its values, the legend lists it and the description
// has its values after the bars'.
func TestBarChartALineSeriesIsDrawnOverTheBars(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetTitle("Sales").SetCategories("a", "b").SetDrawLegend(true).
		AddSeries("Sold", []float32{1, 2}).AddLineSeries("Target", []float32{9, float32(math.NaN())})
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, "0.13 0.15 0.17 RG\n") || !strings.Contains(content, "\n2 w\n") { // In ink
		t.Errorf("the line is not in ink: %q", content)
	}
	if !strings.Contains(content, testHex("Target")) {
		t.Errorf("the legend does not list the line: %q", content)
	}
	if !strings.Contains(content, "<"+testHex("8")+">") { // The axis reaches 9
		t.Errorf("the axis does not reach 9: %q", content)
	}
	if got := page.structures[0].altDescription; got != "Sales: Sold: a 1, b 2; Target: a 9." {
		t.Errorf("the chart is described as %q", got)
	}
	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a").AddSeries("", []float32{1}).
		AddLineSeriesWithColor("", []float32{2}, 0x123456), page)
	if !strings.Contains(content, "0.07 0.2 0.34 RG\n") {
		t.Errorf("the line is not in its color: %q", content)
	}
}
