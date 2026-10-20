// review_writer_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"os"
	"path/filepath"
	"regexp"
	"slices"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/relationship"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The tests of the writer: the catalog, the structure tree, the annotations,
// the metadata and the files a document carries.

// testWriterDoc writes a document of the compliance with a heading on its one
// page, drawn with draw, and returns it.
func testWriterDoc(t *testing.T, level compliance.Compliance, draw func(pdf *PDF, page *Page)) string {
	t.Helper()
	doc := testNewDoc()
	doc.pdf.SetCompliance(level).SetTitle("Test")
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(testHelvetica(doc.pdf), "Heading").
		SetStructureType(structelem.H1).SetLocation(50, 50).DrawOn(page)
	if draw != nil {
		draw(doc.pdf, page)
	}
	return string(doc.complete())
}

func TestWriterAPDFAOfLevelBIsNotTagged(t *testing.T) {
	testAllowCoreFonts(t)
	for _, level := range []compliance.Compliance{
		compliance.PDF_A_1B, compliance.PDF_A_2B, compliance.PDF_A_3B} {
		raw := testWriterDoc(t, level, nil)
		for _, entry := range []string{"/StructTreeRoot", "/MarkInfo", "/Tabs /S", "/StructParents", "/StructElem"} {
			if strings.Contains(raw, entry) {
				t.Errorf("%v has %s", level, entry)
			}
		}
		for _, entry := range []string{"/Lang <", "/DisplayDocTitle true"} {
			if !strings.Contains(raw, entry) {
				t.Errorf("%v has no %s", level, entry)
			}
		}
	}
	for _, level := range []compliance.Compliance{
		compliance.PDF_A_1A, compliance.PDF_A_2A, compliance.PDF_A_3A,
		compliance.PDF_UA_1, compliance.PDF_A_3A_UA_1} {
		raw := testWriterDoc(t, level, nil)
		for _, entry := range []string{"/StructTreeRoot", "/MarkInfo <</Marked true>>", "/Tabs /S", "/StructParents 0"} {
			if !strings.Contains(raw, entry) {
				t.Errorf("%v has no %s", level, entry)
			}
		}
	}
}

func TestWriterTheNoticeOfAFontIsEscapedInItsMetadata(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.addMetadataObject("Copyright A & B <c>", true)
	raw := string(doc.written())
	if !strings.Contains(raw, "Copyright A &amp; B &lt;c&gt;") {
		t.Error(raw)
	}
	var objects []*PDFobj
	number := addMetadataObject2(&objects, &Font{info: "Copyright A & B"})
	if xml := string(objects[number-1].stream); !strings.Contains(xml, "Copyright A &amp; B") {
		t.Error(xml)
	}
}

// written returns what the PDF has written so far, without completing it.
func (doc *testDoc) written() []byte {
	if err := doc.writer.Flush(); err != nil {
		panic(err)
	}
	return doc.buf.Bytes()
}

// testAnnotations draws a square, a circle, a polygon, a note and a file.
func testAnnotations(pdf *PDF, page *Page) {
	square := NewSquareAnnotation()
	square.SetLocation(100, 100)
	square.SetSize(50, 50)
	square.SetContents("A square")
	square.DrawOn(page)
	circle := NewCircleAnnotation()
	circle.SetLocation(200, 100)
	circle.SetSize(80, 40)
	circle.SetContents("A circle")
	circle.DrawOn(page)
	polygon := NewPolygonAnnotation().SetVertices([]float32{0, 0, 50, 0, 25, 40})
	polygon.SetLocation(300, 300)
	polygon.SetContents("A polygon")
	polygon.DrawOn(page)
	note := NewTextAnnotation()
	note.SetLocation(100, 400)
	note.SetSize(20, 20)
	note.SetContents("A note")
	note.DrawOn(page)
	// A file attached to a page, which PDF/A refuses: in a PDF/A document the
	// four others alone
	if pdf.isPDFA() {
		return
	}
	file := NewEmbeddedFile(pdf, "a.txt", strings.NewReader("A file"), false)
	attachment := NewFileAttachment(file)
	attachment.SetLocation(200, 400)
	attachment.SetContents("A file")
	attachment.DrawOn(page)
}

