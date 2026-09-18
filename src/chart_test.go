// chart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
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

func TestChartTheMarkerOfASeriesIsTheOneItHasWhenTheChartIsDrawn(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf)
	chart.AddSeries("").AddPoint(1, 1).AddPoint(2, 2).SetShape(shape.Invisible)
	chart.DrawOn(page)
	if content := testContent(page); strings.Contains(content, " c\n") {
		t.Errorf("the points are drawn with circles: %q", content)
	}
}

func TestChartAChartDrawnAgainHasTheRangeOfItsDataThen(t *testing.T) {
	pdf := testNewPDF()
	chart := testChart(pdf)
	series := chart.AddSeries("").AddPoint(0, 0).AddPoint(10, 10)
	chart.DrawOn(NewPage(pdf, testLetterPortrait()))
	series.AddPoint(100, 100)
	page := NewPage(pdf, testLetterPortrait())
	chart.DrawOn(page)
	if content := testContent(page); !strings.Contains(content, "<"+testHex("100")+">") {
		t.Errorf("the axes do not reach 100: %q", content)
	}
}

func TestChartAnAxisWithoutGridLinesHasTheRangeOfItsData(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf).SetXAxisMinMax(0, 10, -1).SetYAxisMinMax(0, 1000, 0)
	chart.AddSeries("").AddPoint(1, 1).AddPoint(2, 2)
	chart.DrawOn(page)
	content := testContent(page)
	for _, label := range []string{"1000", "10"} {
		if strings.Contains(content, "<"+testHex(label)+">") {
			t.Errorf("the label %s is drawn: %q", label, content)
		}
	}
	if !strings.Contains(content, "<"+testHex("2.0")+">") {
		t.Errorf("the label 2.0 is not drawn: %q", content)
	}
}

func TestChartTheSubtitleIsGray(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testChart(pdf).SetTitle("Title").SetSubtitle("Subtitle")
	chart.AddSeries("").AddPoint(1, 1).AddPoint(2, 2)
	chart.DrawOn(page)
	content := testContent(page)
	if got := testFillColorBefore(content, "Title"); got != "0 0 0 rg" {
		t.Errorf("the title is drawn with %q", got)
	}
	if got := testFillColorBefore(content, "Subtitle"); got != "0.41 0.41 0.41 rg" {
		t.Errorf("the subtitle is drawn with %q", got)
	}
}

func TestChartAChartIsAFigureDescribedByItsTitleOrItsAlternateDescription(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := NewPage(pdf, testLetterPortrait())
	titled := testChart(pdf).SetTitle("Sales")
	titled.AddSeries("").AddPoint(1, 1).AddPoint(2, 2)
	titled.DrawOn(page)
	content := testContent(page)
	if !strings.HasPrefix(content, "/Figure <</MCID 0>>\nBDC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the chart is not a figure: %q", content)
	}
	described := testChart(pdf).SetTitle("Sales").SetAltDescription("Sales rose from 1 to 2.")
	described.AddSeries("").AddPoint(1, 1).AddPoint(2, 2)
	described.DrawOn(page)
	if page.structures[0].altDescription != "Sales" {
		t.Errorf("the first chart is described as %q", page.structures[0].altDescription)
	}
	if page.structures[1].altDescription != "Sales rose from 1 to 2." {
		t.Errorf("the second chart is described as %q", page.structures[1].altDescription)
	}
}
