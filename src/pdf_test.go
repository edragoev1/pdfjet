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
	"github.com/edragoev1/pdfjet/v9/src/corefont"
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

func TestPDFAPointIsAnArtifact(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	page := NewPage(doc.pdf, letter.Portrait())
	NewPoint(50, 50).DrawOn(page)
	content := testContent(page)
	if !strings.HasPrefix(content, "/Artifact BMC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the point is not an artifact: %q", content)
	}
}

func TestPDFALinkIsInALinkElementAndAnyOtherAnnotationInAnAnnotElement(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testStreamFont(t, doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(font, "PDFjet").SetURIAction("https://pdfjet.com").SetLocation(70, 80).DrawOn(page)
	note := NewTextAnnotation()
	note.SetLocation(70, 100)
	note.SetContents("A note")
	note.DrawOn(page)
	raw := string(doc.complete())
	if n := strings.Count(raw, "/S /Link\n"); n != 1 {
		t.Errorf("%d Link elements", n)
	}
	if n := strings.Count(raw, "/S /Annot\n"); n != 1 {
		t.Errorf("%d Annot elements", n)
	}
}

// The PDFs that are not valid, as the Go fuzz target of the reader found
// them: each fails with a message or reads what it can, and none reads past
// the file, allocates what the file does not have or traps in Swift.

// testReadError returns the message that reading the PDF fails with, or
// "(no error)" when it is read.
func testReadError(t *testing.T, raw string) string {
	t.Helper()
	if _, err := testNewPDF().Read([]byte(raw)); err != nil {
		return err.Error()
	}
	return "(no error)"
}

func TestPDFAnObjectNumberedHigherThanTheFileHasBytesIsRefused(t *testing.T) {
	// One object of every number up to the one it says would take gigabytes
	// of memory for a file of 31 bytes.
	testWant(t, "The PDF of 31 bytes cannot hold an object numbered 44444441.",
		testReadError(t, "44444441 0 obj/Filter/Fl streil"))
}

func TestPDFAStreamLongerThanTheFileIsRefused(t *testing.T) {
	// The bytes of the stream are counted before it is made, so that a file
	// of a few bytes that says its stream is a gigabyte takes no memory.
	testWant(t, "The stream of an object is not in the PDF.",
		testReadError(t, "1 0 obj<</Length 1000000000>>stream\nx\nendstream endobj"))
}

func TestPDFAnObjectStreamThatIsNotANumberIsRefused(t *testing.T) {
	testWant(t, `The object stream of the PDF is malformed: "x" is not a number.`,
		testReadError(t, "1 0 obj<</Type/ObjStm/First x>>stream\n\nendstream endobj"))
	// An object stream with no stream of its own has no objects.
	testWant(t, "(no error)", testReadError(t, "1 0 obj 1 0 obj/Type/ObjStm/First 0"))
}

func TestPDFAReferenceToAnObjectThatIsNotThereHasNoContents(t *testing.T) {
	// A page whose /Contents names an object the PDF does not have, and one
	// whose dictionary ends where a value belongs.
	objects, err := testNewPDF().Read([]byte(
		"1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
			"2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
			"3 0 obj<</Type/Page/Parent 2 0 R/Contents 99 0 R>>endobj\n"))
	if err != nil {
		t.Fatal(err)
	}
	pages := testNewPDF().GetPageObjects(objects)
	if len(pages) != 1 {
		t.Fatalf("pages %d", len(pages))
	}
	if pages[0].GetContentObject(objects) != nil {
		t.Error("a page whose contents are not in the PDF has contents")
	}
	if pages[0].GetResourcesObject(objects) != nil {
		t.Error("a page with no resources has resources")
	}
	testWant(t, "", pages[0].GetValue("/Contents2"))
}

func TestPDFADictionaryThatEndsInTheMiddleOfAValueIsClosedThere(t *testing.T) {
	objects, err := testNewPDF().Read([]byte("1 0 obj<</Type/Catalog/Kids[3 0 R\n"))
	if err != nil {
		t.Fatal(err)
	}
	if len(objects) != 1 {
		t.Fatalf("objects %d", len(objects))
	}
	testWant(t, "[ 3 0 R ]", objects[0].GetValue("/Kids"))
	testWant(t, "", objects[0].GetValue("/Nothing"))
}

func TestPDFALengthThatIsNotANumberIsAnError(t *testing.T) {
	// The /Length is read for every stream, so a PDF that writes anything
	// there, or that ends before it, has to fail with a message: it read past
	// the tokens in the Java, C# and Go ports.
	testWant(t, "The /Length of a stream is not a number.",
		testReadError(t, "1 0 obj<</Length stream\nx\nendstream endobj"))
	testWant(t, "The stream of an object is not in the PDF.",
		testReadError(t, "1 0 obj<</Length 1 stream"))
	// A /Length that names an object with no length of its own.
	testWant(t, "The /Length of a stream is not a number.",
		testReadError(t, "1 0 obj<</Length 2 0 R>>stream\nx\nendstream endobj\n2 0 obj endobj"))
}

func TestPDFAMediaBoxThatIsNotFourNumbersIsLetterSize(t *testing.T) {
	// The size of a page is read from its /MediaBox, which a PDF that was
	// read can write as anything: it was four tokens past the key, which
	// trapped in Swift and read past the tokens in the other ports.
	for _, box := range []string{"[0 0 612", "[a b c d]", "5 0 R", "[]", ""} {
		objects := testRead(t, testPDFWithObjects(
			"<< /Type /Catalog /Pages 2 0 R >>",
			"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
			"<< /Type /Page /Parent 2 0 R /MediaBox "+box+" >>"))
		size := testNewPDF().GetPageObjects(objects)[0].GetPageSize()
		if size.GetWidth() != 612 || size.GetHeight() != 792 {
			t.Errorf("%s: %gx%g", box, size.GetWidth(), size.GetHeight())
		}
	}
}

func TestPDFThePageSizeIsTheDistanceBetweenTheCornersOfTheMediaBox(t *testing.T) {
	// The box is a rectangle of two opposite corners, in either order, and
	// its origin is not always 0 0: the size is what lies between them.
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [9 9 621 801] >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [612 792 0 0] >>"))
	for _, page := range testNewPDF().GetPageObjects(objects) {
		size := page.GetPageSize()
		if size.GetWidth() != 612 || size.GetHeight() != 792 {
			t.Errorf("%gx%g", size.GetWidth(), size.GetHeight())
		}
	}
}

// The pages of a PDF that a stamp is drawn on, each with a dictionary that
// ends where a value belongs or that names an object the file does not have.
var testBrokenPages = []string{
	"<< /Type /Page /Parent 2 0 R /Resources",
	"<< /Type /Page /Parent 2 0 R /Resources 99 0 R /Contents 99 0 R >>",
	"<< /Type /Page /Parent 2 0 R /Resources << /Font 99 0 R >> /Contents",
	"<< /Type /Page /Parent 2 0 R /Resources << /XObject 99 0 R >> /Contents 4 0 R >>",
	"<< /Type /Page /Parent 2 0 R /Resources << >> /Contents 4 0 R /MediaBox [0 0 612",
	"<< /Type /Page /Parent 2 0 R /Contents [ 4 0 R",
	"<< /Type /Page /Parent 2 0 R /Contents 4",
	"<< /Type /Page /Parent 2 0 R /Resources << /ExtGState 99 0 R >> /Contents 4 0 R >>",
}

func TestPDFAPageHoldsTheEntriesItInheritsFromThePageTree(t *testing.T) {
	// /Resources, /MediaBox, /CropBox and /Rotate can be written once on a
	// node above the pages, and a page of another program's PDF often carries
	// none of them: such a page read as letter size whatever its size was,
	// and had no resources, so its fonts and images were not copied. The page
	// that has one of its own keeps it.
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 /MediaBox [0 0 595 842]"+
			" /Resources << /Font << /F1 5 0 R >> >> /Rotate 90 >>",
		"<< /Type /Page /Parent 2 0 R >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
		"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"))
	pages := testNewPDF().GetPageObjects(objects)
	size := pages[0].GetPageSize()
	if size.GetWidth() != 595 || size.GetHeight() != 842 {
		t.Errorf("the inherited size is %gx%g", size.GetWidth(), size.GetHeight())
	}
	if pages[0].GetResourcesObject(objects) == nil {
		t.Error("a page that inherits its resources has none")
	}
	testWant(t, "90", pages[0].GetValue("/Rotate"))
	if pages[1].GetPageSize().GetWidth() != 612 {
		t.Error("a page with a box of its own lost it")
	}
	// The entries are added once, however often the pages are returned.
	count := len(pages[0].GetDict())
	if again := len(testNewPDF().GetPageObjects(objects)[0].GetDict()); again != count {
		t.Errorf("the page grew from %d to %d tokens", count, again)
	}
}

