// review3_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"math"
	"os"
	"path/filepath"
	"regexp"
	"runtime"
	"slices"
	"strconv"
	"strings"
	"sync"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// The findings of the third review, of 12 October 2026, as tests.

// A text rendering mode set back to 0 is written: Tr lasts past ET, and the
// text after an invisible line stayed invisible (F1).
func TestReview3TheTextRenderingModeIsSetBackToZero(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := testNewPage()
	NewTextLine(font, "plain").SetLocation(50, 50).DrawOn(page)
	if strings.Contains(testContent(page), " Tr\n") {
		t.Errorf("a page that never set the mode writes it: %q", testContent(page))
	}
	page.SetTextRenderingMode(3)
	NewTextLine(font, "hidden").SetLocation(50, 100).DrawOn(page)
	page.SetTextRenderingMode(0)
	NewTextLine(font, "VISIBLE AGAIN").SetLocation(50, 150).DrawOn(page)
	content := testContent(page)
	visible := strings.Index(content, testHex("VISIBLE AGAIN"))
	if zero := strings.LastIndex(content[:visible], "0 Tr\n"); zero == -1 ||
		strings.LastIndex(content[:visible], "3 Tr\n") > zero {
		t.Errorf("the mode is not set back to 0 before the text: %q", content)
	}
}

// A font of another PDF is refused and nothing is drawn with it, on a page or
// a stamp: drawing read what the font recorded for its own PDF while another
// goroutine drew new characters on that PDF, and the process died with
// "concurrent map read and map write" (S1, C3). Run with -race.
func TestReview3AFontOfAnotherPDFDrawsNothing(t *testing.T) {
	owner := testNewPDF()
	font := testTrueTypeFont(t, owner)
	glyphs := regexp.MustCompile(`<[0-9A-F]+> Tj`)
	var wg sync.WaitGroup
	wg.Add(1)
	go func() {
		defer wg.Done()
		page := NewPage(owner, testLetterPortrait())
		for i := range 200 {
			NewTextLine(font, string(rune(0x41+i%26))+"١۱é").SetLocation(50, 50).DrawOn(page)
		}
	}()
	for range 4 {
		wg.Add(1)
		go func() {
			defer wg.Done()
			page := testNewPage()
			for range 50 {
				NewTextLine(font, "۱١ Hello").SetLocation(50, 50).DrawOn(page)
			}
			testRecorded(t, page.pdf, "The font belongs to another PDF.")
			if content := testContent(page); glyphs.MatchString(content) {
				t.Errorf("the page draws the font of another PDF: %q", content)
			}
			pdf := testNewPDF()
			stamp := NewStamp(pdf).SetSize(100, 100)
			stamp.DrawText(font, 12, 0, 12, "۱١ Hello")
			testRecorded(t, pdf, "The font belongs to another PDF.")
			if stamp.buf.Len() != 0 {
				t.Errorf("the stamp draws the font of another PDF: %q", stamp.buf.String())
			}
		}()
	}
	wg.Wait()
}

// A text frame at NaN, or a Markdown text with NaN margins, is refused: no
// word was placed, and pages were made until the memory ran out (F4).
func TestReview3ANaNLocationOrMarginIsRefused(t *testing.T) {
	nan := float32(math.NaN())
	inf := float32(math.Inf(1))
	for _, xywh := range [][4]float32{{nan, 50, 200, 0}, {50, nan, 200, 0}, {50, 50, nan, 0}, {50, 50, 200, inf}} {
		pdf := testNewPDF()
		frame := NewTextFrame(testHelvetica(pdf), []string{"Hello frame text"}).SetWidth(xywh[2]).SetHeight(xywh[3])
		frame.SetLocation(xywh[0], xywh[1])
		var pages []*Page
		frame.DrawOnPages(pdf, &pages, testLetterPortrait())
		testRecorded(t, pdf, "The text frame's location and size must be numbers, not NaN or infinite.")
		if len(pages) != 0 {
			t.Errorf("%v: %d pages", xywh, len(pages))
		}
	}
	for _, margin := range []float32{nan, inf} {
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		var pages []*Page
		NewMarkdown(font, font, font, font, font).SetMargins(margin, 72, 72, 72).
			DrawOnPages(pdf, "Some *text*.", &pages, testLetterPortrait())
		testRecorded(t, pdf, "The margins of the Markdown must be numbers, not NaN or infinite.")
		if len(pages) != 0 {
			t.Errorf("%v: %d pages", margin, len(pages))
		}
	}
}

// An image of the Markdown that cannot be read fails the document, and does
// not panic: Markdown from a user could stop a server (F5).
func TestReview3AnImageThatCannotBeReadFailsTheMarkdown(t *testing.T) {
	dir := t.TempDir()
	png, err := os.ReadFile(testRepoPath(t, "tests/data/PngSuite/BASN2C08.PNG"))
	if err != nil {
		t.Fatal(err)
	}
	files := map[string][]byte{
		"notes.txt": []byte("Not an image."),
		"cut.png":   png[:40],
		"bad.svg":   []byte("Not SVG."),
	}
	for name, data := range files {
		if err := os.WriteFile(filepath.Join(dir, name), data, 0o644); err != nil {
			t.Fatal(err)
		}
	}
	for name := range files {
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		var pages []*Page
		message, panicked := testPanic(func() {
			NewMarkdown(font, font, font, font, font).SetImageDirectory(dir).
				DrawOnPages(pdf, "Before\n\n![x]("+name+")\n\nAfter", &pages, testLetterPortrait())
		})
		if panicked {
			t.Errorf("%s: panicked with %q", name, message)
		} else if pdf.err == nil || !strings.HasPrefix(pdf.err.Error(), "The image "+name+" could not be read: ") {
			t.Errorf("%s: the error is %v", name, pdf.err)
		}
	}
}