func TestWriterEveryAnnotationIsPrintedAndHasAnAppearance(t *testing.T) {
	testAllowCoreFonts(t)
	for _, level := range []compliance.Compliance{compliance.PDF_1_7, compliance.PDF_A_2B, compliance.PDF_A_3A} {
		raw := testWriterDoc(t, level, testAnnotations)
		want := 5
		if level != compliance.PDF_1_7 {
			want = 4 // No file attached
		}
		if n := strings.Count(raw, "/Type /Annot\n"); n != want {
			t.Fatalf("%v: %d annotations", level, n)
		}
		if n := strings.Count(raw, "/F 4\n"); n != want {
			t.Errorf("%v: %d annotations are printed", level, n)
		}
		if n := strings.Count(raw, "/AP <</N "); n != want {
			t.Errorf("%v: %d appearances", level, n)
		}
		if n := strings.Count(raw, "/Subtype /Form\n"); n != want {
			t.Errorf("%v: %d forms", level, n)
		}
	}
	// The square is drawn in its fill color in its box, which is its rectangle.
	raw := testWriterDoc(t, compliance.PDF_1_7, testAnnotations)
	if !strings.Contains(raw, "/BBox [100 642 150 692]\n/Length 34\n>>\nstream\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n") {
		t.Error(raw)
	}
	// The note and the file are drawn as their icons, scaled to their boxes.
	for _, icon := range []string{
		"q\n20 0 0 20 100 372 cm\n" + noteIcon + "Q\n",
		"q\n24 0 0 24 200 368 cm\n" + pushPinIcon + "Q\n",
	} {
		if !strings.Contains(raw, icon) {
			t.Errorf("no %q", icon)
		}
	}
}

func TestWriterTheRectangleOfAnAnnotationIsFromItsLowerLeftCorner(t *testing.T) {
	raw := testWriterDoc(t, compliance.PDF_1_7, func(pdf *PDF, page *Page) {
		testAnnotations(pdf, page)
		NewTextLine(testHelvetica(pdf), "Link").SetURIAction("https://pdfjet.com").
			SetLocation(50, 100).DrawOn(page)
	})
	for _, rect := range []string{
		"/Rect [100 642 150 692]", "/Rect [200 652 280 692]", "/Rect [300 452 350 492]",
		"/Rect [100 372 120 392]", "/Rect [200 368 224 392]",
	} {
		if !strings.Contains(raw, rect) {
			t.Errorf("no %s", rect)
		}
	}
	rects := regexp.MustCompile(`/Rect \[(\S+) (\S+) (\S+) (\S+)\]`).FindAllStringSubmatch(raw, -1)
	if len(rects) != 6 {
		t.Errorf("%d rectangles", len(rects))
	}
	for _, rect := range rects {
		x1, _ := strconv.ParseFloat(rect[1], 32)
		y1, _ := strconv.ParseFloat(rect[2], 32)
		x2, _ := strconv.ParseFloat(rect[3], 32)
		y2, _ := strconv.ParseFloat(rect[4], 32)
		if x1 > x2 || y1 > y2 {
			t.Errorf("%s", rect[0])
		}
	}
}

func TestWriterAShapeThatIsNotOpaqueIsDrawnWithItsOpacity(t *testing.T) {
	raw := testWriterDoc(t, compliance.PDF_1_7, func(pdf *PDF, page *Page) {
		square := NewSquareAnnotation()
		square.SetLocation(100, 100)
		square.SetSize(50, 50)
		square.SetOpacity(0.5)
		square.SetContents("A square")
		square.DrawOn(page)
	})
	if !strings.Contains(raw, "/BBox [100 642 150 692]\n/Resources <</ExtGState <</GS0 <</CA 0.5 /ca 0.5>>>>>>\n"+
		"/Length 42\n>>\nstream\n/GS0 gs\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n") {
		t.Error(raw)
	}
}

