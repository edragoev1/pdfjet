// draw_glyphs_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"compress/zlib"
	"io"
	"math"
	"regexp"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// DrawGlyphs draws glyphs another engine shaped, by their numbers, as
// outlines read from the font file: paths, no text and no font in the
// document; each glyph a form XObject placed wherever it is; in a tagged
// document marked content with the text as its actual text.

const testDevanagari = "examples/fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf"
const testCascadia = "tests/fonts/CascadiaMono-Regular.ttf"

// testForms returns the content of each form XObject of the document, inflated.
func testForms(t *testing.T, doc []byte) []string {
	var forms []string
	text := string(doc)
	for i := strings.Index(text, "/Subtype /Form"); i >= 0; {
		start := strings.Index(text[i:], "stream\n") + i + 7
		end := strings.Index(text[start:], "\nendstream") + start
		reader, err := zlib.NewReader(bytes.NewReader(doc[start:end]))
		if err != nil {
			t.Fatal(err)
		}
		content, err := io.ReadAll(reader)
		if err != nil {
			t.Fatal(err)
		}
		forms = append(forms, string(content))
		next := strings.Index(text[i+1:], "/Subtype /Form")
		if next < 0 {
			break
		}
		i += 1 + next
	}
	return forms
}

func TestDrawGlyphsTheGlyphsAreOutlinesAndNoFontIsWritten(t *testing.T) {
	doc := testNewDoc()
	font := testPlexSans(doc.pdf)
	font.SetSize(12)
	page := NewPage(doc.pdf, letter.Portrait())
	a := font.unicodeToGID.get('A')
	b := font.unicodeToGID.get('B')
	page.DrawGlyphs(font, []int{a, b}, []float32{50, 100, 60, 100}, "AB", "en")
	content := testContent(page)
	// Each placed by the matrix of the em at its place
	if !strings.Contains(content, "q 0.012 0 0 0.012 50 692 cm /Fm") ||
		!strings.Contains(content, "q 0.012 0 0 0.012 60 692 cm /Fm") {
		t.Error(content)
	}
	if strings.Contains(content, "BT") || strings.Contains(content, "Tf") || strings.Contains(content, "BDC") {
		t.Error(content) // Untagged: just the shapes
	}
	pdf := string(doc.complete())
	for _, part := range []string{"/FontFile", "/Type /Font", "/ToUnicode"} {
		if strings.Contains(pdf, part) {
			t.Errorf("%s in the document", part)
		}
	}
	// The outlines: paths in the font's units, filled, no text
	forms := testForms(t, []byte(pdf))
	if len(forms) != 2 {
		t.Fatalf("%d forms", len(forms))
	}
	for _, form := range forms {
		if !strings.Contains(form, " m\n") || !strings.Contains(form, " l\n") || !strings.HasSuffix(form, "f\n") ||
			strings.Contains(form, "BT") || strings.Contains(form, "Tj") {
			t.Error(form)
		}
	}
}

func TestDrawGlyphsAGlyphDrawnTwiceIsOneStampPlacedTwice(t *testing.T) {
	doc := testNewDoc()
	font := testPlexSans(doc.pdf)
	font.SetSize(10)
	page := NewPage(doc.pdf, letter.Portrait())
	a := font.unicodeToGID.get('A')
	b := font.unicodeToGID.get('B')
	space := font.unicodeToGID.get(' ')
	page.DrawGlyphs(font, []int{a, space, b, a}, []float32{50, 100, 60, 100, 65, 100, 70, 100}, "A BA", "")
	content := testContent(page)
	m := regexp.MustCompile(`q 0.01 0 0 0.01 (50|70) 692 cm (/Fm\d+) Do Q\n`).FindAllStringSubmatch(content, -1)
	if len(m) != 2 || m[0][2] != m[1][2] {
		t.Fatal(content)
	}
	name := m[0][2]
	if strings.Count(content, " Do Q\n") != 3 { // The space draws nothing
		t.Error(content)
	}
	// Drawn again, in another run and at another size, A is placed again
	font.SetSize(20)
	page.DrawGlyphs(font, []int{a}, []float32{80, 100}, "A", "")
	content = testContent(page)
	if !strings.Contains(content, "q 0.02 0 0 0.02 80 692 cm "+name+" Do Q\n") {
		t.Error(content)
	}
	pdf := string(doc.complete())
	if strings.Count(pdf, "/Subtype /Form") != 2 {
		t.Error("A and B, each once")
	}
	if !strings.Contains(pdf, name+" ") {
		t.Error("the stamp is not a resource")
	}
}

