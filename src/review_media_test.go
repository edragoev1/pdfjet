// review_media_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"io"
	"math"
	"os"
	"regexp"
	"strconv"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testOpenTypeWithTable returns the font with the table of the name replaced
// by the bytes, which are put at the end of the font.
func testOpenTypeWithTable(t *testing.T, font []byte, name string, table []byte) []byte {
	t.Helper()
	entry := testOpenTypeEntry(t, font, name)
	patched := append(append([]byte(nil), font...), table...)
	binary.BigEndian.PutUint32(patched[entry+8:], uint32(len(font)))
	binary.BigEndian.PutUint32(patched[entry+12:], uint32(len(table)))
	return patched
}

// testGposBomb returns a GPOS table of one lookup, of the given number of
// MarkToBase subtables that are all the same one: its marks and its letters
// are a coverage table of format 2 with one range, of glyph 0 alone, but at
// the coverage index 65535, and its letters have no classes of marks.
func testGposBomb(subtables int) []byte {
	var gpos []byte
	put16 := func(v int) { gpos = binary.BigEndian.AppendUint16(gpos, uint16(v)) }
	gpos = binary.BigEndian.AppendUint32(gpos, 0x00010000)
	put16(0)  // The script list
	put16(0)  // The feature list
	put16(10) // The lookup list
	put16(1)  // One lookup
	put16(4)  // At 4 from the lookup list
	put16(4)  // MarkToBase
	put16(0)  // No flags
	put16(subtables)
	for i := 0; i < subtables; i++ {
		put16(6 + 2*subtables) // Every subtable is the one after the lookup
	}
	put16(1)  // Format 1
	put16(12) // The marks
	put16(12) // The letters
	put16(0)  // No classes of marks
	put16(22) // The mark array
	put16(22) // The letter array
	put16(2)  // A coverage table of format 2
	put16(1)  // One range
	put16(0)  // From glyph 0
	put16(0)  // To glyph 0
	put16(65535)
	return append(gpos, make([]byte, 16)...)
}

func TestReviewMediaAGposTableOfSubtablesThatAreOneAnotherLoadsAtOnce(t *testing.T) {
	// The coverage index of the one glyph makes room for 65,536 glyphs, and
	// each of the 20,000 subtables went through all of them: 40 KB of GPOS
	// table kept a font from loading for a minute.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	font = testOpenTypeWithTable(t, font, "GPOS", testGposBomb(20000))
	start := time.Now()
	testWant(t, "(no panic)", testOpenTypeFontDraws(font))
	if elapsed := time.Since(start); elapsed > 2*time.Second {
		t.Errorf("the font loads in %v", elapsed)
	}
}

// testCmapOfSegments returns a character map of a format 4 subtable alone,
// for the Windows platform, of the segments of the start and end codes, each
// of delta 0, followed by the segment of 0xFFFF that ends every table.
func testCmapOfSegments(starts, ends []int) []byte {
	segments := len(starts) + 1
	var cmap []byte
	put16 := func(v int) { cmap = binary.BigEndian.AppendUint16(cmap, uint16(v)) }
	put16(0) // The version
	put16(1) // One encoding record
	put16(3) // Windows
	put16(1) // Unicode BMP
	cmap = binary.BigEndian.AppendUint32(cmap, 12)
	put16(4) // Format 4
	put16(16 + 8*segments)
	put16(0) // The language
	put16(2 * segments)
	put16(0) // The search range, the entry selector and the range shift
	put16(0)
	put16(0)
	for _, end := range ends {
		put16(end)
	}
	put16(0xFFFF)
	put16(0) // The reserved pad
	for _, start := range starts {
		put16(start)
	}
	put16(0xFFFF)
	for i := 0; i < segments; i++ {
		put16(0) // The deltas
	}
	for i := 0; i < segments; i++ {
		put16(0) // The range offsets
	}
	return cmap
}