func TestWriterAPDFA1DrawsAShapeThatIsNotOpaqueOpaque(t *testing.T) {
	testAllowCoreFonts(t)
	// PDF/A-1 has no transparency: the opacity is ignored there, and kept in
	// the other levels.
	for _, level := range []compliance.Compliance{
		compliance.PDF_A_1B, compliance.PDF_A_1A, compliance.PDF_A_2B, compliance.PDF_1_7} {
		raw := testWriterDoc(t, level, func(pdf *PDF, page *Page) {
			square := NewSquareAnnotation()
			square.SetLocation(100, 100)
			square.SetSize(50, 50)
			square.SetOpacity(0.5)
			square.SetContents("A square")
			square.DrawOn(page)
		})
		transparent := level != compliance.PDF_A_1B && level != compliance.PDF_A_1A
		for _, entry := range []string{"/CA ", "/ExtGState", "/GS0 gs"} {
			if strings.Contains(raw, entry) != transparent {
				t.Errorf("%v: %s", level, entry)
			}
		}
		if !transparent && !strings.Contains(raw, "/Length 34\n>>\nstream\n0.5 0.5 0.5 rg\n100 642 50 50 re f\n") {
			t.Errorf("%v: %s", level, raw)
		}
	}
}

func TestWriterALinkToADestinationTheDocumentDoesNotHaveIsRefused(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	NewTextLine(testHelvetica(pdf), "Nowhere").SetGoToAction("missing").SetLocation(50, 100).DrawOn(page)
	testCompleteFails(t, pdf, "The link goes to the destination missing, which the document does not have.")
}

func TestWriterTheTypesOfPDF20AreMappedInTheRoleMap(t *testing.T) {
	testAllowCoreFonts(t)
	raw := testWriterDoc(t, compliance.PDF_UA_1, nil)
	if strings.Contains(raw, "/RoleMap") {
		t.Error("a role map with no type to map")
	}
	raw = testWriterDoc(t, compliance.PDF_UA_1, func(pdf *PDF, page *Page) {
		font := testHelvetica(pdf)
		NewTextLine(font, "Strong").SetStructureType(structelem.Strong).SetLocation(50, 100).DrawOn(page)
		NewTextLine(font, "Title").SetStructureType(structelem.Title).SetLocation(50, 120).DrawOn(page)
	})
	if !strings.Contains(raw, "/RoleMap << /Title /P /Strong /Span >>\n") {
		t.Error(raw)
	}
}

func TestWriterATextLineOfTheTypeArtifactIsAnArtifact(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(testHelvetica(doc.pdf), "Header").
		SetStructureType(structelem.Artifact).SetLocation(50, 50).DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "/Artifact BMC\n") || strings.Contains(content, "/Artifact <<") {
		t.Error(content)
	}
	if len(page.structures) != 0 {
		t.Errorf("%d elements", len(page.structures))
	}

	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1)
	page = NewPage(pdf, letter.Portrait())
	page.BeginStructElement(structelem.Artifact)
	testRecorded(t, pdf, "An artifact is not a structure element: draw it between AddArtifactBMC and AddEMC.")
}

func TestWriterAnAnnotationOnAWrittenPageOfATaggedDocumentIsRefused(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page1 := NewPage(pdf, letter.Portrait())
	NewPage(pdf, letter.Portrait())
	square := NewSquareAnnotation()
	square.SetContents("Late")
	square.DrawOn(page1)
	testRecorded(t, pdf, "The page was already written to the PDF: "+
		"draw on a page before creating the next page or completing the PDF.")
	if len(page1.annots) != 0 {
		t.Error("the annotation was added")
	}
}

