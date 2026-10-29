// barchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"
	"strconv"
	"strings"
	"testing"
	"unicode/utf16"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

func testBarChart(pdf *PDF) *BarChart {
	font := testHelvetica(pdf)
	chart := NewBarChart(font, font).SetSize(300, 200)
	chart.SetLocation(50, 50)
	return chart
}

// testTextString returns the text as PDF writes a text string: UTF-16 with its
// byte order mark, in lower case hexadecimal, as appendTextString writes it.
func testTextString(text string) string {
	var b strings.Builder
	b.WriteString("<feff")
	for _, unit := range utf16.Encode([]rune(text)) {
		fmt.Fprintf(&b, "%04x", unit)
	}
	b.WriteString(">")
	return b.String()
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
	chart.AddSeries("", []float32{2.5, -1}).SetValueLabels(true)
	chart.SetFractionDigits(1)
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("2.5")) || !strings.Contains(content, testHex("-1.0")) {
		t.Errorf("value labels missing from %q", content)
	}
}

func TestBarChartAManualAxisRangeIsUsedAsGiven(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a")
	chart.AddSeries("", []float32{5}).SetValueAxisRange(0, 12) // In steps of 2
	content := testDrawBarChart(t, chart, page)
	if !strings.Contains(content, testHex("12")) || !strings.Contains(content, "<"+testHex("2")+">") {
		t.Errorf("labels missing from %q", content)
	}
	if strings.Contains(content, "<"+testHex("3")+">") {
		t.Errorf("a label of 3, not on a step of 2, in %q", content)
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
	chart.SetStacked(true).SetValueLabels(true)
	stacked := testDrawBarChart(t, chart, page)
	// The total of each stack above it
	for _, want := range []string{"75", "50", "85"} {
		if !strings.Contains(stacked, testHex(want)) {
			t.Errorf("%s missing from the stacked content %q", want, stacked)
		}
	}
}

// The first series in blue, the grid lines light gray and solid, the text gray
// or ink, and no black line or text.
func TestBarChartTheDefaultLookIsQuiet(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b").SetTitle("t").SetSubtitle("s")
	chart.AddSeries("one", []float32{20, 75}).AddSeries("two", []float32{30, 10})
	content := testDrawBarChart(t, chart, page)
	for _, want := range []string{"0.31 0.47 0.65 rg", "0.95 0.56 0.17 rg", "0.88 0.89 0.91 RG\n0.5 w\n"} {
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
	chart.SetValueAxisRange(0, 8000).SetValueLabels(true).SetValueLabelsInside(true)
	chart.SetGroupingUsed(true).SetGridLineColor(color.LightGray)
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
	// 8 steps of 10 across about 70 points: 9 points a step, where a label
	// of one decimal, 70.0, needs more
	chart := testBarChart(pdf).SetCategories("a").SetHorizontal(true).SetSize(120, 200)
	chart.AddSeries("", []float32{72.6}).SetValueAxisRange(0, 80).SetFractionDigits(1)
	chart.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("80.0")) {
		t.Errorf("the label of the last line missing from %q", content)
	}
	if strings.Contains(content, testHex("70.0")) {
		t.Errorf("70.0, between 60.0 and 80.0, would meet them, in %q", content)
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
	wide := testBarChart(pdf).SetCategories("a").SetLegend(true)
	for i := 0; i < 8; i++ {
		wide.AddSeries("Series number "+strconv.Itoa(i), []float32{1})
	}
	content := testDrawBarChart(t, wide, page)
	if testPositionOf(t, content, "Series number 0")[1] == testPositionOf(t, content, "Series number 7")[1] {
		t.Errorf("the legend is not wrapped in %q", content)
	}
	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a", "b").SetLegend(true).
		AddSeriesWithColors("own", []float32{1, 2}, []int32{0x123456, 0x654321}), page)
	swatch := strings.Index(content, "0.07 0.2 0.34 rg\n")
	if swatch < 0 || swatch >= strings.Index(content, testHex("own")) {
		t.Errorf("the swatch is not the color of the first bar in %q", content)
	}
}

// testBarHeight returns the height of the first bar the content fills in blue.
func testBarHeight(content string) float32 {
	blue := strings.Index(content, "0.31 0.47 0.65 rg\n")
	i := blue + strings.Index(content[blue:], " re\n")
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
	chart := testBarChart(pdf).SetTitle("Sales").SetCategories("a", "b").SetLegend(true).
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
	if got := page.structures[0].altDescription; got != "Sales: Sold: a 1, b 2; Target, a line: a 9." {
		t.Errorf("the chart is described as %q", got)
	}
	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a").AddSeries("", []float32{1}).
		AddLineSeriesWithColor("", []float32{2}, 0x123456), page)
	if !strings.Contains(content, "0.07 0.2 0.34 RG\n") {
		t.Errorf("the line is not in its color: %q", content)
	}
}

