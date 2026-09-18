// donutchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
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
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := NewPage(pdf, testLetterPortrait())
	donut := testDonutChart(pdf).AddSlice(NewSlice(25, color.Red, "Apples")).
		AddSlice(NewSlice(75, color.Blue, "Oranges"))
	donut.DrawOn(page)
	content := testContent(page)
	if !strings.HasPrefix(content, "/Figure <</MCID 0>>\nBDC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the chart is not a figure: %q", content)
	}
	if got := testFillColorBefore(content, "Apples"); got != "0 0 0 rg" {
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