func TestWriterAPDFUADocumentNeedsATitle(t *testing.T) {
	for _, level := range []compliance.Compliance{compliance.PDF_UA_1, compliance.PDF_A_3A_UA_1} {
		pdf := testNewPDF()
		pdf.SetCompliance(level).SetTitle(" ")
		NewPage(pdf, letter.Portrait())
		testCompleteFails(t, pdf, "A PDF/UA document needs a title: use SetTitle.")
	}
}

func TestWriterAnAnnotationOfATaggedDocumentNeedsADescription(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	page := NewPage(pdf, letter.Portrait())
	NewSquareAnnotation().DrawOn(page)
	testRecorded(t, pdf, "An annotation of a tagged document, PDF/UA or PDF/A of level A, "+
		"needs contents, a title or an alternative description.")
	// One that is not tagged needs none.
	doc := testNewDoc()
	NewSquareAnnotation().DrawOn(NewPage(doc.pdf, letter.Portrait()))
	doc.complete()
}

func TestWriterTheInformationSaysWhatTheMetadataSays(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_2B).SetTitle("A\x01B\xffC￾D\tE").SetAuthor("F\x02G")
	NewPage(doc.pdf, letter.Portrait())
	raw := string(doc.complete())
	if !strings.Contains(raw, "<rdf:li xml:lang=\"x-default\">ABCD\tE</rdf:li>") ||
		!strings.Contains(raw, "/Title "+testWriterTextString("ABCD\tE")+"\n") ||
		!strings.Contains(raw, "<rdf:li>FG</rdf:li>") ||
		!strings.Contains(raw, "/Author "+testWriterTextString("FG")+"\n") {
		t.Error(raw)
	}
}

// testTextString returns the text as the writer writes a text string that is
// not encrypted.
func testWriterTextString(text string) string {
	doc := testNewDoc()
	doc.pdf.appendTextString(text)
	return string(doc.written())[len("%PDF-1.7\n%\xf2\xf3\xf4\xf5\xf6\n"):]
}

func TestWriterTheDateOfAnEmbeddedFileIsEncrypted(t *testing.T) {
	doc := testNewDoc()
	enc, err := NewEncryption(doc.pdf, encryption.NewPasswords(), encryption.NewPermissions())
	if err != nil {
		t.Fatal(err)
	}
	doc.pdf.SetEncryption(enc)
	date := doc.pdf.getDate()
	NewEmbeddedFileWithRelationship(doc.pdf, "a.xml", strings.NewReader("<a/>"), false,
		"text/xml", relationship.Data, "")
	NewPage(doc.pdf, letter.Portrait())
	raw := string(doc.complete())
	if strings.Contains(raw, date) || strings.Contains(raw, testHex(date)) {
		t.Error("the date is not encrypted")
	}
	if !strings.Contains(raw, "/ModDate <") {
		t.Error(raw)
	}
}

func TestWriterCompleteClosesTheFileWhenItFails(t *testing.T) {
	pdf, err := NewPDFFile(filepath.Join(t.TempDir(), "a.pdf"))
	if err != nil {
		t.Fatal(err)
	}
	file := pdf.file
	if pdf.Complete() == nil {
		t.Fatal("a PDF of no pages was completed")
	}
	if _, err := file.Write([]byte("x")); !errors.Is(err, os.ErrClosed) {
		t.Errorf("the file is open: %v", err)
	}
}

func TestWriterTheHexadecimalOfATextIsItsUTF16(t *testing.T) {
	if got := toUTF16Hex("Azé\U0001F600"); got != "FEFF0041007A00E9D83DDE00" {
		t.Error(got)
	}
}

