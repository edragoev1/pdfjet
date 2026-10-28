// barchart_review_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

// The findings of the review of BarChart, DonutChart and ChartScale, of 28
// October 2026.

// testCountOf returns how many times the text is drawn in a core font.
func testCountOf(content, text string) int {
	return strings.Count(content, "<"+testHex(text)+">")
}

// The total of a stack below a range that is set is not written: it was
// written under the plot, or left of it by horizontal bars.
func TestBarChartATotalBelowARangeThatIsSetIsNotWritten(t *testing.T) {
	pdf := testNewPDF()
	for _, horizontal := range []bool{false, true} {
		page := NewPage(pdf, testLetterPortrait())
		chart := testBarChart(pdf).SetCategories("A", "B").SetStacked(true).SetValueLabels(true).
			SetHorizontal(horizontal).SetValueAxisRange(50, 100).
			AddSeries("x", []float32{10, 30}).AddSeries("y", []float32{10, 30})
		content := testDrawBarChart(t, chart, page)
		if testCountOf(content, "20") != 0 {
			t.Errorf("the total of A is written: %q", content)
		}
		if testCountOf(content, "30") != 1 { // B's second segment
			t.Errorf("the values of B are not written: %q", content)
		}
	}
}

// Values under 0.01 are written with the digits of the step of the axis, and
// described with theirs; the values that had their digits keep them.
func TestBarChartSmallValuesAreNotWrittenAsZero(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, testLetterPortrait())
	chart := testBarChart(pdf).SetTitle("Rates").SetCategories("A", "B").SetValueLabels(true).
		AddSeries("s", []float32{0.00012, 0.00034})
	content := testDrawBarChart(t, chart, page)
	if testCountOf(content, "0.00012") != 1 || testCountOf(content, "0.00034") != 1 {
		t.Errorf("the values are not written: %q", content)
	}
	if got := page.structures[0].altDescription; got != "Rates: s: A 0.00012, B 0.00034." {
		t.Errorf("the chart is described as %q", got)
	}

	page = NewPage(pdf, testLetterPortrait())
	chart = testBarChart(pdf).SetCategories("a", "b", "c").SetValueLabels(true).
		AddSeries("", []float32{12.5, 1234, 0.012})
	content = testDrawBarChart(t, chart, page)
	for _, label := range []string{"12.5", "1,234", "0.01"} {
		if testCountOf(content, label) != 1 {
			t.Errorf("%s is not written: %q", label, content)
		}
	}
	if got := page.structures[0].altDescription; got != "Bar chart: a 12.5, b 1,234, c 0.012." {
		t.Errorf("the chart is described as %q", got)
	}
}

// A label wider than its bar is not written inside it: a stacked one is left
// out, and one inside a grouped bar is written past its end. The labels past
// the ends of narrow bars, and the totals over narrow stacks, are written
// where they leave the one before them room.
func TestBarChartLabelsKeepToTheWidthOfTheirBars(t *testing.T) {
	pdf := testNewPDF()
	values := make([]float32, 12)
	for i := range values {
		values[i] = 12345
	}
	page := NewPage(pdf, testLetterPortrait())
	content := testDrawBarChart(t, testBarChart(pdf).SetStacked(true).SetValueLabels(true).
		AddSeries("", values).AddSeries("", values), page)
	if testCountOf(content, "12,345") != 0 {
		t.Errorf("a stacked label is wider than its bar: %q", content)
	}
	if count := testCountOf(content, "24,690"); count == 0 || count >= 12 {
		t.Errorf("%d totals of 12", count)
	}

	page = NewPage(pdf, testLetterPortrait())
	content = testDrawBarChart(t, testBarChart(pdf).SetValueLabels(true).SetValueLabelsInside(true).
		AddSeries("", values), page)
	if strings.Contains(content, "1 1 1 rg") {
		t.Errorf("a label is written inside a bar narrower than it: %q", content)
	}
	if count := testCountOf(content, "12,345"); count == 0 || count >= 12 {
		t.Errorf("%d labels of 12", count)
	}
}

// By horizontal bars a category is written where it leaves the one before
// it room: forty of them ran into each other.
func TestBarChartHorizontalCategoriesThatDoNotFitAreSkipped(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	names := make([]string, 40)
	values := make([]float32, 40)
	for i := range names {
		names[i] = "Category " + strconv.Itoa(i)
		values[i] = float32(i + 1)
	}
	content := testDrawBarChart(t, testBarChart(pdf).SetHorizontal(true).SetCategories(names...).
		AddSeries("", values), page)
	if testCountOf(content, "Category 0") != 1 || testCountOf(content, "Category 1") != 0 {
		t.Errorf("%q", content)
	}
}

// The numbers of an axis written with a power of ten have the significant
// digits that write them apart.
func TestBarChartLargeNumbersOfAnAxisAreApart(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	content := testDrawBarChart(t, testBarChart(pdf).SetCategories("a").SetValueAxisRange(4.84e15, 4.85e15).
		AddSeries("", []float32{4.845e15}), page)
	if testCountOf(content, "4.84e15") != 1 || testCountOf(content, "4.841e15") != 1 {
		t.Errorf("%q", content)
	}
	if got := format(4.841e15, 0, 0); got != "4.84e15" {
		t.Errorf("format writes %q", got)
	}
	if got := axisSignificant(1e15, 1e15, 4, 0); got != 3 {
		t.Errorf("%d significant digits", got)
	}
}

// A percentage is written inside its slice only when the slice is as long
// across the middle of the ring as the percentage is wide.
func TestDonutChartAPercentageWiderThanItsSliceIsNotWritten(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testDonutChart(pdf).SetRadii(20, 0).Add("a", 1).Add("b", 17)
	chart.DrawOn(page)
	content := testContent(page)
	if testCountOf(content, "6%") != 0 || testCountOf(content, "94%") != 1 {
		t.Errorf("%q", content)
	}
}