func TestPDFAResourcesObjectThatNamesItsOwnPageIsAddedToOnce(t *testing.T) {
	// The font was added to the page for every "/Resources" left in its
	// dictionary, and adding it grew that dictionary, so a resources object
	// whose /Font names the page itself never ended.
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /Resources 4 0 R /Contents 5 0 R >>",
		"<< /Font 3 0 R >>",
		"<< /Length 5 >>\nstream\nHELLO\nendstream"))
	page := testNewPDF().GetPageObjects(objects)[0]
	page.AddCoreFontResource(corefont.Helvetica(), &objects)
	if len(page.GetDict()) > 32 {
		t.Errorf("the page grew to %d tokens", len(page.GetDict()))
	}
}

func TestPDFAStampOnAPageWhoseDictionaryIsBrokenDrawsNothing(t *testing.T) {
	// Every one of these crashed a port: the methods that add a font, an
	// image, a content stream or a graphics state to a page that was read
	// indexed its dictionary and the objects of the PDF unchecked.
	for _, page := range testBrokenPages {
		objects := testRead(t, testPDFWithObjects(
			"<< /Type /Catalog /Pages 2 0 R >>",
			"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
			page,
			"<< /Length 5 >>\nstream\nHELLO\nendstream"))
		pages := testNewPDF().GetPageObjects(objects)
		if len(pages) != 1 {
			t.Fatalf("%s: pages %d", page, len(pages))
		}
		obj := pages[0]
		obj.AddCoreFontResource(corefont.Helvetica(), &objects)
		obj.AddContent([]byte("BT ET\n"), &objects)
		obj.AddPrefixContent([]byte("q Q\n"), &objects)
		obj.SetGraphicsState(NewGraphicsState().SetAlphaStroking(0.5), &objects)
		// The objects that were read are written as they are.
		stamped := testNewDoc()
		if err := stamped.pdf.AddObjects(objects); err != nil {
			t.Fatalf("%s: %v", page, err)
		}
		stamped.complete()
		// The fonts and the images of the pages are copied into a new PDF.
		imported := testNewDoc()
		imported.pdf.AddResourceObjects(objects)
		NewPage(imported.pdf, letter.Portrait())
		imported.complete()
	}
}
