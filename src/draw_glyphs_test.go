// draw_glyphs_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// DrawGlyphs draws glyphs another engine shaped, by their numbers, as real
// text: a span of actual text around them, each glyph at its place, and the
// glyphs no character maps to mapped to their cluster's text in ToUnicode.

// testUnmappedGlyph returns the first glyph of the font no character maps
// to, like a conjunct.
func testUnmappedGlyph(t *testing.T, font *Font) int {
	for gid := 1; gid < len(font.unicodeOf); gid++ {
		if font.unicodeOf[gid] < 0 {
			return gid
		}
	}
	t.Fatal("the font maps every glyph")
	return 0
}

func TestDrawGlyphsTheGlyphsAreRealTextInATaggedParagraph(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := testPlexSans(doc.pdf)
	font.SetSize(12)
	page := NewPage(doc.pdf, letter.Portrait())
	a := font.unicodeToGID.get('A')
	x := testUnmappedGlyph(t, font)
	page.AddBDC(structelem.P, "en", "", "")
	page.DrawGlyphs(font, []int{a, x}, []float32{50, 100, 60, 100}, []int{0, 1}, "AXY", "hi")
	page.AddEMC()
	content := testContent(page)
	if !strings.Contains(content, "/Span <</Lang <6869> /ActualText <"+toUTF16Hex("AXY")+">>> BDC\nBT\n") {
		t.Error(content)
	}
	if !strings.Contains(content, fmt.Sprintf("1 0 0 1 50 692 Tm\n<%04X> Tj\n", a)) {
		t.Error(content)
	}
	if !strings.Contains(content, fmt.Sprintf("1 0 0 1 60 692 Tm\n<%04X> Tj\nET\nEMC\n", x)) {
		t.Error(content)
	}
	if strings.Count(content, "BDC\n") != 2 { // The paragraph's, and the span's
		t.Error(content)
	}
	// The unmapped glyph maps to its cluster's characters, "XY"; the mapped
	// one keeps its own
	cmap := strings.ToLower(toUnicodeCMap(font, nil))
	if !strings.Contains(cmap, fmt.Sprintf("<%04x> <00580059>", x)) || strings.Contains(cmap, fmt.Sprintf("<%04x> <0058", a)) {
		t.Error(cmap)
	}
	if doc.pdf.err != nil {
		t.Error(doc.pdf.err)
	}
}

func TestDrawGlyphsOutsideAnyContentTheGlyphsAreAParagraphOfTheirOwn(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	page.DrawGlyphs(font, []int{font.unicodeToGID.get('A')}, []float32{50, 100}, nil, "A", "en")
	content := testContent(page)
	if !strings.HasPrefix(content, "/P <</MCID 0>>\nBDC\n/Span <<") || !strings.HasSuffix(content, "ET\nEMC\nEMC\n") {
		t.Error(content)
	}
	if len(page.structures) != 1 {
		t.Errorf("%d elements", len(page.structures))
	}
}

func TestDrawGlyphsAnUntaggedPageHasTheSpanOfActualTextAlone(t *testing.T) {
	doc := testNewDoc()
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	page.DrawGlyphs(font, []int{font.unicodeToGID.get('A')}, []float32{50, 100}, nil, "A", "")
	content := testContent(page)
	if !strings.HasPrefix(content, "/Span <</ActualText <"+toUTF16Hex("A")+">>> BDC\nBT\n") || strings.Contains(content, "MCID") {
		t.Error(content)
	}
}

func TestDrawGlyphsMisuseIsReported(t *testing.T) {
	check := func(name string, draw func(page *Page, plex, core *Font, a int)) {
		doc := testNewDoc()
		plex := testPlexSans(doc.pdf)
		core := testHelvetica(doc.pdf)
		page := NewPage(doc.pdf, letter.Portrait())
		draw(page, plex, core, plex.unicodeToGID.get('A'))
		if doc.pdf.err == nil {
			t.Errorf("%s: no error", name)
		}
	}
	check("core font", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(core, []int{1}, []float32{0, 0}, nil, "A", "")
	})
	check("positions", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{a}, []float32{0}, nil, "A", "")
	})
	check("glyph number", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{len(plex.advanceWidth)}, []float32{0, 0}, nil, "A", "")
	})
	check("cluster outside", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{a}, []float32{0, 0}, []int{1}, "A", "")
	})
	check("clusters count", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{a}, []float32{0, 0}, []int{0, 0}, "A", "")
	})
}
