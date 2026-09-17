// page_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
)

func TestPageANewPageTracksTheDefaultGraphicsState(t *testing.T) {
	page := testNewPage()
	testNear(t, "pen width", 1, page.GetPenWidth(), 0)
	testAssertRGB(t, 0, 0, 0, page.GetPenColor())
	testAssertRGB(t, 0, 0, 0, page.GetBrushColor())
}

func TestPageAppendWritesStringsInUtf8AndIntegersAndNumbersInDecimal(t *testing.T) {
	euros := strings.Repeat("€", 256)
	digits := strings.Repeat("0123456789", 1000) + "é"
	page := testNewPage()
	page.appendString("BT é≠\U0001F600 ")
	page.appendInteger(-2147483648)
	page.appendByte(' ')
	page.appendInteger(2147483647)
	page.appendByte(' ')
	page.appendFloat32(-8388607.5)
	page.appendByte(' ')
	page.appendFloat32(0.125)
	page.appendByte(' ')
	page.appendString(euros)
	page.appendByte(' ')
	page.appendString(digits)
	want := "BT é≠\U0001F600 -2147483648 2147483647 -8388607.5 0.13 " + euros + " " + digits
	if got := string(page.buf); got != want {
		t.Errorf("content of %d bytes differs from the %d bytes wanted", len(got), len(want))
	}
}

func TestPageCmykSettersWriteCmykAndTrackTheRgbOfTheStandard(t *testing.T) {
	page := testNewPage()
	page.SetPenColorCMYK(0, 1, 1, 0)
	page.SetBrushColorCMYK(0, 0, 0, 1)
	if got := testContent(page); got != "0 1 1 0 K\n0 0 0 1 k\n" {
		t.Errorf("content %q", got)
	}
	testAssertRGB(t, 1, 0, 0, page.GetPenColor())
	testAssertRGB(t, 0, 0, 0, page.GetBrushColor())
}

func TestPageRestoreGraphicsStateRestoresTheTrackedState(t *testing.T) {
	page := testNewPage()
	page.SetPenColor(0xFF0000)
	page.SaveGraphicsState()
	page.SetPenWidth(3)
	page.SetPenColor(0x00FF00)
	page.RestoreGraphicsState()
	testNear(t, "pen width", 1, page.GetPenWidth(), 0)
	testAssertRGB(t, 1, 0, 0, page.GetPenColor())
	if content := testContent(page); !strings.HasSuffix(content, "q\n3 w\n0 1 0 RG\nQ\n") {
		t.Errorf("content %q", content)
	}
}

func TestPageGettersReturnCopies(t *testing.T) {
	page := testNewPage()
	pen := page.GetPenColor()
	pen[0] = 1
	testAssertRGB(t, 0, 0, 0, page.GetPenColor())
	page.DrawLine(0, 0, 10, 10)
	page.GetContent()[0] = 'X'
	if testContent(page)[0] == 'X' {
		t.Error("GetContent returns the live buffer")
	}
}

func TestPageDrawLineWritesAStrokedPathWithTheYFlipped(t *testing.T) {
	page := testNewPage()
	page.DrawLine(10, 20, 30, 40)
	if content := testContent(page); !strings.Contains(content, "10 772 m\n30 752 l\nS\n") {
		t.Errorf("content %q", content)
	}
}

func TestPageAGoToLinkPointsAtItsDestinationOnAnotherPage(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page1 := NewPage(doc.pdf, testLetterPortrait())
	NewTextLine(font, "Go").SetGoToAction("there").SetLocation(50, 50).DrawOn(page1)
	NewRect(10, 10, 20, 20).SetGoToAction("there").DrawOn(page1)
	NewTextLine(font, "Nowhere").SetGoToAction("missing").SetLocation(50, 100).DrawOn(page1)
	page2 := NewPage(doc.pdf, testLetterPortrait())
	page2.AddDestinationAt("there", 30, 100)
	file := string(doc.complete())
	// The text and the rect link to the destination, 100 points down page 2; the
	// link to a destination no page has is written without a /Dest
	if strings.Count(file, "/Dest [") != 2 || strings.Count(file, "/XYZ 30 692 0]") != 2 ||
		strings.Count(file, "/Subtype /Link") != 3 {
		t.Errorf("links in %q", file)
	}
}