func TestReviewMediaTheCharacterMapIsReadInOnePassOverItsSegments(t *testing.T) {
	// 32,766 segments of the character 0xFFFE alone, before the last one: a
	// search of every segment for every character took 2^31 comparisons. The
	// font has no OS/2 table, so that every character is looked up.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	font = testOpenTypeWithout(t, font, "OS/2")
	starts := make([]int, 32766)
	ends := make([]int, 32766)
	for i := range starts {
		starts[i] = 0xFFFE
		ends[i] = 0xFFFE
	}
	start := time.Now()
	otf := newOpenTypeFont(bytes.NewReader(testOpenTypeWithTable(t, font, "cmap",
		testCmapOfSegments(starts, ends))))
	if elapsed := time.Since(start); elapsed > 2*time.Second {
		t.Errorf("the font loads in %v", elapsed)
	}
	// The first segment of the character is the one it is in.
	if otf.unicodeToGID[0xFFFE] != 0xFFFE || otf.unicodeToGID['A'] != 0 {
		t.Errorf("0xFFFE is glyph %d, A glyph %d", otf.unicodeToGID[0xFFFE], otf.unicodeToGID['A'])
	}
	// Segments of 'A' to 'C' and 'X' to 'Z', with a delta of 0, map each
	// character to the glyph of its code, and none between them.
	otf = newOpenTypeFont(bytes.NewReader(testOpenTypeWithTable(t, font, "cmap",
		testCmapOfSegments([]int{'A', 'X'}, []int{'C', 'Z'}))))
	for ch, gid := range map[rune]int{'@': 0, 'A': 'A', 'C': 'C', 'D': 0, 'W': 0, 'X': 'X', 'Z': 'Z', '[': 0} {
		if otf.unicodeToGID[ch] != gid {
			t.Errorf("%q is glyph %d, not %d", ch, otf.unicodeToGID[ch], gid)
		}
	}
}

func TestReviewMediaAFontWithoutAnOS2TableHasItsCharacters(t *testing.T) {
	// The OS/2 table says the first and the last character of the font; a
	// font without one has every character of its character map, where it
	// had none and drew every character as .notdef.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	with := newOpenTypeFont(bytes.NewReader(font))
	without := newOpenTypeFont(bytes.NewReader(testOpenTypeWithout(t, font, "OS/2")))
	for _, ch := range []rune{'A', 'z', 'ก'} {
		if without.unicodeToGID[ch] == 0 || without.unicodeToGID[ch] != with.unicodeToGID[ch] {
			t.Errorf("%q is glyph %d, not %d", ch, without.unicodeToGID[ch], with.unicodeToGID[ch])
		}
	}
}

// testPDFAError returns the error of completing a document of the compliance
// that holds the image of the file.
func testPDFAError(t *testing.T, level compliance.Compliance, path string) string {
	t.Helper()
	data, err := os.ReadFile(testRepoPath(t, path))
	if err != nil {
		t.Fatal(err)
	}
	return testPDFAImageError(t, level, data)
}

// testPDFAImageError returns the error of completing a document of the
// compliance that holds the image.
func testPDFAImageError(t *testing.T, level compliance.Compliance, data []byte) string {
	t.Helper()
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	pdf.SetCompliance(level)
	pdf.SetTitle("Title")
	font := testTrueTypeFont(t, pdf)
	page := NewPage(pdf, letter.Portrait())
	NewTextLine(font, "Text").SetLocation(50, 50).DrawOn(page)
	NewImage(pdf, bytes.NewReader(data)).SetAltDescription("An image").
		SetLocation(50, 100).DrawOn(page)
	if err := pdf.Complete(); err != nil {
		return err.Error()
	}
	return ""
}

func TestReviewMediaAPDFADocumentHoldsNoImageItsLevelHasNot(t *testing.T) {
	// The output intent of PDF/A is sRGB, so its images are not CMYK; PDF/A-1
	// has no soft masks, and 8 bits per component at most.
	failed := "The PDF was not completed because of an earlier error: "
	testWant(t, failed+"A document of PDF_A_2B cannot hold a CMYK image: "+
		"its output intent is sRGB, so its images are gray or RGB.",
		testPDFAError(t, compliance.PDF_A_2B, "images/cmyk.jpg"))
	testWant(t, failed+"A document of PDF_A_1B cannot hold an image with transparency: "+
		"PDF/A-1 has no soft masks, so its images are opaque.",
		testPDFAError(t, compliance.PDF_A_1B, "PngSuite/BASN6A08.PNG"))
	testWant(t, failed+"A document of PDF_A_1A cannot hold an image of 16 bits per component: "+
		"PDF/A-1 has 8 at most.",
		testPDFAError(t, compliance.PDF_A_1A, "PngSuite/BASN2C16.PNG"))
	// PDF/A-2 and PDF/A-3 hold both, and a document that is not PDF/A all three.
	testWant(t, "", testPDFAError(t, compliance.PDF_A_2B, "PngSuite/BASN6A08.PNG"))
	testWant(t, "", testPDFAError(t, compliance.PDF_A_3B, "PngSuite/BASN2C16.PNG"))
	testWant(t, "", testPDFAError(t, compliance.PDF_UA_1, "images/cmyk.jpg"))
	testWant(t, "", testPDFAError(t, compliance.PDF_A_1B, "PngSuite/BASN2C08.PNG"))
}

