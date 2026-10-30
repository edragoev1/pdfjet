// simplescatterchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/hex"
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
	return chart.SetTitle("Life expectancy and children per woman").SetSubtitle("by country").
		SetXAxisTitle("children per woman").SetYAxisTitle("life expectancy").
		Add(5.2, 61).Add(3, 70).AddNamed("Canada", 1.3, 82).Add(2, 76)
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
	want := "Life expectancy and children per woman. By country: 4 points, life expectancy " +
		"against children per woman; Canada 1.3 and 82; the trend falls."
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

// In a PDF/UA document a highlighted point that is a link is a figure of its
// own, described by its name, in the Link that holds its annotation; a point
// that is not a link stays in the chart's figure.
func TestSimpleScatterChartALinkedPointIsAFigureInItsLink(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	font := testTrueTypeFont(t, doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	chart := NewSimpleScatterChart(font, font).SetSize(300, 200).
		Add(1, 1).AddLinked("Andorra", 2, 2, "https://pdfjet.com/a").
		AddLinked("Belize", 3, 1, "https://pdfjet.com/b").AddNamed("Chad", 4, 3)
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	raw := string(doc.complete())
	elements := testElements(raw)
	links := testLinks(raw)
	if len(links) != 2 {
		t.Fatalf("%d Link elements: %v", len(links), links)
	}
	for number, link := range links {
		figure := strings.Fields(strings.Trim(link[2], "["))[0]
		if elements[figure][0] != "Figure" || elements[figure][1] != number ||
			!strings.Contains(link[2], "/Type /OBJR") {
			t.Errorf("the Link %s holds %s, not a Figure and its annotation", number, link[2])
		}
	}
	for _, name := range []string{"Andorra", "Belize"} {
		if !strings.Contains(raw, "/Alt "+testTextString(name)) {
			t.Errorf("no figure is described as %q", name)
		}
	}
	if strings.Count(raw, "/Subtype /Link") != 2 {
		t.Errorf("%d link annotations, not 2", strings.Count(raw, "/Subtype /Link"))
	}
}

// In a document that is not tagged, a linked point is drawn in the chart, its
// dot and its name the link, and no elements are made.
func TestSimpleScatterChartALinkedPointOfADocumentNotTaggedIsDrawnInTheChart(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	NewSimpleScatterChart(font, font).Add(1, 1).AddLinked("Andorra", 2, 2, "https://pdfjet.com/a").DrawOn(page)
	raw := string(doc.complete())
	if strings.Contains(raw, "/StructElem") || strings.Count(raw, "/Subtype /Link") != 1 {
		t.Error("the link of the point is not drawn in the chart")
	}
	if !strings.Contains(raw, "/URI <"+hex.EncodeToString([]byte("https://pdfjet.com/a"))+">") {
		t.Error("the URI of the point is missing")
	}
}

// A point outside a range that is set is not drawn; a point far past it, or
// points whose span is past the largest float, are drawn without a panic or
// an error that lost the document (review of 26 October 2026).
func TestSimpleScatterChartAPointOutsideTheRangeIsNotDrawn(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	testSimpleScatterChart(pdf).SetXAxisRange(0, 2).SetYAxisRange(0, 100).AddNamed("Far", 20, 30).
		Add(1e37, 1e37).DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("Canada")) || strings.Contains(content, testHex("Far")) {
		t.Errorf("%q", content)
	}
	NewSimpleScatterChart(font, font).Add(-3e38, -3e38).Add(3e38, 3e38).DrawOn(NewPage(pdf, testLetterPortrait()))
}

// The name of a highlighted point is on a patch of the panel's color, so that
// the gray dots under it do not show through.
func TestSimpleScatterChartANameIsOnAPatchOfThePanel(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	testSimpleScatterChart(pdf).DrawOn(page)
	content := testContent(page)
	name := strings.Index(content, "<"+testHex("Canada"))
	patch := strings.LastIndex(content[:name], "0.95 0.95 0.96 rg\n")
	if patch <= strings.Index(content, "0.12 0.23 0.37 rg\n") { // After the navy dot
		t.Errorf("no patch after the navy dot in %q", content)
	}
	if !strings.Contains(content[patch:name], " re\n") {
		t.Errorf("no patch under the name in %q", content)
	}
}