func TestPageATextLineAddsItsDestinationWhenItIsDrawn(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page1 := NewPage(doc.pdf, testLetterPortrait())
	NewTextLine(font, "Go").SetGoToAction("there").SetLocation(50, 50).DrawOn(page1)
	page2 := NewPage(doc.pdf, testLetterPortrait())
	target := NewTextLine(font, "There").SetDestination("there")
	target.SetLocation(30, 100+font.size)
	target.DrawOn(page2)
	file := string(doc.complete())
	// The destination is at the left edge of page 2, a font size above the baseline.
	if target.GetDestination() != "there" || strings.Count(file, "/Dest [") != 1 ||
		strings.Count(file, "/XYZ 0 692 0]") != 1 {
		t.Errorf("destination in %q", file)
	}
}

func TestPagePositiveAnglesTurnClockwise(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	// y grows downward, so text turned a quarter clockwise runs down the page
	// and text turned a quarter counterclockwise runs up from its location.
	down := NewTextLine(font, "Down").SetTextRotation(90).SetLocation(100, 100).DrawOn(page)
	up := NewTextLine(font, "Up").SetTextRotation(-90).SetLocation(300, 100).DrawOn(page)
	if down[1] <= 100+font.StringWidth(font.size, "Down")/2 {
		t.Errorf("down %v", down[1])
	}
	if up[1] < 99.99 || up[1] > 100.01 {
		t.Errorf("up %v", up[1])
	}
	page.SetRotation(90)
	file := string(doc.complete())
	if !strings.Contains(file, "/Rotate 90") {
		t.Errorf("no /Rotate 90")
	}
}

func TestPageAPathWithFewerThanTwoPointsPaintsNothing(t *testing.T) {
	page := testNewPage()
	path := make([]*Point, 0)
	page.DrawPath(path, pathoperator.Stroke)
	path = append(path, NewPoint(10, 10))
	page.DrawPath(path, pathoperator.Stroke)
	if got := testContent(page); got != "" {
		t.Errorf("content %q", got)
	}
}

func TestPageTheShapesAreDrawnOrFilled(t *testing.T) {
	page := testNewPage()
	page.DrawCircle(50, 50, 10)
	page.FillCircle(50, 50, 10)
	page.DrawRoundedRect(10, 10, 100, 50, 5, 5)
	page.FillRoundedRect(10, 10, 100, 50, 5, 5)
	// A drawn shape is stroked with S, and a filled one filled with f.
	painted := []string{}
	for _, token := range strings.Fields(testContent(page)) {
		if token == "S" || token == "f" {
			painted = append(painted, token)
		}
	}
	if strings.Join(painted, " ") != "S f S f" {
		t.Errorf("operators %v in %q", painted, testContent(page))
	}
}

func TestPageSetTextRenderingModeRejectsAModeOutsideTheRange(t *testing.T) {
	page := testNewPage()
	if _, err := page.SetTextRenderingMode(3); err != nil {
		t.Error(err)
	}
	if _, err := page.SetTextRenderingMode(8); err == nil {
		t.Error("no error for mode 8")
	}
}

func TestPageARadioButtonFontSizeLeavesTheFontAlone(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	NewRadioButton(font, "Yes").SetFontSize(20).SetLocation(50, 50).DrawOn(page)
	testNear(t, "font size", 12, font.GetSize(), 0)
	if !strings.Contains(testContent(page), " 20 Tf\n") {
		t.Errorf("content %q", testContent(page))
	}
}

