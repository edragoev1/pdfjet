// readbudget_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"compress/zlib"
	"fmt"
	"runtime"
	"strings"
	"testing"
)

// testDeflate returns the data compressed with Flate.
func testDeflate(data []byte) []byte {
	var buf bytes.Buffer
	w := zlib.NewWriter(&buf)
	w.Write(data)
	w.Close()
	return buf.Bytes()
}

// testPDFOfStreams returns a PDF, without a cross-reference table, so that it
// is read by scanning, of a catalog, an object stream that holds one object,
// and a content stream: each Flate, decoding to the given number of bytes.
func testPDFOfStreams(objectStreamBytes, contentBytes int) []byte {
	var pdf bytes.Buffer
	pdf.WriteString("%PDF-1.5\n")
	pdf.WriteString("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
	pdf.WriteString("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
	// Object 5 in the object stream, padded to the length asked for
	body := "5 0 << /Padding /"
	body = body + strings.Repeat("x", max(0, objectStreamBytes-len(body)-3)) + " >>"
	first := len("5 0 ")
	stream := testDeflate([]byte(body))
	fmt.Fprintf(&pdf, "3 0 obj\n<< /Type /ObjStm /N 1 /First %d /Length %d /Filter /FlateDecode >>\nstream\n", first, len(stream))
	pdf.Write(stream)
	pdf.WriteString("\nendstream\nendobj\n")
	content := testDeflate([]byte(strings.Repeat("q Q\n", contentBytes/4)))
	fmt.Fprintf(&pdf, "4 0 obj\n<< /Length %d /Filter /FlateDecode >>\nstream\n", len(content))
	pdf.Write(content)
	pdf.WriteString("\nendstream\nendobj\n")
	pdf.WriteString("trailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n")
	return pdf.Bytes()
}

func testObjectOfNumber(objects []*PDFobj, number int) *PDFobj {
	for _, obj := range objects {
		if obj.GetNumber() == number {
			return obj
		}
	}
	return nil
}

func TestReadAStreamIsDecodedWhenItsDataIsFirstAskedFor(t *testing.T) {
	objects, err := testNewPDF().Read(testPDFOfStreams(100, 4000))
	if err != nil {
		t.Fatal(err)
	}
	content := testObjectOfNumber(objects, 4)
	if content == nil || !content.undecoded || content.data != nil {
		t.Fatalf("the content stream is decoded before it is asked for: %+v", content)
	}
	if data := content.GetData(); len(data) != 4000 || content.undecoded {
		t.Errorf("the content stream decodes to %d bytes, not 4000", len(data))
	}
	// The object stream was read when the PDF was: its object is there.
	if padded := testObjectOfNumber(objects, 5); padded == nil || padded.GetValue("/Padding") == "" {
		t.Errorf("the object of the object stream was not read: %+v", padded)
	}
}

func TestReadTheStreamsOfAPDFDecodeToNoMoreThanTheBudgetTogether(t *testing.T) {
	defer func(was int) { maxDecodedTotal = was }(maxDecodedTotal)
	// 5000 for the streams, and what the one object of the object stream
	// takes of the budget besides
	maxDecodedTotal = 5000 + objectCost

	// The object stream, read with the PDF, is within the budget, and the
	// content stream is decoded up to what is left of it: 4000 of 5000 is
	// left after the object stream of 1000, so a content of 4000 is decoded
	// and one of 4001 is not.
	objects, err := testNewPDF().Read(testPDFOfStreams(1000, 4000))
	if err != nil {
		t.Fatal(err)
	}
	if data := testObjectOfNumber(objects, 4).GetData(); len(data) != 4000 {
		t.Errorf("a content within the budget decodes to %d bytes, not 4000", len(data))
	}
	objects, err = testNewPDF().Read(testPDFOfStreams(1000, 4004))
	if err != nil {
		t.Fatal(err)
	}
	if data := testObjectOfNumber(objects, 4).GetData(); data != nil {
		t.Errorf("a content past the budget decodes to %d bytes, not to nothing", len(data))
	}

	// An object stream past the budget is an error, as the PDF cannot be
	// read without its objects.
	_, err = testNewPDF().Read(testPDFOfStreams(6000, 0))
	if err == nil || err.Error() != fmt.Sprintf("the streams of the PDF decode to more than %d bytes together", 5000+objectCost) {
		t.Errorf("an object stream past the budget: %v", err)
	}
}

func TestReadAStreamWhoseDecoderFailsHasNoDataAndDoesNotPanic(t *testing.T) {
	// No PDF is known to make a decoder fail with a runtime error, which
	// 1,218 corrupted files did not, so one is made to here: a RunLength
	// stream, with a budget below zero that no PDF can have, makes a slice
	// of a negative capacity. GetData is called after Read, which recovers
	// nothing then, and it has no data, as the other ports have.
	objects, err := testNewPDF().Read(testPDFOfStreams(100, 4000))
	if err != nil {
		t.Fatal(err)
	}
	content := testObjectOfNumber(objects, 4)
	for i, token := range content.dict {
		if token == "/FlateDecode" {
			content.dict[i] = "/RunLengthDecode"
		}
	}
	content.budget = &decodeBudget{left: -1}
	defer func() {
		if r := recover(); r != nil {
			t.Fatalf("GetData panics: %v", r)
		}
	}()
	if data := content.GetData(); data != nil {
		t.Errorf("the stream has %d bytes of data, not none", len(data))
	}
	if content.GetData() != nil {
		t.Error("the stream has data the second time it is asked for")
	}
}

// A /Contents array that names one stream again and again is charged to the
// budget for each copy, as its data is copied each time: past the budget the
// page has no content (review of 11 October 2026, #29).
func TestReadAContentStreamNamedAgainIsChargedAgain(t *testing.T) {
	defer func(was int) { maxDecodedTotal = was }(maxDecodedTotal)
	maxDecodedTotal = 5000
	page := func(times int) *PDFobj {
		obj := newPDFobj()
		obj.dict = append(obj.dict, "/Contents", "[")
		for range times {
			obj.dict = append(obj.dict, "4", "0", "R")
		}
		obj.dict = append(obj.dict, "]")
		return obj
	}
	// 100 of the object stream, 1000 of the content decoded once, 1001 for
	// each copy after the first: three copies take 3102 of 5000
	objects, err := testNewPDF().Read(testPDFOfStreams(100, 1000))
	if err != nil {
		t.Fatal(err)
	}
	if content := page(3).GetContentObject(objects); content == nil || len(content.data) != 3003 {
		t.Errorf("three copies within the budget: %v", content)
	}
	// Five take 5104, past it
	objects, err = testNewPDF().Read(testPDFOfStreams(100, 1000))
	if err != nil {
		t.Fatal(err)
	}
	if content := page(5).GetContentObject(objects); content != nil {
		t.Errorf("five copies past the budget make a content of %d bytes, not none", len(content.data))
	}
}

// An object stream gives no more objects than its /N says, and each object
// takes from the PDF's budget: a header of millions of "1 0 " is refused, not
// millions of objects (review of 11 October 2026, #30).
func TestReadAnObjectStreamGivesNoMoreObjectsThanItsN(t *testing.T) {
	objectStream := func(n int, header string) []byte {
		var pdf bytes.Buffer
		pdf.WriteString("%PDF-1.5\n")
		pdf.WriteString("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
		pdf.WriteString("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
		body := header + "<< /A 1 >> << /B 2 >>"
		stream := testDeflate([]byte(body))
		fmt.Fprintf(&pdf, "3 0 obj\n<< /Type /ObjStm /N %d /First %d /Length %d /Filter /FlateDecode >>\nstream\n", n, len(header), len(stream))
		pdf.Write(stream)
		pdf.WriteString("\nendstream\nendobj\n")
		pdf.WriteString("trailer\n<< /Root 1 0 R /Size 7 >>\n%%EOF\n")
		return pdf.Bytes()
	}
	// /N 1 of a header of two: object 5 alone
	objects, err := testNewPDF().Read(objectStream(1, "5 0 6 11 "))
	if err != nil {
		t.Fatal(err)
	}
	if testObjectOfNumber(objects, 5) == nil || testObjectOfNumber(objects, 6) != nil {
		t.Errorf("an object stream of /N 1 gives objects other than its first")
	}
	// The header is read no further than its /N: one of 16 MB was split
	// into its tokens first (review of 12 October 2026, R6)
	var before, after runtime.MemStats
	long := objectStream(1, "5 0 "+strings.Repeat("1 0 ", 4<<20))
	runtime.ReadMemStats(&before)
	_, err = testNewPDF().Read(long)
	runtime.ReadMemStats(&after)
	if err != nil {
		t.Fatal(err)
	}
	if allocated := after.TotalAlloc - before.TotalAlloc; allocated > 64<<20 {
		t.Errorf("%d MB allocated", allocated>>20)
	}
	// A header of more objects than the budget takes is refused
	defer func(was int) { maxDecodedTotal = was }(maxDecodedTotal)
	maxDecodedTotal = 100 * objectCost
	header := strings.Repeat("1 0 ", 1000)
	if _, err := testNewPDF().Read(objectStream(1000, header)); err == nil {
		t.Error("an object stream of 1000 objects past the budget is read")
	}
}

// A cross-reference stream of more objects than the PDF has room for, or of
// offsets past its end, is not taken: the objects are found by scanning, and
// the entries are not all kept first (review of 11 October 2026, #31).
func TestReadACrossReferenceStreamOfTooManyEntriesIsNotTaken(t *testing.T) {
	var pdf bytes.Buffer
	pdf.WriteString("%PDF-1.5\n")
	pdf.WriteString("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
	pdf.WriteString("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
	// 200,000 entries of type 1, at offset 0, with /W [1 0 0]
	rows := bytes.Repeat([]byte{1}, 200000)
	stream := testDeflate(rows)
	xref := pdf.Len()
	fmt.Fprintf(&pdf, "3 0 obj\n<< /Type /XRef /W [1 0 0] /Size 200000 /Root 1 0 R /Length %d /Filter /FlateDecode >>\nstream\n", len(stream))
	pdf.Write(stream)
	pdf.WriteString("\nendstream\nendobj\n")
	fmt.Fprintf(&pdf, "startxref\n%d\n%%%%EOF\n", xref)
	objects, err := testNewPDF().Read(pdf.Bytes())
	if err != nil {
		t.Fatal(err)
	}
	if testObjectOfNumber(objects, 1) == nil || testObjectOfNumber(objects, 2) == nil {
		t.Error("the objects are not found by scanning")
	}
	if len(objects) > 10 {
		t.Errorf("%d objects of a PDF of three", len(objects))
	}
}

// The output of ASCII85Decode is taken from the budget, as the other filters'
// is: a stream of 'z's after FlateDecode decodes to four bytes for each, past
// the budget (review of 11 October 2026, #32).
func TestReadTheOutputOfASCII85IsChargedToTheBudget(t *testing.T) {
	defer func(was int) { maxDecodedTotal = was }(maxDecodedTotal)
	maxDecodedTotal = 5000
	pdf := func(zs int) []byte {
		var b bytes.Buffer
		b.WriteString("%PDF-1.5\n")
		b.WriteString("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
		b.WriteString("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
		stream := testDeflate([]byte(strings.Repeat("z", zs) + "~>"))
		fmt.Fprintf(&b, "4 0 obj\n<< /Length %d /Filter [/FlateDecode /ASCII85Decode] >>\nstream\n", len(stream))
		b.Write(stream)
		b.WriteString("\nendstream\nendobj\n")
		b.WriteString("trailer\n<< /Root 1 0 R /Size 5 >>\n%%EOF\n")
		return b.Bytes()
	}
	// 500 'z's, 502 bytes deflated and 2000 decoded, within it
	objects, err := testNewPDF().Read(pdf(500))
	if err != nil {
		t.Fatal(err)
	}
	if data := testObjectOfNumber(objects, 4).GetData(); len(data) != 2000 {
		t.Errorf("500 z's decode to %d bytes, not 2000", len(data))
	}
	// 2000, 8000 bytes, past it
	objects, err = testNewPDF().Read(pdf(2000))
	if err != nil {
		t.Fatal(err)
	}
	if data := testObjectOfNumber(objects, 4).GetData(); data != nil {
		t.Errorf("2000 z's past the budget decode to %d bytes, not to nothing", len(data))
	}
}

// AddObjects writes every object and reference at generation 0, as the
// cross-reference table has them: a PDF whose objects had generation 1 lost
// its pages (review of 11 October 2026, #4).
func TestReadAddObjectsWritesTheGenerationsAsZero(t *testing.T) {
	var b bytes.Buffer
	b.WriteString("%PDF-1.4\n")
	objects := []string{
		"<< /Type /Catalog /Pages 2 1 R >>",
		"<< /Type /Pages /Kids [3 1 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 1 R /MediaBox [0 0 612 792] >>",
	}
	offsets := []int{}
	for i, object := range objects {
		offsets = append(offsets, b.Len())
		fmt.Fprintf(&b, "%d 1 obj\n%s\nendobj\n", i+1, object)
	}
	xref := b.Len()
	fmt.Fprintf(&b, "xref\n0 4\n0000000000 65535 f \n")
	for _, offset := range offsets {
		fmt.Fprintf(&b, "%010d 00001 n \n", offset)
	}
	fmt.Fprintf(&b, "trailer\n<< /Root 1 1 R /Size 4 >>\nstartxref\n%d\n%%%%EOF\n", xref)
	read := testRead(t, b.Bytes())
	doc := testNewDoc()
	if err := doc.pdf.AddObjects(read); err != nil {
		t.Fatal(err)
	}
	written := doc.complete()
	if strings.Contains(string(written), " 1 R") || strings.Contains(string(written), " 1 obj") {
		t.Errorf("a generation of 1 is written: %q", written)
	}
	if pages := testNewPDF().GetPageObjects(testRead(t, written)); len(pages) != 1 {
		t.Errorf("%d pages, not 1", len(pages))
	}
}

// A page that /Kids lists again has its resources added once, and a name is
// found in what was added without a look through it: one page listed 2,000
// times over 2,000 fonts took two minutes (review of 11 October 2026, #35).
func TestReadAddResourceObjectsAddsARepeatedPageOnce(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 3 0 R 3 0 R] /Count 3 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> >>",
		"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
		"<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"))
	pdf := testNewPDF()
	pdf.AddResourceObjects(objects)
	testWant(t, "/F1 4 0 R /F2 5 0 R", strings.Join(pdf.importedFonts, " "))
	if pdf.importedFontNames["/F2"] != 4 {
		t.Errorf("/F2 is at %d of the fonts, not 4", pdf.importedFontNames["/F2"])
	}
}

// testRepeatedPage returns the objects of a PDF of one page of n fonts that
// /Kids lists n times, under a node of the /MediaBox.
func testRepeatedPage(n int) []string {
	fonts := make([]string, n)
	kids := make([]string, n)
	for i := range n {
		fonts[i] = fmt.Sprintf("/F%d 4 0 R", i)
		kids[i] = "3 0 R"
	}
	return []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		fmt.Sprintf("<< /Type /Pages /Kids [%s] /Count %d /MediaBox [0 0 612 792] >>", strings.Join(kids, " "), n),
		fmt.Sprintf("<< /Type /Page /Parent 2 0 R /Resources << /Font << %s >> >> >>", strings.Join(fonts, " ")),
		"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
	}
}

// A page that /Kids lists again is returned again, but has its entries added
// once: one page of 2,000 fonts listed 2,000 times copied the page at each,
// 3.7 GB in Go for a file of 37 KB (review of 12 October 2026, R5).
func TestReadARepeatedPageHasItsEntriesAddedOnce(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(testRepeatedPage(2000)...))
	var before, after runtime.MemStats
	runtime.ReadMemStats(&before)
	pages := testNewPDF().GetPageObjects(objects)
	merged := testNewDoc()
	if err := merged.pdf.Merge(objects); err != nil {
		t.Fatal(err)
	}
	runtime.ReadMemStats(&after)
	if len(pages) != 2000 || pages[0] != pages[1999] {
		t.Errorf("%d pages", len(pages))
	}
	testWant(t, "[ 0 0 612 792 ]", pages[0].GetValue("/MediaBox"))
	if allocated := after.TotalAlloc - before.TotalAlloc; allocated > 64<<20 {
		t.Errorf("%d MB allocated", allocated>>20)
	}
}

// testHugeInheritedBox returns the objects of a PDF of n pages under a node
// of a /CropBox of 20,000 numbers.
func testHugeInheritedBox(n int) []string {
	kids := make([]string, n)
	for i := range n {
		kids[i] = fmt.Sprintf("%d 0 R", 3+i)
	}
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		fmt.Sprintf("<< /Type /Pages /Kids [%s] /Count %d /MediaBox [0 0 612 792] /CropBox [%s] >>",
			strings.Join(kids, " "), n, strings.Repeat(" 1", 20000)),
	}
	for range n {
		objects = append(objects, "<< /Type /Page /Parent 2 0 R >>")
	}
	return objects
}

// An inherited value too large for what it is, as a /CropBox of 20,000
// numbers, is not inherited: 1,000 pages under it, a PDF of 117 KB, took
// 6.2 GB in Go (review of 12 October 2026, R4).
func TestReadAnInheritedValueTooLargeForWhatItIsIsNotInherited(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(testHugeInheritedBox(1000)...))
	var before, after runtime.MemStats
	runtime.ReadMemStats(&before)
	pages := testNewPDF().GetPageObjects(objects)
	merged := testNewDoc()
	if err := merged.pdf.Merge(objects); err != nil {
		t.Fatal(err)
	}
	runtime.ReadMemStats(&after)
	if len(pages) != 1000 {
		t.Errorf("%d pages", len(pages))
	}
	testWant(t, "[ 0 0 612 792 ]", pages[0].GetValue("/MediaBox"))
	testWant(t, "", pages[0].GetValue("/CropBox"))
	if allocated := after.TotalAlloc - before.TotalAlloc; allocated > 64<<20 {
		t.Errorf("%d MB allocated", allocated>>20)
	}
}

// testSharedResources returns the objects of a PDF of n pages that share one
// /Resources of n fonts: an object of its own, or a dictionary in their node.
func testSharedResources(n int, inNode bool) []string {
	fonts := make([]string, n)
	kids := make([]string, n)
	for i := range n {
		fonts[i] = fmt.Sprintf("/F%d 4 0 R", i)
		kids[i] = fmt.Sprintf("%d 0 R", 5+i)
	}
	resources := "<< /Font << " + strings.Join(fonts, " ") + " >> >>"
	node := fmt.Sprintf("<< /Type /Pages /Kids [%s] /Count %d >>", strings.Join(kids, " "), n)
	page := "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources 3 0 R >>"
	if inNode {
		node = fmt.Sprintf("<< /Type /Pages /Kids [%s] /Count %d /Resources %s >>", strings.Join(kids, " "), n, resources)
		page = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"
	}
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		node,
		resources,
		"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
	}
	for range n {
		objects = append(objects, page)
	}
	return objects
}

// Pages that share their resources, as an object of their own or from their
// node, have them added once: 4,000 pages of one resources object of 4,000
// fonts allocated 4.8 GB in Go (review of 12 October 2026, R8).
func TestReadSharedResourcesAreAddedOnce(t *testing.T) {
	for _, inNode := range []bool{false, true} {
		objects := testRead(t, testPDFWithObjects(testSharedResources(2000, inNode)...))
		pdf := testNewPDF()
		var before, after runtime.MemStats
		runtime.ReadMemStats(&before)
		pdf.AddResourceObjects(objects)
		runtime.ReadMemStats(&after)
		if len(pdf.importedFonts) != 4*2000 || pdf.importedFontNames["/F1999"] != 4*1999 {
			t.Errorf("in the node %v: %d tokens of fonts", inNode, len(pdf.importedFonts))
		}
		if allocated := after.TotalAlloc - before.TotalAlloc; allocated > 64<<20 {
			t.Errorf("in the node %v: %d MB allocated", inNode, allocated>>20)
		}
	}
}
