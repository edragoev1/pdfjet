// helpers_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"encoding/hex"
	"fmt"
	"math"
	"os"
	"path/filepath"
	"slices"
	"strconv"
	"strings"
	"testing"
	"time"
	"unicode/utf16"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
)

// The helpers of the unit tests, which mirror tests/java. Their names start
// with test so that they do not clash with the names of the package.

// testDelta is the tolerance for coordinates and widths, a hundredth of a point.
const testDelta = 0.01

// testDoc is a PDF written to memory.
type testDoc struct {
	pdf    *PDF
	buf    *bytes.Buffer
	writer *bufio.Writer
}

func testNewDoc() *testDoc {
	buf := new(bytes.Buffer)
	writer := bufio.NewWriter(buf)
	return &testDoc{pdf: NewPDF(writer), buf: buf, writer: writer}
}

// complete completes the PDF and returns its bytes.
func (doc *testDoc) complete() []byte {
	if err := doc.pdf.Complete(); err != nil {
		panic(err)
	}
	if err := doc.writer.Flush(); err != nil {
		panic(err)
	}
	return doc.buf.Bytes()
}

func testNewPDF() *PDF {
	return testNewDoc().pdf
}

func testNewPage() *Page {
	return NewPage(testNewPDF(), letter.Portrait())
}

func testHelvetica(pdf *PDF) *Font {
	return NewCoreFont(pdf, corefont.Helvetica())
}

// testAllowCoreFonts lets the test draw in a core font in a document of PDF/A
// or PDF/UA, which Complete refuses: for the tests of what such a document
// tags, which find the text of a core font by its encoding.
func testAllowCoreFonts(t *testing.T) {
	coreFontsInCompliance = true
	t.Cleanup(func() { coreFontsInCompliance = false })
}