func TestPageAColorOrPenWidthThatIsSetAlreadyIsNotWrittenAgain(t *testing.T) {
	page := testNewPage()
	page.SetBrushColor(color.Black) // Written, as a new page has written no color
	page.SetBrushColor(color.Black)
	page.SetPenColor(color.Red)
	page.SetPenColorRGB([3]float32{1, 0, 0})
	page.SetPenWidth(0)
	page.SetDefaultPenWidth()
	if got := testContent(page); got != "0 0 0 rg\n1 0 0 RG\n0 w\n" {
		t.Errorf("content %q", got)
	}
}

func TestPageQAndQKeepWhatTheContentHasWritten(t *testing.T) {
	page := testNewPage()
	page.SetBrushColor(color.Black)
	page.SaveGraphicsState()
	page.SetBrushColor(color.Blue)
	page.SetBrushColor(color.Blue)
	page.RestoreGraphicsState()
	page.SetBrushColor(color.Black) // Q restored black
	page.SetBrushColor(color.Blue)
	if got := testContent(page); got != "0 0 0 rg\nq\n0 0 1 rg\nQ\n0 0 1 rg\n" {
		t.Errorf("content %q", got)
	}
}

func TestPageAnRgbColorAfterACmykColorIsWritten(t *testing.T) {
	page := testNewPage()
	page.SetBrushColor(color.Black)
	page.SetBrushColorCMYK(0, 0, 0, 1)
	page.SetBrushColor(color.Black)
	if got := testContent(page); got != "0 0 0 rg\n0 0 0 1 k\n0 0 0 rg\n" {
		t.Errorf("content %q", got)
	}
}

func TestPageTheFontOfTheTextIsWrittenWhenItChanges(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	NewTextLine(font, "a").SetLocation(10, 20).DrawOn(page)
	NewTextLine(font, "b").SetLocation(10, 40).DrawOn(page)
	NewTextLine(font, "c").SetFontSize(14).SetLocation(10, 60).DrawOn(page)
	page.SaveGraphicsState()
	NewTextLine(font, "d").SetLocation(10, 80).DrawOn(page)
	page.RestoreGraphicsState()
	NewTextLine(font, "e").SetFontSize(14).SetLocation(10, 100).DrawOn(page)
	content := testContent(page)
	fonts := make([]string, 0)
	for _, line := range strings.Split(content, "\n") {
		if strings.HasSuffix(line, " Tf") {
			fonts = append(fonts, line[strings.Index(line, " ")+1:])
		}
	}
	if strings.Join(fonts, ",") != "12 Tf,14 Tf,12 Tf" {
		t.Errorf("fonts %q in %q", fonts, content)
	}
}

func TestPageFillRectWritesOneRectangleWithTheEdgesOfThePath(t *testing.T) {
	page := testNewPage()
	page.FillRect(10, 20, 30, 40)
	if got := testContent(page); got != "10 732 30 40 re\nf\n" {
		t.Errorf("content %q", got)
	}
	// A path wrote the top edge at 792 and the bottom one at 791.99, where
	// rounding the height alone would make it 0.
	page = testNewPage()
	page.FillRect(0, 0.004, 1, 0.004)
	if got := testContent(page); got != "0 791.99 1 0.01 re\nf\n" {
		t.Errorf("content %q", got)
	}
	// Far outside the page the rectangle is still a path.
	page = testNewPage()
	page.FillRect(200000, 0, 10, 10)
	if got := testContent(page); got != "200000 792 m\n200010 792 l\n200010 782 l\n200000 782 l\nf\n" {
		t.Errorf("content %q", got)
	}
}

func TestPageImportedContentIsSeparatedFromTheOperatorAfterIt(t *testing.T) {
	page := testNewPage()
	page.DrawContents([]byte("BT ET"), 100, 0, 0, 1, 1)
	if content := testContent(page); !strings.Contains(content, "BT ET\nQ\n") {
		t.Error(content)
	}
}
