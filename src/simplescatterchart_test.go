// simplescatterchart_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/hex"
	"math"
	"regexp"
	"strconv"
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

// A linked dot is drawn as a point of Add is, stroke for stroke, with nothing
// beside it: in a document that is not tagged it adds a link alone, described
// by its name, and the chart's description counts it and does not name it.
func TestSimpleScatterChartALinkedDotIsADotWithADescription(t *testing.T) {
	for _, withSeries := range []bool{false, true} {
		draw := func(linked bool) (string, string, string) {
			doc := testNewDoc()
			font := testHelvetica(doc.pdf)
			page := NewPage(doc.pdf, testLetterPortrait())
			chart := NewSimpleScatterChart(font, font).SetTitle("T").SetTrendLine(true).Add(1, 1).Add(3, 4)
			if withSeries {
				chart.AddSeries("Series")
			}
			if linked {
				chart.AddLinkedDot("Andorra: 84.0 years", 2, 2, "https://pdfjet.com/a")
			} else {
				chart.Add(2, 2)
			}
			alt := chart.getAltDescription()
			chart.DrawOn(page)
			return testContent(page), alt, string(doc.complete())
		}
		plain, plainAlt, _ := draw(false)
		content, alt, raw := draw(true)
		if content != plain {
			t.Errorf("series %v: not drawn as a plain point:\n%s\n%s", withSeries, content, plain)
		}
		if alt != plainAlt {
			t.Errorf("series %v: described as %q, not as %q", withSeries, alt, plainAlt)
		}
		if strings.Count(raw, "/Subtype /Link") != 1 || !strings.Contains(raw, "/Contents "+testTextString("Andorra: 84.0 years")) {
			t.Errorf("series %v: no link described by the name", withSeries)
		}
		if !strings.Contains(raw, "/URI <"+hex.EncodeToString([]byte("https://pdfjet.com/a"))+">") {
			t.Errorf("series %v: the URI of the dot is missing", withSeries)
		}
	}
}

// In a PDF/UA document a linked dot is a figure of its own in the Link that
// holds its annotation, as a linked point is, and looks as it does in the
// chart: transparent, or the outline of its series, undashed. A highlighted
// point stays over it.
func TestSimpleScatterChartALinkedDotIsAFigureInItsLink(t *testing.T) {
	for _, withSeries := range []bool{false, true} {
		doc := testNewDoc()
		doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
		font := testTrueTypeFont(t, doc.pdf)
		page := NewPage(doc.pdf, testLetterPortrait())
		page.SetStrokeDashPattern("[3 3] 0")
		chart := NewSimpleScatterChart(font, font).SetSize(300, 200).SetHighlightColor(color.Red).Add(1, 1)
		if withSeries {
			chart.AddSeries("Series")
		}
		chart.AddLinkedDot("Andorra", 2, 2, "https://pdfjet.com/a").
			AddLinkedDot("Belize", 3, 1, "https://pdfjet.com/b").
			AddLinked("Chad", 4, 3, "https://pdfjet.com/c").AddNamed("Dominica", 2, 3)
		chart.SetLocation(50, 50)
		alt := chart.getAltDescription()
		chart.DrawOn(page)
		content := testContent(page)
		raw := string(doc.complete())
		elements := testElements(raw)
		links := testLinks(raw)
		if len(links) != 3 {
			t.Fatalf("%d Link elements: %v", len(links), links)
		}
		for number, link := range links {
			figure := strings.Fields(strings.Trim(link[2], "["))[0]
			if elements[figure][0] != "Figure" || elements[figure][1] != number ||
				!strings.Contains(link[2], "/Type /OBJR") {
				t.Errorf("the Link %s holds %s, not a Figure and its annotation", number, link[2])
			}
		}
		for _, name := range []string{"Andorra", "Belize", "Chad"} {
			if !strings.Contains(raw, "/Alt "+testTextString(name)) {
				t.Errorf("no figure is described as %q", name)
			}
		}
		if strings.Contains(alt, "Andorra") || strings.Contains(alt, "Belize") {
			t.Errorf("a dot named in the chart's description: %q", alt)
		}
		// The chart, the dots' figures, the named point over them, Chad's
		chartFigure := strings.Index(content, "/Figure <</MCID 0>>")
		first := strings.Index(content, "/Figure <</MCID 1>>")
		second := strings.Index(content, "/Figure <</MCID 2>>")
		last := strings.Index(content, "/Figure <</MCID 3>>")
		artifact := strings.Index(content, "/Artifact BMC")
		if chartFigure == -1 || first == -1 || second == -1 || last == -1 || artifact < second || artifact > last {
			t.Fatalf("the order of the figures: %d, %d, %d, %d, the artifact %d", chartFigure, first, second, last, artifact)
		}
		dot := content[first:second]
		if strings.Contains(dot, "1 0 0 rg") {
			t.Errorf("the dot is highlighted: %q", dot)
		}
		if !strings.Contains(dot, "\nS\n") && !strings.Contains(dot, "\nf\n") {
			t.Errorf("the dot is not drawn: %q", dot)
		}
		if withSeries != strings.Contains(dot, "\nS\n") {
			t.Errorf("series %v: not drawn as a dot of the chart: %q", withSeries, dot)
		}
		before := content[strings.LastIndex(content[:first], "EMC"):first]
		if !strings.Contains(before, "[] 0 d\n") || !strings.Contains(before, " gs\n") {
			t.Errorf("the dots have not the state of the chart: %q", before)
		}
	}
}

