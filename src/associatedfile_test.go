// associatedfile_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/relationship"
)

// The files a document carries with it, which PDF/A-3 calls associated files.

const testXML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<invoice/>\n"

// testAttach embeds the file and says what it holds, how it relates to the
// document and what it is, as a file the document carries has to.
func testAttach(pdf *PDF, fileName, text string) *EmbeddedFile {
	return NewEmbeddedFileWithRelationship(pdf, fileName, strings.NewReader(text), false,
		"text/xml", relationship.Alternative, "The invoice, as data.")
}

// testCarry writes a document of one page that carries the files.
func testCarry(doc *testDoc, fileNames ...string) string {
	for _, fileName := range fileNames {
		doc.pdf.AddAssociatedFile(testAttach(doc.pdf, fileName, testXML))
	}
	line := NewTextLine(testHelvetica(doc.pdf), "Invoice")
	line.SetLocation(50, 50)
	line.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	return string(doc.complete())
}

// testCarried writes a document of PDF/A-3B that carries the files.
func testCarried(fileNames ...string) string {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_3B)
	return testCarry(doc, fileNames...)
}

func TestAssociatedFileTheCatalogSaysWhichFilesTheDocumentCarries(t *testing.T) {
	raw := testCarried("factur-x.xml")
	files := regexp.MustCompile(`/AF \[(\d+) 0 R\]`).FindStringSubmatch(raw)
	if files == nil {
		t.Fatal(raw[strings.LastIndex(raw, "/Type /Catalog"):])
	}
	// The number is the file specification, not the stream of the bytes.
	if !strings.Contains(raw, files[1]+" 0 obj\n<<\n/Type /Filespec") {
		t.Errorf("object %s is not a file specification", files[1])
	}
}

func TestAssociatedFileAReaderFindsTheFileByItsName(t *testing.T) {
	raw := testCarried("factur-x.xml")
	names := regexp.MustCompile(
		`/Names <</EmbeddedFiles <</Names \[<([0-9a-fA-F]+)> (\d+) 0 R\]>>>>`).FindStringSubmatch(raw)
	if names == nil {
		t.Fatal(raw[strings.LastIndex(raw, "/Type /Catalog"):])
	}
	if name := testUTF16Hex(t, names[1]); name != "factur-x.xml" {
		t.Errorf("name %q", name)
	}
	if !strings.Contains(raw, "/AF ["+names[2]+" 0 R]") {
		t.Errorf("the name tree points at object %s, which /AF does not", names[2])
	}
}

// testNamesInTree returns the names of the name tree of /EmbeddedFiles, in
// the order the document writes them in.
func testNamesInTree(t *testing.T, raw string) string {
	t.Helper()
	names := regexp.MustCompile(`/Names \[(.+?)\]>>>>`).FindStringSubmatch(raw)
	if names == nil {
		t.Fatal(raw[strings.LastIndex(raw, "/Type /Catalog"):])
	}
	order := make([]string, 0)
	for _, name := range regexp.MustCompile(`<([0-9a-fA-F]+)>`).FindAllStringSubmatch(names[1], -1) {
		order = append(order, testUTF16Hex(t, name[1]))
	}
	return strings.Join(order, " ")
}

func TestAssociatedFileTheNamesOfTheFilesAreInOrder(t *testing.T) {
	raw := testCarried("invoice.xml", "data.xml", "Notes.txt")
	if got := testNamesInTree(t, raw); got != "Notes.txt data.xml invoice.xml" {
		t.Errorf("order %q", got)
	}
	// The /AF array is the order the files were added in, which the
	// specification leaves to the writer of the document.
	if count := strings.Count(raw, "/Type /Filespec"); count != 3 {
		t.Errorf("%d file specifications", count)
	}
}

func TestAssociatedFileTheNamesAreInTheOrderOfTheirUtf16CodeUnits(t *testing.T) {
	// The other ports compare the names by their UTF-16 code units, where a
	// character outside the basic plane is a pair that starts below U+E000.
	// Go compares strings by their UTF-8 bytes, which would put the ligature
	// first.
	if got := testNamesInTree(t, testCarried("ﬀ.xml", "\U0001F600.xml")); got != "\U0001F600.xml ﬀ.xml" {
		t.Errorf("order %q", got)
	}
}

func TestAssociatedFileTheFileSaysWhatItHoldsAndHowItRelatesToTheDocument(t *testing.T) {
	raw := testCarried("factur-x.xml")
	filespec := strings.Index(raw, "/Type /Filespec")
	dictionary := raw[filespec : filespec+strings.Index(raw[filespec:], "endobj")]
	// Readers of PDF 1.7 look at /UF first and older ones at /F, and PDF/A-3
	// asks for both, in the file specification and in /EF.
	for _, entry := range []string{"/AFRelationship /Alternative\n", "/Desc <", "/F <", "/UF <"} {
		if !strings.Contains(dictionary, entry) {
			t.Errorf("%q is missing:\n%s", entry, dictionary)
		}
	}
	desc := dictionary[strings.Index(dictionary, "/Desc <")+6:]
	if got := testUTF16Hex(t, desc[:strings.Index(desc, ">")+1]); got != "The invoice, as data." {
		t.Errorf("description %q", got)
	}
	stream := regexp.MustCompile(`/EF <</F (\d+) 0 R /UF (\d+) 0 R>>`).FindStringSubmatch(dictionary)
	if stream == nil {
		t.Fatal(dictionary)
	}
	if stream[1] != stream[2] {
		t.Errorf("/F is object %s and /UF is object %s", stream[1], stream[2])
	}

	file := strings.Index(raw, stream[1]+" 0 obj\n<<\n/Type /EmbeddedFile")
	embedded := raw[file : file+strings.Index(raw[file:], "stream\n")]
	size := strconv.Itoa(len(testXML))
	for _, entry := range []string{
		"/Subtype /text#2Fxml\n", "/Params <</Size " + size + " /ModDate (D:", "/Length " + size + "\n"} {
		if !strings.Contains(embedded, entry) {
			t.Errorf("%q is missing:\n%s", entry, embedded)
		}
	}
}

