// review_reader_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"fmt"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Reading PDFs that are made to be slow to read, or to break what is made of
// them. The time limits are generous: each of these took from five seconds to
// more than a minute, and takes a time in proportion to its size now.

// testReadQuickly reads the PDF, which must take less than five seconds, and
// returns its objects, or nil when it cannot be read.
func testReadQuickly(t *testing.T, pdf []byte) []*PDFobj {
	t.Helper()
	start := time.Now()
	objects, err := testNewPDF().Read(pdf)
	if elapsed := time.Since(start); elapsed > 5*time.Second {
		t.Errorf("reading %d bytes took %v", len(pdf), elapsed)
	}
	if err != nil {
		return nil
	}
	return objects
}

// testNumberedObjects returns a PDF with no cross-reference table, of a
// catalog, a page tree of one page and then count objects "<< /A i >>", with
// or without their endobj.
func testNumberedObjects(count int, endobj bool) []byte {
	var sb strings.Builder
	sb.WriteString("%PDF-1.7\n")
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
	}
	for i := 0; i < count; i++ {
		objects = append(objects, fmt.Sprintf("<< /A %d >>", i))
	}
	for i, object := range objects {
		fmt.Fprintf(&sb, "%d 0 obj\n%s\n", i+1, object)
		if endobj {
			sb.WriteString("endobj\n")
		}
	}
	fmt.Fprintf(&sb, "trailer\n<< /Size %d /Root 1 0 R >>\n", len(objects)+1)
	return []byte(sb.String())
}

func TestReviewReaderARunOfWhiteSpaceIsScannedOnce(t *testing.T) {
	// Every space looked at all the spaces after it for a number.
	testReadQuickly(t, append([]byte("%PDF-1.7\n"), bytes.Repeat([]byte(" "), 400000)...))
	testReadQuickly(t, append([]byte("%PDF-1.7\n1"), bytes.Repeat([]byte("\n"), 400000)...))
}

func TestReviewReaderObjectsWithNoEndobjAreReadOnce(t *testing.T) {
	// Every object was read to the end of the PDF.
	testReadQuickly(t, bytes.Repeat([]byte("1 0 obj\n"), 31000))

	// Each object ends where the next one starts.
	objects := testReadQuickly(t, testNumberedObjects(20000, false))
	if len(objects) != 20003 {
		t.Fatalf("objects %d", len(objects))
	}
	testWant(t, "7", objects[10].GetValue("/A"))
	testWant(t, "[ 3 0 R ]", objects[1].GetValue("/Kids"))
	if n := len(testNewPDF().GetPageObjects(objects)); n != 1 {
		t.Errorf("pages %d", n)
	}
}

func TestReviewReaderObjectsWithNoEndobjThatTheTableListsAreReadOnce(t *testing.T) {
	// Every object that the cross-reference table lists was read to the end
	// of the PDF.
	var sb strings.Builder
	sb.WriteString("%PDF-1.7\n")
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
	}
	for i := 0; i < 20000; i++ {
		objects = append(objects, fmt.Sprintf("<< /A %d >>", i))
	}
	offsets := make([]int, 0)
	for i, object := range objects {
		offsets = append(offsets, sb.Len())
		fmt.Fprintf(&sb, "%d 0 obj\n%s\n", i+1, object)
	}
	xref := sb.Len()
	fmt.Fprintf(&sb, "xref\n0 %d\n0000000000 65535 f \n", len(objects)+1)
	for _, offset := range offsets {
		fmt.Fprintf(&sb, "%010d 00000 n \n", offset)
	}
	fmt.Fprintf(&sb, "trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n", len(objects)+1, xref)
	read := testReadQuickly(t, []byte(sb.String()))
	if len(read) != 20003 {
		t.Fatalf("objects %d", len(read))
	}
	testWant(t, "7", read[10].GetValue("/A"))
	// The object is "11 0 obj << /A 7 >>", without the objects after it.
	testWant(t, "11 0 obj << /A 7 >>", strings.Join(read[10].dict, " "))
}

