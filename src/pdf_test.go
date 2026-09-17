// pdf_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"os"
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
	if err := doc.pdf.Complete(); err != nil {
		t.Fatal(err)
	}
	if !strings.HasSuffix(doc.buf.String(), "%%EOF\n") {
		t.Error("the writer was not flushed")
	}
}

func TestPDFCrossReferenceOffsetsAreTenDigits(t *testing.T) {
	for offset, want := range map[int64]string{17: "0000000017", 9999999999: "9999999999"} {
		entry, err := xrefOffset(offset)
		if err != nil {
			t.Error(err)
		}
		testWant(t, want, entry)
	}
	// Java throws an IOException; Go returns the error, and Complete returns it.
	_, err := xrefOffset(10000000000)
	if err == nil {
		t.Fatal("no error")
	}
	testWant(t, "The PDF is too large for a cross-reference table: an object starts at byte 10000000000.",
		err.Error())
}

// testPDFWithStream is a PDF with one stream whose data starts with a line
// feed, the byte that ends the stream keyword. Every stream of an encrypted
// PDF starts with a random IV, so one in 256 of them starts that way.
func testPDFWithStream(data string) []byte {
	o1 := "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
	o2 := "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n"
	o3 := "3 0 obj\n<< /Length " + strconv.Itoa(len(data)) + " >>\nstream\n" + data + "\nendstream\nendobj\n"
	header := "%PDF-1.4\n"
	off1 := len(header)
	off2 := off1 + len(o1)
	off3 := off2 + len(o2)
	xref := off3 + len(o3)
	body := header + o1 + o2 + o3 +
		"xref\n0 4\n0000000000 65535 f \n" +
		fmt.Sprintf("%010d 00000 n \n%010d 00000 n \n%010d 00000 n \n", off1, off2, off3) +
		"trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n" + strconv.Itoa(xref) + "\n%%EOF\n"
	return []byte(body)
}

func TestPDFAStreamThatStartsWithALineFeedKeepsIt(t *testing.T) {
	for _, obj := range testRead(t, testPDFWithStream("\nHELLO")) {
		if obj.GetNumber() == 3 {
			if got := string(obj.GetData()); got != "\nHELLO" {
				t.Errorf("got %q", got)
			}
			return
		}
	}
	t.Error("object 3 not read")
}

func TestPDFAnEmptyDocumentPropertyIsNotWritten(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetTitle("").SetAuthor("").SetSubject("").SetKeywords("").SetCreator("")
	NewPage(doc.pdf, letter.Portrait())
	raw := string(doc.complete())
	for _, key := range []string{"/Title", "/Author", "/Subject", "/Keywords", "/Creator"} {
		if strings.Contains(raw, key) {
			t.Errorf("%s was written", key)
		}
	}
}

func TestPDFNewPDFReaderReadsADocument(t *testing.T) {
	objects, err := NewPDFReader().Read(testDocument(letter.Portrait(), a4.Portrait()))
	if err != nil {
		t.Fatal(err)
	}
	if pages := NewPDFReader().GetPageObjects(objects); len(pages) != 2 {
		t.Errorf("pages %d", len(pages))
	}
}

func TestPDFNewPDFFileReportsAFileThatCannotBeCreated(t *testing.T) {
	if _, err := NewPDFFile("/no/such/directory/out.pdf"); err == nil {
		t.Error("no error")
	}
}

func TestPDFAShapeWithoutADescriptionWritesNoAltText(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	page := NewPage(doc.pdf, letter.Portrait())
	NewLine(10, 10, 100, 10).DrawOn(page)
	NewLine(10, 20, 100, 20).SetAltDescription("A rule").DrawOn(page)
	raw := string(doc.complete())
	if n := strings.Count(raw, "/Alt <"); n != 1 {
		t.Errorf("%d /Alt entries", n)
	}
	if strings.Contains(raw, "/ActualText") {
		t.Error("an /ActualText entry")
	}
}

// testPageNumbers returns the numbers of the page objects, in page order.
func testPageNumbers(t *testing.T, pdf []byte) []string {
	t.Helper()
	numbers := make([]string, 0)
	for _, page := range NewPDFReader().GetPageObjects(testRead(t, pdf)) {
		numbers = append(numbers, strconv.Itoa(page.GetNumber()))
	}
	return numbers
}

