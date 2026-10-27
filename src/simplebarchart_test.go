// simplebarchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
)

func testSimpleBarChart(pdf *PDF) *SimpleBarChart {
	font := testHelvetica(pdf)
	chart := NewSimpleBarChart(font, font).SetWidth(330)
	chart.SetLocation(50, 50)
	return chart.SetTitle("Statement, 12 pages").SetSubtitle("warm ms").
		Add("PDFjet", 7.3).Add("iText", 72.6).Add("OpenPDF", 18.4).Add("PDFBox", 44.1)
}

// testRectWidths returns the widths of the rectangles filled on the page, in
// the order they were drawn.
func testRectWidths(t *testing.T, content string) []float32 {
	t.Helper()
	widths := make([]float32, 0)
	for _, m := range regexp.MustCompile(`([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+) re`).FindAllStringSubmatch(content, -1) {
		w, err := strconv.ParseFloat(m[3], 32)
		if err != nil {
			t.Fatal(err)
		}
		widths = append(widths, float32(w))
	}
	return widths
}

func TestSimpleBarChartEachBarIsItsShareOfTheLargestValue(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testSimpleBarChart(pdf)
	chart.DrawOn(page)
	widths := testRectWidths(t, testContent(page))
	if len(widths) != 5 {
		t.Fatalf("%d rectangles, not the panel and 4 bars: %v", len(widths), widths)
	}
	testNear(t, "the panel", 330, widths[0], testDelta)
	longest := widths[2] // iText's 72.6
	testNear(t, "PDFjet", longest*7.3/72.6, widths[1], 0.05)
	testNear(t, "OpenPDF", longest*18.4/72.6, widths[3], 0.05)
	testNear(t, "PDFBox", longest*44.1/72.6, widths[4], 0.05)
}

func TestSimpleBarChartTheHighlightedBarHasItsOwnColor(t *testing.T) {
	for _, highlighted := range []bool{true, false} {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		chart := testSimpleBarChart(pdf).SetHighlightColor(color.Red)
		if highlighted {
			chart.SetHighlight(0)
		}
		chart.DrawOn(page)
		if got := strings.Contains(testContent(page), "1 0 0 rg"); got != highlighted {
			t.Errorf("highlighted %v: the highlight color drawn %v", highlighted, got)
		}
	}
}

func TestSimpleBarChartDescribesItselfFromItsValues(t *testing.T) {
	chart := testSimpleBarChart(testNewPDF()).SetFractionDigits(1)
	want := "Statement, 12 pages. Warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1."
	if got := chart.getAltDescription(); got != want {
		t.Errorf("got %q, want %q", got, want)
	}
	chart.SetAltDescription("Four libraries")
	if got := chart.getAltDescription(); got != "Four libraries" {
		t.Errorf("the description set: got %q", got)
	}
}

func TestSimpleBarChartValuesHaveTheDigitsTheyNeedUnlessSet(t *testing.T) {
	chart := testSimpleBarChart(testNewPDF())
	if got := chart.label(7.25); got != "7.25" {
		t.Errorf("7.25 written %q", got)
	}
	if got := chart.label(72); got != "72" {
		t.Errorf("72 written %q", got)
	}
	chart.SetFractionDigits(1)
	if got := chart.label(72); got != "72.0" {
		t.Errorf("72 with one digit written %q", got)
	}
}

func TestSimpleBarChartIsAsTallAsItsRows(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	chart := testSimpleBarChart(pdf)
	h := chart.GetHeight()
	xy := chart.DrawOn(page)
	testAssertXY(t, 380, 50+h, xy)
	taller := testSimpleBarChart(pdf).Add("Another", 10)
	testNear(t, "a row more", h+chart.rowHeight(), taller.GetHeight(), testDelta)
}

func TestSimpleBarChartANegativeValueHasNoBarButItsValue(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	chart := NewSimpleBarChart(font, font).Add("up", 5).Add("down", -2)
	chart.DrawOn(page)
	content := testContent(page)
	if widths := testRectWidths(t, content); len(widths) != 2 {
		t.Errorf("%d rectangles, not the panel and one bar: %v", len(widths), widths)
	}
	if !strings.Contains(content, testHex("-2")) {
		t.Errorf("the negative value not written in %q", content)
	}
}

func TestSimpleBarChartWithoutBarsDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	NewSimpleBarChart(font, font).SetTitle("Nothing").DrawOn(page)
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

// A name wider than two fifths of the chart is shortened with an ellipsis,
// and the bars keep their room: in a chart narrower than its names they went
// to nothing, the values past the panel (review of 26 October 2026). The
// description keeps the name whole.
func TestSimpleBarChartALongNameIsShortened(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	name := "A name far too long for the column of names of this chart"
	chart := NewSimpleBarChart(font, font).SetWidth(200).Add(name, 5).Add("b", 3)
	chart.DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, testHex(name)) {
		t.Errorf("the name is drawn whole in %q", content)
	}
	shortened := shorten(font, font.size, name, float32(0.4*(200-float32(2*font.size))))
	// The ellipsis is 85 in WinAnsi, the encoding of a core font
	start := strings.TrimSuffix(shortened, "…")
	if !strings.HasSuffix(shortened, "…") || !strings.Contains(content, testHex(start)+"85>") {
		t.Errorf("%q is not drawn in %q", shortened, content)
	}
	if !strings.Contains(chart.getAltDescription(), name) {
		t.Errorf("the description %q does not keep the name whole", chart.getAltDescription())
	}
	cases := []struct {
		text  string
		width float32
		want  string
	}{
		{"A name", font.StringWidth(10, "A n…"), "A n…"},
		{"A name", 100, "A name"},
		{"A  name", font.StringWidth(10, "A …") - 0.1, "A…"}, // The spaces before the ellipsis go
	}
	for _, c := range cases {
		if got := shorten(font, 10, c.text, c.width); got != c.want {
			t.Errorf("shorten(%q, %v) = %q, want %q", c.text, c.width, got, c.want)
		}
	}
}

// A chart without its fonts draws nothing and has no height; a title too long
// for the chart is shortened; a value of 3e38 is written short (the review of
// 26 October 2026, the second).
func TestSimpleBarChartTheSecondReviewOfTheCharts(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	none := NewSimpleBarChart(nil, nil).Add("a", 1)
	none.DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("a chart without its fonts drew %q", content)
	}
	if h := none.GetHeight(); h != 0 {
		t.Errorf("a chart without its fonts is %v high", h)
	}
	title := "A title far too long for a chart two hundred points wide"
	NewSimpleBarChart(font, font).SetWidth(200).SetTitle(title).Add("a", 3e38).DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, testHex(title)) {
		t.Errorf("the title is not shortened: %q", content)
	}
	if !strings.Contains(content, testHex("3e38")) {
		t.Errorf("3e38 is not written short: %q", content)
	}
	if got := format(1.2345e20, 0, 2); got != "1.23e20" {
		t.Errorf("1.2345e20 is %q", got)
	}
	if got := groupDigits(format(-4.5e16, 0, 2)); got != "-4.5e16" {
		t.Errorf("-4.5e16 is %q", got)
	}
}