func TestWriterTheLayersAreOrderedByTheirUTF16Names(t *testing.T) {
	doc := testNewDoc()
	page := NewPage(doc.pdf, letter.Portrait())
	var groups []*OptionalContentGroup
	for _, name := range []string{"", "b", "a", "\U0001F600", "a"} {
		group := NewOptionalContentGroup(doc.pdf, name)
		group.Add(NewRect(10, 10, 20, 20))
		group.DrawOn(page)
		groups = append(groups, group)
	}
	raw := string(doc.complete())
	order := "/Order ["
	for _, i := range []int{2, 4, 1, 3, 0} {
		order += " " + strconv.Itoa(groups[i].objNumber) + " 0 R "
	}
	if !strings.Contains(raw, order+"]\n") {
		t.Errorf("want %s in %s", order, raw[strings.Index(raw, "/Order"):])
	}
}

func TestWriterAPDFA3FileNeedsAMediaType(t *testing.T) {
	for _, level := range []compliance.Compliance{compliance.PDF_A_3A, compliance.PDF_A_3B, compliance.PDF_A_3A_UA_1} {
		pdf := testNewPDF()
		pdf.SetCompliance(level)
		pdf.AddAssociatedFile(NewEmbeddedFileWithRelationship(pdf, "a.xml", strings.NewReader("<a/>"),
			false, "", relationship.Data, "Data."))
		testRecorded(t, pdf, "The file a.xml was embedded without a media type, "+
			"which a file of a document of PDF/A-3 needs.")
	}
	pdf := testNewPDF()
	pdf.AddAssociatedFile(NewEmbeddedFileWithRelationship(pdf, "a.xml", strings.NewReader("<a/>"),
		false, "", relationship.Data, "Data."))
	if pdf.err != nil {
		t.Error(pdf.err)
	}
}

func TestWriterTheProducerIsTheVersionOfTheLibrary(t *testing.T) {
	doc := testNewDoc()
	NewPage(doc.pdf, letter.Portrait())
	if raw := string(doc.complete()); !strings.Contains(raw, "/Producer "+testWriterTextString("PDFjet v9.1.1")) {
		t.Error("the producer")
	}
}

func TestWriterTheFontFileRefersToItsMetadataWhoseNoticeIsEscaped(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_2B)
	font := NewFontFromFile(doc.pdf, testRepoPath(t, "fonts/NotoSansJP/NotoSansJP-Regular.ttf"))
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(font, "Hello").SetLocation(50, 50).DrawOn(page)
	raw := string(doc.complete())
	if !regexp.MustCompile(`\d+ 0 obj\n<<\n/Filter /FlateDecode\n/Length1 \d+\n/Metadata \d+ 0 R\n/Length \d+\n>>\nstream\n`).MatchString(raw) {
		t.Error("the font file does not refer to its metadata")
	}
	if !strings.Contains(raw, "<xmpRights:UsageTerms>") || !strings.Contains(raw, " &amp; ") {
		t.Error("the notice is not escaped")
	}
}

func TestWriterAPDFA1MapsTheElementOfAnAnnotationToASpan(t *testing.T) {
	testAllowCoreFonts(t)
	// PDF/A-1, of PDF 1.4, does not know the type Annot, of PDF 1.5: it is
	// role-mapped there (veraPDF, PDF/A-1 6.8.3.4), and not in PDF/A-2.
	for _, level := range []compliance.Compliance{compliance.PDF_A_1A, compliance.PDF_A_2A} {
		raw := testWriterDoc(t, level, func(pdf *PDF, page *Page) {
			square := NewSquareAnnotation()
			square.SetLocation(100, 100)
			square.SetSize(50, 50)
			square.SetContents("A square")
			square.DrawOn(page)
		})
		if mapped := strings.Contains(raw, "/Annot /Span"); mapped != (level == compliance.PDF_A_1A) {
			t.Errorf("%v: Annot mapped %v", level, mapped)
		}
	}
}