// testPlexSans returns IBM Plex Sans, a font that is embedded, as a document
// of PDF/A or PDF/UA needs: a core font is refused in one (review of
// 11 October 2026, #6).
func testPlexSans(pdf *PDF) *Font {
	dir, err := os.Getwd()
	if err != nil {
		panic(err)
	}
	for {
		if info, err := os.Stat(filepath.Join(dir, "tests/data/PngSuite")); err == nil && info.IsDir() {
			return NewFontFromFile(pdf, filepath.Join(dir, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			panic("the repository is not above " + dir)
		}
		dir = parent
	}
}

// testRepoPath returns the path of a file of the repository: the tests run in
// their package directory, so the root is the first directory above it that
// has the tests/data/PngSuite directory.
func testRepoPath(t *testing.T, path string) string {
	t.Helper()
	dir, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	for {
		if info, err := os.Stat(filepath.Join(dir, "tests/data/PngSuite")); err == nil && info.IsDir() {
			return filepath.Join(dir, path)
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			t.Fatal("no tests/data/PngSuite directory above the working directory")
		}
		dir = parent
	}
}

// testFontBytes returns the bytes of a file of the repository, or, for a
// path under fonts that is not a file, of the font PDFjet carries, as
// NewFontFromFile reads it: for the tests that read or patch a font file
// itself.
func testFontBytes(t *testing.T, path string) []byte {
	t.Helper()
	buf, err := readFontFile(testRepoPath(t, path))
	if err != nil {
		t.Fatal(err)
	}
	return buf
}

// testWithSharedGlyphs returns the font with its character map rewritten so
// that the second character of each pair is drawn with the glyph of the
// first, as Noto Sans Arabic draws ١ and ۱, and ل and its isolated form ﻝ,
// with one glyph, which no font PDFjet carries does in Arabic. The map is a
// format 12 subtable of the Windows platform, of every character of the font
// to its glyph, put at the end of the font in place of its cmap table, which
// PDFjet reads as it reads any font's.
func testWithSharedGlyphs(t *testing.T, font []byte, pairs [][2]rune) []byte {
	t.Helper()
	glyphs := make(map[rune]int)
	newOpenTypeFont(font).unicodeToGID.each(func(c rune, gid int) {
		glyphs[c] = gid
	})
	for _, pair := range pairs {
		gid, ok := glyphs[pair[0]]
		if !ok {
			t.Fatalf("the font has no U+%04X", pair[0])
		}
		glyphs[pair[1]] = gid
	}
	chars := make([]rune, 0, len(glyphs))
	for c := range glyphs {
		chars = append(chars, c)
	}
	slices.Sort(chars)
	cmap := make([]byte, 0, 12+16+12*len(chars))
	cmap = binary.BigEndian.AppendUint16(cmap, 0)  // The version
	cmap = binary.BigEndian.AppendUint16(cmap, 1)  // One record
	cmap = binary.BigEndian.AppendUint16(cmap, 3)  // Windows
	cmap = binary.BigEndian.AppendUint16(cmap, 10) // Unicode full
	cmap = binary.BigEndian.AppendUint32(cmap, 12)
	cmap = binary.BigEndian.AppendUint16(cmap, 12)
	cmap = binary.BigEndian.AppendUint16(cmap, 0)
	cmap = binary.BigEndian.AppendUint32(cmap, uint32(16+12*len(chars)))
	cmap = binary.BigEndian.AppendUint32(cmap, 0)
	cmap = binary.BigEndian.AppendUint32(cmap, uint32(len(chars)))
	for _, c := range chars {
		cmap = binary.BigEndian.AppendUint32(cmap, uint32(c))
		cmap = binary.BigEndian.AppendUint32(cmap, uint32(c))
		cmap = binary.BigEndian.AppendUint32(cmap, uint32(glyphs[c]))
	}
	patched := append(append([]byte(nil), font...), cmap...)
	tables := int(binary.BigEndian.Uint16(font[4:]))
	for i := 0; i < tables; i++ {
		entry := 12 + 16*i
		if string(font[entry:entry+4]) == "cmap" {
			binary.BigEndian.PutUint32(patched[entry+8:], uint32(len(font)))
			binary.BigEndian.PutUint32(patched[entry+12:], uint32(len(cmap)))
			return patched
		}
	}
	t.Fatal("the font has no cmap table")
	return nil
}

// testContent returns the content stream of the page, which is not compressed yet.
func testContent(page *Page) string {
	return string(page.GetContent())
}

// testHex returns ASCII text as a core font draws it: upper case hexadecimal.
func testHex(text string) string {
	return fmt.Sprintf("%X", []byte(text))
}

// testFillColorBefore returns the fill color set last before the text, drawn
// in a core font, as its operator, for example "1 1 1 rg".
func testFillColorBefore(content, text string) string {
	i := strings.Index(content, "<"+testHex(text)+">")
	if i == -1 {
		return ""
	}
	rg := strings.LastIndex(content[:i], " rg\n")
	if rg == -1 {
		return ""
	}
	return content[strings.LastIndex(content[:rg], "\n")+1 : rg+3]
}

// testPositionOf returns the x and the y of the Td before the first text,
// drawn in a core font, that starts with the text.
func testPositionOf(t *testing.T, content, text string) [2]float32 {
	t.Helper()
	i := strings.Index(content, "<"+testHex(text))
	if i == -1 {
		t.Fatalf("%q is not drawn:\n%s", text, content)
	}
	td := strings.LastIndex(content[:i], " Td\n")
	parts := strings.Split(content[strings.LastIndex(content[:td], "\n")+1:td], " ")
	x, _ := strconv.ParseFloat(parts[0], 32)
	y, _ := strconv.ParseFloat(parts[1], 32)
	return [2]float32{float32(x), float32(y)}
}

// testUTF16Hex decodes a PDF text string written as a hexadecimal string with
// a UTF-16BE byte order mark.
func testUTF16Hex(t *testing.T, value string) string {
	t.Helper()
	digits := strings.NewReplacer("<", "", ">", "", " ", "", "\n", "").Replace(value)
	b, err := hex.DecodeString(digits)
	if err != nil {
		t.Fatalf("%q: %v", value, err)
	}
	units := make([]uint16, len(b)/2)
	for i := range units {
		units[i] = uint16(b[2*i])<<8 | uint16(b[2*i+1])
	}
	return strings.TrimPrefix(string(utf16.Decode(units)), "\uFEFF")
}

func testRead(t *testing.T, pdf []byte) []*PDFobj {
	t.Helper()
	objects, err := testNewPDF().Read(pdf)
	if err != nil {
		t.Fatal(err)
	}
	return objects
}

// testTrailerID returns the first /ID of the trailer.
func testTrailerID(pdf []byte) string {
	raw := string(pdf)
	start := strings.LastIndex(raw, "/ID[<") + 5
	return raw[start : start+strings.Index(raw[start:], ">")]
}

// testFindObject returns the object that holds the key, or nil.
func testFindObject(objects []*PDFobj, key string) *PDFobj {
	for _, obj := range objects {
		if obj.GetValue(key) != "" {
			return obj
		}
	}
	return nil
}

func testNear(t *testing.T, name string, want, got, delta float32) {
	t.Helper()
	if math.Abs(float64(want-got)) > float64(delta) {
		t.Errorf("%s: want %v, got %v", name, want, got)
	}
}

func testAssertXY(t *testing.T, x, y float32, xy [2]float32) {
	t.Helper()
	testNear(t, "x", x, xy[0], testDelta)
	testNear(t, "y", y, xy[1], testDelta)
}

func testAssertRGB(t *testing.T, r, g, b float32, rgb [3]float32) {
	t.Helper()
	testNear(t, "red", r, rgb[0], 0.0001)
	testNear(t, "green", g, rgb[1], 0.0001)
	testNear(t, "blue", b, rgb[2], 0.0001)
}

// testPanic calls the function and returns the value it panics with as a
// string, and whether it panicked: where Java throws, Go panics.
func testPanic(fn func()) (message string, panicked bool) {
	defer func() {
		if r := recover(); r != nil {
			message = fmt.Sprint(r)
			panicked = true
		}
	}()
	fn()
	return "", false
}

func testLetterPortrait() pagesize.PageSize {
	return letter.Portrait()
}

// testShortest returns the time the work takes, the shortest of up to three
// runs: a run over the limit is run again, as a machine under load can make a
// linear run slow once, where a quadratic one is slow every time (review of
// 12 October 2026, C6).
func testShortest(limit time.Duration, work func()) time.Duration {
	return fuzzShortest(limit, work)
}

// testLinearTime checks that the work of size 2n takes less than 3.8 times
// the work of size n, each the shortest of three runs: the time of a linear
// algorithm doubles, and that of a quadratic one, whose time dominates at n,
// comes near four times. A ratio holds on a busy computer, where limits in
// seconds failed (CI, 30 October 2026); the sizes are timed in turns, and all
// again up to three times, as load can fall on one of them only, where a
// quadratic algorithm fails every time.
func testLinearTime(t *testing.T, n int, work func(n int)) {
	t.Helper()
	var small, large time.Duration
	for attempt := 0; attempt < 3; attempt++ {
		small, large = math.MaxInt64, math.MaxInt64
		for run := 0; run < 3; run++ {
			start := time.Now()
			work(n)
			small = min(small, time.Since(start))
			start = time.Now()
			work(2 * n)
			large = min(large, time.Since(start))
		}
		if 5*large < 19*small {
			return
		}
	}
	t.Errorf("size %d took %v, and size %d %v", 2*n, large, n, small)
}