// The rows of Markdown tables past 100,000 cells are counted, not kept: each
// row was padded to the header's 64 columns first, and 195 KB of rows of "|"
// made 336 MB of cells before the document was refused (C2).
func TestReview3TheRowsPastTheTableLimitAreNotKept(t *testing.T) {
	text := "|" + strings.Repeat(" a |", 64) + "\n|" + strings.Repeat("---|", 64) + "\n" +
		strings.Repeat("|\n", 100000)
	var before, after runtime.MemStats
	runtime.GC()
	runtime.ReadMemStats(&before)
	blocks := markdownParser{}.parse(text)
	runtime.ReadMemStats(&after)
	if allocated := after.TotalAlloc - before.TotalAlloc; allocated > 64<<20 {
		t.Errorf("%d MB allocated for %d KB of text", allocated>>20, len(text)>>10)
	}
	if rows := len(blocks[0].rows); rows > 100000/64+1 {
		t.Errorf("%d rows kept", rows)
	}
	if cells, _ := tableCells(blocks); cells != 64*100001 {
		t.Errorf("%d cells, not %d", cells, 64*100001)
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	var pages []*Page
	NewMarkdown(font, font, font, font, font).DrawOnPages(pdf, text, &pages, testLetterPortrait())
	testRecorded(t, pdf, "The tables of the Markdown have more than 100000 cells: draw a table that large with BigTable.")
}

// Two characters copy as the same text, as a letter and its presentation
// form, need no actual text for the second: Arabic text had three times the
// spans it needed, its content 27% larger (T2). Noto Sans Arabic, which this
// test read before 9.1.7, draws ل and its isolated form ﻝ with one glyph, and
// ١ and ۱ with one; IBM Plex Sans Arabic draws each with its own, so its
// character map is made to draw them as Noto Sans Arabic's does.
func TestReview3ACharacterCopiedAsTheFirstHasNoActualText(t *testing.T) {
	pdf := testNewPDF()
	font := NewFont(pdf, bytes.NewReader(testWithSharedGlyphs(t,
		testFontBytes(t, "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"),
		[][2]rune{{0x0644, 0xFEDD}, {0x0661, 0x06F1}})))
	if font.unicodeToGID.get(0x0644) != font.unicodeToGID.get(0xFEDD) {
		t.Fatal("the font draws ل and its isolated form with two glyphs")
	}
	font.useChar(0xFEDD, font.unicodeToGID.get(0xFEDD))
	if font.drawnAsAnother(0x0644) {
		t.Error("ل copies as its isolated form, and has an actual text")
	}
	font.useChar(0x0661, font.unicodeToGID.get(0x0661))
	if !font.drawnAsAnother(0x06F1) {
		t.Error("۱ copies as ١, and has no actual text")
	}
}

// A glyph drawn for another character stays in the span of a word with
// moved marks, whose actual text gives the character: in a span of its own,
// the word's span began with a moved mark and the space that stands for it,
// and "ةِمى" was copied as "ىمِ ة" (T1).
func TestReview3AWordWithMarksIsCopiedWithoutASpaceInside(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"))
	page := NewPage(pdf, testLetterPortrait())
	NewTextLine(font, "\u0628").SetLocation(50, 50).DrawOn(page)
	// The glyph of ة as drawn first for another character, which no two
	// characters of this font copied as different text are
	font.program.useFirstChar(font.unicodeToGID.get(0x0629), 'X')
	NewTextLine(font, "ةِمى").SetLocation(50, 100).DrawOn(page)
	content := testContent(page)
	if strings.Contains(content, "/ActualText <FEFF0020") {
		t.Errorf("an actual text starts with a space: %q", content)
	}
	if !strings.Contains(content, "/ActualText <"+toUTF16Hex("ةِمى")+">") {
		t.Errorf("the word is not copied as written: %q", content)
	}
}

// A cell's word broken between its characters breaks again at a soft hyphen,
// as TextBlock and TextFrame do: "1234<SHY>5678" was 123 / 4<SHY>56 / 78 (T4).
func TestReview3ACellBreaksAtASoftHyphenAfterACharacterBreak(t *testing.T) {
	font := testHelvetica(testNewPDF())
	cell := NewCell(font, "1234­5678")
	cell.SetWidth(font.StringWidth(font.GetSize(), "123"))
	cell.SetLeftPadding(0)
	cell.SetRightPadding(0)
	var lines []string
	for _, line := range wrapCellText([]*Cell{cell}, 0) {
		lines = append(lines, strings.ReplaceAll(line, "­", ""))
	}
	if want := []string{"123", "4-", "567", "8"}; strings.Join(lines, "/") != strings.Join(want, "/") {
		t.Errorf("the lines %q, not %q", lines, want)
	}
	cell = NewCell(font, "W")
	cell.SetWidth(1)
	if lines := wrapCellText([]*Cell{cell}, 0); len(lines) != 1 || lines[0] != "W" {
		t.Errorf("a character wider than the column: %q", lines)
	}
}

// A CJK font fits all of a text in a width of more characters than it has,
// as 1e30 or an infinity, as Java does: int of it is no number of characters,
// and none fit in Go (T6).
func TestReview3ACJKFontFitsAllOfATextInAHugeWidth(t *testing.T) {
	font := NewCJKFont(testNewPDF(), cjkfont.AdobeMingStdLight)
	text := "\u4E2D\u6587\u5B57"
	for _, width := range []float32{1e30, float32(math.Inf(1)), math.MaxFloat32} {
		if got := font.GetFitChars(text, width); got != 3 {
			t.Errorf("%g: %d characters fit, not 3", width, got)
		}
	}
	for _, width := range []float32{float32(math.NaN()), -1, 0} {
		if got := font.GetFitChars(text, width); got != 0 {
			t.Errorf("%g: %d characters fit, not 0", width, got)
		}
	}
	if got := font.GetFitChars(text, 2*font.GetSize()); got != 2 {
		t.Errorf("%d characters fit in two, not 2", got)
	}
}

// A zero width joiner the font has no glyph for, in left to right text
// between two LRMs, is in the actual text of the run, as the stamp keeps it:
// it was left out (T7).
func TestReview3AJoinerInARunIsInItsActualText(t *testing.T) {
	pdf := testNewPDF()
	// IBM Plex Sans JP, the .otf PDFjet carries, has no glyph for the joiners
	// or the LRM, as Noto Sans JP, before 9.1.7, had none.
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf"))
	if font.unicodeToGID.get(0x200D) != 0 {
		t.Fatal("the font has a glyph for the ZWJ")
	}
	page := NewPage(pdf, testLetterPortrait())
	NewTextLine(font, "d‍‎‌e‍").SetLocation(50, 50).DrawOn(page)
	if content := testContent(page); !strings.Contains(content, "/ActualText <"+toUTF16Hex("‎e‍‎")+">") {
		t.Errorf("the ZWJ is not in the actual text: %q", content)
	}
}

// testAllocated returns the bytes the function allocates.
func testAllocated(fn func()) uint64 {
	var before, after runtime.MemStats
	runtime.GC()
	runtime.ReadMemStats(&before)
	fn()
	runtime.ReadMemStats(&after)
	return after.TotalAlloc - before.TotalAlloc
}

// A /MediaBox that refers to an object of more than a box is not read: 1,000
// pages over an array of a million numbers, a PDF of 2 MB, copied the array
// for each page, 15 GB in Go (N1).
func TestReview3AMediaBoxOfAnotherObjectIsReadOnlyWhenItIsABox(t *testing.T) {
	const pages = 200
	kids := make([]string, pages)
	for i := range kids {
		kids[i] = strconv.Itoa(5+i) + " 0 R"
	}
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [" + strings.Join(kids, " ") + "] /Count " + strconv.Itoa(pages) + " >>",
		"[" + strings.Repeat("0 ", 100000) + "]",
		"[0 0 612 792]",
	}
	for i := range pages {
		box := "3 0 R"
		if i == 0 {
			box = "4 0 R"
		}
		objects = append(objects, "<< /Type /Page /Parent 2 0 R /MediaBox "+box+" >>")
	}
	pdf := testNewPDF()
	read := testRead(t, testPDFWithObjects(objects...))
	var found []*PDFobj
	if allocated := testAllocated(func() { found = pdf.GetPageObjects(read) }); allocated > 64<<20 {
		t.Errorf("%d MB allocated", allocated>>20)
	}
	if len(found) != pages {
		t.Fatalf("%d pages", len(found))
	}
	if box := found[0].GetValue("/MediaBox"); box != "[ 0 0 612 792 ]" {
		t.Errorf("the box of the first page is not read: %q", box)
	}
	if box := found[1].GetValue("/MediaBox"); box != "3" {
		t.Errorf("the box of the second page is read: %.20q", box)
	}
}

