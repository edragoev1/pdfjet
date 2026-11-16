// review_9_1_9_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"encoding/binary"
	"math"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansKR"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The faults found by the review of 9.1.9 (16 November 2026), each kept from
// coming back.

// testOutlineWithGlyphs returns the outline of Cascadia Mono with the glyphs
// given made the bytes given, written after its glyf table.
func testOutlineWithGlyphs(t *testing.T, glyphs map[int][]byte) *glyphOutline {
	t.Helper()
	cascadia := NewFontFromFile(testNewPDF(), testRepoPath(t, testCascadia))
	base, err := newGlyphOutline(cascadia.program.font, false)
	if err != nil {
		t.Fatal(err)
	}
	if !base.longOffsets {
		t.Fatal("short loca offsets")
	}
	o := *base
	data := append([]byte(nil), base.data...)
	for gid, g := range glyphs {
		for (len(data)-base.glyfOffset)%4 != 0 {
			data = append(data, 0)
		}
		start := len(data) - base.glyfOffset
		data = append(data, g...)
		// The glyph after is not one of those given, so its start is kept
		binary.BigEndian.PutUint32(data[base.locaOffset+4*gid:], uint32(start))
		binary.BigEndian.PutUint32(data[base.locaOffset+4*gid+4:], uint32(start+len(g)))
	}
	o.data = data
	o.glyfLength = len(data) - base.glyfOffset
	return &o
}

// testCompositeGlyph returns a composite glyph of the parts, each placed at 0 0.
func testCompositeGlyph(parts ...int) []byte {
	glyph := []byte{0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0} // -1 contours, the box
	for i, part := range parts {
		flags := uint16(0x0002) // ARGS_ARE_XY_VALUES, the arguments bytes
		if i < len(parts)-1 {
			flags |= 0x0020 // MORE_COMPONENTS
		}
		glyph = binary.BigEndian.AppendUint16(glyph, flags)
		glyph = binary.BigEndian.AppendUint16(glyph, uint16(part))
		glyph = append(glyph, 0, 0)
	}
	return glyph
}

// A composite glyph of four parts, each of four parts, 16 deep, down to an O,
// was 4^16 Os read; the work of a glyph is bounded.
func TestATrueTypeCompositeGlyphOfManyPartsIsRefusedQuickly(t *testing.T) {
	cascadia := NewFontFromFile(testNewPDF(), testRepoPath(t, testCascadia))
	o := cascadia.unicodeToGID.get('O')
	// Every other glyph of the last 40, so that the glyph after each keeps
	// its start, and none is the O
	glyphs := make(map[int][]byte)
	below := o
	first := len(cascadia.advanceWidth) - 40
	gid := first
	for level := 0; level < 16; level++ {
		glyphs[gid] = testCompositeGlyph(below, below, below, below)
		below = gid
		gid += 2
	}
	outline := testOutlineWithGlyphs(t, glyphs)
	// Two levels are drawn, as a font may have them
	if path, err := outline.outline(first + 2); err != nil || strings.Count(path.operators, " m\n") != 32 {
		t.Errorf("two levels: %v, %d contours", err, strings.Count(path.operators, " m\n"))
	}
	start := time.Now()
	if _, err := outline.outline(below); err == nil {
		t.Error("no error")
	}
	if elapsed := time.Since(start); elapsed > 2*time.Second {
		t.Errorf("%v", elapsed)
	}
}

// testCFFOutlineWithCharString returns the outline of IBM Plex Sans with the
// charstring of glyph 1 made the bytes given.
func testCFFOutlineWithCharString(t *testing.T, cs []byte) *glyphOutline {
	t.Helper()
	base, err := newGlyphOutline(testPlexSans(testNewPDF()).program.font, true)
	if err != nil {
		t.Fatal(err)
	}
	o := *base
	o.data = append(append([]byte(nil), base.data...), cs...)
	objects := append([]int(nil), base.charStrings.objects...)
	objects[1] = len(base.data)
	objects[2] = len(o.data)
	o.charStrings.objects = objects
	return &o
}

// A charstring's arithmetic made a point of 1.15e18 units, written as a
// number of no meaning, or one Swift trapped on; a point past the bound is a
// damaged glyph.
func TestACFFGlyphOfAPointFarOutIsRefused(t *testing.T) {
	// 32767 dup mul dup mul 0 rmoveto 0 1 rlineto endchar
	cs := []byte{0x1C, 0x7F, 0xFF, 0x0C, 0x1B, 0x0C, 0x18, 0x0C, 0x1B, 0x0C, 0x18, 0x8B, 0x15, 0x8B, 0x8C, 0x05, 0x0E}
	if path, err := testCFFOutlineWithCharString(t, cs).outline(1); err == nil {
		t.Errorf("no error: %.80s", path.operators)
	}
}

