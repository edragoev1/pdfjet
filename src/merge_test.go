// merge_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"os"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Merging the pages of documents that were read.

// testMergeDocument returns a document with one page for each text, drawn with Helvetica.
func testMergeDocument(texts ...string) []byte {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	for _, text := range texts {
		page := NewPage(doc.pdf, letter.Portrait())
		NewTextLine(font, text).SetLocation(50, 50).DrawOn(page)
	}
	return doc.complete()
}

// testPageContents returns the decoded content of each page, in the order of the pages.
func testPageContents(objects []*PDFobj) []string {
	contents := make([]string, 0)
	for _, page := range testNewPDF().GetPageObjects(objects) {
		contents = append(contents, string(page.GetContentObject(objects).GetData()))
	}
	return contents
}

// testReferencesResolve checks that every reference of the document is to an object it has.
func testReferencesResolve(t *testing.T, objects []*PDFobj) {
	t.Helper()
	for _, obj := range objects {
		for i := 0; i+2 < len(obj.dict); i++ {
			if obj.dict[i+2] == "R" && isObjectNumberToken(obj.dict[i]) && isObjectNumberToken(obj.dict[i+1]) {
				number, _ := strconv.Atoi(obj.dict[i])
				if number < 1 || number > len(objects) || len(objects[number-1].dict) == 0 {
					t.Errorf("object %d refers to the missing object %d", obj.number, number)
				}
			}
		}
	}
}

// testMergeError checks the error that Merge or AddObjects returned.
func testMergeError(t *testing.T, err error, message string) {
	t.Helper()
	if err == nil || err.Error() != message {
		t.Errorf("want %q, got %v", message, err)
	}
}

func testContainsAll(t *testing.T, contents []string, texts ...string) {
	t.Helper()
	if len(contents) != len(texts) {
		t.Fatalf("pages: want %d, got %d", len(texts), len(contents))
	}
	for i, text := range texts {
		if !strings.Contains(contents[i], testHex(text)) {
			t.Errorf("page %d has no %s: %q", i+1, text, contents[i])
		}
	}
}

func TestMergeMergesDocumentsInTheirOrder(t *testing.T) {
	doc := testNewDoc()
	if err := doc.pdf.Merge(testRead(t, testMergeDocument("A1", "A2"))); err != nil {
		t.Fatal(err)
	}
	if err := doc.pdf.Merge(testRead(t, testMergeDocument("B1"))); err != nil {
		t.Fatal(err)
	}
	objects := testRead(t, doc.complete())
	testContainsAll(t, testPageContents(objects), "A1", "A2", "B1")
	testReferencesResolve(t, objects)
}

func TestMergeDrawnPagesKeepTheirPlace(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	NewTextLine(font, "G1").SetLocation(50, 50).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	if err := doc.pdf.Merge(testRead(t, testMergeDocument("B1"))); err != nil {
		t.Fatal(err)
	}
	NewTextLine(font, "G2").SetLocation(50, 50).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	objects := testRead(t, doc.complete())
	testContainsAll(t, testPageContents(objects), "G1", "B1", "G2")
	testReferencesResolve(t, objects)
}

func TestMergeAMergedPageInheritsFromThePageTree(t *testing.T) {
	content := "BT /F1 24 Tf 20 300 Td (Inherited) Tj ET"
	source := "%PDF-1.4\n" +
		"1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n" +
		"2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 300 400] /Rotate 90" +
		" /Resources << /Font << /F1 4 0 R >> >> >> endobj\n" +
		"3 0 obj << /Type /Page /Parent 2 0 R /Contents 5 0 R >> endobj\n" +
		"4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n" +
		"5 0 obj << /Length " + strconv.Itoa(len(content)) + " >>\nstream\n" + content + "\nendstream\nendobj\n" +
		"trailer << /Root 1 0 R >>\n%%EOF\n"
	doc := testNewDoc()
	if err := doc.pdf.Merge(testRead(t, []byte(source))); err != nil {
		t.Fatal(err)
	}
	objects := testRead(t, doc.complete())
	page := testNewPDF().GetPageObjects(objects)[0]
	testNear(t, "width", 300, page.GetPageSize().GetWidth(), 0)
	testNear(t, "height", 400, page.GetPageSize().GetHeight(), 0)
	if rotate := page.GetValue("/Rotate"); rotate != "90" {
		t.Errorf("rotate %q", rotate)
	}
	if !strings.Contains(strings.Join(page.dict, " "), "/F1") {
		t.Errorf("no /F1 in %v", page.dict)
	}
	parent := page.getObjectNumbers("/Parent")[0]
	if objType := objects[parent-1].GetValue("/Type"); objType != "/Pages" {
		t.Errorf("parent type %q", objType)
	}
	if !strings.Contains(testPageContents(objects)[0], "(Inherited) Tj") {
		t.Error("the content was not merged")
	}
	testReferencesResolve(t, objects)
}