// A /Font dictionary that is an object of its own, which small resources of
// each page refer to, is added once: 4,000 pages over one of 4,000 fonts, a
// PDF of 1 MB, copied it for each page, 4.6 GB in Go (N2).
func TestReview3AFontDictionaryOfItsOwnIsAddedOnce(t *testing.T) {
	const pages, fonts = 1000, 1000
	entries := make([]string, fonts)
	for i := range entries {
		entries[i] = "/F" + strconv.Itoa(i) + " 4 0 R"
	}
	kids := make([]string, pages)
	for i := range kids {
		kids[i] = strconv.Itoa(5+i) + " 0 R"
	}
	objects := []string{
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [" + strings.Join(kids, " ") + "] /Count " + strconv.Itoa(pages) + " >>",
		"<< " + strings.Join(entries, " ") + " >>",
		"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
	}
	for range pages {
		objects = append(objects, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font 3 0 R >> >>")
	}
	pdf := testNewPDF()
	read := testRead(t, testPDFWithObjects(objects...))
	if allocated := testAllocated(func() { pdf.AddResourceObjects(read) }); allocated > 64<<20 {
		t.Errorf("%d MB allocated", allocated>>20)
	}
	if len(pdf.importedFonts) != 4*fonts {
		t.Errorf("%d tokens of fonts", len(pdf.importedFonts))
	}
}

// A token of an object stream's header longer than any number is copied no
// further than 20 bytes, and the TIFF predictor is undone in the data the
// reader decoded: each copied a stream, uncharged (N3).
func TestReview3TheReaderMakesNoUnchargedCopyOfAStream(t *testing.T) {
	header := objectStreamHeader([]byte(strings.Repeat("7", 100000)+" 5"), 2)
	if len(header) != 2 || header[0] != strings.Repeat("7", 20)+"..." || header[1] != "5" {
		t.Errorf("the header %.40q", header)
	}
	if _, err := objectStreamNumber(header[0]); err == nil {
		t.Error("a token of 100,000 digits is a number")
	}
	data := []byte{1, 1, 1, 5, 0, 0}
	if decoded := decompressor.ApplyPredictor(data, 2, 1, 8, 3); &decoded[0] != &data[0] {
		t.Error("the TIFF predictor copied the data")
	}
}

// An image of a PDF whose /Width is past an int32 takes the largest or the
// smallest, as in Java and Swift: int of 1e19 was the smallest int64, and
// 1e400 panicked (C8).
func TestReview3AWidthPastAnIntTakesTheLargestInt32(t *testing.T) {
	for width, pixels := range map[string]float32{
		"1e19": math.MaxInt32, "1e400": math.MaxInt32, "-1e19": math.MinInt32, "12.7": 12} {
		obj := newPDFobj()
		obj.dict = []string{"<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", width, "/Height", "1", ">>"}
		obj.stream = []byte{0} // an image object without one is refused
		if image := NewImageFromPDFobj(testNewPDF(), obj); image.GetWidth() != pixels {
			t.Errorf("%s: %g", width, image.GetWidth())
		}
	}
	// Only a number in decimal, in the four ports (review of 17 October 2026)
	for _, width := range []string{"NaN", "nan", "100f", "0x10", "inf", " 12", "5 0 R"} {
		obj := newPDFobj()
		obj.dict = []string{"<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", width, "/Height", "1", ">>"}
		obj.stream = []byte{0}
		func() {
			defer func() {
				if r := recover(); r != "The /Width of the image object is not a number: "+width {
					t.Errorf("%q: %v", width, r)
				}
			}()
			NewImageFromPDFobj(testNewPDF(), obj)
		}()
	}
}

// An extension lookup's offset of 32 bits is read unsigned, as Java and C#
// read it: on 32 bits, -58 wrapped round into the table, to the mark-to-base
// subtable before it (S4). CI runs it with GOARCH=386.
func TestGPOSAnExtensionOffsetPastTheTableIsNoSubtable(t *testing.T) {
	table := []byte{
		0, 1, 0, 0, 0, 0, 0, 0, 0, 56, // The header: version 1.0, lookup list at 56
		0, 1, 0, 12, 0, 18, 0, 1, 0, 24, 0, 36, // Mark to base at 10
		0, 1, 0, 1, 0, 5, // Mark coverage: glyph 5
		0, 1, 0, 1, 0, 7, // Base coverage: glyph 7
		0, 1, 0, 0, 0, 6, 0, 1, 0, 10, 0, 20, // Mark array: class 0, anchor 10, 20
		0, 1, 0, 4, 0, 1, 0, 30, 0, 40, // Base array: anchor 30, 40
		0, 1, 0, 4, // Lookup list at 56: one lookup, at 60
		0, 9, 0, 0, 0, 1, 0, 8, // Extension lookup: one subtable, at 68
		0, 1, 0, 4, 0xFF, 0xFF, 0xFF, 0xC6, // Extension: mark to base, at -58
	}
	if g := newGpos(table, 0, len(table), 10).read(); len(g.markAnchors) != 0 {
		t.Error("the extension wrapped round to the subtable before it")
	}
}

// An image turned a quarter of the way is as wide as it was tall: it gave its
// corner unturned, and in a table cell ran over the rows after it (F3).
func TestReview3AnImageTurnedAQuarterHasItsDrawnSize(t *testing.T) {
	pdf := testNewPDF()
	image := NewImageFromFile(pdf, testRepoPath(t, "tests/data/PngSuite/BASN2C08.PNG")) // 32 by 32
	image.ScaleByWidthAndHeight(0.75, 0.375)                                            // 24 by 12
	image.SetLocation(50, 50)
	for _, degrees := range []int{90, 270} {
		image.SetRotation(degrees)
		testAssertXY(t, 62, 74, image.DrawOn(nil))
		testNear(t, "width", 12, image.GetWidth(), testDelta)
		testNear(t, "height", 24, image.GetHeight(), testDelta)
		page := NewPage(pdf, testLetterPortrait())
		testAssertXY(t, 62, 74, image.DrawOn(page))
	}
	image.SetRotation(0)
	testAssertXY(t, 74, 62, image.DrawOn(nil))
	// Turned, the image fits the page as it is drawn
	image.SetRotation(90)
	image.ResizeToFit(NewPage(pdf, testLetterPortrait()), false)
	xy := image.DrawOn(nil)
	testNear(t, "right", 612, xy[0], testDelta)
	testNear(t, "bottom", 792, xy[1], testDelta)
}

// A text line in a text column is raised by its vertical offset once: the
// column added it, and drawing the line added it again, so a superscript
// raised 13 points rose 26 (F6).
func TestReview3ATextColumnRaisesASuperscriptOnce(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	paragraph := NewParagraph().Add(NewTextLine(font, "Base")).Add(NewTextLine(font, "sup").SetVerticalOffset(-13))
	column := NewTextColumn()
	column.SetLocation(50, 100)
	column.SetWidth(400)
	column.AddParagraph(paragraph)
	column.DrawOn(page)
	content := testContent(page)
	base := testPositionOf(t, content, "Base")
	sup := testPositionOf(t, content, "sup")
	testNear(t, "rise", 13, sup[1]-base[1], testDelta)
}

// A destination the document names keeps its name, and a bookmark whose
// name it is, dest#1, is named again: a link to the document's dest#1 went to
// the bookmark (F7).
func TestReview3ADestinationNameOfTheDocumentComesFirst(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	page.AddDestination("dest#1", 500)
	NewTextLine(font, "go").SetGoToAction("dest#1").SetLocation(50, 50).DrawOn(page)
	bookmark := NewBookmark(doc.pdf).AddBookmark(page, NewTitle(font, "A", 10, 100))
	raw := string(doc.complete())
	dests := regexp.MustCompile(`/Dest \[\d+ 0 R /XYZ 0 ([\d.]+) 0\]`).FindAllStringSubmatch(raw, -1)
	if len(dests) != 2 || dests[0][1] != "292" || dests[1][1] == "292" {
		t.Errorf("the link and the bookmark go to %v", dests)
	}
	if name := bookmark.GetDestinationName(); name == "dest#1" {
		t.Errorf("the bookmark's destination is %s", name)
	}
}

// A bookmark on a page that is not added fails the document: it went to
// /Dest [0 0 R], and readers refused the document (F8).
func TestReview3ABookmarkOnAPageNotAddedFailsTheDocument(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	NewPage(pdf, testLetterPortrait())
	detached := NewPageDetached(pdf, testLetterPortrait())
	NewBookmark(pdf).AddBookmark(detached, NewTitle(font, "Lost", 10, 100))
	testCompleteFails(t, pdf, "The bookmark Lost is on a page that is not added to the PDF.")
}

// An empty dash pattern of an arc is refused, with a stroke color and
// without, as the other shapes and the other ports refuse it: Go took it for
// none with a stroke color (S3, C7).
func TestReview3AnArcRefusesAnEmptyDashPattern(t *testing.T) {
	for _, colored := range []bool{false, true} {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		arc := NewArc().SetRadius(50).SetSweep(90).SetStrokeDashPattern("")
		arc.SetLocation(100, 100)
		if colored {
			arc.SetStrokeColor(color.Red)
		}
		arc.DrawOn(page)
		testRecorded(t, pdf, "The dash pattern \"\" is not an array of non-negative numbers, not all zero, "+
			"followed by a phase, such as \"[3 3] 0\".")
	}
}

// The edge cases of SetColumnWidthsInPercent: the columns set again after the
// percentages, fewer panicked and more were drawn past the page (C4); a
// column too narrow for " ..." drew it over the next column; the largest
// float32 as a share made the widths infinite, and Complete failed; a margin
// of half the page or more made widths of less than nothing (T5).
func TestReview3TheEdgeCasesOfColumnWidthsInPercent(t *testing.T) {
	draw := func(change func(*BigTable)) (*PDF, []*Page, error) {
		doc := testNewDoc()
		font := NewCoreFont(doc.pdf, corefont.Helvetica())
		table := NewBigTable(doc.pdf, font, font, letter.Portrait()).SetNumberOfColumns(3)
		table.SetLocation(36, 20)
		table.SetFooter("", nil)
		change(table)
		long := "A field far wider than a fifth of the width of the table"
		table.SetTableRows([]string{"A", "B", "C", "D"}, slices.Values([][]string{{long, "b", long, "d"}}))
		err := table.Complete()
		return doc.pdf, table.GetPages(), err
	}
	for _, columns := range []int{2, 4} {
		message, panicked := testPanic(func() {
			pdf, _, _ := draw(func(table *BigTable) {
				table.SetColumnWidthsInPercent(20, 30, 50)
				table.SetNumberOfColumns(columns)
			})
			testRecorded(t, pdf, "The table has "+strconv.Itoa(columns)+" columns, and 3 percentages: "+
				"give one for each column.")
		})
		if panicked {
			t.Errorf("%d columns: %s", columns, message)
		}
	}
	pdf, pages, err := draw(func(table *BigTable) { table.SetColumnWidthsInPercent(1, 98, 1) })
	if err != nil || pdf.err != nil {
		t.Fatal(err, pdf.err)
	}
	for _, text := range testDrawnText(pages[0]) {
		if strings.HasSuffix(text, " ...") && len(text) <= 4 {
			t.Errorf("a column too narrow for it draws %q", text)
		}
	}
	pdf, pages, err = draw(func(table *BigTable) { table.SetColumnWidthsInPercent(math.MaxFloat32, 1, 1) })
	if err != nil || pdf.err != nil {
		t.Fatal(err, pdf.err)
	}
	if !strings.Contains(testContent(pages[0]), "576 ") {
		t.Errorf("the first column is not the width of the table: %.300q", testContent(pages[0]))
	}
	pdf, _, _ = draw(func(table *BigTable) {
		table.SetColumnWidthsInPercent(20, 30, 50)
		table.SetLocation(400, 20)
	})
	testRecorded(t, pdf, "The margin of the table, its x, is half the width of the page or more: "+
		"the columns have no room.")
}

// A NaN or infinite angle fails the document, as a NaN coordinate does: it
// wrote 0 0 0 0 0 0 cm, and the arc or the container vanished (F12).
func TestReview3ANaNRotationFailsTheDocument(t *testing.T) {
	for _, degrees := range []float32{float32(math.NaN()), float32(math.Inf(1))} {
		pdf := testNewPDF()
		arc := NewArc().SetRadius(50).SetSweep(90).SetRotation(degrees)
		arc.SetLocation(100, 100)
		arc.DrawOn(NewPage(pdf, testLetterPortrait()))
		testRecorded(t, pdf, notWritable)

		pdf = testNewPDF()
		container := NewContainer(100, 100)
		container.SetRotation(float64(degrees))
		container.Add(NewArc().SetRadius(10).SetSweep(90))
		container.DrawOn(NewPage(pdf, testLetterPortrait()))
		testRecorded(t, pdf, notWritable)
	}
}

// A curve has two "c" control points, or one "v" or "y" one: a "c" curve of
// one control point wrote "c" with four numbers, not six, which readers
// refused (F11).
func TestReview3ACurveOfTheWrongControlPointsIsRefused(t *testing.T) {
	start, end := NewPoint(10, 10), NewPoint(100, 100)
	for _, path := range [][]*Point{
		{start, NewControlPointC(20, 20), end},
		{start, NewControlPointV(20, 20), NewControlPointV(30, 30), end},
		{start, NewControlPointC(20, 20), NewControlPointY(30, 30), end},
		{start, NewControlPointC(20, 20), NewControlPointC(30, 30), NewControlPointC(40, 40), end},
	} {
		pdf := testNewPDF()
		page := NewPage(pdf, testLetterPortrait())
		page.DrawPath(path, pathoperator.Stroke)
		testRecorded(t, pdf, "A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.")
		if strings.Contains(testContent(page), " c\n") {
			t.Errorf("the curve is drawn: %q", testContent(page))
		}
		pdf = testNewPDF()
		NewStamp(pdf).SetSize(200, 200).DrawPath(path, pathoperator.Stroke)
		testRecorded(t, pdf, "A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.")
	}
	for _, path := range [][]*Point{
		{start, NewControlPointC(20, 20), NewControlPointC(30, 30), end},
		{start, NewControlPointV(20, 20), end, NewControlPointY(30, 30), start},
	} {
		pdf := testNewPDF()
		NewPage(pdf, testLetterPortrait()).DrawPath(path, pathoperator.Stroke)
		if pdf.err != nil {
			t.Error(pdf.err)
		}
	}
}

// A point a path lists twice is scaled once: (10, 10) listed twice and scaled
// by 2 became (40, 40), not (20, 20) (F9).
func TestReview3APointListedTwiceIsScaledOnce(t *testing.T) {
	shared := NewPoint(10, 10)
	other := NewPoint(30, 5)
	NewPath().Add(shared).Add(other).Add(shared).ScaleBy(2)
	testNear(t, "x", 20, shared.GetX(), testDelta)
	testNear(t, "y", 20, shared.GetY(), testDelta)
	testNear(t, "other x", 60, other.GetX(), testDelta)
}

// A rounded rectangle of a negative width and height is the same rectangle
// the other way round: its corners were turned inside out (F10).
func TestReview3ARoundedRectangleOfANegativeSizeIsTheSame(t *testing.T) {
	draw := func(x, y, w, h float32) string {
		page := testNewPage()
		NewRect(x, y, w, h).SetCornerRadius(10).SetFillColor(color.Red).DrawOn(page)
		return testContent(page)
	}
	if want, got := draw(50, 60, 50, 40), draw(100, 100, -50, -40); got != want {
		t.Errorf("drew %q, not %q", got, want)
	}
}

// A stamp is given its size before anything is drawn on it, as its y runs
// down from its top: drawn before, the content was below the stamp, and was
// not seen (F13).
func TestReview3AStampIsSizedBeforeItIsDrawnOn(t *testing.T) {
	pdf := testNewPDF()
	NewStamp(pdf).FillRect(0, 0, 20, 10)
	testRecorded(t, pdf, "Set the size of the stamp before drawing on it: its y runs down from its top.")
	pdf = testNewPDF()
	NewStamp(pdf).SetSize(20, 10).FillRect(0, 0, 20, 10).SetSize(40, 20)
	testRecorded(t, pdf, "Set the size of the stamp before drawing on it: its y runs down from its top.")
	pdf = testNewPDF()
	NewStamp(pdf).SetSize(20, 10).FillRect(0, 0, 20, 10).SetSize(40, 10)
	if pdf.err != nil {
		t.Error(pdf.err)
	}
}

// Nothing is written after %%EOF: an image made after Complete was written
// after it, into the writer of the caller, with no error (F14).
func TestReview3NothingIsWrittenAfterTheEndOfThePDF(t *testing.T) {
	doc := testNewDoc()
	NewPage(doc.pdf, testLetterPortrait())
	raw := doc.complete()
	NewImageFromFile(doc.pdf, testRepoPath(t, "tests/data/PngSuite/BASN2C08.PNG"))
	doc.writer.Flush()
	if after := doc.buf.Bytes(); len(after) != len(raw) {
		t.Errorf("%d bytes written after %%%%EOF", len(after)-len(raw))
	}
	testRecorded(t, doc.pdf, "The PDF was already completed.")
}

// A point that is a link, drawn on its own, makes its link, and a marker of
// lines is stroked in the point's color: the link was not made, and a plus
// was drawn in the pen's color (F15).
func TestReview3APointDrawnOnItsOwnHasItsLinkAndColor(t *testing.T) {
	page := testNewPage()
	NewPoint(100, 100).SetURIAction("https://pdfjet.com").DrawOn(page)
	if len(page.annots) != 1 || page.annots[0].uri != "https://pdfjet.com" {
		t.Errorf("the links of the page: %d", len(page.annots))
	}
	page = testNewPage()
	NewPoint(100, 100).SetShape(shape.Plus).SetFillColor(color.Red).DrawOn(page)
	if content := testContent(page); !strings.Contains(content, "1 0 0 RG\n") {
		t.Errorf("the plus is not red: %q", content)
	}
}

// A heading that starts a list item or a quote keeps a line of the text after
// it on its page, as any other heading does: it was drawn into the bottom
// margin, down to 732 past a margin at 720, with nothing after it (F16).
func TestReview3AHeadingThatStartsAnItemStaysAboveTheMargin(t *testing.T) {
	testAllowCoreFonts(t)
	for _, container := range []string{"- ", "> "} {
		for lines := 15; lines < 35; lines++ {
			text := strings.Repeat("Line\n\n", lines) + container + "# HEADING\n" + container + "\n" + container + "after\n"
			drawn := testDrawMarkdown(text, "")
			for _, content := range drawn.contents {
				if !strings.Contains(content, testHex("HEADING")) {
					continue
				}
				if xy := testPositionOf(t, content, "HEADING"); xy[1] < 72+12 {
					t.Errorf("%q after %d lines: the heading is at %v, in the margin", container, lines, xy[1])
				}
				if !strings.Contains(content, testHex("after")) {
					t.Errorf("%q after %d lines: the heading has no text after it on its page", container, lines)
				}
			}
		}
	}
}

// A paragraph a text column draws has its coordinates, as one a text frame
// draws does: they stayed 0 (F17).
func TestReview3AParagraphOfATextColumnHasItsCoordinates(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	paragraph := NewParagraph().Add(NewTextLine(font, "Hello, world"))
	column := NewTextColumn()
	column.SetLocation(50, 100)
	column.SetWidth(400)
	column.AddParagraph(paragraph)
	column.DrawOn(NewPage(pdf, testLetterPortrait()))
	testNear(t, "x1", 50, paragraph.GetX1(), testDelta)
	testNear(t, "y1", 100, paragraph.GetY1(), testDelta)
	testNear(t, "text y", 100+font.GetAscent(font.GetSize()), paragraph.GetTextY(), testDelta)
	if !(paragraph.GetY2() > paragraph.GetTextY() && paragraph.GetX2() > 50) {
		t.Errorf("x2 %g, y2 %g", paragraph.GetX2(), paragraph.GetY2())
	}
}

// A cap style, a join style, a page layout, a page mode or a cell's vertical
// alignment that is not one is refused: CapStyle(7) wrote "7 J", "Single
// Page" broke the catalog, and alignment.Left in a cell panicked (F18).
func TestReview3AValueThatIsNotOneOfItsTypeIsRefused(t *testing.T) {
	page := testNewPage()
	page.SetLineCapStyle(capstyle.CapStyle(7))
	testRecorded(t, page.pdf, "The cap style 7 is not one: use capstyle.Butt, capstyle.Round or capstyle.ProjectingSquare.")
	page = testNewPage()
	page.SetLineJoinStyle(joinstyle.JoinStyle(-1))
	testRecorded(t, page.pdf, "The join style -1 is not one: use joinstyle.Miter, joinstyle.Round or joinstyle.Bevel.")
	pdf := testNewPDF().SetPageLayout("Single Page")
	testRecorded(t, pdf, "The page layout Single Page is not one: use one of pagelayout.")
	pdf = testNewPDF().SetPageMode("Outlines")
	testRecorded(t, pdf, "The page mode Outlines is not one: use one of pagemode.")

	pdf = testNewPDF()
	font := testHelvetica(pdf)
	table := NewTable()
	cell := NewCell(font, "text").SetVerticalAlignment(alignment.Left)
	table.SetTableData([][]*Cell{{cell}}, 0)
	table.SetLocation(50, 50)
	page = NewPage(pdf, testLetterPortrait())
	if message, panicked := testPanic(func() { table.DrawOn(page) }); panicked {
		t.Fatal(message)
	}
	testRecorded(t, pdf, cellValignMessage)
}

// Drawing on a page of a PDF that was read: the page's own content is drawn
// in a q and a Q of its own, so that a scale it leaves set does not change
// the drawing; a page with no /Contents gets the drawing, which was lost; and
// the drawing starts at the corner of the /MediaBox, as [100 100 712 892] put
// it outside the page (F2).
func TestReview3DrawingOnAPageOfAPDFThatWasRead(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [100 100 712 892] /Contents 4 0 R >>",
		"<< /Length 18 >>\nstream\n0.5 0 0 0.5 0 0 cm\nendstream",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
	))
	pdf := testNewPDF()
	pages := pdf.GetPageObjects(objects)
	for _, pageObj := range pages {
		page := NewPageFromObject(pdf, pageObj)
		page.FillRect(0, 0, 30, 30)
		page.Complete(&objects)
	}
	if pdf.err != nil {
		t.Fatal(pdf.err)
	}
	wrapped := pages[0].dict
	i := slices.Index(wrapped, "/Contents")
	if i == -1 || strings.Join(wrapped[i+1:i+12], " ") != "[ 6 0 R 4 0 R 7 0 R ]" {
		t.Errorf("the contents of the first page: %v", wrapped)
	}
	if before := string(objects[5].stream); before != "q\n" {
		t.Errorf("the stream before: %q", before)
	}
	if after := string(objects[6].stream); !strings.HasPrefix(after, "Q\nq\n1 0 0 1 100 100 cm\n") {
		t.Errorf("the stream after: %q", after)
	}
	added := pages[1].dict
	if i := slices.Index(added, "/Contents"); i == -1 || strings.Join(added[i+1:i+6], " ") != "[ 8 0 R ]" {
		t.Errorf("the contents of the second page: %v", added)
	}
}

