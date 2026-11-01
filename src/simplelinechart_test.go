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
	"github.com/edragoev1/pdfjet/v9/src/compliance"
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
	want := "Vehicles. By model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;" +
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
// were pushed down past the bottom of the panel, and with many lines up past
// its top. As many as the plot holds are drawn, the highlighted one first,
// then the others in their order.
func TestSimpleLineChartTheEndLabelsStayOverThePlot(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewSimpleLineChart(font, font).SetSize(400, 120)
	for i := 0; i < 30; i++ {
		chart.AddLine("line "+strconv.Itoa(i), []float32{10, 0})
	}
	chart.AddLine("top", []float32{0, 10}).SetHighlight(30)
	chart.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("top 10")) || !strings.Contains(content, testHex("line 0 0")) {
		t.Errorf("the highlighted label or the first is left out: %q", content)
	}
	if strings.Contains(content, testHex("line 29 0")) {
		t.Errorf("more labels than the plot holds: %q", content)
	}
	// The page is 792 high, the chart from 0 to 120 down from its top
	for i := 0; i < 30; i++ {
		text := "line " + strconv.Itoa(i) + " 0"
		if strings.Contains(content, testHex(text)) {
			if y := testPositionOf(t, content, text)[1]; y <= 792-120 || y >= 792 {
				t.Errorf("%s at %v", text, y)
			}
		}
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

// The review of 26 October 2026, the second: a chart without its fonts draws
// nothing; the value of the first point of a rise is after it and under the
// line, and that of a peak at the top of the plot under it.
func TestSimpleLineChartTheSecondReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	NewSimpleLineChart(nil, nil).AddLine("a", []float32{1, 2}).DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("a chart without its fonts drew %q", content)
	}
	NewSimpleLineChart(font, font).AddLine("big", []float32{0, 3e38}).DrawOn(page)

	page = NewPage(pdf, testLetterPortrait())
	NewSimpleLineChart(font, font).SetCategories("a", "b", "c").SetYAxisRange(0, 50).
		AddLine("x", []float32{12, 50, 23}).SetValueLabels(true).DrawOn(page)
	content := testContent(page)
	// The points, from the line drawn in gray: "x y m" of the first point,
	// "x y l" of the peak
	numbers := func(end int) []string {
		return strings.Split(content[strings.LastIndex(content[:end], "\n")+1:end], " ")
	}
	parse := func(text string) float32 {
		v, _ := strconv.ParseFloat(text, 32)
		return float32(v)
	}
	rg := strings.Index(content, "0.55 0.59 0.64 RG\n")
	m := rg + strings.Index(content[rg:], " m\n")
	start := numbers(m)
	x0, y0 := parse(start[0]), parse(start[1])
	first := testPositionOf(t, content, "12")
	if !(first[0] > x0 && first[1] < y0) {
		t.Errorf("the first value is not after its point and under it: %v by %v, %v", first, x0, y0)
	}
	peakLine := m + strings.Index(content[m:], " l\n")
	peakAt := numbers(peakLine)
	value := strings.LastIndex(content, "<"+testHex("50")+">")
	td := strings.LastIndex(content[:value], " Td\n")
	valueAt := numbers(td)
	if !(parse(valueAt[1]) < parse(peakAt[1])) {
		t.Errorf("the value of the peak at the top is not under it: %v by %v", valueAt[1], peakAt[1])
	}
}

// Values under 0.01 have the digits their axis needs, where every number of
// the axis was 0.00 and the description said from 0 to 0 (the third review of
// 26 October 2026).
func TestSimpleLineChartTinyValuesHaveTheirDigits(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewSimpleLineChart(font, font).AddLine("a", []float32{0.001, 0.004})
	chart.DrawOn(page)
	if content := testContent(page); !strings.Contains(content, testHex("a 0.004")) {
		t.Errorf("no a 0.004: %q", content)
	}
	if got := chart.getAltDescription(); !strings.Contains(got, "from 0.001 to 0.004") {
		t.Errorf("the chart is described as %q", got)
	}
}

// An axis title longer than the plot is shortened as the other texts of the
// chart are: the y title of a chart 250 points high ran past its top and its
// bottom (the review of 28 October 2026).
func TestSimpleLineChartALongAxisTitleIsShortened(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	title := "Number of battery electric vehicles registered in the state, thousands, by county"
	chart := testSimpleLineChart(pdf).SetSize(400, 250).SetXAxisTitle(title).SetYAxisTitle(title)
	chart.DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, testHex(title)) {
		t.Errorf("the axis titles are not shortened: %q", content)
	}
	if strings.Count(content, "<"+testHex("Number of battery")) != 2 {
		t.Errorf("the axis titles are not drawn: %q", content)
	}
}