func testStreamFont(t *testing.T, pdf *PDF) *Font {
	t.Helper()
	file, err := os.Open(testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
	if err != nil {
		t.Skip("the fonts directory is not here")
	}
	defer file.Close()
	return NewFont(pdf, file)
}

func TestPDFADetachedPageThatIsNeverAddedLeavesNoTrace(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testStreamFont(t, doc.pdf)
	// A dry run, like one that measures the text, on a page that is never added.
	dry := NewPageDetached(doc.pdf, letter.Portrait())
	NewTextLine(font, "PDFjet").SetURIAction("https://pdfjet.com").SetLocation(70, 80).DrawOn(dry)
	page1 := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(font, "Go to page 2").SetGoToAction("dest2").SetLocation(70, 80).DrawOn(page1)
	page2 := NewPage(doc.pdf, letter.Portrait())
	page2.AddDestination("dest2", 100)
	pdf := doc.complete()

	raw := string(pdf)
	if strings.Contains(raw, "/Pg 0 0 R") {
		t.Error("a structure element of a page that is not in the document")
	}
	if n := strings.Count(raw, "/Type /Annot\n"); n != 1 {
		t.Errorf("%d annotations", n)
	}
	// The link leads to the second page, not to the object before it.
	dest := regexp.MustCompile(`/Dest \[(\d+) 0 R`).FindStringSubmatch(raw)
	if dest == nil {
		t.Fatal("no /Dest")
	}
	testWant(t, testPageNumbers(t, pdf)[1], dest[1])
}

func TestPDFTheStructureTreeFollowsThePagesNotTheOrderTheyWereDrawnIn(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testStreamFont(t, doc.pdf)
	second := NewPageDetached(doc.pdf, letter.Portrait())
	NewTextLine(font, "Second").SetLocation(70, 80).DrawOn(second)
	first := NewPageDetached(doc.pdf, letter.Portrait())
	NewTextLine(font, "First").SetLocation(70, 80).DrawOn(first)
	doc.pdf.AddPage(first)
	doc.pdf.AddPage(second)
	pdf := doc.complete()

	// The structure elements are written, and listed by the document element,
	// in the order of their pages.
	pages := testPageNumbers(t, pdf)
	pg := regexp.MustCompile(`/Pg (\d+) 0 R`).FindAllStringSubmatch(string(pdf), -1)
	if len(pg) < 2 {
		t.Fatalf("%d structure elements", len(pg))
	}
	testWant(t, pages[0], pg[0][1])
	testWant(t, pages[1], pg[1][1])
}

func TestPDFTextStringsAreUtf16SoThatEveryReaderDecodesThem(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	page := NewPage(doc.pdf, letter.Portrait())
	NewLine(10, 20, 100, 20).SetAltDescription("Gr\u00fc\u00dfe \u2013 \u7dda").DrawOn(page)
	element := testFindObject(testRead(t, doc.complete()), "/Alt")
	if element == nil {
		t.Fatal("no /Alt")
	}
	if alt := strings.ToLower(element.GetValue("/Alt")); !strings.HasPrefix(alt, "<feff") {
		t.Errorf("alt %s", alt)
	}
	testWant(t, "Gr\u00fc\u00dfe \u2013 \u7dda", testUTF16Hex(t, element.GetValue("/Alt")))
}

func testPDFWithObjects(objects ...string) []byte {
	var sb strings.Builder
	sb.WriteString("%PDF-1.4\n")
	offsets := make([]int, 0)
	for i, object := range objects {
		offsets = append(offsets, sb.Len())
		fmt.Fprintf(&sb, "%d 0 obj\n%s\nendobj\n", i+1, object)
	}
	xref := sb.Len()
	fmt.Fprintf(&sb, "xref\n0 %d\n0000000000 65535 f \n", len(objects)+1)
	for _, offset := range offsets {
		fmt.Fprintf(&sb, "%010d 00000 n \n", offset)
	}
	fmt.Fprintf(&sb, "trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n", len(objects)+1, xref)
	return []byte(sb.String())
}

// A PDF whose font has its widths and its encoding in objects of their own, as
// the PDFs that Word makes do.
func TestPDFAFontIsImportedWithTheObjectsItRefersTo(t *testing.T) {
	source := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]"+
			" /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
		"<< /Length 32 >>\nstream\nBT /F1 24 Tf 72 700 Td (H) Tj ET\nendstream",
		"<< /Type /Font /Subtype /TrueType /BaseFont /Helvetica /FirstChar 72 /LastChar 72"+
			" /Widths 6 0 R /Encoding 7 0 R >>",
		"[ 722 ]",
		"<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [ 72 /H ] >>"))
	doc := testNewDoc()
	doc.pdf.AddResourceObjects(source)
	page := NewPage(doc.pdf, letter.Portrait())
	content := doc.pdf.GetPageObjects(source)[0].GetContentObject(source)
	page.DrawContents(content.GetData(), 792, 0, 0, 1, 1)

	objects := testRead(t, doc.complete())
	testWant(t, "/Font", objects[4].GetValue("/Type"))
	if !contains(objects[5].GetDict(), "722") {
		t.Errorf("widths %v", objects[5].GetDict())
	}
	testWant(t, "/Encoding", objects[6].GetValue("/Type"))
}

func TestPDFAPageTreeThatLoopsIsReadOnce(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 4 0 R 99 0 R] /Count 1 >>",
		"<< /Type /Pages /Parent 2 0 R /Kids [3 0 R 2 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"))
	if n := len(NewPDFReader().GetPageObjects(objects)); n != 1 {
		t.Errorf("%d pages", n)
	}
	doc := testNewDoc()
	if err := doc.pdf.Merge(objects); err != nil {
		t.Fatal(err)
	}
	doc.complete()
}

func TestPDFObjectsWithoutAPageTreeHaveNoPages(t *testing.T) {
	objects := testRead(t, testPDFWithObjects("<< /Type /Catalog >>"))
	if n := len(NewPDFReader().GetPageObjects(objects)); n != 0 {
		t.Errorf("%d pages", n)
	}
}

func TestPDFTheNameOfAnEmbeddedFileIsATextStringInFAndUF(t *testing.T) {
	doc := testNewDoc()
	page := NewPage(doc.pdf, letter.Portrait())
	file := NewEmbeddedFile(doc.pdf, "\u00dcbersicht \u2013 r\u00e9sum\u00e9.txt", strings.NewReader("Hello"), false)
	NewFileAttachment(file).SetLocation(100, 100).DrawOn(page)
	spec := testFindObject(testRead(t, doc.complete()), "/UF")
	if spec == nil {
		t.Fatal("no /UF")
	}
	testWant(t, "/Filespec", spec.GetValue("/Type"))
	testWant(t, "\u00dcbersicht \u2013 r\u00e9sum\u00e9.txt", testUTF16Hex(t, spec.GetValue("/UF")))
	testWant(t, spec.GetValue("/UF"), spec.GetValue("/F"))
}