// A document of PDF/A-1 of more than 8191 pages, the most entries of an array
// of PDF/A-1, is refused, and a text of PDF/A of more than 16383 characters,
// a string longer than 32767 bytes: veraPDF failed both (W1).
func TestReview3TheSizeLimitsOfPDFAAreChecked(t *testing.T) {
	pdf := testNewPDF().SetCompliance(compliance.PDF_A_1B)
	for range 8192 {
		NewPage(pdf, testLetterPortrait())
	}
	testCompleteFails(t, pdf, "PDF/A-1 limits an array to 8191 entries, and the page tree has 8192: "+
		"make the document PDF/A-2 or PDF/A-3.")

	for _, characters := range []int{16383, 16384} {
		pdf := testNewPDF().SetCompliance(compliance.PDF_A_2B)
		font := testPlexSans(pdf)
		page := NewPage(pdf, testLetterPortrait())
		NewTextLine(font, strings.Repeat("a", characters)).SetLocation(10, 20).DrawOn(page)
		if characters == 16383 && pdf.err != nil {
			t.Error(pdf.err)
		} else if characters == 16384 {
			testRecorded(t, pdf, pdfaStringTooLong)
		}
	}
}

// A /MediaBox of a NaN or infinite size is no size, and the page is Letter,
// as of one of 0: Swift stopped where such a page was watermarked (K1).
func TestReview3AMediaBoxOfANaNSizeIsLetter(t *testing.T) {
	huge := strings.Repeat("9", 40)
	for _, box := range [][]string{{"0", "0", "nan", "792"}, {"0", "0", "inf", "792"}, {huge, "0", "-" + huge, "792"}} {
		obj := newPDFobj()
		obj.dict = append([]string{"<<", "/Type", "/Page", "/MediaBox", "["}, append(box, "]", ">>")...)
		if size := obj.GetPageSize(); size.GetWidth() != 612 || size.GetHeight() != 792 {
			t.Errorf("%v: %g by %g", box, size.GetWidth(), size.GetHeight())
		}
	}
}

