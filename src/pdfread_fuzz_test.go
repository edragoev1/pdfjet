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
	"os"
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
// from then on.

// fuzzReadPDF reads the bytes as a PDF and uses what it read as a merge does,
// and returns what the ports are compared on: the objects, the pages and what
// each page draws.
func fuzzReadPDF(data []byte, password string) string {
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	objects, err := pdf.ReadWithPassword(data, password)
	if err != nil {
		return "error"
	}
	var report strings.Builder
	fmt.Fprintf(&report, "objects=%d", len(objects))
	pages := NewPDF(bufio.NewWriter(io.Discard)).GetPageObjects(objects)
	fmt.Fprintf(&report, " pages=%d", len(pages))
	for _, page := range pages {
		content := page.GetContentObject(objects)
		if content == nil {
			report.WriteString(" page=none")
			continue
		}
		fmt.Fprintf(&report, " page=%s", fuzzDigest(content.GetData()))
	}
	// The merge writes the objects that were read into a document of its own.
	merged := NewPDF(bufio.NewWriter(io.Discard))
	if err := merged.Merge(objects); err != nil {
		fmt.Fprintf(&report, " merge=error")
	} else if err := merged.Complete(); err != nil {
		fmt.Fprintf(&report, " merge=error")
	} else {
		report.WriteString(" merge=ok")
	}
	return report.String()
}

// fuzzPDFSeeds are documents that PDFjet writes, the PDFs of the repository
// that other programs wrote, and one with a cross-reference stream and an
// object stream, which PDFjet reads and does not write.
func fuzzPDFSeeds(f *testing.F) [][]byte {
	seeds := [][]byte{fuzzPDFDocument(f, ""), fuzzPDFDocument(f, "secret"), fuzzPDFStreamXref()}
	for _, name := range []string{"scanned-linearized.pdf", "rc65-16e.pdf", "pdfjet-5.81-logo.pdf"} {
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

// FuzzPDFRead fuzzes the bytes of a PDF and the password it is read with.
func FuzzPDFRead(f *testing.F) {
	for _, seed := range fuzzPDFSeeds(f) {
		f.Add(seed, "")
		f.Add(seed, "secret")
		f.Add(seed[:len(seed)/2], "")
	}
	f.Add([]byte("%PDF-1.4\n"), "")
	f.Add([]byte{}, "")
	f.Fuzz(func(t *testing.T, data []byte, password string) {
		fuzzRun(t, len(data), func() {
			fuzzReadPDF(data, password)
		})
	})
}