func TestMergeLinksPointAtTheMergedPages(t *testing.T) {
	source := testNewDoc()
	font1 := testHelvetica(source.pdf)
	page1 := NewPage(source.pdf, letter.Portrait())
	NewTextLine(font1, "Go").SetGoToAction("there").SetLocation(50, 50).DrawOn(page1)
	page2 := NewPage(source.pdf, letter.Portrait())
	page2.AddDestinationAt("there", 30, 100)
	sourceBytes := source.complete()

	doc := testNewDoc()
	NewTextLine(testHelvetica(doc.pdf), "Cover").SetLocation(50, 50).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	if err := doc.pdf.Merge(testRead(t, sourceBytes)); err != nil {
		t.Fatal(err)
	}
	objects := testRead(t, doc.complete())
	pages := testNewPDF().GetPageObjects(objects)
	if len(pages) != 3 {
		t.Fatalf("pages %d", len(pages))
	}
	var link *PDFobj
	for _, obj := range objects {
		if obj.GetValue("/Subtype") == "/Link" {
			link = obj
		}
	}
	if link == nil {
		t.Fatal("no link annotation")
	}
	// The link on the second page points at the third page and is listed by the second.
	dest := -1
	for i, token := range link.dict {
		if token == "/Dest" {
			dest = i
			break
		}
	}
	if dest == -1 || dest+4 >= len(link.dict) || link.dict[dest+1] != "[" ||
		link.dict[dest+2] != strconv.Itoa(pages[2].number) || link.dict[dest+4] != "R" {
		t.Errorf("the /Dest of %v is not page %d", link.dict, pages[2].number)
	}
	listed := false
	for _, number := range pages[1].getObjectNumbers("/Annots") {
		if number == link.number {
			listed = true
		}
	}
	if !listed {
		t.Error("the second page does not list the link")
	}
	testReferencesResolve(t, objects)
}

func TestMergeAnEncryptedDocumentMergesThePagesEncrypted(t *testing.T) {
	doc := testNewDoc()
	enc, err := NewEncryption(doc.pdf, encryption.NewPasswords(), encryption.NewPermissions())
	if err != nil {
		t.Fatal(err)
	}
	doc.pdf.SetEncryption(enc)
	if err := doc.pdf.Merge(testRead(t, testMergeDocument("Secret A", "Secret B"))); err != nil {
		t.Fatal(err)
	}
	bytes := doc.complete()
	if !strings.Contains(string(bytes), "/Encrypt ") {
		t.Error("the merged document is not encrypted")
	}
	objects, err := testNewPDF().ReadWithPassword(bytes, "")
	if err != nil {
		t.Fatal(err)
	}
	testContainsAll(t, testPageContents(objects), "Secret A", "Secret B")
	testReferencesResolve(t, objects)
}

func TestMergeAnEncryptedDocumentIsMergedDecrypted(t *testing.T) {
	source := testNewDoc()
	enc, err := NewEncryption(source.pdf, encryption.NewPasswords(), encryption.NewPermissions())
	if err != nil {
		t.Fatal(err)
	}
	source.pdf.SetEncryption(enc)
	NewTextLine(testHelvetica(source.pdf), "Plain").SetLocation(50, 50).DrawOn(NewPage(source.pdf, letter.Portrait()))
	sourceObjects, err := testNewPDF().ReadWithPassword(source.complete(), "")
	if err != nil {
		t.Fatal(err)
	}

	doc := testNewDoc()
	if err := doc.pdf.Merge(sourceObjects); err != nil {
		t.Fatal(err)
	}
	bytes := doc.complete()
	if strings.Contains(string(bytes), "/Encrypt ") {
		t.Error("the merged document is encrypted")
	}
	objects := testRead(t, bytes)
	testContainsAll(t, testPageContents(objects), "Plain")
	testReferencesResolve(t, objects)
}

func TestMergeMergesTheTestDocuments(t *testing.T) {
	doc := testNewDoc()
	for _, name := range []string{"wirth.pdf", "rc65-16e.pdf", "PDFjetLogo.pdf"} {
		buf, err := os.ReadFile(testRepoPath(t, "data/testPDFs/"+name))
		if err != nil {
			t.Fatal(err)
		}
		if err := doc.pdf.Merge(testRead(t, buf)); err != nil {
			t.Fatal(err)
		}
	}
	objects := testRead(t, doc.complete())
	pages := testNewPDF().GetPageObjects(objects)
	if len(pages) != 8 {
		t.Fatalf("pages %d", len(pages))
	}
	for i, page := range pages {
		content := page.GetContentObject(objects)
		if content == nil || content.GetData() == nil {
			t.Errorf("page %d has no content", i+1)
		}
	}
	testReferencesResolve(t, objects)
}

func TestMergeMergeIsRefusedWhereItWouldBreakTheDocument(t *testing.T) {
	objects := testRead(t, testMergeDocument("A"))

	ua := testNewDoc()
	ua.pdf.SetCompliance(compliance.PDF_UA_1)
	testMergeError(t, ua.pdf.Merge(objects),
		"Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.")

	completed := testNewDoc()
	NewPage(completed.pdf, letter.Portrait())
	completed.complete()
	testMergeError(t, completed.pdf.Merge(objects), "The PDF was already completed.")

	rewritten := testNewDoc()
	if err := rewritten.pdf.AddObjects(testRead(t, testMergeDocument("B"))); err != nil {
		t.Fatal(err)
	}
	testMergeError(t, rewritten.pdf.Merge(objects), "Merge and AddObjects cannot be used on the same PDF.")

	merged := testNewDoc()
	if err := merged.pdf.Merge(objects); err != nil {
		t.Fatal(err)
	}
	testMergeError(t, merged.pdf.AddObjects(testRead(t, testMergeDocument("C"))),
		"Merge and AddObjects cannot be used on the same PDF.")

	empty := testNewDoc()
	testMergeError(t, empty.pdf.Merge([]*PDFobj{}), "The objects have no root /Pages object.")
}