// The chart keeps copies of its categories and its values: a caller that
// reused one slice for every line drew every line with the last values (the
// review of 28 October 2026).
func TestSimpleLineChartKeepsCopiesOfItsData(t *testing.T) {
	font := testHelvetica(testNewPDF())
	categories := []string{"2021", "2022"}
	values := []float32{11, 12}
	chart := NewSimpleLineChart(font, font).SetCategories(categories...).AddLine("a", values)
	values[0], values[1] = 21, 22
	chart.AddLine("b", values)
	categories[0] = "1999"
	want := "Line chart, 2021 to 2022: a from 11 to 12; b from 21 to 22."
	if got := chart.getAltDescription(); got != want {
		t.Errorf("got %q, want %q", got, want)
	}
}

// A title that ends a sentence is not followed by a comma, and a line of one
// value is described by it alone, where they were "Sales., 2019 to 2024:" and
// "a from 5 to 5" (the review of 28 October 2026).
func TestSimpleLineChartTheDescriptionReadsWell(t *testing.T) {
	font := testHelvetica(testNewPDF())
	chart := NewSimpleLineChart(font, font).SetTitle("Sales.").SetCategories("2019", "2024").
		AddLine("a", []float32{5, float32(math.NaN())}).AddLine("b", []float32{1, 2})
	if got, want := chart.getAltDescription(), "Sales. 2019 to 2024: a 5; b from 1 to 2."; got != want {
		t.Errorf("got %q, want %q", got, want)
	}
	chart = NewSimpleLineChart(font, font).SetTitle("Vehicles?").AddLine("a", []float32{1, 2})
	if got, want := chart.getAltDescription(), "Vehicles? a from 1 to 2."; got != want {
		t.Errorf("got %q, want %q", got, want)
	}
}

// The numbers of an axis written with a power of ten have the significant
// digits that write them apart: an axis of 4.84e15 to 4.85e15 was written
// 4.84e15 at each line (the review of 28 October 2026).
func TestSimpleLineChartLargeNumbersOfAnAxisAreApart(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testSimpleLineChart(pdf).SetYAxisRange(4.84e15, 4.85e15).DrawOn(page)
	content := testContent(page)
	for _, label := range []string{"4.84e15", "4.841e15", "4.849e15"} {
		if got := strings.Count(content, "<"+testHex(label)+">"); got != 1 {
			t.Errorf("%s drawn %d times: %q", label, got, content)
		}
	}
	if got := stepSignificant(4.84e15, 2e12); got != 4 {
		t.Errorf("%d significant digits", got)
	}
	if got := stepSignificant(12345, 1000); got != 3 {
		t.Errorf("%d significant digits", got)
	}
}

// testLineNote draws the chart on a page of its own and returns its content
// and its description.
func testLineNote(pdf *PDF, chart *SimpleLineChart) (string, string) {
	page := NewPage(pdf, testLetterPortrait())
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	return testContent(page), chart.getAltDescription()
}

// A value outside a range that is set cuts its line, and the chart says so, in
// print and in its description: an outlier was clipped without a word. A value
// that is no number is not outside, and a chart with none outside adds nothing.
func TestSimpleLineChartSaysHowManyValuesAreOutsideTheRange(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	nan, inf := float32(math.NaN()), float32(math.Inf(1))
	base := func() *SimpleLineChart {
		return NewSimpleLineChart(font, font).SetTitle("T").SetSize(300, 200).SetCategories("a", "b", "c")
	}
	cases := []struct {
		name  string
		chart *SimpleLineChart
		note  string
	}{
		{"no range", base().AddLine("x", []float32{1, 100, 3}), ""},
		{"on the ends", base().AddLine("x", []float32{0, 5, 10}).SetYAxisRange(0, 10), ""},
		{"not numbers", base().AddLine("x", []float32{1, nan, inf}).SetYAxisRange(0, 10), ""},
		{"past the categories", base().AddLine("x", []float32{1, 2, 3, 50}).SetYAxisRange(0, 10), ""},
		{"above", base().AddLine("x", []float32{1, 50, 3}).SetYAxisRange(0, 10), "1 value outside the range"},
		{"below", base().AddLine("x", []float32{1, 2, -50}).SetYAxisRange(0, 10), "1 value outside the range"},
		{"both", base().AddLine("x", []float32{1, 50, -50}).SetYAxisRange(0, 10), "2 values outside the range"},
		{"two lines", base().AddLine("x", []float32{1, 50, 3}).AddLine("y", []float32{50, 2, 3}).SetYAxisRange(0, 10),
			"2 values outside the range"},
	}
	for _, c := range cases {
		content, description := testLineNote(pdf, c.chart)
		if c.note == "" {
			if strings.Contains(content, testHex(" outside")) || strings.Contains(description, "outside") {
				t.Errorf("%s: a note: %q", c.name, description)
			}
			continue
		}
		if !strings.Contains(content, "<"+testHex(c.note)+">") {
			t.Errorf("%s: %q is not drawn", c.name, c.note)
		}
		if !strings.HasSuffix(description, "; "+c.note+".") {
			t.Errorf("%s: description %q", c.name, description)
		}
	}
}