// An image or a barcode in a table cell is where the caller put it after the
// table is drawn, as a text block is: one at 7, 8 was left at 202, 319.9
// (R22).
func TestReview3ADrawableOfACellStaysWhereTheCallerPutIt(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	image := NewImageFromFile(pdf, testRepoPath(t, "tests/data/PngSuite/BASN2C08.PNG"))
	image.SetLocation(7, 8)
	barcode := NewBarcode(CODE_128, "12345")
	barcode.SetLocation(7, 8)
	before := [][2]float32{image.DrawOn(nil), barcode.DrawOn(nil)}
	table := NewTable()
	table.SetTableData([][]*Cell{{NewEmptyCell(font).SetImage(image), NewEmptyCell(font).SetBarcode(barcode)}}, 0)
	table.SetLocation(200, 300)
	table.DrawOn(NewPage(pdf, testLetterPortrait()))
	for i, after := range [][2]float32{image.DrawOn(nil), barcode.DrawOn(nil)} {
		testAssertXY(t, before[i][0], before[i][1], after)
	}
}

// A word of a text column too wide for its line breaks at a soft hyphen, the
// line ending with a hyphen, as TextBlock and TextFrame break it: TextColumn
// never broke at one.
func TestReview3ATextColumnBreaksAWordAtASoftHyphen(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	column := NewTextColumn()
	column.SetLocation(50, 100)
	column.SetWidth(testSoftHyphenWidth(font))
	column.AddParagraph(NewParagraph().Add(NewTextLine(font, "Ab "+testSoftHyphenWord)))
	page := NewPage(pdf, testLetterPortrait())
	column.DrawOn(page)
	// Ab Donau- / dampfschiff- / fahrt
	content := testContent(page)
	donau := testPositionOf(t, content, "Donau-")
	dampf := testPositionOf(t, content, "dampfschiff-")
	fahrt := testPositionOf(t, content, "fahrt")
	if !(dampf[1] < donau[1] && fahrt[1] < dampf[1]) {
		t.Errorf("the word is not broken at its soft hyphens: %q", content)
	}
}

