// simplescatterchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

func testSimpleScatterChart(pdf *PDF) *SimpleScatterChart {
	font := testHelvetica(pdf)
	chart := NewSimpleScatterChart(font, font).SetSize(400, 300)
	chart.SetLocation(50, 50)
	return chart.SetTitle("Phones and internet").SetSubtitle("by country").
		SetXAxisTitle("phones").SetYAxisTitle("internet").
		Add(0.1, 5).Add(0.5, 30).AddNamed("Canada", 0.5, 65).Add(1.2, 70)
}

func TestSimpleScatterChartTheTrendIsTheLeastSquaresLine(t *testing.T) {
	points := []scatterPoint{{x: 0, y: 1}, {x: 1, y: 3}, {x: 2, y: 5}, {x: 3, y: 7}}
	slope, intercept, ok := trend(points)
	if !ok || math.Abs(slope-2) > 1e-9 || math.Abs(intercept-1) > 1e-9 {
		t.Errorf("y = 2x + 1 fitted as y = %vx + %v, %v", slope, intercept, ok)
	}
	if _, _, ok := trend([]scatterPoint{{x: 1, y: 1}, {x: 1, y: 2}}); ok {
		t.Error("points at one x have no trend line")
	}
	if _, _, ok := trend([]scatterPoint{{x: 1, y: 1}}); ok {
		t.Error("one point has no trend line")
	}
}

func TestSimpleScatterChartANamedPointIsHighlighted(t *testing.T) {
	for _, named := range []bool{true, false} {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		font := testHelvetica(pdf)
		chart := NewSimpleScatterChart(font, font).SetHighlightColor(color.Red).Add(1, 1).Add(2, 3)
		if named {
			chart.AddNamed("Canada", 1.5, 2)
		}
		chart.DrawOn(page)
		content := testContent(page)
		if got := strings.Contains(content, "1 0 0 rg"); got != named {
			t.Errorf("named %v: the highlight color drawn %v", named, got)
		}
		if got := strings.Contains(content, testHex("Canada")); got != named {
			t.Errorf("named %v: the name drawn %v", named, got)
		}
	}
}

func TestSimpleScatterChartDescribesItselfFromItsData(t *testing.T) {
	chart := testSimpleScatterChart(testNewPDF()).Add(float32(math.NaN()), 1).SetTrendLine(true)
	want := "Phones and internet, by country: 4 points, internet against phones; Canada 0.5 and 65; the trend rises."
	if got := chart.getAltDescription(); got != want {
		t.Errorf("got %q, want %q", got, want)
	}
	chart.SetAltDescription("Four countries")
	if got := chart.getAltDescription(); got != "Four countries" {
		t.Errorf("the description set: got %q", got)
	}
}

func TestSimpleScatterChartAnAxisRangeSetIsUsed(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testSimpleScatterChart(pdf).SetYAxisRange(0, 200).DrawOn(page)
	if !strings.Contains(testContent(page), testHex("200")) {
		t.Errorf("200, the top of the range set, not drawn in %q", testContent(page))
	}
}

func TestSimpleScatterChartTheDotsAreTransparentButInPDFA1(t *testing.T) {
	for _, level := range []compliance.Compliance{compliance.PDF_UA_1, compliance.PDF_A_1B} {
		doc := testNewDoc()
		doc.pdf.SetCompliance(level)
		page := NewPage(doc.pdf, testLetterPortrait())
		testAllowCoreFonts(t)
		testSimpleScatterChart(doc.pdf).DrawOn(page)
		transparent := strings.Contains(testContent(page), " gs")
		if want := level != compliance.PDF_A_1B; transparent != want {
			t.Errorf("%v: transparent %v, want %v", level, transparent, want)
		}
	}
}

func TestSimpleScatterChartWithoutPointsDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	xy := NewSimpleScatterChart(font, font).SetTitle("Nothing").Add(float32(math.NaN()), 1).DrawOn(page)
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
	testAssertXY(t, 400, 300, xy)
}