// The note is in the gray of the numbers, in the small size, on a row of its
// own inside the chart, under the categories and the x title, with the plot
// that much lower and nothing else moved; its right end at the plot's.
func TestSimpleLineChartTheNoteIsUnderThePlot(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	note := "1 value outside the range"
	for _, title := range []string{"", "year"} {
		chart := func(middle float32) *SimpleLineChart {
			return NewSimpleLineChart(font, font).SetSize(300, 200).SetXAxisTitle(title).
				SetCategories("a", "b", "c").SetYAxisRange(0, 10).AddLine("x", []float32{1, middle, 3})
		}
		without, _ := testLineNote(pdf, chart(2))
		with, _ := testLineNote(pdf, chart(50))
		small := float32(0.8 * font.size)
		at := testPositionOf(t, with, note)
		c := testPositionOf(t, with, "c")
		testNear(t, "the right end", c[0]+font.StringWidth(font.size, "c")/2.0, at[0]+font.StringWidth(small, note), 0.1)
		if at[0] < 50 || at[0]+font.StringWidth(small, note) > 350 {
			t.Errorf("the note at %v", at)
		}
		testNear(t, "the baseline", 792-(250-font.size), at[1], 0.01)
		testNear(t, "the plot", testPositionOf(t, without, "c")[1]+float32(1.4*small), c[1], 0.02)
		last := c[1]
		if title != "" {
			last = testPositionOf(t, with, title)[1]
		}
		if at[1]+font.GetAscent(small) > last-font.GetDescent(font.size) {
			t.Errorf("the note at %v, the row above it at %v", at, last)
		}
		if got, want := testFillColorBefore(with, note), testFillColorBefore(with, "c"); got != want {
			t.Errorf("the color of the note %q, of the categories %q", got, want)
		}
	}
	// In a chart too narrow for it, shortened and inside
	content, description := testLineNote(pdf, NewSimpleLineChart(font, font).SetSize(110, 200).
		SetYAxisRange(0, 10).AddLine("", []float32{1, 50, 3}))
	at := testPositionOf(t, content, "1 value")
	if at[0] < 50+font.size {
		t.Errorf("the note at %v", at)
	}
	if !strings.HasSuffix(description, "; "+note+".") {
		t.Errorf("description %q", description)
	}
}

// A description that is set is followed by the note, for a screen reader to
// have what the chart prints; with none outside it stays as it is.
func TestSimpleLineChartTheNoteFollowsADescriptionThatIsSet(t *testing.T) {
	font := testHelvetica(testNewPDF())
	chart := NewSimpleLineChart(font, font).AddLine("a", []float32{1, 2}).SetAltDescription("A line")
	if got := chart.getAltDescription(); got != "A line" {
		t.Errorf("description %q", got)
	}
	chart.AddLine("b", []float32{1, 50}).SetYAxisRange(0, 10)
	if got := chart.getAltDescription(); got != "A line. 1 value outside the range." {
		t.Errorf("description %q", got)
	}
}

// In a tagged document the note is text of the chart's figure, and its
// description says it.
func TestSimpleLineChartTheNoteIsInTheFigure(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	font := testTrueTypeFont(t, doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	NewSimpleLineChart(font, font).SetSize(300, 200).SetYAxisRange(0, 10).AddLine("a", []float32{1, 50}).DrawOn(page)
	content := testContent(page)
	if strings.Count(content, "BDC\n") != 1 || strings.Count(content, "EMC\n") != 1 ||
		strings.LastIndex(content, "ET\n") > strings.LastIndex(content, "EMC\n") {
		t.Errorf("text outside the figure: %q", content)
	}
	if raw := string(doc.complete()); !strings.Contains(raw, "/Alt "+testTextString(
		"Line chart: a from 1 to 50; 1 value outside the range.")) {
		t.Error("the note is not in the description")
	}
}

// A chart barely tall enough keeps its plot, as drawn without the note, and
// its description says the number: the note cost it the plot.
func TestSimpleLineChartTheNoteNeverCostsThePlot(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	draw := func(height, middle float32) (string, string) {
		return testLineNote(pdf, NewSimpleLineChart(font, font).SetSize(300, height).
			SetYAxisRange(0, 10).AddLine("a", []float32{1, middle, 3}))
	}
	// The least height that draws, found from below
	height := float32(1)
	for !strings.Contains(func() string { c, _ := draw(height, 2); return c }(), " Td\n") {
		height += 0.25
	}
	for _, h := range []float32{height, height + 1, height + 5} {
		without, _ := draw(h, 2)
		with, description := draw(h, 50)
		if strings.Contains(with, testHex(" outside")) {
			t.Errorf("%v: the note left no room for the plot", h)
		}
		if !strings.Contains(with, "W\nn\n") || strings.Count(with, " Td\n") != strings.Count(without, " Td\n") {
			t.Errorf("%v: the plot is not drawn", h)
		}
		if !strings.HasSuffix(description, "; 1 value outside the range.") {
			t.Errorf("%v: description %q", h, description)
		}
	}
	// With the room, the note
	with, _ := draw(height+20, 50)
	if !strings.Contains(with, testHex(" outside")) {
		t.Error("no note in a chart with room for it")
	}
}