// The number that tells a font program apart, and that the tag of its subset
// is made of, folds the blocks of 256 characters the font has glyphs in, and
// leaves out the rest: it folded all 65,536 characters, 236 empty blocks of
// IBM Plex Sans's 256. The number is the same in the four ports.
func TestReview3AFontChecksumLeavesOutEmptyBlocks(t *testing.T) {
	font := NewFontFromFile(testNewPDF(), testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
	if font.checksum != 0xABF1C2C88C082202 {
		t.Errorf("checksum %X", font.checksum)
	}
}

// A font is read into a buffer of its size, once, from a reader that says
// how many bytes it has left: io.ReadAll grew its buffer as it read, and
// copied a font of 200 KB several times, about half of what a document of
// three embedded fonts allocated.
func TestReview3AFontIsReadIntoABufferOfItsSize(t *testing.T) {
	data := testFontBytes(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")
	var before, after runtime.MemStats
	runtime.GC()
	runtime.ReadMemStats(&before)
	buf := readFontData(bytes.NewReader(data))
	runtime.ReadMemStats(&after)
	if !bytes.Equal(buf, data) {
		t.Fatal("the font read is not the font")
	}
	if allocated := after.TotalAlloc - before.TotalAlloc; allocated > uint64(len(data))*5/4*fuzzRaceAlloc {
		t.Errorf("reading a font of %d bytes allocated %d", len(data), allocated)
	}
}

// A word too wide for the column whose first piece does not fit the rest of
// its line breaks at its soft hyphens on the next lines: it was put on the
// next line whole, past the column's edge (review of 12 October 2026).
func TestReview3ATextColumnBreaksAWordThatStartsANewLine(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	font.SetSize(10)
	column := NewTextColumn()
	column.SetLocation(50, 100)
	column.SetWidth(100)
	word := strings.Repeat("bbbbbbbbbb­", 3) + "bbbbbbbbbb"
	column.AddParagraph(NewParagraph().Add(NewTextLine(font, strings.Repeat("a", 17)+" "+word)))
	page := NewPage(pdf, testLetterPortrait())
	column.DrawOn(page)
	// aaaaaaaaaaaaaaaaa / bbbbbbbbbb- / bbbbbbbbbb- / bbbbbbbbbb- / bbbbbbbbbb
	content := testContent(page)
	a := testPositionOf(t, content, strings.Repeat("a", 17))
	b := testPositionOf(t, content, "bbbbbbbbbb-")
	if !(b[1] < a[1]) {
		t.Errorf("the word is not broken at its soft hyphens: %q", content)
	}
}
