// barchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
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
	if !strings.Contains(stacked, testHex("90")) || !strings.Contains(stacked, testHex("75")) {
		t.Errorf("labels missing from the stacked content %q", stacked)
	}
}
