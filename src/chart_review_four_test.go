// chart_review_four_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"strings"
	"testing"
)

// The findings of the fourth read-only review of the charts, of 26 October
// 2026.

// testEndOf returns the right end of the text, drawn in the font at its size.
func testEndOf(t *testing.T, content string, font *Font, text string) float32 {
	t.Helper()
	return testPositionOf(t, content, text)[0] + font.StringWidth(font.size, text)
}

func TestBarChartAxisKeepsTheDigitsOfItsStepWithNoFractionDigits(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewBarChart(font, font).SetSize(300, 300).SetCategories("a", "b").SetFractionDigits(0)
	chart.SetLocation(50, 50)
	chart.AddSeries("", []float32{1, 2})
	chart.DrawOn(page)
	if content := testContent(page); !strings.Contains(content, "<"+testHex("0.25")+">") {
		t.Errorf("not 0, 0, 1, 1: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	chart = NewBarChart(font, font).SetSize(300, 300).SetCategories("a", "b")
	chart.SetLocation(50, 50)
	chart.AddSeries("", []float32{0.002, 0.004})
	chart.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "<"+testHex("0.0020")+">") { // The digits of 0.0005
		t.Errorf("no 0.0020: %q", content)
	}
	if strings.Contains(content, "<"+testHex("0.00")+">") {
		t.Errorf("0.00: %q", content)
	}
}

func TestBarChartOfASpanPastTheLargestFloatDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b")
	chart.AddSeries("", []float32{-3e38, 3e38})
	testDrawBarChart(t, chart, page)
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestChartDataAtTheLargestFloatHasAnAxisOfItsOwn(t *testing.T) {
	axis := axisOf([]float32{math.MaxFloat32}, 0, 0, false)
	if !drawable(axis) {
		t.Error("the axis is not drawable")
	}
	if !(axis.min > 1e38 && axis.max == math.MaxFloat32) {
		t.Errorf("%v to %v", axis.min, axis.max)
	}
	if lo, hi := flatRange(5); lo != 5 || hi != 6 {
		t.Errorf("flat range of 5: %v to %v", lo, hi)
	}
}

func TestChartGroupDigitsLeavesWordsAsTheyAre(t *testing.T) {
	for label, want := range map[string]string{
		"Infinity":    "Infinity",
		"-Infinity":   "-Infinity",
		"NaN":         "NaN",
		"3e38":        "3e38",
		"-1234567.25": "-1,234,567.25",
	} {
		if got := groupDigits(label); got != want {
			t.Errorf("%q: want %q, got %q", label, want, got)
		}
	}
}

func TestSimpleBarChartWritesNoValueThatIsNotANumber(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	inf := float32(math.Inf(1))
	nan := float32(math.NaN())
	chart := NewSimpleBarChart(font, font).SetTitle("Sizes").
		Add("one", 1200).Add("two", inf).Add("three", nan)
	if got := chart.label(inf); got != "" {
		t.Errorf("infinity: %q", got)
	}
	if got := chart.label(nan); got != "" {
		t.Errorf("NaN: %q", got)
	}
	if got := chart.getAltDescription(); got != "Sizes: one 1,200." {
		t.Errorf("description %q", got)
	}
	page := NewPage(pdf, testLetterPortrait())
	chart.DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, testHex("fin")) || strings.Contains(content, testHex("NaN")) {
		t.Errorf("a value that is not a number written: %q", content)
	}
}

func TestChartStepDigitsGoPastSix(t *testing.T) {
	cases := []struct {
		name string
		got  int
		want int
	}{
		{"stepDigits(2.5e-8)", stepDigits(2.5e-8), 9},
		{"digitsOf(2.5e-8)", digitsOf(2.5e-8), 9},
		{"digitsOf(0.25)", digitsOf(0.25), 2},
		{"digitsOf(10)", digitsOf(10), 2},
		{"stepDigits(1/3)", stepDigits(float32(1) / 3), 6},
		{"digitsOf(NaN)", digitsOf(float32(math.NaN())), 2},
	}
	for _, c := range cases {
		if c.got != c.want {
			t.Errorf("%s: want %d, got %d", c.name, c.want, c.got)
		}
	}
}

