// review_page_test.go
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
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The drawing of a page: shapes, text lines, containers and annotations, as
// the review of the page drawing found them.

// testTaggedPage returns a page of a PDF/UA document.
func testTaggedPage() *Page {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1)
	return NewPage(pdf, letter.Portrait())
}

func TestReviewPageAPolygonInATurnedContainerIsTurned(t *testing.T) {
	page := testNewPage()
	vertices := []float32{0, 0, 20, 0, 0, 10}
	polygon := NewPolygonAnnotation()
	polygon.SetLocation(10, 10)
	polygon.SetVertices(vertices)
	container := NewContainer(100, 100)
	container.SetLocation(100, 100)
	container.SetRotation(90)
	container.Add(polygon)
	// Drawn twice, the container turns the polygon the same both times
	container.DrawOn(page)
	container.DrawOn(page)
	for _, annot := range page.annots {
		// The first vertex is 40 left of and above the center, 150 and 150,
		// and a quarter turn clockwise puts it 40 right of it and above it.
		testNear(t, "x", 190, annot.x1, testDelta)
		testNear(t, "y", 792-110, annot.y1, testDelta)
		want := []float32{0, 0, 0, 20, -10, 0}
		for i := range want {
			testNear(t, "vertex", want[i], annot.vertices[i], testDelta)
		}
	}
	if vertices[2] != 20 || vertices[3] != 0 {
		t.Errorf("the vertices of the polygon were changed: %v", vertices)
	}
}

func TestReviewPageAPointTwiceInAPathIsOffsetOnce(t *testing.T) {
	page := testNewPage()
	point := NewPoint(10, 10)
	path := NewPath()
	path.Add(point).Add(NewPoint(50, 10)).Add(point)
	path.SetLocation(100, 0)
	xy := path.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "110 782 m\n150 782 l\n110 782 l\n") {
		t.Errorf("content %q", content)
	}
	if point.x != 10 || point.y != 10 {
		t.Errorf("the point was moved to %f %f", point.x, point.y)
	}
	testAssertXY(t, 150, 10, xy)
}

func TestReviewPageAPathThatEndsOnAControlPointIsRefused(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	path := []*Point{NewPoint(10, 10), NewControlPointC(20, 20), NewControlPointC(30, 20)}
	page.DrawPath(path, pathoperator.Stroke)
	testRecorded(t, pdf, pathEndsOnControlPoint)
	if content := testContent(page); content != "" {
		t.Errorf("content %q", content)
	}

	pdf2 := testNewPDF()
	stamp := NewStamp(pdf2).SetSize(50, 50)
	stamp.DrawPath(path, pathoperator.Stroke)
	testRecorded(t, pdf2, pathEndsOnControlPoint)
	if stamp.buf.Len() != 0 {
		t.Errorf("stamp content %q", stamp.buf.String())
	}
}

func TestReviewPageTheLinkOfATurnedTextLineCoversTheText(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "Turned")
	line.SetTextRotation(90)
	line.SetURIAction("https://pdfjet.com")
	line.SetLocation(100, 200)
	line.DrawOn(page)
	annot := page.annots[0]
	// A quarter turn clockwise runs the text down the page from its location.
	testNear(t, "left", 100-font.GetDescent(12), annot.x1, testDelta)
	testNear(t, "right", 100+font.GetAscent(12), annot.x2, testDelta)
	testNear(t, "top", 792-200, annot.y1, testDelta)
	testNear(t, "bottom", 792-(200+font.StringWidth(12, "Turned")), annot.y2, testDelta)
}

func TestReviewPageASuperscriptIsRaisedOnce(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	composite := NewCompositeTextLine(100, 100)
	composite.SetFontSize(12)
	composite.AddFormula(font, "x^2")
	composite.DrawOn(page)
	two := composite.GetTextLine(1)
	// Raised by the superscript position of the base font size, 0.35 of 12
	testNear(t, "baseline", 792-(100-4.2), testPositionOf(t, testContent(page), "2")[1], testDelta)
	// Measured where it is drawn, the superscript reaches no higher than the x
	top := min(100-font.GetAscent(12), 100-4.2-font.GetAscent(two.GetFontSize()))
	testNear(t, "top", top, composite.GetMinMaxY()[0], testDelta)
	testNear(t, "ascent", 100-top, composite.GetAscent(), testDelta)
	testNear(t, "right", composite.GetWidth()+100, composite.DrawOn(nil)[0], testDelta)
	testNear(t, "superscript", 100-4.2, two.DrawOn(nil)[1], testDelta)
}