func TestReviewReaderACrossReferenceSectionThatIsItsOwnPrevIsReadOnce(t *testing.T) {
	// The section of a big table was read a thousand times: its /Prev is
	// itself. The PDF is then read by looking for its objects.
	pdf := string(testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"))
	xref := strings.Index(pdf, "xref")
	var sb strings.Builder
	sb.WriteString(pdf[:xref])
	fmt.Fprintf(&sb, "xref\n0 4\n0000000000 65535 f \n")
	for i := 1; i <= 3; i++ {
		fmt.Fprintf(&sb, "%010d 00000 n \n", strings.Index(pdf, fmt.Sprintf("%d 0 obj", i)))
	}
	// Free entries, which make the table big.
	for i := 0; i < 100000; i++ {
		sb.WriteString("0000000000 65535 f \n")
	}
	fmt.Fprintf(&sb, "trailer\n<< /Size 4 /Root 1 0 R /Prev %d >>\nstartxref\n%d\n%%%%EOF\n", xref, xref)
	objects := testReadQuickly(t, []byte(sb.String()))
	if n := len(testNewPDF().GetPageObjects(objects)); n != 1 {
		t.Errorf("pages %d", n)
	}
}

// testPDFWithStreams returns a PDF of count streams, each with its /Length in
// an object of its own.
func testPDFWithStreams(count int) []byte {
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
	}
	for i := 0; i < count; i++ {
		objects = append(objects,
			fmt.Sprintf("<< /Length %d 0 R >>\nstream\nq Q\nendstream", len(objects)+2), "3")
	}
	return testPDFWithObjects(objects...)
}

func TestReviewReaderTheLengthOfAStreamIsFoundByItsNumber(t *testing.T) {
	// Every stream looked for its /Length among all the objects.
	objects := testReadQuickly(t, testPDFWithStreams(60000))
	if len(objects) != 120003 {
		t.Fatalf("objects %d", len(objects))
	}
	testWant(t, "q Q", string(objects[119999].GetData()))
}

func TestReviewReaderTheLengthOfAStreamIsItsNewestVersion(t *testing.T) {
	// The /Length of the stream is updated from 2 to 15 at the end of the PDF,
	// and the stream holds the keyword that the first length ends at.
	pdf := string(testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>",
		"<< /Length 5 0 R >>\nstream\nAB endstream CD\nendstream",
		"2"))
	prev := strings.Index(pdf, "xref")
	var sb strings.Builder
	sb.WriteString(pdf)
	offset := sb.Len()
	sb.WriteString("5 0 obj\n15\nendobj\n")
	xref := sb.Len()
	fmt.Fprintf(&sb, "xref\n0 1\n0000000000 65535 f \n5 1\n%010d 00000 n \n", offset)
	fmt.Fprintf(&sb, "trailer\n<< /Size 6 /Root 1 0 R /Prev %d >>\nstartxref\n%d\n%%%%EOF\n", prev, xref)
	objects := testRead(t, []byte(sb.String()))
	testWant(t, "AB endstream CD", string(objects[3].GetData()))
}

// testChainOfObjects returns a PDF whose page uses a form XObject that refers
// to the next object, which refers to the next, count times, and whose page
// tree has count nodes, one under the other.
func testChainOfObjects(count int) []byte {
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Page /MediaBox [0 0 612 792] /Resources << /XObject << /X0 3 0 R >> >> >>",
	}
	for i := 0; i < count; i++ { // Objects 3 and on
		objects = append(objects, fmt.Sprintf("<< /Next %d 0 R >>", len(objects)+2))
	}
	objects = append(objects, "<< >>")
	first := len(objects) + 1
	for i := 0; i < count; i++ {
		kid := len(objects) + 2
		if i == count-1 {
			kid = 2 // The page
		}
		objects = append(objects, fmt.Sprintf("<< /Type /Pages /Kids [%d 0 R] /Count 1 >>", kid))
	}
	objects[0] = fmt.Sprintf("<< /Type /Catalog /Pages %d 0 R >>", first)
	return testPDFWithObjects(objects...)
}

func TestReviewReaderLongChainsOfObjectsAreFollowedWithoutRecursion(t *testing.T) {
	// The other ports ran out of stack.
	count := 100000
	objects := testRead(t, testChainOfObjects(count))
	doc := testNewDoc()
	pages := doc.pdf.GetPageObjects(objects)
	if len(pages) != 1 {
		t.Fatalf("pages %d", len(pages))
	}
	doc.pdf.AddResourceObjects(objects)
	NewPage(doc.pdf, letter.Portrait())
	written := testRead(t, doc.complete())
	testWant(t, "<< >>", strings.Join(objectValue(written[count+2]), " "))
}