// A document of PDF/A or PDF/UA refuses a font that is not embedded, and one
// of PDF/A a file attached to a page or embedded but not as an associated
// file of PDF/A-3: each was written, and the document was not what it said
// (review of 11 October 2026, #5 and #6).
func TestWriterACompliantDocumentRefusesWhatItCannotBe(t *testing.T) {
	complete := func(level compliance.Compliance, draw func(pdf *PDF, page *Page)) error {
		doc := testNewDoc()
		doc.pdf.SetCompliance(level).SetTitle("Test")
		page := NewPage(doc.pdf, letter.Portrait())
		draw(doc.pdf, page)
		return doc.pdf.Complete()
	}
	text := func(font func(pdf *PDF) *Font) func(pdf *PDF, page *Page) {
		return func(pdf *PDF, page *Page) {
			NewTextLine(font(pdf), "Text").SetLocation(50, 50).DrawOn(page)
		}
	}
	for _, level := range []compliance.Compliance{compliance.PDF_UA_1, compliance.PDF_A_1B, compliance.PDF_A_3A} {
		err := complete(level, text(testHelvetica))
		if err == nil || !strings.Contains(err.Error(), "The font Helvetica is not embedded") {
			t.Errorf("%v: Helvetica: %v", level, err)
		}
		if err := complete(level, text(testPlexSans)); err != nil {
			t.Errorf("%v: IBM Plex Sans: %v", level, err)
		}
	}
	attach := func(pdf *PDF, page *Page) {
		text(testPlexSans)(pdf, page)
		file := NewEmbeddedFile(pdf, "a.txt", strings.NewReader("A file"), false)
		NewFileAttachment(file).DrawOn(page)
	}
	embed := func(pdf *PDF, page *Page) {
		text(testPlexSans)(pdf, page)
		NewEmbeddedFile(pdf, "a.txt", strings.NewReader("A file"), false)
	}
	for _, level := range []compliance.Compliance{compliance.PDF_A_1B, compliance.PDF_A_2B, compliance.PDF_A_3B} {
		if err := complete(level, attach); err == nil || !strings.Contains(err.Error(), "cannot have a file attached to a page") {
			t.Errorf("%v: a file attached: %v", level, err)
		}
		if err := complete(level, embed); err == nil || !strings.Contains(err.Error(), "but as an associated file") {
			t.Errorf("%v: a file embedded: %v", level, err)
		}
	}
	// Outside PDF/A a file is attached as before
	if err := complete(compliance.PDF_UA_1, attach); err != nil {
		t.Errorf("PDF/UA: a file attached: %v", err)
	}
}

// A file of PDF/A-3 attached to a page is taken when the document carries it
// as an associated file too, the annotation's /FS the file specification
// that /AF lists, which veraPDF passes: it was refused (review of 12 October
// 2026, R24). Attached alone it is refused, as before.
func TestWriterAFileOfPDFA3AttachedToAPageIsTakenWhenItIsAssociated(t *testing.T) {
	complete := func(level compliance.Compliance, associate bool) error {
		doc := testNewDoc()
		doc.pdf.SetCompliance(level).SetTitle("Test")
		page := NewPage(doc.pdf, letter.Portrait())
		NewTextLine(testPlexSans(doc.pdf), "Text").SetLocation(50, 50).DrawOn(page)
		file := NewEmbeddedFileWithRelationship(doc.pdf, "data.xml", strings.NewReader("<a/>"), false,
			"text/xml", relationship.Data, "The data")
		if associate {
			doc.pdf.AddAssociatedFile(file)
		}
		NewFileAttachment(file).DrawOn(page)
		return doc.pdf.Complete()
	}
	if err := complete(compliance.PDF_A_3B, true); err != nil {
		t.Errorf("an associated file attached: %v", err)
	}
	if err := complete(compliance.PDF_A_3B, false); err == nil || !strings.Contains(err.Error(), "cannot have a file attached to a page") {
		t.Errorf("a file attached alone: %v", err)
	}
}