func TestReviewMediaTheCommentsOfAStyleSheetAreLeftOutInOnePass(t *testing.T) {
	svg := `<svg width="10" height="10"><style>` + strings.Repeat("/**/", 40000) +
		`.a { fill: /* red */ blue } /* .a { fill: red } */ .b { fill: green } /* not closed .a { fill: red }` +
		`</style><rect class="a" width="5" height="5"/></svg>`
	start := time.Now()
	content := testDrawSVG(t, svg)
	if elapsed := time.Since(start); elapsed > 2*time.Second {
		t.Errorf("the style sheet is read in %v", elapsed)
	}
	if !strings.HasPrefix(content, "0 0 1 rg\n") {
		t.Errorf("content %q", content)
	}
}

func TestReviewMediaTheRulesOfTheClassesOfAnElementAreFoundByClass(t *testing.T) {
	// Rules and elements of three classes each: every element went through
	// every rule for each of its classes. Four times as many of both take
	// about four times the time, not sixteen; the two times are measured in
	// the same run, the shortest of three each, so that a busy computer slows
	// both (a limit of 2 s failed under load, 9 October 2026).
	small := testClassRulesTime(t, 5000)
	large := testClassRulesTime(t, 20000)
	if large > 8*small {
		t.Errorf("20000 rules and elements took %v, and 5000 %v", large, small)
	}
	// The rules are those of the style sheet, in its order, whatever the
	// order of the classes, and a class named twice has its rules once.
	content := testDrawSVG(t, `<svg width="10" height="10"><style>.b{fill:red} .a{fill:blue} .b{stroke:green}`+
		`</style><rect class="b a b" width="5" height="5"/></svg>`)
	if !strings.HasPrefix(content, "0 0 1 rg\n") || !strings.Contains(content, "0 0.5 0 RG\n") {
		t.Errorf("content %q", content)
	}
}

// testFirstControlPoints returns the first control points of the cubic
// curves of the path data, rounded to two decimals.
func testFirstControlPoints(t *testing.T, data string) string {
	t.Helper()
	operations, err := toPDF(newSVGParser().getOperations(data))
	if err != nil {
		t.Fatal(err)
	}
	points := make([]string, 0)
	for _, op := range operations {
		if op.cmd == 'C' {
			points = append(points, strconv.FormatFloat(float64(op.x1), 'f', 2, 32)+","+
				strconv.FormatFloat(float64(op.y1), 'f', 2, 32))
		}
	}
	return strings.Join(points, " ")
}

func TestReviewMediaASmoothCurveReflectsTheControlPointOfACurveOfItsKind(t *testing.T) {
	// T reflects the control point of the quadratic curve before, Q or T,
	// and else starts from the current point; S reflects the second control
	// point of the cubic curve before, C or S, and else starts from the
	// current point. The first control point of a cubic curve made of a
	// quadratic one is two thirds of the way to the quadratic control point.
	for _, c := range []struct{ data, want string }{
		// The second T reflects (15, -10), the control point of the first.
		{"M0 0 Q 5 10 10 0 T 20 0 T 30 0", "3.33,6.67 13.33,-6.67 23.33,6.67"},
		{"M0 0 Q 5 10 10 0 t 10 0 t 10 0", "3.33,6.67 13.33,-6.67 23.33,6.67"},
		{"M0 0 C 0 10 10 10 10 0 T 20 0", "0.00,10.00 10.00,0.00"},
		{"M0 0 Q 5 10 10 0 S 20 10 20 0", "3.33,6.67 10.00,0.00"},
		{"M0 0 C 0 10 10 10 10 0 S 20 -10 20 0", "0.00,10.00 10.00,-10.00"},
		{"M0 0 S 10 10 20 0 S 30 -10 40 0", "0.00,0.00 30.00,-10.00"},
	} {
		if got := testFirstControlPoints(t, c.data); got != c.want {
			t.Errorf("%q: %s, not %s", c.data, got, c.want)
		}
	}
}

