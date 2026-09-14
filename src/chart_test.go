// chart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

func testChart(pdf *PDF) *Chart {
	font := testHelvetica(pdf)
	chart := NewChart(font, font).SetSize(300, 200)
	chart.SetLocation(50, 50)
	return chart
}

func testDrawChart(ys ...float32) string {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf)
	series := chart.AddSeries("")
	for i, y := range ys {
		series.AddPoint(float32(i+1), y)
	}
	chart.DrawOn(page)
	return testContent(page)
}

func TestChartAChartWithoutPointsDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf)
	chart.AddSeries("empty")
	testAssertXY(t, 350, 250, chart.DrawOn(page))
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestChartAllNegativeDataGetsNegativeAxisLabels(t *testing.T) {
	content := testDrawChart(-5, -2.5, -1)
	if !strings.Contains(content, testHex("-5.0")) || !strings.Contains(content, testHex("-1.0")) {
		t.Errorf("labels missing from %q", content)
	}
	if strings.Contains(content, "NaN") {
		t.Error("NaN in the content")
	}
}

func TestChartFlatDataIsDrawnWithoutNaN(t *testing.T) {
	content := testDrawChart(3, 3, 3)
	if strings.Contains(content, "NaN") {
		t.Error("NaN in the content")
	}
	if !strings.Contains(content, testHex("3.0")) || !strings.Contains(content, testHex("4.0")) {
		t.Errorf("labels missing from %q", content)
	}
}

func TestChartWholeNumberStepsGetWholeNumberLabels(t *testing.T) {
	content := testDrawChart(10, 60, 35)
	if !strings.Contains(content, testHex("60")) || strings.Contains(content, testHex("60.00")) {
		t.Errorf("labels in %q", content)
	}
}

func TestChartAPathSeriesKeepsItsStrokeWidthAndIsListedInTheLegend(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf)
	chart.AddSeries("label").SetDrawPath(true).SetShape(shape.Invisible).
		SetStrokeWidth(20).SetStrokeColor(color.Blue).
		AddPoint(1, 2).AddPoint(3, 2)
	chart.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "20 w") || !strings.Contains(content, testHex("label")) {
		t.Errorf("stroke width or legend missing from %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	chart.SetDrawLegend(false).DrawOn(page)
	if strings.Contains(testContent(page), testHex("label")) {
		t.Errorf("legend drawn in %q", testContent(page))
	}
}

func TestChartAPointWithoutAColorIsDrawnInTheColorOfItsSeries(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf)
	chart.AddSeries("").SetStrokeColor(color.Red).
		AddPoint(1, 1).
		AddPointWithMarker(NewPoint(2, 2).SetStrokeColor(color.Blue).SetShape(shape.Box))
	chart.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "1 0 0 RG") || !strings.Contains(content, "0 0 1 RG") {
		t.Errorf("marker colors missing from %q", content)
	}
}

func TestChartLabelsUseAPeriod(t *testing.T) {
	// Java sets a German default locale here; Go formats without a locale.
	content := testDrawChart(1, 2, 3)
	if !strings.Contains(content, testHex("1.25")) || strings.Contains(content, testHex("1,25")) {
		t.Errorf("labels in %q", content)
	}
}

func TestChartTheBordersTheAxisLinesTheGridColorAndTheSubtitleWorkAsInABarChart(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf)
	chart.AddSeries("").SetDrawPath(true).SetShape(shape.Invisible).
		SetStrokeWidth(3).SetStrokeColor(color.Blue).
		AddPoint(1, 1).AddPoint(2, 2)
	chart.DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, "l\ns\n") {
		t.Errorf("a border width of 0 drew a border in %q", content)
	}
	if !strings.Contains(content, "0.5 w\n") || strings.Contains(content, "1 0 0 RG") {
		t.Errorf("no axis lines or a red grid in %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	chart.SetChartBorderWidth(2).SetInnerBorderWidth(1).SetAxisLineWidth(0).
		SetGridLineColor(color.Red).SetSubtitle("Subtitle")
	chart.DrawOn(page)
	content = testContent(page)
	if n := strings.Count(content, "l\ns\n"); n != 2 {
		t.Errorf("%d borders in %q", n, content)
	}
	if strings.Contains(content, "0.5 w\n") || !strings.Contains(content, "1 0 0 RG") {
		t.Errorf("axis lines or no red grid in %q", content)
	}
	if !strings.Contains(content, testHex("Subtitle")) {
		t.Errorf("no subtitle in %q", content)
	}
}
