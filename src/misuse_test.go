// misuse_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// A program that uses the API the wrong way gets the misuse recorded on the
// PDF, and Complete then refuses to finish the document, so no broken PDF is
// written. Where Java throws at the call, Go records the same message.

const testEarlier = "The PDF was not completed because of an earlier error: "

// testRecorded checks the first misuse recorded on the PDF.
func testRecorded(t *testing.T, pdf *PDF, message string) {
	t.Helper()
	if pdf.err == nil || pdf.err.Error() != message {
		t.Errorf("recorded: want %q, got %v", message, pdf.err)
	}
}

// testRefused checks that Complete refuses the document because of the recorded misuse.
func testRefused(t *testing.T, pdf *PDF, message string) {
	t.Helper()
	testCompleteFails(t, pdf, testEarlier+message)
}

// testCompleteFails checks the error that Complete returns.
func testCompleteFails(t *testing.T, pdf *PDF, message string) {
	t.Helper()
	if err := pdf.Complete(); err == nil || err.Error() != message {
		t.Errorf("Complete: want %q, got %v", message, err)
	}
}

func TestMisuseANumberThatIsNotFiniteOrTooLargeIsRefused(t *testing.T) {
	for _, bad := range []float32{float32(math.NaN()), float32(math.Inf(1)), -1e30, 2147483648} {
		pdf := testNewPDF()
		page := NewPage(pdf, letter.Portrait())
		page.DrawLine(10, 10, bad, 200)
		testRecorded(t, pdf, "A coordinate, size or width is NaN, infinite or too large for a PDF.")
		if content := testContent(page); strings.Contains(content, "NaN") || strings.Contains(content, "Inf") {
			t.Errorf("content %q", content)
		}
		testRefused(t, pdf, "A coordinate, size or width is NaN, infinite or too large for a PDF.")
	}
	page := testNewPage()
	page.DrawLine(0, 0, 100000000, 0)
	if content := testContent(page); !strings.Contains(content, "100000000 792 l\n") {
		t.Errorf("content %q", content)
	}
	if page.pdf.err != nil {
		t.Error(page.pdf.err)
	}
}

func TestMisuseANaNFontSizeIsRefused(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	font.SetSize(float32(math.NaN()))
	NewTextLine(font, "Hello").SetLocation(50, 50).DrawOn(page)
	testRecorded(t, pdf, "A coordinate, size or width is NaN, infinite or too large for a PDF.")
}

func TestMisuseADashPatternIsAnArrayAndAPhase(t *testing.T) {
	page := testNewPage()
	page.SetStrokeDashPattern("[] 0")
	page.SetStrokeDashPattern("[3 3] 0")
	page.SetStrokeDashPattern(" [2.5 .5 1]  0.5 ")
	if content := testContent(page); !strings.HasSuffix(content, " [2.5 .5 1]  0.5  d\n") || page.pdf.err != nil {
		t.Errorf("content %q, error %v", content, page.pdf.err)
	}
	for _, bad := range []string{"3 3", "[3 3]", "[a] 0", "[0 0] 0", "[-1 2] 0", "[3-3] 0", "[1.5.5] 0", "[3 3] 0 ET", ""} {
		pdf := testNewPDF()
		page2 := NewPage(pdf, letter.Portrait())
		page2.SetStrokeDashPattern(bad)
		testRecorded(t, pdf, "The dash pattern \""+bad+"\" is not an array of non-negative numbers, "+
			"not all zero, followed by a phase, such as \"[3 3] 0\".")
		if content := testContent(page2); content != "" {
			t.Errorf("%q wrote %q", bad, content)
		}
	}
}

func TestMisuseANegativePenWidthIsRefused(t *testing.T) {
	page := testNewPage()
	page.SetPenWidth(-2)
	testRecorded(t, page.pdf, "The pen width cannot be negative.")
	if content := testContent(page); content != "" {
		t.Errorf("content %q", content)
	}
	page2 := testNewPage()
	page2.SetPenWidth(0)
	if page2.pdf.err != nil {
		t.Error(page2.pdf.err)
	}
}

func TestMisuseTheGraphicsStatesMustBePaired(t *testing.T) {
	page := testNewPage()
	page.RestoreGraphicsState()
	testRecorded(t, page.pdf, "RestoreGraphicsState was called without a matching SaveGraphicsState.")
	if content := testContent(page); content != "" {
		t.Errorf("content %q", content)
	}

	pdf2 := testNewPDF()
	NewPage(pdf2, letter.Portrait()).SaveGraphicsState()
	NewPage(pdf2, letter.Portrait())
	testRecorded(t, pdf2, "A page ends with a SaveGraphicsState that has no RestoreGraphicsState.")

	pdf3 := testNewPDF()
	NewPage(pdf3, letter.Portrait()).SaveGraphicsState()
	testCompleteFails(t, pdf3, "A page ends with a SaveGraphicsState that has no RestoreGraphicsState.")
}