func TestReviewMediaACommandAfterZStartsAtTheStartOfTheSubpathItClosed(t *testing.T) {
	// A line after Z, with no moveto, starts where the closed subpath did;
	// the path is stroked once, and filled with every subpath in place.
	content := testDrawSVG(t, `<svg width="100" height="100">`+
		`<path d="M10 10 L50 10 L50 50 Z L 90 90" fill="red" stroke="black"/></svg>`)
	want := "1 0 0 rg\n10 782 m\n50 782 l\n50 742 l\n10 782 m\n90 702 l\nf\n" +
		"0 0 0 RG\n1 w\n10 782 m\n50 782 l\n50 742 l\nh\n10 782 m\n90 702 l\nS\n"
	testWant(t, want, content)
}

func TestReviewMediaAnUnknownPNGFilterTypeIsRefused(t *testing.T) {
	// Filter types 0 to 4 are all PNG defines; libpng refuses a row of
	// another one, which was read as if it had no filter.
	testWant(t, "Invalid PNG filter type 5.", testPNGError(
		testPNG(2, 1, 8, 2, nil, compressor.Deflate([]byte{5, 1, 2, 3, 4, 5, 6}))))
}

func TestReviewMediaThePNGFiltersAreUndone(t *testing.T) {
	// Two rows of two RGB pixels, the second row with each filter, over a
	// first row of Sub.
	first := []byte{1, 10, 20, 30, 5, 5, 5}
	want := []byte{10, 20, 30, 15, 25, 35}
	for _, c := range []struct {
		row  []byte
		want []byte
	}{
		{[]byte{0, 1, 2, 3, 4, 5, 6}, []byte{1, 2, 3, 4, 5, 6}},
		{[]byte{1, 1, 2, 3, 4, 5, 6}, []byte{1, 2, 3, 5, 7, 9}},
		{[]byte{2, 1, 2, 3, 4, 5, 6}, []byte{11, 22, 33, 19, 30, 41}},
		// (left + above) / 2, with the left one of the first pixel 0.
		{[]byte{3, 1, 2, 3, 4, 5, 6}, []byte{6, 12, 18, 14, 23, 32}},
		// The first pixel takes the one above, as it is nearest; so does
		// the second, of left 6, above 15 and above on the left 10.
		{[]byte{4, 1, 2, 3, 4, 5, 6}, []byte{11, 22, 33, 19, 30, 41}},
	} {
		png := newPNGImage(bytes.NewReader(testPNG(2, 2, 8, 2, nil,
			compressor.Deflate(append(append([]byte(nil), first...), c.row...)))))
		got := testInflate(t, png.GetData())
		if !bytes.Equal(got, append(append([]byte(nil), want...), c.want...)) {
			t.Errorf("filter %d: %v", c.row[0], got)
		}
	}
}

func TestReviewMediaAPhysicalSizeOfMorePixelsThanAPNGNumberHoldsIsPassedOver(t *testing.T) {
	// The numbers of a PNG chunk are 2^31 - 1 at most; one past it drew the
	// image a thousandth of a point wide.
	for _, ppm := range []uint32{0x80000000, 0xFFFFFFFF} {
		png := newPNGImage(bytes.NewReader(testPNGWithPhys(8, 8, ppm, 4724, 1)))
		if png.physicalWidth != 0.0 || png.physicalHeight != 0.0 {
			t.Errorf("%d: physical size %v x %v", ppm, png.physicalWidth, png.physicalHeight)
		}
	}
}

func TestReviewMediaAJPEGAReaderCannotDecodeIsRefused(t *testing.T) {
	// A lossless, a hierarchical or an arithmetic coded JPEG is not one the
	// DCTDecode filter of a PDF reader decodes.
	for _, sof := range []byte{0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF} {
		jpeg := []byte{0xFF, 0xD8, 0xFF, sof, 0x00, 0x11, 8, 0, 8, 0, 8, 3,
			1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0, 0xFF, 0xD9}
		_, err := newJPGImage(bytes.NewReader(jpeg))
		want := "Error: The JPEG is lossless, hierarchical or arithmetic coded (SOF" +
			strconv.Itoa(int(sof-0xC0)) + "), which a PDF reader cannot decode."
		if err == nil || err.Error() != want {
			t.Errorf("SOF%d: %v", sof-0xC0, err)
		}
	}
	for _, sof := range []byte{0xC0, 0xC1, 0xC2} {
		jpeg := append([]byte{0xFF, 0xD8, 0xFF, sof, 0x00, 0x11, 8, 0, 8, 0, 8, 3,
			1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0}, testScan(3)...)
		if _, err := newJPGImage(bytes.NewReader(jpeg)); err != nil {
			t.Errorf("SOF%d: %v", sof-0xC0, err)
		}
	}
}

