// helpers_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/hex"
	"fmt"
	"math"
	"os"
	"path/filepath"
	"strings"
	"testing"
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

// testRepoPath returns the path of a file of the repository: the tests run in
// their package directory, so the root is the first directory above it that
// has the PngSuite directory.
func testRepoPath(t *testing.T, path string) string {
	t.Helper()
	dir, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	for {
		if info, err := os.Stat(filepath.Join(dir, "PngSuite")); err == nil && info.IsDir() {
			return filepath.Join(dir, path)
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			t.Fatal("no PngSuite directory above the working directory")
		}
		dir = parent
	}
}

// testContent returns the content stream of the page, which is not compressed yet.
func testContent(page *Page) string {
	return string(page.GetContent())
}

// testHex returns ASCII text as a core font draws it: upper case hexadecimal.
func testHex(text string) string {
	return fmt.Sprintf("%X", []byte(text))
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