// roll, index, put and get take their operands as numbers before integers,
// so that 2^31 is refused in the four ports alike.
func TestACFFRollOrIndexOfAnOperandTooLargeIsRefused(t *testing.T) {
	// 2^31 as 32767 1 add 65536 mul, with 65536 as 32767 2 mul 2 add
	big := []byte{0x1C, 0x7F, 0xFF, 0x8C, 0x0C, 0x0A, 0x1C, 0x7F, 0xFF, 0x8D, 0x0C, 0x18, 0x8D, 0x0C, 0x0A, 0x0C, 0x18}
	for name, op := range map[string][]byte{
		"roll":  slicesConcat([]byte{0x8C, 0x8D, 0x8E, 0x8E}, big, []byte{0x0C, 0x1E}),
		"index": slicesConcat([]byte{0x8C}, big, []byte{0x0C, 0x1D}),
		// 1 at 2^31 put, and get of 2^31: the storage has 32 places
		"put": slicesConcat([]byte{0x8C}, big, []byte{0x0C, 0x14}),
		"get": slicesConcat(big, []byte{0x0C, 0x15}),
	} {
		cs := slicesConcat(op, []byte{0x8B, 0x8B, 0x15, 0x0E}) // 0 0 rmoveto endchar
		if _, err := testCFFOutlineWithCharString(t, cs).outline(1); err == nil {
			t.Errorf("%s: no error", name)
		}
	}
}

func slicesConcat(parts ...[]byte) []byte {
	var all []byte
	for _, p := range parts {
		all = append(all, p...)
	}
	return all
}

// A position of a glyph not finite, or far off the page, is an error, not a
// number of no meaning in the content.
func TestDrawGlyphsAPositionNotFiniteIsAnError(t *testing.T) {
	for _, x := range []float32{float32(math.Inf(1)), float32(math.NaN()), 2e6} {
		doc := testNewDoc()
		font := testPlexSans(doc.pdf)
		page := NewPage(doc.pdf, letter.Portrait())
		page.DrawGlyphs(font, []int{36}, []float32{x, 100}, "A", "")
		if doc.pdf.err == nil || !strings.Contains(doc.pdf.err.Error(), "positions between") {
			t.Errorf("%v: %v", x, doc.pdf.err)
		}
	}
}

// The header of a BigTable whose header and rows have one font took the
// rows' fallback font: the header has its own, and the rows theirs.
func TestBigTableOfOneFontGivesTheHeaderItsOwnFallbackFont(t *testing.T) {
	pdf := testNewPDF()
	plex := testPlexSans(pdf)
	kr := NewFontFromFile(pdf, IBMPlexSansKR.Regular)
	bt := NewBigTable(pdf, plex, plex, letter.Portrait()).SetFallbackFonts(kr, nil)
	if bt.measureOf(true).fallback != kr || bt.measureOf(false).fallback != nil {
		t.Error("the header's fallback font is not its own")
	}
	bt = NewBigTable(pdf, plex, plex, letter.Portrait()).SetFallbackFonts(nil, kr)
	if bt.measureOf(true).fallback != nil || bt.measureOf(false).fallback != kr {
		t.Error("the rows' fallback font is not theirs")
	}
}

// Two lines that meet end to end, or edge to edge, may paint one pixel, the
// hairline of width 0 most of all: they are stroked in the order of the cells,
// or the color on top of that pixel changed when the borders were joined.
func TestTableLinesThatMeetOverlap(t *testing.T) {
	black := &tableSegment{vertical: true, at: 50, from: 332.94, to: 350.82}
	blue := &tableSegment{vertical: true, at: 50, from: 350.82, to: 368.69, color: [3]float32{0, 0, 1}}
	if !black.overlaps(blue) || !blue.overlaps(black) {
		t.Error("hairlines that meet end to end do not overlap")
	}
	red := &tableSegment{at: 100, from: 50, to: 148, width: 3, color: [3]float32{1, 0, 0}}
	hairline := &tableSegment{vertical: true, at: 148, from: 100, to: 120}
	if !red.overlaps(hairline) {
		t.Error("a hairline at the end of a wide line does not overlap it")
	}
	far := &tableSegment{vertical: true, at: 160, from: 100, to: 120}
	if red.overlaps(far) {
		t.Error("lines 10 points apart overlap")
	}
}

// A heading, a rule and a table: the rule is not a paragraph to measure, and
// Java and C# threw on its text, which is none.
func TestMarkdownHeadingRuleAndTableAreDrawn(t *testing.T) {
	md := "# " + strings.Repeat("word ", 1500) + "\n\n---\n\n| a | b |\n|---|---|\n| 1 | 2 |\n"
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown(md, "")
	if len(drawn.pages) < 2 || !strings.Contains(drawn.text, "a") {
		t.Errorf("%d pages", len(drawn.pages))
	}
}

// Drawing on a page of an existing PDF that has no content: the content
// restored a graphics state it never saved, "Q" first.
func TestDrawingOnAnEmptyPageOfAnExistingPDFIsBalanced(t *testing.T) {
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"))
	doc := testNewDoc()
	page := NewPageFromObject(doc.pdf, doc.pdf.GetPageObjects(objects)[0])
	page.DrawRect(10, 10, 20, 20)
	page.Complete(&objects)
	for _, obj := range objects {
		if content := string(obj.stream); strings.Contains(content, " l\n") {
			if !strings.HasPrefix(content, "q\nQ\n") {
				t.Errorf("the content starts %.20q", content)
			}
			return
		}
	}
	t.Error("no content drawn")
}