func TestSimpleLineChartCategoriesStayInsideTheChart(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewSimpleLineChart(font, font).SetSize(300, 200).
		SetCategories("January 2026", "February 2026", "March 2026 and on").
		AddLine("a", []float32{1, 2, 3})
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	content := testContent(page)
	if x := testPositionOf(t, content, "January 2026")[0]; x < 50 {
		t.Errorf("the first category at %v", x)
	}
	if end := testEndOf(t, content, font, "March 2026 and on"); end > 350 {
		t.Errorf("the last category ends at %v", end)
	}
}

func TestSimpleScatterChartNumbersStayInsideTheChart(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewSimpleScatterChart(font, font).SetSize(400, 200).Add(0, 1).Add(1000000, 5)
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	if end := testEndOf(t, testContent(page), font, "1,000,000"); end > 450 {
		t.Errorf("the last number ends at %v", end)
	}
}

func TestBarChartCategoriesStayInsideTheChart(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetCategories("a", "b", "c", "d", "e", "f", "the last one")
	chart.AddSeries("", []float32{1, 2, 3, 4, 5, 6, 7})
	content := testDrawBarChart(t, chart, page)
	if end := testEndOf(t, content, font, "the last one"); end > 350 {
		t.Errorf("the last category ends at %v", end)
	}
}

func TestSimpleScatterChartDescriptionTellsWhatIsDrawn(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	// A trend line that misses the plot is not drawn, nor described
	chart := NewSimpleScatterChart(font, font).SetTitle("Flat").
		Add(1, 1).Add(2, 1).Add(3, 1).SetTrendLine(true)
	if got := chart.getAltDescription(); got != "Flat: 3 points; the trend is flat." {
		t.Errorf("description %q", got)
	}
	chart.SetYAxisRange(100, 200)
	if got := chart.getAltDescription(); got != "Flat: 0 points." {
		t.Errorf("description over a range that is set %q", got)
	}
	// The points outside a range that is set are not counted
	chart = NewSimpleScatterChart(font, font).SetTitle("Some").
		Add(1, 5).Add(2, 6).AddNamed("far", 3, 50).SetYAxisRange(0, 10)
	if got := chart.getAltDescription(); got != "Some: 2 points." {
		t.Errorf("description %q", got)
	}
}

func TestSimpleLineChartDescriptionOfOneCategory(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	chart := NewSimpleLineChart(font, font).SetTitle("Sales").
		SetCategories("2019").AddLine("a", []float32{4})
	if got := chart.getAltDescription(); !strings.HasPrefix(got, "Sales, 2019: a from") {
		t.Errorf("description %q", got)
	}
	chart.SetCategories("2019", "2020", "")
	if got := chart.getAltDescription(); !strings.HasPrefix(got, "Sales, 2019 to 2020: a from") {
		t.Errorf("description %q", got)
	}
}

func TestSimpleLineChartDrawsAValueAloneBetweenGaps(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	draw := func(values []float32) string {
		page := NewPage(pdf, testLetterPortrait())
		chart := NewSimpleLineChart(font, font).SetSize(300, 200).AddLine("a", values)
		chart.SetLocation(50, 50)
		chart.DrawOn(page)
		return testContent(page)
	}
	if count := strings.Count(draw([]float32{1, 2, 3}), " c\n"); count != 0 {
		t.Errorf("%d curves of a joined line", count)
	}
	if count := strings.Count(draw([]float32{1, float32(math.NaN()), 3}), " c\n"); count == 0 {
		t.Error("no dots of the two values alone")
	}
}

func TestBarChartMakesNoRoomForLabelsItDoesNotWrite(t *testing.T) {
	pdf := testNewPDF()
	var at [2]float32
	values := [][]float32{{5, 7}, {5, -1000000}} // The second wholly outside
	for k := 0; k < 2; k++ {
		page := NewPage(pdf, testLetterPortrait())
		chart := testBarChart(pdf).SetCategories("a", "b").SetHorizontal(true).SetValueLabels(true).
			SetValueAxisRange(0, 10)
		chart.AddSeries("", values[k])
		content := testDrawBarChart(t, chart, page)
		at[k] = testPositionOf(t, content, "10")[0]
	}
	testNear(t, "the x of 10", at[0], at[1], 0.01)
}