// The review of 26 October 2026, the second: a chart without its fonts draws
// nothing; the title, a long category under vertical bars and a legend name
// are shortened; a line series longer than the categories adds no slots; the
// line is on a white line a little wider; and a value past the rounding of the
// largest float32 is drawn on the range of the values.
func TestBarChartTheSecondReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	none := NewBarChart(nil, nil).SetCategories("a").AddSeries("", []float32{1})
	none.DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("a chart without its fonts drew %q", content)
	}

	longName := "A category name far too long to be written under its bar"
	title := "A title far too long for a chart of three hundred points, which ran past it"
	content := testDrawBarChart(t, testBarChart(pdf).SetTitle(title).SetLegend(true).SetCategories(longName, "b").
		AddSeries("Sold", []float32{1, 2}).
		AddLineSeries("Target", []float32{2, 3, 4, 5, 6}), page)
	if strings.Contains(content, testHex(longName)) || strings.Contains(content, testHex(title)) {
		t.Errorf("not shortened: %q", content)
	}
	if !strings.Contains(content, "1 1 1 RG\n3.5 w\n") {
		t.Errorf("no white line under the line: %q", content)
	}
	if got := page.structures[0].altDescription; !strings.HasSuffix(got, "Target, a line: "+longName+" 2, b 3.") {
		t.Errorf("the chart is described as %q", got)
	}

	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a", "b").SetValueLabels(true).
		AddSeries("", []float32{1e30, 3e38}), page)
	if !strings.Contains(content, testHex("3e38")) {
		t.Errorf("3e38 is not written short: %q", content)
	}
}

// The third review of 26 October 2026: a set range past the largest float
// draws nothing, where its NaN coordinates failed the document; a line
// series longer than the categories does not reach the axis past them; a
// bar below 0 has room for its value between the plot and the
// categories; the gaps are kept in their ranges; a stacked sum past the
// largest float is not written; and a bar below a set range has no value.
func TestBarChartTheThirdReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testDrawBarChart(t, testBarChart(pdf).SetCategories("a").AddSeries("", []float32{1}).
		SetValueAxisRange(-3e38, 3e38), page)
	if content := testContent(page); content != "" {
		t.Errorf("a range past the largest float drew %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	content := testDrawBarChart(t, testBarChart(pdf).SetCategories("a", "b").AddSeries("Sold", []float32{1, 2}).
		AddLineSeries("Target", []float32{2, 3, 4, 5, 6}), page)
	if strings.Contains(content, "<"+testHex("6")+">") {
		t.Errorf("the axis reaches the line past the categories: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetHorizontal(true).SetValueLabels(true).SetCategories("A").
		AddSeries("", []float32{-1234567}), page)
	label := testPositionOf(t, content, "-1,234,567")[0]
	name := testPositionOf(t, content, "A")[0]
	font := testHelvetica(pdf)
	if !(name+font.StringWidth(font.GetSize(), "A") <= label) {
		t.Errorf("the value is over the name: %v %v", name, label)
	}

	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a").SetGroupGap(1.5).SetBarGap(-2).
		AddSeries("", []float32{1}).AddSeries("", []float32{2}), page)
	if strings.Contains(content, " -") { // No negative width
		t.Errorf("a negative width: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a").SetStacked(true).SetValueLabels(true).
		AddSeries("", []float32{2e38}).AddSeries("", []float32{2e38}), page)
	if strings.Contains(content, testHex("Infinity")) || strings.Contains(content, testHex("Inf")) {
		t.Errorf("an infinite sum is written: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetCategories("a", "b").SetValueLabels(true).
		SetValueAxisRange(10, 20).AddSeries("", []float32{5, 15}), page)
	if !strings.Contains(content, "<"+testHex("15")+">") {
		t.Errorf("no 15: %q", content)
	}
	if strings.Contains(content, "<"+testHex("5")+">") {
		t.Errorf("the bar below the range has its value: %q", content)
	}
}