func TestMisuseTheMarkedContentMustBePaired(t *testing.T) {
	for _, level := range []compliance.Compliance{compliance.PDF_1_7, compliance.PDF_UA_1} {
		pdf := testNewPDF()
		pdf.SetCompliance(level)
		NewPage(pdf, letter.Portrait()).AddEMC()
		testRecorded(t, pdf, "AddEMC was called without a matching AddBDC or AddArtifactBMC.")

		pdf2 := testNewPDF()
		pdf2.SetCompliance(level)
		NewPage(pdf2, letter.Portrait()).AddBDC(structelem.P, "", "x", "x")
		testCompleteFails(t, pdf2, "A page ends with an AddBDC or AddArtifactBMC that has no AddEMC.")

		doc3 := testNewDoc()
		doc3.pdf.SetCompliance(level)
		page3 := NewPage(doc3.pdf, letter.Portrait())
		page3.AddArtifactBMC()
		page3.AddEMC()
		if err := doc3.pdf.Complete(); err != nil {
			t.Error(err)
		}
	}
}

func TestMisuseAWrittenPageCannotBeDrawnOn(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page1 := NewPage(pdf, letter.Portrait())
	NewPage(pdf, letter.Portrait())
	NewTextLine(font, "Late").SetLocation(50, 50).DrawOn(page1)
	message := "The page was already written to the PDF: draw on a page before " +
		"creating the next page or completing the PDF."
	testRecorded(t, pdf, message)
	testRefused(t, pdf, message)
}

func TestMisuseCompleteFinishesADocumentOnce(t *testing.T) {
	doc := testNewDoc()
	NewPage(doc.pdf, letter.Portrait())
	if err := doc.pdf.Complete(); err != nil {
		t.Fatal(err)
	}
	NewPage(doc.pdf, letter.Portrait())
	testRecorded(t, doc.pdf, "The PDF was already completed.")
	testCompleteFails(t, doc.pdf, "Complete was already called.")
}

func TestMisuseADocumentNeedsAPage(t *testing.T) {
	testCompleteFails(t, testNewPDF(), "A PDF needs at least one page.")
}

func TestMisuseAPageIsAddedOnceAndToItsOwnDocument(t *testing.T) {
	pdf := testNewPDF()
	page := NewPageDetached(pdf, letter.Portrait())
	pdf.AddPage(page)
	pdf.AddPage(page)
	testRecorded(t, pdf, "The page was already added to the PDF.")
	if len(pdf.pages) != 1 {
		t.Errorf("pages %d", len(pdf.pages))
	}

	pdf2 := testNewPDF()
	pdf2.AddPage(NewPageDetached(testNewPDF(), letter.Portrait()))
	testRecorded(t, pdf2, "The page belongs to another PDF.")
}

func TestMisuseFontsImagesStampsAndGroupsBelongToOneDocument(t *testing.T) {
	other := testNewPDF()
	font := testHelvetica(other)

	page := testNewPage()
	NewTextLine(font, "Hello").SetLocation(50, 50).DrawOn(page)
	testRecorded(t, page.pdf, "The font belongs to another PDF.")

	image := NewImageFromFile(other, testRepoPath(t, "images/map407.png"))
	page = testNewPage()
	image.SetLocation(50, 50).DrawOn(page)
	testRecorded(t, page.pdf, "The image belongs to another PDF.")

	stamp := NewStamp(other).SetSize(50, 50)
	stamp.Complete()
	page = testNewPage()
	stamp.DrawOn(page)
	testRecorded(t, page.pdf, "The stamp belongs to another PDF.")

	pdf := testNewPDF()
	NewStamp(pdf).SetSize(50, 50).AddFont(font)
	testRecorded(t, pdf, "The font belongs to another PDF.")

	group := NewOptionalContentGroup(other, "Layer")
	group.Add(NewRect(0, 0, 1, 1))
	page = testNewPage()
	group.DrawOn(page)
	testRecorded(t, page.pdf, "The optional content group belongs to another PDF.")
}

func TestMisuseAStampIsCompletedOnceBeforeItIsDrawn(t *testing.T) {
	page := testNewPage()
	NewStamp(page.pdf).SetSize(50, 50).DrawOn(page)
	testRecorded(t, page.pdf, "Call Complete on the stamp before drawing it.")

	pdf2 := testNewPDF()
	stamp2 := NewStamp(pdf2).SetSize(50, 50)
	stamp2.Complete()
	stamp2.Complete()
	testRecorded(t, pdf2, "Complete was already called on the stamp.")

	pdf3 := testNewPDF()
	stamp3 := NewStamp(pdf3).SetSize(50, 50)
	stamp3.Complete()
	stamp3.DrawRect(0, 0, 10, 10)
	testRecorded(t, pdf3, "The stamp was already completed.")
}