func TestReviewReaderAddObjectsIsRefusedWhereItWouldLosePages(t *testing.T) {
	message := "The objects of an existing PDF cannot be added to a PDF that has pages of its own."
	pdf := testNewPDF()
	NewPage(pdf, letter.Portrait())
	testMergeError(t, pdf.AddObjects(testExistingObjects(t)), message)

	// A page after the objects is not in their page tree.
	pdf = testNewPDF()
	if err := pdf.AddObjects(testExistingObjects(t)); err != nil {
		t.Fatal(err)
	}
	NewPage(pdf, letter.Portrait())
	testRefused(t, pdf,
		"A page cannot be added to a PDF that AddObjects added the objects of an existing PDF to.")

	// The pages were not made for the compliance of the document.
	pdf = testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1)
	testMergeError(t, pdf.AddObjects(testExistingObjects(t)),
		"The objects of an existing PDF cannot be added to a PDF/UA or PDF/A document.")
}

func TestReviewReaderANumberWithNoObjectIsAFreeEntry(t *testing.T) {
	// Object 4 is not in the PDF, and was written as "4 0 obj endobj".
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
		"<< /Removed true >>",
		"<< /Kept true >>"))
	objects[3] = newPDFobj().setNumber(4) // As Read gives a number with no object
	doc := testNewDoc()
	if err := doc.pdf.AddObjects(objects); err != nil {
		t.Fatal(err)
	}
	raw := string(doc.complete())
	if strings.Contains(raw, "\n4 0 obj") {
		t.Error("an empty object is written")
	}
	xref := raw[strings.LastIndex(raw, "\nxref\n"):]
	entries := strings.Split(xref, "\n")
	testWant(t, "0000000000 65535 f ", entries[7]) // Object 4
	testWant(t, "true", testRead(t, []byte(raw))[4].GetValue("/Kept"))
}

func TestReviewReaderTwoPagesThatNameDifferentResourcesAlikeAreRefused(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
		"<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
		"<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 6 0 R >> >> >>",
		"<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream",
		"<< /Subtype /Form /BBox [0 0 20 20] /Length 0 >>\nstream\n\nendstream"))
	pdf := testNewPDF()
	pdf.AddResourceObjects(objects)
	testRefused(t, pdf, "The pages of the PDF use the name /X0 for different resources, "+
		"and the pages of this document share one resources dictionary.")

	// Pages that use the same resource under the same name share it.
	objects = testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
		"<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
		"<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
		"<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream"))
	doc := testNewDoc()
	doc.pdf.AddResourceObjects(objects)
	NewPage(doc.pdf, letter.Portrait())
	doc.complete()
}

func TestReviewReaderTheTypeOfAnObjectIsAnEntryOfItsOwn(t *testing.T) {
	// The /Type of the /Group was taken for the type of the page, which a form
	// refers to, and the page was copied with the form.
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Group << /Type /Group /S /Transparency >> /Type /Page /Parent 2 0 R"+
			" /Resources << /XObject << /X0 4 0 R >> >> >>",
		"<< /Subtype /Form /BBox [0 0 10 10] /Page 3 0 R /Length 0 >>\nstream\n\nendstream"))
	testWant(t, "/Page", objects[2].GetValue("/Type"))
	testWant(t, "", objects[2].GetValue("/S"))
	doc := testNewDoc()
	doc.pdf.AddResourceObjects(objects)
	NewPage(doc.pdf, letter.Portrait())
	if raw := string(doc.complete()); strings.Contains(raw, "/Transparency") {
		t.Error("the page is copied with the form")
	}
}

func TestReviewReaderANumberThatIsNotAnObjectNumberIsSkipped(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /Resources << /XObject <<"+
			" /Im1 abc 0 R /Im2 99999999999 0 R /Im3 4 0 R >> >> >>",
		"<< /Subtype /Form /BBox [0 0 10 10] /Ref 2147483648 0 R /Length 0 >>\nstream\n\nendstream"))
	doc := testNewDoc()
	doc.pdf.AddResourceObjects(objects)
	NewPage(doc.pdf, letter.Portrait())
	doc.complete()
}

func TestReviewReaderTheNumbersOfAnObjectStreamAreDigits(t *testing.T) {
	testWant(t, `The object stream of the PDF is malformed: "+5" is not a number.`,
		testReadError(t, "1 0 obj<</Type/ObjStm/N 1/First 5/Length 9>>stream\n+5 0 <<>>\nendstream endobj"))
	testWant(t, `The object stream of the PDF is malformed: "2147483648" is not a number.`,
		testReadError(t, "1 0 obj<</Type/ObjStm/N 1/First 2147483648/Length 9>>stream\n5 0 <<>>\nendstream endobj"))
	// An offset past the end of the stream, which overflowed an int of 32 bits.
	testWant(t, "(no error)",
		testReadError(t, "1 0 obj<</Type/ObjStm/N 2/First 2147483000/Length 13>>stream\n5 1000 6 1001\nendstream endobj"))
}
