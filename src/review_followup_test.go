// review_followup_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"os"
	"path/filepath"
	"slices"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// What was left open by the review: CMYK colors in PDF/A, the footer of a big
// table, the destinations and headings in a container, and the error of a
// table read from a file.

func TestReviewFollowupAPDFADocumentUsesNoCMYKColor(t *testing.T) {
	// The output intent of PDF/A is sRGB, so its colors are not CMYK, as its
	// images are not.
	for _, level := range []compliance.Compliance{compliance.PDF_A_1B, compliance.PDF_A_2B, compliance.PDF_A_3A_UA_1} {
		for _, pen := range []bool{true, false} {
			pdf := testNewPDF()
			pdf.SetCompliance(level)
			page := NewPage(pdf, letter.Portrait())
			before := testContent(page)
			if pen {
				page.SetPenColorCMYK(0.1, 0.2, 0.3, 0.4)
			} else {
				page.SetBrushColorCMYK(0.1, 0.2, 0.3, 0.4)
			}
			testWant(t, "A document of "+level.String()+" cannot use a CMYK color: "+
				"its output intent is sRGB, so its colors are gray or RGB.", page.pdf.err.Error())
			// Nothing is written
			testWant(t, before, testContent(page))
		}
	}
	// A document that is not PDF/A uses them
	for _, level := range []compliance.Compliance{compliance.PDF_1_7, compliance.PDF_UA_1} {
		pdf := testNewPDF()
		pdf.SetCompliance(level)
		page := NewPage(pdf, letter.Portrait())
		page.SetPenColorCMYK(0.1, 0.2, 0.3, 0.4)
		page.SetBrushColorCMYK(0.1, 0.2, 0.3, 0.4)
		if page.pdf.err != nil {
			t.Errorf("%s: %v", level, page.pdf.err)
		}
		content := testContent(page)
		if !strings.Contains(content, "0.1 0.2 0.3 0.4 K\n") || !strings.Contains(content, "0.1 0.2 0.3 0.4 k\n") {
			t.Errorf("%s: content %q", level, content)
		}
	}
}

func TestReviewFollowupTheFooterOfABigTableIsAPaginationArtifact(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := testHelvetica(doc.pdf)
	pages := testDrawBigTable(t, NewBigTable(doc.pdf, font, font, letter.Portrait()).
		SetNumberOfColumns(3).SetTableRows(testBigTableHeader, slices.Values(testBigTableRows())))
	// The content of the last page, which is written when the PDF is
	content := testContent(pages[len(pages)-1])
	// The footer is marked once, as a footer, and not inside a plain artifact
	footer := strings.Index(content, "/Artifact <</Type /Pagination /Subtype /Footer>> BDC\n")
	if footer < 0 || footer > strings.Index(content, testHex("Page 2 of 2")) {
		t.Errorf("the footer is not a pagination artifact: %q", content)
	}
	if strings.Contains(content, "/Artifact BMC\n/Artifact <<") {
		t.Error("the footer is in an artifact inside another")
	}
	doc.complete()
}

// testDrawInContainer draws the text line with a destination, as a heading,
// in a container at 100, 200 turned by the degrees, or on the page at x, y
// moved by 100, 200 when there is no container, and returns the page.
func testDrawInContainer(inContainer bool, degrees float64) *Page {
	page := testTaggedPage()
	page.pdf.SetTitle("Title")
	font := testHelvetica(page.pdf)
	line := NewTextLine(font, "Heading")
	line.SetDestination("heading")
	line.SetStructureType(structelem.H1)
	if inContainer {
		line.SetLocation(0, 50)
		container := NewContainer(100, 100)
		container.SetLocation(100, 200)
		container.SetRotation(degrees)
		container.Add(line)
		container.DrawOn(page)
	} else {
		line.SetLocation(100, 250)
		line.DrawOn(page)
	}
	return page
}

func TestReviewFollowupADestinationAndAHeadingInAContainerAreWhereTheTextIs(t *testing.T) {
	// Moved with the container, as if drawn where it puts the text
	want := testDrawInContainer(false, 0)
	got := testDrawInContainer(true, 0)
	// The left of the page, where the destination of a text line is, is the
	// left of the container
	testNear(t, "x", want.destinations[0].xPosition+100, got.destinations[0].xPosition, testDelta)
	testNear(t, "y", want.destinations[0].yPosition, got.destinations[0].yPosition, testDelta)
	testNear(t, "top", want.pdf.headings[0].top, got.pdf.headings[0].top, testDelta)
	testNear(t, "top", 250-12, got.pdf.headings[0].top, testDelta)

	// Turned a quarter, the text runs down the page from 50 left of the
	// center, 150 and 250, and its top 12 above the baseline is 12 right of it
	turned := testDrawInContainer(true, 90)
	testNear(t, "x", 162, turned.destinations[0].xPosition, testDelta)
	testNear(t, "y", 792-200, turned.destinations[0].yPosition, testDelta)
	testNear(t, "top", 200, turned.pdf.headings[0].top, testDelta)
}

func TestReviewFollowupATableFromAFileReturnsItsError(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	path := filepath.Join(t.TempDir(), "open.csv")
	if err := os.WriteFile(path, []byte("Name,City\nn1,\"Toronto\n"), 0o600); err != nil {
		t.Fatal(err)
	}
	message := "A quoted field is not closed by the end of the data file: n1,\"Toronto"
	table, err := ReadTableFromFile(font, font, path)
	if table != nil || err == nil {
		t.Fatalf("table %v, error %v", table, err)
	}
	testWant(t, message, err.Error())
	// NewTableFromFile, which returns no error, panics with it, as it did
	func() {
		defer func() {
			r := recover()
			if e, ok := r.(error); !ok || e.Error() != message {
				t.Errorf("panic %v", r)
			}
		}()
		NewTableFromFile(font, font, path)
	}()
	// A file that is not there
	if _, err := ReadTableFromFile(font, font, filepath.Join(t.TempDir(), "none.csv")); !os.IsNotExist(err) {
		t.Errorf("error %v", err)
	}
	// A file that is read
	if err := os.WriteFile(path, []byte("Name,City\nn1,\"Toronto, ON\"\n"), 0o600); err != nil {
		t.Fatal(err)
	}
	table, err = ReadTableFromFile(font, font, path)
	if err != nil || len(table.tableData) != 2 || table.tableData[1][1].GetText() != "Toronto, ON" {
		t.Errorf("table %v, error %v", table, err)
	}
}
