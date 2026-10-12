// review3_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"os"
	"path/filepath"
	"regexp"
	"runtime"
	"strconv"
	"strings"
	"sync"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
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
	png, err := os.ReadFile(testRepoPath(t, "PngSuite/BASN2C08.PNG"))
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
// spans it needed, its content 27% larger (T2).
func TestReview3ACharacterCopiedAsTheFirstHasNoActualText(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"))
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
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"))
	page := NewPage(pdf, testLetterPortrait())
	NewTextLine(font, "\u0628").SetLocation(50, 50).DrawOn(page)
	// The glyph of ة as drawn first for another character, which no two
	// characters of this font copied as different text are
	font.program.chars[font.unicodeToGID.get(0x0629)] = 'X'
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
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/NotoSansJP/NotoSansJP-Regular.ttf"))
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
		"1e19": math.MaxInt32, "1e400": math.MaxInt32, "-1e19": math.MinInt32, "nan": 0, "12.7": 12} {
		obj := newPDFobj()
		obj.dict = []string{"<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", width, "/Height", "1", ">>"}
		if image := NewImageFromPDFobj(testNewPDF(), obj); image.GetWidth() != pixels {
			t.Errorf("%s: %g", width, image.GetWidth())
		}
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
