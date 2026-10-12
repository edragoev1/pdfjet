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
	"strings"
	"sync"
	"testing"
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
