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

func testSeries(ys ...float32) [][]*Point {
	points := make([]*Point, 0)
	for i, y := range ys {
		points = append(points, NewPoint(float32(i+1), y))
	}
	return [][]*Point{points}
}

func testDrawChart(data [][]*Point) string {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	chart := NewChart(font, font).SetSize(300, 200).SetData(data)
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	return testContent(page)
}

func TestChartAChartWithoutPointsDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	chart := NewChart(font, font).SetSize(300, 200).SetData([][]*Point{})
	chart.SetLocation(50, 50)
	testAssertXY(t, 350, 250, chart.DrawOn(page))
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestChartAllNegativeDataGetsNegativeAxisLabels(t *testing.T) {
	content := testDrawChart(testSeries(-5, -2.5, -1))
	if !strings.Contains(content, testHex("-5.0")) || !strings.Contains(content, testHex("-1.0")) {
		t.Errorf("labels missing from %q", content)
	}
	if strings.Contains(content, "NaN") {
		t.Error("NaN in the content")
	}
}

func TestChartFlatDataIsDrawnWithoutNaN(t *testing.T) {
	content := testDrawChart(testSeries(3, 3, 3))
	if strings.Contains(content, "NaN") {
		t.Error("NaN in the content")
	}
	if !strings.Contains(content, testHex("3.0")) || !strings.Contains(content, testHex("4.0")) {
		t.Errorf("labels missing from %q", content)
	}
}

func TestChartWholeNumberStepsGetWholeNumberLabels(t *testing.T) {
	content := testDrawChart(testSeries(10, 60, 35))
	if !strings.Contains(content, testHex("60")) || strings.Contains(content, testHex("60.00")) {
		t.Errorf("labels in %q", content)
	}
}

func TestChartAPathSeriesKeepsItsStrokeWidthAndWritesItsText(t *testing.T) {
	p1 := NewPoint(1, 2).SetDrawPath(true).SetShape(shape.Invisible)
	p1.SetStrokeWidth(20).SetStrokeColor(color.Blue).SetText("label")
	p2 := NewPoint(3, 2).SetShape(shape.Invisible)
	content := testDrawChart([][]*Point{{p1, p2}})
	if !strings.Contains(content, "20 w") || !strings.Contains(content, testHex("label")) {
		t.Errorf("stroke width or text missing from %q", content)
	}
}

func TestChartLabelsUseAPeriod(t *testing.T) {
	// Java sets a German default locale here; Go formats without a locale.
	content := testDrawChart(testSeries(1, 2, 3))
	if !strings.Contains(content, testHex("1.25")) || strings.Contains(content, testHex("1,25")) {
		t.Errorf("labels in %q", content)
	}
}