// A chart without its fonts draws nothing; a long name of a point is
// shortened and kept in the plot (the review of 26 October 2026, the second).
func TestSimpleScatterChartTheSecondReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	NewSimpleScatterChart(nil, nil).Add(1, 1).DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("a chart without its fonts drew %q", content)
	}
	NewSimpleScatterChart(font, font).Add(0, 0).Add(3e38, 3e38).DrawOn(page)
	name := "A name far too long to be written whole beside its point in the chart"
	testSimpleScatterChart(pdf).AddNamed(name, 3, 65).DrawOn(page)
	if content := testContent(page); strings.Contains(content, testHex(name)) {
		t.Errorf("the name is not shortened: %q", content)
	}
}

// The third review of 26 October 2026: no trend line where it misses the
// plot; an empty URI is no link; values under 0.01 have the digits their axis
// needs; numbers are grouped in thousands; and points whose span is past the
// largest float draw nothing.
func TestSimpleScatterChartTheThirdReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	NewSimpleScatterChart(font, font).SetYAxisRange(0, 10).Add(0, 50).Add(10, 50).SetTrendLine(true).DrawOn(page)
	if content := testContent(page); strings.Contains(content, testHex("trend")) {
		t.Errorf("a trend line outside the plot: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	NewSimpleScatterChart(font, font).AddLinked("A", 1, 2, "").Add(2, 3).DrawOn(page)
	if len(page.annots) != 0 {
		t.Errorf("an empty URI made %d links", len(page.annots))
	}

	page = NewPage(pdf, testLetterPortrait())
	tiny := NewSimpleScatterChart(font, font).Add(0.001, 1).Add(0.004, 2)
	tiny.DrawOn(page)
	if content := testContent(page); !strings.Contains(content, testHex("0.001")) {
		t.Errorf("no 0.001: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	NewSimpleScatterChart(font, font).AddNamed("Big", 23507, 1).Add(1000, 2).DrawOn(page)
	if content := testContent(page); !strings.Contains(content, testHex("25,000")) {
		t.Errorf("not grouped: %q", content)
	}

	page = NewPage(pdf, testLetterPortrait())
	NewSimpleScatterChart(font, font).Add(-3e38, -3e38).Add(3e38, 3e38).DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("a span past the largest float drew %q", content)
	}
}

// An axis title longer than the plot is shortened as the other texts of the
// chart are, and the description keeps it whole; a title that ends a sentence
// is not followed by a colon (the review of 28 October 2026).
func TestSimpleScatterChartALongAxisTitleIsShortened(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	title := "Number of battery electric vehicles registered in the state, thousands, by county"
	chart := testSimpleScatterChart(pdf).SetSize(400, 250).SetXAxisTitle(title).SetYAxisTitle(title)
	chart.DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, testHex(title)) {
		t.Errorf("the axis titles are not shortened: %q", content)
	}
	if strings.Count(content, "<"+testHex("Number of battery")) != 2 {
		t.Errorf("the axis titles are not drawn: %q", content)
	}
	if got := chart.getAltDescription(); !strings.Contains(got, title+" against "+title) {
		t.Errorf("the description does not keep the titles whole: %q", got)
	}
	chart = NewSimpleScatterChart(chart.f1, chart.f2).SetTitle("Life expectancy?").Add(1, 2).Add(2, 3)
	if got, want := chart.getAltDescription(), "Life expectancy? 2 points."; got != want {
		t.Errorf("got %q, want %q", got, want)
	}
}

// The end of an axis rounded to steps is never inside the values: 591 steps
// of 0.1 come to 59.100002 in float32, and a point at 59.1 was not drawn (the
// review of 30 October 2026, E1).
func TestSimpleScatterChartAnAxisHoldsItsValues(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	chart := NewSimpleScatterChart(font, font).SetSize(400, 300).AddNamed("Only", 59.1, 59.1)
	chart.DrawOn(page)
	if content := testContent(page); !strings.Contains(content, testHex("Only")) {
		t.Errorf("the point at 59.1 is not drawn: %q", content)
	}
	for i := 1; i <= 20000; i++ {
		value := float32(float32(i) * 0.1)
		axis := axisOf([]float32{value}, 0, 0, false)
		if !onAxis(value, axis) {
			t.Fatalf("%v is not on the axis of %v to %v", value, axis.min, axis.max)
		}
		lo, hi := flatRange(value)
		round := roundMaxAndMinValues(hi, lo)
		if round.minValue > lo || round.maxValue < hi {
			t.Fatalf("%v to %v rounded to %v to %v", lo, hi, round.minValue, round.maxValue)
		}
	}
}