func TestMisuseStampTextAddsItsFont(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
	core := testHelvetica(doc.pdf)
	stamp := NewStamp(doc.pdf).SetSize(100, 50)
	stamp.DrawText(core, 12, 5, 20, "Paid")
	testRecorded(t, doc.pdf, "A stamp draws text with an embedded font, not a core or CJK font.")
	stamp.DrawText(font, 12, 5, 20, "Paid")
	stamp.Complete()
	if err := doc.writer.Flush(); err != nil {
		t.Fatal(err)
	}
	want := fmt.Sprintf("/Font <<\n/F%d %d 0 R\n", font.objNumber, font.objNumber)
	if raw := doc.buf.String(); !strings.Contains(raw, want) {
		t.Errorf("no %q", want)
	}
}

func TestMisuseEncryptionAndComplianceComeBeforeTheContent(t *testing.T) {
	pdf := testNewPDF()
	testHelvetica(pdf)
	enc, err := NewEncryption(pdf, encryption.NewPasswords(), encryption.NewPermissions())
	if enc != nil || err == nil || err.Error() != "Set the encryption before adding fonts, images or pages to the PDF." {
		t.Errorf("NewEncryption: %v %v", enc, err)
	}
	testRecorded(t, pdf, "Set the encryption before adding fonts, images or pages to the PDF.")

	pdfB := testNewPDF()
	testHelvetica(pdfB)
	pdfB.SetCompliance(compliance.PDF_1_7) // No change.
	if pdfB.err != nil {
		t.Error(pdfB.err)
	}
	pdfB.SetCompliance(compliance.PDF_UA_1)
	testRecorded(t, pdfB, "Set the compliance before adding fonts, images or pages to the PDF.")
	if pdfB.GetCompliance() != compliance.PDF_1_7 {
		t.Error("the compliance changed")
	}

	pdf2 := testNewPDF()
	NewPageDetached(pdf2, letter.Portrait())
	pdf2.SetCompliance(compliance.PDF_UA_1)
	testRecorded(t, pdf2, "Set the compliance before adding fonts, images or pages to the PDF.")

	pdf3 := testNewPDF()
	enc3, err := NewEncryption(pdf3, encryption.NewPasswords(), encryption.NewPermissions())
	if err != nil {
		t.Fatal(err)
	}
	testHelvetica(pdf3)
	pdf3.SetEncryption(enc3)
	testRecorded(t, pdf3, "Set the encryption before adding fonts, images or pages to the PDF.")
}

func TestMisuseAPageIsFromThreeTo14400PointsWideAndHigh(t *testing.T) {
	for _, good := range []pagesize.PageSize{pagesize.NewPageSize(3, 3), pagesize.NewPageSize(14400, 14400)} {
		pdf := testNewPDF()
		NewPage(pdf, good)
		if pdf.err != nil {
			t.Error(pdf.err)
		}
	}
	for _, bad := range []pagesize.PageSize{
		pagesize.NewPageSize(0, 0), pagesize.NewPageSize(612, 2),
		pagesize.NewPageSize(14401, 792), pagesize.NewPageSize(float32(math.NaN()), 792)} {
		pdf := testNewPDF()
		NewPage(pdf, bad)
		testRecorded(t, pdf, "A page must be from 3 to 14400 points wide and high.")
	}
}

func TestMisuseTheXmpMetadataLeavesOutControlCharacters(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Report\x01 2026\uFFFF \xff")
	NewPage(doc.pdf, letter.Portrait())
	raw := string(doc.complete())
	if !strings.Contains(raw, "<rdf:li xml:lang=\"x-default\">Report 2026 </rdf:li>") {
		t.Error("the title in the XMP metadata keeps characters XML does not allow")
	}
}

func TestMisuseAZeroSizeImageStampOrContainerDrawsNothing(t *testing.T) {
	page := testNewPage()
	image := NewImageFromFile(page.pdf, testRepoPath(t, "images/map407.png"))
	image.ScaleBy(0)
	image.SetLocation(50, 50).DrawOn(page)
	stamp := NewStamp(page.pdf).SetSize(50, 50)
	stamp.Complete()
	stamp.ScaleBy(0).SetLocation(50, 50).DrawOn(page)
	container := NewContainer(100, 100)
	container.Add(NewRect(0, 0, 10, 10))
	container.ScaleBy(0).SetLocation(50, 50).DrawOn(page)
	if content := testContent(page); strings.Contains(content, " cm\n") {
		t.Errorf("content %q", content)
	}
	if page.pdf.err != nil {
		t.Error(page.pdf.err)
	}
}