func TestReviewPageAHighlightedWordKeepsItsCombiningMarks(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "the cafe\u0301 is open")
	line.SetHighlightColors(map[string]int32{"cafe\u0301": color.Red})
	line.SetLocation(100, 100)
	line.DrawOn(page)
	// The mark is drawn with its letter, in the color of the word; a core font
	// has no mark, and draws a space for it.
	if rg := testFillColorBefore(testContent(page), "cafe "); rg != "1 0 0 rg" {
		t.Errorf("the word is drawn in %q:\n%s", rg, testContent(page))
	}
}

func TestReviewPageAShapeWithNoDescriptionIsAnArtifact(t *testing.T) {
	page := testTaggedPage()
	NewLine(10, 10, 100, 10).DrawOn(page)
	arc := NewArc()
	arc.SetLocation(50, 50)
	arc.SetRadius(10)
	arc.SetSweep(90)
	arc.DrawOn(page)
	NewCheckBox(testHelvetica(page.pdf), "").SetLocation(10, 100).DrawOn(page)
	stamp := NewStamp(page.pdf).SetSize(20, 20)
	stamp.DrawRect(0, 0, 20, 20)
	stamp.Complete()
	stamp.SetLocation(200, 200).DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, "/P <<") || strings.Count(content, "/Artifact BMC") != 4 {
		t.Errorf("content %q", content)
	}

	// Described, a line is read
	page2 := testTaggedPage()
	NewLine(10, 10, 100, 10).SetAltDescription("A rule").DrawOn(page2)
	if content := testContent(page2); !strings.Contains(content, "/P <</MCID 0>>") {
		t.Errorf("content %q", content)
	}
}

func TestReviewPageARunningFooterAndAWatermarkAreArtifacts(t *testing.T) {
	page := testTaggedPage()
	font := testHelvetica(page.pdf)
	page.AddFooter(NewTextLine(font, "Page 1"))
	page.AddHeader(NewTextLine(font, "Report"))
	page.AddWatermark(font, "DRAFT")
	content := testContent(page)
	for _, subtype := range []string{"Footer", "Header", "Watermark"} {
		if !strings.Contains(content, "/Artifact <</Type /Pagination /Subtype /"+subtype+">> BDC\n") {
			t.Errorf("no %s artifact in %q", subtype, content)
		}
	}
	if strings.Contains(content, "/P <<") {
		t.Errorf("content %q", content)
	}
}

func TestReviewPageARotationOfOneDegreeIsWrittenAsOne(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "Turned")
	line.SetTextRotation(1)
	line.SetLocation(100, 100).DrawOn(page)
	if content := testContent(page); !strings.Contains(content, "0.99985 -0.01745 0.01745 0.99985 100 692 Tm\n") {
		t.Errorf("content %q", content)
	}

	page2 := testNewPage()
	container := NewContainer(10, 10)
	container.SetRotation(1)
	container.DrawOn(page2)
	if content := testContent(page2); !strings.Contains(content, "0.99985 -0.01745 0.01745 0.99985 0 0 cm\n") {
		t.Errorf("content %q", content)
	}
}

func TestReviewPageTheSizeOfAnAnnotationFollowsItsLocation(t *testing.T) {
	page := testNewPage()
	square := NewSquareAnnotation()
	square.SetSize(60, 30)
	square.SetLocation(100, 200)
	testAssertXY(t, 160, 230, square.DrawOn(page))
	annot := page.annots[0]
	testNear(t, "x2", 160, annot.x2, testDelta)
	testNear(t, "y2", 792-230, annot.y2, testDelta)
}

func TestReviewPageACheckBoxIsTheSizeOfItsFont(t *testing.T) {
	font := testHelvetica(testNewPDF())
	checkBox := NewCheckBox(font, "Yes")
	checkBox.SetFontSize(24)
	checkBox.SetLocation(100, 100)
	xy := checkBox.DrawOn(nil)
	testNear(t, "right", 100+3*font.GetAscent(24)+font.StringWidth(24, "Yes"), xy[0], testDelta)
}