func TestDrawGlyphsInATaggedParagraphTheGlyphsCarryTheirText(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	a := font.unicodeToGID.get('A')
	page.AddBDC(structelem.P, "hi", "", "")
	page.DrawGlyphs(font, []int{a}, []float32{50, 100}, "क", "hi")
	page.AddEMC()
	content := testContent(page)
	if !strings.Contains(content, "/P <</MCID 0>>\nBDC\n/Span <</Lang <6869> /ActualText <"+
		toUTF16Hex("क")+">>> BDC\nq ") ||
		!strings.Contains(content, " Do Q\nEMC\nEMC\n") || strings.Count(content, "BDC\n") != 2 {
		t.Error(content)
	}
	if strings.Contains(string(doc.complete()), "/FontFile") {
		t.Error("a font program")
	}
}

func TestDrawGlyphsOutsideAnyContentTheGlyphsAreAParagraphOfTheirOwn(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	a := font.unicodeToGID.get('A')
	page.DrawGlyphs(font, []int{a}, []float32{50, 100}, "A", "")
	content := testContent(page)
	if !strings.Contains(content, "/P <</MCID 0>>\nBDC\n/Span <</ActualText <"+toUTF16Hex("A")+">>> BDC\n") {
		t.Error(content)
	}
	if doc.pdf.err != nil {
		t.Fatal(doc.pdf.err)
	}
	// A tagged document asks for the text the glyphs stand for
	next := NewPage(doc.pdf, letter.Portrait())
	next.DrawGlyphs(font, []int{a}, []float32{50, 100}, "", "")
	if doc.pdf.err == nil {
		t.Error("no error for no text")
	}
}

func TestDrawGlyphsAFontUsedForTextTooIsWritten(t *testing.T) {
	doc := testNewDoc()
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	page.DrawGlyphs(font, []int{font.unicodeToGID.get('A')}, []float32{50, 100}, "A", "")
	NewTextLine(font, "Text").SetLocation(50, 200).DrawOn(page)
	if !strings.Contains(string(doc.complete()), "/FontFile3") {
		t.Error("no font of the text line")
	}
}

func TestDrawGlyphsTrueTypeOutlinesAreCubicCurvesAndCompositesHaveTheirParts(t *testing.T) {
	pdf := testNewPDF()
	cascadia := NewFontFromFile(pdf, testRepoPath(t, testCascadia))
	outline, err := newGlyphOutline(cascadia.program.font, false)
	if err != nil {
		t.Fatal(err)
	}
	if math.Abs(outline.matrixOf(1)[0]-1.0/2048) > 1e-12 {
		t.Error(outline.matrixOf(1))
	}
	ops := func(o *glyphOutline, gid int) string {
		path, err := o.outline(gid)
		if err != nil {
			t.Fatal(err)
		}
		return path.operators
	}
	// O: two contours of quadratic curves, drawn as cubic ones
	o := ops(outline, cascadia.unicodeToGID.get('O'))
	if strings.Count(o, " m\n") != 2 || !strings.Contains(o, " c\n") {
		t.Error(o)
	}
	// Aacute, a composite glyph: the contours of A and of the acute
	capitalA := ops(outline, cascadia.unicodeToGID.get('A'))
	composite := ops(outline, cascadia.unicodeToGID.get(0x00C1))
	if strings.Count(composite, " m\n") <= strings.Count(capitalA, " m\n") ||
		!(strings.HasPrefix(composite, capitalA) || strings.HasSuffix(composite, capitalA)) {
		t.Error(composite)
	}

	devanagari := NewFontFromFile(pdf, testRepoPath(t, testDevanagari))
	noto, err := newGlyphOutline(devanagari.program.font, false)
	if err != nil {
		t.Fatal(err)
	}
	ii := ops(noto, 12) // iideva, a composite glyph
	if !strings.Contains(ii, " m\n") || !strings.Contains(ii, " c\n") {
		t.Error(ii)
	}
	if ops(noto, devanagari.unicodeToGID.get(' ')) != "" {
		t.Error("the space has an outline")
	}
}

func TestDrawGlyphsCFFOutlinesHaveTheirSubroutinesAndMatrix(t *testing.T) {
	pdf := testNewPDF()
	plex := testPlexSans(pdf)
	outline, err := newGlyphOutline(plex.program.font, true)
	if err != nil {
		t.Fatal(err)
	}
	if math.Abs(outline.matrixOf(1)[0]-0.001) > 1e-12 {
		t.Error(outline.matrixOf(1))
	}
	o, err := outline.outline(plex.unicodeToGID.get('O'))
	if err != nil {
		t.Fatal(err)
	}
	if strings.Count(o.operators, " m\n") != 2 || o.yMin >= 0 || o.yMax <= 600 {
		t.Error(o)
	}
	space, _ := outline.outline(plex.unicodeToGID.get(' '))
	if space.operators != "" {
		t.Error("the space has an outline")
	}
}

