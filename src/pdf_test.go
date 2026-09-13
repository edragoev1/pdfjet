// pdf_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
)

// Writing a document and reading it back.

func testDocument(sizes ...pagesize.PageSize) []byte {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	for _, size := range sizes {
		page := NewPage(doc.pdf, size)
		line := NewTextLine(font, "Page")
		line.SetLocation(50, 50)
		line.DrawOn(page)
	}
	return doc.complete()
}

func TestPDFStartsWithTheHeaderAndEndsWithEof(t *testing.T) {
	raw := string(testDocument(letter.Portrait()))
	if !strings.HasPrefix(raw, "%PDF-1.7\n%") || !strings.HasSuffix(raw, "%%EOF\n") {
		t.Errorf("header or trailer wrong: %q ... %q", raw[:10], raw[len(raw)-10:])
	}
}

func TestPDFTheCrossReferenceTablePointsAtEveryObject(t *testing.T) {
	raw := string(testDocument(letter.Portrait(), a4.Portrait()))
	header := regexp.MustCompile("xref\n0 (\\d+)\n").FindStringSubmatchIndex(raw)
	if header == nil {
		t.Fatal("no xref table")
	}
	count, _ := strconv.Atoi(raw[header[2]:header[3]])
	entries := header[1]
	for number := 1; number < count; number++ {
		entry := raw[entries+20*number : entries+20*number+20]
		if entry[17] == 'n' {
			offset, _ := strconv.Atoi(entry[:10])
			if !strings.HasPrefix(raw[offset:], strconv.Itoa(number)+" 0 obj") {
				t.Errorf("object %d is not at %d", number, offset)
			}
		}
	}
	startxref := regexp.MustCompile("startxref\n(\\d+)\n%%EOF\n$").FindStringSubmatch(raw)
	if startxref == nil {
		t.Fatal("no startxref")
	}
	offset, _ := strconv.Atoi(startxref[1])
	if !strings.HasPrefix(raw[offset:], "xref\n") {
		t.Error("startxref does not point at the xref table")
	}
}

func TestPDFDocumentIdsAreRandomAndDifferent(t *testing.T) {
	ids := make(map[string]bool)
	valid := regexp.MustCompile("^[0-9a-f]{32}$")
	for i := 0; i < 100; i++ {
		doc := testNewDoc()
		NewPage(doc.pdf, a4.Portrait())
		id := testTrailerID(doc.complete())
		if !valid.MatchString(id) {
			t.Errorf("id %q", id)
		}
		ids[id] = true
	}
	if len(ids) != 100 {
		t.Errorf("%d different ids", len(ids))
	}
}

func TestPDFTheXmpDocumentIdIsTheTrailerId(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	NewPage(doc.pdf, letter.Portrait())
	raw := doc.complete()
	if !strings.Contains(string(raw), "<xapMM:DocumentID>uuid:"+testTrailerID(raw)+"</xapMM:DocumentID>") {
		t.Error("the XMP DocumentID is not the trailer ID")
	}
}

func TestPDFTheInfoDictionaryHasTheTitleInUtf16(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetTitle("Grüße (x)").SetAuthor("Author")
	line := NewTextLine(testHelvetica(doc.pdf), "x")
	line.SetLocation(10, 10)
	line.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	info := testFindObject(testRead(t, doc.complete()), "/Producer")
	if info == nil {
		t.Fatal("no info dictionary")
	}
	if got := testUTF16Hex(t, info.GetValue("/Title")); got != "Grüße (x)" {
		t.Errorf("title %q", got)
	}
	if got := testUTF16Hex(t, info.GetValue("/Author")); got != "Author" {
		t.Errorf("author %q", got)
	}
}

func TestPDFPageSizesSurviveReadingBack(t *testing.T) {
	objects := testRead(t, testDocument(letter.Portrait(), a4.Landscape(), letter.Portrait()))
	pages := testNewPDF().GetPageObjects(objects)
	if len(pages) != 3 {
		t.Fatalf("pages %d", len(pages))
	}
	sizes := [][2]float32{{612, 792}, {842, 595}, {612, 792}}
	for i, size := range sizes {
		got := pages[i].GetPageSize()
		if got.GetWidth() != size[0] || got.GetHeight() != size[1] {
			t.Errorf("page %d: %v x %v", i, got.GetWidth(), got.GetHeight())
		}
	}
}

func TestPDFReadsAPdfWhoseCrossReferenceOffsetIsWrong(t *testing.T) {
	raw := string(testDocument(letter.Portrait(), letter.Portrait()))
	damaged := regexp.MustCompile("startxref\n\\d+\n").ReplaceAllString(raw, "startxref\n12\n")
	if got := len(testNewPDF().GetPageObjects(testRead(t, []byte(damaged)))); got != 2 {
		t.Errorf("pages %d", got)
	}
}

func TestPDFReadsAPdfWithABlankPage(t *testing.T) {
	doc := testNewDoc()
	NewPage(doc.pdf, letter.Portrait())
	if got := len(testNewPDF().GetPageObjects(testRead(t, doc.complete()))); got != 1 {
		t.Errorf("pages %d", got)
	}
}

func TestPDFCompleteFlushesTheWriter(t *testing.T) {
	// Java's complete closes the stream; Go's Complete flushes the writer,
	// and closes the file that NewPDFFile made.
	doc := testNewDoc()
	NewPage(doc.pdf, letter.Portrait())
	doc.pdf.Complete()
	if !strings.HasSuffix(doc.buf.String(), "%%EOF\n") {
		t.Error("the writer was not flushed")
	}
}