func TestReviewPageShapesLeaveThePenAsTheyFoundIt(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	page.SetPenWidth(2)
	page.SetPenColor(color.Red)
	path := NewPath().Add(NewPoint(10, 10)).Add(NewPoint(20, 20))
	path.SetStrokeWidth(5)
	path.DrawOn(page)
	NewTextLine(font, "Underlined").SetUnderline(true).SetStrikeout(true).SetLocation(10, 50).DrawOn(page)
	NewCheckBox(font, "Check").SetLocation(10, 80).DrawOn(page)
	radio := NewRadioButton(font, "Radio")
	radio.SetLocation(10, 110)
	radio.DrawOn(page)
	if page.GetPenWidth() != 2 {
		t.Errorf("pen width %f", page.GetPenWidth())
	}
	testAssertRGB(t, 1, 0, 0, page.GetPenColor())
	content := testContent(page)
	if strings.Count(content, "q\n") != 4 || strings.Count(content, "Q\n") != 4 {
		t.Errorf("content %q", content)
	}
}

func TestReviewPageAnArcOfNoSweepOrOfTooMuchIsBounded(t *testing.T) {
	page := testNewPage()
	arc := NewArc()
	arc.SetLocation(50, 50)
	arc.SetRadius(10)
	arc.SetSweep(0)
	arc.DrawOn(page)
	if content := testContent(page); content != "" {
		t.Errorf("content %q", content)
	}
	// A sweep of a billion degrees is a full turn, four curves
	arc.SetSweep(1e9)
	arc.DrawOn(page)
	if n := strings.Count(testContent(page), " c\n"); n != 4 {
		t.Errorf("%d curves", n)
	}

	pdf := testNewPDF()
	page2 := NewPage(pdf, letter.Portrait())
	for _, sweep := range []float32{float32(math.NaN()), float32(math.Inf(1))} {
		arc.SetSweep(sweep)
		arc.DrawOn(page2)
		page2.AddArcToPath(50, 50, 10, 10, 0, sweep)
	}
	testRecorded(t, pdf, "The sweep of an arc must be a finite number of degrees.")
	if content := testContent(page2); content != "" {
		t.Errorf("content %q", content)
	}
}

func TestReviewPageTheBoundingBoxAfterAFigureIsNotItsOwn(t *testing.T) {
	page := testTaggedPage()
	page.AddBDC(structelem.Figure, "", "", "A figure")
	page.SetFigureBoundingBox(10, 10, 20, 20)
	page.AddEMC()
	page.SetFigureBoundingBox(50, 50, 5, 5)
	if attributes := page.structures[0].attributes; attributes != "<</O /Layout /BBox [10 762 30 782]>>" {
		t.Errorf("attributes %q", attributes)
	}
}

func TestReviewPageAnEmptyTextLineAddsItsDestination(t *testing.T) {
	page := testNewPage()
	NewTextLine(testHelvetica(page.pdf), "").SetDestination("top").SetLocation(10, 100).DrawOn(page)
	if len(page.destinations) != 1 || page.destinations[0].name != "top" {
		t.Errorf("destinations %v", page.destinations)
	}
}

func TestReviewPageAFormWithoutItsFontsIsRefused(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	form := NewForm([]*Field{NewField(0, "Name", "Value")})
	form.SetLocation(10, 10)
	form.DrawOn(page)
	testRecorded(t, pdf, "A form needs a label font and a value font: SetLabelFont and SetValueFont.")
}

