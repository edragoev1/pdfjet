// pdfread_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"fmt"
	"io"
	"io/fs"
	"os"
	"path/filepath"
	"runtime"
	"runtime/debug"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/internal/compressor"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz target of the reader: the cross-reference table and stream, the
// objects, the object streams and the encryption of a PDF that PDFjet reads,
// merges and splits. Any input either reads or fails with an error: an index
// out of range, a hang or gigabytes of memory is a bug. go test runs the
// seeds, and
//
//	go test ./src -run '^$' -fuzz '^FuzzPDFRead$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on. The PDFs of the corpora that tests/corpus fetches are seeds too
// when PDFJET_FUZZ_CORPUS names their folder:
//
//	PDFJET_FUZZ_CORPUS=../.corpora go test ./src -run '^$' -fuzz '^FuzzPDFRead$'
//
// They are fetched and not committed, as their copyright is many people's, so
// only an input the fuzzing makes of them, cut down to what fails, is kept.

// fuzzSafe runs the function and reports what it did. A panic of PDFjet's
// own is the error of a port that panics where the others throw, and is what
// the input did; a Go runtime error -- an index out of range, a nil pointer
// or a division by zero -- is a bug of the reader, and fails the test where
// the ports that do not recover from it would crash.
func fuzzSafe(t *testing.T, what string, fn func()) (failed bool) {
	defer func() {
		if r := recover(); r != nil {
			if err, ok := r.(runtime.Error); ok {
				t.Fatalf("%s: runtime error: %v\n%s", what, err, debug.Stack())
			}
			failed = true
		}
	}()
	fn()
	return false
}

// fuzzCheckError fails the test when reading ended in a Go runtime error,
// which ReadWithPassword recovers from and returns as an ordinary error: the
// other three ports do not recover, so the same input crashes there.
func fuzzCheckError(t *testing.T, what string, err error) {
	if _, ok := err.(runtime.Error); ok {
		t.Fatalf("%s: runtime error: %v", what, err)
	}
}

// fuzzReadPDF reads the bytes as a PDF and uses what it read as a merge, a
// split and a stamp do, and returns what the ports are compared on: the
// objects, the pages, the size of each page and what each page draws.
func fuzzReadPDF(t *testing.T, data []byte, password string) string {
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	objects, err := pdf.ReadWithPassword(data, password)
	if err != nil {
		fuzzCheckError(t, "read", err)
		return "error"
	}
	var report strings.Builder
	fmt.Fprintf(&report, "objects=%d", len(objects))
	pages := NewPDF(bufio.NewWriter(io.Discard)).GetPageObjects(objects)
	fmt.Fprintf(&report, " pages=%d", len(pages))
	for _, page := range pages {
		if fuzzSafe(t, "getPageSize", func() {
			size := page.GetPageSize()
			fmt.Fprintf(&report, " size=%gx%g", size.GetWidth(), size.GetHeight())
		}) {
			report.WriteString(" size=error")
		}
		if fuzzSafe(t, "getResourcesObject", func() {
			page.GetResourcesObject(objects)
		}) {
			report.WriteString(" resources=error")
		}
		content := page.GetContentObject(objects)
		if content == nil {
			report.WriteString(" page=none")
			continue
		}
		fmt.Fprintf(&report, " page=%s", fuzzDigest(content.GetData()))
	}
	// The merge writes the objects that were read into a document of its own.
	merged := NewPDF(bufio.NewWriter(io.Discard))
	if fuzzSafe(t, "merge", func() {
		if err := merged.Merge(objects); err != nil {
			report.WriteString(" merge=error")
		} else if err := merged.Complete(); err != nil {
			report.WriteString(" merge=error")
		} else {
			report.WriteString(" merge=ok")
		}
	}) {
		report.WriteString(" merge=error")
	}
	// The split merges one page of what was read into a document of its own.
	if len(pages) > 0 {
		split := NewPDF(bufio.NewWriter(io.Discard))
		if fuzzSafe(t, "split", func() {
			if err := split.MergePages(objects, 1); err != nil {
				report.WriteString(" split=error")
			} else if err := split.Complete(); err != nil {
				report.WriteString(" split=error")
			} else {
				report.WriteString(" split=ok")
			}
		}) {
			report.WriteString(" split=error")
		}
	}
	// The stamp writes the objects that were read as they are, with the fonts
	// and the images of their pages, and draws on a page of them.
	stamp := NewPDF(bufio.NewWriter(io.Discard))
	if fuzzSafe(t, "stamp", func() {
		stamp.AddResourceObjects(objects)
		if err := stamp.AddObjects(objects); err != nil {
			report.WriteString(" stamp=error")
			return
		}
		if len(pages) > 0 {
			font := pages[0].AddCoreFontResource(corefont.Helvetica(), &objects)
			pages[0].AddContent([]byte("BT /F1 12 Tf 50 50 Td (x) Tj ET\n"), &objects)
			pages[0].SetGraphicsState(NewGraphicsState(), &objects)
			_ = font
		}
		report.WriteString(" stamp=ok")
	}) {
		report.WriteString(" stamp=error")
	}
	return report.String()
}

// fuzzPDFSeeds are documents that PDFjet writes, the PDFs of the repository
// that other programs wrote, and one with a cross-reference stream and an
// object stream, which PDFjet reads and does not write.
func fuzzPDFSeeds(f *testing.F) [][]byte {
	seeds := [][]byte{fuzzPDFDocument(f, ""), fuzzPDFDocument(f, "secret"), fuzzPDFStreamXref()}
	for _, name := range []string{"scanned-linearized.pdf", "rc65-16e.pdf", "PDFjetLogo.pdf"} {
		data, err := os.ReadFile("../data/testPDFs/" + name)
		if err != nil {
			f.Fatal(err)
		}
		seeds = append(seeds, data)
	}
	return seeds
}