// A dot's description is its name, or its URI when it has no name, and a dot
// without a URI is a plain point; one outside a range that is set is no link.
func TestSimpleScatterChartALinkedDotWithoutANameOrAURI(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	font := testTrueTypeFont(t, doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	chart := NewSimpleScatterChart(font, font).SetSize(300, 200).SetXAxisRange(0, 5).
		AddLinkedDot("", 1, 1, "https://pdfjet.com/a").AddLinkedDot("Belize", 2, 2, "").
		AddLinkedDot("Chad", 9, 3, "https://pdfjet.com/c")
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	raw := string(doc.complete())
	if strings.Count(raw, "/Subtype /Link") != 1 {
		t.Errorf("%d links, not 1", strings.Count(raw, "/Subtype /Link"))
	}
	if !strings.Contains(raw, "/Alt "+testTextString("https://pdfjet.com/a")) {
		t.Error("a dot without a name is not described by its URI")
	}
	if strings.Contains(raw, testTextString("Belize")) || strings.Contains(raw, testTextString("Chad")) {
		t.Error("a dot that is no link is described")
	}
}

// The link of a dot is at least 8 points square and holds the dot, on the page
// as written, and inside the page, in a corner too.
func TestSimpleScatterChartTheLinkOfADotCanBeHit(t *testing.T) {
	rect := regexp.MustCompile(`/Rect \[([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+)\]`)
	for _, size := range []float32{2, 9, 30} {
		doc := testNewDoc()
		font := testHelvetica(doc.pdf)
		font.SetSize(size)
		page := NewPage(doc.pdf, testLetterPortrait())
		chart := NewSimpleScatterChart(font, font).SetSize(612, 792).SetXAxisRange(0, 4).SetYAxisRange(0, 4).
			AddLinkedDot("Middle", 2, 2, "https://pdfjet.com/m").AddLinkedDot("Left", 0, 2, "https://pdfjet.com/l")
		chart.SetLocation(0, 0)
		chart.DrawOn(page)
		// Past the corners of the page, as a chart that hangs over it
		page.addAnnotation(&annotationObject{annotationType: annotationLink, x1: -1, y1: -1, x2: 0, y2: 0,
			uri: "https://pdfjet.com/c", hitSize: scatterHitSize})
		page.addAnnotation(&annotationObject{annotationType: annotationLink, x1: 612, y1: 792, x2: 613, y2: 793,
			uri: "https://pdfjet.com/d", hitSize: scatterHitSize})
		rects := rect.FindAllStringSubmatch(string(doc.complete()), -1)
		if len(rects) != 4 {
			t.Fatalf("size %v: %d links, not 4", size, len(rects))
		}
		for i, m := range rects {
			x1, _ := strconv.ParseFloat(m[1], 64)
			y1, _ := strconv.ParseFloat(m[2], 64)
			x2, _ := strconv.ParseFloat(m[3], 64)
			y2, _ := strconv.ParseFloat(m[4], 64)
			if x2-x1 < 8 || math.Abs(y2-y1) < 8 {
				t.Errorf("size %v: %v is under 8 points", size, m[0])
			}
			if i < 2 && x2-x1 < float64(float32(0.9*size)) {
				t.Errorf("size %v: %v does not hold the dot", size, m[0])
			}
			if x1 < 0 || x2 > 612 || min(y1, y2) < 0 || max(y1, y2) > 792 {
				t.Errorf("size %v: %v is past the page", size, m[0])
			}
		}
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

// testScatterNote draws the chart on a page of its own and returns its content
// and its description.
func testScatterNote(pdf *PDF, chart *SimpleScatterChart) (string, string) {
	page := NewPage(pdf, testLetterPortrait())
	chart.SetLocation(50, 50)
	chart.DrawOn(page)
	return testContent(page), chart.getAltDescription()
}

// A point outside a range that is set is not drawn, and the chart says so, in
// print and in its description: an outlier vanished without a word. A value
// that is no number is not outside, and a chart with none outside adds nothing.
func TestSimpleScatterChartSaysHowManyPointsAreOutsideTheRange(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	nan, inf := float32(math.NaN()), float32(math.Inf(1))
	base := func() *SimpleScatterChart {
		return NewSimpleScatterChart(font, font).SetTitle("T").SetSize(300, 200).Add(1, 1).Add(2, 2)
	}
	cases := []struct {
		name  string
		chart *SimpleScatterChart
		note  string
	}{
		{"no range", base().Add(100, 100), ""},
		{"on the ends", base().Add(0, 10).Add(10, 0).SetXAxisRange(0, 10).SetYAxisRange(0, 10), ""},
		{"not numbers", base().Add(nan, 50).Add(50, inf).SetXAxisRange(0, 10).SetYAxisRange(0, 10), ""},
		{"x", base().Add(50, 1).SetXAxisRange(0, 10), "1 point outside the range"},
		{"y", base().Add(1, -50).SetYAxisRange(0, 10), "1 point outside the range"},
		{"both of one", base().Add(50, 50).SetXAxisRange(0, 10).SetYAxisRange(0, 10), "1 point outside the range"},
		{"x twice", base().Add(50, 1).Add(-50, 1).SetXAxisRange(0, 10), "2 points outside the range"},
		{"y twice", base().Add(1, 50).AddNamed("far", 2, 60).SetYAxisRange(0, 10), "2 points outside the range"},
		{"x and y", base().Add(50, 1).Add(1, 50).SetXAxisRange(0, 10).SetYAxisRange(0, 10), "2 points outside the range"},
		{"a linked dot", base().AddLinkedDot("a", 50, 1, "https://pdfjet.com/a").SetXAxisRange(0, 10), "1 point outside the range"},
	}
	for _, c := range cases {
		content, description := testScatterNote(pdf, c.chart)
		if c.note == "" {
			if strings.Contains(content, testHex(" outside")) || strings.Contains(description, "outside") {
				t.Errorf("%s: a note: %q", c.name, description)
			}
			continue
		}
		if !strings.Contains(content, "<"+testHex(c.note)+">") {
			t.Errorf("%s: %q is not drawn", c.name, c.note)
		}
		if !strings.HasSuffix(description, "; "+c.note+".") {
			t.Errorf("%s: description %q", c.name, description)
		}
	}
}

// The note is in the gray of the numbers, in the small size, on a row of its
// own inside the chart, under the numbers and the x title, with the plot that
// much lower and nothing else moved; right under the plot's right end.
func TestSimpleScatterChartTheNoteIsUnderThePlot(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	note := "2 points outside the range"
	for _, title := range []string{"", "children per woman"} {
		chart := func() *SimpleScatterChart {
			return NewSimpleScatterChart(font, font).SetSize(300, 200).SetXAxisTitle(title).
				SetXAxisRange(0, 10).SetYAxisRange(0, 10).Add(1, 1).Add(2, 2)
		}
		without, _ := testScatterNote(pdf, chart())
		with, _ := testScatterNote(pdf, chart().Add(50, 1).Add(1, 50))
		small := float32(0.8 * font.size)
		at := testPositionOf(t, with, note)
		right := min(350-font.size, 350-font.size/2.0-font.StringWidth(font.size, "10")/2.0)
		testNear(t, "the right end", right, at[0]+font.StringWidth(small, note), 0.1)
		if at[0] < 50 || at[0]+font.StringWidth(small, note) > 350 {
			t.Errorf("the note at %v", at)
		}
		testNear(t, "the baseline", 792-(250-font.size), at[1], 0.01)
		number := testPositionOf(t, with, "0")
		testNear(t, "the plot", testPositionOf(t, without, "0")[1]+float32(1.4*small), number[1], 0.02)
		// The numbers of the x axis, or the title under them
		last := number[1] + font.GetAscent(font.size)/2.0 - float32(1.4*font.size)
		if title != "" {
			last = testPositionOf(t, with, title)[1]
		}
		if at[1]+font.GetAscent(small) > last-font.GetDescent(font.size) {
			t.Errorf("the note at %v, the row above it at %v", at, last)
		}
		if got, want := testFillColorBefore(with, note), testFillColorBefore(with, "0"); got != want {
			t.Errorf("the color of the note %q, of the numbers %q", got, want)
		}
	}
	// In a chart too narrow for it, shortened and inside
	content, description := testScatterNote(pdf, NewSimpleScatterChart(font, font).SetSize(90, 200).
		SetXAxisRange(0, 1).Add(0, 1).Add(5, 1).Add(6, 1))
	at := testPositionOf(t, content, "2 points")
	if at[0] < 50+font.size {
		t.Errorf("the note at %v", at)
	}
	if !strings.HasSuffix(description, "; "+note+".") {
		t.Errorf("description %q", description)
	}
}

// A description that is set is followed by the note, for a screen reader to
// have what the chart prints; with none outside it stays as it is.
func TestSimpleScatterChartTheNoteFollowsADescriptionThatIsSet(t *testing.T) {
	font := testHelvetica(testNewPDF())
	chart := NewSimpleScatterChart(font, font).Add(1, 1).Add(2, 2).SetAltDescription("Two points")
	if got := chart.getAltDescription(); got != "Two points" {
		t.Errorf("description %q", got)
	}
	chart.Add(50, 1).SetXAxisRange(0, 10)
	if got := chart.getAltDescription(); got != "Two points. 1 point outside the range." {
		t.Errorf("description %q", got)
	}
	chart.SetAltDescription("Two points.")
	if got := chart.getAltDescription(); got != "Two points. 1 point outside the range." {
		t.Errorf("description %q", got)
	}
}

// In a tagged document the note is text of the chart's figure, and its
// description says it.
func TestSimpleScatterChartTheNoteIsInTheFigure(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	font := testTrueTypeFont(t, doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	NewSimpleScatterChart(font, font).SetSize(300, 200).Add(1, 1).Add(50, 1).SetXAxisRange(0, 10).DrawOn(page)
	content := testContent(page)
	if strings.Count(content, "BDC\n") != 1 || strings.Count(content, "EMC\n") != 1 ||
		strings.LastIndex(content, "ET\n") > strings.LastIndex(content, "EMC\n") {
		t.Errorf("text outside the figure: %q", content)
	}
	if raw := string(doc.complete()); !strings.Contains(raw, "/Alt "+testTextString(
		"Scatter chart: 1 points; 1 point outside the range.")) {
		t.Error("the note is not in the description")
	}
}

// A chart barely tall enough keeps its plot, as drawn without the note, and
// its description says the number: the note cost it the plot.
func TestSimpleScatterChartTheNoteNeverCostsThePlot(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	draw := func(height, far float32) (string, string) {
		return testScatterNote(pdf, NewSimpleScatterChart(font, font).SetSize(300, height).
			SetXAxisRange(0, 10).Add(1, 1).Add(2, 2).Add(far, 1))
	}
	// The least height that draws, found from below
	height := float32(1)
	for !strings.Contains(func() string { c, _ := draw(height, 3); return c }(), " Td\n") {
		height += 0.25
	}
	for _, h := range []float32{height, height + 1, height + 5} {
		without, _ := draw(h, 3)
		with, description := draw(h, 50)
		if strings.Contains(with, testHex(" outside")) {
			t.Errorf("%v: the note left no room for the plot", h)
		}
		if strings.Count(with, " l\n") != strings.Count(without, " l\n") || len(with) < len(without)-200 {
			t.Errorf("%v: the plot is not drawn", h)
		}
		if !strings.HasSuffix(description, "; 1 point outside the range.") {
			t.Errorf("%v: description %q", h, description)
		}
	}
	// With the room, the note
	with, _ := draw(height+20, 50)
	if !strings.Contains(with, testHex(" outside")) {
		t.Error("no note in a chart with room for it")
	}
}