func TestReviewPageTheLinksInNestedContainersAreWhereTheirTextIs(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "Link")
	line.SetURIAction("https://pdfjet.com")
	line.SetLocation(1, 20)
	inner := NewContainer(50, 50)
	inner.SetLocation(5, 5)
	inner.Add(line)
	middle := NewContainer(100, 100)
	middle.SetLocation(10, 10)
	middle.Add(inner)
	outer := NewContainer(200, 200)
	outer.SetLocation(100, 100)
	outer.Add(middle)
	outer.DrawOn(page)
	annot := page.annots[0]
	testNear(t, "left", 116, annot.x1, testDelta)
	testNear(t, "top", 792-(135-font.GetAscent(12)), annot.y1, testDelta)

	// A figure is where it is drawn
	page2 := testTaggedPage()
	barcode := NewBarcode(CODE_128, "12345")
	barcode.SetAltDescription("12345")
	barcode.SetLocation(110, 110).DrawOn(page2)
	page3 := testTaggedPage()
	barcode = NewBarcode(CODE_128, "12345")
	barcode.SetAltDescription("12345")
	barcode.SetLocation(10, 10)
	NewContainer(100, 100).Add(barcode).SetLocation(100, 100).DrawOn(page3)
	if page2.structures[0].attributes != page3.structures[0].attributes {
		t.Errorf("%q, not %q", page3.structures[0].attributes, page2.structures[0].attributes)
	}

	// Turned a quarter, the link of a text line is turned with it
	page4 := NewPage(page.pdf, letter.Portrait())
	link := NewTextLine(font, "Link")
	link.SetURIAction("https://pdfjet.com")
	link.SetLocation(0, 50)
	turned := NewContainer(100, 100)
	turned.SetLocation(100, 100)
	turned.SetRotation(90)
	turned.Add(link)
	turned.DrawOn(page4)
	annot = page4.annots[0]
	// The text runs down the page, from 50 left of the center to 50 right of it
	testNear(t, "left", 150-font.GetDescent(12), annot.x1, testDelta)
	testNear(t, "top", 792-100, annot.y1, testDelta)
	testNear(t, "right", 150+font.GetAscent(12), annot.x2, testDelta)
	testNear(t, "bottom", 792-(100+font.StringWidth(12, "Link")), annot.y2, testDelta)
}

func TestReviewPageADashPatternWithAVerticalTabIsRefused(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	page.SetStrokeDashPattern("[3\v3] 0")
	testRecorded(t, pdf, "The dash pattern \"[3\v3] 0\" is not an array of non-negative numbers, "+
		"not all zero, followed by a phase, such as \"[3 3] 0\".")
}

func TestReviewPageTheSpacesOfUnicodeAreSpaces(t *testing.T) {
	page := testTaggedPage()
	font := testHelvetica(page.pdf)
	NewTextLine(font, "\u3000Annual\u3000 report\u00a0").SetStructureType(structelem.H1).SetLocation(10, 50).DrawOn(page)
	if title := page.pdf.headings[0].title; title != "Annual report" {
		t.Errorf("title %q", title)
	}
	page.AddBDC(structelem.Figure, "", "", "\u3000")
	testRecorded(t, page.pdf, "A figure of a tagged document, PDF/UA or PDF/A of level A, needs an alternative description.")
}

func TestReviewPageAnEmptyURIIsNoLink(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	NewTextLine(font, "Text").SetURIAction("").SetGoToAction("").SetLocation(10, 50).DrawOn(page)
	NewTextBlock(font, "Text").SetURIAction("").SetLocation(10, 100).DrawOn(page)
	if len(page.annots) != 0 {
		t.Errorf("%d annotations", len(page.annots))
	}
}

func TestReviewPageAKeywordIsLowerCasedWhateverTheLocale(t *testing.T) {
	page := testNewPage()
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "LINK")
	line.SetHighlightColors(map[string]int32{"link": color.Red})
	line.SetLocation(10, 50).DrawOn(page)
	if rg := testFillColorBefore(testContent(page), "LINK"); rg != "1 0 0 rg" {
		t.Errorf("the word is drawn in %q", rg)
	}
}

func TestReviewPageAKeywordIsMatchedByItsCodePoints(t *testing.T) {
	// A keyword of an e with an acute accent is not the same word as one of an
	// e and a combining accent.
	page := testNewPage()
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "the cafe\u0301 is open")
	line.SetHighlightColors(map[string]int32{"caf\u00e9": color.Red})
	line.SetLocation(100, 100).DrawOn(page)
	if rg := testFillColorBefore(testContent(page), "cafe "); rg != "0 0 0 rg" {
		t.Errorf("the word is drawn in %q", rg)
	}

	// Nor is a description of the one the text of the other
	page2 := testTaggedPage()
	described := NewTextLine(testHelvetica(page2.pdf), "cafe\u0301")
	described.SetAltDescription("caf\u00e9")
	described.SetLocation(100, 100).DrawOn(page2)
	if alt := page2.structures[0].altDescription; alt != "caf\u00e9" {
		t.Errorf("alt %q", alt)
	}
}

func TestReviewPageAShortTransformIsRefused(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	page.Transform([]float32{1, 0, 0})
	testRecorded(t, pdf, transformCount)
	if content := testContent(page); content != "" {
		t.Errorf("content %q", content)
	}
	// The most negative and the most positive angles are angles too
	page.SetTextRotation(math.MinInt)
	page.SetTextRotation(math.MaxInt)
}