// An associated file embedded in another PDF is refused, as a file attached
// with FileAttachment is: /AF and /EmbeddedFiles named an object of the other
// PDF (review of 12 October 2026, R11).
func TestWriterAnAssociatedFileOfAnotherPDFIsRefused(t *testing.T) {
	other := testNewDoc()
	file := NewEmbeddedFileWithRelationship(other.pdf, "data.xml", strings.NewReader("<a/>"), false,
		"text/xml", relationship.Data, "The data")
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_A_3B).SetTitle("Test")
	doc.pdf.AddAssociatedFile(file)
	testRecorded(t, doc.pdf, "The embedded file belongs to another PDF.")
}

// testDecryptedStrings reads the PDF, encrypted with no user password, and
// returns the strings after the key in its objects, as a reader decrypts
// them.
func testDecryptedStrings(t *testing.T, pdf []byte, key string) []string {
	t.Helper()
	objects, err := testNewPDF().ReadWithPassword(pdf, "")
	if err != nil {
		t.Fatal(err)
	}
	var values []string
	for _, obj := range objects {
		for i, token := range obj.dict {
			if token == key && i+1 < len(obj.dict) {
				values = append(values, string(toBytes(obj.dict[i+1])))
			}
		}
	}
	return values
}

// testEncryptedDoc returns a document of the compliance encrypted with no
// passwords.
func testEncryptedDoc(t *testing.T, level compliance.Compliance) *testDoc {
	t.Helper()
	doc := testNewDoc()
	doc.pdf.SetCompliance(level).SetTitle("Test")
	enc, err := NewEncryption(doc.pdf, encryption.NewPasswords(), encryption.NewPermissions())
	if err != nil {
		t.Fatal(err)
	}
	doc.pdf.SetEncryption(enc)
	return doc
}

// The strings of the system of characters of a CJK font, Adobe and the
// ordering, are encrypted in an encrypted document: written as they were, a
// reader decrypted them to nothing, and the CMap matched no font (review of
// 11 October 2026, #19).
func TestWriterTheCharactersOfACJKFontAreEncrypted(t *testing.T) {
	doc := testEncryptedDoc(t, compliance.PDF_1_7)
	NewTextLine(NewCJKFont(doc.pdf, cjkfont.KozMinProVIRegular), "\u65e5\u672c").
		SetLocation(50, 50).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	pdf := doc.complete()
	if got := testDecryptedStrings(t, pdf, "/Registry"); len(got) != 1 || got[0] != "Adobe" {
		t.Errorf("/Registry %q", got)
	}
	if got := testDecryptedStrings(t, pdf, "/Ordering"); len(got) != 1 || got[0] != "Japan1" {
		t.Errorf("/Ordering %q", got)
	}
}

// The name of the configuration of the layers of a document of PDF/UA is
// encrypted in an encrypted document, where a reader decrypted it to nothing
// (review of 11 October 2026, #20).
func TestWriterTheNameOfTheLayersIsEncrypted(t *testing.T) {
	doc := testEncryptedDoc(t, compliance.PDF_UA_1)
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(testPlexSans(doc.pdf), "Heading").
		SetStructureType(structelem.H1).SetLocation(50, 50).DrawOn(page)
	group := NewOptionalContentGroup(doc.pdf, "Layer").SetVisible(true)
	group.Add(NewRect(10, 100, 20, 20))
	group.DrawOn(page)
	if got := testDecryptedStrings(t, doc.complete(), "/Name"); !slices.Contains(got, "Default") {
		t.Errorf("/Name %q", got)
	}
}

// The XMP packet's header begins with the byte order mark, which ISO 16684-1
// asks of it, in the document's metadata and a font's (review of 11 October
// 2026, #28).
func TestWriterTheXMPPacketHasItsBeginAttribute(t *testing.T) {
	testAllowCoreFonts(t)
	raw := testWriterDoc(t, compliance.PDF_UA_1, nil)
	header := "<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>"
	if !strings.Contains(raw, header) || strings.Contains(raw, "<?xpacket id=") {
		t.Error("the packet has no begin attribute")
	}
}