func TestAssociatedFileTheSizeOfACompressedFileIsTheSizeItHadBeforeItWasCompressed(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_3B)
	text := strings.Repeat("<line>The same line, over and over.</line>\n", 1000)
	doc.pdf.AddAssociatedFile(NewEmbeddedFileWithRelationship(doc.pdf, "long.xml",
		strings.NewReader(text), true, "text/xml", relationship.Data, "A long file."))
	line := NewTextLine(testHelvetica(doc.pdf), "x")
	line.SetLocation(50, 50)
	line.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	raw := string(doc.complete())
	if !strings.Contains(raw, "/Params <</Size "+strconv.Itoa(len(text))+" /ModDate (D:") {
		t.Error("the size")
	}
	if !strings.Contains(raw, "/Filter /FlateDecode\n") {
		t.Error("the filter")
	}
	length := regexp.MustCompile(`/Length (\d+)\n`).FindStringSubmatch(
		raw[strings.Index(raw, "/Type /EmbeddedFile"):])
	if length == nil {
		t.Fatal("no length")
	}
	if written, _ := strconv.Atoi(length[1]); written >= len(text)/10 {
		t.Errorf("length %s", length[1])
	}
}

func TestAssociatedFileTheMediaTypeIsWrittenAsANameWhateverItHolds(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_3B)
	doc.pdf.AddAssociatedFile(NewEmbeddedFileWithRelationship(doc.pdf, "sheet.ods",
		strings.NewReader("x"), false, "application/vnd.oasis.opendocument.spreadsheet",
		relationship.Source, "A sheet."))
	doc.pdf.AddAssociatedFile(NewEmbeddedFileWithRelationship(doc.pdf, "odd.bin",
		strings.NewReader("x"), false, "application/x-(odd) #1",
		relationship.Supplement, "Something else."))
	line := NewTextLine(testHelvetica(doc.pdf), "x")
	line.SetLocation(50, 50)
	line.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	raw := string(doc.complete())
	for _, entry := range []string{
		"/Subtype /application#2Fvnd.oasis.opendocument.spreadsheet\n",
		"/Subtype /application#2Fx-#28odd#29#20#231\n",
		"/AFRelationship /Source\n", "/AFRelationship /Supplement\n"} {
		if !strings.Contains(raw, entry) {
			t.Errorf("%q is missing", entry)
		}
	}
}

func TestAssociatedFileADocumentThatCarriesNoFilesSaysNothingAboutThem(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_3B)
	raw := testCarry(doc)
	if strings.Contains(raw, "/AF [") {
		t.Error("the array")
	}
	if strings.Contains(raw, "/EmbeddedFiles") {
		t.Error("the names")
	}
}

func TestAssociatedFileAFileThatSaysNothingAboutItselfIsNotOneTheDocumentCanCarry(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_A_3B)
	pdf.AddAssociatedFile(NewEmbeddedFile(pdf, "factur-x.xml", strings.NewReader(testXML), false))
	if pdf.err == nil || !strings.Contains(pdf.err.Error(), "factur-x.xml") {
		t.Fatalf("recorded %v", pdf.err)
	}
	// The file it does not carry is still embedded, for an annotation of a
	// page to point at, and is written without the entries of PDF/A-3.
	if strings.Contains(pdf.err.Error(), "/AFRelationship") {
		t.Error(pdf.err)
	}
}

func TestAssociatedFileTheDocumentsThatCannotCarryAFileSayTheyCannot(t *testing.T) {
	for _, level := range []compliance.Compliance{
		compliance.PDF_A_1A, compliance.PDF_A_1B,
		compliance.PDF_A_2A, compliance.PDF_A_2B} {
		pdf := testNewPDF()
		pdf.SetCompliance(level)
		pdf.AddAssociatedFile(testAttach(pdf, "factur-x.xml", testXML))
		if pdf.err == nil || !strings.Contains(pdf.err.Error(), level.String()) {
			t.Errorf("%v: recorded %v", level, pdf.err)
		}
	}
	// The documents that can: PDF/A-3, and the ones of no profile at all.
	for _, level := range []compliance.Compliance{
		compliance.PDF_A_3A, compliance.PDF_A_3B,
		compliance.PDF_1_7, compliance.PDF_UA_1} {
		doc := testNewDoc()
		doc.pdf.SetCompliance(level)
		if !strings.Contains(testCarry(doc, "factur-x.xml"), "/AF [") {
			t.Errorf("%v carries no file", level)
		}
	}
}

func TestAssociatedFileTheMetadataCarriesTheDescriptionsOfTheStandardsOfTheDocument(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_3B)
	doc.pdf.AddMetadata("<rdf:Description rdf:about=\"\" xmlns:fx=\"urn:test:1p0#\">\n" +
		"  <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n" +
		"</rdf:Description>")
	raw := testCarry(doc, "factur-x.xml")
	description := strings.Index(raw, "<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>")
	if description == -1 {
		t.Fatal("the property is not written")
	}
	// Inside the metadata: after the description of the document and before
	// the end of the RDF.
	if !strings.Contains(raw[:description], "</pdfaid:conformance>") {
		t.Error("after the document")
	}
	if !strings.Contains(raw[description:], "</rdf:RDF>") {
		t.Error("before the end")
	}
	if !strings.Contains(raw[description:], "</x:xmpmeta>") {
		t.Error("inside the metadata")
	}
}