// fuzzPDFDocument returns a document of two pages, encrypted with the
// password when there is one.
func fuzzPDFDocument(f *testing.F, password string) []byte {
	var buf bytes.Buffer
	pdf := NewPDF(bufio.NewWriter(&buf))
	if password != "" {
		enc, err := NewEncryption(pdf,
			encryption.NewPasswords().SetUserPassword(password), encryption.NewPermissions())
		if err != nil {
			f.Fatal(err)
		}
		pdf.SetEncryption(enc)
	}
	font := NewCoreFont(pdf, corefont.Helvetica())
	for _, text := range []string{"Page one", "Page two"} {
		NewTextLine(font, text).SetLocation(50, 50).DrawOn(NewPage(pdf, letter.Portrait()))
	}
	if err := pdf.Complete(); err != nil {
		f.Fatal(err)
	}
	return buf.Bytes()
}

// fuzzPDFStreamXref returns a document whose objects are in an object stream
// and whose cross-reference is a stream, as a PDF of version 1.5 and later
// may have them and PDFjet does not write them.
func fuzzPDFStreamXref() []byte {
	// The catalog, the page tree and the page, in an object stream.
	inner := []string{
		"<</Type/Catalog/Pages 2 0 R>>",
		"<</Type/Pages/Kids[3 0 R]/Count 1>>",
		"<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 5 0 R>>",
	}
	var pairs, bodies strings.Builder
	for i, object := range inner {
		fmt.Fprintf(&pairs, "%d %d ", i+1, bodies.Len())
		bodies.WriteString(object)
		bodies.WriteString(" ")
	}
	first := len(pairs.String())
	objStm := compressor.Deflate([]byte(pairs.String() + bodies.String()))
	content := compressor.Deflate([]byte("BT /F1 12 Tf 50 700 Td (Hello) Tj ET\n"))

	var pdf bytes.Buffer
	offsets := make([]int, 7)
	pdf.WriteString("%PDF-1.5\n")
	offsets[4] = pdf.Len()
	fmt.Fprintf(&pdf, "4 0 obj\n<</Type/ObjStm/N %d/First %d/Length %d/Filter/FlateDecode>>\nstream\n",
		len(inner), first, len(objStm))
	pdf.Write(objStm)
	pdf.WriteString("\nendstream\nendobj\n")
	offsets[5] = pdf.Len()
	fmt.Fprintf(&pdf, "5 0 obj\n<</Length %d/Filter/FlateDecode>>\nstream\n", len(content))
	pdf.Write(content)
	pdf.WriteString("\nendstream\nendobj\n")

	// The cross-reference stream: one byte of type, two of the offset or the
	// object stream it is in, and one of the index in it.
	xrefOffset := pdf.Len()
	var xref bytes.Buffer
	xref.Write([]byte{0, 0, 0, 255}) // Object 0, the free one
	for i := 1; i <= 3; i++ {
		xref.Write([]byte{2, 0, 4, byte(i - 1)}) // In the object stream 4
	}
	for _, number := range []int{4, 5, 6} {
		xref.Write([]byte{1, byte(offsets[number] >> 8), byte(offsets[number]), 0})
	}
	xrefData := compressor.Deflate(xref.Bytes())
	offsets[6] = xrefOffset
	fmt.Fprintf(&pdf, "6 0 obj\n<</Type/XRef/Size 7/W[1 2 1]/Root 1 0 R/Length %d/Filter/FlateDecode>>\nstream\n",
		len(xrefData))
	pdf.Write(xrefData)
	pdf.WriteString("\nendstream\nendobj\n")
	fmt.Fprintf(&pdf, "startxref\n%d\n%%%%EOF\n", xrefOffset)
	return pdf.Bytes()
}

// fuzzMaxCorpusSeed is the size of the largest PDF of the corpora that is a
// seed: the fuzzing changes a few bytes of a seed at a time, and a large one
// takes long to read on each run for what a small one finds as well.
const fuzzMaxCorpusSeed = 256 * 1024

// fuzzCorpusSeeds returns the PDFs of the folder PDFJET_FUZZ_CORPUS names, the
// ones of at most fuzzMaxCorpusSeed bytes, or none when it names no folder.
func fuzzCorpusSeeds(f *testing.F) [][]byte {
	dir := os.Getenv("PDFJET_FUZZ_CORPUS")
	if dir == "" {
		return nil
	}
	seeds := [][]byte{}
	err := filepath.WalkDir(dir, func(path string, entry fs.DirEntry, err error) error {
		if err != nil || entry.IsDir() || !strings.EqualFold(filepath.Ext(path), ".pdf") {
			return err
		}
		if info, err := entry.Info(); err != nil || info.Size() > fuzzMaxCorpusSeed {
			return err
		}
		data, err := os.ReadFile(path)
		if err == nil {
			seeds = append(seeds, data)
		}
		return err
	})
	if err != nil {
		f.Fatal(err)
	}
	return seeds
}

// FuzzPDFRead fuzzes the bytes of a PDF and the password it is read with.
func FuzzPDFRead(f *testing.F) {
	for _, seed := range fuzzPDFSeeds(f) {
		f.Add(seed, "")
		f.Add(seed, "secret")
		f.Add(seed[:len(seed)/2], "")
	}
	f.Add([]byte("%PDF-1.4\n"), "")
	f.Add([]byte{}, "")
	for _, seed := range fuzzCorpusSeeds(f) {
		f.Add(seed, "")
	}
	f.Fuzz(func(t *testing.T, data []byte, password string) {
		fuzzRun(t, len(data), func() {
			fuzzReadPDF(t, data, password)
		})
	})
}