func TestReviewMediaTheImageObjectHasEveryPixelOfAWideImage(t *testing.T) {
	// A float32 holds every whole number only up to 2^24, and 2^24 + 1
	// pixels were written as 2^24.
	width := 1<<24 + 1
	png := testPNG(int32(width), 1, 1, 0, nil, compressor.Deflate(make([]byte, 1+(width+7)/8)))
	doc := testNewDoc()
	image := NewImage(doc.pdf, bytes.NewReader(png))
	NewPage(doc.pdf, letter.Portrait())
	image.SetLocation(0, 0)
	if raw := string(doc.complete()); !strings.Contains(raw, "/Width 16777217\n") {
		t.Errorf("the image object has not the width of the image")
	}
}

func TestReviewMediaTheLinkOfATurnedImageCoversItAsItIsDrawn(t *testing.T) {
	// The link covered the image as it is drawn unturned, which a quarter
	// turn makes as wide as it was tall.
	for _, degrees := range []int{0, 90, 180, 270} {
		doc := testNewDoc()
		page := NewPage(doc.pdf, letter.Portrait())
		image := NewImageFromFile(doc.pdf, testRepoPath(t, "images/GLA250.png"))
		image.SetRotation(degrees).SetURIAction("https://pdfjet.com").SetLocation(10, 20)
		image.DrawOn(page)
		raw := string(doc.complete())
		match := regexp.MustCompile(`/Rect \[([\d.]+) ([\d.]+) ([\d.]+) ([\d.]+)\]`).FindStringSubmatch(raw)
		if match == nil {
			t.Fatalf("%d: no link", degrees)
		}
		var rect [4]float64
		for i := range rect {
			rect[i], _ = strconv.ParseFloat(match[i+1], 64)
		}
		w, h := float64(image.GetWidth()), float64(image.GetHeight())
		if degrees == 90 || degrees == 270 {
			w, h = h, w
		}
		if rect[0] != 10 || math.Abs(rect[2]-rect[0]-w) > 0.01 || math.Abs(rect[3]-rect[1]-h) > 0.01 ||
			rect[3] != 792-20 {
			t.Errorf("%d: /Rect %v", degrees, rect)
		}
	}
}

func TestReviewMediaTextFitsInAnyWidthAtAFontSizeOfZero(t *testing.T) {
	// Text of no size has no width; the size divided the width.
	font := NewFontFromFile(testNewPDF(), testRepoPath(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf"))
	for _, f := range []*Font{testHelvetica(testNewPDF()), font} {
		f.SetSize(0)
		if got := f.GetFitChars("Hello", 10); got != 5 {
			t.Errorf("%d characters fit", got)
		}
		if got := f.GetFitChars("Hello", -1); got != 0 {
			t.Errorf("%d characters fit in no width", got)
		}
	}
}

func TestReviewMediaAFontIsEmbeddedOnceWhenItIsAddedTwice(t *testing.T) {
	doc := testNewDoc()
	path := testRepoPath(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	font1 := NewFontFromFile(doc.pdf, path)
	font2 := NewFontFromFile(doc.pdf, path)
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(font1, "A").SetLocation(50, 50).DrawOn(page)
	NewTextLine(font2, "B").SetLocation(50, 80).DrawOn(page)
	if raw := doc.complete(); bytes.Count(raw, []byte("/Length1 ")) != 1 {
		t.Errorf("the font is embedded %d times", bytes.Count(raw, []byte("/Length1 ")))
	}
}

// testClassRulesTime returns the shortest of three times an image of the
// rules and the elements of three classes each is read.
func testClassRulesTime(t *testing.T, count int) time.Duration {
	var sb strings.Builder
	sb.WriteString(`<svg width="10" height="10"><style>`)
	for i := 0; i < count; i++ {
		sb.WriteString(".c" + strconv.Itoa(i) + "{fill:red}")
	}
	sb.WriteString(`</style>` + strings.Repeat(`<g class="x y z"/>`, count) + `</svg>`)
	shortest := time.Duration(math.MaxInt64)
	for run := 0; run < 3; run++ {
		start := time.Now()
		testNewSVG(t, sb.String())
		shortest = min(shortest, time.Since(start))
	}
	return shortest
}