func TestDrawGlyphsCFFOutlinesOfAFontMatrixOfTheirOwnAreThoseOfTheTrueTypeEdition(t *testing.T) {
	// Cascadia Mono's .otf has 2,048 units to the em, in its FontMatrix,
	// 0.00048828: its glyphs, of the numbers of those of its .ttf, are of
	// their size and in their place
	otf := NewFontFromFile(testNewPDF(), "fonts/CascadiaMono/CascadiaMono-Regular.otf")
	// In a PDF of its own: a font of the name and the glyphs of one read
	// before is that one
	ttf := NewFontFromFile(testNewPDF(), testRepoPath(t, testCascadia))
	cff, err := newGlyphOutline(otf.program.font, true)
	if err != nil {
		t.Fatal(err)
	}
	glyf, err := newGlyphOutline(ttf.program.font, false)
	if err != nil {
		t.Fatal(err)
	}
	if !otf.program.cff || ttf.program.cff || math.Abs(cff.matrixOf(1)[0]-1.0/2048) > 1e-8 {
		t.Error(cff.matrixOf(1))
	}
	for _, c := range "OAgé" {
		gid := otf.unicodeToGID.get(c)
		if gid == 0 || gid != ttf.unicodeToGID.get(c) {
			t.Fatalf("%c: glyph %d, of the .ttf %d", c, gid, ttf.unicodeToGID.get(c))
		}
		a, err := cff.outline(gid)
		if err != nil {
			t.Fatal(err)
		}
		b, err := glyf.outline(gid)
		if err != nil {
			t.Fatal(err)
		}
		if a.xMin != b.xMin || a.yMin != b.yMin || a.xMax != b.xMax || a.yMax != b.yMax {
			t.Errorf("%c: %v %v %v %v, the .ttf %v %v %v %v", c, a.xMin, a.yMin, a.xMax, a.yMax,
				b.xMin, b.yMin, b.xMax, b.yMax)
		}
	}
}

func TestDrawGlyphsNumbersAreHundredthsRoundedHalfAwayFromZero(t *testing.T) {
	for _, c := range []struct {
		value    float64
		decimals int
		want     string
	}{
		{-0.004, 2, "0"}, {1.5, 2, "1.5"}, {-8.0 / 3, 2, "-2.67"}, {12.0 / 2048, 7, "0.0058594"}, {99.999, 2, "100"},
	} {
		if got := glyphDecimals(c.value, c.decimals); got != c.want {
			t.Errorf("%v: %s, not %s", c.value, got, c.want)
		}
	}
}

func TestDrawGlyphsAttachTextCarriesTheTextAsAnAssociatedFile(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(font, "Text").SetLocation(50, 200).DrawOn(page)
	doc.pdf.AttachText("article-1.txt", "सभी text")
	pdf := string(doc.complete())
	for _, part := range []string{"/Subtype /text#2Fplain\n", "/AFRelationship /Source", "/AF [", "/Desc "} {
		if !strings.Contains(pdf, part) {
			t.Errorf("no %s", part)
		}
	}
}

func TestDrawGlyphsAttachTextIsRefusedInPDFA1AndPDFA2(t *testing.T) {
	for _, level := range []compliance.Compliance{compliance.PDF_A_1B, compliance.PDF_A_2B} {
		doc := testNewDoc()
		doc.pdf.SetCompliance(level)
		doc.pdf.AttachText("a.txt", "text")
		if doc.pdf.err == nil {
			t.Errorf("%s: no error", level.String())
		}
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
		page.DrawGlyphs(core, []int{1}, []float32{0, 0}, "A", "")
	})
	check("positions", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{a}, []float32{0}, "A", "")
	})
	check("glyph number", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{len(plex.advanceWidth)}, []float32{0, 0}, "A", "")
	})
	check("negative glyph number", func(page *Page, plex, core *Font, a int) {
		page.DrawGlyphs(plex, []int{-1}, []float32{0, 0}, "A", "")
	})
}

func TestDrawGlyphsAGlyphPastTheHorizontalMetricsIsDrawn(t *testing.T) {
	// Cascadia Mono has 3,183 horizontal metrics and 4,319 glyphs: the
	// glyphs past the metrics, U+2427 among them, have the advance of the
	// last, as OpenType has it. They were refused as glyphs the font has
	// not, and the whole document failed.
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testCascadia))
	font.SetSize(20)
	page := NewPage(doc.pdf, letter.Portrait())
	gid := font.unicodeToGID.get(0x2427)
	if gid < len(font.advanceWidth) {
		t.Fatalf("glyph %d is not past the %d metrics", gid, len(font.advanceWidth))
	}
	page.DrawGlyphs(font, []int{gid}, []float32{50, 100}, "␧", "")
	if doc.pdf.err != nil {
		t.Fatal(doc.pdf.err)
	}
	if forms := testForms(t, doc.complete()); len(forms) != 1 || !strings.HasSuffix(forms[0], "f\n") {
		t.Error(forms)
	}
}
